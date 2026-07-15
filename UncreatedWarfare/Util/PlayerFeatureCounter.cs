using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Tracks features that can be applied to either individual players or all players.
/// This is useful for effects that can be applied from multiple places, such as the modal widget (enabling the mouse).
/// </summary>
/// <typeparam name="TPlayerComponent">Player component that tracks individual handle counts.</typeparam>
public sealed class PlayerFeatureCounter<TPlayerComponent>
    where TPlayerComponent : class, IPlayerComponent
{
    private readonly IPlayerService _playerService;
    private readonly Func<TPlayerComponent, int, int> _addToHandle;
    private readonly Action<TPlayerComponent?> _addFeature;
    private readonly Action<TPlayerComponent?> _removeFeature;

    private int _globalHandleCount;
    private bool _anyHasFeature;
    private int _anyHasFeatureVersion;
    private int _cachedAnyHasFeatureVersion;

    /// <summary>
    /// Whether or not this feature is applied globally (to all players).
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool AppliedGlobally
    {
        get
        {
            GameThread.AssertCurrent();
            return _globalHandleCount > 0;
        }
    }

    /// <summary>
    /// Whether or not this feature is applied to at least one player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool AppliedToAnyPlayers
    {
        get
        {
            GameThread.AssertCurrent();
            if (_globalHandleCount > 0)
                return true;

            if (_anyHasFeatureVersion != _cachedAnyHasFeatureVersion)
            {
                RecalcHasFeature();
            }

            return _anyHasFeature;
        }
    }

    /// <summary>
    /// Create a new <see cref="PlayerFeatureCounter{TPlayerComponent}"/>.
    /// </summary>
    /// <param name="playerService">The player service.</param>
    /// <param name="addToHandle">Function that adds a value to the current handle count. This is also passed a value of <c>0</c> to get the value of the handle.</param>
    /// <param name="addFeature">Invoked when the feature should be turned on for a player or all players. Not invoked on join.</param>
    /// <param name="removeFeature">Invoked when the feature should be turned off for a player or all players. not invoked on leave.</param>
    public PlayerFeatureCounter(
        IPlayerService playerService,
        Func<TPlayerComponent, int, int> addToHandle,
        Action<TPlayerComponent?> addFeature,
        Action<TPlayerComponent?> removeFeature)
    {
        _playerService = playerService;
        _addToHandle = addToHandle;
        _addFeature = addFeature;
        _removeFeature = removeFeature;
    }

    /// <summary>
    /// Determines whether or not a player has a feature.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool HasFeature(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        TPlayerComponent? c = player.ComponentOrNull<TPlayerComponent>();
        return c != null && HasFeature(c);
    }

    /// <inheritdoc cref="HasFeature(WarfarePlayer)"/>
    public bool HasFeature(TPlayerComponent player)
    {
        return _globalHandleCount > 0 || _addToHandle(player, 0) > 0;
    }

    /// <summary>
    /// Increment the value for a player.
    /// </summary>
    /// <param name="player">The player component to increment it for, or <see langword="null"/> to apply to all players.</param>
    public void Increment(TPlayerComponent? player)
    {
        IncrementIntl(player);
    }

    /// <summary>
    /// Increment the value for all players.
    /// </summary>
    public void Increment()
    {
        IncrementIntl(null);
    }

    private void IncrementIntl(TPlayerComponent? player)
    {
        if (!GameThread.IsCurrent)
        {
            UniTask.Create((@this: this, player), static async args =>
            {
                await UniTask.SwitchToMainThread();
                args.@this.IncrementIntl(args.player);
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
            int newHandleCount = _addToHandle(player, 1);
            
            _anyHasFeature = true;
            _cachedAnyHasFeatureVersion = _anyHasFeatureVersion;

            if (newHandleCount != 1 || _globalHandleCount > 0 || !player.Player.IsOnline)
                return;
        }

        _addFeature(player);
    }

    /// <summary>
    /// Decrement the value for a player.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    /// <param name="player">The player component to decrement it for, or <see langword="null"/> to apply to all players.</param>
    public void Decrement(TPlayerComponent? player)
    {
        DecrementIntl(player);
    }

    /// <summary>
    /// Decrement the value for all players.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public void Decrement()
    {
        DecrementIntl(null);
    }

    private void DecrementIntl(TPlayerComponent? player)
    {
        if (!GameThread.IsCurrent)
        {
            UniTask.Create((@this: this, player), static async args =>
            {
                await UniTask.SwitchToMainThread();
                args.@this.DecrementIntl(args.player);
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
            int newHandleCount = _addToHandle(player, -1);
            if (newHandleCount > 0 || _globalHandleCount > 0 || !player.Player.IsOnline)
                return;

            ++_anyHasFeatureVersion;
        }

        _removeFeature(player);
    }

    /// <summary>
    /// Invoke this function when <paramref name="player"/> leaves.
    /// </summary>
    public void NotifyPlayerLeft(WarfarePlayer player)
    {
        if (_addToHandle(player.Component<TPlayerComponent>(), 0) > 0)
        {
            ++_anyHasFeatureVersion;
        }
    }

    private void RecalcHasFeature()
    {
        bool isHidden = false;
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            TPlayerComponent component = player.Component<TPlayerComponent>();
            int handleCount = _addToHandle(component, 0);
            if (handleCount <= 0)
                continue;

            isHidden = true;
            break;
        }

        _anyHasFeature = isHidden;
        _cachedAnyHasFeatureVersion = _anyHasFeatureVersion;
    }
}