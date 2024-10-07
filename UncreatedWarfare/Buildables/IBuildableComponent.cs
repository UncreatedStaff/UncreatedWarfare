using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Buildables;

/// <summary>
/// Component auto-added to Buildables on when they are dropped into the world, and removed when they are destroyed.
/// </summary>
public interface IBuildableComponent
{
    IBuildable Buildable { get; protected set; }

    /// <summary>
    /// This function is called on player join and after every layout starts (to re-initialize scoped services).
    /// </summary>
    void Init(IServiceProvider serviceProvider);
}