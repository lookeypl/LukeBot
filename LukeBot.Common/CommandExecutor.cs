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
        public delegate Task CommandDelegateAsync();

        private class CommandTask
        {
            public CommandDelegate task;
            public CommandDelegateAsync asyncTask;

            public CommandTask(CommandDelegate d)
            {
                task = d;
                asyncTask = null;
            }

            public CommandTask(CommandDelegateAsync d)
            {
                asyncTask = d;
                task = null;
            }
        }

        private Thread mWorkerThread = null;
        private bool mWorkerDone = false;
        private bool mIsProcessing = false;
        private readonly object mIsProcessingLock = new();
        private ManualResetEvent mCommandAvailableEvent = new(false);
        private Queue<CommandTask> mCommandQueue = new();
        private Mutex mCommandQueueMutex = new();

        public bool IsProcessing
        {
            get
            {
                lock (mIsProcessingLock)
                {
                    return mIsProcessing;
                }
            }
        }

        private async void ThreadMain()
        {
            while (!mWorkerDone)
            {
                if (GetCommandCount() == 0)
                {
                    lock (mIsProcessingLock)
                    {
                        mIsProcessing = false;
                    }

                    mCommandAvailableEvent.WaitOne();
                    mCommandAvailableEvent.Reset();
                }

                if (mWorkerDone)
                    break;

                lock (mIsProcessingLock)
                {
                    mIsProcessing = true;
                }

                CommandTask cmd = DequeueCommand();
                if (cmd != null)
                {
                    if (cmd.task != null && cmd.asyncTask != null)
                    {
                        throw new InvalidOperationException("Both task and asyncTask were not null - should not happen");
                    }

                    try
                    {
                        if (cmd.task != null) cmd.task();
                        else if (cmd.asyncTask != null) await cmd.asyncTask();
                    }
                    catch (System.Exception e)
                    {
                        Logger.Log().Error("Caught {0} while executing a Command: {1}", e.GetType().Name, e.Message);
                    }
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
            mCommandQueue.Enqueue(new CommandTask(command));
            mCommandQueueMutex.ReleaseMutex();
        }

        private void EnqueueCommand(CommandDelegateAsync asyncCommand)
        {
            mCommandQueueMutex.WaitOne();
            mCommandQueue.Enqueue(new CommandTask(asyncCommand));
            mCommandQueueMutex.ReleaseMutex();
        }

        private CommandTask DequeueCommand()
        {
            CommandTask cmd = null;

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

        public void ExecuteAsync(CommandDelegateAsync command)
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
