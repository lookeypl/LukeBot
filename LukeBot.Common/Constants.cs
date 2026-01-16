using LukeBot.Config;

namespace LukeBot.Common
{
    public class Constants
    {
        public const string LUKEBOT_USER_ID = "lukebot";

        public const string PROP_STORE_SERVER_IP_PROP_NAME = "server_ip";
        public const string PROP_STORE_SERVER_PORT_PROP_NAME = "server_port";
        public const string PROP_STORE_HTTPS_DOMAIN_PROP_NAME = "https_domain";
        public const string PROP_STORE_HTTPS_EMAIL_PROP_NAME = "https_email";
        public const string PROP_STORE_USER_DOMAIN = "user";
        public const string PROP_STORE_USERS_PROP_NAME = "users";
        public const string PROP_STORE_TOKEN_PROP = "token";
        public const string PROP_STORE_LOGIN_PROP = "login";
        public const string PROP_STORE_RECONNECT_COUNT_PROP_NAME = "reconnect_count";
        public const string PROP_STORE_SERVER_PING_THRESHOLD_PROP_NAME = "ping_threshold";
        public const string PROP_STORE_TTS_ENDPOINT_PROP_NAME = "tts_endpoint";

        public static readonly Path PROP_STORE_SERVER_IP_PROP = Path.Form(LUKEBOT_USER_ID, PROP_STORE_SERVER_IP_PROP_NAME);
        public static readonly Path PROP_STORE_SERVER_PORT_PROP = Path.Form(LUKEBOT_USER_ID, PROP_STORE_SERVER_PORT_PROP_NAME);
        public static readonly Path PROP_STORE_HTTPS_DOMAIN_PROP = Path.Form(LUKEBOT_USER_ID, PROP_STORE_HTTPS_DOMAIN_PROP_NAME);
        public static readonly Path PROP_STORE_HTTPS_EMAIL_PROP = Path.Form(LUKEBOT_USER_ID, PROP_STORE_HTTPS_EMAIL_PROP_NAME);
        public static readonly Path PROP_STORE_USERS_PROP = Path.Form(LUKEBOT_USER_ID, PROP_STORE_USERS_PROP_NAME);
        public static readonly Path PROP_STORE_RECONNECT_COUNT_PROP = Path.Form(LUKEBOT_USER_ID, PROP_STORE_RECONNECT_COUNT_PROP_NAME);
        public static readonly Path PROP_STORE_SERVER_PING_THRESHOLD_PROP = Path.Form(LUKEBOT_USER_ID, PROP_STORE_SERVER_PING_THRESHOLD_PROP_NAME);
        public static readonly Path PROP_STORE_TTS_ENDPOINT_PROP = Path.Form(LUKEBOT_USER_ID, PROP_STORE_TTS_ENDPOINT_PROP_NAME);

        public const string DEFAULT_SERVER_IP = "127.0.0.1";
        public const int DEFAULT_SERVER_PORT = 443;
        public const string DEFAULT_SERVER_HTTPS_DOMAIN = "localhost";
        public const string PROPERTY_STORE_FILE = "Data/props.lukebot";
        public const string DEFAULT_LOGIN_NAME = "SET_BOT_LOGIN_HERE";
        public const string DEFAULT_CLIENT_ID_NAME = "SET_YOUR_CLIENT_ID_HERE";
        public const string DEFAULT_CLIENT_SECRET_NAME = "SET_YOUR_CLIENT_SECRET_HERE";
        public const int DEFAULT_PING_TIMER_THRESHOLD = 2 * 60 * 1000; // 2 minutes = 120 seconds in miliseconds

        public const int SERVERCLI_DEFAULT_PORT = 55268; // in T9: LKBOT

        public const string EVENT_SERVICE_NAME = "event";
        public const string INTERMEDIARY_SERVICE_NAME = "intermediary";
        public const string SPOTIFY_SERVICE_NAME = "spotify";
        public const string TWITCH_SERVICE_NAME = "twitch";
        public const string USER_SERVICE_NAME = "user";
        public const string WIDGET_SERVICE_NAME = "widget";

        public const string PROP_STORE_MODULES_DOMAIN = "modules";
        public const string PROP_STORE_WIDGETS_DOMAIN = "widgets";
        public const string PROP_STORE_ACCOUNT_DOMAIN = "account";
        public const string PROP_STORE_PASSWORD = "password";
        public const string PROP_STORE_PERMISSION_LEVEL = "permission";
    }
}
