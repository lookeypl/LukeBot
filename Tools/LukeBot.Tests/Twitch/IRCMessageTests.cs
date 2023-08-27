using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Common;
using System;
using System.Collections.Generic;


namespace LukeBot.Tests.Twitch
{
    // TODO these tests probably needs massive expansion in order to check if everything is parsed
    // properly and without issues. Especially check for:
    //  - Potential parsing issues in IRCMessage.Parse()
    //  - Potential malformed parameters when creating a message to send
    //  - Check each command/reply for expected specific message structure/parameters
    [TestClass]
    public class IRCMessageTests
    {
        [TestMethod]
        public void IRCMessage_Parse()
        {
            string msg = "@taga=test;tag_number_two=also_a_test;tag_without_val :nick!user@host.org PRIVMSG #channel random_param :this is a long message with spaces";

            IRCMessage m = IRCMessage.Parse(msg);

            string tagValue;
            Assert.IsTrue(m.GetTag("taga", out tagValue));
            Assert.AreEqual("test", tagValue);

            Assert.IsTrue(m.GetTag("tag_number_two", out tagValue));
            Assert.AreEqual("also_a_test", tagValue);

            Assert.IsTrue(m.GetTag("tag_without_val", out tagValue));
            Assert.AreEqual("", tagValue);

            // check if not_a_tag does not exist
            Assert.IsFalse(m.GetTag("not_a_tag", out tagValue));
            Assert.AreEqual(null, tagValue);


            Assert.AreEqual("nick", m.Nick);
            Assert.AreEqual("user", m.User);
            Assert.AreEqual("host.org", m.Host);
            Assert.AreEqual(IRCCommand.PRIVMSG, m.Command);
            Assert.AreEqual(IRCReply.INVALID, m.Reply);

            // Channel should exist as a separate field without the # attached to it
            Assert.AreEqual("channel", m.Channel);

            // Params should not contain the trailing one
            Assert.AreEqual(2, m.GetParams().Count, 2);
            Assert.AreEqual("#channel", m.GetParams()[0]);
            Assert.AreEqual("random_param", m.GetParams()[1]);
            Assert.AreEqual("this is a long message with spaces", m.GetTrailingParam());
        }

        [TestMethod]
        public void IRCMessage_VerifyCommandEnum()
        {
            // This test makes sure that all IRCCommand enums are covered

            // First, check from the perspective of creating a message to send
            foreach (IRCCommand c in Enum.GetValues(typeof(IRCCommand)))
            {
                // skip INVALID and REPLY enums, they are special internal ones
                if (c == IRCCommand.INVALID || c == IRCCommand.REPLY)
                    continue;

                IRCMessage m = new IRCMessage(c);
                Assert.AreEqual(m.Command, c);
            }

            // Then, check from the perspective of parsing a message
            foreach (IRCCommand c in Enum.GetValues(typeof(IRCCommand)))
            {
                // skip INVALID and REPLY enums, they are special internal ones
                if (c == IRCCommand.INVALID || c == IRCCommand.REPLY)
                    continue;

                IRCMessage m = IRCMessage.Parse(c.ToString());
                Assert.AreEqual(m.Command, c);
            }
        }

        [TestMethod]
        public void IRCMessage_VerifyReplyEnum()
        {
            // This test makes sure that all IRCCommand enums are covered

            // First, check from the perspective of creating a message to send
            foreach (IRCReply r in Enum.GetValues(typeof(IRCReply)))
            {
                // skip INVALID enum, it is a special internal one
                if (r == IRCReply.INVALID)
                    continue;

                IRCMessage m = new IRCMessage(r);
                Assert.AreEqual(m.Command, IRCCommand.REPLY);
                Assert.AreEqual(m.Reply, r);
            }

            // Then, check from the perspective of parsing a message
            foreach (IRCReply r in Enum.GetValues(typeof(IRCReply)))
            {
                // skip INVALID enum, it is a special internal one
                if (r == IRCReply.INVALID)
                    continue;

                IRCMessage m = IRCMessage.Parse(((int)r).ToString());
                Assert.AreEqual(m.Command, IRCCommand.REPLY);
                Assert.AreEqual(m.Reply, r);
            }
        }

        [TestMethod]
        public void IRCMessage_TagWithSpaceEscapes()
        {
            string msg = "@tag_one=value\\swith\\sspaces :nick!user@host.org PRIVMSG #channel random_param :this is a long message with spaces";

            IRCMessage m = IRCMessage.Parse(msg);

            // IRCMessage should replace \s for us, which is used as escape character for space char
            Assert.AreEqual(1, m.GetTagCount());
            Assert.AreEqual("value with spaces", m.GetTag("tag_one"));

            // space is used to separate tags from classic IRC message
            // getting an actual message with spaces as tags should fail with ParsingErrorException
            string msg2 = "@tag_one=value with bad spaces :nick!user@host.org PRIVMSG #channel random_param :this is a long message with spaces";
            Assert.ThrowsException<ParsingErrorException>(() => m = IRCMessage.Parse(msg2));
        }

        [TestMethod]
        public void IRCMessage_TagWithEqualSign()
        {
            string msg = "@tag_one=test\\sdata;tag_no_value;reply-msg=testing\\ssomething\\s=\\ssomething\\selse :nick!user@host.org PRIVMSG #channel random_param :this is a long message with spaces";

            // this should NOT throw
            IRCMessage m = IRCMessage.Parse(msg);

            Assert.AreEqual(3, m.GetTagCount());
            Assert.AreEqual("test data", m.GetTag("tag_one"));
            Assert.AreEqual("", m.GetTag("tag_no_value"));
            Assert.AreEqual("testing something = something else", m.GetTag("reply-msg"));
        }
    }
}