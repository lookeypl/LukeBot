using System.Threading;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;

namespace LukeBot.Common
{
    public class ConnectionManager
    {
        private static readonly Lazy<ConnectionManager> mInstance =
            new Lazy<ConnectionManager>(() => new ConnectionManager(50000, 65535));
        public static ConnectionManager Instance { get { return mInstance.Value; } }

        private class PortStatus
        {
            public bool taken;

            public PortStatus()
            {
                taken = false;
            }
        }

        private Mutex mMutex = new Mutex();
        private int mFirstPort;
        private int mLastPort;
        private int mNextPort;
        private PortStatus[] mPorts;
        private IPGlobalProperties mIPProperties;

        private ConnectionManager(int firstPort, int lastPort)
        {
            Debug.Assert(firstPort < lastPort, "Invalid firstPort", "First port {0} must be lower than last port {1}", firstPort, lastPort);
            Debug.Assert(lastPort <= 65535, "Available port range too high", "Available ports cannot exceed 65535 (requested {0})", lastPort);

            mFirstPort = firstPort;
            mLastPort = lastPort;
            mNextPort = mFirstPort;
            mPorts = new PortStatus[mLastPort - mFirstPort + 1];
            mIPProperties = IPGlobalProperties.GetIPGlobalProperties();

            IPEndPoint[] tcpEndpoints = mIPProperties.GetActiveTcpListeners();
            IPEndPoint[] udpEndpoints = mIPProperties.GetActiveUdpListeners();

            foreach (IPEndPoint ep in tcpEndpoints)
            {
                if (ep.Port < mFirstPort || ep.Port > mLastPort)
                    continue;

                int idx = PortToArrayIdx(ep.Port);
                mPorts[idx] = new PortStatus();
                mPorts[idx].taken = true;
                Logger.Log().Secure("Marking TCP port {0} as taken by outside service", ep.Port);
            }

            foreach (IPEndPoint ep in udpEndpoints)
            {
                if (ep.Port < mFirstPort || ep.Port > mLastPort)
                    continue;

                int idx = PortToArrayIdx(ep.Port);
                mPorts[idx] = new PortStatus();
                mPorts[idx].taken = true;
                Logger.Log().Secure("Marking UDP port {0} as taken by outside service", ep.Port);
            }
        }

        ~ConnectionManager()
        {
        }

        private int PortToArrayIdx(int port)
        {
            return port - mFirstPort;
        }

        private bool IsPortInUse(int port)
        {
            int idx = PortToArrayIdx(port);
            Debug.Assert((idx >= 0) && (idx <= mPorts.Length), "Invalid port array index");
            if (mPorts[idx] == null)
                return false;

            return mPorts[idx].taken;
        }

        public ConnectionPort AcquirePort()
        {
            mMutex.WaitOne();

            int searchStartPort = mNextPort;
            while (IsPortInUse(mNextPort))
            {
                mNextPort++;
                if (mNextPort > mLastPort)
                    mNextPort = mFirstPort;

                if (mNextPort == searchStartPort)
                    throw new NoFreePortException("Cannot find a free port");
            }

            int idx = PortToArrayIdx(mNextPort);
            mPorts[idx] = new PortStatus();
            mPorts[idx].taken = true;

            mMutex.ReleaseMutex();

            return new ConnectionPort(mNextPort);
        }

        public void ReleasePort(int port)
        {
            mMutex.WaitOne();

            Debug.Assert(mPorts[PortToArrayIdx(port)] != null, "Tried to release not yet reached port");
            Debug.Assert(mPorts[PortToArrayIdx(port)].taken == true, "Tried to release freed port");
            mPorts[PortToArrayIdx(port)].taken = false;

            mMutex.ReleaseMutex();
        }
    }
}