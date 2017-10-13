using log4net;
using System.IO;
using System.Reflection;
using SebastianHaeni.ThermoBox.Common.Component;

namespace SebastianHaeni.ThermoBox.IRCompressor
{
    internal class CompressorComponent : ThermoBoxComponent
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public CompressorComponent()
        {
            Subscription(Commands.Compress, (channel, sourceFile) =>
            {
                var outputVideoFile = $"{sourceFile}.avi";
                IRSensorDataCompression.Compress(sourceFile, outputVideoFile, IRSensorDataCompression.Mode.Train);

                Log.Info($"Deleting original file {sourceFile}");
                File.Delete(sourceFile);

                Publish(Commands.Upload, outputVideoFile);
            });
        }
    }
}
