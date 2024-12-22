#if DEBUG
//#define ICONS_DEBUG_LOGGING
#endif
using SDG.Framework.Utilities;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction.Icons;

public class WorldIconManager : ILayoutHostedService, IEventListener<PlayerLeft>, IEventListener<PlayerTeamChanged>
{
    public const float DefaultTickSpeed = 1f;
    private const float TimeTolerance = 0.05f;

    private readonly Dictionary<Guid, List<WorldIconInfo>> _iconsByGuid = new Dictionary<Guid, List<WorldIconInfo>>();
    private readonly List<WorldIconInfo> _allIcons = new List<WorldIconInfo>(256);
    private static readonly HashSet<ITransportConnection> WorkingConnectionHashSet = new HashSet<ITransportConnection>(64);
    private static readonly List<WorldIconInfo> IconWorkingSet = new List<WorldIconInfo>(4);

    private float _lastRealtime;
    private float _lastTickTime;
    private float _lowestTickSpeed;
    private float _greatestTickSpeed;

    private readonly IPlayerService _playerService;
    private readonly ILogger<WorldIconManager> _logger;

    private static readonly ClientStaticMethod<Guid>? SendEffectClearByGuid =
        ReflectionUtility.FindRpc<EffectManager, ClientStaticMethod<Guid>>("SendEffectClearByGuid");

    public WorldIconManager(IPlayerService playerService, ILogger<WorldIconManager> logger)
    {
        _playerService = playerService;
        _logger = logger;

        _lowestTickSpeed = DefaultTickSpeed;
        _greatestTickSpeed = DefaultTickSpeed;
    }

    [Conditional("ICONS_DEBUG_LOGGING"), StringFormatMethod(nameof(msg))]
    private void Log(string msg, params object?[]? args)
    {
        _logger.LogDebug(msg, args);
    }

