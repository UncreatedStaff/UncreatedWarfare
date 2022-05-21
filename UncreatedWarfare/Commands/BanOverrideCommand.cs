using Rocket.API;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Players;
using SteamGameServerNetworkingUtils = SDG.Unturned.SteamGameServerNetworkingUtils;

namespace Uncreated.Warfare.Commands;

public class BanOverrideCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "ban";
    public string Help => "Ban players who are misbehaving.";
    public string Syntax => "/ban <player> <duration minutes> [reason] ";
    private readonly List<string> _aliases = new List<string>(0);
    public List<string> Aliases => _aliases;
    private readonly List<string> _permissions = new List<string>(1) { "uc.ban" };
	public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
        CommandContext ctx = new CommandContext(caller, command);
        if (!ctx.HasArgs(3))
        {
            ctx.Reply("ban_syntax");
        }
        else if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target))
        {
            ctx.Reply("ban_no_player_found", ctx.Parameters[0]);
        }
        else
        {
            if (!ctx.TryGet(1, out int duration) || duration < 1)
            {
                if (ctx.MatchParameterPartial(1, "perm"))
                    duration = -1;
                else
                {
                    ctx.Reply("ban_invalid_number", ctx.Parameters[1]);
                    return;
                }
            }
            string? reason = ctx.GetRange(2);
            if (string.IsNullOrEmpty(reason))
            {
                ctx.Reply("ban_no_reason_provided", ctx.Parameters[1]);
            }
            else
            {
                FPlayerName name;
                FPlayerName callerName;
                uint ipv4;
                if (target is not null) // player is online
                {
                    CSteamID id = target.Player.channel.owner.playerID.steamID;
                    ipv4 = SteamGameServerNetworkingUtils.getIPv4AddressOrZero(id);
                    name = F.GetPlayerOriginalNames(target);
                    Provider.requestBanPlayer(Provider.server, id, ipv4, reason, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration) * 60);
                }
                else
                {
                    ipv4 = Data.DatabaseManager.GetPackedIP(targetId);
                    name = F.GetPlayerOriginalNames(targetId);
                    F.OfflineBan(targetId, ipv4, ctx.Caller == null ? CSteamID.Nil : ctx.Caller.Player.channel.owner.playerID.steamID,
                        reason!, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration) * 60);
                }
                if (ctx.Caller is not null)
                    callerName = F.GetPlayerOriginalNames(ctx.Caller);
                else
                    callerName = FPlayerName.Console;
                ActionLog.Add(EActionLogType.BAN_PLAYER, $"BANNED {targetId.ToString(Data.Locale)} FOR \"{reason}\" DURATION: " +
                    (duration == -1 ? "PERMANENT" : duration.ToString(Data.Locale)), ctx.CallerID);
                if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                {
                    Data.DatabaseManager.AddBan(targetId, ctx.CallerID, duration, reason!);
                    OffenseManager.NetCalls.SendPlayerBanned.NetInvoke(targetId, ctx.CallerID, reason!, duration, DateTime.Now);
                }
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
                    string time = Translation.GetTimeFromMinutes(duration, JSONMethods.DEFAULT_LANGUAGE);
                    if (ctx.IsConsole)
                    {
                        L.Log(Translation.Translate("ban_console_operator", JSONMethods.DEFAULT_LANGUAGE, out _, name.PlayerName, targetId.ToString(Data.Locale), reason!, time), ConsoleColor.Cyan);
                        bool f = false;
                        foreach (LanguageSet set in Translation.EnumerateLanguageSets())
                        {
                            if (f || !set.Language.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal))
                            {
                                time = duration.GetTimeFromMinutes(set.Language);
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
                                time = duration.GetTimeFromMinutes(set.Language);
                                f = true;
                            }
                            Chat.Broadcast(set, "ban_broadcast", name.CharacterName, callerName.CharacterName, time);
                        }
                        if (f)
                            time = duration.GetTimeFromMinutes(ctx.CallerID);
                        else if (Data.Languages.TryGetValue(ctx.CallerID, out string lang) && !lang.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal))
                            time = duration.GetTimeFromMinutes(lang);
                        ctx.Reply("ban_feedback", name.CharacterName, time);
                    }
                }
            }
        }
    }
}