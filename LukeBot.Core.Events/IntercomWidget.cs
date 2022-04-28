using System.Net.WebSockets;
using System.Threading.Tasks;


namespace LukeBot.Core.Events
{
    namespace Intercom
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

        public class GetWidgetPageMessage: MessageBase
        {
            public string widgetID { get; private set; }

            public GetWidgetPageMessage(string id)
                : base(Messages.GET_WIDGET_PAGE)
            {
                widgetID = id;
            }
        }

        public class GetWidgetPageResponse: ResponseBase
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

        public class AssignWSMessage: MessageBase
        {
            public string widgetID { get; private set; }
            public WebSocket ws;

            public AssignWSMessage(string widget, ref WebSocket ws)
                : base(Messages.ASSIGN_WS)
            {
                this.widgetID = widget;
                this.ws = ws;
            }

            public ref WebSocket GetWebSocket()
            {
                return ref ws;
            }
        }

        public class AssignWSResponse: ResponseBase
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
}