using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.Entities;

/// <summary>
/// An item placed on a FOB.
/// </summary>
public interface IFobEntity : ITransformObject
{
    IAssetLink<Asset> IdentifyingAsset { get; }

    Vector3 ITransformObject.Scale { get => Vector3.one; set => throw new NotSupportedException(); }
}