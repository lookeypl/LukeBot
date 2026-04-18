using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using LukeBot.Common;
using System.Collections.Generic;
using System;
using System.Text.Json.Serialization;


namespace LukeBot.Tests.Common
{
    [TestClass]
    public class ConfigurationFieldTests_Serializable
    {
        private class DefaultTestConfiguration
        {
            public int defaultField = 30;
            public int nonSerializableField = 20;
            public int nonVisibleSerializableField = 15;

            public DefaultTestConfiguration()
            {
            }
        }

        private static readonly DefaultTestConfiguration DEFAULT_TEST_CONFIGURATION = new();

        private class DefaultSerializedTestConfiguration
        {
            [JsonInclude]
            public int defaultField = DEFAULT_TEST_CONFIGURATION.defaultField;

            [JsonInclude]
            public string EventName = nameof(TestConfiguration);
            [JsonInclude]
            public Guid EventID;  // Guid is generated every new instance of EventArgsBase; this will be assigned before testing serialization
            [JsonInclude]
            public string FullConfigurableTypeName = typeof(TestConfiguration).FullName;
        }

        public class TestConfiguration: Configuration<TestConfiguration>
        {
            [ConfigurationField]
            public int defaultField = DEFAULT_TEST_CONFIGURATION.defaultField;
            [ConfigurationField]
            [ConfigurationSerializationIgnore]
            public int nonSerializableField = DEFAULT_TEST_CONFIGURATION.nonSerializableField;
            [ConfigurationField]
            [ConfigurationFieldHidden]
            [ConfigurationSerializationIgnore]
            public int nonVisibleSerializableField = DEFAULT_TEST_CONFIGURATION.nonVisibleSerializableField;

            public void CheckFields()
            {
                // check if fields exist
                Assert.IsNotNull(Field(nameof(defaultField)));
                Assert.IsNotNull(Field(nameof(nonSerializableField)));
                Assert.IsNotNull(Field(nameof(nonVisibleSerializableField)));

                // check if accessors exist
                Assert.IsNotNull(Accessor<int>(nameof(defaultField)));
                Assert.IsNotNull(Accessor<int>(nameof(nonSerializableField)));
                Assert.IsNotNull(Accessor<int>(nameof(nonVisibleSerializableField)));

                // check values
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.defaultField, defaultField);
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.nonSerializableField, nonSerializableField);
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.nonVisibleSerializableField, nonVisibleSerializableField);

                // check default visibility
                Assert.IsTrue(Field(nameof(defaultField)).Visible);
                Assert.IsTrue(Field(nameof(nonSerializableField)).Visible);
                Assert.IsFalse(Field(nameof(nonVisibleSerializableField)).Visible);
            }
        }

        [TestMethod]
        public void ConfigurationField_Visibility_Default()
        {
            TestConfiguration conf = new();
            conf.CheckFields();
        }

        [TestMethod]
        public void ConifgurationField_Serializable_Serialization()
        {
            TestConfiguration conf = new();
            conf.CheckFields();

            DefaultSerializedTestConfiguration expected = new();
            expected.EventID = conf.EventID;
            Assert.AreEqual(JsonSerializer.Serialize<DefaultSerializedTestConfiguration>(expected), conf.Serialize());
        }
    }
}
