using Rocket.API;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        public static int currentstep = 0;
        public string Syntax => "/test <mode>";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string> { "uc.test" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            Player player = (caller as UnturnedPlayer).Player;
            if(command.Length > 0)
            {
                if (command[0] == "zone")
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
                }
                else if (command[0] == "sign")
                {
                    InteractableSign sign = BuildManager.GetInteractableFromLook<InteractableSign>(player.look);
                    if (sign == null) player.SendChat("No sign found.", UCWarfare.GetColor("default"));
                    else
                    {
                        player.SendChat("Sign text: \"" + sign.text + '\"', UCWarfare.GetColor("default"));
                        CommandWindow.Log("Sign text: \"" + sign.text + '\"');
                    }
                }
                else if (command[0] == "visualize")
                {
                    Flag flag = UCWarfare.I.FlagManager.AllFlags.FirstOrDefault(f => f.PlayerInRange(player));
                    Zone zone;
                    string zoneName;
                    string zoneColor;
                    if (flag == default(Flag))
                    {
                        List<Zone> zones = UCWarfare.I.ExtraZones.Values.ToList();
                        zones.Sort(delegate (Zone a, Zone b)
                        {
                            return a.BoundsArea.CompareTo(b.BoundsArea);
                        });
                        Zone extrazone = zones.FirstOrDefault(z => z.IsInside(player.transform.position));
                        if (extrazone == default(Zone))
                        {
                            player.SendChat("not_in_zone", UCWarfare.GetColor("default"), player.transform.position.x, player.transform.position.y, player.transform.position.z, UCWarfare.I.FlagManager.FlagRotation.Count);
                            return;
                        }
                        else
                        {
                            zone = extrazone;
                            zoneName = extrazone.Name;
                            zoneColor = UCWarfare.GetColorHex("default");
                        }
                    }
                    else
                    {
                        zone = flag.ZoneData;
                        zoneName = flag.Name;
                        zoneColor = flag.TeamSpecificHexColor;
                    }
                    Vector2[] points;
                    Vector2[] corners;
                    Vector2 center;
                    if (command.Length == 2)
                    {
                        if (float.TryParse(command[1], out float spacing))
                        {
                            points = zone.GetParticleSpawnPoints(out corners, out center, -1, spacing);
                        }
                        else
                        {
                            player.SendChat("/test visualize [spacing] [perline]. Specifying perline will disregard spacing.", UCWarfare.GetColor("default"));
                            return;
                        }
                    }
                    else
                    {
                        points = zone.GetParticleSpawnPoints(out corners, out center);
                    }
                    CSteamID channel = player.channel.owner.playerID.steamID;
                    foreach (Vector2 Point in points)
                    {   // Border
                        float y = F.GetTerrainHeightAt2DPoint(Point.x, Point.y);
                        if (y == 0) y = player.transform.position.y;
                        Vector3 pos = new Vector3(Point.x, y + 0.5f, Point.y);
                        F.TriggerEffectReliable(117, channel, pos);
                        F.TriggerEffectReliable(120, channel, pos);
                    }
                    foreach (Vector2 Point in corners)
                    {   // Corners
                        float y = F.GetTerrainHeightAt2DPoint(Point.x, Point.y);
                        if (y == 0) y = player.transform.position.y;
                        Vector3 pos = new Vector3(Point.x, y + 0.5f, Point.y);
                        F.TriggerEffectReliable(115, channel, pos);
                        F.TriggerEffectReliable(120, channel, pos);
                    }
                    {   // Center
                        float y = F.GetTerrainHeightAt2DPoint(center.x, center.y);
                        if (y == 0) y = player.transform.position.y;
                        Vector3 pos = new Vector3(center.x, y + 0.5f, center.y);
                        F.TriggerEffectReliable(113, channel, pos);
                        F.TriggerEffectReliable(120, channel, pos);
                    }
                    player.SendChat($"Spawned {points.Length + corners.Length} particles around zone <color=#{zoneColor}>{zoneName}</color>. They will despawn in 1 minute.", UCWarfare.GetColor("default"));
                }
                else if (command[0] == "goto")
                {
                    if(command.Length > 1)
                    {
                        string arg = command[1].ToLower();
                        if (command.Length > 2)
                        {
                            StringBuilder sb = new StringBuilder();
                            for(int i = 1; i < command.Length; i++)
                            {
                                if (i != 1) sb.Append(' ');
                                sb.Append(command[i]);
                            }
                            arg = sb.ToString().ToLower();
                        }
                        Flag flag;
                        if (arg == "obj1" && UCWarfare.I.FlagManager.ObjectiveTeam1 != null)
                            flag = UCWarfare.I.FlagManager.ObjectiveTeam1;
                        else if (arg == "obj2" && UCWarfare.I.FlagManager.ObjectiveTeam2 != null)
                            flag = UCWarfare.I.FlagManager.ObjectiveTeam2;
                        else
                            flag = UCWarfare.I.FlagManager.AllFlags.FirstOrDefault(f => f.Name.ToLower().Contains(arg) || (int.TryParse(arg, out int o) && f.ID == o));
                        if(flag == default(Flag))
                        {
                            Dictionary<int, Zone> eZones = UCWarfare.I.ExtraZones;
                            KeyValuePair<int, Zone> zone = eZones.FirstOrDefault(f => f.Value.Name.ToLower().Contains(arg) || (int.TryParse(arg, out int o) && f.Key == o));
                            if(zone.Equals(default(KeyValuePair<int, Zone>)))
                            {
                                player.SendChat("No zone or flag found from search terms: \"" + arg + "\"", UCWarfare.GetColor("default"));
                                return;
                            }
                            player.teleportToLocation(new Vector3(zone.Value.Center.x, F.GetTerrainHeightAt2DPoint(zone.Value.Center) + 1f, zone.Value.Center.y), 90f);
                            player.SendChat("Teleported to extra zone " + zone.Value.Name + '.', UCWarfare.GetColor("default"));
                            return;
                        }
                        player.teleportToLocation(new Vector3(flag.ZoneData.Center.x, F.GetTerrainHeightAt2DPoint(flag.ZoneData.Center) + 1f, flag.ZoneData.Center.y), 90f);
                        player.SendChat("Teleported to flag <color=#" + flag.TeamSpecificHexColor + ">" + flag.Name + "</color>.", UCWarfare.GetColor("default"));
                        return;
                    } else
                    {
                        player.SendChat("Syntax: /test goto <flag name|zone name|flag id|zone id>", UCWarfare.GetColor(""));
                    }
                }
                else if (command[0] == "player")
                {
                    player.SendChat($"Position:" +
                        $" ({Math.Round(player.transform.position.x, 3)}, {Math.Round(player.transform.position.y, 3)}, {Math.Round(player.transform.position.z, 3)})" +
                        $" LookForward: " +
                        $"({Math.Round(player.look.aim.forward.x, 3)}, {Math.Round(player.look.aim.forward.y, 3)}, {Math.Round(player.look.aim.forward.z, 3)})", UCWarfare.GetColor("default"));
                }
                else if (command[0] == "level")
                {
                    player.SendChat($"Size: {Level.size}, Height: {Level.HEIGHT}, Border: {Level.border}, ObjectName: {Level.level.name}, ObjectType: {Level.level.GetType().FullName}", UCWarfare.GetColor("default"));
                }
                else if (command[0] == "togglecoroutinetiming")
                {
                    UCWarfare.I.CoroutineTiming = !UCWarfare.I.CoroutineTiming;
                    player.SendChat((UCWarfare.I.CoroutineTiming ? "Enabled" : "Disabled") + " coroutine timing.", UCWarfare.GetColor("default"));
                } else if (command[0] == "zonearea")
                {
                    bool all = true;
                    bool extra = true;
                    if (command.Length > 2)
                    {
                        if (command[1] == "active") all = false;
                        else if (command[1] != "all") player.SendChat("Syntax: /test zonearea [active|all] <show extra zones: true|false>", UCWarfare.GetColor("defaulterror"));
                        if (command[2] == "false") extra = false;
                        else if (command[2] != "true") player.SendChat("Syntax: /test zonearea [active|all] <show extra zones: true|false>", UCWarfare.GetColor("defaulterror"));
                    }
                    else
                    {
                        player.SendChat("Syntax: /test zonearea <active|all> <show extra zones: true|false>", UCWarfare.GetColor("defaulterror"));
                        return;
                    }
                    List<Zone> zones = new List<Zone>();
                    foreach (Flag flag in all ? UCWarfare.I.FlagManager.AllFlags : UCWarfare.I.FlagManager.FlagRotation)
                        zones.Add(flag.ZoneData);
                    if (extra)
                    {
                        foreach (Zone zone in UCWarfare.I.ExtraZones.Values)
                            zones.Add(zone);
                    }
                    player.SendChat("Picture has to generate, wait around a minute.", UCWarfare.GetColor("default"));
                    UCWarfare.I.DatabaseManager.SendScreenshot(player, zones);
                } else if (command[0] == "quickcap")
                {
                    Flag flag = UCWarfare.I.FlagManager.FlagRotation.FirstOrDefault(f => f.PlayersOnFlag.Contains(player));
                    if(flag == default(Flag))
                    {
                        player.SendChat("not_in_zone", UCWarfare.GetColor("default"), player.transform.position.x, player.transform.position.y, player.transform.position.z, UCWarfare.I.FlagManager.FlagRotation.Count);
                        return;
                    }
                    ulong team = player.GetTeam();
                    if (team == 1)
                        flag.CapT1(Flag.MaxPoints - flag.Points - 1);
                    else if (team == 2)
                        flag.CapT2(Flag.MaxPoints - flag.Points - 1);
                    else player.SendChat("You're not on a team that can capture flags.", UCWarfare.GetColor("default"));
                } else if (command[0] == "quickwin")
                {
                    ulong team = F.GetTeam(player);
                    if(team != 1 && team != 2)
                    {
                        player.SendChat("You're not on a team that can capture flags.", UCWarfare.GetColor("default"));
                        return;
                    }
                    foreach(Flag flag in UCWarfare.I.FlagManager.FlagRotation)
                    {
                        if (team == 1)
                            flag.CapT1(Flag.MaxPoints - flag.Points);
                        else if (team == 2)
                            flag.CapT2(Flag.MaxPoints - flag.Points);
                    }
                } else if (command[0] == "playtime")
                {
                    player.SendChat("Playtime: " + F.GetTimeFromSeconds((uint)Mathf.Round(Mathf.Abs(F.GetCurrentPlaytime(player)))) + '.', UCWarfare.GetColor("default"));
                }
            }
        }


        
    }
}