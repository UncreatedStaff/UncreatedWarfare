using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players.Cooldowns;

namespace Uncreated.Warfare.Tweaks;

internal class CombatCooldownTweak : IEventListener<PlayerDamaged>, IEventListener<PlayerDied>
{
    private readonly CooldownManager _cooldownManager;

    public CombatCooldownTweak(CooldownManager cooldownManager)
    {
        _cooldownManager = cooldownManager;
    }

    /// <inheritdoc />
    public void HandleEvent(PlayerDamaged e, IServiceProvider serviceProvider)
    {
        _cooldownManager.StartCooldown(e.Player, KnownCooldowns.Combat);
    }

    /// <inheritdoc />
    public void HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
    {
        _cooldownManager.RemoveCooldown(e.Player, KnownCooldowns.Combat);
    }
}