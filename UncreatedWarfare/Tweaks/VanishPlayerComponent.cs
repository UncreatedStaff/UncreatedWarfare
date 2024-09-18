using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Tweaks;

/// <summary>
/// Manages the player's ability to be seen by other players.
/// </summary>
public class VanishPlayerComponent : IPlayerComponent
{
    public static readonly PermissionLeaf VanishPermission = new PermissionLeaf("features.vanish");

    public bool IsActive
    {
        get => Player.UnturnedPlayer.movement.canAddSimulationResultsToUpdates;
        set => Player.UnturnedPlayer.movement.canAddSimulationResultsToUpdates = value;
    }

    public WarfarePlayer Player { get; private set; }
    public void Init(IServiceProvider serviceProvider)
    {
        IsActive = false;
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    // todo implement
}