using System.Collections.Generic;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Structure;

namespace SebastianHaeni.ThermoBox.Common.Motion
{
    public class EntryDetector
    {
        public DetectorState Detect(
            Image<Gray, byte> background,
            IEnumerable<Image<Gray, byte>> images,
            DetectorState current)
        {
            var motionFinder = new MotionFinder<byte>(background.Convert<Gray, byte>());

            var evaluator = current.GetEvaluator(background.Size);

            var threshold = new Gray(40.0);
            var maxValue = new Gray(255.0);

            evaluator.BoundingBoxes = images
                .Select(image => motionFinder.FindBoundingBox(image, threshold, maxValue))
                .Where(box => box.HasValue)
                .Select(box => box.Value);

            return evaluator.Evaluate();
        }
    }
}
