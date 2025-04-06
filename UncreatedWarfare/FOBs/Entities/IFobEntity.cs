using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.Entities;

/// <summary>
/// An item placed on a FOB.
/// </summary>
public interface IFobEntity : ITransformObject
{
    /// <summary>
    /// The primary asset that identifies this fob entity.
    /// </summary>
    IAssetLink<Asset> IdentifyingAsset { get; }

    /// <summary>
    /// The team this buildable belongs to.
    /// </summary>
    Team Team { get; }

    Vector3 ITransformObject.Scale { get => Vector3.one; set => throw new NotSupportedException(); }

    /// <summary>
    /// Called when the <see cref="FobConfiguration"/> is updated.
    /// </summary>
    void UpdateConfiguration(FobConfiguration configuration);
}