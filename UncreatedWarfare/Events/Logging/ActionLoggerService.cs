using System;
using System.Globalization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Logging;


public sealed class ActionLoggerService : IEventListener<IActionLoggableEvent>
{
    public void AddAction(in ActionLogEntry entry)
    {
        //Console.WriteLine(entry.Player.ToString("D17", CultureInfo.InvariantCulture) + " " + entry.Type.LogName + (entry.Message != null ? " " + entry.Message : string.Empty));
    }

    /// <inheritdoc />
    public void HandleEvent(IActionLoggableEvent e, IServiceProvider serviceProvider)
    {
        ActionLogEntry[]? multipleEntries = null;

        ActionLogEntry entry = e.GetActionLogEntry(serviceProvider, ref multipleEntries);

        if (multipleEntries != null)
        {
            foreach (ActionLogEntry entry2 in multipleEntries)
            {
                AddAction(in entry2);
            }
        }

        if (entry.Message != null)
        {
            AddAction(in entry);
        }
    }

    public static string DescribeInput(InputInfo info)
    {
        switch (info.type)
        {
            case ERaycastInfoType.NONE:
                return "No hit";

            case ERaycastInfoType.SKIP:
                return "Skipped";

            case ERaycastInfoType.OBJECT:
                ObjectInfo obj = LevelObjectUtility.FindObject(info.transform);
                if (!obj.HasValue)
                {
                    return $"Hit unknown object at {info.point:F2}.";
                }
                return $"Hit object: {AssetLink.ToDisplayString(obj.Object.asset)}, " +
                       $"Instance ID: {obj.Object.instanceID} @ {obj.Object.transform.position:F2}, {obj.Object.transform.eulerAngles:F2}, " +
                       $"Section: {info.section}, Collider: {info.colliderTransform?.name}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.PLAYER:
                if (info.player == null)
                    break;
                PlayerNames names = new PlayerNames(info.player);
                return $"Hit player: {names.ToString()}, Limb: {EnumUtility.GetName(info.limb)}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.ZOMBIE:
                if (info.zombie == null)
                    break;
                return $"Hit zombie, Limb: {EnumUtility.GetName(info.limb)}, Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.ANIMAL:
                if (info.animal == null)
                    break;
                return $"Hit animal: {AssetLink.ToDisplayString(info.animal.asset)}, Limb: {EnumUtility.GetName(info.limb)}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.VEHICLE:
                if (info.vehicle == null)
                    break;
                return $"Hit vehicle: {AssetLink.ToDisplayString(info.vehicle.asset)} owned by {info.vehicle.lockedOwner.m_SteamID} ({info.vehicle.lockedGroup.m_SteamID}), " +
                       $"Collider: {info.colliderTransform?.name}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.BARRICADE:
                BarricadeDrop? barricade = BarricadeManager.FindBarricadeByRootTransform(info.transform);
                if (barricade == null)
                    break;

                BarricadeData barricadeData = barricade.GetServersideData();
                return $"Hit barricade: {AssetLink.ToDisplayString(barricade.asset)} owned by {barricadeData.owner} ({barricadeData.group}), " +
                       $"Instance ID: {barricade.instanceID} @ {barricadeData.point:F2}, {barricadeData.rotation:F2}, " +
                       $"Collider: {info.colliderTransform?.name}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.STRUCTURE:
                StructureDrop? structure = StructureManager.FindStructureByRootTransform(info.transform);
                if (structure == null)
                    break;

                StructureData structureData = structure.GetServersideData();
                return $"Hit structure: {AssetLink.ToDisplayString(structure.asset)} owned by {structureData.owner} ({structureData.group}), " +
                       $"Instance ID: {structure.instanceID} @ {structureData.point:F2}, {structureData.rotation:F2}, " +
                       $"Collider: {info.colliderTransform?.name}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.RESOURCE:
                if (!ResourceManager.tryGetRegion(info.transform, out byte x, out byte y, out ushort index))
                    break;

                ResourceSpawnpoint tree = LevelGround.trees[x, y][index];
                return $"Hit resource: {AssetLink.ToDisplayString(tree.asset)}, " +
                       $"Instance ID: ({x}, {y}, # {index}) @ {tree.point:F2}, {tree.angle:F2}, " +
                       $"Collider: {info.colliderTransform?.name}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";
        }

        return $"Hit {info.type}, collider: {info.colliderTransform?.GetSceneHierarchyPath()}, " +
               $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";
    }
}
