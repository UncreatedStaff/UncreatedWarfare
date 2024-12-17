using System;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Fobs;

/// <summary>
/// A FOB that can show up on the FOB list.
/// </summary>
public interface IFob : IDeployable, IComparable<IFob>
{
    /// <summary>
    /// The display name of the FOB on the FOB list.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The display color of this FOB on the FOB list.
    /// </summary>
    Color32 Color { get; }

    /// <summary>
    /// The team that owns the FOB.
    /// </summary>
    Team Team { get; }

    /// <summary>
    /// Destroy the FOB and save any data relating to it.
    /// </summary>
    UniTask DestroyAsync(CancellationToken token = default);

    /// <summary>
    /// Add an item to a FOB.
    /// </summary>
    /// <exception cref="NotSupportedException"/>
    UniTask AddItemAsync(IFobItem fobItem, CancellationToken token = default);

    /// <summary>
    /// Upgrade an item built on a FOB to it's next state or level.
    /// </summary>
    /// <exception cref="NotSupportedException"/>
    UniTask BuildItemAsync(IFobItem fobItem, CancellationToken token = default);
}

/// <summary>
/// A FOB that can store resources.
/// </summary>
public interface IResourceFob : IFob
{
    /// <summary>
    /// Number of Supplies on the FOB.
    /// </summary>
    int BuildCount { get; }
    int AmmoCount { get; }
}