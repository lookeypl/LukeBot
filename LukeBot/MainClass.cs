using LukeBot.Common;
using System;
using System.IO;


namespace LukeBot
{
    class MainClass
    {
        static void Main(string[] args)
        {
            FileUtils.SetUnifiedCWD();
            Logger.SetProjectRootDir(Directory.GetCurrentDirectory());

            try
            {
                LukeBot bot = new LukeBot();
                bot.Run(args);
            }
            catch (Exception e)
            {
                Logger.Log().Error("Caught exception: " + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
