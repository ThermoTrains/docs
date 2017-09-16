using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using log4net;

namespace SebastianHaeni.ThermoBox.Common
{
    public abstract class ThermoBoxComponent
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string REDIS_HOST = ConfigurationManager.AppSettings["REDIS_HOST"];

        private Dictionary<string, Action<string, string>> subscriptions = new Dictionary<string, Action<string, string>>();
        private PubSub pubSub;

        protected ThermoBoxComponent()
        {
            pubSub = new PubSub(REDIS_HOST);
            Configure();
        }

        public void Run()
        {
            foreach (var key in subscriptions.Keys)
            {
                pubSub.Subscribe(key, subscriptions[key]);
            }

            pubSub.Subscribe(Commands.Kill, (channel, message) => Environment.Exit(0));

            // prevent exit until Ctrl-C
            while (Console.ReadLine() != null) { }

            log.Info("Shutting down");
        }

        protected abstract void Configure();

        protected void Subscription(string channel, Action<string, string> handler)
        {
            subscriptions.Add(channel, handler);
        }

        protected void Publish(string channel, string message)
        {
            pubSub.Publish(channel, message);
        }
    }
}
