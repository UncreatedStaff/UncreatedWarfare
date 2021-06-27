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
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Flags;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.XP;
using UnityEngine;
using Flag = Uncreated.Warfare.Flags.Flag;

namespace Uncreated.Warfare.Commands
{
#pragma warning disable IDE1006 // Naming Styles
    internal class _DebugCommand : IRocketCommand
#pragma warning restore IDE1006 // Naming Styles
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "test";
        public string Help => "Collection of test commands.";
        public static int currentstep = 0;
        public string Syntax => "/test <mode>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "uc.test" };
        public async void Execute(IRocketPlayer caller, string[] command)
        {
            Player player = caller.DisplayName == "Console" ? Provider.clients.FirstOrDefault()?.player : (caller as UnturnedPlayer).Player;
            if(command.Length > 0)
            {
                if (command.Length == 3 && (command[0].ToLower() == "givexp" || command[0].ToLower() == "giveof"))
                {
                    var target = UCPlayer.FromName(command[1]);
                    if (target != null)
                    {
                        if (Int32.TryParse(command[2], System.Globalization.NumberStyles.Any, Data.Locale, out var amount))
                        {
                            if (command[0].ToLower() == "givexp")
                            {
                                await XPManager.AddXP(target.Player, target.GetTeam(), amount);
                                player.SendChat($"Given {amount} XP to {target.CharacterName}.", UCWarfare.GetColor("default"));
                            }
                            else if (command[0].ToLower() == "giveof")
                            {
                                await OfficerManager.AddOfficerPoints(target.Player, target.GetTeam(), amount);
                                player.SendChat($"Given {amount} Officer Points to {target.CharacterName}.", UCWarfare.GetColor("default"));
                            }
                        }
                        else
                            player.SendChat($"'{command[2]}' is not a valid amount.", UCWarfare.GetColor("default"));
                    }
                    else
                        player.SendChat($"Could not find player called '{command[1]}'.", UCWarfare.GetColor("default"));
                }
                else if (command[0] == "zone")
                {
                    Flag flag = Data.FlagManager.FlagRotation.FirstOrDefault(f => f.PlayerInRange(player));
                    if (flag == default(Flag))
                    {
                        player.SendChat("not_in_zone", UCWarfare.GetColor("default"), player.transform.position.x, player.transform.position.y, player.transform.position.z, player.transform.rotation.eulerAngles.y, Data.FlagManager.FlagRotation.Count);
                    }
                    else
                    {
                        player.SendChat("current_zone", UCWarfare.GetColor("default"), flag.Name, player.transform.position.x, player.transform.position.y, player.transform.position.z);
                    }
                }
                else if (command[0] == "sign")
                {
                    InteractableSign sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.look);
                    if (sign == null) player.SendChat("No sign found.", UCWarfare.GetColor("default"));
                    else
                    {
                        player.SendChat("Sign text: \"" + sign.text + '\"', UCWarfare.GetColor("default"));
                        F.Log("Sign text: \"" + sign.text + '\"', ConsoleColor.Green);
                    }
                }
                else if (command[0] == "visualize")
                {
                    Flag flag = Data.FlagManager.AllFlags.FirstOrDefault(f => f.PlayerInRange(player));
                    Zone zone;
                    string zoneName;
                    string zoneColor;
                    if (flag == default)
                    {
                        List<Zone> zones = Data.ExtraZones.Values.ToList();
                        zones.Sort(delegate (Zone a, Zone b)
                        {
                            return a.BoundsArea.CompareTo(b.BoundsArea);
                        });
                        Zone extrazone = zones.FirstOrDefault(z => z.IsInside(player.transform.position));
                        if (extrazone == default)
                        {
                            player.SendChat("not_in_zone", UCWarfare.GetColor("default"), player.transform.position.x, player.transform.position.y, player.transform.position.z, Data.FlagManager.FlagRotation.Count);
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
                        if (float.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out float spacing))
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
                    if (command.Length > 1)
                    {
                        string arg = command[1].ToLower();
                        if (command.Length > 2)
                        {
                            StringBuilder sb = new StringBuilder();
                            for (int i = 1; i < command.Length; i++)
                            {
                                if (i != 1) sb.Append(' ');
                                sb.Append(command[i]);
                            }
                            arg = sb.ToString().ToLower();
                        }
                        Flag flag;
                        if (arg == "obj1" && Data.FlagManager.ObjectiveTeam1 != null)
                            flag = Data.FlagManager.ObjectiveTeam1;
                        else if (arg == "obj2" && Data.FlagManager.ObjectiveTeam2 != null)
                            flag = Data.FlagManager.ObjectiveTeam2;
                        else
                            flag = Data.FlagManager.AllFlags.FirstOrDefault(f => f.Name.ToLower().Contains(arg) || (int.TryParse(arg, System.Globalization.NumberStyles.Any, Data.Locale, out int o) && f.ID == o));
                        if (flag == default)
                        {
                            Dictionary<int, Zone> eZones = Data.ExtraZones;
                            KeyValuePair<int, Zone> zone = eZones.FirstOrDefault(f => f.Value.Name.ToLower().Contains(arg) || (int.TryParse(arg, System.Globalization.NumberStyles.Any, Data.Locale, out int o) && f.Key == o));
                            if (zone.Equals(default(KeyValuePair<int, Zone>)))
                            {
                                player.SendChat("No zone or flag found from search terms: \"" + arg + "\"", UCWarfare.GetColor("default"));
                                return;
                            }
                            player.teleportToLocation(zone.Value.Center3DAbove, 90f);
                            player.SendChat("Teleported to extra zone " + zone.Value.Name + '.', UCWarfare.GetColor("default"));
                            return;
                        }
                        player.teleportToLocation(flag.ZoneData.Center3DAbove, 90f);
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
                else if (command[0] == "time")
                {
                    UCWarfare.I.CoroutineTiming = !UCWarfare.I.CoroutineTiming;
                    player.SendChat((UCWarfare.I.CoroutineTiming ? "Enabled" : "Disabled") + " coroutine timing.", UCWarfare.GetColor("default"));
                }
                else if (command[0] == "usernames")
                {
                    FPlayerName newplayer = await Data.DatabaseManager.GetUsernames(player.channel.owner.playerID.steamID.m_SteamID);
                    F.Log(newplayer.ToString());
                    player.SendChat(newplayer.PlayerName, UCWarfare.GetColor("default"));
                    await ThreadTool.SwitchToGameThread();
                    player.SendChat(newplayer.PlayerName, UCWarfare.GetColor("default"));
                } 
                else if (command[0] == "zonearea")
                {
                    const string zonesyntax = "Syntax: /test zonearea [active|all] <show extra zones: true|false> <show path: true|false> <show range: true|false>";
                    bool all = false;
                    bool extra = false;
                    bool path = true;
                    bool range = false;
                    bool drawIn = false;
                    if (command.Length > 4)
                    {
                        if (command[1] == "all") all = true;
                        else if (command[1] != "active") player.SendChat(zonesyntax, UCWarfare.GetColor("defaulterror"));
                        if (command[2] == "true") extra = true;
                        else if (command[2] != "false") player.SendChat(zonesyntax, UCWarfare.GetColor("defaulterror"));
                        if (command[3] == "false") path = false;
                        else if (command[3] != "true") player.SendChat(zonesyntax, UCWarfare.GetColor("defaulterror"));
                        if (command[4] == "true") range = true;
                        else if (command[4] != "false") player.SendChat(zonesyntax, UCWarfare.GetColor("defaulterror"));
                        if (command[5] == "true") drawIn = true;
                        else if (command[5] != "false") player.SendChat(zonesyntax, UCWarfare.GetColor("defaulterror"));
                    }
                    else if (command.Length != 1)
                    {
                        player.SendChat("Syntax: /test zonearea <active|all> <show extra zones: true|false>", UCWarfare.GetColor("defaulterror"));
                        return;
                    }
                    List<Zone> zones = new List<Zone>();
                    foreach (Flag flag in all ? Data.FlagManager.AllFlags : Data.FlagManager.FlagRotation)
                        zones.Add(flag.ZoneData);
                    if (extra)
                    {
                        foreach (Zone zone in Data.ExtraZones.Values)
                            zones.Add(zone);
                    }
                    if (player != default)
                        player.SendChat("Picture has to generate, wait around a minute.", UCWarfare.GetColor("default"));
                    ZoneDrawing.CreateFlagTestAreaOverlay(player, zones, path, range, drawIn);
                } 
                else if (command[0] == "quickcap")
                {
                    Flag flag = Data.FlagManager.FlagRotation.FirstOrDefault(f => f.PlayersOnFlag.Contains(player));
                    if(flag == default)
                    {
                        player.SendChat("not_in_zone", UCWarfare.GetColor("default"), player.transform.position.x, player.transform.position.y, player.transform.position.z, Data.FlagManager.FlagRotation.Count);
                        return;
                    }
                    ulong team = player.GetTeam();
                    if (team == 1)
                        flag.CapT1(Flag.MaxPoints - flag.Points - 1);
                    else if (team == 2)
                        flag.CapT2(Flag.MaxPoints - flag.Points - 1);
                    else player.SendChat("You're not on a team that can capture flags.", UCWarfare.GetColor("default"));
                } 
                else if (command[0] == "quickwin")
                {
                    ulong team;
                    if (command.Length > 1 && ulong.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out ulong id))
                        team = id;
                    else team = F.GetTeam(player);
                    if (team != 1 && team != 2)
                    {
                        if (caller.DisplayName == "Console")
                            F.Log("That's not a team that can capture flags.", ConsoleColor.Red);
                        player.SendChat("You're not on a team that can capture flags.", UCWarfare.GetColor("default"));
                        return;
                    }
                    if(team == 1)
                    {
                        while(!Data.FlagManager.isScreenUp)
                        {
                            Data.FlagManager.ObjectiveTeam1.CapT1();
                        }
                    } else
                    {
                        while (!Data.FlagManager.isScreenUp)
                        {
                            Data.FlagManager.ObjectiveTeam2.CapT2();
                        }
                    }
                } 
                else if (command[0] == "playtime")
                {
                    player.SendChat("Playtime: " + F.GetTimeFromSeconds((uint)Mathf.Round(Mathf.Abs(F.GetCurrentPlaytime(player)))) + '.', UCWarfare.GetColor("default"));
                }
                else if (command[0] == "rotation")
                {
                    Data.FlagManager.PrintFlagRotation();
                }
                else if (command[0] == "lastshot")
                {
                    PlaytimeComponent c = F.GetPlaytimeComponent(player, out bool success);
                    if (success)
                    {
                        player.SendChat($"Last shot: {(c.lastShot == default(ushort) ? "none" : c.lastShot.ToString(Data.Locale))}, " +
                            $"Last projected: {(c.lastProjected == default(ushort) ? "none" : c.lastProjected.ToString(Data.Locale))}, " +
                            $"Last landmine: {(c.LastLandmineExploded.Equals(default(LandmineDataForPostAccess)) ? "none" : c.LastLandmineExploded.barricadeID.ToString(Data.Locale))}, " +
                            $"Last thrown: {(c.thrown == null || c.thrown.Count == 0 ? "none" : c.thrown.Last().asset.itemName)}", UCWarfare.GetColor("default"));
                    } else
                    {
                        player.SendChat("Could not find " + player.name + "'s PlaytimeComponent.", UCWarfare.GetColor("default"));
                    }
                } 
                else if (command[0] == "down")
                {
                    DamagePlayerParameters p = new DamagePlayerParameters()
                    {
                        applyGlobalArmorMultiplier = false,
                        respectArmor = false,
                        bleedingModifier = 0,
                        bonesModifier = 0,
                        cause = EDeathCause.KILL,
                        damage = 99,
                        direction = Vector3.down,
                        killer = player.channel.owner.playerID.steamID,
                        foodModifier = 0,
                        hallucinationModifier = 0,
                        limb = ELimb.SKULL,
                        player = player,
                        ragdollEffect = ERagdollEffect.GOLD,
                        times = 1.0f,
                        trackKill = false,
                        virusModifier = 0,
                        waterModifier = 0
                    };
                    DamageTool.damagePlayer(p, out _);
                    DamageTool.damagePlayer(p, out _);
                    player.SendChat("Applied 198 damage to player.", UCWarfare.GetColor("default"));
                } 
                else if (command[0] == "printzone")
                {
                    Flag flag = Data.FlagManager.AllFlags.FirstOrDefault(f => f.PlayerInRange(player));
                    Zone zone;
                    string zoneName;
                    string type;
                    if (flag == default(Flag))
                    {
                        List<Zone> zones = Data.ExtraZones.Values.ToList();
                        zones.Sort(delegate (Zone a, Zone b)
                        {
                            return a.BoundsArea.CompareTo(b.BoundsArea);
                        });
                        zone = zones.FirstOrDefault(z => z.IsInside(player.transform.position));
                        if (zone == default(Zone))
                        {
                            player.SendChat("not_in_zone", UCWarfare.GetColor("default"), player.transform.position.x, player.transform.position.y, player.transform.position.z, Data.FlagManager.FlagRotation.Count);
                            return;
                        }
                        else
                        {
                            zoneName = zone.Name;
                            type = "Extra Zone";
                        }
                    }
                    else
                    {
                        zone = flag.ZoneData;
                        zoneName = flag.Name;
                        type = "Flag";
                    }
                    StringBuilder sb = new StringBuilder();
                    sb.Append(type + " - " + zoneName + ":\n");
                    zone.GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center);
                    sb.Append(zone.data.type + " (" + zone.GetType().Name + ") : \"" + zone.data.data + "\"\n");
                    sb.Append($"({center.x}, {center.y})\n");
                    if (zone.GetType() == typeof(CircleZone))
                    {
                        sb.Append($"Radius: " + ((CircleZone)zone).Radius.ToString(Data.Locale) + '\n');
                    }
                    for (int i = 0; i < corners.Length; i++)
                    {
                        sb.Append($"{i}. ({corners[i].x}, {corners[i].y})\n");
                    }
                    F.Log(sb.ToString(), ConsoleColor.Cyan);
                } 
                else if (command[0] == "setflagradius")
                {
                    if (command.Length > 1 && float.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out float newValue) && !newValue.Equals(float.NaN) && !newValue.Equals(float.NegativeInfinity) && !newValue.Equals(float.PositiveInfinity))
                        ObjectivePathing.FLAG_RADIUS_SEARCH = newValue;
                }
                else if (command[0] == "setmainradius")
                {
                    if (command.Length > 1 && float.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out float newValue) && !newValue.Equals(float.NaN) && !newValue.Equals(float.NegativeInfinity) && !newValue.Equals(float.PositiveInfinity))
                        ObjectivePathing.MAIN_RADIUS_SEARCH = newValue;
                }
                else if (command[0] == "setmaxflags")
                {
                    if (command.Length > 1 && int.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out int newValue) && newValue != 0)
                        ObjectivePathing.MAX_FLAGS = newValue;
                } 
                else if (command[0] == "savemanyzones")
                {
                    if (command.Length < 2 || !uint.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out uint times))
                        times = 1U;
                    List<Zone> zones = new List<Zone>();
                    Data.FlagManager.FlagRotation.ForEach(x => { zones.Add(x.ZoneData); });
                    for(int i = 0; i < times; i++)
                    {
                        ReloadCommand.ReloadFlags();
                        ZoneDrawing.CreateFlagTestAreaOverlay(player, zones, true, false, false, true, @"ZoneExport\zonearea_" + i.ToString(Data.Locale));
                        F.Log("Done with " + (i + 1).ToString(Data.Locale) + '/' + times.ToString(Data.Locale));
                    }
                } 
                else if (command[0] == "delay")
                {
                    F.Log("Starting delay");
                    await Task.Delay(20000);
                    F.Log("Finished the delay");
                }
            }
        }
    }
}