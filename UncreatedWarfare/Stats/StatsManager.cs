using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;
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
    private static int _weaponCounter = -1;
    private static int _kitCounter = -1;
    private static int _vehicleCounter = -1;
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
                DataVersion = WarfareTeam.CurrentDataVersion,
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
                DataVersion = WarfareTeam.CurrentDataVersion,
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
        if (Team1Stats.DataVersion != WarfareTeam.CurrentDataVersion || Team1Stats.Team != 1)
        {
            Team1Stats.DataVersion = WarfareTeam.CurrentDataVersion;
            Team1Stats.Team = 1;
            WarfareTeam.IO.WriteTo(Team1Stats, t1);
        }
        if (Team2Stats.DataVersion != WarfareTeam.CurrentDataVersion || Team2Stats.Team != 2)
        {
            Team2Stats.DataVersion = WarfareTeam.CurrentDataVersion;
            Team2Stats.Team = 2;
            WarfareTeam.IO.WriteTo(Team2Stats, t2);
        }
    }
    const int BackupTickSpeedMins = 5;
    public static void BackupTick()
    {
        if (_minsCounter > BackupTickSpeedMins - 1)
            _minsCounter = 0;
        if (_minsCounter != BackupTickSpeedMins - 1)
        {
            _minsCounter++;
            return;
        }
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_weaponCounter == -1)
            _weaponCounter = UnityEngine.Random.Range(0, Weapons.Count);
        if (_kitCounter == -1)
            _kitCounter = UnityEngine.Random.Range(0, Kits.Count);
        if (_vehicleCounter == -1)
            _vehicleCounter = UnityEngine.Random.Range(0, Vehicles.Count);
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
                    (Weapons[_weaponCounter].ID.ToString(Data.AdminLocale) + " - " + Weapons[_weaponCounter].KitID)));
        }
        if (Vehicles.Count > 0)
        {
            NetCalls.BackupVehicle.NetInvoke(Vehicles[_vehicleCounter]);
            if (UCWarfare.Config.Debug)
                L.Log("[VEHICLE] Backed up: " + (Assets.find(EAssetType.VEHICLE, Vehicles[_vehicleCounter].ID) is VehicleAsset asset ?
                    asset.vehicleName :
                    Vehicles[_vehicleCounter].ID.ToString(Data.AdminLocale)));
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
                if (weapon.DataVersion != WarfareWeapon.CurrentDataVersion)
                {
                    weapon.DataVersion = WarfareWeapon.CurrentDataVersion;
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
                if (vehicle.DataVersion != WarfareVehicle.CurrentDataVersion)
                {
                    vehicle.DataVersion = WarfareVehicle.CurrentDataVersion;
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
                if (kit.DataVersion != WarfareKit.CurrentDataVersion)
                {
                    kit.DataVersion = WarfareKit.CurrentDataVersion;
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
            weapon.DataVersion = WarfareWeapon.CurrentDataVersion;
            modification.Invoke(weapon);
            Weapons.Add(weapon);
            WarfareWeapon.IO.WriteTo(weapon, dir);
            return true;
        }
        weapon = new WarfareWeapon()
        {
            DataVersion = WarfareWeapon.CurrentDataVersion,
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
        string dir = Path.Combine(VehiclesDirectory, id.ToString(Data.AdminLocale) + ".dat");
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
            weapon.DataVersion = WarfareVehicle.CurrentDataVersion;
            modification.Invoke(weapon);
            Vehicles.Add(weapon);
            WarfareVehicle.IO.WriteTo(weapon, dir);
            return true;
        }
        weapon = new WarfareVehicle()
        {
            DataVersion = WarfareVehicle.CurrentDataVersion,
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
        if (!Data.TrackStats || !Util.IsValidSteam64Id(s64)) return false;
        string dir = Path.Combine(StatsDirectory, s64.ToString(Data.AdminLocale) + ".dat");
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
            stats.DataVersion = WarfareWeapon.CurrentDataVersion;
            modification.Invoke(stats);
            WarfareStats.IO.WriteTo(stats, dir);
            return true;
        }
        stats = new WarfareStats()
        {
            DataVersion = WarfareStats.CurrentDataVersion,
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
            kit.DataVersion = WarfareWeapon.CurrentDataVersion;
            modification.Invoke(kit);
            Kits.Add(kit);
            WarfareKit.IO.WriteTo(kit, dir);
            return true;
        }
        kit = new WarfareKit()
        {
            DataVersion = WarfareKit.CurrentDataVersion,
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
        string dir = Path.Combine(StatsDirectory, s64.ToString(Data.AdminLocale) + ".dat");
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
                    L.LogWarning("Failed to read " + s64.ToString(Data.AdminLocale) + "'s stat file, creating a backup and resetting it.");

                    string p2 = Path.Combine(StatsDirectory, s64.ToString(Data.AdminLocale) + "_corrupt.dat");
                    File.Delete(p2);
                    File.Move(dir, p2);
                    WarfareStats reset = new WarfareStats()
                    {
                        DataVersion = WarfareStats.CurrentDataVersion,
                        Kits = new List<WarfareStats.KitData>(),
                        Steam64 = s64
                    };
                    WarfareStats.IO.WriteTo(reset, dir);
                    OnlinePlayers.Add(reset);
                }
            }
            else
            {
                WarfareStats reset = new WarfareStats
                {
                    DataVersion = WarfareStats.CurrentDataVersion,
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
        WarfareStats? stats = SaveCachedPlayer(s64);
        if (stats == null) return;
        NetCalls.BackupStats.NetInvoke(stats);
        OnlinePlayers.Remove(stats);
    }
    public static WarfareStats? SaveCachedPlayer(ulong player)
    {
        WarfareStats? stats = OnlinePlayers.FirstOrDefault(x => x.Steam64 == player);
        if (stats != null)
            WarfareStats.IO.WriteTo(stats, Path.Combine(StatsDirectory, player.ToString(Data.AdminLocale) + ".dat"));
        return stats;
    }
    private static void OnPlayerDied(PlayerDied e)
    {
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
                        if (flag.PlayerInRange(kilPos))
                        {
                            def = flag.IsContested(out ulong winner) || winner != e.KillerTeam;
                            atk = !def;
                            break;
                        }
                    }
                }
                if (e.Killer.HasKit || !string.IsNullOrEmpty(e.KillerKitName))
                {
                    Kit? kit = e.KillerKitName != null ? KitManager.GetSingletonQuick()?.FindKitNoLock(e.KillerKitName, true) : e.Killer.GetActiveKit();
                    if (kit != null)
                    {
                        ModifyStats(e.Killer.Steam64, s =>
                        {
                            s.Kills++;
                            WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID.Equals(kit.InternalName, StringComparison.OrdinalIgnoreCase) && k.Team == e.KillerTeam);
                            if (kitData == default)
                            {
                                kitData = new WarfareStats.KitData { KitID = kit.InternalName, Team = (byte)e.KillerTeam, Kills = 1 };
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
                        if (!e.PrimaryAssetIsVehicle && Assets.find(e.PrimaryAsset) is ItemAsset asset)
                        {
                            ModifyWeapon(asset.id, kit.InternalName, x =>
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
                                        x.ArmKills++;
                                        break;
                                    case ELimb.LEFT_FOOT or ELimb.RIGHT_FOOT or ELimb.LEFT_LEG or ELimb.RIGHT_LEG:
                                        x.LegKills++;
                                        break;
                                }
                                x.AverageKillDistance = (x.AverageKillDistance * x.AverageKillDistanceCounter + e.KillDistance) / ++x.AverageKillDistanceCounter;
                            }, true);
                            ModifyKit(kit.InternalName, k =>
                            {
                                k.Kills++;
                                if (e.Cause == EDeathCause.GUN)
                                    k.AverageGunKillDistance =
                                        (k.AverageGunKillDistance * k.AverageGunKillDistanceCounter + e.KillDistance) / ++k.AverageGunKillDistanceCounter;
                            }, true);
                        }
                    }
                }
                else
                    ModifyStats(e.Killer.Steam64, s =>
                    {
                        s.Kills++;
                        if (atk) s.KillsWhileAttackingFlags++;
                        else if (def) s.KillsWhileDefendingFlags++;
                    }, false);

            }
        }

        Task.Run(() => Data.DatabaseManager.AddDeath(e.Player.Steam64, e.DeadTeam));
        ModifyTeam(e.DeadTeam, t => t.Deaths++, false);
        Kit? kit2 = e.PlayerKitName != null ? KitManager.GetSingletonQuick()?.FindKitNoLock(e.PlayerKitName, true) : e.Player.GetActiveKit();
        if (kit2 != null)
        {
            ModifyStats(e.Player.Steam64, s =>
            {
                s.Deaths++;
                WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit2.InternalName && k.Team == e.DeadTeam);
                if (kitData == default)
                {
                    kitData = new WarfareStats.KitData { KitID = kit2.InternalName, Team = (byte)e.DeadTeam, Deaths = 1 };
                    s.Kits.Add(kitData);
                }
                else kitData.Deaths++;
            }, false);
            ItemJar? primary = e.Player.Player.inventory.items[0].items.FirstOrDefault();
            ItemJar? secondary = e.Player.Player.inventory.items[1].items.FirstOrDefault();
            if (primary != null)
                ModifyWeapon(primary.item.id, kit2.InternalName, x => x.Deaths++, true);
            if (secondary != null && (primary == null || primary.item.id != secondary.item.id)) // prevents 2 of the same gun from counting twice
                ModifyWeapon(secondary.item.id, kit2.InternalName, x => x.Deaths++, true);
            ModifyKit(kit2.InternalName, k => k.Deaths++, true);
        }
        else
            ModifyStats(e.Player.Steam64, s => s.Deaths++, false);
    }

    internal static void OnFlagCaptured(Gamemodes.Flags.Flag flag, ulong capturedTeam, ulong lostTeam)
    {
        ModifyTeam(capturedTeam, t => t.FlagsCaptured++, false);
        ModifyTeam(lostTeam, t => t.FlagsLost++, false);
        List<uint> kits = new List<uint>(flag.Team1TotalPlayers + flag.Team2TotalPlayers);
        List<UCPlayer> c = capturedTeam == 1 ? flag.PlayersOnFlagTeam1 : flag.PlayersOnFlagTeam2;
        List<UCPlayer> l = capturedTeam == 1 ? flag.PlayersOnFlagTeam2 : flag.PlayersOnFlagTeam1;
        for (int p = 0; p < c.Count; p++)
        {
            UCPlayer pl = c[p];
            ModifyStats(pl.Steam64, s => s.FlagsCaptured++, false);
            Kit? kit = pl.GetActiveKit();
            if (kit != null && !kits.Contains(kit.PrimaryKey))
            {
                string? name = kit.InternalName;
                if (name != null)
                {
                    ModifyKit(name, k => k.FlagsCaptured++, true);
                    kits.Add(kit.PrimaryKey);
                }
            }
        }
        if (flag.IsObj(TeamManager.Other(capturedTeam)))
            for (int p = 0; p < l.Count; p++)
                ModifyStats(l[p].Steam64, s => s.FlagsLost++, false);
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
            WarfareStats? stats = SaveCachedPlayer(player);
            bool online = UCPlayer.FromID(player) is { IsOnline: true };
            string dir = Path.Combine(StatsDirectory, player.ToString(Data.AdminLocale) + ".dat");
            if (stats != null || WarfareStats.IO.ReadFrom(dir, out stats))
            {
                context.Reply(SendPlayerData, stats, online);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 2002)]
        internal static async Task ReceiveRequestKitData(MessageContext context, string kitId)
        {
            Class @class = Class.None;
            string sname = kitId;
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                Kit? kit2 = await manager.FindKit(kitId).ConfigureAwait(false);
                if (kit2 is not null)
                {
                    await manager.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        @class = kit2.Class;
                        sname = kit2.GetDisplayName();
                    }
                    finally
                    {
                        manager.Release();
                    }
                }
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
                context.Reply(SendTeamData, Team2Stats);
        }

        [NetCall(ENetCall.FROM_SERVER, 2006)]
        internal static async Task ReceiveRequestWeaponData(MessageContext context, ushort weaponid, string kitId)
        {
            string dir = WeaponsDirectory + GetWeaponName(weaponid, kitId);
            if (WarfareWeapon.IO.ReadFrom(dir, out WarfareWeapon weapon))
            {
                string name = Assets.find(EAssetType.ITEM, weaponid) is ItemAsset asset ? asset.itemName : weaponid.ToString();
                string kitname = kitId;
                KitManager? manager = KitManager.GetSingletonQuick();
                if (manager != null)
                {
                    Kit? kit2 = await manager.FindKit(kitId).ConfigureAwait(false);
                    if (kit2 is not null)
                    {
                        await manager.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            if (kit2 != null)
                            {
                                kitname = kit2.GetDisplayName();
                            }
                        }
                        finally
                        {
                            manager.Release();
                        }
                    }
                }
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
        internal static async Task ReceiveRequestKitList(MessageContext context)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager == null)
            {
                context.Reply(SendKitList, Array.Empty<string>());
                return;
            }

            await manager.WaitAsync().ConfigureAwait(false);
            try
            {
                await manager.WriteWaitAsync().ConfigureAwait(false);
                List<string> kits = new List<string>();
                try
                {
                    for (int i = 0; i < manager.Items.Count; ++i)
                    {
                        Kit kit = manager.Items[i];
                        if (kit != null && kit.Type != KitType.Loadout)
                            kits.Add(kit.InternalName);
                    }

                    context.Reply(SendKitList, kits.ToArray());
                }
                finally
                {
                    manager.WriteRelease();
                }
            }
            finally
            {
                manager.Release();
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 2012)]
        internal static void ReceiveRequestTeamData(MessageContext context) => context.Reply(SendTeams, Team1Stats, Team2Stats);

        [NetCall(ENetCall.FROM_SERVER, 2020)]
        internal static async Task ReceiveWeaponRequest(MessageContext context, ushort weapon)
        {
            if (!Directory.Exists(WeaponsDirectory)) SendWeapons.NetInvoke(Array.Empty<WarfareWeapon>(), string.Empty, Array.Empty<string>());
            string[] files = Directory.GetFiles(WeaponsDirectory, $"{weapon}*.dat");
            string itemName = Assets.find(EAssetType.ITEM, weapon) is ItemAsset asset ? asset.itemName : weapon.ToString(Data.AdminLocale);
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                await manager.WaitAsync().ConfigureAwait(false);
                try
                {
                    List<string> kitnames = new List<string>();
                    List<WarfareWeapon> weapons = new List<WarfareWeapon>();
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (WarfareWeapon.IO.ReadFrom(files[i], out WarfareWeapon w))
                        {
                            weapons.Add(w);
                            Kit? kit2 = await manager.FindKit(w.KitID).ConfigureAwait(false);
                            kitnames.Add(kit2?.GetDisplayName() ?? w.KitID);
                        }
                    }
                    context.Reply(SendWeapons, weapons.ToArray(), itemName, kitnames.ToArray());
                }
                finally
                {
                    manager.Release();
                }
            }
            else
            {
                WarfareWeapon[] weapons2 = Weapons.Where(x => x.ID == weapon).ToArray();
                string[] names = new string[weapons2.Length];
                for (int i = 0; i < weapons2.Length; ++i)
                    names[i] = string.Empty;
                context.Reply(SendWeapons, weapons2, itemName, names);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 2025)]
        internal static void ReceiveRequestEveryWeapon(MessageContext context)
        {
            WarfareWeapon[] allWeapons = Weapons.ToArray();
            string[] weaponnames = new string[allWeapons.Length];
            for (int i = 0; i < weaponnames.Length; i++)
            {
                weaponnames[i] = Assets.find(EAssetType.ITEM, allWeapons[i].ID) is ItemAsset asset ? asset.itemName : allWeapons[i].ID.ToString();
            }

            context.Reply(SendEveryWeapon, allWeapons, weaponnames);
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
        internal static async Task ReceiveRequestEveryKit(MessageContext context)
        {
            WarfareKit[] allkits = Kits.ToArray();
            string[] kitnames = new string[allkits.Length];
            byte[] classes = new byte[kitnames.Length];
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                await manager.WaitAsync().ConfigureAwait(false);
                try
                {
                    for (int i = 0; i < allkits.Length; i++)
                    {
                        Kit? k = manager.FindKitNoLock(allkits[i].KitID, true);
                        if (k != null)
                        {
                            classes[i] = (byte)k.Class;
                            kitnames[i] = k.GetDisplayName();
                        }
                    }
                }
                finally
                {
                    manager.Release();
                }
            }

            context.Reply(SendEveryKit, allkits, kitnames, classes);
        }

        [NetCall(ENetCall.FROM_SERVER, 2028)]
        internal static void ReceiveRequestEveryVehicle(MessageContext context)
        {
            WarfareVehicle[] vehs = Vehicles.ToArray();
            string[] vehiclenames = new string[vehs.Length];
            for (int i = 0; i < vehs.Length; i++)
            {
                vehiclenames[i] = Assets.find(EAssetType.VEHICLE, vehs[i].ID) is VehicleAsset asset ? asset.vehicleName : vehs[i].ID.ToString();
            }
            context.Reply(SendEveryVehicle, vehs, vehiclenames);
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
