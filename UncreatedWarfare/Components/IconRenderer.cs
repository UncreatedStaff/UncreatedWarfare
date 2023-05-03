﻿using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using UnityEngine;

namespace Uncreated.Warfare.Components;

public static class IconManager
{
    private static readonly List<IconRenderer> Icons = new List<IconRenderer>();
    static IconManager()
    {
        EventDispatcher.GroupChanged += OnGroupChanged;
    }
    public static void OnGamemodeReloaded()
    {
        for (int i = Icons.Count - 1; i >= 0; i--)
        {
            IconRenderer iconRenderer = Icons[i];
            DeleteIcon(iconRenderer);
        }

        OnLevelLoaded();
    }
    public static void OnLevelLoaded()
    {
        foreach (BarricadeDrop barricade in UCBarricadeManager.NonPlantedBarricades)
            OnBarricadePlaced(barricade);
    }
    public static void OnBarricadePlaced(BarricadeDrop drop, bool isFOBRadio = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (drop.model == null || drop.model.TryGetComponent(out IconRenderer _))
            return;
        
        // ammo bag
        if (Gamemode.Config.BarricadeAmmoBag.MatchGuid(drop.asset.GUID) && Gamemode.Config.EffectMarkerAmmo.ValidReference(out Guid guid))
            AttachIcon(guid, drop.model, drop.GetServersideData().group.GetTeam(), 1f);
    }
    private static void OnGroupChanged(GroupChanged e)
    {
        DrawNewMarkers(e.Player, true);
    }
    public static void DrawNewMarkers(UCPlayer player, bool clearOld)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<Guid> seenTypes = clearOld ? new List<Guid>(Icons.Count) : null!;

        ulong team = player.GetTeam();
        foreach (IconRenderer icon in Icons)
        {
            if (clearOld)
            {
                if (!seenTypes.Contains(icon.EffectGUID))
                {
                    seenTypes.Add(icon.EffectGUID);
                    EffectManager.ClearEffectByGuid(icon.EffectGUID, player.Connection);
                }
            }

            if (icon.Team == 0 || (icon.Team != 0 && icon.Team == team))
            {
                icon.SpawnNewIcon(player.Connection);
            }
        }
    }
    public static void AttachIcon(Guid effectGUID, Transform transform, ulong team = 0, float yOffset = 0)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        IconRenderer icon = transform.gameObject.AddComponent<IconRenderer>();
        icon.Initialize(effectGUID, new Vector3(transform.position.x, transform.position.y + yOffset, transform.position.z), team);
        
        icon.SpawnNewIcon(Data.GetPooledTransportConnectionList((icon.Team == 0
                ? PlayerManager.OnlinePlayers
                : PlayerManager.OnlinePlayers.Where(x => x.GetTeam() == icon.Team))
            .Select(x => x.Connection)));

        Icons.Add(icon);
    }
    public static void DeleteIcon(IconRenderer icon, bool destroy = true)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        EffectManager.ClearEffectByGuid_AllPlayers(icon.EffectGUID);
        Icons.Remove(icon);
        if (destroy)
            icon.Destroy();

        SpawnNewIconsOfType(icon.EffectGUID);
    }
    private static void SpawnNewIconsOfType(Guid effectGUID)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (IconRenderer icon in Icons)
        {
            if (icon.EffectGUID == effectGUID)
            {
                icon.SpawnNewIcon(Data.GetPooledTransportConnectionList((icon.Team == 0
                        ? PlayerManager.OnlinePlayers
                        : PlayerManager.OnlinePlayers.Where(x => x.GetTeam() == icon.Team))
                    .Select(x => x.Connection)));
            }
        }
    }
}


public class IconRenderer : MonoBehaviour, IManualOnDestroy
{
    public Guid EffectGUID { get; private set; }
    public EffectAsset Effect { get; private set; }
    public ulong Team { get; private set; }
    public Vector3 Point { get; private set; }
    public void Initialize(Guid effectGUID, Vector3 point, ulong team = 0)
    {
        Point = point;

        EffectGUID = effectGUID;

        Team = team;

        if (Assets.find(EffectGUID) is EffectAsset effect)
        {
            Effect = effect;
        }
        else
            L.LogWarning("IconSpawner could not start: Effect asset not found: " + effectGUID);
    }

    [UsedImplicitly]
    void OnDestroy()
    {
        IconManager.DeleteIcon(this, false);
    }
    public void Destroy()
    {
        Destroy(this);
    }
    public void SpawnNewIcon(ITransportConnection player)
    {
        if (Effect == null)
            return;
        F.TriggerEffectReliable(Effect, player, Point);
    }
    public void SpawnNewIcon(PooledTransportConnectionList players)
    {
        if (Effect == null)
            return;
        F.TriggerEffectReliable(Effect, players, Point);
    }

    public void ManualOnDestroy()
    {
        Destroy();
    }
}
