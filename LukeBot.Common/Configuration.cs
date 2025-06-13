using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using LukeBot.Logging;

namespace LukeBot.Common
{
    public abstract class Configuration: EventArgsBase
    {
        protected Dictionary<string, ConfigurationField> mFields = new();
        public delegate Configuration ConfigurationAllocator();
        private static Dictionary<string, ConfigurationAllocator> mAllocators = new();

        public static void RegisterAllocator(string confName, ConfigurationAllocator allocator)
        {
            mAllocators.Add(confName, allocator);
        }

        public static Configuration AllocateInstanceOf(string confName)
        {
            if (!mAllocators.ContainsKey(confName))
                throw new ConfigurationException("Allocator for configuration of type {0} does not exist", confName);

            return mAllocators[confName]();
        }

        public static Configuration Deserialize(string confString)
        {
            JsonSerializerOptions opts = new();
            opts.Converters.Add(new ConfigurationJsonConverter());

            return JsonSerializer.Deserialize<Configuration>(confString, opts);
        }

        protected void RegisterField(ConfigurationField field)
        {
            if (mFields.ContainsKey(field.Name))
            {
                throw new ConfigurationException("Configuration field {0} already exists", field.Name);
            }

            mFields.Add(field.Name, field);
        }

        protected void RegisterField<T>(string name, Expression<Func<T>> field)
        {
            RegisterField(new ConfigurationFieldAccessor<T>(name, field));
        }

        protected void RegisterField<T>(string name, Expression<Func<T>> field, IConfigurationFieldValidator<T> validator)
        {
            RegisterField(new ConfigurationFieldAccessor<T>(name, field, validator));
        }

        private ConfigurationField AllocateFieldAccessor(FieldInfo fi, ConfigurationFieldAttribute attribute)
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
            LambdaExpression accessorExpression = lambdaCreator.Invoke(null, new object[] { fieldMemberAccess, new ParameterExpression[] { } }) as LambdaExpression;

            // Fetch validator field info for our field type
            FieldInfo validatorField = attribute.GetType().GetField("validator", BindingFlags.Public | BindingFlags.Instance);

            // Fetch validator object (if available)
            // TODO this needs type checks...
            object validator = null;
            Type attributeType = attribute.GetType();
            FieldInfo[] fields = attributeType.GetFields();
            foreach (FieldInfo f in fields)
            {
                if (f.Name == "validator")
                {
                    Type validatorGenericArg = f.FieldType.GetGenericArguments()[0];
                    if (fi.FieldType != validatorGenericArg)
                        throw new ConfigurationFieldException("Validator's generic type {0} does not match field type {1}",
                                                                    validatorGenericArg.Name, fi.FieldType.Name);

                    validator = f.GetValue(attribute);
                    break;
                }
            }

            // finally, invoke the ConfigurationFieldAccessor<T> constructor and return the object for registration
            Type accessorType = typeof(ConfigurationFieldAccessor<>);
            Type[] typeArgs = { fi.FieldType };
            Type constructedType = accessorType.MakeGenericType(typeArgs);

            // if validator was present add it to constructor argument list
            object[] constructorArgs = { fi.Name, accessorExpression };
            if (validator != null)
            {
                constructorArgs = constructorArgs.Append(validator).ToArray();
            }

            // how does this spaghetti work I still have no idea
            return Activator.CreateInstance(constructedType, constructorArgs) as ConfigurationField;
        }

        public Configuration(string name)
            : base(name)
        {
            MemberInfo[] members = this.GetType().GetMembers(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            foreach (MemberInfo member in members)
            {
                Attribute[] attrs = Attribute.GetCustomAttributes(member, typeof(ConfigurationFieldAttribute), true);

                if (attrs != null && attrs.Length == 1)
                {
                    if (member.MemberType != MemberTypes.Field)
                    {
                        Logger.Log().Error("Configuration declared member {0} as configuration field, but it's not a Field - skipping", member.Name);
                        continue;
                    }

                    // attribute found - register given member as a field
                    ConfigurationField field = AllocateFieldAccessor(member as FieldInfo, attrs[0] as ConfigurationFieldAttribute);
                    if (field == null)
                    {
                        Logger.Log().Error("Configuration failed to allocate accessor for member {0} - skipping", member.Name);
                        continue;
                    }

                    RegisterField(field);
                }
            }
        }

        public string Serialize()
        {
            JsonSerializerOptions opts = new();
            opts.Converters.Add(new ConfigurationJsonConverter());

            return JsonSerializer.Serialize(this, opts);
        }

        public Dictionary<string, ConfigurationField> GetFields()
        {
            return mFields;
        }

        public ConfigurationField Get(string name)
        {
            if (!mFields.ContainsKey(name))
            {
                throw new ConfigurationException("Field {0} does not exist", name);
            }

            return mFields[name];
        }

        public ConfigurationFieldAccessor<T> Get<T>(string name)
        {
            return Get(name) as ConfigurationFieldAccessor<T>;
        }
    }
}
