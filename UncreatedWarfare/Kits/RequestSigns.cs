using SDG.Unturned;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Kits;

[SingletonDependency(typeof(KitManager))]
public class RequestSigns : ListSingleton<RequestSign>
{
    public static RequestSigns Singleton;
    public static bool Loaded => Singleton.IsLoaded<RequestSigns, RequestSign>();
    public RequestSigns() : base("kitsigns", Path.Combine(Data.Paths.StructureStorage, "request_signs.json")) { }
    protected override string LoadDefaults() => EMPTY_LIST;
    public override void Load()
    {
        Singleton = this;
        EventDispatcher.OnGroupChanged += OnGroupChanged;
    }
    public override void Unload()
    {
        EventDispatcher.OnGroupChanged -= OnGroupChanged;
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
            if (!sign.Exists)
                L.LogError("Failed to spawn sign " + sign.KitName);
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
        if (drop != null && !Singleton.ObjectExists(x => x.InstanceId == drop.instanceID, out signadded))
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
        Singleton.RemoveWhere(x => x.InstanceId == sign.InstanceId);
        //await sign.InvokeUpdate();
    }
    public static void RemoveRequestSigns(string kitname)
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
        for (int i = Singleton.Count - 1; i >= 0; ++i)
        {
            Singleton.RemoveAt(i);
            if (Singleton[i].BarricadeTransform != default)
            {
                BarricadeDrop bd = BarricadeManager.FindBarricadeByRootTransform(Singleton[i].BarricadeTransform);
                Signs.BroadcastSignUpdate(bd);
            }
        }
        Singleton.RemoveWhere(x => x.KitName.Equals(kitname, StringComparison.OrdinalIgnoreCase));
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
            if (drop != null && drop.instanceID == Singleton[i].InstanceId)
            {
                found = Singleton[i];
                return true;
            }
        }
        found = null!;
        return false;
    }
    private void OnGroupChanged(GroupChanged e)
    {
        UpdateAllSigns(e.Player);
    }
    public static bool SignExists(uint instance_id, out RequestSign found)
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
        return Singleton.ObjectExists(s => s != default && s.InstanceId == instance_id, out found);
    }
    public static bool SignExists(string kitName, out RequestSign sign)
    {
        Singleton.AssertLoaded<RequestSigns, RequestSign>();
        return Singleton.ObjectExists(x => x.KitName.Equals(kitName, StringComparison.OrdinalIgnoreCase), out sign);
    }
    public static void UpdateAllSigns(UCPlayer? player = null)
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
    public static byte[] SetSignTextSneaky(InteractableSign sign, string text)
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
        BarricadeManager.updateState(barricadeByRootFast.model, numArray1, numArray1.Length);
        sign.updateState(barricadeByRootFast.asset, numArray1);
        return numArray1;
    }
}
public class RequestSign
{
    [JsonIgnore]
    public string SignText => "sign_" + KitName;

    [JsonIgnore]
    public Transform? BarricadeTransform { get; private set; }

    [JsonPropertyName("kit_name")]
    public string KitName { get; set; }

    [JsonPropertyName("position")]
    public Vector3 Position { get; set; }

    [JsonPropertyName("rotation")]
    public Vector3 Rotation { get; set; }

    [JsonPropertyName("guid")]
    public Guid Guid { get; set; }

    [JsonPropertyName("owner")]
    public ulong OwnerId { get; set; }

    [JsonPropertyName("group")]
    public ulong GroupId { get; set; }

    [JsonPropertyName("instance_id")]
    public uint InstanceId { get; set; }

