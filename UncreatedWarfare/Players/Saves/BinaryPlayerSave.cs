using DanielWillett.SpeedBytes;
using System;
using System.IO;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Saves;

public class BinaryPlayerSave : ISaveableState
{
    private const byte DataVersion = 1;

    private const int FlagLength = 9;

    private static readonly ByteReader Reader = new ByteReader();
    private static readonly ByteWriter Writer = new ByteWriter(64);
    private static readonly bool[] FlagBuffer = new bool[FlagLength];

    private readonly ILogger _logger;

    public CSteamID Steam64 { get; }
    public ulong TeamId { get; set; }
    public uint KitId { get; set; }
    public byte SquadTeamIdentificationNumber { get; set; }

    // todo: not used
    public bool HasQueueSkip { get; set; }
    public ulong LastGameId { get; set; }
    public bool ShouldRespawnOnJoin { get; set; }
    /// <summary>
    /// If the player has not died yet during this layout.
    /// </summary>
    public bool IsFirstLife { get; set; }
    public bool IMGUI { get; set; }
    public bool WasKitLowAmmo { get; set; }
    public bool HasSeenVoiceChatNotice { get; set; }
    public bool WasNitroBoosting { get; set; }

    /// <summary>
    /// If quests (mainly daily quests) are auto-tracked.
    /// </summary>
    public bool TrackQuests { get; set; } = true;
    public bool IsNerd { get; set; }

    /// <summary>
    /// If this save has been read from or written to a file.
    /// </summary>
    public bool WasReadFromFile { get; private set; }

    internal BinaryPlayerSave(CSteamID steam64, ILogger logger)
    {
        Steam64 = steam64;
        _logger = logger;
        SquadTeamIdentificationNumber = 0;
    }

    public void ResetOnGameStart()
    {
        KitId = 0;
        SquadTeamIdentificationNumber = 0;
        ShouldRespawnOnJoin = false;
    }

    public static string GetPlayerSaveFilePath(CSteamID steam64)
    {
        return ConfigurationHelper.GetPlayerFilePath(steam64, "Player Save.dat");
    }

    public void Save()
    {
        GameThread.AssertCurrent();

        bool[] flags = FlagBuffer;
        flags[0] = HasQueueSkip;
        flags[1] = ShouldRespawnOnJoin;
        flags[2] = IsFirstLife;
        flags[3] = IMGUI;
        flags[4] = WasNitroBoosting;
        flags[5] = TrackQuests;
        flags[6] = IsNerd;
        flags[7] = HasSeenVoiceChatNotice;
        flags[8] = WasKitLowAmmo;

        Writer.Write(DataVersion);

        Writer.Write(TeamId);
        Writer.Write(KitId);
        Writer.Write(SquadTeamIdentificationNumber);
        Writer.Write(LastGameId);

        Writer.Write(flags);

        Thread.BeginCriticalRegion();
        try
        {
            string path = GetPlayerSaveFilePath(Steam64);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            ArraySegment<byte> data = Writer.ToArraySegmentAndDontFlush();
            
            using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            stream.Write(data.Array!, data.Offset, data.Count);

            Writer.Flush();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {0}'s BinaryPlayerSave because an exception was thrown.", Steam64);
        }
        finally
        {
            Thread.EndCriticalRegion();
        }

        WasReadFromFile = true;
    }

    public void Load()
    {
        GameThread.AssertCurrent();

        WasReadFromFile = false;
        string path = GetPlayerSaveFilePath(Steam64);

        if (!File.Exists(path))
            return;

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read {0}'s BinaryPlayerSave because an exception was thrown.", Steam64);
            return;
        }

        Reader.LoadNew(bytes);

        // version
        _ = Reader.ReadUInt8();

        TeamId = Reader.ReadUInt64();
        KitId = Reader.ReadUInt32();
        SquadTeamIdentificationNumber = Reader.ReadUInt8();
        LastGameId = Reader.ReadUInt64();

        bool[] flags = Reader.ReadBoolArray();
        if (flags.Length < FlagLength)
            Array.Resize(ref flags, FlagLength);

        if (Reader.HasFailed)
        {
            _logger.LogWarning("Corrupted player save: {0}.", Steam64);
            TeamId = 0;
            KitId = 0;
            SquadTeamIdentificationNumber = 0;
            LastGameId = 0;
            Save();
            return;
        }

        HasQueueSkip = flags[0];
        ShouldRespawnOnJoin = flags[1];
        IsFirstLife = flags[2];
        IMGUI = flags[3];
        WasNitroBoosting = flags[4];
        TrackQuests = flags[5];
        IsNerd = flags[6];
        HasSeenVoiceChatNotice = flags[7];
        WasKitLowAmmo = flags[8];

        WasReadFromFile = true;

        Save();
    }
}