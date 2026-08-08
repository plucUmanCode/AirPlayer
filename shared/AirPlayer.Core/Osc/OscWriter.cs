using System;
using System.Buffers.Binary;
using System.Text;

namespace AirPlayer.Core.Osc
{
    /// <summary>
    /// Encodes <see cref="OscMessage"/> instances into OSC 1.0 binary packets.
    /// The Write overload targets a caller-owned buffer so hot paths can stay
    /// allocation-free (CLAUDE.md: no per-frame allocation in input/network loops).
    /// </summary>
    public static class OscWriter
    {
        /// <summary>Encodes into a caller-owned buffer. Returns the packet length in bytes.</summary>
        public static int Write(OscMessage message, byte[] buffer)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            int offset = 0;
            WritePaddedString(message.Address, buffer, ref offset);
            WriteTypeTags(message.Args, buffer, ref offset);

            for (int i = 0; i < message.Args.Length; i++)
            {
                OscArg arg = message.Args[i];
                switch (arg.Type)
                {
                    case OscArgType.Int32:
                        BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(buffer, offset, 4), arg.AsInt());
                        offset += 4;
                        break;
                    case OscArgType.Float32:
                        BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(buffer, offset, 4), BitConverter.SingleToInt32Bits(arg.AsFloat()));
                        offset += 4;
                        break;
                    case OscArgType.String:
                        WritePaddedString(arg.AsString(), buffer, ref offset);
                        break;
                    case OscArgType.Bool:
                        // T/F live entirely in the type tag string; no payload bytes.
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported OSC arg type {arg.Type}.");
                }
            }

            return offset;
        }

        /// <summary>Convenience overload that allocates an exactly-sized packet.</summary>
        public static byte[] Encode(OscMessage message)
        {
            byte[] buffer = new byte[MeasureSize(message)];
            Write(message, buffer);
            return buffer;
        }

        public static int MeasureSize(OscMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            int size = PaddedLength(Encoding.UTF8.GetByteCount(message.Address));
            size += PaddedLength(1 + CountTagChars(message.Args));

            for (int i = 0; i < message.Args.Length; i++)
            {
                switch (message.Args[i].Type)
                {
                    case OscArgType.Int32:
                    case OscArgType.Float32:
                        size += 4;
                        break;
                    case OscArgType.String:
                        size += PaddedLength(Encoding.UTF8.GetByteCount(message.Args[i].AsString()));
                        break;
                    case OscArgType.Bool:
                        break;
                }
            }

            return size;
        }

        private static int CountTagChars(OscArg[] args)
        {
            return args.Length;
        }

        private static void WriteTypeTags(OscArg[] args, byte[] buffer, ref int offset)
        {
            int start = offset;
            buffer[offset++] = (byte)',';
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].Type)
                {
                    case OscArgType.Int32:
                        buffer[offset++] = (byte)'i';
                        break;
                    case OscArgType.Float32:
                        buffer[offset++] = (byte)'f';
                        break;
                    case OscArgType.String:
                        buffer[offset++] = (byte)'s';
                        break;
                    case OscArgType.Bool:
                        buffer[offset++] = args[i].AsBool() ? (byte)'T' : (byte)'F';
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported OSC arg type {args[i].Type}.");
                }
            }
            Pad(buffer, start, ref offset);
        }

        private static void WritePaddedString(string value, byte[] buffer, ref int offset)
        {
            int start = offset;
            offset += Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, offset);
            Pad(buffer, start, ref offset);
        }

        /// <summary>Appends 1 to 4 NUL bytes so that (offset - start) is a multiple of 4.</summary>
        private static void Pad(byte[] buffer, int start, ref int offset)
        {
            int written = offset - start;
            int padded = (written + 4) & ~3;
            while (written < padded)
            {
                buffer[offset++] = 0;
                written++;
            }
        }

        private static int PaddedLength(int rawLength)
        {
            return (rawLength + 4) & ~3;
        }
    }
}
