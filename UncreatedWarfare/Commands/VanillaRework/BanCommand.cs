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
            throw ctx.Reply("ban_no_player_found", ctx.Parameters[0]);
        int duration = F.ParseTime(ctx.Get(1)!);

        if (duration == 0 || duration < -1)
            throw ctx.Reply("ban_invalid_number", ctx.Parameters[1]);

        string? reason = ctx.GetRange(2);
        if (string.IsNullOrEmpty(reason))
            throw ctx.Reply("ban_no_reason_provided", ctx.Parameters[1]);
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
            ActionLog.Add(EActionLogType.BAN_PLAYER, $"BANNED {targetId.ToString(Data.Locale)} FOR \"{reason}\" DURATION: " +
                (duration == -1 ? "PERMANENT" : duration.ToString(Data.Locale) + " SECONDS"), ctx.CallerID);

            // TODO Convert database to seconds!!!

            OffenseManager.LogBanPlayer(targetId, ctx.CallerID, reason!, duration, DateTime.Now);
            await UCWarfare.ToUpdate();

            if (duration == -1)
            {
                if (ctx.IsConsole)
                {
                    L.Log(Translation.Translate("ban_permanent_console_operator", JSONMethods.DEFAULT_LANGUAGE, out _, name.PlayerName, targetId.ToString(Data.Locale), reason!), ConsoleColor.Cyan);
                    Chat.Broadcast("ban_permanent_broadcast_operator", name.CharacterName);
                }
                else
                {
                    L.Log(Translation.Translate("ban_permanent_console", 0, out _, name.PlayerName, targetId.ToString(Data.Locale), callerName.PlayerName,
                        ctx.CallerID.ToString(Data.Locale), reason!), ConsoleColor.Cyan);
                    Chat.BroadcastToAllExcept(ctx.CallerID, "ban_permanent_broadcast", name.CharacterName, callerName.CharacterName);
                    ctx.Reply("ban_permanent_feedback", name.CharacterName);
                }
            }
            else
            {
                string time = duration.GetTimeFromSeconds(JSONMethods.DEFAULT_LANGUAGE);
                if (ctx.IsConsole)
                {
                    L.Log(Translation.Translate("ban_console_operator", JSONMethods.DEFAULT_LANGUAGE, out _, name.PlayerName, targetId.ToString(Data.Locale), reason!, time), ConsoleColor.Cyan);
                    bool f = false;
                    foreach (LanguageSet set in Translation.EnumerateLanguageSets())
                    {
                        if (f || !set.Language.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal))
                        {
                            time = duration.GetTimeFromSeconds(set.Language);
                            f = true;
                        }
                        Chat.Broadcast(set, "ban_broadcast_operator", name.PlayerName, time);
                    }
                }
                else
                {
                    L.Log(Translation.Translate("ban_console", 0, out _, name.PlayerName, targetId.ToString(Data.Locale), callerName.PlayerName,
                        ctx.CallerID.ToString(Data.Locale), reason!, time), ConsoleColor.Cyan);
                    bool f = false;
                    foreach (LanguageSet set in Translation.EnumerateLanguageSetsExclude(ctx.CallerID))
                    {
                        if (f || !set.Language.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal))
                        {
                            time = duration.GetTimeFromSeconds(set.Language);
                            f = true;
                        }
                        Chat.Broadcast(set, "ban_broadcast", name.CharacterName, callerName.CharacterName, time);
                    }
                    if (f)
                        time = duration.GetTimeFromSeconds(ctx.CallerID);
                    else if (Data.Languages.TryGetValue(ctx.CallerID, out string lang) && !lang.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal))
                        time = duration.GetTimeFromSeconds(lang);
                    ctx.Reply("ban_feedback", name.CharacterName, time);
                }
            }
        });
        ctx.Defer();
    }
}