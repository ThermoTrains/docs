using SebastianHaeni.ThermoBox.Common.Component;
using SebastianHaeni.ThermoBox.IRReader.Recorder;

namespace SebastianHaeni.ThermoBox.IRReader
{
    internal static class Program
    {
        private static void Main()
        {
            ComponentLauncher.Launch(() =>
                new RecorderComponent(CameraDiscover.InitCameraDiscovery().GetAwaiter().GetResult()));
        }
    }
}
