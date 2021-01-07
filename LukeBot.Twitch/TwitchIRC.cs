using LukeBot.Common;
using LukeBot.Common.OAuth;
using System;
using System.IO;
using LukeBot;

namespace LukeBot.Twitch
{
    public class TwitchIRC: IModule
    {
        private string mName = "lukeboto";
        private string mChannel = "lookey";
        private Connection mConnection = null;
        private Token mToken;

        public EventHandler<OnChatMessageArgs> OnChatMessage;

        void ProcessMessage(string msg)
        {
            if (msg == null)
            {
                Logger.Error("Message is NULL");
                return;
            }

            string[] tokens = msg.Split(' ');

            // TODO PING-PONG with Twitch IRC server should be handled as a message
            if (tokens[0].Equals("PING"))
            {
                Logger.Debug("PING received - " + msg);
                Logger.Debug("Responding with PONG " + tokens[1]);
                mConnection.Send("PONG " + tokens[1]);
                return;
            }

            Logger.Info("Recv: {0}", msg);
        }

        public TwitchIRC()
        {
        }

        ~TwitchIRC()
        {
            if (mConnection != null)
                mConnection.Send("QUIT");
        }

        public void Init()
        {
            CommunicationManager.Instance.Register(Constants.SERVICE_NAME);

            mConnection = new Connection("irc.chat.twitch.tv", 6697, true);

            mToken = new TwitchToken(AuthFlow.AuthorizationCode);

            // log in
            Logger.Info("Logging in to Twitch IRC server...");
            Logger.Debug("Login details: Name {0} Channel {1}", mName, mChannel);

            // WORKAROUND TO NOT PASS DATA TO TWITCH EVERY LAUNCH WHILE WORKING
            string token;
            if (FileUtils.Exists("Data/oauth_token.lukebot"))
            {
                Logger.Debug("Found already existing token, using it");
                StreamReader fileStream = File.OpenText("Data/oauth_token.lukebot");
                token = fileStream.ReadLine();
            }
            else
            {
                Logger.Debug("No token found, acquiring new token");
                token = mToken.Get("chat:read chat:edit");
                FileStream fileStream = File.OpenWrite("Data/oauth_token.lukebot");
                StreamWriter writerStream = new StreamWriter(fileStream);
                writerStream.WriteLine(token);
                writerStream.Close();
                fileStream.Close();
            }

            mConnection.Send("PASS oauth:" + token);
            mConnection.Send("NICK " + mName);
            mConnection.Send("JOIN #" + mChannel);

            string response = mConnection.Read();
            Logger.Info(response);

            Logger.Info("Twitch IRC module initialized");
        }

        public void Run()
        {
            Logger.Info("Awaiting response...");

            while (true)
            {
                string msg = mConnection.Read();

                if (msg == null)
                    throw new InvalidOperationException("Received empty message");

                ProcessMessage(msg);
            }
        }
    }
}
