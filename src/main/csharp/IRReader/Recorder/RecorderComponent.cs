using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using Flir.Atlas.Live;
using Flir.Atlas.Live.Device;
using Flir.Atlas.Live.Recorder;
using Flir.Atlas.Live.Remote;
using log4net;
using Newtonsoft.Json;
using SebastianHaeni.ThermoBox.Common;
using SebastianHaeni.ThermoBox.IRReader.DeviceParameters;

namespace SebastianHaeni.ThermoBox.IRReader.Recorder
{
    class RecorderComponent : ThermoBoxComponent
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string RECORDINGS_FOLDER = ConfigurationManager.AppSettings["IR_RECORDINGS_FOLDER"];

        private ThermalGigabitCamera camera;
        private static string currentRecordingDirectory;

        public RecorderComponent(ThermalGigabitCamera camera) : base()
        {
            this.camera = camera;
            camera.ConnectionStatusChanged += ConnectionStatusChanged;
        }

        private void ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            if (e.Status == ConnectionStatus.Connected)
            {
                return;
            }

            log.Error("Lost connection to camera => Exiting");
            Environment.Exit(1);
        }

        protected override void Configure()
        {
            Subscription(Commands.CaptureStart, (channel, message) => StartCapture(message));
            Subscription(Commands.CaptureStop, (channel, message) => StopCapture());
            Subscription(Commands.CaptureAbort, (channel, message) => AbortCapture());
        }

        /// <summary>
        /// Starts a new recording. Before recording starts, a short calibration is executed.
        /// </summary>
        /// <param name="message"></param>
        private void StartCapture(string message)
        {
            if (camera.Recorder.Status != RecorderState.Stopped)
            {
                log.Warn($"Cannot start recording. Current state is {camera.Recorder.Status}");
                return;
            }

            // Sending a NUC command (Non-uniformity correction => it calibrates the camera)

            // We do this to get optimal results and the camera will not calibrate during the recording.
            // The default setting of NUC intervals is 4 minutes. So in case the recording is longer than 4
            // minutes, the camera will NUC itself and the video will have a short freeze.

            // The NUC consumes about 0.5 seconds. The recording will start right after. In case this starting
            // delay is too long we maybe have to consider other techniques to calibrate the camera while not recording.
            camera.DeviceControl.SetDeviceParameter("NUCAction", GenICamType.Command);

            // ensuring the recordings directory exists
            DirectoryInfo recordingDirectory = new DirectoryInfo(RECORDINGS_FOLDER);
            if (!recordingDirectory.Exists)
            {
                recordingDirectory.Create();
            }

            log.Info($"Starting capture with id {message}");
            currentRecordingDirectory = $@"{RECORDINGS_FOLDER}\{message}";

            Retry(() => camera.Recorder.Start($@"{currentRecordingDirectory}\Recording.seq"), () => camera.Recorder.Status == RecorderState.Recording);
        }

        /// <summary>
        /// Stops the recording, collects device parameters and writes them to a file, zips the folder and publishes it
        /// with cmd:delivery:upload.
        /// </summary>
        private void StopCapture()
        {
            // copy this as a new recording might start while we're finishing this one
            var artifactDirectory = currentRecordingDirectory;

            if (camera.Recorder.Status != RecorderState.Recording)
            {
                log.Warn($"Cannot stop recording. Current state is {camera.Recorder.Status}");
                return;
            }

            log.Info($"Stopping capture");
            Retry(() => camera.Recorder.Stop(), () => camera.Recorder.Status == RecorderState.Stopped);
            log.Info($"Recorded {camera.Recorder.FrameCount} frames");

            CreateDeviceParamsFiles(artifactDirectory);
            string zipFilename = CreateZip(artifactDirectory);

            // Deleting source artifact (that one that's uncompressed on the disk)
            new DirectoryInfo(artifactDirectory).Delete(true);

            // Send publish command with the zip file
            Publish(Commands.Upload, zipFilename);
        }

        /// <summary>
        /// Fetching and writing all device params to JSON (around 319 params are available on the FLIR A65).
        /// </summary>
        /// <param name="artifactDirectory"></param>
        private void CreateDeviceParamsFiles(string artifactDirectory)
        {
            var deviceParams = camera.DeviceControl.GetDeviceParameters();
            var mappedValues = (from param in deviceParams
                                where DeviceParameter.HasValue(param)
                                select new DeviceParameter(param));

            var json = JsonConvert.SerializeObject(new DeviceParameters.DeviceParameters() { Parameters = mappedValues }, Formatting.Indented);
            File.WriteAllText($@"{artifactDirectory}\DeviceParams.json", json);
        }

        /// <summary>
        /// Zipping the file as it's a non compressed format and compression rates of 50 - 80% can be achieved.
        /// </summary>
        /// <param name="artifactDirectory"></param>
        /// <returns></returns>
        private static string CreateZip(string artifactDirectory)
        {
            var zipFilename = $@"{artifactDirectory}.zip";
            log.Info($"Zipping directory {artifactDirectory} to {zipFilename}");

            if (File.Exists(zipFilename))
            {
                log.Warn($"File {zipFilename} already exists, overwriting it");
                File.Delete(zipFilename);
            }

            ZipFile.CreateFromDirectory(artifactDirectory, zipFilename);
            return zipFilename;
        }

        /// <summary>
        /// Aborts a running capture by stopping the recording and purging the generated artifact.
        /// </summary>
        private void AbortCapture()
        {
            if (camera.Recorder.Status == RecorderState.Stopped)
            {
                log.Warn($"Cannot stop recording. It is already stopped");
                return;
            }

            log.Info($"Aborting capture");
            Retry(() => camera.Recorder.Stop(), () => camera.Recorder.Status == RecorderState.Stopped);

            // Deleting generated artifact
            new DirectoryInfo(currentRecordingDirectory).Delete(true);
        }

        /// <summary>
        /// Tries to execute an action. If an exception is thrown, it tries again until a threshold is reached.
        /// </summary>
        /// <param name="action">Action with potential exception thrown</param>
        /// <param name="testSuccess">Function to test the success of the action</param>
        private static void Retry(Action action, Func<bool> testSuccess)
        {
            int tries = 0;
            int MAX_TRIES = 5;

            while (tries < MAX_TRIES)
            {
                try
                {
                    action.Invoke();

                    if (testSuccess.Invoke())
                    {
                        return;
                    }
                }
                catch (CommandFailedException ex)
                {
                    log.Warn($"Exception while executing camera command", ex);
                }

                tries++;
                log.Warn($"Failed executing camera command (try {tries} of {MAX_TRIES})");
                Thread.Sleep(10);
            }

            log.Error($"Could not execute command after {tries} tries.");
        }
    }
}
