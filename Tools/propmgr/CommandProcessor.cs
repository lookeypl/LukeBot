using LukeBot.Common;
using LukeBot.Core;
using CommandLine;

namespace propmgr;

public class Command
{
    [Option('d', "dir",
        Required = false,
        Default = Constants.PROPERTY_STORE_FILE,
        HelpText = "Property Store file to use.")
    ]
    public string StoreDir { get; set; }

    public Command()
    {
        StoreDir = "";
    }
}

[Verb("create", HelpText = "Create a new empty Property Store. Overwrites old one if exists.")]
public class CreateCommand: Command
{
    public CreateCommand()
    {
    }
}

[Verb("add", HelpText = "Add a new Property to the store.")]
public class AddCommand: Command
{
    [Value(0, MetaName = "prop_name", Required = true, HelpText = "Name of property to add.")]
    public string Name { get; set; }
    [Value(1, MetaName = "prop_type", Required = true, HelpText = "Type of added property.")]
    public string Type { get; set; }
    [Value(2, MetaName = "prop_value", Required = true, HelpText = "Value of added property.")]
    public string Value { get; set; }

    public AddCommand()
    {
        Name = "";
        Type = "";
        Value = "";
    }
}

[Verb("remove", HelpText = "Remove a property.")]
public class RemoveCommand: Command
{
    [Value(0, MetaName = "prop_name", Required = true, HelpText = "Name of property to add.")]
    public string Name { get; set; }

    public RemoveCommand()
    {
        Name = "";
    }
}

[Verb("edit", HelpText = "Modify an existing property. Type must match with existing property's type.")]
public class ModifyCommand: Command
{
    [Value(0, MetaName = "prop_name", Required = true, HelpText = "Name of property to add.")]
    public string Name { get; set; }
    [Value(1, MetaName = "prop_value", Required = true, HelpText = "Value of added property.")]
    public string Value { get; set; }

    public ModifyCommand()
    {
        Name = "";
        Value = "";
    }
}

[Verb("list", HelpText = "List all existing properties in the Store.")]
public class ListCommand: Command
{
    public ListCommand()
    {
    }
}

public class CommandProcessor
{
    private PropertyStore? mStore;

    private bool OpenStore(Command cmd)
    {
        try
        {
            mStore = new PropertyStore(cmd.StoreDir);
            mStore.Load();
        }
        catch (System.IO.FileNotFoundException)
        {
            Logger.Log().Error("Property Store file {0} not found.", cmd.StoreDir);
            return false;
        }
        catch (Exception e)
        {
            Logger.Log().Error("Other error: {0}", e.Message);
            return false;
        }

        return true;
    }

    public void CreatePropertyStore(CreateCommand cmd)
    {
        Logger.Log().Info("Creating Property Store at {0}", cmd.StoreDir);
        mStore = new PropertyStore(cmd.StoreDir);
        mStore.Save();
    }

    public void AddProperty(AddCommand cmd)
    {
        Logger.Log().Info("Adding Property {0} {1} to store {2}", cmd.Type, cmd.Name, cmd.StoreDir);
        if (!OpenStore(cmd))
            return;
        mStore.Add(cmd.Name, Property.Create(cmd.Type, cmd.Value));
        mStore.Save();
    }

    public void RemoveProperty(RemoveCommand cmd)
    {
        Logger.Log().Info("Removing Property {0} from store {1}", cmd.Name, cmd.StoreDir);
        if (!OpenStore(cmd))
            return;
        mStore.Remove(cmd.Name);
        mStore.Save();
    }

    public void ModifyProperty(ModifyCommand cmd)
    {
        Logger.Log().Info("Modify Property {0} in store {1}", cmd.Name, cmd.StoreDir);
        if (!OpenStore(cmd))
            return;
        mStore.Modify(cmd.Name, cmd.Value);
        mStore.Save();
    }

    public void ListProperties(ListCommand cmd)
    {
        Logger.Log().Info("List properties in store {0}", cmd.StoreDir);
        if (!OpenStore(cmd))
            return;
        mStore.PrintDebug(LogLevel.Info);
    }

    public CommandProcessor()
    {
        mStore = null;
    }
}
