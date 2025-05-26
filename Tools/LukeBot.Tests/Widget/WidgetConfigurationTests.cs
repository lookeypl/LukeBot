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
    public class ConfigurationTests
    {
        // NOTE: For test purposes this mirrors actual config class that would be used to serialize
        // and send to Widget's WebSocket. Changes here might require changes to TestConfiguration
        // and vice versa.
        //
        // NOTE 2 ELECTRIC BOOGALOO: the ordering matters for Serialize test. Attribute-registered
        // members are registered when base constructor is called.
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

            // NOTE public to make it easier to use...
            public int privateIntRegisteredViaAttribute = 202020;

            public string restrictedField = "restricted";
            public int evenSingleDigitsField = 2;
            public string manuallyRestrictedField = "manual";

            public bool boolRegisteredManually = false;
            // NOTE public to make it easier to use...
            public int privateIntRegisteredManually = 5060;

            public EventTestConfiguration()
                : base("TestConfiguration")
            {
            }
        }

        private class TestRestrictedFieldValidator: IConfigurationFieldValidator<string>
        {
            public bool Validate(string input)
            {
                switch (input)
                {
                case "restricted":
                case "unrestricted":
                    return true;
                default:
                    return false;
                }
            }

            public string Allowed()
            {
                return "restricted, unrestricted";
            }
        }

        private class TestManuallyRestrictedFieldValidator: IConfigurationFieldValidator<string>
        {
            public bool Validate(string input)
            {
                switch (input)
                {
                case "manual":
                case "auto":
                    return true;
                default:
                    return false;
                }
            }

            public string Allowed()
            {
                return "manual, auto";
            }
        }

        private static readonly EventTestConfiguration EVENT_TEST_CONFIGURATION = new();

        private class TestConfiguration: Configuration
        {
            [ConfigurationField]
            public bool boolField = EVENT_TEST_CONFIGURATION.boolField;
            [ConfigurationField]
            public int intField = EVENT_TEST_CONFIGURATION.intField;
            [ConfigurationField]
            public string stringField = EVENT_TEST_CONFIGURATION.stringField;
            [ConfigurationField]
            public bool[] boolArrayField = new bool[EVENT_TEST_CONFIGURATION.boolArrayField.Length];
            [ConfigurationField]
            public int[] intArrayField = new int[EVENT_TEST_CONFIGURATION.intArrayField.Length];
            [ConfigurationField]
            public string[] stringArrayField = new string[EVENT_TEST_CONFIGURATION.stringArrayField.Length];
            [ConfigurationField]
            public List<bool> boolListField = new(EVENT_TEST_CONFIGURATION.boolListField);
            [ConfigurationField]
            public List<int> intListField = new(EVENT_TEST_CONFIGURATION.intListField);
            [ConfigurationField]
            public List<string> stringListField = new(EVENT_TEST_CONFIGURATION.stringListField);
            [ConfigurationField]
            private int privateIntRegisteredViaAttribute = EVENT_TEST_CONFIGURATION.privateIntRegisteredViaAttribute;
            [ConfigurationRestrictedField<string>(typeof(TestRestrictedFieldValidator))]
            public string restrictedField = EVENT_TEST_CONFIGURATION.restrictedField;
            [ConfigurationListRestrictedField<int>(new[] {2, 4, 6, 8})]
            public int evenSingleDigitsField = EVENT_TEST_CONFIGURATION.evenSingleDigitsField;

            public string manuallyRestrictedField = EVENT_TEST_CONFIGURATION.manuallyRestrictedField;

            // below field on purpose has no attribute, as we are registering it manually with RegisterField()
            public bool boolRegisteredManually = EVENT_TEST_CONFIGURATION.boolRegisteredManually;
            private int privateIntRegisteredManually = EVENT_TEST_CONFIGURATION.privateIntRegisteredManually;

            private int privateIntNotRegistered = 0;

            static TestConfiguration()
            {
                Configuration.RegisterAllocator(nameof(TestConfiguration), () => new TestConfiguration());
            }

            public TestConfiguration()
                : base("TestConfiguration")
            {
                Array.Copy(EVENT_TEST_CONFIGURATION.boolArrayField, boolArrayField, EVENT_TEST_CONFIGURATION.boolArrayField.Length);
                Array.Copy(EVENT_TEST_CONFIGURATION.intArrayField, intArrayField, EVENT_TEST_CONFIGURATION.intArrayField.Length);
                Array.Copy(EVENT_TEST_CONFIGURATION.stringArrayField, stringArrayField, EVENT_TEST_CONFIGURATION.stringArrayField.Length);

                RegisterField(nameof(manuallyRestrictedField), () => manuallyRestrictedField, new TestManuallyRestrictedFieldValidator());
                RegisterField(nameof(boolRegisteredManually), () => boolRegisteredManually);
                RegisterField(nameof(privateIntRegisteredManually), () => privateIntRegisteredManually);
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
                Assert.IsNotNull(Get(nameof(privateIntRegisteredViaAttribute)));
                Assert.IsNotNull(Get(nameof(restrictedField)));
                Assert.IsNotNull(Get(nameof(manuallyRestrictedField)));
                Assert.IsNotNull(Get(nameof(evenSingleDigitsField)));
                Assert.IsNotNull(Get(nameof(boolRegisteredManually)));
                Assert.IsNotNull(Get(nameof(privateIntRegisteredManually)));

                // check if some random field does not exist
                Assert.ThrowsException<ConfigurationException>(() => Get(nameof(privateIntNotRegistered)));
                Assert.ThrowsException<ConfigurationException>(() => Get("randomNamedFieldWhichShouldNotExist"));

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

                Assert.AreEqual(EVENT_TEST_CONFIGURATION.restrictedField, restrictedField);
                Assert.AreEqual(EVENT_TEST_CONFIGURATION.manuallyRestrictedField, manuallyRestrictedField);

                Assert.AreEqual(EVENT_TEST_CONFIGURATION.boolRegisteredManually, boolRegisteredManually);

                Assert.AreEqual(EVENT_TEST_CONFIGURATION.privateIntRegisteredManually, privateIntRegisteredManually);
                Assert.AreEqual(EVENT_TEST_CONFIGURATION.privateIntRegisteredViaAttribute, privateIntRegisteredViaAttribute);
            }

            public void TryRegisterExisting()
            {
                // this should throw as boolField was already registered by attribute
                RegisterField(nameof(boolField), () => boolField);
            }
        }

        public class MismatchedAttributeAndFieldType: Configuration
        {
            // Attribute generic type (string) matches validator generic type, but not field type (int)
            [ConfigurationRestrictedField<string>(typeof(TestRestrictedFieldValidator))]
            public int myTypeDoesNotMatchAttributeType = 420;

            static MismatchedAttributeAndFieldType()
            {
                Configuration.RegisterAllocator(nameof(MismatchedAttributeAndFieldType), () => new MismatchedAttributeAndFieldType());
            }

            public MismatchedAttributeAndFieldType()
                : base("MismatchedAttirbuteAndFieldType")
            {
            }
        }

        public class MismatchedAttributeAndValidatorType: Configuration
        {
            // Attribute generic type (string) matches field type, but not validator generic type
            [ConfigurationRestrictedField<int>(typeof(TestRestrictedFieldValidator))]
            public int myTypeMatchesButValidatorDoesNot = 420;

            static MismatchedAttributeAndValidatorType()
            {
                Configuration.RegisterAllocator(nameof(MismatchedAttributeAndValidatorType), () => new MismatchedAttributeAndValidatorType());
            }

            public MismatchedAttributeAndValidatorType()
                : base("MismatchedAttributeAndValidatorType")
            {
            }
        }


        [TestMethod]
        public void Configuration_Register()
        {
            TestConfiguration conf = new();
            conf.CheckFields();
        }

        [TestMethod]
        public void Configuration_RegisterExisting()
        {
            TestConfiguration conf = new();
            conf.CheckFields();

            Assert.ThrowsException<ConfigurationException>(() => conf.TryRegisterExisting());
        }

        [TestMethod]
        public void Configuration_GetFields()
        {
            TestConfiguration mainConf = new TestConfiguration();
            IWidgetConfiguration conf = mainConf;

            Dictionary<string, ConfigurationField> fields = conf.GetFields();

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
        public void Configuration_Get_Field()
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
        public void Configuration_UpdateViaMember()
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
        public void Configuration_UpdateViaSet()
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
        public void Configuration_UpdateRestricted()
        {
            const string fieldName = "restrictedField";

            TestConfiguration conf = new();

            // this should work
            conf.Get<string>(fieldName).Set("unrestricted");
            Assert.AreEqual("unrestricted", conf.restrictedField);

            // this should throw
            Assert.ThrowsException<ConfigurationFieldValidatorException>(() => conf.Get<string>(fieldName).Set("what"));

            // old value should still be there
            Assert.AreEqual("unrestricted", conf.restrictedField);
        }

        [TestMethod]
        public void Configuration_UpdateManuallyRestricted()
        {
            const string fieldName = "manuallyRestrictedField";

            TestConfiguration conf = new();

            // this should work
            conf.Get<string>(fieldName).Set("auto");
            Assert.AreEqual("auto", conf.manuallyRestrictedField);

            // this should throw
            Assert.ThrowsException<ConfigurationFieldValidatorException>(() => conf.Get<string>(fieldName).Set("nope"));

            // old value should still be there
            Assert.AreEqual("auto", conf.manuallyRestrictedField);
        }

        [TestMethod]
        public void Configuration_UpdateListRestricted()
        {
            const string fieldName = "evenSingleDigitsField";

            TestConfiguration conf = new();

            // this should work
            conf.Get<int>(fieldName).Set(4);
            Assert.AreEqual(4, conf.evenSingleDigitsField);

            // this should throw
            Assert.ThrowsException<ConfigurationFieldValidatorException>(() => conf.Get<int>(fieldName).Set(1));

            // old value should still be there
            Assert.AreEqual(4, conf.evenSingleDigitsField);
        }

        [TestMethod]
        public void Configuration_MismatchedAttributeAndFieldType()
        {
            Assert.ThrowsException<ConfigurationFieldException>(() => new MismatchedAttributeAndFieldType());
        }

        [TestMethod]
        public void Configuration_MismatchedAttributeAndValidatorType()
        {
            Assert.ThrowsException<ConfigurationFieldException>(() => new MismatchedAttributeAndValidatorType());
        }

        [TestMethod]
        public void Configuration_JsonConverterTest_Serialize()
        {
            TestConfiguration conf = new();
            conf.CheckFields();

            Assert.AreEqual(JsonConvert.SerializeObject(EVENT_TEST_CONFIGURATION), conf.Serialize());
        }

        [TestMethod]
        public void Configuration_JsonConverterTest_Deserialize()
        {
            string serialized = JsonConvert.SerializeObject(EVENT_TEST_CONFIGURATION);

            TestConfiguration conf = Configuration.Deserialize(serialized) as TestConfiguration;
            Assert.IsNotNull(conf);
            Assert.AreEqual(EVENT_TEST_CONFIGURATION.EventName, conf.EventName);

            conf.CheckFields();
        }
    }
}
