using log4net;
using SebastianHaeni.ThermoBox.Common;
using System.IO;
using System.Reflection;

namespace SebastianHaeni.ThermoBox.IRCompressor
{
    class CompressorComponent : ThermoBoxComponent
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected override void Configure()
        {
            Subscription(Commands.Compress, (channel, sourceFile) =>
            {
                var currentDirectory = Path.GetDirectoryName(sourceFile);

                string outputVideoFile = $"{sourceFile}.avi";
                IRSensorDataCompression.Compress(sourceFile, outputVideoFile);

                log.Info($"Deleting original file {sourceFile}");
                File.Delete(sourceFile);

                Publish(Commands.Upload, outputVideoFile);
            });
        }
    }
}
