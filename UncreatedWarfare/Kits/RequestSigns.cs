using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Kits;

[SingletonDependency(typeof(KitManager))]
public class RequestSigns : ListSingleton<RequestSign>
{
    private static RequestSigns Singleton;
    public static bool Loaded => Singleton.IsLoaded<RequestSigns, RequestSign>();
    public RequestSigns() : base("kitsigns", Data.StructureStorage + "request_signs.json", RequestSign.WriteRequestSign, RequestSign.ReadRequestSign) { }
    protected override string LoadDefaults() => EMPTY_LIST;
    public override void Load()
    {
        Singleton = this;
    }
    public override void Unload()
    {
        Singleton = null!;
    }
    public static void DropAllSigns()
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (RequestSign sign in Singleton)
        {
            sign.SpawnCheck(false);
            if (!sign.exists)
                L.LogError("Failed to spawn sign " + sign.kit_name);
        }
        Singleton.Save();
    }
    public static void SaveSingleton()
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
        Singleton.Save();
    }
    public static bool AddRequestSign(InteractableSign sign, out RequestSign signadded)
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        signadded = default!;
        BarricadeDrop? drop = UCBarricadeManager.GetSignFromInteractable(sign);
        if (drop != null && !Singleton.ObjectExists(x => x.instance_id == drop.instanceID, out signadded))
        {
            signadded = new RequestSign(sign);
            Singleton.AddObjectToSave(signadded);
            return true;
        }
        return false;
    }
    public static void RemoveRequestSign(RequestSign sign)
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Singleton.RemoveWhere(x => x.instance_id == sign.instance_id);
        //await sign.InvokeUpdate();
    }
    public static void RemoveRequestSigns(string kitname)
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
        Singleton.RemoveWhere(x => x.kit_name == kitname);
    }
    public static bool SignExists(InteractableSign sign, out RequestSign found)
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (sign == null)
        {
            found = null!;
            return false;
        }
        BarricadeDrop? drop = UCBarricadeManager.GetSignFromInteractable(sign);
        if (drop == null)
        {
            found = null!;
            return false;
        }
        for (int i = 0; i < Singleton.Count; i++)
        {
            if (drop != null && drop.instanceID == Singleton[i].instance_id)
            {
                found = Singleton[i];
                return true;
            }
        }
        found = null!;
        return false;
    }
    public static bool SignExists(uint instance_id, out RequestSign found)
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
        return Singleton.ObjectExists(s => s != default && s.instance_id == instance_id, out found);
    }
    public static bool SignExists(string kitName, out RequestSign sign)
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
        return Singleton.ObjectExists(x => x.kit_name == kitName, out sign);
    }
    public static void UpdateSignsOfKit(string kitName, SteamPlayer? player = null)
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (KitManager.KitExists(kitName, out Kit kitobj))
        {
            if (kitobj.IsLoadout)
            {
                for (int i = 0; i < Singleton.Count; i++)
                {
                    if (Singleton[i].kit_name.StartsWith("loadout_"))
                        Singleton[i].InvokeUpdate();
                }
                return;
            }
        }
        if (player is null)
        {
            foreach (RequestSign sign in Singleton.GetObjectsWhere(x => x.kit_name == kitName))
                sign.InvokeUpdate();
        }
        else
        {
            foreach (RequestSign sign in Singleton.GetObjectsWhere(x => x.kit_name == kitName))
                sign.InvokeUpdate(player);
        }
    }
    public static void UpdateAllSigns(SteamPlayer? player = null)
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player is not null)
        {
            for (int i = 0; i < Singleton.Count; i++)
            {
                Singleton[i].InvokeUpdate(player);
            }
        }
        else
        {
            for (int i = 0; i < Singleton.Count; i++)
            {
                Singleton[i].InvokeUpdate();
            }
        }
    }
    public static void UpdateLoadoutSigns(SteamPlayer? player = null)
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player is not null)
        {
            for (int i = 0; i < Singleton.Count; i++)
            {
                if (Singleton[i].kit_name.StartsWith("loadout_"))
                    Singleton[i].InvokeUpdate(player);
            }
        }
        else
        {
            for (int i = 0; i < Singleton.Count; i++)
            {
                if (Singleton[i].kit_name.StartsWith("loadout_"))
                    Singleton[i].InvokeUpdate();
            }
        }
    }
    public static void SetSignTextSneaky(InteractableSign sign, string text)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeDrop barricadeByRootFast = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
        byte[] state = barricadeByRootFast.GetServersideData().barricade.state;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
        if (bytes.Length > byte.MaxValue)
        {
            L.LogWarning(text + " is too long to go on a sign! (SetSignTextSneaky)");
        }
        byte[] numArray1 = new byte[17 + bytes.Length];
        byte[] numArray2 = numArray1;
        Buffer.BlockCopy(state, 0, numArray2, 0, 16);
        numArray1[16] = (byte)bytes.Length;
        if (bytes.Length != 0)
            Buffer.BlockCopy(bytes, 0, numArray1, 17, bytes.Length);
        barricadeByRootFast.GetServersideData().barricade.state = numArray1;
        sign.updateState(barricadeByRootFast.asset, numArray1);
    }
}
public class RequestSign : IJsonReadWrite
{
    [JsonIgnore]
    public Kit? Kit
    {
        get
        {
            if (KitManager.KitExists(kit_name, out Kit k))
                return k;
            else return default;
        }
    }
    [JsonIgnore]
    public string SignText
    {
        get => "sign_" + kit_name;
        set
        {
            if (value == default) kit_name = TeamManager.DefaultKit;
            else if (value.Length > 5 && value.StartsWith("sign_"))
                kit_name = value.Substring(5);
            else kit_name = value;
        }
    }
    [JsonSettable]
    public string kit_name;
    public SerializableTransform transform;
    [JsonSettable]
    public Guid sign_id;
    [JsonSettable]
    public ulong owner;
    [JsonSettable]
    public ulong group;
    [JsonIgnore]
    public Transform? barricadetransform;
    public uint instance_id;
    [JsonIgnore]
    public bool exists;
    [JsonConstructor]
    public RequestSign(string kit_name, SerializableTransform transform, Guid sign_id, ulong owner, ulong group, uint instance_id)
    {
        this.kit_name = kit_name;
        this.transform = transform;
        this.sign_id = sign_id;
        this.owner = owner;
        this.group = group;
        this.instance_id = instance_id;
        this.exists = false;
    }
    public RequestSign(InteractableSign sign)
    {
        if (sign == default) throw new ArgumentNullException(nameof(sign));
        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
        if (drop != null)
        {
            this.sign_id = drop.GetServersideData().barricade.asset.GUID;
            this.instance_id = drop.instanceID;
            this.transform = new SerializableTransform(sign.transform);
            this.barricadetransform = sign.transform;
            this.SignText = sign.text;
            this.group = sign.group.m_SteamID;
            this.owner = sign.owner.m_SteamID;
        }
        else throw new ArgumentNullException(nameof(sign));
    }
    public RequestSign()
    {
        this.kit_name = "default";
        this.transform = SerializableTransform.Zero;
        this.sign_id = Guid.Empty;
        this.owner = 0;
        this.group = 0;
        this.instance_id = 0;
        this.barricadetransform = default;
        this.exists = false;
    }
    public void InvokeUpdate(SteamPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (barricadetransform != null)
        {
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(barricadetransform);
            if (drop != null && drop.model.TryGetComponent(out InteractableSign sign))
            {
                F.InvokeSignUpdateFor(player, sign, SignText);
            }
            else L.LogError("Failed to find barricade from saved transform!");
        }
        else
        {
            SDG.Unturned.BarricadeData? data = UCBarricadeManager.GetBarricadeFromInstID(instance_id, out BarricadeDrop? drop);
            if (data != null && drop != null)
            {
                BarricadeDrop drop2 = BarricadeManager.FindBarricadeByRootTransform(drop.model.transform);
                if (drop2 != null && drop2.model.TryGetComponent(out InteractableSign sign))
                    F.InvokeSignUpdateFor(player, sign, true, SignText);
                else L.LogError("Failed to find barricade after respawning again!");
            }
            else L.LogError("Failed to find barricade after respawn!");
        }
    }
    public void InvokeUpdate()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (barricadetransform != null)
        {
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(barricadetransform);
            if (drop != null && drop.model.TryGetComponent(out InteractableSign sign) && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
            {
                F.InvokeSignUpdateForAll(sign, x, y, SignText);
            }
            else L.LogError("Failed to find barricade from saved transform!");
        }
        else
        {
            SDG.Unturned.BarricadeData? data = UCBarricadeManager.GetBarricadeFromInstID(instance_id, out BarricadeDrop? drop);
            if (data != null && drop != null)
            {
                BarricadeDrop drop2 = BarricadeManager.FindBarricadeByRootTransform(drop.model.transform);
                if (drop2 != null && drop2.model.TryGetComponent(out InteractableSign sign) && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
                    F.InvokeSignUpdateForAll(sign, x, y, SignText);
                else L.LogError("Failed to find barricade after respawning again!");
            }
            else L.LogError("Failed to find barricade after respawn!");
        }
    }
    /// <summary>Spawns the sign if it is not already placed.</summary>
    public void SpawnCheck(bool save)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SDG.Unturned.BarricadeData? data = UCBarricadeManager.GetBarricadeFromInstID(instance_id, out BarricadeDrop? drop);
        if (drop == null || data == null)
        {
            if (Assets.find(sign_id) is not ItemBarricadeAsset asset)
            {
                L.LogError("Failed to find barricade with " + sign_id.ToString("N"));
                return;
            }
            this.barricadetransform = BarricadeManager.dropNonPlantedBarricade(
                new Barricade(asset),
                transform.position.Vector3, transform.Rotation, owner, group
                );
            if (barricadetransform == null)
            {
                exists = false;
                L.LogWarning("Failed to spawn request sign for " + kit_name);
                return;
            }
            drop = BarricadeManager.FindBarricadeByRootTransform(barricadetransform);
            if (drop != null)
            {
                L.Log("Replaced lost request sign for " + kit_name, ConsoleColor.Gray);
                instance_id = drop.instanceID;
                exists = true;
                InvokeUpdate();
                if (save) RequestSigns.SaveSingleton();
            }
            else
            {
                exists = false;
                L.LogWarning("Failed to find newly spawned request sign for " + kit_name);
            }
        }
        else
        {
            exists = true;
            this.barricadetransform = drop.model.transform;
            this.transform = new SerializableTransform(barricadetransform);
            if (save) RequestSigns.SaveSingleton();
            InvokeUpdate();
        }
        if (exists && barricadetransform != null && barricadetransform.TryGetComponent(out InteractableSign sign))
        {
            if (sign.text != SignText)
            {
                RequestSigns.SetSignTextSneaky(sign, SignText);
                sign.updateText(SignText);
            }
        }
    }
    public static void WriteRequestSign(RequestSign obj, Utf8JsonWriter writer) => obj.WriteJson(writer);
    public void WriteJson(Utf8JsonWriter writer)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        writer.WriteProperty(nameof(kit_name), kit_name);
        writer.WriteProperty(nameof(transform), transform);
        writer.WriteProperty(nameof(sign_id), sign_id);
        writer.WriteProperty(nameof(owner), owner);
        writer.WriteProperty(nameof(group), group);
        writer.WriteProperty(nameof(instance_id), instance_id);
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string val = reader.GetString()!;
                if (reader.Read())
                {
                    switch (val)
                    {
                        case nameof(kit_name):
                            kit_name = reader.GetString()!;
                            break;
                        case nameof(transform):
                            if (reader.TokenType == JsonTokenType.StartObject)
                                transform.ReadJson(ref reader);
                            break;
                        case nameof(sign_id):
                            sign_id = reader.GetGuid();
                            break;
                        case nameof(owner):
                            owner = reader.GetUInt64();
                            break;
                        case nameof(group):
                            group = reader.GetUInt64();
                            break;
                        case nameof(instance_id):
                            instance_id = reader.GetUInt32();
                            break;
                    }
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
                break;
        }
    }

    public override string ToString() =>
        $"Request sign: " + kit_name + ", Instance ID: " + instance_id + ", Placed by: " + owner + " guid: " + sign_id.ToString("N");
    public static RequestSign ReadRequestSign(ref Utf8JsonReader reader)
    {
        RequestSign rs = new RequestSign();
        rs.ReadJson(ref reader);
        return rs;
    }
}
