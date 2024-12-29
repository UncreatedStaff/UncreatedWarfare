using System;
using System.Collections.Generic;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.Entities;

/// <summary>
/// An item placed on a FOB.
/// </summary>
public interface IFobEntity : IDisposable
{
    Vector3 Position { get; }
    Quaternion Rotation { get; }
    IAssetLink<Asset> IdentifyingAsset { get; }
}