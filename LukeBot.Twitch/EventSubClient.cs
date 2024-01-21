using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.API;
using LukeBot.Communication;
using LukeBot.Communication.Common;
using LukeBot.Twitch.Common;
using LukeBot.Logging;
using Newtonsoft.Json;


[assembly: InternalsVisibleTo("LukeBot.Tests")]

namespace LukeBot.Twitch
{
    internal class EventSubClient: IEventPublisher
    {
        public const string EVENTSUB_URI_MAIN = "wss://eventsub.wss.twitch.tv/ws";

        private const string SUB_CHANNEL = "channel";

        private const string SUB_CHANNEL_POINTS_REDEMPTION = SUB_CHANNEL + ".channel_points_custom_reward_redemption";
        public const string SUB_CHANNEL_POINTS_REDEMPTION_ADD = SUB_CHANNEL_POINTS_REDEMPTION + ".add";
        public const string SUB_CHANNEL_POINTS_REDEMPTION_UPDATE = SUB_CHANNEL_POINTS_REDEMPTION + ".update";

        public const string SUB_SUBSCRIBE = SUB_CHANNEL + ".subscribe";
        private const string SUB_SUBSCRIPTION = SUB_CHANNEL + ".subscription";
        public const string SUB_SUBSCRIPTION_GIFT = SUB_SUBSCRIPTION + ".gift";
        public const string SUB_SUBSCRIPTION_MESSAGE = SUB_SUBSCRIPTION + ".message";

        private ImmutableArray<string> mValidSubscriptions = ImmutableArray.Create(
            SUB_CHANNEL_POINTS_REDEMPTION_ADD,
            SUB_CHANNEL_POINTS_REDEMPTION_UPDATE,
            SUB_SUBSCRIBE,
            SUB_SUBSCRIPTION_GIFT,
            SUB_SUBSCRIPTION_MESSAGE
        );

        private string mLBUser = null;
        private EventCallback mChannelPointsRedemptionCallback;
        private EventCallback mSubscriptionCallback;
        private ClientWebSocket mSocket = null;
        private Uri mConnectURI = null;
        private Token mToken = null;
        private string mUserID = "";
        private string mSessionID = "";
        private int mKeepaliveTimeoutSeconds = 10;
        private Thread mReceiveThread = null;
        private bool mReceiveThreadDone = false;
        private AutoResetEvent mSessionReadyEvent = new(false);
        private Dictionary<string, API.Twitch.EventSubSubscriptionResponseData> mSubscriptions = new(); // id to subscription data acquired on creation

        // mostly used for tests to halt the test until Reconnect arrives and passes through
        public event EventHandler Reconnected;

        public string SessionID
        {
            get
            {
                return mSessionID;
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
            case SUB_SUBSCRIBE:
            case SUB_SUBSCRIPTION_GIFT:
            case SUB_SUBSCRIPTION_MESSAGE:
                return "1";
            default:
                return "Invalid";
            }
        }

        // IEventPublisher APIs
        public string GetName()
        {
            return "EventSubClient";
        }

        public List<EventDescriptor> GetEvents()
        {
            List<EventDescriptor> events = new();

            events.Add(new EventDescriptor()
            {
                Name = Events.TWITCH_CHANNEL_POINT_REDEMPTION,
                TargetDispatcher = null // TODO SHOULD BE A QUEUED DISPATCHER
            });
            events.Add(new EventDescriptor()
            {
                Name = Events.TWITCH_SUBSCRIPTION,
                TargetDispatcher = null // TODO SHOULD BE A QUEUED DISPATCHER
            });

            return events;
        }

        public EventSubClient(string lbUser)
        {
            mLBUser = lbUser;
            mReceiveThread = new(ReceiveThreadMain);
            mReceiveThread.Name = "EventSub Receive Thread";

            List<EventCallback> events = Comms.Event.User(mLBUser).RegisterPublisher(this);

            foreach (EventCallback e in events)
            {
                switch (e.eventName)
                {
                case Events.TWITCH_CHANNEL_POINT_REDEMPTION:
                    mChannelPointsRedemptionCallback = e;
                    break;
                case Events.TWITCH_SUBSCRIPTION:
                    mSubscriptionCallback = e;
                    break;
                default:
                    Logger.Log().Warning("Received unknown event type from Event system");
                    break;
                }
            }
        }

