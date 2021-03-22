using System;
using LukeBot.Common;

namespace LukeBot.CLI
{
    public class Interface
    {
        private readonly string PROMPT = "> ";

        private bool mDone = false;

        void ProcessCommand(string cmd)
        {
            if (cmd == "quit")
            {
                Console.WriteLine("Exiting");
                mDone = true;
            }
        }

        public Interface()
        {
        }

        ~Interface()
        {
        }

        public void MainLoop()
        {
            try
            {
                while (!mDone)
                {
                    Console.Write(PROMPT);
                    string response = Console.ReadLine();
                    if (response != null)
                        ProcessCommand(response);
                }
            }
            catch (System.OperationCanceledException e)
            {
                Logger.Warning("CLI input cancelled");
            }
            catch (Exception e)
            {
                Logger.Error("{0} caught: {1}", e.ToString(), e.Message);
            }
        }

        public void Terminate()
        {
            mDone = true;
            Utils.CancelConsoleIO();
        }
    }
}
