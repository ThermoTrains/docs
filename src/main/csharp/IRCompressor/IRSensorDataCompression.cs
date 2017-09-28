using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Flir.Atlas.Image;
using Flir.Atlas.Image.Interfaces;
using log4net;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SebastianHaeni.ThermoBox.IRCompressor
{
    public class IRSensorDataCompression
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Compress(string sourceFile, string outputVideoFile)
        {
            log.Info($"Compressing {sourceFile} with H.264 to {outputVideoFile}");

            using (var thermalImage = new ThermalImageFile(sourceFile))
            {
                var compression = VideoWriter.Fourcc('H', '2', '6', '4');
                int fps = (int)thermalImage.ThermalSequencePlayer.FrameRate;

                var minValue = double.MaxValue;
                var maxValue = double.MinValue;
                double scale = 0;

                using (var videoWriter = new VideoWriter(outputVideoFile, compression, fps, thermalImage.Size, false))
                {
                    var convertedImages = new Image<Gray, ushort>[thermalImage.ThermalSequencePlayer.Count()];

                    // loop through every frame, grab image and calculate min and max values
                    for (var i = 0; i < thermalImage.ThermalSequencePlayer.Count(); i++)
                    {
                        thermalImage.ThermalSequencePlayer.Next();
                        convertedImages[i] = GetSignalImage(thermalImage);

                        if (thermalImage.MinSignalValue < minValue)
                        {
                            minValue = thermalImage.MinSignalValue;
                        }

                        if (thermalImage.MaxSignalValue > maxValue)
                        {
                            maxValue = thermalImage.MaxSignalValue;
                        }
                    }

                    scale = 256f / (maxValue - minValue);

                    var formatedScalePercent = ((1 - scale) * 100).ToString("N");
                    log.Info($"Precision loss: {formatedScalePercent}%");

                    // Scale images and write them into a video
                    for (var i = 0; i < thermalImage.ThermalSequencePlayer.Count(); i++)
                    {
                        // Floor values to min value (img * 1 + img * 0 - minValue)
                        var normalized = convertedImages[i].AddWeighted(convertedImages[i], 1, 0, -minValue);

                        // Loosing precision, but there is no open video codec supporting 16 bit grayscale :(
                        // Scaling values down to our established value span as a factor of 256
                        var image8 = normalized.ConvertScale<byte>(scale, 0);

                        videoWriter.Write(image8.Mat);
                    }
                }

                // Add compression parameters to file as metadata.
                AddCompressionParameters(outputVideoFile, minValue, scale);
            }
        }

        private static void AddCompressionParameters(string outputVideoFile, double minValue, double scale)
        {
            var tagFile = TagLib.File.Create(outputVideoFile);
            tagFile.Tag.Comment = $"{minValue}/{scale}";
            tagFile.Save();
        }

        private static Image<Gray, ushort> GetSignalImage(ThermalImage thermalImage)
        {
            IPixels pixels = thermalImage.ImageProcessing.GetPixels();

            // Lock thermal image pixel data
            pixels.LockPixelData();

            // Declare an array to hold the bytes of the signal.
            byte[] signalValues = new byte[pixels.Stride * pixels.Height];

            // Copy the signal values as bytes into the array.
            Marshal.Copy(pixels.PixelData, signalValues, 0, signalValues.Length);

            // Write the bytes into the new image.
            var image = new Image<Gray, ushort>(thermalImage.Width, thermalImage.Height);

            for (int column = 0; column < pixels.Width; column++)
            {
                for (int row = 0; row < pixels.Height; row++)
                {
                    int index = 2 * (row * pixels.Width + column);
                    // Each part contains one byte
                    var part1 = signalValues[index];
                    var part2 = signalValues[index + 1];

                    // Merge two bytes into one short
                    var merged = BitConverter.ToUInt16(new byte[2] { part1, part2 }, 0);
                    image.Data[row, column, 0] = merged;
                }
            }

            // Free thermal image lock
            pixels.UnlockPixelData();

            return image;
        }
    }
}
