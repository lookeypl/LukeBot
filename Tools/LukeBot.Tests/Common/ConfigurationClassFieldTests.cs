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
    public class ConfigurationClassFieldTests
    {
        public class InternalObject
        {
            [ConfigurationField]
            [JsonInclude]
            public int intField = 30;
            [ConfigurationField]
            [JsonInclude]
            public string stringField = "This is an internal string test";

            #pragma warning disable CS0414
            int shouldNotBeRegistered = 15;
            #pragma warning restore CS0414
        }

        public class ObjectWithAnObject
        {
            [ConfigurationField]
            [JsonInclude]
            public InternalObject internalObject = new();

            InternalObject shouldNotBeRegistered = new();
        }

        private class EventTestConfiguration
        {
            [JsonInclude]
            public InternalObject internalObject = new();
            [JsonInclude]
            public ObjectWithAnObject objectWithAnObject = new();

            // metadata
            [JsonInclude]
            public string EventName = nameof(TestConfiguration);
            [JsonInclude]
            public string FullConfigurableTypeName = typeof(TestConfiguration).FullName;
        }

        private static readonly EventTestConfiguration EVENT_TEST_CONFIGURATION = new();


        private class TestConfiguration : Configuration<TestConfiguration>
        {
            [ConfigurationField]
            public InternalObject internalObject = new();

            [ConfigurationField]
            public ObjectWithAnObject objectWithAnObject = new();

            public void CheckFields()
            {
                // check if fields exist
                Assert.IsNotNull(Field(nameof(internalObject)));
                Assert.IsNotNull(Field(nameof(objectWithAnObject)));

                Assert.IsNotNull(Field("internalObject.intField"));
                Assert.IsNotNull(Field("internalObject.stringField"));

                Assert.IsNotNull(Field("objectWithAnObject.internalObject"));
                Assert.IsNotNull(Field("objectWithAnObject.internalObject.intField"));
                Assert.IsNotNull(Field("objectWithAnObject.internalObject.stringField"));

                // check if accessors exist
                Assert.IsNotNull(Accessor<InternalObject>(nameof(internalObject)));
                Assert.IsNotNull(Accessor<ObjectWithAnObject>(nameof(objectWithAnObject)));

                Assert.IsNotNull(Accessor<int>("internalObject.intField"));
                Assert.IsNotNull(Accessor<string>("internalObject.stringField"));

                Assert.IsNotNull(Accessor<InternalObject>("objectWithAnObject.internalObject"));
                Assert.IsNotNull(Accessor<int>("objectWithAnObject.internalObject.intField"));
                Assert.IsNotNull(Accessor<string>("objectWithAnObject.internalObject.stringField"));

                // check field types
                Assert.AreEqual(ConfigurationFieldType.Class, Field(nameof(internalObject)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.Class, Field(nameof(objectWithAnObject)).FieldType);

                Assert.AreEqual(ConfigurationFieldType.Simple, Field("internalObject.intField").FieldType);
                Assert.AreEqual(ConfigurationFieldType.String, Field("internalObject.stringField").FieldType);

                Assert.AreEqual(ConfigurationFieldType.Class, Field("objectWithAnObject.internalObject").FieldType);
                Assert.AreEqual(ConfigurationFieldType.Simple, Field("objectWithAnObject.internalObject.intField").FieldType);
                Assert.AreEqual(ConfigurationFieldType.String, Field("objectWithAnObject.internalObject.stringField").FieldType);

                InternalObject expected = new();
                Assert.AreEqual(expected.intField, Get<int>("internalObject.intField"));
                Assert.AreEqual(expected.intField, Get<int>("objectWithAnObject.internalObject.intField"));

                Assert.AreEqual(expected.stringField, Get<string>("internalObject.stringField"));
                Assert.AreEqual(expected.stringField, Get<string>("objectWithAnObject.internalObject.stringField"));
            }

            public void TryRegisterExisting()
            {
                // this should throw as boolField was already registered by attribute
                RegisterField(nameof(internalObject), () => internalObject);
            }
        }


        [TestMethod]
        public void Configuration_ClassField_Register()
        {
            TestConfiguration conf = new();
            conf.CheckFields();
        }

        [TestMethod]
        public void Configuration_ClassField_RegisterExisting()
        {
            TestConfiguration conf = new();
            conf.CheckFields();

            Assert.ThrowsException<ConfigurationException>(() => conf.TryRegisterExisting());
        }

        [TestMethod]
        public void Configuration_GetFields()
        {
            TestConfiguration mainConf = new TestConfiguration();
            Configuration<TestConfiguration> conf = mainConf;

            Dictionary<string, ConfigurationField> fields = conf.GetFields();

            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.internalObject)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.objectWithAnObject)));

            Assert.IsTrue(fields.ContainsKey("internalObject.intField"));
            Assert.IsTrue(fields.ContainsKey("internalObject.stringField"));
            Assert.IsTrue(fields.ContainsKey("objectWithAnObject.internalObject"));
            Assert.IsTrue(fields.ContainsKey("objectWithAnObject.internalObject.intField"));
            Assert.IsTrue(fields.ContainsKey("objectWithAnObject.internalObject.stringField"));
        }

        [TestMethod]
        public void Configuration_ClassField_UpdateViaMember()
        {
            const int newInt = 80;
            const int otherNewInt = 99;
            const string newString = "I am different now";
            const string otherNewString = "I am also different";

            TestConfiguration conf = new();
            conf.CheckFields();

            conf.internalObject.intField = newInt;
            conf.internalObject.stringField = newString;
            conf.objectWithAnObject.internalObject.intField = otherNewInt;
            conf.objectWithAnObject.internalObject.stringField = otherNewString;

            Assert.AreEqual(newInt, conf.Get<int>("internalObject.intField"));
            Assert.AreEqual(newString, conf.Get<string>("internalObject.stringField"));
            Assert.AreEqual(otherNewInt, conf.Get<int>("objectWithAnObject.internalObject.intField"));
            Assert.AreEqual(otherNewString, conf.Get<string>("objectWithAnObject.internalObject.stringField"));
        }

        [TestMethod]
        public void Configuration_ClassField_UpdateViaSet()
        {
            const int newInt = 80;
            const int otherNewInt = 99;
            const string newString = "I am different now";
            const string otherNewString = "I am also different";

            TestConfiguration conf = new();
            conf.CheckFields();

            conf.Set<int>("internalObject.intField", newInt);
            conf.Set<string>("internalObject.stringField", newString);
            conf.Set<int>("objectWithAnObject.internalObject.intField", otherNewInt);
            conf.Set<string>("objectWithAnObject.internalObject.stringField", otherNewString);

            Assert.AreEqual(newInt, conf.internalObject.intField);
            Assert.AreEqual(newString, conf.internalObject.stringField);
            Assert.AreEqual(otherNewInt, conf.objectWithAnObject.internalObject.intField);
            Assert.AreEqual(otherNewString, conf.objectWithAnObject.internalObject.stringField);
        }

        [TestMethod]
        public void Configuration_ClassField_Serialize()
        {
            TestConfiguration conf = new();
            conf.CheckFields();

            Assert.AreEqual(JsonSerializer.Serialize<EventTestConfiguration>(EVENT_TEST_CONFIGURATION), conf.Serialize());
        }

        [TestMethod]
        public void Configuration_ClassField_Deserialize()
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
