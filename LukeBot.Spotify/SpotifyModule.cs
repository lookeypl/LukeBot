using LukeBot.Common;
using LukeBot.Common.OAuth;

namespace LukeBot.Spotify
{
    public class SpotifyModule : IModule
    {
        void Login()
        {
        }

        public SpotifyModule()
        {
            CommunicationManager.Instance.Register(Constants.SERVICE_NAME);
        }

        ~SpotifyModule()
        {
        }

        public void Init()
        {
        }

        public void RequestShutdown()
        {
            throw new System.NotImplementedException();
        }

        public void Run()
        {
            throw new System.NotImplementedException();
        }

        public void Wait()
        {
            throw new System.NotImplementedException();
        }
    }
}
