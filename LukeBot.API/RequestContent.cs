using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;


namespace LukeBot.API
{
    internal enum RequestContentType
    {
        None = 0,
        UrlEncoded,
        Json
    }

    internal abstract class RequestContent
    {
        public RequestContentType Type { get; private set; }
        public abstract HttpContent ToHttpContent();

        protected RequestContent(RequestContentType type)
        {
            Type = type;
        }
    }

    internal class EmptyRequestContent: RequestContent
    {
        public EmptyRequestContent()
            : base(RequestContentType.None)
        {
        }

        public override HttpContent ToHttpContent()
        {
            return null;
        }
    }

    internal class URLEncodedRequestContent: RequestContent
    {
        private Dictionary<string, string> mContent = new();

        public URLEncodedRequestContent()
            : base(RequestContentType.UrlEncoded)
        {
        }

        public override HttpContent ToHttpContent()
        {
            if (mContent.Count == 0)
                return null;
            else
                return new FormUrlEncodedContent(mContent);
        }

        public void AddPair(string name, string value)
        {
            mContent.Add(name, value);
        }
    }

    internal class JsonRequestContent: RequestContent
    {
        public object mObject;

        public JsonRequestContent(object o)
            : base(RequestContentType.Json)
        {
            mObject = o;
        }

        public override HttpContent ToHttpContent()
        {
            return JsonContent.Create(mObject);
        }
    }
}