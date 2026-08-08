using AirPlayer.Core.Osc;
using AirPlayer.Core.Protocol;
using Xunit;

namespace AirPlayer.Companion.Tests
{
    public class ProtocolTests
    {
        [Fact]
        public void HelloCarriesDeviceNameAndProtocolVersion()
        {
            byte[] packet = OscWriter.Encode(AirPlayerProtocol.Hello("Quest3S"));

            OscMessage decoded;
            Assert.True(OscReader.TryParse(packet, packet.Length, out decoded));
            Assert.Equal(AirPlayerProtocol.HelloAddress, decoded.Address);

            string deviceName;
            int version;
            Assert.True(decoded.TryGetString(0, out deviceName));
            Assert.True(decoded.TryGetInt(1, out version));
            Assert.Equal("Quest3S", deviceName);
            Assert.Equal(AirPlayerProtocol.Version, version);
        }

        [Fact]
        public void WelcomeCarriesVersionAndAbletonFlag()
        {
            byte[] packet = OscWriter.Encode(AirPlayerProtocol.Welcome("0.1.0", false));

            OscMessage decoded;
            Assert.True(OscReader.TryParse(packet, packet.Length, out decoded));
            Assert.Equal(AirPlayerProtocol.WelcomeAddress, decoded.Address);

            string version;
            bool abletonConnected;
            Assert.True(decoded.TryGetString(0, out version));
            Assert.True(decoded.TryGetBool(1, out abletonConnected));
            Assert.Equal("0.1.0", version);
            Assert.False(abletonConnected);
        }

        [Fact]
        public void PingAndPongEchoSequenceNumbers()
        {
            byte[] ping = OscWriter.Encode(AirPlayerProtocol.Ping(1234));
            byte[] pong = OscWriter.Encode(AirPlayerProtocol.Pong(1234));

            OscMessage decodedPing;
            OscMessage decodedPong;
            Assert.True(OscReader.TryParse(ping, ping.Length, out decodedPing));
            Assert.True(OscReader.TryParse(pong, pong.Length, out decodedPong));

            int pingSeq;
            int pongSeq;
            Assert.True(decodedPing.TryGetInt(0, out pingSeq));
            Assert.True(decodedPong.TryGetInt(0, out pongSeq));
            Assert.Equal(1234, pingSeq);
            Assert.Equal(1234, pongSeq);
        }

        [Fact]
        public void IncompatibleCarriesRequiredVersion()
        {
            byte[] packet = OscWriter.Encode(AirPlayerProtocol.Incompatible(AirPlayerProtocol.Version));

            OscMessage decoded;
            Assert.True(OscReader.TryParse(packet, packet.Length, out decoded));
            Assert.Equal(AirPlayerProtocol.IncompatibleAddress, decoded.Address);

            int required;
            Assert.True(decoded.TryGetInt(0, out required));
            Assert.Equal(AirPlayerProtocol.Version, required);
        }
    }
}
