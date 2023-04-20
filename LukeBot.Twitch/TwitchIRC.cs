using LukeBot.Common;
using LukeBot.API;
using LukeBot.Communication;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using LukeBot.Communication.Events;

using Command = LukeBot.Twitch.Common.Command;


namespace LukeBot.Twitch
{
    public class TwitchIRC: IEventPublisher
    {
        private string mName;
        private Token mToken;
        private IRCClient mIRCClient = null;
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

        private Command::User EstablishUserIdentity(IRCMessage m)
        {
            Command::User identity = Command::User.Chatter;

            if (m.User == m.Channel)
                identity |= Command::User.Broadcaster;

            string isMod;
            if (m.GetTag("mod", out isMod) && Int32.Parse(isMod) == 1)
                identity |= Command::User.Moderator;

            string isVIP;
            if (m.GetTag("vip", out isVIP) && Int32.Parse(isVIP) == 1)
                identity |= Command::User.VIP;

            string isSub;
            if (m.GetTag("subscriber", out isSub) && Int32.Parse(isSub) == 1)
                identity |= Command::User.Subscriber;

            return identity;
        }

        void ProcessReply(IRCMessage m)
        {
            switch (m.Reply)
            {
            case IRCReply.RPL_TWITCH_WELCOME1:
            case IRCReply.RPL_TWITCH_WELCOME2:
            case IRCReply.RPL_TWITCH_WELCOME3:
            case IRCReply.RPL_TWITCH_WELCOME4:
                Logger.Log().Info("Welcome msg: {0}", m.GetTrailingParam());
                break;
            case IRCReply.RPL_MOTDSTART:
                Logger.Log().Info("Server's Message of the Day:");
                Logger.Log().Info("  {0}", m.GetTrailingParam());
                break;
            case IRCReply.RPL_MOTD:
                Logger.Log().Info("  {0}", m.GetTrailingParam());
                break;
            case IRCReply.RPL_ENDOFMOTD:
                Logger.Log().Info("  {0}", m.GetTrailingParam());
                Logger.Log().Info("End of Message of the Day");
                mLoggedInEvent.Set();
                break;
            default:
                Logger.Log().Info("Reply {0} ({1}): {2}", (int)m.Reply, m.Reply.ToString(), m.GetTrailingParam());
                break;
            }
        }

        void ProcessPRIVMSG(IRCMessage m)
        {
            string chatMsg = m.GetTrailingParam();

            Logger.Log().Info("({0}) #{1} {2}: {3}", m.TagStr, m.Channel, m.User, chatMsg);

            // Message related tags pulled from metadata (if available)
            string msgID;
            if (!m.GetTag("id", out msgID))
                msgID = String.Format("{0}", mMsgIDCounter++);

            TwitchChatMessageArgs message = new TwitchChatMessageArgs(msgID);
            message.Nick = m.User;
            message.Message = chatMsg;

            string userID;
            if (m.GetTag("user-id", out userID))
                message.UserID = userID;

            string color;
            if (m.GetTag("color", out color))
                message.Color = color;

            string displayName;
            if (m.GetTag("display-name", out displayName))
                message.DisplayName = displayName;

            // Twitch global/sub emotes - taken from IRC tags
            string emotes;
            if (m.GetTag("emotes", out emotes))
            {
                message.ParseEmotesString(chatMsg, emotes);
            }

            message.AddExternalEmotes(mExternalEmotes.ParseEmotes(message.Message));

            mMessageEventCallback.PublishEvent(message);

            string[] chatMsgTokens = chatMsg.Split(' ');
            string cmd = chatMsgTokens[0];
            Command::User userIdentity = EstablishUserIdentity(m);
            string response = "";

            mChannelsMutex.WaitOne();

            try
            {
                if (!mChannels.ContainsKey(m.Channel))
                    throw new InvalidDataException(String.Format("Unknown channel: {0}", m.Channel));

                response = mChannels[m.Channel].ProcessMessage(cmd, userIdentity, chatMsgTokens);
            }
            catch (LukeBot.Common.Exception e)
            {
                Logger.Log().Error("Failed to process command: {0}", e.Message);
                mChannelsMutex.ReleaseMutex();
                return;
            }

            mChannelsMutex.ReleaseMutex();

            if (response.Length > 0)
                mIRCClient.Send(IRCMessage.PRIVMSG(m.Channel, response));
        }

