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

        public BadgeVersion(BadgeVersion other)
        {
            ID = other.ID;
            Resource = other.Resource;
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
            AddVersions(badgeSet);
        }

        public BadgeSet(BadgeSet other)
        {
            Name = other.Name;
            Versions = new();
            foreach (KeyValuePair<string, BadgeVersion> v in other.Versions)
            {
                Versions.Add(v.Key, new BadgeVersion(v.Value));
            }
        }

        public void AddVersions(TwitchAPI.GetBadgesResponseSet set)
        {
            foreach (TwitchAPI.GetBadgesResponseBadgeVersion version in set.versions)
            {
                if (Versions.ContainsKey(version.id))
                {
                    Logger.Log().Warning("Badge set {0} already contains version {1}", Name, version.id);
                    continue;
                }

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
            AddBadges(badges);
        }

        public BadgeCollection(BadgeCollection other)
        {
            Sets = new();
            foreach (KeyValuePair<string, BadgeSet> set in other.Sets)
            {
                Sets.Add(set.Key, new BadgeSet(set.Value));
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

        public void AddBadges(TwitchAPI.GetBadgesResponse badges)
        {
            foreach (TwitchAPI.GetBadgesResponseSet set in badges.data)
            {
                if (Sets.ContainsKey(set.set_id))
                    Sets[set.set_id].AddVersions(set);
                else
                    Sets.Add(set.set_id, new BadgeSet(set));
            }
        }
    }
}