namespace Uncreated.Warfare.Events;
public static class EventContinuations
{
    /// <summary>
    /// Try to execute an 'on requested' event on the main thread and if a context switch is required, run <paramref name="continuation"/> after the event is done executing.
    /// </summary>
    public static void Dispatch<TArgs>(TArgs eventArgs, EventDispatcher2 eventDispatcher, CancellationToken token, out bool shouldAllow, CancellableEventContinuationAsync<TArgs> continuation) where TArgs : ICancellable
    {
        ThreadUtil.assertIsGameThread();

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
    public static void Dispatch<TArgs>(TArgs eventArgs, EventDispatcher2 eventDispatcher, CancellationToken token, out bool shouldAllow, CancellableEventContinuation<TArgs> continuation) where TArgs : ICancellable
    {
        ThreadUtil.assertIsGameThread();

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
}

public delegate UniTask CancellableEventContinuationAsync<in TArgs>(TArgs args, CancellationToken token = default);
public delegate void CancellableEventContinuation<in TArgs>(TArgs args);