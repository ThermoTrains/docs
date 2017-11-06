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

        private static Image<Gray, byte> FlippedBackground
        {
            get
            {
                var background = new Image<Rgb, byte>(@"Resources\train-background.jpg").Convert<Gray, byte>();
                CvInvoke.Flip(background, background, FlipType.Horizontal);
                return background;
            }
        }

        [TestMethod]
        public void DetectRightEntryTest()
        {
            var detector = new EntryDetector(Background);

            var images = Enumerable.Range(2, 3)
                .Select(i => new Image<Rgb, byte>($@"Resources\train-{i}.jpg"))
                .Select(train => train.Convert<Gray, byte>())
                .ToArray();

            detector.Tick(images);

            Assert.AreEqual(DetectorState.Entry, detector.CurrentState);
        }

        [TestMethod]
        public void DetectLeftEntryTest()
        {
            var detector = new EntryDetector(FlippedBackground);

            var images = Enumerable.Range(2, 3)
                .Select(i => new Image<Rgb, byte>($@"Resources\train-{i}.jpg"))
                .Select(image => image.Convert<Gray, byte>())
                .Select(image =>
                {
                    // flipping the image and reverse the order will produce a train entering from the other side
                    CvInvoke.Flip(image, image, FlipType.Horizontal);
                    return image;
                })
                .ToArray();

            detector.Tick(images);

            Assert.AreEqual(DetectorState.Entry, detector.CurrentState);
        }

        [TestMethod]
        public void DetectRightExitTest()
        {
            var detector = new EntryDetector(Background)
            {
                CurrentState = DetectorState.Entry
            };

            var images = Enumerable.Range(1, 4)
                .Select(i => new Image<Rgb, byte>($@"Resources\train-{i}.jpg"))
                .Select(image => image.Convert<Gray, byte>())
                .Reverse() // just by reversing, the train exits :)
                .ToArray();

            detector.Tick(images);

            Assert.AreEqual(DetectorState.Exit, detector.CurrentState);
        }

        [TestMethod]
        public void DetectLeftExitTest()
        {
            var detector = new EntryDetector(FlippedBackground) {CurrentState = DetectorState.Entry};

            var images = Enumerable.Range(1, 4)
                .Select(i => new Image<Rgb, byte>($@"Resources\train-{i}.jpg"))
                .Select(image => image.Convert<Gray, byte>())
                .Select(image =>
                {
                    // flipping the image and reverse the order will produce a train entering from the other side
                    CvInvoke.Flip(image, image, FlipType.Horizontal);
                    return image;
                })
                .Reverse()
                .ToArray();

            detector.Tick(images);

            Assert.AreEqual(DetectorState.Exit, detector.CurrentState);
        }

        [TestMethod]
        public void DetectNothingTest()
        {
            var detector = new EntryDetector(Background);

            var images = Enumerable.Range(0, 1) // image 0 and 1 have no train on
                .Select(i => new Image<Rgb, byte>($@"Resources\train-{i}.jpg"))
                .Select(image => image.Convert<Gray, byte>())
                .ToArray();

            detector.Tick(images);

            Assert.AreEqual(DetectorState.Nothing, detector.CurrentState);
        }
    }
}
