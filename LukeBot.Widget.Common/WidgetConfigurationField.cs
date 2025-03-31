using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;


namespace LukeBot.Widget.Common
{
    public class WidgetConfigurationFieldAttribute: Attribute
    {
    }

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
    }

    public class WidgetConfigurationFieldAccessor<T>: WidgetConfigurationField
    {
        private readonly Action<T> Setter;
        private readonly Func<T> Getter;

        public override Type Type { get => typeof(T); }

        public WidgetConfigurationFieldAccessor(string name, Expression<Func<T>> expression)
            : base(name)
        {
            if (expression.Body is not MemberExpression memberExpression)
                throw new WidgetConfigurationFieldException("Accessor can only be used with member expressions");

            ParameterExpression parameter = Expression.Parameter(typeof(T));

            BinaryExpression assignExpression = Expression.Assign(memberExpression, parameter);
            MethodInfo onSetterMethod = typeof(WidgetConfigurationField).GetMethod("OnSetter", BindingFlags.Instance | BindingFlags.NonPublic);

            BlockExpression body = Expression.Block(
                assignExpression,
                Expression.Call(Expression.Constant(this), onSetterMethod)
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
    }
}
