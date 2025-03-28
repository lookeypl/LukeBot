using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;
using System.Collections.Generic;
using LukeBot.Communication.Common;
using LukeBot.Widget;
using LukeBot.Widget.Common;
using Newtonsoft.Json;


namespace LukeBot.Tests.Widget
{
    [TestClass]
    public class WidgetConfigurationTests
    {
        // NOTE: For test purposes this mirrors actual config class that would be used to serialize
        // and send to Widget's WebSocket. Changes here might require changes to TestConfiguration
        // and vice versa.
        private class EventTestConfiguration: EventArgsBase
        {
            public bool boolField = true;
            public int intField = 42;
            public string stringField = "test omg testing hello";

            public bool[] boolArrayField = { true, false, true };
            public int[] intArrayField = { 10, 23, 460, 21, 910 };
            public string[] stringArrayField = { "test1", "test2", "test3", "test420" };

            public List<bool> boolListField = new List<bool> {false, true, false};
            public List<int> intListField = new List<int> { 1, 18, 22, 678, 901 };
            public List<string> stringListField = new List<string> { "list50", "list12", "list1", "listtest420" };

            public bool boolRegisteredManually = false;

            public EventTestConfiguration()
                : base("TestConfiguration")
            {
            }
        }

        private static readonly EventTestConfiguration EVENT_TEST_CONFIGURATION = new();

        private class TestConfiguration: WidgetConfiguration
        {
            [WidgetConfigurationField]
            public bool boolField = EVENT_TEST_CONFIGURATION.boolField;
            [WidgetConfigurationField]
            public int intField = EVENT_TEST_CONFIGURATION.intField;
            [WidgetConfigurationField]
            public string stringField = EVENT_TEST_CONFIGURATION.stringField;
            [WidgetConfigurationField]
            public bool[] boolArrayField = new bool[EVENT_TEST_CONFIGURATION.boolArrayField.Length];
            [WidgetConfigurationField]
            public int[] intArrayField = new int[EVENT_TEST_CONFIGURATION.intArrayField.Length];
            [WidgetConfigurationField]
            public string[] stringArrayField = new string[EVENT_TEST_CONFIGURATION.stringArrayField.Length];
            [WidgetConfigurationField]
            public List<bool> boolListField = new(EVENT_TEST_CONFIGURATION.boolListField);
            [WidgetConfigurationField]
            public List<int> intListField = new(EVENT_TEST_CONFIGURATION.intListField);
            [WidgetConfigurationField]
            public List<string> stringListField = new(EVENT_TEST_CONFIGURATION.stringListField);

            // below field on purpose has no attribute, as we are registering it manually with RegisterField()
            public bool boolRegisteredManually = EVENT_TEST_CONFIGURATION.boolRegisteredManually;

            static TestConfiguration()
            {
                WidgetConfiguration.RegisterAllocator(nameof(TestConfiguration), () => new TestConfiguration());
            }

            public TestConfiguration()
                : base("TestConfiguration")
            {
                Array.Copy(EVENT_TEST_CONFIGURATION.boolArrayField, boolArrayField, EVENT_TEST_CONFIGURATION.boolArrayField.Length);
                Array.Copy(EVENT_TEST_CONFIGURATION.intArrayField, intArrayField, EVENT_TEST_CONFIGURATION.intArrayField.Length);
                Array.Copy(EVENT_TEST_CONFIGURATION.stringArrayField, stringArrayField, EVENT_TEST_CONFIGURATION.stringArrayField.Length);

                RegisterField(nameof(boolRegisteredManually), () => boolRegisteredManually);
            }

            public void CheckFields()
            {
                // check if fields exist
                Assert.IsNotNull(Get(nameof(boolField)));
                Assert.IsNotNull(Get(nameof(intField)));
                Assert.IsNotNull(Get(nameof(stringField)));
                Assert.IsNotNull(Get(nameof(boolArrayField)));
                Assert.IsNotNull(Get(nameof(intArrayField)));
                Assert.IsNotNull(Get(nameof(stringArrayField)));
                Assert.IsNotNull(Get(nameof(boolListField)));
                Assert.IsNotNull(Get(nameof(intListField)));
                Assert.IsNotNull(Get(nameof(stringListField)));
                Assert.IsNotNull(Get(nameof(boolRegisteredManually)));

                // check if some random field does not exist
                Assert.ThrowsException<WidgetConfigurationException>(() => Get("randomNamedFieldWhichShouldNotExist"));

                Assert.AreEqual(EVENT_TEST_CONFIGURATION.boolField, boolField);
                Assert.AreEqual(EVENT_TEST_CONFIGURATION.intField, intField);
                Assert.AreEqual(EVENT_TEST_CONFIGURATION.stringField, stringField);

                Assert.AreEqual(EVENT_TEST_CONFIGURATION.boolArrayField.Length, boolArrayField.Length);
                for (int i = 0; i < boolArrayField.Length; ++i)
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.boolArrayField[i], boolArrayField[i]);
                Assert.AreEqual(EVENT_TEST_CONFIGURATION.intArrayField.Length, intArrayField.Length);
                for (int i = 0; i < intArrayField.Length; ++i)
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.intArrayField[i], intArrayField[i]);
                Assert.AreEqual(EVENT_TEST_CONFIGURATION.stringArrayField.Length, stringArrayField.Length);
                for (int i = 0; i < stringArrayField.Length; ++i)
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.stringArrayField[i], stringArrayField[i]);

