using LukeBot.API;
using LukeBot.Common;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("LukeBot.Tests.Twitch.Impl")]

namespace LukeBot.Twitch.Impl
{
    internal class TwitchUserIdentity
    {
        public class IdentityEntry<T>
        {
            private T mData;
            private T mDefault = default(T);
            private bool mValid = false;

            public T Data
            {
                get
                {
                    if (mValid) return mData;
                    else return mDefault;
                }
                set
                {
                    mData = value;
                    mValid = true;
                }
            }

            public IdentityEntry() {}

            public IdentityEntry(T def)
            {
                mDefault = def;
            }
        }

        // this is what we can get from Twitch via API, so it should always be accessible
        public API.Twitch.GetUserData UserData { get; init; }

        // properties-shorthands for commonm UserData fields
        public string ID { get => UserData.id; }
        public string Username { get => UserData.login; }
        public string DisplayName { get => UserData.display_name; }

        // below entries are purely optional and will be filled eventually
        // (ex. when EventSub or IRC get this information and fill them)
        private IdentityEntry<List<BadgeSet>> mBadges = new(new List<BadgeSet>());
        private IdentityEntry<string> mColor = new("#aaaaaa");
        // ...

        // Property-accessors for optional entries
        public List<BadgeSet> Badges { get => mBadges.Data; set => mBadges.Data = value; }
        public string Color { get => mColor.Data; set => mColor.Data = value; }

        public TwitchUserIdentity(API.Twitch.GetUserData userData)
        {
            UserData = userData;
        }

        public Chatter ToChatter()
        {
            return new Chatter()
            {
                Username = Username,
                DisplayName = DisplayName,
                Color = Color,
                Badges = Badges.Select((b) => b.Name).ToList()
            };
        }
    };
}
