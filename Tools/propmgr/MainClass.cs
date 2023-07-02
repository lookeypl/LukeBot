using System.Collections.Generic;
using System.IO;
using LukeBot.Common;
using LukeBot.Logging;
using CommandLine;

namespace propmgr;

public class MainClass
{
    static CommandProcessor mProcessor = new CommandProcessor();

    private static void HandleCreateCommand(CreateCommand cmd)
    {
        mProcessor.CreatePropertyStore(cmd);
    }

    private static void HandleAddCommand(AddCommand cmd)
    {
        mProcessor.AddProperty(cmd);
    }

    private static void HandleRemoveCommand(RemoveCommand cmd)
    {
        mProcessor.RemoveProperty(cmd);
    }

    private static void HandleModifyCommand(ModifyCommand cmd)
    {
        mProcessor.ModifyProperty(cmd);
    }

    private static void HandleListCommand(ListCommand cmd)
    {
        mProcessor.ListProperties(cmd);
    }

    private static void HandleParseError(IEnumerable<Error> errs)
    {
        foreach (Error e in errs)
        {
            if (e is HelpVerbRequestedError || e is HelpRequestedError)
                continue;

            Logger.Log().Error("ERROR: {0}", e.Tag);
        }
    }

    public static void Main(string[] args)
    {
        FileUtils.SetUnifiedCWD();
        Logger.SetPreamble(false);
        Logger.SetProjectRootDir(Directory.GetCurrentDirectory());

        try
        {
            Parser.Default.ParseArguments<CreateCommand, AddCommand, RemoveCommand, ModifyCommand, ListCommand>(args)
                .WithParsed<CreateCommand>(HandleCreateCommand)
                .WithParsed<AddCommand>(HandleAddCommand)
                .WithParsed<RemoveCommand>(HandleRemoveCommand)
                .WithParsed<ModifyCommand>(HandleModifyCommand)
                .WithParsed<ListCommand>(HandleListCommand)
                .WithNotParsed(HandleParseError);
        }
        catch (System.IO.FileNotFoundException)
        {
            Logger.Log().Error("Property Store file not found.");
        }
        catch (System.Exception e)
        {
            Logger.Log().Error("Other error: {0}", e.Message);
        }
    }
}
