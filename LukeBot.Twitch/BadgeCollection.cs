using System;
using System.Collections.Generic;
using LukeBot.Logging;
using LukeBot.Twitch.Common;

using TwitchAPI = LukeBot.API.Twitch;


namespace LukeBot.Twitch
{
    public class BadgeVersion
    {
        public string ID { get; private set; }
        public string Resource { get; private set; }

        public BadgeVersion(TwitchAPI.GetBadgesResponseBadgeVersion version)
        {
            ID = version.id;
            Uri resUri = new(version.image_url_4x);
            Resource = resUri.Segments[^2].TrimEnd('/');
        }
    }

    public class BadgeSet
    {
        public string Name { get; private set; }
        public Dictionary<string, BadgeVersion> Versions { get; private set; }

        public BadgeSet(TwitchAPI.GetBadgesResponseSet badgeSet)
        {
            Name = badgeSet.set_id;
            Versions = new();

            foreach (TwitchAPI.GetBadgesResponseBadgeVersion version in badgeSet.versions)
            {
                Versions.Add(version.id, new BadgeVersion(version));
            }
        }
    }

    public class BadgeCollection
    {
        public Dictionary<string, BadgeSet> Sets { get; private set; }

        public BadgeCollection(TwitchAPI.GetBadgesResponse badges)
        {
            Sets = new();
            foreach (TwitchAPI.GetBadgesResponseSet set in badges.data)
            {
                Sets.Add(set.set_id, new BadgeSet(set));
            }
        }

        private BadgeVersion GetBadge(string name, string version)
        {
            return Sets[name].Versions[version];
        }

        public List<MessageBadge> GetBadges(string badgeListIRC)
        {
            List<MessageBadge> badges = new();

            string[] badgeTokens = badgeListIRC.Split(',');

            foreach (string token in badgeTokens)
            {
                string[] splitToken = token.Split('/');
                if (splitToken.Length != 2)
                {
                    Logger.Log().Warning("Invalid badge token found, skipping - {0}", token);
                    continue;
                }

                if (Sets.ContainsKey(splitToken[0]))
                {
                    string badgeName = splitToken[0];
                    string badgeVersion = splitToken[1];

                    BadgeVersion badge = GetBadge(badgeName, badgeVersion);
                    badges.Add(new MessageBadge(badgeName, badge.Resource));
                }
            }

            return badges;
        }
    }
}