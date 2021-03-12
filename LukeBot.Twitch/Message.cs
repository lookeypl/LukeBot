using System;
using System.Collections.Generic;
using LukeBot.Common;


namespace LukeBot.Twitch
{
    class Message
    {
        // Original, untouched contents of the message. Useful for debugging.
        public string MessageString { get; private set; }

        // Tags aka. metadata for each message sent. Consists of key-value pairs.
        // Can be empty if capability was not set or if message is missing a token
        // with '@' prefix character.
        public Dictionary<MessageTag, string> Tags { get; private set; }

        // Message prefix. This is 1:1 what prefix was sent minus the ':' symbol.
        // Prefix can be empty if message is missing the token beginning with ':' character.
        public string Prefix { get; private set; }

        // User's nickname, part of prefix before the '!' character. Empty if message was
        // sent with only servername attached.
        public string Nick { get; private set; }

        // User which sent the message in the channel, part of prefix after '!' character
        // and before '@' character. Can be empty if there was no username attached (no '!' char).
        public string User { get; private set; }

        // Host which sent the message. Equal to prefix if server sent us the servername
        // part of the message; otherwise the part after "@" sign in message prefix.
        public string Host { get; private set; }

        // IRC Command sent by server.
        public IRCCommand Command { get; private set; }

        // Message params; most commonly for PRIVMSG is the actual message in chat.
        // For some commands can be set to empty string
        public List<string> Params { get; private set; }

        // Params list in form of a string
        public string ParamsString { get; private set; }

        // Channel name which provided the message. Extracted from Params/ParamsString,
        // in case there is a parameter beginning with '#' character; otherwise empty.
        public string Channel { get; private set; }


        // Parser internals
        private enum State
        {
            Initial,
            Tag,
            Prefix,
            Command,
            Param,
            TrailingParam,
        }

        private static State mState;

        private static Dictionary<MessageTag, string> ParseTags(string token)
        {
            return new Dictionary<MessageTag, string>();
        }

        private static string ParsePrefixToken(string token, ref Message m)
        {
            int exclMarkLocation = token.IndexOf('!');
            int atLocation = token.IndexOf('@');
            if (atLocation == -1 && exclMarkLocation == -1)
            {
                m.Host = token;
            }
            else if (atLocation != -1 && exclMarkLocation == -1)
            {
                m.Nick = token.Substring(0, atLocation);
                m.Host = token.Substring(atLocation + 1);
            }
            else if (atLocation == -1 && exclMarkLocation != -1)
            {
                m.Nick = token.Substring(0, exclMarkLocation);
                m.User = token.Substring(exclMarkLocation + 1);
            }
            else
            {
                m.Nick = token.Substring(0, exclMarkLocation);
                m.User = token.Substring(exclMarkLocation + 1, atLocation - exclMarkLocation - 1);
                m.Host = token.Substring(atLocation + 1);
            }

            return token;
        }

        private static IRCCommand ParseCommandToken(string token)
        {
            int code = 0;
            if (Int32.TryParse(token, out code))
            {
                switch (code)
                {
                case 001: return IRCCommand.LOGIN_001;
                case 002: return IRCCommand.LOGIN_002;
                case 003: return IRCCommand.LOGIN_003;
                case 004: return IRCCommand.LOGIN_004;
                case 372: return IRCCommand.LOGIN_372;
                case 375: return IRCCommand.LOGIN_375;
                case 376: return IRCCommand.LOGIN_376;
                case 421: return IRCCommand.UNKNOWN_421;
                default:
                    return IRCCommand.UNKNOWN_NUMERIC;
                }
            }
            else
            {
                switch (token)
                {
                case "CLEARCHAT":   return IRCCommand.CLEARCHAT;
                case "CLEARMSG":    return IRCCommand.CLEARMSG;
                case "HOSTTARGET":  return IRCCommand.HOSTTARGET;
                case "JOIN":        return IRCCommand.JOIN;
                case "NOTICE":      return IRCCommand.NOTICE;
                case "PART":        return IRCCommand.PART;
                case "PING":        return IRCCommand.PING;
                case "PRIVMSG":     return IRCCommand.PRIVMSG;
                case "RECONNECT":   return IRCCommand.RECONNECT;
                case "ROOMSTATE":   return IRCCommand.ROOMSTATE;
                case "USERNOTICE":  return IRCCommand.USERNOTICE;
                case "USERSTATE":   return IRCCommand.USERSTATE;
                default:
                    throw new ParsingErrorException("Unrecognized string command");
                }
            }
        }

