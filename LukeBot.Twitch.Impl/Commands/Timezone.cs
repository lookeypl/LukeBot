using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using LukeBot.Common;
using LukeBot.Logging;
using LukeBot.Twitch.Command;

[assembly:InternalsVisibleTo("LukeBot.Tests")]


namespace LukeBot.Twitch.Impl.Command
{
    public class Timezone: ICommand
    {
        // messages, collected here for testing
        internal static readonly string MSG_TIMEZONE_PARSE_ERROR = "Something's wrong, I cannot parse this.";
        internal static readonly string MSG_TIMEZONE_FROM_PARSE_ERROR = "Invalid source time/timezone.";
        internal static readonly string MSG_TIMEZONE_TO_PARSE_ERROR = "Invalid target timezone.";
        internal static readonly string MSG_TIMEZONE_HELP_FORMAT = "\"{0}\" to find out this stream's time and timezone; \"{0} <code>\" to learn what stream's time is in the timezone of your choice; \"{0} <time> <code>\" to do a custom conversion.";

        internal static readonly string MSG_TIMEZONE_SET_FORMAT = "It's {0}";
        internal static readonly string MSG_TIMEZONE_SET_NOT_SET_FORMAT = "Stream timezone not set! Set it via \"{0} set <code>\", ex. \"{0} set cet\".";
        internal static readonly string MSG_TIMEZONE_SET_SUCCESS_FORMAT = "Current timezone set to {0}.";
        internal static readonly string MSG_TIMEZONE_SET_SUCCESS_DST_ANNOTATION = " Note that this will change the outcome based on current timezone's DST.";

        internal static readonly string MSG_TIMEZONE_CONVERT_FORMAT = "{0} is {1}.";
        internal static readonly string MSG_TIMEZONE_AMBIGUOUS_ANNOTATION_FORMAT = " Note: {0} abbreviation is ambiguous, assumed \"{1}\". If you didn't mean that, retry using one of: ";
        internal static readonly string MSG_TIMEZONE_DST_ANNOTATION_FORMAT = " Note: assumed {0} is {1} and assumed {2}. If you didn't mean that, retry with one of the following abbreviations: ";


        // below examples assume Timezone Command is added as a "!timezone" chat command
        private enum Mode
        {
            Error = 0, // something went wrong when parsing
            Current, // !timezone
            Help, // !timezone help
            SetCurrent, // !timezone set <code>
            CurrentToArg, // !timezone <code> ex. "!timezone pt" or "!timezone utc-7" or "!timezone utc-6:30"
            CustomTimeToArg, // !timezone <time> <code> ex. "!timezone 10:00 pt" or "!timezone 2:00pm pt"
            CustomTimeZoneToArg, // !timezone <time> <codefrom> <codeto> ex. "!timezone 10:00 et cest" or "!timezone 2:00pm et cest"
        }

        private static readonly Regex TIME_REGEX = new("\\d?\\d:\\d\\d([apAP][mM])?");
        private static readonly Regex ZONE_REGEX = new("([a-zA-Z]{1,5})");

        private TimeZoneShort mStreamerTimeZone = null;
        private TimeSpan mServerUTCSpan = TimeZoneInfo.Utc.BaseUtcOffset;

        public Timezone(Descriptor d)
            : base(d)
        {
            if (!String.IsNullOrEmpty(d.Value))
            {
                // TODO this also should check if that TZ is currently at this point in time in DST or not
                // and if so, we should update it automatically accordingly
                mStreamerTimeZone = TimeZoneDB.GetFromAbbreviation(d.Value);
            }
        }

        private Mode ParseMode(string[] args, out string timeFrom, out string codeFrom, out string codeTo)
        {
            // arg 0 is always command, so we ignore it
            timeFrom = "";
            codeFrom = "";
            codeTo = "";

            if (args.Length == 1) return Mode.Current;

            if (args.Length == 2)
            {
                if (args[1] == "help")
                {
                    return Mode.Help;
                }
                else
                {
                    codeTo = args[1];
                    return Mode.CurrentToArg;
                }
            }

            if (args.Length >= 3)
            {
                if (args[1] == "set")
                {
                    codeFrom = args[2];
                    return Mode.SetCurrent;
                }

                // last but not least time to determine how we convert
                // we now that last arg should be codeTo always so let's just set that
                if (args.Length == 3)
                {
                    timeFrom = args[1];
                    codeTo = args[2];
                    return Mode.CustomTimeToArg;
                }
                else
                {
                    timeFrom = args[1];
                    codeFrom = args[2];
                    codeTo = args[3];
                    return Mode.CustomTimeZoneToArg;
                }
            }

            return Mode.Error;
        }

