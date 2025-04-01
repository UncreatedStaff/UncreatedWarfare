using Uncreated.Warfare.Players.Saves;

namespace Uncreated.Warfare.Events.Models.Players;

public class PlayerChooseSpawnAfterLogin
{
    public required SteamPlayerID PlayerID { get; init; }
    public required BinaryPlayerSave PlayerSave { get; init; }
    public required bool FirstTimeJoiningServer { get; init; }
    public required bool JoiningIntoNewRound { get; init; }
    public required bool NeedsNewSpawnPoint { get; set; }
    public required Vector3 SpawnPoint { get; set; }
    public required float Yaw { get; set; }
    public required EPlayerStance InitialStance { get; set; }
}
