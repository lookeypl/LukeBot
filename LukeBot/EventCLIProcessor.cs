using System;
using System.Collections.Generic;
using LukeBot.Communication;
using LukeBot.Logging;
using LukeBot.Module;
using LukeBot.Interface;
using CommandLine;

namespace LukeBot
{
    [Verb("test", HelpText = "Emit a test LukeBot event on a queue")]
    internal class EventTestCommand
    {
        [Value(0, MetaName = "eventName", Required = true, HelpText = "Event name")]
        public string Event { get; set; }

        [Value(1)]
        public IEnumerable<string> Args { get; set; }
    }

    [Verb("status", HelpText = "List available events and their statuses.")]
    internal class EventStatusCommand
    {
    }

    [Verb("info", HelpText = "Provide detailed information about selected event")]
    internal class EventInfoCommand
    {
        [Value(0, MetaName = "eventName", Required = true, HelpText = "EventName")]
        public string Event { get; set; }
    }


    internal class EventCommandBase
    {
        [Value(0, MetaName = "dispatcher", Required = true, HelpText = "Event dispatcher")]
        public string Dispatcher { get; set; }
    }

    // below verbs all inherit from EventCommandBase
    [Verb("clear", HelpText = "Clear an event queue. Any not-emitted events on the queue will get discarded.")]
    internal class EventClearCommand: EventCommandBase
    {
    }

    [Verb("enable", HelpText = "Enable an event queue after disabling or holding.")]
    internal class EventEnableCommand: EventCommandBase
    {
    }

    [Verb("disable", HelpText = "Disable an event queue. Incoming events will be discarded.")]
    internal class EventDisableCommand: EventCommandBase
    {
    }

    [Verb("hold", HelpText = "Hold an event queue. Events will be queued until the queue is re-enabled via \"event enable\".")]
    internal class EventHoldCommand: EventCommandBase
    {
    }

    [Verb("skip", HelpText = "Skip currently handled event from the Queue.")]
    internal class EventSkipCommand: EventCommandBase
    {
    }

    internal class EventCLIProcessor: ICLIProcessor
    {
        private const string COMMAND_NAME = "event";
        private LukeBot mLukeBot;

        private IEnumerable<(string attrib, string value)> ConvertArgString(IEnumerable<string> argsList)
        {
            List<(string attrib, string value)> ret = new();

            string a = "", v = "";
            bool readingString = false;
            foreach (string s in argsList)
            {
                if (readingString)
                {
                    if (s.EndsWith('"'))
                    {
                        readingString = false;
                        v += ' ' + s.Substring(0, s.Length - 1);
                        ret.Add((a, v));
                    }
                    else
                    {
                        v += ' ' + s;
                    }

                    continue;
                }

                string[] tokens = s.Split('=');
                if (tokens.Length != 2)
                {
                    throw new ArgumentException("Failed to parse test event attributes");
                }

                a = tokens[0];

                if (tokens[1].StartsWith('"'))
                {
                    v = tokens[1].Substring(1);
                    readingString = true;
                }
                else
                {
                    v = tokens[1];
                    ret.Add((a, v));
                }
            }

            return ret;
        }

        void HandleTestCommand(EventTestCommand args, out string msg)
        {
            try
            {
                // Parse args from command line into key=value tuples
                // Notable parsing details:
                //  - Key always has to be a string without spaces
                //  - There must be no spaces surrounding the = sign, so always <key>=<value>
                //  - Longer strings with spaces are allowed if put in quotation marks
                //  - No escape characters are supported (yet) (TODO?)
                // Following args list is valid:
                //  Tier=2 Message="This is a message!" User=username
                // Produces three tuples (all strings):
                //  ("Tier", "2")
                //  ("Message", "This is a message!")
                //  ("User", "username")
                // TestEvent() will further parse the data for correctness against Event's
                // TestArgs list, if available.
                IEnumerable<(string, string)> eventArgs = ConvertArgString(args.Args);

                Comms.Event.User(mLukeBot.GetCurrentUser().Username).TestEvent(args.Event, eventArgs);
                msg = "Test event " + args.Event + " emitted";
            }
            catch (System.Exception e)
            {
                msg = "Failed to emit a test event: " + e.Message;
            }
        }

