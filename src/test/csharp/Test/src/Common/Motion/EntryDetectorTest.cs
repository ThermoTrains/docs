using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SebastianHaeni.ThermoBox.Common.Motion;

namespace Test.Common.Motion
{
    [TestClass]
    public class EntryDetectorTest
    {
        private static Image<Gray, byte> Background =>
            new Image<Rgb, byte>(@"Resources\train-background.jpg").Convert<Gray, byte>();

        [TestMethod]
        public void DetectRightEntryTest()
        {
            var detector = new EntryDetector(Background);

            var images = Enumerable.Range(2, 3)
                .Select(i => new Image<Rgb, byte>($@"Resources\train-{i}.jpg"))
                .Select(train => train.Convert<Gray, byte>());

            var result = detector.Detect(images, DetectorState.Nothing);

            Assert.AreEqual(result, DetectorState.Entry);
        }

        [TestMethod]
        public void DetectLeftEntryTest()
        {
            CvInvoke.Flip(Background, Background, FlipType.Horizontal);
            var detector = new EntryDetector(Background);

            var images = Enumerable.Range(2, 3)
                .Select(i => new Image<Rgb, byte>($@"Resources\train-{i}.jpg"))
                .Select(image => image.Convert<Gray, byte>())
                .Select(image =>
                {
                    // flipping the image and reverse the order will produce a train entering from the other side
                    CvInvoke.Flip(image, image, FlipType.Horizontal);
                    return image;
                });

            var result = detector.Detect(images, DetectorState.Nothing);

            Assert.AreEqual(result, DetectorState.Entry);
        }

        [TestMethod]
        public void DetectRightExitTest()
        {
            var detector = new EntryDetector(new Image<Gray, byte>(@"Resources\train-background-jpg"));

            var images = Enumerable.Range(1, 4)
                .Select(i => new Image<Rgb, byte>($@"Resources\train-{i}.jpg"))
                .Select(image => image.Convert<Gray, byte>())
                .Reverse(); // just by reversing, the train exits :)

            var result = detector.Detect(images, DetectorState.Entry);

            Assert.AreEqual(result, DetectorState.Exit);
        }

        [TestMethod]
        public void DetectLeftExitTest()
        {
            CvInvoke.Flip(Background, Background, FlipType.Horizontal);
            var detector = new EntryDetector(Background);

            var images = Enumerable.Range(1, 4)
                .Select(i => new Image<Rgb, byte>($@"Resources\train-{i}.jpg"))
                .Select(image => image.Convert<Gray, byte>())
                .Select(image =>
                {
                    // flipping the image and reverse the order will produce a train entering from the other side
                    CvInvoke.Flip(image, image, FlipType.Horizontal);
                    return image;
                })
                .Reverse();

            var result = detector.Detect(images, DetectorState.Entry);

            Assert.AreEqual(result, DetectorState.Exit);
        }

        [TestMethod]
        public void DetectNothingTest()
        {
            var detector = new EntryDetector(Background);

            var images = Enumerable.Range(0, 1) // image 0 and 1 have no train on
                .Select(i => new Image<Rgb, byte>($@"Resources\train-{i}.jpg"))
                .Select(image => image.Convert<Gray, byte>());

            var result = detector.Detect(images, DetectorState.Nothing);

            Assert.AreEqual(result, DetectorState.Nothing);
        }
    }
}
