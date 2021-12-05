using System;

namespace LukeBot.Common
{
    public class Exception: System.Exception
    {
        public Exception(string msg): base(msg)
        {
        }

        public void Print(LogLevel level)
        {
            Logger.Log().Message(level, "{0} caught: {1}", this.GetType().FullName, Message);
            Logger.Log().Message(level, "Stack trace:");
            string[] stack = StackTrace.Split('\n');
            foreach (string s in stack)
            {
                Logger.Log().Message(level, "{0}", s);
            }
        }
    }
}
