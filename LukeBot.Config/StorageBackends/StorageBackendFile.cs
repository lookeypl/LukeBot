using System.Collections.Generic;


namespace LukeBot.Config
{
    class StorageBackendFile: IStorageBackend
    {
        public StorageBackendFile(string path)
            : base(path)
        {
        }

        public override void Load(PropertyStore store)
        {

        }

        public override void Save(PropertyStore store)
        {

        }

        public override void Update(Queue<string> path, Property p)
        {
            throw new System.NotImplementedException();
        }
    }
}
