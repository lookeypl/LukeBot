using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using LukeBot.Common;


namespace LukeBot.CLI
{
    public class Interface
    {
        public delegate string CmdDelegate(string[] args);

        private readonly string PROMPT = "> ";

        private bool mDone = false;
        private bool mPromptWritten = false;
        private Mutex mMessageMutex = new Mutex();
        private Dictionary<string, Command> mCommands = new Dictionary<string, Command>();
        private string mPostCommandMessage = "";


        void PreLogMessageEvent(object sender, LogMessageArgs args)
        {
            mMessageMutex.WaitOne();

            if (!mDone)
            {
                mPromptWritten = false;
                Console.Write('\r');
            }
        }

        void PostLogMessageEvent(object sender, LogMessageArgs args)
        {
            if (!mDone)
            {
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

            Command c;
            string[] cmdTokens = cmd.Split(' ');
            if (!mCommands.TryGetValue(cmdTokens[0], out c))
            {
                mPostCommandMessage = "Command invalid - " + cmd;
                return;
            }

            mPostCommandMessage = c.Execute(cmdTokens.Skip(1).ToArray());
        }

        public Interface()
        {
            Logger.AddPreMessageEvent(PreLogMessageEvent);
            Logger.AddPostMessageEvent(PostLogMessageEvent);

            AddCommand("echo", new EchoCommand());
        }

        ~Interface()
        {
        }

        public void AddCommand(string cmd, Command c)
        {
            if (!mCommands.TryAdd(cmd, c))
            {
                Logger.Log().Error("Failed to add command - " + cmd + " already exists");
            }
        }

        public void AddCommand(string cmd, CmdDelegate d)
        {
            AddCommand(cmd, new LambdaCommand(d));
        }

        public void MainLoop()
        {
            try
            {
                while (!mDone)
                {
                    mMessageMutex.WaitOne();

                    if (mPostCommandMessage.Length > 0)
                    {
                        Console.Write('\r');
                        Console.WriteLine(mPostCommandMessage);
                        mPostCommandMessage = "";
                        mPromptWritten = false;
                    }

                    if (!mPromptWritten)
                    {
                        Console.Write(PROMPT);
                        mPromptWritten = true;
                    }

                    mMessageMutex.ReleaseMutex();


                    string response = Console.ReadLine();


                    mMessageMutex.WaitOne();

                    mPromptWritten = false;

                    if (response != null)
                    {
                        ProcessCommand(response);
                    }

                    mMessageMutex.ReleaseMutex();
                }
            }
            catch (System.OperationCanceledException)
            {
                Logger.Log().Warning("CLI input cancelled");
            }
            catch (System.Exception e)
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
