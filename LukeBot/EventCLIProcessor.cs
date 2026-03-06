using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Logging;
using LukeBot.Interface;
using LukeBot.User;
using CommandLine;
using LukeBot.Services;

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
        [Option('d', "dispatcher", Default = "", Required = false, HelpText = "Event dispatcher")]
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
        [Value(
            1, MetaName = "eventOrdinal", Default = 0, Required = false,
            HelpText = "Event to skip from the list. Check \"event status\" for list of currently executed events or omit to skip the event on top of the list"
        )]
        public int eventIdx { get; set; }
    }

    internal class EventCLIProcessor: ICLIProcessor
    {
        private const string COMMAND_NAME = "event";
        private LukeBot mLukeBot;

        private string GetDefaultDispatcher(CLIMessageProxy CLI)
        {
            return Twitch.Utils.DispatcherNameForUser(CLI.GetCurrentUser().GetUsername());
        }

        private IEventService GetEventService()
        {
            return Service.Get<IEventService>();
        }

        void HandleTestCommand(EventTestCommand args, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                // Parse args from command line into key=value tuples
                // See LukeBot.Common.Utils.ConvertArgString() for details
                IEnumerable<(string, string)> eventArgs = Utils.ConvertArgStringsToTuples(args.Args);

                GetEventService().User(CLI.GetCurrentUser().GetUsername()).TestEvent(args.Event, eventArgs);
                msg = "Test event " + args.Event + " emitted";
            }
            catch (System.Exception e)
            {
                msg = "Failed to emit a test event: " + e.Message;
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        void HandleInfoCommand(EventInfoCommand args, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                EventInfo e = GetEventService().User(CLI.GetCurrentUser().GetUsername()).GetEventInfo(args.Event);
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
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        void HandleStatusCommand(EventStatusCommand args, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                msg = "Dispatchers (name - type):\n";

                IEnumerable<EventDispatcherStatus> statuses = GetEventService().User(CLI.GetCurrentUser().GetUsername()).GetDispatcherStatuses();

                foreach (EventDispatcherStatus s in statuses)
                {
                    msg += "  " + s.Name + " - " + s.Type.ToString();

                    if (s.Type != EventDispatcherType.Immediate)
                    {
                        msg += ":\n";
                        msg += "    State: " + s.State + "\n";
                        msg += "    Events: " + s.EventInfo.Count + "\n";
                        for (int i = 0; i < s.EventInfo.Count; ++i)
                        {
                            msg += String.Format("      - {0}. {1}\n", i, s.EventInfo[i]);
                        }
                        msg += "\n";
                    }
                    else
                    {
                        msg += "\n";
                    }
                }

                msg += "Events (name - dispatcher):\n";

                IEnumerable<EventInfo> events = GetEventService().User(CLI.GetCurrentUser().GetUsername()).ListEvents();

                foreach (EventInfo e in events)
                {
                    msg += "  " + e.Name + " - " + e.Dispatcher;

                    if (e.Testable)
                        msg += ", testable";

                    msg += "\n";
                }

                msg += "\n\"Testable\" events can emit a test event using \"event test <name>\"\n";
            }
            catch (System.Exception e)
            {
                msg = "Failed to query event system status: " + e.Message;
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        void HandleClearCommand(EventClearCommand args, CLIMessageProxy CLI, out string msg)
        {
            string dispatcher = args.Dispatcher;

            try
            {
                if (dispatcher == null || dispatcher.Length == 0)
                    dispatcher = GetDefaultDispatcher(CLI);

                EventDispatcher dispatcherObject = GetEventService().User(CLI.GetCurrentUser().GetUsername()).Dispatcher(dispatcher);
                dispatcherObject.Clear();
                dispatcherObject.Skip(0);
                msg = "Events on dispatcher " + dispatcher + " cleared.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to clear " + dispatcher + " dispatcher: " + e.Message;
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        void HandleEnableCommand(EventEnableCommand args, CLIMessageProxy CLI, out string msg)
        {
            string dispatcher = args.Dispatcher;

            try
            {
                if (dispatcher == null || dispatcher.Length == 0)
                    dispatcher = GetDefaultDispatcher(CLI);

                GetEventService().User(CLI.GetCurrentUser().GetUsername()).Dispatcher(dispatcher).Enable();
                msg = "Dispatcher " + dispatcher + " enabled.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to enable " + dispatcher + " dispatcher: " + e.Message;
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        void HandleDisableCommand(EventDisableCommand args, CLIMessageProxy CLI, out string msg)
        {
            string dispatcher = args.Dispatcher;

            try
            {
                if (dispatcher == null || dispatcher.Length == 0)
                    dispatcher = GetDefaultDispatcher(CLI);

                GetEventService().User(CLI.GetCurrentUser().GetUsername()).Dispatcher(dispatcher).Disable();
                msg = "Dispatcher " + dispatcher + " disabled.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to disable " + dispatcher + " dispatcher: " + e.Message;
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        void HandleHoldCommand(EventHoldCommand args, CLIMessageProxy CLI, out string msg)
        {
            string dispatcher = args.Dispatcher;

            try
            {
                if (dispatcher == null || dispatcher.Length == 0)
                    dispatcher = GetDefaultDispatcher(CLI);

                GetEventService().User(CLI.GetCurrentUser().GetUsername()).Dispatcher(dispatcher).Hold();
                msg = "Dispatcher " + dispatcher + " put on hold.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to hold " + dispatcher + " dispatcher: " + e.Message;
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        void HandleSkipCommand(EventSkipCommand args, CLIMessageProxy CLI, out string msg)
        {
            string dispatcher = args.Dispatcher;

            try
            {
                if (dispatcher == null || dispatcher.Length == 0)
                {
                    dispatcher = GetDefaultDispatcher(CLI);
                }

                if (args.eventIdx < 0)
                {
                    msg = "Invalid Event ordinal";
                    return;
                }

                EventDispatcherStatus status = GetEventService().User(CLI.GetCurrentUser().GetUsername()).Dispatcher(dispatcher).Status();
                if (args.eventIdx >= 0 && args.eventIdx >= status.EventInfo.Count)
                {
                    msg = String.Format("Event ordinal provided is too big (maximum {0})", status.EventInfo.Count);
                    return;
                }

                GetEventService().User(CLI.GetCurrentUser().GetUsername()).Dispatcher(dispatcher).Skip(args.eventIdx);
                msg = String.Format("Dispatcher {0} event #{1} skipped.", dispatcher, args.eventIdx);
            }
            catch (System.Exception e)
            {
                msg = "Failed to skip event on " + dispatcher + " dispatcher: " + e.Message;
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;

            UserInterface.CLI.AddCommand(COMMAND_NAME, PermissionLevel.User, (CLIMessageProxy cliProxy, string[] args) =>
            {
                string result = "";

                Parser p = new Parser(with => with.HelpWriter = new CLIUtils.CLIMessageProxyTextWriter(cliProxy));
                p.ParseArguments<EventTestCommand, EventInfoCommand, EventStatusCommand, EventClearCommand, EventEnableCommand, EventDisableCommand, EventHoldCommand, EventSkipCommand>(args)
                    .WithParsed<EventTestCommand>((EventTestCommand args) => HandleTestCommand(args, cliProxy, out result))
                    .WithParsed<EventInfoCommand>((EventInfoCommand args) => HandleInfoCommand(args, cliProxy, out result))
                    .WithParsed<EventStatusCommand>((EventStatusCommand args) => HandleStatusCommand(args, cliProxy, out result))
                    .WithParsed<EventClearCommand>((EventClearCommand args) => HandleClearCommand(args, cliProxy, out result))
                    .WithParsed<EventEnableCommand>((EventEnableCommand args) => HandleEnableCommand(args, cliProxy, out result))
                    .WithParsed<EventDisableCommand>((EventDisableCommand args) => HandleDisableCommand(args, cliProxy, out result))
                    .WithParsed<EventHoldCommand>((EventHoldCommand args) => HandleHoldCommand(args, cliProxy, out result))
                    .WithParsed<EventSkipCommand>((EventSkipCommand args) => HandleSkipCommand(args, cliProxy, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, COMMAND_NAME, out result));
                return result;
            });
        }
    }
}
