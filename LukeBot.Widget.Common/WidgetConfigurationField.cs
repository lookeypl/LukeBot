using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;


namespace LukeBot.Widget.Common
{
    // Validator definition
    public interface IWidgetConfigurationFieldValidator<T>
    {
        public bool Validate(T input);
        public string Allowed(); // for printing and information purposes
    }

    // Default validator which assumes field is unrestricted
    public class WidgetConfigurationFieldUnrestricted<T>: IWidgetConfigurationFieldValidator<T>
    {
        public bool Validate(T input) { return true; }
        public string Allowed() { return ""; }
    }

    // Helper validator with list of allowed elements
    public class WidgetConfigurationFieldListRestricted<T>: IWidgetConfigurationFieldValidator<T>
    {
        public List<T> values = new();

        public WidgetConfigurationFieldListRestricted(T[] vals)
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
    public class WidgetConfigurationFieldAttribute: Attribute
    {
    }

    // Attribute which contains a validator
    public class WidgetConfigurationRestrictedFieldAttribute<T>: WidgetConfigurationFieldAttribute
    {
        public IWidgetConfigurationFieldValidator<T> validator;

        protected WidgetConfigurationRestrictedFieldAttribute()
        {
            this.validator = null; // assumes derived class will call Activator to set it
        }

        public WidgetConfigurationRestrictedFieldAttribute(Type validatorType)
        {
            if (!validatorType.IsAssignableTo(typeof(IWidgetConfigurationFieldValidator<T>)))
                throw new WidgetConfigurationFieldException("Validator type {0} cannot be assigned to {1}",
                                                            validatorType.Name, typeof(IWidgetConfigurationFieldValidator<T>).Name);

            this.validator = Activator.CreateInstance(validatorType) as IWidgetConfigurationFieldValidator<T>;
        }
    }

    // Attribute which takes a list of allowed values instead of a validator type
    public sealed class WidgetConfigurationListRestrictedFieldAttribute<T>: WidgetConfigurationRestrictedFieldAttribute<T>
    {
        public WidgetConfigurationListRestrictedFieldAttribute(T[] values)
        {
            this.validator = Activator.CreateInstance(typeof(WidgetConfigurationFieldListRestricted<T>), values) as IWidgetConfigurationFieldValidator<T>;
        }
    }

    // Base, abstract, type-agnostic configuration field class
    public abstract class WidgetConfigurationField
    {
        public string Name { get; private set; }
        public abstract Type Type { get; }
        public delegate void OnFieldSetDelegate();
        public OnFieldSetDelegate mFieldSetDelegate = null;

        protected void OnSetter()
        {
            if (mFieldSetDelegate != null) mFieldSetDelegate();
        }

        protected WidgetConfigurationField(string name)
        {
            Name = name;
        }

        // TODO maybe we could introduce a possibility to auto-cast the field
        // example:
        //   int x = 10;
        //   WidgetConfigurationField f = new WidgetConfigurationFieldAccessor<int>(nameof(x), () => x);
        //   float y = f.Get<float>();
        // This would probably require getting our hands dirty with Reflection...
        public T Get<T>()
        {
            if (typeof(T) != Type)
                throw new WidgetConfigurationFieldException("Invalid type {0}", typeof(T).ToString());

            WidgetConfigurationFieldAccessor<T> accessor = this as WidgetConfigurationFieldAccessor<T>;
            return accessor.Get();
        }

        public void Set<T>(T val)
        {
            if (typeof(T) != Type)
                throw new WidgetConfigurationFieldException("Invalid type {0}", typeof(T).ToString());

            WidgetConfigurationFieldAccessor<T> accessor = this as WidgetConfigurationFieldAccessor<T>;
            accessor.Set(val);
        }

        public abstract void SetJson(JsonElement element);
        public abstract JsonElement GetJson();
        public abstract string GetValueString();
        public abstract void SetFromString(string s);
        public abstract string DescribeAllowedValues();
    }

    // Field accessor generic
    public class WidgetConfigurationFieldAccessor<T>: WidgetConfigurationField
    {
        private readonly Action<T> Setter;
        private readonly Func<T> Getter;
        public IWidgetConfigurationFieldValidator<T> mValidator = null;

        public override Type Type { get => typeof(T); }

        public WidgetConfigurationFieldAccessor(string name, Expression<Func<T>> expression)
            : this(name, expression, new WidgetConfigurationFieldUnrestricted<T>())
        {
        }

        public WidgetConfigurationFieldAccessor(string name, Expression<Func<T>> expression, IWidgetConfigurationFieldValidator<T> validator)
            : base(name)
        {
            mValidator = validator;

            if (expression.Body is not MemberExpression memberExpression)
                throw new WidgetConfigurationFieldException("Accessor can only be used with member expressions");

            ParameterExpression parameter = Expression.Parameter(typeof(T));

            BinaryExpression assignExpression = Expression.Assign(memberExpression, parameter);
            MethodInfo onSetterMethod = typeof(WidgetConfigurationField).GetMethod("OnSetter", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo validatorMethod = typeof(IWidgetConfigurationFieldValidator<T>).GetMethod("Validate");

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
                Expression.Throw(Expression.Constant(new WidgetConfigurationFieldValidatorException(Name)))
            );
            Setter = Expression.Lambda<Action<T>>(body, parameter).Compile();
            Getter = expression.Compile();
        }

        public void Set(T v) => Setter(v);
        public T Get() => Getter();

        public override void SetJson(JsonElement element)
        {
            Setter(element.Deserialize<T>());
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
