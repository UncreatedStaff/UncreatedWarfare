using System;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SDG.NetTransport;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Signs;

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

    /// <summary>
    /// Set the owner or group of a buildable.
    /// </summary>
    /// <returns><see langword="true"/> if the barricade state was replicated, otherwise <see langword="false"/>.</returns>
    public static bool SetOwnerOrGroup(this IBuildable obj, IServiceProvider serviceProvider, CSteamID? owner = null, CSteamID? group = null)
    {
        switch (obj.Drop)
        {
            case BarricadeDrop bdrop:
                return BarricadeUtility.SetOwnerOrGroup(bdrop, serviceProvider, owner, group);

            case StructureDrop sdrop:
                StructureUtility.SetOwnerOrGroup(sdrop, owner, group);
                return true;

            default:
                throw new InvalidOperationException($"Unable to get drop from IBuildable of type \"{obj.Drop?.GetType().AssemblyQualifiedName ?? "null"}\".");
        }
    }
}
