using LukeBot.API;
using LukeBot.Common;
using LukeBot.Twitch.Common;


namespace LukeBot.Twitch.Command
{
    public class Shoutout: ICommand
    {
        public Shoutout(string name)
            : base(name)
        {}

        public override string Execute(string[] args)
        {
            if (args.Length < 2)
            {
                return "Name to shoutout not provided! Pls provide one :(";
            }

            API.Twitch.GetUserData userData;
            API.Twitch.GetChannelInformationData channelData;

            try
            {
                Token t = AuthManager.Instance.GetToken(ServiceType.Twitch, "lukebot");
                API.Twitch.GetUserResponse userDataResponse = API.Twitch.GetUser(t, args[1]);
                if (userDataResponse.data == null || userDataResponse.data.Count == 0)
                {
                    throw new System.IndexOutOfRangeException("User data came back empty/invalid");
                }

                userData = userDataResponse.data[0];

                API.Twitch.GetChannelInformationResponse channelDataResponse = API.Twitch.GetChannelInformation(t, userDataResponse.data[0].id);
                if (channelDataResponse.data == null || channelDataResponse.data.Count == 0)
                {
                    throw new System.IndexOutOfRangeException("Channel data came back empty/invalid");
                }

                channelData = channelDataResponse.data[0];
            }
            catch (System.Exception e)
            {
                Logger.Log().Warning("Failed to execute Shoutout command: {0}", e.Message);
                return string.Format("Shoutout command failed: {0}", e.Message);
            }

            return string.Format("Make sure to check out {0} at https://twitch.tv/{1} ! They were last seen streaming {2}.",
                                 channelData.broadcaster_name, userData.login, channelData.game_name);
        }

        public override void Edit(string newValue)
        {
            // empty - no parameters that affect message contents
        }

        public override Descriptor ToDescriptor()
        {
            return new Descriptor(mName, TwitchCommandType.shoutout, "");
        }
    }
}