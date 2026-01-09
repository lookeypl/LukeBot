using LukeBot.Communication;


namespace LukeBot.Communication.Impl
{
    public class NoEventProvidedException: System.Exception
    {
        public NoEventProvidedException()
            : base(string.Format("No event was provided"))
        {}
    }
}
