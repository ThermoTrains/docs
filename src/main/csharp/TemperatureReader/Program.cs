using SebastianHaeni.ThermoBox.TemperatureReader.Temper;

namespace SebastianHaeni.ThermoBox.TemperatureReader
{
    class Program
    {
        static void Main(string[] args)
        {
            var temper = new TemperDiscover().DiscoverTemper().GetAwaiter().GetResult();
            new TemperComponent(temper).Run();
        }
    }
}
