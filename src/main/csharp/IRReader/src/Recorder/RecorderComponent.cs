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
using SebastianHaeni.ThermoBox.IRReader.DeviceParameters;
using System.Collections.Generic;
using Flir.Atlas.Image;
using SebastianHaeni.ThermoBox.Common.Component;

namespace SebastianHaeni.ThermoBox.IRReader.Recorder
{
    internal class RecorderComponent : ThermoBoxComponent
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string CaptureFolder = ConfigurationManager.AppSettings["CAPTURE_FOLDER"];

        private readonly ThermalGigabitCamera _camera;
        private static string _currentRecording;

        private static string FlirVideoFileName => $@"{_currentRecording}-Recording.seq";

        public RecorderComponent(ThermalGigabitCamera camera)
        {
            _camera = camera;
            camera.ConnectionStatusChanged += ConnectionStatusChanged;

            Subscription(Commands.CaptureStart, (channel, message) => StartCapture(message));
            Subscription(Commands.CaptureStop, (channel, message) => StopCapture());
            Subscription(Commands.CaptureAbort, (channel, message) => AbortCapture());
        }

        private static void ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            if (e.Status == ConnectionStatus.Connected)
            {
                return;
            }

            Log.Error("Lost connection to camera => Exiting");
            Environment.Exit(1);
        }

        /// <summary>
        /// Starts a new recording. Before recording starts, a short calibration is executed.
        /// </summary>
        /// <param name="message"></param>
        private void StartCapture(string message)
        {
            if (_camera.Recorder.Status != RecorderState.Stopped)
            {
                Log.Warn($"Cannot start recording. Current state is {_camera.Recorder.Status}");
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
            _camera.DeviceControl.SetDeviceParameter("NUCAction", GenICamType.Command);

            // ensuring the recordings directory exists
            var recordingDirectory = new DirectoryInfo(CaptureFolder);
            if (!recordingDirectory.Exists)
            {
                recordingDirectory.Create();
            }

            Log.Info($"Starting capture with id {message}");
            _currentRecording = $@"{CaptureFolder}\{message}";

            Retry(() => _camera.Recorder.Start(FlirVideoFileName),
                () => _camera.Recorder.Status == RecorderState.Recording);
        }

        /// <summary>
        /// Stops the recording, collects device parameters and writes them to a file, zips the folder and publishes it
        /// with cmd:delivery:upload.
        /// </summary>
        private void StopCapture()
        {
            // Copy this because a new recording might start while we're finishing this one.
            var currentRecordingFilename = _currentRecording;

            if (_camera.Recorder.Status != RecorderState.Recording)
            {
                Log.Warn($"Cannot stop recording. Current state is {_camera.Recorder.Status}");
                return;
            }

            Log.Info("Stopping capture");
            Retry(() => _camera.Recorder.Stop(), () => _camera.Recorder.Status == RecorderState.Stopped);
            Log.Info($"Recorded {_camera.Recorder.FrameCount} frames");

            // Extract first frame as reference frame and upload it.
            ExtractSnapshot(FlirVideoFileName);

            // Upload device param file.
            CreateDeviceParamsFiles(currentRecordingFilename);

            // Forwarding FLIR video to compress it.
            Publish(Commands.Compress, currentRecordingFilename);
        }

        /// <summary>
        /// Extracts the first frame of the video as a reference. This reference image will be used as a safety net
        /// in case something goes wrong with the compression or decompression. In the future this step may be removed.
        /// </summary>
        /// <param name="sourceFile"></param>
        private void ExtractSnapshot(string sourceFile)
        {
            using (var thermalImage = new ThermalImageFile(sourceFile))
            {
                var filename = $"{sourceFile}.jpg";
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
                deviceParams = _camera.DeviceControl.GetDeviceParameters();
            }
            catch (CommandFailedException ex)
            {
                Log.Error("Could not load device parameters from camera. Always fails if using emulator.", ex);
                return;
            }

            var mappedValues = from param in deviceParams
                where DeviceParameter.HasValue(param)
                select new DeviceParameter(param);

            var json = JsonConvert.SerializeObject(new DeviceParameters.DeviceParameters
                {
                    Parameters = mappedValues
                },
                Formatting.Indented);
            var deviceParamFile = $@"{sourceFile}-DeviceParams.json";
            File.WriteAllText(deviceParamFile, json);

            Publish(Commands.Upload, deviceParamFile);
        }

        /// <summary>
        /// Aborts a running capture by stopping the recording and purging the generated artifact.
        /// </summary>
        private void AbortCapture()
        {
            if (_camera.Recorder.Status == RecorderState.Stopped)
            {
                Log.Warn("Cannot stop recording. It is already stopped");
                return;
            }

            Log.Info("Aborting capture");
            Retry(() => _camera.Recorder.Stop(), () => _camera.Recorder.Status == RecorderState.Stopped);

            // Deleting generated artifact
            new DirectoryInfo(_currentRecording).Delete(true);
        }

        /// <summary>
        /// Tries to execute an action. If an exception is thrown, it tries again until a threshold is reached.
        /// </summary>
        /// <param name="action">Action with potential exception thrown</param>
        /// <param name="testSuccess">Function to test the success of the action</param>
        private static void Retry(Action action, Func<bool> testSuccess)
        {
            var tries = 0;
            const int maxTries = 5;

            while (tries < maxTries)
            {
                try
                {
                    action.Invoke();

                    if (testSuccess.Invoke())
                    {
                        return;
                    }

                    throw new Exception("Camera state was not as expected");
                }
                catch (Exception ex)
                {
                    Log.Warn("Exception while executing camera command", ex);
                }

                tries++;
                Log.Warn($"Failed executing camera command (try {tries} of {maxTries})");
                Thread.Sleep(10);
            }

            Log.Error($"Could not execute command after {tries} tries.");
        }
    }
}
