using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Interaction.Commands;

/// <summary>
/// Allows await a command execution from either all players or a given player.
/// </summary>
/// <remarks>Access through <see cref="CommandDispatcher.WaitForCommand"/></remarks>
public class CommandWaitTask : CustomYieldInstruction, IDisposable
{
    private readonly CommandWaitAwaiter _awaiter;
    private Lazy<CommandContext>? _contextFactory;
    private readonly DateTime _startTime;

    /// <summary>
    /// The command to wait for.
    /// </summary>
    public CommandInfo Command { get; }
    
    /// <summary>
    /// The user that should run the command. <see langword="null"/> matches all players.
    /// </summary>
    public ICommandUser? User { get; }

    /// <summary>
    /// The amount of time before the wait is aborted.
    /// </summary>
    public TimeSpan Timeout { get; }
    
    /// <summary>
    /// Flags deciding how the task is awaited.
    /// </summary>
    public CommandWaitOptions Options { get; }

    /// <summary>
    /// If the command was executed.
    /// </summary>
    public bool IsSuccessfullyExecuted => _awaiter.IsSuccessfullyExecuted;

    /// <summary>
    /// If the command was executed or timed out.
    /// </summary>
    public bool IsCompleted => _awaiter.IsCompleted;

    /// <summary>
    /// If the command timed out.
    /// </summary>
    /// <remarks>Always <see langword="true"/> if <see cref="IsDisconnected"/> is <see langword="true"/>.</remarks>
    public bool IsTimedOut => _awaiter.IsTimedOut;

    /// <summary>
    /// If the user disconnected before the timeout.
    /// </summary>
    public bool IsDisconnected => _awaiter.IsDisconnected;

    /// <summary>
    /// If the task was aborted before the timeout.
    /// </summary>
    public bool IsAborted => _awaiter.IsAborted;

    /// <summary>
    /// If the task was cancelled using a <see cref="CancellationToken"/> before the timeout.
    /// </summary>
    public bool IsCancelled => _awaiter.IsCancelled;

    /// <summary>
    /// Property for <see cref="CustomYieldInstruction"/>. <see langword="true"/> when a coroutine should continue waiting on this task.
    /// </summary>
    public override bool keepWaiting => !_awaiter.IsCompleted;

    /// <summary>
    /// Get info about the command execution after completion.
    /// </summary>
    /// <exception cref="InvalidOperationException">Task not completed.</exception>
    /// <exception cref="TimeoutException">Task timed out or user disconnected.</exception>
    /// <exception cref="OperationCanceledException">Operation was aborted.</exception>
    public CommandWaitResult Result => _awaiter.GetResult();


    /// <exception cref="InvalidOperationException">Services <see cref="CommandDispatcher"/> and/or <see cref="ILoopTickerFactory"/> not found.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="token"/> is cancelled.</exception>
    public CommandWaitTask(CommandInfo command, ICommandUser? user, TimeSpan timeout, CancellationToken token, CommandWaitOptions flags, IServiceProvider serviceProvider)
        : this(command, user, timeout, token, flags,
            serviceProvider.GetRequiredService<CommandDispatcher>(), serviceProvider.GetRequiredService<ILoopTickerFactory>()) { }
    public CommandWaitTask(CommandInfo command, ICommandUser? user, TimeSpan timeout, CancellationToken token, CommandWaitOptions flags, CommandDispatcher commandDispatcher, ILoopTickerFactory tickerFactory)
    {
        if ((flags & CommandWaitOptions.ThrowOnCancel) != 0)
            token.ThrowIfCancellationRequested();
        if (timeout.Ticks < 0)
            timeout = System.Threading.Timeout.InfiniteTimeSpan;

        Options = flags;
        Timeout = timeout;
        Command = command;
        User = user;
        _startTime = DateTime.UtcNow;
        _awaiter = new CommandWaitAwaiter(this, commandDispatcher, tickerFactory, token);
    }

    public CommandWaitAwaiter GetAwaiter()
    {
        return _awaiter;
    }

    /// <summary>
    /// Cancel this task.
    /// </summary>
    public void Abort()
    {
        _awaiter.Abort();
    }

    /// <summary>
    /// Notify this task that it's command has been executed by <paramref name="user"/>.
    /// </summary>
    public void MarkCompleted(Lazy<CommandContext> contextFactory, ICommandUser user)
    {
        _awaiter.MarkCompleted(contextFactory, user);
    }

    /// <summary>
    /// Notify this task that it's user disconnected.
    /// </summary>
    public void MarkDisconnected()
    {
        _awaiter.MarkDisconnected();
    }

    /// <summary>
    /// Dispose will be called after the command is executed so calling dispose manually usually isn't necessary.
    /// </summary>
    void IDisposable.Dispose()
    {
        _awaiter.Dispose();
    }

