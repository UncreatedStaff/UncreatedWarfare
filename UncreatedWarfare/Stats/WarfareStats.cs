using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Newtonsoft.Json;
using SDG.Unturned;
using UnityEngine;

namespace Uncreated.Warfare.Stats
{
    public class WarfareStats : StatsCollection
    {
        public const string WarfareName = "ucwarfare";
        public const string WarfareDisplayName = "Uncreated Warfare";
        public float time_deployed;
        public uint kills;
        public uint deaths;
        public uint teamkills;
        public uint credits;
        public uint xp;
        public uint level;
        public string rank;
        public string rank_abbreviation;
        public List<Team> teams;
        public Offences offences;
        public void TellKill(UCWarfare.KillEventArgs parameters)
        {
            this.kills++;
            ulong killerteam = F.GetTeam(parameters.killer);
            int teamindex = teams.FindIndex(x => x.id == killerteam);
            if(teamindex != -1)
            {
                teams[teamindex].AddKill(false, parameters);
            } else
            {
                if(killerteam == 1 || killerteam == 2)
                { 
                    Team team = new Team(killerteam, killerteam == 1 ? Teams.TeamManager.Team1Code : Teams.TeamManager.Team2Code, Teams.TeamManager.TranslateName(killerteam, 0, false));
                    team.AddKill(false, parameters);
                    teams.Add(team);
                }
            }
            Save();
        }
        [JsonConstructor]
        public WarfareStats(long playtime, float time_deployed, uint kills, uint deaths, uint teamkills, uint credits, uint xp, uint level, string rank, string rank_abbreviation, List<Team> teams, Offences offences)
        {
            this.name = WarfareName;
            this.display_name = WarfareDisplayName;
            this.playtime = playtime;
            this.time_deployed = time_deployed;
            this.kills = kills;
            this.deaths = deaths;
            this.teamkills = teamkills;
            this.credits = credits;
            this.xp = xp;
            this.level = level == default ? Teams.XP.XPtoLevel(this.xp) : level;
            this.rank = rank ?? Teams.XP.GetRankName(this.level);
            this.rank_abbreviation = rank_abbreviation ?? Teams.XP.GetRankAbbreviation(this.level);
            this.teams = teams ?? new List<Team>();
            this.offences = offences ?? new Offences();
            this.offences.OnNeedsSave += SaveEscalator;
            foreach (Team team in this.teams) team.OnNeedsSave += SaveEscalator;
        }
        public WarfareStats()
        {
            this.name = WarfareName;
            this.display_name = WarfareDisplayName;
            this.playtime = 0;
            this.time_deployed = 0;
            this.kills = 0;
            this.deaths = 0;
            this.teamkills = 0;
            this.credits = 0;
            this.xp = 0;
            this.level = 0;
            this.rank = Teams.XP.GetRankName(0);
            this.rank_abbreviation = Teams.XP.GetRankAbbreviation(0);
            this.teams = new List<Team>();
            this.offences = new Offences();
            this.offences.OnNeedsSave += SaveEscalator;
        }
    }
    public class Offences : PlayerObject
    {
        public List<Ban> bans;
        public List<Kick> kicks;
        public List<Warning> warnings;
        public List<BattleyeKick> battleye_kicks;
        public List<Teamkill> teamkills;
        [JsonConstructor]
        public Offences(List<Ban> bans, List<Kick> kicks, List<Warning> warnings, List<BattleyeKick> battleye_kicks, List<Teamkill> teamkills)
        {
            this.bans = bans;
            this.kicks = kicks;
            this.warnings = warnings;
            this.battleye_kicks = battleye_kicks;
            this.teamkills = teamkills;
            foreach (Teamkill teamkill in this.teamkills) teamkill.OnNeedsSave += SaveEscalator;
            foreach (Ban ban in this.bans) ban.OnNeedsSave += SaveEscalator;
            foreach (BattleyeKick battleye_kick in this.battleye_kicks) battleye_kick.OnNeedsSave += SaveEscalator;
            foreach (Warning warning in this.warnings) warning.OnNeedsSave += SaveEscalator;
            foreach (Kick kick in this.kicks) kick.OnNeedsSave += SaveEscalator;
        }
        public Offences()
        {
            this.bans = new List<Ban>();
            this.kicks = new List<Kick>();
            this.warnings = new List<Warning>();
            this.battleye_kicks = new List<BattleyeKick>();
            this.teamkills = new List<Teamkill>();
        }
    }
    public class Teamkill : PlayerObject
    {
        public ulong player_killed;
        public ushort item_id;
        public byte death_cause;
        public List<string> next_chats;
        public long time;
        [JsonConstructor]
        public Teamkill(ulong player_killed, ushort item_id, byte death_cause, List<string> next_chats, long time)
        {
            this.player_killed = player_killed;
            this.item_id = item_id;
            this.death_cause = death_cause;
            this.next_chats = next_chats;
            this.time = time;
        }
    }
    public class Warning : PlayerObject
    {
        public ulong admin_id;
        public string reason;
        public long time;
        [JsonConstructor]
        public Warning(ulong admin_id, string reason, long time)
        {
            this.admin_id = admin_id;
            this.reason = reason;
            this.time = time;
        }
    }
    public class Kick : PlayerObject
    {
        public ulong admin_id;
        public string reason;
        public long time;
        [JsonConstructor]
        public Kick(ulong admin_id, string reason, long time)
        {
            this.admin_id = admin_id;
            this.reason = reason;
            this.time = time;
        }
    }
    public class Ban : PlayerObject
    {
        public ulong admin_id;
        public string reason;
        /// <summary>In minutes.</summary>
        public uint duration;
        public long time;
        public long time_over;
        [JsonConstructor]
        public Ban(ulong admin_id, string reason, long time)
        {
            this.admin_id = admin_id;
            this.reason = reason;
            this.time = time;
        }
    }
    public class BattleyeKick : PlayerObject
    {
        public string reason;
        public long time;
        [JsonConstructor]
        public BattleyeKick(string reason, long time)
        {
            this.reason = reason;
            this.time = time;
        }
    }
    public class Unban : PlayerObject
    {
        public ulong admin_id;
        public long time;
        [JsonConstructor]
        public Unban(ulong admin_id, long time)
        {
            this.admin_id = admin_id;
            this.time = time;
        }
    }
    public class Team : PlayerObject
    {
        public ulong id;
        public string name;
        public string display_name;
        public uint kills;
        public uint deaths;
        public uint teamkills;
        public uint credits;
        public uint xp;
        public uint level;
        public string rank;
        public string rank_abbreviation;
        public List<Kit> kits;
        public List<string> owned_paid_kits;
        public List<KillTrack> kill_counts;
        public float time_deployed;
        public float playtime;

