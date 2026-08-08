using AirPlayer.Core.Osc;

namespace AirPlayer.Core.Protocol
{
    /// <summary>
    /// AirPlayer application protocol: addresses, ports, protocol version,
    /// and factories for the handshake/heartbeat messages (Loop 0 scope).
    /// See docs/architecture.md for the full protocol table.
    /// </summary>
    public static class AirPlayerProtocol
    {
        public const int Version = 1;

        public const int QuestToCompanionPort = 9000;
        public const int CompanionToQuestPort = 9001;

        public const string HelloAddress = "/airplayer/hello";
        public const string WelcomeAddress = "/airplayer/welcome";
        public const string IncompatibleAddress = "/airplayer/incompatible";
        public const string PingAddress = "/airplayer/ping";
        public const string PongAddress = "/airplayer/pong";

        public static OscMessage Hello(string deviceName)
        {
            return new OscMessage(HelloAddress, OscArg.Str(deviceName), OscArg.Int(Version));
        }

        public static OscMessage Welcome(string companionVersion, bool abletonConnected)
        {
            return new OscMessage(WelcomeAddress, OscArg.Str(companionVersion), OscArg.Bool(abletonConnected));
        }

        public static OscMessage Incompatible(int requiredVersion)
        {
            return new OscMessage(IncompatibleAddress, OscArg.Int(requiredVersion));
        }

        public static OscMessage Ping(int sequence)
        {
            return new OscMessage(PingAddress, OscArg.Int(sequence));
        }

        public static OscMessage Pong(int sequence)
        {
            return new OscMessage(PongAddress, OscArg.Int(sequence));
        }
    }
}
