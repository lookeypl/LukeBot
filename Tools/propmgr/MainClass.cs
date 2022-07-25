using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.Common;
using CommandLine;

namespace propmgr;

public class MainClass
{
    public static ProgramOptions mOptions = new ProgramOptions();
    static CommandProcessor mProcessor = new CommandProcessor();

    private static void HandleProgramOptions(ProgramOptions opts)
    {
        mOptions = opts;
    }

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
            if (e is HelpVerbRequestedError)
                continue;

            Logger.Log().Error("  {0}", e.ToString());
        }
    }

    public static void Main(string[] args)
    {
        FileUtils.SetUnifiedCWD();
        Logger.SetProjectRootDir(Directory.GetCurrentDirectory());

        Parser.Default.ParseArguments<CreateCommand, AddCommand, RemoveCommand, ModifyCommand, ListCommand>(args)
            .WithParsed<CreateCommand>(HandleCreateCommand)
            .WithParsed<AddCommand>(HandleAddCommand)
            .WithParsed<RemoveCommand>(HandleRemoveCommand)
            .WithParsed<ModifyCommand>(HandleModifyCommand)
            .WithParsed<ListCommand>(HandleListCommand)
            .WithNotParsed(HandleParseError);
    }
}
