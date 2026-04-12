using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.API;
using LukeBot.Communication;
using LukeBot.Common;
using LukeBot.Twitch;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.User;
using System.Threading.Channels;


[assembly: InternalsVisibleTo("LukeBot.Tests")]

namespace LukeBot.Twitch.Impl
{
    internal class EventSubClient: IEventPublisher
    {
        public const string EVENTSUB_URI_MAIN = "wss://eventsub.wss.twitch.tv/ws";

        private const string SUB_CHANNEL = "channel";

        private const string SUB_CHANNEL_POINTS_REDEMPTION = SUB_CHANNEL + ".channel_points_custom_reward_redemption";
        public const string SUB_CHANNEL_POINTS_REDEMPTION_ADD = SUB_CHANNEL_POINTS_REDEMPTION + ".add";
        public const string SUB_CHANNEL_POINTS_REDEMPTION_UPDATE = SUB_CHANNEL_POINTS_REDEMPTION + ".update";

        public const string SUB_CHEER = SUB_CHANNEL + ".cheer";

        public const string SUB_SUBSCRIBE = SUB_CHANNEL + ".subscribe";
        private const string SUB_SUBSCRIPTION = SUB_CHANNEL + ".subscription";
        public const string SUB_SUBSCRIPTION_GIFT = SUB_SUBSCRIPTION + ".gift";
        public const string SUB_SUBSCRIPTION_MESSAGE = SUB_SUBSCRIPTION + ".message";


        private const string SUB_STREAM = "stream";
        public const string SUB_STREAM_ONLINE = SUB_STREAM + ".online";
        public const string SUB_STREAM_OFFLINE = SUB_STREAM + ".offline";

        private ImmutableArray<string> mValidSubscriptions = ImmutableArray.Create(
            SUB_CHANNEL_POINTS_REDEMPTION_ADD,
            SUB_CHANNEL_POINTS_REDEMPTION_UPDATE,
            SUB_CHEER,
            SUB_SUBSCRIBE,
            SUB_SUBSCRIPTION_GIFT,
            SUB_SUBSCRIPTION_MESSAGE,
            SUB_STREAM_ONLINE,
            SUB_STREAM_OFFLINE
        );

        private IUserContext mLBUser = null;
        private TwitchUserIdentity mChannelIdentity = null;
        private int mConnectionCounter = 0;
        private EventCallback mChannelPointsRedemptionCallback;
        private EventCallback mCheerCallback;
        private EventCallback mSubscriptionCallback;
        private EventCallback mStreamOnlineCallback;
        private EventCallback mStreamOfflineCallback;
        private ClientWebSocket mSocket = null;
        private Uri mConnectURI = null;
        private Token mToken = null;
        private string mSessionID = "";
        private int mKeepaliveTimeoutSeconds = 10;
        private Thread mReceiveThread = null;
        private AsyncLocal<string> mThreadLogPreamble = new();
        private bool mReceiveThreadDone = false;
        private Dictionary<string, API.Twitch.EventSubSubscriptionResponseData> mSubscriptions = new(); // id to subscription data acquired on creation
        private Queue<string> mSubscriptionQueue = new();
        private bool mCanSubscribe = false;
        private readonly object mProcessSubscriptionsLock = new(); // locks mCanSubscribe and mSubscriptionQueue
        private ClientWebSocket mOldSocket = null; // only used to store old connection until we switch to a new one

        // mostly used for tests to halt the test until Reconnect arrives and passes through
        public event EventHandler Connected;
        public event EventHandler Reconnected;

        public string SessionID
        {
            get
            {
                return mSessionID;
            }
        }

        private TwitchUserModule GetUserModule()
        {
            return Service.Get<ITwitchService>().GetModule(mLBUser) as TwitchUserModule;
        }

        private void OnConnected()
        {
            EventHandler handler = Connected;
            if (handler != null)
            {
                handler(this, null);
            }
        }

        private void OnReconnected()
        {
            EventHandler handler = Reconnected;
            if (handler != null)
            {
                handler(this, null);
            }
        }