        private TimeZoneShort ParseTZ(string tz)
        {
            return ParseTimeAndTZ(null, tz).Item2;
        }

        private (TimeOnly?, TimeZoneShort) ParseTimeAndTZ(string time, string zone)
        {
            (TimeOnly?, TimeZoneShort) ret = (null, null);

            if (!String.IsNullOrEmpty(time))
            {
                Match match = TIME_REGEX.Match(time);
                if (match.Success && !String.IsNullOrEmpty(match.Value))
                {
                    ret.Item1 = TimeOnly.Parse(match.Value);
                }
            }

            if (!String.IsNullOrEmpty(zone))
            {
                Match match = ZONE_REGEX.Match(zone);

                if (match.Success && !String.IsNullOrEmpty(match.Value))
                {
                    ret.Item2 = TimeZoneDB.TryGetFromAbbreviation(match.Value.ToUpper());
                }
            }

            return ret;
        }

        private bool HasDST(TimeZoneShort tz)
        {
            return tz.AlternativeKind == AlternativeKind.DST_ALT || tz.AlternativeKind == AlternativeKind.DST_ALIAS;
        }

        // true if dt is between from and to, false otherwise
        private bool DateTimeBetween(DateTime dt, DateTime from, DateTime to)
        {
            return (dt.CompareTo(from) >= 0) && (dt.CompareTo(to) <= 0);
        }

        private TimeZoneShort AdjustToDST(TimeZoneShort tz)
        {
            TimeZoneShort ret = tz;
            DateTime now = DateTime.Now;

            if (HasDST(tz))
            {
                TimeZoneInfo info = TimeZoneDB.GetTimeZoneInfo(tz);
                TimeZoneInfo.AdjustmentRule[] rules = info.GetAdjustmentRules();
                if (rules.Length == 0) return ret;

                foreach (TimeZoneInfo.AdjustmentRule rule in rules)
                {
                    Logger.Log().Debug("{0} adjustment start date: {1:D} end date: {2:D}", tz.Abbreviation, rule.DateStart, rule.DateEnd);
                    if (DateTimeBetween(now, rule.DateStart, rule.DateEnd))
                    {
                        if (tz.AlternativeKind == AlternativeKind.DST_ALT) return TimeZoneDB.GetFromAbbreviation(tz.Alternatives[0]);
                        else if (tz.AlternativeKind == AlternativeKind.DST_ALIAS) return TimeZoneDB.GetFromAbbreviation(tz.Alternatives[1]);
                    }

                    if (now.CompareTo(rule.DateEnd) > 0) break; // further rules are in the future
                }
            }

            if (ret.AlternativeKind == AlternativeKind.DST_ALIAS)
            {
                ret = TimeZoneDB.GetFromAbbreviation(ret.Alternatives[0]);
            }

            return ret;
        }

        private string Current()
        {
            if (mStreamerTimeZone == null)
            {
                return String.Format(MSG_TIMEZONE_SET_NOT_SET_FORMAT, mName);
            }

            return String.Format(MSG_TIMEZONE_SET_FORMAT, mStreamerTimeZone.ToString(TimeOnly.FromDateTime(DateTime.Now)));
        }

        private string SetCurrent(ChatUser callerPrivilege, TimeZoneShort tz)
        {
            if (!CheckPrivilege(callerPrivilege, ChatUser.Broadcaster | ChatUser.Moderator))
            {
                return ""; // silently exit, this user cannot do this action
            }

            if (tz == null)
            {
                return ""; // conversion failed
            }

            mStreamerTimeZone = tz;
            UpdateConfig();
            string ret = String.Format(MSG_TIMEZONE_SET_SUCCESS_FORMAT, mStreamerTimeZone.Abbreviation);
            if (HasDST(tz))
            {
                ret += MSG_TIMEZONE_SET_SUCCESS_DST_ANNOTATION;
            }

            return ret;
        }

