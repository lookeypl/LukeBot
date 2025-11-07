using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using LukeBot.Common;
using System.Collections.Generic;
using System;


namespace LukeBot.Tests.Common
{
    [TestClass]
    public class ConfigurationFieldTests
    {
        [TestMethod]
        public void ConfigurationField_Get()
        {
            const string name = "field";
            const int value = 20;

            int v = value;

            ConfigurationField field = new ConfigurationFieldAccessor<int>(name, () => v);
            Assert.AreEqual(name, field.Name);
            Assert.AreEqual(value, v);
            Assert.AreEqual(value, field.Get<int>());

            Assert.ThrowsException<ConfigurationFieldException>(() => field.Get<bool>());
            Assert.ThrowsException<ConfigurationFieldException>(() => field.Get<float>());
            Assert.ThrowsException<ConfigurationFieldException>(() => field.Get<string>());
        }

        [TestMethod]
        public void ConfigurationField_GetJson()
        {
            const string name = "field";
            const int value = 20;

            int v = value;

            ConfigurationField field = new ConfigurationFieldAccessor<int>(name, () => v);
            JsonElement element = field.GetJson();
            Assert.IsNotNull(element);
            Assert.AreEqual(value, element.GetInt32());
        }

        [TestMethod]
        public void ConfigurationField_Set()
        {
            const string name = "field";
            const int value = 20;
            const int newValue = 30;

            int v = value;

            ConfigurationField field = new ConfigurationFieldAccessor<int>(name, () => v);
            Assert.AreEqual(name, field.Name);
            Assert.AreEqual(value, v);
            Assert.AreEqual(value, field.Get<int>());

            field.Set<int>(newValue);
            Assert.AreEqual(newValue, v);
            Assert.AreEqual(newValue, field.Get<int>());

            Assert.ThrowsException<ConfigurationFieldException>(() => field.Set<bool>(true));
            Assert.ThrowsException<ConfigurationFieldException>(() => field.Set<float>(1.5f));
            Assert.ThrowsException<ConfigurationFieldException>(() => field.Set<string>("this should fail"));
        }

        public static IEnumerable<object[]> AttributeTestCases()
        {
            int intValue = 3;
            float floatValue = 2.0f;
            string stringValue = "test";
            List<int> listValue = new List<int> { 1, 2, 3 };
            IEnumerable<float> enumerableValue = new List<float> { 1.0f, 2.0f, 3.5f };
            int[] arrayValue = new[] { 1, 2, 3 };

            return new[]
            {
                new object[] {
                    new ConfigurationFieldAccessor<int>("field", () => intValue),
                    typeof(int), null, ConfigurationFieldType.Simple
                },
                new object[] {
                    new ConfigurationFieldAccessor<float>("field", () => floatValue),
                    typeof(float), null, ConfigurationFieldType.Simple
                },
                new object[] {
                    new ConfigurationFieldAccessor<string>("field", () => stringValue),
                    typeof(string), null, ConfigurationFieldType.String
                },
                new object[] {
                    new ConfigurationFieldAccessor<List<int>>("field", () => listValue),
                    typeof(List<int>), typeof(int), ConfigurationFieldType.List
                },
                new object[] {
                    new ConfigurationFieldAccessor<IEnumerable<float>>("field", () => enumerableValue),
                    typeof(IEnumerable<float>), typeof(float), ConfigurationFieldType.Enumerable
                },
                new object[] {
                    new ConfigurationFieldAccessor<int[]>("field", () => arrayValue),
                    typeof(int[]), typeof(int), ConfigurationFieldType.Array
                },
            };
        }

        [DataTestMethod]
        [DynamicData(nameof(AttributeTestCases), DynamicDataSourceType.Method)]
        public void ConfigurationField_Attributes(ConfigurationField field, Type expectedType, Type expectedUnderlyingType, ConfigurationFieldType expectedFieldType)
        {
            const string expectedName = "field";

            Assert.AreEqual(expectedName, field.Name);
            Assert.AreEqual(expectedType, field.Type);
            Assert.AreEqual(expectedUnderlyingType, field.UnderlyingType);
            Assert.AreEqual(expectedFieldType, field.FieldType);
            Assert.IsTrue(field.Visible); // by default all fields should be visible
        }
    }
}
