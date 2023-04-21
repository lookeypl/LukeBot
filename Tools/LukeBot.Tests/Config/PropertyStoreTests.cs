using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Config;
using System;
using System.IO;
using System.Collections.Generic;

namespace LukeBot.Tests.Config
{
    [TestClass]
    public class PropertyStoreTests
    {
        private class ComplexObj
        {
            public int i { get; set; }
            public float f { get; set; }
            public string s { get; set; }

            public ComplexObj(int i, float f, string s)
            {
                this.i = i;
                this.f = f;
                this.s = s;
            }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                if (object.ReferenceEquals(this, obj)) return true;

                ComplexObj o = obj as ComplexObj;
                if (o == null) return false;

                return (this.i == o.i) &&
                       (this.f == o.f) &&
                       (this.s == o.s);
            }

            public override int GetHashCode()
            {
                return $"${this.i}${this.f}${this.s}".GetHashCode();
            }
        };

        private const string PROPERTY_STORE_TEST_DATA_FILE = "Data/test.store.lukebot";

        PropertyStore mStore = new PropertyStore(PROPERTY_STORE_TEST_DATA_FILE);

        private void TestPropertyValue<T>(string name, T val)
        {
            Assert.AreEqual<T>(mStore.Get(name).Get<T>(), val);
        }

        private void TestArrayPropertyValue<T>(string name, T[] val)
        {
            T[] arr = mStore.Get(name).Get<T[]>();

            Assert.AreEqual(val.Length, arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                Assert.AreEqual<T>(val[i], arr[i]);
            }
        }

        [ClassInitialize]
        static public void PropertyStore_TestClassStartup(TestContext context)
        {
            Console.WriteLine("TestClassInit");
        }

        [TestInitialize]
        public void PropertyStore_TestStartup()
        {
            Console.WriteLine("TestInit");

            mStore.Clear();
        }

        [TestMethod]
        public void PropertyStore_AddGet()
        {
            mStore.Add("test.a", Property.Create<int>(2));
            mStore.Add("test.b", Property.Create<int>(5));
            mStore.Add("test.test.c", Property.Create<float>(2.0f));

            TestPropertyValue<int>("test.a", 2);
            TestPropertyValue<int>("test.b", 5);
            TestPropertyValue<float>("test.test.c", 2.0f);
        }

        [TestMethod]
        public void PropertyStore_AddGetComplex()
        {
            const int TEST_INT = 10;
            const float TEST_FLOAT = 3.14f;
            const string TEST_STRING = "I am sped";

            ComplexObj complexSrc = new ComplexObj(TEST_INT, TEST_FLOAT, TEST_STRING);
            mStore.Add("test.complex", Property.Create<ComplexObj>(complexSrc));

            TestPropertyValue<ComplexObj>("test.complex", complexSrc);
        }

        [TestMethod]
        public void PropertyStore_AddGetArrays()
        {
            string[] arr = new string[] { "I don't know", "what to say" };
            mStore.Add("test.array", Property.Create<string[]>(arr));

            TestPropertyValue<string[]>("test.array", arr);
        }

        [TestMethod]
        public void PropertyStore_AddExisting()
        {
            mStore.Add("test.a", Property.Create<int>(2));
            TestPropertyValue<int>("test.a", 2);

            Assert.ThrowsException<PropertyAlreadyExistsException>(() => mStore.Add("test.a", Property.Create<int>(5)));
        }

        [TestMethod]
        public void PropertyStore_GetInvalidType()
        {
            mStore.Add("test.a", Property.Create<int>(2));
            Assert.ThrowsException<PropertyTypeInvalidException>(() => mStore.Get("test.a").Get<float>());
        }

        [TestMethod]
        public void PropertyStore_AddToNonDomain()
        {
            mStore.Add("test.a", Property.Create<int>(1));
            Assert.ThrowsException<PropertyNotADomainException>(() => mStore.Add("test.a.b", Property.Create<int>(2)));
        }

        [TestMethod]
        public void PropertyStore_RemoveNonExistent()
        {
            Assert.ThrowsException<PropertyNotFoundException>(() => mStore.Remove("test.doesnt_exist"));
        }

        [TestMethod]
        public void PropertyStore_AddRemoveGetNotFound()
        {
            mStore.Add("test.a", Property.Create<int>(1));
            TestPropertyValue<int>("test.a", 1);

            mStore.Remove("test.a");
            Assert.ThrowsException<PropertyNotFoundException>(() => mStore.Get("test.a"));
        }

        [TestMethod]
        public void PropertyStore_ModifyGet()
        {
            mStore.Add("test.a", Property.Create<int>(1));
            TestPropertyValue<int>("test.a", 1);

            mStore.Modify<int>("test.a", 4);
            TestPropertyValue<int>("test.a", 4);
        }

        [TestMethod]
        public void PropertyStore_ModifyTypeInvalid()
        {
            mStore.Add("test.a", Property.Create<int>(1));
            TestPropertyValue<int>("test.a", 1);

            Assert.ThrowsException<PropertyTypeInvalidException>(() => mStore.Modify<float>("test.a", 4));
        }

