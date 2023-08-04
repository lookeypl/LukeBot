using LukeBot.Twitch.Common;
using Command = LukeBot.Twitch.Common.Command;
using Intercom = LukeBot.Communication.Common.Intercom;


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
        public Command::Type Type { get; set; }
        public string Param { get; set; }

        public AddCommandIntercomMsg()
            : base(Endpoints.TWITCH_MAIN_MODULE, Messages.ADD_COMMAND)
        {
        }
    }

    public class EditCommandIntercomMsg: IntercomMessageBase
    {
        public string Name { get; set; }
        public string Param { get; set; }

        public EditCommandIntercomMsg()
            : base(Endpoints.TWITCH_MAIN_MODULE, Messages.EDIT_COMMAND)
        {
        }
    }

    public class DeleteCommandIntercomMsg: IntercomMessageBase
    {
        public string Name { get; set; }

        public DeleteCommandIntercomMsg()
            : base(Endpoints.TWITCH_MAIN_MODULE, Messages.DELETE_COMMAND)
        {
        }
    }
}
