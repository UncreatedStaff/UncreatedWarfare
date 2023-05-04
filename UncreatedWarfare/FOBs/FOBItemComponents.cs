using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Structures;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using UnityEngine;
using XPReward = Uncreated.Warfare.Levels.XPReward;

namespace Uncreated.Warfare.FOBs;

public class RadioComponent : MonoBehaviour, IManualOnDestroy, IFOBItem, IShovelable, ISalvageListener
{
    private bool _destroyed;
#nullable disable
    public FOB FOB { get; set; }
#nullable restore
    public RadioState State { get; private set; }
    public BuildableType Type => BuildableType.Radio;
    public BarricadeDrop Barricade { get; private set; }
    public BuildableData? Buildable => null;
    public ulong Owner { get; private set; }
    public JsonAssetReference<EffectAsset>? Icon { get; private set; }
    public float IconOffset => 3.5f;
    public ulong Team { get; private set; }
    public bool IsSalvaged { get; set; }
    public ulong Salvager { get; set; }
    public bool Destroyed => _destroyed;
    public TickResponsibilityCollection Builders { get; } = new TickResponsibilityCollection();
    public Vector3 Position => transform.position;
    public bool NeedsRestock { get; internal set; }
    public float LastRestock { get; internal set; }

    [UsedImplicitly]
    private void Awake()
    {
        Barricade = BarricadeManager.FindBarricadeByRootTransform(transform);
        if (Barricade == null)
        {
            L.LogDebug($"[FOBS] RadioComponent added to unknown barricade: {name}.");
            goto destroy;
        }
        
        if (!Gamemode.Config.FOBRadios.Value.HasGuid(Barricade.asset.GUID))
        {
            if (Gamemode.Config.BarricadeFOBRadioDamaged.MatchGuid(Barricade.asset))
            {
                State = RadioState.Bleeding;
                Icon = Gamemode.Config.EffectMarkerRadioDamaged;
            }
            else
            {
                L.LogDebug($"[FOBS] RadioComponent unable to find a valid buildable: {(Buildable?.Foundation?.Value?.Asset?.itemName)}.");
                goto destroy;
            }
        }
        else
        {
            State = RadioState.Alive;
            Icon = Gamemode.Config.EffectMarkerRadio;
        }

        Owner = Barricade.GetServersideData().owner;
        Team = Barricade.GetServersideData().group.GetTeam();
        Builders.Set(Owner, FOBManager.Config.BaseFOBRepairHits);
        
        if (Barricade.interactable is InteractableStorage storage)
        {
            storage.despawnWhenDestroyed = true;
            storage.items.onStateUpdated += InvalidateRestock;
        }

        return;
        destroy:
        State = RadioState.Destroyed;
        Destroy(this);
    }

