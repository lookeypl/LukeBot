

namespace LukeBot.Core
{
    public class Systems
    {
        static private CommunicationSystem mCommunicationSystem;
        static private ConnectionSystem mConnectionSystem;
        static private EventSystem mEventSystem;
        static private IntercomSystem mIntercomSystem;
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

        static public IntercomSystem Intercom
        {
            get
            {
                return mIntercomSystem;
            }
        }

        static public void Initialize()
        {
            if (mInitialized)
                return;

            mEventSystem = new EventSystem();
            mIntercomSystem = new IntercomSystem();

            mCommunicationSystem = new CommunicationSystem();
            mConnectionSystem = new ConnectionSystem(50000, 65535);
            mInitialized = true;
        }

        static public void Teardown()
        {
            mCommunicationSystem = null;
            mConnectionSystem = null;

            mIntercomSystem = null;
            mEventSystem = null;

            mInitialized = false;
        }
    }
}
