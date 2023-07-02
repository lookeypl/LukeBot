using System.Diagnostics;

namespace LukeBot.Logging
{
    internal class Timer
    {
        private double mFreq = 0;
        private long mStart = 0;

        public Timer()
        {
            mFreq = Stopwatch.Frequency;
        }

        public void Start()
        {
            mStart = Stopwatch.GetTimestamp();
        }

        public double Stop()
        {
            long stop = Stopwatch.GetTimestamp();
            return (double)(stop - mStart) / mFreq;
        }
    }
}
