using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.Players;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Color = UnityEngine.Color;
using SteamGameServerNetworkingUtils = SDG.Unturned.SteamGameServerNetworkingUtils;

namespace Uncreated.Warfare;

public static class OffenseManager
{
    private static readonly List<Ban> PendingBans = new List<Ban>(8);
    private static readonly List<Unban> PendingUnbans = new List<Unban>(8);
    private static readonly List<Kick> PendingKicks = new List<Kick>(8);
    private static readonly List<Warn> PendingWarnings = new List<Warn>(8);
    private static readonly List<Mute> PendingMutes = new List<Mute>(8);
    private static readonly List<BattlEyeKick> PendingBattlEyeKicks = new List<BattlEyeKick>(8);
    private static readonly List<Teamkill> PendingTeamkills = new List<Teamkill>(8);
    private static readonly List<VehicleTeamkill> PendingVehicleTeamkills = new List<VehicleTeamkill>(8);
    private static readonly List<Unmute> PendingUnmutes = new List<Unmute>(9);
    private static volatile int _version = 0;
    // calls the type initializer
    internal static void Init()
    {
        EventDispatcher.PlayerDied += OnPlayerDied;
        EventDispatcher.PlayerJoined += OnPlayerJoined;
        EventDispatcher.PlayerPendingAsync += OnPlayerPending;
        Load<Ban>(0);
        Load<Unban>(1);
        Load<Kick>(2);
        Load<Warn>(3);
        Load<Mute>(4);
        Load<BattlEyeKick>(5);
        Load<Teamkill>(6);
        Load<VehicleTeamkill>(7);
        Load<Unmute>(8);
    }
    internal static void Deinit()
    {
        EventDispatcher.PlayerPendingAsync -= OnPlayerPending;
        EventDispatcher.PlayerJoined -= OnPlayerJoined;
        EventDispatcher.PlayerDied -= OnPlayerDied;
        for (int i = 0; i < Pendings.Length; ++i)
        {
            IList l = Pendings[i];
            lock (l)
                l.Clear();
        }
    }
    private static async Task OnPlayerPending(PlayerPending e, CancellationToken token = default)
    {
        List<uint> packs = new List<uint>(4);
        await Data.DatabaseManager.QueryAsync("SELECT `Packed` FROM `ip_addresses` WHERE `Steam64` = @0;", new object[] { e.Steam64 }, reader =>
        {
            packs.Add(reader.GetUInt32(0));
        }, token);
        for (int i = 0; i < SteamBlacklist.list.Count; ++i)
        {
            uint ip = SteamBlacklist.list[i].ip;
            for (int j = 0; j < packs.Count; ++j)
            {
                if (ip != 0 && ip == packs[j])
                {
                    throw e.Reject("Your IP is banned for: " + SteamBlacklist.list[i].reason);
                }
            }
        }
    }
    private static void OnPlayerJoined(PlayerJoined e)
    {
        byte[][] bytes = (e.Player.SteamPlayer.playerID.GetHwids() as byte[][])!;
        int[] update = new int[bytes.Length];
        int updates = 0;
        ulong s64 = e.Steam64;
        uint packed = e.Player.SteamPlayer.transportConnection.TryGetIPv4Address(out uint ip) ? ip : 0u;
        Task.Run(async () =>
        {
            try
            {
                await Data.DatabaseManager.QueryAsync("SELECT `Id`, `Index`, `HWID` FROM `hwids` WHERE `Steam64` = @0 ORDER BY `LastLogin` DESC;",
                    new object[] { s64 },
                    R =>
                    {
                        int id = R.GetInt32(0);
                        byte[] buffer = new byte[20];
                        R.GetBytes(2, 0, buffer, 0, 20);
                        for (int i = 0; i < update.Length; ++i)
                        {
                            if (update[i] > 0) continue;
                            byte[] b = bytes[i];
                            if (b is null) continue;
                            for (int x = 0; x < 20; ++x)
                                if (b[x] != buffer[x])
                                    goto cont;
                            update[i] = id;
                            ++updates;
                            break;
                        cont:;
                        }
                    });

                uint? id = null;
                await Data.DatabaseManager.QueryAsync("SELECT `Id` FROM `ip_addresses` WHERE `Steam64` = @0 AND `Packed` = @1 LIMIT 1;", new object[] { s64, packed },
                    R => id = R.GetUInt32(0));
                StringBuilder sbq = new StringBuilder(64);
                StringBuilder sbq2 = new StringBuilder(64);
                int c = 1;
                for (int i = 0; i < update.Length; ++i)
                {
                    if (update[i] < 1 && bytes[i] is not null && bytes[i].Length == 20 && i < 8)
                        c += 2;
                }
                object[] objs = new object[c];
                objs[0] = s64;
                c = 0;
                int d = 0;
                object[] o2 = new object[update.Length];
                for (int i = 0; i < update.Length; ++i)
                {
                    if (update[i] > 0)
                    {
                        sbq2.Append("UPDATE `hwids` SET `LoginCount` = `LoginCount` + 1, `LastLogin` = NOW() WHERE `Id` = @" + i + ";");
                        ++d;
                        o2[i] = update[i];
                        continue;
                    }
                    if (bytes[i] is not null && bytes[i].Length == 20 && i < 8)
                    {
                        if (c != 0)
                            sbq.Append(',');
                        sbq.Append('(').Append("@0, @").Append(c * 2 + 1).Append(", @").Append(c * 2 + 2).Append(')');
                        objs[c * 2 + 1] = i;
                        objs[c * 2 + 2] = bytes[i];
                        ++c;
                    }

                    o2[i] = 0;
                }
                if (c > 0)
                {
                    sbq.Insert(0, "INSERT INTO `hwids` (`Steam64`, `Index`, `HWID`) VALUES ");
                    sbq.Append(';');
                    string query = sbq.ToString();

                    await Data.DatabaseManager.NonQueryAsync(query, objs);
                }
                if (d > 0)
                {
                    await Data.DatabaseManager.NonQueryAsync(sbq2.ToString(), o2);
                }
                if (id.HasValue)
                    await Data.DatabaseManager.NonQueryAsync(
                        "UPDATE `ip_addresses` SET `LoginCount` = `LoginCount` + 1, `LastLogin` = NOW() WHERE `Id` = @0 LIMIT 1;",
                        new object[] { id.Value });
                else
                    await Data.DatabaseManager.NonQueryAsync(
                        "INSERT INTO `ip_addresses` (`Steam64`, `Packed`, `Unpacked`) VALUES (@0, @1, @2);",
                        new object[] { s64, packed, Parser.getIPFromUInt32(packed) });
            }
            catch (Exception ex)
            {
                L.LogError("Error handling player connect: ");
                L.LogError(ex);
                await UCWarfare.ToUpdate();
                Provider.kick(e.Player.CSteamID, "There was a fatal error connecting you to the server.");
            }
        }).ConfigureAwait(false);
    }
    public static async Task<List<byte[]>> GetAllHWIDs(ulong s64, CancellationToken token = default)
    {
        List<byte[]> bytes = new List<byte[]>(8);
        await Data.DatabaseManager.QueryAsync("SELECT `Id`, `Index`, `HWID` FROM `hwids` WHERE `Steam64` = @0 ORDER BY `Index` ASC, `LastLogin` DESC;",
            new object[] { s64 },
            reader =>
            {
                byte[] buffer = new byte[20];
                reader.GetBytes(2, 0, buffer, 0, 20);
                bytes.Add(buffer);
            }, token).ConfigureAwait(false);
        return bytes;
    }
    public static async Task ApplyMuteSettings(UCPlayer joining, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (joining == null) return;
        string? reason = null;
        int duration = -2;
        DateTime timestamp = DateTime.MinValue;
        EMuteType type = EMuteType.NONE;
        await Data.DatabaseManager.QueryAsync(
            "SELECT `Reason`, `Duration`, `Timestamp`, `Type` FROM `muted` WHERE `Steam64` = @0 AND `Deactivated` = 0 AND " +
            "(`Duration` = -1 OR TIME_TO_SEC(TIMEDIFF(`Timestamp`, NOW())) * -1 < `Duration`) ORDER BY (TIME_TO_SEC(TIMEDIFF(`Timestamp`, NOW())) * -1) - `Duration` LIMIT 1;",
            new object[] { joining.Steam64 },
            reader =>
            {
                int dir = reader.GetInt32(1);
                DateTime ts = reader.GetDateTime(2);
                if (dir == -1 || dir > duration)
                {
                    duration = dir;
                    timestamp = ts;
                    reason = reader.GetString(0);
                }
                else if (reason is null)
                    reason = reader.GetString(0);
                type |= (EMuteType)reader.GetByte(3);
            }, token
        );
        if (type == EMuteType.NONE) return;
        DateTime unmutedTime = duration == -1 ? DateTime.MaxValue : timestamp + TimeSpan.FromSeconds(duration);
        joining.TimeUnmuted = unmutedTime;
        joining.MuteReason = reason;
        joining.MuteType = type;
    }
    public static bool IsValidSteam64Id(CSteamID id)
    {
        return id.m_SteamID / 100000000000000ul == 765;
    }
    private static string GetSavePath(int index) => Path.Combine(Data.Paths.PendingOffenses, index switch
    {
        0 => "bans",
        1 => "unbans",
        2 => "kicks",
        3 => "warnings",
        4 => "mutes",
        5 => "battleye_kicks",
        6 => "teamkills",
        7 => "vehicle_teamkills",
        8 => "unmute",
        _ => throw new ArgumentOutOfRangeException("#" + index.ToString(Data.AdminLocale) + " doesn't match a pending type.")
    } + ".json");

