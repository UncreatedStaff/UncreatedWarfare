using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;

namespace Uncreated.Warfare.Commands.VanillaRework;

public class BanCommand : AsyncCommand
{
    private const string Syntax = "/ban <player> <duration> <reason ...>";

    public BanCommand() : base("ban", EAdminType.MODERATOR, 1)
    {
        Structure = new CommandStructure
        {
            Description = "Prevents a player from joining the server for a specified amount of time.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Player", typeof(IPlayer))
                {
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Duration", typeof(TimeSpan), "Permanent")
                        {
                            Aliases = new string[] { "perm" },
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Reason", typeof(string))
                                {
                                    IsRemainder = true
                                }
                            }
                        }
                    }
                }
            }
        };
    }
    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertArgs(3, Syntax);

        if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target))
            throw ctx.Reply(T.PlayerNotFound);
        int duration = Util.ParseTime(ctx.Get(1)!);

        if (duration == 0 || duration < -1)
            throw ctx.Reply(T.InvalidTime);

        string? reason = ctx.GetRange(2);
        if (string.IsNullOrEmpty(reason))
            throw ctx.Reply(T.NoReasonProvided);
        PlayerNames name;
        uint ipv4;
        List<byte[]> hwids = await OffenseManager.GetAllHWIDs(targetId, token).ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);

        if (target is not null && target.IsOnline) // player is online
        {
            CSteamID id = target.Player.channel.owner.playerID.steamID;
            target.Player.channel.owner.transportConnection.TryGetIPv4Address(out ipv4);
            name = target.Name;
            Provider.requestBanPlayer(Provider.server, id, ipv4, hwids, reason, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration));
        }
        else
        {
            ipv4 = await Data.DatabaseManager.TryGetPackedIPAsync(targetId, token).ConfigureAwait(false);
            name = await F.GetPlayerOriginalNamesAsync(targetId, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            F.OfflineBan(targetId, ipv4, ctx.Caller == null ? CSteamID.Nil : ctx.Caller.Player.channel.owner.playerID.steamID,
                reason!, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration), hwids.ToArray());
        }
        PlayerNames callerName = ctx.Caller is not null ? ctx.Caller.Name : PlayerNames.Console;
        ActionLog.Add(ActionLogType.BanPlayer, $"BANNED {targetId.ToString(Data.AdminLocale)} FOR \"{reason}\" DURATION: " +
            (duration == -1 ? "PERMANENT" : duration.ToString(Data.AdminLocale) + " SECONDS"), ctx.CallerID);

        OffenseManager.LogBanPlayer(targetId, ctx.CallerID, reason!, duration, DateTime.Now);

        if (duration == -1)
        {
            if (ctx.IsConsole)
            {
                ctx.ReplyString($"{name.PlayerName} ({targetId.ToString(Data.LocalLocale)}) was permanently banned by an operator because: {reason!}", ConsoleColor.Cyan);
                Chat.Broadcast(T.BanPermanentSuccessBroadcastOperator, name);
            }
            else
            {
                L.Log($"{name.PlayerName} ({targetId.ToString(Data.AdminLocale)}) was banned by {callerName.PlayerName} ({ctx.CallerID}) because: {reason!}.", ConsoleColor.Cyan);
                Chat.Broadcast(LanguageSet.AllBut(ctx.CallerID), T.BanPermanentSuccessBroadcast, name, callerName);
                ctx.Reply(T.BanPermanentSuccessFeedback, name);
            }
        }
        else
        {
            string time = duration.GetTimeFromSeconds(L.Default);
            if (ctx.IsConsole)
            {
                L.Log($"{name.PlayerName} ({targetId.ToString(Data.AdminLocale)}) was banned by an operator for {time} because: {reason}.", ConsoleColor.Cyan);
                bool f = false;
                foreach (LanguageSet set in LanguageSet.All())
                {
                    if (f || !set.Language.Equals(L.Default, StringComparison.Ordinal))
                    {
                        time = duration.GetTimeFromSeconds(set.Language);
                        f = true;
                    }
                    Chat.Broadcast(set, T.BanSuccessBroadcastOperator, name, time);
                }
            }
            else
            {
                L.Log($"{name.PlayerName} ({targetId}) was banned by {callerName.PlayerName} ({ctx.CallerID.ToString(Data.AdminLocale)}) for {time} because: {reason}.", ConsoleColor.Cyan);
                bool f = false;
                foreach (LanguageSet set in LanguageSet.AllBut(ctx.CallerID))
                {
                    if (f || !set.Language.Equals(L.Default, StringComparison.Ordinal))
                    {
                        time = duration.GetTimeFromSeconds(set.Language);
                        f = true;
                    }
                    Chat.Broadcast(set, T.BanSuccessBroadcast, name, callerName, time);
                }
                if (f)
                    time = duration.GetTimeFromSeconds(ctx.CallerID);
                else if (Data.Languages.TryGetValue(ctx.CallerID, out string lang) && !lang.Equals(L.Default, StringComparison.Ordinal))
                    time = duration.GetTimeFromSeconds(lang);
                ctx.Reply(T.BanSuccessFeedback, name, time);
            }
        }
    }
}