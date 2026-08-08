using System.Collections.Generic;
using System.Net;
using AirPlayer.Core.Connection;
using AirPlayer.Core.Net;
using AirPlayer.Core.Osc;
using AirPlayer.Core.Protocol;
using UnityEngine;

namespace AirPlayer.Runtime
{
    /// <summary>
    /// Drives the connection to the companion: owns the UDP transport and the
    /// pure-C# state machine, translating between them once per frame.
    /// All socket I/O happens on the transport's background threads; Update()
    /// only moves messages across queues (no per-frame allocation).
    /// </summary>
    public sealed class ConnectionManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Device name sent in the hello handshake.")]
        private string deviceName = "Quest 3S";

        private readonly List<ConnectionAction> _pendingActions = new List<ConnectionAction>(8);
        private ConnectionStateMachine _stateMachine;
        private OscUdpTransport _transport;
        private string _companionLabel = "";

        public ConnectionState State
        {
            get { return _stateMachine != null ? _stateMachine.State : ConnectionState.Disconnected; }
        }

        public double AverageRttMs
        {
            get { return _stateMachine != null ? _stateMachine.AverageRttMs : 0.0; }
        }

        public int RttSampleCount
        {
            get { return _stateMachine != null ? _stateMachine.RttSampleCount : 0; }
        }

        public string CompanionLabel
        {
            get { return _companionLabel; }
        }

        public bool IsIncompatible
        {
            get { return _stateMachine != null && _stateMachine.IncompatibleRequiredVersion >= 0; }
        }

        /// <summary>Connects to a companion (from discovery or manual IP entry).</summary>
        public void ConnectTo(IPAddress address, string label)
        {
            DisposeTransport();

            _companionLabel = string.IsNullOrEmpty(label) ? address.ToString() : label;
            _transport = new OscUdpTransport(
                AirPlayerProtocol.CompanionToQuestPort,
                new IPEndPoint(address, AirPlayerProtocol.QuestToCompanionPort));

            _stateMachine = new ConnectionStateMachine();
            _stateMachine.ConnectionLost += HandleConnectionLost;
            _stateMachine.Connect(Time.realtimeSinceStartupAsDouble);

            Debug.Log($"[AirPlayer] Connecting to {_companionLabel} ({address})");
        }

        public void DisconnectFromCompanion()
        {
            if (_stateMachine != null)
            {
                _stateMachine.Disconnect();
            }
            DisposeTransport();
        }

        private void Update()
        {
            if (_transport == null || _stateMachine == null)
            {
                return;
            }

            double now = Time.realtimeSinceStartupAsDouble;

            OscMessage incoming;
            while (_transport.TryDequeueIncoming(out incoming))
            {
                HandleIncoming(incoming, now);
            }

            _stateMachine.Tick(now, _pendingActions);
            for (int i = 0; i < _pendingActions.Count; i++)
            {
                ConnectionAction action = _pendingActions[i];
                switch (action.Type)
                {
                    case ConnectionActionType.SendHello:
                        _transport.Send(AirPlayerProtocol.Hello(deviceName));
                        break;
                    case ConnectionActionType.SendPing:
                        _transport.Send(AirPlayerProtocol.Ping(action.PingSequence));
                        break;
                }
            }
        }

        private void HandleIncoming(OscMessage message, double now)
        {
            switch (message.Address)
            {
                case AirPlayerProtocol.WelcomeAddress:
                {
                    string companionVersion;
                    bool abletonConnected;
                    if (message.TryGetString(0, out companionVersion) &&
                        message.TryGetBool(1, out abletonConnected))
                    {
                        _stateMachine.OnWelcomeReceived(companionVersion, abletonConnected, now);
                    }
                    break;
                }
                case AirPlayerProtocol.PongAddress:
                {
                    int sequence;
                    if (message.TryGetInt(0, out sequence))
                    {
                        _stateMachine.OnPongReceived(sequence, now);
                    }
                    break;
                }
                case AirPlayerProtocol.IncompatibleAddress:
                {
                    int requiredVersion;
                    if (message.TryGetInt(0, out requiredVersion))
                    {
                        _stateMachine.OnIncompatibleReceived(requiredVersion);
                        Debug.LogWarning($"[AirPlayer] Companion requires protocol v{requiredVersion}, app speaks v{AirPlayerProtocol.Version}. Update needed.");
                    }
                    break;
                }
            }
        }

        private void HandleConnectionLost()
        {
            Debug.LogWarning("[AirPlayer] Connection to companion lost; retrying.");
        }

        private void OnDestroy()
        {
            DisposeTransport();
        }

        private void DisposeTransport()
        {
            if (_transport != null)
            {
                _transport.Dispose();
                _transport = null;
            }
        }
    }
}
