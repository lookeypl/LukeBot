using System;

namespace LukeBot.Common {

public class Logger
{
    enum LogLevel
    {
        Error = 0,
        Warning,
        Info,
        Debug,
        Trace
    }

    private static Logger mInstance = null;
    private static readonly object mLock = new object();

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
    }

    private void Log(LogLevel level, string msg, params object[] args)
    {
        string tag;

        switch (level)
        {
        case LogLevel.Error:
            tag = "[ ERROR ] ";
            break;
        case LogLevel.Warning:
            tag = "[WARNING] ";
            break;
        case LogLevel.Info:
            tag = "[ INFO  ] ";
            break;
        case LogLevel.Debug:
            tag = "[ DEBUG ] ";
            break;
        case LogLevel.Trace:
            tag = "[ TRACE ] ";
            break;
        default:
            tag = "[ UNKNOWN ] ";
            break;
        }

        string formatted = string.Format(msg, args);
        Console.WriteLine(tag + formatted);
    }

    public static void LogE(string msg, params object[] args)
    {
        Instance.Log(LogLevel.Error, msg, args);
    }

    public static void LogW(string msg, params object[] args)
    {
        Instance.Log(LogLevel.Warning, msg, args);
    }

    public static void LogI(string msg, params object[] args)
    {
        Instance.Log(LogLevel.Info, msg, args);
    }

    public static void LogD(string msg, params object[] args)
    {
    #if (DEBUG)
        Instance.Log(LogLevel.Debug, msg, args);
    #endif
    }

    public static void LogT(string msg, params object[] args)
    {
    #if (TRACE)
        Instance.Log(LogLevel.Trace, msg, args);
    #endif
    }
}

}
