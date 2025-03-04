using System;
using System.Collections.Generic;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.UI;

public class HudManager
{
    private readonly WarfareModule _module;
    private readonly IPlayerService _playerService;
    private int _handleCount;

    public HudManager(WarfareModule module, IPlayerService playerService)
    {
        _module = module;
        _playerService = playerService;
    }

    private void ClearHud()
    {
        ILifetimeScope scope = _module.ScopedProvider;

        foreach (IHudUIListener listener in scope.Resolve<IEnumerable<IHudUIListener>>())
        {
            listener.Hide(null);
        }

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            player.UnturnedPlayer.disablePluginWidgetFlag(EPluginWidgetFlags.Default);
        }
    }

    private void ShowHud()
    {
        ILifetimeScope scope = _module.ScopedProvider;

        foreach (IHudUIListener listener in scope.Resolve<IEnumerable<IHudUIListener>>())
        {
            listener.Restore(null);
        }

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            player.UnturnedPlayer.enablePluginWidgetFlag(EPluginWidgetFlags.Default);
        }
    }

    public IDisposable HideHud()
    {
        return new HudHandle(this);
    }

    private void Increment()
    {
        int value = Interlocked.Increment(ref _handleCount);
        if (value != 1)
            return;
        if (GameThread.IsCurrent)
        {
            ClearHud();
            return;
        }

        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();
            ClearHud();
        });
    }

    private void Decrement()
    {
        int value = Interlocked.Decrement(ref _handleCount);
        if (value != 0)
            return;

        if (GameThread.IsCurrent)
        {
            ShowHud();
            return;
        }

        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();
            ShowHud();
        });
    }

    private class HudHandle : IDisposable
    {
        private readonly HudManager _manager;

        public HudHandle(HudManager manager)
        {
            _manager = manager;
            _manager.Increment();
        }

        public void Dispose()
        {
            _manager.Decrement();
        }
    }
}