    internal void InvalidateRestock()
    {
        if (!NeedsRestock)
            LastRestock = Time.realtimeSinceStartup - 40f;
        NeedsRestock = true;
        L.Log($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Restock invalidated.");
    }
    void ISalvageListener.OnSalvageRequested(SalvageRequested e)
    {
        if (!e.Player.OnDuty())
        {
            L.Log($"[FOBS] [{FOB?.Name ?? "FLOATING"}] {e.Player} tried to salvage the radio.");
            e.Break();
            e.Player.SendChat(T.WhitelistProhibitedSalvage, Barricade.asset);
        }
    }
    [UsedImplicitly]
    private void Start()
    {
        FOB?.Restock();
        L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Radio Initialized: {Barricade.asset.itemName}. (State: {State}).");
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        if (!_destroyed && Barricade != null && Barricade.model != null && !Barricade.GetServersideData().barricade.isDead &&
            BarricadeManager.tryGetRegion(Barricade.model, out byte x, out byte y, out ushort plant, out _))
        {
            BarricadeManager.destroyBarricade(Barricade, x, y, plant);
            _destroyed = true;
            Barricade = null!;
        }

        FOBManager.EnsureDisposed(this);

        State = RadioState.Destroyed;
    }

    void IManualOnDestroy.ManualOnDestroy()
    {
        if (Barricade is { interactable: InteractableStorage { items: { } } storage })
            storage.items.onStateUpdated -= InvalidateRestock;
        _destroyed = true;
        Destroy(this);
    }

    public enum RadioState
    {
        Alive,
        Bleeding,
        Destroyed
    }

    public bool Shovel(UCPlayer shoveler)
    {
        if (shoveler.GetTeam() != Team) return false;
        if (State is RadioState.Bleeding or RadioState.Alive)
        {
            ushort maxHealth = Barricade.asset.health;
            float amt = maxHealth / FOBManager.Config.BaseFOBRepairHits * FOBManager.GetBuildIncrementMultiplier(shoveler);
            if (Barricade.GetServersideData().barricade.health + amt > maxHealth)
                amt = Barricade.asset.health - Barricade.GetServersideData().barricade.health;
            if (amt == 0)
                return true;
            BarricadeManager.repair(Barricade.model, amt, 1, shoveler.CSteamID);
            FOBManager.TriggerBuildEffect(transform.position);
            Builders.Increment(shoveler.Steam64, amt);
            UpdateHitsUI();

            if (State == RadioState.Bleeding && Barricade.GetServersideData().barricade.health >= maxHealth)
                FOB.UpdateRadioState(RadioState.Alive);

            return true;
        }

        return false;
    }
    public void QuickShovel(UCPlayer shoveler)
    {
        if (State is RadioState.Bleeding or RadioState.Alive)
        {
            ushort maxHealth = Barricade.asset.health;
            float amt = maxHealth - Barricade.GetServersideData().barricade.health;
            BarricadeManager.repair(Barricade.model, amt, 1, shoveler.CSteamID);
            FOBManager.TriggerBuildEffect(transform.position);
            Builders.Increment(shoveler.Steam64, amt);
            UpdateHitsUI();

            if (State == RadioState.Bleeding)
                FOB.UpdateRadioState(RadioState.Alive);
        }
    }
    private void UpdateHitsUI()
    {
        Builders.RetrieveLock();
        try
        {
            float time = Time.realtimeSinceStartup;
            ToastMessage msg = new ToastMessage(
                Points.GetProgressBar(Barricade.GetServersideData().barricade.health, Barricade.asset.health, 25).Colorize("ff9966"),
                ToastMessageSeverity.Progress);
            foreach (TickResponsibility responsibility in Builders)
            {
                if (time - responsibility.LastUpdated < 5f)
                {
                    if (UCPlayer.FromID(responsibility.Steam64) is { } pl && pl.Player.TryGetPlayerData(out UCPlayerData component))
                        component.QueueMessage(msg, true);
                }
            }
        }
        finally
        {
            Builders.ReturnLock();
        }
    }
}

public class ShovelableComponent : MonoBehaviour, IManualOnDestroy, IFOBItem, IShovelable, ISalvageListener
{
    private bool _destroyed;
    private int _buildRemoved;
    private float _progressToBuild;
    public FOB? FOB { get; set; }
    public BuildableType Type { get; private set; }
    public BuildableState State { get; private set; }
    public BuildableData Buildable { get; private set; }
    public IBuildable? ActiveStructure { get; private set; }
    public InteractableVehicle? ActiveVehicle { get; private set; }
    public IBuildable? Base { get; private set; }
    public Vector3 Position { get; private set; }
    public ulong Team { get; private set; }
    public ulong Owner { get; private set; }
    public float Progress { get; private set; }
    public float Total { get; private set; }
    public bool IsSalvaged { get; set; }
    public ulong Salvager { get; set; }
    public bool IsFloating { get; private set; }
    public JsonAssetReference<EffectAsset>? Icon { get; protected set; }
    public float IconOffset { get; protected set; }
    public TickResponsibilityCollection Builders { get; } = new TickResponsibilityCollection();
    public Asset Asset { get; protected set; }

