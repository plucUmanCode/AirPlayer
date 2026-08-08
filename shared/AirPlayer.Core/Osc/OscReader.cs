using System;
using System.Buffers.Binary;
using System.Text;

namespace AirPlayer.Core.Osc
{
    /// <summary>
    /// Decodes OSC 1.0 binary packets. Malformed input never throws:
    /// TryParse returns false and the packet is dropped by the caller.
    /// </summary>
    public static class OscReader
    {
        public static bool TryParse(byte[] buffer, int length, out OscMessage message)
        {
            message = null;
            if (buffer == null || length < 8 || length > buffer.Length || (length & 3) != 0)
            {
                return false;
            }

            int offset = 0;
            if (!TryReadPaddedString(buffer, length, ref offset, out string address))
            {
                return false;
            }
            if (address.Length == 0 || address[0] != '/')
            {
                return false;
            }

            if (!TryReadPaddedString(buffer, length, ref offset, out string typeTags))
            {
                return false;
            }
            if (typeTags.Length == 0 || typeTags[0] != ',')
            {
                return false;
            }

            int argCount = typeTags.Length - 1;
            OscArg[] args = argCount == 0 ? OscMessage.NoArgs : new OscArg[argCount];

            for (int i = 0; i < argCount; i++)
            {
                char tag = typeTags[i + 1];
                switch (tag)
                {
                    case 'i':
                    {
                        if (!TryReadInt32(buffer, length, ref offset, out int intValue))
                        {
                            return false;
                        }
                        args[i] = OscArg.Int(intValue);
                        break;
                    }
                    case 'f':
                    {
                        if (!TryReadInt32(buffer, length, ref offset, out int bits))
                        {
                            return false;
                        }
                        args[i] = OscArg.Float(BitConverter.Int32BitsToSingle(bits));
                        break;
                    }
                    case 's':
                    {
                        if (!TryReadPaddedString(buffer, length, ref offset, out string stringValue))
                        {
                            return false;
                        }
                        args[i] = OscArg.Str(stringValue);
                        break;
                    }
                    case 'T':
                        args[i] = OscArg.Bool(true);
                        break;
                    case 'F':
                        args[i] = OscArg.Bool(false);
                        break;
                    default:
                        // Unsupported type tag: drop the whole packet rather than guess.
                        return false;
                }
            }

            message = new OscMessage(address, args);
            return true;
        }

        private static bool TryReadInt32(byte[] buffer, int length, ref int offset, out int value)
        {
            value = 0;
            if (offset + 4 > length)
            {
                return false;
            }
            value = BinaryPrimitives.ReadInt32BigEndian(new ReadOnlySpan<byte>(buffer, offset, 4));
            offset += 4;
            return true;
        }

        private static bool TryReadPaddedString(byte[] buffer, int length, ref int offset, out string value)
        {
            value = null;
            if (offset >= length)
            {
                return false;
            }

            int terminator = Array.IndexOf(buffer, (byte)0, offset, length - offset);
            if (terminator < 0)
            {
                return false;
            }

            value = Encoding.UTF8.GetString(buffer, offset, terminator - offset);
            int consumed = terminator - offset + 1;
            int padded = (consumed + 3) & ~3;
            if (offset + padded > length)
            {
                return false;
            }
            offset += padded;
            return true;
        }
    }
}
