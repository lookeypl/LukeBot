using System.Collections.Generic;
using LukeBot.Common;

namespace LukeBot.Config
{
    public class PropertyStore
    {
        internal const string PROP_STORE_DOMAIN_ROOT = "root";
        private const string PROP_STORE_METADATA_DOMAIN = "store";
        private const string PROP_STORE_VERSION_PROP_NAME = "version";
        private readonly string PROP_STORE_VERSION_PROP = Utils.FormConfName(PROP_STORE_METADATA_DOMAIN, PROP_STORE_VERSION_PROP_NAME);
        private const int PROP_STORE_FILE_VERSION = 1;

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

        private void Load()
        {
            mStorage.Load(this);
            ValidateMetadata();
        }

        private void FillStoreMetadata()
        {
            Add(PROP_STORE_VERSION_PROP, Property.Create<int>(PROP_STORE_FILE_VERSION));
        }

        private void ValidateMetadata()
        {
            // validate if we have supported version of store file loaded
            int storeVer = Get(PROP_STORE_VERSION_PROP).Get<int>();
            if (storeVer != PROP_STORE_FILE_VERSION)
            {
                throw new PropertyFileInvalidException("Unsupported version of Store loaded: {0}", storeVer);
            }
        }

        public PropertyStore(string storePath)
        {
            mRootDomain = new PropertyDomain(PROP_STORE_DOMAIN_ROOT);
            mStorage = new StorageBackendJSON(storePath);

            if (FileUtils.Exists(storePath))
            {
                Load();
            }
            else
            {
                FillStoreMetadata();
            }
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