        ~EventSubClient()
        {
            RequestShutdown();
            WaitForShutdown();
        }

        public async Task<EventSub.Message> ReceiveAsync()
        {
            EventSub.Message result = new();

            if (mSocket.State != WebSocketState.Open)
                return result;

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
            }
            catch (OperationCanceledException)
            {
                // Reconnect, as we did not receive a single message for more than Keepalive seconds timeout
                result.Status = EventSub.InternalStatus.Reconnect;
                return result;
            }

            if (recvResult.MessageType == WebSocketMessageType.Close)
            {
                result.Success = true;
                result.Status = EventSub.InternalStatus.Closed;
                return result;
            }

            result = JsonConvert.DeserializeObject<EventSub.Message>(recvMsgString, new EventSub.Deserializer());
            result.Success = true;

            return result;
        }

        private async Task Reconnect(string newURL, bool resub = false)
        {
            ClientWebSocket newSocket = await ConnectInternal(mToken, mUserID, newURL);

            if (resub)
            {
                // Sometimes we have to re-subscribe to all events upon reconnect.
                // Form new subscription list based on existing one.
                // TODO
            }

            if (mSocket.State == WebSocketState.Open)
            {
                await mSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            }

            // TODO This is a race condition potentially with Receive Thread
            mSocket.Abort();
            mSocket = newSocket;
            OnReconnected();
        }

        private void EmitChannelPointsEvent(EventSub.PayloadEvent eventData)
        {
            EventSub.PayloadChannelPointRedemptionEvent data = eventData as EventSub.PayloadChannelPointRedemptionEvent;

            if (data.reward == null)
            {
                Logger.Log().Warning("EventSub: Reward data is null, ignoring notification");
                return;
            }

            TwitchChannelPointsRedemptionArgs args = new(data.user_login, data.user_name, data.reward.title);
            mChannelPointsRedemptionCallback.PublishEvent(args);
        }

        private void EmitSubscriptionEvent(TwitchSubscriptionType type, EventSub.PayloadEvent eventData)
        {
            TwitchSubscriptionDetails details;

            switch (type)
            {
            case TwitchSubscriptionType.New:
            {
                EventSub.PayloadSubEvent data = eventData as EventSub.PayloadSubEvent;
                details = new TwitchSubscriptionDetails(data.tier);
                break;
            }
            case TwitchSubscriptionType.Resubscription:
            {
                EventSub.PayloadSubMessageEvent data = eventData as EventSub.PayloadSubMessageEvent;
                details = new TwitchResubscriptionDetails(
                    data.tier,
                    data.cumulative_months,
                    (data.streak_months != null) ? (int)data.streak_months : 0,
                    data.duration_months
                );
                break;
            }
            case TwitchSubscriptionType.Gift:
            {
                // TODO I wanna make this smarter. I'd love to set up a "gift pending" situation here
                // and then fetch next data.total subscription
                EventSub.PayloadSubGiftEvent data = eventData as EventSub.PayloadSubGiftEvent;
                details = new TwitchGiftSubscriptionDetails(data.tier, data.total);
                break;
            }
            default:
                throw new ArgumentException();
            }

            TwitchSubscriptionArgs subArgs = new(eventData.user_login, eventData.user_name, details);

            mSubscriptionCallback.PublishEvent(subArgs);
        }

        private async Task HandleSessionWelcome(EventSub.Message msg)
        {
            if (!msg.Success || msg.Status != EventSub.InternalStatus.Fine ||
                msg.Metadata.message_type != EventSub.MessageType.session_welcome)
            {
                Logger.Log().Error("Received an invalid welcome message from Twitch - aborting.");
                await mSocket.CloseAsync(WebSocketCloseStatus.ProtocolError, "Invalid welcome message", new CancellationTokenSource(mKeepaliveTimeoutSeconds * 1000).Token);
                throw new EventSubConnectFailedException();
            }

            Logger.Log().Debug("EventSub: Welcome");
            // Update necessary parameters for further work
            mSessionID = msg.Payload.Session.id;
            if (msg.Payload.Session.keepalive_timeout_seconds != null)
                mKeepaliveTimeoutSeconds = (int)msg.Payload.Session.keepalive_timeout_seconds;

            mSessionReadyEvent.Set();
        }

