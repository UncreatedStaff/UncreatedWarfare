using System;
using System.Collections.Generic;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;
using Uncreated.Warfare.Zones.Pathing;

namespace Uncreated.Warfare.Layouts.Phases.Flags;
public class FlagPhaseSettings
{
    /// <summary>
    /// A type implementing <see cref="IZoneProvider"/> used to get available zones.
    /// </summary>
    public string? FlagPool { get; set; }

    /// <summary>
    /// A list of types implementing <see cref="IZoneProvider"/> used to get available zones.
    /// </summary>
    public string?[]? FlagPools { get; set; }

    /// <summary>
    /// A type implementing <see cref="IZonePathingProvider"/> used to create an ordered list of zones used for the current game.
    /// </summary>
    public string? Pathing { get; set; }
    
    /// <summary>
    /// Parse <see cref="FlagPool"/> and <see cref="FlagPools"/> into a list of provider types.
    /// </summary>
    public IReadOnlyList<Type> GetFlagPoolTypes(ILogger logger)
    {
        List<Type?> types = new List<Type?>(FlagPools?.Length ?? 1);
        Type? type;
        if (FlagPools != null)
        {
            foreach (string? typeName in FlagPools)
            {
                if (typeName == null)
                {
                    logger.LogWarning("FlagPools includes a null value in FlagPhaseSettings.");
                    continue;
                }

                if (!ContextualTypeResolver.TryResolveType(typeName, out type, typeof(IZoneProvider)))
                {
                    logger.LogWarning("FlagPools includes an unknown or non-{1} type, \"{0}\", in FlagPhaseSettings.", typeName, typeof(IZoneProvider));
                    continue;
                }

                if (types.Contains(type))
                {
                    logger.LogInformation("Duplicate type {0} in FlagPools in FlagPhaseSettings.", type);
                }
                else
                {
                    types.Add(type);
                }
            }
        }

        if (FlagPool == null)
        {
            if (types.Count == 0)
            {
                logger.LogWarning("FlagPool is null in FlagPhaseSettings.");
            }

            return types!;
        }

        if (!ContextualTypeResolver.TryResolveType(FlagPool, out type, typeof(IZoneProvider)))
        {
            logger.LogWarning("FlagPool is an unknown or non-{1} type, \"{0}\", in FlagPhaseSettings.", FlagPool, typeof(IZoneProvider));
            return types!;
        }

        if (types.Contains(type))
        {
            logger.LogInformation("Duplicate type {0} in FlagPools in FlagPhaseSettings.", type);
        }
        else
        {
            types.Add(type);
        }

        return types!;
    }
}
