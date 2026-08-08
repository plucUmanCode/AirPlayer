using System.Collections.Generic;
using System.Net;
using AirPlayer.Core.Osc;
using AirPlayer.Core.Protocol;
using Xunit;

namespace AirPlayer.Companion.Tests
{
    public class CompanionEngineTests
    {
        private static readonly IPEndPoint HeadsetSource = new IPEndPoint(IPAddress.Parse("192.168.1.42"), 54321);

        private static CompanionEngine NewEngine()
        {
            return new CompanionEngine("0.1.0");
        }

        private static List<OutboundPacket> Send(CompanionEngine engine, OscMessage message, double now)
        {
            byte[] packet = OscWriter.Encode(message);
            return engine.HandleDatagram(packet, packet.Length, HeadsetSource, now);
        }

        private static OscMessage Decode(OutboundPacket packet)
        {
            OscMessage decoded;
            Assert.True(OscReader.TryParse(packet.Data, packet.Data.Length, out decoded));
            return decoded;
        }

        [Fact]
        public void HelloGetsWelcomeOnPort9001()
        {
            CompanionEngine engine = NewEngine();
            List<OutboundPacket> replies = Send(engine, AirPlayerProtocol.Hello("Quest3S"), 0.0);

            OutboundPacket reply = Assert.Single(replies);
            Assert.Equal(HeadsetSource.Address, reply.Target.Address);
            Assert.Equal(AirPlayerProtocol.CompanionToQuestPort, reply.Target.Port);

            OscMessage welcome = Decode(reply);
            Assert.Equal(AirPlayerProtocol.WelcomeAddress, welcome.Address);

            string version;
            bool abletonConnected;
            Assert.True(welcome.TryGetString(0, out version));
            Assert.True(welcome.TryGetBool(1, out abletonConnected));
            Assert.Equal("0.1.0", version);
            Assert.False(abletonConnected);

            Assert.True(engine.HasClient);
            Assert.Equal("Quest3S", engine.ClientDeviceName);
        }

        [Fact]
        public void HelloRaisesClientConnectedOnce()
        {
            CompanionEngine engine = NewEngine();
            int connectedEvents = 0;
            engine.ClientConnected += (name, endpoint) => connectedEvents++;

            Send(engine, AirPlayerProtocol.Hello("Quest3S"), 0.0);
            Send(engine, AirPlayerProtocol.Hello("Quest3S"), 1.0);

            Assert.Equal(1, connectedEvents);
        }

        [Fact]
        public void WrongProtocolVersionGetsIncompatible()
        {
            CompanionEngine engine = NewEngine();
            OscMessage badHello = new OscMessage(AirPlayerProtocol.HelloAddress,
                OscArg.Str("OldQuest"), OscArg.Int(AirPlayerProtocol.Version + 1));

            List<OutboundPacket> replies = Send(engine, badHello, 0.0);

            OutboundPacket reply = Assert.Single(replies);
            OscMessage incompatible = Decode(reply);
            Assert.Equal(AirPlayerProtocol.IncompatibleAddress, incompatible.Address);

            int required;
            Assert.True(incompatible.TryGetInt(0, out required));
            Assert.Equal(AirPlayerProtocol.Version, required);

            Assert.False(engine.HasClient);
        }

        [Fact]
        public void PingGetsPongEchoingTheSequence()
        {
            CompanionEngine engine = NewEngine();
            Send(engine, AirPlayerProtocol.Hello("Quest3S"), 0.0);

            List<OutboundPacket> replies = Send(engine, AirPlayerProtocol.Ping(77), 1.0);

            OutboundPacket reply = Assert.Single(replies);
            OscMessage pong = Decode(reply);
            Assert.Equal(AirPlayerProtocol.PongAddress, pong.Address);

            int sequence;
            Assert.True(pong.TryGetInt(0, out sequence));
            Assert.Equal(77, sequence);
        }

        [Fact]
        public void MalformedDatagramProducesNoReply()
        {
            CompanionEngine engine = NewEngine();
            byte[] garbage = { 1, 2, 3, 4, 5, 6, 7, 8 };

            List<OutboundPacket> replies = engine.HandleDatagram(garbage, garbage.Length, HeadsetSource, 0.0);

            Assert.Empty(replies);
        }

        [Fact]
        public void HelloWithMissingArgumentsProducesNoReply()
        {
            CompanionEngine engine = NewEngine();
            OscMessage badHello = new OscMessage(AirPlayerProtocol.HelloAddress, OscArg.Str("NoVersion"));

            List<OutboundPacket> replies = Send(engine, badHello, 0.0);

            Assert.Empty(replies);
            Assert.False(engine.HasClient);
        }

        [Fact]
        public void SilentClientTimesOutAfterFourSeconds()
        {
            CompanionEngine engine = NewEngine();
            string disconnectedDevice = null;
            engine.ClientDisconnected += name => disconnectedDevice = name;

            Send(engine, AirPlayerProtocol.Hello("Quest3S"), 0.0);

            engine.Tick(3.9);
            Assert.True(engine.HasClient);
            Assert.Null(disconnectedDevice);

            engine.Tick(4.1);
            Assert.False(engine.HasClient);
            Assert.Equal("Quest3S", disconnectedDevice);
        }

        [Fact]
        public void RegularPingsKeepTheClientAlive()
        {
            CompanionEngine engine = NewEngine();
            bool disconnected = false;
            engine.ClientDisconnected += name => disconnected = true;

            Send(engine, AirPlayerProtocol.Hello("Quest3S"), 0.0);
            for (int second = 1; second <= 10; second++)
            {
                Send(engine, AirPlayerProtocol.Ping(second), second);
                engine.Tick(second + 0.5);
            }

            Assert.False(disconnected);
            Assert.True(engine.HasClient);
        }

        [Fact]
        public void WelcomeReportsAbletonConnectedWhenSet()
        {
            CompanionEngine engine = NewEngine();
            engine.AbletonConnected = true;

            List<OutboundPacket> replies = Send(engine, AirPlayerProtocol.Hello("Quest3S"), 0.0);

            OscMessage welcome = Decode(replies[0]);
            bool abletonConnected;
            Assert.True(welcome.TryGetBool(1, out abletonConnected));
            Assert.True(abletonConnected);
        }
    }
}
