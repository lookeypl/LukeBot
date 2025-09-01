using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Common;
using System.Collections.Generic;


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
            Assert.AreEqual(typeof(int), field.Type);
            Assert.AreEqual(null, field.UnderlyingType);

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
            Assert.AreEqual(typeof(string), field.Type);
            Assert.AreEqual(null, field.UnderlyingType);

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
            Assert.AreEqual(typeof(TestObject), field.Type);
            Assert.AreEqual(null, field.UnderlyingType);

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

        [TestMethod]
        public void ConfigurationFieldAccessor_Enumerable()
        {
            string name = "arrayField";
            int[] values = { 3, 2, 1 };

            ConfigurationFieldAccessor<int[]> field = new ConfigurationFieldAccessor<int[]>(name, () => values);
            Assert.AreEqual(name, field.Name);
            Assert.IsNotNull(field.Get());
            Assert.AreEqual(values, field.Get());
            Assert.AreEqual(typeof(int[]), field.Type);
            Assert.AreEqual(typeof(int), field.UnderlyingType);

            values[1] = 4;
            Assert.AreEqual(4, field.Get()[1]);

            field.Set(new int[] { 2, 3 });
            Assert.AreEqual(2, values.Length);
        }

        [TestMethod]
        public void ConfigurationFieldAccessor_Array()
        {
            string name = "listField";
            List<int> values = new List<int>{ 3, 2, 1 };

            ConfigurationFieldAccessor<List<int>> field = new ConfigurationFieldAccessor<List<int>>(name, () => values);
            Assert.AreEqual(name, field.Name);
            Assert.IsNotNull(field.Get());
            Assert.AreEqual(values, field.Get());
            Assert.AreEqual(typeof(List<int>), field.Type);
            Assert.AreEqual(typeof(int), field.UnderlyingType);

            values[1] = 4;
            Assert.AreEqual(4, field.Get()[1]);

            field.Set(new List<int> { 2, 3 });
            Assert.AreEqual(2, values.Count);
        }
    }
}
