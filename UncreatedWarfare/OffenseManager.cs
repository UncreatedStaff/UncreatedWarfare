using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Moderation.Records;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;

namespace Uncreated.Warfare;

[HarmonyPatch]
public static class OffenseManager
{
    public static IPAddress Unpack(uint address)
    {
        uint newAddr = address << 24 | ((address >> 8) & 0xFF) << 16 | ((address >> 16) & 0xFF) << 8 | (address >> 24);
        return new IPAddress(newAddr);
    }
    public static uint Pack(IPAddress address)
    {
        byte[] ipv4 = address.MapToIPv4().GetAddressBytes();
        return ((uint)ipv4[0] << 24) | ((uint)ipv4[1] << 16) | ((uint)ipv4[2] << 8) | ipv4[3];
    }
    // calls the type initializer
    internal static void Init()
    {
        EventDispatcher.PlayerDied += OnPlayerDied;
        EventDispatcher.PlayerJoined += OnPlayerJoined;
        EventDispatcher.PlayerPendingAsync += OnPlayerPending;
#if false
        List<IPv4Range> ranges = new List<IPv4Range>(128);
        for (int i = 0; i < RemotePlayAddressFilters.Length; ++i)
            ranges.AddRange(RemotePlayAddressFilters[i].Ranges);
        ranges = IPv4Range.GetNonOverlappingIPs(ranges);
        L.Log($"Counted {IPv4Range.CountIncludedIPs(ranges)} filtered IPs for remote play.", ConsoleColor.Cyan);
#endif
    }

