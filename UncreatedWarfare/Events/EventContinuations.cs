using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events;
public static class EventContinuations
{
    private struct EventContinuationState<TArgs, TAction>
        where TArgs : class
        where TAction : Delegate
    {
        public TArgs EventArgs;
        public TAction Action;
        public UniTask<bool> Task;
        public CancellationToken Token;
    }

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
        EventContinuationState<TArgs, CancellableEventContinuationAsync<TArgs>> state;
        state.EventArgs = eventArgs;
        state.Action = continuation;
        state.Task = task;
        state.Token = token;

        UniTask.Create(state, static async state =>
        {
            if (!await state.Task)
                return;

            await UniTask.SwitchToMainThread(state.Token);

            await state.Action(state.EventArgs, state.Token);
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

        EventContinuationState<TArgs, CancellableEventContinuation<TArgs>> state;
        state.EventArgs = eventArgs;
        state.Action = continuation;
        state.Task = task;
        state.Token = token;

        UniTask.Create(state, static async state =>
        {
            // task returns false if cancelled
            if (!await state.Task)
                return;

            await UniTask.SwitchToMainThread(state.Token);
            
            state.Action(state.EventArgs);
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

        EventContinuationState<TArgs, CancellableEventContinuationAsync<TArgs>> state;
        state.EventArgs = eventArgs;
        state.Action = continuation;
        state.Task = task;
        state.Token = token;

        UniTask.Create(state, static async state =>
        {
            if (!await state.Task)
                return;

            await UniTask.SwitchToMainThread(state.Token);

            await state.Action(state.EventArgs, state.Token);
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

        EventContinuationState<TArgs, CancellableEventContinuation<TArgs>> state;
        state.EventArgs = eventArgs;
        state.Action = continuation;
        state.Task = task;
        state.Token = token;

        UniTask.Create(state, static async state =>
        {
            // task returns false if cancelled
            if (!await state.Task)
                return;

            await UniTask.SwitchToMainThread(state.Token);

            state.Action(state.EventArgs);
        });

        shouldAllow = false;
    }

    /// <summary>
    /// Try to execute an 'on requested' event on the main thread and if a context switch is required, run <paramref name="continuation"/> after the event is done executing.
    /// </summary>
    public static void DispatchNoCancel<TArgs>(TArgs eventArgs, EventDispatcher eventDispatcher, CancellationToken token, out bool shouldAllow, CancellableEventContinuationAsync<TArgs> continuation) where TArgs : class
    {
        GameThread.AssertCurrent();

        UniTask<bool> task = eventDispatcher.DispatchEventAsync(eventArgs, token);

        if (task.Status != UniTaskStatus.Pending)
        {
            shouldAllow = true;
            return;
        }

        // cancel and wait on continuation.

        EventContinuationState<TArgs, CancellableEventContinuationAsync<TArgs>> state;
        state.EventArgs = eventArgs;
        state.Action = continuation;
        state.Task = task;
        state.Token = token;

        UniTask.Create(state, static async state =>
        {
            await UniTask.SwitchToMainThread(state.Token);

            await state.Action(state.EventArgs, state.Token);
        });

        shouldAllow = false;
    }

    /// <summary>
    /// Try to execute an 'on requested' event on the main thread and if a context switch is required, run <paramref name="continuation"/> after the event is done executing.
    /// </summary>
    public static void DispatchNoCancel<TArgs>(TArgs eventArgs, EventDispatcher eventDispatcher, CancellationToken token, out bool shouldAllow, CancellableEventContinuation<TArgs> continuation) where TArgs : class
    {
        GameThread.AssertCurrent();

        UniTask<bool> task = eventDispatcher.DispatchEventAsync(eventArgs, token);

        if (task.Status != UniTaskStatus.Pending)
        {
            shouldAllow = true;
            return;
        }

        // cancel and wait on continuation.

        EventContinuationState<TArgs, CancellableEventContinuation<TArgs>> state;
        state.EventArgs = eventArgs;
        state.Action = continuation;
        state.Task = task;
        state.Token = token;

        UniTask.Create(state, static async state =>
        {
            await UniTask.SwitchToMainThread(state.Token);

            state.Action(state.EventArgs);
        });

        shouldAllow = false;
    }

    /// <summary>
    /// Try to execute an 'on requested' event on the main thread and if a context switch is required, run <paramref name="continuation"/> after the event is done executing.
    /// </summary>
    /// <remarks>This overload allows you to continue even if the task ends in time.</remarks>
    public static void DispatchNoCancel<TArgs>(TArgs eventArgs, EventDispatcher eventDispatcher, CancellationToken token, out bool shouldAllow, CancellableEventContinuationAsync<TArgs> continuation, Func<TArgs, bool> needsToContinue) where TArgs : class
    {
        GameThread.AssertCurrent();

        UniTask<bool> task = eventDispatcher.DispatchEventAsync(eventArgs, token);

        if (task.Status != UniTaskStatus.Pending)
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

        // cancel and wait on continuation.
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread(WarfareModule.Singleton.UnloadToken);

            await continuation(eventArgs, token);
        });

        shouldAllow = false;
    }

    /// <summary>
    /// Try to execute an 'on requested' event on the main thread and if a context switch is required, run <paramref name="continuation"/> after the event is done executing.
    /// </summary>
    /// <remarks>This overload allows you to continue even if the task ends in time.</remarks>
    public static void DispatchNoCancel<TArgs>(TArgs eventArgs, EventDispatcher eventDispatcher, CancellationToken token, out bool shouldAllow, CancellableEventContinuation<TArgs> continuation, Func<TArgs, bool> needsToContinue) where TArgs : class
    {
        GameThread.AssertCurrent();

        UniTask<bool> task = eventDispatcher.DispatchEventAsync(eventArgs, token);

        if (task.Status != UniTaskStatus.Pending)
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

        // cancel and wait on continuation.

        EventContinuationState<TArgs, CancellableEventContinuation<TArgs>> state;
        state.EventArgs = eventArgs;
        state.Action = continuation;
        state.Task = task;
        state.Token = token;

        UniTask.Create(state, static async state =>
        {
            await UniTask.SwitchToMainThread(state.Token);

            state.Action(state.EventArgs);
        });

        shouldAllow = false;
    }
}

public delegate UniTask CancellableEventContinuationAsync<in TArgs>(TArgs args, CancellationToken token = default);
public delegate void CancellableEventContinuation<in TArgs>(TArgs args);