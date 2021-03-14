using LukeBot.Common;
using LukeBot.Common.OAuth;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;


namespace LukeBot.Twitch
{
    public class TwitchIRC: IModule
    {
        private string mName = "lukeboto";
        private Connection mConnection = null;
        private Token mToken;
        private Dictionary<string, IRCChannel> mChannels;

        private bool mRunning = false;
        private AutoResetEvent mLoggedInEvent;

        private Thread mWorker;
        private Mutex mChannelsMutex;

        void ProcessReply(Message m)
        {
            switch (m.Reply)
            {
            case IRCReply.RPL_TWITCH_WELCOME1:
            case IRCReply.RPL_TWITCH_WELCOME2:
            case IRCReply.RPL_TWITCH_WELCOME3:
            case IRCReply.RPL_TWITCH_WELCOME4:
                Logger.Info("Welcome msg: {0}", m.ParamsString);
                break;
            case IRCReply.RPL_MOTDSTART:
                Logger.Info("Server's Message of the Day:");
                Logger.Info("  {0}", m.ParamsString);
                break;
            case IRCReply.RPL_MOTD:
                Logger.Info("  {0}", m.ParamsString);
                break;
            case IRCReply.RPL_ENDOFMOTD:
                Logger.Info("  {0}", m.ParamsString);
                Logger.Info("End of Message of the Day");
                mLoggedInEvent.Set();
                break;
            default:
                Logger.Info("Reply {0} ({1}): {2}", (int)m.Reply, m.Reply.ToString(), m.ParamsString);
                break;
            }
        }

        void ProcessPRIVMSG(Message m)
        {
            string chatMsg = m.Params[m.Params.Count - 1];
            Logger.Info("{0} #{1}: {2}", m.User, m.Channel, chatMsg);
            if (chatMsg[0] != '!')
                return;

            string[] chatMsgTokens = chatMsg.Split(' ');
            string cmd = chatMsgTokens[0].Substring(1); // strip '!' character
            string response = "";

            mChannelsMutex.WaitOne();

            try
            {
                if (!mChannels.ContainsKey(m.Channel))
                    throw new InvalidDataException(String.Format("Unknown channel: {0}", m.Channel));

                Logger.Debug("Processing command {0}", cmd);
                response = mChannels[m.Channel].ProcessMessage(cmd, chatMsgTokens);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process command: {0}", e.Message);
                mChannelsMutex.ReleaseMutex();
                return;
            }

            mChannelsMutex.ReleaseMutex();

            if (response.Length > 0)
                mConnection.Send(String.Format("PRIVMSG #{0} :{1}", m.Channel, response));
        }

        bool ProcessMessage(Message m)
        {
            switch (m.Command)
            {
            // Numeric commands (aka. replies)
            case IRCCommand.REPLY:
                ProcessReply(m);
                break;

            // String commands
            case IRCCommand.JOIN:
                Logger.Info("Joined channel {0}", m.Channel);
                break;
            case IRCCommand.NOTICE:
                Logger.Info("Received a Notice from server: {0}", m.ParamsString);
                break;
            case IRCCommand.PART:
                Logger.Info("Leaving channel {0}", m.Channel);
                break;
            case IRCCommand.PING:
                Logger.Debug("Received PING - responding with PONG");
                mConnection.Send("PONG :" + m.Params[m.Params.Count - 1]);
                break;
            case IRCCommand.PRIVMSG:
                ProcessPRIVMSG(m);
                break;
            default:
                throw new ArgumentException(String.Format("Invalid IRC command: {0}", m.Command));
            }

            return true;
        }

        bool ProcessMessage(string msg)
        {
            if (msg == null)
            {
                Logger.Warning("Connection dropped - exiting");
                return false;
            }

            Message m = Message.Parse(msg);
            return ProcessMessage(m);
        }

        void WorkerMain()
        {
            Logger.Info("TwitchIRC Worker thread started.");
            try
            {
                Login();
                mRunning = true;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to login to Twitch IRC server: {0}", e.Message);
                return;
            }

            Logger.Info("Listening for response...");

            while (mRunning)
                mRunning = ProcessMessage(mConnection.Read());
        }

        bool CheckIfLoginSuccessful()
        {
            string msg = mConnection.Read();
            if (msg == null)
                return false; // no message, connection was dropped without any reason

            Message m = Message.Parse(msg);
            if (m.Command == IRCCommand.NOTICE)
            {
                Logger.Info("While trying to login received Notice from Server:");
                Logger.Info("  {0}", msg);

                if (m.Params[m.Params.Count - 1].Equals("Login authentication failed"))
                    return false;
                else
                    return true;
            }

            // Login fail comes as IRC "NOTICE" call. If we don't get it, assume we logged in successfully.
            // Process the message as normal afterwards.
            ProcessMessage(m);
            return true;
        }

        // TODO parametrize
        void Login()
        {
            mConnection = new Connection("irc.chat.twitch.tv", 6697, true);

            mToken = new OAuth.TwitchToken(AuthFlow.AuthorizationCode);

            // log in
            Logger.Info("Logging in to Twitch IRC server...");
            Logger.Debug("Bot login account: {0}", mName);

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
                Logger.Error("Login to Twitch IRC server failed");
                if (!tokenFromRequest)
                {
                    // token might be old; refresh it
                    Logger.Info("OAuth token might be expired - refreshing...");
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
                        throw new InvalidOperationException(
                            "Failed to refresh OAuth Token. Token has been removed, restart to re-login and request a fresh OAuth token"
                        );
                    }
                }
                else
                    throw new InvalidOperationException("Failed to login to Twitch IRC");
            }
        }

        void Disconnect()
        {
            if (mRunning)
            {
                foreach (var c in mChannels)
                {
                    mConnection.Send("PART #" + c.Key);
                }
                mConnection.Send("QUIT");
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
            Disconnect();
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

        public bool AwaitLoggedIn(int timeoutMs)
        {
            return mLoggedInEvent.WaitOne(timeoutMs);
        }

        // IModule overrides

        public void Init()
        {
        }

        public void Run()
        {
            mWorker.Start();
        }

        public void RequestShutdown()
        {
            Disconnect();
        }

        public void Wait()
        {
            mWorker.Join();

            mConnection.Close();
            mConnection = null;
        }
    }
}
