using LukeBot.Common;

namespace LukeBot.Auth
{
    class AuthToken: PromiseData
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        //public List<string> scope { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }

        public override void Fill(PromiseData data)
        {
            AuthToken r = (AuthToken)data;

            access_token = r.access_token;
            refresh_token = r.refresh_token;
            //scope = r.scope;
            expires_in = r.expires_in;
            token_type = r.token_type;
        }
    }
}
