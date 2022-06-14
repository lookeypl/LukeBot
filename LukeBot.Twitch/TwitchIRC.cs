using LukeBot.Common;
using LukeBot.API;
using LukeBot.Core;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using LukeBot.Twitch.Common;
using LukeBot.Core.Events;


namespace LukeBot.Twitch
{
    public class TwitchIRC: IEventPublisher
    {
        private string mName = "lukeboto";
        private Connection mConnection = null;
        private Token mToken;
        private Dictionary<string, IRCChannel> mChannels;
        private bool mTagsEnabled = false;
        private EmoteProvider mExternalEmotes;
        private EventCallback mMessageEventCallback;
        private EventCallback mMessageClearEventCallback;
        private EventCallback mUserClearEventCallback;

        private bool mRunning = false;
        private AutoResetEvent mLoggedInEvent;
        private int mMsgIDCounter = 0; // backup for when we don't have metadata

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
                Logger.Log().Info("Welcome msg: {0}", m.ParamsString);
                break;
            case IRCReply.RPL_MOTDSTART:
                Logger.Log().Info("Server's Message of the Day:");
                Logger.Log().Info("  {0}", m.ParamsString);
                break;
            case IRCReply.RPL_MOTD:
                Logger.Log().Info("  {0}", m.ParamsString);
                break;
            case IRCReply.RPL_ENDOFMOTD:
                Logger.Log().Info("  {0}", m.ParamsString);
                Logger.Log().Info("End of Message of the Day");
                mLoggedInEvent.Set();
                break;
            default:
                Logger.Log().Info("Reply {0} ({1}): {2}", (int)m.Reply, m.Reply.ToString(), m.ParamsString);
                break;
            }
        }

        void ProcessPRIVMSG(Message m)
        {
            string chatMsg = m.Params[m.Params.Count - 1];
            Logger.Log().Info("({0} tags) #{1} {2}: {3}", m.Tags.Count, m.Channel, m.User, chatMsg);

            // Message related tags pulled from metadata (if available)
            string msgID;
            if (!m.Tags.TryGetValue("id", out msgID))
                msgID = String.Format("{0}", mMsgIDCounter++);

            TwitchChatMessageArgs message = new TwitchChatMessageArgs(msgID);
            message.Nick = m.User;
            message.Message = chatMsg;

            string userID;
            if (m.Tags.TryGetValue("user-id", out userID))
                message.UserID = userID;

            string color;
            if (m.Tags.TryGetValue("color", out color))
                message.Color = color;

            string displayName;
            if (m.Tags.TryGetValue("display-name", out displayName))
                message.DisplayName = displayName;

            // Twitch global/sub emotes - taken from IRC tags
            string emotes;
            if (m.Tags.TryGetValue("emotes", out emotes))
            {
                message.ParseEmotesString(chatMsg, emotes);
            }

            message.AddExternalEmotes(mExternalEmotes.ParseEmotes(message.Message));

            mMessageEventCallback.PublishEvent(message);

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

                Logger.Log().Debug("Processing command {0}", cmd);
                response = mChannels[m.Channel].ProcessMessage(cmd, chatMsgTokens);
            }
            catch (LukeBot.Common.Exception e)
            {
                Logger.Log().Error("Failed to process command: {0}", e.Message);
                mChannelsMutex.ReleaseMutex();
                return;
            }

            mChannelsMutex.ReleaseMutex();

            if (response.Length > 0)
                mConnection.Send(String.Format("PRIVMSG #{0} :{1}", m.Channel, response));
        }

        void ProcessCLEARCHAT(Message m)
        {
            string msg = m.Params[m.Params.Count - 1];
            Logger.Log().Warning("CLEARCHAT ({0} tags) #{1} :{2}", m.Tags.Count, m.Channel, msg);
            TwitchChatUserClearArgs message = new TwitchChatUserClearArgs(msg);
            mUserClearEventCallback.PublishEvent(message);
        }

        void ProcessCLEARMSG(Message m)
        {
            string msg = m.Params[m.Params.Count - 1];
            Logger.Log().Warning("CLEARMSG ({0} tags) #{1} :{2}", m.Tags.Count, m.Channel, msg);

            TwitchChatMessageClearArgs message = new TwitchChatMessageClearArgs(msg);

            string msgID;
            if (m.Tags.TryGetValue("target-msg-id", out msgID))
                message.MessageID = msgID;

            mMessageClearEventCallback.PublishEvent(message);
        }

        void ProcessCAP(Message m)
        {
            // TODO complete this part to discover if CAP was acquired
            Logger.Log().Debug("CAP response: {0}", m.MessageString);
        }

        void ProcessNOTICE(Message m)
        {
            Logger.Log().Info("Received a Notice from server: {0}", m.ParamsString);
        }

        void ProcessUSERNOTICE(Message m)
        {
            Logger.Log().Info("Received a User Notice from server");
            Logger.Log().Secure("USERNOTICE message details:");
            m.Print(LogLevel.Secure);
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
                Logger.Log().Info("Joined channel {0}", m.Channel);
                break;
            case IRCCommand.NOTICE:
                ProcessNOTICE(m);
                break;
            case IRCCommand.USERNOTICE:
                ProcessUSERNOTICE(m);
                break;
            case IRCCommand.PART:
                Logger.Log().Info("Leaving channel {0}", m.Channel);
                break;
            case IRCCommand.PING:
                Logger.Log().Debug("Received PING - responding with PONG");
                mConnection.Send("PONG :" + m.Params[m.Params.Count - 1]);
                break;
            case IRCCommand.PRIVMSG:
                ProcessPRIVMSG(m);
                break;
            case IRCCommand.CLEARCHAT:
                ProcessCLEARCHAT(m);
                break;
            case IRCCommand.CLEARMSG:
                ProcessCLEARMSG(m);
                break;
            case IRCCommand.CAP:
                ProcessCAP(m);
                break;
            }

            return true;
        }

        bool ProcessMessage(string msg)
        {
            if (msg == null)
            {
                Logger.Log().Warning("Connection dropped - exiting");
                return false;
            }

            Message m = Twitch.Message.Parse(msg);
            return ProcessMessage(m);
        }

        bool CheckIfLoginSuccessful()
        {
            string msg = mConnection.Read();
            if (msg == null)
                return false; // no message, connection was dropped without any reason

            Message m = Twitch.Message.Parse(msg);
            if (m.Command == IRCCommand.NOTICE)
            {
                Logger.Log().Info("While trying to login received Notice from Server:");
                Logger.Log().Info("  {0}", msg);

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
            if (!mToken.Loaded)
                throw new InvalidOperationException("Provided token was not loaded properly");

            // log in
            Logger.Log().Debug("Bot login account: {0}", mName);

            mConnection = new Connection("irc.chat.twitch.tv", 6697, true);

            mConnection.Send("PASS oauth:" + mToken.Get());
            mConnection.Send("NICK " + mName);
            if (!CheckIfLoginSuccessful())
            {
                Logger.Log().Warning("Login to Twitch IRC server failed - retrying in 2 seconds...");
                mConnection.Close();

                Thread.Sleep(2000);
                mConnection = new Connection("irc.chat.twitch.tv", 6697, true);

                mConnection.Send("PASS oauth:" + mToken.Get());
                mConnection.Send("NICK " + mName);
                if (!CheckIfLoginSuccessful())
                {
                    throw new LoginFailedException("Login to Twitch IRC server failed");
                }
            }

            mConnection.Send("CAP REQ :twitch.tv/tags");
            mConnection.Send("CAP REQ :twitch.tv/commands");
        }

        void WorkerMain()
        {
            Logger.Log().Info("TwitchIRC Worker thread started.");
            try
            {
                Login();
                mRunning = true;
            }
            catch (LukeBot.Common.Exception e)
            {
                Logger.Log().Error("Twitch IRC worker thread exited with error.");
                e.Print(LogLevel.Error);
                throw e;
            }

            Logger.Log().Info("Listening for response...");

            while (mRunning)
                mRunning = ProcessMessage(mConnection.Read());
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

        public TwitchIRC(Token token)
        {
            mWorker = new Thread(this.WorkerMain);
            mChannelsMutex = new Mutex();
            mLoggedInEvent = new AutoResetEvent(false);
            mChannels = new Dictionary<string, IRCChannel>();
            mToken = token;
            mExternalEmotes = new EmoteProvider();

            List<EventCallback> events = Core.Systems.Event.RegisterEventPublisher(
                this, Core.Events.Type.TwitchChatMessage | Core.Events.Type.TwitchChatMessageClear | Core.Events.Type.TwitchChatUserClear
            );

            foreach (EventCallback e in events)
            {
                switch (e.type)
                {
                case Core.Events.Type.TwitchChatMessage:
                    mMessageEventCallback = e;
                    break;
                case Core.Events.Type.TwitchChatMessageClear:
                    mMessageClearEventCallback = e;
                    break;
                case Core.Events.Type.TwitchChatUserClear:
                    mUserClearEventCallback = e;
                    break;
                default:
                    Logger.Log().Warning("Received unknown event type from Event system");
                    break;
                }
            }

            Logger.Log().Info("Twitch IRC module initialized");
        }

        ~TwitchIRC()
        {
            Disconnect();
            WaitForShutdown();
        }

        public void JoinChannel(API.Twitch.GetUserResponse user)
        {
            mChannelsMutex.WaitOne();

            if (mChannels.ContainsKey(user.data[0].login))
            {
                mChannelsMutex.ReleaseMutex();
                throw new ArgumentException(String.Format("Cannot join channel {0} - already joined", user.data[0].login));
            }

            mConnection.Send("JOIN #" + user.data[0].login);

            mChannels.Add(user.data[0].login, new IRCChannel(user.data[0].login));

            mExternalEmotes.AddEmoteSource(new FFZEmoteSource(user.data[0].id));
            mExternalEmotes.AddEmoteSource(new SevenTVEmoteSource(user.data[0].login));

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

        public void Run()
        {
            mWorker.Start();
        }

        public void RequestShutdown()
        {
            Disconnect();
        }

        public void WaitForShutdown()
        {
            if (mWorker.IsAlive)
            {
                mWorker.Join();
            }

            if (mConnection != null)
            {
                mConnection.Close();
                mConnection = null;
            }
        }
    }
}
