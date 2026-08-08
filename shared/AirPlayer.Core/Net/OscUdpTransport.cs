using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AirPlayer.Core.Osc;

namespace AirPlayer.Core.Net
{
    /// <summary>
    /// Bidirectional OSC-over-UDP transport with dedicated send and receive
    /// threads, so no network I/O ever runs on the Unity main thread
    /// (CLAUDE.md rule). The main thread only enqueues outgoing messages and
    /// drains incoming ones.
    /// </summary>
    public sealed class OscUdpTransport : IDisposable
    {
        private const int SendQueueCapacity = 256;

        private readonly UdpClient _udp;
        private readonly IPEndPoint _remoteEndpoint;
        private readonly BlockingCollection<OscMessage> _sendQueue =
            new BlockingCollection<OscMessage>(new ConcurrentQueue<OscMessage>(), SendQueueCapacity);
        private readonly ConcurrentQueue<OscMessage> _receivedQueue = new ConcurrentQueue<OscMessage>();
        private readonly byte[] _sendBuffer = new byte[1024]; // owned by the send thread
        private readonly Thread _sendThread;
        private readonly Thread _receiveThread;
        private volatile bool _running = true;

        /// <summary>Incremented when an incoming packet fails to parse; useful for debugging.</summary>
        public int MalformedPacketCount { get { return Volatile.Read(ref _malformedPacketCount); } }
        private int _malformedPacketCount;

        public OscUdpTransport(int localPort, IPEndPoint remoteEndpoint)
        {
            if (remoteEndpoint == null)
            {
                throw new ArgumentNullException(nameof(remoteEndpoint));
            }
            _remoteEndpoint = remoteEndpoint;

            _udp = new UdpClient();
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, localPort));

            _sendThread = new Thread(SendLoop) { IsBackground = true, Name = "AirPlayer.OscSend" };
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "AirPlayer.OscReceive" };
            _sendThread.Start();
            _receiveThread.Start();
        }

        /// <summary>Thread-safe, non-blocking. Drops the message if the queue is full.</summary>
        public bool Send(OscMessage message)
        {
            if (!_running)
            {
                return false;
            }
            return _sendQueue.TryAdd(message);
        }

        /// <summary>Called from the main thread to drain received messages.</summary>
        public bool TryDequeueIncoming(out OscMessage message)
        {
            return _receivedQueue.TryDequeue(out message);
        }

        private void SendLoop()
        {
            try
            {
                foreach (OscMessage message in _sendQueue.GetConsumingEnumerable())
                {
                    int length = OscWriter.Write(message, _sendBuffer);
                    _udp.Send(_sendBuffer, length, _remoteEndpoint);
                }
            }
            catch (ObjectDisposedException)
            {
                // Socket closed during shutdown.
            }
            catch (SocketException)
            {
                // Transient network failure; the connection state machine will
                // notice missing pongs and surface the disconnection.
            }
        }

        private void ReceiveLoop()
        {
            IPEndPoint anySource = new IPEndPoint(IPAddress.Any, 0);
            while (_running)
            {
                try
                {
                    byte[] datagram = _udp.Receive(ref anySource);
                    OscMessage message;
                    if (OscReader.TryParse(datagram, datagram.Length, out message))
                    {
                        _receivedQueue.Enqueue(message);
                    }
                    else
                    {
                        Interlocked.Increment(ref _malformedPacketCount);
                    }
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
                    // ICMP port-unreachable surfaces here on Windows; ignore
                    // and keep listening.
                }
            }
        }

        public void Dispose()
        {
            if (!_running)
            {
                return;
            }
            _running = false;
            _sendQueue.CompleteAdding();
            _udp.Close();
            _sendThread.Join(1000);
            _receiveThread.Join(1000);
        }
    }
}
