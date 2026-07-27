
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Twitch.Command;
using LukeBot.Twitch.Impl;
using LukeBot.Twitch.Impl.Command;
using LukeBot.Common;

using System;
using System.Threading;
using System.Globalization;
using System.Linq;

namespace LukeBot.Tests.Twitch.Impl.Command
{
    [TestClass]
    public class Command_TimezoneTests: TwitchCommandTestBase
    {
        private static readonly string TIMEZONE_CMD_NAME = "!tz";
        private ICommand testCommand;

        [ClassInitialize]
        static public void Command_Timezone_Initialize(TestContext testContext)
        {
            InitializeTestClass();
        }

        [ClassCleanup]
        static public void Command_Timezone_Teardown()
        {
            CleanupTestClass();
        }

        [TestInitialize]
        public void Command_Timezone_InitializeTest()
        {
            // TODO: Test set and store, also test restore from Descriptor
            testCommand = AllocateCommand(TIMEZONE_CMD_NAME, CommandType.timezone);
        }

        [TestCleanup]
        public void Command_Timezone_CleanupTest()
        {
            testCommand = null;
        }

        [TestMethod]
        public void Command_Timezone_Help()
        {
            string response = ExecuteCommand(testCommand, ChatUser.Chatter, TIMEZONE_CMD_NAME + " help");
            Assert.IsFalse(String.IsNullOrEmpty(response));
            Assert.AreEqual(String.Format(Timezone.MSG_TIMEZONE_HELP_FORMAT, TIMEZONE_CMD_NAME), response);
        }

        private void CheckSuccessfulSetCommands(string timezone, bool expectDST)
        {
            string expected = String.Format(Timezone.MSG_TIMEZONE_SET_SUCCESS_FORMAT, timezone.ToUpper());
            if (expectDST) expected += Timezone.MSG_TIMEZONE_SET_SUCCESS_DST_ANNOTATION;
            TimeZoneShort tz = TimeZoneDB.GetFromAbbreviation(timezone.ToUpper());

            // set as moderator
            string response = ExecuteCommand(testCommand, ChatUser.Moderator, TIMEZONE_CMD_NAME + " set " + timezone);
            Assert.IsFalse(String.IsNullOrEmpty(response));
            Assert.AreEqual(expected, response);

            // check
            response = ExecuteCommand(testCommand, ChatUser.Chatter, TIMEZONE_CMD_NAME);
            Assert.AreEqual(String.Format(Timezone.MSG_TIMEZONE_SET_FORMAT, tz.ToString(TimeOnly.FromDateTime(DateTime.Now))), response);

            // set as broadcaster
            response = ExecuteCommand(testCommand, ChatUser.Broadcaster, TIMEZONE_CMD_NAME + " set " + timezone);
            Assert.IsFalse(String.IsNullOrEmpty(response));
            Assert.AreEqual(expected, response);

            // check
            response = ExecuteCommand(testCommand, ChatUser.Chatter, TIMEZONE_CMD_NAME);
            Assert.AreEqual(String.Format(Timezone.MSG_TIMEZONE_SET_FORMAT, tz.ToString(TimeOnly.FromDateTime(DateTime.Now))), response);
        }

        [TestMethod]
        public void Command_Timezone_Set()
        {
            // timezone "set" command can only be called by the broadcaster and mods
            string response = ExecuteCommand(testCommand, ChatUser.Chatter, TIMEZONE_CMD_NAME + " set cet");
            Assert.IsTrue(String.IsNullOrEmpty(response));
            Assert.AreEqual(String.Format(Timezone.MSG_TIMEZONE_SET_NOT_SET_FORMAT, TIMEZONE_CMD_NAME), ExecuteCommand(testCommand, ChatUser.Chatter, TIMEZONE_CMD_NAME));

            response = ExecuteCommand(testCommand, ChatUser.Subscriber, TIMEZONE_CMD_NAME + " set cet");
            Assert.IsTrue(String.IsNullOrEmpty(response));
            Assert.AreEqual(String.Format(Timezone.MSG_TIMEZONE_SET_NOT_SET_FORMAT, TIMEZONE_CMD_NAME), ExecuteCommand(testCommand, ChatUser.Chatter, TIMEZONE_CMD_NAME));

            response = ExecuteCommand(testCommand, ChatUser.VIP, TIMEZONE_CMD_NAME + " set cet");
            Assert.IsTrue(String.IsNullOrEmpty(response));
            Assert.AreEqual(String.Format(Timezone.MSG_TIMEZONE_SET_NOT_SET_FORMAT, TIMEZONE_CMD_NAME), ExecuteCommand(testCommand, ChatUser.Chatter, TIMEZONE_CMD_NAME));

            CheckSuccessfulSetCommands("eat", false);
            CheckSuccessfulSetCommands("cet", true);
            CheckSuccessfulSetCommands("pt", true);
        }

