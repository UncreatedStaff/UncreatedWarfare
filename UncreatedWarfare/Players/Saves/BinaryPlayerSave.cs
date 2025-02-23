using System;
using System.IO;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Saves;

public class BinaryPlayerSave : ISaveableState
{
    private const byte DataVersion = 4;

    private readonly ILogger _logger;

    public CSteamID Steam64 { get; }
    public int TeamId { get; set; }
    public uint KitId { get; set; }
    public string SquadName { get; set; }
    public bool HasQueueSkip { get; set; }
    public uint LastGameId { get; set; }
    public bool ShouldRespawnOnJoin { get; set; }
    /// <summary>
    /// If the player has not died yet during this layout.
    /// </summary>
    public bool IsFirstLife { get; set; }
    public bool IMGUI { get; set; }
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
    }

    public static string GetPlayerSaveFilePath(CSteamID steam64)
    {
        return ConfigurationHelper.GetPlayerFilePath(steam64, "Player Save.dat");
    }

    public void Save()
    {
        GameThread.AssertCurrent();

        Block block = new Block { longBinaryData = true };
        block.writeByte(DataVersion);
        block.writeInt32(TeamId);
        block.writeUInt32(KitId);
        block.writeString(SquadName);
        block.writeBoolean(HasQueueSkip);
        block.writeInt64(LastGameId);
        block.writeBoolean(ShouldRespawnOnJoin);
        block.writeBoolean(IsFirstLife);
        block.writeBoolean(IMGUI);
        block.writeBoolean(WasNitroBoosting);
        block.writeBoolean(TrackQuests);
        block.writeBoolean(IsNerd);
        block.writeBoolean(HasSeenVoiceChatNotice);

        Thread.BeginCriticalRegion();
        try
        {
            string path = GetPlayerSaveFilePath(Steam64);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            ReadWrite.writeBlock(path, false, false, block);
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
        if (!ServerSavedata.fileExists(GetPlayerSaveFilePath(Steam64)))
            return;

        string path = GetPlayerSaveFilePath(Steam64);

        Block block;
        try
        {
            block = ReadWrite.readBlock(path, false, false, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read {0}'s BinaryPlayerSave because an exception was thrown.", Steam64);
            return;
        }

        block.longBinaryData = true;

        byte v = block.readByte();
        TeamId = block.readInt32();
        KitId = block.readUInt32();
        SquadName = block.readString();
        LastGameId = block.readUInt32();
        HasQueueSkip = block.readBoolean();
        ShouldRespawnOnJoin = block.readBoolean();
        if (v > 3)
            IsFirstLife = block.readBoolean();
        IMGUI = block.readBoolean();
        WasNitroBoosting = block.readBoolean();
        TrackQuests = block.readBoolean();
        if (v > 1)
            IsNerd = block.readBoolean();
        if (v > 2)
            HasSeenVoiceChatNotice = block.readBoolean();

        WasReadFromFile = true;

        Save();
    }
}