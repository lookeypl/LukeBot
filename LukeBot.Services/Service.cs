using LukeBot.Module;
using LukeBot.Spotify;
using LukeBot.Twitch;
using LukeBot.Widget;


namespace LukeBot.Services
{
    public class Service
    {
        static private UserModuleManager mModuleManager = null;
        static private SpotifyService mSpotifyService = null;
        static private TwitchService mTwitchService = null;
        static private WidgetService mWidgetService = null;
        static private bool mInitialized = false;

        static public UserModuleManager UserModuleManager
        {
            get
            {
                return mModuleManager;
            }
        }

        static public SpotifyService Spotify
        {
            get
            {
                return mSpotifyService;
            }
        }

        static public TwitchService Twitch
        {
            get
            {
                return mTwitchService;
            }
        }

        static public WidgetService Widget
        {
            get
            {
                return mWidgetService;
            }
        }

        static public void Initialize()
        {
            if (mInitialized)
                return;

            mModuleManager = new UserModuleManager();

            mSpotifyService = new SpotifyService();
            mTwitchService = new TwitchService();
            mWidgetService = new WidgetService();

            mModuleManager.RegisterUserModule(mSpotifyService.GetUserModuleDescriptor());
            mModuleManager.RegisterUserModule(mTwitchService.GetUserModuleDescriptor());
            mModuleManager.RegisterUserModule(mWidgetService.GetUserModuleDescriptor());

            mInitialized = true;
        }

        static public void Run()
        {
            mTwitchService.Run();
            mWidgetService.Run();

            // wait until modules are ready
            mTwitchService.AwaitIRCLoggedIn(60 * 1000);
        }

        static public void Stop()
        {
            if (mTwitchService != null) mTwitchService.RequestShutdown();
            if (mWidgetService != null) mWidgetService.RequestShutdown();

            if (mTwitchService != null) mTwitchService.WaitForShutdown();
            if (mWidgetService != null) mWidgetService.WaitForShutdown();
        }

        static public void Teardown()
        {
            mSpotifyService = null;
            mTwitchService = null;
            mWidgetService = null;

            mInitialized = false;
        }
    }
}