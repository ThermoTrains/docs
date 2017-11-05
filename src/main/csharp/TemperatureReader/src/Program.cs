using SebastianHaeni.ThermoBox.TemperatureReader.Temper;

namespace SebastianHaeni.ThermoBox.TemperatureReader
{
    internal static class Program
    {
        private static void Main()
        {
            var temper = new TemperDiscover().DiscoverTemper().GetAwaiter().GetResult();
            new TemperComponent(temper).Run();
        }
    }
}
