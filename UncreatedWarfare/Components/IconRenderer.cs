using JetBrains.Annotations;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using SDG.Framework.Utilities;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using UnityEngine;

namespace Uncreated.Warfare.Components;

public static class IconManager
{
    private const float FullTickLoopTime = 0.25f;
    private static readonly List<IconRenderer> Icons = new List<IconRenderer>();
    private static int _tickIndex;
    private static float _tickIndexProgress;
    static IconManager()
    {
        EventDispatcher.GroupChanged += OnGroupChanged;
        EventDispatcher.PlayerLeft += OnPlayerLeft;
        TimeUtility.updated += OnUpdate;
    }
    private static void OnPlayerLeft(PlayerEvent e)
    {
        for (int i = Icons.Count - 1; i >= 0; i--)
        {
            IconRenderer iconRenderer = Icons[i];
            if (iconRenderer.Player != e.Steam64)
                continue;

            Icons.RemoveAt(i);
            iconRenderer.Destroy();
        }
    }

    public static void OnGamemodeReloaded(bool onLoad)
    {
        if (!onLoad)
            return;

        DeleteAllIcons();
        CheckExistingBuildables();
    }
    public static void DeleteAllIcons()
    {
        for (int i = Icons.Count - 1; i >= 0; i--)
        {
            IconRenderer iconRenderer = Icons[i];
            DeleteIcon(iconRenderer);
        }
    }
    public static void CheckExistingBuildables()
    {
        foreach (BarricadeDrop barricade in UCBarricadeManager.NonPlantedBarricades)
        {
            ulong team = barricade.GetServersideData().group.GetTeam();
            if (team is not 1ul and not 2ul)
                continue;

            CheckBuildable(barricade.asset, barricade.model, team);
        }
        foreach (StructureDrop structure in UCBarricadeManager.AllStructures)
        {
            ulong team = structure.GetServersideData().group.GetTeam();
            if (team is not 1ul and not 2ul)
                continue;

            CheckBuildable(structure.asset, structure.model, team);
        }

        void CheckBuildable(Asset asset, Transform transform, ulong team)
        {
            BuildableData? buildableData = FOBManager.FindBuildable(asset);
            if (buildableData == null || !buildableData.FullBuildable.MatchGuid(asset.GUID))
                return;
            L.LogDebug($"[ICONS] [{asset.FriendlyName}] Found existing buildable, try-applying marker type: {buildableData.Type}.");
            switch (buildableData.Type)
            {
                case BuildableType.AmmoCrate:
                    if (Gamemode.Config.EffectMarkerAmmo.ValidReference(out Guid guid))
                        AttachIcon(guid, transform, team, 1.75f);
                    break;
                case BuildableType.RepairStation:
                    if (Gamemode.Config.EffectMarkerRepair.ValidReference(out guid))
                        AttachIcon(guid, transform, team, 4.5f);
                    break;
            }
        }
    }
    private static void OnGroupChanged(GroupChanged e)
    {
        DrawNewMarkers(e.Player, true);
    }
    public static void DrawNewMarkers(UCPlayer player, bool clearOld)
    {
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
    public static void AttachIcon(Guid effectGUID, Transform transform, ulong team = 0, float yOffset = 0, ulong player = 0)
        => AttachIcon(effectGUID, transform, new Vector3(0f, yOffset, 0f), team, player);
    public static void AttachIcon(Guid effectGUID, Transform transform, Vector3 offset, ulong team = 0, ulong player = 0)
    {
        if (transform.gameObject.TryGetComponent(out IconRenderer icon))
            DeleteIcon(icon);
        
        icon = transform.gameObject.AddComponent<IconRenderer>();
        icon.Initialize(effectGUID, offset, team, player);

        SpawnIcons(icon);

        Icons.Add(icon);
        L.LogDebug($"[ICONS] [{icon.Effect?.name}] Icon attached.");
    }
    public static void DeleteIcon(IconRenderer icon, bool destroy = true)
    {
        if (!Icons.Contains(icon))
            return;
        EffectManager.ClearEffectByGuid_AllPlayers(icon.EffectGUID);
        Icons.Remove(icon);
        if (destroy)
            icon.Destroy();

        SpawnNewIconsOfType(icon.EffectGUID);
        L.LogDebug($"[ICONS] [{icon.Effect?.name}] Icon deleted.");
    }
    public static void DrawNewIconsOfType(Guid effectGUID)
    {
        EffectManager.ClearEffectByGuid_AllPlayers(effectGUID);
        SpawnNewIconsOfType(effectGUID);
    }
    public static void SpawnNewIconsOfType(Guid effectGUID)
    {
        foreach (IconRenderer icon in Icons)
        {
            if (icon.EffectGUID == effectGUID)
                SpawnIcons(icon);
        }
    }
    public static void SpawnIcons(IconRenderer icon)
    {
        icon.SpawnNewIcon(Data.GetPooledTransportConnectionList((icon.Player == 0 ? (icon.Team == 0
                ? PlayerManager.OnlinePlayers
                : PlayerManager.OnlinePlayers.Where(x => x.GetTeam() == icon.Team)) : (UCPlayer.FromID(icon.Player) is not { } player ? Array.Empty<UCPlayer>() : new UCPlayer[] { player }))
            .Select(x => x.Connection)));
    }
    private static void OnUpdate()
    {
        if (Icons.Count == 0) return;
        float tickAmount = Time.deltaTime * Icons.Count / FullTickLoopTime;
        _tickIndexProgress += tickAmount;
        int newTickIndex = Mathf.FloorToInt(_tickIndexProgress);
        if (newTickIndex < Icons.Count)
        {
            if (newTickIndex == _tickIndex)
                return;
            for (int i = _tickIndex; i < newTickIndex; ++i)
            {
                IconRenderer renderer = Icons[i];
                renderer.Tick();
            }
        }
        else
        {
            newTickIndex %= Icons.Count;
            for (int i = _tickIndex; i < Icons.Count; ++i)
            {
                IconRenderer renderer = Icons[i];
                renderer.Tick();
            }
            for (int i = 0; i < newTickIndex; ++i)
            {
                IconRenderer renderer = Icons[i];
                renderer.Tick();
            }
        }

        _tickIndex = newTickIndex;
        _tickIndexProgress = newTickIndex;
    }
}


public class IconRenderer : MonoBehaviour, IManualOnDestroy
{
    private float _lastBroadcast;
    private Vector3 _lastPosition;
    public Guid EffectGUID { get; private set; }
    public EffectAsset Effect { get; private set; }
    public ulong Team { get; private set; }
    public ulong Player { get; private set; }
    public Vector3 Point => _lastPosition;
    public Vector3 Offset { get; private set; }
    public float Lifetime { get; private set; }
    public bool LifetimeCheck { get; private set; }
    public void Initialize(Guid effectGUID, Vector3 offset, ulong team = 0, ulong player = 0)
    {
        EffectGUID = effectGUID;

        Offset = offset;

        Team = team;
        Player = player;

        if (Assets.find(EffectGUID) is EffectAsset effect)
        {
            Effect = effect;
            Lifetime = effect.lifetime;
            if (effect.lifetimeSpread != 0)
                L.LogWarning($"[{effect.name}] Effect " + ActionLog.AsAsset(effect) + " has a non-zero lifetime spread.", method: "ICONS");
            LifetimeCheck = Lifetime != 0;
            if (!LifetimeCheck)
                L.LogWarning($"[{effect.name}] Effect " + ActionLog.AsAsset(effect) + " has a zero lifetime.", method: "ICONS");
        }
        else
            L.LogWarning($"IconSpawner could not start: Effect asset not found: " + effectGUID.ToString("N") + ".", method: "ICONS");
    }

