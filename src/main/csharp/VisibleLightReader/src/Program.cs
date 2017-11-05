using SebastianHaeni.ThermoBox.Common.Component;

namespace SebastianHaeni.ThermoBox.VisibleLightReader
{
    internal static class Program
    {
        public static void Main()
        {
            ComponentLauncher.Launch(() => new VisibleLightReaderComponent());
        }
    }
}