        [JsonConstructor]
        public Team(ulong id, string name, string display_name, uint kills, uint deaths, uint teamkills, uint credits, uint xp, uint level, string rank, string rank_abbreviation, List<Kit> kits, List<string> owned_paid_kits, List<KillTrack> kill_counts, float time_deployed, float playtime)
        {
            this.id = id;
            this.name = name ?? string.Empty;
            this.display_name = display_name ?? string.Empty;
            this.kills = kills;
            this.deaths = deaths;
            this.teamkills = teamkills;
            this.credits = credits;
            this.xp = xp;
            this.level = level == default ? Teams.XP.XPtoLevel(this.xp) : level;
            this.rank = rank ?? Teams.XP.GetRankName(this.level);
            this.rank_abbreviation = rank_abbreviation ?? Teams.XP.GetRankAbbreviation(this.level);
            this.kits = kits ?? new List<Kit>();
            this.owned_paid_kits = owned_paid_kits ?? new List<string>();
            this.kill_counts = kill_counts ?? new List<KillTrack>();
            this.time_deployed = time_deployed;
            this.playtime = playtime;
            foreach (KillTrack killtrack in this.kill_counts) killtrack.OnNeedsSave += SaveEscalator;
            foreach (Kit kit in this.kits) kit.OnNeedsSave += SaveEscalator;
        }
        public Team(ulong id, string name, string display_name)
        {
            this.id = id;
            this.name = name ?? string.Empty;
            this.display_name = display_name ?? string.Empty;
            this.kills = 0;
            this.deaths = 0;
            this.teamkills = 0;
            this.credits = 0;
            this.xp = 0;
            this.level = Teams.XP.XPtoLevel(this.xp);
            this.rank = Teams.XP.GetRankName(this.level);
            this.rank_abbreviation = Teams.XP.GetRankAbbreviation(this.level);
            this.kits = new List<Kit>();
            this.owned_paid_kits = new List<string>();
            this.kill_counts = new List<KillTrack>();
            this.time_deployed = 0;
            this.playtime = 0;
        }
        public void AddKill(bool save, UCWarfare.KillEventArgs parameters)
        {
            kills++;
            int kill_countsindex = kill_counts.FindIndex(x => x.player_id == parameters.dead.channel.owner.playerID.steamID.m_SteamID);
            if (kill_countsindex != -1)
            {
                kill_counts[kill_countsindex].kills_on++;
            }
            else
            {
                kill_counts.Add(new KillTrack(parameters.dead.channel.owner.playerID.steamID.m_SteamID, 1, 0, 0));
            }
            if (parameters.killer != null)
            {
                string kitname = LogoutSaver.GetKit(parameters.killer.channel.owner.playerID.steamID.m_SteamID);
                int kitindex = kits.FindIndex(x => x.name == kitname);
                Kit kit;
                if (kitindex != -1)
                {
                    kit = kits[kitindex];
                }
                else
                {
                    kit = new Kit(kitname, F.GetKitDisplayName(kitname));
                    kits.Add(kit);
                }
                kit.AddKill(false, parameters);
            }
            else F.Log("killer was null");
            if (save) Save();
        }
    }
    public class Kit : PlayerObject
    {
        public string name;
        public string display_name;
        public List<Item> items;
        public long playtime;
        public float time_deployed;
        public Playstyle playstyle;
        public List<Vehicle> vehicles;
        public uint wins;
        /// <summary> Similar to win/loss but is its effect to the old win loss is influenced by percent of time in the game. </summary>
        public uint win_value;
        public uint losses;
        public uint kills;
        public uint deaths;
        public uint teamkills;
        public List<Point> points;
        public void AddKill(bool save, UCWarfare.KillEventArgs parameters)
        {
            kills++;
            if (parameters.item != 0)
            {
                int itemindex = items.FindIndex(x => x.id == parameters.item);
                Item item;
                if (itemindex != -1)
                {
                    item = items[itemindex];
                    item.kills++;
                    int limbindex = item.item_kills.FindIndex(x => x.limb_id == (byte)parameters.limb);
                    if(limbindex != -1)
                    {
                        int causeindex = item.item_kills[limbindex].kills.FindIndex(x => x.type == (byte)parameters.cause);
                        if(causeindex != -1)
                        {
                            item.item_kills[limbindex].kills[causeindex].kills++;
                        } else
                        {
                            item.item_kills[limbindex].kills.Add(new LimbKill((byte)parameters.cause, 1));
                        }
                    } else
                    {
                        item.item_kills.Add(new ItemKill((byte)parameters.limb, new List<LimbKill> { new LimbKill((byte)parameters.cause, 1) }));
                    }
                }
                else
                {
                    item = new Item(parameters.item, 1, 0, 0, new List<ItemKill> { new ItemKill((byte)parameters.limb, new List<LimbKill> { new LimbKill((byte)parameters.cause, 1) }) });
                    items.Add(item);
                }
            }
            if(parameters.cause == EDeathCause.ROADKILL)
            {
                int vehicleindex = vehicles.FindIndex(x => x.id == parameters.item);
                if(vehicleindex != -1) 
                {
                    vehicles[vehicleindex].kills_by_roadkill++;
                } else
                {
                    vehicles.Add(new Vehicle(parameters.item, 0, 0, 0, 0, 0, 0, 1));
                }
            }

            decimal DistanceFromObjective = Convert.ToDecimal(Math.Sqrt(F.GetSqrDistanceFromClosestObjective(parameters.killer.transform.position, out Flags.Flag closestObjective, false)));
            if (playstyle != default)
            {
                playstyle.avg_distance_from_objective_on_kill = ((playstyle.avg_distance_from_objective_on_kill * (kills - 1)) + DistanceFromObjective) / kills;
                playstyle.avg_kill_distance = ((playstyle.avg_kill_distance * (playstyle.avg_kill_distance_counter++)) + Convert.ToDecimal(parameters.distance)) / playstyle.avg_kill_distance_counter;
                Vector2 gridSquare = F.RoundLocationToGrid(parameters.killer.transform.position);
                int squareindex = playstyle.locations.FindIndex(l => l.x == gridSquare.x && l.y == gridSquare.y);
                Location location;
                if (squareindex != -1)
                {
                    location = playstyle.locations[squareindex];
                    location.kills++;
                }
                else
                    location = new Location(gridSquare.x, gridSquare.y, 1, 0, 0, 0, 0);
            }
            if (save) Save();
        }
        [JsonConstructor]
        public Kit(string name, string display_name, List<Item> items, long playtime, float time_deployed, Playstyle playstyle, List<Vehicle> vehicles, uint wins, uint win_value, uint losses, uint kills, uint deaths, uint teamkills, List<Point> points)
        {
            this.name = name ?? string.Empty;
            this.display_name = display_name ?? string.Empty;
            this.items = items ?? new List<Item>();
            this.playtime = playtime;
            this.time_deployed = time_deployed;
            this.playstyle = playstyle ?? new Playstyle();
            this.vehicles = vehicles ?? new List<Vehicle>();
            this.wins = wins;
            this.win_value = win_value;
            this.losses = losses;
            this.kills = kills;
            this.deaths = deaths;
            this.teamkills = teamkills;
            this.points = points ?? new List<Point>();
            foreach (Point point in this.points) point.OnNeedsSave += SaveEscalator;
            foreach (Item item in this.items) item.OnNeedsSave += SaveEscalator;
            foreach (Vehicle vehicle in this.vehicles) vehicle.OnNeedsSave += SaveEscalator;
            this.playstyle.OnNeedsSave += SaveEscalator;
        }
        public Kit(string name, string display_name)
        {
            this.name = name ?? string.Empty;
            this.display_name = display_name ?? string.Empty;
            this.items = new List<Item>();
            this.playtime = 0;
            this.time_deployed = 0;
            this.playstyle = new Playstyle();
            this.vehicles = new List<Vehicle>();
            this.wins = 0;
            this.win_value = 0;
            this.losses = 0;
            this.kills = 0;
            this.deaths = 0;
            this.teamkills = 0;
            this.points = new List<Point>();
        }
    }
    public class Point : PlayerObject
    {
        public uint point_id;
        public uint point_display_name;
        public uint captures;
        /// <summary> Kills where either the killer or dead player was on point. </summary>
        public uint kills_near_point;
        /// <summary> Deaths where either the killer or dead player was on point. </summary>
        public uint deaths_near_point;
        public float time_on_point;

