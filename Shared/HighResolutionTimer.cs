using System;
using System.Runtime.InteropServices;

namespace Shared
{
    public class HighResolutionTimer
    {
        private const string Lib = "Kernel32.dll";
        private readonly long _frequency;
        private readonly bool _isPerfCounterSupported;

        public HighResolutionTimer()
        {
            // Query the high-resolution timer only if it is supported.
            // A returned frequency of 1000 typically indicates that it is not
            // supported and is emulated by the OS using the same value that is
            // returned by Environment.TickCount.
            // A return value of 0 indicates that the performance counter is
            // not supported.
            var returnVal = QueryPerformanceFrequency(ref _frequency);

            if (returnVal != 0 && _frequency != 1000)
            {
                // The performance counter is supported.
                _isPerfCounterSupported = true;
            }
            else
            {
                // The performance counter is not supported. Use
                // Environment.TickCount instead.
                _frequency = 1000;
            }
        }

        public long Frequency
        {
            get { return _frequency; }
        }

        public long Value
        {
            get
            {
                if (!_isPerfCounterSupported)
                    return Environment.TickCount;

                long tickCount = 0;
                QueryPerformanceCounter(ref tickCount);
                return tickCount;
            }
        }

        [DllImport(Lib)]
        private static extern int QueryPerformanceCounter(ref long count);

        [DllImport(Lib)]
        private static extern int QueryPerformanceFrequency(ref long frequency);
    }
}