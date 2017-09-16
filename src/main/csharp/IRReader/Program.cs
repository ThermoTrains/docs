using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Flir.Atlas.Live.Device;
using Flir.Atlas.Live.Discovery;
using Flir.Atlas.Live.Recorder;
using Flir.Atlas.Live.Remote;
using log4net;
using SebastianHaeni.ThermoBox.Common;

namespace SebastianHaeni.ThermoBox.IRReader
{
    class Program
    {
        private static readonly string REDIS_HOST = ConfigurationManager.AppSettings["REDIS_HOST"];
        private static readonly string CAMERA_NAME = ConfigurationManager.AppSettings["CAMERA_NAME"];
        private static readonly string RECORDINGS_FOLDER = ConfigurationManager.AppSettings["IR_RECORDINGS_FOLDER"];

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static Discovery discovery;
        private static List<CameraDeviceInfo> foundCameras = new List<CameraDeviceInfo>();
        private static Camera camera;
        private static string currentRecordingDirectory;

        static void Main(string[] args)
        {
            InitCameraDiscovery();
            StartComponent();
        }

        private static void InitCameraDiscovery()
        {
            discovery = new Discovery();
            discovery.DeviceFound += Discovery_DeviceFound;
            discovery.DeviceError += Discovery_DeviceError;

            log.Info("Discovering cameras");
            discovery.Start(10);

            if (camera != null)
            {
                return;
            }

            var emulator = foundCameras.Find(c => c.Name.Equals("Camera Emulator"));
            if (emulator != null)
            {
                log.Warn("Fallback to emulator camera");
                ConnectCamera(new CameraDeviceInfo(emulator));
                return;
            }

            log.Error("Could not find a camera");
            Environment.Exit(1);
        }

        private static void Discovery_DeviceFound(object sender, CameraDeviceInfoEventArgs e)
        {
            foundCameras.Add(e.CameraDevice);

            if (e.CameraDevice.Name.Contains(CAMERA_NAME))
            {
                log.Info($"Connecting to camera: {e.CameraDevice.Name}");
                ConnectCamera(new CameraDeviceInfo(e.CameraDevice));
            }
        }

        private static void ConnectCamera(CameraDeviceInfo info)
        {
            camera = new ThermalCamera();
            camera.Connect(info);
            discovery.Stop();
        }

        private static void Discovery_DeviceError(object sender, DeviceErrorEventArgs e)
        {
            log.Error($"Device Error: {e.ErrorMessage}");
        }

        private static void StartComponent()
        {
            ThermoBoxComponent
                .Init()
                .Host(REDIS_HOST)
                .Subscription(Commands.CaptureStart, (channel, message) => StartCapture(message))
                .Subscription(Commands.CaptureStop, (channel, message) => StopCapture())
                .Subscription(Commands.CaptureAbort, (channel, message) => AbortCapture())
                .Run();
        }

        private static void StartCapture(string message)
        {
            if (camera.Recorder.Status != RecorderState.Stopped)
            {
                log.Warn($"Cannot start recording. Current state is {camera.Recorder.Status}");
                return;
            }

            // ensuring the recordings directory exists
            DirectoryInfo recordingDirectory = new DirectoryInfo(RECORDINGS_FOLDER);
            if (!recordingDirectory.Exists)
            {
                recordingDirectory.Create();
            }

            log.Info($"Starting capture with id {message}");
            currentRecordingDirectory = $@"{RECORDINGS_FOLDER}\{message}";

            Retry(() => camera.Recorder.Start($@"{currentRecordingDirectory}\FLIR Recording.seq"));
        }

        private static void StopCapture()
        {
            if (camera.Recorder.Status != RecorderState.Recording)
            {
                log.Warn($"Cannot stop recording. Current state is {camera.Recorder.Status}");
                return;
            }

            log.Info($"Stopping capture");
            Retry(() => camera.Recorder.Stop());
            log.Info($"Recorded {camera.Recorder.FrameCount} frames");

            // Zipping the file as it's a non compressed format and compression rates of 50 - 80% can be achieved
            var zipFilename = $@"{currentRecordingDirectory}.zip";
            log.Info($"Zipping directory {currentRecordingDirectory} to {zipFilename}");

            if (File.Exists(zipFilename))
            {
                log.Warn($"File {zipFilename} already exists, overwriting it");
                File.Delete(zipFilename);
            }

            ZipFile.CreateFromDirectory(currentRecordingDirectory, zipFilename);

            // Deleting source artifact (that one that's uncompressed on the disk)
            new DirectoryInfo(currentRecordingDirectory).Delete(true);

            // Send publish command with the zip file
            PubSub.Publish(Commands.Upload, zipFilename);
        }

        private static void AbortCapture()
        {
            if (camera.Recorder.Status == RecorderState.Stopped)
            {
                log.Warn($"Cannot stop recording. It is already stopped");
                return;
            }

            log.Info($"Aborting capture");
            Retry(() => camera.Recorder.Stop());

            // Deleting generated artifact
            new DirectoryInfo(currentRecordingDirectory).Delete(true);
        }

        /// <summary>
        /// Tries to execute an action. If an exception is thrown, it tries again until a threshold is reached.
        /// </summary>
        /// <param name="action">Action with potential exception thrown</param>
        private static void Retry(Action action)
        {
            int tries = 0;
            int MAX_TRIES = 5;

            while (tries < MAX_TRIES)
            {
                try
                {
                    action.Invoke();
                    return;
                }
                catch (CommandFailedException ex)
                {
                    tries++;
                    log.Warn($"Error executing camera command (try {tries} of {MAX_TRIES})", ex);
                }
            }

            log.Error($"Could not execute command after {tries} tries.");
        }
    }
}
