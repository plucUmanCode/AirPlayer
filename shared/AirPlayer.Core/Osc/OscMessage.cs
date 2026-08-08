using System;

namespace AirPlayer.Core.Osc
{
    /// <summary>
    /// An OSC message: an address pattern plus typed arguments.
    /// Immutable; build once, encode with <see cref="OscWriter"/>.
    /// </summary>
    public sealed class OscMessage
    {
        public static readonly OscArg[] NoArgs = new OscArg[0];

        public string Address { get; }
        public OscArg[] Args { get; }

        public OscMessage(string address, params OscArg[] args)
        {
            if (string.IsNullOrEmpty(address) || address[0] != '/')
            {
                throw new ArgumentException("OSC address must start with '/'.", nameof(address));
            }
            Address = address;
            Args = args ?? NoArgs;
        }

        public bool TryGetInt(int index, out int value)
        {
            value = 0;
            if (index < 0 || index >= Args.Length || Args[index].Type != OscArgType.Int32)
            {
                return false;
            }
            value = Args[index].AsInt();
            return true;
        }

        public bool TryGetFloat(int index, out float value)
        {
            value = 0f;
            if (index < 0 || index >= Args.Length || Args[index].Type != OscArgType.Float32)
            {
                return false;
            }
            value = Args[index].AsFloat();
            return true;
        }

        public bool TryGetString(int index, out string value)
        {
            value = null;
            if (index < 0 || index >= Args.Length || Args[index].Type != OscArgType.String)
            {
                return false;
            }
            value = Args[index].AsString();
            return true;
        }

        public bool TryGetBool(int index, out bool value)
        {
            value = false;
            if (index < 0 || index >= Args.Length || Args[index].Type != OscArgType.Bool)
            {
                return false;
            }
            value = Args[index].AsBool();
            return true;
        }

        public override string ToString()
        {
            return Args.Length == 0 ? Address : Address + " " + string.Join(" ", Args);
        }
    }
}
