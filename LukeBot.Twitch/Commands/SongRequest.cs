using LukeBot.Communication;
using LukeBot.Communication.Common.Intercom;
using LukeBot.Spotify.Common;
using Command = LukeBot.Twitch.Common.Command;


namespace LukeBot.Twitch.Command
{
    public class SongRequest: ICommand
    {
        private string mLBUser;
        private string mHelpMessage;

        public SongRequest(Command::Descriptor d, string lbUser)
            : base(d)
        {
            mLBUser = lbUser;
            mHelpMessage = d.Value;
        }

        public override string Execute(Command::User callerPrivilege, string[] args)
        {
            if (args.Length < 2)
            {
                if (mHelpMessage != null && mHelpMessage.Length > 0)
                    return mHelpMessage;
                else
                    return "Provide Spotify URL to a track you want to add";
            }

            AddSongToQueueMsg msg = new AddSongToQueueMsg();
            msg.User = mLBUser;
            msg.URL = args[1];
            AddSongToQueueResponse resp = Comms.Intercom.Request<AddSongToQueueResponse, AddSongToQueueMsg>(msg);
            resp.Wait();

            if (resp.Status == MessageStatus.SUCCESS)
            {
                return string.Format("Added {0} - {1} successfully", resp.Artist, resp.Title);
            }
            else
            {
                return resp.ErrorReason;
            }
        }

        public override void Edit(string newValue)
        {
        }

        public override Command::Descriptor ToDescriptor()
        {
            return new Command::Descriptor(mName, Command::Type.songrequest, mPrivilegeLevel, mHelpMessage);
        }
    }
}