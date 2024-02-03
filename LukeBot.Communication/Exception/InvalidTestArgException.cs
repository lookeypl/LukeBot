using LukeBot.Communication.Common;


namespace LukeBot.Communication
{
    public class InvalidTestArgException: System.Exception
    {
        public InvalidTestArgException(string arg)
            : base(string.Format("Invalid test argument: {0}", arg))
        {}
    }
}
