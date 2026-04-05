using System;
using System.Linq;
using System.Runtime.InteropServices.ObjectiveC;
using System.Text.Json;
using System.Text.Json.Serialization;
using LukeBot.Logging;


namespace LukeBot.API
{
    internal class AuthTokenJsonConverter: JsonConverter<AuthToken>
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(AuthToken).IsAssignableFrom(objectType);
        }

        public override void Write(Utf8JsonWriter writer, AuthToken value, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Json Writes are not supported by AuthToken deserializer");
        }

        public override AuthToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                AuthToken ret = new();

                // we expect these fields all so let's just assume they all are here
                // if some service disagrees we'll adjust the serialization behavior
                ret.access_token = doc.RootElement.GetProperty("access_token").GetString();
                ret.expires_in = doc.RootElement.GetProperty("expires_in").GetInt32();
                ret.token_type = doc.RootElement.GetProperty("token_type").GetString();

                // Spotify - doesn't provide refresh_token, instead we have to reuse the initial one
                if (doc.RootElement.TryGetProperty("refresh_token", out JsonElement refreshToken))
                {
                    ret.refresh_token = refreshToken.GetString();
                }
                else
                {
                    ret.refresh_token = null;
                }

                // Twitch - returns scope as an array
                // Spotify - returns scope as a space-separated string
                JsonElement scope = doc.RootElement.GetProperty("scope");
                if (scope.ValueKind == JsonValueKind.String)
                {
                    ret.scope = scope.GetString().Split(' ').ToList();
                }
                else if (scope.ValueKind == JsonValueKind.Array)
                {
                    JsonElement.ArrayEnumerator enumerator = scope.EnumerateArray();
                    ret.scope = new();
                    ret.scope.AddRange(enumerator.Select((elem) => elem.GetString()));
                }

                return ret;
            }
        }
    }
}
