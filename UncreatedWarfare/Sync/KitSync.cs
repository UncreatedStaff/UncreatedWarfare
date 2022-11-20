using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Json;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Sync;
public static class KitSync
{
    private static readonly List<string> pendingKits;
    private static readonly List<string> pendingKitDeletions;
    private static readonly List<ulong> pendingAccessChanges;
    static KitSync()
    {
        pendingKits = new List<string>(8);
        pendingKitDeletions = new List<string>(2);
        pendingAccessChanges = new List<ulong>(64);
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
                                    if (reader.TokenType == JsonTokenType.String)
                                    {
                                        string id = reader.GetString()!;
                                        if (!pendingKits.Contains(id))
                                            pendingKits.Add(id);
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
                                    if (reader.TokenType == JsonTokenType.String)
                                    {
                                        string id = reader.GetString()!;
                                        if (!pendingKitDeletions.Contains(id))
                                            pendingKitDeletions.Add(id);
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
                                        if (reader.TryGetUInt64(out ulong id) && !pendingAccessChanges.Contains(id))
                                        {
                                            pendingAccessChanges.Add(id);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private static void SavePendings()
    {
        using FileStream stream = new FileStream(Data.Paths.KitSync, FileMode.Create, FileAccess.Write, FileShare.Read);
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.condensedWriterOptions);
        writer.WriteStartObject();
        writer.WritePropertyName("kit_updates");

        writer.WriteStartArray();
        for (int i = 0; i < pendingKits.Count; ++i)
            writer.WriteStringValue(pendingKits[i]);
        writer.WriteEndArray();

        writer.WritePropertyName("kit_deletions");

        writer.WriteStartArray();
        for (int i = 0; i < pendingKitDeletions.Count; ++i)
            writer.WriteStringValue(pendingKitDeletions[i]);
        writer.WriteEndArray();

        writer.WritePropertyName("kit_access_changes");

        writer.WriteStartArray();
        for (int i = 0; i < pendingAccessChanges.Count; ++i)
            writer.WriteNumberValue(pendingAccessChanges[i]);
        writer.WriteEndArray();
    }
    public static void OnKitUpdated(string kit)
    {
        pendingKits.Add(kit);
        if (!UCWarfare.CanUseNetCall)
            return;
        Task.Run(async () =>
        {
            if (!UCWarfare.CanUseNetCall)
                return;
            RequestResponse response = await NetCalls.MulticastKitUpdated.RequestAck(UCWarfare.I.NetClient!, kit);
            if (response.Responded)
                pendingKits.Remove(kit);
            else
            {
                L.LogWarning("Failed to send kit update to homebase.");
                SavePendings();
            }
        });
    }
    public static void OnKitDeleted(string name)
    {
        pendingKitDeletions.Add(name);
        if (!UCWarfare.CanUseNetCall)
            return;
        Task.Run(async () =>
        {
            if (!UCWarfare.CanUseNetCall)
                return;
            RequestResponse response = await NetCalls.MulticastKitDeleted.RequestAck(UCWarfare.I.NetClient!, name);
            if (response.Responded)
                pendingKitDeletions.Remove(name);
            else
            {
                L.LogWarning("Failed to send kit deletion to homebase.");
                SavePendings();
            }
        });
    }
    public static void OnAccessChanged(ulong steamid)
    {
        pendingAccessChanges.Add(steamid);
        if (!UCWarfare.CanUseNetCall)
            return;
        Task.Run(async () =>
        {
            if (!UCWarfare.CanUseNetCall)
                return;
            RequestResponse response = await NetCalls.MulticastKitAccessChanged.RequestAck(UCWarfare.I.NetClient!, steamid);
            if (response.Responded)
                pendingAccessChanges.Remove(steamid);
            else
            {
                L.LogWarning("Failed to send kit access change to homebase.");
                SavePendings();
            }
        });
    }
    public static class NetCalls
    {
        public static readonly NetCall<string> MulticastKitUpdated = new NetCall<string>(OnForeignKitUpdated!, Kit.CAPACITY);
        public static readonly NetCall<string> MulticastKitDeleted = new NetCall<string>(OnForeignKitDeleted);
        public static readonly NetCall<ulong> MulticastKitAccessChanged = new NetCall<ulong>(OnForeignAccessUpdated);


        [NetCall(ENetCall.FROM_SERVER, 3001)]
        private static async Task OnForeignKitUpdated(MessageContext ctx, string kit)
        {
            try
            {
                KitManager singleton = KitManager.GetSingleton();
                await singleton.RedownloadKit(kit);
                L.Log("Received update notification for kit: \"" + kit + "\" and redownloaded it from admin database.");
                ctx.Acknowledge();
            }
            catch (Exception ex)
            {
                L.LogError("Error saving kit change for \"" + kit + "\".");
                L.LogError(ex);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 3007)]
        private static async Task OnForeignAccessUpdated(MessageContext ctx, ulong steamId)
        {
            UCPlayer? player = UCPlayer.FromID(steamId);
            if (player == null)
                return;
            try
            {
                await player.DownloadKits(true);
                L.Log("Received access update notification for player: \"" + player.Name.PlayerName + "\" and redownloaded their kits.");
                ctx.Acknowledge();
            }
            catch (Exception ex)
            {
                L.LogError("Error updating access for \"" + player.Name.PlayerName + "\".");
                L.LogError(ex);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 3006)]
        private static async Task OnForeignKitDeleted(MessageContext ctx, string kitName)
        {
            try
            {
                await KitManager.DeleteKit(kitName);
                L.Log("Received delete notification for kit: \"" + kitName + "\" and removed it from the cache.");
                ctx.Acknowledge();
            }
            catch (Exception ex)
            {
                L.LogError("Error deleting kit \"" + kitName + "\".");
                L.LogError(ex);
            }
        }
    }
}
