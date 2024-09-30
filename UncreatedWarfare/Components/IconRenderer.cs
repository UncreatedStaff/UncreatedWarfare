using SDG.Framework.Utilities;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Components;

public class IconManager : ILayoutHostedService, IEventListener<PlayerLeft>, IEventListener<PlayerGroupChanged>
{
    private readonly IPlayerService _playerService;
    private const float FullTickLoopTime = 0.25f;
    private readonly List<IconRenderer> _icons = new List<IconRenderer>();
    private int _tickIndex;
    private float _tickIndexProgress;
    public IconManager(IPlayerService playerService)
    {
        _playerService = playerService;
        TimeUtility.updated += OnUpdate;
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        DeleteAllIcons();
        CheckExistingBuildables();
        return UniTask.CompletedTask;
    }

   void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        for (int i = _icons.Count - 1; i >= 0; i--)
        {
            IconRenderer iconRenderer = _icons[i];
            if (iconRenderer.Player != e.Steam64.m_SteamID)
                continue;

            _icons.RemoveAt(i);
            iconRenderer.Destroy();
        }
    }

    public void DeleteAllIcons()
    {
        for (int i = _icons.Count - 1; i >= 0; i--)
        {
            IconRenderer iconRenderer = _icons[i];
            DeleteIcon(iconRenderer);
        }
    }
    public void CheckExistingBuildables()
    {
        // foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateNonPlantedBarricades())
        // {
        //     CheckBuildable(barricade.Drop.asset, barricade.Drop.model, /* todo team */ null);
        // }
        // 
        // foreach (StructureInfo structure in StructureUtility.EnumerateStructures())
        // {
        //     CheckBuildable(structure.Drop.asset, structure.Drop.model, /* todo team */ null);
        // }
        // 
        // void CheckBuildable(Asset asset, Transform transform, Team? team)
        // {
        //     BuildableData? buildableData = FobManager.FindBuildable(asset);
        //     if (buildableData == null || !buildableData.FullBuildable.MatchGuid(asset.GUID))
        //         return;
        // 
        //     L.LogDebug($"[ICONS] [{asset.FriendlyName}] Found existing buildable, try-applying marker type: {buildableData.Type}.");
        //     switch (buildableData.Type)
        //     {
        //         case BuildableType.AmmoCrate:
        //             if (Gamemode.Config.EffectMarkerAmmo.TryGetGuid(out Guid guid))
        //                 AttachIcon(guid, transform, team, 1.75f);
        //             break;
        //         case BuildableType.RepairStation:
        //             if (Gamemode.Config.EffectMarkerRepair.TryGetGuid(out guid))
        //                 AttachIcon(guid, transform, team, 4.5f);
        //             break;
        //     }
        // }
    }
    
    void IEventListener<PlayerGroupChanged>.HandleEvent(PlayerGroupChanged e, IServiceProvider serviceProvider)
    {
        DrawNewMarkers(e.Player, true);
    }

    public void DrawNewMarkers(WarfarePlayer player, bool clearOld)
    {
        List<Guid> seenTypes = clearOld ? new List<Guid>(_icons.Count) : null!;

        Team team = player.Team;
        foreach (IconRenderer icon in _icons)
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
    public void AttachIcon(Guid effectGUID, Transform transform, Team team, float yOffset = 0, ulong player = 0)
        => AttachIcon(effectGUID, transform, new Vector3(0f, yOffset, 0f), team, player);
    public void AttachIcon(Guid effectGUID, Transform transform, Vector3 offset, Team team, ulong player = 0)
    {
        if (transform.gameObject.TryGetComponent(out IconRenderer icon))
            DeleteIcon(icon);
        
        icon = transform.gameObject.AddComponent<IconRenderer>();
        icon.Initialize(effectGUID, offset, team, this, player);

        SpawnIcons(icon);

        _icons.Add(icon);
        L.LogDebug($"[ICONS] [{icon.Effect?.name}] Icon attached.");
    }
    public void DeleteIcon(IconRenderer icon, bool destroy = true)
    {
        if (!_icons.Contains(icon))
            return;
        EffectManager.ClearEffectByGuid_AllPlayers(icon.EffectGUID);
        _icons.Remove(icon);
        if (destroy)
            icon.Destroy();

        SpawnNewIconsOfType(icon.EffectGUID);
        L.LogDebug($"[ICONS] [{icon.Effect?.name}] Icon deleted.");
    }
    public void DrawNewIconsOfType(Guid effectGUID)
    {
        EffectManager.ClearEffectByGuid_AllPlayers(effectGUID);
        SpawnNewIconsOfType(effectGUID);
    }
    public void SpawnNewIconsOfType(Guid effectGUID)
    {
        foreach (IconRenderer icon in _icons)
        {
            if (icon.EffectGUID == effectGUID)
                SpawnIcons(icon);
        }
    }
    public void SpawnIcons(IconRenderer icon)
    {
        // todo optimize
        icon.SpawnNewIcon(Data.GetPooledTransportConnectionList((icon.Player == 0 ? (icon.Team == 0
                ? _playerService.OnlinePlayers
                : _playerService.OnlinePlayers.Where(x => x.Team == icon.Team)) : (_playerService.GetOnlinePlayerOrNull(icon.Player) is not { } player ? Array.Empty<WarfarePlayer>() : [ player ]))
            .Select(x => x.Connection)));
    }
    private void OnUpdate()
    {
        if (_icons.Count == 0) return;
        float tickAmount = Time.deltaTime * _icons.Count / FullTickLoopTime;
        _tickIndexProgress += tickAmount;
        int newTickIndex = Mathf.FloorToInt(_tickIndexProgress);
        if (newTickIndex < _icons.Count)
        {
            if (newTickIndex == _tickIndex)
                return;
            for (int i = _tickIndex; i < newTickIndex; ++i)
            {
                IconRenderer renderer = _icons[i];
                renderer.Tick();
            }
        }
        else
        {
            newTickIndex %= _icons.Count;
            for (int i = _tickIndex; i < _icons.Count; ++i)
            {
                IconRenderer renderer = _icons[i];
                renderer.Tick();
            }
            for (int i = 0; i < newTickIndex; ++i)
            {
                IconRenderer renderer = _icons[i];
                renderer.Tick();
            }
        }

        _tickIndex = newTickIndex;
        _tickIndexProgress = newTickIndex;
    }
}


