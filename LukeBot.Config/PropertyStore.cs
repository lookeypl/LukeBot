using System.IO;
using LukeBot.Logging;

namespace LukeBot.Config
{
    public class PropertyStore
    {
        internal const string PROP_STORE_DOMAIN_ROOT = "root";
        private const string PROP_STORE_METADATA_DOMAIN = "store";
        private const string PROP_STORE_VERSION_PROP_NAME = "version";
        private readonly Path PROP_STORE_VERSION_PROP = Path.Form(PROP_STORE_METADATA_DOMAIN, PROP_STORE_VERSION_PROP_NAME);
        private const int PROP_STORE_FILE_VERSION = 1;

        private PropertyDomain mRootDomain;
        private IStorageBackend mStorage;

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
                throw new PropertyFileUnsupportedException(storeVer);
            }
        }

        public PropertyStore(string storePath)
        {
            mRootDomain = new PropertyDomain(PROP_STORE_DOMAIN_ROOT);
            mStorage = new StorageBackendJSON(storePath);

            if (Directory.Exists(storePath) || File.Exists(storePath))
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
            mRootDomain = null;
            mStorage = null;
        }

        // Add a new property to the Store.
        // Takes full name in form of ex. "doma.domb.name", unwraps it and adds value to the store.
        // If adding fails (ex. value exists) throws an appropriate Exception.
        public void Add(Path p, Property v)
        {
            mRootDomain.Add(p.Copy(), v);
        }

        // Get a Property from the Store.
        // Takes full name in form of ex. "doma.domb.name", unwraps it and returns value if found
        // If not found, or any other error occurs, an appropriate Exception is thrown.
        public Property Get(Path p)
        {
            return mRootDomain.Get(p.Copy());
        }

        // Exception-less check if given Property or PropertyDomain exist.
        // Takes full name in form of ex. "doma.domb.name", unwraps it and returns value if found
        // If property is found (even if it's a domain containing more properties) returns true.
        // Otherwise returns false. Does not throw any Exception.
        public bool Exists(Path p)
        {
            return mRootDomain.Exists(p.Copy());
        }

        // Exception-less check if given Property exists and is of specific type.
        // Takes full name in form of ex. "doma.domb.name", unwraps it and returns value if found
        // If property is found (even if it's a domain containing more properties) returns true.
        // Otherwise returns false. Does not throw any Exception.
        public bool Exists<T>(Path p)
        {
            return mRootDomain.Exists<T>(p.Copy());
        }

        public void Modify<T>(Path p, T value)
        {
            // Get copies the Path, we don't have to do it here
            Get(p).Set<T>(value);
        }

        public void Remove(Path p)
        {
            mRootDomain.Remove(p.Copy());
        }

        public void Save()
        {
            mStorage.Save(this);
        }

        public void PrintDebug(LogLevel level)
        {
            Traverse(new PropertyStorePrintVisitor(level));
        }

        public void PrintDebug(LogLevel level, bool showHidden)
        {
            Traverse(new PropertyStorePrintVisitor(level, showHidden));
        }

        public void Clear()
        {
            mRootDomain = new PropertyDomain(PROP_STORE_DOMAIN_ROOT);
            FillStoreMetadata();
        }

        internal void Traverse(PropertyStoreVisitor v)
        {
            mRootDomain.Accept(v);
        }
    }
}
