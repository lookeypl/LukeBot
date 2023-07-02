using LukeBot.Logging;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    public class Echo: IWidget
    {
        public void OnConnected()
        {
            string msg = "aassbbsss";

            Logger.Log().Info("Echoing: {0}", msg);
            SendToWSAsync(msg);
            string echo = RecvFromWS();
            if (msg == echo)
            {
                Logger.Log().Error("Echo successful");
            }
            else
            {
                Logger.Log().Error("Echo did not return the same message: {0} vs {1}", msg, echo);
            }
        }

        public Echo(string id, string name)
            : base("LukeBot.Widget/Widgets/Echo.html", id, name)
        {
        }

        public override WidgetType GetWidgetType()
        {
            return WidgetType.echo;
        }

        ~Echo()
        {
        }
    }
}