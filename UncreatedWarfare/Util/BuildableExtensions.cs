using System;
using System.Collections.Generic;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Buildables;

namespace Uncreated.Warfare.Util;
public static class BuildableExtensions
{
    private static readonly Dictionary<Guid, Bounds> CachedBarricadeBounds = new Dictionary<Guid, Bounds>();
    private static readonly List<Collider> WorkingColliders = new List<Collider>();

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

        if (buildable.Model != null && buildable.Alive)
        {
            if (!buildable.IsStructure)
            {
                if (buildable.Drop is BarricadeDrop barricadeDrop
                    && !barricadeDrop.GetServersideData().barricade.isDead
                    && BarricadeManager.tryGetRegion(buildable.Model, out byte x, out byte y, out ushort plant, out _))
                {
                    BarricadeUtility.PreventItemDrops(barricadeDrop);
                    SetSalvageInfo(barricadeDrop.model, EDamageOrigin.Unknown, CSteamID.Nil, false, null);
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
                    SetSalvageInfo(structureDrop.model, EDamageOrigin.Unknown, CSteamID.Nil, false, null);
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

        IBuildable newBuildable;

        // planted barricade
        if (buildable.IsOnVehicle && asset is ItemBarricadeAsset barricadeAsset)
        {
            InteractableVehicle? vehicle = buildable.VehicleParent;
            if (vehicle != null)
            {
                if (state == null)
                {
                    state = barricadeAsset.getState(EItemOrigin.ADMIN);
                }
                else
                {
                    BarricadeUtility.VerifyState(state, barricadeAsset);
                }

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
                if (drop == null)
                {
                    throw new Exception("Failed to find added planted barricade. This shouldn't happen.");
                }

                newBuildable = new BuildableBarricade(drop);

                try
                {
                    BuildableContainer container = BuildableContainer.Get(buildable);
                    container.Transfer(newBuildable);
                }
                catch (NullReferenceException) { }

                if (destroyOld && buildable.Alive)
                {
                    buildable.Destroy();
                }

                return newBuildable;

            }
        }

        newBuildable = DropBuildable(asset, buildable.Position, buildable.Rotation, buildable.Owner, buildable.Group, health, state);

        try
        {
            BuildableContainer container = BuildableContainer.Get(buildable);
            container.Transfer(newBuildable);
        }
        catch (NullReferenceException) { }

        if (destroyOld && buildable.Alive)
        {
            buildable.Destroy();
        }

        return newBuildable;
    }

    /// <summary>
    /// Get the local bounds for a buildable asset type. Cached after first use for each asset type.
    /// </summary>
    /// <remarks>Note that these bounds are rotated so that up is actually up.</remarks>
    public static bool TryGetBuildableBounds(ItemPlaceableAsset buildableAsset, out Bounds localBounds)
    {
        localBounds = default;

        if (buildableAsset.GUID == Guid.Empty)
            return false;

        if (CachedBarricadeBounds.TryGetValue(buildableAsset.GUID, out localBounds))
            return true;

        GameObject? model = buildableAsset switch
        {
            ItemBarricadeAsset barricade => barricade.barricade,
            ItemStructureAsset structure => structure.structure,
            _ => null
        };

        if (model == null)
            return false;

        Bounds workingBounds = default;
        model.GetComponentsInChildren(WorkingColliders);
        try
        {
            if (WorkingColliders.Count == 0)
                return false;

            for (int i = 0; i < WorkingColliders.Count; i++)
            {
                Collider collider = WorkingColliders[i];
                if (collider.gameObject.layer == LayerMasks.NAVMESH)
                    continue;

                Bounds b = default;
                switch (collider)
                {
                    case BoxCollider box:
                        b.center = box.center;
                        b.size = box.size;
                        break;

                    case CapsuleCollider capsule:
                        b.center = capsule.center;
                        float r = capsule.radius;
                        b.extents = capsule.direction switch
                        {
                            0 => new Vector3(capsule.height / 2f, r, r),
                            1 => new Vector3(r, capsule.height / 2f, r),
                            _ => new Vector3(r, r, capsule.height / 2f)
                        };
                        break;

                    case MeshCollider mesh:
                        b = mesh.sharedMesh.bounds;
                        break;

                    case SphereCollider sphere:
                        b.center = sphere.center;
                        r = sphere.radius;
                        b.extents = new Vector3(r, r, r);
                        break;

                    default:
                        continue;
                }

                if (i == 0)
                    workingBounds = b;
                else
                    workingBounds.Encapsulate(b);
            }
        }
        finally
        {
            WorkingColliders.Clear();
        }

        Vector3 e = workingBounds.extents;
        workingBounds.extents = new Vector3(Math.Abs(e.x), Math.Abs(e.z), Math.Abs(e.y));

        if (buildableAsset is ItemBarricadeAsset { offset: not 0 } bAsset)
        {
            Vector3 c = workingBounds.center;
            c.y += bAsset.offset;
            workingBounds.center = c;
        }

        localBounds = workingBounds;
        CachedBarricadeBounds.Add(buildableAsset.GUID, workingBounds);
        return true;
    }
}
