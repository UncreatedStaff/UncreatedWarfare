
using Rocket.API;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Commands
{
    public class RallyCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "rally";
        public string Help => "Deploys you to a rallypoint";
        public string Syntax => "/rally";
        private readonly List<string> _aliases = new List<string>(0);
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>() { "uc.rally" };
		public List<string> Permissions => _permissions;
        public void Execute(IRocketPlayer caller, string[] command)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
            if (player == null) return;
            if (!Data.Is(out ISquads ctf))
            {
                player.SendChat("command_e_gamemode");
                return;
            }
            if (!UCWarfare.Config.EnableSquads)
            {
                player.SendChat("squads_disabled");
                return;
            }

            if (player.Squad != null)
            {
                if (RallyManager.HasRally(player, out RallyPoint rallypoint) && rallypoint.IsActive)
                {
                    if (command.Length == 0)
                    {
                        if (rallypoint.timer <= 0)
                        {
                            rallypoint.TeleportPlayer(player);
                        }
                        else
                        {
                            if (!rallypoint.AwaitingPlayers.Exists(p => p.Steam64 == player.Steam64))
                            {
                                rallypoint.AwaitingPlayers.Add(player);
                                rallypoint.UpdateUIForAwaitingPlayers();
                                player.Message("rally_wait", rallypoint.timer.ToString(Data.Locale));
                            }
                            else
                            {
                                player.Message("rally_e_alreadywaiting");
                            }
                        }
                    }
                    else if (command.Length == 1 && command[0].ToLower() == "cancel" || command[0].ToLower() == "c" || command[0].ToLower() == "abort")
                    {
                        if (rallypoint.AwaitingPlayers.Exists(p => p.Steam64 == player.Steam64))
                        {
                            rallypoint.AwaitingPlayers.RemoveAll(p => p.Steam64 == player.Steam64);
                            rallypoint.ShowUIForPlayer(player);
                            player.Message("rally_aborted");
                        }
                        else
                        {
                            player.Message("rally_e_notwaiting");
                        }
                    }
                }
                else
                {
                    player.Message("rally_e_unavailable");
                }
            }
            else
            {
                player.Message("rally_e_notinsquad");
            }
        }
    }
}
