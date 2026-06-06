using LukeBot.Communication;


namespace LukeBot.Communication.Impl
{
    public class TestEventFailedException: System.Exception
    {
        public TestEventFailedException(string eventName, string reason)
            : base(string.Format("Test event {0} failed to emit: {1}", eventName, reason))
        {}
    }
}
