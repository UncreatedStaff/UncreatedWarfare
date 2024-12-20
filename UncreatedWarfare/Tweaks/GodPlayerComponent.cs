using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Tweaks;

/// <summary>
/// Manages the player's ability to take damage.
/// </summary>
[PlayerComponent]
public class GodPlayerComponent : IPlayerComponent, IEventListener<DamagePlayerRequested>
{
    public const string GodPermissionName = "warfare::features.god";
    public static readonly PermissionLeaf GodPermission = new PermissionLeaf(GodPermissionName);
    public bool IsActive { get; set; }
    public WarfarePlayer Player { get; private set; }
    public void Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        if (!isOnJoin)
            return;

        IsActive = false;
    }

    [EventListener(Priority = 100)]
    void IEventListener<DamagePlayerRequested>.HandleEvent(DamagePlayerRequested e, IServiceProvider serviceProvider)
    {
        if (IsActive)
            e.Cancel();
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}