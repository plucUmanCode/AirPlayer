using System;

namespace AirPlayer.Core.Osc
{
    public enum OscArgType : byte
    {
        Int32,
        Float32,
        String,
        Bool
    }

    /// <summary>
    /// A single OSC argument. Value type to avoid boxing in hot paths.
    /// Supported OSC 1.0 type tags: i, f, s, T, F.
    /// </summary>
    public readonly struct OscArg : IEquatable<OscArg>
    {
        private readonly int _intValue;
        private readonly float _floatValue;
        private readonly string _stringValue;

        public OscArgType Type { get; }

        private OscArg(OscArgType type, int intValue, float floatValue, string stringValue)
        {
            Type = type;
            _intValue = intValue;
            _floatValue = floatValue;
            _stringValue = stringValue;
        }

        public static OscArg Int(int value)
        {
            return new OscArg(OscArgType.Int32, value, 0f, null);
        }

        public static OscArg Float(float value)
        {
            return new OscArg(OscArgType.Float32, 0, value, null);
        }

        public static OscArg Str(string value)
        {
            return new OscArg(OscArgType.String, 0, 0f, value ?? string.Empty);
        }

        public static OscArg Bool(bool value)
        {
            return new OscArg(OscArgType.Bool, value ? 1 : 0, 0f, null);
        }

        public int AsInt()
        {
            if (Type != OscArgType.Int32)
            {
                throw new InvalidOperationException($"OSC arg is {Type}, not Int32.");
            }
            return _intValue;
        }

        public float AsFloat()
        {
            if (Type != OscArgType.Float32)
            {
                throw new InvalidOperationException($"OSC arg is {Type}, not Float32.");
            }
            return _floatValue;
        }

        public string AsString()
        {
            if (Type != OscArgType.String)
            {
                throw new InvalidOperationException($"OSC arg is {Type}, not String.");
            }
            return _stringValue;
        }

        public bool AsBool()
        {
            if (Type != OscArgType.Bool)
            {
                throw new InvalidOperationException($"OSC arg is {Type}, not Bool.");
            }
            return _intValue != 0;
        }

        public bool Equals(OscArg other)
        {
            if (Type != other.Type)
            {
                return false;
            }
            switch (Type)
            {
                case OscArgType.Int32:
                case OscArgType.Bool:
                    return _intValue == other._intValue;
                case OscArgType.Float32:
                    return _floatValue.Equals(other._floatValue);
                case OscArgType.String:
                    return string.Equals(_stringValue, other._stringValue, StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is OscArg other && Equals(other);
        }

        public override int GetHashCode()
        {
            switch (Type)
            {
                case OscArgType.Int32:
                case OscArgType.Bool:
                    return (int)Type * 397 ^ _intValue;
                case OscArgType.Float32:
                    return (int)Type * 397 ^ _floatValue.GetHashCode();
                case OscArgType.String:
                    return (int)Type * 397 ^ (_stringValue != null ? _stringValue.GetHashCode() : 0);
                default:
                    return (int)Type;
            }
        }

        public override string ToString()
        {
            switch (Type)
            {
                case OscArgType.Int32:
                    return _intValue.ToString();
                case OscArgType.Float32:
                    return _floatValue.ToString("R");
                case OscArgType.String:
                    return "\"" + _stringValue + "\"";
                case OscArgType.Bool:
                    return _intValue != 0 ? "true" : "false";
                default:
                    return "?";
            }
        }
    }
}
