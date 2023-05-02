using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Structures;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;
using XPReward = Uncreated.Warfare.Levels.XPReward;

namespace Uncreated.Warfare.Components;

public class RadioComponent : MonoBehaviour, IManualOnDestroy, IFOBItem, IShovelable, ISalvageInfo
{
    private bool _destroyed;
#nullable disable
    public FOB FOB { get; set; }
#nullable restore
    public RadioState State { get; private set; }
    public BuildableType Type => BuildableType.Radio;
    public BarricadeDrop Barricade { get; private set; }
    public BuildableData Buildable { get; private set; }
    public ulong Owner { get; private set; }
    public JsonAssetReference<EffectAsset>? Icon { get; private set; }
    public ulong Team { get; private set; }
    public bool IsSalvaged { get; set; }
    public ulong Salvager { get; set; }
    public bool Destroyed => _destroyed;
    public TickResponsibilityCollection Builders { get; } = new TickResponsibilityCollection();

    [UsedImplicitly]
    private void Awake()
    {
        Barricade = BarricadeManager.FindBarricadeByRootTransform(transform);
        if (Barricade == null)
        {
            L.LogDebug($"[FOBS] RadioComponent added to unknown barricade: {name}.");
            goto destroy;
        }

        Buildable = FOBManager.FindBuildable(Barricade.asset)!;
        if (Buildable is not { Type: BuildableType.Radio })
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
        Builders.Set(Owner, 1);

        if (Barricade.interactable is InteractableStorage storage)
            storage.despawnWhenDestroyed = true;

        L.LogDebug("[FOBS] Radio Initialized: " + Barricade.asset.itemName + ". (State: " + State + ").");

        return;
        destroy:
        State = RadioState.Destroyed;
        Destroy(this);
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        if (!_destroyed && Barricade != null && Barricade.model != null &&
            BarricadeManager.tryGetRegion(Barricade.model, out byte x, out byte y, out ushort plant, out _))
        {
            BarricadeManager.destroyBarricade(Barricade, x, y, plant);
            _destroyed = true;
            Barricade = null!;
        }

        State = RadioState.Destroyed;
        ((IManualOnDestroy)this).ManualOnDestroy();
    }

    void IManualOnDestroy.ManualOnDestroy()
    {
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
        if (State == RadioState.Bleeding && shoveler.GetTeam() == Team)
        {
            ushort maxHealth = Barricade.asset.health;
            float amt = maxHealth / FOBManager.Config.BaseFOBRepairHits * FOBManager.GetBuildIncrementMultiplier(shoveler);

            BarricadeManager.repair(Barricade.model, amt, 1, shoveler.CSteamID);

            if (Barricade.GetServersideData().barricade.health >= maxHealth)
            {
                FOB.UpdateRadioState(RadioState.Alive);
            }

            return true;
        }

        return false;
    }
}

public class ShovelableComponent : MonoBehaviour, IManualOnDestroy, IFOBItem, IShovelable, ISalvageInfo
{
    private bool _destroyed;
    private bool _subbedToStructureEvent;
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
                    L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] ShovelableComponent not added to barricade, structure, or vehicle: {name}.");
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
                _subbedToStructureEvent = true;
                EventDispatcher.StructureDestroyed += OnStructureDestroyed;
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
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] ShovelableComponent unable to find a valid buildable: " +
                         $"{Buildable?.Foundation?.Value?.Asset?.itemName} ({Asset.FriendlyName}).");
            goto destroy;
        }

        if (ActiveStructure != null && Buildable.Foundation.MatchGuid(ActiveStructure.Asset.GUID))
        {
            Total = Buildable.RequiredHits;
            State = BuildableState.Foundation;
        }
        else
            State = BuildableState.Full;

        InitAwake();
        L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] {Asset.FriendlyName} Initialized: {Buildable} in state: {State}.");
        return;
        destroy:
        Destroy(this);
    }

    [UsedImplicitly]
    private void Start()
    {
        IsFloating = FOB == null;
        InitStart();
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        if (_subbedToStructureEvent)
            EventDispatcher.StructureDestroyed -= OnStructureDestroyed;
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
                VehicleSpawner.DeleteVehicle(ActiveVehicle);
                ActiveVehicle = null;
                _destroyed = true;
            }
        }
        if (Base != null && Base.Destroy())
        {
            _destroyed = true;
            ActiveStructure = null!;
        }
        L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Destroyed: {Buildable} ({Asset.FriendlyName}).");
    }
    void IManualOnDestroy.ManualOnDestroy()
    {
        _destroyed = true;
        Destroy(this);
    }
    private void OnStructureDestroyed(StructureDestroyed e)
    {
        if (ActiveStructure != null && ActiveStructure.Type == StructType.Structure && ActiveStructure.InstanceId == e.InstanceID)
        {
            _destroyed = true;
            Destroy(this);
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
            if (!IsFloating && Buildable.Type == BuildableType.Bunker && FOB!.Bunker != null)
            {
                shoveler.SendChat(T.BuildTickStructureExists, Buildable);
                return true;
            }

            return true;
        }

        return false;
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
            if (Data.Gamemode is { State: Gamemodes.State.Staging or Gamemodes.State.Active })
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
                        if (vehicle.lockedGroup.m_SteamID.GetTeam() != Team)
                            continue;

                        if (vehicle.asset.engine is not EEngine.PLANE and not EEngine.HELICOPTER &&
                            (Position - vehicle.transform.position).sqrMagnitude > 12f * 12f)
                            continue;

                        if (vehicle.health >= vehicle.asset.health && vehicle.fuel >= vehicle.asset.fuel)
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
}

public interface IFOBItem
{
    FOB? FOB { get; set; }
    BuildableType Type { get; }
    BuildableData Buildable { get; }
    ulong Team { get; }
    ulong Owner { get; }
    JsonAssetReference<EffectAsset>? Icon { get; }
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
