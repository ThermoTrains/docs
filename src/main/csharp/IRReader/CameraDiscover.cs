using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Threading.Tasks;
using Flir.Atlas.Live.Device;
using Flir.Atlas.Live.Discovery;
using log4net;

namespace SebastianHaeni.ThermoBox.IRReader
{
    class CameraDiscover
    {
        private static readonly string CAMERA_NAME = ConfigurationManager.AppSettings["CAMERA_NAME"];
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static Discovery discovery;
        private static List<CameraDeviceInfo> foundCameras = new List<CameraDeviceInfo>();
        private static Camera camera;

        private static TaskCompletionSource<Camera> correctCameraFound = new TaskCompletionSource<Camera>();

        public async static Task<Camera> InitCameraDiscovery()
        {
            // return when correct camera found or await discovery period, if still not found use emulator
            return await Task.WhenAny(correctCameraFound.Task, DiscoverAndFallback()).Result;
        }

        private static Task<Camera> DiscoverAndFallback()
        {
            return Task.Run(() =>
            {
                discovery = new Discovery();
                discovery.DeviceFound += Discovery_DeviceFound;
                discovery.DeviceError += Discovery_DeviceError;

                log.Info("Discovering cameras");
                discovery.Start(10); // this call is blocking

                if (camera != null)
                {
                    return camera;
                }

                var emulator = foundCameras.Find(c => c.Name.Equals("Camera Emulator"));
                if (emulator != null)
                {
                    log.Warn("Fallback to emulator camera");
                    ConnectCamera(new CameraDeviceInfo(emulator));
                    return camera;
                }

                log.Error("Could not find a camera");
                Environment.Exit(1);

                return null; // doesn't matter but makes the compiler happy
            });
        }

        private static void Discovery_DeviceFound(object sender, CameraDeviceInfoEventArgs e)
        {
            foundCameras.Add(e.CameraDevice);

            if (e.CameraDevice.Name.Contains(CAMERA_NAME))
            {
                ConnectCamera(new CameraDeviceInfo(e.CameraDevice));
            }
        }

        private static void ConnectCamera(CameraDeviceInfo info)
        {
            log.Info("Stopping discovery");
            discovery.Stop();
            log.Info($"Connecting to camera: {info.Name}");
            camera = new ThermalCamera();
            camera.Connect(info);
            correctCameraFound.SetResult(camera);
        }

        private static void Discovery_DeviceError(object sender, DeviceErrorEventArgs e)
        {
            log.Error($"Device Error: {e.ErrorMessage}");
        }
    }
}
