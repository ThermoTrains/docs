using System;
using System.Collections.Generic;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
using SharpRaven;
using SharpRaven.Data;

namespace SebastianHaeni.ThermoBox.Common.Util
{
    // ReSharper disable once UnusedMember.Global
    public class RavenAppender : AppenderSkeleton
    {
        private IRavenClient _ravenClient;

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        // ReSharper disable once InconsistentNaming
        private string DSN { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private string Logger { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (DSN == null)
            {
                return;
            }

            var dsn = Environment.ExpandEnvironmentVariables(DSN);

            if (dsn.Length == 0 || dsn.Equals("%SENTRY_DSN%"))
            {
                return;
            }

            if (_ravenClient == null)
            {
                _ravenClient = new RavenClient(dsn)
                {
                    Logger = Logger,
                    ErrorOnCapture = ex => LogLog.Error(typeof(RavenAppender), ex.Message, ex)
                };
            }

            var level = Translate(loggingEvent.Level);

            var sentryEvent = loggingEvent.ExceptionObject != null
                ? new SentryEvent(loggingEvent.ExceptionObject)
                : new SentryEvent(loggingEvent.MessageObject as string);

            sentryEvent.Level = level;
            sentryEvent.Tags = new Dictionary<string, string> {{"assembly", AppDomain.CurrentDomain.FriendlyName}};

            _ravenClient.Capture(sentryEvent);
        }

        private static ErrorLevel Translate(Level level)
        {
            switch (level.DisplayName)
            {
                case "WARN":
                    return ErrorLevel.Warning;
                case "NOTICE":
                    return ErrorLevel.Info;
            }

            return !Enum.TryParse(level.DisplayName, true, out ErrorLevel errorLevel)
                ? ErrorLevel.Error
                : errorLevel;
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            foreach (var loggingEvent in loggingEvents)
            {
                Append(loggingEvent);
            }
        }
    }
}
