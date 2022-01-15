using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Unturned.Player;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Commands
{
    class DutyCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "duty";
        public string Help => "Go on or off duty.";
        public string Syntax => "/duty";
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.duty" };
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
            L.Log(Translation.Translate("duty_admin_on_console", 0, out _, names.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
            R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, player);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, player);
            player.Player.look.sendFreecamAllowed(true);
            player.Player.look.sendWorkzoneAllowed(true);
            player.SendChat("duty_on_feedback");
            Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "duty_on_broadcast", names.CharacterName);
            Invocations.Shared.DutyChanged.NetInvoke(player.CSteamID.m_SteamID, true);
        }
        public static void AdminOnToOff(UnturnedPlayer player, FPlayerName names)
        {
            L.Log(Translation.Translate("duty_admin_off_console", 0, out _, names.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
            R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, player);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, player);
            Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "duty_off_broadcast", names.CharacterName);
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
                player.Player.look.sendWorkzoneAllowed(false);
                player.SendChat("duty_off_feedback");
            }
            Invocations.Shared.DutyChanged.NetInvoke(player.CSteamID.m_SteamID, false);
        }
        public static void InternOffToOn(UnturnedPlayer player, FPlayerName names)
        {
            L.Log(Translation.Translate("duty_intern_on_console", 0, out _, names.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
            R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, player);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, player);
            player.SendChat("duty_on_feedback");
            Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "duty_on_broadcast", names.CharacterName);
            Invocations.Shared.DutyChanged.NetInvoke(player.CSteamID.m_SteamID, true);
        }
        public static void InternOnToOff(UnturnedPlayer player, FPlayerName names)
        {
            L.Log(Translation.Translate("duty_intern_off_console", 0, out _, names.PlayerName, names.Steam64.ToString(Data.Locale)), ConsoleColor.Cyan);
            R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, player);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, player);
            Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "duty_off_broadcast", names.CharacterName);
            if (player == null)
                return;
            if (player.Features != null && player.Features.gameObject != null)
            {
                player.Features.GodMode = false;
                player.Features.VanishMode = false;
            }
            if (player.Player != null && player.Player.look != null)
            {
                player.SendChat("duty_off_feedback");
            }
            Invocations.Shared.DutyChanged.NetInvoke(player.CSteamID.m_SteamID, false);
        }
    }
}