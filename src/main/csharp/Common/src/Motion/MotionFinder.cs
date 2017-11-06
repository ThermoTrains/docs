using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using SebastianHaeni.ThermoBox.Common.Util;

namespace SebastianHaeni.ThermoBox.Common.Motion
{
    public class MotionFinder<TDepth>
        where TDepth : new()
    {
        public Image<Gray, TDepth> Background { get; private set; }

        public MotionFinder(Image<Gray, TDepth> background)
        {
            Background = background;
        }

        public Rectangle? FindBoundingBox(
            Image<Gray, TDepth> source,
            Gray threshold,
            Gray maxValue)
        {
            // compute absolute diff between current frame and first frame
            var diff = Background.AbsDiff(source);

            // binarize image
            var t = diff.ThresholdBinary(threshold, maxValue);

            // erode to get rid of small dots
            t = t.Erode(8);

            // dilate the threshold image to fill in holes
            t = t.Dilate(8);

            // find contours
            var contours = new VectorOfVectorOfPoint();
            var hierarchy = new Mat();
            CvInvoke.FindContours(t, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            if (contours.Size == 0)
            {
                // no contours, so we purge
                return null;
            }

            // create bounding box of all contours
            var bbox = MathUtil.GetMaxRectangle(contours);

            return bbox;
        }
    }
}
