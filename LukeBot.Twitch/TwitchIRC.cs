using LukeBot.Common;
using LukeBot.Auth;
using LukeBot.Core;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;


namespace LukeBot.Twitch
{
    public struct TwitchIRCMessage
    {
        public string Type { get; private set; }
        public string MessageID { get; private set; }
        public string UserID { get; set; }
        public string Color { get; set; }
        public List<MessageEmote> Emotes { get; private set; }
        public string Nick { get; set; }
        public string DisplayName { get; set; }
        public string Message { get; set; }

        public TwitchIRCMessage(string msgID)
        {
            Type = "TwitchIRCMessage";
            MessageID = msgID;
            UserID = "";
            Color = "#dddddd";
            Emotes = new List<MessageEmote>();
            Nick = "";
            DisplayName = "";
            Message = "";
        }

        private string GetEmoteName(string msg, string range)
        {
            int dash = range.IndexOf('-');
            int from = Int32.Parse(range.Substring(0, dash));
            int count = Int32.Parse(range.Substring(dash + 1)) - from + 1;
            return msg.Substring(from, count);
        }

        public void ParseEmotesString(string msg, string emotesStr)
        {
            if (emotesStr.Length == 0)
                return;

            string[] emotes = emotesStr.Split('/');
            foreach (string e in emotes)
            {
                int separatorIdx = e.IndexOf(':');
                string ranges = e.Substring(separatorIdx + 1);
                int firstRangeIdx = ranges.IndexOf(',');
                string name;
                if (firstRangeIdx == -1)
                    name = GetEmoteName(msg, ranges);
                else
                    name = GetEmoteName(msg, ranges.Substring(0, firstRangeIdx));

                Emotes.Add(new MessageEmote(EmoteSource.Twitch, name, e.Substring(0, separatorIdx), 32, 32, e.Substring(separatorIdx + 1)));
            }
        }

        public void AddExternalEmotes(List<MessageEmote> emotes)
        {
            List<MessageEmote> filteredEmotes = new List<MessageEmote>(emotes.Count);
            foreach (MessageEmote e in emotes)
            {
                if (Emotes.Exists(x => x.Name == e.Name))
                {
                    Logger.Log().Debug("Removing external emote {0} from message, duplicated by sub emotes", e.Name);
                    continue;
                }

                filteredEmotes.Add(e);
            }

            Emotes.AddRange(filteredEmotes);
        }
    }

    public struct TwitchIRCClearChat
    {
        public string Type { get; private set; }
        public string Nick { get; private set; }

        public TwitchIRCClearChat(string nick)
        {
            Type = "TwitchIRCClearChat";
            Nick = nick;
        }
    }

    public struct TwitchIRCClearMsg
    {
        public string Type { get; private set; }
        public string Message { get; private set; }
        public string MessageID { get; set; }

        public TwitchIRCClearMsg(string message)
        {
            Type = "TwitchIRCClearMsg";
            Message = message;
            MessageID = "";
        }
    }

    public struct TwitchIRCUserNotice
    {
        public string Type { get; private set; }
        public string NoticeType { get; private set; }

        public TwitchIRCUserNotice(string noticeType)
        {
            Type = "TwitchIRCUserNotice";
            NoticeType = noticeType;
        }
    }

    public class TwitchIRC: IEventPublisher
    {
        private string mName = "lukeboto";
        private Connection mConnection = null;
        private Token mToken;
        private Dictionary<string, IRCChannel> mChannels;
        private bool mTagsEnabled = false;
        private EmoteProvider mExternalEmotes;

        private bool mRunning = false;
        private AutoResetEvent mLoggedInEvent;
        private int mMsgIDCounter = 0; // backup for when we don't have metadata
        public EventHandler<TwitchIRCMessage> MessageEvent;
        public EventHandler<TwitchIRCClearChat> ClearChatEvent;
        public EventHandler<TwitchIRCClearMsg> ClearMsgEvent;
        public EventHandler<TwitchIRCUserNotice> UserNoticeEvent;

        private Thread mWorker;
        private Mutex mChannelsMutex;

        private void OnMessage(TwitchIRCMessage args)
        {
            EventHandler<TwitchIRCMessage> handler = MessageEvent;
            if (handler != null)
                handler(this, args);
        }

        private void OnClearChat(TwitchIRCClearChat args)
        {
            EventHandler<TwitchIRCClearChat> handler = ClearChatEvent;
            if (handler != null)
                handler(this, args);
        }

        private void OnClearMsg(TwitchIRCClearMsg args)
        {
            EventHandler<TwitchIRCClearMsg> handler = ClearMsgEvent;
            if (handler != null)
                handler(this, args);
        }

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

            TwitchIRCMessage message = new TwitchIRCMessage(msgID);
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
                Logger.Log().Debug("Emotes string: {0}", emotes);
                message.ParseEmotesString(chatMsg, emotes);
            }

            message.AddExternalEmotes(mExternalEmotes.ParseEmotes(message.Message));

            OnMessage(message);

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
            catch (Common.Exception e)
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
            TwitchIRCClearChat message = new TwitchIRCClearChat(msg);
            OnClearChat(message);
        }

        void ProcessCLEARMSG(Message m)
        {
            string msg = m.Params[m.Params.Count - 1];
            Logger.Log().Warning("CLEARMSG ({0} tags) #{1} :{2}", m.Tags.Count, m.Channel, msg);

            TwitchIRCClearMsg message = new TwitchIRCClearMsg(msg);

            string msgID;
            if (m.Tags.TryGetValue("target-msg-id", out msgID))
                message.MessageID = msgID;

            OnClearMsg(message);
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
            catch (Common.Exception e)
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

            Logger.Log().Info("Twitch IRC module initialized");
        }

        ~TwitchIRC()
        {
            Disconnect();
            WaitForShutdown();
        }

        public void JoinChannel(API.GetUserResponse user)
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
