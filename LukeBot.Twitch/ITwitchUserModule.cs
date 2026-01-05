using System;
using System.Collections.Generic;
using LukeBot.Services;
using LukeBot.User;
using LukeBot.Twitch.Command;

namespace LukeBot.Twitch
{
    public interface ITwitchUserModule: IUserModule, IDisposable
    {
        public void AddChatCommand(Descriptor d);
        public void AddChatCommand(string name, CommandType type, string value);
        public void DeleteChatCommand(string commandName);
        public void EditChatCommand(string commandName, string newValue);
        public List<Descriptor> GetChatCommandDescriptors();
        public Descriptor GetChatCommandDescriptor(string name);
        public void AllowChatCommandPrivilege(string name, ChatUser privilege);
        public void DenyChatCommandPrivilege(string name, ChatUser privilege);
        public void SetChatCommandEnabled(string name, bool enabled);
        public void RefreshEmotes();
        public void RestartEventSub();
        public void UpdateLogin(string newLogin);
    }
}