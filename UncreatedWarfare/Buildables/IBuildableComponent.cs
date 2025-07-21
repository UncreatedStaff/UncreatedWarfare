using System;

namespace Uncreated.Warfare.Buildables;

/// <summary>
/// Component auto-added to Buildables on when they are dropped into the world, and removed when they are destroyed.
/// </summary>
public interface IBuildableComponent : IDisposable
{
    IBuildable Buildable { get; }
}

/// <summary>
/// Component that will be transfered when the buildable is replaced.
/// </summary>
public interface IReplaceableBuildableComponent : IBuildableComponent
{
    IBuildableComponent? TryTransfer(IBuildable newBuildable);
}