using System.IO;
using Newtonsoft.Json;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Communication.Events;


namespace LukeBot.Widget
{
    public class Alerts: IWidget
    {
        public void OnChannelPointsEvent(object o, EventArgsBase args)
        {
            TwitchChannelPointsRedemptionArgs a = (TwitchChannelPointsRedemptionArgs)args;
            string msg = JsonConvert.SerializeObject(a);
            Logger.Log().Debug("{0}", msg);
            SendToWSAsync(msg);
        }

        public Alerts()
            : base("LukeBot.Widget/Widgets/Alerts.html")
        {
            Comms.Event.TwitchChannelPointsRedemption += OnChannelPointsEvent;
        }

        ~Alerts()
        {
        }
    }
}
