using System.Collections.Generic;
using LukeBot.Module;
using LukeBot.Interface;
using CommandLine;

namespace LukeBot
{
    internal class EventCommandBase
    {
        [Value(0, MetaName = "queue", Required = false, Default = "", HelpText = "Event queue")]
        public string Queue { get; set; }
    }

    [Verb("emit", HelpText = "Emit a test LukeBot event on a queue")]
    internal class EventEmitCommand: EventCommandBase
    {
        public EventEmitCommand()
        {
        }
    }

    // TODO
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

    [Verb("list", HelpText = "List available events and their statuses.")]
    internal class EventListCommand
    {
    }


    internal class EventCLIProcessor: ICLIProcessor
    {
        private const string COMMAND_NAME = "event";
        private LukeBot mLukeBot;

        void HandleEmitCommand(EventEmitCommand args, out string msg)
        {
            try
            {
                // TODO
                msg = "";
            }
            catch (System.Exception e)
            {
                msg = "Failed to emit a test event: " + e.Message;
            }
        }

        void HandleClearCommand(EventClearCommand args, out string msg)
        {
            try
            {
                // TODO
                msg = "";
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
                // TODO
                msg = "";
            }
            catch (System.Exception e)
            {
                msg = "Failed to enable an event: " + e.Message;
            }
        }

        void HandleDisableCommand(EventDisableCommand args, out string msg)
        {
            try
            {
                // TODO
                msg = "";
            }
            catch (System.Exception e)
            {
                msg = "Failed to disable an event: " + e.Message;
            }
        }

        void HandleHoldCommand(EventHoldCommand args, out string msg)
        {
            try
            {
                // TODO
                msg = "";
            }
            catch (System.Exception e)
            {
                msg = "Failed to emit a test event: " + e.Message;
            }
        }

        void HandleListCommand(EventListCommand args, out string msg)
        {
            try
            {
                // TODO
                msg = "";
            }
            catch (System.Exception e)
            {
                msg = "Failed to emit a test event: " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;

            UserInterface.CommandLine.AddCommand(COMMAND_NAME, (string[] args) =>
            {
                string result = "";
                Parser.Default.ParseArguments<EventEmitCommand, EventClearCommand, EventEnableCommand, EventDisableCommand, EventHoldCommand, EventListCommand>(args)
                    .WithParsed<EventEmitCommand>((EventEmitCommand args) => HandleEmitCommand(args, out result))
                    .WithParsed<EventClearCommand>((EventClearCommand args) => HandleClearCommand(args, out result))
                    .WithParsed<EventEnableCommand>((EventEnableCommand args) => HandleEnableCommand(args, out result))
                    .WithParsed<EventDisableCommand>((EventDisableCommand args) => HandleDisableCommand(args, out result))
                    .WithParsed<EventHoldCommand>((EventHoldCommand args) => HandleHoldCommand(args, out result))
                    .WithParsed<EventListCommand>((EventListCommand args) => HandleListCommand(args, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, COMMAND_NAME, out result));
                return result;
            });
        }
    }
}