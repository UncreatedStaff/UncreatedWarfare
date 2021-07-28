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
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.XP;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;
using System.Reflection;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Flags;

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
        public async void Execute(IRocketPlayer caller, string[] command)
        {
            Player player = caller.DisplayName == "Console" ? Provider.clients.FirstOrDefault()?.player : (caller as UnturnedPlayer).Player;
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
                        await XPManager.AddXP(target.Player, target.GetTeam(), amount, isConsole ? F.Translate("xp_from_operator", target.Steam64) :
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
                        await OfficerManager.AddOfficerPoints(target.Player, target.GetTeam(), amount, isConsole ? F.Translate("ofp_from_operator", target.Steam64) :
                            F.Translate("ofp_from_player", target.Steam64, F.GetPlayerOriginalNames(player).CharacterName.ToUpper()));
                        player.SendChat("test_giveof_success", amount.ToString(Data.Locale), amount.S(), F.GetPlayerOriginalNames(target).CharacterName);
                    }
                    else
                        player.SendChat("test_giveof_invalid_amount", command[2]);
                }
                else if (command[0] == "quickcap")
                {
                    if (Data.Gamemode is FlagGamemode fg)
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
                                await flag.CapT1(Math.Abs(flag.Points));
                            }
                            else
                            {
                                await flag.CapT1(Flag.MaxPoints - flag.Points - 1);
                            }
                        }
                        else if (team == 2)
                        {
                            if (flag.Points > 0)
                            {
                                await flag.CapT2(flag.Points);
                            }
                            else
                            {
                                await flag.CapT2(Flag.MaxPoints - flag.Points - 2);
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
                    if (Data.Gamemode is TeamCTF fg)
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
                            while (!fg.isScreenUp)
                            {
                                await fg.ObjectiveTeam1.CapT1();
                            }
                        }
                        else
                        {
                            while (!fg.isScreenUp)
                            {
                                await fg.ObjectiveTeam2.CapT2();
                            }
                        }
                    }
                    else
                    {
                        await Data.Gamemode.DeclareWin(team);
                    }
                }
                else if (command[0] == "savemanyzones")
                {
                    if (Data.Gamemode is FlagGamemode fg)
                    {
                        if (command.Length < 2 || !uint.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out uint times))
                            times = 1U;
                        List<Zone> zones = new List<Zone>();
                        if (!Directory.Exists(Data.FlagStorage + "ZoneExport\\"))
                            Directory.CreateDirectory(Data.FlagStorage + "ZoneExport\\");
                        for (int i = 0; i < times; i++)
                        {
                            zones.Clear();
                            await ReloadCommand.ReloadFlags();
                            fg.Rotation.ForEach(x => zones.Add(x.ZoneData));
                            ZoneDrawing.CreateFlagTestAreaOverlay(fg, player, zones, true, true, false, false, true, Data.FlagStorage + @"ZoneExport\zonearea_" + i.ToString(Data.Locale));
                            F.Log("Done with " + (i + 1).ToString(Data.Locale) + '/' + times.ToString(Data.Locale));
                        }
                    }
                    else player.SendChat("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
                }
                else if (command[0] == "savemanygraphs")
                {
                    if (Data.Gamemode is FlagGamemode fg)
                    {
                        if (command.Length < 2 || !uint.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out uint times))
                            times = 1U;
                        List<Zone> zones = new List<Zone>();
                        if (!Directory.Exists(Data.FlagStorage + "GraphExport\\"))
                            Directory.CreateDirectory(Data.FlagStorage + "GraphExport\\");
                        for (int i = 0; i < times; i++)
                        {
                            zones.Clear();
                            await ReloadCommand.ReloadFlags();
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
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
        private void zone(string[] command, Player player)
        {
            if (player == default)
            {
                F.LogError(F.Translate("test_no_players_console", 0, out _));
                return;
            }
            if (Data.Gamemode is FlagGamemode fg)
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
            if (Data.Gamemode is FlagGamemode fg)
            {
                Flag flag = fg.AllFlags.FirstOrDefault(f => f.PlayerInRange(player));
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
                            fg.AllFlags.Count.ToString(Data.Locale));
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
            } else
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
            if (Data.Gamemode is FlagGamemode fg)
            {
                Flag flag;
                if (fg is TeamCTF ctf)
                {
                    if (arg == "obj1" && ctf.ObjectiveTeam1 != null)
                        flag = ctf.ObjectiveTeam1;
                    else if (arg == "obj2" && ctf.ObjectiveTeam2 != null)
                        flag = ctf.ObjectiveTeam2;
                    else
                        flag = fg.AllFlags.FirstOrDefault(f => f.Name.ToLower().Contains(arg) || (int.TryParse(arg, System.Globalization.NumberStyles.Any, Data.Locale, out int o) && f.ID == o));
                }
                else
                    flag = fg.AllFlags.FirstOrDefault(f => f.Name.ToLower().Contains(arg) || (int.TryParse(arg, System.Globalization.NumberStyles.Any, Data.Locale, out int o) && f.ID == o));
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
            } else
            {
                if (UCWarfare.I.CoroutineTiming)
                    player.SendChat("test_time_enabled");
                else
                    player.SendChat("test_time_disabled");
            }
        }
        private void zonearea(string[] command, Player player)
        {
            if (Data.Gamemode is FlagGamemode fg)
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
                foreach (Flag flag in all ? fg.AllFlags : fg.Rotation)
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
            if (Data.Gamemode is FlagGamemode fg)
            {
                Flag flag = fg.AllFlags.FirstOrDefault(f => f.PlayerInRange(player));
                if (flag == default)
                {
                    player.SendChat("test_zone_test_zone_not_in_zone", player.transform.position.x.ToString(Data.Locale),
                        player.transform.position.y.ToString(Data.Locale), player.transform.position.z.ToString(Data.Locale),
                        fg.AllFlags.Count.ToString(Data.Locale));
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
            if(Data.Gamemode is FlagGamemode fg)
                fg.PrintFlagRotation();
        }
        private void down(string[] command, Player player)
        {
            if (player == default)
            {
                F.LogError(F.Translate("test_no_players_console", 0, out _));
                return;
            }
            player.life.askDamage(99, Vector3.down, EDeathCause.KILL, ELimb.SKULL, player.channel.owner.playerID.steamID, out _, false, ERagdollEffect.GOLD, false, true);
            player.life.askDamage(99, Vector3.down, EDeathCause.KILL, ELimb.SKULL, player.channel.owner.playerID.steamID, out _, false, ERagdollEffect.GOLD, false, true);
            player.SendChat("test_down_success", 198.ToString(Data.Locale));
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
            if (Data.Gamemode is FlagGamemode fg)
            {
                Flag flag = fg.AllFlags.FirstOrDefault(f => f.PlayerInRange(player));
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
                            fg.AllFlags.Count.ToString(Data.Locale));
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
    }
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE1006 // Naming Styles
}