        [TestMethod]
        public void PropertyStore_ModifyDoesNotExist()
        {
            Assert.ThrowsException<PropertyNotFoundException>(() => mStore.Modify<int>("test.a", 4));
        }

        [TestMethod]
        public void PropertyStore_SaveJSON()
        {
            // to create some simple property tree which could be saved
            PropertyStore_AddGet();
            mStore.Add("other_test.a", Property.Create<float>(5.3f));
            mStore.Add("other_test.b", Property.Create<float>(420.69f));
            mStore.Add("other_test.text", Property.Create<string>("many_values"));

            TestPropertyValue<float>("other_test.a", 5.3f);
            TestPropertyValue<float>("other_test.b", 420.69f);
            TestPropertyValue<string>("other_test.text", "many_values");

            // Save the file to hard drive
            mStore.Save();
        }

        [TestMethod]
        public void PropertyStore_LoadJSON()
        {
            const bool TEST_BOOL = true;
            const int TEST_INT = 10;
            const float TEST_FLOAT = 3.14f;
            const string TEST_STRING = "I am sped";

            const int TEST_INT_2 = 22;

            const int TEST_INT_3 = 84096;
            const float TEST_FLOAT_2 = 1.23456f;
            const string TEST_STRING_2 = "I don't really know what to put in here";

            // fill our store with some props and save it
            mStore.Add("loadtest.bool", Property.Create<bool>(TEST_BOOL));
            mStore.Add("loadtest.int", Property.Create<int>(TEST_INT));
            mStore.Add("loadtest.nested.float", Property.Create<float>(TEST_FLOAT));
            mStore.Add("loadtest.nested.string", Property.Create<string>(TEST_STRING));

            mStore.Add("other.simple", Property.Create<int>(TEST_INT_2));
            ComplexObj complexSrc = new ComplexObj(TEST_INT_3, TEST_FLOAT_2, TEST_STRING_2);
            mStore.Add("other.complex", Property.Create<ComplexObj>(complexSrc));

            TestPropertyValue<bool>("loadtest.bool", TEST_BOOL);
            TestPropertyValue<int>("loadtest.int", TEST_INT);
            TestPropertyValue<float>("loadtest.nested.float", TEST_FLOAT);
            TestPropertyValue<string>("loadtest.nested.string", TEST_STRING);
            TestPropertyValue<int>("other.simple", TEST_INT_2);
            TestPropertyValue<ComplexObj>("other.complex", complexSrc);

            mStore.Save();

            // create a new store, load it and check if it has the values correct
            mStore = new PropertyStore(PROPERTY_STORE_TEST_DATA_FILE);
            TestPropertyValue<bool>("loadtest.bool", TEST_BOOL);
            TestPropertyValue<int>("loadtest.int", TEST_INT);
            TestPropertyValue<float>("loadtest.nested.float", TEST_FLOAT);
            TestPropertyValue<string>("loadtest.nested.string", TEST_STRING);
            TestPropertyValue<int>("other.simple", TEST_INT_2);
            TestPropertyValue<ComplexObj>("other.complex", complexSrc);
        }

        [TestMethod]
        public void PropertyStore_ArraysGet()
        {
            int[] simpleArray = new int[3];
            simpleArray[0] = 0;
            simpleArray[1] = 1;
            simpleArray[2] = 2;

            ComplexObj[] complexArray = new ComplexObj[3];
            complexArray[0] = new ComplexObj(1, 1.0f, "nr: 1");
            complexArray[1] = new ComplexObj(2, 2.0f, "nr: 2");
            complexArray[2] = new ComplexObj(3, 3.0f, "nr: 3");

            mStore.Add("array.simple", Property.Create<int[]>(simpleArray));
            mStore.Add("array.complex", Property.Create<ComplexObj[]>(complexArray));

            TestArrayPropertyValue<int>("array.simple", simpleArray);
            TestArrayPropertyValue<ComplexObj>("array.complex", complexArray);
        }

        [TestMethod]
        public void PropertyStore_ArraysSaveLoad()
        {
            int[] simpleArray = new int[3];
            simpleArray[0] = 0;
            simpleArray[1] = 1;
            simpleArray[2] = 2;

            ComplexObj[] complexArray = new ComplexObj[3];
            complexArray[0] = new ComplexObj(1, 1.0f, "nr: 1");
            complexArray[1] = new ComplexObj(2, 2.0f, "nr: 2");
            complexArray[2] = new ComplexObj(3, 3.0f, "nr: 3");

            mStore.Add("array.simple", Property.Create<int[]>(simpleArray));
            mStore.Add("array.complex", Property.Create<ComplexObj[]>(complexArray));

            mStore.Save();

            mStore = new PropertyStore(PROPERTY_STORE_TEST_DATA_FILE);
            TestArrayPropertyValue<int>("array.simple", simpleArray);
            TestArrayPropertyValue<ComplexObj>("array.complex", complexArray);
        }