    [HarmonyPatch(typeof(SteamBlacklist), nameof(SteamBlacklist.checkBanned), new Type[]
    {
        typeof(CSteamID), typeof(uint), typeof(IEnumerable<byte[]>), typeof(SteamBlacklistID)
    }, new ArgumentType[]
    {
        ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out
    })]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static void CheckBannedPrefix(CSteamID playerID, ref uint ip, ref IEnumerable<byte[]>? hwids, SteamBlacklistID blacklistID)
    {
        ip = 0;
        hwids = null;
    }
    internal static void Deinit()
    {
        EventDispatcher.PlayerPendingAsync -= OnPlayerPending;
        EventDispatcher.PlayerJoined -= OnPlayerJoined;
        EventDispatcher.PlayerDied -= OnPlayerDied;
    }
    public static HWID[] ConvertVanillaHWIDs(IEnumerable<byte[]> hwids)
    {
        byte[][] arr = hwids.ToArrayFast();
        HWID[] outArray = new HWID[arr.Length];
        for (int i = 0; i < arr.Length; i++)
            outArray[i] = new HWID(arr[i]);
        
        return outArray;
    }
    public static void OnModerationEntryUpdated(ModerationEntry entry)
    {
        L.LogDebug(JsonSerializer.Serialize(entry, JsonEx.serializerSettings));

        UCPlayer? player = UCPlayer.FromID(entry.Player);
        if (player == null)
            return;
        if (entry.PendingReputation != 0d)
        {
            entry.PendingReputation = 0d;
            player.AddReputation((int)Math.Round(entry.PendingReputation));
            UCWarfare.RunTask(Data.ModerationSql.AddOrUpdate, entry, CancellationToken.None, ctx: "Apply reputation.");
        }
        if (entry is Ban ban && ban.IsApplied(true))
        {
            string msg;
            if (ban.IsPermanent)
                msg = T.RejectPermanentBanned.Translate(player, false, entry.Message ?? "No message.");
            else
                msg = T.RejectBanned.Translate(player, false, entry.Message ?? "No message.", (int)Math.Round(ban.GetTimeUntilExpiry(false).TotalSeconds, MidpointRounding.AwayFromZero));
            Provider.kick(player.CSteamID, msg);
        }
        else if (entry is Kick)
        {
            Provider.kick(player.CSteamID, entry.Message ?? "No message.");
        }
        else if (entry is Mute mute && mute.IsApplied(true))
        {
            DateTime dt = mute.GetExpiryTimestamp(true).LocalDateTime;
            if (player.TimeUnmuted <= dt)
            {
                player.MuteReason = mute.Message;
                player.MuteType = mute.Type;
                player.TimeUnmuted = dt;
            }
            else if (player.MuteType != mute.Type)
                player.MuteType = MuteType.Both;
        }
    }
    public static void OnNewModerationEntryAdded(ModerationEntry entry)
    {
        L.Log($"Moderation entry added: {entry.GetType().Name}, Player: {entry.Player}.", ConsoleColor.Cyan);

        OnModerationEntryUpdated(entry);
        UniTask.Create(async () =>
        {
            CancellationToken token = UCWarfare.UnloadCancel;
            if (entry is not Ban and not Kick and not Warning and not Mute)
                return;

            OfflinePlayerName? adminActor = entry.TryGetPrimaryAdmin(out RelatedActor actor)
                ? new OfflinePlayerName(actor.Actor.Id, await actor.Actor.GetDisplayName(Data.ModerationSql, token))
                : null;
            IPlayer player;

            if (UCPlayer.FromID(entry.Player) is { } pl)
                player = pl;
            else
                player = await Data.ModerationSql.GetUsernames(entry.Player, true, token).ConfigureAwait(false);
            
            await UniTask.SwitchToMainThread(token);

            switch (entry)
            {
                case Ban ban:
                    if (adminActor.HasValue)
                    {
                        if (ban.IsPermanent)
                            Chat.Broadcast(T.BanPermanentSuccessBroadcast, player, adminActor);
                        else
                            Chat.Broadcast(T.BanSuccessBroadcast, player, adminActor, Util.ToTimeString((int)Math.Round(ban.Duration.TotalSeconds, MidpointRounding.AwayFromZero)));
                    }
                    else
                    {
                        if (ban.IsPermanent)
                            Chat.Broadcast(T.BanPermanentSuccessBroadcastOperator, player);
                        else
                            Chat.Broadcast(T.BanSuccessBroadcastOperator, player, Util.ToTimeString((int)Math.Round(ban.Duration.TotalSeconds, MidpointRounding.AwayFromZero)));
                    }
                    break;
                case Mute mute:
                    if (adminActor.HasValue)
                    {
                        if (mute.IsPermanent)
                            Chat.Broadcast(T.MutePermanentSuccessBroadcast, player, player, mute.Type, adminActor);
                        else
                            Chat.Broadcast(T.MuteSuccessBroadcast, player, player, Util.ToTimeString((int)Math.Round(mute.Duration.TotalSeconds, MidpointRounding.AwayFromZero)), mute.Type, adminActor);
                    }
                    else
                    {
                        if (mute.IsPermanent)
                            Chat.Broadcast(T.MutePermanentSuccessBroadcastOperator, player, player, mute.Type);
                        else
                            Chat.Broadcast(T.MuteSuccessBroadcastOperator, player, player, Util.ToTimeString((int)Math.Round(mute.Duration.TotalSeconds, MidpointRounding.AwayFromZero)), mute.Type);
                    }
                    break;
                case Kick:
                    if (adminActor.HasValue)
                        Chat.Broadcast(T.KickSuccessBroadcast, player, adminActor);
                    else
                        Chat.Broadcast(T.KickSuccessBroadcastOperator, player);
                    
                    break;
                case Warning warn:
                    if (adminActor.HasValue)
                        Chat.Broadcast(T.WarnSuccessBroadcast, player, adminActor);
                    else
                        Chat.Broadcast(T.WarnSuccessBroadcastOperator, player);

                    TryDisplayWarning(warn, true);
                    break;
            }
        });
    }
    internal static bool TryDisplayWarning(Warning warning, bool save)
    {
        if (warning.HasBeenDisplayed)
            return true;

        if (UCPlayer.FromID(warning.Player) is not { } onlinePlayer)
            return false;

        warning.DisplayedTimestamp = DateTimeOffset.UtcNow;
        if (save)
            UCWarfare.RunTask(Data.ModerationSql.AddOrUpdate, warning, CancellationToken.None, ctx: "Apply warning.");

        UniTask.Create(async () =>
        {
            OfflinePlayerName? adminActor = warning.TryGetPrimaryAdmin(out RelatedActor actor)
                ? new OfflinePlayerName(actor.Actor.Id, await actor.Actor.GetDisplayName(Data.ModerationSql, CancellationToken.None))
                : null;

            if (!onlinePlayer.IsOnline)
                return;

            if (adminActor.HasValue)
                ToastMessage.QueueMessage(onlinePlayer, ToastMessage.Popup(T.WarnSuccessTitle.Translate(onlinePlayer, false), T.WarnSuccessDM.Translate(onlinePlayer, false, adminActor, warning.Message ?? "No message.")));
            else
                ToastMessage.QueueMessage(onlinePlayer, ToastMessage.Popup(T.WarnSuccessTitle.Translate(onlinePlayer, false), T.WarnSuccessDMOperator.Translate(onlinePlayer, false, warning.Message ?? "No message.")));
        });

        return true;
    }
    private static async Task OnPlayerPending(PlayerPending e, CancellationToken token = default)
    {
        if (e.PendingPlayer.playerID.GetHwids().Count() is not 3 and not 2)
            throw e.Reject("Likely HWID spoofer.");

        PlayerIPAddress[] ipAddresses = await Data.ModerationSql.GetIPAddresses(e.Steam64, true, token).ConfigureAwait(false);
        PlayerHWID[] hwids = await Data.ModerationSql.GetHWIDs(e.Steam64, token).ConfigureAwait(false);
        e.AsyncData.HWIDs = new List<PlayerHWID>(hwids.Length + 3);
        e.AsyncData.IPAddresses = new List<PlayerIPAddress>(ipAddresses.Length + 1);
        e.AsyncData.HWIDs.AddRange(hwids);
        e.AsyncData.IPAddresses.AddRange(ipAddresses);

        Ban[] bans = await Data.ModerationSql.GetActiveEntries<Ban>(e.Steam64, ipAddresses, hwids, false, false, token: token).ConfigureAwait(false);
        LanguagePreferences languageInfo = await Data.LanguageDataStore.GetLanguagePreferences(e.Steam64, token).ConfigureAwait(false);
        LanguageInfo language = languageInfo.Language;
        if (bans.Length > 0)
        {
            Ban ban = bans[0];
            string msg = ban.Message ?? "No Message";
            if (!ban.IsPermanent)
                throw e.Reject(T.RejectBanned.Translate(language, msg, (int)Math.Round(ban.GetTimeUntilExpiry(false).TotalSeconds, MidpointRounding.AwayFromZero)));
            
            throw e.Reject(T.RejectPermanentBanned.Translate(language, msg));
        }
    }
    private static void OnPlayerJoined(PlayerJoined e)
    {
        byte[][] bytes = (e.Player.SteamPlayer.playerID.GetHwids() as byte[][])!;
        int[] update = new int[bytes.Length];
        ulong s64 = e.Steam64;
        uint packed = e.Player.SteamPlayer.transportConnection.TryGetIPv4Address(out uint ip) ? ip : 0u;
        Task.Run(async () =>
        {
            try
            {
                if (!e.Player.UsingRemotePlay)
                {
                    await Data.DatabaseManager.QueryAsync("SELECT `Id`, `Index`, `HWID` FROM `hwids` WHERE `Steam64` = @0 ORDER BY `LastLogin` DESC;",
                        new object[] { s64 },
                        reader =>
                        {
                            int id = reader.GetInt32(0);
                            byte[] buffer = new byte[20];
                            reader.GetBytes(2, 0, buffer, 0, 20);
                            for (int i = 0; i < update.Length; ++i)
                            {
                                if (update[i] > 0) continue;
                                byte[] b = bytes[i];
                                if (b is null) continue;
                                for (int x = 0; x < 20; ++x)
                                    if (b[x] != buffer[x])
                                        goto cont;
                                update[i] = id;
                                break;
                                cont:;
                            }
                        });
                    StringBuilder sbq = new StringBuilder(64);
                    StringBuilder sbq2 = new StringBuilder(64);
                    int c = 2;
                    for (int i = 0; i < update.Length; ++i)
                    {
                        if (update[i] < 1 && bytes[i] is not null && bytes[i].Length == 20 && i < 8)
                            c += 2;
                    }
                    object[] objs = new object[c];
                    objs[0] = s64;
                    objs[1] = DateTime.UtcNow;
                    c = 1;
                    int d = 0;
                    object[] o2 = new object[update.Length];
                    for (int i = 0; i < update.Length; ++i)
                    {
                        if (update[i] > 0)
                        {
                            sbq2.Append($"UPDATE `{WarfareSQL.TableHWIDs}` SET `{WarfareSQL.ColumnHWIDsLoginCount}` = `{WarfareSQL.ColumnHWIDsLoginCount}` + 1," +
                                        $" `{WarfareSQL.ColumnHWIDsLastLogin}` = UTC_TIMESTAMP() WHERE `{WarfareSQL.ColumnHWIDsPrimaryKey}` = @" + i + ";");
                            ++d;
                            o2[i] = update[i];
                            continue;
                        }
                        if (bytes[i] is not null && bytes[i].Length == 20 && i < 8)
                        {
                            if (c != 1)
                                sbq.Append(',');
                            sbq.Append("(@0, @1, @").Append(c * 2).Append(", @").Append(c * 2 + 1).Append(",1,@1)");
                            objs[c * 2] = i;
                            objs[c * 2 + 1] = bytes[i];
                            ++c;
                        }

                        o2[i] = 0;
                    }
                    if (c > 1)
                    {
                        sbq.Insert(0, $"INSERT INTO `{WarfareSQL.TableHWIDs}` (`{WarfareSQL.ColumnHWIDsSteam64}`, `{WarfareSQL.ColumnHWIDsFirstLogin}`, " +
                                      $"`{WarfareSQL.ColumnHWIDsIndex}`, `{WarfareSQL.ColumnHWIDsHWID}`, `{WarfareSQL.ColumnHWIDsLoginCount}`, `{WarfareSQL.ColumnHWIDsLastLogin}`) VALUES ");
                        sbq.Append(';');
                        string query = sbq.ToString();

                        await Data.DatabaseManager.NonQueryAsync(query, objs);
                    }
                    if (d > 0)
                    {
                        await Data.DatabaseManager.NonQueryAsync(sbq2.ToString(), o2);
                    }
                }

                uint? id = null;
                await Data.DatabaseManager.QueryAsync("SELECT `Id` FROM `ip_addresses` WHERE `Steam64` = @0 AND `Packed` = @1 LIMIT 1;", new object[] { s64, packed },
                    reader => id = reader.GetUInt32(0));
                if (id.HasValue)
                    await Data.DatabaseManager.NonQueryAsync(
                        $"UPDATE `{WarfareSQL.TableIPAddresses}` SET " +
                        $"`{WarfareSQL.ColumnIPAddressesLoginCount}` = `{WarfareSQL.ColumnIPAddressesLoginCount}` + 1, `{WarfareSQL.ColumnIPAddressesLastLogin}` = " +
                        $"UTC_TIMESTAMP() WHERE `{WarfareSQL.ColumnIPAddressesPrimaryKey}` = @0 LIMIT 1;",
                        new object[] { id.Value });
                else
                    await Data.DatabaseManager.NonQueryAsync(
                        $"INSERT INTO `{WarfareSQL.TableIPAddresses}` (`{WarfareSQL.ColumnIPAddressesSteam64}`, " +
                        $"`{WarfareSQL.ColumnIPAddressesPackedIP}`, `{WarfareSQL.ColumnIPAddressesUnpackedIP}`, " +
                        $"`{WarfareSQL.ColumnIPAddressesFirstLogin}`, `{WarfareSQL.ColumnIPAddressesLoginCount}`, " +
                        $"`{WarfareSQL.ColumnIPAddressesLastLogin}`) VALUES (@0, @1, @2, @3, 1, @4);",
                        new object[] { s64, packed, Parser.getIPFromUInt32(packed), DateTime.UtcNow, DateTime.UtcNow });
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
    public static async Task ApplyMuteSettings(UCPlayer joining, CancellationToken token = default)
    {
        if (joining == null) return;
        MuteType type = MuteType.None;
        Mute[] mutes = await Data.ModerationSql.GetActiveEntries<Mute>(joining.Steam64, joining.IPAddresses, joining.HWIDs, false, false, token: token).ConfigureAwait(false);
        Mute? longest = null;
        DateTimeOffset expiry = default;
        for (int i = 0; i < mutes.Length; ++i)
        {
            Mute mute = mutes[i];
            if (mute.Type is not MuteType.Voice and not MuteType.Text and not MuteType.Both)
                continue;

            if (longest == null)
            {
                longest = mute;
                expiry = longest.GetExpiryTimestamp(false);
                type = mute.Type;
                break;
            }

            DateTimeOffset newExpiry = mute.GetExpiryTimestamp(false);
            if (newExpiry >= expiry)
            {
                longest = mute;
                expiry = newExpiry;
                type |= mute.Type;
            }
        }
        if (longest == null)
            return;

        joining.TimeUnmuted = expiry.LocalDateTime;
        joining.MuteReason = longest.Message;
        joining.MuteType = type;
    }
    public static bool IsValidSteam64Id(CSteamID id)
    {
        return id.m_SteamID / 100000000000000ul == 765;
    }
#if false
    /// <returns>0 for a successful ban.</returns>
    internal static async Task<StandardErrorCode> BanPlayerAsync(ulong targetId, ulong callerId, string reason, int duration, DateTimeOffset timestamp, CancellationToken token = default)
    {
        UCPlayer? target = UCPlayer.FromID(targetId);
        UCPlayer? caller = UCPlayer.FromID(callerId);
        PlayerNames name;
        PlayerNames callerName;
        uint ipv4;
        List<byte[]> hwids = target is not null ? target.SteamPlayer.playerID.GetHwids().ToList() : (await Data.ModerationSql.GetHWIDs(targetId, token).ConfigureAwait(false)).Select(x => x.HWID.ToByteArray()).ToList();
        if (target is not null) // player is online
        {
            CSteamID id = target.Player.channel.owner.playerID.steamID;
            await UCWarfare.ToUpdate();
            target.Player.channel.owner.transportConnection.TryGetIPv4Address(out ipv4);
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

        // LogBanPlayer(targetId, callerId, reason, duration, timestamp);

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
            string time = Localization.GetTimeFromSeconds(duration);
            if (callerId == 0)
            {
                L.Log($"{name.PlayerName} ({targetId}) was banned for {time} by an operator because {reason}.", ConsoleColor.Cyan);
                bool f = false;
                foreach (LanguageSet set in LanguageSet.All())
                {
                    if (f || !set.Language.IsDefault)
                    {
                        time = Localization.GetTimeFromSeconds(duration, set.Language, set.CultureInfo);
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
                    if (f || !set.Language.IsDefault)
                    {
                        time = Localization.GetTimeFromSeconds(duration, set.Language, set.CultureInfo);
                        f = true;
                    }
                    Chat.Broadcast(set, T.BanSuccessBroadcast, target as IPlayer ?? name, caller as IPlayer ?? callerName, time);
                }
                if (f)
                    time = Localization.GetTimeFromSeconds(duration, caller);
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
            Chat.Broadcast(T.KickSuccessBroadcastOperator, target);
        }
        else
        {
            UCPlayer? callerPlayer = UCPlayer.FromID(callerId);
            PlayerNames callerNames = await F.GetPlayerOriginalNamesAsync(callerId, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            L.Log($"{names.PlayerName} ({targetId}) was kicked by {callerNames.PlayerName} ({callerId}) because {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(LanguageSet.AllBut(callerId), T.KickSuccessBroadcast, target, callerPlayer as IPlayer ?? callerNames);
            callerPlayer?.SendChat(T.KickSuccessFeedback, target);
        }

        return StandardErrorCode.Success;
    }
    /// <returns>0 for a successful unban, 2 when the target isn't banned.</returns>
    internal static async Task<StandardErrorCode> UnbanPlayer(ulong targetId, ulong callerId, DateTimeOffset timestamp, CancellationToken token = default)
    {
        PlayerNames targetNames = await F.GetPlayerOriginalNamesAsync(targetId, token).ConfigureAwait(false);
        await UniTask.SwitchToMainThread(token);
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
            await UniTask.SwitchToMainThread(token);
            L.Log($"{targetNames.PlayerName} ({tid}) was unbanned by {callerNames.PlayerName} ({callerId}).", ConsoleColor.Cyan);
            caller?.SendChat(T.UnbanSuccessFeedback, targetNames);
            Chat.Broadcast(LanguageSet.AllBut(callerId), T.UnbanSuccessBroadcast, targetNames, caller as IPlayer ?? callerNames);
        }

        return StandardErrorCode.Success;
    }
    /// <returns>0 for a successful warn, 2 when the target isn't banned.</returns>
    internal static async Task<StandardErrorCode> WarnPlayer(ulong targetId, ulong callerId, string reason, DateTimeOffset timestamp, CancellationToken token = default)
    {
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
            
            ToastMessage.QueueMessage(target, ToastMessage.Popup(T.WarnSuccessTitle.Translate(target, false), T.WarnSuccessDMOperator.Translate(target, false, reason)));

            target.SendChat(T.WarnSuccessDMOperator, reason);
        }
        else
        {
            PlayerNames callerNames = await F.GetPlayerOriginalNamesAsync(callerId, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            L.Log($"{targetNames.PlayerName} ({targetId}) was warned by {callerNames.PlayerName} ({caller}) because {reason}.", ConsoleColor.Cyan);
            IPlayer caller2 = caller as IPlayer ?? callerNames;
            Chat.Broadcast(LanguageSet.AllBut(callerId, targetId), T.WarnSuccessBroadcast, target, caller2);
            caller?.SendChat(T.WarnSuccessFeedback, target);
            
            ToastMessage.QueueMessage(target, ToastMessage.Popup(T.WarnSuccessTitle.Translate(target, false), T.WarnSuccessDM.Translate(target, false, callerNames, reason)));
            target.SendChat(T.WarnSuccessDM, caller2, reason);
        }

        return StandardErrorCode.Success;
    }
    /// <returns>0 for a successful mute.</returns>
    internal static async Task<StandardErrorCode> MutePlayerAsync(ulong target, ulong admin, MuteType type, int duration, string reason, DateTimeOffset timestamp, CancellationToken token = default)
    {
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

        string dur = duration == -1 ? "PERMANENT" : Localization.GetTimeFromSeconds(duration);
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
                foreach (LanguageSet set in LanguageSet.AllBut(target))
                    Chat.Broadcast(set, T.MutePermanentSuccessBroadcast, names, names, type, names2);

                L.Log($"{names.PlayerName} ({target}) was permanently {type} muted for {reason} by {names2.PlayerName} ({admin}).", ConsoleColor.Cyan);
            }
            else
            {
                foreach (LanguageSet set in LanguageSet.AllBut(target))
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
        DateTime now = DateTime.UtcNow;
        if (names.WasFound)
        {
            int rows = await Data.DatabaseManager.NonQueryAsync(
                    "UPDATE `muted` SET `Deactivated` = 1, `DeactivateTimestamp` = @1 WHERE `Steam64` = @0 AND " +
                    "`Deactivated` = 0 AND (`Duration` = -1 OR TIMESTAMPDIFF(SECOND, `Timestamp`, UTC_TIMESTAMP()) < `Duration`)", new object[] { targetId, now })
                .ConfigureAwait(false);

            await UCWarfare.ToUpdate();
            if (rows == 0)
            {
                if (caller is not null)
                    caller.SendChat(T.UnmuteNotMuted, names);
                else if (callerId == 0)
                    L.Log(Util.RemoveRichText(T.UnmuteNotMuted.Translate(Localization.GetDefaultLanguage(), names, out Color color)), Util.GetClosestConsoleColor(color));
                return StandardErrorCode.GenericError;
            }
            else
            {
                onlinePlayer ??= UCPlayer.FromID(targetId);
                if (onlinePlayer is not null)
                {
                    onlinePlayer.MuteReason = null;
                    onlinePlayer.MuteType = MuteType.None;
                    onlinePlayer.TimeUnmuted = DateTime.MinValue;
                }
                LogUnmutePlayer(targetId, callerId, now);
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
                L.Log(Util.RemoveRichText(T.PlayerNotFound.Translate(Localization.GetDefaultLanguage(), out Color color)), Util.GetClosestConsoleColor(color));
            return StandardErrorCode.NotFound;
        }
    }
#endif
    private static void OnPlayerDied(PlayerDied e)
    {
        if (!e.WasTeamkill || e.Killer is null)
            return;

        Asset a = Assets.find(e.PrimaryAsset);
        string itemName = a?.FriendlyName ?? e.PrimaryAsset.ToString("N");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Teamkill log = new Teamkill
        {
            Player = e.Instigator.m_SteamID,
            Actors = new RelatedActor[]
            {
                new RelatedActor(Teamkill.RoleTeamkilled, false, Actors.GetActor(e.Steam64))
            },
            RelevantLogsEnd = now,
            StartedTimestamp = now,
            ResolvedTimestamp = now,
            Message = e.DefaultMessage,
            Cause = e.Cause,
            Reputation = -40,
            Item = e.PrimaryAsset,
            ItemName = itemName,
            Limb = e.Limb,
            Distance = e.KillDistance
        };

        if (e.Killer is { IsOnline: true })
            e.Killer.AddReputation(-40);
        else
            log.PendingReputation = -40;

        UCWarfare.RunTask(Data.ModerationSql.AddOrUpdate, log, CancellationToken.None, ctx: "Log teamkill.");
    }
    // ReSharper restore AutoPropertyCanBeMadeGetOnly.Local
    public static class NetCalls
    {
#if false
        public static readonly NetCall<ulong, ulong, string, int, DateTimeOffset> SendBanRequest = new NetCall<ulong, ulong, string, int, DateTimeOffset>(ReceiveBanRequest);
        public static readonly NetCall<ulong, ulong, DateTimeOffset> SendUnbanRequest = new NetCall<ulong, ulong, DateTimeOffset>(ReceiveUnbanRequest);
        public static readonly NetCall<ulong, ulong, string, DateTimeOffset> SendKickRequest = new NetCall<ulong, ulong, string, DateTimeOffset>(ReceieveKickRequest);
        public static readonly NetCall<ulong, ulong, string, DateTimeOffset> SendWarnRequest = new NetCall<ulong, ulong, string, DateTimeOffset>(ReceiveWarnRequest);
        public static readonly NetCall<ulong, ulong, MuteType, int, string, DateTimeOffset> SendMuteRequest = new NetCall<ulong, ulong, MuteType, int, string, DateTimeOffset>(ReceieveMuteRequest);
        public static readonly NetCall<ulong, ulong, DateTimeOffset> SendUnmuteRequest = new NetCall<ulong, ulong, DateTimeOffset>(ReceieveUnmuteRequest);
        public static readonly NetCall<ulong, ulong, DateTimeOffset, uint, byte, bool> SendIPWhitelistRequest = new NetCall<ulong, ulong, DateTimeOffset, uint, byte, bool>(ReceieveIPWhitelistRequest);
#endif
        public static readonly NetCall<ulong> GrantAdminRequest = new NetCall<ulong>(ReceiveGrantAdmin);
        public static readonly NetCall<ulong> RevokeAdminRequest = new NetCall<ulong>(ReceiveRevokeAdmin);
        public static readonly NetCall<ulong> GrantInternRequest = new NetCall<ulong>(ReceiveGrantIntern);
        public static readonly NetCall<ulong> RevokeInternRequest = new NetCall<ulong>(ReceiveRevokeIntern);
        public static readonly NetCall<ulong> GrantHelperRequest = new NetCall<ulong>(ReceiveGrantHelper);
        public static readonly NetCall<ulong> RevokeHelperRequest = new NetCall<ulong>(ReceiveRevokeHelper);

        public static readonly NetCall<ulong, ulong, string, int, DateTimeOffset> SendPlayerBanned = new NetCall<ulong, ulong, string, int, DateTimeOffset>(KnownNetMessage.SendPlayerBanned);
        public static readonly NetCall<ulong, ulong, DateTimeOffset> SendPlayerUnbanned = new NetCall<ulong, ulong, DateTimeOffset>(KnownNetMessage.SendPlayerUnbanned);
        public static readonly NetCall<ulong, ulong, string, DateTimeOffset> SendPlayerKicked = new NetCall<ulong, ulong, string, DateTimeOffset>(KnownNetMessage.SendPlayerKicked);
        public static readonly NetCall<ulong, ulong, string, DateTimeOffset> SendPlayerWarned = new NetCall<ulong, ulong, string, DateTimeOffset>(KnownNetMessage.SendPlayerWarned);
        public static readonly NetCall<ulong, string, DateTimeOffset> SendPlayerBattleyeKicked = new NetCall<ulong, string, DateTimeOffset>(KnownNetMessage.SendPlayerBattleyeKicked);
        public static readonly NetCall<ulong, ulong, string, string, DateTimeOffset> SendTeamkill = new NetCall<ulong, ulong, string, string, DateTimeOffset>(KnownNetMessage.SendTeamkill);
        public static readonly NetCall<ulong, ulong, MuteType, int, string, DateTimeOffset> SendPlayerMuted = new NetCall<ulong, ulong, MuteType, int, string, DateTimeOffset>(KnownNetMessage.SendPlayerMuted);
        public static readonly NetCall<ulong, ushort, string, DateTimeOffset> SendVehicleTeamkilled = new NetCall<ulong, ushort, string, DateTimeOffset>(KnownNetMessage.SendVehicleTeamkilled);
        public static readonly NetCall<ulong, ulong, DateTimeOffset> SendPlayerUnmuted = new NetCall<ulong, ulong, DateTimeOffset>(KnownNetMessage.SendPlayerUnmuted);
        public static readonly NetCall<ulong, ulong, DateTimeOffset, uint, byte, bool> SendPlayerIPWhitelisted = new NetCall<ulong, ulong, DateTimeOffset, uint, byte, bool>(KnownNetMessage.SendPlayerIPWhitelisted);
#if false
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.SendBanRequest)]
        internal static async Task ReceiveBanRequest(MessageContext context, ulong target, ulong admin, string reason, int duration, DateTimeOffset timestamp)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await BanPlayerAsync(target, admin, reason, duration, timestamp));
        }

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.SendUnbanRequest)]
        internal static async Task ReceiveUnbanRequest(MessageContext context, ulong target, ulong admin, DateTimeOffset timestamp)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await UnbanPlayer(target, admin, timestamp));
        }

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.SendKickRequest)]
        internal static async Task ReceieveKickRequest(MessageContext context, ulong target, ulong admin, string reason, DateTimeOffset timestamp)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await KickPlayer(target, admin, reason, timestamp));
        }

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.SendMuteRequest)]
        internal static async Task ReceieveMuteRequest(MessageContext context, ulong target, ulong admin, MuteType type, int duration, string reason, DateTimeOffset timestamp)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await MutePlayerAsync(target, admin, type, duration, reason, timestamp));
        }

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.SendUnmuteRequest)]
        internal static async Task ReceieveUnmuteRequest(MessageContext context, ulong target, ulong admin, DateTimeOffset timestamp)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await UnmutePlayerAsync(target, admin, timestamp));
        }
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.SendIPWhitelistRequest)]
        internal static async Task ReceieveIPWhitelistRequest(MessageContext context, ulong target, ulong admin, DateTimeOffset timestamp, uint ip, byte mask, bool add)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await WhitelistIP(target, admin, new IPv4Range(ip, mask), add, timestamp));
        }

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.SendWarnRequest)]
        internal static async Task ReceiveWarnRequest(MessageContext context, ulong target, ulong admin, string reason, DateTimeOffset timestamp)
        {
            await UCWarfare.ToUpdate();
            context.Acknowledge(await WarnPlayer(target, admin, reason, timestamp));
        }
#endif

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.GrantAdminRequest)]
        internal static void ReceiveGrantAdmin(MessageContext context, ulong player)
        {
            PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.ADMIN);
        }

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RevokeAdminRequest)]
        internal static void ReceiveRevokeAdmin(MessageContext context, ulong player)
        {
            RevokeAll(player);
        }

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.GrantInternRequest)]
        internal static void ReceiveGrantIntern(MessageContext context, ulong player)
        {
            PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.TRIAL_ADMIN);
        }

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RevokeInternRequest)]
        internal static void ReceiveRevokeIntern(MessageContext context, ulong player)
        {
            RevokeAll(player);
        }

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.GrantHelperRequest)]
        internal static void ReceiveGrantHelper(MessageContext context, ulong player)
        {
            PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.HELPER);
        }

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RevokeHelperRequest)]
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