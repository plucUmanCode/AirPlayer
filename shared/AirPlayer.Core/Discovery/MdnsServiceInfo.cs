using System.Net;

namespace AirPlayer.Core.Discovery
{
    /// <summary>A discovered AirPlayer companion on the local network.</summary>
    public readonly struct MdnsServiceInfo
    {
        public string InstanceName { get; }
        public IPAddress Address { get; }
        public ushort Port { get; }

        public MdnsServiceInfo(string instanceName, IPAddress address, ushort port)
        {
            InstanceName = instanceName;
            Address = address;
            Port = port;
        }

        public override string ToString()
        {
            return $"{InstanceName} @ {Address}:{Port}";
        }
    }
}
