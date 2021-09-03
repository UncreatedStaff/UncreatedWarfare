using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Uncreated.Warfare.Stats
{
    public static class StatsManager
    {
        public static readonly string SaveDirectory = Environment.GetEnvironmentVariable("APPDATA") + @"\Uncreated\";
        public static readonly string StatsDirectory = SaveDirectory + @"Players\";
        public static readonly string WeaponsDirectory = SaveDirectory + @"Weapons\";
        public static readonly string KitsDirectory = SaveDirectory + @"Kits\";
        public static WarfareTeam Team1Stats;
        public static WarfareTeam Team2Stats;
        public static readonly List<WarfareWeapon> Weapons = new List<WarfareWeapon>();
        public static readonly List<WarfareKit> Kits = new List<WarfareKit>();
        public static readonly List<WarfareStats> OnlinePlayers = new List<WarfareStats>();
        public static void LoadTeams()
        {
            WarfareTeam.IO.InitializeTo(
                () => new WarfareTeam()
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
                SaveDirectory + "team1.dat"
            );
            WarfareTeam.IO.InitializeTo( 
                () => new WarfareTeam()
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
                SaveDirectory + "team1.dat"
            );
            WarfareTeam.IO.ReadFrom(SaveDirectory + "team1.dat", out Team1Stats);
            WarfareTeam.IO.ReadFrom(SaveDirectory + "team2.dat", out Team2Stats);
            if (Team1Stats.DATA_VERSION != WarfareTeam.CURRENT_DATA_VERSION)
            {
                Team1Stats.DATA_VERSION = WarfareTeam.CURRENT_DATA_VERSION;
                WarfareTeam.IO.WriteTo(Team1Stats, SaveDirectory + "team1.dat");
            }
            if (Team2Stats.DATA_VERSION != WarfareTeam.CURRENT_DATA_VERSION)
            {
                Team2Stats.DATA_VERSION = WarfareTeam.CURRENT_DATA_VERSION;
                WarfareTeam.IO.WriteTo(Team2Stats, SaveDirectory + "team2.dat");
            }
        }
        public static void ModifyTeam(byte team, Action<WarfareTeam> modification, bool save = true)
        {
            if (team == 1)
            {
                modification.Invoke(Team1Stats);
                if (save) WarfareTeam.IO.WriteTo(Team1Stats, SaveDirectory + "team1.dat");
            } 
            else if (team == 2)
            {
                modification.Invoke(Team2Stats);
                if (save) WarfareTeam.IO.WriteTo(Team2Stats, SaveDirectory + "team2.dat");
            }
        }
        public static void ModifyTeam(ulong team, Action<WarfareTeam> modification, bool save = true)
        {
            if (team == 1)
            {
                modification.Invoke(Team1Stats);
                if (save) WarfareTeam.IO.WriteTo(Team1Stats, SaveDirectory + "team1.dat");
            } 
            else if (team == 2)
            {
                modification.Invoke(Team2Stats);
                if (save) WarfareTeam.IO.WriteTo(Team2Stats, SaveDirectory + "team2.dat");
            }
        }
        public static void SaveTeams()
        {
            WarfareTeam.IO.WriteTo(Team1Stats, SaveDirectory + "team1.dat");
            WarfareTeam.IO.WriteTo(Team2Stats, SaveDirectory + "team2.dat");
        }
        public static void LoadWeapons()
        {
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
                    F.LogWarning("Invalid weapon file: " + file.FullName);
                }
            }
        }
        private static string GetWeaponName(ushort ID, string KitID) => $"{ID}_{KitID.RemoveMany(false, Data.BAD_FILE_NAME_CHARACTERS)}.dat";
        public static bool ModifyWeapon(ushort ID, string KitID, Action<WarfareWeapon> modification, bool save = true)
        {
            string dir = WeaponsDirectory + GetWeaponName(ID, KitID);
            for (int i = 0; i < Weapons.Count; i++)
            {
                if (Weapons[i].ID == ID && Weapons[i].KitID == KitID)
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
                ID = ID,
                KitID = KitID
            };
            modification.Invoke(weapon);
            Weapons.Add(weapon);
            WarfareWeapon.IO.WriteTo(weapon, dir);
            return true;
        }
        public static bool ModifyStats(ulong Steam64, Action<WarfareStats> modification, bool save = true)
        {
            string dir = StatsDirectory + Steam64.ToString(Data.Locale) + ".dat";
            for (int i = 0; i < OnlinePlayers.Count; i++)
            {
                if (OnlinePlayers[i].Steam64 == Steam64)
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
                Steam64 = Steam64
            };
            modification.Invoke(stats);
            WarfareStats.IO.WriteTo(stats, dir);
            return true;
        }
        public static bool ModifyKit(string KitID, Action<WarfareKit> modification, bool save = true)
        {
            string dir = KitsDirectory + KitID + ".dat";
            for (int i = 0; i < Kits.Count; i++)
            {
                if (Kits[i].KitID == KitID)
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
                KitID = KitID
            };
            modification.Invoke(kit);
            Kits.Add(kit);
            WarfareKit.IO.WriteTo(kit, dir);
            return true;
        }
        public static void RegisterPlayer(ulong Steam64)
        {
            string dir = StatsDirectory + Steam64.ToString(Data.Locale) + ".dat";
            if (!OnlinePlayers.Exists(x => x.Steam64 == Steam64))
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
                        using (FileStream fs = new FileStream(StatsDirectory + Steam64.ToString(Data.Locale) + "_corrupt.dat", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                        {
                            fs.Write(bytes, 0, bytes.Length);
                            fs.Close();
                            fs.Dispose();
                        }
                        F.LogWarning("Failed to read " + Steam64.ToString(Data.Locale) + "'s stat file, creating a backup and resetting it.");
                        WarfareStats reset = new WarfareStats()
                        {
                            DATA_VERSION = WarfareStats.CURRENT_DATA_VERSION,
                            Kits = new List<WarfareStats.KitData>(),
                            Steam64 = Steam64
                        };
                        WarfareStats.IO.WriteTo(reset, dir);
                        OnlinePlayers.Add(reset);
                    }
                } else
                {
                    WarfareStats reset = new WarfareStats()
                    {
                        DATA_VERSION = WarfareStats.CURRENT_DATA_VERSION,
                        Kits = new List<WarfareStats.KitData>(),
                        Steam64 = Steam64
                    };
                    WarfareStats.IO.WriteTo(reset, dir);
                    OnlinePlayers.Add(reset);
                }
            }
        }
        public static void DeregisterPlayer(ulong Steam64)
        {
            WarfareStats stats = OnlinePlayers.FirstOrDefault(x =>  x.Steam64 == Steam64);
            if (stats == default) return;
            WarfareStats.IO.WriteTo(stats, StatsDirectory + Steam64.ToString(Data.Locale) + ".dat");
            OnlinePlayers.Remove(stats);
        }
    }
}
