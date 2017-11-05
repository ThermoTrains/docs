using SebastianHaeni.ThermoBox.Common.Component;
using SebastianHaeni.ThermoBox.TemperatureReader.Temper;

namespace SebastianHaeni.ThermoBox.TemperatureReader
{
    internal static class Program
    {
        private static void Main()
        {
            ComponentLauncher.Launch(() =>
                new TemperComponent(new TemperDiscover().DiscoverTemper().GetAwaiter().GetResult()));
        }
    }
}
