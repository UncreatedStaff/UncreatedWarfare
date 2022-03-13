using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Networking;

public sealed class CommandWaitTask
{
    public static List<CommandWaitTask> awaiters = new List<CommandWaitTask>(1);
    private readonly CommandWaitTaskAwaiter _awaiter;
    private readonly UCPlayer player;
    private readonly string command;
    static CommandWaitTask()
    {
        Rocket.Core.R.Commands.OnExecuteCommand += OnCommandExecuted;
    }
    private static void OnCommandExecuted(Rocket.API.IRocketPlayer player, Rocket.API.IRocketCommand command, ref bool cancel)
    {
        if (cancel) return;
        for (int i = 0; i < awaiters.Count; ++i)
        {
            if (awaiters[i].command.Equals(command.Name, StringComparison.OrdinalIgnoreCase) && player is UnturnedPlayer pl &&
                pl.Player.channel.owner.playerID.steamID.m_SteamID == awaiters[i].player.Steam64)
            {
                awaiters[i]._awaiter.TellRanCommand();
            }
        }
    }

    public CommandWaitTask(UCPlayer player, string command, int delayMs)
    {
        this.player = player;
        this.command = command;
        _awaiter = new CommandWaitTaskAwaiter(this);
        awaiters.Add(this);
        Task.Run(async () =>
        {
            await Task.Delay(delayMs);
            if (!_awaiter.IsCompleted)
                _awaiter.TellTimeout();
        }).ConfigureAwait(false);
    }
    public static CommandWaitTask WaitForCommand(UCPlayer player, string command, int delayMs)
    {
        return new CommandWaitTask(player, command, delayMs);
    }
    public CommandWaitTaskAwaiter GetAwaiter() => _awaiter;
    public sealed class CommandWaitTaskAwaiter : INotifyCompletion
    {
        public System.Action continuation;
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
        }
        public void TellTimeout()
        {
            _isTimeout = true;
            awaiters.Remove(_task);
            continuation.Invoke();
        }
        public void OnCompleted(System.Action continuation)
        {
            this.continuation = continuation;
        }
        public bool GetResult()
        {
            return _isCompleted && !_isTimeout;
        }
    }
}