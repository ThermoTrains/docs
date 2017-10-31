using System.Collections.Generic;
using Emgu.CV;

namespace SebastianHaeni.ThermoBox.Common.Util
{
    public static class DebugUtil
    {
        public static void PreviewImages<TColor, TDepth>(IEnumerable<Image<TColor, TDepth>> images)
            where TColor : struct, IColor
            where TDepth : new()
        {
            foreach (var image in images)
            {
                CvInvoke.Imshow("Preview Image", image.Mat);
                CvInvoke.WaitKey();
                CvInvoke.DestroyAllWindows();
            }
        }
    }
}
