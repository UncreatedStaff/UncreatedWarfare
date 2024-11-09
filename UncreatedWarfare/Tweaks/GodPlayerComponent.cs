using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Tweaks;

/// <summary>
/// Manages the player's ability to take damage.
/// </summary>
[PlayerComponent]
public class GodPlayerComponent : IPlayerComponent
{
    public static readonly PermissionLeaf GodPermission = new PermissionLeaf("warfare::features.god");
    public bool IsActive { get; set; }
    public WarfarePlayer Player { get; private set; }
    public void Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        if (!isOnJoin)
            return;

        IsActive = false;
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    // todo implement
}