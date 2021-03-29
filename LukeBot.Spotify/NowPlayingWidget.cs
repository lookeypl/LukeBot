using System.IO;
using LukeBot.Common;
using LukeBot.Common.OAuth;


namespace LukeBot.Spotify
{
    class NowPlayingWidget: IWidget
    {
        Token mToken;

        public NowPlayingWidget(Token token)
        {
            mToken = token;
        }

        ~NowPlayingWidget()
        {
        }

        public override string GetPage()
        {
            StreamReader reader = File.OpenText("LukeBot.Spotify/Widgets/NowPlaying.html");
            string p = reader.ReadToEnd();
            reader.Close();
            return p;
        }
    }
}