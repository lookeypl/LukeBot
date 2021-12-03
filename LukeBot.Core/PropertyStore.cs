using System;
using System.Collections;
using System.Text;
using System.IO;
using LukeBot.Common;

namespace LukeBot
{
    class PropertyStore
    {
        private string mPath;
        private Hashtable mStore;

        void Create()
        {
        }

        void Load()
        {
        }

        void Save()
        {
        }

        public PropertyStore(string storePath)
        {
            mPath = storePath;

            if (File.Exists(mPath))
                Load();
            else
                Create();
        }

        ~PropertyStore()
        {
            Save();
        }

        public void Add(string name, string value)
        {
            // ...
        }
    }
}
