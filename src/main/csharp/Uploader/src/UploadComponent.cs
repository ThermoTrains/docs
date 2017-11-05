using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using log4net;
using SebastianHaeni.ThermoBox.Common.Component;
using SebastianHaeni.ThermoBox.Common.Util;

namespace SebastianHaeni.ThermoBox.Uploader
{
    internal class UploadComponent : ThermoBoxComponent
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _requestUriString;
        private readonly string _username;
        private readonly string _password;

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
            _username = userSettingsParts[0];
            _password = userSettingsParts[1];

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

            // Get the object used to communicate with the server.  
            var request = (FtpWebRequest) WebRequest.Create($"ftp://{_requestUriString}/{filename}");
            request.Method = WebRequestMethods.Ftp.UploadFile;

            // Provide credentials.
            request.Credentials = new NetworkCredential(_username, _password);

            // Copy the contents of the file to the request stream.  
            var sourceBytes = File.ReadAllBytes(filePath);
            request.ContentLength = sourceBytes.Length;
            
            Log.Info(
                $"Uploading {filePath} to {_requestUriString}, " +
                $"File Size: {FileUtil.GetSizeRepresentation((ulong)sourceBytes.Length)}");

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var requestStream = request.GetRequestStream();
            requestStream.Write(sourceBytes, 0, sourceBytes.Length);
            requestStream.Close();

            var response = (FtpWebResponse) request.GetResponse();

            // Get the elapsed time as a TimeSpan value.
            var ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            var elapsedTime = $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";

            Log.Info(
                $"Upload file complete, took {elapsedTime}, " +
                $"Status: {response.StatusDescription.Replace(Environment.NewLine, " ")}");

            response.Close();

            Log.Info($"Moving file to recycle bin: {filePath}");
            FileUtil.MoveToRecycleBin(filePath);
        }
    }
}
