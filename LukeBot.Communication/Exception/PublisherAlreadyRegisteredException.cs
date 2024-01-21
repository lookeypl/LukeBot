using LukeBot.Communication.Common;


namespace LukeBot.Communication
{
    public class PublisherAlreadyRegisteredException: System.Exception
    {
        public PublisherAlreadyRegisteredException(string pubName)
            : base(string.Format("Publisher \"{0}\" already registered", pubName))
        {}
    }
}
