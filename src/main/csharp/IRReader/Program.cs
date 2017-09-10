using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO.Compression;
using System.Reflection;
using Flir.Atlas.Live.Device;
using Flir.Atlas.Live.Discovery;
using log4net;
using SebastianHaeni.ThermoBox.Common;

namespace SebastianHaeni.ThermoBox.IRReader
{
    class Program
    {
        private static readonly string REDIS_HOST = ConfigurationManager.AppSettings["REDIS_HOST"];
        private static readonly string CAMERA_NAME = ConfigurationManager.AppSettings["CAMERA_NAME"];
        private static readonly string RECORDINGS_FOLDER = ConfigurationManager.AppSettings["RECORDINGS_FOLDER"];

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
            discovery.DeviceLost += Discovery_DeviceLost;
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

                return;
            }
        }

        private static void ConnectCamera(CameraDeviceInfo info)
        {
            camera = new ThermalCamera();
            camera.Connect(info);
            discovery.Stop();
        }

        private static void Discovery_DeviceLost(object sender, CameraDeviceInfoEventArgs e)
        {
            throw new NotImplementedException();
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
                .Subscription(Commands.CaptureStart, (channel, message) =>
                {
                    log.Info($"Starting capture with id {message}");
                    currentRecordingDirectory = $@"{RECORDINGS_FOLDER}\{message}";
                    camera.Recorder.Start($@"{currentRecordingDirectory}\FLIR Recording.seq");
                })
                .Subscription(Commands.CaptureStop, (channel, message) =>
                {
                    log.Info($"Stopping capture");
                    camera.Recorder.Stop();

                    // Zipping the file as it's a non compressed format and compression rates of 50 - 80% can be achieved
                    var zipFilename = $@"{currentRecordingDirectory}.zip";
                    log.Info($"Zipping {currentRecordingDirectory}");
                    ZipFile.CreateFromDirectory(currentRecordingDirectory, zipFilename);

                    // Send publish command with the zip file
                    PubSub.Publish(Commands.Upload, zipFilename);
                })
                .Run();
        }
    }
}