        private string ConvertTime(TimeOnly time, TimeZoneShort from, TimeZoneShort to)
        {
            string resultMessage = "";

            bool sourceHasDST = HasDST(from);
            bool targetHasDST = HasDST(to);

            TimeZoneShort adjustedFrom = AdjustToDST(from);
            TimeZoneShort adjustedTo = AdjustToDST(to);

            TimeSpan streamerToServer = mServerUTCSpan.Subtract(adjustedFrom.UTC);
            if (streamerToServer.Duration() == TimeSpan.Zero)
            {
                TimeOnly converted = time.Add(adjustedTo.UTC);
                resultMessage = String.Format(MSG_TIMEZONE_CONVERT_FORMAT, adjustedFrom.ToString(time), adjustedTo.ToString(converted));
            }
            else
            {
                TimeOnly converted = time.Add(streamerToServer.Add(adjustedTo.UTC));
                resultMessage = String.Format(MSG_TIMEZONE_CONVERT_FORMAT, adjustedFrom.ToString(time), adjustedTo.ToString(converted));
            }

            switch (to.AlternativeKind)
            {
            case AlternativeKind.NONE:
                break;
            case AlternativeKind.AMBIGUOUS:
            {
                resultMessage += String.Format(
                    MSG_TIMEZONE_AMBIGUOUS_ANNOTATION_FORMAT,
                    to.Abbreviation, to.FullName
                );

                bool first = true;
                foreach (string alt in to.Alternatives)
                {
                    if (!first) resultMessage += ", ";
                    resultMessage += alt;
                    first = false;
                }
                break;
            }
            case AlternativeKind.DST_ALIAS:
            {
                resultMessage += String.Format(
                    MSG_TIMEZONE_DST_ANNOTATION_FORMAT,
                    to.Abbreviation, "todo", to.FullName
                );
                break;
            }
            case AlternativeKind.DST_ALT:
            {
                resultMessage += String.Format(
                    MSG_TIMEZONE_DST_ANNOTATION_FORMAT,
                    to.Abbreviation, "todo", to.FullName
                );

                bool first = true;
                foreach (string alt in to.Alternatives)
                {
                    if (!first) resultMessage += ", ";
                    resultMessage += alt;
                    first = false;
                }
                break;
            }
            }

            return resultMessage;
        }

        public override string Execute(ChatUser callerPrivilege, string[] args)
        {
            try
            {
                Logger.Log().Debug("Timezone called with args: {0}", String.Join(' ', args));
                Mode mode = ParseMode(args, out string timeFrom, out string codeFrom, out string codeTo);
                switch (mode)
                {
                case Mode.Error: return MSG_TIMEZONE_PARSE_ERROR;
                case Mode.Help: return String.Format(MSG_TIMEZONE_HELP_FORMAT, mName);
                case Mode.Current: return Current();
                case Mode.SetCurrent: return SetCurrent(callerPrivilege, ParseTZ(codeFrom));
                case Mode.CurrentToArg: return ConvertTime(TimeOnly.FromDateTime(DateTime.Now), mStreamerTimeZone, ParseTZ(codeTo));
                case Mode.CustomTimeToArg:
                case Mode.CustomTimeZoneToArg:
                {
                    // a bit extra parsing before we do this
                    (TimeOnly?, TimeZoneShort) from = ParseTimeAndTZ(timeFrom, codeFrom);
                    TimeZoneShort to = ParseTZ(codeTo);
                    if (from.Item1 == null && from.Item2 == null) return MSG_TIMEZONE_FROM_PARSE_ERROR;
                    if (to == null) return MSG_TIMEZONE_TO_PARSE_ERROR;

                    return ConvertTime(from.Item1.HasValue ? from.Item1.Value : TimeOnly.FromDateTime(DateTime.Now), from.Item2 != null ? from.Item2 : mStreamerTimeZone, to);
                }
                }
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Caught exception {0} - {1}", e.GetType().ToString(), e.Message);
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }

            return "";
        }

        public override void Edit(string newValue)
        {
            // noop
        }

        public override Descriptor ToDescriptor()
        {
            if (mStreamerTimeZone != null)
            {
                Logger.Log().Debug("Saving current timezone {0}", mStreamerTimeZone.ToString());
            }

            return new Descriptor(mName, CommandType.timezone, mPrivilegeLevel, mEnabled, mStreamerTimeZone != null ? mStreamerTimeZone.Abbreviation : "");
        }
    }
}