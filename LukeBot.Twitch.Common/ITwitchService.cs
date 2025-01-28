using System.Collections.Generic;
using LukeBot.Services;

namespace LukeBot.Twitch.Common
{
    public interface ITwitchService: IService
    {
        public void AddCommandToChannel(string lbUser, string commandName, Command.ICommand command);
        public Command.ICommand AllocateCommand(string lbUser, Command.Descriptor d);
        public Command.ICommand AllocateCommand(string lbUser, string name, Command.Type type, string value);
        public void AwaitIRCLoggedIn(int timeoutMs);
        public void DeleteCommandFromChannel(string lbUser, string commandName);
        public void EditCommandFromChannel(string lbUser, string commandName, string newValue);
        public List<Command.Descriptor> GetCommandDescriptors(string lbUser);
        public Command.Descriptor GetCommandDescriptor(string lbUser, string name);
        public void AllowPrivilegeInCommand(string lbUser, string name, Command.ChatUser privilege);
        public void DenyPrivilegeInCommand(string lbUser, string name, Command.ChatUser privilege);
        public void SetCommandEnabled(string lbUser, string name, bool enabled);
        public void RefreshEmotesForUser(string lbUser);
        public void UpdateLoginForUser(string lbUser, string newLogin);
    }
}