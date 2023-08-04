namespace LukeBot.Communication.Common
{
    public class GlobalTestArgs: GlobalEventArgsBase
    {
        public GlobalTestArgs()
            : base(GlobalEventType.GlobalTest, "GlobalTest")
        {}
    }

    public class UserTestArgs: UserEventArgsBase
    {
        public UserTestArgs()
            : base(UserEventType.UserTest, "UserTest")
        {}
    }
}
