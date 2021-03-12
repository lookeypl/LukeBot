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

        private int mRunning = 0;
        private AutoResetEvent mLoggedInEvent;

        private Thread mWorker;
        private Mutex mChannelsMutex;

        void ProcessPRIVMSG(Message m)
        {
            mChannelsMutex.WaitOne();

            try
            {
                if (!mChannels.ContainsKey(m.Channel))
                    throw new InvalidDataException(String.Format("Unknown channel: {0}", m.Channel));

                Logger.Info("Message for channel {0} from {1}: {2}", m.Channel, m.Nick, m.Params[m.Params.Count - 1]);
                // TODO CALL COMMAND
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process message: {0}", e.Message);
                mChannelsMutex.ReleaseMutex();
                return;
            }

            mChannelsMutex.ReleaseMutex();
        }

        void ProcessMessage(string msg)
        {
            if (msg == null)
                throw new ArgumentNullException("Message is NULL");

            Message m = Message.Parse(msg);

            switch (m.Command)
            {
            case IRCCommand.UNKNOWN_NUMERIC:
                Logger.Warning("Unrecognized command: {0}", m);
                break;
            case IRCCommand.LOGIN_001:
            case IRCCommand.LOGIN_002:
            case IRCCommand.LOGIN_003:
            case IRCCommand.LOGIN_004:
            case IRCCommand.LOGIN_372:
            case IRCCommand.LOGIN_375:
                Logger.Debug("Welcome message: {0}", m.ParamsString);
                break;
            case IRCCommand.LOGIN_376:
                mLoggedInEvent.Set();
                Logger.Debug("Welcome message: {0}", m.ParamsString);
                break;
            case IRCCommand.UNKNOWN_421:
                Logger.Error("Unknown command sent: {0}", string.Join(" ", m.Params));
                break;
            case IRCCommand.JOIN:
                Logger.Info("Joined channel {0}", m.Channel);
                break;
            case IRCCommand.NOTICE:
                Logger.Info("Received a Notice from server: {0}", m.ParamsString);
                break;
            case IRCCommand.PING:
                Logger.Debug("Received PING - responding with PONG");
                mConnection.Send("PONG :" + m.Params[0]);
                break;
            case IRCCommand.PRIVMSG:
                ProcessPRIVMSG(m);
                break;
            default:
                throw new ArgumentException("Invalid IRC command");
            }
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

            Message m = Message.Parse(msg);
            if (m.Command == IRCCommand.NOTICE)
            {
                Logger.Info("While trying to login received Notice from Server:");
                Logger.Info("  {0}", msg);

                if (m.ParamsString.Equals("Login authentication failed"))
                    return false;
                else
                    return true;
            }

            // Login fail comes as IRC "NOTICE" call. If we don't get it, assume we logged in successfully.
            Logger.Debug("Recv: {0}", msg);
            return true;
        }

        // TODO parametrize
        void Login()
        {
            mConnection = new Connection("irc.chat.twitch.tv", 6697, true);

            mToken = new OAuth.TwitchToken(AuthFlow.AuthorizationCode);

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

        public void Wait()
        {
            mWorker.Join();
        }
    }
}