    private void OnUpdate()
    {
        if (Provider.clients.Count == 0)
            return;

        float rt = Time.realtimeSinceStartup;
        float tickTime = rt - _lastTickTime;
        if ((rt - _lastRealtime) < _lowestTickSpeed && tickTime % _lowestTickSpeed >= (_lastRealtime - _lastTickTime) % _lowestTickSpeed)
        {
            _lastRealtime = rt;
            return;
        }

        List<Guid> toUpdate = ListPool<Guid>.claim();

        Log("{0} | Ticking... ({1}) [{2}, {3}]", rt.ToString("000.000", CultureInfo.InvariantCulture), tickTime, _lowestTickSpeed, _greatestTickSpeed);
        foreach (KeyValuePair<Guid, List<WorldIconInfo>> listPair in _iconsByGuid)
        {
            bool any = false;
            foreach (WorldIconInfo icon in listPair.Value)
            {
                Log("{0} | Checking icon... ({1}): {2}", rt.ToString("000.000", CultureInfo.InvariantCulture), rt - icon.LastSpawnRealtime, icon);
                if (rt - icon.LastSpawnRealtime + TimeTolerance < icon.TickSpeed)
                    continue;

                any = true;
                break;
            }

            if (!any)
                continue;

            toUpdate.Add(listPair.Key);
        }

        foreach (Guid guid in toUpdate)
        {
            UpdateIcon(guid);
        }

        ListPool<Guid>.release(toUpdate);

        _lastRealtime = rt;
        if (tickTime > _greatestTickSpeed)
        {
            _lastTickTime = rt;
        }
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        UpdateAllIcons();
        TimeUtility.updated += OnUpdate;
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Updates all icons instantly.
    /// </summary>
    public void UpdateAllIcons()
    {
        float rt = Time.realtimeSinceStartup;
        _lastTickTime = rt;

        foreach (KeyValuePair<Guid, List<WorldIconInfo>> iconSets in _iconsByGuid.ToList())
        {
            if (iconSets.Value.Count > 0)
            {
                UpdateIcon(iconSets.Value[0].Effect);
            }
        }
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        TimeUtility.updated -= OnUpdate;
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Removes all icons from the world.
    /// </summary>
    public void RemoveAllIcons()
    {
        foreach (KeyValuePair<Guid, List<WorldIconInfo>> iconSets in _iconsByGuid)
        {
            ClearEffectsByGuid(iconSets.Key, iconSets.Value);
            iconSets.Value.Clear();
        }

        _iconsByGuid.Clear();
        _allIcons.Clear();

        _lowestTickSpeed = DefaultTickSpeed;
        _greatestTickSpeed = DefaultTickSpeed;
        Log("{0} | Removed all icons", Time.realtimeSinceStartup.ToString("000.000", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Removes a single icon.
    /// </summary>
    #nullable disable
    public void RemoveIcon(WorldIconInfo icon)
    #nullable restore
    {
        if (RemoveIconIntl(icon))
        {
            RecheckTickSpeed();
        }
    }
    
    /// <returns>If tick speed needs to be rechecked.</returns>
    private bool RemoveIconIntl(WorldIconInfo icon)
    {
        if (icon == null || !_allIcons.Remove(icon))
            return false;

        Guid guid = icon.Effect.Guid;
        if (_iconsByGuid.TryGetValue(guid, out List<WorldIconInfo> list))
        {
            list.Remove(icon);
            if (list.Count == 0)
                _iconsByGuid.Remove(guid);
        }

        float rt = Time.realtimeSinceStartup;
        Log("{0} | Removed icon {1}", rt.ToString("000.000", CultureInfo.InvariantCulture), icon);
        bool needsRecheck = _greatestTickSpeed <= icon.TickSpeed || icon.TickSpeed >= _lowestTickSpeed;
        if (!icon.NeedsToBeCleared(rt))
        {
            icon.Dispose();
            return needsRecheck;
        }

        ITransportConnection? singlePlayer = null;
        PooledTransportConnectionList? pooledList = null;
        icon.UpdateRelevantPlayers(_playerService, ref pooledList, ref singlePlayer, WorkingConnectionHashSet);
        WorkingConnectionHashSet.Clear();

        if (pooledList != null)
            ClearEffectsByGuidMultiplePlayers(guid, pooledList);
        else if (singlePlayer != null)
            EffectManager.ClearEffectByGuid(guid, singlePlayer);

        icon.Dispose();
        return needsRecheck;
    }

    /// <summary>
    /// Respawns all icons of a given type.
    /// </summary>
    public void UpdateIcon(IAssetLink<EffectAsset> asset)
    {
        if (asset.TryGetGuid(out Guid guid))
            UpdateIcon(guid);
    }

    /// <summary>
    /// Respawns all icons of a given type.
    /// </summary>
    public void UpdateIcon(Guid guid)
    {
        GameThread.AssertCurrent();

        if (!_iconsByGuid.TryGetValue(guid, out List<WorldIconInfo> list))
            return;

        ClearEffectsByGuid(guid, list);

        float rt = Time.realtimeSinceStartup;
        Log("{0} | Updating {1}", rt.ToString("000.000", CultureInfo.InvariantCulture), list[0].Effect);

        foreach (WorldIconInfo info in list)
        {
            if (IsInactive(info, rt))
            {
                Log("        | Removing inactive {0}", info);
                IconWorkingSet.Add(info);
            }
            else
            {
                Log("        | Updating {0}", info);
                info.SpawnEffect(_playerService, rt, rt - info.LastPositionUpdateRealtime + TimeTolerance > info.TickSpeed);
            }
        }

        bool recheck = false;
        foreach (WorldIconInfo info in IconWorkingSet)
            recheck |= RemoveIconIntl(info);

        IconWorkingSet.Clear();
        if (recheck)
        {
            RecheckTickSpeed();
        }
    }

    private bool IsInactive(WorldIconInfo info, float rt)
    {
        if (!info.Alive)
        {
            Log("        | Inactive - not alive: {0}", info);
            return true;
        }

        if (info.TransformableObject is { Alive: false })
        {
            Log("        | Inactive - transformable not alive: {0}", info);
            return true;
        }

        if (info.UnityObject is not null && info.UnityObject == null)
        {
            Log("        | Inactive - unity object not alive: {0}", info);
            return true;
        }

        if (info.FirstSpawnRealtime > 0 && info.FirstSpawnRealtime + info.LifetimeSeconds < rt)
        {
            Log("        | Inactive - lifetime expired: {0} ({1} + {2})", info, info.FirstSpawnRealtime, info.LifetimeSeconds);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a new icon of a given type.
    /// </summary>
    public void CreateIcon(WorldIconInfo icon)
    {
        GameThread.AssertCurrent();

        if (!icon.Effect.TryGetAsset(out EffectAsset? asset))
        {
            _logger.LogError("Unknown asset for icon {0}.", icon);
            return;
        }

        float rt = Time.realtimeSinceStartup;
        icon.Alive = true;
        if (IsInactive(icon, rt))
        {
            icon.Alive = false;
            Log("{0} | Creating but not adding inactive {1}", rt.ToString("000.000", CultureInfo.InvariantCulture), icon);
            return;
        }

        if (!_iconsByGuid.TryGetValue(asset.GUID, out List<WorldIconInfo> list))
        {
            list = new List<WorldIconInfo>(8) { icon };
            _iconsByGuid.Add(asset.GUID, list);
        }
        else
        {
            list.Add(icon);
        }

        _allIcons.Add(icon);

        Log("{0} | Creating {1}", rt.ToString("000.000", CultureInfo.InvariantCulture), icon);
        icon.SpawnEffect(_playerService, rt, true);

        if (_allIcons.Count == 1)
        {
            _lowestTickSpeed = _greatestTickSpeed = icon.TickSpeed;
        }
        else
        {
            if (icon.TickSpeed < _lowestTickSpeed)
            {
                _lowestTickSpeed = icon.TickSpeed;
            }

            if (icon.TickSpeed > _greatestTickSpeed)
            {
                _greatestTickSpeed = icon.TickSpeed;
            }
        }
    }

    private void ClearEffectsByGuid(Guid guid, List<WorldIconInfo> list)
    {
        float rt = Time.realtimeSinceStartup;
        bool anyNeedToBeCleared = false;
        ITransportConnection? singlePlayer = null;
        PooledTransportConnectionList? pooledList = null;
        foreach (WorldIconInfo info in list)
        {
            info.UpdateRelevantPlayers(_playerService, ref pooledList, ref singlePlayer, WorkingConnectionHashSet);
            if (!anyNeedToBeCleared && info.NeedsToBeCleared(rt))
                anyNeedToBeCleared = true;
        }

        WorkingConnectionHashSet.Clear();

        if (!anyNeedToBeCleared)
            return;

        if (pooledList != null)
        {
            Log("{0} | Cleared {1} for {2} players.", rt.ToString("000.000", CultureInfo.InvariantCulture), list[0].Effect, pooledList.Count);
            ClearEffectsByGuidMultiplePlayers(guid, pooledList);
        }
        else if (singlePlayer != null)
        {
            Log("{0} | Cleared {1} for {2}.", rt.ToString("000.000", CultureInfo.InvariantCulture), list[0].Effect, singlePlayer.TryGetSteamId(out ulong cs) ? new CSteamID(cs) : singlePlayer);
            EffectManager.ClearEffectByGuid(guid, singlePlayer);
        }
        foreach (WorldIconInfo info in list)
        {
            info.OnCleared();
        }
    }

    private void RecheckTickSpeed()
    {
        float min = float.NaN, max = float.NaN;
        foreach (WorldIconInfo info in _allIcons)
        {
            if (float.IsNaN(min) || info.TickSpeed < min)
                min = info.TickSpeed;

            if (float.IsNaN(max) || info.TickSpeed > max)
                max = info.TickSpeed;
        }

        _lowestTickSpeed = float.IsNaN(min) ? DefaultTickSpeed : min;
        _greatestTickSpeed = float.IsNaN(max) ? DefaultTickSpeed : max;
    }

    private static void ClearEffectsByGuidMultiplePlayers(Guid guid, PooledTransportConnectionList list)
    {
        if (SendEffectClearByGuid == null)
        {
            foreach (ITransportConnection c in list)
            {
                EffectManager.ClearEffectByGuid(guid, c);
            }
        }
        else
        {
            SendEffectClearByGuid.Invoke(ENetReliability.Unreliable, list, guid);
        }
    }

    private void UpdateForPlayer(WarfarePlayer player)
    {
        if (!player.IsOnline)
            return;

        float rt = Time.realtimeSinceStartup;
        foreach (List<WorldIconInfo> iconSets in _iconsByGuid.Values)
        {
            bool hasCleared = false;
            foreach (WorldIconInfo icon in iconSets)
            {
                if (!icon.ShouldPlayerSeeIcon(player))
                    continue;

                if (!hasCleared)
                {
                    Log("{0} | Cleared {1} for {2} when updating for player.", rt.ToString("000.000", CultureInfo.InvariantCulture), icon.Effect, player);
                    EffectManager.ClearEffectByGuid(icon.Effect.Guid, player.Connection);
                    hasCleared = true;
                }

                icon.OnCleared();
                if (IsInactive(icon, rt))
                {
                    Log("{0} | Removing inactive when updating for player {1} {2}", rt.ToString("000.000", CultureInfo.InvariantCulture), player, icon);
                    IconWorkingSet.Add(icon);
                }
                else
                {
                    Log("{0} | Respawning for player {1} {2}", rt.ToString("000.000", CultureInfo.InvariantCulture), player, icon);
                    icon.SpawnEffect(_playerService, rt, false, player);
                }
            }
        }

        if (IconWorkingSet.Count <= 0)
            return;

        bool recheck = false;
        foreach (WorldIconInfo info in IconWorkingSet)
            recheck |= RemoveIconIntl(info);

        IconWorkingSet.Clear();
        if (recheck)
        {
            RecheckTickSpeed();
        }
    }

    private void RemovePlayerSpecificIcons(WarfarePlayer player, Team? team)
    {
        bool recheck = false;

        List<Guid>? toRemove = null;
        List<Guid>? toUpdate = null;

        float rt = Time.realtimeSinceStartup;
        foreach (List<WorldIconInfo> list in _iconsByGuid.Values)
        {
            bool anyFound = false;
            Guid guid = Guid.Empty;
            for (int i = list.Count - 1; i >= 0; --i)
            {
                WorldIconInfo icon = list[i];
                if (!player.Equals(icon.TargetPlayer) && (team is null || icon.TargetTeam == team))
                    continue;

                Log("{0} | Removing player specific for {1} {2} - {3}", rt.ToString("000.000", CultureInfo.InvariantCulture), player, team, icon);
                list.RemoveAt(i);
                if (list.Count == 0)
                {
                    toRemove ??= ListPool<Guid>.claim();
                    toRemove.Add(icon.Effect.Guid);
                    list.Clear();
                }

                _allIcons.Remove(icon);
                recheck |= _greatestTickSpeed <= icon.TickSpeed && icon.TickSpeed > _lowestTickSpeed;
                anyFound = true;
                guid = icon.Effect.Guid;
            }

            if (anyFound)
            {
                toUpdate ??= ListPool<Guid>.claim();
                toUpdate.Add(guid);
            }
        }

        if (toRemove != null)
        {
            foreach (Guid guid in toRemove)
                _iconsByGuid.Remove(guid);
            ListPool<Guid>.release(toRemove);
        }

        if (toUpdate != null)
        {
            foreach (Guid guid in toUpdate)
                UpdateIcon(guid);
            ListPool<Guid>.release(toUpdate);
        }

        if (recheck)
        {
            RecheckTickSpeed();
        }
    }

    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        RemovePlayerSpecificIcons(e.Player, null);
    }

    void IEventListener<PlayerTeamChanged>.HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        RemovePlayerSpecificIcons(e.Player, e.OldTeam);
        UpdateForPlayer(e.Player);
    }
}