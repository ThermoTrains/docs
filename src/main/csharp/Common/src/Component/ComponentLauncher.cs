using System;
using System.Diagnostics;
using System.Reflection;
using log4net;

namespace SebastianHaeni.ThermoBox.Common.Component
{
    public static class ComponentLauncher
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Launch(Func<ThermoBoxComponent> action)
        {
            Init();

            try
            {
                action.Invoke().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal("Uncaught exception occured", ex);
                Environment.Exit(1);
            }
        }

        private static void Init()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fvi.FileVersion;
            GlobalContext.Properties["version"] = version;
        }
    }
}
