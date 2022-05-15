using System.Collections.Generic;
using Intercom = LukeBot.Core.Events.Intercom;


namespace LukeBot.Core
{
    // basic idea - replacement for CommunicationSystem
    // A wants to request something from B, but they gotta be hidden behind some abstraction
    // Provide means to:
    //  * Register as someone who awaits communication from some other side
    //  * Drop a request from A to B
    //  * Wait for response from B to A
    public class IntercomSystem
    {
        public Dictionary<string, Intercom::EndpointInfo> mIntercomEndpoints;

        public IntercomSystem()
        {
            mIntercomEndpoints = new Dictionary<string, Intercom::EndpointInfo>();
        }

        public void Register(Intercom::EndpointInfo info)
        {
            mIntercomEndpoints.Add(info.mName, info);
        }

        public TResp Request<TResp, TMsg>(string endpoint, TMsg message)
            where TResp: Intercom::ResponseBase, new()
            where TMsg: Intercom::MessageBase
        {
            Intercom::EndpointInfo endpointInfo;
            if (!mIntercomEndpoints.TryGetValue(endpoint, out endpointInfo))
            {
                TResp r = new TResp();
                r.SignalError(string.Format("Intercom Endpoint {0} not found", endpoint));
                return r;
            }

            Intercom::Delegate endpointDelegate;
            if (!endpointInfo.mMethods.TryGetValue(message.Message, out endpointDelegate))
            {
                TResp r = new TResp();
                r.SignalError(string.Format("Intercom Endpoint {0}: Message {1} not found", endpoint, message.Message));
                return r;
            }

            Intercom::ResponseBase resp = endpointInfo.mResponseAllocator(message);
            endpointDelegate(message, ref resp);
            return resp as TResp;
        }
    };
}