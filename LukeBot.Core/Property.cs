using System;
using System.Reflection;
using LukeBot.Common;
using Newtonsoft.Json;
using System.Linq;


namespace LukeBot.Core
{
    public abstract class Property
    {
        public string Name { get; private set; }
        public System.Type Type { get; private set; }

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
                // more exhaustive search across all assemblies
                valType = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName.Equals(type));

                if (valType == null)
                    throw new TypeNotFoundException("Couldn't create Property of type {0}", type);
            }

            Logger.Log().Debug("Deserializing object {0} to type {1}", serializedVal, valType);
            dynamic jObj = JsonConvert.DeserializeObject(serializedVal, valType);
            Type propType = typeof(PropertyType<>).MakeGenericType(valType);
            return Activator.CreateInstance(propType, new object[] {jObj} ) as Property;
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