        [JsonConstructor]
        public Point(uint point_id, uint point_display_name, uint captures, uint kills_near_point, uint deaths_near_point, float time_on_point)
        {
            this.point_id = point_id;
            this.point_display_name = point_display_name;
            this.captures = captures;
            this.kills_near_point = kills_near_point;
            this.deaths_near_point = deaths_near_point;
            this.time_on_point = time_on_point;
        }
    }
    public class Vehicle : PlayerObject
    {
        public ushort id;
        public uint times_entered;
        public float time_in_vehicle;
        public float time_driving_vehicle;
        public uint passengers_transported;
        /// <summary>Kills by passengers whose enter and exit points were more than 200m apart and were driven by the player. Resets on death.</summary>
        public uint kills_by_recent_passengers;
        public uint kills_by_gunner;
        public uint kills_by_roadkill;
        [JsonConstructor]
        public Vehicle(ushort id, uint times_entered, float time_in_vehicle, float time_driving_vehicle, uint passengers_transported, uint kills_by_recent_passengers, uint kills_by_gunner, uint kills_by_roadkill)
        {
            this.id = id;
            this.times_entered = times_entered;
            this.time_in_vehicle = time_in_vehicle;
            this.time_driving_vehicle = time_driving_vehicle;
            this.passengers_transported = passengers_transported;
            this.kills_by_recent_passengers = kills_by_recent_passengers;
            this.kills_by_gunner = kills_by_gunner;
            this.kills_by_roadkill = kills_by_roadkill;
        }
    }
    public class Playstyle : PlayerObject
    {
        public const uint GRID_SIZE = 10;
        /// <summary> The average distance away from the closest objective a player is when they get a kill. </summary>
        public decimal avg_distance_from_objective_on_kill;
        public decimal avg_kill_distance;
        public uint avg_kill_distance_counter;
        public decimal pct_kills_from_within_gunner_seat;
        /// <summary> Kills per second by a gunner while player is the driver </summary>
        public decimal kills_per_second_while_gunner_gunning;
        public float seconds_driving;
        public uint amt_kills_by_gunner_while_driving;
        public decimal kills_on_point_capturing;
        public decimal pct_kills_on_point_clearing;
        public decimal pct_kills_on_point_losing;
        public decimal pct_kills_on_point_notobj;
        public decimal pct_kills_on_point_secured;
        public decimal pct_time_spent_in_main;
        /// <summary> Percentage of time the player spent on point out of time spent not on main. (deployed) </summary>
        public decimal pct_deploy_time_spent_on_point;
        public List<Location> locations;
        [JsonConstructor]
        public Playstyle(
            decimal avg_distance_from_objective_on_kill, 
            decimal avg_kill_distance,
            uint avg_kill_distance_counter, 
            decimal pct_kills_from_within_gunner_seat, 
            decimal kills_per_second_while_gunner_gunning, 
            float seconds_driving, 
            uint amt_kills_by_gunner_while_driving, 
            decimal pct_kills_on_point_capturing, 
            decimal pct_kills_on_point_clearing, 
            decimal pct_kills_on_point_losing, 
            decimal pct_kills_on_point_notobj, 
            decimal pct_kills_on_point_secured, 
            decimal pct_time_spent_in_main, 
            decimal pct_deploy_time_spent_on_point,
            List<Location> locations
            )
        {
            this.avg_distance_from_objective_on_kill = avg_distance_from_objective_on_kill;
            this.avg_kill_distance = avg_kill_distance;
            this.avg_kill_distance_counter = avg_kill_distance_counter;
            this.pct_kills_from_within_gunner_seat = pct_kills_from_within_gunner_seat;
            this.kills_per_second_while_gunner_gunning = kills_per_second_while_gunner_gunning;
            this.seconds_driving = seconds_driving;
            this.amt_kills_by_gunner_while_driving = amt_kills_by_gunner_while_driving;
            this.kills_on_point_capturing = pct_kills_on_point_capturing;
            this.pct_kills_on_point_clearing = pct_kills_on_point_clearing;
            this.pct_kills_on_point_losing = pct_kills_on_point_losing;
            this.pct_kills_on_point_notobj = pct_kills_on_point_notobj;
            this.pct_kills_on_point_secured = pct_kills_on_point_secured;
            this.pct_time_spent_in_main = pct_time_spent_in_main;
            this.pct_deploy_time_spent_on_point = pct_deploy_time_spent_on_point;
            this.locations = locations ?? new List<Location>();
            foreach (Location location in this.locations) location.OnNeedsSave += SaveEscalator;
        }
        public Playstyle()
        {
            this.avg_distance_from_objective_on_kill = 0;
            this.avg_kill_distance = 0;
            this.avg_kill_distance_counter = 0;
            this.pct_kills_from_within_gunner_seat = 0;
            this.kills_per_second_while_gunner_gunning = 0;
            this.seconds_driving = 0;
            this.amt_kills_by_gunner_while_driving = 0;
            this.kills_on_point_capturing = 0m;
            this.pct_kills_on_point_clearing = 0m;
            this.pct_kills_on_point_losing = 0m;
            this.pct_kills_on_point_notobj = 0m;
            this.pct_kills_on_point_secured = 0m;
            this.pct_time_spent_in_main = 1m;
            this.pct_deploy_time_spent_on_point = 1m;
            this.locations = new List<Location>();
        }
    }
    public class Location : PlayerObject
    {
        public float x;
        public float y;
        public uint kills;
        public uint deaths;
        /// <summary> Amount of times player was logged in this square. </summary>
        public uint times;
        public uint fobs_placed;
        public uint players_spawned_on_fobs_placed;

