using log4net;
using StackExchange.Redis;
using System;
using System.Reflection;

namespace SebastianHaeni.ThermoBox.IRReader
{
  class Program
  {
    private const string redistHost = "localhost";
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    static void Main(string[] args)
    {
      log.Info($"Connecting to redis on {redistHost}");

      ConnectionMultiplexer redis;
      try
      {
        redis = ConnectionMultiplexer.Connect(redistHost);
      }
      catch (RedisConnectionException ex)
      {
        log.Error("unable to connect", ex);
        return;
      }

      log.Info("Connected to redis");
      log.Info("Subscribing to cmd:capture:start");

      var sub = redis.GetSubscriber();
      sub.Subscribe(Commands.CaptureStart, (channel, message) =>
      {
        Console.WriteLine(message);
      });

      // prevent exit until Ctrl + C
      while (Console.ReadLine() != null) { }
    }
  }
}
