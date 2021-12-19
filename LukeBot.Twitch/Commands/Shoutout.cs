using LukeBot.Common;
using LukeBot.Auth;


namespace LukeBot.Twitch.Command
{
    public class Shoutout: ICommand
    {
        public Shoutout() {}

        public string Execute(string[] args)
        {
            if (args.Length < 2)
            {
                return "Name to shoutout not provided! Pls provide one :(";
            }

            API.GetUserData userData;
            API.GetChannelInformationData channelData;

            try
            {
                Token t = AuthManager.Instance.GetToken(ServiceType.Twitch, "lukebot");
                API.GetUserResponse userDataResponse = API.GetUser(t, args[1]);
                if (userDataResponse.data == null || userDataResponse.data.Count == 0)
                {
                    throw new System.IndexOutOfRangeException("User data came back empty/invalid");
                }

                userData = userDataResponse.data[0];

                API.GetChannelInformationResponse channelDataResponse = API.GetChannelInformation(t, userDataResponse.data[0].id);
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

            return string.Format("Make sure to check out {0} at https://twitch.tv/{1}! They were last seen streaming {2}.",
                                 channelData.broadcaster_name, userData.login, channelData.game_name);
        }
    }
}