using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;

namespace UncreatedWarfare.Commands
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
                F.Log(F.Translate("duty_GoOnDuty_Console", 0, names.PlayerName, player.CSteamID.m_SteamID.ToString()), ConsoleColor.Cyan);
                R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, player);
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, player);
                player.Player.look.sendFreecamAllowed(true);
                player.Player.look.sendSpecStatsAllowed(true);
                player.Player.look.sendWorkzoneAllowed(true);
                player.SendChat("duty_GoOnDuty_Feedback", UCWarfare.GetColor("duty_feedback"));
                F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "duty_GoOnDuty_Broadcast", UCWarfare.GetColor("duty_broadcast"), names.CharacterName);
            }
            else if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup))
            {
                F.Log(F.Translate("duty_GoOffDuty_Console", 0, names.PlayerName, player.CSteamID.m_SteamID.ToString()), ConsoleColor.Cyan);
                R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, player);
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, player);
                player.Features.GodMode = false;
                player.Features.VanishMode = false;
                player.Player.look.sendFreecamAllowed(false);
                player.Player.look.sendSpecStatsAllowed(false);
                player.Player.look.sendWorkzoneAllowed(false);
                player.SendChat("duty_GoOffDuty_Feedback", UCWarfare.GetColor("duty_feedback"));
                F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "duty_GoOffDuty_Broadcast", UCWarfare.GetColor("duty_broadcast"), names.CharacterName);
            }
            else if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup))
            {
                F.Log(F.Translate("duty_GoOnDuty_Console", 0, names.PlayerName, player.CSteamID.m_SteamID.ToString()), ConsoleColor.Cyan);
                R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, player);
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, player);
                player.Player.look.sendFreecamAllowed(true);
                player.Player.look.sendSpecStatsAllowed(true);
                player.Player.look.sendWorkzoneAllowed(true);
                player.SendChat("duty_GoOnDuty_Feedback", UCWarfare.GetColor("duty_feedback"));
                F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "duty_GoOnDuty_Broadcast", UCWarfare.GetColor("duty_broadcast"), names.CharacterName);
            }
            else if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup))
            {
                F.Log(F.Translate("duty_GoOffDuty_Console", 0, names.PlayerName, player.CSteamID.m_SteamID.ToString()), ConsoleColor.Cyan);
                R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, player);
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, player);
                player.Features.GodMode = false;
                player.Features.VanishMode = false;
                player.Player.look.sendFreecamAllowed(false);
                player.Player.look.sendSpecStatsAllowed(false);
                player.Player.look.sendWorkzoneAllowed(false);
                player.SendChat("duty_GoOffDuty_Feedback", UCWarfare.GetColor("duty_feedback"));
                F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "duty_GoOffDuty_Broadcast", UCWarfare.GetColor("duty_broadcast"), names.CharacterName);
            }
        }
    }
}