using SDG.Unturned;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Buildables;
using UnityEngine;

namespace Uncreated.Warfare.Events;

public interface IBuildableDestroyedEvent
{
    UCPlayer? Instigator { get; }
    ulong InstigatorId { get; }
    Transform Transform { get; }
    IBuildable Buildable { get; }
    BuildableSave? Save { get; }
    bool IsSaved { get; }
    uint InstanceID { get; }
    byte RegionPosX { get; }
    byte RegionPosY { get; }
    object Region { get; }
    EDamageOrigin DamageOrigin { get; }
    UnturnedAssetReference PrimaryAsset { get; }
    UnturnedAssetReference SecondaryAsset { get; }
}