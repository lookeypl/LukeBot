using CLIface = LukeBot.CLI;
using LukeBot.Twitch;
using LukeBot.Widget;


namespace LukeBot.Globals
{
    public class GlobalModules
    {
        static private CLIface.Interface mCLI = null;
        static private TwitchMainModule mTwitchModule = null;
        static private WidgetMainModule mWidgetModule = null;
        static private bool mInitialized = false;

        static public CLIface.Interface CLI
        {
            get
            {
                return mCLI;
            }
        }

        static public TwitchMainModule Twitch
        {
            get
            {
                return mTwitchModule;
            }
        }

        static public WidgetMainModule Widget
        {
            get
            {
                return mWidgetModule;
            }
        }

        static public void Initialize()
        {
            if (mInitialized)
                return;

            mCLI = new CLIface.Interface();

            mTwitchModule = new TwitchMainModule();
            mWidgetModule = new WidgetMainModule();

            mInitialized = true;
        }

        static public void Run()
        {
            mTwitchModule.Run();
            mWidgetModule.Run();

            // wait until modules are ready
            mTwitchModule.AwaitIRCLoggedIn(60 * 1000);
        }

        static public void Stop()
        {
            mTwitchModule.RequestShutdown();
            mWidgetModule.RequestShutdown();

            mTwitchModule.WaitForShutdown();
            mWidgetModule.WaitForShutdown();

            mCLI.Terminate();
        }

        static public void Teardown()
        {
            mTwitchModule = null;
            mCLI = null;

            mInitialized = false;
        }
    }
}