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
    }
}