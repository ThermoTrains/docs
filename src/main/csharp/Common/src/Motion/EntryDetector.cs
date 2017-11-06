using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Emgu.CV;
using Emgu.CV.Structure;
using log4net;

namespace SebastianHaeni.ThermoBox.Common.Motion
{
    public class EntryDetector
    {
        public event EventHandler TrainEnter;
        public event EventHandler TrainExit;
        public DetectorState CurrentState { get; set; } = DetectorState.Nothing;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const int NoBoundingBoxThreshold = 100;
        private const int MinTimeAfterExit = 30;
        private const int MinTimeAfterEntry = 10;
        private const int MaxEntryDuration = 120;

        private MotionFinder<byte> _motionFinder;
        private int _noBoundingBoxCount;
        private int _foundNothingCount;
        private DateTime _entryDateTime = DateTime.MinValue;
        private DateTime _exitDateTime = DateTime.MinValue;
        private Image<Gray, byte>[] _images;

        public EntryDetector()
        {
        }

        public EntryDetector(Image<Gray, byte> background)
        {
            UpdateMotionFinder(background);
        }

        private void OnTrainEnter()
        {
            if (DateTime.Now.Subtract(TimeSpan.FromSeconds(MinTimeAfterExit)) < _exitDateTime)
            {
                // It has not been long enough since the last exit.
                return;
            }

            CurrentState = DetectorState.Entry;
            _entryDateTime = DateTime.Now;
            _foundNothingCount = 0;
            TrainEnter?.Invoke(this, new EventArgs());
        }

        private void OnTrainExit()
        {
            if (DateTime.Now.Subtract(TimeSpan.FromSeconds(MinTimeAfterEntry)) < _entryDateTime)
            {
                // It has not been long enough since the entry.
                return;
            }

            CurrentState = DetectorState.Exit;
            _exitDateTime = DateTime.Now;
            TrainExit?.Invoke(this, new EventArgs());
        }

        private void OnNothing()
        {
            CurrentState = DetectorState.Nothing;
            _foundNothingCount++;

            if (_foundNothingCount > NoBoundingBoxThreshold)
            {
                UpdateMotionFinder(_images.First());
            }
        }

        public void Tick(Image<Gray, byte>[] images)
        {
            if (CurrentState == DetectorState.Entry &&
                DateTime.Now.Subtract(TimeSpan.FromSeconds(MaxEntryDuration)) > _exitDateTime)
            {
                // We are in enter mode for quite long now, we should abort.
                OnTrainExit();
                return;
            }

            _images = images;
            if (_motionFinder == null)
            {
                UpdateMotionFinder(_images.First());
            }

            var threshold = new Gray(20.0);
            var maxValue = new Gray(byte.MaxValue);

            var boundingBoxes = _images
                .Select(image => _motionFinder.FindBoundingBox(image, threshold, maxValue))
                .Where(box => box.HasValue)
                .Select(box => box.Value)
                .ToArray();

            Evaluate(boundingBoxes);

            // After some time we need to use a new background.
            // We do this if either no bounding box was found n times or if nothing was the result n times.

            if (boundingBoxes.Any())
            {
                _noBoundingBoxCount = 0;
                return;
            }

            _noBoundingBoxCount++;

            if (_noBoundingBoxCount > NoBoundingBoxThreshold)
            {
                UpdateMotionFinder(_images.First());
            }
        }

        private void Evaluate(Rectangle[] boundingBoxes)
        {
            // Not found anything useful. 
            if (!boundingBoxes.Any())
            {
                return;
            }

            var first = boundingBoxes.First();

            var threshold = _motionFinder.Background.Size.Width / 100;
            var leftBound = first.X < threshold;
            var rightBound = first.X + first.Width > _motionFinder.Background.Width - threshold;

            if (!leftBound && !rightBound)
            {
                return;
            }

            var lastWidth = first.Width;

            var indicator = 0;

            // Count if the box is thinning or widening.
            foreach (var box in boundingBoxes)
            {
                if (box.Width > lastWidth)
                {
                    indicator++;
                }
                else if (box.Width < lastWidth)
                {
                    indicator--;
                }

                lastWidth = box.Width;
            }

            // The count of images that indicates consistent width change.
            // The first image represents the start, so we cannot count that.
            var referenceCount = boundingBoxes.Count() - 1;

            // Entry
            if (indicator == referenceCount)
            {
                ChangeState(DetectorState.Entry);
                return;
            }

            // Exit
            if (indicator == -referenceCount)
            {
                ChangeState(DetectorState.Exit);
                return;
            }

            // Nothing
            ChangeState(DetectorState.Nothing);
        }

        private void ChangeState(DetectorState state)
        {
            var newState = CurrentState.GetStates().Contains(state) ? state : CurrentState;

            if (newState == CurrentState)
            {
                return;
            }

            switch (newState)
            {
                case DetectorState.Entry:
                    OnTrainEnter();
                    return;
                case DetectorState.Exit:
                    OnTrainExit();
                    return;
                case DetectorState.Nothing:
                    OnNothing();
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateMotionFinder(Image<Gray, byte> background)
        {
            if (CurrentState == DetectorState.Entry)
            {
                // do not update background as long as something has entered and not exited yet
                return;
            }

            Log.Debug("(Re)initializing background");
            _motionFinder = new MotionFinder<byte>(background);
            _noBoundingBoxCount = 0;
            _foundNothingCount = 0;
        }
    }
}
