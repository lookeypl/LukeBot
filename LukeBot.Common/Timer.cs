using System;
using System.Diagnostics;

namespace LukeBot.Common
{

public class Timer
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
