using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using LukeBot.Common;
using System.Collections.Generic;
using System;


namespace LukeBot.Tests.Common
{
    [TestClass]
    public class ConfigurationFieldTests_Visibility
    {
        private class DefaultTestConfiguration
        {
            public int defaultVisibilityField = 30;
            public int visibilityDecidingField = 0;
            public string visibilityRestrictedDecidingField = "none";
            public int alwaysHiddenField = 30;
            public int visibleWhenIntIsHigherThanTen = 20;
            public int visibleWhenStringFirst = 1;
            public int visibleWhenStringFirstOrThird = 23;

            public DefaultTestConfiguration()
            {
            }
        }

        private static readonly DefaultTestConfiguration DEFAULT_TEST_CONFIGURATION = new();

        public class VisibleWhenHigherThanTen: ConfigurationParameterizedVisibilityAttribute<int>
        {
            public VisibleWhenHigherThanTen(string fieldName)
                : base(fieldName)
            { }

            public override bool Predicate(int parameter)
            {
                return (parameter > 10);
            }
        }

        public class VisibleWhenStringIsFirst: ConfigurationParameterizedVisibilityAttribute<string>
        {
            public VisibleWhenStringIsFirst(string fieldName)
                : base(fieldName)
            { }

            public override bool Predicate(string parameter)
            {
                return parameter == "first";
            }
        }

        public class VisibleWhenStringIsFirstOrThird: ConfigurationParameterizedVisibilityAttribute<string>
        {
            public VisibleWhenStringIsFirstOrThird(string field)
                : base(field)
            { }

            public override bool Predicate(string parameter)
            {
                return parameter == "first" || parameter == "third";
            }
        }

        public class TestConfiguration: Configuration<TestConfiguration>
        {
            [ConfigurationField]
            public int defaultVisibilityField = DEFAULT_TEST_CONFIGURATION.defaultVisibilityField;
            [ConfigurationField]
            public int visibilityDecidingField = DEFAULT_TEST_CONFIGURATION.visibilityDecidingField;
            [ConfigurationListRestrictedField<string>(new[] { "none", "first", "second", "third" })]
            public string visibilityRestrictedDecidingField = DEFAULT_TEST_CONFIGURATION.visibilityRestrictedDecidingField;

            [ConfigurationField]
            [ConfigurationFieldHidden]
            public int alwaysHiddenField = DEFAULT_TEST_CONFIGURATION.alwaysHiddenField;

            [ConfigurationField]
            [VisibleWhenHigherThanTen(nameof(visibilityDecidingField))]
            public int visibleWhenIntIsHigherThanTen = DEFAULT_TEST_CONFIGURATION.visibleWhenIntIsHigherThanTen;

            [ConfigurationField]
            [VisibleWhenStringIsFirst(nameof(visibilityRestrictedDecidingField))]
            public int visibleWhenStringFirst = DEFAULT_TEST_CONFIGURATION.visibleWhenStringFirst;

            [ConfigurationField]
            [VisibleWhenStringIsFirstOrThird(nameof(visibilityRestrictedDecidingField))]
            public int visibleWhenStringFirstOrThird = DEFAULT_TEST_CONFIGURATION.visibleWhenStringFirstOrThird;