        private void ValidateEventList(List<string> subs)
        {
            foreach (string s in subs)
            {
                bool found = false;
                foreach (string valid in mValidSubscriptions)
                {
                    if (s == valid)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    throw new ArgumentException("EventSub: Invalid event list provided");
            }
        }

        private string MapSubscriptionToVersion(string sub)
        {
            switch (sub)
            {
            case SUB_CHANNEL_POINTS_REDEMPTION_ADD:
            case SUB_CHANNEL_POINTS_REDEMPTION_UPDATE:
            case SUB_CHEER:
            case SUB_SUBSCRIBE:
            case SUB_SUBSCRIPTION_GIFT:
            case SUB_SUBSCRIPTION_MESSAGE:
            case SUB_STREAM_ONLINE:
            case SUB_STREAM_OFFLINE:
                return "1";
            default:
                return "Invalid";
            }
        }

        // IEventPublisher APIs and helpers
        private EventArgsBase GenerateTestChannelPointEvent(IEnumerable<(string attrib, string value)> args)
        {
            string user = "test_user";
            string displayName = "Test_User";
            string id = "1234test";
            string title = "Test redemption";
            int cost = 420;
            string prompt = "This is a test channel points redemption";
            string message = String.Empty;

            foreach ((string a, string v) a in args)
            {
                switch (a.a)
                {
                case "User": user = a.v; break;
                case "DisplayName": displayName = a.v; break;
                case "ID": id = a.v; break;
                case "Title": title = a.v; break;
                case "Cost": cost = Int32.Parse(a.v); break;
                case "Prompt": prompt = a.v; break;
                case "Message": message = a.v; break;
                default:
                    Logger.Log().Warning("Unknown test event arg: {0}", a.a);
                    break;
                }
            }

            TwitchChannelPointsRedemptionArgs ret = new (user, displayName, id, title, cost, prompt, message);
            UpdateMessageViaIdentity(ret.Message);
            return ret;
        }

        private EventArgsBase GenerateTestCheerEvent(IEnumerable<(string attrib, string value)> args)
        {
            string user = "test_user";
            string displayName = "Test_User";
            int amount = 1000;
            string message = "Test bits cheer";

            foreach ((string a, string v) a in args)
            {
                switch (a.a)
                {
                case "User": user = a.v; break;
                case "DisplayName": displayName = a.v; break;
                case "Amount": amount = Int32.Parse(a.v); break;
                case "Message": message = a.v; break;
                default:
                    Logger.Log().Warning("Unknown test event arg: {0}", a.a);
                    break;
                }
            }

            TwitchCheerArgs ret = new (Guid.NewGuid().ToString(), user, displayName, amount, message);
            UpdateMessageViaIdentity(ret.Message);
            return ret;
        }

        private EventArgsBase GenerateTestSubscriptionEvent(IEnumerable<(string attrib, string value)> args)
        {
            TwitchSubscriptionType type = TwitchSubscriptionType.New;
            string user = "test_user";
            string displayName = "Test_User";
            string message = "This is a test";
            int tier = 1000;

            foreach ((string a, string v) a in args)
            {
                switch (a.a)
                {
                case "Type": type = Enum.Parse<TwitchSubscriptionType>(a.v); break;
                case "User": user = a.v; break;
                case "DisplayName": displayName = a.v; break;
                case "Tier":
                {
                    tier = Int32.Parse(a.v);
                    if (tier == 1 || tier == 2 || tier == 3)
                    {
                        // Twitch numbers tiers by 1000's, this is a shorthand so it translate correctly
                        tier *= 1000;
                    }

                    if (tier != 1000 && tier != 2000 && tier != 3000)
                    {
                        Logger.Log().Warning("Tier {0} for test Subscription event is incorrect - valid values are 1(000), 2(000), 3(000). Defaulting to 1000.", tier);
                        tier = 1000;
                    }
                    break;
                }
                case "Message": message = a.v; break;
                default:
                    break;
                }
            }

            TwitchSubscriptionDetails details;
            switch (type)
            {
            case TwitchSubscriptionType.New: details = new TwitchSubscriptionDetails(); break;
            case TwitchSubscriptionType.Resub: details = new TwitchResubscriptionDetails(); break;
            case TwitchSubscriptionType.Gift: details = new TwitchGiftSubscriptionDetails(); break;
            default:
                details = new TwitchSubscriptionDetails();
                break;
            }

            details.FillStringArgs(args);
            TwitchSubscriptionArgs ret = new (Guid.NewGuid().ToString(), user, displayName, message, details);
            UpdateMessageViaIdentity(ret.Message);
            return ret;
        }

        private EventArgsBase GenerateTestStreamOnlineEvent(IEnumerable<(string attrib, string value)> args)
        {
            return new TwitchStreamOnlineArgs(mChannelIdentity.ID, mChannelIdentity.Username, DateTime.Now);
        }

        private EventArgsBase GenerateTestStreamOfflineEvent(IEnumerable<(string attrib, string value)> args)
        {
            return new TwitchStreamOfflineArgs(mChannelIdentity.ID, mChannelIdentity.Username);
        }

        public string GetEventPublisherName()
        {
            return "EventSubClient";
        }

        public List<EventDescriptor> GetEvents()
        {
            List<EventDescriptor> events = new();

            events.Add(new EventDescriptor()
            {
                Name = Events.TWITCH_CHANNEL_POINTS_REDEMPTION,
                Description = "Twitch Channel Points reward redemption. Generated when Twitch user redeems a specified Channel Points reward.",
                Dispatcher = Twitch.Utils.DispatcherNameForUser(mLBUser),
                TestGenerator = GenerateTestChannelPointEvent,
                TestParams = new List<EventTestParam>()
                {
                    new() { Name = "User", Description = "Username of Channel Points reward redeemer", Type = EventTestParamType.String },
                    new() { Name = "DisplayName", Description = "Display name of Channel Points reward redeemer", Type = EventTestParamType.String },
                    new() { Name = "ID", Description = "ID of redeemed Channel Points reward", Type = EventTestParamType.String },
                    new() { Name = "Title", Description = "Title of redeemed Channel Points reward", Type = EventTestParamType.String },
                    new() { Name = "Cost", Description = "Cost of redeemed Channel Points reward", Type = EventTestParamType.Integer },
                    new() { Name = "Prompt", Description = "Prompt for redeemed Channel Points reward", Type = EventTestParamType.String },
                    new() { Name = "Message", Description = "Message provided by user while redeeming this Channel Points reward", Type = EventTestParamType.String }
                }
            });
            events.Add(new EventDescriptor()
            {
                Name = Events.TWITCH_CHEER,
                Description = "Twitch bits Cheer. Generated when Twitch user cheers some bits on a channel.",
                Dispatcher = Twitch.Utils.DispatcherNameForUser(mLBUser),
                TestGenerator = GenerateTestCheerEvent,
                TestParams = new List<EventTestParam>()
                {
                    new() { Name = "User", Description = "Username of bits cheerer", Type = EventTestParamType.String },
                    new() { Name = "DisplayName", Description = "Display name of bits cheerer", Type = EventTestParamType.String },
                    new() { Name = "Amount", Description = "Total amount of bits cheered", Type = EventTestParamType.Integer },
                    new() { Name = "Message", Description = "Message that caused the cheer", Type = EventTestParamType.String }
                }
            });
            events.Add(new EventDescriptor()
            {
                Name = Events.TWITCH_SUBSCRIPTION,
                Description = "Twitch channel subscription. Generated when Twitch user subscribes, resubscribes or gifts a subscription in the channel.",
                Dispatcher = Twitch.Utils.DispatcherNameForUser(mLBUser),
                TestGenerator = GenerateTestSubscriptionEvent,
                TestParams = new List<EventTestParam>()
                {
                    new() { Name = "Type", Description = "Type of subscription (New, Resub, Gift)", Type = EventTestParamType.String },
                    new() { Name = "User", Description = "Username of subscriber", Type = EventTestParamType.String },
                    new() { Name = "DisplayName", Description = "Display name of subscriber", Type = EventTestParamType.String },
                    new() { Name = "Tier", Description = "Tier of subscription", Type = EventTestParamType.Integer },
                    new() { Name = "Cumulative", Description = "(Resub-only) Number of total subscriptions", Type = EventTestParamType.Integer },
                    new() { Name = "Streak", Description = "(Resub-only) Subscription streak", Type = EventTestParamType.Integer },
                    new() { Name = "Duration", Description = "(Resub-only) Duration of subscription in months", Type = EventTestParamType.Integer },
                    new() { Name = "Message", Description = "(Resub-only) Resubscription message", Type = EventTestParamType.String },
                    new() { Name = "Recipents", Description = "(Gift-only) Gift recipent count", Type = EventTestParamType.Integer },
                }
            });
            events.Add(new EventDescriptor()
            {
                Name = Events.TWITCH_STREAM_ONLINE,
                Description = "Twitch stream going online/live. This is (mostly) an internal event.",
                TestGenerator = GenerateTestStreamOnlineEvent,
                TestParams = new List<EventTestParam>()
            });
            events.Add(new EventDescriptor()
            {
                Name = Events.TWITCH_STREAM_OFFLINE,
                Description = "Twitch stream going offline. This is (mostly) an internal event.",
                TestGenerator = GenerateTestStreamOfflineEvent,
                TestParams = new List<EventTestParam>()
            });

            return events;
        }


        public EventSubClient(IUserContext lbUser, TwitchUserIdentity channelIdentity)
        {
            mLBUser = lbUser;
            mChannelIdentity = channelIdentity;

            List<EventCallback> events = ServiceUtils.GetEventService().User(mLBUser.GetUsername()).RegisterPublisher(this);

            foreach (EventCallback e in events)
            {
                switch (e.eventName)
                {
                case Events.TWITCH_CHANNEL_POINTS_REDEMPTION:
                    mChannelPointsRedemptionCallback = e;
                    break;
                case Events.TWITCH_CHEER:
                    mCheerCallback = e;
                    break;
                case Events.TWITCH_SUBSCRIPTION:
                    mSubscriptionCallback = e;
                    break;
                case Events.TWITCH_STREAM_ONLINE:
                    mStreamOnlineCallback = e;
                    break;
                case Events.TWITCH_STREAM_OFFLINE:
                    mStreamOfflineCallback = e;
                    break;
                default:
                    Logger.Log().Warning("Received unknown event type from Event system");
                    break;
                }
            }

            mReceiveThread = new(ReceiveThreadMain);
            mReceiveThread.Name = "EventSub Receive Thread (" + mLBUser.GetUsername() + ")";
        }

        public async Task<EventSub.Message> ReceiveAsync()
        {
            EventSub.Message result = new();

            if (mSocket == null || mSocket.State != WebSocketState.Open)
            {
                // Socket is broken, try reconnecting
                Logger.Log().Warning("Socket is not open for unknown reason, attempting reconnect...");
                result.Status = EventSub.InternalStatus.Reconnect;
                return result;
            }

            string recvMsgString = "";
            byte[] buffer = new byte[1024];
            WebSocketReceiveResult recvResult;

            try
            {
                do
                {
                    // Timeout is whatever Keepalive seconds we received from server (see Connect()) plus
                    // an extra 5 seconds cause we feel generous
                    CancellationToken cancelToken = new CancellationTokenSource((mKeepaliveTimeoutSeconds + 5) * 1000).Token;
                    recvResult = await mSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken);
                    recvMsgString += Encoding.UTF8.GetString(buffer, 0, recvResult.Count);
                }
                while (!recvResult.EndOfMessage);

                if (recvResult.MessageType == WebSocketMessageType.Close)
                {
                    result.Status = EventSub.InternalStatus.Closed;
                    return result;
                }

                JsonSerializerOptions opts = new();
                opts.Converters.Add(new EventSub.MessageDeserializer());
                result = JsonSerializer.Deserialize<EventSub.Message>(recvMsgString, opts);
                if (result == null)
                {
                    Logger.Log().Error("EventSubClient {0}: Failed to deserialize EventSub message. Closing connection just in case.", mLBUser.GetUsername());
                    result = new();
                    result.Status = EventSub.InternalStatus.Closed;
                }
                else
                {
                    result.Status = EventSub.InternalStatus.Fine;
                }
            }
            catch (OperationCanceledException)
            {
                // Reconnect, as we did not receive a single message for more than Keepalive seconds timeout
                Logger.Log().Warning("EventSubClient {0}: Keepalive timer expired - attempting to reconnect...", mLBUser.GetUsername());
                result.Status = EventSub.InternalStatus.Reconnect;
                return result;
            }
            catch (System.Exception e)
            {
                Logger.Log().Warning("EventSubClient {0}: Other exception caught - attempting to reconnect...", mLBUser.GetUsername());
                Logger.Log().Trace("Caught: {0}\n{1}", e.Message, e.StackTrace);
                result.Status = EventSub.InternalStatus.Reconnect;
                return result;
            }

            return result;
        }

