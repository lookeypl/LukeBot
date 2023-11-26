using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using LukeBot.Logging;


namespace LukeBot.Interface
{
    public class ServerCLI: CLI
    {
        private AutoResetEvent mCloseEvent = new(false);

        public ServerCLI()
        {
        }

        ~ServerCLI()
        {
        }

        public void AddCommand(string cmd, Command c)
        {
            // noop
        }

        public void AddCommand(string cmd, CLI.CmdDelegate d)
        {
            // noop
        }

        public void Message(string message)
        {
            // TODO this looks over-engineered, but I want to improve CLI vastly over the course
            // of some patches (ex. control the Console Buffer directly to create a pseudo-UI)
            // so it's better to use this now than later replace all Console.WriteLine()-s in
            // rest of the project
            Logger.Log().Info(message);
        }

        public bool Ask(string message)
        {
            Logger.Log().Error("ServerCLI cannot respond to questions");
            return false;
        }

        public string Query(string message)
        {
            Logger.Log().Error("ServerCLI cannot respond to queries");
            return "";
        }

        public void MainLoop()
        {
            try
            {
                Console.CancelKeyPress += delegate {
                    mCloseEvent.Set();
                };

                mCloseEvent.WaitOne();
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("{0} caught: {1}", e.ToString(), e.Message);
            }
        }

        public void Teardown()
        {
        }

        public void SetPromptPrefix(string prefix)
        {
            // noop
        }
    }
}
