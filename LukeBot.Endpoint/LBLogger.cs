using MSLogging = Microsoft.Extensions.Logging;
using LukeBot.Common;
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

        private LukeBot.Common.LogLevel MSLogLevelToLBLogLevel(MSLogging.LogLevel level)
        {
            switch (level)
            {
            case MSLogging.LogLevel.Trace: return LukeBot.Common.LogLevel.Trace;
            case MSLogging.LogLevel.Debug: return LukeBot.Common.LogLevel.Debug;
            case MSLogging.LogLevel.Information: return LukeBot.Common.LogLevel.Info;
            case MSLogging.LogLevel.Warning: return LukeBot.Common.LogLevel.Warning;
            case MSLogging.LogLevel.Error: return LukeBot.Common.LogLevel.Error;
            case MSLogging.LogLevel.Critical: return LukeBot.Common.LogLevel.Error;
            default: return LukeBot.Common.LogLevel.None;
            }
        }

        public IDisposable BeginScope<TState>(TState state) where TState: notnull => default!;

        public bool IsEnabled(MSLogging.LogLevel logLevel)
        {
            return Logger.IsLogLevelEnabled(MSLogLevelToLBLogLevel(logLevel));
        }

        public void Log<TState>(MSLogging.LogLevel logLevel, MSLogging.EventId eventId, TState state, System.Exception exception, Func<TState, System.Exception, string> formatter)
        {
            Logger.Log().Message(MSLogLevelToLBLogLevel(logLevel), "Kestrel: {1}", formatter(state, exception));
        }
    }
}