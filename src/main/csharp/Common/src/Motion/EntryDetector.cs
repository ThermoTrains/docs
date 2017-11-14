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
        public event EventHandler Enter;
        public event EventHandler Exit;
        public event EventHandler Abort;
        public event EventHandler Pause;
        public event EventHandler Resume;

        public DetectorState CurrentState { get; set; } = DetectorState.Nothing;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Once this threshold is reached, the background will be reinitailized.
        /// </summary>
        private const int NoBoundingBoxBackgroundThreshold = 100;

        /// <summary>
        /// Once this threshold is reached, the recording will be resumed.
        /// After we find bounding boxes again, we resume.
        /// </summary>
        private const int NoMotionPauseThreshold = 12;

        /// <summary>
        /// The minimum time that has to pass after a train exited the image.
        /// </summary>
        private const int MinTimeAfterExit = 30;

        /// <summary>
        /// Tge minimum time that has to pass after the train entered. Otherwise an abort will be published.
        /// </summary>
        private const int MinTimeAfterEntry = 10;

        /// <summary>
        /// Maximum time a recording can be. After it has passed, the recording will be stopped.
        /// </summary>
        private const int MaxEntryDuration = 120;

        private MotionFinder<byte> _motionFinder;

        private int _noBoundingBoxCount;
        private int _foundNothingCount;
        private int _noMotionCount;

        private DateTime _entryDateTime = DateTime.MinValue;
        private DateTime _exitDateTime = DateTime.MinValue;
        private Image<Gray, byte>[] _images;

        private bool _paused;

        public EntryDetector()
        {
            // constructor with no background image, background will be initialized lazily
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
            Enter?.Invoke(this, new EventArgs());
        }

        private void OnTrainExit()
        {
            CurrentState = DetectorState.Exit;
            _exitDateTime = DateTime.Now;

            if (DateTime.Now.Subtract(TimeSpan.FromSeconds(MinTimeAfterEntry)) < _entryDateTime)
            {
                // It has not been long enough since the entry. So this probably was a misfire.
                Log.Warn($"Entry followed by exit was shorter than {MinTimeAfterEntry}s. Aborting.");
                Abort?.Invoke(this, new EventArgs());
            }
            else
            {
                // Train properly exited.
                Exit?.Invoke(this, new EventArgs());
            }
        }

        private void OnNothing()
        {
            CurrentState = DetectorState.Nothing;
            _foundNothingCount++;

            if (_foundNothingCount > NoBoundingBoxBackgroundThreshold)
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
                Log.Warn($"Recording for longer than {MaxEntryDuration}s.");
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

                if (CurrentState != DetectorState.Entry)
                {
                    return;
                }

                if (!_motionFinder.HasDifference(_images.First(), _images.Last(), threshold, maxValue))
                {
                    // The train (or whatever) is covering the whole image and it's not moving
                    _noMotionCount++;
                }
                else if (_paused)
                {
                    // We were paused => resume since we have found some moving things again.
                    Resume?.Invoke(this, new EventArgs());
                    _paused = false;
                }

                if (_noMotionCount > NoMotionPauseThreshold)
                {
                    _noMotionCount = 0;
                    // Not found moving things for a while => pause until we find movement again.
                    Pause?.Invoke(this, new EventArgs());
                    _paused = true;
                }

                return;
            }

            _noBoundingBoxCount++;

            if (_noBoundingBoxCount > NoBoundingBoxBackgroundThreshold)
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
