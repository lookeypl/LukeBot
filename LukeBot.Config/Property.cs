using System;
using System.Reflection;
using LukeBot.Common;
using Newtonsoft.Json;
using System.Linq;


namespace LukeBot.Config
{
    public abstract class Property
    {
        public string Name { get; private set; }
        public System.Type Type { get; private set; }

        static private Type FindValueType(string typeName)
        {
            // more exhaustive search across all currently loaded assemblies
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => {
                    Type[] t = a.GetTypes();
                    return t;
                })
                .FirstOrDefault(t => t.FullName.Equals(typeName));
        }

        static private Property AllocateProperty(Type t, string val)
        {
            Logger.Log().Secure("Deserializing object {0} to type {1}", val, t);

            dynamic jObj = JsonConvert.DeserializeObject(val, t);
            Type propType = typeof(PropertyType<>).MakeGenericType(t);
            return Activator.CreateInstance(propType, new object[] {jObj} ) as Property;
        }

        protected Property(System.Type type)
        {
            Type = type;
        }

        public bool IsType(System.Type t)
        {
            return (Type == t);
        }

        public T Get<T>()
        {
            if (!IsType(typeof(T)))
                throw new PropertyTypeInvalidException("Invalid type {0} for property {1}", typeof(T).ToString(), Name);

            return (this as PropertyType<T>).Value;
        }

        public void Set<T>(T v)
        {
            if (!IsType(typeof(T)))
                throw new PropertyTypeInvalidException("Invalid type {0} for property {1}", typeof(T).ToString(), Name);

            (this as PropertyType<T>).Value = v;
        }

        static public Property Create<T>(T val)
        {
            return new PropertyType<T>(val);
        }

        static public Property Create(string type, string serializedVal)
        {
            Type valType = Type.GetType(type);
            if (valType == null)
            {
                valType = FindValueType(type);

                if (valType == null)
                {
                    Logger.Log().Warning("Couldn't locate value type {0}. Property will be lazily loaded on next Get call", type);
                    return new PropertyType<LazyProperty>(new LazyProperty(type, serializedVal)) as Property;
                }
            }

            return AllocateProperty(valType, serializedVal);
        }

        static internal Property CreateFromLazyProperty(LazyProperty p)
        {
            Type t = FindValueType(p.mTypeStr);
            if (t == null)
            {
                throw new PropertyTypeInvalidException("Property type {0} is invalid", p.mTypeStr);
            }

            return AllocateProperty(t, p.mSerializedVal);
        }

        internal void SetName(string name)
        {
            Name = name;
        }

        internal void Accept(PropertyStoreVisitor v)
        {
            if (Type == typeof(PropertyDomain))
            {
                Get<PropertyDomain>().Accept(v);
            }
            else
            {
                AcceptValue(v);
            }
        }

        internal abstract void AcceptValue(PropertyStoreVisitor v);
    };

    public class PropertyType<T>: Property
    {
        internal T Value { get; set; }

        public PropertyType()
            : base(typeof(T))
        {
        }

        public PropertyType(T val)
            : base(typeof(T))
        {
            Value = val;
        }

        internal override void AcceptValue(PropertyStoreVisitor v)
        {
            v.Visit<T>(this);
        }
    }
}
