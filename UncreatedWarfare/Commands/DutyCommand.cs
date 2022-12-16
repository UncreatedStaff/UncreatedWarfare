using SDG.Unturned;
using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Commands.Permissions;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class DutyCommand : Command
{
    private const string Syntax = "/duty";
    private const string Help = "Swap your duty status between on and off. For admins and trial admins.";

    public DutyCommand() : base("duty", EAdminType.TRIAL_ADMIN | EAdminType.ADMIN) { }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, Syntax + " - " + Help);

        EAdminType level = ctx.Caller.PermissionLevel;

        switch (level)
        {
            default:
                throw ctx.SendNoPermission();
            case EAdminType.ADMIN_OFF_DUTY:
                AdminOffToOn(ctx.Caller);
                throw ctx.Defer();
            case EAdminType.ADMIN_ON_DUTY:
                AdminOnToOff(ctx.Caller);
                throw ctx.Defer();
            case EAdminType.TRIAL_ADMIN_OFF_DUTY:
                InternOffToOn(ctx.Caller);
                throw ctx.Defer();
            case EAdminType.TRIAL_ADMIN_ON_DUTY:
                InternOnToOff(ctx.Caller);
                throw ctx.Defer();
        }
    }
    public static void AdminOffToOn(UCPlayer player)
    {
        L.Log($"{player.Name.PlayerName} ({player.Steam64.ToString(Data.AdminLocale)}) went on duty.", ConsoleColor.Cyan);
        PermissionSaver.Instance.SetPlayerPermissionLevel(player.Steam64, EAdminType.ADMIN_ON_DUTY);
        player.Player.look.sendFreecamAllowed(true);
        player.Player.look.sendWorkzoneAllowed(true);
        player.SendChat(T.DutyOnFeedback);
        Chat.Broadcast(LanguageSet.AllBut(player.Steam64), T.DutyOnBroadcast, player);
        Signs.UpdateKitSigns(player, null);
        Signs.UpdateLoadoutSigns(player);
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, true);
        ActionLogger.Add(EActionLogType.DUTY_CHANGED, "ON DUTY", player.CSteamID.m_SteamID);
    }
    public static void AdminOnToOff(UCPlayer player)
    {
        L.Log($"{player.Name.PlayerName} ({player.Steam64.ToString(Data.AdminLocale)}) went off duty.", ConsoleColor.Cyan);
        PermissionSaver.Instance.SetPlayerPermissionLevel(player.Steam64, EAdminType.ADMIN_OFF_DUTY);
        Chat.Broadcast(LanguageSet.AllBut(player.Steam64), T.DutyOffBroadcast, player);
        SetVanishMode(player, false);
        player.GodMode = false;
        if (player.Player != null && player.Player.look != null)
        {
            player.Player.look.sendFreecamAllowed(false);
            player.Player.look.sendWorkzoneAllowed(false);
            player.SendChat(T.DutyOffFeedback);
            Signs.UpdateKitSigns(player, null);
            Signs.UpdateLoadoutSigns(player);
        }
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, false);
        ActionLogger.Add(EActionLogType.DUTY_CHANGED, "OFF DUTY", player.CSteamID.m_SteamID);
    }
    public static void InternOffToOn(UCPlayer player)
    {
        L.Log($"{player.Name.PlayerName} ({player.Steam64.ToString(Data.AdminLocale)}) went on duty.", ConsoleColor.Cyan);
        PermissionSaver.Instance.SetPlayerPermissionLevel(player.Steam64, EAdminType.TRIAL_ADMIN_ON_DUTY);
        player.SendChat(T.DutyOnFeedback);
        Chat.Broadcast(LanguageSet.AllBut(player.Steam64), T.DutyOnBroadcast, player);
        Signs.UpdateKitSigns(player, null);
        Signs.UpdateLoadoutSigns(player);
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, true);
        ActionLogger.Add(EActionLogType.DUTY_CHANGED, "ON DUTY", player.CSteamID.m_SteamID);
    }
    public static void InternOnToOff(UCPlayer player)
    {
        L.Log($"{player.Name.PlayerName} ({player.Steam64.ToString(Data.AdminLocale)}) went off duty.", ConsoleColor.Cyan);
        PermissionSaver.Instance.SetPlayerPermissionLevel(player.Steam64, EAdminType.TRIAL_ADMIN_OFF_DUTY);
        Chat.Broadcast(LanguageSet.AllBut(player.Steam64), T.DutyOffBroadcast, player);
        SetVanishMode(player, false);
        player.GodMode = false;
        if (player.Player != null)
        {
            player.SendChat(T.DutyOffFeedback);
            Signs.UpdateKitSigns(player, null);
            Signs.UpdateLoadoutSigns(player);
        }
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, false);
        ActionLogger.Add(EActionLogType.DUTY_CHANGED, "OFF DUTY", player.CSteamID.m_SteamID);
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