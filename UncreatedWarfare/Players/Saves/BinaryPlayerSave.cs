using System;
using SDG.Unturned;
using System.Net;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Uncreated.Warfare.Players.Saves
{
    public class BinaryPlayerSave : ISaveableState

    {
        private const string DATA_DIRECTORY = "PlayerStates";

        public bool SaveFileExists => ServerSavedata.fileExists(GetPath(Steam64));
        public const byte Version = 1;
        public readonly ulong Steam64;
        private ILogger _logger;
        public ulong TeamID;
        public uint KitId;
        public string SquadName;
        public bool HasQueueSkip;
        public uint LastGameID;
        public bool ShouldRespawnOnJoin;
        public bool IsOtherDonator;
        public bool IMGUI;
        public bool WasNitroBoosting;
        public bool TrackQuests;

        public BinaryPlayerSave(ulong steam64, ILogger logger)
        {
            Steam64 = steam64;
            _logger = logger;
        }

        public void Save()
        {
            Block block = new Block();
            block.writeByte(Version);
            block.writeUInt64(TeamID);
            block.writeUInt32(KitId);
            block.writeString(SquadName);
            block.writeBoolean(HasQueueSkip);
            block.writeInt64(LastGameID);
            block.writeBoolean(ShouldRespawnOnJoin);
            block.writeBoolean(IsOtherDonator);
            block.writeBoolean(IMGUI);
            block.writeBoolean(WasNitroBoosting);
            block.writeBoolean(TrackQuests);
            ServerSavedata.writeBlock(GetPath(Steam64), block);
        }
        public void Load()
        {
            if (!SaveFileExists)
                return;

            string path = GetPath(Steam64);

            try
            {
                Block block = ServerSavedata.readBlock(path, 0);
                _ = block.readByte();
                TeamID = block.readUInt64();
                KitId = block.readUInt32();
                SquadName = block.readString();
                LastGameID = block.readUInt32();
                HasQueueSkip = block.readBoolean();
                ShouldRespawnOnJoin = block.readBoolean();
                IsOtherDonator = block.readBoolean();
                IMGUI = block.readBoolean();
                WasNitroBoosting = block.readBoolean();
                TrackQuests = block.readBoolean();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Could not fully save player {Steam64}'s state because an exception was thrown: {ex}");
            }
        }
        private static string GetPath(ulong steam64) =>
            Path.DirectorySeparatorChar +
            Path.Combine(DATA_DIRECTORY,
            steam64.ToString() +
            "_0", "Uncreated_S",
            "PlayerSave.dat");
    }
}