        void ProcessCLEARCHAT(IRCMessage m)
        {
            string msg = m.GetTrailingParam();
            Logger.Log().Warning("CLEARCHAT ({0} tags) #{1} :{2}", m.GetTagCount(), m.Channel, msg);
            TwitchChatUserClearArgs message = new TwitchChatUserClearArgs(msg);
            mUserClearEventCallback.PublishEvent(message);
        }

        void ProcessCLEARMSG(IRCMessage m)
        {
            string msg = m.GetTrailingParam();
            Logger.Log().Warning("CLEARMSG ({0} tags) #{1} :{2}", m.GetTagCount(), m.Channel, msg);

            TwitchChatMessageClearArgs message = new TwitchChatMessageClearArgs(msg);

            string msgID;
            if (m.GetTag("target-msg-id", out msgID))
                message.MessageID = msgID;

            mMessageClearEventCallback.PublishEvent(message);
        }

        void ProcessCAP(IRCMessage m)
        {
            // TODO complete this part to discover if CAP was acquired
            Logger.Log().Debug("CAP response: {0}", m.ToString());
        }

        void ProcessNOTICE(IRCMessage m)
        {
            Logger.Log().Info("Received a Notice from server: {0}", m.GetTrailingParam());
        }

        void ProcessUSERNOTICE(IRCMessage m)
        {
            Logger.Log().Info("Received a User Notice from server");
            Logger.Log().Secure("USERNOTICE message details:");
            m.Print(LogLevel.Secure);
        }

        bool ProcessMessage(IRCMessage m)
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
                mIRCClient.Send(IRCMessage.PONG(m.GetTrailingParam()));
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
            case IRCCommand.QUIT:
            case IRCCommand.INVALID:
                return false;
            }

