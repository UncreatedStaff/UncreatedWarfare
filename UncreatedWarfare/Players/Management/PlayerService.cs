﻿using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Players.Management;

/// <summary>
/// Handles keeping track of online <see cref="WarfarePlayer"/>'s.
/// </summary>
/// <remarks>Inject <see cref="IPlayerService"/>.</remarks>
public class PlayerService : IPlayerService
{
    /// <summary>
    /// All types will be added to a player on join and removed on leave.
    /// They dont necessarily have to be <see cref="MonoBehaviour"/>'s but must implement <see cref="IPlayerComponent"/>.
    /// </summary>
    /// <remarks>
    /// Components can implement <see cref="IDisposable"/> if desired to run code before the player leaves.
    /// <see cref="MonoBehaviour"/>'s will be automatically destroyed.
    /// 
    /// Components can receive events, but any <see cref="IPlayerEvent"/> args will only be received if they're about the player that owns the component.
    /// </remarks>
    public static readonly Type[] PlayerComponents =
    [
        typeof(AudioRecordPlayerComponent),
        typeof(PlayerEventDispatcher),
        typeof(ItemTrackingPlayerComponent),
        typeof(KitPlayerComponent),
        typeof(ToastManager),
        typeof(ZoneVisualizerComponent)
    ];

    // keep up with a separate array that's replaced every time so the value can be used in multi-threaded operations
    private WarfarePlayer[] _threadsafeList;
    private readonly TrackingList<WarfarePlayer> _onlinePlayers;
    private readonly PlayerDictionary<WarfarePlayer> _onlinePlayersDictionary;

    // makes sure only one player is ever joining at once.
    internal readonly SemaphoreSlim PlayerJoinLock = new SemaphoreSlim(1, 1);

    /// <inheritdoc />
    public ReadOnlyTrackingList<WarfarePlayer> OnlinePlayers
    {
        get
        {
            GameThread.AssertCurrent();
            return _readOnlyOnlinePlayers;
        }
    }

    private readonly ILoggerFactory _loggerFactory; 
    private readonly IServiceProvider _serviceProvider;
    private readonly ReadOnlyTrackingList<WarfarePlayer> _readOnlyOnlinePlayers;

    public PlayerService(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _onlinePlayers = new TrackingList<WarfarePlayer>();
        _threadsafeList = Array.Empty<WarfarePlayer>();
        _onlinePlayersDictionary = new PlayerDictionary<WarfarePlayer>(Provider.maxPlayers);
        _readOnlyOnlinePlayers = _onlinePlayers.AsReadOnly();
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IReadOnlyList<WarfarePlayer> GetThreadsafePlayerList()
    {
        if (GameThread.IsCurrent)
        {
            return OnlinePlayers;
        }

        return _threadsafeList;
    }

    // todo add variation of PendingAsyncData
    internal WarfarePlayer CreateWarfarePlayer(Player player)
    {
        lock (_onlinePlayersDictionary)
        {
            if (_onlinePlayersDictionary.ContainsPlayer(player))
            {
                throw new ArgumentException("This player has already been added to PlayerService.", nameof(player));
            }

            ILogger logger = _loggerFactory.CreateLogger(
                player.channel.owner.playerID.steamID.m_SteamID.ToString("D17", CultureInfo.InvariantCulture)
            );

            IPlayerComponent[] components = AddComponents(player);

            WarfarePlayer joined = new WarfarePlayer(player, logger, components, _serviceProvider);
            _onlinePlayers.Add(joined);
            _onlinePlayersDictionary.Add(joined, joined);

            WarfarePlayer[] newList = new WarfarePlayer[_threadsafeList.Length + 1];
            Array.Copy(_threadsafeList, 0, newList, 0, _threadsafeList.Length);
            newList[^1] = joined;
            _threadsafeList = newList;
            
            for (int i = 0; i < components.Length; ++i)
            {
                IPlayerComponent component = components[i];

                component.Player = joined;
                component.Init(_serviceProvider);
            }

            return joined;
        }
    }

    internal WarfarePlayer OnPlayerLeft(WarfarePlayer player)
    {
        lock (_onlinePlayersDictionary)
        {
            RemoveComponents(player);

            _onlinePlayers.Remove(player);
            _onlinePlayersDictionary.Remove(player.Steam64);

            for (int i = 0; i < _threadsafeList.Length; ++i)
            {
                WarfarePlayer pl = _threadsafeList[i];
                if (!ReferenceEquals(pl, player))
                    continue;
                
                WarfarePlayer[] newList = new WarfarePlayer[_threadsafeList.Length - 1];
                if (i != 0)
                    Array.Copy(_threadsafeList, 0, newList, 0, i);
                if (i != _threadsafeList.Length - 1)
                    Array.Copy(_threadsafeList, i + 1, newList, i, _threadsafeList.Length - i - 1);
                
                _threadsafeList = newList;
                break;
            }

            player.ApplyOfflineState();
            return player;
        }
    }

    private IPlayerComponent[] AddComponents(Player player)
    {
        IPlayerComponent[] components = new IPlayerComponent[PlayerComponents.Length];
        for (int i = 0; i < PlayerComponents.Length; i++)
        {
            Type type = PlayerComponents[i];
            if (!typeof(IPlayerComponent).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Type {Accessor.ExceptionFormatter.Format(type)} does not " +
                                                    $"implement {Accessor.ExceptionFormatter.Format<IPlayerComponent>()}.");
            }

            IPlayerComponent component;
            if (type.IsSubclassOf(typeof(Component)))
            {
                component = (IPlayerComponent)player.gameObject.AddComponent(type);
            }
            else
            {
                component = (IPlayerComponent)ActivatorUtilities.CreateInstance(_serviceProvider, type, player,
                    player.channel.owner, player.channel.owner.playerID);
            }

            components[i] = component;
        }

        return components;
    }

