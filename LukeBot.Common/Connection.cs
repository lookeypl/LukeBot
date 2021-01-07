using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace LukeBot.Common
{
    public class Connection
    {
        private TcpClient mClient;
        private SslStream mSSLStream;
        private StreamReader mInput;
        private StreamWriter mOutput;
        private bool mUseSSL;

        public Connection(string address, int port, bool useSSL = false)
        {
            mUseSSL = useSSL;

            // Establish TCP connection to ip/port
            mClient = new TcpClient(address, port);
            Stream stream = mClient.GetStream();

            if (useSSL)
            {
                mSSLStream = new SslStream(mClient.GetStream(), false);
                mSSLStream.AuthenticateAsClient(address);
                stream = mSSLStream;
            }

            mInput = new StreamReader(stream);
            mOutput = new StreamWriter(stream);
        }

        public void Send(string msg)
        {
            if ((mClient == null) || (!mClient.Connected))
            {
                Logger.Error("Connection not established - cannot send message");
                return;
            }

            Logger.Debug("Send: {0}", msg);
            mOutput.WriteLine(msg);
            mOutput.Flush();
        }

        public string Read()
        {
            if ((mClient == null) || (!mClient.Connected))
            {
                return "Connection not established - cannot read message";
            }

            try
            {
                return mInput.ReadLine();
            }
            catch (System.Exception ex)
            {
                return "Error while reading message: " + ex.Message;
            }
        }
    }
}
