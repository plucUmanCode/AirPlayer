using System;
using System.Collections.Generic;
using System.Net;
using AirPlayer.Core.Osc;
using AirPlayer.Core.Protocol;

namespace AirPlayer.Companion
{
    /// <summary>
    /// Protocol logic of the companion, free of sockets and timers: datagrams
    /// in, datagrams out, injected clock. Program.cs wires it to real UDP.
    /// Thread-safe: HandleDatagram and Tick may be called from different threads.
    ///
    /// Loop 0 scope: handshake (hello/welcome, protocol version check),
    /// heartbeat (ping/pong), client presence tracking. MIDI comes in Loop 1.
    /// </summary>
    public sealed class CompanionEngine
    {
        private sealed class ClientState
        {
            public IPEndPoint ReplyEndpoint;
            public string DeviceName;
            public double LastSeen;
        }

        private readonly object _sync = new object();
        private ClientState _client;

        public string CompanionVersion { get; }

        /// <summary>False until AbletonOSC integration lands (Loop 3).</summary>
        public bool AbletonConnected { get; set; }

        /// <summary>
        /// The headset pings every second; 4 s of silence means it is gone
        /// (mirrors the headset-side threshold of 3 missed pongs).
        /// </summary>
        public double ClientTimeoutSeconds { get; set; } = 4.0;

        public event Action<string> Log;
        public event Action<string, IPEndPoint> ClientConnected;
        public event Action<string> ClientDisconnected;

        public CompanionEngine(string companionVersion)
        {
            if (string.IsNullOrEmpty(companionVersion))
            {
                throw new ArgumentException("Companion version is required.", nameof(companionVersion));
            }
            CompanionVersion = companionVersion;
        }

        public bool HasClient
        {
            get
            {
                lock (_sync)
                {
                    return _client != null;
                }
            }
        }

        public string ClientDeviceName
        {
            get
            {
                lock (_sync)
                {
                    return _client != null ? _client.DeviceName : null;
                }
            }
        }

        /// <summary>Processes one incoming datagram and returns the replies to send.</summary>
        public List<OutboundPacket> HandleDatagram(byte[] data, int length, IPEndPoint source, double now)
        {
            List<OutboundPacket> outbound = new List<OutboundPacket>(1);

            OscMessage message;
            if (!OscReader.TryParse(data, length, out message))
            {
                RaiseLog($"Dropped malformed packet ({length} bytes) from {source}");
                return outbound;
            }

            IPEndPoint replyEndpoint = new IPEndPoint(source.Address, AirPlayerProtocol.CompanionToQuestPort);

            switch (message.Address)
            {
                case AirPlayerProtocol.HelloAddress:
                    HandleHello(message, replyEndpoint, now, outbound);
                    break;

                case AirPlayerProtocol.PingAddress:
                    HandlePing(message, source, replyEndpoint, now, outbound);
                    break;

                default:
                    Touch(source, now);
                    RaiseLog($"Ignored unknown address '{message.Address}' from {source}");
                    break;
            }

            return outbound;
        }

        /// <summary>Advances the presence timeout. Call periodically (~2x per second is plenty).</summary>
        public void Tick(double now)
        {
            string timedOutDevice = null;
            lock (_sync)
            {
                if (_client != null && now - _client.LastSeen > ClientTimeoutSeconds)
                {
                    timedOutDevice = _client.DeviceName;
                    _client = null;
                }
            }

            if (timedOutDevice != null)
            {
                // Loop 1: send all-notes-off to the virtual MIDI port here.
                Action<string> handler = ClientDisconnected;
                if (handler != null)
                {
                    handler(timedOutDevice);
                }
            }
        }

        private void HandleHello(OscMessage message, IPEndPoint replyEndpoint, double now, List<OutboundPacket> outbound)
        {
            string deviceName;
            int protocolVersion;
            if (!message.TryGetString(0, out deviceName) || !message.TryGetInt(1, out protocolVersion))
            {
                RaiseLog($"Dropped hello with bad arguments from {replyEndpoint.Address}");
                return;
            }

            if (protocolVersion != AirPlayerProtocol.Version)
            {
                RaiseLog($"Rejected '{deviceName}' ({replyEndpoint.Address}): protocol v{protocolVersion}, expected v{AirPlayerProtocol.Version}");
                outbound.Add(new OutboundPacket(
                    OscWriter.Encode(AirPlayerProtocol.Incompatible(AirPlayerProtocol.Version)),
                    replyEndpoint));
                return;
            }

            bool isNewClient;
            lock (_sync)
            {
                isNewClient = _client == null ||
                              !_client.ReplyEndpoint.Equals(replyEndpoint) ||
                              !string.Equals(_client.DeviceName, deviceName, StringComparison.Ordinal);
                _client = new ClientState
                {
                    ReplyEndpoint = replyEndpoint,
                    DeviceName = deviceName,
                    LastSeen = now
                };
            }

            outbound.Add(new OutboundPacket(
                OscWriter.Encode(AirPlayerProtocol.Welcome(CompanionVersion, AbletonConnected)),
                replyEndpoint));

            if (isNewClient)
            {
                Action<string, IPEndPoint> handler = ClientConnected;
                if (handler != null)
                {
                    handler(deviceName, replyEndpoint);
                }
            }
        }

        private void HandlePing(OscMessage message, IPEndPoint source, IPEndPoint replyEndpoint, double now, List<OutboundPacket> outbound)
        {
            int sequence;
            if (!message.TryGetInt(0, out sequence))
            {
                RaiseLog($"Dropped ping with bad arguments from {source}");
                return;
            }
            Touch(source, now);
            outbound.Add(new OutboundPacket(
                OscWriter.Encode(AirPlayerProtocol.Pong(sequence)),
                replyEndpoint));
        }

        private void Touch(IPEndPoint source, double now)
        {
            lock (_sync)
            {
                if (_client != null && _client.ReplyEndpoint.Address.Equals(source.Address))
                {
                    _client.LastSeen = now;
                }
            }
        }

        private void RaiseLog(string line)
        {
            Action<string> handler = Log;
            if (handler != null)
            {
                handler(line);
            }
        }
    }
}