        private void HandleSessionKeepalive()
        {
            // NO-OP - this is purely a message to ensure the server is healthy.
            // Its purpose is to clear the keepalive timeout timer when no other message came through.
            // If we don't get it within mKeepaliveTimeoutSeconds seconds, we should reconnect. This is
            // handled inside ReceiveAsync as a timeout.
            //Logger.Log().Debug("EventSub: Keepalive");
        }

        private async Task HandleSessionReconnect(EventSub.PayloadSession sessionReconnect)
        {
            Logger.Log().Debug("EventSub: Reconnect");
            await Reconnect(sessionReconnect.reconnect_url);
        }

        private void HandleNotification(EventSub.PayloadSubscription subscription, EventSub.PayloadEvent eventData)
        {
            Logger.Log().Debug("EventSub: Notification: {0}", subscription.type);

            switch (subscription.type)
            {
            case SUB_CHANNEL_POINTS_REDEMPTION_ADD:
                EmitChannelPointsEvent(eventData);
                break;
            case SUB_SUBSCRIBE:
                EmitSubscriptionEvent(TwitchSubscriptionType.New, eventData);
                break;
            case SUB_SUBSCRIPTION_GIFT:
                EmitSubscriptionEvent(TwitchSubscriptionType.Gift, eventData);
                break;
            case SUB_SUBSCRIPTION_MESSAGE:
                EmitSubscriptionEvent(TwitchSubscriptionType.Resubscription, eventData);
                break;
            default:
                Logger.Log().Warning("EventSub: Unknown notification received");
                break;
            }
        }

        private void HandleRevocation()
        {
            Logger.Log().Debug("EventSub: Revocation");
        }

        private async void ReceiveThreadMain()
        {
            while (!mReceiveThreadDone)
            {
                EventSub.Message msg = await ReceiveAsync();

                if (!msg.Success)
                {
                    Logger.Log().Error("Received unsuccessful message from EventSub");
                    continue;
                }

                switch (msg.Status)
                {
                case EventSub.InternalStatus.Reconnect:
                    Logger.Log().Warning("Keepalive timeout occured, reconnecting...");
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
                    Logger.Log().Error("Invalid EventSub message type: {0}", msg.Metadata.message_type);
                    break;
                }
            }
        }

        private async Task<ClientWebSocket> ConnectInternal(Token token, string userId, string url)
        {
            mSessionReadyEvent.Reset();

            mToken = token;
            mUserID = userId;
            mConnectURI = new(url);

            ClientWebSocket socket = new ClientWebSocket();
            await socket.ConnectAsync(mConnectURI, new CancellationTokenSource(mKeepaliveTimeoutSeconds * 1000).Token);

            return socket;
        }

        public void Connect(Token token, string userId, string url = EVENTSUB_URI_MAIN)
        {
            var mSocketTask = ConnectInternal(token, userId, url);
            mSocketTask.Wait();
            mSocket = mSocketTask.Result;

            // fire the receive thread
            mReceiveThread.Start();
        }

        public async Task ConnectAsync(Token token, string userId, string url = EVENTSUB_URI_MAIN)
        {
            mSocket = await ConnectInternal(token, userId, url);

            mReceiveThread.Start();
        }

        public void Subscribe(List<string> events)
        {
            ValidateEventList(events);

            // wait for welcome message to appear
            mSessionReadyEvent.WaitOne();

            // Subscribe to requested events
            mSubscriptions.Clear();
            foreach (string sub in events)
            {
                API.Twitch.CreateEventSubSubscriptionResponse resp = API.Twitch.CreateEventSubSubscription(
                    mToken,
                    sub,
                    MapSubscriptionToVersion(sub),
                    mUserID,
                    mSessionID
                );

                if (!resp.IsSuccess)
                {

                    throw new EventSubSubscriptionFailedException(sub, resp.code, resp.responseData.message);
                }

                mSubscriptions.Add(resp.data[0].id, resp.data[0]);
            }
        }

        public async void RequestShutdown()
        {
            mReceiveThreadDone = true;
            if (mSocket != null)
            {
                try
                {
                    await mSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("Error during EventSub shutdown request: {0}", e.Message);
                }
            }
        }

        public void WaitForShutdown()
        {
            if (mReceiveThread != null && mReceiveThread.ThreadState != ThreadState.Unstarted)
                mReceiveThread.Join();

            mSocket = null;
            Comms.Event.User(mLBUser).UnregisterPublisher(this);
        }
    }
}
