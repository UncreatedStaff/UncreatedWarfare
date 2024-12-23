using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Saves;

namespace Uncreated.Warfare.Events.Models.Players;
public class PlayerChooseSpawnAfterLogin
{
    required public SteamPlayerID PlayerID { get; init; }
    required public BinaryPlayerSave PlayerSave { get; init; }
    required public bool FirstTimeJoiningServer { get; init; }
    required public bool JoiningIntoNewRound { get; init; }
    required public bool NeedsNewSpawnPoint { get; set; }
    required public Vector3 SpawnPoint { get; set; }
    required public float Yaw { get; set; }
    required public EPlayerStance InitialStance { get; set; }
}
