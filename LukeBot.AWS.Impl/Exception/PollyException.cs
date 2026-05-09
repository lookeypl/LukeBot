using System;
using LukeBot.Common;


namespace LukeBot.AWS.Impl
{
    public class PollyException: LukeBot.Common.Exception
    {
        public PollyException(string reasonFmt, params object[] args)
            : base("Polly request failed: " + String.Format(reasonFmt, args))
        {}
    }
}
