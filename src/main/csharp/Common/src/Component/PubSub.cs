using System;
using System.Reflection;
using log4net;
using StackExchange.Redis;

namespace SebastianHaeni.ThermoBox.Common.Component
{
    public class PubSub
    {
        private readonly string _redisHost;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static ConnectionMultiplexer _redis;

        public PubSub(string redisHost)
        {
            _redisHost = redisHost;
        }

        private ConnectionMultiplexer Redis
        {
            get
            {
                if (_redis != null)
                {
                    return _redis;
                }

                Log.Info($"Connecting to redis on {_redisHost}");

                try
                {
                    _redis = ConnectionMultiplexer.Connect(_redisHost);
                }
                catch (RedisConnectionException ex)
                {
                    Log.Error("unable to connect", ex);
                    Environment.Exit(1);
                }

                Log.Info("Connected to redis");

                return _redis;
            }
        }

        public void Subscribe(string channel, Action<string, string> handler)
        {
            var sub = Redis.GetSubscriber();

            Log.Info($"Subscribing to {channel}");

            sub.Subscribe(channel, (redisChannel, message) =>
            {
                Log.Info($"Received message on channel {redisChannel}: {message}");
                handler(redisChannel, message);
            });
        }

        public void Publish(string channel, string message)
        {
            var sub = Redis.GetSubscriber();

            Log.Info($"Publishing on channel {channel}: {message}");
            sub.Publish(channel, message);
        }
    }
}
