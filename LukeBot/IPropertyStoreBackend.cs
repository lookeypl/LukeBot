namespace LukeBot
{
    interface IPropertyStoreBackend
    {
        bool Load();
        void Close();

        T Read<T>(string propertyName);
        bool Write<T>(string propertyName, T property);
    }
}