        private async Task Reconnect(string newURL, bool resub = false)
        {
            lock (mProcessSubscriptionsLock)
            {
                mCanSubscribe = false;
            }

            if (resub)
            {
                lock (mProcessSubscriptionsLock)
                {
                    // Sometimes we have to re-subscribe to all events upon reconnect.
                    // Move all actual subscriptions to mSubscriptionQueue so that when we pick up
                    // a Welcome message we will handle them
                    foreach (API.Twitch.EventSubSubscriptionResponseData subData in mSubscriptions.Values)
                    {
                        mSubscriptionQueue.Enqueue(subData.type);
                    }

                    mSubscriptions.Clear();
                }
            }

            Logger.Log().Info("EventSubClient {0}: Reconnecting...", mLBUser.GetUsername());
            ClientWebSocket newSocket = await ConnectInternal(mToken, newURL);

            mOldSocket = mSocket;
            mSocket = newSocket;
            Logger.Log().Info("EventSubClient {0}: Reconnect: New socket acquired", mLBUser.GetUsername());
        }

        private void UpdateMessageViaIdentity(TwitchChatMessageArgs msg)
        {
            if (msg == null) return;

            try
            {
                TwitchUserCollection userCollection = GetUserModule().GetTwitchUsers();
                TwitchUserIdentity identity = userCollection.FetchUser(mToken, true, msg.User);

                msg.Color = identity.Color;
                if (identity.Badges.Count > 0)
                {
                    msg.Badges.AddRange(identity.Badges);
                }
            }
            catch (System.Exception e) when (e is KeyNotFoundException || e is APIErrorException)
            {
                // user was not present and fetch failed - assume test generator called us, set default color and quietly leave
                msg.Color = Constants.DEFAULT_CHAT_USER_COLOR;
                return;
            }
        }

