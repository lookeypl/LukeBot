using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using LukeBot.Twitch.Common.Command;


namespace LukeBot.Tests.Twitch
{
    [TestClass]
    public class UserExtensionsTests
    {
        [TestMethod]
        public void UserExtensions_GetStringRepresentation_Simple()
        {
            Assert.AreEqual("Everyone", User.Everyone.GetStringRepresentation());
            Assert.AreEqual("Chatter", User.Chatter.GetStringRepresentation());
            Assert.AreEqual("Subscriber", User.Subscriber.GetStringRepresentation());
            Assert.AreEqual("VIP", User.VIP.GetStringRepresentation());
            Assert.AreEqual("Moderator", User.Moderator.GetStringRepresentation());
            Assert.AreEqual("Broadcaster", User.Broadcaster.GetStringRepresentation());
        }

        [TestMethod]
        public void UserExtensions_GetStringRepresentation_Complex()
        {
            Assert.AreEqual("VIP,Chatter", (User.Chatter | User.VIP).GetStringRepresentation());
            Assert.AreEqual("Broadcaster,Chatter", (User.Chatter | User.Broadcaster).GetStringRepresentation());
            Assert.AreEqual("Moderator,VIP,Subscriber", (User.Moderator | User.VIP | User.Subscriber).GetStringRepresentation());
            Assert.AreEqual("Everyone", (User.Chatter | User.Subscriber | User.VIP | User.Moderator | User.Broadcaster).GetStringRepresentation());
        }

        [TestMethod]
        public void UserExtensions_ToUserEnum_Simple()
        {
            Assert.AreEqual(User.Everyone, "Everyone".ToUserEnum());
            Assert.AreEqual(User.Chatter, "Chatter".ToUserEnum());
            Assert.AreEqual(User.Subscriber, "Subscriber".ToUserEnum());
            Assert.AreEqual(User.VIP, "VIP".ToUserEnum());
            Assert.AreEqual(User.Moderator, "Moderator".ToUserEnum());
            Assert.AreEqual(User.Broadcaster, "Broadcaster".ToUserEnum());
        }

        [TestMethod]
        public void UserExtensions_ToUserEnum_Complex()
        {
            Assert.AreEqual(User.Chatter | User.VIP, "VIP,Chatter".ToUserEnum());
            Assert.AreEqual(User.Chatter | User.Broadcaster, "Broadcaster,Chatter".ToUserEnum());
            Assert.AreEqual(User.Moderator | User.VIP | User.Subscriber, "Moderator,VIP,Subscriber".ToUserEnum());
            Assert.AreEqual(User.Chatter | User.Subscriber | User.VIP | User.Moderator | User.Broadcaster, "Everyone".ToUserEnum());

            // order of values in string should not matter
            Assert.AreEqual(User.Chatter | User.VIP, "Chatter,VIP".ToUserEnum());
            Assert.AreEqual(User.Chatter | User.Broadcaster, "Chatter,Broadcaster".ToUserEnum());
            Assert.AreEqual(User.Moderator | User.VIP | User.Subscriber, "Subscriber,Moderator,VIP".ToUserEnum());
            Assert.AreEqual(User.Moderator | User.VIP | User.Subscriber, "Moderator,Subscriber,VIP".ToUserEnum());
            Assert.AreEqual(User.Moderator | User.VIP | User.Subscriber, "VIP,Moderator,Subscriber".ToUserEnum());
        }

        [TestMethod]
        public void UserExtensions_ToUserEnum_Shorts()
        {
            Assert.AreEqual(User.Moderator | User.VIP | User.Subscriber, "Mod,V,sub".ToUserEnum());
            Assert.AreEqual(User.Everyone, "b,m,v,s,c".ToUserEnum());
            Assert.AreEqual(User.Moderator | User.VIP | User.Chatter, "V,M,C".ToUserEnum());
            Assert.AreEqual(User.Broadcaster | User.Moderator | User.VIP | User.Subscriber, "b,mod,sub,vip".ToUserEnum());
            Assert.AreEqual(User.Everyone, "every".ToUserEnum());
        }

        [TestMethod]
        public void UserExtensions_ToUserEnum_Invalid()
        {
            Assert.ThrowsException<ArgumentException>(() => "Everynoe".ToUserEnum());
            Assert.ThrowsException<ArgumentException>(() => "Chtater".ToUserEnum());
            Assert.ThrowsException<ArgumentException>(() => "Subscirber".ToUserEnum());
            Assert.ThrowsException<ArgumentException>(() => "Vpi".ToUserEnum());
            Assert.ThrowsException<ArgumentException>(() => "????".ToUserEnum());

            Assert.ThrowsException<ArgumentException>(() => "VIP;Moderator".ToUserEnum());
        }
    }
}