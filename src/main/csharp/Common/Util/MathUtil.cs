using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Util;

namespace SebastianHaeni.ThermoBox.Common.Util
{
    public static class MathUtil
    {
        public static Rectangle GetMaxRectangle(VectorOfVectorOfPoint contours)
        {
            var rectangles = new List<Rectangle>();
            for (var i = 0; i < contours.Size; i++)
            {
                var bbox = CvInvoke.BoundingRectangle(contours[i]);
                rectangles.Add(bbox);
            }

            return GetMaxRectangle(rectangles);
        }

        private static Rectangle GetMaxRectangle(IEnumerable<Rectangle> rectangles)
        {
            var enumerable = rectangles as Rectangle[] ?? rectangles.ToArray();

            var x = enumerable.Select(r => r.X).Min();
            var y = enumerable.Select(r => r.Y).Min();

            return new Rectangle
            {
                X = x,
                Y = y,
                Width = enumerable.Select(r => r.X + r.Width).Max() - x,
                Height = enumerable.Select(r => r.Y + r.Height).Max() - y
            };
        }

        public static Rectangle GetMedianRectangle(IEnumerable<Rectangle> rectangles)
        {
            var enumerable = rectangles as Rectangle[] ?? rectangles.ToArray();

            return new Rectangle
            {
                X = GetMedian(enumerable, r => r.X),
                Y = GetMedian(enumerable, r => r.Y),
                Width = GetMedian(enumerable, r => r.Width),
                Height = GetMedian(enumerable, r => r.Height)
            };
        }

        private static int GetMedian<T>(IEnumerable<T> values, Func<T, int> selector)
        {
            return Median(values.Select(selector).ToArray());
        }

        public static int Median(int[] numArray)
        {
            return Median(numArray, (a, b) => (a + b) / 2);
        }

        private static T Median<T>(T[] numArray, Func<T, T, T> mean)
        {
            if (numArray.Length == 0)
            {
                return default(T);
            }

            var clone = (T[]) numArray.Clone();
            Array.Sort(clone);

            return clone.Length % 2 == 0
                ? mean.Invoke(clone[clone.Length / 2], clone[clone.Length / 2 - 1])
                : clone[clone.Length / 2];
        }

        public static float RectDiff(Rectangle rect1, Rectangle rect2)
        {
            var diff = Math.Abs(rect1.X - rect2.X) +
                       Math.Abs(rect1.Y - rect2.Y) +
                       Math.Abs(rect1.Width - rect2.Width) +
                       Math.Abs(rect1.Height - rect2.Height);

            var size = rect1.X + rect1.Y + rect1.Width + rect1.Height;

            if (size == 0)
            {
                return 0;
            }

            return diff / (float) size;
        }
    }
}
