using SebastianHaeni.ThermoBox.IRReader.Recorder;

namespace SebastianHaeni.ThermoBox.IRReader
{
    internal static class Program
    {
        private static void Main()
        {
            var camera = CameraDiscover.InitCameraDiscovery().GetAwaiter().GetResult();
            new RecorderComponent(camera).Run();
        }
    }
}
