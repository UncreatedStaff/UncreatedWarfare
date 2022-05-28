using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Unturned.Player;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Commands;

class DutyCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "duty";
    public string Help => "Go on or off duty.";
    public string Syntax => "/duty";
    private readonly List<string> _aliases = new List<string>(0);
    public List<string> Aliases => _aliases;
    private readonly List<string> _permissions = new List<string>(1) { "uc.duty" };
		public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CommandContext ctx = new CommandContext(caller, command);
        if (ctx.IsConsole)
        {
            ctx.SendPlayerOnlyError();
            return;
        }
        FPlayerName names = F.GetPlayerOriginalNames(ctx.Caller!);
        List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(ctx.Caller, false);
        if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup))
        {
            AdminOffToOn(ctx.Caller!, names);
        }
        else if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup))
        {
            AdminOnToOff(ctx.Caller!, names);
        }
        else if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup))
        {
            InternOffToOn(ctx.Caller!, names);
        }
        else if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup))
        {
            InternOnToOff(ctx.Caller!, names);
        }
        else
            ctx.SendNoPermission();
    }
    public static void AdminOffToOn(UCPlayer player, FPlayerName names)
    {
        L.Log(Translation.Translate("duty_admin_on_console", 0, out _, names.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
        R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, player);
        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, player);
        player.Player.look.sendFreecamAllowed(true);
        player.Player.look.sendWorkzoneAllowed(true);
        player.SendChat("duty_on_feedback");
        Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "duty_on_broadcast", names.CharacterName);
        RequestSigns.UpdateAllSigns(player.Player.channel.owner);
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, true);
        ActionLog.Add(EActionLogType.DUTY_CHANGED, "ON DUTY", player.CSteamID.m_SteamID);
    }
    public static void AdminOnToOff(UCPlayer player, FPlayerName names)
    {
        L.Log(Translation.Translate("duty_admin_off_console", 0, out _, names.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
        R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, player);
        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, player);
        Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "duty_off_broadcast", names.CharacterName);
        if (player.Player.TryGetComponent(out UnturnedPlayerFeatures features))
        {
            features.GodMode = false;
            features.VanishMode = false;
        }
        if (player.Player != null && player.Player.look != null)
        {
            player.Player.look.sendFreecamAllowed(false);
            player.Player.look.sendWorkzoneAllowed(false);
            player.SendChat("duty_off_feedback");
            RequestSigns.UpdateAllSigns(player.Player.channel.owner);
        }
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, false);
        ActionLog.Add(EActionLogType.DUTY_CHANGED, "OFF DUTY", player.CSteamID.m_SteamID);
    }
    public static void InternOffToOn(UCPlayer player, FPlayerName names)
    {
        L.Log(Translation.Translate("duty_intern_on_console", 0, out _, names.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
        R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, player);
        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, player);
        player.SendChat("duty_on_feedback");
        Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "duty_on_broadcast", names.CharacterName);
        RequestSigns.UpdateAllSigns(player.Player.channel.owner);
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, true);
        ActionLog.Add(EActionLogType.DUTY_CHANGED, "ON DUTY", player.CSteamID.m_SteamID);
    }
    public static void InternOnToOff(UCPlayer player, FPlayerName names)
    {
        L.Log(Translation.Translate("duty_intern_off_console", 0, out _, names.PlayerName, names.Steam64.ToString(Data.Locale)), ConsoleColor.Cyan);
        R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, player);
        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, player);
        Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "duty_off_broadcast", names.CharacterName);
        if (player.Player.TryGetComponent(out UnturnedPlayerFeatures features))
        {
            features.GodMode = false;
            features.VanishMode = false;
        }
        if (player.Player != null)
        {
            player.SendChat("duty_off_feedback");
            RequestSigns.UpdateAllSigns(player.Player.channel.owner);
        }
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, false);
        ActionLog.Add(EActionLogType.DUTY_CHANGED, "OFF DUTY", player.CSteamID.m_SteamID);
    }
}