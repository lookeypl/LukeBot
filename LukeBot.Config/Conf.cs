using LukeBot.Common;


namespace LukeBot.Config
{
    public class Conf
    {
        private static PropertyStore mStore;

        public static void Initialize(string storeDir)
        {
            mStore = new PropertyStore(storeDir);
        }

        public static void Add(string name, Property p)
        {
            mStore.Add(name, p);
        }

        public static bool Exists(string name)
        {
            return mStore.Exists(name);
        }

        public static bool Exists<T>(string name)
        {
            return mStore.Exists<T>(name);
        }

        public static Property Get(string name)
        {
            return mStore.Get(name);
        }

        public static T Get<T>(string name)
        {
            return mStore.Get(name).Get<T>();
        }

        public static void Modify<T>(string name, T value)
        {
            mStore.Modify<T>(name, value);
        }

        public static void Remove(string name)
        {
            mStore.Remove(name);
        }

        public static void Save()
        {
            mStore.Save();
        }

        public static void PrintDebug(LogLevel level)
        {
            mStore.PrintDebug(level);
        }

        public static void Teardown()
        {
            Save();
            mStore = null;
        }
    }
}