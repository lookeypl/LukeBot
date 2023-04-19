using System.Net.WebSockets;
using System.Threading.Tasks;
using Intercom = LukeBot.Communication.Events.Intercom;


namespace LukeBot.Widget.Common
{
    public class Endpoints
    {
        public const string WIDGET_MANAGER = "WidgetManager";
    };

    public class Messages
    {
        public const string GET_WIDGET_PAGE = "GetWidgetPageMessage";
        public const string ASSIGN_WS = "AssignWS";
    };

    public class GetWidgetPageMessage: Intercom::MessageBase
    {
        public string widgetID { get; private set; }

        public GetWidgetPageMessage(string id)
            : base(Endpoints.WIDGET_MANAGER, Messages.GET_WIDGET_PAGE)
        {
            widgetID = id;
        }
    }

    public class GetWidgetPageResponse: Intercom::ResponseBase
    {
        public string pageContents { get; private set; }

        public GetWidgetPageResponse()
            : base()
        {
        }

        public void SetContents(string page)
        {
            pageContents = page;
        }
    }

    public class AssignWSMessage: Intercom::MessageBase
    {
        public string widgetID { get; private set; }
        public WebSocket ws;

        public AssignWSMessage(string widget, WebSocket ws)
            : base(Endpoints.WIDGET_MANAGER, Messages.ASSIGN_WS)
        {
            this.widgetID = widget;
            this.ws = ws;
        }

        public WebSocket GetWebSocket()
        {
            return ws;
        }
    }

    public class AssignWSResponse: Intercom::ResponseBase
    {
        public Task lifetimeTask { get; private set; }

        public AssignWSResponse()
            : base()
        {
        }

        public void SetLifetimeTask(Task lifetimeTask)
        {
            this.lifetimeTask = lifetimeTask;
        }
    }
}