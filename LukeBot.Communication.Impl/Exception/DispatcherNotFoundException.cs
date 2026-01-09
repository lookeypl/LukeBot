using LukeBot.Communication;


namespace LukeBot.Communication.Impl
{
    public class DispatcherNotFoundException: LukeBot.Common.Exception
    {
        public DispatcherNotFoundException(string dispatcherName)
            : base(string.Format("Not found dispatcher: {0}", dispatcherName))
        {}
    }
}
