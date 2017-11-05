using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using log4net;

namespace SebastianHaeni.ThermoBox.Common.Component
{
    public abstract class ThermoBoxComponent
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string RedisHost = ConfigurationManager.AppSettings["REDIS_HOST"];

        private readonly Dictionary<string, Action<string, string>> _subscriptions =
            new Dictionary<string, Action<string, string>>();

        private readonly PubSub _pubSub;

        protected ThermoBoxComponent()
        {
            _pubSub = new PubSub(RedisHost);
        }

        public void Run()
        {
            foreach (var key in _subscriptions.Keys)
            {
                _pubSub.Subscribe(key, _subscriptions[key]);
            }

            _pubSub.Subscribe(Commands.Kill, (channel, message) => Environment.Exit(0));

            // prevent exit until Ctrl-C
            while (Console.ReadLine() != null)
            {
            }

            Log.Info("Shutting down");
        }

        protected void Subscription(string channel, Action<string, string> handler)
        {
            _subscriptions.Add(channel, handler);
        }

        protected void Publish(string channel, string message)
        {
            _pubSub.Publish(channel, message);
        }
    }
}