    [UsedImplicitly]
    private void Awake()
    {
        BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(transform);
        if (barricade == null)
        {
            StructureDrop structure = StructureManager.FindStructureByRootTransform(transform);
            if (structure == null)
            {
                InteractableVehicle vehicle = DamageTool.getVehicle(transform);
                if (vehicle == null)
                {
                    L.LogWarning($"[FOBS] ShovelableComponent not added to barricade, structure, or vehicle: {name}.");
                    goto destroy;
                }

                ActiveVehicle = vehicle;
                ActiveStructure = null;
                Asset = vehicle.asset;
                Position = vehicle.transform.position;
                Team = vehicle.lockedGroup.m_SteamID.GetTeam();
                Owner = vehicle.lockedOwner.m_SteamID;
            }
            else
            {
                ActiveStructure = new UCStructure(structure);
                Asset = structure.asset;
                Position = structure.model.position;
                Team = structure.GetServersideData().group.GetTeam();
                Owner = structure.GetServersideData().owner;
            }
        }
        else
        {
            ActiveStructure = new UCBarricade(barricade);
            Asset = barricade.asset;
            Position = barricade.model.position;
            Team = barricade.GetServersideData().group.GetTeam();
            Owner = barricade.GetServersideData().owner;
        }

        Progress = 0f;

        Buildable = FOBManager.FindBuildable(Asset)!;
        Type = Buildable.Type;
        if (Buildable is not { Type: not BuildableType.Radio })
        {
            L.LogWarning($"[FOBS] ShovelableComponent unable to find a valid buildable: " +
                         $"{Buildable?.Foundation?.Value?.Asset?.itemName} ({Asset.FriendlyName}).");
            goto destroy;
        }

        IconOffset = Buildable.Type switch
        {
            BuildableType.Bunker => 5.5f,
            BuildableType.AmmoCrate => 1.75f,
            BuildableType.RepairStation => 4.5f,
            _ => default
        };
        if (ActiveStructure != null && Buildable.Foundation.MatchGuid(ActiveStructure.Asset.GUID))
        {
            Total = Buildable.RequiredHits;
            State = BuildableState.Foundation;
            if (Buildable.RequiredHits > 15)
                Icon = Gamemode.Config.EffectMarkerBuildable;
        }
        else
        {
            State = BuildableState.Full;
            Icon = Buildable.Type switch
            {
                BuildableType.Bunker => Gamemode.Config.EffectMarkerBunker,
                BuildableType.AmmoCrate => Gamemode.Config.EffectMarkerAmmo,
                BuildableType.RepairStation => Gamemode.Config.EffectMarkerRepair,
                _ => default
            };
        }

        InitAwake();
        return;
        destroy:
        Destroy(this);
    }

    [UsedImplicitly]
    private void Start()
    {
        IsFloating = FOB == null;
        InitStart();
        L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] {Asset.FriendlyName} Initialized: {Buildable} in state: {State}.");
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        Destroy();
        if (!_destroyed)
        {
            if (ActiveStructure != null)
            {
                if (ActiveStructure.Destroy())
                {
                    _destroyed = true;
                    ActiveStructure = null!;
                }
            }
            else if (ActiveVehicle != null)
            {
                for (int i = 0; i < ActiveVehicle.turrets.Length; ++i)
                {
                    byte[] state = ActiveVehicle.turrets[i].state;
                    if (state.Length != 18)
                        continue;
                    Attachments.parseFromItemState(state, out _, out _, out _, out _, out ushort mag);
                    byte amt = state[10];
                    if (mag != 0 && Assets.find(EAssetType.ITEM, mag) is ItemMagazineAsset asset)
                        ItemManager.dropItem(new Item(asset.id, amt, 100), ActiveVehicle.transform.position, true, false, true);
                }
                VehicleBarricadeRegion region = BarricadeManager.findRegionFromVehicle(ActiveVehicle);
                if (region != null)
                {
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        if (region.drops[i].interactable is InteractableStorage st)
                            st.despawnWhenDestroyed = true;
                    }
                }
#if false // explode the vehicle instead of destroying it
                if (!ActiveVehicle.isExploded)
                    VehicleManager.sendVehicleExploded(ActiveVehicle);
#else
                VehicleManager.askVehicleDestroy(ActiveVehicle);
#endif
                ActiveVehicle = null;
                _destroyed = true;
            }
        }
        if (Base != null && Base.Destroy())
        {
            _destroyed = true;
            ActiveStructure = null!;
        }


