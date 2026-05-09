namespace LukeBot.Common
{
    public class InvalidCredentialsException: Exception
    {
        public InvalidCredentialsException(string service)
            : base(string.Format("Credentials for service {0} invalid or not set.", service))
        {}
    }
}
