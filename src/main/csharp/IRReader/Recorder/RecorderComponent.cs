using System;
using System.Configuration;
using System.IO;
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
using System.Collections.Generic;
using Flir.Atlas.Image;

namespace SebastianHaeni.ThermoBox.IRReader.Recorder
{
    class RecorderComponent : ThermoBoxComponent
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string CAPTURE_FOLDER = ConfigurationManager.AppSettings["CAPTURE_FOLDER"];

        private ThermalGigabitCamera camera;
        private static string currentRecording;

        private string FlirVideoFileName
        {
            get
            {
                return $@"{currentRecording}-Recording.seq";
            }
        }

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

            // It is possible to disable automatic NUCing but it is handy to have it while testing. The NUC
            // while recording might improve data quality. It is not planned to record for longer than a minute anyway.

            // The NUC consumes about 0.5 seconds. The recording will start right after. In case this starting
            // delay is too long we maybe have to consider other techniques to calibrate the camera while not recording.
            camera.DeviceControl.SetDeviceParameter("NUCAction", GenICamType.Command);

            // ensuring the recordings directory exists
            DirectoryInfo recordingDirectory = new DirectoryInfo(CAPTURE_FOLDER);
            if (!recordingDirectory.Exists)
            {
                recordingDirectory.Create();
            }

            log.Info($"Starting capture with id {message}");
            currentRecording = $@"{CAPTURE_FOLDER}\{message}";

            Retry(() => camera.Recorder.Start(FlirVideoFileName), () => camera.Recorder.Status == RecorderState.Recording);
        }

        /// <summary>
        /// Stops the recording, collects device parameters and writes them to a file, zips the folder and publishes it
        /// with cmd:delivery:upload.
        /// </summary>
        private void StopCapture()
        {
            // Copy this as a new recording might start while we're finishing this one.
            var currentRecordingFilename = currentRecording;

            if (camera.Recorder.Status != RecorderState.Recording)
            {
                log.Warn($"Cannot stop recording. Current state is {camera.Recorder.Status}");
                return;
            }

            log.Info($"Stopping capture");
            Retry(() => camera.Recorder.Stop(), () => camera.Recorder.Status == RecorderState.Stopped);
            log.Info($"Recorded {camera.Recorder.FrameCount} frames");

            // Extract first frame as reference frame and upload it.
            ExtractFirstFrame(currentRecordingFilename);

            // Upload device param file.
            CreateDeviceParamsFiles(currentRecordingFilename);

            // Forwarding FLIR video to compress it.
            Publish(Commands.Compress, currentRecordingFilename);
        }

        /// <summary>
        /// Extracts the first frame of the video as a reference. This reference image will be used to recreate the original
        /// FLIR sequence file.
        /// </summary>
        /// <param name="sourceFile"></param>
        private void ExtractFirstFrame(string sourceFile)
        {
            using (var thermalImage = new ThermalImageFile(sourceFile))
            {
                string filename = $"{sourceFile}.jpg";
                thermalImage.SaveSnapshot(filename);
                Publish(Commands.Upload, filename);
            }
        }

        /// <summary>
        /// Fetching and writing all device params to JSON (around 319 params are available on the FLIR A65).
        /// </summary>
        /// <param name="sourceFile"></param>
        private void CreateDeviceParamsFiles(string sourceFile)
        {
            List<GenICamParameter> deviceParams;
            try
            {
                deviceParams = camera.DeviceControl.GetDeviceParameters();
            }
            catch (CommandFailedException ex)
            {
                log.Error("Could not load device parameters from camera. Always fails if using emulator.", ex);
                return;
            }

            var mappedValues = (from param in deviceParams
                                where DeviceParameter.HasValue(param)
                                select new DeviceParameter(param));

            var json = JsonConvert.SerializeObject(new DeviceParameters.DeviceParameters() { Parameters = mappedValues }, Formatting.Indented);
            string deviceParamFile = $@"{sourceFile}-DeviceParams.json";
            File.WriteAllText(deviceParamFile, json);

            Publish(Commands.Upload, deviceParamFile);
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
            new DirectoryInfo(currentRecording).Delete(true);
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
                    else
                    {
                        throw new Exception("Camera state was not as expected");
                    }
                }
                catch (Exception ex)
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
