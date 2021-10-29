using Rocket.API;
using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands
{
    public class Command_deploy : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "deploy";
        public string Help => "deploys you to a nearby FOB";
        public string Syntax => "/deploy";
        public List<string> Aliases => new List<string>() { "dep" };
        public List<string> Permissions => new List<string>() { "uc.deploy" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            if (Data.Is(out IRevives r) && r.ReviveManager.DownedPlayers.ContainsKey(player.Steam64))
            {
                player.Message("deploy_e_injured");
                return;
            }

            if (command.Length == 1)
            {
                PlaytimeComponent c = F.GetPlaytimeComponent(player.Player, out _);

                bool IsInMain = player.Player.IsInMain();
                bool IsInLobby = TeamManager.LobbyZone.IsInside(player.Player.transform.position);
                bool shouldCancelOnMove = !IsInMain;
                bool shouldCancelOnDamage = !IsInMain;

                ulong team = player.GetTeam();

                if (!(IsInMain || IsInLobby))
                {
                    if (CooldownManager.HasCooldown(player, ECooldownType.COMBAT, out Cooldown combatlog))
                    {
                        player.Message("deploy_e_incombat", combatlog.ToString());
                        return;
                    }
                    if (!player.IsNearFOB())
                    {
                        player.Message("deploy_e_notnearfob");
                        return;
                    }
                    if (CooldownManager.HasCooldown(player, ECooldownType.DEPLOY, out Cooldown cooldown))
                    {
                        player.Message("deploy_e_cooldown", cooldown.ToString());
                        return;
                    }
                }
                if (command[0].ToLower() == "main")
                {
                    c.TeleportDelayed(team.GetBaseSpawnFromTeam(), team.GetBaseAngle(), FOBManager.config.Data.DeloyMainDelay, shouldCancelOnMove, shouldCancelOnDamage, true, "<color=#d1b780>main</color>");
                }
#if false
                else if (command[0].ToLower() == "lobby")
                {
                    c.TeleportDelayed(TeamManager.LobbySpawn, TeamManager.LobbySpawnAngle, FOBManager.config.Data.DeloyMainDelay, shouldCancelOnMove, shouldCancelOnDamage, true, "<color=#bb80d1>lobby</color>");
                }
#endif
                else
                {
                    if (!Data.Is(out IFOBs fobs))
                    {
                        player.SendChat("command_e_gamemode");
                        return;
                    }
                    if (CooldownManager.HasCooldown(player, ECooldownType.DEPLOY, out Cooldown cooldown))
                    {
                        player.Message("deploy_e_cooldown", cooldown.ToString());
                        return;
                    }
                    if (!FOBManager.FindFOBByName(command[0], player.GetTeam(), out var FOB))
                    {
                        player.Message("deploy_e_fobnotfound", command[0]);
                        return;
                    }
                    if (FOB.nearbyEnemies.Count != 0)
                    {
                        player.Message("deploy_e_enemiesnearby", command[0]);
                        return;
                    }

                    c.TeleportDelayed(FOB.Structure.model.position, 0, FOBManager.config.Data.DeloyMainDelay, shouldCancelOnMove, shouldCancelOnDamage, true, $"<color=#54e3ff>{FOB.Name}</color>", FOB);
                }
            }
            else
            {
                player.Message("correct_usage", "/deploy main -OR- /deploy <fob name>");
            }
        }
    }
}