        void HandleInfoCommand(EventInfoCommand args, out string msg)
        {
            try
            {
                EventInfo e = Comms.Event.User(mLukeBot.GetCurrentUser().Username).GetEventInfo(args.Event);
                msg = e.Name + " event:\n";
                msg += "  " + e.Description + "\n";
                msg += "\n";
                msg += "Dispatcher: " + e.Dispatcher + "\n";
                msg += "Testable: " + e.Testable + "\n";
                if (e.Testable)
                {
                    msg += "\n";
                    msg += "Available test parameters:\n";
                    foreach (EventTestParam param in e.TestParams)
                    {
                        msg += "\\_ [" + param.Type.ToString() + "] \"" + param.Name + "\" - " + param.Description + "\n";
                    }
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed to list information about event: " + e.Message;
            }
        }

        void HandleStatusCommand(EventStatusCommand args, out string msg)
        {
            try
            {
                // TODO
                msg = "Dispatchers (name - type):\n";

                IEnumerable<EventDispatcherStatus> statuses = Comms.Event.User(mLukeBot.GetCurrentUser().Username).GetDispatcherStatuses();

                foreach (EventDispatcherStatus s in statuses)
                {
                    msg += "  " + s.Name + " - " + s.Type.ToString();

                    if (s.Type == EventDispatcherType.Queued)
                    {
                        msg += ":\n";
                        msg += "    State: " + s.State + "\n";
                        msg += "    Events: " + s.EventCount + "\n";
                        msg += "\n";
                    }
                    else
                    {
                        msg += "\n";
                    }
                }

                msg += "Events (name - dispatcher):\n";

                IEnumerable<EventInfo> events = Comms.Event.User(mLukeBot.GetCurrentUser().Username).ListEvents();

                foreach (EventInfo e in events)
                {
                    msg += "  " + e.Name + " - " + e.Dispatcher;

                    if (e.Testable)
                        msg += ", testable";

                    msg += "\n";
                }

                msg += "\n\"Testable\" events can emit a test event using \"event emit <name>\"\n";
            }
            catch (System.Exception e)
            {
                msg = "Failed to query event system status: " + e.Message;
            }
        }

        void HandleClearCommand(EventClearCommand args, out string msg)
        {
            try
            {
                Comms.Event.User(mLukeBot.GetCurrentUser().Username).Dispatcher(args.Dispatcher).Clear();
                msg = "Events on dispatcher " + args.Dispatcher + " cleared.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to emit a test event: " + e.Message;
            }
        }

        void HandleEnableCommand(EventEnableCommand args, out string msg)
        {
            try
            {
                Comms.Event.User(mLukeBot.GetCurrentUser().Username).Dispatcher(args.Dispatcher).Enable();
                msg = "Dispatcher " + args.Dispatcher + " enabled.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to enable an event dispatcher: " + e.Message;
            }
        }

        void HandleDisableCommand(EventDisableCommand args, out string msg)
        {
            try
            {
                Comms.Event.User(mLukeBot.GetCurrentUser().Username).Dispatcher(args.Dispatcher).Disable();
                msg = "Dispatcher " + args.Dispatcher + " disabled.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to disable an event dispatcher: " + e.Message;
            }
        }

        void HandleHoldCommand(EventHoldCommand args, out string msg)
        {
            try
            {
                Comms.Event.User(mLukeBot.GetCurrentUser().Username).Dispatcher(args.Dispatcher).Hold();
                msg = "Dispatcher " + args.Dispatcher + " put on hold.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to hold " + args.Dispatcher + " dispatcher: " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;

            UserInterface.CommandLine.AddCommand(COMMAND_NAME, (string[] args) =>
            {
                string result = "";
                Parser.Default.ParseArguments<EventTestCommand, EventInfoCommand, EventStatusCommand, EventClearCommand, EventEnableCommand, EventDisableCommand, EventHoldCommand>(args)
                    .WithParsed<EventTestCommand>((EventTestCommand args) => HandleTestCommand(args, out result))
                    .WithParsed<EventInfoCommand>((EventInfoCommand args) => HandleInfoCommand(args, out result))
                    .WithParsed<EventStatusCommand>((EventStatusCommand args) => HandleStatusCommand(args, out result))
                    .WithParsed<EventClearCommand>((EventClearCommand args) => HandleClearCommand(args, out result))
                    .WithParsed<EventEnableCommand>((EventEnableCommand args) => HandleEnableCommand(args, out result))
                    .WithParsed<EventDisableCommand>((EventDisableCommand args) => HandleDisableCommand(args, out result))
                    .WithParsed<EventHoldCommand>((EventHoldCommand args) => HandleHoldCommand(args, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, COMMAND_NAME, out result));
                return result;
            });
        }
    }
}