            public void CheckFields()
            {
                // check if fields exist
                Assert.IsNotNull(Field(nameof(defaultVisibilityField)));
                Assert.IsNotNull(Field(nameof(visibilityDecidingField)));
                Assert.IsNotNull(Field(nameof(visibilityRestrictedDecidingField)));
                Assert.IsNotNull(Field(nameof(alwaysHiddenField)));
                Assert.IsNotNull(Field(nameof(visibleWhenIntIsHigherThanTen)));
                Assert.IsNotNull(Field(nameof(visibleWhenStringFirst)));
                Assert.IsNotNull(Field(nameof(visibleWhenStringFirstOrThird)));

                // check if accessors exist
                Assert.IsNotNull(Accessor<int>(nameof(defaultVisibilityField)));
                Assert.IsNotNull(Accessor<int>(nameof(visibilityDecidingField)));
                Assert.IsNotNull(Accessor<string>(nameof(visibilityRestrictedDecidingField)));
                Assert.IsNotNull(Accessor<int>(nameof(alwaysHiddenField)));
                Assert.IsNotNull(Accessor<int>(nameof(visibleWhenIntIsHigherThanTen)));
                Assert.IsNotNull(Accessor<int>(nameof(visibleWhenStringFirst)));
                Assert.IsNotNull(Accessor<int>(nameof(visibleWhenStringFirstOrThird)));

                // check values
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.defaultVisibilityField, defaultVisibilityField);
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.visibilityDecidingField, visibilityDecidingField);
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.visibilityRestrictedDecidingField, visibilityRestrictedDecidingField);
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.alwaysHiddenField, alwaysHiddenField);
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.visibleWhenIntIsHigherThanTen, visibleWhenIntIsHigherThanTen);
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.visibleWhenStringFirst, visibleWhenStringFirst);
                Assert.AreEqual(DEFAULT_TEST_CONFIGURATION.visibleWhenStringFirstOrThird, visibleWhenStringFirstOrThird);

                // check default visibility
                Assert.IsTrue(Field(nameof(defaultVisibilityField)).Visible);
                Assert.IsTrue(Field(nameof(visibilityDecidingField)).Visible);
                Assert.IsTrue(Field(nameof(visibilityRestrictedDecidingField)).Visible);
                Assert.IsFalse(Field(nameof(alwaysHiddenField)).Visible);
                Assert.IsFalse(Field(nameof(visibleWhenIntIsHigherThanTen)).Visible);
                Assert.IsFalse(Field(nameof(visibleWhenStringFirst)).Visible);
                Assert.IsFalse(Field(nameof(visibleWhenStringFirstOrThird)).Visible);
            }
        }

        [TestMethod]
        public void ConfigurationField_Visibility_Default()
        {
            TestConfiguration conf = new();
            conf.CheckFields();
        }

        [TestMethod]
        public void ConfigurationField_Visibility_Test()
        {
            TestConfiguration conf = new();
            conf.CheckFields();

            conf.visibilityDecidingField = 10;
            Assert.IsFalse(conf.Field(nameof(conf.alwaysHiddenField)).Visible);
            Assert.IsFalse(conf.Field(nameof(conf.visibleWhenIntIsHigherThanTen)).Visible);
            Assert.IsFalse(conf.Field(nameof(conf.visibleWhenStringFirst)).Visible);
            Assert.IsFalse(conf.Field(nameof(conf.visibleWhenStringFirstOrThird)).Visible);

            conf.visibilityDecidingField = 11;
            Assert.IsFalse(conf.Field(nameof(conf.alwaysHiddenField)).Visible);
            Assert.IsTrue(conf.Field(nameof(conf.visibleWhenIntIsHigherThanTen)).Visible);
            Assert.IsFalse(conf.Field(nameof(conf.visibleWhenStringFirst)).Visible);
            Assert.IsFalse(conf.Field(nameof(conf.visibleWhenStringFirstOrThird)).Visible);

            conf.Field(nameof(conf.visibilityRestrictedDecidingField)).Set<string>("first");
            Assert.IsFalse(conf.Field(nameof(conf.alwaysHiddenField)).Visible);
            Assert.IsTrue(conf.Field(nameof(conf.visibleWhenIntIsHigherThanTen)).Visible);
            Assert.IsTrue(conf.Field(nameof(conf.visibleWhenStringFirst)).Visible);
            Assert.IsTrue(conf.Field(nameof(conf.visibleWhenStringFirstOrThird)).Visible);

            conf.Field(nameof(conf.visibilityRestrictedDecidingField)).Set<string>("second");
            Assert.IsFalse(conf.Field(nameof(conf.alwaysHiddenField)).Visible);
            Assert.IsTrue(conf.Field(nameof(conf.visibleWhenIntIsHigherThanTen)).Visible);
            Assert.IsFalse(conf.Field(nameof(conf.visibleWhenStringFirst)).Visible);
            Assert.IsFalse(conf.Field(nameof(conf.visibleWhenStringFirstOrThird)).Visible);

            conf.Field(nameof(conf.visibilityRestrictedDecidingField)).Set<string>("third");
            Assert.IsFalse(conf.Field(nameof(conf.alwaysHiddenField)).Visible);
            Assert.IsTrue(conf.Field(nameof(conf.visibleWhenIntIsHigherThanTen)).Visible);
            Assert.IsFalse(conf.Field(nameof(conf.visibleWhenStringFirst)).Visible);
            Assert.IsTrue(conf.Field(nameof(conf.visibleWhenStringFirstOrThird)).Visible);
        }


        public class InvalidVisibilityConfiguration: Configuration<InvalidVisibilityConfiguration>
        {
            [ConfigurationField]
            [VisibleWhenHigherThanTen("thisFieldDoesNotExist")]
            public int thisFieldsVisibilityRefersToNonExistentField;
        }

        [TestMethod]
        public void ConfigurationField_Visibility_Invalid()
        {
            Assert.ThrowsException<ConfigurationFieldException>(() => new InvalidVisibilityConfiguration());
        }
    }
}
