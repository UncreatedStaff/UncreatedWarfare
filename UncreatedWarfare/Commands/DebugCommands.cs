using Rocket.API;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Flags;
using UncreatedWarfare.FOBs;
using UnityEngine;
using Flag = UncreatedWarfare.Flags.Flag;

namespace UncreatedWarfare.Commands
{
    internal class ZoneCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "test";

        public string Help => "Get the current zone the player is in if any";

        public string Syntax => "/test <mode>";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string> { "uc.test" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            Player player = (caller as UnturnedPlayer).Player;
            if(command.Length > 0)
            {
                if(command[0] == "zone")
                {
                    Flag flag = UCWarfare.I.FlagManager.FlagRotation.FirstOrDefault(f => f.PlayerInRange(player));
                    if (flag == default(Flag))
                    {
                        player.SendChat("not_in_zone", UCWarfare.GetColor("default"), player.transform.position.x, player.transform.position.y, player.transform.position.z, UCWarfare.I.FlagManager.FlagRotation.Count);
                    }
                    else
                    {
                        player.SendChat("current_zone", UCWarfare.GetColor("default"), flag.Name, player.transform.position.x, player.transform.position.y, player.transform.position.z);
                    }
                } else if (command[0] == "sign")
                {
                    InteractableSign sign = BuildManager.GetInteractableFromLook<InteractableSign>(player.look);
                    if (sign == null) player.SendChat("No sign found.", UCWarfare.GetColor("default"));
                    else
                    {
                        player.SendChat("Sign text: \"" + sign.text + '\"', UCWarfare.GetColor("default"));
                        CommandWindow.Log("Sign text: \"" + sign.text + '\"');
                    }
                } else if (command[0] == "visualize")
                {
                    Flag flag = UCWarfare.I.FlagManager.FlagRotation.FirstOrDefault(f => f.PlayerInRange(player));
                    if(flag == default(Flag))
                    {
                        player.SendChat("not_in_zone", UCWarfare.GetColor("default"), player.transform.position.x, player.transform.position.y, player.transform.position.z, UCWarfare.I.FlagManager.FlagRotation.Count);
                    } else
                    {
                        Vector2[] points;
                        Vector2[] corners;
                        Vector2 center;
                        if (command.Length == 2)
                        {
                            if(float.TryParse(command[1], out float spacing))
                            {
                                points = flag.ZoneData.GetParticleSpawnPoints(out corners, out center, -1, spacing);
                            } else
                            {
                                player.SendChat("/test visualize [spacing] [perline]. Specifying perline will disregard spacing.", UCWarfare.GetColor("default"));
                                return;
                            }
                        } else
                        {
                            points = flag.ZoneData.GetParticleSpawnPoints(out corners, out center);
                        }
                        CSteamID channel = player.channel.owner.playerID.steamID;
                        foreach(Vector2 Point in points)
                        {
                            float y = F.GetTerrainHeightAt2DPoint(Point.x, Point.y);
                            if (y == 0) y = player.transform.position.y;
                            Vector3 pos = new Vector3(Point.x, y + 0.5f, Point.y);
                            TriggerEffectParameters p = new TriggerEffectParameters(117)
                            {
                                position = pos,
                                reliable = true,
                                relevantPlayerID = channel
                            };
                            TriggerEffectParameters p2 = new TriggerEffectParameters(120)
                            {
                                position = pos,
                                reliable = true,
                                relevantPlayerID = channel
                            };
                            EffectManager.triggerEffect(p);
                            EffectManager.triggerEffect(p2);
                        }
                        foreach (Vector2 Point in corners)
                        {
                            float y = F.GetTerrainHeightAt2DPoint(Point.x, Point.y);
                            if (y == 0) y = player.transform.position.y;
                            Vector3 pos = new Vector3(Point.x, y + 0.5f, Point.y);
                            TriggerEffectParameters p = new TriggerEffectParameters(115)
                            {
                                position = pos,
                                reliable = true,
                                relevantPlayerID = channel
                            };
                            TriggerEffectParameters p2 = new TriggerEffectParameters(120)
                            {
                                position = pos,
                                reliable = true,
                                relevantPlayerID = channel
                            };
                            EffectManager.triggerEffect(p);
                            EffectManager.triggerEffect(p2);
                        }
                        {
                            float y = F.GetTerrainHeightAt2DPoint(center.x, center.y);
                            if (y == 0) y = player.transform.position.y;
                            Vector3 pos = new Vector3(center.x, y + 0.5f, center.y);
                            TriggerEffectParameters p = new TriggerEffectParameters(113)
                            {
                                position = pos,
                                reliable = true,
                                relevantPlayerID = channel
                            };
                            TriggerEffectParameters p2 = new TriggerEffectParameters(120)
                            {
                                position = pos,
                                reliable = true,
                                relevantPlayerID = channel
                            };
                            EffectManager.triggerEffect(p);
                            EffectManager.triggerEffect(p2);
                        }
                        player.SendChat($"Spawned {points.Length + corners.Length} particles around zone <color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>. They will despawn in 1 minute.", UCWarfare.GetColor("default"));
                    }
                } else if (command[0] == "player")
                {
                    player.SendChat($"Position:" +
                        $" ({Math.Round(player.transform.position.x, 3)}, {Math.Round(player.transform.position.y, 3)}, {Math.Round(player.transform.position.z, 3)})" +
                        $" LookForward: " +
                        $"({Math.Round(player.look.aim.forward.x, 3)}, {Math.Round(player.look.aim.forward.y, 3)}, {Math.Round(player.look.aim.forward.z, 3)})", UCWarfare.GetColor("default"));
                } else if (command[0] == "level")
                {
                    player.SendChat($"Size: {Level.size}, Height: {Level.HEIGHT}, Border: {Level.border}, ObjectName: {Level.level.name}, ObjectType: {Level.level.GetType().FullName}", UCWarfare.GetColor("default"));
                } else if (command[0] == "set")
                {
                    if(command.Length == 2)
                    {

                    } 
                }
            }
        }
    }
}