        private void EmitChannelPointsEvent(EventSub.PayloadEvent eventData)
        {
            EventSub.PayloadChannelPointRedemptionEvent data = eventData as EventSub.PayloadChannelPointRedemptionEvent;

            if (data == null)
            {
                Logger.Log().Error("EventSubClient {0}: Got invalid Event Data payload, ignoring notification", mLBUser.GetUsername());
                return;
            }

            if (data.reward == null)
            {
                Logger.Log().Error("EventSubClient {0}: Reward data is null, ignoring notification", mLBUser.GetUsername());
                return;
            }

            TwitchChannelPointsRedemptionArgs args = new(data.user_login, data.user_name,
                data.reward.id, data.reward.title, data.reward.cost, data.reward.prompt, data.user_input);
            UpdateMessageViaIdentity(args.Message);
            mChannelPointsRedemptionCallback.PublishEvent(args);
        }

        private void EmitCheerEvent(EventSub.PayloadEvent eventData)
        {
            EventSub.PayloadCheerEvent data = eventData as EventSub.PayloadCheerEvent;

            string login, displayName;
            if (data.is_anonymous)
            {
                login = "anonymous";
                displayName = "Anonymous";
            }
            else
            {
                login = data.user_login;
                displayName = data.user_name;
            }

            TwitchCheerArgs args = new(Guid.NewGuid().ToString(), login, displayName, data.bits, data.message);
            UpdateMessageViaIdentity(args.Message);
            mCheerCallback.PublishEvent(args);
        }

