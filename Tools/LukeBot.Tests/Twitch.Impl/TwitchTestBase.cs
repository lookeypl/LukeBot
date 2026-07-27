using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Communication;
using LukeBot.Communication.Impl;
using LukeBot.Config;
using LukeBot.Services;
using LukeBot.Twitch.Command;
using LukeBot.Twitch;
using LukeBot.Twitch.Impl;
using LukeBot.User;
using LukeBot.User.Impl;
using System;


namespace LukeBot.Tests.Twitch.Impl
{
    public class TwitchTestBase
    {
        private static bool mInitialized = false;

        protected static readonly string TWITCH_TEST_USER = "testUserTwitch";

        protected static readonly string TWITCH_MOCK_USERID = "420691234";
        protected static readonly string TWITCH_MOCK_LOGIN = "verygoodstreamer";
        protected static readonly string TWITCH_MOCK_URI = "ws://127.0.0.1:8080/ws";

        protected static IEventService eventService = null;
        protected static IUserService userService = null;
        protected static IUserContext testUserContext = null;

        protected static API.Twitch.GetUserData mockUserData = new ()
        {
            broadcaster_type = "partner",
            description = "fake streamer lol",
            id = TWITCH_MOCK_USERID,
            login = TWITCH_MOCK_LOGIN,
            display_name = TWITCH_MOCK_LOGIN,
            type = "",
            view_count = 300,
            email = "streamer@streamers.paradise",
            created_at = DateTime.Now
        };
        internal static TwitchUserIdentity mockIdentity = new(mockUserData);


        // creates and fetches a faux User Context for tests
        // call this from a ClassInitialize-decorated static method
        protected static void InitializeTestClass()
        {
            if (mInitialized) return;

            // Needed to cause LukeBot.Twitch.Common assembly to load, which is
            // required by EventSystem. Warnings suppressed for that part.
            #pragma warning disable 0168
            TwitchChannelPointsRedemptionArgs args;
            #pragma warning restore 0168

            // EventSub needs config for twitch.api_endpoint reference
            // This is to access mock Twitch API set up by Twitch CLI
            // and test subscriptions
            Conf.Initialize(Constants.TEST_PROPS_DATA_FILE);

            eventService = EventService.Create();
            Service.Register(eventService);

            userService = UserService.Create();
            userService.CreateNewUser(TWITCH_TEST_USER); // creates EventService user-specific part
            testUserContext = userService.GetUser(TWITCH_TEST_USER);

            eventService.User(TWITCH_TEST_USER).AddEventDispatcher(
                global::LukeBot.Twitch.Utils.DispatcherNameForUser(testUserContext), EventDispatcherType.SubscriberQueued
            );

            mInitialized = true;
        }

        protected static void CleanupTestClass()
        {
            if (!mInitialized) return;

            Service.Unregister(eventService);
            mInitialized = false;
        }
    }
}