    private static void RemoveComponents(WarfarePlayer player)
    {
        foreach (IPlayerComponent component in player.Components)
        {
            if (component is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (component is Component unityComponent && unityComponent != null)
            {
                Object.Destroy(unityComponent);
            }
        }
    }

    /// <inheritdoc />
    public WarfarePlayer GetOnlinePlayer(ulong steam64)
    {
        GameThread.AssertCurrent();

        // ReSharper disable once InconsistentlySynchronizedField
        if (!_onlinePlayersDictionary.TryGetValue(Unsafe.As<ulong, CSteamID>(ref steam64), out WarfarePlayer? player))
            throw new PlayerOfflineException(steam64);

        return player;
    }

    /// <inheritdoc />
    public WarfarePlayer? GetOnlinePlayerOrNull(ulong steam64)
    {
        GameThread.AssertCurrent();

        // ReSharper disable once InconsistentlySynchronizedField
        _onlinePlayersDictionary.TryGetValue(Unsafe.As<ulong, CSteamID>(ref steam64), out WarfarePlayer? player);
        return player;
    }

    /// <inheritdoc />
    public WarfarePlayer GetOnlinePlayerThreadSafe(ulong steam64)
    {
        lock (_onlinePlayersDictionary)
        {
            if (!_onlinePlayersDictionary.TryGetValue(Unsafe.As<ulong, CSteamID>(ref steam64), out WarfarePlayer? player))
                throw new PlayerOfflineException(steam64);

            return player;
        }
    }

    /// <inheritdoc />
    public WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(ulong steam64)
    {
        lock (_onlinePlayersDictionary)
        {
            _onlinePlayersDictionary.TryGetValue(Unsafe.As<ulong, CSteamID>(ref steam64), out WarfarePlayer? player);
            return player;
        }
    }
}

/// <summary>
/// Thrown when a player can't be found in <see cref="IPlayerService"/>.
/// </summary>
public class PlayerOfflineException : Exception
{
    public PlayerOfflineException(ulong steam64) : base(
        $"Could not get WarfarePlayer '{steam64.ToString("D17", CultureInfo.InvariantCulture)}' " +
        $"because they were not found in the list of online players.")
    {

    }
}

public class PlayerComponentNotFoundException : Exception
{
    public PlayerComponentNotFoundException(Type type, WarfarePlayer player)
        : base($"The component {Accessor.Formatter.Format(type)} could not be found on player {player.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture)}.")
    { }
}