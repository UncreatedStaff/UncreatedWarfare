using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Tweaks;

/// <summary>
/// Manages the player's ability to take damage.
/// </summary>
public class GodPlayerComponent : IPlayerComponent
{
    public static readonly PermissionLeaf GodPermission = new PermissionLeaf("features.god");
    public bool IsActive { get; set; }
    public WarfarePlayer Player { get; private set; }
    public void Init(IServiceProvider serviceProvider)
    {
        IsActive = false;
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    // todo implement
}