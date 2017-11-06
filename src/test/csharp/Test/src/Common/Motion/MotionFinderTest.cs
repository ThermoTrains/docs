using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SebastianHaeni.ThermoBox.Common.Motion;

namespace Test.Common.Motion
{
    [TestClass]
    public class MotionFinderTest
    {
        [TestMethod]
        public void FindBoundingBoxTest()
        {
            var background = new Image<Rgb, byte>(@"Resources\train-background.jpg");

            var motionFinder = new MotionFinder<byte>(background.Convert<Gray, byte>());

            var threshold = new Gray(40.0);
            var maxValue = new Gray(byte.MaxValue);

            var gray= new Image<Rgb, byte>($@"Resources\train-2.jpg").Convert<Gray, byte>();
            var bbox = motionFinder.FindBoundingBox(gray, threshold, maxValue);

            Assert.IsTrue(bbox.HasValue);
        }
    }
}
