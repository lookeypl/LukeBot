using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using LukeBot.Logging;


namespace LukeBot.Interface
{
    public class CLI
    {
        private enum State
        {
            INIT = 0,
            NEED_PROMPT,
            PROMPT,
            COMMAND,
            LOG,
            DONE,
        };

        public delegate string CmdDelegate(string[] args);

        private static CLI mInstance = null;
        private static readonly object mLock = new();

        private readonly string PROMPT = "> ";

        private State mState = State.INIT;
        private Mutex mMessageMutex = new Mutex();
        private Dictionary<string, Command> mCommands = new Dictionary<string, Command>();
        private string mPostCommandMessage = "";
        private string mSelectedUser = "";


        public static CLI Instance
        {
            get
            {
                lock (mLock)
                {
                    if (mInstance == null)
                        mInstance = new CLI();

                    return mInstance;
                }
            }
        }


        private void PreLogMessageEvent(object sender, LogMessageArgs args)
        {
            mMessageMutex.WaitOne();

            if (mState == State.PROMPT)
            {
                mState = State.LOG;
                Console.Write('\r');
            }
        }

        private void PostLogMessageEvent(object sender, LogMessageArgs args)
        {
            if (mState == State.LOG)
            {
                mState = State.NEED_PROMPT;
                WritePrompt();
            }

            mMessageMutex.ReleaseMutex();
        }

        private void ProcessCommand(string cmd)
        {
            if (cmd == "quit")
            {
                mState = State.DONE;
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

        // Ensure this call is done only inside mMessageMutex
        private void WritePrompt()
        {
            if (mState == State.NEED_PROMPT)
            {
                Console.Write(mSelectedUser + PROMPT);
                mState = State.PROMPT;
            }
        }

        public CLI()
        {
            Logger.AddPreMessageEvent(PreLogMessageEvent);
            Logger.AddPostMessageEvent(PostLogMessageEvent);

            AddCommand("echo", new EchoCommand());
        }

        ~CLI()
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

        // To be used inside CLI commands to query user for a yes/no choice
        public void Message(string message)
        {
            if (mState != State.COMMAND)
            {
                Logger.Log().Error("This call can only be used in the middle of CLI Command execution");
                return;
            }

            // TODO this looks over-engineered, but I want to improve CLI vastly over the course
            // of some patches (ex. control the Console Buffer directly to create a pseudo-UI)
            // so it's better to use this now than later replace all Console.WriteLine()-s in
            // rest of the project
            Console.WriteLine(message);
        }

        /**
         * Ask a simple yes/no question. Returns true if user responded "y", false if user
         * responded "n".
         */
        public bool Ask(string message)
        {
            if (mState != State.COMMAND)
            {
                Logger.Log().Error("This call can only be used in the middle of CLI Command execution");
                return false;
            }

            string response = "";
            while (response != "y" && response != "n")
            {
                Console.Write(message + "(y/n): ");
                response = Console.ReadLine();

                if (response != "y" && response != "n")
                    Console.WriteLine("Invalid response: " + response);
            }

            return (response == "y");
        }

        /**
         * Query user for a specific answer. Returns 1:1 what user typed in.
         */
        public string Query(string message)
        {
            if (mState != State.COMMAND)
            {
                Logger.Log().Error("This call can only be used in the middle of CLI Command execution");
                return "";
            }

            Console.Write(message + ": ");
            return Console.ReadLine();
        }

        public void SaveSelectedUser(string username)
        {
            mSelectedUser = username;
        }

        public string GetSelectedUser()
        {
            return mSelectedUser;
        }

        public void MainLoop()
        {
            try
            {
                while (mState != State.DONE)
                {
                    mMessageMutex.WaitOne();

                    if (mPostCommandMessage.Length > 0)
                    {
                        Console.WriteLine(mPostCommandMessage);
                        mPostCommandMessage = "";
                    }

                    mState = State.NEED_PROMPT;
                    WritePrompt();

                    mMessageMutex.ReleaseMutex();


                    string response = Console.ReadLine();


                    mMessageMutex.WaitOne();

                    if (response != null)
                        mState = State.COMMAND;

                    mMessageMutex.ReleaseMutex();

                    if (mState == State.COMMAND)
                        ProcessCommand(response);
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

        public void Teardown()
        {
            mState = State.DONE;
            Utils.CancelConsoleIO();
        }
    }
}
