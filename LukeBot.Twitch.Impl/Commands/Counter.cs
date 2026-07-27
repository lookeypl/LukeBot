using LukeBot.Logging;
using LukeBot.Twitch.Command;


namespace LukeBot.Twitch.Impl.Command
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

        public Counter(Descriptor d)
            : base(d)
        {
            if (d.Value.Length > 0)
                mCounter = int.Parse(d.Value);
        }

        public override string Execute(ChatUser callerPrivilege, string[] args)
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
            if (CheckPrivilege(callerPrivilege, ChatUser.Broadcaster | ChatUser.Moderator))
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

        public override Descriptor ToDescriptor()
        {
            Logger.Log().Debug("Saving counter {0}", mCounter.ToString());
            return new Descriptor(mName, CommandType.counter, mPrivilegeLevel, mEnabled, mCounter.ToString());
        }
    }
}