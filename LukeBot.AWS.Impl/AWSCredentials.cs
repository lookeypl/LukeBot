using System;
using LukeBot.Common;
using LukeBot.Config;


namespace LukeBot.AWS.Impl
{
    internal class AWSCredentials
    {
        private static readonly Config.Path CLIENT_ID_PATH = Path.Start()
                                                                 .Push(Common.Constants.AWS_SERVICE_NAME)
                                                                 .Push(Common.Constants.PROP_STORE_CLIENT_ID_PROP_NAME);
        private static readonly Config.Path CLIENT_SECRET_PATH = Path.Start()
                                                                     .Push(Common.Constants.AWS_SERVICE_NAME)
                                                                     .Push(Common.Constants.PROP_STORE_CLIENT_SECRET_PROP_NAME);

        private static string mClientID = String.Empty;
        private static string mClientSecret = String.Empty;

        public static string ID
        {
            get
            {
                if (String.IsNullOrEmpty(mClientID))
                {
                    mClientID = Conf.Get<string>(CLIENT_ID_PATH);
                    if (mClientID == Common.Constants.DEFAULT_CLIENT_ID_NAME)
                    {
                        mClientID = String.Empty;
                        throw new InvalidCredentialsException(Common.Constants.AWS_SERVICE_NAME);
                    }
                }

                return mClientID;
            }
        }

        public static string Secret
        {
            get
            {
                if (String.IsNullOrEmpty(mClientSecret))
                {
                    mClientSecret = Conf.Get<string>(CLIENT_SECRET_PATH);
                    if (mClientSecret == Common.Constants.DEFAULT_CLIENT_SECRET_NAME)
                    {
                        mClientSecret = String.Empty;
                        throw new InvalidCredentialsException(Common.Constants.AWS_SERVICE_NAME);
                    }
                }

                return mClientSecret;
            }
        }
    }
}
