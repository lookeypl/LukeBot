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

            PortStatus()
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
            Debug.Assert(firstPort > lastPort, "Invalid firstPort", "First port {0} must be lower than last port {1}", firstPort, lastPort);
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

                mPorts[PortToArrayIdx(ep.Port)].taken = true;
                Logger.Debug("Marking TCP port {0} as taken by outside service", ep.Port);
            }

            foreach (IPEndPoint ep in udpEndpoints)
            {
                if (ep.Port < mFirstPort || ep.Port > mLastPort)
                    continue;

                mPorts[PortToArrayIdx(ep.Port)].taken = true;
                Logger.Debug("Marking UDP port {0} as taken by outside service", ep.Port);
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
            return mPorts[idx].taken;
        }

        public ConnectionPort AcquirePort()
        {
            int searchStartPort = mNextPort;
            while (!IsPortInUse(mNextPort))
            {
                mNextPort++;
                if (mNextPort > mLastPort)
                    mNextPort = mFirstPort;

                if (mNextPort == searchStartPort)
                    throw new NoFreePortException("Cannot find a free port");
            }

            return new ConnectionPort(mNextPort);
        }

        public void ReleasePort(int port)
        {
            mPorts[PortToArrayIdx(port)].taken = false;
        }
    }
}