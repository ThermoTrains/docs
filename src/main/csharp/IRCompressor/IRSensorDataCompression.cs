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

                using (var videoWriter = new VideoWriter(outputVideoFile, compression, fps, thermalImage.Size, false))
                {
                    // loop through every frame and transform it to fit into video
                    for (var i = 0; i < thermalImage.ThermalSequencePlayer.Count(); i++)
                    {
                        thermalImage.ThermalSequencePlayer.Next();
                        Image<Gray, ushort> image16 = GetSignalImage(thermalImage);

                        // this makes the picture eye friendly but looses the signal data
                        // TODO normalize with the same known parameters for every frame
                        CvInvoke.Normalize(image16, image16, 0, ushort.MaxValue, NormType.MinMax);

                        // Loosing precision, but there is no open video codec supporting 16 bit grayscale :(
                        var image8 = image16.ConvertScale<byte>(1 / 256f, 0);

                        videoWriter.Write(image8.Mat);
                    }
                }
            }

            // TODO emit compression parameters file to reconstruct original file (normalization parameters) and publish it to upload
        }

        private static Image<Gray, ushort> GetSignalImage(ThermalImageFile thermalImage)
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
