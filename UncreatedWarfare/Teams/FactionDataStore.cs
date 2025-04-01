using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Teams;

/// <summary>
/// Stores <see cref="FactionInfo"/> instances generated from MySQL.
/// </summary>
public interface IFactionDataStore
{
    /// <summary>
    /// List of all registered factions.
    /// </summary>
    IReadOnlyList<FactionInfo> Factions { get; }

    /// <summary>
    /// Reload <see cref="Factions"/> from wherever it's stored.
    /// </summary>
    Task ReloadCache(CancellationToken token = default);
}

public static class FactionDataStoreExtensions
{
    /// <summary>
    /// Search a faction from it's database model.
    /// </summary>
    public static FactionInfo? FindFaction(this IFactionDataStore dataStore, [NotNullWhen(true)] Faction? faction)
    {
        return faction == null ? null : dataStore.Factions.FirstOrDefault(f => f.PrimaryKey == faction.Key);
    }
    
    /// <summary>
    /// Search a faction from it's primary key.
    /// </summary>
    public static FactionInfo? FindFaction(this IFactionDataStore dataStore, uint primaryKey)
    {
        return dataStore.Factions.FirstOrDefault(f => f.PrimaryKey == primaryKey);
    }
    
    /// <summary>
    /// Search a faction from it's primary key.
    /// </summary>
    public static FactionInfo? FindFaction(this IFactionDataStore dataStore, uint? primaryKey)
    {
        return primaryKey.HasValue ? dataStore.Factions.FirstOrDefault(f => f.PrimaryKey == primaryKey.Value) : null;
    }

    /// <summary>
    /// Search a faction using text.
    /// </summary>
    public static FactionInfo? FindFaction(this IFactionDataStore dataStore, [NotNullWhen(true)] string? search, bool exact = true, bool onlyOneMatch = true)
    {
        if (search == null)
            return null;

        IReadOnlyList<FactionInfo> factions = dataStore.Factions;
        foreach (FactionInfo faction in factions)
        {
            if (search.Equals(faction.FactionId, StringComparison.OrdinalIgnoreCase))
                return faction;
        }
        foreach (FactionInfo faction in factions)
        {
            if (search.Equals(faction.Name, StringComparison.OrdinalIgnoreCase))
                return faction;
        }
        foreach (FactionInfo faction in factions)
        {
            if (search.Equals(faction.ShortName, StringComparison.OrdinalIgnoreCase))
                return faction;
        }

        if (exact)
            return null;

        FactionInfo? match = null;
        foreach (FactionInfo faction in factions)
        {
            if (!faction.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase))
                continue;

            if (!onlyOneMatch)
                return faction;
            if (match != null)
                return null;
            match = faction;
        }

        if (match != null)
            return match;

        foreach (FactionInfo faction in factions)
        {
            if (faction.ShortName == null || !faction.ShortName.Contains(search, StringComparison.InvariantCultureIgnoreCase))
                continue;

            if (!onlyOneMatch)
                return faction;
            if (match != null)
                return null;
            match = faction;
        }

        return match;
    }
}

public class FactionDataStore : IFactionDataStore, IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    public IReadOnlyList<FactionInfo> Factions { get; private set; } = Array.Empty<FactionInfo>();

    public FactionDataStore(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ReloadCache(CancellationToken token = default)
    {
        IServiceScope scope = _serviceProvider.CreateScope();
        await using IFactionDbContext dbContext = scope.ServiceProvider.GetRequiredService<IFactionDbContext>();

        List<Faction> dbModels = await dbContext.Factions
            .Include(faction => faction.Assets)
            .Include(faction => faction.Translations)
            .OrderBy(faction => faction.Key)
            .ToListAsync(token);

        List<FactionInfo> factions = new List<FactionInfo>(dbModels.Count);
        for (int i = 0; i < dbModels.Count; ++i)
        {
            FactionInfo newInfo = new FactionInfo(dbModels[i]);

            FactionInfo? existingFaction = Factions.FirstOrDefault(x => x.PrimaryKey == newInfo.PrimaryKey);
            if (existingFaction != null)
            {
                existingFaction.CloneFrom(newInfo);
                factions.Add(existingFaction);
            }
            else
            {
                factions.Add(newInfo);
            }
        }

        Factions = factions.AsReadOnly();
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        return ReloadCache(token).AsUniTask();
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
}
