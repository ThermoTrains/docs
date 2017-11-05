using SebastianHaeni.ThermoBox.Common.Component;

namespace SebastianHaeni.ThermoBox.IRCompressor
{
    internal static class Program
    {
        public static void Main()
        {
            ComponentLauncher.Launch(() => new CompressorComponent());
        }
    }
}
