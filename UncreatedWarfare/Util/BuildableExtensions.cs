using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Logging;

namespace Uncreated.Warfare.Util;
public static class BuildableExtensions
{
    /// <summary>
    /// Destroy the structure or barricade.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static bool Destroy(this IBuildable buildable)
    {
        if (buildable == null)
            throw new ArgumentNullException(nameof(buildable));

        GameThread.AssertCurrent();

        try
        {
            if (buildable.Model != null)
            {
                if (!buildable.IsStructure)
                {
                    if (buildable.Drop is BarricadeDrop d1
                        && !d1.GetServersideData().barricade.isDead
                        && BarricadeManager.tryGetRegion(buildable.Model, out byte x, out byte y, out ushort plant, out _))
                    {
                        BarricadeManager.destroyBarricade(d1, x, y, plant);
                        return true;
                    }
                }
                else
                {
                    if (buildable.Drop is StructureDrop s1
                        && !s1.GetServersideData().structure.isDead
                        && StructureManager.tryGetRegion(buildable.Model, out byte x, out byte y, out _))
                    {
                        StructureManager.destroyStructure(s1, x, y, Vector3.zero);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            L.LogError($"Error destroying buildable: {buildable.Asset.itemName} (#{buildable.InstanceId}).");
            L.LogError(ex);
        }

        return false;
    }
}
