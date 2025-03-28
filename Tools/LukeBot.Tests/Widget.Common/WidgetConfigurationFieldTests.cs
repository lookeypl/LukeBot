using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using LukeBot.Widget.Common;


namespace LukeBot.Tests.Widget.Common
{
    [TestClass]
    public class WidgetConfigurationFieldTests
    {
        [TestMethod]
        public void WidgetConfigurationField_Get()
        {
            const string name = "field";
            const int value = 20;

            int v = value;

            WidgetConfigurationField field = new WidgetConfigurationFieldAccessor<int>(name, () => v);
            Assert.AreEqual(name, field.Name);
            Assert.AreEqual(value, v);
            Assert.AreEqual(value, field.Get<int>());

            Assert.ThrowsException<WidgetConfigurationFieldException>(() => field.Get<bool>());
            Assert.ThrowsException<WidgetConfigurationFieldException>(() => field.Get<float>());
            Assert.ThrowsException<WidgetConfigurationFieldException>(() => field.Get<string>());
        }

        [TestMethod]
        public void WidgetConfigurationField_GetJson()
        {
            const string name = "field";
            const int value = 20;

            int v = value;

            WidgetConfigurationField field = new WidgetConfigurationFieldAccessor<int>(name, () => v);
            JsonElement element = field.GetJson();
            Assert.IsNotNull(element);
            Assert.AreEqual(value, element.GetInt32());
        }

        [TestMethod]
        public void WidgetConfigurationField_Set()
        {
            const string name = "field";
            const int value = 20;
            const int newValue = 30;

            int v = value;

            WidgetConfigurationField field = new WidgetConfigurationFieldAccessor<int>(name, () => v);
            Assert.AreEqual(name, field.Name);
            Assert.AreEqual(value, v);
            Assert.AreEqual(value, field.Get<int>());

            field.Set<int>(newValue);
            Assert.AreEqual(newValue, v);
            Assert.AreEqual(newValue, field.Get<int>());

            Assert.ThrowsException<WidgetConfigurationFieldException>(() => field.Set<bool>(true));
            Assert.ThrowsException<WidgetConfigurationFieldException>(() => field.Set<float>(1.5f));
            Assert.ThrowsException<WidgetConfigurationFieldException>(() => field.Set<string>("this should fail"));
        }
    }
}