        FOBManager.EnsureDisposed(this);
        L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Destroyed: {Buildable} ({Asset.FriendlyName}).");
    }
    void IManualOnDestroy.ManualOnDestroy()
    {
        _destroyed = true;
        Destroy(this);
    }
    void ISalvageListener.OnSalvageRequested(SalvageRequested e)
    {
        if (State != BuildableState.Foundation)
        {
            if (!e.Player.OnDuty())
            {
                L.Log($"[FOBS] [{FOB?.Name ?? "FLOATING"}] {e.Player} tried to salvage {Buildable}.");
                e.Break();
                e.Player.SendChat(T.WhitelistProhibitedSalvage, ActiveStructure?.Asset ?? Buildable.Foundation.GetAsset()!);
            }
        }
        else if (_buildRemoved > 0 && FOB != null)
        {
            int refund = Mathf.CeilToInt(_buildRemoved * (FOBManager.Config.SalvageRefundPercentage / 100f));
            FOBManager.ShowResourceToast(new LanguageSet(e.Player), build: refund);
            FOB.ModifyBuild(refund);
            _buildRemoved = 0;
        }
    }
    protected virtual void InitAwake() { }
    protected virtual void InitStart() { }
    protected virtual void Destroy() { }

    public bool Shovel(UCPlayer shoveler)
    {
        if (State == BuildableState.Foundation && shoveler.GetTeam() == Team)
        {
            if (FOB == null && !IsFloating)
            {
                shoveler.SendChat(T.BuildTickNotInRadius);
                return true;
            }
            if (!IsFloating && !FOB!.ValidatePlacement(Buildable, shoveler, this) ||
                IsFloating && Data.Is(out IFOBs fobs) && !fobs.FOBManager.ValidateFloatingPlacement(Buildable, shoveler, transform.position, this))
            {
                return false;
            }

            float amount = FOBManager.GetBuildIncrementMultiplier(shoveler);

            L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Incrementing build: {shoveler} ({Progress} + {amount} = {Progress + amount} / {Total}).");
            Progress += amount;
            
            FOBManager.TriggerBuildEffect(transform.position);
            
            Builders.Increment(shoveler.Steam64, amount);


            if (FOB != null)
            {
                float build = (float)Buildable.RequiredBuild / (float)Buildable.RequiredHits * amount;
                _progressToBuild += build;
                int build2 = Mathf.FloorToInt(_progressToBuild);
                L.LogDebug($"[FOBS] [{FOB.Name ?? "FLOATING"}]  Removing build: {build:F4} (rounded to: {build2}).");
                if (FOB.BuildSupply < _progressToBuild)
                {
                    shoveler.SendChat(T.BuildMissingSupplies, FOB.BuildSupply, Buildable.RequiredBuild - _buildRemoved);
                    return true;
                }
                if (build2 > 0)
                {
                    _progressToBuild -= build2;
                    _buildRemoved += build2;
                    SendBuildToastToBuilders(-build2);
                    FOB.ModifyBuild(-build2);
                }
            }


            if (Progress >= Total)
            {
                int buildRemaining = Buildable.RequiredBuild - _buildRemoved;
                _progressToBuild = 0;
                if (FOB != null)
                {
                    if (FOB.BuildSupply < buildRemaining)
                    {
                        shoveler.SendChat(T.BuildMissingSupplies, FOB.BuildSupply, Buildable.RequiredBuild - _buildRemoved);
                        return true;
                    }

                    SendBuildToastToBuilders(-buildRemaining);
                    FOB?.ModifyBuild(-buildRemaining);
                }
                UpdateHitsUI();
                Build();
            }
            else
                UpdateHitsUI();

            return true;
        }

        return false;
    }
    public void QuickShovel(UCPlayer shoveler)
    {
        if (State == BuildableState.Foundation)
        {
            float amount = Total - Progress;
            L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Incrementing build: {shoveler} ({Progress} + {amount} = {Progress + amount} / {Total}).");
            Progress += amount;

            FOBManager.TriggerBuildEffect(transform.position);

            Builders.Increment(shoveler.Steam64, amount);
            UpdateHitsUI();

            if (Progress >= Total)
                Build();
        }
    }

    private void UpdateHitsUI()
    {
        Builders.RetrieveLock();
        try
        {
            float time = Time.realtimeSinceStartup;
            ToastMessage msg = new ToastMessage(Points.GetProgressBar(Progress, Total, 25), ToastMessageSeverity.Progress);
            foreach (TickResponsibility responsibility in Builders)
            {
                if (time - responsibility.LastUpdated < 5f && UCPlayer.FromID(responsibility.Steam64) is { } pl && pl.Player.TryGetPlayerData(out UCPlayerData component))
                    component.QueueMessage(msg, true);
            }
        }
        finally
        {
            Builders.ReturnLock();
        }
    }
    private void SendBuildToastToBuilders(int delta)
    {
        if (delta == 0) return;
        Builders.RetrieveLock();
        try
        {
            float time = Time.realtimeSinceStartup;
            foreach (TickResponsibility responsibility in Builders)
            {
                if (time - responsibility.LastUpdated < 5f && UCPlayer.FromID(responsibility.Steam64) is { } pl)
                    FOBManager.ShowResourceToast(new LanguageSet(pl), build: delta);
            }
        }
        finally
        {
            Builders.ReturnLock();
        }
    }

    public bool Build()
    {
        if (State != BuildableState.Foundation)
            return false;
        IBuildable? newBase = null;
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        ulong group = TeamManager.GetGroupID(Team);
        // base
        if (Buildable.Emplacement != null && Buildable.Emplacement.BaseBarricade.ValidReference(out ItemAsset @base))
        {
            if (@base is ItemBarricadeAsset bAsset)
            {
                FOBManager.IgnorePlacingBarricade = true;
                try
                {
                    Barricade b = new Barricade(bAsset, bAsset.health, bAsset.getState());
                    Transform? t = BarricadeManager.dropNonPlantedBarricade(b, position, rotation, Owner, group);
                    BarricadeDrop? drop = t == null ? null : BarricadeManager.FindBarricadeByRootTransform(t);
                    if (drop != null)
                        newBase = new UCBarricade(drop);
                }
                finally
                {
                    FOBManager.IgnorePlacingBarricade = false;
                }
            }
            else if (@base is ItemStructureAsset sAsset)
            {
                FOBManager.IgnorePlacingStructure = true;
                try
                {
                    Structure s = new Structure(sAsset, sAsset.health);
                    bool success = StructureManager.dropReplicatedStructure(s, position, rotation, Owner, group);
                    if (success)
                    {
                        if (Regions.tryGetCoordinate(position, out byte x, out byte y) && StructureManager.tryGetRegion(x, y, out StructureRegion region))
                            newBase = new UCStructure(region.drops.GetTail());
                    }
                }
                finally
                {
                    FOBManager.IgnorePlacingStructure = false;
                }
            }


            if (newBase == null)
                L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Unable to place base: {@base.itemName}.");
            else
                L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Placed base: {@base.itemName}.");
        }

        Transform? newTransform = null;

        // emplacement
        if (Buildable.Emplacement != null && Buildable.Emplacement.EmplacementVehicle.ValidReference(out VehicleAsset vehicle))
        {
            InteractableVehicle veh = FOBManager.SpawnEmplacement(vehicle, position, rotation, Owner, group);

            if (veh == null)
                L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Unable to spawn vehicle: {vehicle.vehicleName}.");
            else
            {
                newTransform = veh.transform;
                L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Spawned vehicle: {vehicle.vehicleName}.");
            }
        }

        // fortification
        if (newTransform == null && Buildable.FullBuildable.ValidReference(out ItemAsset buildable))
        {
            if (buildable is ItemBarricadeAsset bAsset)
            {
                FOBManager.IgnorePlacingBarricade = true;
                try
                {
                    Barricade b = new Barricade(bAsset, bAsset.health, bAsset.getState());
                    Transform? t = BarricadeManager.dropNonPlantedBarricade(b, position, rotation, Owner, group);
                    BarricadeDrop? drop = t == null ? null : BarricadeManager.FindBarricadeByRootTransform(t);
                    if (drop != null)
                        newTransform = drop.model;
                }
                finally
                {
                    FOBManager.IgnorePlacingBarricade = false;
                }
            }
            else if (buildable is ItemStructureAsset sAsset)
            {
                FOBManager.IgnorePlacingStructure = true;
                try
                {
                    Structure s = new Structure(sAsset, sAsset.health);
                    bool success = StructureManager.dropReplicatedStructure(s, position, rotation, Owner, group);
                    if (success)
                    {
                        if (Regions.tryGetCoordinate(position, out byte x, out byte y) && StructureManager.tryGetRegion(x, y, out StructureRegion region))
                            newTransform = region.drops.GetTail().model;
                    }
                }
                finally
                {
                    FOBManager.IgnorePlacingStructure = false;
                }
            }

            if (newTransform == null)
                L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Unable to place buildable: {buildable.itemName}.");
            else
                L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Placed buildable: {buildable.itemName}.");
        }

        if (newTransform == null)
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Parent for buildable upgrade not spawned: {Buildable}.");
            if (newBase != null)
                newBase.Destroy();
            return false;
        }

        IFOBItem? @new = null;
        if (FOB != null)
            @new = FOB.UpgradeItem(this, newTransform);
        else if (Data.Is(out IFOBs fobs))
            @new = fobs.FOBManager.UpgradeFloatingItem(this, newTransform);
        
        if (@new == null)
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Unable to upgrade buildable: {Buildable}.");
            newBase?.Destroy();
            return false;
        }

        if (@new is ShovelableComponent sh)
            sh.Base = newBase;

        return true;
    }

    public enum BuildableState
    {
        Full,
        Foundation,
        Destroyed
    }
}

