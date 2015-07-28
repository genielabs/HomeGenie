using System.Collections;
using System.Linq;


namespace MIG.Utility
{
    //
    // Networking class from CodeProject example
    // http://www.codeproject.com/Articles/25467/Networking-in-Silverlight-and-WPF-or-How-to-Make-t
    //

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Net.Sockets;
    using System.IO;
    using System.Threading;
    using System.Net;
    using System.Diagnostics;


    /// <summary>
    /// Abstract NetworkChannel base class
    /// </summary>
    public abstract class NetworkChannel
    {
        /// <summary>
        /// This event occures on channel connection
        /// </summary>
        public event EventHandler ChannelConnected;

        protected void FireChannelConnected()
        {
            if (ChannelConnected != null) ChannelConnected(this, EventArgs.Empty);
        }

        protected string m_ChannelName;

        /// <summary>
        /// This event occures on channel disconnection
        /// </summary>
        public event EventHandler ChannelDisconnected;

        protected void FireChannelDisconnected()
        {
            if (ChannelDisconnected != null) ChannelDisconnected(this, EventArgs.Empty);
        }

        internal NetworkChannel(string channelName)
        {
            m_ChannelName = channelName;
        }

        /// <summary>
        /// Network channel destructor
        /// </summary>
        ~NetworkChannel()
        {
            Stop();
        }

        internal abstract bool SendData(Socket socket, byte[] data);

        protected abstract void Stop();

        protected Socket m_mainSocket = null;

        public string Name { get { return m_ChannelName; } }

        public bool IsListening
        {
            get
            {
                return (m_mainSocket != null && m_mainSocket.IsBound);
            }
        }

