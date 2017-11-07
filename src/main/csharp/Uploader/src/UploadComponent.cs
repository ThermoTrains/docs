using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using log4net;
using SebastianHaeni.ThermoBox.Common.Component;
using SebastianHaeni.ThermoBox.Common.Util;

namespace SebastianHaeni.ThermoBox.Uploader
{
    internal class UploadComponent : ThermoBoxComponent
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _requestUriString;
        private readonly NetworkCredential _clientCredentials;

        public UploadComponent()
        {
            var connectionSettings = Environment.GetEnvironmentVariable("THERMOBOX_FTP");

            if (connectionSettings == null)
            {
                throw new InvalidOperationException(
                    "Could not find connection settings in environment variables. Exiting");
            }

            var settingsParts = connectionSettings.Split('@');
            var userSettings = settingsParts[0];
            var userSettingsParts = userSettings.Split(':');

            _requestUriString = settingsParts[1];
            var username = userSettingsParts[0];
            var password = userSettingsParts[1];

            _clientCredentials = new NetworkCredential(username, password);

            Subscription(Commands.Upload, (channel, message) => UploadFile(message));
        }

        private void UploadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Log.Error($"Could not find file: {filePath}");
                return;
            }

            var filename = Path.GetFileName(filePath);

            Log.Info($"Uploading {filePath} to {_requestUriString}");

            // URL to upload the file to.
            var requestUriString = $"ftp://{_requestUriString}/{filename}";

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            using (var client = new WebClient())
            {
                client.Credentials = _clientCredentials;
                client.UploadFile(requestUriString, WebRequestMethods.Ftp.UploadFile, filePath);
                throw new Exception("haha");
            }

            // Get the elapsed time as a TimeSpan value.
            var ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            var elapsedTime = $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";

            Log.Info($"Upload of file {filePath} complete, took {elapsedTime}, moving it to recycle bin.");
            FileUtil.MoveToRecycleBin(filePath);
        }
    }
}
