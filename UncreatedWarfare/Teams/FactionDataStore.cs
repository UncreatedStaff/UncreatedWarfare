using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util.DependencyInjection;

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
        await using IFactionDbContext dbContext = _serviceProvider.GetRequiredService<DontDispose<IFactionDbContext>>().Value;

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