public class IconRenderer : MonoBehaviour, IManualOnDestroy
{
    private IconManager _iconManager;
    private float _lastBroadcast;
    private Vector3 _lastPosition;
    public Guid EffectGUID { get; private set; }
    public EffectAsset Effect { get; private set; }
    public Team? Team { get; private set; }
    public ulong Player { get; private set; }
    public Vector3 Point => _lastPosition;
    public Vector3 Offset { get; private set; }
    public float Lifetime { get; private set; }
    public bool LifetimeCheck { get; private set; }
    public void Initialize(Guid effectGUID, Vector3 offset, Team? team, IconManager iconManager, ulong player = 0)
    {
        _iconManager = iconManager;

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
        _iconManager.DeleteIcon(this, false);
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
                _iconManager.DrawNewIconsOfType(EffectGUID);
                drew = true;
            }
        }
        if (!drew && isActiveAndEnabled)
        {
            Vector3 position = transform.position;
            if (!_lastPosition.IsNearlyEqual(position))
            {
                _iconManager.DrawNewIconsOfType(EffectGUID);
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
        EffectUtility.TriggerEffect(Effect, player, _lastPosition + Offset, false);
        _lastBroadcast = Time.realtimeSinceStartup;
        L.LogDebug($"[ICONS] [{Effect.name}] Spawning icon for {player.GetAddressString(true)}.");
    }
    public void SpawnNewIcon(PooledTransportConnectionList players)
    {
        if (Effect == null)
            return;
        _lastPosition = transform.position;
        EffectUtility.TriggerEffect(Effect, players, _lastPosition + Offset, false);
        _lastBroadcast = Time.realtimeSinceStartup;
        L.LogDebug($"[ICONS] [{Effect.name}] Spawning icon for {players.Count} player(s).");
    }

   void IManualOnDestroy.ManualOnDestroy()
    {
        Destroy();
    }
}
