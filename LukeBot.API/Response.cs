using System.Net;
using Newtonsoft.Json.Linq;


// TODO this probably should be moved to separate DLL like LukeBot.Network
namespace LukeBot.API
{
    public class Response
    {
        public HttpStatusCode code { get; set; }

        public Response()
        {
            code = HttpStatusCode.OK;
        }

        public Response(HttpStatusCode c)
        {
            code = c;
        }
    }

    public class ResponseJObject: Response
    {
        public JObject obj { get; private set; }

        public ResponseJObject(HttpStatusCode c)
            : base(c)
        {
            obj = null;
        }

        public ResponseJObject(HttpStatusCode c, string jsonStr)
            : base(c)
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

        public ResponseJArray(HttpStatusCode c)
            : base(c)
        {
            array = null;
        }

        public ResponseJArray(HttpStatusCode c, string jsonStr)
            : base(c)
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
