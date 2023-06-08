using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.API;


namespace LukeBot.Twitch
{
    public class IRCClient
    {
        private string mAddress = null;
        private int mPort = 0;
        private bool mUseSSL = false;
        private Connection mConnection = null;
        private Token mToken;

        public bool Connected { get; private set; }

        public IRCClient(string address, int port, bool useSSL)
        {
            mAddress = address;
            mPort = port;
            mUseSSL = useSSL;
            Connected = false;
        }

        ~IRCClient()
        {
        }

        public void Login(string nick, Token token)
        {
            mToken = token;

            if (mConnection != null)
                Close();

            mConnection = new Connection(mAddress, mPort, mUseSSL);
            mConnection.Send("PASS oauth:" + token.Get());
            mConnection.Send("NICK " + nick);

            Connected = true;
        }

        public void Close()
        {
            mConnection.Close();
            mConnection = null;
            Connected = false;
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