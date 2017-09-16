namespace SebastianHaeni.ThermoBox.IRReader
{
    class Program
    {
        static void Main(string[] args)
        {
            var camera = CameraDiscover.InitCameraDiscovery().GetAwaiter().GetResult();
            new RecorderComponent(camera).Run();
        }
    }
}
