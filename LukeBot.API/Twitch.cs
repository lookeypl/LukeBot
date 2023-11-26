using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using LukeBot.Config;
using LukeBot.Logging;
using Newtonsoft.Json;


namespace LukeBot.API
{
    public class Twitch
    {
        public const string DEFAULT_API_URI = "https://api.twitch.tv/helix";

        private static readonly string API_URI = GetEndpoint();
        private static readonly string GET_USERS_API_URI = API_URI + "/users";
        private static readonly string GET_CHANNEL_INFORMATION_API_URI = API_URI + "/channels";

        private static readonly string EVENTSUB_BASE_URI = API_URI + "/eventsub";
        private static readonly string EVENTSUB_SUBSCRIPTIONS_API_URI = EVENTSUB_BASE_URI + "/subscriptions";

        private static string GetEndpoint()
        {
            if (Conf.TryGet<string>(Path.Parse("twitch.api_endpoint"), out string ret))
            {
                Logger.Log().Debug("Hostname: {0}", ret);
                return ret;
            }
            else
                return DEFAULT_API_URI;
        }

        public class PaginationData
        {
            public string cursor { get; set; }
        }

        public struct GetUserData
        {
            public string broadcaster_type { get; set; }
            public string description { get; set; }
            public string display_name { get; set; }
            public string id { get; set; }
            public string login { get; set; }
            public string offline_image_url { get; set; }
            public string profile_image_url { get; set; }
            public string type { get; set; }
            public int view_count { get; set; }
            public string email { get; set; }
            public string created_at { get; set; }
        }

        public class GetUserResponse: Response
        {
            public List<GetUserData> data { get; set; }
        }

        public struct GetChannelInformationData
        {
            public string broadcaster_id { get; set; }
            public string broadcaster_name { get; set; }
            public string game_name { get; set; }
            public string game_id { get; set; }
            public string broadcaster_language { get; set; }
            public string title { get; set; }
            public int delay { get; set; }
        }

        public class GetChannelInformationResponse: Response
        {
            public List<GetChannelInformationData> data { get; set; }
        }

        public class EventSubSubscriptionCondition
        {
            public string broadcaster_user_id { get; set; }

            public EventSubSubscriptionCondition(string userId)
            {
                broadcaster_user_id = userId;
            }
        }

        public class EventSubSubscriptionTransport
        {
            public string method { get; set; }
            public string session_id { get; set; }

            [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
            public DateTime? connected_at { get; set; }
            [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
            public DateTime? disconnected_at { get; set; }

            public EventSubSubscriptionTransport(string sessionId)
            {
                // we only support websockets as transport method
                method = "websocket";
                session_id = sessionId;
                // below fields are only returned by EventSub
                connected_at = null;
                disconnected_at = null;
            }
        }

        public class EventSubSubscriptionData
        {
            public string type { get; set; }
            public string version { get; set; }
            public EventSubSubscriptionCondition condition { get; set; }
            public EventSubSubscriptionTransport transport { get; set; }

            public EventSubSubscriptionData(string type, string version, string userId, string sessionId)
            {
                this.type = type;
                this.version = version;
                condition = new(userId);
                transport = new(sessionId);
            }
        }

        public class EventSubSubscriptionResponseData
        {
            public string id { get; set; }
            public string status { get; set; }
            public string type { get; set; }
            public string version { get; set; }
            public EventSubSubscriptionCondition condition { get; set; }
            public DateTime created_at { get; set; }
            public EventSubSubscriptionTransport transport { get; set; }
            public int cost { get; set; }
        }

        public class CreateEventSubSubscriptionResponse: Response
        {
            public List<EventSubSubscriptionResponseData> data { get; set; }
            public int total { get; set; }
            public int total_cost { get; set; }
            public int max_total_cost { get; set; }
        }

        public class GetEventSubSubscriptionsResponse: Response
        {
            public List<EventSubSubscriptionData> data { get; set; }
            public int total { get; set; }
            public int total_cost { get; set; }
            public int max_total_cost { get; set; }
            public PaginationData pagination { get; set; }
        }


        // Get data about specified user. If login field is empty, gets data about user
        // based on provided Token.
        public static GetUserResponse GetUser(Token token, string login = "")
        {
            Dictionary<string, string> uriQuery = null;
            if (login.Length > 0)
            {
                uriQuery = new Dictionary<string, string>();
                uriQuery.Add("login", login);
            }

            return Request.Get<GetUserResponse>(GET_USERS_API_URI, token, uriQuery);
        }

        public static GetChannelInformationResponse GetChannelInformation(Token token, string id)
        {
            if (id.Length == 0)
                throw new System.ArgumentException("Broadcaster ID has to be provided");

            Dictionary<string, string> uriQuery = new Dictionary<string, string>();
            uriQuery.Add("broadcaster_id", id);

            return Request.Get<GetChannelInformationResponse>(GET_CHANNEL_INFORMATION_API_URI, token, uriQuery);
        }

        /**
         * @p type subscription type to create
         * @p version version of subscription to create
         * @p userId Twitch user ID aka. "to which channel subscribe to"
         * @p sessionId EventSub WebSocket session ID
         */
        public static CreateEventSubSubscriptionResponse CreateEventSubSubscription(Token token,
                                                                                    string type,
                                                                                    string version,
                                                                                    string userId,
                                                                                    string sessionId)
        {
            EventSubSubscriptionData subData = new EventSubSubscriptionData(type, version, userId, sessionId);
            JsonRequestContent content = new(subData);

            return Request.Post<CreateEventSubSubscriptionResponse>(EVENTSUB_SUBSCRIPTIONS_API_URI, token, null, content);
        }

        public static Response DeleteEventSubSubscription(Token token, string id)
        {
            if (id.Length == 0)
                throw new System.ArgumentException("Subscription ID has to be provided");

            Dictionary<string, string> query = new();
            query.Add("id", id);

            return Request.Delete<Response>(EVENTSUB_SUBSCRIPTIONS_API_URI, token, query);
        }

        public static GetEventSubSubscriptionsResponse GetEventSubSubscriptions(Token token,
                                                                                string status = null,
                                                                                string type = null,
                                                                                string userId = null,
                                                                                string after = null)
        {
            bool filterAdded = false;
            Dictionary<string, string> uriQuery = new();

            if (status != null)
            {
                uriQuery.Add("status", status);
                filterAdded = true;
            }

            if (type != null)
            {
                if (filterAdded)
                    throw new ArgumentException("GetEventSub API call filters are mutually exclusive. Add only one filter.");
                uriQuery.Add("type", type);
                filterAdded = true;
            }

            if (userId != null)
            {
                if (filterAdded)
                    throw new ArgumentException("GetEventSub API call filters are mutually exclusive. Add only one filter.");
                uriQuery.Add("user_id", userId);
            }

            // pagination is not a filter and can always be added if needed
            if (after != null)
                uriQuery.Add("after", after);

            return Request.Get<GetEventSubSubscriptionsResponse>(EVENTSUB_SUBSCRIPTIONS_API_URI, token, uriQuery);
        }
    }
}
