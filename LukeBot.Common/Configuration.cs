using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using LukeBot.Logging;

namespace LukeBot.Common
{
    public class ConfigurationFactory
    {
        public delegate ConfigurationBase ConfigurationAllocator();
        private static Dictionary<string, ConfigurationAllocator> mAllocators = new();

        internal static void RegisterAllocator(string fullName, ConfigurationAllocator allocator)
        {
            mAllocators.Add(fullName, allocator);
        }

        internal static ConfigurationBase AllocateInstanceOf(string confName)
        {
            if (!mAllocators.ContainsKey(confName))
                throw new ConfigurationException("Allocator for configuration of type {0} does not exist", confName);

            return mAllocators[confName]();
        }

        public static Configurable Deserialize<Configurable>(string confString)
            where Configurable : Configuration<Configurable>, new()
        {
            // ensures the Configurable's static constructor ran and registered its allocator
            // C# Runtime ensures it will only be ran once too, so it's a win-win
            RuntimeHelpers.RunClassConstructor(typeof(Configuration<Configurable>).TypeHandle);

            JsonSerializerOptions opts = new();
            opts.Converters.Add(new ConfigurationJsonConverter<Configurable>());
            opts.IncludeFields = true;

            return JsonSerializer.Deserialize<Configurable>(confString, opts);
        }

        public static ConfigurationBase Deserialize(string confString)
        {
            JsonDocument confDoc = JsonDocument.Parse(confString);
            if (confDoc == null)
                throw new ConfigurationException("Failed to parse configuration string");

            JsonElement confNameElement = confDoc.RootElement.GetProperty("FullConfigurableTypeName");
            if (confDoc == null)
                throw new ConfigurationException("Failed to extract configuration type name element");

            Type[] confType = AppDomain.CurrentDomain.GetAssemblies().Reverse()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetTypes())
                .Where(t => t.FullName.Equals(confNameElement.GetString()))
                .ToArray();

            if (confType.Length == 0)
                throw new ConfigurationException("Target configuration Type not found");

            if (confType.Length > 1)
                throw new ConfigurationException("Target configuration Type is ambiguous");

            MethodInfo deserializerGeneric = typeof(ConfigurationFactory).GetMethod(
                "Deserialize",
                1,
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(String) },
                null
            );