        private void EmitSubscriptionEvent(TwitchSubscriptionType type, EventSub.PayloadEvent eventData)
        {
            TwitchSubscriptionDetails details;
            string resubMessage = null;

            switch (type)
            {
            case TwitchSubscriptionType.New:
            {
                EventSub.PayloadSubEvent data = eventData as EventSub.PayloadSubEvent;
                details = new TwitchSubscriptionDetails(Int32.Parse(data.tier));
                break;
            }
            case TwitchSubscriptionType.Resub:
            {
                EventSub.PayloadSubMessageEvent data = eventData as EventSub.PayloadSubMessageEvent;
                details = new TwitchResubscriptionDetails(
                    Int32.Parse(data.tier) / 1000,
                    data.cumulative_months,
                    (data.streak_months != null) ? (int)data.streak_months : 0,
                    data.duration_months
                );
                resubMessage = data.message.text;
                break;
            }
            case TwitchSubscriptionType.Gift:
            {
                // TODO I wanna make this smarter. I'd love to set up a "gift pending" situation here
                // and then fetch next data.total subscriptions
                EventSub.PayloadSubGiftEvent data = eventData as EventSub.PayloadSubGiftEvent;
                details = new TwitchGiftSubscriptionDetails(Int32.Parse(data.tier), data.total);
                break;
            }
            default:
                throw new ArgumentException();
            }

            TwitchSubscriptionArgs subArgs = new(Guid.NewGuid().ToString(),
                eventData.user_login, eventData.user_name, resubMessage, details
            );
            UpdateMessageViaIdentity(subArgs.Message);
            mSubscriptionCallback.PublishEvent(subArgs);
        }