    private static readonly IList[] Pendings =
    {
        PendingBans,
        PendingUnbans,
        PendingKicks,
        PendingWarnings,
        PendingMutes,
        PendingBattlEyeKicks,
        PendingTeamkills,
        PendingVehicleTeamkills,
        PendingUnmutes
    };
    private static void Save<T>(int index) where T : ITimestampOffense
    {
        if (Pendings[index] is List<T> col)
        {
            F.CheckDir(Data.Paths.PendingOffenses, out bool success);
            if (success)
            {
                lock (col)
                {
                    string path = GetSavePath(index);
                    using FileStream str = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    JsonSerializer.Serialize(str, col, JsonEx.condensedSerializerSettings);
                }
            }
        }
    }
    private static void Load<T>(int index) where T : ITimestampOffense
    {
        if (Pendings[index] is List<T> col)
        {
            lock (col)
            {
                col.Clear();
                string path = GetSavePath(index);
                if (File.Exists(path))
                {
                    T[]? t;
                    using (FileStream str = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        t = JsonSerializer.Deserialize<T[]>(str, JsonEx.condensedSerializerSettings);
                    }
                    if (t is null) return;
                    col.AddRange(t);
                }
            }
        }
    }

    private static void RemoveFromSave<T>(T obj, int index) where T : ITimestampOffense
    {
        if (Pendings[index] is List<T> col)
        {
            lock (col)
            {
                col.Remove(obj);
            }
        }
    }
    private static ITimestampOffense[] GetOffenses()
    {
        lock (PendingBans)
        lock (PendingUnbans)
        lock (PendingKicks)
        lock (PendingWarnings)
        lock (PendingMutes)
        lock (PendingBattlEyeKicks)
        lock (PendingTeamkills)
        lock (PendingVehicleTeamkills)
        lock (PendingUnmutes)
        {
            return PendingBans
                .Cast<ITimestampOffense>()
                .Concat(PendingUnbans
                    .Cast<ITimestampOffense>())
                .Concat(PendingKicks
                    .Cast<ITimestampOffense>())
                .Concat(PendingWarnings
                    .Cast<ITimestampOffense>())
                .Concat(PendingMutes
                    .Cast<ITimestampOffense>())
                .Concat(PendingBattlEyeKicks
                    .Cast<ITimestampOffense>())
                .Concat(PendingTeamkills
                    .Cast<ITimestampOffense>())
                .Concat(PendingVehicleTeamkills
                    .Cast<ITimestampOffense>())
                .Concat(PendingUnmutes
                    .Cast<ITimestampOffense>())
                .OrderBy(x => x.Timestamp)
                .ToArray();
        }
    }
    internal static async Task OnConnected(CancellationToken token)
    {
        int v = Interlocked.Increment(ref _version);
        try
        {
            ITimestampOffense[] stamps = GetOffenses();

            if (stamps.Length == 0)
            {
                L.Log("No queued past offenses.", ConsoleColor.Magenta);
                return;
            }

            L.Log("Sending past offenses, " + stamps.Length.ToString(Data.AdminLocale) + " queued.", ConsoleColor.Magenta);
            int ct = Math.Max(1, stamps.Length / 10);
            try
            {
                for (int i = 0; i < stamps.Length; ++i)
                {
                    if (_version != v)
                    {
                        L.LogWarning("Cancelling active send job.");
                        return;
                    }
                    await UCWarfare.ToUpdate();
                    bool brk = false;
                    switch (stamps[i])
                    {
                        case Ban ban:
                            if (UCWarfare.CanUseNetCall && (await NetCalls.SendPlayerBanned.RequestAck(
                                    UCWarfare.I.NetClient!, ban.Violator, ban.Admin, ban.Reason, ban.Duration, ban.Timestamp,
                                    10000)).Responded)
                            {
                                RemoveFromSave(ban, 0);
                                Save<Ban>(0);
                            }
                            else
                            {
                                L.LogWarning("  Failed to send ban #" + i.ToString(Data.AdminLocale) + "!");
                                brk = true;
                            }
                            break;
                        case Unban unban:
                            if (UCWarfare.CanUseNetCall && (await NetCalls.SendPlayerUnbanned.RequestAck(
                                    UCWarfare.I.NetClient!, unban.Violator, unban.Admin, unban.Timestamp, 10000)).Responded)
                            {
                                RemoveFromSave(unban, 1);
                                Save<Unban>(1);
                            }
                            else
                            {
                                L.LogWarning("  Failed to send unban #" + i.ToString(Data.AdminLocale) + "!");
                                brk = true;
                            }
                            break;
                        case Kick kick:
                            if (UCWarfare.CanUseNetCall && (await NetCalls.SendPlayerKicked.RequestAck(
                                    UCWarfare.I.NetClient!, kick.Violator, kick.Admin, kick.Reason, kick.Timestamp, 10000))
                                .Responded)
                            {
                                RemoveFromSave(kick, 2);
                                Save<Kick>(2);
                            }
                            else
                            {
                                L.LogWarning("  Failed to send kick #" + i.ToString(Data.AdminLocale) + "!");
                                brk = true;
                            }
                            break;
                        case Warn warn:
                            if (UCWarfare.CanUseNetCall && (await NetCalls.SendPlayerWarned.RequestAck(
                                    UCWarfare.I.NetClient!, warn.Violator, warn.Admin, warn.Reason, warn.Timestamp, 10000))
                                .Responded)
                            {
                                RemoveFromSave(warn, 3);
                                Save<Warn>(3);
                            }
                            else
                            {
                                L.LogWarning("  Failed to send warn #" + i.ToString(Data.AdminLocale) + "!");
                                brk = true;
                            }
                            break;
                        case Mute mute:
                            if (UCWarfare.CanUseNetCall && (await NetCalls.SendPlayerMuted.RequestAck(
                                    UCWarfare.I.NetClient!, mute.Violator, mute.Admin, mute.MuteType, mute.Duration, mute.Reason,
                                    mute.Timestamp, 10000)).Responded)
                            {
                                RemoveFromSave(mute, 4);
                                Save<Mute>(4);
                            }
                            else
                            {
                                L.LogWarning("  Failed to send mute #" + i.ToString(Data.AdminLocale) + "!");
                                brk = true;
                            }
                            break;
                        case BattlEyeKick bekick:
                            if (UCWarfare.CanUseNetCall &&
                                (await NetCalls.SendPlayerBattleyeKicked.RequestAck(
                                    UCWarfare.I.NetClient!, bekick.Violator, bekick.Reason, bekick.Timestamp,
                                    10000)).Responded)
                            {
                                RemoveFromSave(bekick, 5);
                                Save<BattlEyeKick>(5);
                            }
                            else
                            {
                                L.LogWarning("  Failed to send battleye kick #" + i.ToString(Data.AdminLocale) + "!");
                                brk = true;
                            }
                            break;
                        case Teamkill teamkill:
                            if (UCWarfare.CanUseNetCall && (await NetCalls.SendTeamkill.RequestAck(
                                    UCWarfare.I.NetClient!, teamkill.Violator, teamkill.Dead, teamkill.DeathCause,
                                    teamkill.ItemName, teamkill.Timestamp, 10000)).Responded)
                            {
                                RemoveFromSave(teamkill, 6);
                                Save<BattlEyeKick>(6);
                            }
                            else
                            {
                                L.LogWarning("  Failed to send teamkill #" + i.ToString(Data.AdminLocale) + "!");
                                brk = true;
                            }
                            break;
                        case VehicleTeamkill vehicleTeamkill:
                            if (UCWarfare.CanUseNetCall && (await NetCalls.SendVehicleTeamkilled.RequestAck(
                                    UCWarfare.I.NetClient!, vehicleTeamkill.Violator,
                                    vehicleTeamkill.VehicleID, vehicleTeamkill.VehicleName, vehicleTeamkill.Timestamp, 10000))
                                .Responded)
                            {
                                RemoveFromSave(vehicleTeamkill, 7);
                                Save<VehicleTeamkill>(7);
                            }
                            else
                            {
                                L.LogWarning("  Failed to send vehicle teamkill #" + i.ToString(Data.AdminLocale) + "!");
                                brk = true;
                            }
                            break;
                        case Unmute unmute:
                            if (UCWarfare.CanUseNetCall && (await NetCalls.SendUnmuteRequest.RequestAck(
                                    UCWarfare.I.NetClient!, unmute.Violator, unmute.Admin, unmute.Timestamp, 10000)).Responded)
                            {
                                RemoveFromSave(unmute, 8);
                                Save<Unmute>(8);
                            }
                            else
                            {
                                L.LogWarning("  Failed to send unmute #" + i.ToString(Data.AdminLocale) + "!");
                                brk = true;
                            }
                            break;
                    }
                    if (brk)
                        break;
                    if (i % ct == 0)
                        L.Log("  Sending past offenses: " + i.ToString(Data.AdminLocale) + "/" +
                              stamps.Length.ToString(Data.AdminLocale), ConsoleColor.Magenta);
                }
            }
            catch (Exception ex)
            {
                L.LogError("Error sending past offenses.");
                L.LogError(ex);
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error sending offenses.");
            L.LogError(ex);
        }
    }
    public static void LogBanPlayer(ulong violator, ulong caller, string reason, int duration, DateTimeOffset timestamp)
    {
        UCWarfare.RunTask(async () =>
        {
            await Data.DatabaseManager.AddBan(violator, caller, duration, reason).ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                await UCWarfare.ToUpdate();
                RequestResponse response = await NetCalls.SendPlayerBanned.RequestAck(UCWarfare.I.NetClient!, violator, caller, reason, duration, timestamp, 10000);
                if (response.Responded)
                    return;
            }
            lock (PendingBans)
                PendingBans.Add(new Ban(violator, caller, reason, duration, timestamp));
            Save<Ban>(0);
        }, ctx: "Ban log");
    }
    public static void LogUnbanPlayer(ulong violator, ulong caller, DateTimeOffset timestamp)
    {
        UCWarfare.RunTask(async () =>
        {
            await Data.DatabaseManager.AddUnban(violator, caller).ConfigureAwait(false);
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendPlayerUnbanned.RequestAck(UCWarfare.I.NetClient!, violator, caller, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (PendingUnbans)
                PendingUnbans.Add(new Unban(violator, caller, timestamp));
            Save<Unban>(1);
        }, "Unban log");
    }
    public static void LogKickPlayer(ulong violator, ulong caller, string reason, DateTimeOffset timestamp)
    {
        UCWarfare.RunTask(async () =>
        {
            await Data.DatabaseManager.AddKick(violator, caller, reason).ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendPlayerKicked.RequestAck(UCWarfare.I.NetClient!, violator, caller, reason, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (PendingKicks)
                PendingKicks.Add(new Kick(violator, caller, reason, timestamp));
            Save<Kick>(2);
        }, ctx: "Kick log");
    }
    public static void LogWarnPlayer(ulong violator, ulong caller, string reason, DateTimeOffset timestamp)
    {
        UCWarfare.RunTask(async () =>
        {
            await Data.DatabaseManager.AddWarning(violator, caller, reason).ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendPlayerWarned.RequestAck(UCWarfare.I.NetClient!, violator, caller, reason, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (PendingWarnings)
                PendingWarnings.Add(new Warn(violator, caller, reason, timestamp));
            Save<Warn>(3);
        }, ctx: "Warn log");
    }
    public static void LogMutePlayer(ulong violator, ulong caller, EMuteType type, int duration, string reason, DateTimeOffset timestamp)
    {
        UCWarfare.RunTask(async () =>
        {
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendPlayerMuted.RequestAck(UCWarfare.I.NetClient!, violator, caller, type, duration, reason, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (PendingMutes)
                PendingMutes.Add(new Mute(violator, caller, type, duration, reason, timestamp));
            Save<Mute>(4);
        }, ctx: "Mute log");
    }
    public static void LogBattlEyeKicksPlayer(ulong violator, string reason, DateTimeOffset timestamp)
    {
        UCWarfare.RunTask(async () =>
        {
            await Data.DatabaseManager.AddBattleyeKick(violator, reason).ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendPlayerBattleyeKicked.RequestAck(UCWarfare.I.NetClient!, violator, reason, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (PendingBattlEyeKicks)
                PendingBattlEyeKicks.Add(new BattlEyeKick(violator, reason, timestamp));
            Save<BattlEyeKick>(5);
        }, ctx: "BattlEye log");
    }
    public static void LogTeamkill(ulong violator, ulong teamkilled, string deathCause, string itemName, ushort itemId, float distance, DateTimeOffset timestamp)
    {
        UCWarfare.RunTask(async () =>
        {
            await Data.DatabaseManager.AddTeamkill(violator, teamkilled, deathCause, itemName, itemId, distance).ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendTeamkill.RequestAck(UCWarfare.I.NetClient!, violator, teamkilled, deathCause, itemName, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (PendingTeamkills)
                PendingTeamkills.Add(new Teamkill(violator, teamkilled, deathCause, itemName, timestamp));
            Save<Teamkill>(6);
        }, ctx: "Teamkill log");
    }
    public static void LogVehicleTeamkill(ulong violator, ushort vehicleId, string vehicleName, DateTimeOffset timestamp)
    {
        UCWarfare.RunTask(async () =>
        {
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendVehicleTeamkilled.RequestAck(UCWarfare.I.NetClient!, violator, vehicleId, vehicleName, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (PendingVehicleTeamkills)
                PendingVehicleTeamkills.Add(new VehicleTeamkill(violator, vehicleId, vehicleName, timestamp));
            Save<VehicleTeamkill>(7);
        }, ctx: "Vehicle teamkill log");
    }
    public static void LogUnmutePlayer(ulong violator, ulong callerId, DateTimeOffset timestamp)
    {
        UCWarfare.RunTask(async () =>
        {
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendPlayerUnmuted.RequestAck(UCWarfare.I.NetClient!, violator, callerId, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (PendingUnmutes)
                PendingUnmutes.Add(new Unmute(violator, callerId, timestamp));
            Save<Unmute>(8);
        }, ctx: "Unmute log");
    }
    /// <returns>0 for a successful ban.</returns>
    internal static async Task<StandardErrorCode> BanPlayerAsync(ulong targetId, ulong callerId, string reason, int duration, DateTimeOffset timestamp, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? target = UCPlayer.FromID(targetId);
        UCPlayer? caller = UCPlayer.FromID(callerId);
        PlayerNames name;
        PlayerNames callerName;
        uint ipv4;
        List<byte[]> hwids = target is not null ? target.SteamPlayer.playerID.GetHwids().ToList() : (await GetAllHWIDs(targetId, token).ConfigureAwait(false));
        if (target is not null) // player is online
        {
            CSteamID id = target.Player.channel.owner.playerID.steamID;
            await UCWarfare.ToUpdate();
            ipv4 = SteamGameServerNetworkingUtils.getIPv4AddressOrZero(id);
            name = target.Name;
            Provider.requestBanPlayer(Provider.server, id, ipv4, hwids, reason, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration));
        }
        else
        {
            ipv4 = await Data.DatabaseManager.TryGetPackedIPAsync(targetId, token).ConfigureAwait(false);
            name = await F.GetPlayerOriginalNamesAsync(targetId, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            F.OfflineBan(targetId, ipv4, caller is null ? CSteamID.Nil : caller.Player.channel.owner.playerID.steamID,
                reason, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration), hwids.ToArray());
        }
        if (callerId != 0)
            callerName = await F.GetPlayerOriginalNamesAsync(callerId, token).ConfigureAwait(false);
        else
            callerName = PlayerNames.Console;
        ActionLog.Add(ActionLogType.BanPlayer, $"BANNED {targetId.ToString(Data.AdminLocale)} FOR \"{reason}\" DURATION: " +
            (duration == -1 ? "PERMANENT" : (duration.ToString(Data.AdminLocale) + " SECONDS")), callerId);

        LogBanPlayer(targetId, callerId, reason, duration, timestamp);

        if (duration == -1)
        {
            if (callerId == 0)
            {
                L.Log($"{name.PlayerName} ({targetId}) was permanently banned by an operator because {reason}.", ConsoleColor.Cyan);
                Chat.Broadcast(T.BanPermanentSuccessBroadcastOperator, target as IPlayer ?? name);
            }
            else
            {
                L.Log($"{name.PlayerName} ({targetId}) was permanenly banned by {callerName.PlayerName} ({callerId}) because {reason}.", ConsoleColor.Cyan);
                Chat.Broadcast(LanguageSet.AllBut(callerId), T.BanPermanentSuccessBroadcast, target as IPlayer ?? name, caller as IPlayer ?? callerName);
                caller?.SendChat(T.BanPermanentSuccessFeedback, target as IPlayer ?? name);
            }
        }
        else
        {
            string time = duration.GetTimeFromSeconds(L.Default);
            if (callerId == 0)
            {
                L.Log($"{name.PlayerName} ({targetId}) was banned for {time} by an operator because {reason}.", ConsoleColor.Cyan);
                bool f = false;
                foreach (LanguageSet set in LanguageSet.All())
                {
                    if (f || !set.Language.Equals(L.Default, StringComparison.Ordinal))
                    {
                        time = duration.GetTimeFromSeconds(set.Language);
                        f = true;
                    }
                    Chat.Broadcast(set, T.BanSuccessBroadcastOperator, target as IPlayer ?? name, time);
                }
            }
            else
            {
                L.Log($"{name.PlayerName} ({targetId}) was banned for {time} by {callerName.PlayerName} ({callerId}) because {reason}.", ConsoleColor.Cyan);
                bool f = false;
                foreach (LanguageSet set in LanguageSet.AllBut(callerId))
                {
                    if (f || !set.Language.Equals(L.Default, StringComparison.Ordinal))
                    {
                        time = duration.GetTimeFromSeconds(set.Language);
                        f = true;
                    }
                    Chat.Broadcast(set, T.BanSuccessBroadcast, target as IPlayer ?? name, caller as IPlayer ?? callerName, time);
                }
                if (f)
                    time = duration.GetTimeFromSeconds(callerId);
                else if (Data.Languages.TryGetValue(callerId, out string lang) && !lang.Equals(L.Default, StringComparison.Ordinal))
                    time = duration.GetTimeFromSeconds(lang);
                caller?.SendChat(T.BanSuccessFeedback, target as IPlayer ?? name, time);
            }
        }
        return StandardErrorCode.Success;
    }
    /// <returns>0 for a successful kick, 2 when the target is offline.</returns>
    internal static async Task<StandardErrorCode> KickPlayer(ulong targetId, ulong callerId, string reason, DateTimeOffset timestamp, CancellationToken token = default)
    {
        UCPlayer? target = UCPlayer.FromID(targetId);
        if (target is null)
            return StandardErrorCode.NotFound;
        PlayerNames names = target.Name;
        Provider.kick(target.Player.channel.owner.playerID.steamID, reason);

        LogKickPlayer(targetId, callerId, reason, timestamp);

        ActionLog.Add(ActionLogType.KickPlayer, $"KICKED {targetId.ToString(Data.AdminLocale)} FOR \"{reason}\"", callerId);
        if (callerId == 0)
        {
            L.Log($"{names.PlayerName} ({targetId}) was kicked by an operator because {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(T.KickSuccessBroadcastOperator, target as IPlayer);
        }
        else
        {
            UCPlayer? callerPlayer = UCPlayer.FromID(callerId);
            PlayerNames callerNames = await F.GetPlayerOriginalNamesAsync(callerId, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            L.Log($"{names.PlayerName} ({targetId}) was kicked by {callerNames.PlayerName} ({callerId}) because {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(LanguageSet.AllBut(callerId), T.KickSuccessBroadcast, target, callerPlayer as IPlayer ?? callerNames);
            callerPlayer?.SendChat(T.KickSuccessFeedback, target);
        }

        return StandardErrorCode.Success;
    }
    /// <returns>0 for a successful unban, 2 when the target isn't banned.</returns>
    internal static async Task<StandardErrorCode> UnbanPlayer(ulong targetId, ulong callerId, DateTimeOffset timestamp, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        PlayerNames targetNames = await F.GetPlayerOriginalNamesAsync(targetId, token).ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);
        if (!Provider.requestUnbanPlayer(callerId == 0 ? CSteamID.Nil : new CSteamID(callerId), new CSteamID(targetId)))
        {
            L.Log(callerId + " not banned.", ConsoleColor.Cyan);
            return StandardErrorCode.NotFound;
        }

        LogUnbanPlayer(targetId, callerId, DateTime.Now);

        string tid = targetId.ToString(Data.AdminLocale);
        ActionLog.Add(ActionLogType.UnbanPlayer, $"UNBANNED {tid}", callerId);
        if (callerId == 0)
        {
            L.Log($"{targetNames.PlayerName} ({tid}) was unbanned by an operator.", ConsoleColor.Cyan);
            Chat.Broadcast(T.UnbanSuccessBroadcastOperator, targetNames);
        }
        else
        {
            UCPlayer? caller = UCPlayer.FromID(callerId);
            PlayerNames callerNames = await F.GetPlayerOriginalNamesAsync(callerId, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            L.Log($"{targetNames.PlayerName} ({tid}) was unbanned by {callerNames.PlayerName} ({callerId}).", ConsoleColor.Cyan);
            caller?.SendChat(T.UnbanSuccessFeedback, targetNames);
            Chat.Broadcast(LanguageSet.AllBut(callerId), T.UnbanSuccessBroadcast, targetNames, caller as IPlayer ?? callerNames);
        }

        return StandardErrorCode.Success;
    }
    /// <returns>0 for a successful warn, 2 when the target isn't banned.</returns>
    internal static async Task<StandardErrorCode> WarnPlayer(ulong targetId, ulong callerId, string reason, DateTimeOffset timestamp, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? target = UCPlayer.FromID(targetId);
        if (target is null)
            return StandardErrorCode.NotFound;
        UCPlayer? caller = UCPlayer.FromID(callerId);
        PlayerNames targetNames = target.Name;

        LogWarnPlayer(targetId, callerId, reason, DateTime.Now);

        string tid = targetId.ToString(Data.AdminLocale);
        ActionLog.Add(ActionLogType.WarnPlayer, $"WARNED {tid} FOR \"{reason}\"", callerId);
        if (callerId == 0)
        {
            L.Log($"{targetNames.PlayerName} ({targetId}) was warned by an operator because {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(LanguageSet.AllBut(targetId), T.WarnSuccessBroadcastOperator, target);

            string lang = Localization.GetLang(target.Steam64);
            ToastMessage.QueueMessage(target, new ToastMessage(T.WarnSuccessDMOperator.Translate(T.WarnSuccessDMOperator.Translate(lang),
                lang, reason, target, target.GetTeam(), T.WarnSuccessDMOperator.Flags | TranslationFlags.UnityUI), EToastMessageSeverity.WARNING));

            target.SendChat(T.WarnSuccessDMOperator, reason);
        }
        else
        {
            PlayerNames callerNames = await F.GetPlayerOriginalNamesAsync(callerId, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            L.Log($"{targetNames.PlayerName} ({targetId}) was warned by {callerNames.PlayerName} ({caller}) because {reason}.", ConsoleColor.Cyan);
            IPlayer caller2 = caller as IPlayer ?? callerNames;
            Chat.Broadcast(LanguageSet.AllBut(callerId, targetId), T.WarnSuccessBroadcast, target, caller2);
            caller?.SendChat(T.WarnSuccessFeedback, target);

            string lang = Localization.GetLang(target.Steam64);
            ToastMessage.QueueMessage(target, new ToastMessage(T.WarnSuccessDM.Translate(T.WarnSuccessDM.Translate(lang),
                lang, caller2, reason, target, target.GetTeam(), T.WarnSuccessDM.Flags | TranslationFlags.UnityUI), EToastMessageSeverity.WARNING));

            target.SendChat(T.WarnSuccessDM, caller2, reason);
        }

        return StandardErrorCode.Success;
    }
    /// <returns>0 for a successful mute.</returns>
    internal static async Task<StandardErrorCode> MutePlayerAsync(ulong target, ulong admin, EMuteType type, int duration, string reason, DateTimeOffset timestamp, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? muted = UCPlayer.FromID(target);
        DateTime now = DateTime.Now;
        await Data.DatabaseManager.NonQueryAsync(
            "INSERT INTO `muted` (`Steam64`, `Admin`, `Reason`, `Duration`, `Timestamp`, `Type`) VALUES (@0, @1, @2, @3, @4, @5);",
            new object[] { target, admin, reason, duration, now, (byte)type }, token).ConfigureAwait(false);
        PlayerNames names = await F.GetPlayerOriginalNamesAsync(target, token).ConfigureAwait(false);
        PlayerNames names2 = await F.GetPlayerOriginalNamesAsync(admin, token).ConfigureAwait(false);
        await UCWarfare.ToUpdate();
        DateTime unmutedTime = duration == -1 ? DateTime.MaxValue : now + TimeSpan.FromSeconds(duration);
        if (muted is not null && muted.TimeUnmuted < unmutedTime)
        {
            muted.TimeUnmuted = unmutedTime;
            muted.MuteReason = reason;
            muted.MuteType = type;
        }

        LogMutePlayer(target, admin, type, duration, reason, DateTime.Now);

        string dur = duration == -1 ? "PERMANENT" : duration.GetTimeFromSeconds(0);
        ActionLog.Add(ActionLogType.MutePlayer, $"MUTED {target} FOR \"{reason}\" DURATION: " + dur, admin);

        if (admin == 0)
        {
            if (duration == -1)
            {
                foreach (LanguageSet set in LanguageSet.AllBut(target))
                    Chat.Broadcast(set, T.MutePermanentSuccessBroadcastOperator, names, names, type);

                L.Log($"{names.PlayerName} ({target}) was permanently {type} muted for {reason} by an operator.", ConsoleColor.Cyan);
            }
            else
            {
                foreach (LanguageSet set in LanguageSet.AllBut(target))
                    Chat.Broadcast(set, T.MuteSuccessBroadcastOperator, names, names, dur, type);

                L.Log($"{names.PlayerName} ({target}) was {type} muted for {reason} by an operator. Duration: {dur}.", ConsoleColor.Cyan);
            }
        }
        else
        {
            if (duration == -1)
            {
                foreach (LanguageSet set in LanguageSet.AllBut(target, admin))
                    Chat.Broadcast(set, T.MutePermanentSuccessBroadcast, names, names, type, names2);

                L.Log($"{names.PlayerName} ({target}) was permanently {type} muted for {reason} by {names2.PlayerName} ({admin}).", ConsoleColor.Cyan);
            }
            else
            {
                foreach (LanguageSet set in LanguageSet.AllBut(target, admin))
                    Chat.Broadcast(set, T.MuteSuccessBroadcast, names, names, dur, type, names2);

                L.Log($"{names.PlayerName} ({target}) was {type} muted for {reason} by {names2.PlayerName} ({admin}). Duration: {dur}.", ConsoleColor.Cyan);
            }
        }
        if (muted != null)
        {
            if (admin == 0)
            {
                if (duration == -1)
                    muted.SendChat(T.MuteSuccessDMPermanentOperator, reason, type);
                else
                    muted.SendChat(T.MuteSuccessDMOperator, reason, dur, type);
            }
            else
            {
                if (duration == -1)
                    muted.SendChat(T.MuteSuccessDMPermanent, names2, reason, type);
                else
                    muted.SendChat(T.MuteSuccessDM, names2, reason, dur, type);
            }
        }

        return StandardErrorCode.Success;
    }
    /// <returns>0 for a successful unmute, 2 when the target isn't muted.</returns>
    internal static async Task<StandardErrorCode> UnmutePlayerAsync(ulong targetId, ulong callerId, DateTimeOffset timestamp)
    {
        UCPlayer? caller = UCPlayer.FromID(callerId);
        UCPlayer? onlinePlayer = UCPlayer.FromID(targetId);
        PlayerNames names = await Data.DatabaseManager.GetUsernamesAsync(targetId).ConfigureAwait(false);
        if (names.WasFound)
        {
            int rows = await Data.DatabaseManager.NonQueryAsync(
                    "UPDATE `muted` SET `Deactivated` = 1 WHERE `Steam64` = @0 AND " + 
                    "`Deactivated` = 0 AND (`Duration` = -1 OR TIME_TO_SEC(TIMEDIFF(`Timestamp`, NOW())) / -60 < `Duration`)", new object[] { targetId })
                .ConfigureAwait(false);

            await UCWarfare.ToUpdate();
            if (rows == 0)
            {
                if (caller is not null)
                    caller.SendChat(T.UnmuteNotMuted, names);
                else if (callerId == 0)
                    L.Log(Util.RemoveRichText(T.UnmuteNotMuted.Translate(L.Default, names, out Color color)), Util.GetClosestConsoleColor(color));
                return StandardErrorCode.GenericError;
            }
            else
            {
                onlinePlayer ??= UCPlayer.FromID(targetId);
                if (onlinePlayer is not null)
                {
                    onlinePlayer.MuteReason = null;
                    onlinePlayer.MuteType = EMuteType.NONE;
                    onlinePlayer.TimeUnmuted = DateTime.MinValue;
                }
                LogUnmutePlayer(targetId, callerId, DateTime.Now);
                PlayerNames n2 = await F.GetPlayerOriginalNamesAsync(callerId).ConfigureAwait(false);
                await UCWarfare.ToUpdate();
                if (callerId == 0)
                {
                    Chat.Broadcast(LanguageSet.AllBut(targetId), T.UnmuteSuccessBroadcastOperator, names);
                    onlinePlayer?.SendChat(T.UnmuteSuccessDMOperator);
                }
                else
                {
                    Chat.Broadcast(LanguageSet.AllBut(targetId, callerId), T.UnmuteSuccessBroadcast, names, n2);
                    onlinePlayer?.SendChat(T.UnmuteSuccessDM, n2);
                    caller?.SendChat(T.UnmuteSuccessFeedback, names);
                }
                ActionLog.Add(ActionLogType.UnmutePlayer, targetId.ToString() + " unmuted.", callerId);
                L.Log($"{names.PlayerName} ({targetId}) was unmuted by {(callerId == 0 ? "an operator" : (n2.PlayerName + "(" + callerId + ")"))}.");
                return StandardErrorCode.Success;
            }
        }
        else
        {
            await UCWarfare.ToUpdate();
            if (caller is not null)
                caller.SendChat(T.PlayerNotFound);
            else if (callerId == 0)
                L.Log(Util.RemoveRichText(T.PlayerNotFound.Translate(L.Default, out Color color)), Util.GetClosestConsoleColor(color));
            return StandardErrorCode.NotFound;
        }
    }
    private static void OnPlayerDied(PlayerDied e)
    {
        if (e.WasTeamkill && e.Killer is not null)
        {
            Asset a = Assets.find(e.PrimaryAsset);
            string itemName = a?.FriendlyName ?? e.PrimaryAsset.ToString("N");
            LogTeamkill(e.Killer.Steam64, e.Player.Steam64, e.Cause.ToString(), itemName, a == null ? (ushort)0 : a.id, e.KillDistance, DateTime.Now);
        }
    }
    private interface ITimestampOffense
    {
        public DateTimeOffset Timestamp { get; set; }
    }

    private struct Ban : ITimestampOffense, IEquatable<Ban>
    {
        public readonly ulong Violator;
        public readonly ulong Admin;
        public readonly string Reason;
        public readonly int Duration;
        public DateTimeOffset Timestamp { get; set; }
        [JsonConstructor]
        public Ban(ulong violator, ulong admin, string reason, int duration, DateTimeOffset timestamp)
        {
            Violator = violator;
            Admin = admin;
            Reason = reason;
            Duration = duration;
            Timestamp = timestamp;
        }
        public bool Equals(Ban offense)
        {
            return Violator == offense.Violator && Admin == offense.Admin && Duration == offense.Duration && Timestamp == offense.Timestamp;
        }
    }
    private struct Unban : ITimestampOffense, IEquatable<Unban>
    {
        public readonly ulong Violator;
        public readonly ulong Admin;
        public DateTimeOffset Timestamp { get; set; }
        [JsonConstructor]
        public Unban(ulong violator, ulong admin, DateTimeOffset timestamp)
        {
            Violator = violator;
            Admin = admin;
            Timestamp = timestamp;
        }
        public bool Equals(Unban offense)
        {
            return Violator == offense.Violator && Admin == offense.Admin && Timestamp == offense.Timestamp;
        }
    }
    private struct Kick : ITimestampOffense, IEquatable<Kick>
    {
        public readonly ulong Violator;
        public readonly ulong Admin;
        public readonly string Reason;
        public DateTimeOffset Timestamp { get; set; }
        [JsonConstructor]
        public Kick(ulong violator, ulong admin, string reason, DateTimeOffset timestamp)
        {
            Violator = violator;
            Admin = admin;
            Reason = reason;
            Timestamp = timestamp;
        }
        public bool Equals(Kick offense)
        {
            return Violator == offense.Violator && Admin == offense.Admin && Timestamp == offense.Timestamp;
        }
    }
    private struct Warn : ITimestampOffense, IEquatable<Warn>
    {
        public readonly ulong Violator;
        public readonly ulong Admin;
        public readonly string Reason;
        public DateTimeOffset Timestamp { get; set; }
        [JsonConstructor]
        public Warn(ulong violator, ulong admin, string reason, DateTimeOffset timestamp)
        {
            Violator = violator;
            Admin = admin;
            Reason = reason;
            Timestamp = timestamp;
        }
        public bool Equals(Warn offense)
        {
            return Violator == offense.Violator && Admin == offense.Admin && Timestamp == offense.Timestamp;
        }
    }
    private struct Mute : ITimestampOffense, IEquatable<Mute>
    {
        public readonly ulong Violator;
        public readonly ulong Admin;
        public readonly EMuteType MuteType;
        public readonly int Duration;
        public readonly string Reason;
        public DateTimeOffset Timestamp { get; set; }
        [JsonConstructor]
        public Mute(ulong violator, ulong admin, EMuteType muteType, int duration, string reason, DateTimeOffset timestamp)
        {
            Violator = violator;
            Admin = admin;
            MuteType = muteType;
            Duration = duration;
            Reason = reason;
            Timestamp = timestamp;
        }
        public bool Equals(Mute offense)
        {
            return Violator == offense.Violator && Admin == offense.Admin && MuteType == offense.MuteType && Duration == offense.Duration && Timestamp == offense.Timestamp;
        }
    }
    private struct BattlEyeKick : ITimestampOffense, IEquatable<BattlEyeKick>
    {
        public readonly ulong Violator;
        public readonly string Reason;
        public DateTimeOffset Timestamp { get; set; }
        [JsonConstructor]
        public BattlEyeKick(ulong violator, string reason, DateTimeOffset timestamp)
        {
            Violator = violator;
            Reason = reason;
            Timestamp = timestamp;
        }
        public bool Equals(BattlEyeKick offense)
        {
            return Violator == offense.Violator && Timestamp == offense.Timestamp;
        }
    }
    private struct Teamkill : ITimestampOffense, IEquatable<Teamkill>
    {
        public readonly ulong Violator;
        public readonly ulong Dead;
        public readonly string DeathCause;
        public readonly string ItemName;
        public DateTimeOffset Timestamp { get; set; }
        [JsonConstructor]
        public Teamkill(ulong violator, ulong dead, string deathCause, string itemName, DateTimeOffset timestamp)
        {
            Violator = violator;
            Dead = dead;
            DeathCause = deathCause;
            ItemName = itemName;
            Timestamp = timestamp;
        }
        public bool Equals(Teamkill offense)
        {
            return Violator == offense.Violator && Dead == offense.Dead && Timestamp == offense.Timestamp;
        }
    }
    private struct VehicleTeamkill : ITimestampOffense, IEquatable<VehicleTeamkill>
    {
        public readonly ulong Violator;
        public readonly ushort VehicleID;
        public readonly string VehicleName;
        public DateTimeOffset Timestamp { get; set; }
        [JsonConstructor]
        public VehicleTeamkill(ulong violator, ushort vehicleId, string vehicleName, DateTimeOffset timestamp)
        {
            Violator = violator;
            VehicleID = vehicleId;
            VehicleName = vehicleName;
            Timestamp = timestamp;
        }
        public bool Equals(VehicleTeamkill offense)
        {
            return Violator == offense.Violator && VehicleID == offense.VehicleID && Timestamp == offense.Timestamp;
        }
    }
    private struct Unmute : ITimestampOffense, IEquatable<Unmute>
    {
        public readonly ulong Violator;
        public readonly ulong Admin;
        public DateTimeOffset Timestamp { get; set; }
        [JsonConstructor]
        public Unmute(ulong violator, ulong admin, DateTimeOffset timestamp)
        {
            Violator = violator;
            Admin = admin;
            Timestamp = timestamp;
        }
        public bool Equals(Unmute offense)
        {
            return Violator == offense.Violator && Admin == offense.Admin && Timestamp == offense.Timestamp;
        }
    }
    public static class NetCalls
    {
        public static readonly NetCall<ulong, ulong, string, int, DateTimeOffset> SendBanRequest = new NetCall<ulong, ulong, string, int, DateTimeOffset>(ReceiveBanRequest);
        public static readonly NetCall<ulong, ulong, DateTimeOffset> SendUnbanRequest = new NetCall<ulong, ulong, DateTimeOffset>(ReceiveUnbanRequest);
        public static readonly NetCall<ulong, ulong, string, DateTimeOffset> SendKickRequest = new NetCall<ulong, ulong, string, DateTimeOffset>(ReceieveKickRequest);
        public static readonly NetCall<ulong, ulong, string, DateTimeOffset> SendWarnRequest = new NetCall<ulong, ulong, string, DateTimeOffset>(ReceiveWarnRequest);
        public static readonly NetCall<ulong, ulong, EMuteType, int, string, DateTimeOffset> SendMuteRequest = new NetCall<ulong, ulong, EMuteType, int, string, DateTimeOffset>(ReceieveMuteRequest);
        public static readonly NetCall<ulong, ulong, DateTimeOffset> SendUnmuteRequest = new NetCall<ulong, ulong, DateTimeOffset>(ReceieveUnmuteRequest);
        public static readonly NetCall<ulong> GrantAdminRequest = new NetCall<ulong>(ReceiveGrantAdmin);
        public static readonly NetCall<ulong> RevokeAdminRequest = new NetCall<ulong>(ReceiveRevokeAdmin);
        public static readonly NetCall<ulong> GrantInternRequest = new NetCall<ulong>(ReceiveGrantIntern);
        public static readonly NetCall<ulong> RevokeInternRequest = new NetCall<ulong>(ReceiveRevokeIntern);
        public static readonly NetCall<ulong> GrantHelperRequest = new NetCall<ulong>(ReceiveGrantHelper);
        public static readonly NetCall<ulong> RevokeHelperRequest = new NetCall<ulong>(ReceiveRevokeHelper);

        public static readonly NetCall<ulong, ulong, string, int, DateTimeOffset> SendPlayerBanned = new NetCall<ulong, ulong, string, int, DateTimeOffset>(1001);
        public static readonly NetCall<ulong, ulong, DateTimeOffset> SendPlayerUnbanned = new NetCall<ulong, ulong, DateTimeOffset>(1002);
        public static readonly NetCall<ulong, ulong, string, DateTimeOffset> SendPlayerKicked = new NetCall<ulong, ulong, string, DateTimeOffset>(1003);
        public static readonly NetCall<ulong, ulong, string, DateTimeOffset> SendPlayerWarned = new NetCall<ulong, ulong, string, DateTimeOffset>(1004);
        public static readonly NetCall<ulong, string, DateTimeOffset> SendPlayerBattleyeKicked = new NetCall<ulong, string, DateTimeOffset>(1005);
        public static readonly NetCall<ulong, ulong, string, string, DateTimeOffset> SendTeamkill = new NetCall<ulong, ulong, string, string, DateTimeOffset>(1006);
        public static readonly NetCall<ulong, ulong, EMuteType, int, string, DateTimeOffset> SendPlayerMuted = new NetCall<ulong, ulong, EMuteType, int, string, DateTimeOffset>(1027);
        public static readonly NetCall<ulong, ushort, string, DateTimeOffset> SendVehicleTeamkilled = new NetCall<ulong, ushort, string, DateTimeOffset>(1112);
        public static readonly NetCall<ulong, ulong, DateTimeOffset> SendPlayerUnmuted = new NetCall<ulong, ulong, DateTimeOffset>(1020);

        [NetCall(ENetCall.FROM_SERVER, 1007)]
        internal static async Task ReceiveBanRequest(MessageContext context, ulong target, ulong admin, string reason, int duration, DateTimeOffset timestamp)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await BanPlayerAsync(target, admin, reason, duration, timestamp));
        }

        [NetCall(ENetCall.FROM_SERVER, 1009)]
        internal static async Task ReceiveUnbanRequest(MessageContext context, ulong target, ulong admin, DateTimeOffset timestamp)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await UnbanPlayer(target, admin, timestamp));
        }

        [NetCall(ENetCall.FROM_SERVER, 1010)]
        internal static async Task ReceieveKickRequest(MessageContext context, ulong target, ulong admin, string reason, DateTimeOffset timestamp)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await KickPlayer(target, admin, reason, timestamp));
        }

        [NetCall(ENetCall.FROM_SERVER, 1028)]
        internal static async Task ReceieveMuteRequest(MessageContext context, ulong target, ulong admin, EMuteType type, int duration, string reason, DateTimeOffset timestamp)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await MutePlayerAsync(target, admin, type, duration, reason, timestamp));
        }

        [NetCall(ENetCall.FROM_SERVER, 1021)]
        internal static async Task ReceieveUnmuteRequest(MessageContext context, ulong target, ulong admin, DateTimeOffset timestamp)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await UnmutePlayerAsync(target, admin, timestamp));
        }

        [NetCall(ENetCall.FROM_SERVER, 1011)]
        internal static async Task ReceiveWarnRequest(MessageContext context, ulong target, ulong admin, string reason, DateTimeOffset timestamp)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await WarnPlayer(target, admin, reason, timestamp));
        }

        [NetCall(ENetCall.FROM_SERVER, 1103)]
        internal static void ReceiveGrantAdmin(MessageContext context, ulong player)
        {
            PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.ADMIN);
        }

        [NetCall(ENetCall.FROM_SERVER, 1104)]
        internal static void ReceiveRevokeAdmin(MessageContext context, ulong player)
        {
            RevokeAll(player);
        }

        [NetCall(ENetCall.FROM_SERVER, 1105)]
        internal static void ReceiveGrantIntern(MessageContext context, ulong player)
        {
            PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.TRIAL_ADMIN);
        }

        [NetCall(ENetCall.FROM_SERVER, 1106)]
        internal static void ReceiveRevokeIntern(MessageContext context, ulong player)
        {
            RevokeAll(player);
        }

        [NetCall(ENetCall.FROM_SERVER, 1107)]
        internal static void ReceiveGrantHelper(MessageContext context, ulong player)
        {
            PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.HELPER);
        }

        [NetCall(ENetCall.FROM_SERVER, 1108)]
        internal static void ReceiveRevokeHelper(MessageContext context, ulong player)
        {
            RevokeAll(player);
        }
        private static void RevokeAll(ulong player)
        {
            PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.MEMBER);
        }
    }
}