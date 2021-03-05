using LukeBot.Common;
using LukeBot.Common.OAuth;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using LukeBot;

namespace LukeBot.Twitch
{
    public class TwitchIRC: IModule
    {
        private string mName = "lukeboto";
        private Connection mConnection = null;
        private Token mToken;
        private Dictionary<string, IRCChannel> mChannels;

        private int mRunning = 0;
        private AutoResetEvent mLoggedInEvent;
        private bool mLoggedIn = false;

        private Thread mWorker;
        private Mutex mChannelsMutex;

        void ProcessSystemMsg(string msg)
        {
            Logger.Debug("Sys Recv: {0}", msg);
            string[] tokens = msg.Split(' ');

            if (tokens[1].Equals("NOTICE"))
            {
                string notice = "";
                for (int i = 3; i < tokens.Length; ++i)
                {
                    notice = String.Concat(notice, tokens[i]);
                }
                Logger.Info("Received a Notice from server: {0}", notice);
                return;
            }

            if (tokens[1].Equals("376"))
            {
                mLoggedIn = true;
                mLoggedInEvent.Set();
                return;
            }
        }

        void ProcessMessage(string msg)
        {
            if (msg == null)
                throw new ArgumentNullException("Message is NULL");

            string[] tokens = msg.Split(' ');

            // TODO PING-PONG with Twitch IRC server should be handled as an
            // event, like all other messages
            if (tokens[0].Equals("PING"))
            {
                Logger.Debug("PING received - " + msg);
                Logger.Debug("Responding with PONG " + tokens[1]);
                mConnection.Send("PONG " + tokens[1]);
                return;
            }

            // TODO this should be handled as an event too
            if (tokens[0].Equals(":tmi.twitch.tv"))
            {
                ProcessSystemMsg(msg);
                return;
            }

            if (!mLoggedIn)
                return;

            mChannelsMutex.WaitOne();

            try
            {
                if (!mChannels.ContainsKey(tokens[1]))
                    throw new InvalidDataException(String.Format("Unknown channel: {0}", tokens[1]));

                Logger.Info("Message for channel {0}: {1}", tokens[1], msg);
                // TODO CALL COMMAND
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process message: {0}", e.Message);
                mChannelsMutex.ReleaseMutex();
                return;
            }

            mChannelsMutex.ReleaseMutex();

            Logger.Info("Unrecognized message: {0}", msg);
        }

        void WorkerMain()
        {
            Logger.Info("TwitchIRC Worker thread started.");
            try
            {
                Login();
                Interlocked.Exchange(ref mRunning, 1);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to login to Twitch IRC server: {0}", e.Message);
                return;
            }

            Logger.Info("Listening for response...");

            while (Interlocked.Equals(mRunning, 1))
                ProcessMessage(mConnection.Read());
        }

        bool CheckIfLoginSuccessful()
        {
            string msg = mConnection.Read();
            if (msg == null)
                return true; // no message, we good

            string[] msgTokens = msg.Split(' ');
            if (msgTokens[1] == "NOTICE")
            {
                Logger.Info("While trying to login received Notice from Server:");
                Logger.Info("  {0}", msg);

                if (msg.Contains(":Login authentication failed"))
                    return false;
                else
                    return true;
            }

            // Login fail comes as IRC "NOTICE" call. If we don't get it,
            // assume we logged in successfully.
            Logger.Debug("Recv: {0}", msg);
            return true;
        }

        // TODO parametrize
        void Login()
        {
            mConnection = new Connection("irc.chat.twitch.tv", 6697, true);

            mToken = new TwitchToken(AuthFlow.AuthorizationCode);

            // log in
            Logger.Info("Logging in to Twitch IRC server...");
            Logger.Debug("Bot login to: {0}", mName);

            // WORKAROUND TO NOT PASS DATA TO TWITCH EVERY LAUNCH WHILE WORKING
            string token;
            string tokenPath = "Data/oauth_token.lukebot";
            string tokenScope = "chat:read chat:edit";
            bool tokenFromRequest = false;
            if (FileUtils.Exists(tokenPath))
            {
                Logger.Debug("Found already existing token, using it");
                mToken.ImportFromFile(tokenPath);
                token = mToken.Get();
            }
            else
            {
                Logger.Debug("No token found, acquiring new token");
                token = mToken.Request(tokenScope);
                mToken.ExportToFile(tokenPath);
                tokenFromRequest = true;
            }

            mConnection.Send("PASS oauth:" + token);
            mConnection.Send("NICK " + mName);
            if (!CheckIfLoginSuccessful())
            {
                if (!tokenFromRequest)
                {
                    // token might be old; refresh it
                    token = mToken.Refresh();
                    mToken.ExportToFile(tokenPath);

                    // Connection must be remade
                    mConnection.Close();
                    mConnection = new Connection("irc.chat.twitch.tv", 6697, true);

                    mConnection.Send("PASS oauth:" + token);
                    mConnection.Send("NICK " + mName);

                    if (!CheckIfLoginSuccessful())
                    {
                        File.Delete(tokenPath);
                        throw new InvalidOperationException("Failed to refresh OAuth Token; restart to login and request a fresh one");
                    }
                }
                else
                    throw new InvalidOperationException("Failed to login to Twitch IRC");
            }
        }

        void CloseConnection()
        {
            if (mConnection != null)
            {
                mConnection.Send("QUIT");
                mConnection.Close();
                mConnection = null;
            }
        }

        public TwitchIRC()
        {
            mWorker = new Thread(this.WorkerMain);
            mChannelsMutex = new Mutex();
            mLoggedInEvent = new AutoResetEvent(false);
            mChannels = new Dictionary<string, IRCChannel>();
            CommunicationManager.Instance.Register(Constants.SERVICE_NAME);
            Logger.Info("Twitch IRC module initialized");
        }

        ~TwitchIRC()
        {
            Interlocked.Exchange(ref mRunning, 0);
            CloseConnection();
            Wait();
        }

        public void JoinChannel(string channel)
        {
            mChannelsMutex.WaitOne();

            if (mChannels.ContainsKey(channel))
            {
                mChannelsMutex.ReleaseMutex();
                throw new ArgumentException(String.Format("Cannot join channel {0} - already joined", channel));
            }

            mConnection.Send("JOIN #" + channel);

            mChannels.Add(channel, new IRCChannel(channel));

            mChannelsMutex.ReleaseMutex();
        }

        public void AddCommandToChannel(string channel, string commandName, Command.ICommand command)
        {
            mChannelsMutex.WaitOne();

            if (!mChannels.ContainsKey(channel))
            {
                mChannelsMutex.ReleaseMutex();
                throw new ArgumentException(String.Format("Invalid channel name {0}", channel));
            }

            mChannels[channel].AddCommand(commandName, command);

            mChannelsMutex.ReleaseMutex();
        }

        public void AwaitLoggedIn()
        {
            mLoggedInEvent.WaitOne(30 * 1000);
        }

        // IModule overrides

        public void Init()
        {
        }

        public void Run()
        {
            mWorker.Start();
        }

        public void Wait()
        {
            mWorker.Join();
        }
    }
}
