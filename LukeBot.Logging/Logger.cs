using System;
using System.Runtime.CompilerServices;


namespace LukeBot.Logging
{
    public class Log
    {
        private string mFile;
        private int mLine;
        private string mFunc;

        public Log(string file, int line, string func)
        {
            mFile = file;
            mLine = line;
            mFunc = func;
        }

        public void Message(LogLevel level, string msg, params object[] args)
        {
            if (LoggerSingleton.IsLogLevelEnabled(level))
                LoggerSingleton.Instance.LogInternal(level, mFile, mLine, mFunc, msg, args);
        }

        public void Error(string msg, params object[] args)
        {
            LoggerSingleton.Instance.LogInternal(LogLevel.Error, mFile, mLine, mFunc, msg, args);
        }

        public void Warning(string msg, params object[] args)
        {
            LoggerSingleton.Instance.LogInternal(LogLevel.Warning, mFile, mLine, mFunc, msg, args);
        }

        public void Info(string msg, params object[] args)
        {
            LoggerSingleton.Instance.LogInternal(LogLevel.Info, mFile, mLine, mFunc, msg, args);
        }

        public void Debug(string msg, params object[] args)
        {
        #if (DEBUG)
            LoggerSingleton.Instance.LogInternal(LogLevel.Debug, mFile, mLine, mFunc, msg, args);
        #endif
        }

        public void Trace(string msg, params object[] args)
        {
        #if (TRACE)
            LoggerSingleton.Instance.LogInternal(LogLevel.Trace, mFile, mLine, mFunc, msg, args);
        #endif
        }

        public void Secure(string msg, params object[] args)
        {
        #if (ENABLE_SECURE_LOGS)
            LoggerSingleton.Instance.LogInternal(LogLevel.Secure, mFile, mLine, mFunc, msg, args);
        #endif
        }
    }

    public class Logger
    {
        // TODO HACK has to be removed when CLI is done properly (via separate app)
        public static void AddPreMessageEvent(EventHandler<LogMessageArgs> f)
        {
            LoggerSingleton.Instance.PreLogMessage += f;
        }

        // TODO HACK has to be removed when CLI is done properly (via separate app)
        public static void AddPostMessageEvent(EventHandler<LogMessageArgs> f)
        {
            LoggerSingleton.Instance.PostLogMessage += f;
        }

        public static bool IsLogLevelEnabled(LogLevel level)
        {
            return LoggerSingleton.IsLogLevelEnabled(level);
        }

        public static void SetProjectRootDir(string dir)
        {
            LoggerSingleton.Instance.SetProjectRootDir(dir);
        }

        public static void SetPreamble(bool enabled)
        {
            LoggerSingleton.Instance.SetPreamble(enabled);
        }

        public static Log Log([CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string func = "")
        {
            return new Log(file, line, func);
        }
    }
}
