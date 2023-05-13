using LukeBot.Interface;
using LukeBot.Module;
using LukeBot.Twitch;
using LukeBot.Widget;


namespace LukeBot.Globals
{
    public class GlobalModules
    {
        static private UserModuleManager mModuleManager = null;
        static private TwitchMainModule mTwitchMainModule = null;
        static private WidgetMainModule mWidgetMainModule = null;
        static private bool mInitialized = false;

        static public UserModuleManager UserModuleManager
        {
            get
            {
                return mModuleManager;
            }
        }

        static public TwitchMainModule Twitch
        {
            get
            {
                return mTwitchMainModule;
            }
        }

        static public WidgetMainModule Widget
        {
            get
            {
                return mWidgetMainModule;
            }
        }

        static public void Initialize()
        {
            if (mInitialized)
                return;

            mModuleManager = new UserModuleManager();

            mTwitchMainModule = new TwitchMainModule();
            mWidgetMainModule = new WidgetMainModule();

            mModuleManager.RegisterUserModule(mTwitchMainModule.GetUserModuleDescriptor());
            mModuleManager.RegisterUserModule(mWidgetMainModule.GetUserModuleDescriptor());

            mInitialized = true;
        }

        static public void Run()
        {
            mTwitchMainModule.Run();
            mWidgetMainModule.Run();

            // wait until modules are ready
            mTwitchMainModule.AwaitIRCLoggedIn(60 * 1000);
        }

        static public void Stop()
        {
            mTwitchMainModule.RequestShutdown();
            mWidgetMainModule.RequestShutdown();

            mTwitchMainModule.WaitForShutdown();
            mWidgetMainModule.WaitForShutdown();
        }

        static public void Teardown()
        {
            mTwitchMainModule = null;
            mWidgetMainModule = null;

            mInitialized = false;
        }
    }
}