using System;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Squads.Spotted;

/// <summary>
/// Object that can spot other objects. Right now this is either a <see cref="WarfarePlayer"/> or UAV.
/// </summary>
public interface ISpotter : ITransformObject
{
    /// <summary>
    /// If the spotted object's position continues to update until the duration has ran out.
    /// </summary>
    bool IsTrackable { get; }

    /// <summary>
    /// The team to show spotted effects to.
    /// </summary>
    Team Team { get; }

    /// <summary>
    /// Runs when this spotter is no longer viable.
    /// </summary>
    event Action<ISpotter>? OnDestroyed;
}