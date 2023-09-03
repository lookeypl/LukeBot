using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;


// TODO this probably should be moved to separate DLL like LukeBot.Network
namespace LukeBot.API
{
    public class Response
    {
        public HttpStatusCode code { get; set; }
        public HttpResponseMessage message { get; set; }
        public bool IsSuccess
        {
            get
            {
                return message != null && message.IsSuccessStatusCode;
            }
        }

        public Response()
        {
            code = HttpStatusCode.OK;
        }

        public Response(HttpResponseMessage msg)
        {
            Fill(msg);
        }

        public void Fill(HttpResponseMessage msg)
        {
            code = msg.StatusCode;
            message = msg;
        }
    }

    public class ResponseJObject: Response
    {
        public JObject obj { get; private set; }

        public ResponseJObject(HttpResponseMessage msg)
            : base(msg)
        {
            obj = null;
        }

        public ResponseJObject(HttpResponseMessage msg, string jsonStr)
            : base(msg)
        {
            obj = JObject.Parse(jsonStr);
        }

        public override string ToString()
        {
            string r = "code = " + code.ToString();
            if (code == HttpStatusCode.OK)
            {
                r += ", data = " + obj.ToString();
            }
            return r;
        }
    }

    public class ResponseJArray: Response
    {
        public JArray array { get; private set; }

        public ResponseJArray(HttpResponseMessage msg)
            : base(msg)
        {
            array = null;
        }

        public ResponseJArray(HttpResponseMessage msg, string jsonStr)
            : base(msg)
        {
            array = JArray.Parse(jsonStr);
        }

        public override string ToString()
        {
            string r = "code = " + code.ToString();
            if (code == HttpStatusCode.OK)
            {
                r += ", data = " + array.ToString();
            }
            return r;
        }
    }
}
