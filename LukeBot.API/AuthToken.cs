﻿using System;
using System.Text.Json;
using LukeBot.Common;

namespace LukeBot.API
{
    class AuthToken: PromiseData
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        //public string scope { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public long acquiredTimestamp { get; set; }

        public override void Fill(PromiseData data)
        {
            AuthToken r = data as AuthToken;

            access_token = r.access_token;
            refresh_token = r.refresh_token;
            //scope = r.scope;
            expires_in = r.expires_in;
            token_type = r.token_type;
        }

        static public AuthToken FromJson(string jsonString)
        {
            AuthToken token = JsonSerializer.Deserialize<AuthToken>(jsonString);
            token.acquiredTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return token;
        }
    }
}
