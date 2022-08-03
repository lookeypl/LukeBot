using LukeBot.Config;

namespace propmgr;

public abstract class StoreTemplate
{
    public abstract void Fill(PropertyStore store);

    public static StoreTemplate Select(string template)
    {
        switch (template)
        {
            case "empty": return new EmptyStoreTemplate();
            case "default": return new DefaultStoreTemplate();
            default:
                throw new System.ArgumentException(string.Format("Unknown Store template selected: {0}", template));
        }
    }
}