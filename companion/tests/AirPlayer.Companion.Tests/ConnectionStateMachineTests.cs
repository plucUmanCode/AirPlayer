using System.Collections.Generic;
using System.Linq;
using AirPlayer.Core.Connection;
using Xunit;

namespace AirPlayer.Companion.Tests
{
    public class ConnectionStateMachineTests
    {
        private readonly List<ConnectionAction> _actions = new List<ConnectionAction>();

        private static ConnectionStateMachine NewMachine()
        {
            // Default settings: hello 1 s, ping 1 s, 3 missed pongs, RTT window 10.
            return new ConnectionStateMachine();
        }

        [Fact]
        public void ConnectSendsHelloImmediately()
        {
            ConnectionStateMachine machine = NewMachine();
            machine.Connect(0.0);
            Assert.Equal(ConnectionState.Connecting, machine.State);

            machine.Tick(0.0, _actions);
            Assert.Single(_actions);
            Assert.Equal(ConnectionActionType.SendHello, _actions[0].Type);
        }

        [Fact]
        public void HelloIsResentAtIntervalWhileConnecting()
        {
            ConnectionStateMachine machine = NewMachine();
            machine.Connect(0.0);

            machine.Tick(0.0, _actions);
            Assert.Single(_actions);

            machine.Tick(0.5, _actions);
            Assert.Empty(_actions);

            machine.Tick(1.0, _actions);
            Assert.Single(_actions);
            Assert.Equal(ConnectionActionType.SendHello, _actions[0].Type);

            machine.Tick(2.1, _actions);
            Assert.Single(_actions);
        }

        [Fact]
        public void WelcomeMovesToConnectedAndStartsPinging()
        {
            ConnectionStateMachine machine = NewMachine();
            machine.Connect(0.0);
            machine.Tick(0.0, _actions);

            machine.OnWelcomeReceived("0.1.0", false, 0.1);
            Assert.Equal(ConnectionState.Connected, machine.State);
            Assert.Equal("0.1.0", machine.CompanionVersion);
            Assert.False(machine.AbletonConnected);

            machine.Tick(0.2, _actions);
            Assert.Single(_actions);
            Assert.Equal(ConnectionActionType.SendPing, _actions[0].Type);
            Assert.Equal(0, _actions[0].PingSequence);
        }

        [Fact]
        public void PongRecordsRoundTripTime()
        {
            ConnectionStateMachine machine = NewMachine();
            machine.Connect(0.0);
            machine.OnWelcomeReceived("0.1.0", false, 0.0);

            machine.Tick(1.0, _actions);
            int sequence = _actions[0].PingSequence;

            machine.OnPongReceived(sequence, 1.050);
            Assert.Equal(50.0, machine.LastRttMs, 6);
            Assert.Equal(1, machine.RttSampleCount);
        }

        [Fact]
        public void AverageRttCoversTheLastTenPings()
        {
            ConnectionStateMachine machine = NewMachine();
            machine.Connect(0.0);
            machine.OnWelcomeReceived("0.1.0", false, 0.0);

            // 12 ping/pong pairs with RTTs of 10 ms, 20 ms, ... 120 ms.
            for (int i = 0; i < 12; i++)
            {
                double sendTime = 1.0 + i;
                machine.Tick(sendTime, _actions);
                Assert.Single(_actions);
                machine.OnPongReceived(_actions[0].PingSequence, sendTime + (i + 1) * 0.010);
            }

            // Window keeps the last 10: 30 ms .. 120 ms, average 75 ms.
            Assert.Equal(10, machine.RttSampleCount);
            Assert.Equal(75.0, machine.AverageRttMs, 6);
            Assert.Equal(120.0, machine.LastRttMs, 6);
        }

        [Fact]
        public void ThreeMissedPongsRaiseConnectionLostWithinFourSeconds()
        {
            ConnectionStateMachine machine = NewMachine();
            machine.Connect(0.0);
            machine.OnWelcomeReceived("0.1.0", false, 0.0);

            bool lostRaised = false;
            double lostAt = -1.0;
            machine.ConnectionLost += () => lostRaised = true;

            for (double t = 0.0; t <= 5.0 && !lostRaised; t += 0.1)
            {
                machine.Tick(t, _actions);
                if (lostRaised)
                {
                    lostAt = t;
                }
            }

            Assert.True(lostRaised);
            Assert.True(lostAt < 4.0, $"Loss detected at {lostAt} s, must be under 4 s (Loop 0 CA #4).");
            Assert.Equal(ConnectionState.Connecting, machine.State);
        }

        [Fact]
        public void ReceivedPongsKeepTheConnectionAlive()
        {
            ConnectionStateMachine machine = NewMachine();
            machine.Connect(0.0);
            machine.OnWelcomeReceived("0.1.0", false, 0.0);

            bool lostRaised = false;
            machine.ConnectionLost += () => lostRaised = true;

            for (double t = 0.0; t <= 30.0; t += 0.1)
            {
                machine.Tick(t, _actions);
                foreach (ConnectionAction action in _actions.Where(a => a.Type == ConnectionActionType.SendPing))
                {
                    machine.OnPongReceived(action.PingSequence, t + 0.02);
                }
            }

            Assert.False(lostRaised);
            Assert.Equal(ConnectionState.Connected, machine.State);
        }

        [Fact]
        public void AfterConnectionLossHelloResumes()
        {
            ConnectionStateMachine machine = NewMachine();
            machine.Connect(0.0);
            machine.OnWelcomeReceived("0.1.0", false, 0.0);

            // Let it die.
            for (double t = 0.0; t <= 5.0; t += 0.5)
            {
                machine.Tick(t, _actions);
            }
            Assert.Equal(ConnectionState.Connecting, machine.State);

            machine.Tick(6.0, _actions);
            Assert.Single(_actions);
            Assert.Equal(ConnectionActionType.SendHello, _actions[0].Type);

            // And it can reconnect.
            machine.OnWelcomeReceived("0.1.0", false, 6.1);
            Assert.Equal(ConnectionState.Connected, machine.State);
        }

        [Fact]
        public void UnknownPongSequenceIsIgnored()
        {
            ConnectionStateMachine machine = NewMachine();
            machine.Connect(0.0);
            machine.OnWelcomeReceived("0.1.0", false, 0.0);

            machine.OnPongReceived(999, 1.0);
            Assert.Equal(0, machine.RttSampleCount);
        }

        [Fact]
        public void IncompatibleProtocolStopsReconnecting()
        {
            ConnectionStateMachine machine = NewMachine();
            machine.Connect(0.0);
            machine.Tick(0.0, _actions);

            machine.OnIncompatibleReceived(2);
            Assert.Equal(ConnectionState.Disconnected, machine.State);
            Assert.Equal(2, machine.IncompatibleRequiredVersion);

            machine.Tick(10.0, _actions);
            Assert.Empty(_actions);
        }

        [Fact]
        public void StateChangedFiresOnTransitions()
        {
            ConnectionStateMachine machine = NewMachine();
            List<ConnectionState> transitions = new List<ConnectionState>();
            machine.StateChanged += state => transitions.Add(state);

            machine.Connect(0.0);
            machine.OnWelcomeReceived("0.1.0", false, 0.1);
            machine.Disconnect();

            Assert.Equal(
                new[] { ConnectionState.Connecting, ConnectionState.Connected, ConnectionState.Disconnected },
                transitions);
        }
    }
}
