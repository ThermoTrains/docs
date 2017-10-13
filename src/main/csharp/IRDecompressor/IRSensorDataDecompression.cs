using System;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Linq;

namespace SebastianHaeni.ThermoBox.IRDecompressor
{
    public static class IRSensorDataDecompression
    {
        public static void Decompress(string sourceFile, string outputFile)
        {
            (var minValue, var scale) = GetCompressionParameters(sourceFile);

            // inverse the scale because now we're decompressing
            var inverseScale = 1 / scale;

            using (var capture = new VideoCapture(sourceFile))
            {
                var frameHeader = File.ReadAllBytes(@"Resources\flir-seq-frame-header");
                var binaryWriter = new BinaryWriter(new FileStream(outputFile, FileMode.Create));

                Mat mat;

                while ((mat = capture.QueryFrame()) != null)
                {
                    var frame = mat.ToImage<Gray, byte>();

                    // Write static header containing EXIF information
                    // notable information data in that header:
                    // - image resolution (640*512)
                    // - emissivity (1)
                    // - frame rate (30)
                    // - and lots more, use exiftool and pass the frame header file to see all
                    binaryWriter.Write(frameHeader);

                    // Multiply and shift values
                    var denormalized = frame.ConvertScale<ushort>(inverseScale, minValue);

                    // Flatten data
                    var data = denormalized.Data.Cast<ushort>().ToArray();

                    // Write image data
                    var bytes = data.SelectMany(BitConverter.GetBytes).ToArray();
                    binaryWriter.Write(bytes);
                }
            }
        }

        /// <summary>
        /// Extracts parameters used to compress the file.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        private static (double minValue, double scale) GetCompressionParameters(string sourceFile)
        {
            var tagFile = TagLib.File.Create(sourceFile);
            var parts = tagFile.Tag.Comment.Split('/');

            double.TryParse(parts[0], out var minValue);
            double.TryParse(parts[1], out var scale);

            return (minValue, scale);
        }
    }
}
