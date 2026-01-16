namespace LukeBot.Communication.Impl
{
    public class Comms
    {
        static private EventSystem mEventSystem;
        static private bool mInitialized;

        static public EventSystem Event
        {
            get
            {
                return mEventSystem;
            }
        }

        static public bool Initialized
        {
            get
            {
                return mInitialized;
            }
        }

        static public void Initialize()
        {
            if (mInitialized)
                return;

            mEventSystem = new EventSystem();

            mInitialized = true;
        }

        static public void Teardown()
        {
            mEventSystem = null;

            mInitialized = false;
        }
    }
}
