using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles;

[SingletonDependency(typeof(VehicleSpawner))]
[SingletonDependency(typeof(VehicleBay))]
[SingletonDependency(typeof(StructureSaverOld))]
public class VehicleSigns : ListSingleton<VehicleSign>, ILevelStartListener
{
    public VehicleSigns() : base("vehiclesigns", Path.Combine(Data.Paths.VehicleStorage, "signs.json")) { }
    private static VehicleSigns Singleton;
    public static bool Loaded => Singleton.IsLoaded<VehicleSigns, VehicleSign>();
    public override void Load()
    {
        Singleton = this;
    }
    public override void Unload()
    {
        Singleton = null!;
    }
    public void OnLevelReady()
    {
        InitAllSigns();
    }
    public void InitAllSigns()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Count; i++)
        {
            try
            {
                this[i].InitVars();
            }
            catch (Exception ex)
            {
                L.LogError("Failed to initialize a vbs sign.");
                L.LogError(ex);
            }
        }
    }
    internal void OnBarricadeDestroyed(BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Count; i++)
        {
            VehicleSign vs = this[i];
            if (vs is not null && vs.InstanceId == instanceID)
            {
                StructureSaverOld.RemoveSave(vs.StructureSave);
                Remove(vs);
                Save();
                break;
            }
        }
    }
    public static IEnumerable<VehicleSign> GetLinkedSigns(VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSigns, VehicleSign>();
        return Singleton.GetObjectsWhere(x => x.VehicleBay != null && x.VehicleBay.InstanceId == spawn.InstanceId && x.VehicleBay.StructureType == spawn.StructureType);
    }

    protected override string LoadDefaults() => "[]";
    public static void SaveSingleton()
    {
        Singleton.AssertLoaded<VehicleSigns, VehicleSign>();
        Singleton.Save();
    }
    public static void UnlinkSign(InteractableSign sign)
    {
        Singleton.AssertLoaded<VehicleSigns, VehicleSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
        if (drop != null)
        {
            for (int i = 0; i < Singleton.Count; i++)
            {
                if (Singleton[i] != null && Singleton[i].InstanceId == drop.instanceID)
                {
                    RequestSigns.SetSignTextSneaky(sign, string.Empty);
                    VehicleSign vs = Singleton[i];
                    StructureSaverOld.RemoveSave(vs.StructureSave);
                    Singleton.Remove(Singleton[i]);
                    if (VehicleSpawner.Loaded)
                    {
                        foreach (VehicleSpawn spawn in VehicleSpawner.Spawners)
                        {
                            if (spawn.LinkedSign == Singleton[i])
                            {
                                spawn.LinkedSign = null;
                                spawn.UpdateSign();
                                Signs.BroadcastSignUpdate(drop);
                            }
                        }
                    }
                    Singleton.Save();
                    break;
                }
            }
        }
    }
    public static void OnFlagCaptured()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (VehicleSpawner.Loaded)
        {
            foreach (VehicleSpawn spawn in VehicleSpawner.Spawners)
            {
                if (VehicleBay.VehicleExists(spawn.VehicleGuid, out VehicleData data) && (data.HasDelayType(EDelayType.FLAG) || data.HasDelayType(EDelayType.FLAG_PERCENT)))
                {
                    spawn.UpdateSign();
                }
            }
        }
    }
    public static bool SignExists(InteractableSign sign, out VehicleSign vbsign)
    {
        Singleton.AssertLoaded<VehicleSigns, VehicleSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
        if (drop != null)
        {
            return Singleton.ObjectExists(x => x != default && x.InstanceId == drop.instanceID, out vbsign) && vbsign.VehicleBay != null;
        }
        vbsign = default!;
        return false;
    }
    public static bool LinkSign(InteractableSign sign, VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSigns, VehicleSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
        if (drop != null)
        {
            if (!StructureSaverOld.SaveExists(drop, out SavedStructure structure))
                StructureSaverOld.AddBarricade(drop, out structure);

            VehicleSign n = Singleton.AddObjectToSave(new VehicleSign(drop, sign, structure, spawn));
            spawn.LinkedSign = n;

            n.StructureSave.Metadata = RequestSigns.SetSignTextSneaky(sign, n.SignText);
            StructureSaverOld.SaveSingleton();
            spawn.UpdateSign();
            return true;
        }
        return false;
    }

