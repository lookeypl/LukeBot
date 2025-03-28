using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Widget.Common;


namespace LukeBot.Tests.Widget.Common
{
    [TestClass]
    public class WidgetConfigurationFieldAccessorTests
    {
        public class TestObject
        {
            public int i;
            public float f;

            public TestObject(int i, float f)
            {
                this.i = i;
                this.f = f;
            }
        }

        [TestMethod]
        public void WidgetConfigurationFieldAccessor_Value()
        {
            const string name = "field";
            const int value = 20;

            int v = value;

            WidgetConfigurationFieldAccessor<int> field = new WidgetConfigurationFieldAccessor<int>(name, () => v);
            Assert.AreEqual(name, field.Name);
            Assert.AreEqual(value, v);
            Assert.AreEqual(value, field.Get());

            v = 30;
            Assert.AreEqual(30, field.Get());

            field.Set(20);
            Assert.AreEqual(20, v);
        }

        [TestMethod]
        public void WidgetConfigurationFieldAccessor_String()
        {
            const string name = "field";
            const string value = "This is a test string";

            string v = value;

            WidgetConfigurationFieldAccessor<string> field = new WidgetConfigurationFieldAccessor<string>(name, () => v);
            Assert.AreEqual(name, field.Name);
            Assert.IsNotNull(field.Get());
            Assert.AreEqual(value, field.Get());

            v = "This is another string";
            Assert.AreEqual("This is another string", field.Get());

            field.Set("This is yet another string");
            Assert.AreEqual("This is yet another string", v);
        }

        [TestMethod]
        public void WidgetConfigurationFieldAccessor_Object()
        {
            const string name = "field";

            TestObject o = new(30, 4.2f);

            WidgetConfigurationFieldAccessor<TestObject> field = new WidgetConfigurationFieldAccessor<TestObject>(name, () => o);
            Assert.AreEqual(name, field.Name);
            Assert.IsNotNull(field.Get());
            Assert.AreEqual(30, field.Get().i);
            Assert.AreEqual(4.2f, field.Get().f);

            o.i = 42;
            o.f = 3.0f;
            Assert.AreEqual(42, field.Get().i);
            Assert.AreEqual(3.0f, field.Get().f);

            TestObject newObject = new(1, 0.5f);
            field.Set(newObject);
            Assert.AreEqual(o, field.Get());
            Assert.AreEqual(1, field.Get().i);
            Assert.AreEqual(0.5f, field.Get().f);
        }
    }
}
