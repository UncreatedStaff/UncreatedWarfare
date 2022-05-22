using Rocket.API;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.ReportSystem;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Commands
{
#pragma warning disable IDE1006 // Naming Styles
    internal class _DebugCommand : IRocketCommand
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int currentstep = 0;
        private readonly List<string> _aliases = new List<string>(0);
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "test";
        public string Help => "Collection of test commands.";
        public string Syntax => "/test <mode>";
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>(1) { "uc.test" };
		public List<string> Permissions => _permissions;
        private readonly Type type = typeof(_DebugCommand);
        public void Execute(IRocketPlayer caller, string[] command)
        {
            bool isConsole = caller is ConsolePlayer;
            Player? player = (caller as UnturnedPlayer)?.Player;
            if (command.Length > 0)
            {
                try
                {
                    MethodInfo info = type.GetMethod(command[0], BindingFlags.NonPublic | BindingFlags.Instance);
                    if (info == null)
                    {
                        if (player == null) L.LogError(Translation.Translate("test_no_method", 0, out _, command[0]));
                        else player.SendChat("test_no_method", command[0]);
                    }
                    else
                    {
                        try
                        {
#if DEBUG
                            using IDisposable profiler = ProfilingUtils.StartTracking(info.Name + " Debug Command");
#endif
                            info.Invoke(this, new object[2] { command, player! });
                        }
                        catch (Exception ex)
                        {
                            L.LogError(ex.InnerException ?? ex);
                            if (player == null) L.LogError(Translation.Translate("test_error_executing", 0, out _, info.Name, (ex.InnerException ?? ex).GetType().Name));
                            else player.SendChat("test_error_executing", info.Name, (ex.InnerException ?? ex).GetType().Name);
                        }
                    }
                }
                catch (AmbiguousMatchException)
                {
                    if (player == null) L.LogError(Translation.Translate("test_multiple_matches", 0, out _, command[0]));
                    else player.SendChat("test_multiple_matches", command[0]);
                }
            }
            else
            {
                if (player == null) L.LogError("Usage: /test <operation> [parameters...]");
                else player.SendChat("Usage: /test <operation> [parameters...]", Color.red);
            }
        }
#pragma warning disable IDE1006
#pragma warning disable IDE0060
#pragma warning disable IDE0051

        private void givexp(string[] command, Player player)
        {
            if (command.Length < 3)
            {
                if (player == null)
                    L.LogWarning(Translation.Translate("test_givexp_syntax", 0, out _));
                else
                    player.SendChat("test_givexp_syntax");
                return;
            }
            if (int.TryParse(command[2], out int amount))
            {
                UCPlayer? target = UCPlayer.FromName(command[1]);
                if (target == null)
                {
                    if (player == null)
                        L.LogWarning(Translation.Translate("test_givexp_player_not_found", 0, out _, command[1]));
                    else
                        player.SendChat("test_givexp_player_not_found", command[1]);
                    return;
                }
                Points.AwardXP(target, amount, player == null ? Translation.Translate("xp_from_operator", target.Steam64) :
                    Translation.Translate("xp_from_player", target.Steam64, player == null ? "Console" : F.GetPlayerOriginalNames(player).CharacterName.ToUpper()));
                if (player == null)
                    L.Log(Translation.Translate("test_givexp_success", 0, out _, amount.ToString(Data.Locale), F.GetPlayerOriginalNames(target).CharacterName));
                else
                    player.SendChat("test_givexp_success", amount.ToString(Data.Locale), F.GetPlayerOriginalNames(target).CharacterName);
            }
            else if (player == null)
                L.LogWarning(Translation.Translate("test_givexp_invalid_amount", 0, out _, command[2]));
            else
                player.SendChat("test_givexp_invalid_amount", command[2]);
        }
        private void giveofp(string[] command, Player player)
        {
            if (command.Length < 3)
            {
                if (player == null)
                    L.LogWarning(Translation.Translate("test_giveof_syntax", 0, out _));
                else
                    player.SendChat("test_giveof_syntax");
                return;
            }
            if (int.TryParse(command[2], out int amount))
            {
                UCPlayer? target = UCPlayer.FromName(command[1]);
                if (target == null)
                {
                    if (player == null)
                        L.LogWarning(Translation.Translate("test_giveof_player_not_found", 0, out _, command[1]));
                    else
                        player.SendChat("test_giveof_player_not_found", command[1]);
                    return;
                }
                if (player == null)
                    L.Log(Translation.Translate("test_giveof_success", 0, out _, amount.ToString(Data.Locale), amount.S(), F.GetPlayerOriginalNames(target).CharacterName));
                else
                    player.SendChat("test_giveof_success", amount.ToString(Data.Locale), amount.S(), F.GetPlayerOriginalNames(target).CharacterName);
            }
            else if (player == null)
                L.LogWarning(Translation.Translate("test_giveof_invalid_amount", 0, out _, command[2]));
            else
                player.SendChat("test_giveof_invalid_amount", command[2]);
        }
        private void givecredits(string[] command, Player player)
        {
            if (command.Length < 3)
            {
                if (player == null)
                    L.LogWarning(Translation.Translate("test_givecredits_syntax", 0, out _));
                else
                    player.SendChat("test_givecredits_syntax");
                return;
            }
            if (int.TryParse(command[2], out int amount))
            {
                UCPlayer? target = UCPlayer.FromName(command[1]);
                if (target == null)
                {
                    if (player == null)
                        L.LogWarning(Translation.Translate("test_givecredits_player_not_found", 0, out _, command[1]));
                    else
                        player.SendChat("test_givecredits_player_not_found", command[1]);
                    return;
                }
                Points.AwardCredits(target, amount, player == null ? Translation.Translate("credits_from_operator", target.Steam64) :
                    Translation.Translate("credits_from_player", target.Steam64, player == null ? "Console" : F.GetPlayerOriginalNames(player).CharacterName.ToUpper()));
                if (player == null)
                    L.Log(Translation.Translate("test_givecredits_success", 0, out _, amount.ToString(Data.Locale), F.GetPlayerOriginalNames(target).CharacterName));
                else
                    player.SendChat("test_givecredits_success", amount.ToString(Data.Locale), F.GetPlayerOriginalNames(target).CharacterName);
            }
            else if (player == null)
                L.LogWarning(Translation.Translate("test_givecredits_invalid_amount", 0, out _, command[2]));
            else
                player.SendChat("test_givecredits_invalid_amount", command[2]);
        }
        private void quickcap(string[] command, Player player)
        {
            if (player == null)
            {
                L.LogError(Translation.Translate("test_no_players_console", 0, out _));
                return;
            }
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
        private void quickwin(string[] command, Player player)
        {
            ulong team;
            if (command.Length > 1 && ulong.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out ulong id))
                team = id;
            else if (player != null) team = player.GetTeam();
            else team = 0;
            if (team != 1 && team != 2)
            {
                if (player == null)
                    L.LogError(Translation.Translate("gamemode_flag_not_on_cap_team_console", 0, out _));
                else
                    player.SendChat("gamemode_flag_not_on_cap_team");
                return;
            }
            Data.Gamemode.DeclareWin(team);
        }
        private void savemanyzones(string[] command, Player player)
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
                    L.Log("Done with " + (i + 1).ToString(Data.Locale) + '/' + times.ToString(Data.Locale));
                }
            }
            else if (player == null)
                L.LogWarning(Translation.Translate("gamemode_not_flag_gamemode", 0, out _, Data.Gamemode == null ? "null" : Data.Gamemode.Name));
            else
                player.SendChat("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
        }
        private void savemanygraphs(string[] command, Player player)
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
                    L.Log("Done with " + (i + 1).ToString(Data.Locale) + '/' + times.ToString(Data.Locale));
                }
            }
            else if (player == null)
                L.LogWarning(Translation.Translate("gamemode_not_flag_gamemode", 0, out _, Data.Gamemode == null ? "null" : Data.Gamemode.Name));
            else
                player.SendChat("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
        }
        private void zone(string[] command, Player player)
        {
            if (player == default)
            {
                L.LogError(Translation.Translate("test_no_players_console", 0, out _));
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
                L.LogError(Translation.Translate("test_no_players_console", 0, out _));
                return;
            }
            InteractableSign? sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.look);
            if (sign == null) player.SendChat("test_sign_no_sign");
            else
            {
                player.SendChat("test_sign_success", sign.text);
                L.Log(Translation.Translate("test_sign_success", 0, out _, sign.text), ConsoleColor.Green);
            }
        }
        private void time(string[] command, Player player)
        {
            UCWarfare.I.CoroutineTiming = !UCWarfare.I.CoroutineTiming;
            if (player == default)
            {
                if (UCWarfare.I.CoroutineTiming)
                    L.Log(Translation.Translate("test_time_enabled_console", 0, out _));
                else
                    L.Log(Translation.Translate("test_time_disabled_console", 0, out _));
            }
            else
            {
                if (UCWarfare.I.CoroutineTiming)
                    player.SendChat("test_time_enabled");
                else
                    player.SendChat("test_time_disabled");
            }
        }
        // test zones: test zonearea all true false false true false
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
                        L.LogError(Translation.Translate("test_zonearea_syntax", 0, out _));
                        return;
                    }
                    player.SendChat("test_zonearea_syntax");
                    return;
                }
                List<Zone> zones = new List<Zone>();
                if (all)
                {
                    if (extra)
                        zones.AddRange(Data.ZoneProvider.Zones);
                    else
                        zones.AddRange(Data.ZoneProvider.Zones.Where(x => x.Data.UseCase == EZoneUseCase.FLAG));
                }
                else
                    zones.AddRange(fg.Rotation.Select(x => x.ZoneData));
                if (player != default)
                    player.SendChat("test_zonearea_started");
                else L.Log(Translation.Translate("test_zonearea_started", 0, out _));
                ActionLog.Add(EActionLogType.BUILD_ZONE_MAP, "ZONEAREA", player == null ? 0 : player.channel.owner.playerID.steamID.m_SteamID);
                ZoneDrawing.CreateFlagTestAreaOverlay(fg, player, zones, path, range, drawIn, drawAngles, true);
            }
            else player.SendChat("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
        }
        private void drawzone(string[] command, Player player)
        {
            if (player == default)
            {
                L.LogError(Translation.Translate("test_no_players_console", 0, out _));
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
                ActionLog.Add(EActionLogType.BUILD_ZONE_MAP, "DRAWZONE", player == null ? 0 : player.channel.owner.playerID.steamID.m_SteamID);
                ZoneDrawing.CreateFlagTestAreaOverlay(fg, player, zones, false, true, false, true, true, Data.FlagStorage + "zonerange_" + zoneName);
            }
            else player.SendChat("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
        }
        private void drawgraph(string[] command, Player player)
        {
            if (Data.Gamemode is FlagGamemode fg)
            {
                ActionLog.Add(EActionLogType.BUILD_ZONE_MAP, "DRAWMAP", player == null ? 0 : player.channel.owner.playerID.steamID.m_SteamID);
                ZoneDrawing.DrawZoneMap(fg, null);
            }
            else if (player == null) L.LogError(Translation.Translate("gamemode_not_flag_gamemode", 0, out _, Data.Gamemode == null ? "null" : Data.Gamemode.Name));
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
                L.LogError(Translation.Translate("test_no_players_console", 0, out _));
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
                L.LogError(Translation.Translate("test_no_players_console", 0, out _));
                return;
            }
            L.Log(F.GetLayer(player.look.aim.position, player.look.aim.forward, RayMasks.BLOCK_COLLISION), ConsoleColor.DarkCyan); // so as to not hit player
        }
        private void clearui(string[] command, Player player)
        {
            Data.SendEffectClearAll.InvokeAndLoopback(ENetReliability.Reliable, new ITransportConnection[] { player.channel.owner.transportConnection });
            UCPlayer? pl = UCPlayer.FromPlayer(player);
            if (pl != null) pl.HasUIHidden = !pl.HasUIHidden;
        }
        private void reloadui(string[] command, Player player)
        {
            UCWarfare.I.UpdateLangs(player.channel.owner);
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
                if (player == null) L.LogWarning("Syntax: /test playersave <Steam64> <Property> <Value>.");
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
                        if (player == null) L.LogWarning($"Couldn't change {command[2].ToUpper()} to {command[3]}, not allowed.");
                        else player.SendChat($"Couldn't change {command[2].ToUpper()} to {command[3]}, not allowed.");
                        return;
                    }
                    if (!parsed) // error - invalid argument value
                    {
                        if (player == null) L.LogWarning($"Couldn't change {command[2].ToUpper()} to {command[3]}, invalid type.");
                        else player.SendChat($"Couldn't change {command[2].ToUpper()} to {command[3]}, invalid type.");
                        return;
                    }
                    if (!foundproperty || !set) // error - invalid property name
                    {
                        if (player == null) L.LogWarning($"There is no property in [PlayerSave] called {command[2].ToUpper()}.");
                        else player.SendChat($"There is no property in PlayerSave called {command[2].ToUpper()}.");
                        return;
                    }
                    Players.FPlayerName names = Data.DatabaseManager.GetUsernames(Steam64);
                    if (player == null) L.Log($"Changed {command[2].ToUpper()} in player {names.PlayerName} to {command[3]}.");
                    else player.SendChat($"Changed {command[2].ToUpper()} in player {names.PlayerName} to {command[3]}.");
                }
                else
                {
                    if (player == null) L.LogWarning("Couldn't find a save by that ID.");
                    else player.SendChat("Couldn't find a save by that ID.");
                }
            } else
            {
                if (player == null) L.LogWarning("Couldn't parse argument [Steam64] as a [UInt64].");
                else player.SendChat("Couldn't parse to Steam64.");
            }
        }
        private void gamemode(string[] command, Player player)
        {
            if (command.Length != 2)
            {
                if (player == null) L.LogWarning("Syntax: /test gamemode <GamemodeName>");
                else player.SendChat("Syntax:  <i>/test gamemode <GamemodeName>.</i>");
                return;
            }
            if (Data.Is(out IStagingPhase gm))
            {
                if (player == null) L.Log("Skipped staging phase.");
                else player.SendChat("Skipped staging phase.");
                gm.SkipStagingPhase();
            }
            Gamemode? newGamemode = Gamemode.FindGamemode(command[1]);
            try
            {
                if (newGamemode != null)
                {
                    ActionLog.Add(EActionLogType.CHANGE_GAMEMODE_COMMAND, newGamemode.DisplayName, player != null ? player.channel.owner.playerID.steamID.m_SteamID : 0);
                    if (Data.Gamemode != null)
                    {
                        Data.Gamemode.Dispose();
                        UnityEngine.Object.Destroy(Data.Gamemode);
                    }
                    Data.Gamemode = newGamemode;
                    Data.Gamemode.Init();
                    Data.Gamemode.OnLevelLoaded();
                    Chat.Broadcast("force_loaded_gamemode", Data.Gamemode.DisplayName);
                    for (int i = 0; i < Provider.clients.Count; i++)
                    {
                        UCPlayer? pl = UCPlayer.FromSteamPlayer(Provider.clients[i]);
                        if (pl != null)
                            Data.Gamemode.OnPlayerJoined(pl, true, false);
                    }
                }
                else
                {
                    if (player == null) L.LogWarning("Failed to find gamemode: \"" + command[1] + "\".");
                    else player.SendChat("Failed to find gamemode: \"<i>" + command[1] + "</i>\".");
                }
            }
            catch (Exception ex)
            {
                L.LogError("Error loading gamemode, falling back to TeamCTF:");
                L.LogError(ex);
                if (Data.Gamemode != null)
                {
                    Data.Gamemode.Dispose();
                    UnityEngine.Object.Destroy(Data.Gamemode);
                }
                Data.Gamemode = UCWarfare.I.gameObject.AddComponent<TeamCTF>();
                Data.Gamemode.Init();
                Data.Gamemode.OnLevelLoaded();
                throw;
            }
        }
        private void trackstats(string[] command, Player player)
        {
            Data.TrackStats = !Data.TrackStats;
            if (player == null) L.LogWarning("Stat tracking " + (Data.TrackStats ? "enabled." : "disabled."));
            else player.SendChat("Stat tracking " + (Data.TrackStats ? "<b>enabled</b>." : "<b>disabled</b>."));
        }
        private void destroyblocker(string[] command, Player player)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    for (int i = 0; i < BarricadeManager.regions[x, y].drops.Count; i++)
                    {
                        BarricadeDrop d = BarricadeManager.regions[x, y].drops[i];
                        if (d.asset.id == 36058 || d.asset.id == 36059)
                        {
                            BarricadeManager.destroyBarricade(d, (byte)x, (byte)y, ushort.MaxValue);
                        }
                    }
                }
            }
        }
        private void skipstaging(string[] command, Player player)
        {
            if (Data.Is(out IStagingPhase gm))
            {
                if (player == null) L.Log("Skipped staging phase.");
                else player.SendChat("Skipped staging phase.");
                gm.SkipStagingPhase();
            }
            else if (player == null) L.Log("Staging phase is disabled.");
            else player.SendChat("Staging phase is disabled.");
        }
        private void resetlobby(string[] command, Player player)
        {
            if (Data.Is(out ITeams t) && t.UseJoinUI)
            {
                if (command.Length < 1)
                {
                    if (player == null)
                        L.Log("Syntax: /resetlobby <Player>", ConsoleColor.Yellow);
                    else
                        player.SendChat("Syntax: /resetlobby <Player>", Color.red);
                    return;
                }
                UCPlayer? ucplayer = UCPlayer.FromName(command[1]);
                if (ucplayer == null)
                {
                    if (player == null)
                        L.Log("Unable to find a player by that name.", ConsoleColor.Yellow);
                    else
                        player.SendChat("Unable to find a player by that name.", Color.red);
                    return;
                }
                t.JoinManager.OnPlayerDisconnected(ucplayer);
                t.JoinManager.CloseUI(ucplayer);
                t.JoinManager.OnPlayerConnected(ucplayer, true);
            }
            else if (player == null)
                L.Log(Translation.Translate("gamemode_not_team_gamemode", 0, out _, Data.Gamemode == null ? "null" : Data.Gamemode.Name));
            else
                player.SendChat("gamemode_not_team_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
        }
        private void clearcooldowns(string[] command, Player player)
        {
            if (player == null)
            {
                L.Log("This command can only be called by a player.");
                return;
            }
            UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
            if (ucplayer == null)
            {
                player.SendChat("UCPlayer error", Color.yellow);
                return;
            }
            CooldownManager.RemoveCooldown(ucplayer);
        }
        private void queuetest(string[] command, Player player)
        {
            if (player == default)
            {
                L.LogError(Translation.Translate("test_no_players_console", 0, out _));
                return;
            }
            ToastMessage.QueueMessage(player, new ToastMessage("some info 1", EToastMessageSeverity.INFO));
            ToastMessage.QueueMessage(player, new ToastMessage("some info 2", EToastMessageSeverity.INFO));
            ToastMessage.QueueMessage(player, new ToastMessage("some severe info 3", EToastMessageSeverity.SEVERE));
            ToastMessage.QueueMessage(player, new ToastMessage("some warned info 4", EToastMessageSeverity.WARNING));
            ToastMessage.QueueMessage(player, new ToastMessage("some xp 5", "lots of xp", EToastMessageSeverity.MINI));
            ToastMessage.QueueMessage(player, new ToastMessage("some ofp 7", "lots of ofp", EToastMessageSeverity.MINI));
            ToastMessage.QueueMessage(player, new ToastMessage("Medium message! Very nice", EToastMessageSeverity.MEDIUM));
            ToastMessage.QueueMessage(player, new ToastMessage("Another Medium message! Wild", EToastMessageSeverity.MEDIUM));
            ToastMessage.QueueMessage(player, new ToastMessage("BIG MESSAGE HOW COOL", "actually sick ngl", EToastMessageSeverity.BIG));
            ToastMessage.QueueMessage(player, new ToastMessage("ANOTHER BIG MESSAGE HOW COOL", "blown out of my socks because of this amazing advancement in queue technology made by siege pro league player.", "This one even has BOTTOM TEXT", EToastMessageSeverity.BIG));
        }
        private void instid(string[] command, Player player)
        {
            if (player == default)
            {
                L.LogError(Translation.Translate("test_no_players_console", 0, out _));
                return;
            }
            Transform? t = UCBarricadeManager.GetTransformFromLook(player.look, RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.LARGE | RayMasks.MEDIUM | RayMasks.SMALL | RayMasks.VEHICLE);
            if (t == null)
            {
                player.SendChat("No transform found");
                return;
            }
            BarricadeDrop? bd = BarricadeManager.FindBarricadeByRootTransform(t);
            if (bd != null)
            {
                player.SendChat(bd.instanceID.ToString());
                return;
            }
            StructureDrop? dp = StructureManager.FindStructureByRootTransform(t);
            if (dp != null)
            {
                player.SendChat(dp.instanceID.ToString());
                return;
            }
            for (int i = 0; i < VehicleManager.vehicles.Count; i++)
            {
                if (VehicleManager.vehicles[i].transform == t)
                {
                    player.SendChat(VehicleManager.vehicles[i].instanceID.ToString());
                    return;
                }
            }
            for (byte b = 0; b < Regions.WORLD_SIZE; b++)
            {
                for (byte b2 = 0; b2 < Regions.WORLD_SIZE; b2++)
                {
                    for (int i = 0; i < LevelObjects.objects[b, b2].Count; i++)
                    {
                        LevelObject obj = LevelObjects.objects[b, b2][i];
                        if (obj.transform == t)
                        {
                            player.SendChat(obj.instanceID.ToString());
                            return;
                        }
                    }
                }
            }
            player.SendChat("No instanced object found");
        }
        private void linkto(string[] command, Player player)
        {
            if (player == default)
            {
                L.LogError(Translation.Translate("test_no_players_console", 0, out _));
                return;
            }
            if (command.Length < 2 || !uint.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out uint instanceid))
            {
                player.SendChat("Command usage: /test linkto <spawn's expected vb instance id>");
                return;
            }
            Transform? t = UCBarricadeManager.GetTransformFromLook(player.look, RayMasks.BARRICADE | RayMasks.STRUCTURE); 
            if (t == null)
            {
                player.SendChat("No transform found");
                return;
            }
            BarricadeDrop? bd = BarricadeManager.FindBarricadeByRootTransform(t);
            if (bd != null)
            {
                if (StructureSaver.StructureExists(bd.instanceID, EStructType.BARRICADE, out _))
                {
                    if (VehicleSpawner.SpawnExists(instanceid, EStructType.BARRICADE, out Vehicles.VehicleSpawn spawn))
                    {
                        BarricadeDrop? oldd = UCBarricadeManager.GetBarricadeFromInstID(instanceid);
                        if (oldd != null && oldd.model.gameObject.TryGetComponent(out VehicleBayComponent vbc))
                            UnityEngine.Object.Destroy(vbc);
                        spawn.SpawnPadInstanceID = bd.instanceID;
                        spawn.SpawnpadLocation = new SerializableTransform(bd.model);
                        spawn.BarricadeData = bd.GetServersideData();
                        spawn.BarricadeDrop = bd;
                        spawn.initialized = true;
                        spawn.IsActive = true;
                        if (VehicleBay.VehicleExists(spawn.VehicleID, out VehicleData data))
                            bd.model.gameObject.AddComponent<VehicleBayComponent>().Init(spawn, data);
                        else
                            L.LogError("Failed to get vehicle data for " + spawn.VehicleID.ToString("N"));
                        player.SendChat("Modified inst id from existing barricade.");
                        VehicleSpawner.Save();
                    }
                    else
                    {
                        player.SendChat("No vehicle spawn by this instance id.");
                    }
                }
                else if (VehicleSpawner.SpawnExists(instanceid, EStructType.BARRICADE, out Vehicles.VehicleSpawn spawn))
                {
                    BarricadeDrop? oldd = UCBarricadeManager.GetBarricadeFromInstID(instanceid);
                    if (oldd != null && oldd.model.gameObject.TryGetComponent(out VehicleBayComponent vbc))
                        UnityEngine.Object.Destroy(vbc);
                    StructureSaver.AddStructure(bd, bd.GetServersideData(), out _);
                    spawn.SpawnPadInstanceID = bd.instanceID;
                    spawn.SpawnpadLocation = new SerializableTransform(bd.model);
                    spawn.BarricadeData = bd.GetServersideData();
                    spawn.BarricadeDrop = bd;
                    spawn.initialized = true;
                    spawn.IsActive = true;
                    if (VehicleBay.VehicleExists(spawn.VehicleID, out VehicleData data))
                        bd.model.gameObject.AddComponent<VehicleBayComponent>().Init(spawn, data);
                    else
                        L.LogError("Failed to get vehicle data for " + spawn.VehicleID.ToString("N"));
                    player.SendChat("Modified inst id from new barricade.");
                    VehicleSpawner.Save();
                }
                else
                    player.SendChat("No vehicle spawn by this instance id.");
                return;
            }
            StructureDrop dp = StructureManager.FindStructureByRootTransform(t);
            if (dp != null)
            {
                if (StructureSaver.StructureExists(dp.instanceID, EStructType.STRUCTURE, out _))
                {
                    if (VehicleSpawner.SpawnExists(instanceid, EStructType.STRUCTURE, out Vehicles.VehicleSpawn spawn))
                    {
                        StructureDrop? oldd = UCBarricadeManager.GetStructureFromInstID(instanceid);
                        if (oldd != null && oldd.model.gameObject.TryGetComponent(out VehicleBayComponent vbc))
                            UnityEngine.Object.Destroy(vbc);
                        spawn.SpawnPadInstanceID = dp.instanceID;
                        spawn.SpawnpadLocation = new SerializableTransform(dp.model);
                        spawn.StructureData = dp.GetServersideData();
                        spawn.StructureDrop = dp;
                        spawn.initialized = true;
                        spawn.IsActive = true;
                        if (VehicleBay.VehicleExists(spawn.VehicleID, out VehicleData data))
                            dp.model.gameObject.AddComponent<VehicleBayComponent>().Init(spawn, data);
                        else
                            L.LogError("Failed to get vehicle data for " + spawn.VehicleID.ToString("N"));
                        player.SendChat("Modified inst id from existing barricade.");
                        VehicleSpawner.Save();
                    }
                    else
                    {
                        player.SendChat("No vehicle spawn by this instance id.");
                    }
                }
                else if (VehicleSpawner.SpawnExists(instanceid, EStructType.STRUCTURE, out Vehicles.VehicleSpawn spawn))
                {
                    StructureDrop? oldd = UCBarricadeManager.GetStructureFromInstID(instanceid);
                    if (oldd != null && oldd.model.gameObject.TryGetComponent(out VehicleBayComponent vbc))
                        UnityEngine.Object.Destroy(vbc);
                    StructureSaver.AddStructure(dp, dp.GetServersideData(), out _);
                    spawn.SpawnPadInstanceID = dp.instanceID;
                    spawn.SpawnpadLocation = new SerializableTransform(dp.model);
                    spawn.StructureData = dp.GetServersideData();
                    spawn.StructureDrop = dp;
                    spawn.initialized = true;
                    spawn.IsActive = true;
                    if (VehicleBay.VehicleExists(spawn.VehicleID, out VehicleData data))
                        dp.model.gameObject.AddComponent<VehicleBayComponent>().Init(spawn, data);
                    else
                        L.LogError("Failed to get vehicle data for " + spawn.VehicleID.ToString("N"));
                    player.SendChat("Modified inst id from new barricade.");
                    VehicleSpawner.Save();
                }
                else
                    player.SendChat("No vehicle spawn by this instance id.");
                return;
            }
            player.SendChat("Found no structure or barricade.");
        }
        private void fakereport(string[] command, Player player)
        {
            Report report = new ChatAbuseReport()
            {
                Message = string.Join(" ", command),
                Reporter = 76561198267927009,
                Time = DateTime.Now,
                Violator = 76561198267927009,
                ChatRecords = new string[]
                {
                    "%SPEAKER%: chat 1",
                    "%SPEAKER%: chat 2",
                    "%SPEAKER%: chat 3",
                    "[2x] %SPEAKER%: chat 4",
                }
            };
            Reporter.NetCalls.SendReportInvocation.NetInvoke(report, false);
            L.Log("Sent chat abuse report.");
        }
        private void readtest(string[] command, Player player)
        {
            Kits.RequestSigns.Reload();
            foreach (Kits.RequestSign sign in Kits.RequestSigns.ActiveObjects)
            {
                L.Log("Sign: " + sign.kit_name + " instid: " + sign.instance_id + " owner: " + sign.owner + " id: " +
                      sign.sign_id);
            }

            Kits.RequestSigns.Save();
        }
        private void testpos(string[] command, Player player)
        {
            if (player == default)
            {
                L.LogError(Translation.Translate("test_no_players_console", 0, out _));
                return;
            }

            L.Log(F.ToGridPosition(player.transform.position));
        }
        private void questdump(string[] command, Player player)
        {
            QuestManager.PrintAllQuests(player == null ? null : UCPlayer.FromPlayer(player));
        }
        private void completequest(string[] command, Player player)
        {
            if (player == default)
            {
                L.LogError(Translation.Translate("test_no_players_console", 0, out _));
                return;
            }
            if (command.Length == 2 && Enum.TryParse(command[1], true, out EQuestType type))
            {
                for (int i = 0; i < QuestManager.RegisteredTrackers.Count; i++)
                {
                    if (QuestManager.RegisteredTrackers[i].Player!.Steam64 == player.channel.owner.playerID.steamID.m_SteamID && 
                        QuestManager.RegisteredTrackers[i].QuestData?.QuestType == type)
                    {
                        QuestManager.RegisteredTrackers[i].ManualComplete();
                        break;
                    }
                }
            }
            else if (command.Length == 2 && Guid.TryParse(command[1], out Guid key))
            {
                for (int i = 0; i < QuestManager.RegisteredTrackers.Count; i++)
                {
                    if (QuestManager.RegisteredTrackers[i].Player!.Steam64 == player.channel.owner.playerID.steamID.m_SteamID &&
                        QuestManager.RegisteredTrackers[i].PresetKey == key)
                    {
                        QuestManager.RegisteredTrackers[i].ManualComplete();
                        break;
                    }
                }
            }
        }
        private void saveall(string[] command, Player player)
        {
            F.SaveProfilingData();
        }
        private void questtest(string[] command, Player player)
        {
            UCPlayer? pl = UCPlayer.FromPlayer(player);
            if (pl == null) return;
            foreach (BaseQuestData data in QuestManager.Quests)
            {
                try
                {
                    QuestManager.CreateTracker(data, pl);
                }
                catch (Exception ex)
                {
                    L.LogError(ex);
                }
            }
        }

        private const long TEST_CASES = 1000000000;
        private void zonecheckspeed(string[] command, Player player)
        {
            Vector3 outsideBoundsPos = new Vector3(-749, 36, -312);
            Vector3 insideBoundsPos = new Vector3(841, 48, -487);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < TEST_CASES; ++i)
            {
                TeamManager.Team1Main.IsInside(outsideBoundsPos);
            }

            decimal msPer = ((decimal)stopwatch.Elapsed.TotalMilliseconds) / TEST_CASES;
            stopwatch.Stop();
            L.Log("Outside bounds: " + stopwatch.Elapsed.TotalMilliseconds.ToString() + " (" + msPer.ToString("N50") + "ms) per check");
            

            stopwatch.Reset();
            stopwatch.Start();
            for (int i = 0; i < TEST_CASES; ++i)
            {
                TeamManager.Team1Main.IsInside(insideBoundsPos);
            }
            stopwatch.Stop();
            msPer = ((decimal)stopwatch.Elapsed.TotalMilliseconds) / TEST_CASES;
            L.Log("Inside bounds: " + stopwatch.Elapsed.TotalMilliseconds.ToString() + " (" + msPer.ToString("N50") + "ms) per check");
        }
