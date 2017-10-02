using Emgu.CV;
using Emgu.CV.Structure;
using Flir.Atlas.Image;
using log4net;
using SebastianHaeni.ThermoBox.IRCompressor.Motion;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using SebastianHaeni.ThermoBox.Common.Util;

namespace SebastianHaeni.ThermoBox.IRCompressor
{
    public static class IRSensorDataCompression
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Compress(string sourceFile, string outputVideoFile)
        {
            Log.Info($"Compressing {sourceFile} with H.264 to {outputVideoFile}");

            using (var thermalImage = new ThermalImageFile(sourceFile))
            {
                // loop through every frame and calculate min and max values
                var (minValue, maxValue) = FindMinMaxValues(thermalImage);

                // Find bounding box of moving train
                var boundingBoxes = FindTrainBoundingBoxes(thermalImage, maxValue, minValue);

                // Find min and max within the train bounds
                var (minTrain, maxTrain) = boundingBoxes.Count > 0
                    ? FindMinMaxTrainValues(boundingBoxes, thermalImage)
                    : (minValue, maxValue);

                // Write video from first bbox index to last with extracted min and max values
                var trainScale = 256f / (maxTrain - minTrain);
                var formatedScalePercent = ((1 - trainScale) * 100).ToString("N");
                Log.Info($"Precision loss: {formatedScalePercent}%");

                WriteVideo(outputVideoFile, boundingBoxes, thermalImage, minTrain, trainScale);

                // Add compression parameters to file as metadata.
                AddCompressionParameters(outputVideoFile, minTrain, trainScale);
            }
        }

        private static void WriteVideo(
            string outputVideoFile,
            IReadOnlyCollection<(int index, Rectangle rect)> boundingBoxes,
            ThermalImageFile thermalImage,
            int minTrain,
            float trainScale)
        {
            var firstFrame = boundingBoxes.Select(v => v.index).DefaultIfEmpty(0).Min();
            var lastFrame = boundingBoxes.Select(v => v.index)
                .DefaultIfEmpty(thermalImage.ThermalSequencePlayer.Count() - 1).Max();

            thermalImage.ThermalSequencePlayer.SelectedIndex = firstFrame;

            var compression = VideoWriter.Fourcc('H', '2', '6', '4');
            var fps = (int) thermalImage.ThermalSequencePlayer.FrameRate;

            using (var videoWriter = new VideoWriter(outputVideoFile, compression, fps, thermalImage.Size, false))
            {
                while (thermalImage.ThermalSequencePlayer.SelectedIndex < lastFrame)
                {
                    var image = GetSignalImage(thermalImage);
                    thermalImage.ThermalSequencePlayer.Next();

                    var image8 = ScaleDown(image, minTrain, trainScale);

                    videoWriter.Write(image8.Mat);
                }
            }
        }

        private static (int minValue, int maxValue) FindMinMaxValues(ThermalImageFile thermalImage)
        {
            thermalImage.ThermalSequencePlayer.First();

            var minValue = int.MaxValue;
            var maxValue = int.MinValue;

            for (var i = 0; i < thermalImage.ThermalSequencePlayer.Count(); i++)
            {
                if (thermalImage.MinSignalValue < minValue)
                {
                    minValue = thermalImage.MinSignalValue;
                }

                if (thermalImage.MaxSignalValue > maxValue)
                {
                    maxValue = thermalImage.MaxSignalValue;
                }

                thermalImage.ThermalSequencePlayer.Next();
            }

            return (minValue, maxValue);
        }

