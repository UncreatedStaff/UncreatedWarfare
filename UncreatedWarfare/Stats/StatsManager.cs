using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Networking;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using UnityEngine;

namespace Uncreated.Warfare.Stats;

public static class StatsManager
{
    public static readonly string SaveDirectory = Path.Combine(Data.Paths.BaseDirectory, "Stats") + Path.DirectorySeparatorChar;
    public static readonly string StatsDirectory = Path.Combine(SaveDirectory, "Players") + Path.DirectorySeparatorChar;
    public static readonly string WeaponsDirectory = Path.Combine(SaveDirectory, "Weapons") + Path.DirectorySeparatorChar;
    public static readonly string VehiclesDirectory = Path.Combine(SaveDirectory, "Vehicles") + Path.DirectorySeparatorChar;
    public static readonly string KitsDirectory = Path.Combine(SaveDirectory, "Kits") + Path.DirectorySeparatorChar;
    public static WarfareTeam Team1Stats;
    public static WarfareTeam Team2Stats;
    public static readonly List<WarfareWeapon> Weapons = new List<WarfareWeapon>();
    public static readonly List<WarfareKit> Kits = new List<WarfareKit>();
    public static readonly List<WarfareVehicle> Vehicles = new List<WarfareVehicle>();
    public static readonly List<WarfareStats> OnlinePlayers = new List<WarfareStats>();
    private static int _weaponCounter;
    private static int _kitCounter;
    private static int _vehicleCounter;
    private static int _teamBackupCounter;
    private static int _minsCounter;
    public static void LoadEvents()
    {
        EventDispatcher.PlayerDied += OnPlayerDied;
    }
    internal static void UnloadEvents()
    {
        EventDispatcher.PlayerDied -= OnPlayerDied;
    }
    public static void LoadTeams()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string t1 = Path.Combine(SaveDirectory, "team1.dat");
        string t2 = Path.Combine(SaveDirectory, "team2.dat");
        WarfareTeam.IO.InitializeTo(
            () => new WarfareTeam
            {
                DATA_VERSION = WarfareTeam.CURRENT_DATA_VERSION,
                Team = 1,
                Kills = 0,
                Deaths = 0,
                Downs = 0,
                EmplacementsBuilt = 0,
                FlagsCaptured = 0,
                FlagsLost = 0,
                FobsBuilt = 0,
                FobsDestroyed = 0,
                FortificationsBuilt = 0,
                Losses = 0,
                Revives = 0,
                Teamkills = 0,
                VehiclesDestroyed = 0,
                VehiclesRequested = 0,
                Wins = 0
            },
            t1
        );
        WarfareTeam.IO.InitializeTo(
            () => new WarfareTeam
            {
                DATA_VERSION = WarfareTeam.CURRENT_DATA_VERSION,
                Team = 2,
                Kills = 0,
                Deaths = 0,
                Downs = 0,
                EmplacementsBuilt = 0,
                FlagsCaptured = 0,
                FlagsLost = 0,
                FobsBuilt = 0,
                FobsDestroyed = 0,
                FortificationsBuilt = 0,
                Losses = 0,
                Revives = 0,
                Teamkills = 0,
                VehiclesDestroyed = 0,
                VehiclesRequested = 0,
                Wins = 0
            },
            t2
        );
        WarfareTeam.IO.ReadFrom(t1, out Team1Stats);
        WarfareTeam.IO.ReadFrom(t2, out Team2Stats);
        if (Team1Stats.DATA_VERSION != WarfareTeam.CURRENT_DATA_VERSION || Team1Stats.Team != 1)
        {
            Team1Stats.DATA_VERSION = WarfareTeam.CURRENT_DATA_VERSION;
            Team1Stats.Team = 1;
            WarfareTeam.IO.WriteTo(Team1Stats, t1);
        }
        if (Team2Stats.DATA_VERSION != WarfareTeam.CURRENT_DATA_VERSION || Team2Stats.Team != 2)
        {
            Team2Stats.DATA_VERSION = WarfareTeam.CURRENT_DATA_VERSION;
            Team2Stats.Team = 2;
            WarfareTeam.IO.WriteTo(Team2Stats, t2);
        }
    }
    const int TICK_SPEED_MINS = 5;
    public static void BackupTick()
    {
        if (_minsCounter > TICK_SPEED_MINS - 1)
            _minsCounter = 0;
        if (_minsCounter != TICK_SPEED_MINS - 1)
        {
            _minsCounter++;
            return;
        }
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_weaponCounter >= Weapons.Count)
            _weaponCounter = 0;
        if (_vehicleCounter >= Vehicles.Count)
            _vehicleCounter = 0;
        if (_kitCounter >= Kits.Count)
            _kitCounter = 0;
        if (_teamBackupCounter > 60)
            _teamBackupCounter = 0;
        if (Weapons.Count > 0)
        {
            NetCalls.BackupWeapon.NetInvoke(Weapons[_weaponCounter]);
            if (UCWarfare.Config.Debug)
                L.Log("[WEAPON] Backed up: " + (Assets.find(EAssetType.ITEM, Weapons[_weaponCounter].ID) is ItemAsset asset ?
                    (asset.itemName + " - " + Weapons[_weaponCounter].KitID) :
                    (Weapons[_weaponCounter].ID.ToString() + " - " + Weapons[_weaponCounter].KitID)));
        }
        if (Vehicles.Count > 0)
        {
            NetCalls.BackupVehicle.NetInvoke(Vehicles[_vehicleCounter]);
            if (UCWarfare.Config.Debug)
                L.Log("[VEHICLE] Backed up: " + (Assets.find(EAssetType.VEHICLE, Vehicles[_vehicleCounter].ID) is VehicleAsset asset ?
                    asset.vehicleName :
                    Vehicles[_vehicleCounter].ID.ToString()));
        }
        if (Kits.Count > 0)
        {
            NetCalls.BackupKit.NetInvoke(Kits[_kitCounter]);
            if (UCWarfare.Config.Debug)
                L.Log("[KITS] Backed up: " + Kits[_kitCounter].KitID);
        }
        if (_teamBackupCounter == 30)
        {
            NetCalls.BackupTeam.NetInvoke(Team1Stats);
            L.LogDebug("[TEAMS] Backed up: TEAM 1");
        }
        else if (_teamBackupCounter == 60)
        {
            NetCalls.BackupTeam.NetInvoke(Team2Stats);
            L.LogDebug("[TEAMS] Backed up: TEAM 2");
        }
        _weaponCounter++;
        _vehicleCounter++;
        _kitCounter++;
        _teamBackupCounter++;
        _minsCounter++;
    }
    public static void ModifyTeam(byte team, Action<WarfareTeam> modification, bool save = true)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.TrackStats) return;
        if (team == 1)
        {
            modification.Invoke(Team1Stats);
            if (save) WarfareTeam.IO.WriteTo(Team1Stats, Path.Combine(SaveDirectory, "team1.dat"));
        }
        else if (team == 2)
        {
            modification.Invoke(Team2Stats);
            if (save) WarfareTeam.IO.WriteTo(Team2Stats, Path.Combine(SaveDirectory, "team2.dat"));
        }
    }
    public static void ModifyTeam(ulong team, Action<WarfareTeam> modification, bool save = true)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.TrackStats) return;
        if (team == 1)
        {
            modification.Invoke(Team1Stats);
            if (save) WarfareTeam.IO.WriteTo(Team1Stats, Path.Combine(SaveDirectory, "team1.dat"));
        }
        else if (team == 2)
        {
            modification.Invoke(Team2Stats);
            if (save) WarfareTeam.IO.WriteTo(Team2Stats, Path.Combine(SaveDirectory, "team2.dat"));
        }
    }
    public static void SaveTeams()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        WarfareTeam.IO.WriteTo(Team1Stats, Path.Combine(SaveDirectory, "team1.dat"));
        WarfareTeam.IO.WriteTo(Team2Stats, Path.Combine(SaveDirectory, "team2.dat"));
    }
    public static void LoadWeapons()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Directory.Exists(WeaponsDirectory))
            Directory.CreateDirectory(WeaponsDirectory);
        string[] weapons = Directory.GetFiles(WeaponsDirectory);
        for (int i = 0; i < weapons.Length; i++)
        {
            FileInfo file = new FileInfo(weapons[i]);
            if (WarfareWeapon.IO.ReadFrom(file, out WarfareWeapon weapon) && weapon != null)
            {
                if (weapon.DATA_VERSION != WarfareWeapon.CURRENT_DATA_VERSION)
                {
                    weapon.DATA_VERSION = WarfareWeapon.CURRENT_DATA_VERSION;
                    WarfareWeapon.IO.WriteTo(weapon, file);
                }
                if (!Weapons.Exists(x => x.ID == weapon.ID && x.KitID == weapon.KitID))
                    Weapons.Add(weapon);
            }
            else
            {
                L.LogWarning("Invalid weapon file: " + file.FullName);
            }
        }
    }
    public static void LoadVehicles()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Directory.Exists(VehiclesDirectory))
            Directory.CreateDirectory(VehiclesDirectory);
        string[] vehicles = Directory.GetFiles(VehiclesDirectory);
        for (int i = 0; i < vehicles.Length; i++)
        {
            FileInfo file = new FileInfo(vehicles[i]);
            if (WarfareVehicle.IO.ReadFrom(file, out WarfareVehicle vehicle) && vehicle != null)
            {
                if (vehicle.DATA_VERSION != WarfareVehicle.CURRENT_DATA_VERSION)
                {
                    vehicle.DATA_VERSION = WarfareVehicle.CURRENT_DATA_VERSION;
                    WarfareVehicle.IO.WriteTo(vehicle, file);
                }
                if (!Vehicles.Exists(x => x.ID == vehicle.ID))
                    Vehicles.Add(vehicle);
            }
            else
            {
                L.LogWarning("Invalid vehicle file: " + file.FullName);
            }
        }
    }
    public static void LoadKits()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Directory.Exists(KitsDirectory))
            Directory.CreateDirectory(KitsDirectory);
        string[] kits = Directory.GetFiles(KitsDirectory);
        for (int i = 0; i < kits.Length; i++)
        {
            FileInfo file = new FileInfo(kits[i]);
            if (WarfareKit.IO.ReadFrom(file, out WarfareKit kit) && kit != null)
            {
                if (kit.DATA_VERSION != WarfareKit.CURRENT_DATA_VERSION)
                {
                    kit.DATA_VERSION = WarfareKit.CURRENT_DATA_VERSION;
                    WarfareKit.IO.WriteTo(kit, file);
                }
                if (!Kits.Exists(x => x.KitID == kit.KitID))
                    Kits.Add(kit);
            }
            else
            {
                L.LogWarning("Invalid kit file: " + file.FullName);
            }
        }
    }
    private static string GetWeaponName(ushort id, string kitId) => $"{id}_{kitId.RemoveMany(false, Data.Paths.BadFileNameCharacters)}.dat";
    public static bool ModifyWeapon(ushort id, string kitId, Action<WarfareWeapon> modification, bool save = true)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.TrackStats) return false;
        string dir = Path.Combine(WeaponsDirectory, GetWeaponName(id, kitId));
        for (int i = 0; i < Weapons.Count; i++)
        {
            if (Weapons[i].ID == id && Weapons[i].KitID == kitId)
            {
                modification.Invoke(Weapons[i]);
                if (save) WarfareWeapon.IO.WriteTo(Weapons[i], dir);
                return true;
            }
        }
        if (File.Exists(dir) && WarfareWeapon.IO.ReadFrom(dir, out WarfareWeapon weapon) && weapon != null)
        {
            weapon.DATA_VERSION = WarfareWeapon.CURRENT_DATA_VERSION;
            modification.Invoke(weapon);
            Weapons.Add(weapon);
            WarfareWeapon.IO.WriteTo(weapon, dir);
            return true;
        }
        weapon = new WarfareWeapon()
        {
            DATA_VERSION = WarfareWeapon.CURRENT_DATA_VERSION,
            ID = id,
            KitID = kitId
        };
        modification.Invoke(weapon);
        Weapons.Add(weapon);
        WarfareWeapon.IO.WriteTo(weapon, dir);
        return true;
    }
    public static bool ModifyVehicle(ushort id, Action<WarfareVehicle> modification, bool save = true)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.TrackStats) return false;
        string dir = Path.Combine(VehiclesDirectory, id.ToString(Data.Locale) + ".dat");
        for (int i = 0; i < Vehicles.Count; i++)
        {
            if (Vehicles[i].ID == id)
            {
                modification.Invoke(Vehicles[i]);
                if (save) WarfareVehicle.IO.WriteTo(Vehicles[i], dir);
                return true;
            }
        }
        if (File.Exists(dir) && WarfareVehicle.IO.ReadFrom(dir, out WarfareVehicle weapon) && weapon != null)
        {
            weapon.DATA_VERSION = WarfareVehicle.CURRENT_DATA_VERSION;
            modification.Invoke(weapon);
            Vehicles.Add(weapon);
            WarfareVehicle.IO.WriteTo(weapon, dir);
            return true;
        }
        weapon = new WarfareVehicle()
        {
            DATA_VERSION = WarfareVehicle.CURRENT_DATA_VERSION,
            ID = id
        };
        modification.Invoke(weapon);
        Vehicles.Add(weapon);
        WarfareVehicle.IO.WriteTo(weapon, dir);
        return true;
    }
    public static bool ModifyStats(ulong s64, Action<WarfareStats> modification, bool save = true)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.TrackStats) return false;
        string dir = Path.Combine(StatsDirectory, s64.ToString(Data.Locale) + ".dat");
        for (int i = 0; i < OnlinePlayers.Count; i++)
        {
            if (OnlinePlayers[i].Steam64 == s64)
            {
                modification.Invoke(OnlinePlayers[i]);
                if (save) WarfareStats.IO.WriteTo(OnlinePlayers[i], dir);
                return true;
            }
        }
        if (File.Exists(dir) && WarfareStats.IO.ReadFrom(dir, out WarfareStats stats) && stats != null)
        {
            stats.DATA_VERSION = WarfareWeapon.CURRENT_DATA_VERSION;
            modification.Invoke(stats);
            WarfareStats.IO.WriteTo(stats, dir);
            return true;
        }
        stats = new WarfareStats()
        {
            DATA_VERSION = WarfareStats.CURRENT_DATA_VERSION,
            Kits = new List<WarfareStats.KitData>(),
            Steam64 = s64
        };
        modification.Invoke(stats);
        WarfareStats.IO.WriteTo(stats, dir);
        return true;
    }
    public static bool ModifyKit(string kitId, Action<WarfareKit> modification, bool save = true)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.TrackStats) return false;
        string dir = Path.Combine(KitsDirectory, kitId + ".dat");
        for (int i = 0; i < Kits.Count; i++)
        {
            if (Kits[i].KitID == kitId)
            {
                modification.Invoke(Kits[i]);
                if (save) WarfareKit.IO.WriteTo(Kits[i], dir);
                return true;
            }
        }
        if (File.Exists(dir) && WarfareKit.IO.ReadFrom(dir, out WarfareKit kit) && kit != null)
        {
            kit.DATA_VERSION = WarfareWeapon.CURRENT_DATA_VERSION;
            modification.Invoke(kit);
            Kits.Add(kit);
            WarfareKit.IO.WriteTo(kit, dir);
            return true;
        }
        kit = new WarfareKit()
        {
            DATA_VERSION = WarfareKit.CURRENT_DATA_VERSION,
            KitID = kitId
        };
        modification.Invoke(kit);
        Kits.Add(kit);
        WarfareKit.IO.WriteTo(kit, dir);
        return true;
    }
    public static void RegisterPlayer(ulong s64)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Directory.Exists(StatsDirectory))
            Directory.CreateDirectory(StatsDirectory);
        string dir = Path.Combine(StatsDirectory, s64.ToString(Data.Locale) + ".dat");
        if (!OnlinePlayers.Exists(x => x.Steam64 == s64))
        {
            if (File.Exists(dir))
            {
                if (WarfareStats.IO.ReadFrom(dir, out WarfareStats stats) && stats != null)
                {
                    OnlinePlayers.Add(stats);
                }
                else
                {
                    // copy to new file appended with _corrupt
                    byte[] bytes;
                    using (FileStream fs = new FileStream(dir, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        bytes = new byte[fs.Length];
                        fs.Read(bytes, 0, bytes.Length);
                        fs.Close();
                        fs.Dispose();
                    }
                    using (FileStream fs = new FileStream(Path.Combine(StatsDirectory, s64.ToString(Data.Locale) + "_corrupt.dat"),
                               FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                    {
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Close();
                        fs.Dispose();
                    }
                    L.LogWarning("Failed to read " + s64.ToString(Data.Locale) + "'s stat file, creating a backup and resetting it.");
                    WarfareStats reset = new WarfareStats()
                    {
                        DATA_VERSION = WarfareStats.CURRENT_DATA_VERSION,
                        Kits = new List<WarfareStats.KitData>(),
                        Steam64 = s64
                    };
                    WarfareStats.IO.WriteTo(reset, dir);
                    OnlinePlayers.Add(reset);
                }
            }
            else
            {
                WarfareStats reset = new WarfareStats()
                {
                    DATA_VERSION = WarfareStats.CURRENT_DATA_VERSION,
                    Kits = new List<WarfareStats.KitData>(),
                    Steam64 = s64
                };
                WarfareStats.IO.WriteTo(reset, dir);
                OnlinePlayers.Add(reset);
            }
        }
    }
    public static void DeregisterPlayer(ulong s64)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        WarfareStats? stats = OnlinePlayers.FirstOrDefault(x => x.Steam64 == s64);
        if (stats == null) return;
        WarfareStats.IO.WriteTo(stats, Path.Combine(StatsDirectory, s64.ToString(Data.Locale) + ".dat"));
        NetCalls.BackupStats.NetInvoke(stats);
        OnlinePlayers.Remove(stats);
    }
    private static void OnPlayerDied(PlayerDied e)
    {
        Kit kit;
        if (e.Killer is not null)
        {
            if (e.WasTeamkill)
            {
                Task.Run(() => Data.DatabaseManager.AddTeamkill(e.Killer.Steam64, e.KillerTeam));
                ModifyStats(e.Killer.Steam64, s => s.Teamkills++, false);
                ModifyTeam(e.KillerTeam, t => t.Teamkills++, false);
            }
            else
            {
                Task.Run(() => Data.DatabaseManager.AddKill(e.Killer.Steam64, e.KillerTeam));
                ModifyStats(e.Killer.Steam64, s => s.Kills++, false);
                ModifyTeam(e.KillerTeam, t => t.Kills++, false);
                if (e.TurretVehicleOwner != default && Assets.find(e.TurretVehicleOwner) is VehicleAsset vasset)
                    ModifyVehicle(vasset.id, v => v.KillsWithGunner++);
                bool atk = false;
                bool def = false;
                Vector3 kilPos = e.Killer.Position;
                if (Data.Is(out IFlagRotation fg))
                {
                    for (int f = 0; f < fg.Rotation.Count; f++)
                    {
                        Gamemodes.Flags.Flag flag = fg.Rotation[f];
                        if (flag.ZoneData.IsInside(kilPos))
                        {
                            def = flag.IsContested(out ulong winner) || winner != e.KillerTeam;
                            atk = !def;
                            break;
                        }
                    }
                }
                if (KitManager.HasKit(e.Killer, out kit))
                {
                    ModifyStats(e.Killer.Steam64, s =>
                    {
                        s.Kills++;
                        WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == e.KillerTeam);
                        if (kitData == default)
                        {
                            kitData = new WarfareStats.KitData() { KitID = kit.Name, Team = (byte)e.KillerTeam, Kills = 1 };
                            if (e.Cause is EDeathCause.GUN or EDeathCause.SPLASH)
                                kitData.AverageGunKillDistance = (kitData.AverageGunKillDistance * kitData.AverageGunKillDistanceCounter + e.KillDistance) / ++kitData.AverageGunKillDistanceCounter;
                            s.Kits.Add(kitData);
                        }
                        else
                        {
                            kitData.Kills++;
                            if (e.Cause is EDeathCause.GUN or EDeathCause.SPLASH)
                                kitData.AverageGunKillDistance = (kitData.AverageGunKillDistance * kitData.AverageGunKillDistanceCounter + e.KillDistance) / ++kitData.AverageGunKillDistanceCounter;
                        }
                        if (atk)
                        {
                            s.KillsWhileAttackingFlags++;
                        }
                        else if (def)
                        {
                            s.KillsWhileDefendingFlags++;
                        }
                    }, false);
                }
                else
                    ModifyStats(e.Killer.Steam64, s =>
                    {
                        s.Kills++;
                        if (atk) s.KillsWhileAttackingFlags++;
                        else if (def) s.KillsWhileDefendingFlags++;
                    }, false);

                if (e.KitName is not null && KitManager.KitExists(e.KitName, out kit) && !e.PrimaryAssetIsVehicle && Assets.find(e.PrimaryAsset) is ItemAsset asset)
                {
                    ModifyWeapon(asset.id, kit.Name, x =>
                    {
                        x.Kills++;
                        switch (e.Limb)
                        {
                            case ELimb.SKULL:
                                x.SkullKills++;
                                break;
                            case ELimb.SPINE or ELimb.LEFT_FRONT or ELimb.RIGHT_FRONT or ELimb.LEFT_BACK or ELimb.RIGHT_BACK:
                                x.BodyKills++;
                                break;
                            case ELimb.LEFT_HAND or ELimb.RIGHT_HAND or ELimb.LEFT_ARM or ELimb.RIGHT_ARM:
                                x.BodyKills++;
                                break;
                            case ELimb.LEFT_FOOT or ELimb.RIGHT_FOOT or ELimb.LEFT_LEG or ELimb.RIGHT_LEG:
                                x.LegKills++;
                                break;
                        }
                        x.AverageKillDistance = (x.AverageKillDistance * x.AverageKillDistanceCounter + e.KillDistance) / ++x.AverageKillDistanceCounter;
                    }, true);
                    ModifyKit(kit.Name, k =>
                    {
                        k.Kills++;
                        if (e.Cause == EDeathCause.GUN)
                            k.AverageGunKillDistance =
                                (k.AverageGunKillDistance * k.AverageGunKillDistanceCounter + e.KillDistance) / ++k.AverageGunKillDistanceCounter;
                    }, true);
                }
            }
        }

        Task.Run(() => Data.DatabaseManager.AddDeath(e.Player.Steam64, e.DeadTeam));
        ModifyTeam(e.DeadTeam, t => t.Deaths++, false);
        if (KitManager.HasKit(e.Player, out kit))
        {
            ModifyStats(e.Player.Steam64, s =>
            {
                s.Deaths++;
                WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == e.DeadTeam);
                if (kitData == default)
                {
                    kitData = new WarfareStats.KitData() { KitID = kit.Name, Team = (byte)e.DeadTeam, Deaths = 1 };
                    s.Kits.Add(kitData);
                }
                else
                {
                    kitData.Deaths++;
                }
            }, false);
            ItemJar? primary = e.Player.Player.inventory.items[0].items.FirstOrDefault();
            ItemJar? secondary = e.Player.Player.inventory.items[1].items.FirstOrDefault();
            if (primary != null)
                ModifyWeapon(primary.item.id, kit.Name, x => x.Deaths++, true);
            if (secondary != null && (primary == null || primary.item.id != secondary.item.id)) // prevents 2 of the same gun from counting twice
                ModifyWeapon(secondary.item.id, kit.Name, x => x.Deaths++, true);
            ModifyKit(kit.Name, k => k.Deaths++, true);
        }
        else
            ModifyStats(e.Player.Steam64, s => s.Deaths++, false);
    }

    internal static void OnFlagCaptured(Gamemodes.Flags.Flag flag, ulong capturedTeam, ulong lostTeam)
    {
        ModifyTeam(capturedTeam, t => t.FlagsCaptured++, false);
        ModifyTeam(lostTeam, t => t.FlagsLost++, false);
        List<string> kits = new List<string>(flag.Team1TotalPlayers + flag.Team2TotalPlayers);
        if (capturedTeam == 1)
        {
            for (int p = 0; p < flag.PlayersOnFlagTeam1.Count; p++)
            {
                ModifyStats(flag.PlayersOnFlagTeam1[p].Steam64, s => s.FlagsCaptured++, false);
                if (KitManager.HasKit(flag.PlayersOnFlagTeam1[p], out Kit kit) && !kits.Contains(kit.Name))
                {
                    ModifyKit(kit.Name, k => k.FlagsCaptured++, true);
                    kits.Add(kit.Name);
                }
            }
            if (flag.IsObj(2))
                for (int p = 0; p < flag.PlayersOnFlagTeam2.Count; p++)
                    ModifyStats(flag.PlayersOnFlagTeam2[p].Steam64, s => s.FlagsLost++, false);
        }
        else if (capturedTeam == 2)
        {
            if (flag.IsObj(1))
                for (int p = 0; p < flag.PlayersOnFlagTeam1.Count; p++)
                    ModifyStats(flag.PlayersOnFlagTeam1[p].Steam64, s => s.FlagsLost++, false);
            for (int p = 0; p < flag.PlayersOnFlagTeam2.Count; p++)
            {
                ModifyStats(flag.PlayersOnFlagTeam2[p].Steam64, s => s.FlagsCaptured++, false);
                if (KitManager.HasKit(flag.PlayersOnFlagTeam2[p], out Kit kit) && !kits.Contains(kit.Name))
                {
                    ModifyKit(kit.Name, k => k.FlagsCaptured++, true);
                    kits.Add(kit.Name);
                }
            }
        }
    }

    public static class NetCalls
    {
        public static readonly NetCall<ulong> RequestPlayerData = new NetCall<ulong>(ReceiveRequestPlayerData);
        public static readonly NetCall<string> RequestKitData = new NetCall<string>(ReceiveRequestKitData);
        public static readonly NetCall<byte> RequestTeamData = new NetCall<byte>(ReceiveRequestTeamData);
        public static readonly NetCall<ushort, string> RequestWeaponData = new NetCall<ushort, string>(ReceiveRequestWeaponData);
        public static readonly NetCall<ushort> RequestVehicleData = new NetCall<ushort>(ReceiveRequestVehicleData);
        public static readonly NetCall RequestKitList = new NetCall(ReceiveRequestKitList);
        public static readonly NetCall RequestTeamsData = new NetCall(ReceiveRequestTeamData);
        public static readonly NetCall<ushort> RequestAllWeapons = new NetCall<ushort>(ReceiveWeaponRequest);
        public static readonly NetCall RequestEveryWeapon = new NetCall(ReceiveRequestEveryWeapon);
        public static readonly NetCall RequestEveryPlayer = new NetCall(ReceiveRequestEveryPlayer);
        public static readonly NetCall RequestEveryKit = new NetCall(ReceiveRequestEveryKit);
        public static readonly NetCall RequestEveryVehicle = new NetCall(ReceiveRequestEveryVehicle);

        public static readonly NetCallRaw<WarfareKit, string, byte> SendKitData = new NetCallRaw<WarfareKit, string, byte>(2003, WarfareKit.Read, null, null, WarfareKit.Write, null, null);
        public static readonly NetCallRaw<WarfareTeam> SendTeamData = new NetCallRaw<WarfareTeam>(2005, WarfareTeam.Read, WarfareTeam.Write);
        public static readonly NetCallRaw<WarfareStats, bool> SendPlayerData = new NetCallRaw<WarfareStats, bool>(2001, WarfareStats.Read, null, WarfareStats.Write, null);
        public static readonly NetCallRaw<WarfareWeapon, string, string> SendWeaponData = new NetCallRaw<WarfareWeapon, string, string>(2007, WarfareWeapon.Read, null, null, WarfareWeapon.Write, null, null);
        public static readonly NetCallRaw<WarfareVehicle, string> SendVehicleData = new NetCallRaw<WarfareVehicle, string>(2009, WarfareVehicle.Read, null, WarfareVehicle.Write, null);
        public static readonly NetCallRaw<string[]> SendKitList = new NetCallRaw<string[]>(2011, null, null);
        public static readonly NetCallRaw<WarfareTeam, WarfareTeam> SendTeams = new NetCallRaw<WarfareTeam, WarfareTeam>(2013, WarfareTeam.Read, WarfareTeam.Read, WarfareTeam.Write, WarfareTeam.Write);
        public static readonly NetCallRaw<WarfareWeapon[], string, string[]> SendWeapons = new NetCallRaw<WarfareWeapon[], string, string[]>(2019, ReadWeaponArray, null, null, WriteWeaponArray, null, null);
        public static readonly NetCallRaw<WarfareWeapon[], string[]> SendEveryWeapon = new NetCallRaw<WarfareWeapon[], string[]>(2021, ReadWeaponArray, null, WriteWeaponArray, null);
        public static readonly NetCallRaw<WarfareStats[]> SendEveryPlayer = new NetCallRaw<WarfareStats[]>(2022, ReadStatArray, WriteStatArray);
        public static readonly NetCallRaw<WarfareKit[], string[], byte[]> SendEveryKit = new NetCallRaw<WarfareKit[], string[], byte[]>(2023, ReadKitArray, null, null, WriteKitArray, null, null);
        public static readonly NetCallRaw<WarfareVehicle[], string[]> SendEveryVehicle = new NetCallRaw<WarfareVehicle[], string[]>(2024, ReadVehicleArray, null, WriteVehicleArray, null);

        public static readonly NetCallRaw<WarfareStats> BackupStats = new NetCallRaw<WarfareStats>(2090, WarfareStats.Read, WarfareStats.Write);
        public static readonly NetCallRaw<WarfareTeam> BackupTeam = new NetCallRaw<WarfareTeam>(2091, WarfareTeam.Read, WarfareTeam.Write);
        public static readonly NetCallRaw<WarfareWeapon> BackupWeapon = new NetCallRaw<WarfareWeapon>(2092, WarfareWeapon.Read, WarfareWeapon.Write);
        public static readonly NetCallRaw<WarfareVehicle> BackupVehicle = new NetCallRaw<WarfareVehicle>(2093, WarfareVehicle.Read, WarfareVehicle.Write);
        public static readonly NetCallRaw<WarfareKit> BackupKit = new NetCallRaw<WarfareKit>(2094, WarfareKit.Read, WarfareKit.Write);

        [NetCall(ENetCall.FROM_SERVER, 2000)]
        internal static void ReceiveRequestPlayerData(MessageContext context, ulong player)
        {
            bool online = Provider.clients.Exists(x => x.playerID.steamID.m_SteamID == player);
            string dir = Path.Combine(StatsDirectory, player.ToString() + ".dat");
            if (WarfareStats.IO.ReadFrom(dir, out WarfareStats stats))
            {
                context.Reply(SendPlayerData, stats, online);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 2002)]
        internal static void ReceiveRequestKitData(MessageContext context, string kitId)
        {
            EClass @class = EClass.NONE;
            string sname = kitId;
            if (KitManager.KitExists(kitId, out Kit kit2))
            {
                @class = kit2.Class;
                if (!kit2.SignTexts.TryGetValue(L.DEFAULT, out sname))
                    sname = kit2.SignTexts.Count > 0 ? kit2.SignTexts.Values.ElementAt(0) : kitId;
            }
            string dir = Path.Combine(KitsDirectory, kitId + ".dat");
            if (WarfareKit.IO.ReadFrom(dir, out WarfareKit kit))
            {
                context.Reply(SendKitData, kit, sname, (byte)@class);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 2004)]
        internal static void ReceiveRequestTeamData(MessageContext context, byte team)
        {
            if (team == 1)
                context.Reply(SendTeamData, Team1Stats);
            else if (team == 2)
                context.Reply(SendTeamData, Team1Stats);
        }

        [NetCall(ENetCall.FROM_SERVER, 2006)]
        internal static void ReceiveRequestWeaponData(MessageContext context, ushort weaponid, string kitId)
        {
            string dir = WeaponsDirectory + GetWeaponName(weaponid, kitId);
            if (WarfareWeapon.IO.ReadFrom(dir, out WarfareWeapon weapon))
            {
                string name = Assets.find(EAssetType.ITEM, weaponid) is ItemAsset asset ? asset.itemName : weaponid.ToString();
                string kitname;
                if (KitManager.KitExists(kitId, out Kit kit))
                {
                    if (!kit.SignTexts.TryGetValue(L.DEFAULT, out kitname))
                    {
                        kitname = kit.SignTexts.Count > 0 ? kit.SignTexts.Values.ElementAt(0) : kitId;
                    }
                }
                else
                    kitname = kitId;
                context.Reply(SendWeaponData, weapon, name, kitname);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 2008)]
        internal static void ReceiveRequestVehicleData(MessageContext context, ushort vehicleID)
        {
            string dir = Path.Combine(VehiclesDirectory, vehicleID + ".dat");
            string name = Assets.find(EAssetType.VEHICLE, vehicleID) is VehicleAsset asset ? asset.vehicleName : vehicleID.ToString();
            if (WarfareVehicle.IO.ReadFrom(dir, out WarfareVehicle vehicle))
                context.Reply(SendVehicleData, vehicle, name);
        }

        [NetCall(ENetCall.FROM_SERVER, 2010)]
        internal static void ReceiveRequestKitList(MessageContext context)
            => context.Reply(SendKitList, KitManager.GetKitsWhere(k => !k.IsLoadout).Select(k => k.Name).ToArray());

        [NetCall(ENetCall.FROM_SERVER, 2012)]
        internal static void ReceiveRequestTeamData(MessageContext context) => context.Reply(SendTeams, Team1Stats, Team2Stats);

        [NetCall(ENetCall.FROM_SERVER, 2020)]
        internal static void ReceiveWeaponRequest(MessageContext context, ushort weapon)
        {
            if (!Directory.Exists(WeaponsDirectory)) SendWeapons.NetInvoke(Array.Empty<WarfareWeapon>(), string.Empty, Array.Empty<string>());
            string[] files = Directory.GetFiles(WeaponsDirectory, $"{weapon}*.dat");
            List<WarfareWeapon> weapons = new List<WarfareWeapon>();
            List<string> kitnames = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                if (WarfareWeapon.IO.ReadFrom(files[i], out WarfareWeapon w))
                {
                    weapons.Add(w);
                    string kitname = w.KitID;
                    if (KitManager.KitExists(w.KitID, out Kit kit))
                        if (!kit.SignTexts.TryGetValue(L.DEFAULT, out kitname))
                            if (kit.SignTexts.Count > 0)
                                kitname = kit.SignTexts.Values.ElementAt(0);
                    kitnames.Add(kitname);
                }
            }
            context.Reply(SendWeapons, weapons.ToArray(), Assets.find(EAssetType.ITEM, weapon) is ItemAsset asset ? asset.itemName : weapon.ToString(Data.Locale), kitnames.ToArray());
        }

        [NetCall(ENetCall.FROM_SERVER, 2025)]
        internal static void ReceiveRequestEveryWeapon(MessageContext context)
        {
            string[] weaponnames = new string[Weapons.Count];
            for (int i = 0; i < weaponnames.Length; i++)
            {
                weaponnames[i] = Assets.find(EAssetType.ITEM, Weapons[i].ID) is ItemAsset asset ? asset.itemName : Weapons[i].ID.ToString();
            }
            context.Reply(SendEveryWeapon, Weapons.ToArray(), weaponnames);
        }

        [NetCall(ENetCall.FROM_SERVER, 2026)]
        internal static void ReceiveRequestEveryPlayer(MessageContext context)
        {
            if (!Directory.Exists(StatsDirectory))
                Directory.CreateDirectory(StatsDirectory);
            string[] stats = Directory.GetFiles(StatsDirectory);
            List<WarfareStats> rtn = new List<WarfareStats>();
            for (int i = 0; i < stats.Length; i++)
            {
                FileInfo file = new FileInfo(stats[i]);
                if (WarfareStats.IO.ReadFrom(file, out WarfareStats stat) && stat != null)
                {
                    rtn.Add(stat);
                }
                else
                {
                    L.LogWarning("Invalid vehicle file: " + file.FullName);
                }
            }
            context.Reply(SendEveryPlayer, rtn.ToArray());
        }

        [NetCall(ENetCall.FROM_SERVER, 2027)]
        internal static void ReceiveRequestEveryKit(MessageContext context)
        {
            string[] kitnames = new string[Kits.Count];
            byte[] classes = new byte[Kits.Count];
            for (int i = 0; i < kitnames.Length; i++)
            {
                if (KitManager.KitExists(Kits[i].KitID, out Kit kit))
                {
                    classes[i] = (byte)kit.Class;
                    kitnames[i] = Kits[i].KitID;
                    if (!kit.SignTexts.TryGetValue(L.DEFAULT, out kitnames[i]))
                        if (kit.SignTexts.Count > 0)
                            kitnames[i] = kit.SignTexts.Values.ElementAt(0);
                }
            }
            context.Reply(SendEveryKit, Kits.ToArray(), kitnames, classes);
        }

        [NetCall(ENetCall.FROM_SERVER, 2028)]
        internal static void ReceiveRequestEveryVehicle(MessageContext context)
        {
            string[] vehiclenames = new string[Vehicles.Count];
            for (int i = 0; i < vehiclenames.Length; i++)
            {
                vehiclenames[i] = Assets.find(EAssetType.VEHICLE, Weapons[i].ID) is VehicleAsset asset ? asset.vehicleName : Vehicles[i].ID.ToString();
            }
            context.Reply(SendEveryVehicle, Vehicles.ToArray(), vehiclenames);
        }

        private static WarfareWeapon[] ReadWeaponArray(ByteReader reader)
        {
            int length = reader.ReadInt32();
            WarfareWeapon[] weapons = new WarfareWeapon[length];
            for (int i = 0; i < length; i++)
            {
                weapons[i] = WarfareWeapon.Read(reader);
            }
            return weapons;
        }
        private static void WriteWeaponArray(ByteWriter writer, WarfareWeapon[] array)
        {
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                WarfareWeapon.Write(writer, array[i]);
            }
        }
        private static WarfareStats[] ReadStatArray(ByteReader reader)
        {
            int length = reader.ReadInt32();
            WarfareStats[] stats = new WarfareStats[length];
            for (int i = 0; i < length; i++)
            {
                stats[i] = WarfareStats.Read(reader);
            }
            return stats;
        }
        private static void WriteStatArray(ByteWriter writer, WarfareStats[] array)
        {
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                WarfareStats.Write(writer, array[i]);
            }
        }
        private static WarfareVehicle[] ReadVehicleArray(ByteReader reader)
        {
            int length = reader.ReadInt32();
            WarfareVehicle[] vehicles = new WarfareVehicle[length];
            for (int i = 0; i < length; i++)
            {
                vehicles[i] = WarfareVehicle.Read(reader);
            }
            return vehicles;
        }
        private static void WriteVehicleArray(ByteWriter writer, WarfareVehicle[] array)
        {
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                WarfareVehicle.Write(writer, array[i]);
            }
        }
        private static WarfareKit[] ReadKitArray(ByteReader reader)
        {
            int length = reader.ReadInt32();
            WarfareKit[] kits = new WarfareKit[length];
            for (int i = 0; i < length; i++)
            {
                kits[i] = WarfareKit.Read(reader);
            }
            return kits;
        }
        private static void WriteKitArray(ByteWriter writer, WarfareKit[] array)
        {
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                WarfareKit.Write(writer, array[i]);
            }
        }
    }
}
