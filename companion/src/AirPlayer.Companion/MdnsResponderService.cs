using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AirPlayer.Core.Discovery;

namespace AirPlayer.Companion
{
    /// <summary>
    /// Announces `_airplayer._udp.local` and answers headset queries.
    /// Responses are sent both unicast (to the querier, for Android headsets
    /// without a multicast lock) and multicast (for standard mDNS caches).
    /// </summary>
    public sealed class MdnsResponderService : IDisposable
    {
        private static readonly IPAddress MulticastGroup = IPAddress.Parse("224.0.0.251");
        private const int MdnsPort = 5353;

        private readonly byte[] _response;
        private readonly Action<string> _log;
        private readonly Socket _socket;
        private readonly IPEndPoint _multicastEndpoint = new IPEndPoint(MulticastGroup, MdnsPort);
        private Thread _thread;
        private volatile bool _running;

        public MdnsResponderService(string instanceLabel, string hostLabel, IPAddress ipv4, ushort servicePort, Action<string> log)
        {
            _response = MdnsMessages.BuildResponse(instanceLabel, hostLabel, ipv4, servicePort);
            _log = log ?? delegate { };
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.Bind(new IPEndPoint(IPAddress.Any, MdnsPort));
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                new MulticastOption(MulticastGroup, IPAddress.Any));
        }

        public void Start()
        {
            _running = true;
            _thread = new Thread(ReceiveLoop) { IsBackground = true, Name = "AirPlayer.Mdns" };
            _thread.Start();

            // Unsolicited announcements so already-listening headsets find us
            // without waiting for their next query.
            Announce();
        }

        public void Announce()
        {
            try
            {
                _socket.SendTo(_response, _multicastEndpoint);
            }
            catch (SocketException ex)
            {
                _log($"mDNS announce failed: {ex.Message}");
            }
        }

        private void ReceiveLoop()
        {
            byte[] buffer = new byte[2048];
            EndPoint source = new IPEndPoint(IPAddress.Any, 0);

            while (_running)
            {
                int received;
                try
                {
                    received = _socket.ReceiveFrom(buffer, ref source);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    if (!_running)
                    {
                        break;
                    }
                    continue;
                }

                if (!MdnsMessages.TryParseQuery(buffer, received))
                {
                    continue;
                }

                try
                {
                    _socket.SendTo(_response, source);              // unicast to the querier
                    _socket.SendTo(_response, _multicastEndpoint);  // and the regular multicast copy
                }
                catch (SocketException ex)
                {
                    _log($"mDNS reply failed: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            _running = false;
            _socket.Close();
            if (_thread != null)
            {
                _thread.Join(1000);
            }
        }
    }
}
