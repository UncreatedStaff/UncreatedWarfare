using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Fobs;

/// <summary>
/// An item placed on a FOB.
/// </summary>
public interface IFobItem
{
    IBuildable Buildable { get; }
    public Vector3 Position { get => Buildable.Position; }
    public Quaternion Rotation { get => Buildable.Rotation; }
    public int GetHashCode() => (int)Buildable.InstanceId;
}