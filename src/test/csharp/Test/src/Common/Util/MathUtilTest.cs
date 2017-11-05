using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SebastianHaeni.ThermoBox.Common.Util;

namespace Test.Common.Util
{
    [TestClass]
    public class MathUtilTest
    {
        [TestMethod]
        public void TestMedian123()
        {
            Assert.AreEqual(2, MathUtil.Median(new[] {1, 2, 3}));
        }

        [TestMethod]
        public void TestMedian1And4()
        {
            Assert.AreEqual(2, MathUtil.Median(new[] {1, 4}));
        }

        [TestMethod]
        public void TestMedian1And5And10()
        {
            Assert.AreEqual(5, MathUtil.Median(new[] {1, 5, 10}));
        }

        [TestMethod]
        public void TestGetMedianRectangleEmpty()
        {
            Assert.AreEqual(new Rectangle(0, 0, 0, 0), MathUtil.GetMedianRectangle(new List<Rectangle>()));
        }

        [TestMethod]
        public void TestGetMedianRectangleSingle()
        {
            var rectangles = new List<Rectangle>
            {
                new Rectangle(1, 1, 1, 1)
            };
            Assert.AreEqual(new Rectangle(1, 1, 1, 1), MathUtil.GetMedianRectangle(rectangles));
        }

        [TestMethod]
        public void TestGetMedianRectangleTwo()
        {
            var rectangles = new List<Rectangle>
            {
                new Rectangle(1, 1, 1, 1),
                new Rectangle(3, 3, 3, 3)
            };
            Assert.AreEqual(new Rectangle(2, 2, 2, 2), MathUtil.GetMedianRectangle(rectangles));
        }

        [TestMethod]
        public void TestRectDiffZero()
        {
            var a = new Rectangle(0, 0, 0, 0);
            var b = new Rectangle(0, 0, 0, 0);
            var diff = MathUtil.RectDiff(a, b);

            Assert.AreEqual(0, diff);
        }

        [TestMethod]
        public void TestRectDiffLittle()
        {
            var a = new Rectangle(100, 100, 100, 100);
            var b = new Rectangle(101, 101, 101, 101);
            var diff = MathUtil.RectDiff(a, b);

            Assert.AreEqual(.01f, diff);
        }

        [TestMethod]
        public void TestRectDiffBig()
        {
            var a = new Rectangle(100, 100, 100, 100);
            var b = new Rectangle(0, 0, 0, 0);
            var diff = MathUtil.RectDiff(a, b);

            Assert.AreEqual(1, diff);
        }
    }
}
