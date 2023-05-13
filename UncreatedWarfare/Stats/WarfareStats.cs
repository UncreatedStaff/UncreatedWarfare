using System.Collections.Generic;
using Uncreated.Encoding;
using Uncreated.Encoding.IO;

namespace Uncreated.Warfare.Stats;

public class WarfareStats
{
    public static readonly RawByteIO<WarfareStats> IO = new RawByteIO<WarfareStats>(Read, Write, null!, 85);
    public const uint CurrentDataVersion = 1;
    public uint DataVersion;
    public ulong Steam64;
    public uint PlaytimeMinutes;
    public long LastOnline;
    public uint Kills;
    public uint Deaths;
    public uint Teamkills;
    public uint Downs;
    public uint Revives;
    public uint Wins;
    public uint Losses;
    public uint VehiclesRequested;
    public uint VehiclesDestroyed;
    public uint FlagsCaptured;
    public uint FlagsLost;
    public uint FobsBuilt;
    public uint FobsDestroyed;
    public uint EmplacementsBuilt;
    public uint FortificationsBuilt;
    public uint KillsWhileAttackingFlags;
    public uint KillsWhileDefendingFlags;
    public List<KitData> Kits;
    public class KitData
    {
        public string KitID;
        public byte Team;
        public uint Kills;
        public uint Deaths;
        public uint Downs;
        public uint Revives;
        public uint TimesRequested;
        public float AverageGunKillDistance;
        public uint AverageGunKillDistanceCounter;
        public uint PlaytimeMinutes;
    }
    public static void Write(ByteWriter writer, WarfareStats stats)
    {
        writer.Write(CurrentDataVersion);
        writer.Write(stats.Steam64);
        writer.Write(stats.PlaytimeMinutes);
        writer.Write(stats.LastOnline);
        writer.Write(stats.Kills);
        writer.Write(stats.Deaths);
        writer.Write(stats.Teamkills);
        writer.Write(stats.Downs);
        writer.Write(stats.Revives);
        writer.Write(stats.Wins);
        writer.Write(stats.Losses);
        writer.Write(stats.VehiclesRequested);
        writer.Write(stats.VehiclesDestroyed);
        writer.Write(stats.FlagsCaptured);
        writer.Write(stats.FlagsLost);
        writer.Write(stats.FobsBuilt);
        writer.Write(stats.FobsDestroyed);
        writer.Write(stats.EmplacementsBuilt);
        writer.Write(stats.FortificationsBuilt);
        writer.Write(stats.KillsWhileAttackingFlags);
        writer.Write(stats.KillsWhileDefendingFlags);
        writer.Write((byte)stats.Kits.Count);
        for (int i = 0; i < stats.Kits.Count; i++)
        {
            KitData kitData = stats.Kits[i];
            writer.Write(kitData.KitID);
            writer.Write(kitData.Team);
            writer.Write(kitData.Kills);
            writer.Write(kitData.Deaths);
            writer.Write(kitData.Downs);
            writer.Write(kitData.Revives);
            writer.Write(kitData.TimesRequested);
            writer.Write(kitData.AverageGunKillDistance);
            writer.Write(kitData.AverageGunKillDistanceCounter);
            writer.Write(kitData.PlaytimeMinutes);
        }
    }
    public static WarfareStats Read(ByteReader reader)
    {
        WarfareStats stats = new WarfareStats
        {
            DataVersion = reader.ReadUInt32(),
            Steam64 = reader.ReadUInt64()
        };
        if (stats.DataVersion > 0)
        {
            stats.PlaytimeMinutes = reader.ReadUInt32();
            stats.LastOnline = reader.ReadInt64();
            stats.Kills = reader.ReadUInt32();
            stats.Deaths = reader.ReadUInt32();
            stats.Teamkills = reader.ReadUInt32();
            stats.Downs = reader.ReadUInt32();
            stats.Revives = reader.ReadUInt32();
            stats.Wins = reader.ReadUInt32();
            stats.Losses = reader.ReadUInt32();
            stats.VehiclesRequested = reader.ReadUInt32();
            stats.VehiclesDestroyed = reader.ReadUInt32();
            stats.FlagsCaptured = reader.ReadUInt32();
            stats.FlagsLost = reader.ReadUInt32();
            stats.FobsBuilt = reader.ReadUInt32();
            stats.FobsDestroyed = reader.ReadUInt32();
            stats.EmplacementsBuilt = reader.ReadUInt32();
            stats.FortificationsBuilt = reader.ReadUInt32();
            stats.KillsWhileAttackingFlags = reader.ReadUInt32();
            stats.KillsWhileDefendingFlags = reader.ReadUInt32();
            int kitCount = reader.ReadUInt8();
            stats.Kits = new List<KitData>(kitCount);
            for (int i = 0; i < kitCount; i++)
            {
                stats.Kits.Add(
                    new KitData
                    {
                        KitID = reader.ReadString(),
                        Team = reader.ReadUInt8(),
                        Kills = reader.ReadUInt32(),
                        Deaths = reader.ReadUInt32(),
                        Downs = reader.ReadUInt32(),
                        Revives = reader.ReadUInt32(),
                        TimesRequested = reader.ReadUInt32(),
                        AverageGunKillDistance = reader.ReadFloat(),
                        AverageGunKillDistanceCounter = reader.ReadUInt32(),
                        PlaytimeMinutes = reader.ReadUInt32()
                    }
                );
            }
        }
        return stats;
    }
}
public class WarfareWeapon
{
    public static readonly RawByteIO<WarfareWeapon> IO = new RawByteIO<WarfareWeapon>(Read, Write, null!, 49);
    public const uint CurrentDataVersion = 1;
    public uint DataVersion;
    public ushort ID;
    public string KitID;
    public uint Kills;
    public uint Deaths;
    public uint Downs;
    public float AverageKillDistance;
    public uint AverageKillDistanceCounter;
    public uint SkullKills;
    public uint BodyKills;
    public uint ArmKills;
    public uint LegKills;
    public static void Write(ByteWriter writer, WarfareWeapon weapon)
    {
        writer.Write(CurrentDataVersion);
        writer.Write(weapon.ID);
        writer.Write(weapon.KitID);
        writer.Write(weapon.Kills);
        writer.Write(weapon.Deaths);
        writer.Write(weapon.Downs);
        writer.Write(weapon.AverageKillDistance);
        writer.Write(weapon.AverageKillDistanceCounter);
        writer.Write(weapon.SkullKills);
        writer.Write(weapon.BodyKills);
        writer.Write(weapon.ArmKills);
        writer.Write(weapon.LegKills);
    }
    public static WarfareWeapon Read(ByteReader reader)
    {
        WarfareWeapon weapon = new WarfareWeapon
        {
            DataVersion = reader.ReadUInt32(),
            ID = reader.ReadUInt16(),
            KitID = reader.ReadString()
        };
        if (weapon.DataVersion > 0)
        {
            weapon.Kills = reader.ReadUInt32();
            weapon.Deaths = reader.ReadUInt32();
            weapon.Downs = reader.ReadUInt32();
            weapon.AverageKillDistance = reader.ReadFloat();
            weapon.AverageKillDistanceCounter = reader.ReadUInt32();
            weapon.SkullKills = reader.ReadUInt32();
            weapon.BodyKills = reader.ReadUInt32();
            weapon.ArmKills = reader.ReadUInt32();
            weapon.LegKills = reader.ReadUInt32();
        }
        return weapon;
    }
}
public class WarfareKit
{
    public static readonly RawByteIO<WarfareKit> IO = new RawByteIO<WarfareKit>(Read, Write, null!, 34);
    public const uint CurrentDataVersion = 1;
    public uint DataVersion;
    public string KitID;
    public uint Kills;
    public uint Deaths;
    public uint TimesRequested;
    public float AverageGunKillDistance;
    public uint AverageGunKillDistanceCounter;
    public uint FlagsCaptured;
    public static void Write(ByteWriter writer, WarfareKit kit)
    {
        writer.Write(CurrentDataVersion);
        writer.Write(kit.KitID);
        writer.Write(kit.Kills);
        writer.Write(kit.Deaths);
        writer.Write(kit.TimesRequested);
        writer.Write(kit.AverageGunKillDistance);
        writer.Write(kit.AverageGunKillDistanceCounter);
        writer.Write(kit.FlagsCaptured);
    }
    public static WarfareKit Read(ByteReader reader)
    {
        WarfareKit kit = new WarfareKit
        {
            DataVersion = reader.ReadUInt32(),
            KitID = reader.ReadString()
        };
        if (kit.DataVersion > 0)
        {
            kit.Kills = reader.ReadUInt32();
            kit.Deaths = reader.ReadUInt32();
            kit.TimesRequested = reader.ReadUInt32();
            kit.AverageGunKillDistance = reader.ReadFloat();
            kit.AverageGunKillDistanceCounter = reader.ReadUInt32();
            kit.FlagsCaptured = reader.ReadUInt32();
        }
        return kit;
    }
}
public class WarfareTeam
{
    public static readonly RawByteIO<WarfareTeam> IO = new RawByteIO<WarfareTeam>(Read, Write, null!, 65);
    public const uint CurrentDataVersion = 1;
    public uint DataVersion;
    public byte Team;
    public uint Kills;
    public uint Deaths;
    public uint Teamkills;
    public uint Downs;
    public uint Revives;
    public uint Wins;
    public uint Losses;
    public uint VehiclesRequested;
    public uint VehiclesDestroyed;
    public uint FlagsCaptured;
    public uint FlagsLost;
    public uint FobsBuilt;
    public uint FobsDestroyed;
    public uint EmplacementsBuilt;
    public uint FortificationsBuilt;
    public float AveragePlayers;
    public uint AveragePlayersCounter;
    public static void Write(ByteWriter writer, WarfareTeam team)
    {
        writer.Write(CurrentDataVersion);
        writer.Write(team.Team);
        writer.Write(team.Kills);
        writer.Write(team.Deaths);
        writer.Write(team.Teamkills);
        writer.Write(team.Downs);
        writer.Write(team.Revives);
        writer.Write(team.Wins);
        writer.Write(team.Losses);
        writer.Write(team.VehiclesRequested);
        writer.Write(team.VehiclesDestroyed);
        writer.Write(team.FlagsCaptured);
        writer.Write(team.FlagsLost);
        writer.Write(team.FobsBuilt);
        writer.Write(team.FobsDestroyed);
        writer.Write(team.EmplacementsBuilt);
        writer.Write(team.FortificationsBuilt);
        writer.Write(team.AveragePlayers);
        writer.Write(team.AveragePlayersCounter);
    }
    public static WarfareTeam Read(ByteReader reader)
    {
        WarfareTeam team = new WarfareTeam
        {
            DataVersion = reader.ReadUInt32(),
            Team = reader.ReadUInt8()
        };
        if (team.DataVersion > 0)
        {
            team.Kills = reader.ReadUInt32();
            team.Deaths = reader.ReadUInt32();
            team.Teamkills = reader.ReadUInt32();
            team.Downs = reader.ReadUInt32();
            team.Revives = reader.ReadUInt32();
            team.Wins = reader.ReadUInt32();
            team.Losses = reader.ReadUInt32();
            team.VehiclesRequested = reader.ReadUInt32();
            team.VehiclesDestroyed = reader.ReadUInt32();
            team.FlagsCaptured = reader.ReadUInt32();
            team.FlagsLost = reader.ReadUInt32();
            team.FobsBuilt = reader.ReadUInt32();
            team.FobsDestroyed = reader.ReadUInt32();
            team.EmplacementsBuilt = reader.ReadUInt32();
            team.FortificationsBuilt = reader.ReadUInt32();
            team.AveragePlayers = reader.ReadFloat();
            team.AveragePlayersCounter = reader.ReadUInt32();
        }
        return team;
    }
}
public class WarfareVehicle
{
    public static readonly RawByteIO<WarfareVehicle> IO = new RawByteIO<WarfareVehicle>(Read, Write, null!, 22);
    public const uint CurrentDataVersion = 1;
    public uint DataVersion;
    public ushort ID;
    public uint TimesRequested;
    public uint TimesDestroyed;
    public uint KillsWithGunner;

    public static void Write(ByteWriter writer, WarfareVehicle vehicle)
    {
        writer.Write(CurrentDataVersion);
        writer.Write(vehicle.ID);
        writer.Write(vehicle.TimesRequested);
        writer.Write(vehicle.TimesDestroyed);
        writer.Write(vehicle.KillsWithGunner);
    }
    public static WarfareVehicle Read(ByteReader reader)
    {
        WarfareVehicle vehicle = new WarfareVehicle
        {
            DataVersion = reader.ReadUInt32(),
            ID = reader.ReadUInt16()
        };
        if (vehicle.DataVersion > 0)
        {
            vehicle.TimesRequested = reader.ReadUInt32();
            vehicle.TimesDestroyed = reader.ReadUInt32();
            vehicle.KillsWithGunner = reader.ReadUInt32();
        }
        return vehicle;
    }
}
