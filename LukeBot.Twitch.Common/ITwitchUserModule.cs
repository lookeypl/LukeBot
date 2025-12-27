using System;
using System.Collections.Generic;
using LukeBot.Services;
using LukeBot.User;

namespace LukeBot.Twitch.Common
{
    public interface ITwitchUserModule: IUserModule, IDisposable
    {
        public void AddChatCommand(Command.Descriptor d);
        public void AddChatCommand(string name, Command.Type type, string value);
        public void DeleteChatCommand(string commandName);
        public void EditChatCommand(string commandName, string newValue);
        public List<Command.Descriptor> GetChatCommandDescriptors();
        public Command.Descriptor GetChatCommandDescriptor(string name);
        public void AllowChatCommandPrivilege(string name, Command.ChatUser privilege);
        public void DenyChatCommandPrivilege(string name, Command.ChatUser privilege);
        public void SetChatCommandEnabled(string name, bool enabled);
        public void RefreshEmotes();
        public void RestartEventSub();
        public void UpdateLogin(string newLogin);
    }
}