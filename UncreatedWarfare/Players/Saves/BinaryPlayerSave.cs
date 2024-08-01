using System;
using System.Globalization;
using System.IO;

namespace Uncreated.Warfare.Players.Saves;

public class BinaryPlayerSave : ISaveableState
{
    private const string Directory = "PlayerStates";
    private const byte DataVersion = 1;

    private readonly ILogger _logger;

    public CSteamID Steam64 { get; }
    public int TeamId { get; set; }
    public uint KitId { get; set; }
    public string SquadName { get; set; }
    public bool HasQueueSkip { get; set; }
    public uint LastGameId { get; set; }
    public bool ShouldRespawnOnJoin { get; set; }
    public bool IMGUI { get; set; }
    public bool WasNitroBoosting { get; set; }
    public bool TrackQuests { get; set; }

    /// <summary>
    /// If this save has been read to or written from a file.
    /// </summary>
    public bool WasReadFromFile { get; private set; }
    internal BinaryPlayerSave(CSteamID steam64, ILogger logger)
    {
        Steam64 = steam64;
        _logger = logger;
    }

    private static string GetPath(CSteamID steam64)
    {
        return Path.DirectorySeparatorChar +
               Path.Combine(
                   Directory,
                   steam64.m_SteamID.ToString(CultureInfo.InvariantCulture) + "_0",
                   "Uncreated_S" + UCWarfare.Season.ToString(CultureInfo.InvariantCulture),
                   "PlayerSave.dat"
               );
    }

    public void Save()
    {
        Block block = new Block { longBinaryData = true };
        block.writeByte(DataVersion);
        block.writeInt32(TeamId);
        block.writeUInt32(KitId);
        block.writeString(SquadName);
        block.writeBoolean(HasQueueSkip);
        block.writeInt64(LastGameId);
        block.writeBoolean(ShouldRespawnOnJoin);
        block.writeBoolean(IMGUI);
        block.writeBoolean(WasNitroBoosting);
        block.writeBoolean(TrackQuests);

        try
        {
            ServerSavedata.writeBlock(GetPath(Steam64), block);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {0}'s BinaryPlayerSave because an exception was thrown.", Steam64);
        }

        WasReadFromFile = true;
    }

    public void Load()
    {
        WasReadFromFile = false;
        if (!ServerSavedata.fileExists(GetPath(Steam64)))
            return;

        string path = GetPath(Steam64);

        Block block;
        try
        {
            block = ServerSavedata.readBlock(path, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read {0}'s BinaryPlayerSave because an exception was thrown.", Steam64);
            return;
        }

        block.longBinaryData = true;

        _ = block.readByte();
        TeamId = block.readInt32();
        KitId = block.readUInt32();
        SquadName = block.readString();
        LastGameId = block.readUInt32();
        HasQueueSkip = block.readBoolean();
        ShouldRespawnOnJoin = block.readBoolean();
        IMGUI = block.readBoolean();
        WasNitroBoosting = block.readBoolean();
        TrackQuests = block.readBoolean();
        WasReadFromFile = true;

        Save();
    }
}