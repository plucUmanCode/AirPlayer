using System;
using System.Net;
using System.Net.Sockets;

namespace AirPlayer.Core.Discovery
{
    /// <summary>
    /// Query side of the discovery: sends PTR questions for the AirPlayer
    /// service and collects responses. Blocking socket API — run it on a
    /// dedicated thread (never on the Unity main thread, per CLAUDE.md).
    /// </summary>
    public sealed class MdnsClient : IDisposable
    {
        private static readonly IPAddress MulticastGroup = IPAddress.Parse("224.0.0.251");
        private const int MdnsPort = 5353;

        private readonly Socket _socket;
        private readonly IPEndPoint _multicastEndpoint = new IPEndPoint(MulticastGroup, MdnsPort);
        private readonly byte[] _query = MdnsMessages.BuildQuery();
        private readonly byte[] _receiveBuffer = new byte[2048];
        private bool _disposed;

        public MdnsClient()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.Bind(new IPEndPoint(IPAddress.Any, MdnsPort));
            try
            {
                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                    new MulticastOption(MulticastGroup, IPAddress.Any));
            }
            catch (SocketException)
            {
                // Joining the group can fail on some networks; unicast (QU)
                // responses still reach us, so discovery keeps working.
            }
        }

        public void SendQuery()
        {
            _socket.SendTo(_query, _multicastEndpoint);
        }

        /// <summary>
        /// Waits up to <paramref name="timeoutMs"/> for one AirPlayer response.
        /// Non-AirPlayer mDNS traffic on the wire is silently skipped.
        /// </summary>
        public bool TryReceive(int timeoutMs, out MdnsServiceInfo info)
        {
            info = default(MdnsServiceInfo);
            try
            {
                if (!_socket.Poll(timeoutMs * 1000, SelectMode.SelectRead))
                {
                    return false;
                }
                EndPoint source = new IPEndPoint(IPAddress.Any, 0);
                int received = _socket.ReceiveFrom(_receiveBuffer, ref source);
                return MdnsMessages.TryParseResponse(_receiveBuffer, received, out info);
            }
            catch (SocketException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _socket.Close();
        }
    }
}
