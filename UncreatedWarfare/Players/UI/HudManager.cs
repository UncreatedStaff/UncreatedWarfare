using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.UI;

/// <summary>
/// Allows hiding and re-showing the entire HUD when needed, tracking overlapping requests if needed.
/// </summary>
/// <remarks>Also contains the logic for tracking plugin voting.</remarks>
public sealed class HudManager : IEventListener<PlayerLeft>
{
    private readonly WarfareModule _module;
    private readonly IPlayerService _playerService;
    private readonly ILogger<HudManager> _logger;

    private int _cachedIsHiddenForAnyVersion;
    private int _isHiddenForAnyVersion;
    private bool _isHiddenForAny;

    /// <summary>
    /// Incremented when the HUD needs to be hidden for all players, then decremented when that request gets disposed.
    /// </summary>
    private int _globalHandleCount;
    
    /// <summary>
    /// Invoked when a player's plugin voting status changes.
    /// </summary>
    /// <remarks>Used by the Duty UI to lower it when a vote is happening.</remarks>
    public event Action<WarfarePlayer, bool>? OnPluginVotingUpdated;

    public HudManager(WarfareModule module, IPlayerService playerService, ILogger<HudManager> logger)
    {
        _module = module;
        _playerService = playerService;
        _logger = logger;
    }

    /// <summary>
    /// Whether or not HUD elements are hidden for all players. Note that some may be hidden for only specific players.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool IsHiddenForAllPlayers
    {
        get
        {
            GameThread.AssertCurrent();
            return _globalHandleCount > 0;
        }
    }

    /// <summary>
    /// Whether or not HUD elements are hidden for at least one online player (or all players).
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool IsHiddenForAnyPlayers
    {
        get
        {
            GameThread.AssertCurrent();
            if (_globalHandleCount > 0)
                return true;

            if (_isHiddenForAnyVersion != _cachedIsHiddenForAnyVersion)
            {
                RecalcIsHidden();
            }

            return _isHiddenForAny;
        }
    }

    private void RecalcIsHidden()
    {
        bool isHidden = false;
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            if (player.Component<HudPlayerComponent>().HandleCount <= 0)
                continue;

            isHidden = true;
            break;
        }

