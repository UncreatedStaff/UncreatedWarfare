using System;
using System.Runtime.CompilerServices;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Utility for quickly checking if the current thread is the game thread.
/// </summary>
public static class GameThread
{
    /// <summary>
    /// <see langword="true"/> when fetched on the main thread, otherwise <see langword="false"/>.
    /// </summary>
    /// <remarks>Much more effecient than <see cref="ThreadUtil.IsGameThread"/>.</remarks>
    [ThreadStatic]
    public static readonly bool IsCurrent;

    /// <summary>
    /// Throw an error if this function isn't ran on the main thread.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertCurrent()
    {
        if (IsCurrent)
            return;

        throw new GameThreadException();
    }
    
    /// <summary>
    /// Throw an error if this function isn't ran on the main thread.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertCurrent(string feature)
    {
        if (IsCurrent)
            return;

        throw new GameThreadException(feature);
    }

    static GameThread()
    {
        GameThread.AssertCurrent();
        IsCurrent = true;
    }
}

/// <summary>
/// Thrown when something can't be ran off the main thread.
/// </summary>
public class GameThreadException : NotSupportedException
{
    public GameThreadException() : base("This feature can only be used when on the main thread.") { }
    public GameThreadException(string feature) : base($"{feature} can only be used when on the main thread.") { }
}