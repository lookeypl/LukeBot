using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Twitch.Command;
using LukeBot.Twitch.Impl.Command;
using LukeBot.Widget.Impl;

namespace LukeBot.Tests.Twitch.Impl.Command
{
    public class TwitchCommandTestBase: TwitchTestBase
    {
        protected ICommand AllocateCommand(string name, CommandType type)
        {
            Descriptor descriptor = new(name, CommandType.timezone, "");

            ICommand command = LukeBot.Twitch.Impl.Utils.AllocateChatCommand(testUserContext, descriptor);
            Assert.IsNotNull(command);
            return command;
        }

        protected string ExecuteCommand(ICommand cmd, ChatUser privilege, string command)
        {
            Assert.IsNotNull(cmd);
            return cmd.Execute(privilege, command.Split(' '));
        }
    }
}
