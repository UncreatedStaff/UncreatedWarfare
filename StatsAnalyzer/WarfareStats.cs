using System.Collections.Generic;
using Uncreated.Networking.Encoding;
using Uncreated.Networking.Encoding.IO;

namespace Uncreated.Warfare.Stats
{
    public class WarfareStats
    {
        public readonly static RawByteIO<WarfareStats> IO = new RawByteIO<WarfareStats>(Read, Write, null, 85);
        public const uint CURRENT_DATA_VERSION = 1;
        public uint DATA_VERSION;
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
        public static void Write(ByteWriter W, WarfareStats S)
        {
            W.Write(S.DATA_VERSION);
            W.Write(S.Steam64);
            W.Write(S.PlaytimeMinutes);
            W.Write(S.LastOnline);
            W.Write(S.Kills);
            W.Write(S.Deaths);
            W.Write(S.Teamkills);
            W.Write(S.Downs);
            W.Write(S.Revives);
            W.Write(S.Wins);
            W.Write(S.Losses);
            W.Write(S.VehiclesRequested);
            W.Write(S.VehiclesDestroyed);
            W.Write(S.FlagsCaptured);
            W.Write(S.FlagsLost);
            W.Write(S.FobsBuilt);
            W.Write(S.FobsDestroyed);
            W.Write(S.EmplacementsBuilt);
            W.Write(S.FortificationsBuilt);
            W.Write(S.KillsWhileAttackingFlags);
            W.Write(S.KillsWhileDefendingFlags);
            W.Write((byte)S.Kits.Count);
            for (int i = 0; i < S.Kits.Count; i++)
            {
                KitData K = S.Kits[i];
                W.Write(K.KitID);
                W.Write(K.Team);
                W.Write(K.Kills);
                W.Write(K.Deaths);
                W.Write(K.Downs);
                W.Write(K.Revives);
                W.Write(K.TimesRequested);
                W.Write(K.AverageGunKillDistance);
                W.Write(K.AverageGunKillDistanceCounter);
                W.Write(K.PlaytimeMinutes);
            }
        }
        public static WarfareStats Read(ByteReader R)
        {
            WarfareStats S = new WarfareStats() { DATA_VERSION = R.ReadUInt32() };
            S.Steam64 = R.ReadUInt64();
            if (S.DATA_VERSION > 0)
            {
                S.PlaytimeMinutes = R.ReadUInt32();
                S.LastOnline = R.ReadInt64();
                S.Kills = R.ReadUInt32();
                S.Deaths = R.ReadUInt32();
                S.Teamkills = R.ReadUInt32();
                S.Downs = R.ReadUInt32();
                S.Revives = R.ReadUInt32();
                S.Wins = R.ReadUInt32();
                S.Losses = R.ReadUInt32();
                S.VehiclesRequested = R.ReadUInt32();
                S.VehiclesDestroyed = R.ReadUInt32();
                S.FlagsCaptured = R.ReadUInt32();
                S.FlagsLost = R.ReadUInt32();
                S.FobsBuilt = R.ReadUInt32();
                S.FobsDestroyed = R.ReadUInt32();
                S.EmplacementsBuilt = R.ReadUInt32();
                S.FortificationsBuilt = R.ReadUInt32();
                S.KillsWhileAttackingFlags = R.ReadUInt32();
                S.KillsWhileDefendingFlags = R.ReadUInt32();
                int kitCount = R.ReadUInt8();
                S.Kits = new List<KitData>(kitCount);
                for (int i = 0; i < kitCount; i++)
                {
                    S.Kits.Add(
                        new KitData()
                        {
                            KitID = R.ReadString(),
                            Team = R.ReadUInt8(),
                            Kills = R.ReadUInt32(),
                            Deaths = R.ReadUInt32(),
                            Downs = R.ReadUInt32(),
                            Revives = R.ReadUInt32(),
                            TimesRequested = R.ReadUInt32(),
                            AverageGunKillDistance = R.ReadFloat(),
                            AverageGunKillDistanceCounter = R.ReadUInt32(),
                            PlaytimeMinutes = R.ReadUInt32()
                        }
                    );
                }
            }
            return S;
        }
    }
    public class WarfareWeapon
    {
        public readonly static RawByteIO<WarfareWeapon> IO = new RawByteIO<WarfareWeapon>(Read, Write, null, 49);
        public const uint CURRENT_DATA_VERSION = 1;
        public uint DATA_VERSION;
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
        public static void Write(ByteWriter W, WarfareWeapon S)
        {
            W.Write(S.DATA_VERSION);
            W.Write(S.ID);
            W.Write(S.KitID);
            W.Write(S.Kills);
            W.Write(S.Deaths);
            W.Write(S.Downs);
            W.Write(S.AverageKillDistance);
            W.Write(S.AverageKillDistanceCounter);
            W.Write(S.SkullKills);
            W.Write(S.BodyKills);
            W.Write(S.ArmKills);
            W.Write(S.LegKills);
        }
        public static WarfareWeapon Read(ByteReader R)
        {
            WarfareWeapon W = new WarfareWeapon() { DATA_VERSION = R.ReadUInt32() };
            W.ID = R.ReadUInt16();
            W.KitID = R.ReadString();
            if (W.DATA_VERSION > 0)
            {
                W.Kills = R.ReadUInt32();
                W.Deaths = R.ReadUInt32();
                W.Downs = R.ReadUInt32();
                W.AverageKillDistance = R.ReadFloat();
                W.AverageKillDistanceCounter = R.ReadUInt32();
                W.SkullKills = R.ReadUInt32();
                W.BodyKills = R.ReadUInt32();
                W.ArmKills = R.ReadUInt32();
                W.LegKills = R.ReadUInt32();
            }
            return W;
        }
    }
    public class WarfareKit
    {
        public readonly static RawByteIO<WarfareKit> IO = new RawByteIO<WarfareKit>(Read, Write, null, 34);
        public const uint CURRENT_DATA_VERSION = 1;
        public uint DATA_VERSION;
        public string KitID;
        public uint Kills;
        public uint Deaths;
        public uint TimesRequested;
        public float AverageGunKillDistance;
        public uint AverageGunKillDistanceCounter;
        public uint FlagsCaptured;
        public static void Write(ByteWriter W, WarfareKit S)
        {
            W.Write(S.DATA_VERSION);
            W.Write(S.KitID);
            W.Write(S.Kills);
            W.Write(S.Deaths);
            W.Write(S.TimesRequested);
            W.Write(S.AverageGunKillDistance);
            W.Write(S.AverageGunKillDistanceCounter);
            W.Write(S.FlagsCaptured);
        }
        public static WarfareKit Read(ByteReader R)
        {
            WarfareKit K = new WarfareKit() { DATA_VERSION = R.ReadUInt32() };
            K.KitID = R.ReadString();
            if (K.DATA_VERSION > 0)
            {
                K.Kills = R.ReadUInt32();
                K.Deaths = R.ReadUInt32();
                K.TimesRequested = R.ReadUInt32();
                K.AverageGunKillDistance = R.ReadFloat();
                K.AverageGunKillDistanceCounter = R.ReadUInt32();
                K.FlagsCaptured = R.ReadUInt32();
            }
            return K;
        }
    }
    public class WarfareTeam
    {
        public readonly static RawByteIO<WarfareTeam> IO = new RawByteIO<WarfareTeam>(Read, Write, null, 65);
        public const uint CURRENT_DATA_VERSION = 1;
        public uint DATA_VERSION;
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
        public static void Write(ByteWriter W, WarfareTeam S)
        {
            W.Write(S.DATA_VERSION);
            W.Write(S.Team);
            W.Write(S.Kills);
            W.Write(S.Deaths);
            W.Write(S.Teamkills);
            W.Write(S.Downs);
            W.Write(S.Revives);
            W.Write(S.Wins);
            W.Write(S.Losses);
            W.Write(S.VehiclesRequested);
            W.Write(S.VehiclesDestroyed);
            W.Write(S.FlagsCaptured);
            W.Write(S.FlagsLost);
            W.Write(S.FobsBuilt);
            W.Write(S.FobsDestroyed);
            W.Write(S.EmplacementsBuilt);
            W.Write(S.FortificationsBuilt);
            W.Write(S.AveragePlayers);
            W.Write(S.AveragePlayersCounter);
        }
        public static WarfareTeam Read(ByteReader R)
        {
            WarfareTeam T = new WarfareTeam() { DATA_VERSION = R.ReadUInt32() };
            T.Team = R.ReadUInt8();
            if (T.DATA_VERSION > 0)
            {
                T.Kills = R.ReadUInt32();
                T.Deaths = R.ReadUInt32();
                T.Teamkills = R.ReadUInt32();
                T.Downs = R.ReadUInt32();
                T.Revives = R.ReadUInt32();
                T.Wins = R.ReadUInt32();
                T.Losses = R.ReadUInt32();
                T.VehiclesRequested = R.ReadUInt32();
                T.VehiclesDestroyed = R.ReadUInt32();
                T.FlagsCaptured = R.ReadUInt32();
                T.FlagsLost = R.ReadUInt32();
                T.FobsBuilt = R.ReadUInt32();
                T.FobsDestroyed = R.ReadUInt32();
                T.EmplacementsBuilt = R.ReadUInt32();
                T.FortificationsBuilt = R.ReadUInt32();
                T.AveragePlayers = R.ReadFloat();
                T.AveragePlayersCounter = R.ReadUInt32();
            }
            return T;
        }
    }
}
