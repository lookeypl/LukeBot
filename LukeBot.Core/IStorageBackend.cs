using System.Collections.Generic;


namespace LukeBot.Core
{
    public abstract class IStorageBackend
    {
        protected string mPath;

        public IStorageBackend(string path)
        {
            mPath = path;
        }

        public abstract void Load(PropertyStore store);
        public abstract void Save(PropertyStore store);
        public abstract void Update(Queue<string> path, Property p);
    }
}