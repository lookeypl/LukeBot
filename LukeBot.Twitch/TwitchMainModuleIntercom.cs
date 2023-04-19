using LukeBot.Communication;
using LukeBot.Twitch.Common;
using Intercom = LukeBot.Communication.Events.Intercom;


namespace LukeBot.Twitch
{
    public class IntercomMessageBase: Intercom::MessageBase
    {
        // LukeBot user to add the command for
        public string User { get; set; }

        public IntercomMessageBase(string endpoint, string msgType)
            : base(endpoint, msgType)
        {
        }
    }

    public class AddCommandIntercomMsg: IntercomMessageBase
    {
        public string Name { get; set; }
        public TwitchCommandType Type { get; set; }
        public string Param { get; set; }

        public AddCommandIntercomMsg()
            : base(TwitchIntercomMessages.TWITCH_INTERCOM_ENDPOINT, TwitchIntercomMessages.ADD_COMMAND_MSG)
        {
        }
    }

    public class EditCommandIntercomMsg: IntercomMessageBase
    {
        public string Name { get; set; }
        public string Param { get; set; }

        public EditCommandIntercomMsg()
            : base(TwitchIntercomMessages.TWITCH_INTERCOM_ENDPOINT, TwitchIntercomMessages.EDIT_COMMAND_MSG)
        {
        }
    }

    public class DeleteCommandIntercomMsg: IntercomMessageBase
    {
        public string Name { get; set; }

        public DeleteCommandIntercomMsg()
            : base(TwitchIntercomMessages.TWITCH_INTERCOM_ENDPOINT, TwitchIntercomMessages.DELETE_COMMAND_MSG)
        {
        }
    }
}
