using LukeBot.Communication;
using LukeBot.Communication.Common.Intercom;
using LukeBot.Spotify;
using LukeBot.Twitch.Command;


namespace LukeBot.Twitch.Impl.Command
{
    public class SongRequest: ICommand
    {
        private string mLBUser;
        private string mHelpMessage;

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

        public override Descriptor ToDescriptor()
        {
            return new Descriptor(mName, CommandType.songrequest, mPrivilegeLevel, mEnabled, mHelpMessage);
        }
    }
}