using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;


namespace LukeBot.Common
{
    public enum LogLevel
    {
        None = 0,
        Error,
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

    class LoggerSingleton
    {
        private static LoggerSingleton mInstance = null;
        private static readonly object mLock = new();
        private Mutex mLoggingMutex = new();
        private CultureInfo mCultureInfo = new("en-US");
        private Timer mTimer = new();
        private ConsoleColor mDefaultColor = Console.ForegroundColor;
        private string mProjectRootDir = "";
        private bool mAllowPreamble = true;

        public event EventHandler<LogMessageArgs> PreLogMessage;
        public event EventHandler<LogMessageArgs> PostLogMessage;

        public static LoggerSingleton Instance
        {
            get
            {
                lock (mLock)
                {
                    if (mInstance == null)
                        mInstance = new LoggerSingleton();

                    return mInstance;
                }
            }
        }

        private LoggerSingleton()
        {
            mTimer.Start();
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

        public void SetProjectRootDir(string dir)
        {
            mProjectRootDir = dir;
        }

        public void SetPreamble(bool enable)
        {
            mAllowPreamble = enable;
        }

        public void LogInternal(LogLevel level, string file, int line, string func, string msg, params object[] args)
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

            string fileToPrint;
            if (mProjectRootDir.Length > 0)
            {
                // Kind of a happy assumption, but the root dir should always be the same if it's correct
                fileToPrint = file.Substring(mProjectRootDir.Length + 1);
            }
            else
            {
                fileToPrint = file;
            }

            double timestamp = mTimer.Stop();
            string intro = string.Format(mCultureInfo, "{0:f4} {1} {2} @ {3} <{4}>: ", timestamp, tag, fileToPrint, line, func);
            string formatted = string.Format(mCultureInfo, msg, args);
            Console.ForegroundColor = color;

            LogMessageArgs msgArgs = new LogMessageArgs {
                msg = formatted,
                severity = level
            };

            {
                mLoggingMutex.WaitOne();

                OnPreLogMessage(msgArgs);
                if (mAllowPreamble)
                    Console.WriteLine(intro + formatted);
                else
                    Console.WriteLine(formatted);
                Console.ForegroundColor = mDefaultColor;
                OnPostLogMessage(msgArgs);

                mLoggingMutex.ReleaseMutex();
            }
        }

        public static bool IsLogLevelEnabled(LogLevel level)
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
    }

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
