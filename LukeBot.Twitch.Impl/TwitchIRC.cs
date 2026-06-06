using LukeBot.Common;
using LukeBot.API;
using LukeBot.Communication;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.User;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

using CommonUtils = LukeBot.Common.Utils;
using CommonConstants = LukeBot.Common.Constants;
using System.Threading.Channels;


namespace LukeBot.Twitch.Impl
{
    internal class TwitchIRC
    {
        private enum ConnectionState
        {
            DISCONNECTED,
            CONNECTED,
            QUIT,
            BROKEN
        }

        private string mName;
        private Token mToken;
        private IRCClient mIRCClient = null;
        private Dictionary<string, IRCChannel> mChannels = new();
        private bool mTagsEnabled = false;

        private ConnectionState mConnectionState = ConnectionState.DISCONNECTED;
        private AutoResetEvent mLoggedInEvent;

        private Thread mWorker;
        private object mChannelsLock = new();

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
            string response = "";
            Logger.Log().Info("({0} tags) #{1} {2}: {3}", m.GetTagCount(), m.Channel, m.User, m.GetTrailingParam());

            lock (mChannelsLock)
            {
                if (!mChannels.ContainsKey(m.Channel))
                    throw new UnknownChannelException(m.Channel);

                response = mChannels[m.Channel].ProcessMSG(m, mTagsEnabled);
            }

            if (!String.IsNullOrEmpty(response))
                mIRCClient.Send(IRCMessage.PRIVMSG(m.Channel, response));
        }

        void ProcessCLEARCHAT(IRCMessage m)
        {
            if (!mChannels.ContainsKey(m.Channel))
                throw new UnknownChannelException(m.Channel);

            string nick = m.GetTrailingParam();
            Logger.Log().Warning("CLEARCHAT ({0} tags) #{1} :{2}", m.GetTagCount(), m.Channel, nick);
            mChannels[m.Channel].ProcessCLEARCHAT(nick);
        }

        void ProcessCLEARMSG(IRCMessage m)
        {
            if (!mChannels.ContainsKey(m.Channel))
                throw new UnknownChannelException(m.Channel);

            string msg = m.GetTrailingParam();
            Logger.Log().Warning("CLEARMSG ({0} tags) #{1} :{2}", m.GetTagCount(), m.Channel, msg);

            string msgID = "";
            if (mTagsEnabled)
            {
                m.GetTag("target-msg-id", out msgID);
            }

            mChannels[m.Channel].ProcessCLEARMSG(msg, msgID);
        }

        void ProcessCAP(IRCMessage m)
        {
            Logger.Log().Debug("CAP response: {0}", m.ToString());

            if (m.GetParams()[1] == "ACK" && m.GetTrailingParam().Equals("twitch.tv/tags"))
            {
                Logger.Log().Debug("IRC Tag capability enabled");
                mTagsEnabled = true;
            }
        }

        void ProcessNOTICE(IRCMessage m)
        {
            Logger.Log().Info("Received a Notice from server: {0}", m.GetTrailingParam());
        }

        void ProcessUSERNOTICE(IRCMessage m)
        {
            Logger.Log().Info("Received a User Notice from server");
            Logger.Log().Info("USERNOTICE message details:");
            m.Print(LogLevel.Info);

            mChannels[m.Channel].ProcessUSERNOTICE(m, mTagsEnabled);
        }

