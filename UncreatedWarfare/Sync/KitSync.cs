using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.SQL;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Sync;
public static class KitSync
{
    private static readonly List<int> PendingKits;
    private static readonly List<int> PendingKitDeletions;
    private static readonly List<ulong> PendingAccessChanges;
    private static int _deleting = -1;
    private static int _updating = -1;
    private static int _version;
    static KitSync()
    {
        PendingKits = new List<int>(8);
        PendingKitDeletions = new List<int>(2);
        PendingAccessChanges = new List<ulong>(64);
        UCWarfare.RunOnMainThread(ReadPendings);
    }
    private static void ReadPendings()
    {
        ThreadUtil.assertIsGameThread();
        if (File.Exists(Data.Paths.KitSync))
        {
            using FileStream stream = new FileStream(Data.Paths.KitSync, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length < 2) return;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? prop = reader.GetString();
                    if (reader.Read() && prop != null)
                    {
                        if (prop.Equals("kit_updates", StringComparison.OrdinalIgnoreCase))
                        {
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                                {
                                    if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int pk))
                                    {
                                        if (!PendingKits.Contains(pk))
                                            PendingKits.Add(pk);
                                    }
                                }
                            }
                        }
                        else if (prop.Equals("kit_deletions", StringComparison.OrdinalIgnoreCase))
                        {
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                                {
                                    if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int pk))
                                    {
                                        if (!PendingKitDeletions.Contains(pk))
                                            PendingKitDeletions.Add(pk);
                                    }
                                }
                            }
                        }
                        else if (prop.Equals("kit_access_changes", StringComparison.OrdinalIgnoreCase))
                        {
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                                {
                                    if (reader.TokenType == JsonTokenType.Number)
                                    {
                                        if (reader.TryGetUInt64(out ulong id) && !PendingAccessChanges.Contains(id))
                                        {
                                            PendingAccessChanges.Add(id);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        else SavePendings();
    }
    private static void SavePendings()
    {
        ThreadUtil.assertIsGameThread();
        using FileStream stream = new FileStream(Data.Paths.KitSync, FileMode.Create, FileAccess.Write, FileShare.Read);
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.condensedWriterOptions);
        writer.WriteStartObject();
        writer.WritePropertyName("kit_updates");

        writer.WriteStartArray();
        for (int i = 0; i < PendingKits.Count; ++i)
            writer.WriteNumberValue(PendingKits[i]);
        writer.WriteEndArray();

        writer.WritePropertyName("kit_deletions");

        writer.WriteStartArray();
        for (int i = 0; i < PendingKitDeletions.Count; ++i)
            writer.WriteNumberValue(PendingKitDeletions[i]);
        writer.WriteEndArray();

        writer.WritePropertyName("kit_access_changes");

        writer.WriteStartArray();
        for (int i = 0; i < PendingAccessChanges.Count; ++i)
            writer.WriteNumberValue(PendingAccessChanges[i]);
        writer.WriteEndArray();

        writer.Flush();
    }
    public static void OnKitUpdated(SqlItem<Kit> kit)
    {
        if (_updating == kit.LastPrimaryKey.Key)
            return;
        PendingKits.Add(kit.LastPrimaryKey);
        if (!UCWarfare.CanUseNetCall)
            return;
        Task.Run(async () =>
        {
            if (!UCWarfare.CanUseNetCall)
                return;
            RequestResponse response = await NetCalls.MulticastKitUpdated.RequestAck(UCWarfare.I.NetClient!, kit.LastPrimaryKey);
            if (response.Responded)
            {
                PendingKits.Remove(kit.LastPrimaryKey);
                await UCWarfare.ToUpdate();
                SavePendings();
            }
            else
                L.LogWarning("Failed to send kit update to homebase.");
        });
    }
    public static void OnKitDeleted(PrimaryKey kit)
    {
        if (_deleting == kit.Key)
            return;
        PendingKitDeletions.Add(kit);
        if (!UCWarfare.CanUseNetCall)
            return;
        Task.Run(async () =>
        {
            if (!UCWarfare.CanUseNetCall)
                return;
            RequestResponse response = await NetCalls.MulticastKitDeleted.RequestAck(UCWarfare.I.NetClient!, kit);
            if (response.Responded)
            {
                PendingKitDeletions.Remove(kit);
                await UCWarfare.ToUpdate();
                SavePendings();
            }
            else
                L.LogWarning("Failed to send kit deletion to homebase.");
        });
    }
    public static void OnAccessChanged(ulong steamid)
    {
        PendingAccessChanges.Add(steamid);
        if (!UCWarfare.CanUseNetCall)
            return;
        Task.Run(async () =>
        {
            if (!UCWarfare.CanUseNetCall)
                return;
            RequestResponse response = await NetCalls.MulticastKitAccessChanged.RequestAck(UCWarfare.I.NetClient!, steamid);
            if (response.Responded)
            {
                PendingAccessChanges.Remove(steamid);
                await UCWarfare.ToUpdate();
                SavePendings();
            }
            else
                L.LogWarning("Failed to send kit access change to homebase.");
        });
    }
    public static void OnConnected()
    {
        int v = ++_version;
        Task.Run(async () =>
        {
            PendingKits.RemoveAll(x => PendingKitDeletions.Contains(x));
            foreach (int update in PendingKits.ToList())
            {
                if (!UCWarfare.CanUseNetCall || v != _version)
                    return;
                RequestResponse response = await NetCalls.MulticastKitUpdated.RequestAck(UCWarfare.I.NetClient!, update);
                if (response.Responded)
                {
                    PendingKits.Remove(update);
                    await UCWarfare.ToUpdate();
                    SavePendings();
                }
                else
                    L.LogWarning("Failed to send kit update to homebase.");
            }
            foreach (int delete in PendingKitDeletions.ToList())
            {
                if (!UCWarfare.CanUseNetCall || v != _version)
                    return;
                RequestResponse response = await NetCalls.MulticastKitDeleted.RequestAck(UCWarfare.I.NetClient!, delete);
                if (response.Responded)
                {
                    PendingKitDeletions.Remove(delete);
                    await UCWarfare.ToUpdate();
                    SavePendings();
                }
                else
                    L.LogWarning("Failed to send kit deletion to homebase.");
            }
            foreach (ulong access in PendingAccessChanges.ToList())
            {
                if (!UCWarfare.CanUseNetCall || v != _version)
                    return;
                RequestResponse response = await NetCalls.MulticastKitAccessChanged.RequestAck(UCWarfare.I.NetClient!, access);
                if (response.Responded)
                {
                    PendingAccessChanges.Remove(access);
                    await UCWarfare.ToUpdate();
                    SavePendings();
                }
                else
                    L.LogWarning("Failed to send kit access change to homebase.");
            }
        });
    }
    public static class NetCalls
    {
        private static UCSemaphore _semaphore;
        public static readonly NetCall<int> MulticastKitUpdated = new NetCall<int>(OnForeignKitUpdated);
        public static readonly NetCall<int> MulticastKitDeleted = new NetCall<int>(OnForeignKitDeleted);
        public static readonly NetCall<ulong> MulticastKitAccessChanged = new NetCall<ulong>(OnForeignAccessUpdated);
        private static UCSemaphore Semaphore
        {
            get
            {
                if (_semaphore != null)
                    return _semaphore;
                _semaphore = new UCSemaphore();
#if DEBUG
                _semaphore.WaitCallback += SemaphoreWaitCallback;
                _semaphore.ReleaseCallback += SemaphoreReleaseCallback;
#endif
                return _semaphore;
            }
        }
#if DEBUG
        private static void SemaphoreWaitCallback()
        {
            L.LogDebug("Waiting in KitSync semaphore.");
        }
        private static void SemaphoreReleaseCallback(int ct)
        {
            L.LogDebug("Released " + ct.ToString(Data.AdminLocale) + " in KitSync semaphore.");
        }
#endif

        [NetCall(ENetCall.FROM_SERVER, 3008)]
        private static async Task OnForeignKitUpdated(MessageContext ctx, int pk)
        {
            try
            {
                await Semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    KitManager? manager = KitManager.GetSingletonQuick();
                    if (manager == null) return;
                    _updating = pk;
                    SqlItem<Kit>? kit = await manager.Download(pk);
                    L.Log("Received update notification for kit: \"" + (kit?.Item == null ? "<null>" : kit.Item.Id) + "\" " + pk
                          + " and redownloaded it from admin database.");
                    ctx.Acknowledge();
                }
                finally
                {
                    _updating = -1;
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                L.LogError("Error saving kit change for \"" + pk + "\".");
                L.LogError(ex);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 3006)]
        private static async Task OnForeignKitDeleted(MessageContext ctx, int pk)
        {
            try
            {
                await Semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    KitManager? manager = KitManager.GetSingletonQuick();
                    if (manager == null) return;
                    _deleting = pk;
                    SqlItem<Kit>? kit = await manager.FindProxy(pk).ConfigureAwait(false);
                    if (kit is not null)
                        await kit.Delete().ConfigureAwait(false);
                    L.Log("Received delete notification for kit: \"" + pk + "\" and removed it from the cache.", ConsoleColor.DarkGray);
                    ctx.Acknowledge();
                }
                finally
                {
                    _deleting = -1;
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                L.LogError("Error deleting kit \"" + pk + "\".");
                L.LogError(ex);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 3007)]
        private static async Task OnForeignAccessUpdated(MessageContext ctx, ulong steamId)
        {
            try
            {
                UCPlayer? player = UCPlayer.FromID(steamId);
                if (player == null)
                    return;
                await KitManager.DownloadPlayerKitData(player, true);
                L.Log("Received access update notification for player: \"" + player.Name.PlayerName + "\" and redownloaded their kits.", ConsoleColor.DarkGray);
                ctx.Acknowledge();
            }
            catch (Exception ex)
            {
                L.LogError("Error updating access for {" + steamId + "}.");
                L.LogError(ex);
            }
        }
    }
}
