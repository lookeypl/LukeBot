


using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using LukeBot.Logging;

namespace LukeBot.API
{
    public class Streamlabs
    {
        // bases on textreader.pro's implementation of unofficial TTS API from Streamlabs
        // https://github.com/petercunha/tts/blob/master/lambda/index.js
        private const string STREAMLABS_BASE_URI = "https://streamlabs.com/";
        private const string STREAMLABS_POLLY_SPEAK_URI = STREAMLABS_BASE_URI + "polly/speak";

        public enum Voice
        {
            None = 0,
            Brian,
            Ivy,
            Justin
        };

        private struct TTSRequest
        {
            public string voice { get; set; }
            public string text { get; set; }

            public TTSRequest(Voice v, string t)
            {
                voice = v.ToString();
                text = t;
            }
        };

        public struct TTS
        {
            public bool success { get; set; }
            public string speak_url { get; set; }

            public TTS()
            {
                success = false;
                speak_url = "";
            }

            public TTS(bool s, string url)
            {
                success = s;
                speak_url = url;
            }
        };

        public static TTS GetTTS(Voice voice, string text)
        {
            if (voice == Voice.None || text.Length == 0)
                return new TTS();

            TTSRequest request = new TTSRequest(voice, text);

            HttpResponseMessage ret = Request.Post(STREAMLABS_POLLY_SPEAK_URI, null, null, new JsonRequestContent(request));

            if (!ret.IsSuccessStatusCode)
            {
                Logger.Log().Error("SL TTS request failed: {0} ({1})", ret.StatusCode, ret.ReasonPhrase);
                return new TTS();
            }

            TTS r = JsonSerializer.Deserialize<TTS>(ret.Content.ReadAsStream());
            if (!r.success)
            {
                Logger.Log().Error("SL TTS request invalid");
                return new TTS();
            }

            Logger.Log().Debug("SL TTS returned URL: {0}", r.speak_url);
            return r;
        }
    }
}