        [TestMethod]
        public void Command_Timezone_Convert_StreamerToDifferentTZ()
        {
            const string STREAMER_TIMEZONE = "EAT";
            const string TARGET_TIMEZONE = "BNT";
            TimeZoneShort STREAMER_TZ = TimeZoneDB.GetFromAbbreviation(STREAMER_TIMEZONE);
            TimeZoneShort TARGET_TZ = TimeZoneDB.GetFromAbbreviation(TARGET_TIMEZONE);
            TimeSpan DIFF = TARGET_TZ.UTC.Subtract(STREAMER_TZ.UTC);

            CheckSuccessfulSetCommands(STREAMER_TIMEZONE, false);

            TimeOnly now = TimeOnly.FromDateTime(DateTime.Now);
            if (now.Millisecond < 20 || now.Millisecond > 980)
            {
                // this is to ensure DateTime.Now has the same minute/second here and when Timezone Command fetches it
                Thread.Sleep(100);
                now = TimeOnly.FromDateTime(DateTime.Now);
            }

            string response = ExecuteCommand(testCommand, ChatUser.Chatter, TIMEZONE_CMD_NAME + " " + TARGET_TIMEZONE);
            TimeOnly target = now.Add(DIFF);

            string expected = String.Format(Timezone.MSG_TIMEZONE_CONVERT_FORMAT, STREAMER_TZ.ToString(now), TARGET_TZ.ToString(target));
            Assert.AreEqual(expected, response);
        }

        [TestMethod]
        public void Command_Timezone_Convert_CustomTime()
        {
            const string STREAMER_TIMEZONE = "EAT";
            const string TARGET_TIMEZONE = "BNT";
            TimeZoneShort STREAMER_TZ = TimeZoneDB.GetFromAbbreviation(STREAMER_TIMEZONE);
            TimeZoneShort TARGET_TZ = TimeZoneDB.GetFromAbbreviation(TARGET_TIMEZONE);
            TimeSpan DIFF = TARGET_TZ.UTC.Subtract(STREAMER_TZ.UTC);

            CheckSuccessfulSetCommands(STREAMER_TIMEZONE, false);

            TimeOnly source = new(12, 42, 00);
            string sourceString = source.ToString("t", CultureInfo.CreateSpecificCulture("en-NL"));
            TimeOnly target = source.Add(DIFF);

            string response = ExecuteCommand(testCommand, ChatUser.Chatter, TIMEZONE_CMD_NAME + " " + sourceString + " " + TARGET_TIMEZONE);

            string expected = String.Format(Timezone.MSG_TIMEZONE_CONVERT_FORMAT, STREAMER_TZ.ToString(source), TARGET_TZ.ToString(target));
            Assert.AreEqual(expected, response);
        }

        [TestMethod]
        public void Command_Timezone_Convert_CustomTimeAndZone()
        {
            const string STREAMER_TIMEZONE = "EAT";
            const string SOURCE_TIMEZONE = "DAVT";
            const string TARGET_TIMEZONE = "BNT";
            TimeZoneShort STREAMER_TZ = TimeZoneDB.GetFromAbbreviation(STREAMER_TIMEZONE);
            TimeZoneShort SOURCE_TZ = TimeZoneDB.GetFromAbbreviation(SOURCE_TIMEZONE);
            TimeZoneShort TARGET_TZ = TimeZoneDB.GetFromAbbreviation(TARGET_TIMEZONE);
            TimeSpan DIFF = TARGET_TZ.UTC.Subtract(SOURCE_TZ.UTC);

            CheckSuccessfulSetCommands(STREAMER_TIMEZONE, false);

            TimeOnly source = new(9, 38, 00);
            string sourceString = new string(
                source.ToString("t", CultureInfo.CreateSpecificCulture("en-US"))
                    .Where(c => !Char.IsWhiteSpace(c))
                    .ToArray()
            );
            TimeOnly target = source.Add(DIFF);

            string response = ExecuteCommand(testCommand, ChatUser.Chatter, TIMEZONE_CMD_NAME + " " + sourceString + " " + SOURCE_TIMEZONE + " " + TARGET_TIMEZONE);

            string expected = String.Format(Timezone.MSG_TIMEZONE_CONVERT_FORMAT, SOURCE_TZ.ToString(source), TARGET_TZ.ToString(target));
            Assert.AreEqual(expected, response);
        }
    }
}
