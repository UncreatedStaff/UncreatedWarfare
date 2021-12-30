using Rocket.API;
using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
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
        public List<string> Aliases => new List<string>(1) { "dep" };
        public List<string> Permissions => new List<string>(1) { "uc.deploy" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            if (Data.Is(out IRevives r) && r.ReviveManager.DownedPlayers.ContainsKey(player.Steam64))
            {
                player.Message("deploy_e_injured");
                return;
            }

            if (!Data.Is(out IFOBs fobs))
            {
                player.SendChat("command_e_gamemode");
                return;
            }

            if (command.Length == 1)
            {
                PlaytimeComponent c = player.Player.GetPlaytimeComponent(out _);

                ulong team = player.GetTeam();
                bool IsInMain = player.Player.IsInMain();
                bool IsInLobby = TeamManager.LobbyZone.IsInside(player.Player.transform.position);
                bool shouldCancelOnMove = !IsInMain;
                bool shouldCancelOnDamage = !IsInMain;

                if (CooldownManager.HasCooldown(player, ECooldownType.DEPLOY, out Cooldown cooldown))
                {
                    player.Message("deploy_e_cooldown", cooldown.ToString());
                    return;
                }
                if (!(IsInMain || IsInLobby))
                {
                    if (CooldownManager.HasCooldown(player, ECooldownType.COMBAT, out Cooldown combatlog))
                    {
                        player.Message("deploy_e_incombat", combatlog.ToString());
                        return;
                    }
                    if (!player.IsOnFOB(out _))
                    {
                        player.Message("deploy_e_notnearfob");
                        return;
                    }
                }

                if (!FOBManager.FindFOBByName(command[0], player.GetTeam(), out object deployable))
                {
                    if (command[0] == "main")
                        c.TeleportTo(team.GetBaseSpawnFromTeam(), FOBManager.config.Data.DeloyMainDelay, shouldCancelOnMove, false, team.GetBaseAngle());
                    else if (command[0] == "lobby")
                        player.SendChat("deploy_lobby_removed");
                    else
                        player.Message("deploy_e_fobnotfound", command[0]);
                    return;
                }

                if (deployable is FOB FOB)
                {
                    if (FOB.Bunker == null)
                    {
                        player.Message("deploy_e_nobunker", command[0]);
                        return;
                    }
                    if (FOB.NearbyEnemies.Count != 0)
                    {
                        player.Message("deploy_e_enemiesnearby", command[0]);
                        return;
                    }

                    c.TeleportTo(FOB, FOBManager.config.Data.DeloyFOBDelay, shouldCancelOnMove);
 
                }
                else if (deployable is SpecialFOB special)
                {
                    c.TeleportTo(special, FOBManager.config.Data.DeloyFOBDelay, shouldCancelOnMove);
                }
                else if (deployable is Cache cache)
                {
                    if (cache.NearbyAttackers.Count != 0)
                    {
                        player.Message("deploy_e_enemiesnearby", command[0]);
                        return;
                    }

                    c.TeleportTo(cache, FOBManager.config.Data.DeloyFOBDelay, shouldCancelOnMove);
                }
#if false
                else if (command[0].ToLower() == "lobby")
                {
                    c.TeleportDelayed(TeamManager.LobbySpawn, TeamManager.LobbySpawnAngle, FOBManager.config.Data.DeloyMainDelay, shouldCancelOnMove, shouldCancelOnDamage, true, "<color=#bb80d1>lobby</color>");
                }
#endif
                else
                {
                    player.Message("deploy_e_fobnotfound", command[0]);
                }


            }
            else
            {
                player.Message("correct_usage", "/deploy main -OR- /deploy <fob name>");
            }
        }
    }
}