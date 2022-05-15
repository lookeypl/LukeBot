using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LukeBot.Common;

namespace LukeBot.Core
{
    public class PropertyStore
    {
        private PropertyDomain mRootDomain;
        private IStorageBackend mStorage;

        private Queue<string> UnwrapName(string name)
        {
            string[] split = name.Split('.');

            Queue<string> domainQueue = new Queue<string>();

            foreach (string s in split)
                domainQueue.Enqueue(s);

            return domainQueue;
        }

        public PropertyStore(string storePath)
        {
            mRootDomain = new PropertyDomain(Constants.PROPERTY_DOMAIN_ROOT);
            mStorage = new StorageBackendJSON(storePath);
        }

        ~PropertyStore()
        {
            Save();
        }

        // Takes full name in form of ex. "doma.domb.name", unwraps it and adds value to the store.
        // If value exists, returns false.
        public void Add(string name, Property v)
        {
            mRootDomain.Add(UnwrapName(name), v);
        }

        // Takes full name in form of ex. "doma.domb.name", unwraps it and returns value if found
        // If not found, empty property is returned
        public Property Get(string name)
        {
            return mRootDomain.Get(UnwrapName(name));
        }

        public void Modify<T>(string name, T value)
        {
            Get(name).Set<T>(value);
        }

        public void Remove(string name)
        {
            mRootDomain.Remove(UnwrapName(name));
        }

        public void Load()
        {
            mStorage.Load(this);
        }

        public void Save()
        {
            mStorage.Save(this);
        }

        public void PrintDebug(LogLevel level)
        {
            Traverse(new PropertyStorePrintVisitor(level));
        }

        internal void Traverse(PropertyStoreVisitor v)
        {
            mRootDomain.Accept(v);
        }
    }
}