            MethodInfo deserializer = deserializerGeneric.MakeGenericMethod(new Type[] { confType[0] });
            return deserializer.Invoke(null, new[] { confString }) as ConfigurationBase;
        }
    }

    /**
     * Base for Configuration generic
     */
    public abstract class ConfigurationBase: EventArgsBase
    {
        public delegate void OnUpdateDelegate();
        public abstract OnUpdateDelegate UpdateNotifier { set; }

        // used when calling ConfigurationFactory.Deserialize()
        public string FullConfigurableTypeName;

        internal static ConfigurationFieldType DetermineFieldType(Type fieldType)
        {
            if (fieldType == null)
            {
                return ConfigurationFieldType.None;
            }

            if (fieldType.IsArray)
            {
                return ConfigurationFieldType.Array;
            }
            else if (fieldType.IsClass || fieldType.IsInterface)
            {
                if (typeof(IEnumerable).IsAssignableFrom(fieldType))
                {
                    if (typeof(string).IsAssignableFrom(fieldType))
                    {
                        // String is an IEnumerable-implementing type, we use it
                        // as a special case cause JSON might prefer to save it as ""
                        // field instead of an array of chars
                        return ConfigurationFieldType.String;
                    }
                    else if (fieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        return ConfigurationFieldType.List;
                    }
                    else
                    {
                        return ConfigurationFieldType.Enumerable;
                    }
                }
                else
                {
                    return ConfigurationFieldType.Class;
                }
            }

            return ConfigurationFieldType.Simple;
        }

        internal static bool IsRootField(string name)
        {
            return !name.Contains('.');
        }

        protected ConfigurationBase(string eventName, string configurableName)
            : base(eventName)
        {
            FullConfigurableTypeName = configurableName;
        }

        public abstract string Serialize(bool includeHidden = false);
        public abstract Dictionary<string, ConfigurationField> GetFields();
        public virtual string ToShortString() { return EventName; }
    }

    /**
     * Configuration generic class, representing an object that is configurable.
     *
     * This implementation is done to make editing of any Configurable objects easy and possible
     * via same CLI tools.
     *
     * Fields that are meant to be configurable in a Configurable object should have ConfigurationField
     * attribute or one of its derivatives.
     *
     * Complex types (ex. classes) will be inspected internally for ConfigurationField-derived attributes.
     * Any ConfigurationField-attributed fields which have simple/value types will be added as owner's
     * sub-field with '.' delimiter. In case of encountering an array or an IEnumerable-derived type
     * objects will be treated as an array and registered with standard array-access [] operator.
     */
    public abstract class Configuration<Configurable>: ConfigurationBase
        where Configurable : Configuration<Configurable>, new()
    {
        protected Dictionary<string, ConfigurationField> mFields = new();

        private void ListNotifierUpdater<ListT>(List<ListT> list, OnUpdateDelegate notifier)
            where ListT: ConfigurationBase
        {
            foreach (ListT l in list)
            {
                l.UpdateNotifier = notifier;
            }
        }

        public override OnUpdateDelegate UpdateNotifier
        {
            set
            {
                foreach (ConfigurationField field in mFields.Values)
                {
                    field.mUpdateDelegate = value;

                    // place the update notifier on complex Fields as well (if we have any)
                    if (field.FieldType == ConfigurationFieldType.Class &&
                        field.Type.IsAssignableTo(typeof(ConfigurationBase)))
                    {
                        field.Get<ConfigurationBase>().UpdateNotifier = value;
                    }
                    else if (field.FieldType == ConfigurationFieldType.List &&
                             field.UnderlyingFieldType == ConfigurationFieldType.Class &&
                             field.UnderlyingType.IsAssignableTo(typeof(ConfigurationBase)))
                    {
                        // get the List updater and invoke
                        MethodInfo updater = typeof(Configuration<>).MakeGenericType(new Type[] { typeof(Configurable) })
                            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                            .Single(m => m.Name == "ListNotifierUpdater" && m.IsGenericMethodDefinition &&
                                         m.GetParameters().Length == 2 &&
                                         m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(List<>) &&
                                         m.GetParameters()[1].ParameterType == typeof(OnUpdateDelegate))
                            .MakeGenericMethod(field.UnderlyingType);
                        updater.Invoke(this, new object[] { field.GetRawObject(), value });
                    }
                }
            }
        }

        static Configuration()
        {
            ConfigurationFactory.RegisterAllocator(typeof(Configurable).FullName, () => new Configurable());
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

        private ConfigurationField AllocateFieldAccessor(object owner, FieldInfo fi, ConfigurationFieldAttribute attribute, string namePrefix)
        {
            // Form a field member access based on constant expression (this)
            ConstantExpression thisConstant = Expression.Constant(owner);
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
            object[] constructorArgs = { namePrefix + fi.Name, accessorExpression };
            if (validator != null)
            {
                constructorArgs = constructorArgs.Append(validator).ToArray();
            }

            // how does this spaghetti work I still have no idea
            try
            {
                return Activator.CreateInstance(constructedType, constructorArgs) as ConfigurationField;
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        private void RegisterConfigurationFields(object fieldRef, string prefix = "")
        {
            MemberInfo[] members = fieldRef.GetType().GetMembers(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            foreach (MemberInfo member in members)
            {
                ConfigurationField confField = null;
                Attribute[] attrs = Attribute.GetCustomAttributes(member, typeof(ConfigurationFieldAttribute), true);

                if (attrs == null)
                {
                    // no attributes, quietly ignore this member
                    continue;
                }

                if (attrs.Length > 1)
                {
                    throw new ConfigurationFieldException("Field {0} can only have one ConfigurationField attribute.", member.Name);
                }

                if (attrs != null && attrs.Length == 1)
                {
                    if (member.MemberType != MemberTypes.Field)
                    {
                        Logger.Log().Warning("Configuration declared member {0} as configuration field, but it's not a Field - skipping", member.Name);
                        continue;
                    }

                    FieldInfo field = member as FieldInfo;
                    ConfigurationFieldType confFieldType = DetermineFieldType(field.FieldType);

                    // attribute found - register given member as a field
                    // NOTE: This will also register complex fields which are further processed below. This is fine.
                    confField = AllocateFieldAccessor(fieldRef, field, attrs[0] as ConfigurationFieldAttribute, prefix);
                    if (confField == null)
                    {
                        Logger.Log().Warning("Configuration failed to allocate accessor for member {0} - skipping", field.Name);
                        continue;
                    }

                    RegisterField(confField);

                    if (confFieldType == ConfigurationFieldType.Class)
                    {
                        Logger.Log().Debug("Found class-type field! Inspecting it internally");
                        RegisterConfigurationFields(field.GetValue(fieldRef), prefix + field.Name + ".");
                    }
                }
            }
        }

        private bool InheritsRawGeneric(Type type, Type genericBase)
        {
            while (type != null && type != typeof(object))
            {
                Type current = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (current == genericBase) return true;
                type = current;
            }
            return false;
        }

        public void CollectConfigurationVisibilityAttributes(object fieldRef)
        {
            MemberInfo[] members = fieldRef.GetType().GetMembers(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            foreach (MemberInfo member in members)
            {
                Attribute[] attrs = Attribute.GetCustomAttributes(member, typeof(ConfigurationFieldVisibilityAttribute), true);

                if (attrs == null)
                {
                    // no attributes, quietly ignore this member
                    continue;
                }

                if (attrs.Length > 1)
                {
                    throw new ConfigurationFieldException("Field {0} can only have one visibility attribute.", member.Name);
                }

                if (attrs.Length == 1)
                {
                    if (member.MemberType != MemberTypes.Field)
                    {
                        Logger.Log().Warning("Configuration declared member {0} as configuration field, but it's not a Field - skipping", member.Name);
                        continue;
                    }

                    FieldInfo field = member as FieldInfo;

                    // dig out the field from our preexisting collection
                    if (!mFields.ContainsKey(field.Name))
                    {
                        // if it doesn't exist, it means we should simply ignore it
                        // visibility attribute will have no effect
                        // TODO should we though?
                        continue;
                    }

                    ConfigurationFieldVisibilityAttribute visibility = attrs[0] as ConfigurationFieldVisibilityAttribute;
                    ConfigurationField confField = mFields[field.Name];

                    if (visibility.GetType().IsAssignableTo(typeof(ConfigurationParameterizedVisibilityBaseAttribute)))
                    {
                        ConfigurationParameterizedVisibilityBaseAttribute baseVisibility = visibility as ConfigurationParameterizedVisibilityBaseAttribute;

                        if (!mFields.ContainsKey(baseVisibility.mParameterName))
                        {
                            throw new ConfigurationFieldException("Predicate-based visibility attribute refers to non-existent field: {0}", baseVisibility.mParameterName);
                        }

                        baseVisibility.SetPredicateAccessor(mFields[baseVisibility.mParameterName]);
                    }

                    // check if visibility attribute is actually a derivative of ConfigurationParameterizedVisibilityAttribute
                    // if it is, we need to perform field resolution so that Predicate() calls work

                    confField.SetVisibilityPredicate(visibility);
                }
            }
        }

        public Configuration()
            : base(typeof(Configurable).Name, typeof(Configurable).FullName)
        {
            RegisterConfigurationFields(this);
            CollectConfigurationVisibilityAttributes(this);
        }

        public override string Serialize(bool includeHidden = false)
        {
            JsonSerializerOptions opts = new();
            opts.Converters.Add(new ConfigurationJsonConverter<Configurable>(includeHidden));

            return JsonSerializer.Serialize(this, opts);
        }

        public override Dictionary<string, ConfigurationField> GetFields()
        {
            return mFields;
        }

        public ConfigurationField Field(string name)
        {
            if (!mFields.ContainsKey(name))
            {
                throw new ConfigurationException("Field {0} does not exist", name);
            }

            return mFields[name];
        }

        public ConfigurationFieldAccessor<T> Accessor<T>(string name)
        {
            return Field(name) as ConfigurationFieldAccessor<T>;
        }

        public T Get<T>(string name)
        {
            return Field(name).Get<T>();
        }

        public void Parse(string name, string value)
        {
            if (!mFields.ContainsKey(name))
            {
                throw new ConfigurationException("Field {0} does not exist", name);
            }

            Field(name).SetFromString(value);
        }

        public void Set<T>(string name, T value)
        {
            if (!mFields.ContainsKey(name))
            {
                throw new ConfigurationException("Field {0} does not exist", name);
            }

            Field(name).Set<T>(value);
        }
    }
}
