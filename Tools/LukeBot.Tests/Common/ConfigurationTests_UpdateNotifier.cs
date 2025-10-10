using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Widget;
using LukeBot.Widget.Common;


namespace LukeBot.Tests.Common
{
    [TestClass]
    public class ConfigurationTests_UpdateNotifier
    {
        private class InnerConfiguration: Configuration<InnerConfiguration>
        {
            [ConfigurationField]
            public int otherIntField = 20;
        }

        private class TestConfiguration: Configuration<TestConfiguration>
        {
            [ConfigurationField]
            public int intField = 30;

            [ConfigurationField]
            public InnerConfiguration innerConf = new();

            [ConfigurationField]
            public List<InnerConfiguration> listOfInnerConfs = new();
        }


        [TestMethod]
        public void Configuration_UpdateNotifier_Simple()
        {
            bool updated = false;
            ConfigurationBase.OnUpdateDelegate updater = () =>
            {
                updated = true;
            };

            TestConfiguration conf = new();
            conf.UpdateNotifier = updater;

            conf.Set<int>("intField", 50);

            Assert.AreEqual(50, conf.intField);
            Assert.IsTrue(updated);
        }

        [TestMethod]
        public void Configuration_UpdateNotifier_Inner()
        {
            bool updated = false;
            ConfigurationBase.OnUpdateDelegate updater = () =>
            {
                updated = true;
            };

            // create config and attach the notifier to it
            // it should automatically propagate inside InnerConfiguration
            TestConfiguration conf = new();
            conf.UpdateNotifier = updater;

            conf.Set<int>("innerConf.otherIntField", 42);

            Assert.AreEqual(42, conf.innerConf.otherIntField);
            Assert.IsTrue(updated);
        }

        [TestMethod]
        public void Configuration_UpdateNotifier_InnerList()
        {
            const int COUNTER_TARGET = 5;
            int updatedCounter = 0;
            ConfigurationBase.OnUpdateDelegate updater = () =>
            {
                updatedCounter++;
            };

            // create config and fill the list
            TestConfiguration conf = new();
            for (int i = 0; i < COUNTER_TARGET; ++i)
            {
                conf.listOfInnerConfs.Add(new InnerConfiguration());
            }

            // attach the notifier to the config
            // it should automatically propagate inside all the list items
            conf.UpdateNotifier = updater;

            for (int i = 0; i < COUNTER_TARGET; ++i)
            {
                conf.Get<List<InnerConfiguration>>("listOfInnerConfs")[i].Set("otherIntField", 42 + i);
            }

            for (int i = 0; i < COUNTER_TARGET; ++i)
            {
                Assert.AreEqual(42 + i, conf.listOfInnerConfs[i].otherIntField);
            }

            Assert.AreEqual(COUNTER_TARGET, updatedCounter);
        }
    }
}
