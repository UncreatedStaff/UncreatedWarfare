using System;

#if PROFILING

namespace Uncreated.Warfare.Profiling;

internal class ProfilerRecord
{
    public readonly UncreatedProfilerKey Info;


    // stored like this so they can be Interlocked.CompareExchange'd to avoid locks
    private long _packedAverageAndSampleCount;
    private long _packedMinMax;


    /// <summary>
    /// Average time in <c>ticks / <see cref="ProfilerUtil.FrequencyResolution"/></c>.
    /// </summary>
    public float Average
    {
        unsafe get
        {
            uint avgBits = unchecked( (uint)(Volatile.Read(ref _packedAverageAndSampleCount) >>> 32) );
            return *(float*)&avgBits;
        }
    }

    /// <summary>
    /// Number of times this function was called.
    /// </summary>
    public uint SampleCount => unchecked( (uint)Volatile.Read(ref _packedAverageAndSampleCount) );

    /// <summary>
    /// Minimum time in <c>ticks / <see cref="ProfilerUtil.FrequencyResolution"/></c>.
    /// </summary>
    public uint Minimum => unchecked( (uint)(Volatile.Read(ref _packedMinMax) >>> 32) );

    /// <summary>
    /// Maximum time in <c>ticks / <see cref="ProfilerUtil.FrequencyResolution"/></c>.
    /// </summary>
    public uint Maximum => unchecked( (uint)Volatile.Read(ref _packedMinMax) );

    public ProfilerRecord(UncreatedProfilerKey info)
    {
        Info = info;
        _packedMinMax = unchecked ( (long)0xFFFFFFFF00000000 );
    }

    public unsafe void AddSample(long stopwatchTicks)
    {
        stopwatchTicks /= ProfilerUtil.FrequencyResolution;

        if (stopwatchTicks > uint.MaxValue)
        {
            Console.WriteLine($"ticks too long: {stopwatchTicks}. {Info}.");
            return;
        }

        uint newTicks = (uint)stopwatchTicks;

        long oldPacked, newPacked;
        do
        {
            // atomic exchange
            oldPacked = Volatile.Read(ref _packedAverageAndSampleCount);

            unchecked
            {
                uint avgBits = (uint)(oldPacked >>> 32);
                float average = *(float*)&avgBits;
                uint samples = (uint)oldPacked;

                // incremental average
                ++samples;
                average += (newTicks - average) / samples;
                
                newPacked = ((long)*(uint*)&average << 32) | samples;
            }
        }
        while (Interlocked.CompareExchange(ref _packedAverageAndSampleCount, newPacked, oldPacked) != oldPacked);

        do
        {
            oldPacked = Volatile.Read(ref _packedMinMax);

            unchecked
            {
                uint min = (uint)(oldPacked >>> 32);
                uint max = (uint)oldPacked;

                min = Math.Min(min, newTicks);
                max = Math.Max(max, newTicks);
                newPacked = ((long)min << 32) | max;
            }
        }
        while (Interlocked.CompareExchange(ref _packedMinMax, newPacked, oldPacked) != oldPacked);
    }
}
#endif