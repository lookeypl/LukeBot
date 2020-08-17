﻿using LukeBot.Common;
using System.IO;

namespace LukeBot.Twitch
{
    public class TwitchIRC: IModule
    {
        private string mName;
        private string mChannel;
        private string mOAuth;
        private Connection mConnection = null;

        public TwitchIRC(string name, string channel, string oauthPath)
        {
            mName = name;
            mChannel = channel;

            // TODO get an OAuth token the proper way
            StreamReader oauthStream = File.OpenText(oauthPath);
            mOAuth = oauthStream.ReadLine();
            Logger.Info("Read OAuth password from file " + oauthPath);
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

                string[] tokens = msg.Split(' ');

                if (tokens[0].Equals("PING"))
                {
                    Logger.Info("PING received - " + msg);
                    Logger.Info("Responding with PONG " + tokens[1]);
                    mConnection.Send("PONG " + tokens[1]);
                    continue;
                }

                Logger.Info(msg);
            }
        }
    }
}
