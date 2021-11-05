using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes;
using UnityEngine;

namespace Uncreated.Warfare.Commands
{
    public class DevCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "dev";
        public string Help => "Dev command for various server setup features.";
        public string Syntax => "/dev [arguments]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.dev" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            if (command.Length > 0 && command[0].ToLower() == "addcache")
            {
                if (Data.Is(out Insurgency insurgency))
                {
                    SerializableTransform transform = new SerializableTransform(player.Player.transform);
                    insurgency.Config.CacheSpawns.Add(transform);
                    insurgency.SaveConfig();

                        player.Message("Added new cache spawn: " + transform.ToString().Colorize("dbc39e"));
                    }
                    else
                        player.Message("Gamemode must be Insurgency in order to use this command.".Colorize("dba29e"));
                }
                else if (command.Length > 1 && command[0].ToLower() == "addintel")
                {
                    if (Data.Is(out Insurgency insurgency))
                    {
                        if (int.TryParse(command[1], out int points))
                        {
                            insurgency.AddIntelligencePoints(points);

                            player.Message($"Added {points} intelligence points.".Colorize("dbc39e"));
                        }
                        else
                            player.Message($"'{command[1]}' is not a valid number of intelligence points.".Colorize("dba29e"));
                    }
                    else
                        player.Message("Gamemode must be Insurgency in order to use this command.".Colorize("dba29e"));
                }
                else
                    player.Message($"Dev command did not recognise those arguments.".Colorize("dba29e"));
            }
        }
    }
}