#pragma warning disable IDE0031
    internal static void TimeSync()
    {
        if (VehicleSpawner.Loaded)
        {
            for (int i = 0; i < VehicleSpawner.Singleton.Count; ++i)
            {
                VehicleSpawn spawn = VehicleSpawner.Singleton[i];
                if (spawn.Component != null)
                    spawn.Component.TimeSync();
            }
        }
    }
#pragma warning restore IDE0031
}
[JsonSerializable(typeof(VehicleSign))]
public class VehicleSign
{
    [JsonIgnore]
    private SavedStructure _structureSave;
    [JsonIgnore]
    private VehicleSpawn _vehicleBay;

    [JsonIgnore]
    public SavedStructure StructureSave => _structureSave;

    [JsonIgnore]
    public VehicleSpawn VehicleBay => _vehicleBay;

    [JsonIgnore]
    public BarricadeDrop? SignDrop { get; private set; }

    [JsonIgnore]
    public InteractableSign? SignInteractable { get; private set; }

    [JsonPropertyName("sign_instance_id")]
    public uint InstanceId { get; set; }

    [JsonPropertyName("bay_instance_id")]
    public uint BayInstanceId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("bay_type")]
    public EStructType BayStructureType { get; set; }

    [JsonPropertyName("sign_text")]
    public string SignText { get; set; }

    public override string ToString() => $"Instance id: {InstanceId}, bay: {BayInstanceId}, text: {SignText}";
    public VehicleSign()
    {
        this.SignText = string.Empty;
        this.BayStructureType = EStructType.UNKNOWN;
    }
    public void InitVars()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeDrop? drop = UCBarricadeManager.GetBarricadeFromInstID(InstanceId);
        if (!StructureSaverOld.SaveExists(this.InstanceId, EStructType.BARRICADE, out _structureSave))
        {
            if (drop == null)
            {
                L.LogWarning("Failed to link sign to the correct instance id.");
            }
            else if (!StructureSaverOld.SaveExists(drop, out _structureSave))
            {
                if (!StructureSaverOld.AddBarricade(drop, out _structureSave))
                {
                    L.LogWarning("Failed to add sign to structure saver.");
                    return;
                }
            }
        }
        SignDrop = drop;
        SignInteractable = drop?.interactable as InteractableSign;

        if (SignDrop == null)
            L.LogWarning("Unable to get drop of sign " + InstanceId);
        else if (SignInteractable == null)
            L.LogWarning("Unable to get interactable of sign " + InstanceId);

        if (!VehicleSpawner.IsRegistered(this.BayInstanceId, out _vehicleBay, this.BayStructureType))
        {
            L.LogWarning("Sign not linked: " + this.InstanceId);
        }
        else
        {
            VehicleBay.LinkedSign = this;
            VehicleBay.UpdateSign();
        }
    }
    public VehicleSign(BarricadeDrop drop, InteractableSign sign, SavedStructure save, VehicleSpawn bay)
    {
        if (save == null || bay == null) throw new ArgumentNullException("save or bay", "Can not create a vehicle sign unless save and bay are defined.");
        _structureSave = save;
        _vehicleBay = bay;
        this.InstanceId = save.InstanceID;
        this.BayInstanceId = bay.InstanceId;
        this.BayStructureType = bay.StructureType;
        Asset? asset = Assets.find(bay.VehicleGuid);
        this.SignText = $"sign_vbs_" + (asset == null ? bay.VehicleGuid.ToString("N") : asset.id.ToString(Data.Locale));
        this.SignInteractable = sign;
        this.SignDrop = drop;
        bay.LinkedSign = this;
        if (!StructureSaverOld.SaveExists(bay.InstanceId, bay.StructureType, out SavedStructure s))
        {
            if (bay.StructureType == EStructType.BARRICADE)
            {
                BarricadeData? paddata =
                    UCBarricadeManager.GetBarricadeFromInstID(bay.InstanceId, out BarricadeDrop? paddrop);
                if (paddata != null)
                    StructureSaverOld.AddBarricade(paddrop!, out _);
            }
            else if (bay.StructureType == EStructType.STRUCTURE)
            {
                StructureData? paddata =
                    UCBarricadeManager.GetStructureFromInstID(bay.InstanceId, out StructureDrop? paddrop);
                if (paddata != null)
                    StructureSaverOld.AddStructure(paddrop!, out _);
            }
        }
    }
}
