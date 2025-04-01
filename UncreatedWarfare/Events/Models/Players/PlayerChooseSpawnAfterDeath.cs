namespace Uncreated.Warfare.Events.Models.Players;

public class PlayerChooseSpawnAfterDeath : PlayerEvent
{
    public required bool WantsToRespawnAtBedroll { get; init; }
    public required Vector3 SpawnPoint { get; set; }
    public required float Yaw { get; set; }
}
