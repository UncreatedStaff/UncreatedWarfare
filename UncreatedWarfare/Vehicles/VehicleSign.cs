﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Uncreated.SQL;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;

namespace Uncreated.Warfare.Vehicles;

[SingletonDependency(typeof(VehicleSpawner))]
[SingletonDependency(typeof(VehicleBay))]
[SingletonDependency(typeof(StructureSaver))]
[SingletonDependency(typeof(Level))]
public class VehicleSignsOld : ListSingleton<VehicleSign>, ILevelStartListener
{
    public VehicleSignsOld() : base("vehiclesigns", Path.Combine(Data.Paths.VehicleStorage, "signs.json")) { }
    private static VehicleSignsOld Singleton;
    public static bool Loaded => Singleton.IsLoaded<VehicleSignsOld, VehicleSign>();
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
                Data.Singletons.GetSingleton<StructureSaver>()?.BeginRemoveItem(vs.StructureSave);
                Remove(vs);
                Save();
                break;
            }
        }
    }
    public static IEnumerable<VehicleSign> GetLinkedSigns(VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSignsOld, VehicleSign>();
        return Singleton.GetObjectsWhere(x => x.VehicleBay != null && x.VehicleBay.InstanceId == spawn.InstanceId && x.VehicleBay.StructureType == spawn.StructureType);
    }

    protected override string LoadDefaults() => "[]";
    public static void SaveSingleton()
    {
        Singleton.AssertLoaded<VehicleSignsOld, VehicleSign>();
        Singleton.Save();
    }
    public static void UnlinkSign(InteractableSign sign)
    {
        Singleton.AssertLoaded<VehicleSignsOld, VehicleSign>();
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
                    Signs.SetSignTextServerOnly(sign, string.Empty);
                    VehicleSign vs = Singleton[i];
                    Data.Singletons.GetSingleton<StructureSaver>()?.RemoveItem(vs.StructureSave);
                    Singleton.Remove(Singleton[i]);
                    if (VehicleSpawnerOld.Loaded)
                    {
                        foreach (VehicleSpawn spawn in VehicleSpawnerOld.Spawners)
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
        if (VehicleSpawnerOld.Loaded)
        {
            foreach (VehicleSpawn spawn in VehicleSpawnerOld.Spawners)
            {
                if (spawn.Data?.Item != null && (spawn.Data.Item.HasDelayType(DelayType.Flag) || spawn.Data.Item.HasDelayType(DelayType.FlagPercentage)))
                {
                    spawn.UpdateSign();
                }
            }
        }
    }
    public static bool SignExists(InteractableSign sign, out VehicleSign vbsign)
    {
        Singleton.AssertLoaded<VehicleSignsOld, VehicleSign>();
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
        Singleton.AssertLoaded<VehicleSignsOld, VehicleSign>();
#if DEBUG
        IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        if (drop != null && saver != null)
        {
            Task.Run(async () =>
            {
                try
                {
                    (SqlItem<SavedStructure> item, _) = await saver.AddBarricade(drop);
                    await UCWarfare.ToUpdate();
                    VehicleSign n = Singleton.AddObjectToSave(new VehicleSign(drop, sign, item.Item!, spawn));
                    spawn.LinkedSign = n;

                    Signs.SetSignTextServerOnly(sign, n.SignText);
                    n.StructureSave.Metadata = drop.GetServersideData().barricade.state;
                    spawn.UpdateSign();
                }
                catch (Exception ex)
                {
                    L.LogError(ex);
                }
#if DEBUG
                profiler.Dispose();
#endif
            });
            return true;
        }
#if DEBUG
        profiler.Dispose();
#endif
        return false;
    }

#pragma warning disable IDE0031
    internal static void TimeSync()
    {
        if (VehicleSpawnerOld.Loaded)
        {
            for (int i = 0; i < VehicleSpawnerOld.Singleton.Count; ++i)
            {
                VehicleSpawn spawn = VehicleSpawnerOld.Singleton[i];
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
    public StructType BayStructureType { get; set; }

    [JsonPropertyName("sign_text")]
    public string SignText { get; set; }

    public override string ToString() => $"Instance id: {InstanceId}, bay: {BayInstanceId}, text: {SignText}";
    public VehicleSign()
    {
        this.SignText = string.Empty;
        this.BayStructureType = StructType.Unknown;
    }
    public void InitVars()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeDrop? drop = UCBarricadeManager.FindBarricade(InstanceId);
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        if (saver != null)
        {
            if (!saver.TryGetSaveNoLock(this.InstanceId, StructType.Barricade, out _structureSave))
            {
                if (drop == null)
                {
                    L.LogWarning("Failed to link sign to the correct instance id.");
                }
                else if (!saver.TryGetSaveNoLock(drop, out _structureSave))
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            if (!saver.IsLoading)
                                return;
                            (SqlItem<SavedStructure> item, _) = await saver.AddBarricade(drop, F.DebugTimeout).ConfigureAwait(false);
                            if (item.Item == null)
                                L.LogWarning("Failed to add sign to structure saver.");
                            else
                            {
                                await UCWarfare.ToUpdate();
                                this._structureSave = item.Item;
                            }
                        }
                        catch (Exception ex)
                        {
                            L.LogError("Error adding vehicle sign save.");
                            L.LogError(ex);
                        }
                    });

                }
            }
        }
        SignDrop = drop;
        SignInteractable = drop?.interactable as InteractableSign;

        if (SignDrop == null)
            L.LogWarning("Unable to get drop of sign " + InstanceId);
        else if (SignInteractable == null)
            L.LogWarning("Unable to get interactable of sign " + InstanceId);

        if (!VehicleSpawnerOld.IsRegistered(this.BayInstanceId, out _vehicleBay, this.BayStructureType))
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
        if (save == null || bay == null) throw new ArgumentNullException("save/bay", "Can not create a vehicle sign unless save and bay are defined.");
        _structureSave = save;
        _vehicleBay = bay;
        this.InstanceId = save.InstanceID;
        this.BayInstanceId = bay.InstanceId;
        this.BayStructureType = bay.StructureType;
        Asset? asset = Assets.find(bay.VehicleGuid);
        this.SignText = "sign_vbs_" + (asset == null ? bay.VehicleGuid.ToString("N") : asset.id.ToString(Data.LocalLocale));
        this.SignInteractable = sign;
        this.SignDrop = drop;
        bay.LinkedSign = this;
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        if (saver != null)
        {
            if (!saver.TryGetSaveNoLock(bay.InstanceId, bay.StructureType, out SavedStructure _))
            {
                if (bay.StructureType == StructType.Barricade)
                {
                    BarricadeDrop? paddrop = UCBarricadeManager.FindBarricade(bay.InstanceId);
                    if (paddrop != null)
                        saver.BeginAddBarricade(paddrop);
                }
                else if (bay.StructureType == StructType.Structure)
                {
                    StructureDrop? paddrop = UCBarricadeManager.FindStructure(bay.InstanceId);
                    if (paddrop != null)
                        saver.BeginAddStructure(paddrop);
                }
            }
        }
    }
}
