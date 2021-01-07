using System;
using System.Collections.Generic;
using System.Text;

namespace LukeBot.Common.OAuth
{
    class AuthTokenResponse: PromiseData
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
        public List<string> scope { get; set; }
        public string token_type { get; set; }

        public override void Fill(PromiseData data)
        {
            AuthTokenResponse r = (AuthTokenResponse)data;

            access_token = r.access_token;
            refresh_token = r.refresh_token;
            expires_in = r.expires_in;
            scope = r.scope;
            token_type = r.token_type;
        }
    }
}
