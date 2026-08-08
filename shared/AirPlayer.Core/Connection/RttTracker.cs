using System;

namespace AirPlayer.Core.Connection
{
    /// <summary>
    /// Fixed-size sliding window of round-trip-time samples.
    /// Loop 0 acceptance criterion: display the average over the last 10 pings.
    /// </summary>
    public sealed class RttTracker
    {
        private readonly double[] _samples;
        private int _count;
        private int _next;

        public RttTracker(int windowSize)
        {
            if (windowSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(windowSize));
            }
            _samples = new double[windowSize];
        }

        public double LastMs { get; private set; }

        public int Count
        {
            get { return _count; }
        }

        public double AverageMs
        {
            get
            {
                if (_count == 0)
                {
                    return 0.0;
                }
                double sum = 0.0;
                for (int i = 0; i < _count; i++)
                {
                    sum += _samples[i];
                }
                return sum / _count;
            }
        }

        public void Add(double rttMs)
        {
            LastMs = rttMs;
            _samples[_next] = rttMs;
            _next = (_next + 1) % _samples.Length;
            if (_count < _samples.Length)
            {
                _count++;
            }
        }

        public void Reset()
        {
            _count = 0;
            _next = 0;
            LastMs = 0.0;
        }
    }
}
