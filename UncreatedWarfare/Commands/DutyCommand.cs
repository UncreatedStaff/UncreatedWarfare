using SDG.Unturned;
using System;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Kits;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class DutyCommand : Command
{
    private const string SYNTAX = "/duty";
    private const string HELP = "Swap your duty status between on and off. For admins and trial admins.";

    public DutyCommand() : base("duty", EAdminType.TRIAL_ADMIN | EAdminType.ADMIN) { }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        FPlayerName names = F.GetPlayerOriginalNames(ctx.Caller!);

        EAdminType level = ctx.Caller.PermissionLevel;

        switch (level)
        {
            default:
                throw ctx.SendNoPermission();
            case EAdminType.ADMIN_OFF_DUTY:
                AdminOffToOn(ctx.Caller, names);
                throw ctx.Defer();
            case EAdminType.ADMIN_ON_DUTY:
                AdminOnToOff(ctx.Caller, names);
                throw ctx.Defer();
            case EAdminType.TRIAL_ADMIN_OFF_DUTY:
                InternOffToOn(ctx.Caller, names);
                throw ctx.Defer();
            case EAdminType.TRIAL_ADMIN_ON_DUTY:
                InternOnToOff(ctx.Caller, names);
                throw ctx.Defer();
        }
    }
    public static void AdminOffToOn(UCPlayer player, FPlayerName names)
    {
        L.Log(Localization.Translate("duty_admin_on_console", 0, out _, names.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
        PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.ADMIN_ON_DUTY);
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
        L.Log(Localization.Translate("duty_admin_off_console", 0, out _, names.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
        PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.ADMIN_OFF_DUTY);
        Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "duty_off_broadcast", names.CharacterName);
        SetVanishMode(player, false);
        player.GodMode = false;
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
        L.Log(Localization.Translate("duty_intern_on_console", 0, out _, names.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
        PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.TRIAL_ADMIN_ON_DUTY);
        player.SendChat("duty_on_feedback");
        Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "duty_on_broadcast", names.CharacterName);
        RequestSigns.UpdateAllSigns(player.Player.channel.owner);
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, true);
        ActionLog.Add(EActionLogType.DUTY_CHANGED, "ON DUTY", player.CSteamID.m_SteamID);
    }
    public static void InternOnToOff(UCPlayer player, FPlayerName names)
    {
        L.Log(Localization.Translate("duty_intern_off_console", 0, out _, names.PlayerName, names.Steam64.ToString(Data.Locale)), ConsoleColor.Cyan);
        PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.TRIAL_ADMIN_OFF_DUTY);
        Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "duty_off_broadcast", names.CharacterName);
        SetVanishMode(player, false);
        player.GodMode = false;
        if (player.Player != null)
        {
            player.SendChat("duty_off_feedback");
            RequestSigns.UpdateAllSigns(player.Player.channel.owner);
        }
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, false);
        ActionLog.Add(EActionLogType.DUTY_CHANGED, "OFF DUTY", player.CSteamID.m_SteamID);
    }
    public static void SetVanishMode(Player player, bool vanished)
    {
        if (player.movement.canAddSimulationResultsToUpdates == vanished)
        {
            player.movement.canAddSimulationResultsToUpdates = !vanished;
            player.movement.updates.Add(vanished
                ? new PlayerStateUpdate(Vector3.zero, 0, 0)
                : new PlayerStateUpdate(player.transform.position, player.look.angle, player.look.rot));
        }
    }
}