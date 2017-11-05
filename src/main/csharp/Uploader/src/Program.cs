using SebastianHaeni.ThermoBox.Common.Component;

namespace SebastianHaeni.ThermoBox.Uploader
{
    internal static class Program
    {
        private static void Main()
        {
            ComponentLauncher.Launch(() => new UploadComponent());
        }
    }
}
