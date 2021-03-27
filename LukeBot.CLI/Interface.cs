using System;
using LukeBot.Common;
using System.Threading;

namespace LukeBot.CLI
{
    public class Interface
    {
        private readonly string PROMPT = "> ";

        private bool mDone = false;
        private volatile bool mPromptWritten = false;
        private Mutex mMessageMutex = new Mutex();

        void PreLogMessageEvent(object sender, Logger.LogMessageArgs args)
        {
            mMessageMutex.WaitOne();
            if (!mDone) {
                Console.Write('\r');
                mPromptWritten = false;
            }
        }

        void PostLogMessageEvent(object sender, Logger.LogMessageArgs args)
        {
            if (!mDone) {
                Console.Write(PROMPT);
                mPromptWritten = true;
            }
            mMessageMutex.ReleaseMutex();
        }

        void ProcessCommand(string cmd)
        {
            Logger.Debug("cmd: {0}", cmd);
            if (cmd == "quit")
            {
                mDone = true;
                Console.WriteLine("Exiting");
            }
        }

        public Interface()
        {
            Logger.PreLogMessage += PreLogMessageEvent;
            Logger.PostLogMessage += PostLogMessageEvent;
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
                    mMessageMutex.WaitOne();
                    if (!mPromptWritten)
                    {
                        Console.Write(PROMPT);
                        mPromptWritten = true;
                    }
                    mMessageMutex.ReleaseMutex();

                    string response = Console.ReadLine();
                    if (response != null)
                        ProcessCommand(response);
                }
            }
            catch (System.OperationCanceledException)
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
