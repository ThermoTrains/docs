using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;

namespace SebastianHaeni.ThermoBox.Common
{
    public class ThermoBoxComponent
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<string, Action<string, string>> subscriptions = new Dictionary<string, Action<string, string>>();
        private string Hostname { get; set; }

        private ThermoBoxComponent()
        {
            // nop
        }

        public static ThermoBoxComponent Init()
        {
            return new ThermoBoxComponent();
        }

        public ThermoBoxComponent Host(string hostname)
        {
            Hostname = hostname;

            return this;
        }

        public ThermoBoxComponent Subscription(string channel, Action<string, string> handler)
        {
            subscriptions.Add(channel, handler);

            return this;
        }

        public void Run()
        {
            foreach (var key in subscriptions.Keys)
            {
                PubSub.Subscribe(key, subscriptions[key]);
            }

            PubSub.Subscribe(Commands.Kill, (channel, message) => Environment.Exit(0));

            // prevent exit until Ctrl-C
            while (Console.ReadLine() != null) { }

            log.Info("Shutting down");
        }
    }
}
