using LukeBot.Logging;
using Command = LukeBot.Twitch.Common.Command;


namespace LukeBot.Twitch.Command
{
    public class Counter: ICommand
    {
        private int mCounter = 0;

        private enum Action
        {
            Error = 0,
            Increment,
            Set
        }

        public Counter(Command::Descriptor d)
            : base(d)
        {
            if (d.Value.Length > 0)
                mCounter = int.Parse(d.Value);
        }

        public override string Execute(Command::User callerPrivilege, string[] args)
        {
            // syntax of this command:
            //  +<integer> - increment by <integer>
            //  -<integer> - decrement by <integer>
            //  <integer> - set to <integer>
            //  anything else - error
            //  nothing - return the counter

            Action act = Action.Error;

            if (args.Length < 2)
                return mCounter.ToString();

            if (args[1].StartsWith('+') || args[1].StartsWith('-'))
                act = Action.Increment;
            else
                act = Action.Set;

            // privilege check - assume past that point only broadcaster and mods can
            // change/edit the counter.
            // TODO this should be configurable
            Command::User allowedPrivilege = Command::User.Broadcaster | Command::User.Moderator;
            if ((allowedPrivilege & callerPrivilege) == 0)
            {
                return ""; // no response
            }

            int change;
            if (!int.TryParse(args[1], out change))
            {
                Logger.Log().Error("Counter change failed - failed to parse {0} to Integer", args[1]);
                return "Counter change failed.";
            }

            switch (act)
            {
            case Action.Increment:
                mCounter += change;
                break;
            case Action.Set:
                mCounter = change;
                break;
            default:
                Logger.Log().Error("Counter change error - invalid Action {0}", act);
                return "Counter change failed";
            }

            UpdateConfig();
            return string.Format("Counter set to {0}", mCounter);
        }

        public override void Edit(string newValue)
        {
            // noop
        }

        public override Command::Descriptor ToDescriptor()
        {
            Logger.Log().Debug("Saving counter {0}", mCounter.ToString());
            return new Command::Descriptor(mName, Command::Type.counter, mPrivilegeLevel, mCounter.ToString());
        }
    }
}