using AirPlayer.Core.Osc;
using Xunit;

namespace AirPlayer.Companion.Tests
{
    public class OscReaderTests
    {
        [Fact]
        public void TruncatedIntPayloadIsRejected()
        {
            byte[] packet = OscWriter.Encode(new OscMessage("/x", OscArg.Int(1234)));
            OscMessage decoded;
            Assert.False(OscReader.TryParse(packet, packet.Length - 4, out decoded));
            Assert.Null(decoded);
        }

        [Fact]
        public void MissingLeadingSlashIsRejected()
        {
            // "abcd" + null padding, then a valid empty type tag string.
            byte[] packet = { (byte)'a', (byte)'b', (byte)'c', 0, (byte)',', 0, 0, 0 };
            OscMessage decoded;
            Assert.False(OscReader.TryParse(packet, packet.Length, out decoded));
        }

        [Fact]
        public void MissingTypeTagCommaIsRejected()
        {
            byte[] packet = { (byte)'/', (byte)'x', 0, 0, (byte)'i', 0, 0, 0 };
            OscMessage decoded;
            Assert.False(OscReader.TryParse(packet, packet.Length, out decoded));
        }

        [Fact]
        public void UnknownTypeTagIsRejected()
        {
            // Type tag 'd' (double) is not supported by our subset.
            byte[] packet =
            {
                (byte)'/', (byte)'x', 0, 0,
                (byte)',', (byte)'d', 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            };
            OscMessage decoded;
            Assert.False(OscReader.TryParse(packet, packet.Length, out decoded));
        }

        [Fact]
        public void UnalignedLengthIsRejected()
        {
            byte[] packet = OscWriter.Encode(new OscMessage("/x", OscArg.Int(1)));
            OscMessage decoded;
            Assert.False(OscReader.TryParse(packet, packet.Length - 1, out decoded));
        }

        [Fact]
        public void EmptyAndNullBuffersAreRejected()
        {
            OscMessage decoded;
            Assert.False(OscReader.TryParse(null, 0, out decoded));
            Assert.False(OscReader.TryParse(new byte[0], 0, out decoded));
        }

        [Fact]
        public void StringWithoutTerminatorIsRejected()
        {
            byte[] packet = { (byte)'/', (byte)'x', (byte)'y', (byte)'z' };
            OscMessage decoded;
            Assert.False(OscReader.TryParse(packet, packet.Length, out decoded));
        }
    }
}
