using System;
using System.Collections.Generic;

namespace AirPlayer.Core.Connection
{
    /// <summary>
    /// Headset-side connection state machine. Pure C#, time is injected
    /// (seconds, any monotonic origin) so behaviour is fully unit-testable.
    ///
    /// Disconnected --Connect()--> Connecting --welcome--> Connected
    /// Connected --3 missed pongs--> Connecting (auto-retry, ConnectionLost raised)
    /// Any state --incompatible--> Disconnected
    /// </summary>
    public sealed class ConnectionStateMachine
    {
        private readonly ConnectionSettings _settings;
        private readonly RttTracker _rtt;
        private readonly Dictionary<int, double> _pendingPings = new Dictionary<int, double>();

        private ConnectionState _state = ConnectionState.Disconnected;
        private double _lastHelloTime = double.NegativeInfinity;
        private double _lastPingTime = double.NegativeInfinity;
        private int _nextPingSequence;
        private int _pingsSinceLastPong;

        public ConnectionStateMachine(ConnectionSettings settings = null)
        {
            _settings = settings ?? new ConnectionSettings();
            _rtt = new RttTracker(_settings.RttWindowSize);
        }

        public ConnectionState State
        {
            get { return _state; }
        }

        public double LastRttMs
        {
            get { return _rtt.LastMs; }
        }

        public double AverageRttMs
        {
            get { return _rtt.AverageMs; }
        }

        public int RttSampleCount
        {
            get { return _rtt.Count; }
        }

        public string CompanionVersion { get; private set; }
        public bool AbletonConnected { get; private set; }

        /// <summary>-1 until the companion rejects us for a protocol mismatch.</summary>
        public int IncompatibleRequiredVersion { get; private set; } = -1;

        public event Action<ConnectionState> StateChanged;
        public event Action ConnectionLost;

        public void Connect(double now)
        {
            Reset();
            SetState(ConnectionState.Connecting);
        }

        public void Disconnect()
        {
            Reset();
            SetState(ConnectionState.Disconnected);
        }

        /// <summary>
        /// Advances timers. Appends any due outgoing actions to <paramref name="actions"/>
        /// (the list is cleared first; callers reuse one list to avoid per-frame allocation).
        /// </summary>
        public void Tick(double now, List<ConnectionAction> actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }
            actions.Clear();

            switch (_state)
            {
                case ConnectionState.Connecting:
                    if (now - _lastHelloTime >= _settings.HelloIntervalSeconds)
                    {
                        _lastHelloTime = now;
                        actions.Add(ConnectionAction.Hello());
                    }
                    break;

                case ConnectionState.Connected:
                    if (now - _lastPingTime >= _settings.PingIntervalSeconds)
                    {
                        if (_pingsSinceLastPong >= _settings.MissedPongsBeforeDisconnect)
                        {
                            HandleConnectionLost();
                            break;
                        }
                        int sequence = _nextPingSequence++;
                        _pendingPings[sequence] = now;
                        _pingsSinceLastPong++;
                        _lastPingTime = now;
                        actions.Add(ConnectionAction.Ping(sequence));
                    }
                    break;
            }
        }

        public void OnWelcomeReceived(string companionVersion, bool abletonConnected, double now)
        {
            CompanionVersion = companionVersion;
            AbletonConnected = abletonConnected;

            if (_state == ConnectionState.Connected)
            {
                return;
            }

            _pendingPings.Clear();
            _pingsSinceLastPong = 0;
            _lastPingTime = double.NegativeInfinity;
            _rtt.Reset();
            SetState(ConnectionState.Connected);
        }

        public void OnPongReceived(int sequence, double now)
        {
            double sentAt;
            if (!_pendingPings.TryGetValue(sequence, out sentAt))
            {
                return;
            }
            _pendingPings.Remove(sequence);
            _rtt.Add((now - sentAt) * 1000.0);
            _pingsSinceLastPong = 0;
        }

        public void OnIncompatibleReceived(int requiredVersion)
        {
            IncompatibleRequiredVersion = requiredVersion;
            Reset();
            SetState(ConnectionState.Disconnected);
        }

        private void HandleConnectionLost()
        {
            _pendingPings.Clear();
            _pingsSinceLastPong = 0;
            _lastHelloTime = double.NegativeInfinity;
            Action lost = ConnectionLost;
            if (lost != null)
            {
                lost();
            }
            SetState(ConnectionState.Connecting);
        }

        private void Reset()
        {
            _pendingPings.Clear();
            _pingsSinceLastPong = 0;
            _nextPingSequence = 0;
            _lastHelloTime = double.NegativeInfinity;
            _lastPingTime = double.NegativeInfinity;
            _rtt.Reset();
        }

        private void SetState(ConnectionState next)
        {
            if (next == _state)
            {
                return;
            }
            _state = next;
            Action<ConnectionState> changed = StateChanged;
            if (changed != null)
            {
                changed(next);
            }
        }
    }
}