#if false
        public Dictionary<string, string> conversions = new Dictionary<string, string>()
        {
            { "fr4", "prem_frmed1" },
            { "militia3", "prem_migre1" },
            { "rusni2", "prem_rusni1" },
            { "militia2", "prem_mimar1" },
            { "uscom3", "prem_uscom1" },
            { "militia1", "prem_misql1" },
            { "militia4", "prem_miap1" },
            { "rurif4", "prem_rurif1" },
            { "mozambique", "prem_mzrif1" },
            { "pl1", "prem_plrif1" },
            { "fr2", "prem_frrif1" },
            { "fr1", "prem_frbre1" },
            { "caf3", "prem_cafar1" },
            { "caf2", "prem_cafsni1" },
            { "usmc2", "prem_usmcbre1" },
            { "usmar2", "prem_usmar1" },
            { "usmc1", "prem_usmar1" },
            { "idf4", "prem_idfsni1" },
            { "usrif4", "prem_usrif1" },
            { "usmc3", "prem_usmcsni1" },
            { "usmed4", "prem_usmed1" },
            { "ussni2", "prem_ussni1" },
            { "caf1", "prem_cafrif1" },
            { "fr3", "prem_frrif1" },
            { "ger2", "prem_gerbre1" },
            { "ger1", "prem_gerrif1" },
            { "rubre3", "prem_rubre1" },
            { "rumar2", "prem_rumar1" },
            { "rumed4", "prem_rumed1" },
            { "pl3", "prem_plspec1" },
            { "pl2", "prem_plrif2" },
            { "usmc4", "prem_usmcspec1" },
            { "idf1", "prem_idfrif1" },
            { "ger4", "prem_gelat1" },
            { "ger3", "prem_gesni1" },
            { "usbre3", "prem_usbre1" },
            { "idf2", "prem_idfgre1" },
            { "soviet4", "prem_sovrif2" },
            { "soviet2", "prem_sovsni1" },
            { "soviet3", "prem_sovmar1" },
            { "soviet1", "prem_sovrif1" },
            { "idf3", "prem_idfspec1" },
            { "southafrica", "prem_sasql1" },
        };
        private void kitmerge(string[] command, Player player)
        {
            OldKit[]? kitsOld = JsonSerializer.Deserialize<OldKit[]>(File.ReadAllText(@"C:\SteamCMD\steamapps\common\U3DS\Servers\UncreatedRewrite\Rocket\Plugins\UncreatedWarfare\Kits\kits_old.json"));
            List<Kit> kitsNew = new List<Kit>();
            Utf8JsonReader reader = new Utf8JsonReader(File.ReadAllBytes(@"C:\SteamCMD\steamapps\common\U3DS\Servers\UncreatedRewrite\Rocket\Plugins\UncreatedWarfare\Kits\kits.json"), JsonEx.readerOptions);
            if (reader.TokenType == JsonTokenType.StartArray || (reader.Read() && reader.TokenType == JsonTokenType.StartArray))
            {
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        Kit next = new Kit(true);
                        next.ReadJson(ref reader);
                        kitsNew.Add(next);
                        while (reader.TokenType != JsonTokenType.EndObject && reader.Read()) ;
                    }
                }
            }
            L.Log("Old: " + (kitsOld == null ? "null" : kitsOld.Length.ToString()) + ", New: " + kitsNew.Count);
            if (kitsOld != null)
            {
                foreach (OldKit oldKit in kitsOld)
                {
                    int ind = kitsNew.FindIndex(x => x.Name.Equals(oldKit.Name, StringComparison.Ordinal));
                    if (ind == -1)
                    {
                        kitsNew.Add(new Kit(true)
                        {
                            Branch = oldKit.Branch,
                            Name = oldKit.Name,
                            Class = oldKit.Class,
                            Clothes = oldKit.Clothes.Select(x =>
                            {
                                KitClothing clothing = new KitClothing();
                                Asset a = Assets.find(EAssetType.ITEM, x.ID);
                                if (a == null) return null!;
                                clothing.id = a.GUID;
                                clothing.type = x.type;
                                return clothing;
                            }).Where(x => x != null).ToList(),
                            Items = oldKit.Items.Select(x =>
                            {
                                KitItem item = new KitItem();
                                Asset a = Assets.find(EAssetType.ITEM, x.ID);
                                if (a == null) return null!;
                                item.id = a.GUID;
                                item.x = x.x;
                                item.y = x.y;
                                item.rotation = x.rotation;
                                item.metadata = Convert.FromBase64String(x.metadata);
                                item.page = x.page;
                                item.amount = x.amount;
                                return item;
                            }).Where(x => x != null).ToList(),
                            Cooldown = oldKit.Cooldown,
                            CreditCost = 0,
                            Disabled = false,
                            IsLoadout = oldKit.IsLoadout,
                            IsPremium = oldKit.IsPremium,
                            PremiumCost = oldKit.PremiumCost,
                            SignTexts = oldKit.SignTexts,
                            Skillsets = new Skillset[0],
                            Team = oldKit.Team,
                            TeamLimit = oldKit.TeamLimit,
                            UnlockLevel = oldKit.RequiredLevel,
                            UnlockRequirements = oldKit.RequiredLevel == 0 ? new BaseUnlockRequirement[0] : new BaseUnlockRequirement[1] { new LevelUnlockRequirement() { UnlockLevel = oldKit.RequiredLevel } },
                            Weapons = oldKit.Weapons,
                            AllowedUsers = oldKit.AllowedUsers
                        });
                    }
                    else
                    {
                        Kit kit = kitsNew[ind];
                        kit.AllowedUsers.AddRange(oldKit.AllowedUsers);
                        kit.AllowedUsers = kit.AllowedUsers.Distinct().ToList();
                    }
                }
            }
            for (int i = kitsNew.Count - 1; i >= 0; i--)
            {
                Kit kit = kitsNew[i];
                if (conversions.TryGetValue(kit.Name, out string other))
                {
                    kitsNew.RemoveAll(x => x.Name.Equals(other, StringComparison.Ordinal));
                    kit.Name = other;
                }
            }
            kitsNew.Sort((a, b) => a.Name.CompareTo(b.Name));
            kitsNew.Sort((a, b) => b.IsPremium.CompareTo(a.IsPremium));
            kitsNew.Sort((a, b) => b.IsLoadout.CompareTo(a.IsLoadout));
            L.Log(kitsNew.Count.ToString());
            Task.Run(async () =>
            {
                int i = 0;
                foreach (Kit kit in kitsNew)
                {
                    L.Log(kit.Name);
                    try
                    {
                        await KitManager.AddKit(kit);
                    }
                    catch (Exception ex)
                    {
                        L.LogError(ex);
                    }
                    L.Log(++i + " / " + kitsNew.Count);
                }
            });
        }
#endif
    }
#pragma warning restore IDE0051
#pragma warning restore IDE0060
#pragma warning restore IDE1006
}