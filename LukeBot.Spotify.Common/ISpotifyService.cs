using LukeBot.Services;

namespace LukeBot.Spotify.Common
{
    public interface ISpotifyService: IService
    {
        public void UpdateLoginForUser(string lbUser, string newLogin);
    }
}