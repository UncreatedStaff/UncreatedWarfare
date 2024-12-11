using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Seasons;

namespace Uncreated.Warfare.Util;
public static class TeamUtility
{
    /// <summary>
    /// Get faction information about a faction string from a <see cref="ITeamManager{TTeam}"/> config.
    /// </summary>
    /// <exception cref="LayoutConfigurationException">Failed to find a valid faction.</exception>
    public static async Task<Faction> ResolveTeamFactionHint(string? faction, IGameDataDbContext dbContext, ITeamManager<Team> readFrom, MapScheduler mapScheduler, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(faction))
        {
            throw new LayoutConfigurationException(readFrom, "Missing faction for team.");
        }

        int mapId = -1;
        if (faction.StartsWith("Map", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!int.TryParse(faction.AsSpan(3).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out mapId) || mapId <= 0)
                throw new LayoutConfigurationException(readFrom, "Map team number invalid or less than 1.");
        }

        if (mapId == -1)
        {
            Faction factionObj = await dbContext.Factions.FirstOrDefaultAsync(x => EF.Functions.Like(x.InternalName, faction), token);
            if (factionObj == null)
                throw new LayoutConfigurationException(readFrom, $"Unrecognized faction: \"{faction}\".");

            return factionObj;
        }

        int currentMapId = mapScheduler.Current;

        MapData? map = await dbContext.Maps
            .Include(x => x.Team1Faction)
            .Include(x => x.Team2Faction)
            .FirstOrDefaultAsync(x => x.Id == currentMapId, token);

        if (map == null)
        {
            throw new LayoutConfigurationException(readFrom, $"Unknown current map for faction \"{mapId}\".");
        }

        return mapId switch
        {
            1 => map.Team1Faction,
            2 => map.Team2Faction,
            _ => throw new LayoutConfigurationException(readFrom, $"Map \"{map.DisplayName}\" does not have a faction {mapId}.")
        };
    }
}
