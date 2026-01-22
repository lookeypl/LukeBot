using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Services;

namespace LukeBot
{
    /**
     * Main access point to User Interface implementations in LukeBot.
     *
     * It is assumed that only one type of UI is active at LukeBot's run.
     */
    internal class UserInterface
    {
        private static InterfaceType mType = InterfaceType.none;
        private static CLIBase mInterface = null;
        private static readonly object mLock = new();

        public delegate bool AuthorizeUserDelegate(string user, byte[] passwordHash, out string reason);

        public static void OpenBrowserURLCallback(object o, EventArgsBase args)
        {
            // hooks up to AuthManager's OpenBrowserURL
            API.OpenBrowserURLArgs a = args as API.OpenBrowserURLArgs;
            CLI.OpenBrowserURL(a.LukeBotUser, a.URL);
        }

        /**
         * Returns a User Interface instance.
         *
         * Returned Interface will implement necessary bits like Ask/Query/Message/MainLoop calls.
         *
         * If there is a need to perform some CLI-specific or GUI-specific operations, it is
         * recommended to use CommandLine or Graphical Properties.
         */
        public static CLIBase CLI
        {
            get
            {
                lock (mLock)
                {
                    if (mInterface == null)
                        throw new InterfaceNotInitializedException();

                    return mInterface;
                }
            }
        }

        public InterfaceType Type
        {
            get
            {
                return mType;
            }
        }

        public static void Initialize(InterfaceType type)
        {
            mType = type;

            API.AuthManager i = API.AuthManager.Instance; // triggers constructor and initializes below event's endpoint
            Service.Get<IEventService>().Global().Event(API.Events.AUTHMGR_OPEN_BROWSER).Subscribe(OpenBrowserURLCallback);

            switch (mType)
            {
            case InterfaceType.basic:
                mInterface = new BasicCLI();
                break;
            case InterfaceType.server:
                mInterface = new ServerCLI();
                break;
            default:
                throw new UnrecognizedInterfaceTypeException(mType);
            }
        }

        public static void Teardown()
        {
            if (mInterface != null)
            {
                mInterface.Teardown();
                mInterface = null;
            }

            mType = InterfaceType.none;
        }
    }
}