        private static List<(int index, Rectangle rect)> FindTrainBoundingBoxes(
            ThermalImageFile thermalImage,
            double maxValue,
            double minValue)
        {
            thermalImage.ThermalSequencePlayer.First();

            var background = GetSignalImage(thermalImage); // the background inherently is the first frame
            var scale = 256f / (maxValue - minValue);
            var motionFinder = new MotionFinder<Gray, byte>(ScaleDown(background, minValue, scale));
            var boundingBoxes = new List<(int index, Rectangle rect)>();

            for (var i = 0; i < thermalImage.ThermalSequencePlayer.Count(); i++)
            {
                thermalImage.ThermalSequencePlayer.Next();
                var image = GetSignalImage(thermalImage);

                var image8 = ScaleDown(image, minValue, scale);

                // TODO do not hard code thresholds
                var bbox = motionFinder.FindBoundingBox(image8, new Gray(20.0), new Gray(255.0));

                if (bbox.HasValue)
                {
                    boundingBoxes.Add((index: i, rect: bbox.Value));
                }
            }
            return boundingBoxes;
        }

        private static (int minTrain, int maxTrain) FindMinMaxTrainValues(
            IReadOnlyCollection<(int index, Rectangle rect)> boundingBoxes,
            ThermalImageFile thermalImage)
        {
            var medianBox = MathUtil.GetMedianRectangle(boundingBoxes.Select(v => v.rect));
            var minValues = new List<ushort>();
            var maxValues = new List<ushort>();

            foreach (var (index, rect) in boundingBoxes)
            {
                if (MathUtil.RectDiff(rect, medianBox) > .2)
                {
                    // the difference of the box to the median is too big to be a reliable source
                    continue;
                }

                thermalImage.ThermalSequencePlayer.SelectedIndex = index;
                var image = GetSignalImage(thermalImage);

                // Extract min and max within bounds
                var min = ushort.MaxValue;
                var max = ushort.MinValue;

                for (var x = rect.X; x < rect.X + rect.Width; x++)
                {
                    for (var y = rect.Y + 20; y < rect.Y + rect.Height; y++)
                    {
                        var val = image.Data[y, x, 0];

                        if (val < min)
                        {
                            min = val;
                        }
                        if (val > max)
                        {
                            max = val;
                        }
                    }
                }

                minValues.Add(min);
                maxValues.Add(max);
            }

            var minTrain = MathUtil.Median(minValues.Select(v => (int) v).ToArray());
            var maxTrain = MathUtil.Median(maxValues.Select(v => (int) v).ToArray());

            return (minTrain, maxTrain);
        }

        private static Image<Gray, byte> ScaleDown(Image<Gray, ushort> image, double minValue, double scale)
        {
            // Floor values to min value (img * 1 + img * 0 - minValue)
            var normalized = image.AddWeighted(image, 1, 0, -minValue);

            // Loosing precision, but there is no open video codec supporting 16 bit grayscale :(
            // Scaling values down to our established value span as a factor of 256
            return normalized.ConvertScale<byte>(scale, 0);
        }

        private static void AddCompressionParameters(string outputVideoFile, double minValue, double scale)
        {
            var tagFile = TagLib.File.Create(outputVideoFile);
            tagFile.Tag.Comment = $"{minValue}/{scale}";
            tagFile.Save();
        }

        private static Image<Gray, ushort> GetSignalImage(ImageBase thermalImage)
        {
            var pixels = thermalImage.ImageProcessing.GetPixels();

            // Lock thermal image pixel data
            pixels.LockPixelData();

            // Declare an array to hold the bytes of the signal.
            var signalValues = new byte[pixels.Stride * pixels.Height];

            // Copy the signal values as bytes into the array.
            Marshal.Copy(pixels.PixelData, signalValues, 0, signalValues.Length);

            // Write the bytes into the new image.
            var image = new Image<Gray, ushort>(thermalImage.Width, thermalImage.Height);

            for (var column = 0; column < pixels.Width; column++)
            {
                for (var row = 0; row < pixels.Height; row++)
                {
                    var index = 2 * (row * pixels.Width + column);
                    // Each part contains one byte
                    var part1 = signalValues[index];
                    var part2 = signalValues[index + 1];

                    // Merge two bytes into one short
                    var merged = BitConverter.ToUInt16(new[] {part1, part2}, 0);
                    image.Data[row, column, 0] = merged;
                }
            }

            // Free thermal image lock
            pixels.UnlockPixelData();

            return image;
        }
    }
}
