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
    public class ConfigurationEnumerableFieldTests
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
            public List<int> intListField = new List<int>{ 10, 23, 460, 21, 910 };
            [JsonInclude]
            public List<string> stringListField = new List<string>{ "test1", "test2", "test3", "test420" };
            [JsonInclude]
            public List<InternalObject> objectListField = new List<InternalObject>{ new InternalObject(20, "test1"), new InternalObject(40, "test2") };
            [JsonInclude]
            public List<int> enumerableField = new List<int>{ 20, 30, 40 };

            // metadata
            [JsonInclude]
            public string EventName = nameof(TestConfiguration);
            [JsonInclude]
            public string FullConfigurableTypeName = typeof(TestConfiguration).FullName;
        }

        private static readonly EventTestConfiguration EVENT_TEST_CONFIGURATION = new();

        private class TestConfiguration: Configuration<TestConfiguration>
        {
            [ConfigurationField]
            public List<int> intListField = new(EVENT_TEST_CONFIGURATION.intListField);
            [ConfigurationField]
            public List<string> stringListField = new(EVENT_TEST_CONFIGURATION.stringListField);
            [ConfigurationField]
            public List<InternalObject> objectListField = new(EVENT_TEST_CONFIGURATION.objectListField);
            [ConfigurationField]
            public IEnumerable<int> enumerableField = new List<int>(EVENT_TEST_CONFIGURATION.enumerableField);

            public void CheckFields()
            {
                // check if fields exist
                Assert.IsNotNull(Field(nameof(intListField)));
                Assert.IsNotNull(Field(nameof(stringListField)));
                Assert.IsNotNull(Field(nameof(objectListField)));
                Assert.IsNotNull(Field(nameof(enumerableField)));

                Assert.IsNotNull(Accessor<List<int>>(nameof(intListField)));
                Assert.IsNotNull(Accessor<List<string>>(nameof(stringListField)));
                Assert.IsNotNull(Accessor<List<InternalObject>>(nameof(objectListField)));
                Assert.IsNotNull(Accessor<IEnumerable<int>>(nameof(enumerableField)));

                Assert.AreEqual(ConfigurationFieldType.List, Field(nameof(intListField)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.List, Field(nameof(stringListField)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.List, Field(nameof(objectListField)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.Enumerable, Field(nameof(enumerableField)).FieldType);

                Assert.AreEqual(EVENT_TEST_CONFIGURATION.intListField.Count, intListField.Count);
                for (int i = 0; i < intListField.Count; ++i)
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.intListField[i], intListField[i]);
                Assert.AreEqual(EVENT_TEST_CONFIGURATION.stringListField.Count, stringListField.Count);
                for (int i = 0; i < stringListField.Count; ++i)
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.stringListField[i], stringListField[i]);
                Assert.AreEqual(EVENT_TEST_CONFIGURATION.objectListField.Count, objectListField.Count);
                for (int i = 0; i < objectListField.Count; ++i)
                {
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.objectListField[i].number, objectListField[i].number);
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.objectListField[i].str, objectListField[i].str);
                }
                int counter = 0;
                foreach (int i in enumerableField)
                {
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.enumerableField[counter], i);
                    counter++;
                }
                Assert.AreEqual(EVENT_TEST_CONFIGURATION.enumerableField.Count, counter);
            }

            public void TryRegisterExisting()
            {
                // this should throw as boolField was already registered by attribute
                RegisterField(nameof(intListField), () => intListField);
            }
        }

        [TestMethod]
        public void Configuration_EnumerableField_Register()
        {
            TestConfiguration conf = new();
            conf.CheckFields();
        }

        [TestMethod]
        public void Configuration_EnumerableField_RegisterExisting()
        {
            TestConfiguration conf = new();
            conf.CheckFields();

            Assert.ThrowsException<ConfigurationException>(() => conf.TryRegisterExisting());
        }

        [TestMethod]
        public void Configuration_EnumerableField_GetFields()
        {
            TestConfiguration mainConf = new TestConfiguration();
            Configuration<TestConfiguration> conf = mainConf;

            Dictionary<string, ConfigurationField> fields = conf.GetFields();

            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.intListField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.stringListField)));
        }

        [TestMethod]
        public void Configuration_EnumerableField_UpdateViaMember()
        {
            const int newInt = 30;
            const string newString = "I am different now";

            TestConfiguration conf = new();

            conf.intListField[1] = newInt;
            conf.stringListField[0] = newString;

            Assert.AreEqual(newInt, conf.Get<List<int>>("intListField")[1]);
            Assert.AreEqual(newString, conf.Get<List<string>>("stringListField")[0]);
        }

        [TestMethod]
        public void Configuration_EnumerableField_UpdateViaSet()
        {
            const int newInt = 3000;
            const string newString = "I am even more different now";

            TestConfiguration conf = new();

            conf.Get<List<int>>("intListField")[1] = newInt;
            conf.Get<List<string>>("stringListField")[0] = newString;

            Assert.AreEqual(newInt, conf.intListField[1]);
            Assert.AreEqual(newString, conf.stringListField[0]);
        }

        [TestMethod]
        public void Configuration_EnumerableField_Serialize()
        {
            TestConfiguration conf = new();
            conf.CheckFields();

            Assert.AreEqual(JsonSerializer.Serialize<EventTestConfiguration>(EVENT_TEST_CONFIGURATION), conf.Serialize());
        }

        [TestMethod]
        public void Configuration_EnumerableField_Deserialize()
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
