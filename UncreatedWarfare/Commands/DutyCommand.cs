using SDG.Unturned;
using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class DutyCommand : Command
{
    private const string Syntax = "/duty";
    private const string Help = "Swap your duty status between on and off. For admins and trial admins.";

    public DutyCommand() : base("duty", EAdminType.TRIAL_ADMIN | EAdminType.ADMIN)
    {
        AddAlias("onduty");
        AddAlias("offduty");
        AddAlias("d");
        Structure = new CommandStructure
        {
            Description = "Switch between being on and off duty."
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
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
    private static void ClearAdminPermissions(UCPlayer player)
    {
        if (player.Player != null)
        {
            if (player.Player.look != null)
            {
                player.Player.look.sendFreecamAllowed(false);
                player.Player.look.sendWorkzoneAllowed(false);
            }

            if (player.Player.movement != null)
            {
                if (player.Player.movement.pluginSpeedMultiplier != 1f)
                    player.Player.movement.sendPluginSpeedMultiplier(1f);
                if (player.Player.movement.pluginJumpMultiplier != 1f)
                    player.Player.movement.sendPluginJumpMultiplier(1f);
            }
        }

        player.JumpOnPunch = false;
        SetVanishMode(player, false);
        player.GodMode = false;

        Signs.UpdateKitSigns(player, null);
        Signs.UpdateLoadoutSigns(player);
    }
    private static void GiveAdminPermissions(UCPlayer player, bool isIntern)
    {
        if (player.Player.look != null)
        {
            player.Player.look.sendFreecamAllowed(!isIntern);
            player.Player.look.sendWorkzoneAllowed(!isIntern);
        }
        Signs.UpdateKitSigns(player, null);
        Signs.UpdateLoadoutSigns(player);
    }
    public static void AdminOffToOn(UCPlayer player)
    {
        L.Log($"{player.Name.PlayerName} ({player.Steam64.ToString(Data.AdminLocale)}) went on duty.", ConsoleColor.Cyan);
        PermissionSaver.Instance.SetPlayerPermissionLevel(player.Steam64, EAdminType.ADMIN_ON_DUTY);
        player.SendChat(T.DutyOnFeedback);
        Chat.Broadcast(LanguageSet.AllBut(player.Steam64), T.DutyOnBroadcast, player);
        GiveAdminPermissions(player, false);
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, true);
        ActionLog.Add(ActionLogType.DutyChanged, "ON DUTY", player.CSteamID.m_SteamID);
    }
    public static void AdminOnToOff(UCPlayer player)
    {
        L.Log($"{player.Name.PlayerName} ({player.Steam64.ToString(Data.AdminLocale)}) went off duty.", ConsoleColor.Cyan);
        PermissionSaver.Instance.SetPlayerPermissionLevel(player.Steam64, EAdminType.ADMIN_OFF_DUTY);
        Chat.Broadcast(LanguageSet.AllBut(player.Steam64), T.DutyOffBroadcast, player);
        player.SendChat(T.DutyOffFeedback);
        ClearAdminPermissions(player);
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, false);
        ActionLog.Add(ActionLogType.DutyChanged, "OFF DUTY", player.CSteamID.m_SteamID);
    }
    public static void InternOffToOn(UCPlayer player)
    {
        L.Log($"{player.Name.PlayerName} ({player.Steam64.ToString(Data.AdminLocale)}) went on duty.", ConsoleColor.Cyan);
        PermissionSaver.Instance.SetPlayerPermissionLevel(player.Steam64, EAdminType.TRIAL_ADMIN_ON_DUTY);
        player.SendChat(T.DutyOnFeedback);
        Chat.Broadcast(LanguageSet.AllBut(player.Steam64), T.DutyOnBroadcast, player);
        GiveAdminPermissions(player, true);
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, true);
        ActionLog.Add(ActionLogType.DutyChanged, "ON DUTY", player.CSteamID.m_SteamID);
    }
    public static void InternOnToOff(UCPlayer player)
    {
        L.Log($"{player.Name.PlayerName} ({player.Steam64.ToString(Data.AdminLocale)}) went off duty.", ConsoleColor.Cyan);
        PermissionSaver.Instance.SetPlayerPermissionLevel(player.Steam64, EAdminType.TRIAL_ADMIN_OFF_DUTY);
        Chat.Broadcast(LanguageSet.AllBut(player.Steam64), T.DutyOffBroadcast, player);
        player.SendChat(T.DutyOffFeedback);
        ClearAdminPermissions(player);
        PlayerManager.NetCalls.SendDutyChanged.NetInvoke(player.CSteamID.m_SteamID, false);
        ActionLog.Add(ActionLogType.DutyChanged, "OFF DUTY", player.CSteamID.m_SteamID);
    }
    public static void SetVanishMode(Player player, bool vanished)
    {
        if (player.movement.canAddSimulationResultsToUpdates != vanished)
            return;

        player.movement.canAddSimulationResultsToUpdates = !vanished;
        Vector3 pos = TeamManager.LobbySpawn;
        float angle = TeamManager.LobbySpawnAngle;
        player.movement.updates.Add(vanished
            ? new PlayerStateUpdate(pos, 0, MeasurementTool.angleToByte(angle))
            : new PlayerStateUpdate(player.transform.position, player.look.angle, player.look.rot));
    }
}