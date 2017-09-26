using Emgu.CV;
using Emgu.CV.Structure;
using Flir.Atlas.Image;
using System.Linq;

namespace SebastianHaeni.ThermoBox.IRDecompressor
{
    public class IRSensorDataDecompression
    {
        public static void Decompress(string sourceFile, string snapshot, string outputFile)
        {
            (var minValue, var scale) = GetCompressionParameters(sourceFile);
            var inverseScale = 1 / scale;

            using (var snapshotImage = new ThermalImageFile(snapshot))
            {
                using (var capture = new VideoCapture(sourceFile))
                {
                    Image<Gray, byte> frame = null;
                    var i = 0;

                    while ((frame = capture.QueryFrame().ToImage<Gray, byte>()) != null)
                    {
                        // Multiply and shift values
                        var denormalized = frame.ConvertScale<ushort>(inverseScale, minValue);

                        // Flatten data
                        var data = denormalized.Data.Cast<ushort>().ToArray();

                        snapshotImage.EnterLock();
                        snapshotImage.ImageProcessing.ReplaceSignalValues(data);
                        snapshotImage.SaveSnapshot($@"..\..\..\..\..\..\..\samples\thermal\frames\{++i}.jpg");
                        snapshotImage.ExitLock();
                    }
                }
            }
        }

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