                Assert.AreEqual(EVENT_TEST_CONFIGURATION.boolListField.Count, boolListField.Count);
                for (int i = 0; i < boolListField.Count; ++i)
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.boolListField[i], boolListField[i]);
                Assert.AreEqual(EVENT_TEST_CONFIGURATION.intListField.Count, intListField.Count);
                for (int i = 0; i < intListField.Count; ++i)
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.intListField[i], intListField[i]);
                Assert.AreEqual(EVENT_TEST_CONFIGURATION.stringListField.Count, stringListField.Count);
                for (int i = 0; i < stringListField.Count; ++i)
                    Assert.AreEqual(EVENT_TEST_CONFIGURATION.stringListField[i], stringListField[i]);

                Assert.AreEqual(EVENT_TEST_CONFIGURATION.boolRegisteredManually, boolRegisteredManually);
            }

            public void TryRegisterExisting()
            {
                // this should throw as boolField was already registered by attribute
                RegisterField(nameof(boolField), () => boolField);
            }
        }


        [TestMethod]
        public void WidgetConfiguration_Register()
        {
            TestConfiguration conf = new();
            conf.CheckFields();
        }

        [TestMethod]
        public void WidgetConfiguration_RegisterExisting()
        {
            TestConfiguration conf = new();
            conf.CheckFields();

            Assert.ThrowsException<WidgetConfigurationException>(() => conf.TryRegisterExisting());
        }

        [TestMethod]
        public void WidgetConfiguration_GetFields()
        {
            TestConfiguration mainConf = new TestConfiguration();
            IWidgetConfiguration conf = mainConf;

            Dictionary<string, WidgetConfigurationField> fields = conf.GetFields();

            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.boolField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.intField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.stringField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.boolArrayField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.intArrayField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.stringArrayField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.boolListField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.intListField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.stringListField)));
        }

        [TestMethod]
        public void WidgetConfiguration_Get_Field()
        {
            const bool newBool = false;
            const int newInt = 30;
            const string newString = "I am different now";

            TestConfiguration conf = new();

            conf.boolField = newBool;
            conf.intField = newInt;
            conf.stringField = newString;

            Assert.AreEqual(newBool, conf.Get<bool>("boolField").Get());
            Assert.AreEqual(newInt, conf.Get<int>("intField").Get());
            Assert.AreEqual(newString, conf.Get<string>("stringField").Get());
        }

        [TestMethod]
        public void WidgetConfiguration_UpdateViaMember()
        {
            const bool newBool = false;
            const int newInt = 30;
            const string newString = "I am different now";

            TestConfiguration conf = new();

            conf.boolField = newBool;
            conf.intField = newInt;
            conf.stringField = newString;

            Assert.AreEqual(newBool, conf.Get<bool>("boolField").Get());
            Assert.AreEqual(newInt, conf.Get<int>("intField").Get());
            Assert.AreEqual(newString, conf.Get<string>("stringField").Get());
        }

        [TestMethod]
        public void WidgetConfiguration_UpdateViaSet()
        {
            const bool newBool = false;
            const int newInt = 3000;
            const string newString = "I am even more different now";

            TestConfiguration conf = new();

            conf.Get<bool>("boolField").Set(newBool);
            conf.Get<int>("intField").Set(newInt);
            conf.Get<string>("stringField").Set(newString);

            Assert.AreEqual(newBool, conf.boolField);
            Assert.AreEqual(newInt, conf.intField);
            Assert.AreEqual(newString, conf.stringField);
        }

        [TestMethod]
        public void WidgetConfiguration_JsonConverterTest_Serialize()
        {
            TestConfiguration conf = new();
            conf.CheckFields();

            Assert.AreEqual(JsonConvert.SerializeObject(EVENT_TEST_CONFIGURATION), conf.Serialize());
        }

        [TestMethod]
        public void WidgetConfiguration_JsonConverterTest_Deserialize()
        {
            string serialized = JsonConvert.SerializeObject(EVENT_TEST_CONFIGURATION);

            TestConfiguration conf = WidgetConfiguration.Deserialize(serialized) as TestConfiguration;
            Assert.IsNotNull(conf);
            Assert.AreEqual(EVENT_TEST_CONFIGURATION.EventName, conf.EventName);

            conf.CheckFields();
        }
    }
}
