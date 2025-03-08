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

    private bool _isAdminActive;
    private bool _isGameplayActive;

    public bool IsActive => _isAdminActive || _isGameplayActive;

    public WarfarePlayer Player { get; private set; }

    public void Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        // reset gameplay god mode when the next layout starts
        _isGameplayActive = false;
    }

    /// <summary>
    /// If god mode should be active as requested by an admin.
    /// </summary>
    public void SetAdminActive(bool isAdminActive)
    {
        _isAdminActive = isAdminActive;
    }

    /// <summary>
    /// If god mode should be active as part of the game (ex. during the leaderboard).
    /// </summary>
    public void SetGameplayActive(bool isGameplayActive)
    {
        _isGameplayActive = isGameplayActive;
    }

    [EventListener(Priority = 100)]
    void IEventListener<DamagePlayerRequested>.HandleEvent(DamagePlayerRequested e, IServiceProvider serviceProvider)
    {
        if (IsActive)
            e.Cancel();
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}