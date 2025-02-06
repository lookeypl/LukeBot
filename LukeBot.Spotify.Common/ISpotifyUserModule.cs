using LukeBot.Services;
using LukeBot.User.Common;

namespace LukeBot.Spotify.Common
{
    public interface ISpotifyUserModule: IUserModule
    {
        public void UpdateLogin(string newLogin);
    }
}