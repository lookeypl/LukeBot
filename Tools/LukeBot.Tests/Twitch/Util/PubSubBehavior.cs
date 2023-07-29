using LukeBot.Twitch;
using Newtonsoft.Json;
using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace LukeBot.Tests.Twitch.Util
{
    internal class PubSubBehavior: WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            PubSubCommand cmd = JsonConvert.DeserializeObject<PubSubCommand>(e.Data);

            Console.WriteLine("FakePubSub: Received cmd: {0}", cmd.type);

            switch (cmd.type)
            {
            case PubSubMsgType.LISTEN:
                PubSubResponse response = new PubSubResponse(PubSubMsgType.RESPONSE);
                response.nonce = cmd.nonce;
                response.error = "";
                Send(JsonConvert.SerializeObject(response));
                break;
            case PubSubMsgType.PING:
                PubSubMessage msg = new PubSubMessage(PubSubMsgType.PONG);
                Send(JsonConvert.SerializeObject(msg));
                break;
            }
        }
    }
}