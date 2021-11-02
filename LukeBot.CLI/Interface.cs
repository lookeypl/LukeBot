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

        void PreLogMessageEvent(object sender, LogMessageArgs args)
        {
            mMessageMutex.WaitOne();
            if (!mDone) {
                mPromptWritten = false;
                Console.Write('\r');
            }
        }

        void PostLogMessageEvent(object sender, LogMessageArgs args)
        {
            if (!mDone) {
                Console.Write(PROMPT);
                mPromptWritten = true;
            }
            mMessageMutex.ReleaseMutex();
        }

        void ProcessCommand(string cmd)
        {
            if (cmd == "quit")
            {
                mDone = true;
            }
        }

        public Interface()
        {
            Logger.AddPreMessageEvent(PreLogMessageEvent);
            Logger.AddPostMessageEvent(PostLogMessageEvent);
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

                    mMessageMutex.WaitOne();
                    if (response != null)
                        ProcessCommand(response);
                    mPromptWritten = false;
                    mMessageMutex.ReleaseMutex();
                }
            }
            catch (System.OperationCanceledException)
            {
                Logger.Log().Warning("CLI input cancelled");
            }
            catch (Exception e)
            {
                Logger.Log().Error("{0} caught: {1}", e.ToString(), e.Message);
            }
        }

        public void Terminate()
        {
            mDone = true;
            Utils.CancelConsoleIO();
        }
    }
}