        void ProcessMessage(IRCMessage m)
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
                mConnectionState = ConnectionState.QUIT;
                break;
            case IRCCommand.INVALID:
                // only mark the connection as broken if we didn't switch to quitting already
                if (mConnectionState != ConnectionState.QUIT)
                    mConnectionState = ConnectionState.BROKEN;
                break;
            }
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

        void TryConnect()
        {
            int reconnectCount = Constants.RECONNECT_ATTEMPTS;
            int refreshTokenAfterTries = 3;

            int reconnectCountConf = 0;
            if (Conf.TryGet(CommonConstants.PROP_STORE_RECONNECT_COUNT_PROP, out reconnectCountConf))
            {
                Logger.Log().Debug("Custom reconnect count set in config: {0}", reconnectCountConf);
                reconnectCount = reconnectCountConf;
            }

            Common.Utils.TryConnect(reconnectCount, 1000, (attempt) =>
            {
                if (attempt > 0)
                {
                    Logger.Log().Info("TwitchIRC (re)connect attempt #{0}", attempt);
                }

                mIRCClient = new IRCClient("irc.chat.twitch.tv", 6697, true);
                mIRCClient.Login(mName, mToken);

                if (!CheckIfLoginSuccessful())
                {
                    // connection failed - close, wait, retry
                    mIRCClient.Close();
                    if (attempt == refreshTokenAfterTries)
                    {
                        Logger.Log().Warning("Attempting force-refresh of Twitch login token just in case...");
                        mToken.Refresh();
                    }

                    throw new LoginFailedException("Connection to Twitch IRC failed.");
                }
            });

            Logger.Log().Info("Login to Twitch IRC server successful, acquiring caps");

            mIRCClient.Send(IRCMessage.CAPRequest("twitch.tv/tags"));
            mIRCClient.Send(IRCMessage.CAPRequest("twitch.tv/commands"));

            lock (mChannelsLock)
            {
                foreach (string channelLogin in mChannels.Keys)
                {
                    mIRCClient.Send(IRCMessage.JOIN(channelLogin));
                }
            }

            mConnectionState = ConnectionState.CONNECTED;
            Logger.Log().Info("Twitch IRC connected");
        }

        void Login()
        {
            if (!mToken.Loaded)
                throw new InvalidOperationException("Provided token was not loaded properly");

            Logger.Log().Debug("Bot login account: {0}", mName);
            TryConnect();
        }

        void WorkerMain()
        {
            Logger.Log().Info("TwitchIRC Worker thread started.");

            try
            {
                Login();
            }
            catch (LukeBot.Common.Exception e)
            {
                Logger.Log().Error("Twitch IRC worker thread exited with error.");
                e.Print(LogLevel.Error);
                throw;
            }

            while (mConnectionState == ConnectionState.CONNECTED)
            {
                ProcessMessage(mIRCClient.Receive());

                if (mIRCClient.Connected && mConnectionState == ConnectionState.BROKEN)
                {
                    Logger.Log().Warning("Connection was broken - attempting reconnect");
                    TryConnect();
                }
            }

            mIRCClient.Close();
            mIRCClient = null;

            Logger.Log().Info("TwitchIRC Worker thread completed.");
        }

        void Disconnect()
        {
            if (mConnectionState == ConnectionState.CONNECTED)
            {
                mConnectionState = ConnectionState.QUIT;

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
            mWorker.Name = "TwitchIRC Worker (" + username + ")";
            mLoggedInEvent = new AutoResetEvent(false);
            mToken = token;

            Logger.Log().Info("Twitch IRC module initialized");
        }

        public IRCChannel JoinChannel(IUserContext lbUser, TwitchUserIdentity channelIdentity, Token token)
        {
            IRCChannel channel = null;

            lock (mChannelsLock)
            {
                if (mChannels.ContainsKey(channelIdentity.Username))
                {
                    throw new ChannelAlreadyJoinedException(channelIdentity.Username);
                }

                mIRCClient.Send(IRCMessage.JOIN(channelIdentity.Username));

                channel = new(lbUser, channelIdentity, token);
                mChannels.Add(channelIdentity.Username, channel);
            }

            return channel;
        }

        public void PartChannel(IRCChannel channel)
        {
            lock (mChannelsLock)
            {
                string channelName = channel.GetChannelName();
                if (!mChannels.ContainsKey(channelName))
                {
                    throw new UnknownChannelException(channelName);
                }

                mIRCClient.Send(IRCMessage.PART(channelName));

                mChannels[channelName].Dispose();
                mChannels.Remove(channelName);
            }
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

            if (mIRCClient != null && mIRCClient.Connected)
            {
                mIRCClient.Close();
                mIRCClient = null;
                mConnectionState = ConnectionState.DISCONNECTED;
            }
        }
    }
}
