using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;


namespace LukeBot.Common
{
    public class WebSocketServer
    {
        enum ServerState
        {
            Stopped,
            Listening,
            Connecting,
            Connected,
            Stopping,
        };

        Thread mServerThread;
        TcpListener mListener;
        TcpClient mClient;
        NetworkStream mStream;
        volatile bool mDone;
        ServerState mState;
        AutoResetEvent mEvent;
        ManualResetEvent mConnectedEvent;
        Mutex mStateMutex;
        Queue<WebSocketMessage> mRecvMessageQueue;

        ServerState State
        {
            get
            {
                mStateMutex.WaitOne();
                ServerState s = mState;
                mStateMutex.ReleaseMutex();
                return mState;
            }
            set
            {
                mStateMutex.WaitOne();
                mState = value;
                mStateMutex.ReleaseMutex();

                switch (value)
                {
                case ServerState.Listening:
                    mConnectedEvent.Reset();
                    mListener.Start();
                    break;
                case ServerState.Connected:
                    mConnectedEvent.Set();
                    goto default;
                default:
                    mListener.Stop();
                    break;
                }
            }
        }

        public bool Running
        {
            get
            {
                return (State != ServerState.Stopped);
            }
        }


        private HTTPRequest ReadRequest(NetworkStream stream)
        {
            while (!mStream.DataAvailable);

            // TODO read stream with multiple chunks (while mClient.Available > 0)
            int toRead = mClient.Available;
            byte[] buffer = new byte[toRead];
            int read = stream.Read(buffer, 0, toRead);
            if (read != toRead)
            {
                Logger.Log().Warning("Read different amount of data than expected ({0} vs {1})", read, toRead);
            }

            Logger.Log().Debug("Reading done, read {0} bytes", read);
            string requestStr = Encoding.UTF8.GetString(buffer);
            Logger.Log().Secure("Request:\n{0}", requestStr);

            HTTPRequest request = HTTPRequest.Parse(requestStr);

            Logger.Log().Secure("Parsed:");
            Logger.Log().Secure("Type: {0}", request.Type);
            Logger.Log().Secure("Path: {0}", request.Path);
            Logger.Log().Secure("Version: {0}", request.Version);
            Logger.Log().Secure("Headers:");
            foreach (var h in request.Headers)
            {
                Logger.Log().Secure("  {0} = {1}", h.Key, h.Value);
            }

            return request;
        }

        private void WaitForConnection()
        {
            if (!mListener.Pending())
            {
                Thread.Sleep(1000);
                return;
            }

            mClient = mListener.AcceptTcpClient();
            mStream = mClient.GetStream();
            mState = ServerState.Connecting;
        }

        private void ProcessWSHandshake()
        {
            // TODO if its not websocket, send back 101
            HTTPRequest request = ReadRequest(mStream);

            if (!request.Headers.ContainsKey("Sec-WebSocket-Key"))
            {
                Logger.Log().Error("Received GET request doesn't have websocket handshake related fields. Dropping.");
                mClient.Close();
                mState = ServerState.Listening;
                return;
            }

            string key = request.Headers["Sec-WebSocket-Key"];
            string respKey = key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] respKeySHA1 = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(respKey));

            HTTPResponse response = HTTPResponse.FormResponse(request.Version, HttpStatusCode.SwitchingProtocols);
            response.Headers.Add("Connection", "Upgrade");
            response.Headers.Add("Upgrade", "websocket");
            response.Headers.Add("Sec-WebSocket-Accept", Convert.ToBase64String(respKeySHA1));

            string responseStr = response.GetAsString();
            Logger.Log().Debug("Response to send back:\n{0}", responseStr);

            byte[] buffer = Encoding.UTF8.GetBytes(responseStr);
            mStream.Write(buffer, 0, buffer.Length);

            State = ServerState.Connected;
        }

        private void Disconnect()
        {
            // Closing handshake
            Logger.Log().Debug("Disconnecting Server");
            mStream.Close();
            mClient.Close();
        }

        private void ThreadMain()
        {
            State = ServerState.Listening;

            while (!mDone)
            {
                switch (State)
                {
                case ServerState.Listening:
                    WaitForConnection();
                    break;
                case ServerState.Connecting:
                    ProcessWSHandshake();
                    break;
                case ServerState.Connected:
                    // here send/receive is handled by WSServer user
                    // wait until some sort of event comes up (disconnect/shutdown/etc)
                    mEvent.WaitOne();
                    break;
                }
            }

            if (State == ServerState.Listening)
                mListener.Stop();
            else
                Disconnect();

            State = ServerState.Stopped;
        }

        public WebSocketServer(string address, int port, bool useSSL = true)
            : this(IPAddress.Parse(address), port, useSSL)
        {
        }

        public WebSocketServer(IPAddress address, int port, bool useSSL = true)
        {
            mListener = new TcpListener(address, port);
            mEvent = new AutoResetEvent(false);
            mConnectedEvent = new ManualResetEvent(false);
            mRecvMessageQueue = new Queue<WebSocketMessage>();
            State = ServerState.Stopped;
        }

        public void Start()
        {
            mDone = false;
            mClient = null;
            mServerThread = new Thread(ThreadMain);
            mServerThread.Start();
        }

        public void Send(string msg)
        {
            WebSocketMessage m = new WebSocketMessage();
            m.FromString(msg);
            Send(m);
        }

        public void Send(WebSocketMessage msg)
        {
            if (State != ServerState.Connected)
            {
                Logger.Log().Warning("Message not sent to WebSocket - not connected");
                return;
            }

            byte[] data = msg.ToSendBuffer();
            mStream.Write(data, 0, data.Length);
            mStream.Flush();
        }

        private bool Fetch(out byte[] buffer)
        {
            const int CHUNK_SIZE = 1024;
            byte[] chunk = new byte[CHUNK_SIZE];
            buffer = new byte[0];

            int read = 0;
            int written = 0;
            do
            {
                read = mStream.Read(chunk, 0, CHUNK_SIZE);
                Logger.Log().Debug("Read {0}", read);
                if (read == 0)
                    break;

                Array.Resize(ref buffer, buffer.Length + read);
                Array.Copy(chunk, 0, buffer, written, read);
                written += read;
            }
            while (mStream.DataAvailable);

            return true;
        }

        public WebSocketMessage Recv()
        {
            if (State != ServerState.Connected)
            {
                Logger.Log().Warning("WebSocket server in invalid state - not connected");
                throw new InvalidOperationException("WebSocket server in invalid state - not connected");
            }

            if (mRecvMessageQueue.Count > 0)
                return mRecvMessageQueue.Dequeue();

            byte[] buffer = null;
            if (!Fetch(out buffer))
                return new WebSocketMessage();

            long total = 0;
            while (total != buffer.LongLength)
            {
                WebSocketMessage m = new WebSocketMessage();
                total += m.FromReceivedData(buffer, total);
                mRecvMessageQueue.Enqueue(m);
            }

            return mRecvMessageQueue.Dequeue();
        }

        public void AwaitConnection()
        {
            while (mState != ServerState.Connected)
                mConnectedEvent.WaitOne();
        }

        public void RequestShutdown()
        {
            Logger.Log().Debug("WebSocketServer: Requesting shutdown");
            mDone = true;
            mEvent.Set();
        }

        public void WaitForShutdown()
        {
            if (State != ServerState.Stopped)
                mServerThread.Join();
        }
    }
}
