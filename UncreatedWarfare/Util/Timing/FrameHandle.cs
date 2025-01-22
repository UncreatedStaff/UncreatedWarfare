using System;
using System.Runtime.CompilerServices;

namespace Uncreated.Warfare.Util.Timing;

/// <summary>
/// Allows capturing the current frame and later checking if something happened in the same frame.
/// </summary>
public readonly struct FrameHandle
{
 #pragma warning disable CS0649

    private readonly int _ticks;

 #pragma warning restore CS0649

    /// <summary>
    /// If the handle was ever initialized. Use <see cref="IsActive"/> to check if it is from the current frame.
    /// </summary>
    public bool IsValid => _ticks != 0;

    [Obsolete("Use FrameHandle.Claim instead.", error: true)]
    public FrameHandle() { }

    /// <summary>
    /// Create a new frame handle for the current frame that can be used to check that the frame is still current later on.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static FrameHandle Claim()
    {
        GameThread.AssertCurrent();

        // use + 1 to make 0 an invalid value
        int frameCt = Time.frameCount + 1;
        return Unsafe.As<int, FrameHandle>(ref frameCt);
    }
    
    /// <summary>
    /// Check if this <see cref="FrameHandle"/> was created in the current frame.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool IsActive()
    {
        GameThread.AssertCurrent();

        if (_ticks == 0)
            return false;

        int frameCt = Time.frameCount + 1;
        return _ticks == frameCt;
    }

    /// <summary>
    /// Check if this <see cref="FrameHandle"/> was created in the current frame, otherwise throw an exception.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    /// <exception cref="FrameExpiredException"/>
    public void AssertActive()
    {
        if (!IsActive())
            throw new FrameExpiredException();
    }
}

public sealed class FrameExpiredException : Exception
{
    internal FrameExpiredException() : base("Expected operation to occur within a previous frame.") { }
}