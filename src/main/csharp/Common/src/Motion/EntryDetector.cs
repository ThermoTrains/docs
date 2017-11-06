using System.Collections.Generic;
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
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const int NoBoundingBoxThreshold = 10;

        private int _noBoundingBoxCount;
        private MotionFinder<byte> _motionFinder;
        private int _foundNothingCount;

        public EntryDetector()
        {
            // nop
        }

        public EntryDetector(Image<Gray, byte> background)
        {
            UpdateMotionFinder(background);
        }

        public DetectorState Detect(IEnumerable<Image<Gray, byte>> images, DetectorState current)
        {
            var imageArray = images as Image<Gray, byte>[] ?? images.ToArray();

            if (_motionFinder == null)
            {
                UpdateMotionFinder(imageArray.First());
            }

            var evaluator = current.GetEvaluator(imageArray.First().Size);

            var threshold = new Gray(20.0);
            var maxValue = new Gray(255.0);

            var boundingBoxes = imageArray
                .Select(image => _motionFinder.FindBoundingBox(image, threshold, maxValue))
                .Where(box => box.HasValue)
                .Select(box => box.Value);

            var evaluatorBoundingBoxes = boundingBoxes as Rectangle[] ?? boundingBoxes.ToArray();

            evaluator.BoundingBoxes = evaluatorBoundingBoxes;

            // After some time we need to use a new background.
            // We do this if either no bounding box was found n times or if nothing was the result n times.

            if (evaluatorBoundingBoxes.Any())
            {
                _noBoundingBoxCount = 0;

                var result = evaluator.Evaluate();

                if (result != DetectorState.Nothing)
                {
                    return evaluator.Evaluate();
                }

                _foundNothingCount++;

                if (_foundNothingCount <= NoBoundingBoxThreshold)
                {
                    return DetectorState.Nothing;
                }

                UpdateMotionFinder(imageArray.First());
                _foundNothingCount = 0;

                return DetectorState.Nothing;
            }

            _noBoundingBoxCount++;

            if (_noBoundingBoxCount <= NoBoundingBoxThreshold)
            {
                return evaluator.Evaluate();
            }

            UpdateMotionFinder(imageArray.First());
            _noBoundingBoxCount = 0;

            return evaluator.Evaluate();
        }

        private void UpdateMotionFinder(Image<Gray, byte> background)
        {
            Log.Info("(Re)initializing background");
            _motionFinder = new MotionFinder<byte>(background);
        }
    }
}
