using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events;
public static class EventContinuations
{
    /// <summary>
    /// Try to execute an 'on requested' event on the main thread and if a context switch is required, run <paramref name="continuation"/> after the event is done executing.
    /// </summary>
    public static void Dispatch<TArgs>(TArgs eventArgs, EventDispatcher eventDispatcher, CancellationToken token, out bool shouldAllow, CancellableEventContinuationAsync<TArgs> continuation) where TArgs : class, ICancellable
    {
        GameThread.AssertCurrent();

        UniTask<bool> task = eventDispatcher.DispatchEventAsync(eventArgs, token);

        if (task.Status != UniTaskStatus.Pending)
        {
            shouldAllow = !eventArgs.IsActionCancelled;
            return;
        }

        // cancel and wait on continuation.
        UniTask.Create(async () =>
        {
            if (!await task)
                return;

            await UniTask.SwitchToMainThread(WarfareModule.Singleton.UnloadToken);

            await continuation(eventArgs, token);
        });

        shouldAllow = false;
    }

    /// <summary>
    /// Try to execute an 'on requested' event on the main thread and if a context switch is required, run <paramref name="continuation"/> after the event is done executing.
    /// </summary>
    public static void Dispatch<TArgs>(TArgs eventArgs, EventDispatcher eventDispatcher, CancellationToken token, out bool shouldAllow, CancellableEventContinuation<TArgs> continuation) where TArgs : class, ICancellable
    {
        GameThread.AssertCurrent();

        UniTask<bool> task = eventDispatcher.DispatchEventAsync(eventArgs, token);

        if (task.Status != UniTaskStatus.Pending)
        {
            shouldAllow = !eventArgs.IsActionCancelled;
            return;
        }

        // cancel and wait on continuation.
        UniTask.Create(async () =>
        {
            if (!await task)
                return;

            await UniTask.SwitchToMainThread(WarfareModule.Singleton.UnloadToken);

            continuation(eventArgs);
        });

        shouldAllow = false;
    }

    /// <summary>
    /// Try to execute an 'on requested' event on the main thread and if a context switch is required, run <paramref name="continuation"/> after the event is done executing.
    /// </summary>
    /// <remarks>This overload allows you to continue even if the task ends in time.</remarks>
    public static void Dispatch<TArgs>(TArgs eventArgs, EventDispatcher eventDispatcher, CancellationToken token, out bool shouldAllow, CancellableEventContinuationAsync<TArgs> continuation, Func<TArgs, bool> needsToContinue) where TArgs : class, ICancellable
    {
        GameThread.AssertCurrent();

        UniTask<bool> task = eventDispatcher.DispatchEventAsync(eventArgs, token);

        if (task.Status != UniTaskStatus.Pending)
        {
            if (!eventArgs.IsActionCancelled)
            {
                if (needsToContinue(eventArgs))
                {
                    UniTask.Create(() => continuation(eventArgs, token));
                    shouldAllow = false;
                    return;
                }

                shouldAllow = true;
                return;
            }

            shouldAllow = false;
            return;
        }

        // cancel and wait on continuation.
        UniTask.Create(async () =>
        {
            if (!await task)
                return;

            await UniTask.SwitchToMainThread(WarfareModule.Singleton.UnloadToken);

            await continuation(eventArgs, token);
        });

        shouldAllow = false;
    }

    /// <summary>
    /// Try to execute an 'on requested' event on the main thread and if a context switch is required, run <paramref name="continuation"/> after the event is done executing.
    /// </summary>
    /// <remarks>This overload allows you to continue even if the task ends in time.</remarks>
    public static void Dispatch<TArgs>(TArgs eventArgs, EventDispatcher eventDispatcher, CancellationToken token, out bool shouldAllow, CancellableEventContinuation<TArgs> continuation, Func<TArgs, bool> needsToContinue) where TArgs : class, ICancellable
    {
        GameThread.AssertCurrent();

        UniTask<bool> task = eventDispatcher.DispatchEventAsync(eventArgs, token);

        if (task.Status != UniTaskStatus.Pending)
        {
            if (!eventArgs.IsActionCancelled)
            {
                if (needsToContinue(eventArgs))
                {
                    continuation(eventArgs);
                    shouldAllow = false;
                    return;
                }

                shouldAllow = true;
                return;
            }

            shouldAllow = false;
            return;
        }

        // cancel and wait on continuation.
        UniTask.Create(async () =>
        {
            if (!await task)
                return;

            await UniTask.SwitchToMainThread(WarfareModule.Singleton.UnloadToken);

            continuation(eventArgs);
        });

        shouldAllow = false;
    }
}

public delegate UniTask CancellableEventContinuationAsync<in TArgs>(TArgs args, CancellationToken token = default);
public delegate void CancellableEventContinuation<in TArgs>(TArgs args);