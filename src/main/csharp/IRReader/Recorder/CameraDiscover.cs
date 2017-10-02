using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Threading.Tasks;
using Flir.Atlas.Live.Device;
using Flir.Atlas.Live.Discovery;
using log4net;

namespace SebastianHaeni.ThermoBox.IRReader.Recorder
{
    internal static class CameraDiscover
    {
        private static readonly string CameraName = ConfigurationManager.AppSettings["CAMERA_NAME"];
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly List<CameraDeviceInfo> FoundCameras = new List<CameraDeviceInfo>();
        private static Discovery _discovery;
        private static ThermalGigabitCamera _camera;

        private static readonly TaskCompletionSource<ThermalGigabitCamera> CorrectCameraFound = new TaskCompletionSource<ThermalGigabitCamera>();

        public static async Task<ThermalGigabitCamera> InitCameraDiscovery()
        {
            // return when correct camera found or await discovery period, if still not found use emulator
            return await Task.WhenAny(CorrectCameraFound.Task, DiscoverAndFallback()).Result;
        }

        private static Task<ThermalGigabitCamera> DiscoverAndFallback()
        {
            return Task.Run(() =>
            {
                _discovery = new Discovery();
                _discovery.DeviceFound += Discovery_DeviceFound;
                _discovery.DeviceError += Discovery_DeviceError;

                Log.Info("Discovering cameras");
                _discovery.Start(5); // this call is blocking

                if (_camera != null)
                {
                    return _camera;
                }

                var emulator = FoundCameras.Find(c => c.Name.Equals("Camera Emulator"));
                if (emulator != null)
                {
                    Log.Warn("Fallback to emulator camera");
                    ConnectCamera(new CameraDeviceInfo(emulator));
                    return _camera;
                }

                Log.Error("Could not find a camera");
                Environment.Exit(1);

                return null; // doesn't matter but makes the compiler happy
            });
        }

        private static void Discovery_DeviceFound(object sender, CameraDeviceInfoEventArgs e)
        {
            FoundCameras.Add(e.CameraDevice);

            if (e.CameraDevice.Name.Contains(CameraName))
            {
                ConnectCamera(new CameraDeviceInfo(e.CameraDevice));
            }
        }

        private static void ConnectCamera(CameraDeviceInfo info)
        {
            Log.Info("Stopping discovery");
            _discovery.Stop();
            Log.Info($"Connecting to camera: {info.Name}");
            _camera = new ThermalGigabitCamera();
            _camera.Connect(info);
            CorrectCameraFound.SetResult(_camera);
        }

        private static void Discovery_DeviceError(object sender, DeviceErrorEventArgs e)
        {
            Log.Error($"Device Error: {e.ErrorMessage}");
        }
    }
}
