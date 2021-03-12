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
            Trace
        }

        private static Logger mInstance = null;
        private static readonly object mLock = new object();
        private CultureInfo mCultureInfo = null;
        private Timer mTimer = null;
        private ConsoleColor mDefaultColor;

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
            default:
                tag = "[ UNKNOWN ]";
                color = mDefaultColor;
                break;
            }

            double timestamp = mTimer.Stop();
            string intro = string.Format(mCultureInfo, "{0:f4} {1} ", timestamp, tag);
            string formatted = string.Format(mCultureInfo, msg, args);
            Console.ForegroundColor = color;
            Console.WriteLine(intro + formatted);
            Console.ForegroundColor = mDefaultColor;
        }

        public static void Log(LogLevel level, string msg, params object[] args)
        {
            if (level != LogLevel.Debug && level != LogLevel.Trace)
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
    }
}
