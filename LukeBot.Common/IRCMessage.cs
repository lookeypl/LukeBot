using System;
using System.Collections.Generic;
using System.Diagnostics;
using LukeBot.Common;


namespace LukeBot.Common
{
    public class IRCMessage
    {
        // These are kept as private fields and accessible via API

        // Original, untouched contents of the message. Useful for debugging.
        private string mMessageString;

        // Tags aka. metadata for each message sent. Consists of key-value pairs.
        // Can be empty if capability was not set or if message is missing a token
        // with '@' prefix character.
        private Dictionary<string, string> mTags;

        // Message params; most commonly for PRIVMSG is the actual message in chat.
        // For some commands can be set to empty string.
        // Does NOT contain the trailing param; this one is kept as a separate string.
        private List<string> mParams;

        // Trailing parameter which can be anything with spaces, interpreted as one string.
        private string mTrailingParam;


        // Attributes, for simpler parameters of the Message

        // Message prefix. This is 1:1 what prefix was sent minus the ':' symbol.
        // Prefix can be empty if message is missing the token beginning with ':' character.
        public string Prefix { get; set; }

        // User's nickname, part of prefix before the '!' character. Empty if message was
        // sent with only servername attached.
        public string Nick { get; set; }

        // User which sent the message in the channel, part of prefix after '!' character
        // and before '@' character. Can be empty if there was no username attached (no '!' char).
        public string User { get; set; }

        // Host which sent the message. Equal to prefix if server sent us the servername
        // part of the message; otherwise the part after "@" sign in message prefix.
        public string Host { get; set; }

        // IRC Command sent by server, or a command sent to server.
        public IRCCommand Command { get; private set; }

        // IRC Reply sent from the server in response to command sent by us.
        public IRCReply Reply { get; private set; }

        // Channel name which provided the message. Extracted from Params/ParamsString,
        // in case there is a parameter beginning with '#' character; otherwise empty.
        public string Channel { get; set; }

        // String containing tags attached to this IRC message. Parsed version is
        // available via GetTag()/GetTags()/GetTagCount() API
        public string TagStr { get; private set; }


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

            string[] tagsArray = token.Split(';');
            foreach (string t in tagsArray)
            {
                string[] tArray = t.Split('=');
                if (tArray.Length == 1)
                    tags.Add(tArray[0], "");
                else if (tArray.Length == 2)
                    tags.Add(tArray[0], tArray[1]);
                else
                    throw new InvalidOperationException("Split a key-value tag into more than 2 elements; should not happen");
            }

            return tags;
        }

        private static string ParsePrefixToken(string token, ref IRCMessage m)
        {
            int exclMarkLocation = token.IndexOf('!');
            int atLocation = token.IndexOf('@');

            if (atLocation == -1 && exclMarkLocation == -1)
            {
                // no ! or @ - it's probably just a servername
                m.Host = token;
            }
            else if (atLocation != -1 && exclMarkLocation == -1)
            {
                // there's only @ which means we have nick@host
                m.Nick = token.Substring(0, atLocation);
                m.Host = token.Substring(atLocation + 1);
            }
            else if (atLocation == -1 && exclMarkLocation != -1)
            {
                // only ! means nick!user
                m.Nick = token.Substring(0, exclMarkLocation);
                m.User = token.Substring(exclMarkLocation + 1);
            }
            else
            {
                // all of them is all of them - nick!user@host
                m.Nick = token.Substring(0, exclMarkLocation);
                m.User = token.Substring(exclMarkLocation + 1, atLocation - exclMarkLocation - 1);
                m.Host = token.Substring(atLocation + 1);
            }

            return token;
        }

        private static IRCCommand ParseCommandToken(string token, ref IRCMessage m)
        {
            switch (token)
            {
            case "CAP":         return IRCCommand.CAP;
            case "CLEARCHAT":   return IRCCommand.CLEARCHAT;
            case "CLEARMSG":    return IRCCommand.CLEARMSG;
            case "HOSTTARGET":  return IRCCommand.HOSTTARGET;
            case "JOIN":        return IRCCommand.JOIN;
            case "NICK":        return IRCCommand.NICK;
            case "NOTICE":      return IRCCommand.NOTICE;
            case "PART":        return IRCCommand.PART;
            case "PASS":        return IRCCommand.PASS;
            case "PING":        return IRCCommand.PING;
            case "PONG":        return IRCCommand.PONG;
            case "PRIVMSG":     return IRCCommand.PRIVMSG;
            case "QUIT":        return IRCCommand.QUIT;
            case "RECONNECT":   return IRCCommand.RECONNECT;
            case "ROOMSTATE":   return IRCCommand.ROOMSTATE;
            case "USER":        return IRCCommand.USER;
            case "USERNOTICE":  return IRCCommand.USERNOTICE;
            case "USERSTATE":   return IRCCommand.USERSTATE;
            default:
                throw new ParsingErrorException("Unrecognized string command: {0}; message {1}", token, m.mMessageString);
            }
        }

