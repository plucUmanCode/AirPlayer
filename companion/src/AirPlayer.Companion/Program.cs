using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AirPlayer.Core.Protocol;

namespace AirPlayer.Companion
{
    internal static class Program
    {
        private const string CompanionVersion = "0.1.0";

        private static async Task<int> Main(string[] args)
        {
            Console.WriteLine($"AirPlayer Companion v{CompanionVersion} — protocol v{AirPlayerProtocol.Version}");
            Console.WriteLine("Press Ctrl+C to quit.");
            Console.WriteLine();

            using CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
            };

            IPAddress localIp = PickLocalIPv4();
            if (localIp == null)
            {
                LogLine("ERROR: no usable IPv4 network interface found.");
                return 1;
            }
            LogLine($"Local IP: {localIp}");

            Stopwatch clock = Stopwatch.StartNew();
            double Now() => clock.Elapsed.TotalSeconds;

            CompanionEngine engine = new CompanionEngine(CompanionVersion);
            engine.Log += LogLine;
            engine.ClientConnected += (deviceName, endpoint) =>
                LogLine($"Headset connected: '{deviceName}' @ {endpoint.Address}");
            engine.ClientDisconnected += deviceName =>
                LogLine($"Headset '{deviceName}' lost (no heartbeat for {engine.ClientTimeoutSeconds:0.#} s).");

            using UdpClient oscSocket = new UdpClient(AirPlayerProtocol.QuestToCompanionPort);
            LogLine($"Listening for OSC on udp/{AirPlayerProtocol.QuestToCompanionPort}");

            MdnsResponderService mdns = null;
            try
            {
                mdns = new MdnsResponderService(
                    instanceLabel: $"AirPlayer Companion ({SanitizeLabel(Environment.MachineName)})",
                    hostLabel: SanitizeLabel(Environment.MachineName).ToLowerInvariant() + "-airplayer",
                    ipv4: localIp,
                    servicePort: AirPlayerProtocol.QuestToCompanionPort,
                    log: LogLine);
                mdns.Start();
                LogLine($"mDNS: announcing '{Core.Discovery.MdnsMessages.ServiceName}'");
            }
            catch (SocketException ex)
            {
                LogLine($"WARNING: mDNS unavailable ({ex.Message}). Use manual IP entry on the headset: {localIp}");
            }

            Task tickTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    engine.Tick(Now());
                    try
                    {
                        await Task.Delay(500, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            });

            while (!cts.Token.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await oscSocket.ReceiveAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    LogLine($"Receive error: {ex.Message}");
                    continue;
                }

                foreach (OutboundPacket packet in engine.HandleDatagram(result.Buffer, result.Buffer.Length, result.RemoteEndPoint, Now()))
                {
                    try
                    {
                        await oscSocket.SendAsync(packet.Data, packet.Data.Length, packet.Target);
                    }
                    catch (SocketException ex)
                    {
                        LogLine($"Send error to {packet.Target}: {ex.Message}");
                    }
                }
            }

            LogLine("Shutting down.");
            mdns?.Dispose();
            await tickTask;
            return 0;
        }

        private static void LogLine(string line)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {line}");
        }

        /// <summary>
        /// Picks the IPv4 address announced over mDNS: first operational,
        /// non-loopback, non-virtual interface with a private IPv4.
        /// </summary>
        private static IPAddress PickLocalIPv4()
        {
            IPAddress fallback = null;
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }
                foreach (UnicastIPAddressInformation address in nic.GetIPProperties().UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }
                    if (IsPrivate(address.Address))
                    {
                        return address.Address;
                    }
                    fallback ??= address.Address;
                }
            }
            return fallback;
        }

        private static bool IsPrivate(IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();
            return bytes[0] == 10 ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168);
        }

        /// <summary>Machine names can contain characters invalid in a DNS label; keep it simple.</summary>
        private static string SanitizeLabel(string raw)
        {
            string cleaned = Regex.Replace(raw, "[^A-Za-z0-9 _-]", "-");
            if (cleaned.Length == 0)
            {
                cleaned = "pc";
            }
            return cleaned.Length > 40 ? cleaned.Substring(0, 40) : cleaned;
        }
    }
}
