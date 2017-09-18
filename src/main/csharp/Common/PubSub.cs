using System;
using System.Reflection;
using log4net;
using StackExchange.Redis;

namespace SebastianHaeni.ThermoBox.Common
{
    public class PubSub
    {
        private string redisHost;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static ConnectionMultiplexer redis = null;

        public PubSub(string redisHost)
        {
            this.redisHost = redisHost;
        }

        private ConnectionMultiplexer Redis
        {
            get
            {
                if (redis != null)
                {
                    return redis;
                }

                log.Info($"Connecting to redis on {redisHost}");

                try
                {
                    redis = ConnectionMultiplexer.Connect(redisHost);
                }
                catch (RedisConnectionException ex)
                {
                    log.Error("unable to connect", ex);
                    Environment.Exit(1);

                    return null;
                }

                log.Info("Connected to redis");

                return redis;
            }
        }

        public void Subscribe(string channel, Action<string, string> handler)
        {
            var sub = Redis.GetSubscriber();

            log.Info($"Subscribing to {channel}");

            sub.Subscribe(channel, (redisChannel, message) =>
            {
                log.Info($"Received message on channel {redisChannel}: {message}");
                handler(redisChannel, message);
            });
        }

        public void Publish(string channel, string message)
        {
            var sub = Redis.GetSubscriber();

            log.Info($"Publishing on channel {channel}: {message}");
            sub.Publish(channel, message);
        }
    }
}
