using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Utility for quickly checking if the current thread is the game thread.
/// </summary>
public static class GameThread
{
    [ThreadStatic]
    private static bool _isCurrent;

    /// <summary>
    /// <see langword="true"/> when fetched on the main thread, otherwise <see langword="false"/>.
    /// </summary>
    /// <remarks>Much more effecient than <see cref="ThreadUtil.IsGameThread"/>.</remarks>
    public static bool IsCurrent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _isCurrent;
    }


    /// <summary>
    /// Throw an error if this function isn't ran on the main thread.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertCurrent()
    {
#if PROFILING && DEBUG
        using IDisposable? profiler = ProfilerUtil.Profile();
#endif

        if (_isCurrent)
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
#if PROFILING && DEBUG
        using IDisposable? profiler = ProfilerUtil.Profile();
#endif

        if (_isCurrent)
            return;

        throw new GameThreadException(feature);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void Setup()
    {
        ThreadUtil.assertIsGameThread();
        _isCurrent = true;
    }

    /// <summary>
    /// Execute an action on the game thread.
    /// </summary>
    /// <param name="action">Code to run on the game thread.</param>
    public static void QueueOrRun(Action action)
    {
        if (IsCurrent)
            action();
        else
            GameThreadActions.Enqueue(action);
    }

    /// <summary>
    /// Execute an action on the game thread.
    /// </summary>
    /// <param name="action">Code to run on the game thread.</param>
    public static void Queue(Action action)
    {
        GameThreadActions.Enqueue(action);
    }

    internal static readonly ConcurrentQueue<Action> GameThreadActions = new ConcurrentQueue<Action>();

    /// <summary>
    /// At least this many actions from the queue are processed every frame, assuming there's this many queued.
    /// </summary>
    private const int MinDequeuedPerFrame = 16;

    /// <summary>
    /// Called every tick.
    /// </summary>
    internal static void ProcessGameThreadActions(ILogger logger, bool fullFlush = false)
    {
        int ct = GameThreadActions.Count;
        if (ct == 0)
            return;

#if DEBUG
        using IDisposable? profiler = ProfilerUtil.Profile();
#endif

        // don't dequeue them all at once, just dequeue some to avoid lag spikes when a bunch of things get queued at once
        int toDequeue;
        if (fullFlush)
        {
            toDequeue = int.MaxValue;
        }
        else if (ct > MinDequeuedPerFrame * MinDequeuedPerFrame)
        {
            toDequeue = (int)MathF.Sqrt(ct);
        }
        else
        {
            toDequeue = MinDequeuedPerFrame;
        }

        int i = 0;
        do
        {
            try
            {
                // avoid try-block in loop, kinda weird but works fine
                for (; i < toDequeue && GameThreadActions.TryDequeue(out Action action); ++i)
                {
                    action();
                }

                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error thrown from game thread callback.");
            }
        } while (i < toDequeue);
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