using SDG.Unturned;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;
using UnityEngine;

namespace Uncreated.Warfare.Events;

public interface IBuildableDestroyedEvent
{
    UCPlayer? Instigator { get; }
    ulong InstigatorId { get; }
    Transform Transform { get; }
    IBuildable Buildable { get; }
    SqlItem<SavedStructure>? Save { get; }
    bool IsSaved { get; }
    uint InstanceID { get; }
    byte RegionPosX { get; }
    byte RegionPosY { get; }
    object Region { get; }
    EDamageOrigin DamageOrigin { get; }
}