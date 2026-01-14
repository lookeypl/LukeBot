using System;
using LukeBot.Common;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.Spotify;
using LukeBot.Twitch.Command;
using LukeBot.User;


namespace LukeBot.Twitch.Impl.Command
{
    public class SongRequest: ICommand
    {
        private string mLBUser;
        private string mHelpMessage;

        private ISpotifyUserModule GetSpotifyUserModule()
        {
            IUserContext userContext = (Service.Get(Common.Constants.USER_SERVICE_NAME) as IUserService).GetUser(mLBUser);
            return (Service.Get(Common.Constants.SPOTIFY_SERVICE_NAME) as ISpotifyService).GetModule(userContext) as ISpotifyUserModule;
        }

        public SongRequest(Descriptor d, string lbUser)
            : base(d)
        {
            mLBUser = lbUser;
            mHelpMessage = d.Value;
        }

        public override string Execute(ChatUser callerPrivilege, string[] args)
        {
            if (args.Length < 2)
            {
                if (mHelpMessage != null && mHelpMessage.Length > 0)
                    return mHelpMessage;
                else
                    return "Provide Spotify URL to a track you want to add";
            }

            string url = args[1];
            TrackData addedSongData;

            try
            {
                addedSongData = GetSpotifyUserModule().AddSongToQueue(url);
            }
            catch (System.Exception e)
            {
                Logger.Log().Warning("Failed to add song URL {0} to Spotify queue for user {1}: {2}", url, mLBUser, e.Message);
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
                return String.Format("{0}", e.Message);
            }

            return String.Format("Added {0} - {1} to queue.", addedSongData.Artist, addedSongData.Title);
        }

        public override void Edit(string newValue)
        {
        }

        public override Descriptor ToDescriptor()
        {
            return new Descriptor(mName, CommandType.songrequest, mPrivilegeLevel, mEnabled, mHelpMessage);
        }
    }
}