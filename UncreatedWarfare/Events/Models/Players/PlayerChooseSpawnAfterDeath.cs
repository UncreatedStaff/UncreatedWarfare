using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Saves;

namespace Uncreated.Warfare.Events.Models.Players;
public class PlayerChooseSpawnAfterDeath : PlayerEvent
{
    required public bool WantsToRespawnAtBedroll { get; init; }
    required public Vector3 SpawnPoint { get; set; }
    required public float Yaw { get; set; }
}
