using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.Logging;

namespace LukeBot.Common
{
    // Delegates executing commands to its own separate worker thread
    public class CommandExecutor: IDisposable
    {
        public delegate void CommandDelegate();

        Thread mWorkerThread = null;
        bool mWorkerDone = false;
        ManualResetEvent mCommandAvailableEvent = new(false);
        Queue<CommandDelegate> mCommandQueue = new();
        Mutex mCommandQueueMutex = new();

        private void ThreadMain()
        {
            while (!mWorkerDone)
            {
                if (GetCommandCount() == 0)
                {
                    mCommandAvailableEvent.WaitOne();
                    mCommandAvailableEvent.Reset();
                }

                if (mWorkerDone)
                    break;

                CommandDelegate cmd = DequeueCommand();
                if (cmd != null)
                {
                    cmd();
                }
            }
        }

        private int GetCommandCount()
        {
            mCommandQueueMutex.WaitOne();
            int count = mCommandQueue.Count;
            mCommandQueueMutex.ReleaseMutex();

            return count;
        }

        private void EnqueueCommand(CommandDelegate command)
        {
            mCommandQueueMutex.WaitOne();
            mCommandQueue.Enqueue(command);
            mCommandQueueMutex.ReleaseMutex();
        }

        private CommandDelegate DequeueCommand()
        {
            CommandDelegate cmd = null;

            mCommandQueueMutex.WaitOne();

            if (mCommandQueue.Count > 0)
            {
                cmd = mCommandQueue.Dequeue();
            }

            mCommandQueueMutex.ReleaseMutex();

            return cmd;
        }

        public CommandExecutor()
        {
            mWorkerThread = new(ThreadMain);
            mWorkerThread.Name = "Command Executor Worker Thread";
            mWorkerThread.Start();
        }

        public void Execute(CommandDelegate command)
        {
            EnqueueCommand(command);
            mCommandAvailableEvent.Set();
        }

        public void Dispose()
        {
            Logger.Log().Debug("Dispose; Thread state = {0}", mWorkerThread.ThreadState);
            mWorkerDone = true;
            mCommandAvailableEvent.Set();
            mWorkerThread.Interrupt();
            mWorkerThread.Join();
        }
    }
}
