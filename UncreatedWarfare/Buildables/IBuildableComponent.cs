using System;

namespace Uncreated.Warfare.Buildables;

/// <summary>
/// Component auto-added to Buildables on when they are dropped into the world, and removed when they are destroyed.
/// </summary>
public interface IBuildableComponent : IDisposable
{
    IBuildable Buildable { get; }
}