        private void EmitStreamOnlineEvent(EventSub.PayloadEvent eventData)
        {
            EventSub.PayloadStreamOnlineEvent data = eventData as EventSub.PayloadStreamOnlineEvent;

            TwitchStreamOnlineArgs args = new(data.broadcaster_user_id, data.broadcaster_user_login, data.started_at);
            mStreamOnlineCallback.PublishEvent(args);
        }

        private void EmitStreamOfflineEvent(EventSub.PayloadEvent eventData)
        {
            EventSub.PayloadStreamOfflineEvent data = eventData as EventSub.PayloadStreamOfflineEvent;

            TwitchStreamOfflineArgs args = new(data.broadcaster_user_id, data.broadcaster_user_login);
            mStreamOfflineCallback.PublishEvent(args);
        }

        private void ProcessSubscriptionQueue()
        {
            lock (mProcessSubscriptionsLock)
            {
                // it can happen we are here before we got the Welcome message
                // if we are, silently exit - receive thread will handle this process for us
                if (!mCanSubscribe || (mSubscriptionQueue.Count == 0)) return;

                Logger.Log().Debug("EventSubClient {0}: Processing subscriptions", mLBUser.GetUsername());
                while (mSubscriptionQueue.Count > 0)
                {
                    string sub = mSubscriptionQueue.Dequeue();

                    if (mSubscriptions.ContainsKey(sub))
                    {
                        Logger.Log().Warning("EventSubClient {0}: Already subscribed to {1}, skipping", mLBUser.GetUsername(), sub);
                        continue;
                    }

                    API.Twitch.CreateEventSubSubscriptionResponse resp = API.Twitch.CreateEventSubSubscription(
                        mToken,
                        sub,
                        MapSubscriptionToVersion(sub),
                        mChannelIdentity.ID,
                        mSessionID
                    );

                    if (!resp.IsSuccess)
                    {
                        Logger.Log().Error("EventSubClient {0}: Failed to subscribe to {1}: {2} ({3}).", mLBUser.GetUsername(), sub, resp.code, resp.responseData.message);
                        Logger.Log().Error("EventSubClient {0}: Subscription will be skipped until next EventSubClient reconnect", mLBUser.GetUsername());
                        continue;
                    }

                    mSubscriptions.Add(resp.data[0].id, resp.data[0]);
                    Logger.Log().Info("EventSubClient {0}: Subscribed to {1}", mLBUser.GetUsername(), sub);
                }
            }
        }

