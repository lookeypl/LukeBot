using System;
using System.Linq;

namespace LukeBot.Twitch.Common.Command
{
    [Flags]
    public enum User
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
        public static string GetStringRepresentation(this User u)
        {
            string s = "";

            if ((u & User.Everyone) == User.Everyone)
                return "Everyone";

            User[] users = Enum.GetValues<User>();
            Array.Reverse<User>(users);
            foreach (User usr in users)
            {
                if (usr == User.Everyone)
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

        // PossibleValues skip User.Everyone since it's a special value
        private static User[] PossibleValues = Enum.GetValues<User>().Where(u => u != User.Everyone).ToArray();
        private static string[] PossibleValueStrings = PossibleValues.Select(u => u.ToString().ToLower()).ToArray();

        public static User ToUserEnum(this string s)
        {
            User result = 0;
            bool valueFound = true;
            string[] userList = s.ToLower().Split(',');

            if ("everyone".StartsWith(userList[0]))
                return User.Everyone;

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