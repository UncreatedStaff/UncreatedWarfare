using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;
using SteamGameServerNetworkingUtils = SDG.Unturned.SteamGameServerNetworkingUtils;

namespace Uncreated.Warfare.Commands.VanillaRework;

public class BanCommand : Command
{
    private const string SYNTAX = "/ban <player> <duration minutes> <reason ...>";
    public BanCommand() : base("ban", EAdminType.MODERATOR, 1) { }
    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertArgs(3, SYNTAX);

        if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target))
            throw ctx.Reply(T.PlayerNotFound);
        int duration = F.ParseTime(ctx.Get(1)!);

        if (duration == 0 || duration < -1)
            throw ctx.Reply(T.InvalidTime);

        string? reason = ctx.GetRange(2);
        if (string.IsNullOrEmpty(reason))
            throw ctx.Reply(T.NoReasonProvided);
        FPlayerName name;
        FPlayerName callerName;
        uint ipv4;
        Task.Run(async () =>
        {
            List<byte[]> hwids = await OffenseManager.GetAllHWIDs(targetId);
            await UCWarfare.ToUpdate();
            if (target is not null) // player is online
            {
                CSteamID id = target.Player.channel.owner.playerID.steamID;
                ipv4 = SteamGameServerNetworkingUtils.getIPv4AddressOrZero(id);
                name = F.GetPlayerOriginalNames(target);
                Provider.requestBanPlayer(Provider.server, id, ipv4, hwids, reason, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration));
            }
            else
            {
                ipv4 = Data.DatabaseManager.GetPackedIP(targetId);
                name = F.GetPlayerOriginalNames(targetId);
                F.OfflineBan(targetId, ipv4, ctx.Caller == null ? CSteamID.Nil : ctx.Caller.Player.channel.owner.playerID.steamID,
                    reason!, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration), hwids.ToArray());
            }
            if (ctx.Caller is not null)
                callerName = F.GetPlayerOriginalNames(ctx.Caller);
            else
                callerName = FPlayerName.Console;
            ActionLogger.Add(EActionLogType.BAN_PLAYER, $"BANNED {targetId.ToString(Data.Locale)} FOR \"{reason}\" DURATION: " +
                (duration == -1 ? "PERMANENT" : duration.ToString(Data.Locale) + " SECONDS"), ctx.CallerID);
            
            OffenseManager.LogBanPlayer(targetId, ctx.CallerID, reason!, duration, DateTime.Now);
            await UCWarfare.ToUpdate();

            if (duration == -1)
            {
                if (ctx.IsConsole)
                {
                    ctx.ReplyString($"{name.PlayerName} ({targetId.ToString(Data.Locale)} was permanently banned by an operator because: {reason!}", ConsoleColor.Cyan);
                    Chat.Broadcast(T.BanPermanentSuccessBroadcastOperator, name);
                }
                else
                {
                    L.Log($"{name.PlayerName} ({targetId.ToString(Data.Locale)}) was banned by {callerName.PlayerName} ({ctx.CallerID}) because: {reason!}.", ConsoleColor.Cyan);
                    Chat.Broadcast(LanguageSet.AllBut(ctx.CallerID), T.BanPermanentSuccessBroadcast, name, callerName);
                    ctx.Reply(T.BanPermanentSuccessFeedback, name);
                }
            }
            else
            {
                string time = Localization.GetTimeFromSeconds(duration, L.DEFAULT);
                if (ctx.IsConsole)
                {
                    L.Log($"{name.PlayerName} ({targetId.ToString(Data.Locale)}) was banned by an operator for {time} because: {reason}.", ConsoleColor.Cyan);
                    bool f = false;
                    foreach (LanguageSet set in LanguageSet.All())
                    {
                        if (f || !set.Language.Equals(L.DEFAULT, StringComparison.Ordinal))
                        {
                            time = Localization.GetTimeFromSeconds(duration, set.Language);
                            f = true;
                        }
                        Chat.Broadcast(set, T.BanSuccessBroadcastOperator, name, time);
                    }
                }
                else
                {
                    L.Log($"{name.PlayerName} ({targetId}) was banned by {callerName.PlayerName} ({ctx.CallerID.ToString(Data.Locale)}) for {time} because: {reason}.", ConsoleColor.Cyan);
                    bool f = false;
                    foreach (LanguageSet set in LanguageSet.AllBut(ctx.CallerID))
                    {
                        if (f || !set.Language.Equals(L.DEFAULT, StringComparison.Ordinal))
                        {
                            time = Localization.GetTimeFromSeconds(duration, set.Language);
                            f = true;
                        }
                        Chat.Broadcast(set, T.BanSuccessBroadcast, name, callerName, time);
                    }
                    if (f)
                        time = Localization.GetTimeFromSeconds(duration, ctx.CallerID);
                    else if (Data.Languages.TryGetValue(ctx.CallerID, out string lang) && !lang.Equals(L.DEFAULT, StringComparison.Ordinal))
                        time = Localization.GetTimeFromSeconds(duration, lang);
                    ctx.Reply(T.BanSuccessFeedback, name, time);
                }
            }
        });
        ctx.Defer();
    }
}