            return true;
        }

        bool CheckIfLoginSuccessful()
        {
            try
            {
                IRCMessage m = mIRCClient.Receive();
                if (m.Command == IRCCommand.INVALID)
                {
                    Logger.Log().Info("Connection was dropped for some unknown reason");
                    return false;
                }

                if (m.Command == IRCCommand.NOTICE)
                {
                    Logger.Log().Info("While trying to login received Notice from Server:");
                    Logger.Log().Info("  {0}", m.ToString());

                    if (m.GetTrailingParam().Equals("Login authentication failed"))
                        return false;
                    else
                        return true;
                }

                // Login fail comes as IRC "NOTICE" call. If we don't get it, assume we logged in successfully.
                // Process the message as normal afterwards.
                ProcessMessage(m);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Login to Twitch IRC server failed: " + e.Message);
                return false;
            }

            return true;
        }

        void Login()
        {
            if (!mToken.Loaded)
                throw new InvalidOperationException("Provided token was not loaded properly");

            // log in
            Logger.Log().Debug("Bot login account: {0}", mName);

            mIRCClient = new IRCClient("irc.chat.twitch.tv", 6697, true);
            mIRCClient.Login(mName, mToken);

            if (!CheckIfLoginSuccessful())
            {
                Logger.Log().Warning("Login to Twitch IRC server failed - retrying in 2 seconds...");
                mIRCClient.Close();

                Thread.Sleep(2000);

                mIRCClient = new IRCClient("irc.chat.twitch.tv", 6697, true);
                mIRCClient.Login(mName, mToken);

                if (!CheckIfLoginSuccessful())
                {
                    throw new LoginFailedException("Login to Twitch IRC server failed");
                }
            }

            // TODO check if caps were properly enabled before using
            mIRCClient.Send(IRCMessage.CAPRequest("twitch.tv/tags"));
            mIRCClient.Send(IRCMessage.CAPRequest("twitch.tv/commands"));
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
                mRunning = ProcessMessage(mIRCClient.Receive());
        }

        void Disconnect()
        {
            if (mRunning)
            {
                foreach (var c in mChannels)
                {
                    mIRCClient.Send(IRCMessage.PART(c.Key));
                }
                mIRCClient.Send(IRCMessage.QUIT());
            }
        }

        public TwitchIRC(string username, Token token)
        {
            mName = username;
            mWorker = new Thread(this.WorkerMain);
            mChannelsMutex = new Mutex();
            mLoggedInEvent = new AutoResetEvent(false);
            mChannels = new Dictionary<string, IRCChannel>();
            mToken = token;
            mExternalEmotes = new EmoteProvider();

            List<EventCallback> events = Communication.Comms.Event.RegisterEventPublisher(
                this, Communication.Events.Type.TwitchChatMessage | Communication.Events.Type.TwitchChatMessageClear | Communication.Events.Type.TwitchChatUserClear
            );

            foreach (EventCallback e in events)
            {
                switch (e.type)
                {
                case Communication.Events.Type.TwitchChatMessage:
                    mMessageEventCallback = e;
                    break;
                case Communication.Events.Type.TwitchChatMessageClear:
                    mMessageClearEventCallback = e;
                    break;
                case Communication.Events.Type.TwitchChatUserClear:
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

            mIRCClient.Send(IRCMessage.JOIN(user.data[0].login));

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

        public void DeleteCommandFromChannel(string channel, string commandName)
        {
            mChannelsMutex.WaitOne();

            if (!mChannels.ContainsKey(channel))
            {
                mChannelsMutex.ReleaseMutex();
                throw new ArgumentException(String.Format("Invalid channel name {0}", channel));
            }

            mChannels[channel].DeleteCommand(commandName);

            mChannelsMutex.ReleaseMutex();
        }

        public void EditCommandFromChannel(string channel, string commandName, string newValue)
        {
            mChannelsMutex.WaitOne();

            if (!mChannels.ContainsKey(channel))
            {
                mChannelsMutex.ReleaseMutex();
                throw new ArgumentException(String.Format("Invalid channel name {0}", channel));
            }

            mChannels[channel].EditCommand(commandName, newValue);

            mChannelsMutex.ReleaseMutex();
        }

        public Command::Descriptor[] GetCommandDescriptors(string channel)
        {
            return new Command::Descriptor[0];
        }

        public Command::Descriptor GetCommandDescriptor(string channel, string name)
        {
            mChannelsMutex.WaitOne();

            if (!mChannels.ContainsKey(channel))
            {
                mChannelsMutex.ReleaseMutex();
                throw new ArgumentException(String.Format("Invalid channel name {0}", channel));
            }

            Command::Descriptor d = mChannels[channel].GetCommand(name).ToDescriptor();

            mChannelsMutex.ReleaseMutex();

            return d;
        }

        public void AllowPrivilegeInCommand(string channel, string name, Command::User privilege)
        {
            mChannelsMutex.WaitOne();

            if (!mChannels.ContainsKey(channel))
            {
                mChannelsMutex.ReleaseMutex();
                throw new ArgumentException(String.Format("Invalid channel name {0}", channel));
            }

            mChannels[channel].GetCommand(name).AllowUsers(privilege);

            mChannelsMutex.ReleaseMutex();
        }

        public void DenyPrivilegeInCommand(string channel, string name, Command::User privilege)
        {
            mChannelsMutex.WaitOne();

            if (!mChannels.ContainsKey(channel))
            {
                mChannelsMutex.ReleaseMutex();
                throw new ArgumentException(String.Format("Invalid channel name {0}", channel));
            }

            mChannels[channel].GetCommand(name).DenyUsers(privilege);

            mChannelsMutex.ReleaseMutex();
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

            if (mIRCClient != null)
            {
                mIRCClient.Close();
                mIRCClient = null;
            }
        }
    }
}
