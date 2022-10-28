using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uncreated.Framework;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Structures;

public class StructureSaver : ListSingleton<SavedStructure>, ILevelStartListener
{
    public static bool Loaded => Singleton.IsLoaded<StructureSaver, SavedStructure>();
    internal static StructureSaver Singleton;
    public StructureSaver() : base("structures", Path.Combine(Data.Paths.StructureStorage, "structures.json")) { }
    public override void Load()
    {
        Singleton = this;
    }
    public override void Unload()
    {
        Singleton = null!;
    }
    protected override string LoadDefaults() => EMPTY_LIST;
    void ILevelStartListener.OnLevelReady()
    {
        CheckAll();
    }
    public static bool AddStructure(StructureDrop structure, out SavedStructure newStructure)
    {
        Singleton.AssertLoaded<StructureSaver, SavedStructure>();
        uint id = structure.instanceID;

        for (int i = 0; i < Singleton.Count; ++i)
        {
            if (Singleton[i].InstanceID == id && Singleton[i].ItemGuid == structure.asset.GUID)
            {
                newStructure = Singleton[i];
                return false;
            }
        }

        StructureData data = structure.GetServersideData();
        newStructure = new SavedStructure()
        {
            Group = data.group,
            Owner = data.owner,
            InstanceID = data.instanceID,
            ItemGuid = structure.asset.GUID,
            Position = structure.model.position,
            Rotation = structure.model.rotation.eulerAngles
        };
        Singleton.Add(newStructure);
        Singleton.Save();
        return true;
    }
    public static bool AddBarricade(BarricadeDrop barricade, out SavedStructure newStructure)
    {
        Singleton.AssertLoaded<StructureSaver, SavedStructure>();
        uint id = barricade.instanceID;

        for (int i = 0; i < Singleton.Count; ++i)
        {
            if (Singleton[i].InstanceID == id && Singleton[i].ItemGuid == barricade.asset.GUID)
            {
                newStructure = Singleton[i];
                return false;
            }
        }

        BarricadeData data = barricade.GetServersideData();
        newStructure = new SavedStructure()
        {
            Group = data.group,
            Owner = data.owner,
            InstanceID = data.instanceID,
            ItemGuid = barricade.asset.GUID,
            Position = barricade.model.position,
            Rotation = barricade.model.rotation.eulerAngles
        };
        newStructure.Metadata = GetBarricadeState(barricade, barricade.asset, newStructure);
        Singleton.Add(newStructure);
        Singleton.Save();
        return true;
    }
    public static bool RemoveSave(SavedStructure save)
    {
        Singleton.AssertLoaded<StructureSaver, SavedStructure>();
        uint id = save.InstanceID;
        bool rem = false;
        for (int i = Singleton.Count - 1; i >= 0; --i)
        {
            if (Singleton[i].InstanceID == id && Singleton[i].ItemGuid == save.ItemGuid)
            {
                Singleton.RemoveAt(i);
                rem = true;
            }
        }

        if (rem) Singleton.Save();
        return rem;
    }
    public static bool SaveExists(StructureDrop structure, out SavedStructure found) => SaveExists(structure.instanceID, EStructType.STRUCTURE, out found);
    public static bool SaveExists(BarricadeDrop barricade, out SavedStructure found) => SaveExists(barricade.instanceID, EStructType.BARRICADE, out found);
    public static bool SaveExists(uint instanceId, EStructType type, out SavedStructure found)
    {
        Singleton.AssertLoaded<StructureSaver, SavedStructure>();
        if (type == EStructType.STRUCTURE || type == EStructType.BARRICADE)
        {
            for (int i = 0; i < Singleton.Count; ++i)
            {
                if (Singleton[i].InstanceID == instanceId)
                {
                    if (Assets.find(Singleton[i].ItemGuid) is ItemAsset a1 && (
                        type == EStructType.STRUCTURE && a1 is ItemStructureAsset ||
                        type == EStructType.BARRICADE && a1 is ItemBarricadeAsset))
                    {
                        found = Singleton[i];
                        return true;
                    }
                }
            }
        }

        found = null!;
        return false;
    }
    public static void SaveSingleton()
    {
        Singleton.AssertLoaded<StructureSaver, SavedStructure>();
        Singleton.Save();
    }
    internal void CheckAll()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = Count - 1; i >= 0; --i)
        {
            if (!Check(this[i]))
                RemoveAt(i);
        }

        Save();
    }
    internal bool Check(SavedStructure structure)
    {
        try
        {
            if (Assets.find(structure.ItemGuid) is not ItemAsset asset)
            {
                ReportError(structure, "GUID is not a valid asset.");
                return false;
            }

            uint id = structure.InstanceID;
            Vector3 pos = structure.Position;
            Vector3 rot = structure.Rotation;
            if (asset is ItemStructureAsset structureAsset)
            {
                StructureDrop? drop = GetStructureDrop(id, pos);
                if (drop is not null)
                {
                    Vector3 ea = drop.model.rotation.eulerAngles;
                    if (drop.model.position != pos || rot != ea)
                    {
                        structure.Position = drop.model.position;
                        structure.Rotation = ea;
                    }
                }
                else
                {
                    drop = GetStructureDrop(pos, rot);
                    if (drop is null)
                    {
                        if (Regions.tryGetCoordinate(pos, out byte x, out byte y) &&
                            StructureManager.dropReplicatedStructure(
                                new Structure(structureAsset, structureAsset.health), pos,
                                Quaternion.Euler(rot), structure.Owner, structure.Group))
                        {
                            L.LogDebug("[STRUCTURE SAVER] Spawned new structure: " + structureAsset.itemName + " at " + pos.ToString("F0", Data.Locale));
                            List<StructureDrop> d = StructureManager.regions[x, y].drops;
                            drop = d[d.Count - 1];
                            if (drop.asset.GUID != structureAsset.GUID)
                            {
                                ReportError(structure, "Unable to spawn structure.");
                                return false;
                            }
                            structure.Position = drop.model.position;
                            structure.Rotation = drop.model.rotation.eulerAngles;
                            structure.InstanceID = drop.instanceID;
                            return true;
                        }
                        else
                        {
                            ReportError(structure, "Unable to spawn structure.");
                            return false;
                        }
                    }
                    L.LogDebug("[STRUCTURE SAVER] Identified alternate structure: " + structureAsset.itemName + " at " + pos.ToString("F0", Data.Locale));
                    structure.Position = drop.model.position;
                    structure.Rotation = drop.model.rotation.eulerAngles;
                    structure.InstanceID = drop.instanceID;
                }
                if (drop.model.TryGetComponent(out Interactable2HP hp))
                {
                    hp.hp = 100;
                }

                StructureManager.changeOwnerAndGroup(drop.model, structure.Owner, structure.Group);
                return true;
            }
            else if (asset is ItemBarricadeAsset barricadeAsset)
            {
                BarricadeDrop? drop = GetBarricadeDrop(id, pos);
                if (drop is not null)
                {
                    if (drop.model.parent != null && drop.model.parent.CompareTag("Vehicle"))
                    {
                        ReportError(structure, "Unable to load a planted barricade.");
                        return false;
                    }
                    Vector3 ea = drop.model.rotation.eulerAngles;
                    if (drop.model.position != pos || rot != ea)
                    {
                        structure.Position = drop.model.position;
                        structure.Rotation = ea;
                    }
                }
                else
                {
                    drop = GetBarricadeDrop(pos, rot);
                    if (drop is null)
                    {
                        Transform t = BarricadeManager.dropNonPlantedBarricade(
                            new Barricade(barricadeAsset, barricadeAsset.health, GetBarricadeState(null, barricadeAsset, structure)), pos,
                            Quaternion.Euler(rot), structure.Owner, structure.Group);
                        drop = BarricadeManager.FindBarricadeByRootTransform(t);
                        if (drop is null)
                        {
                            ReportError(structure, "Unable to spawn barricade.");
                            return false;
                        }
                        L.LogDebug("[STRUCTURE SAVER] Spawned new barricade: " + barricadeAsset.itemName + " at " + pos.ToString("F0", Data.Locale));
                        structure.Position = drop.model.position;
                        structure.Rotation = drop.model.rotation.eulerAngles;
                        structure.InstanceID = drop.instanceID;
                        return true;
                    }
                    L.LogDebug("[STRUCTURE SAVER] Identified alternate barricade: " + barricadeAsset.itemName + " at " + pos.ToString("F0", Data.Locale));

                    structure.Position = drop.model.position;
                    structure.Rotation = drop.model.rotation.eulerAngles;
                    structure.InstanceID = drop.instanceID;
                }

                BarricadeData bd = drop.GetServersideData();
                bd.group = structure.Group;
                bd.owner = structure.Owner;

                if (drop.interactable != null)
                {
                    byte[] dropSt = GetBarricadeState(drop, barricadeAsset, structure);
                    byte[] oldSt = drop.GetServersideData().barricade.state;
                    if (dropSt.Length == oldSt.Length)
                    {
                        for (int i = 0; i < dropSt.Length; ++i)
                            if (dropSt[i] != oldSt[i])
                                goto next;
                    }
                    else goto next;
                    structure.Metadata = oldSt;
                    goto setHp;
                next:
                    L.LogDebug("[STRUCTURE SAVER] " + structure.InstanceID + " State: " + string.Join(" ", oldSt.Select(x => x.ToString("X2"))) +
                           " to " + string.Join(" ", dropSt.Select(x => x.ToString("X2"))) + " at " + pos.ToString("F0", Data.Locale));
                    if (drop.interactable is InteractableSign sign)
                    {
                        BarricadeManager.updateState(drop.model, dropSt, dropSt.Length);
                        sign.updateState(barricadeAsset, dropSt);
                        Signs.BroadcastSignUpdate(sign);
                    }
                    else
                        BarricadeManager.updateReplicatedState(drop.model, dropSt, dropSt.Length);
                    structure.Metadata = dropSt;
                }
            setHp:
                if (drop.model.TryGetComponent(out Interactable2HP hp))
                {
                    hp.hp = 100;
                }
                BarricadeManager.changeOwnerAndGroup(drop.model, structure.Owner, structure.Group);
                return true;
            }
            else return false;
        }
        catch (Exception ex)
        {
            L.LogError("Error checking structure save: ");
            L.LogError(ex);
            return false;
        }
    }
    private static void ReportError(SavedStructure structure, string reason)
    {
        L.LogWarning("Error loading " + structure.ToString() + ":", method: "STRUCTURE SAVER");
        L.LogWarning(reason, method: "STRUCTURE SAVER");
    }
    private static byte[] GetBarricadeState(BarricadeDrop? drop, ItemBarricadeAsset asset, SavedStructure structure)
    {
        byte[] st2 = drop != null ? drop.GetServersideData().barricade.state : (structure.Metadata is null || structure.Metadata.Length == 0 ? Array.Empty<byte>() : structure.Metadata);
        byte[] owner = BitConverter.GetBytes(structure.Owner);
        byte[] group = BitConverter.GetBytes(structure.Group);
        byte[] state;
        switch (asset.build)
        {
            case EBuild.DOOR:
            case EBuild.GATE:
            case EBuild.SHUTTER:
            case EBuild.HATCH:
                state = new byte[17];
                Buffer.BlockCopy(owner, 0, state, 0, sizeof(ulong));
                Buffer.BlockCopy(group, 0, state, sizeof(ulong), sizeof(ulong));
                state[16] = (byte)(st2[16] > 0 ? 1 : 0);
                break;
            case EBuild.BED:
                state = owner;
                break;
            case EBuild.STORAGE:
            case EBuild.SENTRY:
            case EBuild.SENTRY_FREEFORM:
            case EBuild.SIGN:
            case EBuild.SIGN_WALL:
            case EBuild.NOTE:
            case EBuild.LIBRARY:
            case EBuild.MANNEQUIN:
                state = Util.CloneBytes(st2);
                if (state.Length > 15)
                {
                    Buffer.BlockCopy(owner, 0, state, 0, sizeof(ulong));
                    Buffer.BlockCopy(group, 0, state, sizeof(ulong), sizeof(ulong));
                }
                break;
            case EBuild.SPIKE:
            case EBuild.WIRE:
            case EBuild.CHARGE:
            case EBuild.BEACON:
            case EBuild.CLAIM:
                state = Array.Empty<byte>();
                if (drop != null)
                {
                    if (drop.interactable is InteractableCharge charge)
                    {
                        charge.owner = structure.Owner;
                        charge.group = structure.Group;
                    }
                    else if (drop.interactable is InteractableClaim claim)
                    {
                        claim.owner = structure.Owner;
                        claim.group = structure.Group;
                    }
                }
                break;
            default:
                state = Util.CloneBytes(st2);
                break;
        }

        return state;
    }
    private static StructureDrop? GetStructureDrop(uint instanceId, Vector3 expectedPosition)
    {
        if (Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y))
        {
            List<StructureDrop> d = StructureManager.regions[x, y].drops;
            for (int i = 0; i < d.Count; ++i)
            {
                if (d[i].instanceID == instanceId)
                    return d[i];
            }
        }
        for (x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                List<StructureDrop> d = StructureManager.regions[x, y].drops;
                for (int i = 0; i < d.Count; ++i)
                {
                    if (d[i].instanceID == instanceId)
                        return d[i];
                }
            }
        }

        return null;
    }
    private static StructureDrop? GetStructureDrop(Vector3 position, Vector3 euler)
    {
        if (Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            List<StructureDrop> d = StructureManager.regions[x, y].drops;
            for (int i = 0; i < d.Count; ++i)
            {
                if (d[i].model.position == position && d[i].model.rotation.eulerAngles == euler)
                    return d[i];
            }
        }

        return null;
    }
    private static BarricadeDrop? GetBarricadeDrop(uint instanceId, Vector3 expectedPosition, bool vehicle = false)
    {
        if (!vehicle)
        {
            if (Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y))
            {
                List<BarricadeDrop> d = BarricadeManager.regions[x, y].drops;
                for (int i = 0; i < d.Count; ++i)
                {
                    if (d[i].instanceID == instanceId)
                        return d[i];
                }
            }
            for (x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    List<BarricadeDrop> d = BarricadeManager.regions[x, y].drops;
                    for (int i = 0; i < d.Count; ++i)
                    {
                        if (d[i].instanceID == instanceId)
                            return d[i];
                    }
                }
            }
        }

        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            List<BarricadeDrop> d = BarricadeManager.vehicleRegions[v].drops;
            for (int i = 0; i < d.Count; ++i)
            {
                if (d[i].instanceID == instanceId)
                    return d[i];
            }
        }

        return null;
    }
    private static BarricadeDrop? GetBarricadeDrop(Vector3 position, Vector3 euler)
    {
        if (Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            List<BarricadeDrop> d = BarricadeManager.regions[x, y].drops;
            for (int i = 0; i < d.Count; ++i)
            {
                if (d[i].model.position == position && d[i].model.rotation.eulerAngles == euler)
                    return d[i];
            }
        }

        return null;
    }
}

public enum EStructType : byte
{
    UNKNOWN = 0,
    STRUCTURE = 1,
    BARRICADE = 2
}