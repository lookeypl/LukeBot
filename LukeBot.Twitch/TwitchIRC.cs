using LukeBot.Common;
using System;
using System.IO;

namespace LukeBot.Twitch
{
    public class TwitchIRC: IModule
    {
        private string mName = "lukeboto";
        private string mChannel = "lookey";
        private string mOAuthPath = "Data/oauth_secret.lukebot";
        private string mOAuth;
        private Connection mConnection = null;

        public EventHandler<OnChatMessageArgs> OnChatMessage;

        void ProcessMessage(string msg)
        {
            string[] tokens = msg.Split(' ');

            // TODO PING-PONG with Twitch IRC server should be handled as a message
            if (tokens[0].Equals("PING"))
            {
                Logger.Debug("PING received - " + msg);
                Logger.Debug("Responding with PONG " + tokens[1]);
                mConnection.Send("PONG " + tokens[1]);
                return;
            }

            Logger.Info(msg);
        }

        public TwitchIRC()
        {
            // TODO get an OAuth token the proper way, aka. authenticate via Twitch as an app
            StreamReader oauthStream = File.OpenText(mOAuthPath);
            mOAuth = oauthStream.ReadLine();
            Logger.Info("Read OAuth password from file " + mOAuthPath);
        }

        ~TwitchIRC()
        {
            mConnection.Send("QUIT");
        }

        public void Init()
        {
            mConnection = new Connection("irc.chat.twitch.tv", 6697, true);

            // log in
            Logger.Info("Logging in to Twitch IRC server...");
            Logger.Debug("Login details: Name {0} Channel {1}", mName, mChannel);
            mConnection.Send("PASS " + mOAuth);
            mConnection.Send("NICK " + mName);
            mConnection.Send("JOIN #" + mChannel);

            Logger.Info("Twitch IRC module initialized");
        }

        public void Run()
        {
            Logger.Info("Awaiting response...");

            while (true)
            {
                string msg = mConnection.Read();

                if (msg == null)
                    Logger.Error("Received empty message");

                ProcessMessage(msg);
            }
        }
    }
}
