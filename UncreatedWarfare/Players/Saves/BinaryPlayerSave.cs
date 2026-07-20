using DanielWillett.SpeedBytes;
using System;
using System.IO;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Saves;

public class BinaryPlayerSave : ISaveableState
{
    private const byte DataVersion = 4;

    private const int FlagLength = 13;

    private static readonly ByteReader Reader = new ByteReader();
    private static readonly ByteWriter Writer = new ByteWriter(64);
    private static readonly bool[] FlagBuffer = new bool[FlagLength];

    private readonly ILogger _logger;

    public CSteamID Steam64 { get; }
    public ulong TeamId { get; set; }
    public CurrentKitState? KitState { get; set; }
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
    public bool HasSeenVoiceChatNotice { get; set; }
    public bool WasNitroBoosting { get; set; }
    public bool NeedsNewKitOnSpawn { get; set; }

    /// <summary>
    /// Whether or not the player can see the cosmetics of their enemies.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>.</remarks>
    public bool ViewEnemyCosmetics { get; set; }

    /// <summary>
    /// Whether or not the player can see the cosmetics of their teammates.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public bool ViewFriendlyCosmetics { get; set; }

    /// <summary>
    /// The (inverted cause default needs to be false) last value of the option in the squad menu that controls whether or not the
    /// requested kit is automatically given when they join/create a squad.
    /// </summary>
    public bool ShouldLeaveSquadMenuOpenAfterRequestingKit { get; set; }

    /// <summary>
    /// If quests (mainly daily quests) are auto-tracked.
    /// </summary>
    public bool TrackQuests { get; set; } = true;
    public bool IsNerd { get; set; }

    /// <summary>
    /// The amount of time a player was main camping when they disconnected.
    /// </summary>
    public DateTime MainCampTime { get; set; }

    /// <summary>
    /// If this save has been read from or written to a file.
    /// </summary>
    public bool WasReadFromFile { get; private set; }

    internal BinaryPlayerSave(CSteamID steam64, ILogger logger)
    {
        Steam64 = steam64;
        _logger = logger;
        SquadTeamIdentificationNumber = 0;
        ViewFriendlyCosmetics = true;
    }

    public void ResetOnGameStart()
    {
        KitState = null;
        SquadTeamIdentificationNumber = 0;
        ShouldRespawnOnJoin = false;
        NeedsNewKitOnSpawn = false;
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
        // reserved: flags[8] = WasKitLowAmmo;
        flags[9] = NeedsNewKitOnSpawn;
        flags[10] = ShouldLeaveSquadMenuOpenAfterRequestingKit;
        flags[11] = ViewEnemyCosmetics;
        flags[12] = ViewFriendlyCosmetics;

        Writer.Write(DataVersion);

        Writer.Write(TeamId);
        Writer.Write(SquadTeamIdentificationNumber);
        Writer.Write(LastGameId);
        Writer.Write(MainCampTime);

        Writer.Write(flags);

        CurrentKitState.ToWriter(KitState, Writer);

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

        try
        {
            byte[] bytes = File.ReadAllBytes(path);

            Reader.LoadNew(bytes);

            // version
            byte v = Reader.ReadUInt8();

            TeamId = Reader.ReadUInt64();
            uint kitId = 0;
            if (v <= 2)
            {
                kitId = Reader.ReadUInt32();
            }
            SquadTeamIdentificationNumber = Reader.ReadUInt8();
            LastGameId = Reader.ReadUInt64();
            MainCampTime = v > 1 ? Reader.ReadDateTime() : default;

            bool[] flags = Reader.ReadBoolArray();
            if (flags.Length < FlagLength)
                Array.Resize(ref flags, FlagLength);

            KitState = v > 2
                ? CurrentKitState.FromReader(Reader)
                : new CurrentKitState(kitId, flags[8]);

            if (Reader.HasFailed)
            {
                _logger.LogWarning("Corrupted player save: {0}.", Steam64);
                TeamId = 0;
                KitState = null;
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
            // reserved if v > 2
            NeedsNewKitOnSpawn = flags[9];
            ShouldLeaveSquadMenuOpenAfterRequestingKit = flags[10];
            ViewEnemyCosmetics = flags[11];
            ViewFriendlyCosmetics = v < 4 || flags[12];

            WasReadFromFile = true;

            Save();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read {0}'s BinaryPlayerSave because an exception was thrown.", Steam64);
        }

    }
}