        // Parse received IRC message and form a Message object for further reference
        // Follows RFC 1459 23.1 + IRCv3 Message Tags extension
        public static IRCMessage Parse(string msg)
        {
            Logger.Log().Secure("Parsing IRC message: {0}", msg);

            IRCMessage m = new IRCMessage(msg);
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
                    m.TagStr = tokens[i].Substring(1);
                    m.mTags = ParseTags(m.TagStr);
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

                    m.mParams.Add(tokens[i]);

                    if (firstParam)
                        firstParam = false;

                    break;
                }
                case State.TrailingParam:
                {
                    m.mTrailingParam = string.Join(' ', tokens, i, tokens.Length - i).Substring(1);

                    i = tokens.Length; // exit parsing
                    break;
                }
                }
            }

            return m;
        }

        private string FormMessageStringInternal()
        {
            string ret = "";

            // Drop tags to string if exist
            if (mTags.Count > 0)
            {
                ret += '@';

                int tagCount = mTags.Count;
                int counter = 0;
                foreach (KeyValuePair<string, string> tag in mTags)
                {
                    // TODO validate

                    if (tag.Value.Length > 0)
                        ret += tag.Key + '=' + tag.Value;
                    else
                        ret += tag.Key;

                    counter++;
                    if (counter < tagCount)
                        ret += ';';
                }

                ret += ' ';
            }

            if (Nick.Length > 0 || User.Length > 0 || Host.Length > 0)
            {
                ret += ':';

                if (Nick.Length > 0)
                {
                    ret += Nick;

                    if (User.Length > 0)
                    {
                        ret += '!' + User;
                    }

                    if (Host.Length > 0)
                    {
                        ret += '@' + Host;
                    }
                }
                else if (User.Length > 0)
                {
                    ret += User;

                    if (Host.Length > 0)
                    {
                        ret += '@' + Host;
                    }
                }
                else if (Host.Length > 0)
                {
                    ret += Host;
                }

                ret += ' ';
            }

            if (Command == IRCCommand.REPLY)
            {
                ret += Reply;
            }
            else
            {
                ret += Command.ToString();
            }
            ret += ' ';

            if (Channel.Length > 0)
            {
                ret += '#' + Channel + ' ';
            }

            if (mParams.Count > 0)
            {
                foreach (string p in mParams)
                {
                    ret += p + ' ';
                }
            }

            if (mTrailingParam.Length > 0)
            {
                ret += ':' + mTrailingParam;
            }

            return ret;
        }


        internal Dictionary<string, string> GetTags()
        {
            return mTags;
        }

        private IRCMessage()
        {
            mMessageString = "";
            mTags = new Dictionary<string, string>();
            mParams = new List<string>();
            mTrailingParam = "";

            Nick = "";
            User = "";
            Host = "";
            Channel = "";
            TagStr = "";

            Command = IRCCommand.INVALID;
            Reply = IRCReply.INVALID;
        }

        private IRCMessage(string msg)
            : this()
        {
            mMessageString = msg;
        }

        public IRCMessage(IRCCommand cmd)
            : this()
        {
            Command = cmd;
        }

        public IRCMessage(IRCReply reply)
            : this(IRCCommand.REPLY)
        {
            Reply = reply;
        }

        public void Print(LogLevel level)
        {
            Logger.Log().Message(level, "Message data:");
            Logger.Log().Message(level, "  String: {0}", mMessageString);
            Logger.Log().Message(level, "  Tags (count {0}):", mTags.Count);
            foreach (var v in mTags)
                Logger.Log().Message(level, "   -> {0} = {1}", v.Key, v.Value);
            Logger.Log().Message(level, "  Prefix {0}", Prefix);
            Logger.Log().Message(level, "   -> Nick: {0}", Nick);
            Logger.Log().Message(level, "   -> User: {0}", User);
            Logger.Log().Message(level, "   -> Host: {0}", Host);
            Logger.Log().Message(level, "  Command {0}", Command.ToString());
            Logger.Log().Message(level, "  Params:");
            foreach (var p in mParams)
                Logger.Log().Message(level, "   -> {0}", p);
            Logger.Log().Message(level, "  Extracted params:");
            Logger.Log().Message(level, "   -> Channel: {0}", Channel);
        }

        public bool GetTag(string tag, out string value)
        {
            return mTags.TryGetValue(tag, out value);
        }

        public int GetTagCount()
        {
            return mTags.Count;
        }

        public List<string> GetParams()
        {
            return mParams;
        }

        public string GetTrailingParam()
        {
            return mTrailingParam;
        }

        public void AddTag(string t, string v)
        {
            mTags.Add(t, v);
        }

        public void AddParam(string p)
        {
            mParams.Add(p);
        }

        public void FormMessageString()
        {
            mMessageString = FormMessageStringInternal();
        }

        public void SetTrailingParam(string p)
        {
            mTrailingParam = p;
        }

        public override string ToString()
        {
            return mMessageString;
        }


        // Static helpers to create IRCMessage objects for specific common purposes

        static public IRCMessage CAPRequest(string cap)
        {
            IRCMessage m = new IRCMessage(IRCCommand.CAP);

            m.AddParam("REQ");
            m.SetTrailingParam(cap);

            return m;
        }

        static public IRCMessage PONG(string ping)
        {
            IRCMessage m = new IRCMessage(IRCCommand.PONG);

            m.SetTrailingParam(ping);

            return m;
        }

        static public IRCMessage JOIN(string channel)
        {
            IRCMessage m = new IRCMessage(IRCCommand.JOIN);

            m.Channel = channel;

            return m;
        }

        static public IRCMessage PART(string channel)
        {
            IRCMessage m = new IRCMessage(IRCCommand.PART);

            m.Channel = channel;

            return m;
        }

        static public IRCMessage PRIVMSG(string channel, string msg)
        {
            IRCMessage m = new IRCMessage(IRCCommand.PRIVMSG);

            m.Channel = channel;
            m.SetTrailingParam(msg);

            return m;
        }

        static public IRCMessage QUIT()
        {
            return new IRCMessage(IRCCommand.QUIT);
        }

        static public IRCMessage INVALID()
        {
            return new IRCMessage();
        }
    }
}
