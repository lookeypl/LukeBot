

namespace LukeBot.Core
{
    public class Systems
    {
        static private CommunicationSystem mCommunicationSystem;
        static private ConnectionSystem mConnectionSystem;
        static private EventSystem mEventSystem;
        static private WidgetSystem mWidgetSystem;
        static private bool mInitialized;

        static public CommunicationSystem Communication
        {
            get
            {
                return mCommunicationSystem;
            }
        }

        static public ConnectionSystem Connection
        {
            get
            {
                return mConnectionSystem;
            }
        }

        static public EventSystem Event
        {
            get
            {
                return mEventSystem;
            }
        }

        static public WidgetSystem Widget
        {
            get
            {
                return mWidgetSystem;
            }
        }

        static public void Initialize()
        {
            if (mInitialized)
                return;

            mEventSystem = new EventSystem();

            mCommunicationSystem = new CommunicationSystem();
            mConnectionSystem = new ConnectionSystem(50000, 65535);
            mWidgetSystem = new WidgetSystem();
            mInitialized = true;
        }

        static public void Teardown()
        {
            mCommunicationSystem = null;
            mConnectionSystem = null;
            mWidgetSystem = null;
            mInitialized = false;
        }
    }
}
