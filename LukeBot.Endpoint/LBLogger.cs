using MSLogging = Microsoft.Extensions.Logging;
using LukeBot.Logging;
using System;

namespace LukeBot.Endpoint
{
    public class LBLogger: MSLogging.ILogger
    {
        private string mName;
        private string mCategory;

        public LBLogger(string name, string category)
        {
            mName = name;
            mCategory = category;

            Logger.Log().Debug("Created LBLogger {0} for Kestrel with category {0}", mName, mCategory);
        }

        private LukeBot.Logging.LogLevel MSLogLevelToLBLogLevel(MSLogging.LogLevel level)
        {
            switch (level)
            {
            case MSLogging.LogLevel.Trace: return LukeBot.Logging.LogLevel.Trace;
            case MSLogging.LogLevel.Debug: return LukeBot.Logging.LogLevel.Debug;
            case MSLogging.LogLevel.Information: return LukeBot.Logging.LogLevel.Info;
            case MSLogging.LogLevel.Warning: return LukeBot.Logging.LogLevel.Warning;
            case MSLogging.LogLevel.Error: return LukeBot.Logging.LogLevel.Error;
            case MSLogging.LogLevel.Critical: return LukeBot.Logging.LogLevel.Error;
            default: return LukeBot.Logging.LogLevel.None;
            }
        }

        public IDisposable BeginScope<TState>(TState state) where TState: notnull => default!;

        public bool IsEnabled(MSLogging.LogLevel logLevel)
        {
            return Logger.IsLogLevelEnabled(MSLogLevelToLBLogLevel(logLevel));
        }

        public void Log<TState>(MSLogging.LogLevel logLevel, MSLogging.EventId eventId, TState state, System.Exception exception, Func<TState, System.Exception, string> formatter)
        {
            Logger.Log().Message(MSLogLevelToLBLogLevel(logLevel), "Kestrel: {0}", formatter(state, exception));
        }
    }
}