        private async Task HandleSessionWelcome(EventSub.Message msg)
        {
            if (msg.Status != EventSub.InternalStatus.Fine ||
                msg.Metadata.message_type != EventSub.MessageType.session_welcome)
            {
                Logger.Log().Error("EventSubClient {0}: Received an invalid welcome message from Twitch - aborting.", mThreadLogPreamble.Value);
                await mSocket.CloseAsync(WebSocketCloseStatus.ProtocolError, "Invalid welcome message", new CancellationTokenSource(mKeepaliveTimeoutSeconds * 1000).Token);
                throw new EventSubConnectFailedException();
            }

            Logger.Log().Info("EventSubClient {0}: Welcome", mThreadLogPreamble.Value);
            // Update necessary parameters for further work
            mSessionID = msg.Payload.Session.id;
            if (msg.Payload.Session.keepalive_timeout_seconds != null)
                mKeepaliveTimeoutSeconds = (int)msg.Payload.Session.keepalive_timeout_seconds;

            // close old connection if it exists
            if (mOldSocket != null)
            {
                try
                {
                    // attempt to gracefully close the socket
                    if (mOldSocket.State == WebSocketState.Open || mOldSocket.State == WebSocketState.CloseSent || mOldSocket.State == WebSocketState.CloseReceived)
                    {
                        Logger.Log().Info("EventSubClient {0}: Closing old socket...", mThreadLogPreamble.Value);
                        await mOldSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    }
                    else if (mOldSocket.State != WebSocketState.Aborted)
                    {
                        Logger.Log().Info("EventSubClient {0}: Old socket in an invalid state, aborting its connection...", mThreadLogPreamble.Value);
                        mOldSocket.Abort();
                    }
                }
                catch (System.Exception e)
                {
                    Logger.Log().Warning("EventSubClient {0}: Exception caught while closing old socket, aborting socket: {1}",
                        mThreadLogPreamble.Value, e.Message);
                    mOldSocket.Abort();
                }
            }

            lock (mProcessSubscriptionsLock)
            {
                // allow for processing subscriptions
                mCanSubscribe = true;
            }

            // immediately check if we have any subscriptions in queue and subscribe if we do
            ProcessSubscriptionQueue();

            if (mOldSocket != null)
            {
                mOldSocket.Dispose();
                mOldSocket = null;

                OnReconnected();
                Logger.Log().Info("EventSubClient {0}: Reconnect completed", mThreadLogPreamble.Value);
            }

            OnConnected();
        }

        private void HandleSessionKeepalive()
        {
            // NO-OP - this is purely a message to ensure the server is healthy.
            // Its purpose is to clear the keepalive timeout timer when no other message came through.
            // If we don't get it within mKeepaliveTimeoutSeconds seconds, we should reconnect. This is
            // handled inside ReceiveAsync as a timeout.
            //Logger.Log().Debug("EventSubClient {0}: Keepalive", mThreadLogPreamble.Value);
        }

        private async Task HandleSessionReconnect(EventSub.PayloadSession sessionReconnect)
        {
            Logger.Log().Info("EventSubClient {0}: Reconnect", mThreadLogPreamble.Value);
            await Reconnect(sessionReconnect.reconnect_url);
        }

        private void HandleNotification(EventSub.PayloadSubscription subscription, EventSub.PayloadEvent eventData)
        {
            Logger.Log().Info("EventSubClient {0}: Notification: {1}", mThreadLogPreamble.Value, subscription.type);

            switch (subscription.type)
            {
            case SUB_CHANNEL_POINTS_REDEMPTION_ADD:
                EmitChannelPointsEvent(eventData);
                break;
            case SUB_CHEER:
                EmitCheerEvent(eventData);
                break;
            case SUB_SUBSCRIBE:
                EmitSubscriptionEvent(TwitchSubscriptionType.New, eventData);
                break;
            case SUB_SUBSCRIPTION_GIFT:
                EmitSubscriptionEvent(TwitchSubscriptionType.Gift, eventData);
                break;
            case SUB_SUBSCRIPTION_MESSAGE:
                EmitSubscriptionEvent(TwitchSubscriptionType.Resub, eventData);
                break;
            case SUB_STREAM_ONLINE:
                EmitStreamOnlineEvent(eventData);
                break;
            case SUB_STREAM_OFFLINE:
                EmitStreamOfflineEvent(eventData);
                break;
            default:
                Logger.Log().Warning("EventSubClient {0}: Unknown notification received", mThreadLogPreamble.Value);
                break;
            }
        }

        private void HandleRevocation()
        {
            Logger.Log().Debug("EventSubClient {0}: Revocation", mThreadLogPreamble.Value);
        }

