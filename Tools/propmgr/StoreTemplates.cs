using LukeBot.Core;
using LukeBot.Common;

namespace propmgr;


public class EmptyStoreTemplate: StoreTemplate
{
    public override void Fill(PropertyStore _)
    {
        // empty - store is left as is
    }
}

public class DefaultStoreTemplate: StoreTemplate
{
    // We want to fill the Store with some basic values used by LukeBot.
    // Most have to be edited by user, but listing them gives a general idea what to provide.
    public override void Fill(PropertyStore store)
    {
        store.Add("lukebot.server_ip", Property.Create<string>("127.0.0.1"));
        store.Add("twitch.client_id", Property.Create<string>(""));
        store.Add("twitch.client_secret", Property.Create<string>(""));
        store.Add("spotify.client_id", Property.Create<string>(""));
        store.Add("spotify.client_secret", Property.Create<string>(""));
    }
}
