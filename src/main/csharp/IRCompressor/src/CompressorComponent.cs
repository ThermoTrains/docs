using log4net;
using System.Reflection;
using SebastianHaeni.ThermoBox.Common.Component;
using SebastianHaeni.ThermoBox.Common.Util;

namespace SebastianHaeni.ThermoBox.IRCompressor
{
    internal class CompressorComponent : ThermoBoxComponent
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public CompressorComponent()
        {
            Subscription(Commands.Compress, (channel, sourceFile) =>
            {
                var outputVideoFile = $"{sourceFile}.mp4";
                IRSensorDataCompression.Compress(sourceFile, outputVideoFile, IRSensorDataCompression.Mode.Other);

                Log.Info($"Moving file to recycle bin: {sourceFile}");
                FileUtil.MoveToRecycleBin(sourceFile);

                Publish(Commands.Upload, outputVideoFile);
            });
        }
    }
}
