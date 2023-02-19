using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.API;


namespace LukeBot.Twitch
{
    public class IRCClient
    {
        private Connection mConnection = null;
        private Token mToken;

        public IRCClient(string address, int port, bool useSSL)
        {
            mConnection = new Connection(address, port, useSSL);
        }

        ~IRCClient()
        {

        }

        public void Login(string nick, Token token)
        {
            mToken = token;

            mConnection.Send("PASS oauth:" + token.Get());
            mConnection.Send("NICK " + nick);
        }

        public void Close()
        {
            mConnection.Close();
        }

        public IRCMessage Receive()
        {
            string msg = mConnection.Read();
            if (msg == null)
            {
                // connection was dropped, leave
                Logger.Log().Warning("Connection dropped");
                return IRCMessage.INVALID();
            }

            return IRCMessage.Parse(msg);
        }

        public void Send(IRCMessage m)
        {
            m.FormMessageString();
            mConnection.Send(m.ToString());
        }
    }
}