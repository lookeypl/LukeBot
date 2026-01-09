using LukeBot.Communication;


namespace LukeBot.Communication.Impl
{
    public class PublisherAlreadyRegisteredException: System.Exception
    {
        public PublisherAlreadyRegisteredException(string pubName)
            : base(string.Format("Publisher \"{0}\" already registered", pubName))
        {}
    }
}
