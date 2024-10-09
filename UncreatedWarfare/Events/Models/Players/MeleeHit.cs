using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Handles when a player melee swings, just before the hit is registered by the server.
/// </summary>
public class MeleeHit : PlayerEvent
{
    /// <summary>
    /// The <see cref="UseableMelee"/> that the player swung with.
    /// </summary>
    public required UseableMelee MeleeWeapon { get; init; }
}
