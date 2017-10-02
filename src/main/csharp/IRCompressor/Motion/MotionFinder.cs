using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing;
using SebastianHaeni.ThermoBox.Common.Util;

namespace SebastianHaeni.ThermoBox.IRCompressor.Motion
{
    public class MotionFinder<TColor, TDepth>
        where TColor : struct, IColor
        where TDepth : new()
    {
        private readonly Image<TColor, TDepth> _background;

        public MotionFinder(Image<TColor, TDepth> background)
        {
            _background = background;
        }

        public Rectangle? FindBoundingBox(
            Image<TColor, TDepth> source,
            TColor threshold,
            TColor maxValue,
            double minWidthFactor = .7)
        {
            // compute absolute diff between current frame and first frame
            var diff = _background.AbsDiff(source);

            // binarize image
            var t = diff.ThresholdBinary(threshold, maxValue);

            // erode to get rid of small dots
            t = t.Erode(3);

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
            var bbox = MathUtil.GetMedianRectangle(contours);

            if (bbox.Width < (source.Size.Width * minWidthFactor))
            {
                // => the motion area covers not almost the whole width
                // this can be due by one the following reasons
                // - it's the start of the train
                // - it's the end of the train
                // - a bird flew over the empty background
                return null;
            }

            return bbox;
        }
    }
}
