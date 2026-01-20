using System;
using LukeBot.Common;
using LukeBot.Config;


namespace LukeBot.API
{
    internal class Utils
    {
        public static string GetCallbackDomainAndPort()
        {
            string domain = Conf.Get<string>(Constants.PROP_STORE_HTTPS_DOMAIN_PROP);

            if (Conf.TryGet<int>(Constants.PROP_STORE_SERVER_PORT_PROP, out int port))
            {
                // assumes we use HTTPS always, so this "update" to returned domain
                // should only be done if the port is not standard HTTPS 443 port
                if (port != 443)
                {
                    domain += String.Format(":{0}", port);
                }
            }

            return domain;
        }
    }
}