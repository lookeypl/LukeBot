using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using LukeBot.Communication.Common;
using LukeBot.Logging;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    public abstract class WidgetConfiguration: EventArgsBase, IWidgetConfiguration
    {
        private Dictionary<string, WidgetConfigurationField> mFields = new();

        public delegate WidgetConfiguration WidgetConfigurationAllocator();
        private static Dictionary<string, WidgetConfigurationAllocator> mAllocators = new();

        public static void RegisterAllocator(string confName, WidgetConfigurationAllocator allocator)
        {
            mAllocators.Add(confName, allocator);
        }

        public static WidgetConfiguration AllocateInstanceOf(string confName)
        {
            if (!mAllocators.ContainsKey(confName))
                throw new WidgetConfigurationException("Allocator for configuration of type {0} does not exist", confName);

            return mAllocators[confName]();
        }

        public static WidgetConfiguration Deserialize(string confString)
        {
            JsonSerializerOptions opts = new();
            opts.Converters.Add(new WidgetConfigurationJsonConverter());

            return JsonSerializer.Deserialize<WidgetConfiguration>(confString, opts);
        }

        protected void RegisterField(WidgetConfigurationField field)
        {
            if (mFields.ContainsKey(field.Name))
            {
                throw new WidgetConfigurationException("Configuration field {0} already exists", field.Name);
            }

            mFields.Add(field.Name, field);
        }

        protected void RegisterField<T>(string name, Expression<Func<T>> field)
        {
            RegisterField(new WidgetConfigurationFieldAccessor<T>(name, field));
        }

        private WidgetConfigurationField AllocateFieldAccessor(FieldInfo fi)
        {
            try
            {
                // Form a field member access based on constant expression (this)
                ConstantExpression thisConstant = Expression.Constant(this);
                MemberExpression fieldMemberAccess = Expression.MakeMemberAccess(thisConstant, fi);

                // Since Expression.Lambda<> is generic, and depends on @p fi type, we need to Reflection it too
                // fetch the Expression.Lambda<Func<T>>(Expression, params ParameterExpression[]) call
                Type funcType = typeof(Func<>);
                Type funcTypeConstructed = funcType.MakeGenericType(new[] { fi.FieldType });

                Type expressionType = typeof(Expression<>);
                Type expressionTypeConstructed = expressionType.MakeGenericType(new[] { funcTypeConstructed });

                MethodInfo lambdaCreatorGeneric = typeof(Expression).GetMethod(
                    "Lambda", 1, new Type[] { typeof(Expression), typeof(ParameterExpression[]) }
                );
                MethodInfo lambdaCreator = lambdaCreatorGeneric.MakeGenericMethod(new[] { funcTypeConstructed });

                // Invoke Expression.Lambda<Func<T>>(fieldMemberAccess) <- no extra parameters here
                LambdaExpression accessorExpression = lambdaCreator.Invoke(null, new object[] { fieldMemberAccess, new ParameterExpression[]{} }) as LambdaExpression;

                // finally, invoke the WidgetConfigurationFieldAccessor<T> constructor and return the object for registration
                Type accessorType = typeof(WidgetConfigurationFieldAccessor<>);
                Type[] typeArgs = { fi.FieldType };
                Type constructedType = accessorType.MakeGenericType(typeArgs);

                object[] args = { fi.Name, accessorExpression };
                return Activator.CreateInstance(constructedType, args) as WidgetConfigurationField;
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Failed to allocate a FieldAccessor instance: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Logger.Log().Error("Inner exception {0}: {1}", e.InnerException.ToString(), e.InnerException.Message);
                }
                return null;
            }
        }

        public WidgetConfiguration(string eventName)
            : base(eventName)
        {
            MemberInfo[] members = this.GetType().GetMembers(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            foreach (MemberInfo member in members)
            {
                var attrs = Attribute.GetCustomAttributes(member, typeof(WidgetConfigurationFieldAttribute), true);
                if (attrs != null && attrs.Length == 1)
                {
                    if (member.MemberType != MemberTypes.Field)
                    {
                        Logger.Log().Error("WidgetConfiguration declared member {0} as configuration field, but it's not a Field - skipping", member.Name);
                        continue;
                    }

                    // attribute found - register given member as a field
                    WidgetConfigurationField field = AllocateFieldAccessor(member as FieldInfo);
                    if (field == null)
                    {
                        Logger.Log().Error("WidgetConfiguration failed to allocate accessor for member {0} - skipping", member.Name);
                        continue;
                    }

                    RegisterField(field);
                }
            }
        }

        public string Serialize()
        {
            JsonSerializerOptions opts = new();
            opts.Converters.Add(new WidgetConfigurationJsonConverter());

            return JsonSerializer.Serialize(this, opts);
        }

        public Dictionary<string, WidgetConfigurationField> GetFields()
        {
            return mFields;
        }

        public WidgetConfigurationField Get(string name)
        {
            if (!mFields.ContainsKey(name))
            {
                throw new WidgetConfigurationException("Field {0} does not exist", name);
            }

            return mFields[name];
        }

        public WidgetConfigurationFieldAccessor<T> Get<T>(string name)
        {
            return Get(name) as WidgetConfigurationFieldAccessor<T>;
        }
    }
}
