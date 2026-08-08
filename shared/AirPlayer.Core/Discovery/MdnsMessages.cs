using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AirPlayer.Core.Discovery
{
    /// <summary>
    /// Minimal mDNS/DNS-SD packet encoding and decoding for the
    /// `_airplayer._udp.local` service. Both endpoints of the protocol are
    /// ours (companion responder, headset resolver), so only the subset we
    /// emit needs to be supported: PTR + SRV + A records, no emitted name
    /// compression. Parsing still handles compression pointers so responses
    /// relayed or rewritten by OS mDNS stacks are not misread.
    /// See docs/adr/003-decouverte-mdns.md.
    /// </summary>
    public static class MdnsMessages
    {
        public const string ServiceName = "_airplayer._udp.local";

        private const ushort TypePtr = 12;
        private const ushort TypeSrv = 33;
        private const ushort TypeA = 1;
        private const ushort TypeAny = 255;
        private const ushort ClassIn = 1;
        private const ushort CacheFlushBit = 0x8000;
        private const ushort UnicastResponseBit = 0x8000;
        private const uint RecordTtlSeconds = 120;

        /// <summary>
        /// Builds a PTR question for the AirPlayer service. The QU (unicast
        /// response) bit is set: Android needs a multicast lock to receive
        /// multicast, so the headset asks for a direct reply instead.
        /// </summary>
        public static byte[] BuildQuery()
        {
            List<byte> packet = new List<byte>(64);
            WriteHeader(packet, isResponse: false, questionCount: 1, answerCount: 0, additionalCount: 0);
            WriteName(packet, ServiceName);
            WriteU16(packet, TypePtr);
            WriteU16(packet, (ushort)(ClassIn | UnicastResponseBit));
            return packet.ToArray();
        }

        /// <summary>Returns true if the packet is a query containing a PTR question for our service.</summary>
        public static bool TryParseQuery(byte[] buffer, int length)
        {
            if (buffer == null || length < 12)
            {
                return false;
            }

            ushort flags = ReadU16(buffer, 2);
            if ((flags & 0x8000) != 0)
            {
                return false; // response bit set, not a query
            }

            ushort questionCount = ReadU16(buffer, 4);
            int offset = 12;
            for (int i = 0; i < questionCount; i++)
            {
                string name;
                if (!TryReadName(buffer, length, ref offset, out name))
                {
                    return false;
                }
                if (offset + 4 > length)
                {
                    return false;
                }
                ushort qtype = ReadU16(buffer, offset);
                offset += 4; // qtype + qclass

                if ((qtype == TypePtr || qtype == TypeAny) &&
                    string.Equals(name, ServiceName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Builds the announce/response packet: PTR (service -> instance),
        /// SRV (instance -> host:port) and A (host -> IPv4).
        /// </summary>
        public static byte[] BuildResponse(string instanceLabel, string hostLabel, IPAddress ipv4, ushort servicePort)
        {
            if (string.IsNullOrEmpty(instanceLabel) || instanceLabel.Contains("."))
            {
                throw new ArgumentException("Instance label must be a single non-empty DNS label.", nameof(instanceLabel));
            }
            if (string.IsNullOrEmpty(hostLabel) || hostLabel.Contains("."))
            {
                throw new ArgumentException("Host label must be a single non-empty DNS label.", nameof(hostLabel));
            }
            if (ipv4 == null || ipv4.GetAddressBytes().Length != 4)
            {
                throw new ArgumentException("An IPv4 address is required.", nameof(ipv4));
            }

            string instanceName = instanceLabel + "." + ServiceName;
            string hostName = hostLabel + ".local";

            List<byte> packet = new List<byte>(256);
            WriteHeader(packet, isResponse: true, questionCount: 0, answerCount: 1, additionalCount: 2);

            // Answer: PTR. Shared record, so no cache-flush bit.
            WriteName(packet, ServiceName);
            WriteU16(packet, TypePtr);
            WriteU16(packet, ClassIn);
            WriteU32(packet, RecordTtlSeconds);
            WriteLengthPrefixedName(packet, instanceName);

            // Additional: SRV.
            WriteName(packet, instanceName);
            WriteU16(packet, TypeSrv);
            WriteU16(packet, ClassIn | CacheFlushBit);
            WriteU32(packet, RecordTtlSeconds);
            int srvRdLengthPosition = packet.Count;
            WriteU16(packet, 0); // placeholder
            int srvRdStart = packet.Count;
            WriteU16(packet, 0); // priority
            WriteU16(packet, 0); // weight
            WriteU16(packet, servicePort);
            WriteName(packet, hostName);
            PatchU16(packet, srvRdLengthPosition, (ushort)(packet.Count - srvRdStart));

            // Additional: A.
            WriteName(packet, hostName);
            WriteU16(packet, TypeA);
            WriteU16(packet, ClassIn | CacheFlushBit);
            WriteU32(packet, RecordTtlSeconds);
            WriteU16(packet, 4);
            packet.AddRange(ipv4.GetAddressBytes());

            return packet.ToArray();
        }

        /// <summary>
        /// Extracts an AirPlayer companion from a response packet. Requires an
        /// SRV record under our service name plus an A record for its target host.
        /// </summary>
        public static bool TryParseResponse(byte[] buffer, int length, out MdnsServiceInfo info)
        {
            info = default(MdnsServiceInfo);
            if (buffer == null || length < 12)
            {
                return false;
            }

            ushort flags = ReadU16(buffer, 2);
            if ((flags & 0x8000) == 0)
            {
                return false; // not a response
            }

            ushort questionCount = ReadU16(buffer, 4);
            int recordCount = ReadU16(buffer, 6) + ReadU16(buffer, 8) + ReadU16(buffer, 10);
            int offset = 12;

            for (int i = 0; i < questionCount; i++)
            {
                string skipped;
                if (!TryReadName(buffer, length, ref offset, out skipped))
                {
                    return false;
                }
                offset += 4;
                if (offset > length)
                {
                    return false;
                }
            }

            string serviceSuffix = "." + ServiceName;
            string instanceName = null;
            string srvTargetHost = null;
            ushort srvPort = 0;
            Dictionary<string, IPAddress> hostAddresses = new Dictionary<string, IPAddress>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < recordCount; i++)
            {
                string ownerName;
                if (!TryReadName(buffer, length, ref offset, out ownerName))
                {
                    return false;
                }
                if (offset + 10 > length)
                {
                    return false;
                }
                ushort recordType = ReadU16(buffer, offset);
                offset += 8; // type + class + ttl
                ushort rdLength = ReadU16(buffer, offset);
                offset += 2;
                if (offset + rdLength > length)
                {
                    return false;
                }
                int rdStart = offset;

                if (recordType == TypeSrv && EndsWithOrdinalIgnoreCase(ownerName, serviceSuffix))
                {
                    if (rdLength < 7)
                    {
                        return false;
                    }
                    srvPort = ReadU16(buffer, rdStart + 4);
                    int targetOffset = rdStart + 6;
                    string target;
                    if (!TryReadName(buffer, length, ref targetOffset, out target))
                    {
                        return false;
                    }
                    srvTargetHost = target;
                    instanceName = ownerName.Substring(0, ownerName.Length - serviceSuffix.Length);
                }
                else if (recordType == TypeA && rdLength == 4)
                {
                    byte[] addressBytes = new byte[4];
                    Array.Copy(buffer, rdStart, addressBytes, 0, 4);
                    hostAddresses[ownerName] = new IPAddress(addressBytes);
                }

                offset = rdStart + rdLength;
            }

            if (instanceName == null || srvTargetHost == null)
            {
                return false;
            }

            IPAddress resolved;
            if (!hostAddresses.TryGetValue(srvTargetHost, out resolved))
            {
                if (hostAddresses.Count == 0)
                {
                    return false;
                }
                foreach (KeyValuePair<string, IPAddress> entry in hostAddresses)
                {
                    resolved = entry.Value;
                    break;
                }
            }

            info = new MdnsServiceInfo(instanceName, resolved, srvPort);
            return true;
        }

        // ---- DNS wire helpers ----

        private static void WriteHeader(List<byte> packet, bool isResponse, ushort questionCount, ushort answerCount, ushort additionalCount)
        {
            WriteU16(packet, 0); // transaction id, always 0 for mDNS
            WriteU16(packet, isResponse ? (ushort)0x8400 : (ushort)0x0000); // QR + AA for responses
            WriteU16(packet, questionCount);
            WriteU16(packet, answerCount);
            WriteU16(packet, 0); // authority
            WriteU16(packet, additionalCount);
        }

        private static void WriteName(List<byte> packet, string dottedName)
        {
            string[] labels = dottedName.Split('.');
            for (int i = 0; i < labels.Length; i++)
            {
                byte[] labelBytes = Encoding.UTF8.GetBytes(labels[i]);
                if (labelBytes.Length == 0 || labelBytes.Length > 63)
                {
                    throw new ArgumentException($"Invalid DNS label '{labels[i]}'.");
                }
                packet.Add((byte)labelBytes.Length);
                packet.AddRange(labelBytes);
            }
            packet.Add(0);
        }

        private static void WriteLengthPrefixedName(List<byte> packet, string dottedName)
        {
            int lengthPosition = packet.Count;
            WriteU16(packet, 0);
            int start = packet.Count;
            WriteName(packet, dottedName);
            PatchU16(packet, lengthPosition, (ushort)(packet.Count - start));
        }

        internal static bool TryReadName(byte[] buffer, int length, ref int offset, out string name)
        {
            name = null;
            StringBuilder builder = new StringBuilder(64);
            int cursor = offset;
            int endOfName = -1; // offset after the name at the original position
            int jumps = 0;

            while (true)
            {
                if (cursor < 0 || cursor >= length)
                {
                    return false;
                }
                byte lengthByte = buffer[cursor];

                if ((lengthByte & 0xC0) == 0xC0)
                {
                    if (cursor + 1 >= length)
                    {
                        return false;
                    }
                    if (++jumps > 32)
                    {
                        return false; // pointer loop
                    }
                    if (endOfName < 0)
                    {
                        endOfName = cursor + 2;
                    }
                    cursor = ((lengthByte & 0x3F) << 8) | buffer[cursor + 1];
                    continue;
                }

                if (lengthByte == 0)
                {
                    if (endOfName < 0)
                    {
                        endOfName = cursor + 1;
                    }
                    break;
                }

                if ((lengthByte & 0xC0) != 0)
                {
                    return false; // reserved label type
                }
                if (cursor + 1 + lengthByte > length)
                {
                    return false;
                }
                if (builder.Length > 512)
                {
                    return false;
                }
                if (builder.Length > 0)
                {
                    builder.Append('.');
                }
                builder.Append(Encoding.UTF8.GetString(buffer, cursor + 1, lengthByte));
                cursor += 1 + lengthByte;
            }

            offset = endOfName;
            name = builder.ToString();
            return true;
        }

        private static bool EndsWithOrdinalIgnoreCase(string value, string suffix)
        {
            return value.Length > suffix.Length &&
                   string.Compare(value, value.Length - suffix.Length, suffix, 0, suffix.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static void WriteU16(List<byte> packet, ushort value)
        {
            packet.Add((byte)(value >> 8));
            packet.Add((byte)value);
        }

        private static void WriteU32(List<byte> packet, uint value)
        {
            packet.Add((byte)(value >> 24));
            packet.Add((byte)(value >> 16));
            packet.Add((byte)(value >> 8));
            packet.Add((byte)value);
        }

        private static void PatchU16(List<byte> packet, int position, ushort value)
        {
            packet[position] = (byte)(value >> 8);
            packet[position + 1] = (byte)value;
        }

        private static ushort ReadU16(byte[] buffer, int offset)
        {
            return (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
        }
    }
}