    public class CommandWaitAwaiter : ICriticalNotifyCompletion, IDisposable
    {
        // - state -
        // 00001 = completed
        // 00010 = timed out or disconnected
        // 00100 = disconnected
        // 01000 = aborted
        // 10000 = cancelled (by CancellationToken)
        private int _state;
        private readonly CommandWaitTask _task;
        private readonly bool _registered;
        private Action? _continuation;
        private ILoopTicker? _ticker;
        private readonly CancellationTokenRegistration _registration;
        private ICommandUser? _user;

        /// <summary>
        /// If the command was executed.
        /// </summary>
        public bool IsSuccessfullyExecuted => _state == 0b00001;

        /// <summary>
        /// If the command was executed or timed out.
        /// </summary>
        public bool IsCompleted => (_state & 0b00001) != 0;

        /// <summary>
        /// If the command timed out.
        /// </summary>
        /// <remarks>Always <see langword="true"/> if <see cref="IsDisconnected"/> is <see langword="true"/>.</remarks>
        public bool IsTimedOut => (_state & 0b00010) != 0;

        /// <summary>
        /// If the user disconnected before the timeout.
        /// </summary>
        public bool IsDisconnected => (_state & 0b00100) != 0;

        /// <summary>
        /// If the task was aborted before the timeout.
        /// </summary>
        /// <remarks>Always <see langword="true"/> if <see cref="IsCancelled"/> is <see langword="true"/>.</remarks>
        public bool IsAborted => (_state & 0b01000) != 0;