    [JsonIgnore]
    public bool Exists { get; private set; }
    public RequestSign(InteractableSign sign)
    {
        if (sign == default) throw new ArgumentNullException(nameof(sign));
        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
        if (drop != null)
        {
            this.Guid = drop.GetServersideData().barricade.asset.GUID;
            this.InstanceId = drop.instanceID;
            this.Position = sign.transform.position;
            this.Rotation = sign.transform.rotation.eulerAngles;
            this.BarricadeTransform = sign.transform;
            if (string.IsNullOrEmpty(sign.text)) KitName = TeamManager.DefaultKit;
            else if (sign.text.Length > 5 && sign.text.StartsWith("sign_", StringComparison.OrdinalIgnoreCase))
                KitName = sign.text.Substring(5);
            else KitName = sign.text;
            this.GroupId = sign.group.m_SteamID;
            this.OwnerId = sign.owner.m_SteamID;
        }
        else throw new ArgumentNullException(nameof(sign));
    }
    public RequestSign()
    {
        this.KitName = TeamManager.DefaultKit;
        this.Guid = Guid.Empty;
        this.OwnerId = 0;
        this.GroupId = 0;
        this.InstanceId = 0;
        this.BarricadeTransform = default;
        this.Exists = false;
    }
    public void InvokeUpdate(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (BarricadeTransform != null)
        {
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(BarricadeTransform);
            if (drop != null)
            {
                Signs.SendSignUpdate(drop, player);
            }
            else L.LogError("Failed to find barricade from saved transform!");
        }
        else
        {
            UCBarricadeManager.GetBarricadeFromInstID(InstanceId, out BarricadeDrop? drop);
            if (drop != null)
            {
                BarricadeTransform = drop.model;
                Signs.SendSignUpdate(drop, player);
            }
            else
            {
                drop = BarricadeManager.FindBarricadeByRootTransform(BarricadeTransform);
                if (drop == null)
                    L.LogError("Failed to find barricade after respawn!");
                else
                {
                    InstanceId = drop.instanceID;
                    Signs.SendSignUpdate(drop, player);
                }
            }
        }
    }
    public void InvokeUpdate()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (BarricadeTransform != null)
        {
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(BarricadeTransform);
            if (drop != null)
            {
                Signs.BroadcastSignUpdate(drop);
            }
            else L.LogError("Failed to find barricade from saved transform!");
        }
        else
        {
            UCBarricadeManager.GetBarricadeFromInstID(InstanceId, out BarricadeDrop? drop);
            if (drop != null)
            {
                BarricadeTransform = drop.model;
                Signs.BroadcastSignUpdate(drop);
            }
            else
            {
                drop = BarricadeManager.FindBarricadeByRootTransform(BarricadeTransform);
                if (drop == null)
                    L.LogError("Failed to find barricade after respawn!");
                else
                {
                    InstanceId = drop.instanceID;
                    Signs.BroadcastSignUpdate(drop);
                }
            }
        }
    }
    /// <summary>Spawns the sign if it is not already placed.</summary>
    public void SpawnCheck(bool save)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCBarricadeManager.GetBarricadeFromInstID(InstanceId, out BarricadeDrop? drop);
        bool needsSave = false;
        if (drop == null)
        {
            if (Assets.find(Guid) is not ItemBarricadeAsset asset)
            {
                L.LogError("Failed to find barricade with " + Guid.ToString("N"));
                return;
            }

            drop = UCBarricadeManager.GetBarricadeFromPosition(Position);
            if (drop == null || drop.asset.GUID != Guid)
            {
                BarricadeTransform = BarricadeManager.dropNonPlantedBarricade(
                    new Barricade(asset),
                    Position, Quaternion.Euler(Rotation), OwnerId, GroupId);
                if (BarricadeTransform != null)
                    drop = BarricadeManager.FindBarricadeByRootTransform(BarricadeTransform);
                if (drop == null)
                {
                    Exists = false;
                    L.LogWarning("Failed to spawn request sign for " + KitName);
                    return;
                }
            }
            InstanceId = drop.instanceID;
            Exists = true;
            needsSave = true;
        }
        Exists = true;
        BarricadeTransform = drop.model;
        Position = BarricadeTransform.position;
        Rotation = BarricadeTransform.rotation.eulerAngles;
        if (drop.interactable is InteractableSign sign)
        {
            string text = "sign_" + KitName;
            sign.updateText(text);
            if (text.Length > 128) text = text.Substring(0, 128);
            byte[] unicode = System.Text.Encoding.UTF8.GetBytes(text);
            byte[] state = new byte[sizeof(ulong) * 2 + 1 + unicode.Length];
            state[sizeof(ulong) * 2] = (byte)unicode.Length;
            Buffer.BlockCopy(unicode, 0, state, sizeof(ulong) * 2 + 1, unicode.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(OwnerId), 0, state, 0, sizeof(ulong));
            Buffer.BlockCopy(BitConverter.GetBytes(GroupId), 0, state, sizeof(ulong), sizeof(ulong));
            BarricadeManager.updateReplicatedState(drop.model, state, state.Length);
            BarricadeManager.changeOwnerAndGroup(drop.model, OwnerId, GroupId);
        }
        if (save && needsSave)
            RequestSigns.SaveSingleton();
        InvokeUpdate();
    }

    public override string ToString() =>
        $"Request sign: " + KitName + ", Instance ID: " + InstanceId + ", Placed by: " + OwnerId + ", Guid: " + Guid.ToString("N");
}
