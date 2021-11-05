using Newtonsoft.Json;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.XP;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

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
        private readonly Type type = typeof(_DebugCommand);
        public void Execute(IRocketPlayer caller, string[] command)
        {
            Player player = caller.DisplayName == "Console" ? null : (caller as UnturnedPlayer).Player;
            bool isConsole = caller.DisplayName == "Console";
            if (command.Length > 0)
            {
                // awaitable commands go in here, others go in the group of methods below...
                if (command[0] == "givexp")
                {
                    if (command.Length < 3)
                    {
                        player.SendChat("test_givexp_syntax");
                        return;
                    }
                    if (int.TryParse(command[2], out int amount))
                    {
                        UCPlayer target = UCPlayer.FromName(command[1]);
                        if (target == default)
                        {
                            player.SendChat("test_givexp_player_not_found", command[1]);
                            return;
                        }
                        XPManager.AddXP(target.Player, amount, isConsole ? F.Translate("xp_from_operator", target.Steam64) :
                            F.Translate("xp_from_player", target.Steam64, F.GetPlayerOriginalNames(player).CharacterName.ToUpper()));
                        player.SendChat("test_givexp_success", amount.ToString(Data.Locale), F.GetPlayerOriginalNames(target).CharacterName);
                    }
                    else
                        player.SendChat("test_givexp_invalid_amount", command[2].ToLower());
                }
                else if (command[0] == "giveof" || command[0] == "giveop")
                {
                    if (command.Length < 3)
                    {
                        player.SendChat("test_giveof_syntax");
                        return;
                    }
                    if (int.TryParse(command[2], out int amount))
                    {
                        UCPlayer target = UCPlayer.FromName(command[1]);
                        if (target == default)
                        {
                            player.SendChat("test_giveof_player_not_found", command[1]);
                            return;
                        }
                        OfficerManager.AddOfficerPoints(target.Player, amount, isConsole ? F.Translate("ofp_from_operator", target.Steam64) :
                            F.Translate("ofp_from_player", target.Steam64, F.GetPlayerOriginalNames(player).CharacterName.ToUpper()));
                        player.SendChat("test_giveof_success", amount.ToString(Data.Locale), amount.S(), F.GetPlayerOriginalNames(target).CharacterName);
                    }
                    else
                        player.SendChat("test_giveof_invalid_amount", command[2]);
                }
                else if (command[0] == "quickcap")
                {
                    if (Data.Is(out IFlagRotation fg))
                    {
                        Flag flag = fg.Rotation.FirstOrDefault(f => f.PlayersOnFlag.Contains(player));
                        if (flag == default)
                        {
                            player.SendChat("test_zone_not_in_zone",
                                player.transform.position.x.ToString(Data.Locale), player.transform.position.y.ToString(Data.Locale),
                                player.transform.position.z.ToString(Data.Locale), fg.Rotation.Count.ToString(Data.Locale));
                            return;
                        }
                        ulong team = player.GetTeam();
                        if (team == 1)
                        {
                            if (flag.Points < 0)
                            {
                                flag.CapT1(Math.Abs(flag.Points));
                            }
                            else
                            {
                                flag.CapT1(Flag.MAX_POINTS - flag.Points - 1);
                            }
                        }
                        else if (team == 2)
                        {
                            if (flag.Points > 0)
                            {
                                flag.CapT2(flag.Points);
                            }
                            else
                            {
                                flag.CapT2(Flag.MAX_POINTS - flag.Points - 2);
                            }
                        }
                        else player.SendChat("gamemode_flag_not_on_cap_team");
                    }
                    else player.SendChat("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
                }
                else if (command[0] == "quickwin")
                {
                    ulong team;
                    if (command.Length > 1 && ulong.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out ulong id))
                        team = id;
                    else team = F.GetTeam(player);
                    if (Data.Gamemode is IFlagTeamObjectiveGamemode fg)
                    {
                        if (team != 1 && team != 2)
                        {
                            if (caller.DisplayName == "Console")
                                F.LogError(F.Translate("gamemode_flag_not_on_cap_team_console", 0, out _));
                            player.SendChat("gamemode_flag_not_on_cap_team");
                            return;
                        }
                        if (team == 1)
                        {
                            while (fg.State == EState.ACTIVE)
                            {
                                fg.ObjectiveTeam1.CapT1();
                            }
                        }
                        else
                        {
                            while (fg.State == EState.ACTIVE)
                            {
                                fg.ObjectiveTeam2.CapT2();
                            }
                        }
                    }
                    else
                    {
                        Data.Gamemode.DeclareWin(team);
                    }
                }
                else if (command[0] == "savemanyzones")
                {
                    if (Data.Is(out IFlagRotation fg))
                    {
                        if (command.Length < 2 || !uint.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out uint times))
                            times = 1U;
                        List<Zone> zones = new List<Zone>();
                        if (!Directory.Exists(Data.FlagStorage + "ZoneExport\\"))
                            Directory.CreateDirectory(Data.FlagStorage + "ZoneExport\\");
                        for (int i = 0; i < times; i++)
                        {
                            zones.Clear();
                            ReloadCommand.ReloadFlags();
                            fg.Rotation.ForEach(x => zones.Add(x.ZoneData));
                            ZoneDrawing.CreateFlagTestAreaOverlay(fg, player, zones, true, true, false, false, true, Data.FlagStorage + @"ZoneExport\zonearea_" + i.ToString(Data.Locale));
                            F.Log("Done with " + (i + 1).ToString(Data.Locale) + '/' + times.ToString(Data.Locale));
                        }
                    }
                    else player.SendChat("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
                }
                else if (command[0] == "savemanygraphs")
                {
                    if (Data.Is(out IFlagRotation fg))
                    {
                        if (command.Length < 2 || !uint.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out uint times))
                            times = 1U;
                        List<Zone> zones = new List<Zone>();
                        if (!Directory.Exists(Data.FlagStorage + "GraphExport\\"))
                            Directory.CreateDirectory(Data.FlagStorage + "GraphExport\\");
                        for (int i = 0; i < times; i++)
                        {
                            zones.Clear();
                            ReloadCommand.ReloadFlags();
                            fg.Rotation.ForEach(x => zones.Add(x.ZoneData));
                            ZoneDrawing.DrawZoneMap(fg, Data.FlagStorage + @"GraphExport\zonegraph_" + i.ToString(Data.Locale));
                            F.Log("Done with " + (i + 1).ToString(Data.Locale) + '/' + times.ToString(Data.Locale));
                        }
                    }
                    else player.SendChat("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
                }
                else if (command[0] == "goto") go(command, player);
                else
                {
                    try
                    {
                        MethodInfo info = type.GetMethod(command[0], BindingFlags.NonPublic | BindingFlags.Instance);
                        if (info == null)
                        {
                            if (caller.DisplayName == "Console") F.LogError(F.Translate("test_no_method", 0, out _, command[0]));
                            else player.SendChat("test_no_method", command[0]);
                        }
                        else
                        {
                            try
                            {
                                info.Invoke(this, new object[2] { command, player });
                            }
                            catch (Exception ex)
                            {
                                F.LogError(ex);
                                if (caller.DisplayName == "Console") F.LogError(F.Translate("test_error_executing", 0, out _, info.Name, ex.GetType().Name));
                                else player.SendChat("test_error_executing", info.Name, ex.GetType().Name);
                            }
                        }
                    }
                    catch (AmbiguousMatchException)
                    {
                        if (caller.DisplayName == "Console") F.LogError(F.Translate("test_multiple_matches", 0, out _, command[0]));
                        else player.SendChat("test_multiple_matches", command[0]);
                    }
                }
            }
        }
