namespace LukeBot.Config
{
    internal class LazyProperty
    {
        public string mTypeStr;
        public string mSerializedVal;

        public LazyProperty(string typeStr, string serializedVal)
        {
            mTypeStr = typeStr;
            mSerializedVal = serializedVal;
        }
    }
}