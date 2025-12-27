using LukeBot.Services;
using LukeBot.User;

namespace LukeBot.Spotify
{
    public interface ISpotifyUserModule: IUserModule
    {
        // TODO: AddSongToQueue should be added to this part and Intercom should be deprecated
        public void UpdateLogin(string newLogin);
    }
}