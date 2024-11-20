using System;
using System.Linq;

namespace LukeBot.Twitch.Common.Command
{
    [Flags]
    public enum ChatUser
    {
        Chatter = (1 << 0),
        Subscriber = (1 << 1),
        VIP = (1 << 2),
        Moderator = (1 << 3),
        Broadcaster = (1 << 4),

        Everyone = Chatter | Subscriber | VIP | Moderator | Broadcaster,
    }

    public static class UserExtensions
    {
        public static string GetStringRepresentation(this ChatUser u)
        {
            string s = "";

            if ((u & ChatUser.Everyone) == ChatUser.Everyone)
                return "Everyone";

            ChatUser[] users = Enum.GetValues<ChatUser>();
            Array.Reverse<ChatUser>(users);
            foreach (ChatUser usr in users)
            {
                if (usr == ChatUser.Everyone)
                    continue;

                if ((u & usr) == usr)
                {
                    s += usr.ToString();
                    s += ',';
                }
            }

            if (s.Length > 0)
                s = s.Substring(0, s.Length - 1);

            return s;
        }

        // PossibleValues skip ChatUser.Everyone since it's a special value
        private static ChatUser[] PossibleValues = Enum.GetValues<ChatUser>().Where(u => u != ChatUser.Everyone).ToArray();
        private static string[] PossibleValueStrings = PossibleValues.Select(u => u.ToString().ToLower()).ToArray();

        public static ChatUser ToUserEnum(this string s)
        {
            ChatUser result = 0;
            bool valueFound = true;
            string[] userList = s.ToLower().Split(',');

            if ("everyone".StartsWith(userList[0]))
                return ChatUser.Everyone;

            foreach (string user in userList)
            {
                valueFound = false;

                for (int i = 0; i < PossibleValues.Length; ++i)
                {
                    if (PossibleValueStrings[i].StartsWith(user))
                    {
                        valueFound = true;
                        result |= PossibleValues[i];
                        break;
                    }
                }

                if (!valueFound)
                    throw new ArgumentException("Provided invalid value: " + user);
            }

            return result;
        }
    }
}