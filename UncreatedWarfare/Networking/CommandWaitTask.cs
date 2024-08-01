using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Commands.Dispatch;

namespace Uncreated.Warfare;

public class CommandWaiter : CustomYieldInstruction
{
    public static readonly List<CommandWaiter> ActiveWaiters = new List<CommandWaiter>(8);
    private readonly UCPlayer player;
    private readonly string? command;
    private readonly Type? commandType;
    private readonly float timeout;
    private Coroutine? timeoutCoroutine;
    public Action? OnExpire;
    private bool _wait;
    private bool _responded;
    public bool Responded => _responded;
    public override bool keepWaiting => _wait;
    public static CommandWaitTask WaitAsync(UCPlayer player, string commandName, int timeoutMs)
        => new CommandWaitTask(player, commandName, timeoutMs);
    public static CommandWaitTask WaitAsync(UCPlayer player, Type commandType, int timeoutMs)
        => new CommandWaitTask(player, commandType, timeoutMs);
    public CommandWaiter(UCPlayer player, string commandName, float timeout) : this(player, timeout)
    {
        command = commandName;
    }
    public CommandWaiter(UCPlayer player, Type commandType, float timeout) : this(player, timeout)
    {
        this.commandType = commandType;
    }
    private CommandWaiter(UCPlayer player, float timeout)
    {
        _wait = true;
        this.player = player;
        this.timeout = timeout;
        timeoutCoroutine = UCWarfare.I.StartCoroutine(TimeoutExpired());
    }
    private IEnumerator TimeoutExpired()
    {
        yield return new WaitForSeconds(timeout);
        _wait = false;
        timeoutCoroutine = null;
        Receive(false);
    }
    public void Cancel()
    {
        Receive(false);
    }
    private void Receive(bool responded)
    {
        _wait = false;
        _responded = responded;
        ActiveWaiters.Remove(this);
        if (timeoutCoroutine != null)
            UCWarfare.I.StopCoroutine(timeoutCoroutine);
    }
    internal static void OnCommandExecuted(UCPlayer? player, IExecutableCommand command)
    {
        for (int i = 0; i < ActiveWaiters.Count; ++i)
        {
            CommandWaiter aw = ActiveWaiters[i];
            if (aw.command != null)
            {
                if ((player == null && aw.player == null || player != null && aw.player != null && player.Steam64 == aw.player.Steam64) && aw.timeoutCoroutine != null && aw.command.Equals(command.CommandName, StringComparison.OrdinalIgnoreCase))
                {
                    aw.Receive(true);
                    return;
                }
            }
            else if (aw.commandType is not null)
            {
                if ((player == null && aw.player == null || player != null && aw.player != null && player.Steam64 == aw.player.Steam64) && aw.timeoutCoroutine != null && aw.commandType.IsInstanceOfType(command))
                {
                    aw.Receive(true);
                    return;
                }
            }
        }

        CommandWaitTask.OnCommandExecuted(player, command);
    }
    public sealed class CommandWaitTask
    {
        public static List<CommandWaitTask> awaiters = new List<CommandWaitTask>(1);
        private readonly CommandWaitTaskAwaiter _awaiter;
        private readonly UCPlayer player;
        private readonly string? command;
        private readonly Type? commandType;
        private readonly CancellationTokenSource cancel;
        public static void OnCommandExecuted(UCPlayer? player, IExecutableCommand command)
        {
            for (int i = 0; i < awaiters.Count; ++i)
            {
                CommandWaitTask aw = awaiters[i];
                if (aw.command != null)
                {
                    if ((player == null && aw.player == null || player != null && aw.player != null && player.Steam64 == aw.player.Steam64) && aw.command.Equals(command.CommandName, StringComparison.OrdinalIgnoreCase))
                    {
                        aw._awaiter.TellRanCommand();
                        break;
                    }
                }
                else if (aw.commandType != null)
                {
                    if ((player == null && aw.player == null || player != null && aw.player != null && player.Steam64 == aw.player.Steam64) && aw.commandType.IsInstanceOfType(command))
                    {
                        aw._awaiter.TellRanCommand();
                        break;
                    }
                }
            }
        }
        public CommandWaitTask(UCPlayer player, string command, int delayMs)
        {
            this.player = player;
            this.command = command;
            cancel = new CancellationTokenSource(delayMs);
            _awaiter = new CommandWaitTaskAwaiter(this);
            awaiters.Add(this);
            Task.Run(async () =>
            {
                await Task.Delay(delayMs);
                if (!_awaiter.IsCompleted)
                    _awaiter.TellTimeout();
            }, cancel.Token).ConfigureAwait(false);
        }
        public CommandWaitTask(UCPlayer player, Type type, int delayMs)
        {
            this.player = player;
            commandType = type;
            cancel = new CancellationTokenSource(delayMs);
            _awaiter = new CommandWaitTaskAwaiter(this);
            awaiters.Add(this);
            Task.Run(async () =>
            {
                await Task.Delay(delayMs);
                if (!_awaiter.IsCompleted)
                    _awaiter.TellTimeout();
            }, cancel.Token).ConfigureAwait(false);
        }

        public CommandWaitTaskAwaiter GetAwaiter() => _awaiter;
        public sealed class CommandWaitTaskAwaiter : INotifyCompletion
        {
            public Action continuation;
            private readonly CommandWaitTask _task;
            public byte[] rtn;
            public bool IsCompleted => _isCompleted;
            private bool _isTimeout;
            private bool _isCompleted;
            public CommandWaitTaskAwaiter(CommandWaitTask task)
            {
                _task = task;
            }
            public void TellRanCommand()
            {
                _isCompleted = true;
                _isTimeout = false;
                awaiters.Remove(_task);
                continuation.Invoke();
                _task.cancel.Cancel();
                _task.cancel.Dispose();
            }
            public void TellTimeout()
            {
                _isTimeout = true;
                awaiters.Remove(_task);
                continuation.Invoke();
                _task.cancel.Dispose();
            }
            public void OnCompleted(Action continuation)
            {
                this.continuation = continuation;
            }
            public bool GetResult()
            {
                return _isCompleted && !_isTimeout;
            }
        }
    }
}