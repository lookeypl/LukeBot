using System.Collections.Generic;
using CommandLine;


namespace LukeBot
{
    public static class CLIUtils
    {
        public static void HandleCLIError(IEnumerable<Error> errs, string command, out string msg)
        {
            msg = "";

            bool otherErrorsExist = false;
            foreach (Error e in errs)
            {
                if (e is HelpVerbRequestedError || e is HelpRequestedError || e is NoVerbSelectedError)
                    continue;

                otherErrorsExist = true;
            }

            if (otherErrorsExist)
            {
                msg = "Error while parsing " + command + " command.";
            }
        }
    }
}