        _isHiddenForAny = isHidden;
        _cachedIsHiddenForAnyVersion = _isHiddenForAnyVersion;
    }

    /// <summary>
    /// Whether or not HUD elements are hidden for the given player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool IsHidden(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        if (!player.IsOnline)
            return true;

        return _globalHandleCount > 0 || player.Component<HudPlayerComponent>().HandleCount > 0;
    }

    /// <summary>
    /// Sets whether or not a player is currently voting with a plugin-controlled vote HUD.
    /// </summary>
    public void SetIsPluginVoting(WarfarePlayer player, bool value)
    {
        HudPlayerComponent comp = player.Component<HudPlayerComponent>();
        if (comp.IsPluginVoting == value)
            return;

        comp.IsPluginVoting = value;
        try
        {
            OnPluginVotingUpdated?.Invoke(player, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnPluginVotingUpdated.");
        }
    }

    /// <summary>
    /// Sets whether or not all players are currently voting with a plugin-controlled vote HUD.
    /// </summary>
    public void SetAllIsPluginVoting(bool value)
    {
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            SetIsPluginVoting(player, value);
        }
    }

    /// <summary>
    /// Gets whether or not a player is currently voting with a plugin-controlled vote HUD.
    /// </summary>
    public bool GetIsPluginVoting(WarfarePlayer player)
    {
        return player.Component<HudPlayerComponent>().IsPluginVoting;
    }

    /// <summary>
    /// Hides the HUD for all players until the returned <see cref="IDisposable"/> is disposed.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public IDisposable HideHud()
    {
        return new HudHandle(this, null);
    }

    /// <summary>
    /// Hides the HUD for <paramref name="player"/> until the returned <see cref="IDisposable"/> is disposed.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public IDisposable HideHud(WarfarePlayer player)
    {
        return new HudHandle(this, player);
    }

    private void IncrementHandleCount(HudPlayerComponent? player)
    {
        if (!GameThread.IsCurrent)
        {
            UniTask.Create((@this: this, player), static async args =>
            {
                await UniTask.SwitchToMainThread();
                args.@this.IncrementHandleCount(args.player);
            });
            return;
        }

        if (player == null)
        {
            ++_globalHandleCount;
            if (_globalHandleCount != 1)
                return;
        }
        else
        {
            ++player.HandleCount;
            if (player.HandleCount != 1 || _globalHandleCount > 0 || !player.Player.IsOnline)
                return;
            _isHiddenForAny = true;
            _cachedIsHiddenForAnyVersion = _isHiddenForAnyVersion;
        }

        ClearHud(player);
    }

    private void DecrementHandleCount(HudPlayerComponent? player)
    {
        if (!GameThread.IsCurrent)
        {
            UniTask.Create((@this: this, player), static async args =>
            {
                await UniTask.SwitchToMainThread();
                args.@this.DecrementHandleCount(args.player);
            });
            return;
        }

        if (player == null)
        {
            --_globalHandleCount;
            if (_globalHandleCount != 0)
                return;
        }
        else
        {
            --player.HandleCount;
            if (player.HandleCount != 0 || _globalHandleCount != 0 || !player.Player.IsOnline)
                return;
            ++_isHiddenForAnyVersion;
        }

        ShowHud(player);
    }

    private void ClearHud(HudPlayerComponent? comp)
    {
        try
        {
            ILifetimeScope scope = _module.ScopedProvider;

            foreach (IHudUIListener listener in scope.Resolve<IEnumerable<IHudUIListener>>())
            {
                try
                {
                    listener.Hide(comp?.Player);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error hiding HUD: {listener.GetType()}.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving HUD listeners.");
        }
        finally
        {
            if (comp != null)
            {
                TrySetVanillaHud(comp.Player, false);
            }
            else foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                TrySetVanillaHud(player, false);
            }
        }
    }

    private void ShowHud(HudPlayerComponent? comp)
    {
        try
        {
            ILifetimeScope scope = _module.ScopedProvider;

            foreach (IHudUIListener listener in scope.Resolve<IEnumerable<IHudUIListener>>())
            {
                if (comp is { HandleCount: > 0 })
                    continue;

                try
                {
                    listener.Restore(comp?.Player);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error showing HUD: {listener.GetType()}.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving HUD listeners.");
        }
        finally
        {
            if (comp != null)
            {
                TrySetVanillaHud(comp.Player, true);
            }
            else foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                if (player.Component<HudPlayerComponent>() is { HandleCount: > 0 })
                    continue;

                TrySetVanillaHud(player, true);
            }
        }
    }

    private void TrySetVanillaHud(WarfarePlayer player, bool isVisible)
    {
        try
        {
            if (isVisible)
                player.UnturnedPlayer.enablePluginWidgetFlag(EPluginWidgetFlags.Default);
            else
                player.UnturnedPlayer.disablePluginWidgetFlag(EPluginWidgetFlags.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error {(isVisible ? "showing" : "hiding")} PlayerLife HUD for player: {player}.");
        }
    }

    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        if (e.Player.Component<HudPlayerComponent>().HandleCount > 0)
            ++_isHiddenForAnyVersion;
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    [PlayerComponent]
    private sealed class HudPlayerComponent : IPlayerComponent
    {
        // note: may look unused but used through OnPluginVotingUpdated event
        public bool IsPluginVoting;

        /// <summary>
        /// Incremented when the HUD needs to be hidden for this player, then decremented when that request gets disposed.
        /// </summary>
        public int HandleCount;

        /// <inheritdoc />
        public required WarfarePlayer Player { get; init; }

        /// <inheritdoc />
        public void Init(IServiceProvider serviceProvider, bool isOnJoin)
        {
            if (!isOnJoin)
                return;

            IsPluginVoting = false;
        }
    }

    private class HudHandle : IDisposable
    {
        private HudManager? _manager;
        private readonly HudPlayerComponent? _playerComponent;

        public HudHandle(HudManager manager, WarfarePlayer? player)
        {
            _manager = manager;
            if (player != null)
                _playerComponent = player.Component<HudPlayerComponent>();

            _manager.IncrementHandleCount(_playerComponent);
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _manager, null)?.DecrementHandleCount(_playerComponent);
        }
    }
}