        /// <summary>
        /// If the task was cancelled using a <see cref="CancellationToken"/> before the timeout.
        /// </summary>
        public bool IsCancelled => (_state & 0b10000) != 0;
        internal CommandWaitAwaiter(CommandWaitTask task, CommandDispatcher dispatcher, ILoopTickerFactory tickerFactory, CancellationToken token)
        {
            _task = task;

            if (task.User is { IsDisconnected: true })
            {
                _state = 0b00101;
                _task._contextFactory = null;
                return;
            }

            if (task.Timeout == TimeSpan.Zero)
            {
                _state = 0b00011;
                _task._contextFactory = null;
                return;
            }

            if (token.IsCancellationRequested)
            {
                _state = 0b11001;
                _task._contextFactory = null;
                return;
            }

            dispatcher.RegisterCommandWaitTask(_task);
            _registered = true;

            if (token.CanBeCanceled)
            {
                _registration = token.Register(HandleCancellationToken);
            }

            if (task.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
                return;

            _ticker = tickerFactory.CreateTicker(task.Timeout, System.Threading.Timeout.InfiniteTimeSpan, task, false, HandleTimeout);
        }

        /// <inheritdoc />
        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

        /// <inheritdoc />
        public void OnCompleted(Action continuation)
        {
            _continuation = continuation;
            if (IsCompleted)
                _continuation.Invoke();
        }

        /// <summary>
        /// Get info about the command execution after completion.
        /// </summary>
        /// <exception cref="InvalidOperationException">Task not completed.</exception>
        /// <exception cref="TimeoutException">Task timed out or user disconnected.</exception>
        /// <exception cref="OperationCanceledException">Operation was aborted.</exception>
        public CommandWaitResult GetResult()
        {
            if (!IsCompleted)
                throw new InvalidOperationException("Invalid explicit use of GetResult on CommandWaitAwaiter.");

            if ((_task.Options & CommandWaitOptions.ThrowOnTimeout) != 0)
            {
                if (IsDisconnected)
                    throw new TimeoutException($"Command {_task.Command.CommandName} not executed by {_task.User} before they disconnected.");

                if (IsTimedOut)
                    throw new TimeoutException($"Command {_task.Command.CommandName} not executed by {_task.User} in time.");
            }

            if (IsCancelled && (_task.Options & CommandWaitOptions.ThrowOnCancel) != 0)
                throw new OperationCanceledException($"Command {_task.Command.CommandName} awaiter was cancelled.");

            if (IsAborted && (_task.Options & CommandWaitOptions.ThrowOnAbort) != 0)
                throw new OperationCanceledException($"Command {_task.Command.CommandName} awaiter was aborted before it could complete.");
            
            return new CommandWaitResult(_state, _task.Command, DateTime.UtcNow - _task._startTime, _task._contextFactory, _user);
        }

        /// <summary>
        /// Dispose will be called after the command is executed so calling dispose manually usually isn't necessary.
        /// </summary>
        public void Dispose()
        {
            if (_registered)
            {
                lock (_task.Command.WaitTasks)
                {
                    _task.Command.WaitTasks.Remove(_task);
                }
            }

            _registration.Dispose();
            ILoopTicker? ticker = Interlocked.Exchange(ref _ticker, null);
            ticker?.Dispose();
        }

        private void HandleCompleteIntl(int state, Lazy<CommandContext>? contextFactory, ICommandUser? user)
        {
            Dispose();
            if ((Interlocked.Exchange(ref _state, state) & 1) != 0)
                return;

            _task._contextFactory = contextFactory;
            _user = user;

            _continuation?.Invoke();
        }

        internal void MarkCompleted(Lazy<CommandContext> contextFactory, ICommandUser user)
        {
            HandleCompleteIntl(0b00001, contextFactory, user);
        }

        internal void Abort()
        {
            HandleCompleteIntl(0b01001, null, null);
        }
        
        private void HandleCancellationToken()
        {
            HandleCompleteIntl(0b11001, null, null);
        }

        internal void MarkDisconnected()
        {
            HandleCompleteIntl(0b00111, null, null);
        }

        private void HandleTimeout(ILoopTicker<CommandWaitTask> ticker, TimeSpan timesincestart, TimeSpan deltatime)
        {
            HandleCompleteIntl(0b00011, null, null);
        }
    }
}

public readonly struct CommandWaitResult
{
    private readonly Lazy<CommandContext>? _contextFactory;
    private readonly int _state;

    /// <summary>
    /// If the command actually ran successfully.
    /// </summary>
    public bool IsSuccessfullyExecuted => _state == 0b0001;

    /// <summary>
    /// If the command timed out.
    /// </summary>
    public bool IsTimedOut => (_state & 0b0010) != 0;

    /// <summary>
    /// If the user disconnected before timeout.
    /// </summary>
    public bool IsDisconnected => (_state & 0b0100) != 0;

    /// <summary>
    /// If the task was aborted before the timeout.
    /// </summary>
    /// <remarks>Always <see langword="true"/> if <see cref="IsCancelled"/> is <see langword="true"/>.</remarks>
    public bool IsAborted => (_state & 0b1000) != 0;

    /// <summary>
    /// If the task was cancelled using a <see cref="CancellationToken"/> before the timeout.
    /// </summary>
    public bool IsCancelled => (_state & 0b10000) != 0;

    /// <summary>
    /// The command that was executed.
    /// </summary>
    public CommandInfo Command { get; }

    /// <summary>
    /// Amount of time between when listening started and when the command finished.
    /// </summary>
    public TimeSpan ResponseTime { get; }

    /// <summary>
    /// The user that actually ran the command.
    /// </summary>
    /// <remarks>This will be <see langword="null"/> if the command execution didn't happen because of a timeout, disconnect, etc.</remarks>
    public ICommandUser? User { get; }
    internal CommandWaitResult(int state, CommandInfo command, TimeSpan responseTime, Lazy<CommandContext>? contextFactory, ICommandUser? user)
    {
        _state = state;
        Command = command;
        ResponseTime = responseTime;
        _contextFactory = contextFactory;
        User = user;
    }
    
    /// <summary>
    /// Get the context of the executed command.
    /// </summary>
    /// <exception cref="InvalidOperationException">Command timed out or otherwise wasn't fully executed.</exception>
    public CommandContext GetExecutionContext()
    {
        if (_contextFactory == null)
            throw new InvalidOperationException($"Command {Command.CommandName} didn't get executed by {User}.");

        return _contextFactory.Value;
    }

    /// <summary>
    /// Get the context of the executed command or <see langword="null"/> if it didn't fully execute.
    /// </summary>
    public CommandContext? GetExecutionContextOrNull()
    {
        return _contextFactory?.Value;
    }
}

/// <summary>
/// Configures how a <see cref="CommandWaitTask"/> awaits.
/// </summary>
[Flags]
public enum CommandWaitOptions
{
    None,

    /// <summary>
    /// A <see cref="OperationCanceledException"/> will be thrown on cancellation via <see cref="CommandWaitTask.Abort"/>
    /// or another command being ran if <see cref="AbortOnOtherCommandExecuted"/> is active.
    /// </summary>
    ThrowOnAbort = 1 << 0,

    /// <summary>
    /// A <see cref="OperationCanceledException"/> will be thrown on cancellation via a <see cref="CancellationToken"/>.
    /// </summary>
    ThrowOnCancel = 1 << 1,

    /// <summary>
    /// A <see cref="TimeoutException"/> will be thrown on timeout.
    /// </summary>
    ThrowOnTimeout = 1 << 2,

    /// <summary>
    /// The original command won't execute if it has a body.
    /// </summary>
    BlockOriginalExecution = 1 << 3,

    /// <summary>
    /// If the user executes another command, it will abort this request.
    /// </summary>
    AbortOnOtherCommandExecuted = 1 << 4,

    /// <summary>
    /// The default configuration for a <see cref="CommandWaitTask"/>.
    /// </summary>
    Default = ThrowOnCancel | BlockOriginalExecution | AbortOnOtherCommandExecuted
}