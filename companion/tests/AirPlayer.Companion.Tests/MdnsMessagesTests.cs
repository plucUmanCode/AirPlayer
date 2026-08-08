using System.Net;
using AirPlayer.Core.Discovery;
using Xunit;

namespace AirPlayer.Companion.Tests
{
    public class MdnsMessagesTests
    {
        [Fact]
        public void QueryIsRecognizedAsAirPlayerQuery()
        {
            byte[] query = MdnsMessages.BuildQuery();
            Assert.True(MdnsMessages.TryParseQuery(query, query.Length));
        }

        [Fact]
        public void ResponseIsNotMistakenForAQuery()
        {
            byte[] response = MdnsMessages.BuildResponse("Companion", "pc", IPAddress.Parse("192.168.1.10"), 9000);
            Assert.False(MdnsMessages.TryParseQuery(response, response.Length));
        }

        [Fact]
        public void QueryIsNotMistakenForAResponse()
        {
            byte[] query = MdnsMessages.BuildQuery();
            MdnsServiceInfo info;
            Assert.False(MdnsMessages.TryParseResponse(query, query.Length, out info));
        }

        [Fact]
        public void ResponseRoundtripCarriesInstanceAddressAndPort()
        {
            byte[] response = MdnsMessages.BuildResponse(
                "AirPlayer Companion (STUDIO-PC)", "studio-pc-airplayer",
                IPAddress.Parse("192.168.1.10"), 9000);

            MdnsServiceInfo info;
            Assert.True(MdnsMessages.TryParseResponse(response, response.Length, out info));
            Assert.Equal("AirPlayer Companion (STUDIO-PC)", info.InstanceName);
            Assert.Equal(IPAddress.Parse("192.168.1.10"), info.Address);
            Assert.Equal(9000, info.Port);
        }

        [Fact]
        public void GarbageIsRejectedByBothParsers()
        {
            byte[] garbage = { 0xDE, 0xAD, 0xBE, 0xEF, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            MdnsServiceInfo info;
            Assert.False(MdnsMessages.TryParseQuery(garbage, garbage.Length));
            Assert.False(MdnsMessages.TryParseResponse(garbage, garbage.Length, out info));
        }

        [Fact]
        public void TinyBuffersAreRejected()
        {
            byte[] tiny = { 0, 0, 0 };
            MdnsServiceInfo info;
            Assert.False(MdnsMessages.TryParseQuery(tiny, tiny.Length));
            Assert.False(MdnsMessages.TryParseResponse(tiny, tiny.Length, out info));
        }

        [Fact]
        public void NameParserFollowsCompressionPointers()
        {
            // Hand-crafted buffer: "foo.local" spelled out at offset 0,
            // then a compression pointer to it at offset 11.
            byte[] buffer =
            {
                3, (byte)'f', (byte)'o', (byte)'o',
                5, (byte)'l', (byte)'o', (byte)'c', (byte)'a', (byte)'l',
                0,
                0xC0, 0x00
            };

            int offset = 11;
            string name;
            Assert.True(MdnsMessages.TryReadName(buffer, buffer.Length, ref offset, out name));
            Assert.Equal("foo.local", name);
            Assert.Equal(13, offset); // pointer is 2 bytes; cursor lands right after it
        }

        [Fact]
        public void NameParserRejectsPointerLoops()
        {
            // A pointer that points at itself must not hang the parser.
            byte[] buffer = { 0xC0, 0x00 };
            int offset = 0;
            string name;
            Assert.False(MdnsMessages.TryReadName(buffer, buffer.Length, ref offset, out name));
        }

        [Fact]
        public void NameParserRejectsTruncatedLabels()
        {
            byte[] buffer = { 10, (byte)'a', (byte)'b' };
            int offset = 0;
            string name;
            Assert.False(MdnsMessages.TryReadName(buffer, buffer.Length, ref offset, out name));
        }
    }
}
