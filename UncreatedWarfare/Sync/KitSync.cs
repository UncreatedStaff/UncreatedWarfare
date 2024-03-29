﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.SQL;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;

namespace Uncreated.Warfare.Sync;
public static class KitSync
{
    private static readonly List<uint> PendingKits;
    private static readonly List<uint> PendingKitDeletions;
    private static readonly List<ulong> PendingAccessChanges;
    private static readonly UCSemaphore Semaphore = new UCSemaphore();
    private static volatile uint _deleting = uint.MaxValue;
    private static volatile uint _updating = uint.MaxValue;
    private static int _version;
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
    static KitSync()
    {
        PendingKits = new List<uint>(8);
        PendingKitDeletions = new List<uint>(2);
        PendingAccessChanges = new List<ulong>(64);
#if DEBUG
        Semaphore.WaitCallback += SemaphoreWaitCallback;
        Semaphore.ReleaseCallback += SemaphoreReleaseCallback;
#endif
    }
    internal static async Task Init()
    {
        await Semaphore.WaitAsync();
        try
        {
            await UCWarfare.ToUpdate();
            ReadPendings();
        }
        finally
        {
            Semaphore.Release();
        }
    }
    private static void ReadPendings()
    {
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
                                    if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt32(out uint pk))
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
                                    if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt32(out uint pk))
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
        string? dir = Path.GetDirectoryName(Data.Paths.KitSync);
        if (dir != null)
            Directory.CreateDirectory(dir);
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

        writer.WriteEndObject();

        writer.Flush();
    }
    public static void OnKitUpdated(Kit kit)
    {
        if (_updating == kit.PrimaryKey)
            return;
        Task.Run(async () =>
        {
            await Semaphore.WaitAsync();
            try
            {
                PendingKits.Add(kit.PrimaryKey);
                if (!UCWarfare.CanUseNetCall)
                    return;
                RequestResponse response = await NetCalls.MulticastKitUpdated.RequestAck(UCWarfare.I.NetClient!, kit.PrimaryKey);
                if (response.Responded)
                    PendingKits.Remove(kit.PrimaryKey);
                else
                    L.LogWarning("Failed to send kit update to homebase.");
                SavePendings();
            }
            finally
            {
                Semaphore.Release();
            }
        });
    }
    public static void OnKitDeleted(PrimaryKey kit)
    {
        if (_deleting == kit.Key)
            return;
        if (!UCWarfare.CanUseNetCall)
            return;
        Task.Run(async () =>
        {
            await Semaphore.WaitAsync();
            try
            {
                PendingKitDeletions.Add(kit);
                if (!UCWarfare.CanUseNetCall)
                    return;
                RequestResponse response = await NetCalls.MulticastKitDeleted.RequestAck(UCWarfare.I.NetClient!, kit);
                if (response.Responded)
                    PendingKitDeletions.Remove(kit);
                else
                    L.LogWarning("Failed to send kit deletion to homebase.");
                SavePendings();
            }
            finally
            {
                Semaphore.Release();
            }
        });
    }
    public static void OnAccessChanged(ulong steamid)
    {
        if (!UCWarfare.CanUseNetCall)
            return;
        Task.Run(async () =>
        {
            await Semaphore.WaitAsync();
            try
            {
                PendingAccessChanges.Add(steamid);
                if (!UCWarfare.CanUseNetCall)
                    return;
                RequestResponse response = await NetCalls.MulticastKitAccessChanged.RequestAck(UCWarfare.I.NetClient!, steamid);
                if (response.Responded)
                    PendingAccessChanges.Remove(steamid);
                else
                    L.LogWarning("Failed to send kit access change to homebase.");
                await UCWarfare.ToUpdate();
                SavePendings();
            }
            finally
            {
                Semaphore.Release();
            }
        });
    }
    public static void OnConnected()
    {
        int v = ++_version;
        Task.Run(async () =>
        {
            await Semaphore.WaitAsync();
            try
            {
                PendingKits.RemoveAll(x => PendingKitDeletions.Contains(x));
                foreach (uint update in PendingKits)
                {
                    if (!UCWarfare.CanUseNetCall || v != _version)
                        return;
                    RequestResponse response = await NetCalls.MulticastKitUpdated.RequestAck(UCWarfare.I.NetClient!, update);
                    if (response.Responded)
                    {
                        PendingKits.Remove(update);
                        SavePendings();
                    }
                    else
                        L.LogWarning("Failed to send kit update to homebase.");
                }
                foreach (uint delete in PendingKitDeletions)
                {
                    if (!UCWarfare.CanUseNetCall || v != _version)
                        return;
                    RequestResponse response = await NetCalls.MulticastKitDeleted.RequestAck(UCWarfare.I.NetClient!, delete);
                    if (response.Responded)
                    {
                        PendingKitDeletions.Remove(delete);
                        SavePendings();
                    }
                    else
                        L.LogWarning("Failed to send kit deletion to homebase.");
                }
                foreach (ulong access in PendingAccessChanges)
                {
                    if (!UCWarfare.CanUseNetCall || v != _version)
                        return;
                    RequestResponse response = await NetCalls.MulticastKitAccessChanged.RequestAck(UCWarfare.I.NetClient!, access);
                    if (response.Responded)
                    {
                        PendingAccessChanges.Remove(access);
                        SavePendings();
                    }
                    else
                        L.LogWarning("Failed to send kit access change to homebase.");
                }
            }
            finally
            {
                Semaphore.Release();
            }
        });
    }
    public static class NetCalls
    {
        public static readonly NetCall<uint> MulticastKitUpdated = new NetCall<uint>(OnForeignKitUpdated);
        public static readonly NetCall<uint> MulticastKitDeleted = new NetCall<uint>(OnForeignKitDeleted);
        public static readonly NetCall<ulong> MulticastKitAccessChanged = new NetCall<ulong>(OnForeignAccessUpdated);

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.MulticastKitUpdated)]
        private static async Task OnForeignKitUpdated(MessageContext ctx, uint pk)
        {
            try
            {
                await Semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    KitManager? manager = KitManager.GetSingletonQuick();
                    if (manager == null) return;
                    _updating = pk;
                    Kit? kit = await manager.Cache.ReloadCache(_updating, UCWarfare.UnloadCancel);
                    L.Log("Received update notification for kit: \"" + (kit == null ? "<null>" : kit.InternalName) + "\" " + pk
                          + " and redownloaded it from admin database.");
                    ctx.Acknowledge();
                }
                finally
                {
                    _updating = uint.MaxValue;
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                L.LogError("Error saving kit change for \"" + pk + "\".");
                L.LogError(ex);
            }
        }
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.MulticastKitDeleted)]
        private static async Task OnForeignKitDeleted(MessageContext ctx, uint pk)
        {
            try
            {
                await Semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    KitManager? manager = KitManager.GetSingletonQuick();
                    if (manager == null) return;
                    _deleting = pk;
                    Kit? kit = await manager.Cache.ReloadCache(_deleting, UCWarfare.UnloadCancel);
                    L.Log("Received delete notification for kit: \"" + (kit == null ? "<null>" : kit.InternalName) + "\" " + pk
                          + " and removed it from the cache.", ConsoleColor.DarkGray);
                    ctx.Acknowledge();
                }
                finally
                {
                    _deleting = uint.MaxValue;
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                L.LogError("Error deleting kit \"" + pk + "\".");
                L.LogError(ex);
            }
        }
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.MulticastKitAccessChanged)]
        private static async Task OnForeignAccessUpdated(MessageContext ctx, ulong steamId)
        {
            try
            {
                KitManager? manager = KitManager.GetSingletonQuick();
                if (manager == null)
                    return;
                UCPlayer? player = UCPlayer.FromID(steamId);
                if (player == null)
                    return;
                await manager.DownloadPlayerKitData(player, true);
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
