namespace LukeBot.Common
{
    public class Constants
    {
        public const string PROP_STORE_LUKEBOT_DOMAIN = "lukebot";
        public const string PROP_STORE_SERVER_IP_PROP_NAME = "server_ip";
        public const string PROP_STORE_USER_DOMAIN = "user";
        public const string PROP_STORE_TOKEN_PROP = "token";

        public static readonly string PROP_STORE_SERVER_IP_PROP = Utils.FormConfName(PROP_STORE_LUKEBOT_DOMAIN, PROP_STORE_SERVER_IP_PROP_NAME);
        public const string SERVER_IP_FILE = "server_ip";
        public const string DEFAULT_SERVER_IP = "127.0.0.1";
        public const string PROPERTY_STORE_FILE = "Data/props.lukebot";
        public const string DEFAULT_LOGIN_NAME = "SET_BOT_LOGIN_HERE";
        public const string DEFAULT_CLIENT_ID_NAME = "SET_YOUR_CLIENT_ID_HERE";
        public const string DEFAULT_CLIENT_SECRET_NAME = "SET_YOUR_CLIENT_SECRET_HERE";
    }
}
