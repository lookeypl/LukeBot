using System.IO;
using Newtonsoft.Json;
using LukeBot.Common;
using LukeBot.Core;
using LukeBot.Core.Events;


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
            Systems.Event.TwitchChannelPointsRedemption += OnChannelPointsEvent;
        }

        ~Alerts()
        {
        }
    }
}
