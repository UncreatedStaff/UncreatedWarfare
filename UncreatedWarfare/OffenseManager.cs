using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Players;
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
    // calls the type initializer
    internal static void Init()
    {
#if false
        EventDispatcher.PlayerDied += OnPlayerDied;
        EventDispatcher.PlayerJoined += OnPlayerJoined;
        EventDispatcher.PlayerPendingAsync += OnPlayerPending;
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
#if false
        EventDispatcher.PlayerPendingAsync -= OnPlayerPending;
        EventDispatcher.PlayerJoined -= OnPlayerJoined;
        EventDispatcher.PlayerDied -= OnPlayerDied;
#endif
    }
    public static HWID[] ConvertVanillaHWIDs(IEnumerable<byte[]> hwids)
    {
        byte[][] arr = hwids.ToArrayFast();
        HWID[] outArray = new HWID[arr.Length];
        for (int i = 0; i < arr.Length; i++)
            outArray[i] = new HWID(arr[i]);
        
        return outArray;
    }
#if false
    public static void OnModerationEntryUpdated(ModerationEntry entry)
    {
        L.LogDebug(JsonSerializer.Serialize(entry, JsonEx.serializerSettings));

        WarfarePlayer? player = WarfarePlayer.FromID(entry.Player);
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

            if (WarfarePlayer.FromID(entry.Player) is { } pl)
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
#endif
    public static bool IsValidSteam64Id(CSteamID id)
    {
        return id.m_SteamID / 100000000000000ul == 765;
    }
#if false
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
#endif
}