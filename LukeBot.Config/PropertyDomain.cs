using System.Collections.Generic;
using System.Threading;
using System;


namespace LukeBot.Config
{
    internal class PropertyDomain
    {
        public string mName { get; private set; }
        private Dictionary<string, Property> mProperties;
        private Mutex mMutex;

        public PropertyDomain(string name)
        {
            mName = name;
            mProperties = new Dictionary<string, Property>();
            mMutex = new Mutex();
        }

        ~PropertyDomain()
        {
            mName = null;
            mProperties.Clear();
            mProperties = null;
            mMutex = null;
        }

        // throws error if property already exists
        public void Add(Path path, Property p)
        {
            string name = path.Pop();

            if (path.Empty)
            {
                // end of path, our prop should be added to this domain
                p.SetName(name);
                if (!mProperties.TryAdd(name, p))
                {
                    throw new PropertyAlreadyExistsException(name);
                }

                // added successfully
                return;
            }

            // not end of path, check if we have a domain like that
            Property prop;
            if (!mProperties.TryGetValue(name, out prop))
            {
                prop = Property.Create<PropertyDomain>(new PropertyDomain(name));
                prop.SetName(name);
                mProperties.Add(name, prop);
            }

            if (!prop.IsType(typeof(PropertyDomain)))
            {
                throw new PropertyNotADomainException(name);
            }

            PropertyDomain domain = prop.Get<PropertyDomain>();
            domain.Add(path, p);
        }

        public bool Exists(Path path)
        {
            string name = path.Pop();

            Property p;

            if (!mProperties.TryGetValue(name, out p))
            {
                return false;
            }

            if (path.Empty)
            {
                return true;
            }

            if (p.IsType(typeof(PropertyDomain)))
            {
                return p.Get<PropertyDomain>().Exists(path);
            }

            return false;
        }

        public bool Exists<T>(Path path)
        {
            string name = path.Pop();

            Property p;

            if (!mProperties.TryGetValue(name, out p))
            {
                return false;
            }

            if (path.Empty)
            {
                return p.IsType(typeof(T));
            }

            if (p.IsType(typeof(PropertyDomain)))
            {
                return p.Get<PropertyDomain>().Exists<T>(path);
            }

            return false;
        }

        public Property Get(Path path)
        {
            string name = path.Pop();

            Property p;

            if (!mProperties.TryGetValue(name, out p))
            {
                throw new PropertyNotFoundException(name);
            }

            if (path.Empty)
            {
                if (p.IsType(typeof(PropertyDomain)))
                {
                    throw new PropertyIsADomainException(name);
                }

                if (p.IsType(typeof(LazyProperty)))
                {
                    // this was loaded earlier but we couldn't create a Property out of it, possibly
                    // because required Assembly was not yet loaded. Retry creating the property.

                    // Store hidden parameter to pass it over to a new property
                    bool hidden = p.Hidden;

                    p = Property.CreateFromLazyProperty(p.Get<LazyProperty>());
                    p.SetName(name);
                    p.SetHidden(hidden);
                    mProperties.Remove(name);
                    mProperties.Add(name, p);
                }

                return p;
            }

            if (p.IsType(typeof(PropertyDomain)))
            {
                return p.Get<PropertyDomain>().Get(path);
            }

            throw new PropertyIsADomainException(name);
        }

        public void Remove(Path path)
        {
            string name = path.Pop();

            if (path.Empty)
            {
                if (!mProperties.Remove(name))
                {
                    throw new PropertyNotFoundException(name);
                }

                return;
            }

            Property p;

            if (!mProperties.TryGetValue(name, out p))
            {
                throw new PropertyNotFoundException(name);
            }

            if (!p.IsType(typeof(PropertyDomain)))
            {
                throw new PropertyNotADomainException( name);
            }

            p.Get<PropertyDomain>().Remove(path);
        }

        internal void Accept(PropertyStoreVisitor v)
        {
            v.VisitStart(this);

            foreach (KeyValuePair<string, Property> e in mProperties)
            {
                e.Value.Accept(v);
            }

            v.VisitEnd(this);
        }
    }
}
