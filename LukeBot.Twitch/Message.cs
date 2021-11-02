using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public Dictionary<string, string> Tags { get; private set; }

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

        // IRC Reply sent from the server in response to command sent by us.
        public IRCReply Reply { get; private set; }

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

        private static Dictionary<string, string> ParseTags(string token)
        {
            Dictionary<string, string> tags = new Dictionary<string, string>();

            string tagsString = token.Substring(1);
            string[] tagsArray = tagsString.Split(';');
            foreach (string t in tagsArray)
            {
                string[] tArray = t.Split('=');
                Debug.Assert(tArray.Length == 2, "Invalid amount of elements");
                tags.Add(tArray[0], tArray[1]);
            }

            return tags;
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

        private static IRCCommand ParseCommandToken(string token, ref Message m)
        {
            switch (token)
            {
            case "CAP":         return IRCCommand.CAP;
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
                throw new ParsingErrorException(String.Format("Unrecognized string command: {0}; message {1}", token, m.MessageString));
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
                    ushort code = 0;
                    if (UInt16.TryParse(tokens[i], out code))
                    {
                        m.Command = IRCCommand.REPLY;
                        IRCReply reply;
                        if (Enum.TryParse<IRCReply>(tokens[i], true, out reply))
                            m.Reply = reply;
                        else
                            m.Reply = IRCReply.INVALID;
                    }
                    else
                    {
                        m.Command = ParseCommandToken(tokens[i], ref m);
                    }
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

            return m;
        }


        public Message(string msg)
        {
            MessageString = msg;
            Tags = new Dictionary<string, string>();
            Nick = "";
            User = "";
            Host = "";
            Command = IRCCommand.INVALID;
            Params = new List<string>();
            ParamsString = "";
            Channel = "";
        }

        public void Print(LogLevel level)
        {
            Logger.Log().Message(level, "Message data:");
            Logger.Log().Message(level, "  String: {0}", MessageString);
            Logger.Log().Message(level, "  Tags (count {0}):", Tags.Count);
            foreach (var v in Tags)
                Logger.Log().Message(level, "   -> {0} = {1}", v.Key, v.Value);
            Logger.Log().Message(level, "  Prefix {0}", Prefix);
            Logger.Log().Message(level, "   -> Nick: {0}", Nick);
            Logger.Log().Message(level, "   -> User: {0}", User);
            Logger.Log().Message(level, "   -> Host: {0}", Host);
            Logger.Log().Message(level, "  Command {0}", Command.ToString());
            Logger.Log().Message(level, "  Params:");
            foreach (var p in Params)
                Logger.Log().Message(level, "   -> {0}", p);
            Logger.Log().Message(level, "  Extracted params:");
            Logger.Log().Message(level, "   -> Channel: {0}", Channel);
        }

        public override string ToString()
        {
            return MessageString;
        }
    }
}
