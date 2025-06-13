using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Common;


namespace LukeBot.Tests.Common
{
    [TestClass]
    public class ConfigurationFieldAccessorTests
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
        public void ConfigurationFieldAccessor_Value()
        {
            const string name = "field";
            const int value = 20;

            int v = value;

            ConfigurationFieldAccessor<int> field = new ConfigurationFieldAccessor<int>(name, () => v);
            Assert.AreEqual(name, field.Name);
            Assert.AreEqual(value, v);
            Assert.AreEqual(value, field.Get());

            v = 30;
            Assert.AreEqual(30, field.Get());

            field.Set(20);
            Assert.AreEqual(20, v);
        }

        [TestMethod]
        public void ConfigurationFieldAccessor_String()
        {
            const string name = "field";
            const string value = "This is a test string";

            string v = value;

            ConfigurationFieldAccessor<string> field = new ConfigurationFieldAccessor<string>(name, () => v);
            Assert.AreEqual(name, field.Name);
            Assert.IsNotNull(field.Get());
            Assert.AreEqual(value, field.Get());

            v = "This is another string";
            Assert.AreEqual("This is another string", field.Get());

            field.Set("This is yet another string");
            Assert.AreEqual("This is yet another string", v);
        }

        [TestMethod]
        public void ConfigurationFieldAccessor_Object()
        {
            const string name = "field";

            TestObject o = new(30, 4.2f);

            ConfigurationFieldAccessor<TestObject> field = new ConfigurationFieldAccessor<TestObject>(name, () => o);
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
