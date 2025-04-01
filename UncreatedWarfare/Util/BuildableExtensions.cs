using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Models.Buildables;

namespace Uncreated.Warfare.Util;
public static class BuildableExtensions
{
    /// <summary>
    /// Destroyes every buildable that isn't currently saved.
    /// </summary>
    /// <remarks>Storages won't drop their items.</remarks>
    public static async Task<int> DestroyUnsavedBuildables(this BuildableSaver buildableSaver, CSteamID instigator = default, CancellationToken token = default)
    {
        List<BuildableSave> saves = await buildableSaver.GetAllSavesAsync(token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        bool isSalvaged = instigator.GetEAccountType() == EAccountType.k_EAccountTypeIndividual;

        int ct = 0;
        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades()
                     .Where(structure => !saves.Exists(x => !x.IsStructure && x.InstanceId == structure.Drop.instanceID))
                     .ToList()
                )
        {
            BarricadeUtility.PreventItemDrops(barricade.Drop);
            SetSalvageInfo(barricade.Drop.model, EDamageOrigin.Unknown, instigator, isSalvaged, null);
            BarricadeManager.destroyBarricade(barricade.Drop, barricade.Coord.x, barricade.Coord.y, barricade.Plant);
            ++ct;
        }

        foreach (StructureInfo structure in StructureUtility.EnumerateStructures()
                     .Where(structure => !saves.Exists(x => x.IsStructure && x.InstanceId == structure.Drop.instanceID))
                     .ToList()
                )
        {
            SetSalvageInfo(structure.Drop.model, EDamageOrigin.Unknown, instigator, isSalvaged, null);
            StructureManager.destroyStructure(structure.Drop, structure.Coord.x, structure.Coord.y, Vector3.zero);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Destroy the structure or barricade.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static bool Destroy(this IBuildable buildable)
    {
        if (buildable == null)
            throw new ArgumentNullException(nameof(buildable));

        GameThread.AssertCurrent();

        if (buildable.Model != null && !buildable.IsDead)
        {
            if (!buildable.IsStructure)
            {
                if (buildable.Drop is BarricadeDrop barricadeDrop
                    && !barricadeDrop.GetServersideData().barricade.isDead
                    && BarricadeManager.tryGetRegion(buildable.Model, out byte x, out byte y, out ushort plant, out _))
                {
                    BarricadeManager.destroyBarricade(barricadeDrop, x, y, plant);
                    return true;
                }
            }
            else
            {
                if (buildable.Drop is StructureDrop structureDrop
                    && !structureDrop.GetServersideData().structure.isDead
                    && StructureManager.tryGetRegion(buildable.Model, out byte x, out byte y, out _))
                {
                    StructureManager.destroyStructure(structureDrop, x, y, Vector3.zero);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Set the owner or group of a buildable.
    /// </summary>
    /// <returns><see langword="true"/> if the barricade state was replicated, otherwise <see langword="false"/>.</returns>
    public static bool SetOwnerOrGroup(this IBuildable obj, IServiceProvider serviceProvider, CSteamID? owner = null, CSteamID? group = null)
    {
        switch (obj.Drop)
        {
            case BarricadeDrop bdrop:
                return BarricadeUtility.SetOwnerOrGroup(bdrop, serviceProvider, owner, group);

            case StructureDrop sdrop:
                StructureUtility.SetOwnerOrGroup(sdrop, owner, group);
                return true;

            default:
                throw new InvalidOperationException($"Unable to get drop from IBuildable of type \"{obj.Drop?.GetType().AssemblyQualifiedName ?? "null"}\".");
        }
    }
    private static readonly List<IDestroyInfo> WorkingDestroyInfo = new List<IDestroyInfo>(2);
    private static readonly List<ISalvageInfo> WorkingSalvageInfo = new List<ISalvageInfo>(2);

    internal static void SetDestroyInfo(Transform buildableTransform, IBaseBuildableDestroyedEvent args, Func<IDestroyInfo, bool>? whileAction)
    {
        GameThread.AssertCurrent();
        buildableTransform.GetComponents(WorkingDestroyInfo);
        try
        {
            foreach (IDestroyInfo destroyInfo in WorkingDestroyInfo)
            {
                destroyInfo.DestroyInfo = args;
                if (whileAction != null && !whileAction(destroyInfo))
                    break;
            }
        }
        finally
        {
            WorkingDestroyInfo.Clear();
        }
    }

    internal static void SetSalvageInfo(Transform buildableTransform, EDamageOrigin damageOrigin, CSteamID? salvager, bool? isSalvaged, Func<ISalvageInfo, bool>? whileAction)
    {
        GameThread.AssertCurrent();
        buildableTransform.GetComponents(WorkingSalvageInfo);
        try
        {
            foreach (ISalvageInfo salvageInfo in WorkingSalvageInfo)
            {
                if (salvager.HasValue && (!isSalvaged.HasValue || isSalvaged.Value))
                    salvageInfo.Salvager = salvager.Value;
                if (isSalvaged.HasValue)
                    salvageInfo.IsSalvaged = isSalvaged.Value;

                if (whileAction != null && !whileAction(salvageInfo))
                    break;
            }
        }
        finally
        {
            WorkingSalvageInfo.Clear();
        }

        DestroyerComponent.AddOrUpdate(buildableTransform.gameObject, isSalvaged.GetValueOrDefault() ? salvager?.m_SteamID ?? 0ul : 0ul, isSalvaged.GetValueOrDefault(), damageOrigin);
    }

    /// <summary>
    /// Creates a <see cref="IBuildable"/> from a root transform of a barricade or structure.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static IBuildable? GetBuildableFromRootTransform(Transform transform)
    {
        GameThread.AssertCurrent();

        if (transform == null)
            return null;

        if (transform.gameObject.layer != LayerMasks.STRUCTURE)
        {
            BarricadeDrop? barricade = BarricadeManager.FindBarricadeByRootTransform(transform);
            if (barricade != null)
                return new BuildableBarricade(barricade);
        }

        if (transform.gameObject.layer != LayerMasks.BARRICADE)
        {
            StructureDrop? structure = StructureManager.FindStructureByRootTransform(transform);
            if (structure != null)
                return new BuildableStructure(structure);
        }

        return null;
    }

    /// <summary>
    /// Places a non-planted barricade or structure.
    /// </summary>
    /// <param name="asset"><see cref="ItemBarricadeAsset"/> or <see cref="ItemStructureAsset"/> to spawn.</param>
    /// <param name="position">Position to spawn the buildable at.</param>
    /// <param name="rotation">Rotation to spawn the buildable at.</param>
    /// <param name="owner">Steam ID of the owner of the buildable, if any.</param>
    /// <param name="group">Steam ID of the group of the buildable, if any.</param>
    /// <param name="health">Starting health of the barricade. If -1, the maximum health will be used.</param>
    /// <param name="state">Optional starting state for barricades.</param>
    /// <returns>The newly-created buildable.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException">Position is not inside a valid region.</exception>
    /// <exception cref="InvalidOperationException"><see cref="BarricadeManager"/> or <see cref="StructureManager"/> is not loaded yet.</exception>
    /// <exception cref="InvalidBarricadeStateException">The given state is not valid.</exception>
    /// <exception cref="ArgumentException">Asset is not a structure or barricade.</exception>
    /// <exception cref="GameThreadException"/>
    /// <exception cref="Exception">Failed to place buildable for some reason.</exception>
    public static IBuildable DropBuildable(ItemPlaceableAsset asset, Vector3 position, Quaternion rotation, CSteamID owner = default, CSteamID group = default, int health = -1, byte[]? state = null)
    {
        GameThread.AssertCurrent();

        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Position does not fall in a valid region.");
        }

        if (asset is ItemBarricadeAsset barricadeAsset)
        {
            if (BarricadeManager.regions == null)
                throw new InvalidOperationException("BarricadeManager not loaded.");

            if (state != null)
            {
                BarricadeUtility.VerifyState(state, barricadeAsset);
            }
            else
            {
                state = barricadeAsset.getState(EItemOrigin.ADMIN);
            }

            Barricade barricade = new Barricade(barricadeAsset, health is < 0 or > ushort.MaxValue ? barricadeAsset.health : (ushort)health, state);
            Transform? t = BarricadeManager.dropNonPlantedBarricade(
                barricade,
                position,
                rotation,
                owner.GetEAccountType() == EAccountType.k_EAccountTypeIndividual ? owner.m_SteamID : 0ul,
                group.m_SteamID
            );

            if (t is null)
            {
                throw new Exception("Failed to add barricade. This shouldn't happen.");
            }

            BarricadeRegion region = BarricadeManager.regions[x, y];
            BarricadeDrop drop;
            if (region.drops.Count > 0)
            {
                drop = region.drops.GetTail();
                if (drop.GetServersideData().barricade == barricade)
                    return new BuildableBarricade(drop);
            }

            drop = BarricadeManager.FindBarricadeByRootTransform(t);
            if (drop != null)
                return new BuildableBarricade(drop);

            throw new Exception("Failed to find added barricade. This shouldn't happen.");
        }
        
        if (asset is ItemStructureAsset structureAsset)
        {
            if (StructureManager.regions == null)
                throw new InvalidOperationException("StructureManager not loaded.");

            Structure structure = new Structure(structureAsset, health is < 0 or > ushort.MaxValue ? structureAsset.health : (ushort)health);
            if (!StructureManager.dropReplicatedStructure(
                    structure,
                    position,
                    rotation,
                    owner.GetEAccountType() == EAccountType.k_EAccountTypeIndividual ? owner.m_SteamID : 0ul,
                    group.m_SteamID))
            {
                throw new Exception("Failed to find added structure. This shouldn't happen.");
            }

            StructureRegion region = StructureManager.regions[x, y];
            if (region.drops.Count > 0)
            {
                StructureDrop drop = region.drops.GetTail();
                if (drop.GetServersideData().structure == structure)
                    return new BuildableStructure(drop);
            }

            throw new Exception("Failed to find added structure. This shouldn't happen.");
        }

        throw new ArgumentException("Expected barricade or structure.", nameof(asset));
    }

    /// <summary>
    /// Places a non-planted barricade or structure at the same position as <paramref name="buildable"/> after optionally destroying it. Planted barricades are supported.
    /// </summary>
    /// <param name="asset"><see cref="ItemBarricadeAsset"/> or <see cref="ItemStructureAsset"/> to spawn.</param>
    /// <param name="health">Starting health of the barricade. If -1, the maximum health will be used.</param>
    /// <param name="state">Optional starting state for barricades.</param>
    /// <returns>The newly-created buildable.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidOperationException"><see cref="BarricadeManager"/> or <see cref="StructureManager"/> is not loaded yet.</exception>
    /// <exception cref="InvalidBarricadeStateException">The given state is not valid.</exception>
    /// <exception cref="ArgumentException">Asset is not a structure or barricade.</exception>
    /// <exception cref="GameThreadException"/>
    /// <exception cref="Exception">Failed to place buildable for some reason.</exception>
    public static IBuildable ReplaceBuildable(this IBuildable buildable, ItemPlaceableAsset asset, bool destroyOld = true, int health = -1, byte[]? state = null)
    {
        GameThread.AssertCurrent();

        if (buildable == null)
            throw new ArgumentNullException(nameof(buildable));
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));
        if (asset is not ItemBarricadeAsset and not ItemStructureAsset)
            throw new ArgumentException("Expected barricade or structure.", nameof(asset));
        if (BarricadeManager.regions == null)
            throw new InvalidOperationException("BarricadeManager not loaded.");
        if (StructureManager.regions == null)
            throw new InvalidOperationException("StructureManager not loaded.");

        if (destroyOld && !buildable.IsDead)
        {
            buildable.Destroy();
        }

        // planted barricade
        if (buildable.IsOnVehicle && asset is ItemBarricadeAsset barricadeAsset)
        {
            InteractableVehicle? vehicle = buildable.VehicleParent;
            if (vehicle != null)
            {
                Barricade barricade = new Barricade(barricadeAsset,
                    health is < 0 or > ushort.MaxValue ? barricadeAsset.health : (ushort)health, state);

                Transform? t = BarricadeManager.dropPlantedBarricade(
                    vehicle.transform,
                    barricade,
                    buildable.Position,
                    buildable.Rotation,
                    buildable.Owner.m_SteamID,
                    buildable.Group.m_SteamID
                );

                if (t is null)
                {
                    throw new Exception("Failed to add planted barricade. This shouldn't happen.");
                }

                VehicleBarricadeRegion region = BarricadeManager.findRegionFromVehicle(vehicle);
                BarricadeDrop drop;
                if (region.drops.Count > 0)
                {
                    drop = region.drops.GetTail();
                    if (drop.GetServersideData().barricade == barricade)
                        return new BuildableBarricade(drop);
                }

                drop = BarricadeManager.FindBarricadeByRootTransform(t);
                if (drop != null)
                    return new BuildableBarricade(drop);

                throw new Exception("Failed to find added planted barricade. This shouldn't happen.");
            }
        }

        IBuildable newBuildable = DropBuildable(asset, buildable.Position, buildable.Rotation, buildable.Owner, buildable.Group, health, state);
        return newBuildable;
    }
}
