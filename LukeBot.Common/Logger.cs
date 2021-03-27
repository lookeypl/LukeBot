using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace LukeBot.Common
{
    public class Logger
    {
        public enum LogLevel
        {
            Error = 0,
            Warning,
            Info,
            Debug,
            Trace,
            Secure,
        }

        public struct LogMessageArgs
        {
            public LogLevel severity;
            public string msg;
        }

        private static Logger mInstance = null;
        private static readonly object mLock = new object();
        private CultureInfo mCultureInfo = null;
        private Timer mTimer = null;
        private ConsoleColor mDefaultColor;

        public static event EventHandler<LogMessageArgs> PreLogMessage;
        public static event EventHandler<LogMessageArgs> PostLogMessage;

        private static Logger Instance
        {
            get
            {
                lock (mLock)
                {
                    if (mInstance == null)
                        mInstance = new Logger();

                    return mInstance;
                }
            }
        }

        private Logger()
        {
            mCultureInfo = new CultureInfo("en-US");
            mTimer = new Timer();
            mTimer.Start();

            mDefaultColor = Console.ForegroundColor;
        }

        private void OnPreLogMessage(LogMessageArgs args)
        {
            EventHandler<LogMessageArgs> handler = PreLogMessage;
            if (handler != null)
                handler(this, args);
        }

        private void OnPostLogMessage(LogMessageArgs args)
        {
            EventHandler<LogMessageArgs> handler = PostLogMessage;
            if (handler != null)
                handler(this, args);
        }

        private void LogInternal(LogLevel level, string msg, params object[] args)
        {
            string tag;
            ConsoleColor color;

            switch (level)
            {
            case LogLevel.Error:
                tag = "[ ERROR ]";
                color = ConsoleColor.Red;
                break;
            case LogLevel.Warning:
                tag = "[WARNING]";
                color = ConsoleColor.Yellow;
                break;
            case LogLevel.Info:
                tag = "[ INFO  ]";
                color = mDefaultColor;
                break;
            case LogLevel.Debug:
                tag = "[ DEBUG ]";
                color = ConsoleColor.DarkCyan;
                break;
            case LogLevel.Trace:
                tag = "[ TRACE ]";
                color = ConsoleColor.DarkGray;
                break;
            case LogLevel.Secure:
                tag = "[ SECURE ]";
                color = ConsoleColor.DarkMagenta;
                break;
            default:
                tag = "[ UNKNOWN ]";
                color = mDefaultColor;
                break;
            }

            double timestamp = mTimer.Stop();
            string intro = string.Format(mCultureInfo, "{0:f4} {1} ", timestamp, tag);
            string formatted = string.Format(mCultureInfo, msg, args);
            Console.ForegroundColor = color;

            LogMessageArgs msgArgs = new LogMessageArgs {
                msg = formatted,
                severity = level
            };

            OnPreLogMessage(msgArgs);
            Console.WriteLine(intro + formatted);
            Console.ForegroundColor = mDefaultColor;
            OnPostLogMessage(msgArgs);
        }

        private static bool IsLogLevelEnabled(LogLevel level)
        {
            switch (level)
            {
            case LogLevel.Error:
            case LogLevel.Warning:
            case LogLevel.Info:
                return true;
            case LogLevel.Debug:
                #if (DEBUG)
                    return true;
                #else
                    return false;
                #endif
            case LogLevel.Trace:
                #if (TRACE)
                    return true;
                #else
                    return false;
                #endif
            case LogLevel.Secure:
                #if (ENABLE_SECURE_LOGS)
                    return true;
                #else
                    return false;
                #endif
            default:
                return false;
            }
        }

        public static void Log(LogLevel level, string msg, params object[] args)
        {
            if (IsLogLevelEnabled(level))
                Instance.LogInternal(level, msg, args);
        }

        public static void Error(string msg, params object[] args)
        {
            Instance.LogInternal(LogLevel.Error, msg, args);
        }

        public static void Warning(string msg, params object[] args)
        {
            Instance.LogInternal(LogLevel.Warning, msg, args);
        }

        public static void Info(string msg, params object[] args)
        {
            Instance.LogInternal(LogLevel.Info, msg, args);
        }

        public static void Debug(string msg, params object[] args)
        {
        #if (DEBUG)
            Instance.LogInternal(LogLevel.Debug, msg, args);
        #endif
        }

        public static void Trace(string msg, params object[] args)
        {
        #if (TRACE)
            Instance.LogInternal(LogLevel.Trace, msg, args);
        #endif
        }

        public static void Secure(string msg, params object[] args)
        {
        #if (ENABLE_SECURE_LOGS)
            Instance.LogInternal(LogLevel.Secure, msg, args);
        #endif
        }
    }
}
