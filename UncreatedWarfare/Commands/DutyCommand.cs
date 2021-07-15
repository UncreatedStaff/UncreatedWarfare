using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Players;

namespace Uncreated.Warfare.Commands
{
    class DutyCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "duty";
        public string Help => "Go on or off duty.";
        public string Syntax => "/duty";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "uc.duty" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            FPlayerName names = F.GetPlayerOriginalNames(player.Player);
            List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(player, false);
            if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup))
            {
                AdminOffToOn(player, names);
            }
            else if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup))
            {
                AdminOnToOff(player, names);
            }
            else if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup))
            {
                InternOffToOn(player, names);
            }
            else if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup))
            {
                InternOnToOff(player, names);
            }
        }
        public static void AdminOffToOn(UnturnedPlayer player, FPlayerName names)
        {
            F.Log(F.Translate("duty_admin_on_console", 0, out _, names.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
            R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, player);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, player);
            player.Player.look.sendFreecamAllowed(true);
            player.Player.look.sendSpecStatsAllowed(true);
            player.Player.look.sendWorkzoneAllowed(true);
            player.SendChat("duty_on_feedback");
            F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "duty_on_broadcast", names.CharacterName);
        }
        public static void AdminOnToOff(UnturnedPlayer player, FPlayerName names)
        {
            F.Log(F.Translate("duty_admin_off_console", 0, out _, names.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
            R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, player);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, player);
            F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "duty_off_broadcast", names.CharacterName);
            if (player == null)
                return;
            if (player.Features != null && player.Features.gameObject != null)
            {
                player.Features.GodMode = false;
                player.Features.VanishMode = false;
            }
            if (player.Player != null && player.Player.look != null)
            {
                player.Player.look.sendFreecamAllowed(false);
                player.Player.look.sendSpecStatsAllowed(false);
                player.Player.look.sendWorkzoneAllowed(false);
                player.SendChat("duty_off_feedback");
            }
        }
        public static void InternOffToOn(UnturnedPlayer player, FPlayerName names)
        {
            F.Log(F.Translate("duty_intern_on_console", 0, out _, names.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
            R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, player);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, player);
            player.Player.look.sendFreecamAllowed(true);
            player.Player.look.sendSpecStatsAllowed(true);
            player.Player.look.sendWorkzoneAllowed(true);
            player.SendChat("duty_on_feedback");
            F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "duty_on_broadcast", names.CharacterName);
        }
        public static void InternOnToOff(UnturnedPlayer player, FPlayerName names)
        {
            F.Log(F.Translate("duty_intern_off_console", 0, out _, names.PlayerName, names.Steam64.ToString(Data.Locale)), ConsoleColor.Cyan);
            R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, player);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, player);
            F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "duty_off_broadcast", names.CharacterName);
            if (player == null)
                return;
            if (player.Features != null && player.Features.gameObject != null)
            {
                player.Features.GodMode = false;
                player.Features.VanishMode = false;
            }
            if (player.Player != null && player.Player.look != null)
            {
                player.Player.look.sendFreecamAllowed(false);
                player.Player.look.sendSpecStatsAllowed(false);
                player.Player.look.sendWorkzoneAllowed(false);
                player.SendChat("duty_off_feedback");
            }
        }
    }
}