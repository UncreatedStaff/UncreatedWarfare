using DanielWillett.ReflectionTools;
using System;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Buildables;

[Priority(10 /* after MainBaseBuildables, before most things */)]
public class MainBaseBuildables : ILevelHostedService, ILayoutHostedService
{
    private readonly BuildableAttributesDataStore _attributes;
    private readonly ZoneStore _zoneStore;

    /// <summary>
    /// Indicates that a barricade in main or the war room will still be wiped.
    /// </summary>
    public const string TransientAttribute = "transient";

    /// <summary>
    /// Indicates that a barricade outside of main and the war room won't be wiped.
    /// </summary>
    public const string PermanentAttribute = "permanent";

    public MainBaseBuildables(BuildableAttributesDataStore attributes, ZoneStore zoneStore)
    {
        _attributes = attributes;
        _zoneStore = zoneStore;
    }

    public void ClearOtherBuildables()
    {
        // if zones are missing this could be bad.
        if (_zoneStore.SearchZone(ZoneType.MainBase) == null)
            throw new InvalidOperationException("Main base not found.");

        if (_zoneStore.SearchZone(ZoneType.WarRoom) == null)
            throw new InvalidOperationException("War room not found.");

        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateNonPlantedBarricades())
        {
            BarricadeData data = barricade.Data;
            if (_zoneStore.IsInsideZone(data.point, ZoneType.MainBase, null) || _zoneStore.IsInsideZone(data.point, ZoneType.WarRoom, null))
            {
                if (!_attributes.HasAttribute(data.instanceID, false, TransientAttribute))
                    continue;
            }
            else if (_attributes.HasAttribute(data.instanceID, false, PermanentAttribute))
            {
                continue;
            }

            BarricadeUtility.PreventItemDrops(barricade.Drop);
            BuildableExtensions.SetSalvageInfo(barricade.Drop.model, EDamageOrigin.Unknown, CSteamID.Nil, false, null);
            BarricadeManager.destroyBarricade(barricade.Drop, barricade.Coord.x, barricade.Coord.y, barricade.Plant);
        }

        foreach (StructureInfo structure in StructureUtility.EnumerateStructures())
        {
            StructureData data = structure.Data;
            if (_zoneStore.IsInsideZone(data.point, ZoneType.MainBase, null) || _zoneStore.IsInsideZone(data.point, ZoneType.WarRoom, null))
            {
                if (!_attributes.HasAttribute(data.instanceID, true, TransientAttribute))
                    continue;
            }
            else if (_attributes.HasAttribute(data.instanceID, true, PermanentAttribute))
            {
                continue;
            }

            BuildableExtensions.SetSalvageInfo(structure.Drop.model, EDamageOrigin.Unknown, CSteamID.Nil, false, null);
            StructureManager.destroyStructure(structure.Drop, structure.Coord.x, structure.Coord.y, Vector3.zero);
        }
    }

    /// <inheritdoc />
    public UniTask LoadLevelAsync(CancellationToken token)
    {
        ClearOtherBuildables();
        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    public UniTask StartAsync(CancellationToken token)
    {
        ClearOtherBuildables();
        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    public UniTask StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
}