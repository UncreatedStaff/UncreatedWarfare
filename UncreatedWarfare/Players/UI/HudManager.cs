using System;
using System.Collections.Generic;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.UI;

public class HudManager
{
    private readonly WarfareModule _module;
    private readonly IPlayerService _playerService;
    private readonly ILogger<HudManager> _logger;
    private int _handleCount;

    public HudManager(WarfareModule module, IPlayerService playerService, ILogger<HudManager> logger)
    {
        _module = module;
        _playerService = playerService;
        _logger = logger;
    }

    private void ClearHud()
    {
        ILifetimeScope scope = _module.ScopedProvider;

        foreach (IHudUIListener listener in scope.Resolve<IEnumerable<IHudUIListener>>())
        {
            try
            {
                listener.Hide(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hiding HUD: {listener.GetType()}.");
            }
        }

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            try
            {
                player.UnturnedPlayer.disablePluginWidgetFlag(EPluginWidgetFlags.Default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hiding PlayerLife HUD for player: {player}.");
            }
        }
    }

    private void ShowHud()
    {
        try
        {
            ILifetimeScope scope = _module.ScopedProvider;

            foreach (IHudUIListener listener in scope.Resolve<IEnumerable<IHudUIListener>>())
            {
                try
                {
                    listener.Restore(null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error showing HUD: {listener.GetType()}.");
                }
            }
        }
        finally
        {
            foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                try
                {
                    player.UnturnedPlayer.enablePluginWidgetFlag(EPluginWidgetFlags.Default);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error showing PlayerLife HUD for player: {player}.");
                }
            }
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