    [UsedImplicitly]
    void OnDestroy()
    {
        IconManager.DeleteIcon(this, false);
        L.LogDebug($"[ICONS] [{Effect?.name}] Icon destroyed: {Effect?.FriendlyName ?? EffectGUID.ToString("N")}");
    }
    public void Tick()
    {
        bool drew = false;
        if (LifetimeCheck)
        {
            float time = Time.realtimeSinceStartup;
            if (_lastBroadcast + Lifetime * 0.95f < time)
            {
                IconManager.DrawNewIconsOfType(EffectGUID);
                drew = true;
            }
        }
        if (!drew && isActiveAndEnabled)
        {
            Vector3 position = transform.position;
            if (!_lastPosition.AlmostEquals(position))
            {
                IconManager.DrawNewIconsOfType(EffectGUID);
                _lastPosition = position;
            }
        }
    }
    public void Destroy()
    {
        Destroy(this);
    }
    public void SpawnNewIcon(ITransportConnection player)
    {
        if (Effect == null)
            return;
        _lastPosition = transform.position;
        F.TriggerEffectUnreliable(Effect, player, _lastPosition + Offset);
        _lastBroadcast = Time.realtimeSinceStartup;
        L.LogDebug($"[ICONS] [{Effect.name}] Spawning icon for {player.GetAddressString(true)}.");
    }
    public void SpawnNewIcon(PooledTransportConnectionList players)
    {
        if (Effect == null)
            return;
        _lastPosition = transform.position;
        F.TriggerEffectUnreliable(Effect, players, _lastPosition + Offset);
        _lastBroadcast = Time.realtimeSinceStartup;
        L.LogDebug($"[ICONS] [{Effect.name}] Spawning icon for {players.Count} player(s).");
    }

    void IManualOnDestroy.ManualOnDestroy()
    {
        Destroy();
    }
}
