using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Widget;
using Org.BouncyCastle.Crypto.Digests;


namespace LukeBot.Tests.Common
{
    [TestClass]
    public class ConfigurationArrayFieldTests
    {
        private class InternalObject
        {
            [ConfigurationField]
            [JsonInclude]
            public int number = 504030;
            [ConfigurationField]
            [JsonInclude]
            public string str = "test internal string";

            public InternalObject(int number, string str)
            {
                this.number = number;
                this.str = str;
            }
        }

        private class EventTestConfiguration
        {
            [JsonInclude]
            public int[] intArrayField = { 10, 23, 460, 21, 910 };
            [JsonInclude]
            public string[] stringArrayField = { "test1", "test2", "test3", "test420" };
            [JsonInclude]
            public InternalObject[] objectArrayField = { new InternalObject(20, "test1"), new InternalObject(40, "test2") };

            // metadata
            [JsonInclude]
            public string EventName = nameof(TestConfiguration);
            [JsonInclude]
            public Guid EventID; // Guid is generated every new instance of EventArgsBase; this will be assigned before testing serialization
            [JsonInclude]
            public string FullConfigurableTypeName = typeof(TestConfiguration).FullName;
        }

        private static readonly EventTestConfiguration EVENT_TEST_CONFIGURATION = new();

        private class TestConfiguration: Configuration<TestConfiguration>
        {
            [ConfigurationField]
            public int[] intArrayField = new int[EVENT_TEST_CONFIGURATION.intArrayField.Length];
            [ConfigurationField]
            public string[] stringArrayField = new string[EVENT_TEST_CONFIGURATION.stringArrayField.Length];
            [ConfigurationField]
            public InternalObject[] objectArrayField = new InternalObject[EVENT_TEST_CONFIGURATION.objectArrayField.Length];

            public TestConfiguration()
            {
                Array.Copy(EVENT_TEST_CONFIGURATION.intArrayField, intArrayField, EVENT_TEST_CONFIGURATION.intArrayField.Length);
                Array.Copy(EVENT_TEST_CONFIGURATION.stringArrayField, stringArrayField, EVENT_TEST_CONFIGURATION.stringArrayField.Length);
                Array.Copy(EVENT_TEST_CONFIGURATION.objectArrayField, objectArrayField, EVENT_TEST_CONFIGURATION.objectArrayField.Length);
            }

            public void CheckFields()
            {
                // check if fields exist
                Assert.IsNotNull(Field(nameof(intArrayField)));
                Assert.IsNotNull(Field(nameof(stringArrayField)));
                Assert.IsNotNull(Field(nameof(objectArrayField)));

                Assert.IsNotNull(Accessor<int[]>(nameof(intArrayField)));
                Assert.IsNotNull(Accessor<string[]>(nameof(stringArrayField)));
                Assert.IsNotNull(Accessor<InternalObject[]>(nameof(objectArrayField)));

                Assert.AreEqual(ConfigurationFieldType.Array, Field(nameof(intArrayField)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.Array, Field(nameof(stringArrayField)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.Array, Field(nameof(objectArrayField)).FieldType);

                Assert.AreEqual(EVENT_TEST_CONFIGURATION.intArrayField.Length, intArrayField.Length);
                for (int i = 0; i < intArrayField.Length; ++i)
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.intArrayField[i], intArrayField[i]);
                Assert.AreEqual(EVENT_TEST_CONFIGURATION.stringArrayField.Length, stringArrayField.Length);
                for (int i = 0; i < stringArrayField.Length; ++i)
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.stringArrayField[i], stringArrayField[i]);

                Assert.AreEqual(EVENT_TEST_CONFIGURATION.objectArrayField.Length, objectArrayField.Length);
                for (int i = 0; i < objectArrayField.Length; ++i)
                {
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.objectArrayField[i].number, objectArrayField[i].number);
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.objectArrayField[i].str, objectArrayField[i].str);
                }
            }

            public void TryRegisterExisting()
            {
                // this should throw as boolField was already registered by attribute
                RegisterField(nameof(intArrayField), () => intArrayField);
            }
        }


        [TestMethod]
        public void Configuration_ArrayField_Register()
        {
            TestConfiguration conf = new();
            conf.CheckFields();
        }

        [TestMethod]
        public void Configuration_ArrayField_RegisterExisting()
        {
            TestConfiguration conf = new();
            conf.CheckFields();

            Assert.ThrowsException<ConfigurationException>(() => conf.TryRegisterExisting());
        }

        [TestMethod]
        public void Configuration_ArrayField_GetFields()
        {
            TestConfiguration mainConf = new TestConfiguration();
            Configuration<TestConfiguration> conf = mainConf;

            Dictionary<string, ConfigurationField> fields = conf.GetFields();

            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.intArrayField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.stringArrayField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.objectArrayField)));
        }

        [TestMethod]
        public void Configuration_ArrayField_UpdateViaMember()
        {
            const int newInt = 30;
            const string newString = "I am different now";

            TestConfiguration conf = new();

            conf.intArrayField[1] = newInt;
            conf.stringArrayField[0] = newString;

            Assert.AreEqual(newInt, conf.Get<int[]>("intArrayField")[1]);
            Assert.AreEqual(newString, conf.Get<string[]>("stringArrayField")[0]);
        }

        [TestMethod]
        public void Configuration_ArrayField_UpdateViaSet()
        {
            const int newInt = 3000;
            const string newString = "I am even more different now";

            TestConfiguration conf = new();

            conf.Get<int[]>("intArrayField")[1] = newInt;
            conf.Get<string[]>("stringArrayField")[0] = newString;

            Assert.AreEqual(newInt, conf.intArrayField[1]);
            Assert.AreEqual(newString, conf.stringArrayField[0]);
        }

        [TestMethod]
        public void Configuration_ArrayField_Serialize()
        {
            TestConfiguration conf = new();
            conf.CheckFields();

            // EventID is taken from just allocated conf. This is because allocation forms a new Guid.
            // We only test serialization here, so if EventID is incorrect it should be reflected somewhere else.
            EVENT_TEST_CONFIGURATION.EventID = conf.EventID;
            Assert.AreEqual(JsonSerializer.Serialize<EventTestConfiguration>(EVENT_TEST_CONFIGURATION), conf.Serialize());
        }

        [TestMethod]
        public void Configuration_ArrayField_Deserialize()
        {
            string serialized = JsonSerializer.Serialize<EventTestConfiguration>(EVENT_TEST_CONFIGURATION);

            TestConfiguration conf = ConfigurationFactory.Deserialize(serialized) as TestConfiguration;
            Assert.IsNotNull(conf);
            Assert.AreEqual(EVENT_TEST_CONFIGURATION.EventName, conf.EventName);
            Assert.AreEqual(EVENT_TEST_CONFIGURATION.FullConfigurableTypeName, conf.FullConfigurableTypeName);
            conf.CheckFields();
        }
    }
}
