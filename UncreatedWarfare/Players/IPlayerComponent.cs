using System;
using Uncreated.Warfare.Events;

namespace Uncreated.Warfare.Players;

/// <summary>
/// Component auto-added to players on join and destroyed on disconnect.
/// </summary>
/// <remarks>Player components can receive events, but any <see cref="IPlayerEvent"/> args will only be received if they're about the player that owns the component.</remarks>
public interface IPlayerComponent
{
    WarfarePlayer Player { get; set; }
    void Init(IServiceProvider serviceProvider);
}