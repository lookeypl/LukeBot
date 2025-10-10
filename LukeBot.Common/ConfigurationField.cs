using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Json;


namespace LukeBot.Common
{
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
        None = 0,
        Simple,
        String,
        Class,
        Array,
        Enumerable,
        List,
        Max
    }

    // Base, abstract, type-agnostic configuration field class
    // TODO: Fields (especially Class fields) could sometimes want to query for a value
    //       required to initialize (ala constructors). Figure out a way to integrate this somehow.
    public abstract class ConfigurationField
    {
        public string Name { get; private set; }
        public ConfigurationFieldType FieldType { get; }
        public ConfigurationFieldType UnderlyingFieldType { get; }
        public abstract Type Type { get; }
        public abstract Type UnderlyingType { get; }
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
            UnderlyingFieldType = ConfigurationBase.DetermineFieldType(UnderlyingType);
        }

        public T Get<T>()
        {
            if (!Type.IsAssignableTo(typeof(T)))
                throw new ConfigurationFieldException("Invalid type {0} - mismatched or cannot be assigned to", typeof(T).ToString());

            return (T)GetRawObject();
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
        public abstract object GetRawObject();
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
        public override Type UnderlyingType
        {
            get
            {
                if (FieldType == ConfigurationFieldType.Array)
                {
                    return typeof(T).GetElementType();
                }
                else if (FieldType == ConfigurationFieldType.Enumerable || FieldType == ConfigurationFieldType.List)
                {
                    return typeof(T).GetGenericArguments()[0];
                }
                else
                {
                    return null;
                }
            }
        }

        private MethodInfo mListValuesGetterGeneric = null;

        private string GetListValuesString<ListT>(List<ListT> list)
        {
            string result = "[";
            bool first = true;

            foreach (ListT item in list)
            {
                if (!first) result += ", ";
                else first = false;

                if (typeof(ConfigurationBase).IsAssignableFrom(typeof(ListT)))
                {
                    ConfigurationBase confBaseItem = item as ConfigurationBase;
                    result += confBaseItem.ToShortString();
                }
                else
                {
                    result += item.ToString();
                }
            }

            return result + ']';
        }

        public ConfigurationFieldAccessor(string name, Expression<Func<T>> expression)
            : this(name, expression, new ConfigurationFieldUnrestricted<T>())
        {
        }

        public ConfigurationFieldAccessor(string name, Expression<Func<T>> expression, IConfigurationFieldValidator<T> validator)
            : base(name, ConfigurationBase.DetermineFieldType(typeof(T)))
        {
            mValidator = validator;
            mListValuesGetterGeneric = typeof(ConfigurationFieldAccessor<>).MakeGenericType(new Type[] { typeof(T) })
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(m => m.Name == "GetListValuesString" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(List<>));

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
        public override object GetRawObject() => Getter();

        public override void SetJson(JsonElement element)
        {
            JsonSerializerOptions opts = new();
            opts.IncludeFields = true;
            Setter(element.Deserialize<T>(opts));
        }

        public override JsonElement GetJson()
        {
            JsonSerializerOptions opts = new();
            opts.IncludeFields = true;
            return JsonSerializer.SerializeToElement<T>(Getter(), opts);
        }

        public override string GetValueString()
        {
            if (FieldType == ConfigurationFieldType.List)
            {
                MethodInfo stringValuesGetter = mListValuesGetterGeneric.MakeGenericMethod(UnderlyingType);
                return (string)stringValuesGetter.Invoke(this, new object[] { Getter() });
            }
            else return Getter().ToString();
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
