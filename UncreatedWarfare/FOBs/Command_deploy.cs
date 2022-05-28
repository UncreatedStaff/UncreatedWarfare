using Rocket.API;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands
{
    public class DeployCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "deploy";
        public string Help => "deploys you to a nearby FOB";
        public string Syntax => "/deploy";
        private readonly List<string> _aliases = new List<string>(1) { "dep" };
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>(1) { "uc.deploy" };
		public List<string> Permissions => _permissions;
        public void Execute(IRocketPlayer caller, string[] command)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
            if (player == null) return;

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
                PlaytimeComponent? c = player.Player.GetPlaytimeComponent(out _);

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
                    if (!(player.IsOnFOB(out _) || 
                          UCBarricadeManager.CountNearbyBarricades(Gamemode.Config.Barricades.InsurgencyCacheGUID, 10, player.Position, player.GetTeam()) != 0))
                    {
                        if (Data.Is(out Insurgency ins))
                            player.Message("deploy_e_notnearfob_ins");
                        else
                            player.Message("deploy_e_notnearfob");
                        return;
                    }
                }

                if (!FOBManager.FindFOBByName(command[0], player.GetTeam(), out object? deployable))
                {
                    if (command[0].ToLower() == "main")
                        c?.TeleportTo(team.GetBaseSpawnFromTeam(), FOBManager.Config.DeloyMainDelay, shouldCancelOnMove, false, team.GetBaseAngle());
                    else if (command[0].ToLower() == "lobby")
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
                    if (FOB.IsBleeding)
                    {
                        player.Message("deploy_e_damaged", command[0]);
                        return;
                    }
                    if (FOB.NearbyEnemies.Count != 0)
                    {
                        player.Message("deploy_e_enemiesnearby", command[0]);
                        return;
                    }

                    c?.TeleportTo(FOB, FOBManager.Config.DeloyFOBDelay, shouldCancelOnMove);
 
                }
                else if (deployable is SpecialFOB special)
                {
                    c?.TeleportTo(special, FOBManager.Config.DeloyFOBDelay, shouldCancelOnMove);
                }
                else if (deployable is Cache cache)
                {
                    if (cache.NearbyAttackers.Count != 0)
                    {
                        player.Message("deploy_e_enemiesnearby", command[0]);
                        return;
                    }

                    c?.TeleportTo(cache, FOBManager.Config.DeloyFOBDelay, shouldCancelOnMove);
                }
#if false
                else if (command[0].ToLower() == "lobby")
                {
                    c.TeleportDelayed(TeamManager.LobbySpawn, TeamManager.LobbySpawnAngle, FOBManager.Config.DeloyMainDelay, shouldCancelOnMove, shouldCancelOnDamage, true, "<color=#bb80d1>lobby</color>");
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