using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Twitch.Command;


namespace LukeBot.Tests.Twitch.Command
{
    [TestClass]
    public class ChatUserExtensionsTests
    {
        [TestMethod]
        public void UserExtensions_GetStringRepresentation_Simple()
        {
            Assert.AreEqual("Everyone", ChatUser.Everyone.GetStringRepresentation());
            Assert.AreEqual("Chatter", ChatUser.Chatter.GetStringRepresentation());
            Assert.AreEqual("Subscriber", ChatUser.Subscriber.GetStringRepresentation());
            Assert.AreEqual("VIP", ChatUser.VIP.GetStringRepresentation());
            Assert.AreEqual("Moderator", ChatUser.Moderator.GetStringRepresentation());
            Assert.AreEqual("Broadcaster", ChatUser.Broadcaster.GetStringRepresentation());
        }

        [TestMethod]
        public void UserExtensions_GetStringRepresentation_Complex()
        {
            Assert.AreEqual("VIP,Chatter", (ChatUser.Chatter | ChatUser.VIP).GetStringRepresentation());
            Assert.AreEqual("Broadcaster,Chatter", (ChatUser.Chatter | ChatUser.Broadcaster).GetStringRepresentation());
            Assert.AreEqual("Moderator,VIP,Subscriber", (ChatUser.Moderator | ChatUser.VIP | ChatUser.Subscriber).GetStringRepresentation());
            Assert.AreEqual("Everyone", (ChatUser.Chatter | ChatUser.Subscriber | ChatUser.VIP | ChatUser.Moderator | ChatUser.Broadcaster).GetStringRepresentation());
        }

        [TestMethod]
        public void UserExtensions_ToUserEnum_Simple()
        {
            Assert.AreEqual(ChatUser.Everyone, "Everyone".ToUserEnum());
            Assert.AreEqual(ChatUser.Chatter, "Chatter".ToUserEnum());
            Assert.AreEqual(ChatUser.Subscriber, "Subscriber".ToUserEnum());
            Assert.AreEqual(ChatUser.VIP, "VIP".ToUserEnum());
            Assert.AreEqual(ChatUser.Moderator, "Moderator".ToUserEnum());
            Assert.AreEqual(ChatUser.Broadcaster, "Broadcaster".ToUserEnum());
        }

        [TestMethod]
        public void UserExtensions_ToUserEnum_Complex()
        {
            Assert.AreEqual(ChatUser.Chatter | ChatUser.VIP, "VIP,Chatter".ToUserEnum());
            Assert.AreEqual(ChatUser.Chatter | ChatUser.Broadcaster, "Broadcaster,Chatter".ToUserEnum());
            Assert.AreEqual(ChatUser.Moderator | ChatUser.VIP | ChatUser.Subscriber, "Moderator,VIP,Subscriber".ToUserEnum());
            Assert.AreEqual(ChatUser.Chatter | ChatUser.Subscriber | ChatUser.VIP | ChatUser.Moderator | ChatUser.Broadcaster, "Everyone".ToUserEnum());

            // order of values in string should not matter
            Assert.AreEqual(ChatUser.Chatter | ChatUser.VIP, "Chatter,VIP".ToUserEnum());
            Assert.AreEqual(ChatUser.Chatter | ChatUser.Broadcaster, "Chatter,Broadcaster".ToUserEnum());
            Assert.AreEqual(ChatUser.Moderator | ChatUser.VIP | ChatUser.Subscriber, "Subscriber,Moderator,VIP".ToUserEnum());
            Assert.AreEqual(ChatUser.Moderator | ChatUser.VIP | ChatUser.Subscriber, "Moderator,Subscriber,VIP".ToUserEnum());
            Assert.AreEqual(ChatUser.Moderator | ChatUser.VIP | ChatUser.Subscriber, "VIP,Moderator,Subscriber".ToUserEnum());
        }

        [TestMethod]
        public void UserExtensions_ToUserEnum_Shorts()
        {
            Assert.AreEqual(ChatUser.Moderator | ChatUser.VIP | ChatUser.Subscriber, "Mod,V,sub".ToUserEnum());
            Assert.AreEqual(ChatUser.Everyone, "b,m,v,s,c".ToUserEnum());
            Assert.AreEqual(ChatUser.Moderator | ChatUser.VIP | ChatUser.Chatter, "V,M,C".ToUserEnum());
            Assert.AreEqual(ChatUser.Broadcaster | ChatUser.Moderator | ChatUser.VIP | ChatUser.Subscriber, "b,mod,sub,vip".ToUserEnum());
            Assert.AreEqual(ChatUser.Everyone, "every".ToUserEnum());
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
