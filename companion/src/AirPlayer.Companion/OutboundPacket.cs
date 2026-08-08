using System.Net;

namespace AirPlayer.Companion
{
    /// <summary>A UDP datagram the engine wants sent. Pure data, so the engine stays socket-free and testable.</summary>
    public readonly struct OutboundPacket
    {
        public byte[] Data { get; }
        public IPEndPoint Target { get; }

        public OutboundPacket(byte[] data, IPEndPoint target)
        {
            Data = data;
            Target = target;
        }
    }
}
