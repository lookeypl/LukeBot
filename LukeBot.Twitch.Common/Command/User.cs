using System;

namespace LukeBot.Twitch.Common.Command
{
    [Flags]
    public enum User
    {
        Chatter = (1 << 0),
        Follower = (1 << 1),
        Subscriber = (1 << 2),
        VIP = (1 << 3),
        Moderator = (1 << 4),
        Broadcaster = (1 << 5),

        Everyone = Chatter | Follower | Subscriber | VIP | Moderator | Broadcaster,
    }

    public static class UserExtensions
    {
        public static string GetStringRepresentation(this User u)
        {
            string s = "";

            if ((u & User.Everyone) == User.Everyone)
                return "Everyone";

            foreach (User usr in Enum.GetValues<User>())
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

        public static User ToUserEnum(this string s)
        {
            if (s == "Everyone")
                return User.Everyone;

            User result = 0;

            foreach (string user in s.Split(','))
            {
                foreach (User u in Enum.GetValues<User>())
                {
                    if (u.ToString() == user)
                    {
                        result |= u;
                        break;
                    }
                }
            }

            return result;
        }
    }
}