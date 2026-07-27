using LukeBot.Services;
using LukeBot.User;

namespace LukeBot.Spotify
{
    public interface ISpotifyUserModule: IUserModule
    {
        public TrackData AddSongToQueue(string url);
        public void RenewAuthToken();
        public void UpdateLogin(string newLogin);
    }
}