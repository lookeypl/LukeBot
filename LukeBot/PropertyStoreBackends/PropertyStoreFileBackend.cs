namespace LukeBot
{
    class PropertyStoreFileBackend : IPropertyStoreBackend
    {
        public PropertyStoreFileBackend()
        {
            
        }

        public bool Load()
        {
            throw new System.NotImplementedException();
        }

        public void Close()
        {
            throw new System.NotImplementedException();
        }

        public T Read<T>(string propertyName)
        {
            throw new System.NotImplementedException();
        }

        public bool Write<T>(string propertyName, T property)
        {
            throw new System.NotImplementedException();
        }
    }
}
