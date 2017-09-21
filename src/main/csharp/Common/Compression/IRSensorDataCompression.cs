using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Flir.Atlas.Image;
using Flir.Atlas.Image.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace SebastianHaeni.ThermoBox.Common.Compression
{
    public class IRSensorDataCompression
    {
        public static void Compress()
        {
            var thermalImage = new ThermalImageFile(@"..\..\..\..\..\..\samples\thermal\book.seq");

            string fileName = $@"..\..\..\..\..\..\samples\thermal\test.mp4";
            var compression = VideoWriter.Fourcc('H', '2', '6', '4');
            //var compression = VideoWriter.Fourcc('Y', 'U', 'V', '9');
            int fps = (int)thermalImage.ThermalSequencePlayer.FrameRate;

            var videoWriter = new VideoWriter(fileName, compression, fps, thermalImage.Size, false);

            for (var i = 0; i < thermalImage.ThermalSequencePlayer.Count(); i++)
            {
                thermalImage.ThermalSequencePlayer.Next();
                Image<Gray, ushort> image = GetSignalImage(thermalImage);

                // this makes the picture eye friendly but looses the signal data
                CvInvoke.Normalize(image, image, 0, ushort.MaxValue, NormType.MinMax);

                // Loosing precision, but there is no video codec supporting 16 bit grayscale :(
                var image8 = image.ConvertScale<byte>(1 / 256f, 0);

                //image.Save($@"..\..\..\..\..\..\samples\thermal\out\{i}.png");
                videoWriter.Write(image8.Mat);
            }

            // recreate thermal image by using reference image and replacing signal value
            //image.ImageProcessing.ReplaceSignalValues(b);
            //image.Save(@"..\..\..\..\..\..\samples\thermal\replaced-pixels.jpg");
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