#pragma warning disable IDE1006
#pragma warning disable IDE0060
#pragma warning disable IDE0051
        private void zone(string[] command, Player player)
        {
            if (player == default)
            {
                F.LogError(F.Translate("test_no_players_console", 0, out _));
                return;
            }
            if (Data.Is(out IFlagRotation fg))
            {
                Flag flag = fg.Rotation.FirstOrDefault(f => f.PlayerInRange(player));
                if (flag == default(Flag))
                {
                    player.SendChat("test_zone_not_in_zone",
                        player.transform.position.x.ToString(Data.Locale), player.transform.position.y.ToString(Data.Locale),
                        player.transform.position.z.ToString(Data.Locale), player.transform.rotation.eulerAngles.y.ToString(Data.Locale),
                        fg.Rotation.Count.ToString(Data.Locale));
                }
                else
                {
                    player.SendChat("test_zone_current_zone", flag.Name,
                        player.transform.position.x.ToString(Data.Locale), player.transform.position.y.ToString(Data.Locale),
                        player.transform.position.z.ToString(Data.Locale));
                }
            }
            else player.SendChat("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
        }
        private void sign(string[] command, Player player)
        {
            if (player == default)
            {
                F.LogError(F.Translate("test_no_players_console", 0, out _));
                return;
            }
            InteractableSign sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.look);
            if (sign == null) player.SendChat("test_sign_no_sign");
            else
            {
                player.SendChat("test_sign_success", sign.text);
                F.Log(F.Translate("test_sign_success", 0, out _, sign.text), ConsoleColor.Green);
            }
        }
        private void visualize(string[] command, Player player)
        {
            if (player == default)
            {
                F.LogError(F.Translate("test_no_players_console", 0, out _));
                return;
            }
            Zone zone;
            string zoneName;
            string zoneColor;
            if (Data.Is(out IFlagRotation fg))
            {
                Flag flag = fg.LoadedFlags.FirstOrDefault(f => f.PlayerInRange(player));
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
                        player.SendChat("test_zone_test_zone_not_in_zone", UCWarfare.GetColor("default"), player.transform.position.x.ToString(Data.Locale),
                            player.transform.position.y.ToString(Data.Locale), player.transform.position.z.ToString(Data.Locale),
                            fg.LoadedFlags.Count.ToString(Data.Locale));
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
            }
            else
            {
                List<Zone> zones = Data.ExtraZones.Values.ToList();
                zones.Sort(delegate (Zone a, Zone b)
                {
                    return a.BoundsArea.CompareTo(b.BoundsArea);
                });
                Zone extrazone = zones.FirstOrDefault(z => z.IsInside(player.transform.position));
                if (extrazone == default)
                {
                    player.SendChat("test_zone_not_in_zone", player.transform.position.x.ToString(Data.Locale),
                        player.transform.position.y.ToString(Data.Locale), player.transform.position.z.ToString(Data.Locale),
                        zones.Count.ToString(Data.Locale));
                    return;
                }
                else
                {
                    zone = extrazone;
                    zoneName = extrazone.Name;
                    zoneColor = UCWarfare.GetColorHex("default");
                }
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
                    player.SendChat("test_visualize_syntax");
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
                float y = Mathf.Max(F.GetTerrainHeightAt2DPoint(Point.x, Point.y), zone.MinHeight);
                if (y == 0) y = player.transform.position.y;
                Vector3 pos = new Vector3(Point.x, y + 0.5f, Point.y);
                F.TriggerEffectReliable(117, channel, pos);
                F.TriggerEffectReliable(120, channel, pos);
            }
            foreach (Vector2 Point in corners)
            {   // Corners
                float y = Mathf.Max(F.GetTerrainHeightAt2DPoint(Point.x, Point.y), zone.MinHeight);
                if (y == 0) y = player.transform.position.y;
                Vector3 pos = new Vector3(Point.x, y + 0.5f, Point.y);
                F.TriggerEffectReliable(115, channel, pos);
                F.TriggerEffectReliable(120, channel, pos);
            }
            {   // Center
                float y = Mathf.Max(F.GetTerrainHeightAt2DPoint(center.x, center.y), zone.MinHeight);
                if (y == 0) y = player.transform.position.y;
                Vector3 pos = new Vector3(center.x, y + 0.5f, center.y);
                F.TriggerEffectReliable(113, channel, pos);
                F.TriggerEffectReliable(120, channel, pos);
            }
            player.SendChat("test_visualize_success", (points.Length + corners.Length + 1).ToString(Data.Locale), zoneName, zoneColor);
        }
        private void go(string[] command, Player player)
        {
            if (player == default)
            {
                F.LogError(F.Translate("test_no_players_console", 0, out _));
                return;
            }
            if (command.Length == 1)
            {
                player.SendChat("test_go_syntax");
                return;
            }
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
            if (Data.Is(out IFlagRotation fg))
            {
                Flag flag;
                if (fg is TeamCTF ctf)
                {
                    if (arg == "obj1" && ctf.ObjectiveTeam1 != null)
                        flag = ctf.ObjectiveTeam1;
                    else if (arg == "obj2" && ctf.ObjectiveTeam2 != null)
                        flag = ctf.ObjectiveTeam2;
                    else
                        flag = fg.LoadedFlags.FirstOrDefault(f => f.Name.ToLower().Contains(arg) || (int.TryParse(arg, System.Globalization.NumberStyles.Any, Data.Locale, out int o) && f.ID == o));
                }
                else if (fg is Invasion inv)
                {
                    if (arg == "obj")
                        flag = (inv.ObjectiveTeam1 ?? inv.ObjectiveTeam2) ?? fg.LoadedFlags.FirstOrDefault(f => f.Name.ToLower().Contains(arg) || (int.TryParse(arg, System.Globalization.NumberStyles.Any, Data.Locale, out int o) && f.ID == o));
                    else
                        flag = fg.LoadedFlags.FirstOrDefault(f => f.Name.ToLower().Contains(arg) || (int.TryParse(arg, System.Globalization.NumberStyles.Any, Data.Locale, out int o) && f.ID == o));
                }
                else
                    flag = fg.LoadedFlags.FirstOrDefault(f => f.Name.ToLower().Contains(arg) || (int.TryParse(arg, System.Globalization.NumberStyles.Any, Data.Locale, out int o) && f.ID == o));
                if (flag == default)
                {
                    Dictionary<int, Zone> eZones = Data.ExtraZones;
                    KeyValuePair<int, Zone> zone = eZones.FirstOrDefault(f => f.Value.Name.ToLower().Contains(arg) || (int.TryParse(arg, System.Globalization.NumberStyles.Any, Data.Locale, out int o) && f.Key == o));
                    if (zone.Equals(default(KeyValuePair<int, Zone>)))
                    {
                        player.SendChat("test_go_no_zone", arg);
                        return;
                    }
                    player.teleportToLocation(zone.Value.Center3DAbove, 90f);
                    player.SendChat("test_go_success_zone", zone.Value.Name);
                    return;
                }
                player.teleportToLocation(flag.ZoneData.Center3DAbove, 90f);
                player.SendChat("test_go_success_flag", flag.Name, flag.TeamSpecificHexColor);
                return;
            }
            else
            {
                Dictionary<int, Zone> eZones = Data.ExtraZones;
                KeyValuePair<int, Zone> zone = eZones.FirstOrDefault(f => f.Value.Name.ToLower().Contains(arg) || (int.TryParse(arg, System.Globalization.NumberStyles.Any, Data.Locale, out int o) && f.Key == o));
                if (zone.Equals(default(KeyValuePair<int, Zone>)))
                {
                    player.SendChat("test_go_no_zone", arg);
                    return;
                }
                player.teleportToLocation(zone.Value.Center3DAbove, 90f);
                player.SendChat("test_go_success_zone", zone.Value.Name);
                return;
            }
        }
        private void time(string[] command, Player player)
        {
            UCWarfare.I.CoroutineTiming = !UCWarfare.I.CoroutineTiming;
            if (player == default)
            {
                if (UCWarfare.I.CoroutineTiming)
                    F.Log(F.Translate("test_time_enabled_console", 0, out _));
                else
                    F.Log(F.Translate("test_time_disabled_console", 0, out _));
            }
            else
            {
                if (UCWarfare.I.CoroutineTiming)
                    player.SendChat("test_time_enabled");
                else
                    player.SendChat("test_time_disabled");
            }
        }
        private void zonearea(string[] command, Player player)
        {
            if (Data.Is(out IFlagRotation fg))
            {
                bool all = false;
                bool extra = false;
                bool path = true;
                bool range = false;
                bool drawIn = false;
                bool drawAngles = false;
                if (command.Length > 6)
                {
                    if (command[1] == "all") all = true;
                    else if (command[1] != "active") player.SendChat("test_zonearea_syntax");
                    if (command[2] == "true") extra = true;
                    else if (command[2] != "false") player.SendChat("test_zonearea_syntax");
                    if (command[3] == "false") path = false;
                    else if (command[3] != "true") player.SendChat("test_zonearea_syntax");
                    if (command[4] == "true") range = true;
                    else if (command[4] != "false") player.SendChat("test_zonearea_syntax");
                    if (command[5] == "true") drawIn = true;
                    else if (command[5] != "false") player.SendChat("test_zonearea_syntax");
                    if (command[6] == "true") drawAngles = true;
                    else if (command[6] != "false") player.SendChat("test_zonearea_syntax");
                }
                else if (command.Length != 1)
                {
                    if (player == default)
                    {
                        F.LogError(F.Translate("test_zonearea_syntax", 0, out _));
                        return;
                    }
                    player.SendChat("test_zonearea_syntax");
                    return;
                }
                List<Zone> zones = new List<Zone>();
                foreach (Flag flag in all ? fg.LoadedFlags : fg.Rotation)
                    zones.Add(flag.ZoneData);
                if (extra)
                {
                    foreach (Zone zone in Data.ExtraZones.Values)
                        zones.Add(zone);
                }
                if (player != default)
                    player.SendChat("test_zonearea_started");
                else F.Log(F.Translate("test_zonearea_started", 0, out _));
                ZoneDrawing.CreateFlagTestAreaOverlay(fg, player, zones, path, range, drawIn, drawAngles, true);
            }
            else player.SendChat("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
        }
        private void drawzone(string[] command, Player player)
        {
            if (player == default)
            {
                F.LogError(F.Translate("test_no_players_console", 0, out _));
                return;
            }
            Zone zone;
            string zoneName;
            string zoneColor;
            if (Data.Is(out IFlagRotation fg))
            {
                Flag flag = fg.LoadedFlags.FirstOrDefault(f => f.PlayerInRange(player));
                if (flag == default)
                {
                    player.SendChat("test_zone_test_zone_not_in_zone", player.transform.position.x.ToString(Data.Locale),
                        player.transform.position.y.ToString(Data.Locale), player.transform.position.z.ToString(Data.Locale),
                        fg.LoadedFlags.Count.ToString(Data.Locale));
                    return;
                }
                else
                {
                    zone = flag.ZoneData;
                    zoneName = flag.Name;
                    zoneColor = flag.TeamSpecificHexColor;
                }
                List<Zone> zones = new List<Zone>(1) { zone };
                ZoneDrawing.CreateFlagTestAreaOverlay(fg, player, zones, false, true, false, true, true, Data.FlagStorage + "zonerange_" + zoneName);
            }
        }
        private void drawgraph(string[] command, Player player)
        {
            if (Data.Gamemode is FlagGamemode fg)
            {
                ZoneDrawing.DrawZoneMap(fg, null);
            }
            else if (player == null) F.LogError(F.Translate("gamemode_not_flag_gamemode", 0, out _, Data.Gamemode == null ? "null" : Data.Gamemode.Name));
            else player.SendChat("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
        }
        private void rotation(string[] command, Player player)
        {
            if (Data.Gamemode is FlagGamemode fg)
                fg.PrintFlagRotation();
        }
        private const byte DOWN_DAMAGE = 55;
        private void down(string[] command, Player player)
        {
            if (player == default)
            {
                F.LogError(F.Translate("test_no_players_console", 0, out _));
                return;
            }
            DamageTool.damage(player, EDeathCause.KILL, ELimb.SPINE, player.channel.owner.playerID.steamID, Vector3.down, DOWN_DAMAGE, 1, out _, false, false);
            DamageTool.damage(player, EDeathCause.KILL, ELimb.SPINE, player.channel.owner.playerID.steamID, Vector3.down, DOWN_DAMAGE, 1, out _, false, false);
            player.SendChat("test_down_success", (DOWN_DAMAGE * 2).ToString(Data.Locale));
        }
        private void layer(string[] command, Player player)
        {
            if (player == default)
            {
                F.LogError(F.Translate("test_no_players_console", 0, out _));
                return;
            }
            F.Log(F.GetLayer(player.look.aim.position, player.look.aim.forward, RayMasks.BLOCK_COLLISION), ConsoleColor.DarkCyan); // so as to not hit player
        }
        private void dumpzone(string[] command, Player player)
        {
            if (player == default)
            {
                F.LogError(F.Translate("test_no_players_console", 0, out _));
                return;
            }
            Zone zone;
            string zoneName;
            string zoneColor;
            if (Data.Is(out IFlagRotation fg))
            {
                Flag flag = fg.LoadedFlags.FirstOrDefault(f => f.PlayerInRange(player));
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
                        player.SendChat("test_zone_test_zone_not_in_zone", player.transform.position.x.ToString(Data.Locale),
                            player.transform.position.y.ToString(Data.Locale), player.transform.position.z.ToString(Data.Locale),
                            fg.LoadedFlags.Count.ToString(Data.Locale));
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
            }
            else
            {
                List<Zone> zones = Data.ExtraZones.Values.ToList();
                zones.Sort(delegate (Zone a, Zone b)
                {
                    return a.BoundsArea.CompareTo(b.BoundsArea);
                });
                Zone extrazone = zones.FirstOrDefault(z => z.IsInside(player.transform.position));
                if (extrazone == default)
                {
                    player.SendChat("test_zone_not_in_zone", player.transform.position.x.ToString(Data.Locale),
                        player.transform.position.y.ToString(Data.Locale), player.transform.position.z.ToString(Data.Locale),
                        zones.Count.ToString(Data.Locale));
                    return;
                }
                else
                {
                    zone = extrazone;
                    zoneName = extrazone.Name;
                    zoneColor = UCWarfare.GetColorHex("default");
                }
            }
            F.Log(zone.Dump(), ConsoleColor.Green);
            player.SendChat("test_check_console");
        }
        private void clearui(string[] command, Player player)
        {
            Data.SendEffectClearAll.InvokeAndLoopback(ENetReliability.Reliable, new ITransportConnection[] { player.channel.owner.transportConnection });
        }
        private void getveh(string[] command, Player player)
        {
            if (player == default)
            {
                F.LogError(F.Translate("test_no_players_console", 0, out _));
                return;
            }
            if (command.Length < 1 || float.TryParse(command[0], System.Globalization.NumberStyles.Any, Data.Locale, out float radius))
            {
                player.SendChat("Invalid syntax for getveh: /getveh <radius>");
                return;
            }
            List<InteractableVehicle> vehs = new List<InteractableVehicle>();
            VehicleManager.getVehiclesInRadius(player.transform.position, radius * radius, vehs);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < vehs.Count; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append(vehs[i].asset.vehicleName).Append(" - ").Append((vehs[i].transform.position - player.transform.position).magnitude).Append("m");
            }
            player.SendChat("Vehicles: " + sb.ToString());
        }
        private void game(string[] command, Player player)
        {
            if (Data.Is(out IFlagRotation fg))
            {
                StringBuilder flags = new StringBuilder();
                for (int f = 0; f < fg.Rotation.Count; f++)
                {
                    if (f == 0) flags.Append('\n');
                    flags.Append(fg.Rotation[f].Name).Append("\nOwner: ").Append(fg.Rotation[f].Owner).Append(" Players: \n1: ")
                        .Append(string.Join(",", fg.Rotation[f].PlayersOnFlagTeam1.Select(x => F.GetPlayerOriginalNames(x).PlayerName))).Append("\n2: ")
                        .Append(string.Join(",", fg.Rotation[f].PlayersOnFlagTeam2.Select(x => F.GetPlayerOriginalNames(x).PlayerName)))
                        .Append("\nPoints: ").Append(fg.Rotation[f].Points).Append(" State: ").Append(fg.Rotation[f].LastDeltaPoints).Append('\n');
                }
                F.Log(
                    $"Game {fg.GameID}: " +
                    $"Tickets: 1: {Tickets.TicketManager.Team1Tickets}, 2: {Tickets.TicketManager.Team2Tickets}\n" +
                    $"Starting Tickets: 1: {Tickets.TicketManager._Team1previousTickets}, 2: {Tickets.TicketManager._Team2previousTickets}\n" +
                    $"{fg.Rotation.Count} Flags: {flags}Players:\n" +
                    $"{string.Join("\n", Provider.clients.Select(x => F.GetPlayerOriginalNames(x) + " - " + (F.TryGetPlaytimeComponent(x.player, out PlaytimeComponent c) ? F.GetTimeFromSeconds((uint)Mathf.RoundToInt(c.CurrentTimeSeconds), 0) : "unknown pt")))}"// ends with \n
                    );
            }
        }
        private void migratelevels(string[] command, Player player)
        {
            if (player != null)
            {
                player.SendChat("This command can only be called from console.");
                return;
            }
            F.Log("Migrating...", ConsoleColor.Yellow);
            Data.DatabaseManager.MigrateLevels();
            F.Log("Migrated all levels.", ConsoleColor.Yellow);
        }
        private void dumpranks(string[] command, Player player)
        {
            if (player != null)
            {
                player.SendChat("This command can only be called from console.");
                return;
            }
            F.Log("Ranks: ");
            for (int i = 0; i < XPManager.config.Data.Ranks.Length; i++)
            {
                F.Log($"{XPManager.config.Data.Ranks[i].name}, {XPManager.config.Data.Ranks[i].level}, {XPManager.config.Data.Ranks[i].XP}");
            }
            F.Log("Officers: ");
            for (int i = 0; i < OfficerManager.config.Data.OfficerRanks.Length; i++)
            {
                F.Log($"{OfficerManager.config.Data.OfficerRanks[i].name}, {OfficerManager.config.Data.OfficerRanks[i].level}, {OfficerManager.config.Data.OfficerRanks[i].XP}");
            }
        }
        private void consolidateKits(string[] command, Player player)
        {
            if (player != null)
            {
                player.SendChat("This command can only be called from console.");
                return;
            }
            string[] files = Directory.GetFiles(StatsManager.StatsDirectory, "*.dat");
            List<WarfareStats.KitData> kits = new List<WarfareStats.KitData>();
            Console.WriteLine(string.Empty);
            int i = 0;
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            for (; i < files.Length; i++)
            {
                Console.CursorLeft = 0;
                Console.Write(((float)i / files.Length * 100f).ToString(Data.Locale) + "% Complete");
                FileInfo info = new FileInfo(files[i]);
                if (WarfareStats.IO.ReadFrom(info, out WarfareStats stats))
                {
                    kits.Clear();
                    for (int k = 0; k < stats.Kits.Count; k++)
                    {
                        WarfareStats.KitData existing = kits.FirstOrDefault(kit => kit.KitID == stats.Kits[k].KitID && kit.Team == stats.Kits[k].Team);
                        if (existing == default)
                        {
                            kits.Add(stats.Kits[k]);
                        }
                        else
                        {
                            existing.Kills += stats.Kits[k].Kills;
                            existing.Deaths += stats.Kits[k].Deaths;
                            existing.Downs += stats.Kits[k].Downs;
                            existing.PlaytimeMinutes += stats.Kits[k].PlaytimeMinutes;
                            existing.AverageGunKillDistance = ((existing.AverageGunKillDistance * existing.AverageGunKillDistanceCounter) + stats.Kits[k].AverageGunKillDistance) / (existing.AverageGunKillDistanceCounter + stats.Kits[k].AverageGunKillDistanceCounter);
                            existing.AverageGunKillDistanceCounter += stats.Kits[k].AverageGunKillDistanceCounter;
                            existing.Revives += stats.Kits[k].Revives;
                            existing.TimesRequested += stats.Kits[k].TimesRequested;
                        }
                    }
                    stats.Kits = kits;
                    WarfareStats.IO.WriteTo(stats, info);
                }
            }
            kits.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.CursorLeft = 0;
            Console.WriteLine("Consolidation of kits complete, " + i.ToString(Data.Locale) + " files affected.");
            Console.WriteLine();
            Console.ForegroundColor = temp;
        }
        private void playersave(string[] command, Player player)
        {
            if (command.Length != 4)
            {
                if (player == null) F.LogWarning("Syntax: /test playersave <Steam64> <Property> <Value>.");
                else player.SendChat("Syntax: /test playersave <Steam64> <Property> <Value>.");
                return;
            }
            if (ulong.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out ulong Steam64))
            {
                if (PlayerManager.HasSave(Steam64, out PlayerSave save))
                {
                    PlayerManager.SetProperty(save, command[2], command[3], out bool set, out bool parsed, out bool foundproperty, out bool allowedToChange);
                    if (!allowedToChange) // error - invalid argument value
                    {
                        if (player == null) F.LogWarning($"Couldn't change {command[2].ToUpper()} to {command[3]}, not allowed.");
                        else player.SendChat($"Couldn't change {command[2].ToUpper()} to {command[3]}, not allowed.");
                        return;
                    }
                    if (!parsed) // error - invalid argument value
                    {
                        if (player == null) F.LogWarning($"Couldn't change {command[2].ToUpper()} to {command[3]}, invalid type.");
                        else player.SendChat($"Couldn't change {command[2].ToUpper()} to {command[3]}, invalid type.");
                        return;
                    }
                    if (!foundproperty || !set) // error - invalid property name
                    {
                        if (player == null) F.LogWarning($"There is no property in [PlayerSave] called {command[2].ToUpper()}.");
                        else player.SendChat($"There is no property in PlayerSave called {command[2].ToUpper()}.");
                        return;
                    }
                    Players.FPlayerName names = Data.DatabaseManager.GetUsernames(Steam64);
                    if (player == null) F.Log($"Changed {command[2].ToUpper()} in player {names.PlayerName} to {command[3]}.");
                    else player.SendChat($"Changed {command[2].ToUpper()} in player {names.PlayerName} to {command[3]}.");
                }
                else
                {
                    if (player == null) F.LogWarning("Couldn't find a save by that ID.");
                    else player.SendChat("Couldn't find a save by that ID.");
                }
            } else
            {
                if (player == null) F.LogWarning("Couldn't parse argument [Steam64] as a [UInt64].");
                else player.SendChat("Couldn't parse to Steam64.");
            }
        }
        private void migratesaves(string[] command, Player player)
        {
            string dir = Data.KitsStorage + "playersaves.json";
            if (!File.Exists(dir))
            {
                if (player == null) F.LogWarning("No playersaves to append.");
                else player.SendChat("No playersaves to append.");
            }
            else
            {
                StreamReader r = File.OpenText(dir);
                try
                {
                    string json = r.ReadToEnd();
                    List<PlayerSave> list = JsonConvert.DeserializeObject<List<PlayerSave>>(json, new JsonSerializerSettings() { Culture = Data.Locale });

                    r.Close();
                    r.Dispose();
                    int i = 0;
                    for (; i < list.Count; i++)
                    {
                        if (!PlayerManager.ActiveObjects.Exists(x => x.Steam64 == list[i].Steam64))
                        {
                            PlayerManager.ActiveObjects.Add(list[i]);
                        }
                    }
                    PlayerManager.Write();
                    if (player == null) F.Log(i + " playersaves appended.");
                    else player.SendChat(i + " playersaves appended.");
                }
                catch (Exception ex)
                {
                    if (r != default)
                    {
                        r.Close();
                        r.Dispose();
                    }
                    if (player == null) F.LogError(ex.GetType().Name + " exception during execution.");
                    else player.SendChat(ex.GetType().Name + " exception during execution.");
                    throw;
                }
            }
        }
        private void gamemode(string[] command, Player player)
        {
            if (command.Length != 2)
            {
                if (player == null) F.LogWarning("Syntax: /test gamemode <GamemodeName>");
                else player.SendChat("Syntax:  <i>/test gamemode <GamemodeName>.</i>");
                return;
            }
            Gamemode newGamemode = Gamemode.FindGamemode(command[1]);
            try
            {
                if (newGamemode != null)
                {
                    if (Data.Gamemode != null)
                    {
                        Data.Gamemode.Dispose();
                        UnityEngine.Object.Destroy(Data.Gamemode);
                    }
                    Data.Gamemode = newGamemode;
                    SteamGameServer.SetGameDescription(F.Translate("server_desc", 0, Data.Gamemode.DisplayName));
                    Data.Gamemode.Init();
                    Data.Gamemode.OnLevelLoaded();
                    F.Broadcast("force_loaded_gamemode", Data.Gamemode.DisplayName);
                    for (int i = 0; i < Provider.clients.Count; i++)
                    {
                        if (Provider.clients[i].player.life.isDead)
                        {
                            Provider.clients[i].player.life.ReceiveRespawnRequest(false);
                        }
                        else
                        {
                            Provider.clients[i].player.teleportToLocation(F.GetBaseSpawn(Provider.clients[i], out ulong playerteam), F.GetBaseAngle(playerteam));
                        }
                        Data.Gamemode.OnPlayerJoined(UCPlayer.FromSteamPlayer(Provider.clients[i]), true);
                    }
                }
                else
                {
                    if (player == null) F.LogWarning("Failed to find gamemode: \"" + command[1] + "\".");
                    else player.SendChat("Failed to find gamemode: \"<i>" + command[1] + "</i>\".");
                }
            }
            catch (Exception ex)
            {
                F.LogError("Error loading gamemode, falling back to TeamCTF:");
                F.LogError(ex);
                if (Data.Gamemode != null)
                {
                    Data.Gamemode.Dispose();
                    UnityEngine.Object.Destroy(Data.Gamemode);
                }
                Data.Gamemode = UCWarfare.I.gameObject.AddComponent<TeamCTF>();
                SteamGameServer.SetGameDescription(F.Translate("server_desc", 0, Data.Gamemode.DisplayName));
                Data.Gamemode.Init();
                Data.Gamemode.OnLevelLoaded();
                throw;
            }
        }
        private void trackstats(string[] command, Player player)
        {
            Data.TrackStats = !Data.TrackStats;
            if (player == null) F.LogWarning("Stat tracking " + (Data.TrackStats ? "enabled." : "disabled."));
            else player.SendChat("Stat tracking " + (Data.TrackStats ? "<b>enabled</b>." : "<b>disabled</b>."));
        }
    }
#pragma warning restore IDE0051
#pragma warning restore IDE0060
#pragma warning restore IDE1006
}