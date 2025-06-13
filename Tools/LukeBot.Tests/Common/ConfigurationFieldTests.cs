using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using LukeBot.Common;


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
    }
}