public class BunkerComponent : ShovelableComponent
{
    public Vector3 SpawnPosition => transform.position;
    public float SpawnYaw => transform.rotation.eulerAngles.y;
}

public class RepairStationComponent : ShovelableComponent
{
    private static readonly List<InteractableVehicle> WorkingVehicles = new List<InteractableVehicle>(12);
    public readonly Dictionary<uint, int> VehiclesRepairing = new Dictionary<uint, int>(3);
    private int _counter;
    protected override void InitAwake()
    {
        if (Buildable.Type != BuildableType.RepairStation)
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] RepairStationComponent not added to a repair station: {Buildable}.");
            goto destroy;
        }
        if (ActiveStructure == null)
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] RepairStationComponent not added to a barricade or structure: {Buildable}.");
            goto destroy;
        }
        
        return;
        destroy:
        Destroy(this);
    }

    protected override void InitStart()
    {
        StartCoroutine(RepairStationLoop());
    }
    private IEnumerator<WaitForSeconds> RepairStationLoop()
    {
        const int tickCountPerBuild = 9;
        const float tickSpeed = 1.5f;

        while (true)
        {
            if (State == BuildableState.Full && Data.Gamemode is { State: Gamemodes.State.Staging or Gamemodes.State.Active } && (FOB == null || !FOB.Bleeding))
            {
#if DEBUG
                IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                VehicleManager.getVehiclesInRadius(Position, 25f * 25f, WorkingVehicles);
                try
                {
                    for (int i = 0; i < WorkingVehicles.Count; i++)
                    {
                        InteractableVehicle vehicle = WorkingVehicles[i];
                        if (vehicle.lockedGroup.m_SteamID.GetTeam() != Team || vehicle.isDead)
                            continue;

                        if (vehicle.asset.engine is not EEngine.PLANE and not EEngine.HELICOPTER &&
                            (Position - vehicle.transform.position).sqrMagnitude > 12f * 12f)
                            continue;

                        if (vehicle.health >= vehicle.asset.health && vehicle.fuel >= vehicle.asset.fuel - 10) // '- 10' so it doesn't use a build as you drive away
                        {
                            if (VehiclesRepairing.ContainsKey(vehicle.instanceID))
                                VehiclesRepairing.Remove(vehicle.instanceID);
                        }
                        else
                        {
                            if (VehiclesRepairing.TryGetValue(vehicle.instanceID, out int ticks))
                            {
                                if (ticks > 0)
                                {
                                    if (vehicle.health < vehicle.asset.health)
                                    {
                                        TickRepair(vehicle);
                                        --ticks;
                                    }
                                    else if (_counter % 3 == 0 && !vehicle.isEngineOn)
                                    {
                                        TickRefuel(vehicle);
                                        --ticks;
                                    }
                                }
                                if (ticks <= 0)
                                    VehiclesRepairing.Remove(vehicle.instanceID);
                                else
                                    VehiclesRepairing[vehicle.instanceID] = ticks;
                            }
                            else
                            {
                                bool inMain = TeamManager.IsInMain(Team, Position);
                                FOB? owningFob = inMain ? null : FOB;
                                if (inMain || (owningFob != null && owningFob.BuildSupply > 0))
                                {
                                    VehiclesRepairing.Add(vehicle.instanceID, tickCountPerBuild);
                                    TickRepair(vehicle);

                                    if (owningFob != null)
                                    {
                                        owningFob.ModifyBuild(-1);
                                        if (vehicle.TryGetComponent(out VehicleComponent comp) && UCPlayer.FromID(comp.LastDriver) is { } lastDriver)
                                            FOBManager.ShowResourceToast(new LanguageSet(lastDriver), build: -1, message: T.FOBResourceToastRepairVehicle.Translate(lastDriver));

                                        UCPlayer? stationPlacer = UCPlayer.FromID(Owner);
                                        if (stationPlacer != null)
                                        {
                                            if (stationPlacer.CSteamID != vehicle.lockedOwner)
                                                Points.AwardXP(stationPlacer, XPReward.RepairVehicle);

                                            if (stationPlacer.Steam64 != owningFob.Owner)
                                                Points.TryAwardFOBCreatorXP(owningFob, XPReward.RepairVehicle, 0.5f);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    _counter++;
                }
                finally
                {
                    WorkingVehicles.Clear();
                }
#if DEBUG
                profiler.Dispose();
#endif
            }
            yield return new WaitForSeconds(tickSpeed);
        }
    }
    public void TickRepair(InteractableVehicle vehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (vehicle.health >= vehicle.asset.health)
            return;

        const ushort amount = 25;

        ushort newHealth = (ushort)Math.Min(vehicle.health + amount, ushort.MaxValue);
        if (vehicle.health + amount >= vehicle.asset.health)
        {
            newHealth = vehicle.asset.health;
            if (vehicle.transform.TryGetComponent(out VehicleComponent c))
            {
                c.DamageTable.Clear();
            }
        }

        VehicleManager.sendVehicleHealth(vehicle, newHealth);
        if (Gamemode.Config.EffectRepair.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicle.transform.position);
        vehicle.updateVehicle();
    }
    public void TickRefuel(InteractableVehicle vehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (vehicle.fuel >= vehicle.asset.fuel)
            return;

        const ushort amount = 180;

        vehicle.askFillFuel(amount);

        if (Gamemode.Config.EffectRefuel.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicle.transform.position);
        vehicle.updateVehicle();
    }
}

public interface IShovelable
{
    TickResponsibilityCollection Builders { get; }
    bool Shovel(UCPlayer shoveler);
    void QuickShovel(UCPlayer shoveler);
}

public interface IFOBItem
{
    FOB? FOB { get; set; }
    BuildableType Type { get; }
    BuildableData? Buildable { get; }
    ulong Team { get; }
    ulong Owner { get; }
    JsonAssetReference<EffectAsset>? Icon { get; }
    float IconOffset { get; }
    Vector3 Position { get; }
}

public enum FobRadius : byte
{
    Short,
    Full,
    FullBunkerDependant,
    FobPlacement,
    EnemyBunkerClaim
}
