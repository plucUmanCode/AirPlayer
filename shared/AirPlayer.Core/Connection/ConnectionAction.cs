namespace AirPlayer.Core.Connection
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }

    public enum ConnectionActionType
    {
        SendHello,
        SendPing
    }

    /// <summary>
    /// An outgoing action requested by the state machine. The caller (the
    /// Unity network layer) turns it into an OSC message and sends it.
    /// </summary>
    public readonly struct ConnectionAction
    {
        public ConnectionActionType Type { get; }
        public int PingSequence { get; }

        private ConnectionAction(ConnectionActionType type, int pingSequence)
        {
            Type = type;
            PingSequence = pingSequence;
        }

        public static ConnectionAction Hello()
        {
            return new ConnectionAction(ConnectionActionType.SendHello, -1);
        }

        public static ConnectionAction Ping(int sequence)
        {
            return new ConnectionAction(ConnectionActionType.SendPing, sequence);
        }
    }

    public sealed class ConnectionSettings
    {
        /// <summary>Interval between hello retries while connecting.</summary>
        public double HelloIntervalSeconds = 1.0;

        /// <summary>Heartbeat interval once connected.</summary>
        public double PingIntervalSeconds = 1.0;

        /// <summary>
        /// Consecutive unanswered pings before the connection is declared lost.
        /// 3 pings at 1 s interval means loss is detected in under 4 s (Loop 0 CA #4).
        /// </summary>
        public int MissedPongsBeforeDisconnect = 3;

        /// <summary>Sliding window size for the displayed average RTT (Loop 0 CA #3).</summary>
        public int RttWindowSize = 10;
    }
}
