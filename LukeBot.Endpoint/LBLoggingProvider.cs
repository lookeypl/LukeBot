using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using LukeBot.Common;


namespace LukeBot.Endpoint
{
    public class LBLoggingProvider: ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, LBLogger> mLoggers =
            new(StringComparer.OrdinalIgnoreCase);


        public ILogger CreateLogger(string categoryName)
        {
            return mLoggers.GetOrAdd(categoryName, name => new LBLogger(name, categoryName));
        }

        public void Dispose()
        {
            mLoggers.Clear();
        }
    }
}