        // Parse received IRC message and form a Message object for further reference
        // Follows RFC 1459 23.1 + IRCv3 Message Tags extension
        public static Message Parse(string msg)
        {
            Message m = new Message(msg);
            mState = State.Initial;
            bool firstParam = true;

            string[] tokens = msg.Split(' ');
            for (int i = 0; i < tokens.Length; ++i)
            {
                char c = tokens[i][0];

                // Update parser state
                switch (c)
                {
                case '@':
                {
                    if (mState == State.Initial)
                        mState = State.Tag;
                    else
                        throw new ParsingErrorException("Unexpected tag token");

                    break;
                }
                case ':':
                {
                    if (mState == State.Initial || mState == State.Tag)
                        mState = State.Prefix;
                    else if (mState == State.Command || mState == State.Param)
                        mState = State.TrailingParam;
                    else
                        throw new ParsingErrorException("Unexpected prefix or trailing param token");

                    break;
                }
                default:
                {
                    if (mState == State.Initial || mState == State.Tag || mState == State.Prefix)
                        mState = State.Command;
                    else if (mState == State.Command)
                        mState = State.Param;
                    else if (mState == State.Param)
                        mState = State.Param;
                    else
                        throw new ParsingErrorException("Unexpected prefix or trailing param token");

                    break;
                }
                }

                // Update fields
                switch (mState)
                {
                case State.Tag:
                {
                    m.Tags = ParseTags(tokens[i].Substring(1));
                    break;
                }
                case State.Prefix:
                {
                    m.Prefix = ParsePrefixToken(tokens[i].Substring(1), ref m);
                    break;
                }
                case State.Command:
                {
                    m.Command = ParseCommandToken(tokens[i]);
                    break;
                }
                case State.Param:
                {
                    if (tokens[i][0] == '#')
                        m.Channel = tokens[i].Substring(1);

                    m.Params.Add(tokens[i]);

                    if (firstParam)
                        firstParam = false;
                    else
                        m.ParamsString += ' ';
                    m.ParamsString += tokens[i];

                    break;
                }
                case State.TrailingParam:
                {
                    string trailingParam = string.Join(' ', tokens, i, tokens.Length - i).Substring(1);
                    m.Params.Add(trailingParam);

                    if (!firstParam)
                        m.ParamsString += ' ';

                    m.ParamsString += trailingParam;

                    i = tokens.Length; // exit parsing
                    break;
                }
                }
            }

            m.Print(Logger.LogLevel.Debug);

            return m;
        }


        public Message(string msg)
        {
            MessageString = msg;
            Tags = new Dictionary<MessageTag, string>();
            Nick = "";
            User = "";
            Host = "";
            Command = IRCCommand.INVALID;
            Params = new List<string>();
            ParamsString = "";
            Channel = "";
        }

        public void Print(Logger.LogLevel level)
        {
            Logger.Log(level, "Message data:");
            Logger.Log(level, "  Tags (count {0}):", Tags.Count);
            foreach (var v in Tags)
                Logger.Log(level, "   -> {0} = {1}", v.Key, v.Value);
            Logger.Log(level, "  Prefix {0}", Prefix);
            Logger.Log(level, "   -> Nick: {0}", Nick);
            Logger.Log(level, "   -> User: {0}", User);
            Logger.Log(level, "   -> Host: {0}", Host);
            Logger.Log(level, "  Command {0}", Command.ToString());
            Logger.Log(level, "  Params:");
            foreach (var p in Params)
                Logger.Log(level, "   -> {0}", p);
            Logger.Log(level, "  Extracted params:");
            Logger.Log(level, "   -> Channel: {0}", Channel);
        }

        public override string ToString()
        {
            return MessageString;
        }
    }
}
