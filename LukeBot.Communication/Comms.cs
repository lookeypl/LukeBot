namespace LukeBot.Communication
{
    public class Comms
    {
        static private CommunicationSystem mCommunicationSystem;
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
            mInitialized = true;
        }

        static public void Teardown()
        {
            mCommunicationSystem = null;

            mIntercomSystem = null;
            mEventSystem = null;

            mInitialized = false;
        }
    }
}
