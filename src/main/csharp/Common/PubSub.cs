using System;
using System.Reflection;
using log4net;
using StackExchange.Redis;

namespace SebastianHaeni.ThermoBox.Common
{
    public class PubSub
    {
        private const string redistHost = "localhost";
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static ConnectionMultiplexer redis = null;

        private static ConnectionMultiplexer Redis
        {
            get
            {
                if (redis != null)
                {
                    return redis;
                }

                log.Info($"Connecting to redis on {redistHost}");

                try
                {
                    redis = ConnectionMultiplexer.Connect(redistHost);
                }
                catch (RedisConnectionException ex)
                {
                    log.Error("unable to connect", ex);
                    return null;
                }

                log.Info("Connected to redis");

                return redis;
            }
        }

        public static void Subscribe(string channel, Action<string, string> handler)
        {
            var sub = Redis.GetSubscriber();

            log.Info($"Subscribing to {channel}");

            sub.Subscribe(channel, (redisChannel, message) =>
            {
                log.Info($"Received message on channel {redisChannel}: {message}");
                handler(redisChannel, message);
            });
        }

        public static void Publish(string channel, string message)
        {
            var sub = Redis.GetSubscriber();

            log.Info($"Publishing on channel {channel}: {message}");
            sub.Publish(channel, message);
        }
    }
}
