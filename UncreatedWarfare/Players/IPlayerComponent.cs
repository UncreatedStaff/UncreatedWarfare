using System;
using Uncreated.Warfare.Events.Models;

namespace Uncreated.Warfare.Players;

/// <summary>
/// Component auto-added to players on join and destroyed on disconnect.
/// </summary>
/// <remarks>Player components can receive events, but any <see cref="IPlayerEvent"/> args will only be received if they're about the player that owns the component.</remarks>
public interface IPlayerComponent
{
    WarfarePlayer Player { get; set; }
    
    /// <summary>
    /// This function is called on player join and after every layout starts (to re-initialize scoped services).
    /// </summary>
    /// <param name="isOnJoin">The player just joined and this isn't called as the result of a new layout.</param>
    void Init(IServiceProvider serviceProvider, bool isOnJoin);
}