using LukeBot.Common;
using System;

namespace LukeBot
{
    class MainClass
    {
        static void Main(string[] args)
        {
            FileUtils.SetUnifiedCWD();

            try
            {
                LukeBot bot = new LukeBot();
                bot.Run();
            }
            catch (Exception e)
            {
                Logger.Error("Caught exception: " + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
