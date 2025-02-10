using System;
using System.Collections.Generic;


namespace LukeBot.Widget.Common
{
    public enum WidgetConfigurationFieldType
    {
        BOOLEAN,
        INTEGER,
        STRING,
        ARRAY,
    };

    public abstract class WidgetConfigurationField
    {
        public string Name { get; private set; }
        public WidgetConfigurationFieldType Type { get; private set; }

        protected WidgetConfigurationField(string name, WidgetConfigurationFieldType type)
        {
            Name = name;
            Type = type;
        }

        public abstract void Parse(string value);
    }

    public abstract class WidgetConfigurationFieldValue<T>: WidgetConfigurationField
    {
        public T Value { get; protected set; }

        protected WidgetConfigurationFieldValue(string name, T value, WidgetConfigurationFieldType type)
            : base(name, type)
        {
            Value = value;
        }

        public void Update(T v)
        {
            Value = v;
        }
    }

    public class WidgetConfigurationFieldBoolean: WidgetConfigurationFieldValue<bool>
    {
        public WidgetConfigurationFieldBoolean(string name, bool value)
            : base(name, value, WidgetConfigurationFieldType.BOOLEAN)
        {
        }

        public override void Parse(string value)
        {
            Update(Boolean.Parse(value));
        }
    }

    public class WidgetConfigurationFieldInteger: WidgetConfigurationFieldValue<int>
    {
        public WidgetConfigurationFieldInteger(string name, int value)
            : base(name, value, WidgetConfigurationFieldType.INTEGER)
        {
        }

        public override void Parse(string value)
        {
            Update(Int32.Parse(value));
        }
    }

    public class WidgetConfigurationFieldString: WidgetConfigurationFieldValue<string>
    {
        public WidgetConfigurationFieldString(string name, string value)
            : base(name, value, WidgetConfigurationFieldType.STRING)
        {
        }

        public override void Parse(string v)
        {
            Update(v);
        }
    }

    public class WidgetConfigurationFieldArray: WidgetConfigurationFieldValue<List<WidgetConfigurationField>>
    {
        public WidgetConfigurationFieldArray(string name, List<WidgetConfigurationField> value)
            : base(name, value, WidgetConfigurationFieldType.ARRAY)
        {
        }

        public override void Parse(string v)
        {
            // TODO should this be supported?
            throw new NotSupportedException("Parsing an Array widget configuration field from string is not supported");
        }

        public void Add(WidgetConfigurationField field)
        {
            Value.Add(field);
        }

        public WidgetConfigurationField this[int i]
        {
            get { return Value[i]; }
            set { Value[i] = value; }
        }
    }

    public interface IWidgetConfiguration
    {
        public IEnumerable<WidgetConfigurationField> GetFields();
    }
}