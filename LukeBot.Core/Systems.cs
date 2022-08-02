namespace LukeBot.Core
{
    public class Systems
    {
        static private CommunicationSystem mCommunicationSystem;
        static private EventSystem mEventSystem;
        static private IntercomSystem mIntercomSystem;
        static private PropertyStore mPropertyStore;
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

        static public PropertyStore Properties
        {
            get
            {
                return mPropertyStore;
            }
        }

        static public void InitializeProperties(string storePath)
        {
            if (mPropertyStore != null)
                return;

            mPropertyStore = new PropertyStore(Common.Constants.PROPERTY_STORE_FILE);
            mPropertyStore.Load();
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

            mPropertyStore.Save();
            mPropertyStore = null;
        }
    }
}