        [JsonConstructor]
        public Location(float x, float y, uint kills, uint deaths, uint times, uint fobs_placed, uint players_spawned_on_fobs_placed)
        {
            this.x = x;
            this.y = y;
            this.kills = kills;
            this.deaths = deaths;
            this.times = times;
            this.fobs_placed = fobs_placed;
            this.players_spawned_on_fobs_placed = players_spawned_on_fobs_placed;
        }
    }
    public class Item : PlayerObject
    {
        public ushort id;
        public uint kills;
        public uint deaths;
        public uint teamkills;
        public List<ItemKill> item_kills;
        [JsonConstructor]
        public Item(ushort id, uint kills, uint deaths, uint teamkills, List<ItemKill> item_kills)
        {
            this.id = id;
            this.kills = kills;
            this.deaths = deaths;
            this.teamkills = teamkills;
            this.item_kills = item_kills;
            foreach (ItemKill item_kill in this.item_kills) item_kill.OnNeedsSave += SaveEscalator;
        }
    }
    public class ItemKill : PlayerObject
    {
        public byte limb_id;
        public List<LimbKill> kills;
        [JsonConstructor]
        public ItemKill(byte limb_id, List<LimbKill> kills)
        {
            this.limb_id = limb_id;
            this.kills = kills;
            foreach (LimbKill limb_kill in this.kills) limb_kill.OnNeedsSave += SaveEscalator;
        }
        public ItemKill(ELimb limb)
        {
            this.limb_id = (byte)limb;
            this.kills = new List<LimbKill>();
        }
    }
    public class LimbKill : PlayerObject
    {
        public byte type;
        public uint kills;
        [JsonConstructor]
        public LimbKill(byte type, uint kills)
        {
            this.type = type;
            this.kills = kills;
        }
    }
    public class KillTrack : PlayerObject
    {
        public ulong player_id;
        public uint kills_on;
        public uint deaths_from;
        public uint teamkills_from;
        [JsonConstructor]
        public KillTrack(ulong player_id, uint kills_on, uint deaths_from, uint teamkills_from)
        {
            this.player_id = player_id;
            this.kills_on = kills_on;
            this.deaths_from = deaths_from;
            this.teamkills_from = teamkills_from;
        }
        public KillTrack(ulong player_id)
        {
            this.player_id = player_id;
            this.kills_on = 0;
            this.deaths_from = 0;
            this.teamkills_from = 0;
        }
    }
}
