using AirPlayer.Core.Osc;
using Xunit;

namespace AirPlayer.Companion.Tests
{
    public class OscRoundtripTests
    {
        private static OscMessage Roundtrip(OscMessage original)
        {
            byte[] packet = OscWriter.Encode(original);
            Assert.Equal(0, packet.Length % 4);
            Assert.Equal(packet.Length, OscWriter.MeasureSize(original));

            OscMessage decoded;
            Assert.True(OscReader.TryParse(packet, packet.Length, out decoded));
            return decoded;
        }

        [Fact]
        public void IntFloatStringBoolRoundtrip()
        {
            OscMessage decoded = Roundtrip(new OscMessage("/test/all",
                OscArg.Int(42),
                OscArg.Float(3.25f),
                OscArg.Str("hello"),
                OscArg.Bool(true),
                OscArg.Bool(false)));

            Assert.Equal("/test/all", decoded.Address);
            Assert.Equal(5, decoded.Args.Length);
            Assert.Equal(42, decoded.Args[0].AsInt());
            Assert.Equal(3.25f, decoded.Args[1].AsFloat());
            Assert.Equal("hello", decoded.Args[2].AsString());
            Assert.True(decoded.Args[3].AsBool());
            Assert.False(decoded.Args[4].AsBool());
        }

        [Theory]
        [InlineData("/a")]
        [InlineData("/ab")]
        [InlineData("/abc")]
        [InlineData("/abcd")]
        [InlineData("/abcde")]
        public void AddressPaddingVariantsRoundtrip(string address)
        {
            OscMessage decoded = Roundtrip(new OscMessage(address, OscArg.Int(7)));
            Assert.Equal(address, decoded.Address);
            Assert.Equal(7, decoded.Args[0].AsInt());
        }

        [Fact]
        public void MessageWithoutArgsRoundtrip()
        {
            OscMessage decoded = Roundtrip(new OscMessage("/airplayer/session/sync"));
            Assert.Equal("/airplayer/session/sync", decoded.Address);
            Assert.Empty(decoded.Args);
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(int.MaxValue)]
        public void IntExtremesRoundtrip(int value)
        {
            OscMessage decoded = Roundtrip(new OscMessage("/i", OscArg.Int(value)));
            Assert.Equal(value, decoded.Args[0].AsInt());
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1.5f)]
        [InlineData(float.MaxValue)]
        [InlineData(float.Epsilon)]
        public void FloatExtremesRoundtrip(float value)
        {
            OscMessage decoded = Roundtrip(new OscMessage("/f", OscArg.Float(value)));
            Assert.Equal(value, decoded.Args[0].AsFloat());
        }

        [Theory]
        [InlineData("")]
        [InlineData("a")]
        [InlineData("abc")]
        [InlineData("abcd")]
        [InlineData("Casque Pier-Luc é🎹")]
        public void StringPaddingAndUtf8Roundtrip(string value)
        {
            OscMessage decoded = Roundtrip(new OscMessage("/s", OscArg.Str(value)));
            Assert.Equal(value, decoded.Args[0].AsString());
        }

        [Fact]
        public void WriteIntoCallerBufferMatchesEncode()
        {
            OscMessage message = new OscMessage("/buf", OscArg.Int(1), OscArg.Str("x"));
            byte[] expected = OscWriter.Encode(message);

            byte[] buffer = new byte[1024];
            int length = OscWriter.Write(message, buffer);

            Assert.Equal(expected.Length, length);
            for (int i = 0; i < length; i++)
            {
                Assert.Equal(expected[i], buffer[i]);
            }
        }
    }
}