        [TestMethod]
        public void PropertyStore_Exists()
        {
            mStore.Add("test.bool", Property.Create<bool>(true));
            mStore.Add("test.int", Property.Create<int>(1));
            mStore.Add("test.nested.float", Property.Create<float>(3.0f));
            mStore.Add("test.nested.string", Property.Create<string>("abcd"));
            mStore.Add("test.nested.domain.int", Property.Create<int>(3));
            mStore.Add("test.nested.domain.float", Property.Create<float>(6.21f));

            // check if all properties exist
            Assert.IsTrue(mStore.Exists("test.bool"));
            Assert.IsTrue(mStore.Exists("test.int"));
            Assert.IsTrue(mStore.Exists("test.nested.float"));
            Assert.IsTrue(mStore.Exists("test.nested.string"));
            Assert.IsTrue(mStore.Exists("test.nested.domain.int"));
            Assert.IsTrue(mStore.Exists("test.nested.domain.float"));

            // check if domains exist
            Assert.IsTrue(mStore.Exists("test"));
            Assert.IsTrue(mStore.Exists("test.nested"));
            Assert.IsTrue(mStore.Exists("test.nested.domain"));

            // check if invalid inputs return false
            Assert.IsFalse(mStore.Exists("testabcd"));
            Assert.IsFalse(mStore.Exists("test.float"));
            Assert.IsFalse(mStore.Exists("test.string"));
            Assert.IsFalse(mStore.Exists("test.nested.bool"));
            Assert.IsFalse(mStore.Exists("test.nested.int"));
            Assert.IsFalse(mStore.Exists("test.nested.domain.bool"));
            Assert.IsFalse(mStore.Exists("test.nested.domain.string"));
        }

        [TestMethod]
        public void PropertyStore_ExistsTyped()
        {
            mStore.Add("test.bool", Property.Create<bool>(true));
            mStore.Add("test.int", Property.Create<int>(1));
            mStore.Add("test.nested.float", Property.Create<float>(3.0f));
            mStore.Add("test.nested.string", Property.Create<string>("abcd"));
            mStore.Add("test.nested.domain.int", Property.Create<int>(3));
            mStore.Add("test.nested.domain.float", Property.Create<float>(6.21f));

            // check if all properties exist
            Assert.IsTrue(mStore.Exists<bool>("test.bool"));
            Assert.IsTrue(mStore.Exists<int>("test.int"));
            Assert.IsTrue(mStore.Exists<float>("test.nested.float"));
            Assert.IsTrue(mStore.Exists<string>("test.nested.string"));
            Assert.IsTrue(mStore.Exists<int>("test.nested.domain.int"));
            Assert.IsTrue(mStore.Exists<float>("test.nested.domain.float"));

            // check if invalid inputs return false
            Assert.IsFalse(mStore.Exists<float>("test.float"));
            Assert.IsFalse(mStore.Exists<string>("test.string"));
            Assert.IsFalse(mStore.Exists<bool>("test.nested.bool"));
            Assert.IsFalse(mStore.Exists<int>("test.nested.int"));
            Assert.IsFalse(mStore.Exists<bool>("test.nested.domain.bool"));
            Assert.IsFalse(mStore.Exists<string>("test.nested.domain.string"));

            // check if valid entries with incorrect types return false
            Assert.IsFalse(mStore.Exists<float>("test.bool"));
            Assert.IsFalse(mStore.Exists<string>("test.int"));
            Assert.IsFalse(mStore.Exists<bool>("test.nested.float"));
            Assert.IsFalse(mStore.Exists<int>("test.nested.string"));
            Assert.IsFalse(mStore.Exists<bool>("test.nested.domain.int"));
            Assert.IsFalse(mStore.Exists<string>("test.nested.domain.float"));
        }

        [TestMethod]
        public void PropertyStore_Clear()
        {
            // Add some data
            mStore.Add("test.bool", Property.Create<bool>(true));
            mStore.Add("test.int", Property.Create<int>(1));
            mStore.Add("test.nested.float", Property.Create<float>(3.0f));
            mStore.Add("test.nested.string", Property.Create<string>("abcd"));

            TestPropertyValue<bool>("test.bool", true);
            TestPropertyValue<int>("test.int", 1);
            TestPropertyValue<float>("test.nested.float", 3.0f);
            TestPropertyValue<string>("test.nested.string", "abcd");

            // Clear store, check if data exists (it shouldn't)
            mStore.Clear();

            Assert.IsFalse(mStore.Exists<bool>("test.bool"));
            Assert.IsFalse(mStore.Exists<int>("test.int"));
            Assert.IsFalse(mStore.Exists<float>("test.nested.float"));
            Assert.IsFalse(mStore.Exists<string>("test.nested.string"));
        }

        [TestCleanup]
        public void PropertyStore_Cleanup()
        {
            Console.WriteLine("TestCleanup");

            if (File.Exists(PROPERTY_STORE_TEST_DATA_FILE))
                File.Delete(PROPERTY_STORE_TEST_DATA_FILE);
        }

        [ClassCleanup]
        static public void PropertyStore_ClassTeardown()
        {
            Console.WriteLine("Cleanup");
        }
    }
}