        /// <summary>
        /// This function is used to disconnect channel socket.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                Stop();
            }
            catch (Exception e)
            {
                FireExceptionOccured(e);
            }
        }


        private AsyncCallback m_pfnCallBack;

        abstract protected void dataReceivedCallback(IAsyncResult asyn);

        protected void WaitForData(int bufferSize, Socket socket)
        {
            if (m_pfnCallBack == null) m_pfnCallBack = new AsyncCallback(dataReceivedCallback);
            SocketPacket theSocPkt = new SocketPacket(socket, bufferSize);

            socket.BeginReceive(theSocPkt.dataBuffer, 0,
                theSocPkt.dataBuffer.Length,
                SocketFlags.None,
                m_pfnCallBack,
                theSocPkt);
        }

        /// <summary>
        /// This event occures on any exception 
        /// </summary>
        public event ErrorEventHandler ExceptionOccurred;

        protected void FireExceptionOccured(Exception e)
        {
            if (ExceptionOccurred != null)
                //Log Event
                ExceptionOccurred(this, new ErrorEventArgs(e));
        }

    }

    /// <summary>
    /// Provides data for ServerConnection events
    /// </summary>
    public class ServerConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_ClientId"></param>
        internal ServerConnectionEventArgs(int _ClientId)
        {
            m_ClientId = _ClientId;
        }

        /// <summary>
        /// Provides ID of currently connected/disconnected client
        /// </summary>
        public int ClientId
        {
            get { return m_ClientId; }
        }

        int m_ClientId;
    }

    /// <summary>
    /// Provides data for channel data events
    /// </summary>
    public class DataEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_Data"></param>
        /// <param name="_DataLength"></param>
        internal DataEventArgs(byte[] _Data, int _DataLength)
        {
            m_Data = _Data;
            m_DataLength = _DataLength;
        }

        byte[] m_Data;

        /// <summary>
        /// Gets the sent/received data byte array
        /// </summary>
        public byte[] Data
        {
            get { return m_Data; }
        }

        int m_DataLength;

        /// <summary>
        /// Gets the actual data buffer length. Note that it can be less than Data.Length
        /// </summary>
        public int DataLength
        {
            get { return m_DataLength; }
        }
    }

    /// <summary>
    /// Provides data for server data events
    /// </summary>
    public class ServerDataEventArgs : DataEventArgs
    {
        internal ServerDataEventArgs(byte[] _Data, int _DataLength, int _ClientId) : base(_Data, _DataLength)
        {
            m_ClientId = _ClientId;
        }

        int m_ClientId;

        /// <summary>
        /// Gets the remote client ID, that sent/received the data
        /// </summary>
        public int ClientId
        {
            get { return m_ClientId; }
        }
    }

    /// <summary>
    /// Represents the method that will handle the 
    /// ChannelClientConnected/ChannelClientDisconnected of a TcpServerChannel
    /// </summary>
    public delegate void ServerConnectionEventHandler(object sender, ServerConnectionEventArgs args);
    /// <summary>
    /// Represents the method that will handle the 
    /// DataReceived/DataSent of a TcpServerChannel
    /// </summary>
    public delegate void ServerDataEventHandler(object sender, ServerDataEventArgs args);
    /// <summary>
    /// Represents the method that will handle the 
    /// DataReceived/DataSent of a TcpClientChannel and UdpChannel
    /// </summary>
    public delegate void DataEventHandler(object sender, DataEventArgs args);

    /// <summary>
    /// This class is managing TCP and UDP Channel allocations
    /// </summary>
    public class NetworkConnectivity
    {
        private NetworkConnectivity()
        {
        }

        #region Configuration

        internal static int BufferSize = 1492;

        #endregion

        static internal List<NetworkChannel> m_Channels = new List<NetworkChannel>();

        /// <summary>
        /// This function is used for creation of TCP server channel.
        /// </summary>
        /// <returns>TCP Server Channel created by Network Connectivity</returns>


        /// <summary>
        /// This function is used for creation of UDP channel.
        /// </summary>
        /// <returns>UDP Channel created by Network Connectivity</returns>
        public static UdpChannel CreateUdpChannel(string channelName)
        {
            UdpChannel uc = new UdpChannel(channelName);
            m_Channels.Add(uc);

            return uc;
        }

        /// <summary>
        /// This function is used for creation of TCP server channel.
        /// </summary>
        /// <returns>TCP Server Channel created by Network Connectivity</returns>
        public static TcpServerChannel CreateTcpServerChannel(string channelName)
        {
            TcpServerChannel ch = new TcpServerChannel(channelName);
            m_Channels.Add(ch);
            return ch;
        }

        /// <summary>
        /// This function is used for creation of TCP client channel.
        /// </summary>
        /// <returns>TCP Client Channel created by Network Connectivity</returns>
        public static TcpClientChannel CreateTcpClientChannel(string channelName)
        {
            TcpClientChannel ch = new TcpClientChannel(channelName);
            m_Channels.Add(ch);

            return ch;
        }

        public static void Close()
        {
            writer.Close();
        }

        internal static NETWriter writer
        {
            get { return NETWriter.GetNETWriter(); }
        }
    }

    internal class NETWriter
    {
        ManualResetEvent exitEvent = new ManualResetEvent(false);
        static NETWriter m_netwriter = null;
        Thread m_thread = null;


        internal static NETWriter GetNETWriter()
        {
            if (m_netwriter == null)
            {
                m_netwriter = new NETWriter();
                m_netwriter.Init();
            }


            return m_netwriter;
        }

        private void Init()
        {
            if (m_thread == null)
            {
                m_thread = new Thread(new ThreadStart(NetWrite));
                m_thread.Name = "NetWriter";
                m_thread.Start();
            }

        }

        internal void Close()
        {
            exitEvent.Set();
            if (m_thread != null)
            {
                m_thread.Join();
                m_thread = null;
            }
        }

        private NETWriter()
        {
            m_MsgQueue = new WaitableQueue<NetQueuedMessage>();
        }

        private WaitableQueue<NetQueuedMessage> m_MsgQueue;


        internal void Write(NetworkChannel channel, Socket socket, byte[] data)
        {
            m_MsgQueue.Enqueue(new NetQueuedMessage(channel, socket, data));
        }

        private bool rWrite(NetworkChannel channel, Socket socket, byte[] data)
        {
            if (socket == null) return false;

            if (socket.SocketType == SocketType.Stream && !socket.Connected) return false;

            return channel.SendData(socket, data);
        }

        private void NetWrite()
        {
            WaitHandle[] wait = new WaitHandle[] {
                exitEvent,
                m_MsgQueue
            };

            bool bCont = true;
            while (bCont)
            {
                int res = WaitHandle.WaitAny(wait);
                switch (res)
                {
                case 0:
                    bCont = false;

                    for (int i = 0; i < NetworkConnectivity.m_Channels.Count; i++)
                    {
                        NetworkChannel ch = (NetworkChannel)NetworkConnectivity.m_Channels[ i ];
                        if (ch != null) ch.Disconnect();
                    }
                    NetworkConnectivity.m_Channels.Clear();
                    break;
                case 1:
                    NetQueuedMessage ntcpm = m_MsgQueue.Dequeue(100) as NetQueuedMessage;
                    if (ntcpm != null && ntcpm.m_Socket != null) rWrite(ntcpm.m_Channel, ntcpm.m_Socket, ntcpm.Data);
                    break;
                }
            }

            exitEvent.Reset();
        }
    }

    internal class NetQueuedMessage
    {
        internal Socket m_Socket = null;
        internal NetworkChannel m_Channel = null;
        internal byte[] Data;

        internal NetQueuedMessage(NetworkChannel channel, Socket socket, byte[] data)
        {
            m_Socket = socket;
            m_Channel = channel;
            Data = data;
        }
    }

    internal class SocketPacket
    {
        private SocketPacket(Socket socket)
        {
            m_currentSocket = socket;
            dataBuffer = new byte[NetworkConnectivity.BufferSize];
        }

        internal SocketPacket(Socket socket, int bufferSize)
        {
            m_currentSocket = socket;
            dataBuffer = new byte[bufferSize];
        }

        private SocketPacket()
        {
        }

        internal Socket m_currentSocket;
        internal byte[] dataBuffer = new byte[NetworkConnectivity.BufferSize];
    }

    /// <summary>
    /// Provides an implementation for a UDP channel.
    /// </summary>
    public class UdpChannel : NetworkChannel
    {
        #region Constructor

        internal UdpChannel(string _ChannelName) : base(_ChannelName)
        {
        }

        #endregion

        IPEndPoint EPCast = null;

        #region Events

        /// <summary>
        /// This event occures when asynchronious receive operation is completed
        /// </summary>
        public event DataEventHandler DataReceived;

        /// <summary>
        /// This event occures when asynchronious send operation is completed
        /// </summary>
        public event DataEventHandler DataSent;


        #endregion

        #region Connection

        /// <summary>
        /// This function is used to initialize UDP channel multicast to Cast Group Ip with certain port.
        /// </summary>
        /// <param name="castGroupIp">Multicast group ip to join</param>
        /// <param name="port">Port to use for connection</param>
        public void Connect(string castGroupIp, int port)
        {
            try
            {
                lock (this)
                {
                    if (m_mainSocket == null)
                    {
                        m_mainSocket = new Socket(AddressFamily.InterNetwork,
                            SocketType.Dgram,
                            ProtocolType.Udp);

                        IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, port);//TBD: We can also choose IP to listen.

                        m_mainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

                        m_mainSocket.Bind(ipLocal);
                    }
                    IPAddress ip = IPAddress.Parse(castGroupIp);

                    EPCast = new IPEndPoint(ip, port);
                    m_mainSocket.SetSocketOption(
                        SocketOptionLevel.IP,
                        SocketOptionName.AddMembership,
                        new MulticastOption(
                            ip,
                            IPAddress.Any
                        )
                    );
                }

                FireChannelConnected();
            }
            catch (SocketException sex)
            {
                FireExceptionOccured(sex);
            }
            catch (Exception ex)
            {
                FireExceptionOccured(ex);
            }
        }

        #endregion

        #region Disconnection

        protected override void Stop()
        {
            try
            {
                lock (this)
                {
                    if (m_mainSocket != null)
                    {
                        m_mainSocket.Close();
                        m_mainSocket = null;
                        //NetworkConnectivity.writer.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                FireChannelDisconnected();
            }
        }

        #endregion

        #region Send

        /// <summary>
        /// This function is used to start asyncronious sending operation to remote machine
        /// </summary>
        /// <param name="data">Buffer to send</param>
        public void Send(byte[] data)
        {
            if (data == null) return;

            lock (this)
            {
                if (m_mainSocket != null) NetworkConnectivity.writer.Write(this, m_mainSocket, data);
            }
        }

        internal override bool SendData(Socket socket, byte[] data)
        {
            try
            {
                int res = 0;
                lock (this)
                {
                    if (m_mainSocket == null)
                    {
                        FireChannelDisconnected();
                        return false;
                    }

                    res = socket.SendTo(data, EPCast);
                }

                if (DataSent != null) DataSent(this, new DataEventArgs(data, res));

                return true;
            }
            catch (SocketException sex)
            {
                FireExceptionOccured(sex);
            }
            catch (Exception ex)
            {
                FireExceptionOccured(ex);
            }

            return false;
        }

        #endregion

        #region Receive

        /// <summary>
        /// This function is used to start asyncronious receive operation from remote machine
        /// </summary>
        /// <param name="dataSize">Sze of buffer to receive</param>
        public void Receive(int dataSize)
        {
            try
            {
                WaitForData(dataSize, m_mainSocket);
            }
            catch (SocketException sex)
            {
                FireExceptionOccured(sex);
            }
            catch (Exception ex)
            {
                FireExceptionOccured(ex);
            }
        }

        override protected void dataReceivedCallback(IAsyncResult asyn)
        {
            SocketPacket socketData = (SocketPacket)asyn.AsyncState;
            int res = socketData.m_currentSocket.EndReceive(asyn);
            if (res <= 0)
            {
                Stop();
            }
            else
            {
                try
                {
                    lock (this)
                    {
                        if (m_mainSocket != null)
                        {
                            if (DataReceived != null)
                                DataReceived(this, new DataEventArgs(socketData.dataBuffer, res));
                        }
                    }
                }
                catch (SocketException sex)
                {
                    FireExceptionOccured(sex);
                }
                catch (Exception ex)
                {
                    //LOG: General Exception
                    FireExceptionOccured(ex);
                }
            }
        }

        #endregion


        /// <summary>
        /// Returns a string representation of an UdpChannel object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string ret = "";
            ret = "UDP Channel " + EPCast.Address.ToString() + ":" + EPCast.Port.ToString();
            return ret;
        }
    }

    /// <summary>
    /// Provides an implementation for a server TCP channel.
    /// </summary>
    public class TcpServerChannel : NetworkChannel
    {
        #region Configurations

        private static int backLog = 100;

        #endregion

        internal TcpServerChannel(string _ChannelName) : base(_ChannelName)
        {
        }

        #region Events

        /// <summary>
        /// This event occures on server accepted connection from client
        /// </summary>
        public event ServerConnectionEventHandler ChannelClientConnected;

        /// <summary>
        /// This event occures on client disconnected from server
        /// </summary>
        public event ServerConnectionEventHandler ChannelClientDisconnected;
        /// <summary>
        /// This event occures when asynchronious receive operation is completed
        /// </summary>
        public event ServerDataEventHandler DataReceived;

        /// <summary>
        /// This event occures when asynchronious send operation is completed
        /// </summary>
        public event ServerDataEventHandler DataSent;

        #endregion

        private Hashtable m_workerSocketList = new Hashtable();

        #region Global active socket list

        public Hashtable SocketList
        {
            get { return m_workerSocketList; }
        }

        #endregion

        #region Connection

        /// <summary>
        /// Gets a value indicating whether a specific client is connected to the server
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <returns>true if the client is connected as of the most recent operation; otherwise, false</returns>
        public bool Connected(int clientId)
        {
            if (m_mainSocket == null || !m_mainSocket.Connected) return false;

            if (!m_workerSocketList.ContainsKey(clientId)) return false;

            Socket socket = (Socket)m_workerSocketList[ clientId ];

            return socket.Connected;
        }

        /// <summary>
        /// This function is used by server channel to listen for connections on certain port.
        /// </summary>
        /// <param name="port">Port to use for listen</param>
        public void Connect(int port)
        {
            try
            {
                lock (this)
                {
                    if (m_mainSocket == null)
                    {
                        m_mainSocket = new Socket(AddressFamily.InterNetwork,
                            SocketType.Stream,
                            ProtocolType.Tcp);
                        IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, port);//TBD: We can also choose IP to listen.
                        m_mainSocket.Bind(ipLocal);
                        m_mainSocket.Listen(backLog);
                        m_mainSocket.BeginAccept(new AsyncCallback(acceptCallback), null);
                    }
                }
                this.BoundPort = port;
            }
            catch (SocketException sex)
            {
                //Log Event
                FireExceptionOccured(sex);
            }
            catch (Exception ex)
            {
                FireExceptionOccured(ex);
            }
        }

        public int BoundPort { get; internal set; }

        private void acceptCallback(IAsyncResult asyn)
        {
            try
            {
                Socket workerSocket = m_mainSocket.EndAccept(asyn);
                m_workerSocketList.Add(workerSocket.GetHashCode(), workerSocket);

                if (ChannelClientConnected != null) ChannelClientConnected(this, new ServerConnectionEventArgs(workerSocket.GetHashCode()));

                //TBD: Send welcome message

                //TDB: Multiclient?
                m_mainSocket.BeginAccept(new AsyncCallback(acceptCallback), null);
            }
            catch (ObjectDisposedException ode)
            {
                //Console.WriteLine(ode.ObjectName);
                //Console.WriteLine(ode.Message);

                FireExceptionOccured(ode);
            }
            catch (SocketException sex)
            {
                //Console.WriteLine(sex.Message);
                //Console.WriteLine(sex.StackTrace);
                //LOG: Socket Exception
                FireExceptionOccured(sex);
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                //Console.WriteLine(ex.StackTrace);
                //LOG: General Exception
                FireExceptionOccured(ex);
            }
        }

        #endregion

        #region Disconnection

        /// <summary>
        /// This function is used by server channel to disconnect specific client.
        /// </summary>
        /// <param name="clientId">Client ID</param>
        public void DisconnectClient(int clientId)
        {
            if (m_mainSocket == null || !m_mainSocket.Connected) return;

            if (!m_workerSocketList.ContainsKey(clientId)) return;

            try
            {
                Socket socket = (Socket)m_workerSocketList[ clientId ];
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                m_workerSocketList.Remove(clientId);
                if (ChannelClientDisconnected != null) ChannelClientDisconnected(this, new ServerConnectionEventArgs(clientId));
            }
            catch (Exception ex)
            {
                FireExceptionOccured(ex);
            }
        }

        /// <summary>
        /// Method is used do disconnect all connected clients from the server and to close 
        /// the main listening socket.
        /// </summary>
        protected override void Stop()
        {
            foreach (System.Collections.DictionaryEntry de in m_workerSocketList)
            {
                int id = (int)de.Key;
                try
                {
                    lock (this)
                    {
                        Socket socket = (Socket)de.Value;
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                        //NetworkConnectivity.writer.Close();
                    }
                    if (ChannelClientDisconnected != null) ChannelClientDisconnected(this, new ServerConnectionEventArgs(id));
                }
                catch (Exception ex)
                {
                    FireExceptionOccured(ex);
                }
            }
            if (m_mainSocket != null)
            {
                m_mainSocket.Close();
                FireChannelDisconnected();
            }
        }

        #endregion

        #region Clients

        public List<Socket> Clients
        {
            get
            {
                return m_workerSocketList.Values.OfType<Socket>().ToList();
            }
        }

        #endregion

        #region Send

        /// <summary>
        /// This function is used to start asyncronious sending operation 
        /// to specific remote client by its ID
        /// </summary>
        /// <param name="data">Buffer to send</param>
        /// <param name="clientId">remote Client ID</param>
        /// <returns>true, if the client exists; otherwise, false </returns>
        public bool Send(byte[] data, int clientId)
        {
            if (!m_workerSocketList.ContainsKey(clientId)) return false;
            if (data == null) return false;

            lock (this)
            {
                Socket socket = (Socket)m_workerSocketList[ clientId ];
                NetworkConnectivity.writer.Write(this, socket, data);
            }
            return true;
        }

        public bool SendAll(byte[] data)
        {
            bool resp = true;
            foreach (System.Collections.DictionaryEntry de in m_workerSocketList)
            {
                resp &= Send(data, (int)de.Key);
            }
            return resp;
        }

        internal override bool SendData(Socket socket, byte[] data)
        {
            try
            {
                if (m_workerSocketList.Contains(socket.GetHashCode()))
                {
                    int res = -1;
                    lock (this)
                    {
                        res = socket.Send(data);
                    }
                    if (DataSent != null) DataSent(this, new ServerDataEventArgs(data, res, socket.GetHashCode()));
                }

                return true;
            }
            catch (SocketException sex)
            {
                if (!socket.Connected)
                {
                    if (ChannelClientDisconnected != null) ChannelClientDisconnected(this, new ServerConnectionEventArgs(socket.GetHashCode()));
                    m_workerSocketList.Remove(socket.GetHashCode());
                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                    }
                    catch
                    {
                    }
                }
                else FireExceptionOccured(sex);
            }
            catch (Exception ex)
            {
                FireExceptionOccured(ex);
            }

            return false;
        }

        #endregion

        #region Receive

        /// <summary>
        /// This function is used to start asynchronious receive operation from remote client 
        /// by its ID
        /// </summary>
        /// <param name="dataSize">The size of buffer to receive</param>
        /// <param name="clientId">remote Client ID</param>
        /// <returns>true, if the client exists; otherwise, false</returns>
        public bool Receive(int dataSize, int clientId)
        {
            if (m_workerSocketList.ContainsKey(clientId))
            {
                Socket socket = (Socket)m_workerSocketList[ clientId ];

                try
                {
                    WaitForData(dataSize, socket);
                    return true;
                }
                catch (SocketException sex)
                {
                    if (!socket.Connected)
                    {
                        m_workerSocketList.Remove(socket.GetHashCode());
                        if (ChannelClientDisconnected != null) ChannelClientDisconnected(this, new ServerConnectionEventArgs(socket.GetHashCode()));
                        try
                        {
                            socket.Shutdown(SocketShutdown.Both);
                            socket.Close();
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        FireExceptionOccured(sex);
                    }
                }
                catch (Exception ex)
                {
                    FireExceptionOccured(ex);
                }
            }

            return false;
        }


        override protected void dataReceivedCallback(IAsyncResult asyn)
        {
            SocketPacket socketData = (SocketPacket)asyn.AsyncState;
            int res = socketData.m_currentSocket.EndReceive(asyn);
            if (res <= 0)
            {
                DisconnectClient(socketData.m_currentSocket.GetHashCode());
            }
            else
            {
                try
                {
                    lock (this)
                    {
                        if (DataReceived != null)
                            DataReceived(this, new ServerDataEventArgs(socketData.dataBuffer, res, socketData.m_currentSocket.GetHashCode()));
                    }
                }
                catch (ObjectDisposedException ode)
                {
                    //Console.WriteLine(ode.ObjectName);
                    //Console.WriteLine(ode.Message);
                    FireExceptionOccured(ode);
                }
                catch (SocketException sex)
                {
                    if (sex.ErrorCode == 10054) // Error code for Connection reset by peer
                    {
                        m_workerSocketList.Remove(socketData.m_currentSocket);
                        if (ChannelClientDisconnected != null)
                            ChannelClientDisconnected(
                                this,
                                new ServerConnectionEventArgs(socketData.m_currentSocket.GetHashCode())
                            );
                    }
                    else
                    {
                        //LOG: Socket Exception
                        //Console.WriteLine(sex.Message);
                        //Console.WriteLine(sex.StackTrace);
                        FireExceptionOccured(sex);
                    }
                }
                catch (Exception e)
                {
                    //LOG: General Exception
                    FireExceptionOccured(e);
                }
            }
        }

        #endregion

        /// <summary>
        /// Returns a string representation of an TcpServerChannel object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string ret = "";
            ret = "TCP Channel ";
            ret += "server ";
            if (m_mainSocket.Connected) ret += "Connected. " + m_mainSocket.LocalEndPoint.ToString();
            return ret;
        }
    }

    /// <summary>
    /// Provides an implementation for a client TCP channel.
    /// </summary>
    public class TcpClientChannel : NetworkChannel
    {
        internal TcpClientChannel(string _ChannelName) : base(_ChannelName)
        {
        }

        #region Events

        /// <summary>
        /// This event occures when asynchronious receive operation is completed
        /// </summary>
        public event DataEventHandler DataReceived;

        /// <summary>
        /// This event occures when asynchronious send operation is completed
        /// </summary>
        public event DataEventHandler DataSent;

        #endregion

        #region Connection

        /// <summary>
        /// Gets a value indicating whether a channel is connected to a remote host
        /// </summary>
        public bool Connected
        {
            get
            {
                if (m_mainSocket == null) return false;

                return m_mainSocket.Connected;
            }
        }

        /// <summary>
        /// This function is used to connect TCP client channel to remote ip with certain port.
        /// </summary>
        /// <param name="ip">Server IP string</param>
        /// <param name="port">Server Port</param>
        public void Connect(string ip, int port)
        {
            try
            {
                lock (this)
                {
                    if (m_mainSocket == null) m_mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint ipEnd = new IPEndPoint(IPAddress.Parse(ip), port);
                    // Connect to the remote host
                    m_mainSocket.BeginConnect(ipEnd, new AsyncCallback(connectedCallback), m_mainSocket);
                }
            }
            catch (SocketException sex)
            {
                FireExceptionOccured(sex);
            }
            catch (Exception ex)
            {
                FireExceptionOccured(ex);
            }
        }

        private void connectedCallback(IAsyncResult asyn)
        {
            try
            {
                Socket socket = (Socket)asyn.AsyncState;
                socket.EndConnect(asyn);

                if (socket.Connected) FireChannelConnected();
            }
            catch (SocketException sex)
            {
                FireExceptionOccured(sex);
            }
            catch (Exception ex)
            {
                FireExceptionOccured(ex);
            }
        }

        #endregion

        #region Disconnection

        protected override void Stop()
        {
            lock (this)
            {
                if (m_mainSocket != null)
                {
                    try
                    {
                        m_mainSocket.Shutdown(SocketShutdown.Both);
                        m_mainSocket.Close();
                    }
                    catch
                    {

                    }
                    m_mainSocket = null;
                }
            }
            FireChannelDisconnected();
        }

        #endregion

        #region Send

        /// <summary>
        /// This function is used to start asyncronious sending operation to remote machine
        /// </summary>
        /// <param name="data">Buffer to send</param>
        /// <returns>true if main socket is connected; otherwise false</returns>
        public bool Send(byte[] data)
        {
            if (m_mainSocket == null || !m_mainSocket.Connected) return false;
            if (data == null) return false;

            lock (this)
            {
                NetworkConnectivity.writer.Write(this, m_mainSocket, data);
            }
            return true;
        }

        internal override bool SendData(Socket socket, byte[] data)
        {
            try
            {
                if (m_mainSocket != null && m_mainSocket == socket)
                {
                    int res = -1;
                    lock (this)
                    {
                        res = m_mainSocket.Send(data);
                    }
                    if (DataSent != null) DataSent(this, new DataEventArgs(data, res));

                    return true;
                }
            }
            catch (SocketException sex)
            {
                if (m_mainSocket.Connected)
                {
                    m_mainSocket.Shutdown(SocketShutdown.Both);
                    m_mainSocket.Close();
                    m_mainSocket = null;
                    FireChannelDisconnected();
                }
                else FireExceptionOccured(sex);
            }
            catch (Exception ex)
            {
                FireExceptionOccured(ex);
            }
            return false;
        }

        #endregion

        #region Receive

        /// <summary>
        /// This function is used to start asyncronious receive operation from remote machine
        /// </summary>
        /// <param name="dataSize">Size of buffer to receive</param>
        /// <returns>true if main socket is connected; otherwise false</returns>
        public bool Receive(int dataSize)
        {
            if (m_mainSocket == null || !m_mainSocket.Connected || dataSize < 0) return false;

            try
            {
                WaitForData(dataSize, m_mainSocket);
                return true;
            }
            catch (SocketException sex)
            {
                if (m_mainSocket.Connected)
                {
                    m_mainSocket.Shutdown(SocketShutdown.Both);
                    m_mainSocket.Close();
                    FireChannelDisconnected();
                }
                else FireExceptionOccured(sex);
            }
            catch (Exception ex)
            {
                FireExceptionOccured(ex);
            }

            return false;
        }

        override protected void dataReceivedCallback(IAsyncResult asyn)
        {
            SocketPacket socketData = (SocketPacket)asyn.AsyncState;
            int res = socketData.m_currentSocket.EndReceive(asyn);
            if (res <= 0)
            {
                Stop();
            }
            else
            {
                try
                {
                    lock (this)
                    {
                        if (DataReceived != null)
                            DataReceived(this, new DataEventArgs(socketData.dataBuffer, res));
                    }
                }
                catch (ObjectDisposedException ode)
                {
                    //Console.WriteLine(ode.ObjectName);
                    //Console.WriteLine(ode.Message);
                    FireExceptionOccured(ode);
                }
                catch (SocketException sex)
                {
                    //LOG: Socket Exception
                    //Console.WriteLine(sex.Message);
                    //Console.WriteLine(sex.StackTrace);
                    if (!m_mainSocket.Connected)
                        FireChannelDisconnected();
                    else
                        FireExceptionOccured(sex);
                }
                catch (Exception e)
                {
                    //LOG: General Exception
                    FireExceptionOccured(e);
                }
            }
        }

        #endregion


        /// <summary>
        /// Returns a string representation of an TcpClientChannel object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string ret = "";
            ret = "TCP Channel ";
            ret += "client ";

            if (Connected) ret += "Connected. " + m_mainSocket.RemoteEndPoint.ToString();
            else ret += "Disconnected";

            return ret;
        }
    }

    /// <summary>
    /// Efficient implementation of thread-safe waitable queue.
    /// </summary>
    /// <remarks>Thread starvation is possible using this queue.
    /// Do not use this class as general-purpose IPC when multiple threads
    /// are calling <see cref="Dequeue"/> method.</remarks>
    public sealed class WaitableQueue<T> : IEnumerable<T>
    {
        #region Constructor

        /// <summary>
        /// Initializes the queue
        /// </summary>
        public WaitableQueue()
        {
            m_WrappedQueue = new Queue<T>();
            m_Event = new AutoResetEvent(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inserts item at the tail of the queue
        /// </summary>
        /// <param name="p_Item">Item to insert</param>
        public void Enqueue(T p_Item)
        {
            bool bWasEmpty = false;

            lock (this)
            {
                bWasEmpty = m_WrappedQueue.Count == 0;

                try
                {
                    m_WrappedQueue.Enqueue(p_Item);
                }
                catch
                {
                    //Console.WriteLine("Enqueue throwed exception,Queue size={0}", m_WrappedQueue.Count);
                    //Console.WriteLine(e.Message);
                    //Console.WriteLine(e.StackTrace);
                }
            }

            //set event only if it wasn't set before
            if (bWasEmpty) m_Event.Set();
        }

        /// <summary>
        /// Returns (and removes) object from the head of the queue.
        /// Waits some time period if queue is empty.
        /// </summary>
        /// <param name="p_Timeout">Time period to wait</param>
        /// <returns>Retrieved object or null if queue is empty</returns>
        /// <remarks>This method uses fast and efficient method of data retrieval from queue, 
        /// optimized for the cases when queue is not empty most of the time. 
        /// Calling thread enters wait state only if queue is empty.</remarks>      
        public object Dequeue(int p_Timeout)
        {
            //Note: thread starvation is possible
            object item = null;

            for (; ;)
            {
                //optimistic approach - get something from the queue
                item = AttemptDequeue();

                //if succeded - we are done
                if (item != null) break;

                //if not - wait for event.
                //Note: queue isn't locked when we enter wait state
                if (!m_Event.WaitOne(p_Timeout, false)) break;

                //We were waked up, so there is something in queue.
                //Although event is signalled, queue may be empty already,
                //because another thread waked up and removed object from the queue.

                //Get something from the queue
                item = AttemptDequeue();

                //If we got something, or there is no timeout - we are done
                if (item != null || ((p_Timeout != Timeout.Infinite) && p_Timeout <= 0))
                {
                    break;
                }
                else
                {
                    //we were waked up, but queue is empty again - continue the process.
                    continue;
                }

            }

            return item;
        }

        /// <summary>
        /// Returns (and removes) object from the head of the queue.
        /// Waits some time period if queue is empty.
        /// </summary>
        /// <param name="p_Timeout">Time period to wait</param>
        /// <returns>Retrieved object or null if queue is empty</returns>
        /// <remarks>This method uses fast and efficient method of data retrieval from queue, 
        /// optimized for the cases when queue is not empty most of the time. 
        /// Calling thread enters wait state only if queue is empty.</remarks>  
        public object Dequeue(TimeSpan p_Timeout)
        {
            return Dequeue((int)p_Timeout.TotalMilliseconds);
        }

        /// <summary>
        /// Returns (and removes) object from the head of the queue.
        /// Waits indefinitely if queue is empty.
        /// </summary>  
        /// <returns>Retrieved object</returns>
        /// <remarks>This method uses fast and efficient method of data retrieval from queue, 
        /// optimized for the cases when queue is not empty most of the time. 
        /// Calling thread enters wait state only if queue is empty.</remarks>  
        public object Dequeue()
        {
            return Dequeue(Timeout.Infinite);
        }

        /// <summary>
        /// Cast operator from WaitableQueue to <see cref="WaitHandle"/> 
        /// </summary>
        /// <param name="p_waitaibleQueue"></param>
        /// <returns>Event what can be waited for</returns>
        public static implicit operator WaitHandle(WaitableQueue<T> p_waitaibleQueue)
        {
            return p_waitaibleQueue.m_Event;
        }

        /// <summary>
        /// Retrieves enumerator of internal queue
        /// </summary>
        /// <returns>Enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_WrappedQueue.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return m_WrappedQueue.GetEnumerator();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Number of objects in the queue
        /// </summary>
        /// <value>Returns m_WrappedQueue.Count</value>
        public int Count
        {
            get
            {
                return m_WrappedQueue.Count;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Retrieves and removed object from the head of the queue.
        /// Resets <see cref="m_Event"/> if last element was removed.
        /// </summary>
        /// <returns>Retrieved object or null</returns>
        object AttemptDequeue()
        {
            object item = null;

            bool bNotEmpty = false;

            lock (this)
            {
                if (m_WrappedQueue.Count != 0)
                {
                    item = m_WrappedQueue.Dequeue();

                    if (m_WrappedQueue.Count == 0)
                    {
                        m_Event.Reset();
                    }
                    else bNotEmpty = true;
                }
            }

            if (bNotEmpty) m_Event.Set();

            return item;
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// Item container
        /// </summary>
        Queue<T> m_WrappedQueue;

        /// <summary>
        /// Event used to wake up waiting threads.
        /// Event is in signalled state if queue isn't empty
        /// </summary>
        AutoResetEvent m_Event;

        #endregion

    }

}