        private async void ReceiveThreadMain()
        {
            mThreadLogPreamble.Value = String.Format("{0} (RT#{1})", mLBUser.GetUsername(), Thread.CurrentThread.ManagedThreadId);
            Logger.Log().Info("EventSubClient {0}: Receive thread started", mThreadLogPreamble.Value);

            while (!mReceiveThreadDone)
            {
                try
                {
                    EventSub.Message msg = await ReceiveAsync();

                    switch (msg.Status)
                    {
                    case EventSub.InternalStatus.Reconnect:
                        await Reconnect(EVENTSUB_URI_MAIN, true);
                        continue;
                    case EventSub.InternalStatus.Closed:
                        mReceiveThreadDone = true;
                        continue;
                    }

                    switch (msg.Metadata.message_type)
                    {
                    case EventSub.MessageType.session_welcome:
                        await HandleSessionWelcome(msg);
                        break;
                    case EventSub.MessageType.session_keepalive:
                        HandleSessionKeepalive();
                        break;
                    case EventSub.MessageType.session_reconnect:
                        await HandleSessionReconnect(msg.Payload.Session);
                        break;
                    case EventSub.MessageType.notification:
                        HandleNotification(msg.Payload.Subscription, msg.Payload.Event);
                        break;
                    case EventSub.MessageType.revocation:
                        HandleRevocation();
                        break;
                    default:
                        Logger.Log().Error("EventSubClient {0}: Invalid EventSub message type: {1}", mThreadLogPreamble.Value, msg.Metadata.message_type);
                        break;
                    }
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("EventSubClient {0}: Caught exception on EventSub recv thread: {1}", mThreadLogPreamble.Value, e.Message);
                    Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
                    Logger.Log().Warning("EventSubClient {0}: Attempting reconnect...", mThreadLogPreamble.Value);
                    try
                    {
                        await mSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                        await Reconnect(EVENTSUB_URI_MAIN, true);
                    }
                    catch (System.Exception ie)
                    {
                        Logger.Log().Error("EventSubClient {0}: Error when attempting to recover: {1}", mThreadLogPreamble.Value, ie.Message);
                        Logger.Log().Error("EventSubClient {0}: Will remain shut down. Manual restart required with \"twitch eventsub-restart\"", mThreadLogPreamble.Value);
                        mReceiveThreadDone = true;
                        break;
                    }
                }
            }

            Logger.Log().Info("EventSubClient {0}: Disconnected, receive thread done.", mThreadLogPreamble.Value);
        }

        private async Task<ClientWebSocket> ConnectInternal(Token token, string url)
        {
            mToken = token;
            mConnectURI = new(url);

            ClientWebSocket socket = new ClientWebSocket();
            mConnectionCounter++;
            await socket.ConnectAsync(mConnectURI, new CancellationTokenSource(mKeepaliveTimeoutSeconds * 1000).Token);

            return socket;
        }

        public void Connect(Token token, string url = EVENTSUB_URI_MAIN)
        {
            var mSocketTask = ConnectInternal(token, url);
            mSocketTask.Wait();
            mSocket = mSocketTask.Result;

            // fire the receive thread
            mReceiveThreadDone = false;
            mReceiveThread.Start();
        }

        public async Task ConnectAsync(Token token, string url = EVENTSUB_URI_MAIN)
        {
            mSocket = await ConnectInternal(token, url);

            // fire the receive thread
            mReceiveThreadDone = false;
            mReceiveThread.Start();
        }

        public void Subscribe(List<string> events)
        {
            if (mSocket == null)
            {
                Logger.Log().Warning("EventSubClient {0}: Cannot subscribe to events, EventSub was not connected.", mLBUser.GetUsername());
                return;
            }

            ValidateEventList(events);

            lock (mProcessSubscriptionsLock)
            {
                foreach (string e in events)
                {
                    mSubscriptionQueue.Enqueue(e);
                }
            }

            ProcessSubscriptionQueue();
        }

        public async void RequestShutdown()
        {
            mReceiveThreadDone = true;
            if (mSocket != null && mSocket.State != WebSocketState.Aborted)
            {
                try
                {
                    await mSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("EventSubClient {0}: Error during EventSub shutdown request: {1}", mLBUser.GetUsername(), e.Message);
                }
            }
        }

        public void WaitForShutdown()
        {
            if (mReceiveThread != null && mReceiveThread.ThreadState != ThreadState.Unstarted)
                mReceiveThread.Join();

            mSocket = null;
            ServiceUtils.GetEventService().User(mLBUser.GetUsername()).UnregisterPublisher(this);
        }
    }
}
