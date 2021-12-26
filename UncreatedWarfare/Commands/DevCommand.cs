using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using UnityEngine;

namespace Uncreated.Warfare.Commands
{
    public class DevCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "dev";
        public string Help => "Dev command for various server setup features.";
        public string Syntax => "/dev [arguments]";
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.dev" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            if (command.Length > 0 && command[0].ToLower() == "addcache")
            {
                if (Data.Is(out Insurgency insurgency))
                {
                    SerializableTransform transform = new SerializableTransform(player.Player.transform);
                    Gamemode.Config.MapConfig.AddCacheSpawn(transform);
                    player.Message("Added new cache spawn: " + transform.ToString().Colorize("ebd491"));
                }
                else
                    player.Message("Gamemode must be Insurgency in order to use this command.".Colorize("c7a29f"));
            }
            else if (command.Length > 1 && command[0].ToLower() == "addintel")
            {
                if (Data.Is(out Insurgency insurgency))
                {
                    if (int.TryParse(command[1], out int points))
                    {
                        insurgency.AddIntelligencePoints(points);

                        player.Message($"Added {points} intelligence points.".Colorize("ebd491"));
                    }
                    else
                        player.Message($"'{command[1]}' is not a valid number of intelligence points.".Colorize("c7a29f"));
                }
                else
                    player.Message("Gamemode must be Insurgency in order to use this command.".Colorize("c7a29f"));
            }
            else if (command.Length == 1 && command[0].ToLower() == "quickbuild" || command[0].ToLower() == "qb")
            {
                var barricade = BarricadeManager.FindBarricadeByRootTransform(UCBarricadeManager.GetBarricadeTransformFromLook(player.Player.look));

                if (barricade.model.TryGetComponent<BuildableComponent>(out var buildable))
                {
                    buildable.Build();
                    player.Message($"Successfully built {barricade.asset.itemName}".Colorize("ebd491"));
                }
                else
                    player.Message($"This barricade ({barricade.asset.itemName}) is not buildable.".Colorize("c7a29f"));
            }
            else
                player.Message($"Dev command did not recognise those arguments.".Colorize("dba29e"));
        }
    }
}
