using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Widget;


namespace LukeBot.Tests.Common
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

        private class DefaultTestConfiguration
        {
            [JsonInclude]
            public bool boolField = true;
            [JsonInclude]
            public int intField = 42;
            [JsonInclude]
            public string stringField = "test omg testing hello";

            // NOTE public to make it easier to use...
            [JsonInclude]
            public int privateIntRegisteredViaAttribute = 202020;

            [JsonInclude]
            public string restrictedField = "restricted";
            [JsonInclude]
            public int evenSingleDigitsField = 2;
            [JsonInclude]
            public string manuallyRestrictedField = "manual";

            [JsonInclude]
            public bool boolRegisteredManually = false;
            // NOTE public to make it easier to use...
            [JsonInclude]
            public int privateIntRegisteredManually = 5060;

            // metadata
            [JsonInclude]
            public string EventName = nameof(TestConfiguration);
            [JsonInclude]
            public Guid EventID;  // Guid is generated every new instance of EventArgsBase; this will be assigned before testing serialization
            [JsonInclude]
            public string FullConfigurableTypeName = typeof(TestConfiguration).FullName;

            // non-JsonInclude-d for purpose, hidden fields should be omitted from JSON drop
            public int hiddenField = 420;

            public DefaultTestConfiguration()
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

        private static readonly DefaultTestConfiguration DEFAULT_TEST_CONFIGURATION = new();

        private class TestConfiguration: Configuration<TestConfiguration>
        {
            [ConfigurationField]
            public bool boolField = DEFAULT_TEST_CONFIGURATION.boolField;
            [ConfigurationField]
            public int intField = DEFAULT_TEST_CONFIGURATION.intField;
            [ConfigurationField]
            public string stringField = DEFAULT_TEST_CONFIGURATION.stringField;
            [ConfigurationField]
            private int privateIntRegisteredViaAttribute = DEFAULT_TEST_CONFIGURATION.privateIntRegisteredViaAttribute;
            [ConfigurationRestrictedField<string>(typeof(TestRestrictedFieldValidator))]
            public string restrictedField = DEFAULT_TEST_CONFIGURATION.restrictedField;
            [ConfigurationListRestrictedField<int>(new[] { 2, 4, 6, 8 })]
            public int evenSingleDigitsField = DEFAULT_TEST_CONFIGURATION.evenSingleDigitsField;

            public string manuallyRestrictedField = DEFAULT_TEST_CONFIGURATION.manuallyRestrictedField;

            // below field on purpose has no attribute, as we are registering it manually with RegisterField()
            public bool boolRegisteredManually = DEFAULT_TEST_CONFIGURATION.boolRegisteredManually;
            private int privateIntRegisteredManually = DEFAULT_TEST_CONFIGURATION.privateIntRegisteredManually;

            private int privateIntNotRegistered = 0;

            [ConfigurationField]
            [ConfigurationFieldHidden]
            public int hiddenField = DEFAULT_TEST_CONFIGURATION.hiddenField;

            public TestConfiguration()
            {
                RegisterField(nameof(manuallyRestrictedField), () => manuallyRestrictedField, new TestManuallyRestrictedFieldValidator());
                RegisterField(nameof(boolRegisteredManually), () => boolRegisteredManually);
                RegisterField(nameof(privateIntRegisteredManually), () => privateIntRegisteredManually);
            }

            public void CheckFields()
            {
                // check if fields exist
                Assert.IsNotNull(Field(nameof(boolField)));
                Assert.IsNotNull(Field(nameof(intField)));
                Assert.IsNotNull(Field(nameof(stringField)));
                Assert.IsNotNull(Field(nameof(privateIntRegisteredViaAttribute)));
                Assert.IsNotNull(Field(nameof(restrictedField)));
                Assert.IsNotNull(Field(nameof(manuallyRestrictedField)));
                Assert.IsNotNull(Field(nameof(evenSingleDigitsField)));
                Assert.IsNotNull(Field(nameof(boolRegisteredManually)));
                Assert.IsNotNull(Field(nameof(privateIntRegisteredManually)));
                Assert.IsNotNull(Field(nameof(hiddenField)));

                // check if accessors exist
                Assert.IsNotNull(Accessor<bool>(nameof(boolField)));
                Assert.IsNotNull(Accessor<int>(nameof(intField)));
                Assert.IsNotNull(Accessor<string>(nameof(stringField)));
                Assert.IsNotNull(Accessor<int>(nameof(privateIntRegisteredViaAttribute)));
                Assert.IsNotNull(Accessor<string>(nameof(restrictedField)));
                Assert.IsNotNull(Accessor<string>(nameof(manuallyRestrictedField)));
                Assert.IsNotNull(Accessor<int>(nameof(evenSingleDigitsField)));
                Assert.IsNotNull(Accessor<bool>(nameof(boolRegisteredManually)));
                Assert.IsNotNull(Accessor<int>(nameof(privateIntRegisteredManually)));
                Assert.IsNotNull(Accessor<int>(nameof(hiddenField)));

                // check if some random field does not exist
                Assert.ThrowsException<ConfigurationException>(() => Field(nameof(privateIntNotRegistered)));
                Assert.ThrowsException<ConfigurationException>(() => Field("randomNamedFieldWhichShouldNotExist"));
                Assert.ThrowsException<ConfigurationException>(() => Accessor<int>(nameof(privateIntNotRegistered)));
                Assert.ThrowsException<ConfigurationException>(() => Accessor<int>("randomNamedFieldWhichShouldNotExist"));
                Assert.ThrowsException<ConfigurationException>(() => Get<int>(nameof(privateIntNotRegistered)));
                Assert.ThrowsException<ConfigurationException>(() => Get<int>("randomNamedFieldWhichShouldNotExist"));

                // check field types
                Assert.AreEqual(ConfigurationFieldType.Simple, Field(nameof(boolField)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.Simple, Field(nameof(intField)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.String, Field(nameof(stringField)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.Simple, Field(nameof(privateIntRegisteredViaAttribute)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.String, Field(nameof(restrictedField)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.String, Field(nameof(manuallyRestrictedField)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.Simple, Field(nameof(evenSingleDigitsField)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.Simple, Field(nameof(boolRegisteredManually)).FieldType);
                Assert.AreEqual(ConfigurationFieldType.Simple, Field(nameof(privateIntRegisteredManually)).FieldType);

                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.boolField, boolField);
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.intField, intField);
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.stringField, stringField);

                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.restrictedField, restrictedField);
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.manuallyRestrictedField, manuallyRestrictedField);

                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.boolRegisteredManually, boolRegisteredManually);

                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.privateIntRegisteredManually, privateIntRegisteredManually);
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.privateIntRegisteredViaAttribute, privateIntRegisteredViaAttribute);

                // check field default visibility
                Assert.IsTrue(Field(nameof(boolField)).Visible);
                Assert.IsTrue(Field(nameof(intField)).Visible);
                Assert.IsTrue(Field(nameof(stringField)).Visible);
                Assert.IsTrue(Field(nameof(privateIntRegisteredViaAttribute)).Visible);
                Assert.IsTrue(Field(nameof(restrictedField)).Visible);
                Assert.IsTrue(Field(nameof(manuallyRestrictedField)).Visible);
                Assert.IsTrue(Field(nameof(evenSingleDigitsField)).Visible);
                Assert.IsTrue(Field(nameof(boolRegisteredManually)).Visible);
                Assert.IsTrue(Field(nameof(privateIntRegisteredManually)).Visible);
                Assert.IsFalse(Field(nameof(hiddenField)).Visible);
            }

            public void TryRegisterExisting()
            {
                // this should throw as boolField was already registered by attribute
                RegisterField(nameof(boolField), () => boolField);
            }
        }

        public class MismatchedAttributeAndFieldType: Configuration<MismatchedAttributeAndFieldType>
        {
            // Attribute generic type (string) matches validator generic type, but not field type (int)
            [ConfigurationRestrictedField<string>(typeof(TestRestrictedFieldValidator))]
            public int myTypeDoesNotMatchAttributeType = 420;
        }

        public class MismatchedAttributeAndValidatorType: Configuration<MismatchedAttributeAndValidatorType>
        {
            // Attribute generic type (string) matches field type, but not validator generic type
            [ConfigurationRestrictedField<int>(typeof(TestRestrictedFieldValidator))]
            public int myTypeMatchesButValidatorDoesNot = 420;
        }

        public class DefaultRestrictedFieldTestConfiguration: Configuration<DefaultRestrictedFieldTestConfiguration>
        {
            [ConfigurationListRestrictedField<string>(new string[] { "first", "second", "third" })]
            public string restrictedSetToSecond = "second";

            [ConfigurationListRestrictedField<string>(new string[] { "first", "second", "third" })]
            public string restrictedSetToFirst = "first";

            [ConfigurationListRestrictedField<int>(new int[] { 10, 20, 30 })]
            public int restrictedSetTo30 = 30;
        }

        public class EmptyRestrictedFieldTestConfiguration: Configuration<EmptyRestrictedFieldTestConfiguration>
        {
            [ConfigurationListRestrictedField<string>(new string[] { "first", "second", "third" })]
            public string restrictedEmpty = "";
        }

        public class WrongDefaultRestrictedFieldTestConfiguration: Configuration<WrongDefaultRestrictedFieldTestConfiguration>
        {
            [ConfigurationListRestrictedField<string>(new string[] { "first", "second", "third" })]
            public string badDefaultRestricted = "wrong";
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
            Configuration<TestConfiguration> conf = mainConf;

            Dictionary<string, ConfigurationField> fields = conf.GetFields();

            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.boolField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.intField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.stringField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.restrictedField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.manuallyRestrictedField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.evenSingleDigitsField)));
            Assert.IsTrue(fields.ContainsKey(nameof(mainConf.boolRegisteredManually)));

            // can't access these two via nameof
            Assert.IsTrue(fields.ContainsKey("privateIntRegisteredViaAttribute"));
            Assert.IsTrue(fields.ContainsKey("privateIntRegisteredManually"));
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

            Assert.AreEqual(newBool, conf.Get<bool>("boolField"));
            Assert.AreEqual(newInt, conf.Get<int>("intField"));
            Assert.AreEqual(newString, conf.Get<string>("stringField"));
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

            Assert.AreEqual(newBool, conf.Get<bool>("boolField"));
            Assert.AreEqual(newInt, conf.Get<int>("intField"));
            Assert.AreEqual(newString, conf.Get<string>("stringField"));
        }

        [TestMethod]
        public void Configuration_UpdateViaSet()
        {
            const bool newBool = false;
            const int newInt = 3000;
            const string newString = "I am even more different now";

            TestConfiguration conf = new();

            conf.Set<bool>("boolField", newBool);
            conf.Set<int>("intField", newInt);
            conf.Set<string>("stringField", newString);

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
            conf.Set<string>(fieldName, "unrestricted");
            Assert.AreEqual("unrestricted", conf.restrictedField);

            // this should throw
            Assert.ThrowsException<ConfigurationFieldValidatorException>(() => conf.Set<string>(fieldName, "what"));

            // old value should still be there
            Assert.AreEqual("unrestricted", conf.restrictedField);
        }

        [TestMethod]
        public void Configuration_UpdateManuallyRestricted()
        {
            const string fieldName = "manuallyRestrictedField";

            TestConfiguration conf = new();

            // this should work
            conf.Set<string>(fieldName, "auto");
            Assert.AreEqual("auto", conf.manuallyRestrictedField);

            // this should throw
            Assert.ThrowsException<ConfigurationFieldValidatorException>(() => conf.Set<string>(fieldName, "nope"));

            // old value should still be there
            Assert.AreEqual("auto", conf.manuallyRestrictedField);
        }

        [TestMethod]
        public void Configuration_UpdateListRestricted()
        {
            const string fieldName = "evenSingleDigitsField";

            TestConfiguration conf = new();

            // this should work
            conf.Set<int>(fieldName, 4);
            Assert.AreEqual(4, conf.evenSingleDigitsField);

            // this should throw
            Assert.ThrowsException<ConfigurationFieldValidatorException>(() => conf.Set<int>(fieldName, 1));

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

            // EventID is taken from just allocated conf. This is because allocation forms a new Guid.
            // We only test serialization here, so if EventID is incorrect it should be reflected somewhere else.
            DEFAULT_TEST_CONFIGURATION.EventID = conf.EventID;
            // default serialization should skip hidden fields
            string expected = JsonSerializer.Serialize<DefaultTestConfiguration>(DEFAULT_TEST_CONFIGURATION);
            Assert.AreEqual(expected, conf.Serialize());

            // we can force Serialize to add hidden fields as well
            // do a simple check to see if it will contain those
            string serializedWithAllFields = conf.Serialize(true);
            Assert.IsTrue(serializedWithAllFields.Contains(nameof(conf.hiddenField)));
        }

        [TestMethod]
        public void Configuration_JsonConverterTest_Deserialize()
        {
            string serialized = JsonSerializer.Serialize<DefaultTestConfiguration>(DEFAULT_TEST_CONFIGURATION);

            TestConfiguration conf = ConfigurationFactory.Deserialize(serialized) as TestConfiguration;
            Assert.IsNotNull(conf);
            Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.EventName, conf.EventName);
            Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.FullConfigurableTypeName, conf.FullConfigurableTypeName);
            conf.CheckFields();
        }

        [TestMethod]
        public void Configuration_RestrictedDefaults()
        {
            // this should not throw
            DefaultRestrictedFieldTestConfiguration conf = new();

            // default value that is part of the restriction list should be left alone
            Assert.AreEqual("second", conf.restrictedSetToSecond);

            // empty or incorrect fields should be default-assigned to first value on the list
            ConfigurationException e = Assert.ThrowsException<ConfigurationException>(() => new EmptyRestrictedFieldTestConfiguration());
            Assert.IsInstanceOfType(e.InnerException, typeof(ConfigurationFieldException));

            e = Assert.ThrowsException<ConfigurationException>(() => new WrongDefaultRestrictedFieldTestConfiguration());
            Assert.IsInstanceOfType(e.InnerException, typeof(ConfigurationFieldException));
        }
    }
}
