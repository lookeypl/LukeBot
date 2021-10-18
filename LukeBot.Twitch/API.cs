using System.Net;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Auth;


namespace LukeBot.Twitch
{

class API
{
    private const string API_URI = "https://api.twitch.tv/helix/";
    private const string GET_USERS_API_URI = API_URI + "users";

    public class GetUserResponse: Auth.Response
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


    // Get data about specified user. If login field is empty, gets data about user
    // based on provided Token.
    public GetUserResponse GetUser(Token token, string login = "")
    {
        Dictionary<string, string> query = null;
        if (login.Length > 0) {
            query = new Dictionary<string, string>();
            query.Add("login", login);
        }
        GetUserResponse response = Request.Get<GetUserResponse>(GET_USERS_API_URI, token, query);

        if (response.code != HttpStatusCode.OK)
        {
            Logger.Error("API call failed: received status code {0}", response.code);
            throw new APIErrorException("API call failed: received status code " + response.code.ToString());
        }

        return response;
    }
}

}
