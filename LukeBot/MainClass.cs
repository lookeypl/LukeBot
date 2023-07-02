using LukeBot.Common;
using LukeBot.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;


namespace LukeBot
{
    class MainClass
    {
        public static ProgramOptions mProgOpts;

        public static void HandleProgramOptions(ProgramOptions opts)
        {
            mProgOpts = opts;
        }

        public static void HandleParsingError(IEnumerable<Error> errs)
        {
            foreach (Error e in errs)
            {
                if (e is HelpVerbRequestedError || e is HelpRequestedError)
                    continue;

                Logger.Log().Error("ERROR: {0}", e.Tag);
            }
        }

        static void Main(string[] args)
        {
            FileUtils.SetUnifiedCWD();
            Logger.SetProjectRootDir(Directory.GetCurrentDirectory());

            Parser.Default.ParseArguments<ProgramOptions>(args)
                .WithParsed<ProgramOptions>(HandleProgramOptions)
                .WithNotParsed(HandleParsingError);

            try
            {
                LukeBot bot = new LukeBot();
                bot.Run(mProgOpts);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Caught exception: " + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
