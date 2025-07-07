using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;


namespace LukeBot.Common
{
    public struct ConfigurationFieldDescriptor
    {
        public string name;
    }

    // Editable interface definition
    // Used to define some custom logic for more complex configuration fields
    public interface IConfigurationEditable
    {
        public List<ConfigurationFieldDescriptor> GetFields();
        public string Get(string field);
        public void Set(string field, string value);
    }


    // Validator definition
    public interface IConfigurationFieldValidator<T>
    {
        public bool Validate(T input);
        public string Allowed(); // for printing and information purposes
    }

    // Default validator which assumes field is unrestricted
    public class ConfigurationFieldUnrestricted<T>: IConfigurationFieldValidator<T>
    {
        public bool Validate(T input) { return true; }
        public string Allowed() { return ""; }
    }

    // Helper validator with list of allowed elements
    public class ConfigurationFieldListRestricted<T>: IConfigurationFieldValidator<T>
    {
        public List<T> values = new();

        public ConfigurationFieldListRestricted(T[] vals)
        {
            foreach (T v in vals)
            {
                values.Add(v);
            }
        }

        public bool Validate(T input)
        {
            return values.Contains(input);
        }

        public string Allowed()
        {
            string listString = "";

            for (int i = 0; i < values.Count; ++i)
            {
                listString += values[i].ToString();
                if (i < values.Count - 1) listString += ", ";
            }

            return listString;
        }
    }


    // Default, unrestricted attribute
    public class ConfigurationFieldAttribute: Attribute
    {
    }

    // Attribute which contains a validator
    public class ConfigurationRestrictedFieldAttribute<T> : ConfigurationFieldAttribute
    {
        public IConfigurationFieldValidator<T> validator;

        protected ConfigurationRestrictedFieldAttribute()
        {
            this.validator = null; // assumes derived class will call Activator to set it
        }

        public ConfigurationRestrictedFieldAttribute(Type validatorType)
        {
            if (!validatorType.IsAssignableTo(typeof(IConfigurationFieldValidator<T>)))
                throw new ConfigurationFieldException("Validator type {0} cannot be assigned to {1}",
                                                            validatorType.Name, typeof(IConfigurationFieldValidator<T>).Name);

            this.validator = Activator.CreateInstance(validatorType) as IConfigurationFieldValidator<T>;
        }
    }

    // Attribute which takes a list of allowed values instead of a validator type
    public sealed class ConfigurationListRestrictedFieldAttribute<T>: ConfigurationRestrictedFieldAttribute<T>
    {
        public ConfigurationListRestrictedFieldAttribute(T[] values)
        {
            this.validator = Activator.CreateInstance(typeof(ConfigurationFieldListRestricted<T>), values) as IConfigurationFieldValidator<T>;
        }
    }

    public enum ConfigurationFieldType
    {
        Simple = 0,
        String,
        Class,
        Array,
        Enumerable,
        Max
    }

    // Base, abstract, type-agnostic configuration field class
    public abstract class ConfigurationField
    {
        public string Name { get; private set; }
        public ConfigurationFieldType FieldType { get; }
        public abstract Type Type { get; }
        public ConfigurationBase.OnUpdateDelegate mUpdateDelegate = null;
        internal bool IsRoot { get; set; }

        protected void OnSetter()
        {
            if (mUpdateDelegate != null) mUpdateDelegate();
        }

        protected ConfigurationField(string name, ConfigurationFieldType type)
        {
            Name = name;
            FieldType = type;
            IsRoot = ConfigurationBase.IsRootField(name);
        }

        // TODO maybe we could introduce a possibility to auto-cast the field
        // example:
        //   int x = 10;
        //   ConfigurationField f = new ConfigurationFieldAccessor<int>(nameof(x), () => x);
        //   float y = f.Get<float>();
        // This would probably require getting our hands dirty with Reflection...
        public T Get<T>()
        {
            if (!Type.IsAssignableTo(typeof(T)))
                throw new ConfigurationFieldException("Invalid type {0} - mismatched or cannot be assigned to", typeof(T).ToString());

            ConfigurationFieldAccessor<T> accessor = this as ConfigurationFieldAccessor<T>;
            return accessor.Get();
        }

        public void Set<T>(T val)
        {
            if (!Type.IsAssignableFrom(typeof(T)))
                throw new ConfigurationFieldException("Invalid type {0} - mismatched or cannot be assigned from", typeof(T).ToString());

            ConfigurationFieldAccessor<T> accessor = this as ConfigurationFieldAccessor<T>;
            accessor.Set(val);
        }

        public abstract void SetJson(JsonElement element);
        public abstract JsonElement GetJson();
        public abstract string GetValueString();
        public abstract void SetFromString(string s);
        public abstract string DescribeAllowedValues();
    }

    // Field accessor generic
    public class ConfigurationFieldAccessor<T>: ConfigurationField
    {
        private readonly Action<T> Setter;
        private readonly Func<T> Getter;
        public IConfigurationFieldValidator<T> mValidator = null;

        public override Type Type { get => typeof(T); }

        public ConfigurationFieldAccessor(string name, Expression<Func<T>> expression)
            : this(name, expression, new ConfigurationFieldUnrestricted<T>())
        {
        }

        public ConfigurationFieldAccessor(string name, Expression<Func<T>> expression, IConfigurationFieldValidator<T> validator)
            : base(name, ConfigurationBase.DetermineFieldType(typeof(T)))
        {
            mValidator = validator;

            if (expression.Body is not MemberExpression memberExpression)
                throw new ConfigurationFieldException("Accessor can only be used with member expressions");

            ParameterExpression parameter = Expression.Parameter(typeof(T));

            BinaryExpression assignExpression = Expression.Assign(memberExpression, parameter);
            MethodInfo onSetterMethod = typeof(ConfigurationField).GetMethod("OnSetter", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo validatorMethod = typeof(IConfigurationFieldValidator<T>).GetMethod("Validate");

            ConditionalExpression body = Expression.IfThenElse
            (
                // condition - run validator
                Expression.Call(Expression.Constant(mValidator), validatorMethod, parameter),
                // validator succeeded
                Expression.Block
                (
                    assignExpression,
                    Expression.Call(Expression.Constant(this), onSetterMethod)
                ),
                // validator failed - throw exception
                Expression.Throw(Expression.Constant(new ConfigurationFieldValidatorException(Name)))
            );
            Setter = Expression.Lambda<Action<T>>(body, parameter).Compile();
            Getter = expression.Compile();
        }

        public void Set(T v) => Setter(v);
        public T Get() => Getter();

        public override void SetJson(JsonElement element)
        {
            JsonSerializerOptions opts = new();
            opts.IncludeFields = true;
            Setter(element.Deserialize<T>(opts));
        }

        public override JsonElement GetJson()
        {
            return JsonSerializer.SerializeToElement<T>(Getter());
        }

        public override string GetValueString()
        {
            return Getter().ToString();
        }

        public override void SetFromString(string s)
        {
            Setter((T)Convert.ChangeType(s, typeof(T)));
        }

        public override string DescribeAllowedValues()
        {
            return mValidator.Allowed();
        }
    }
}
