using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;

namespace Uncreated.Warfare.Moderation.GlobalBans;

public interface IGlobalBanWhitelistService
{
    Task<DateTimeOffset?> GetWhitelistEffectiveDate(CSteamID steam64, CancellationToken token = default);
}

public class GlobalBanWhitelistService : IGlobalBanWhitelistService
{
    private readonly IUserDataDbContext _dbContext;

    public GlobalBanWhitelistService(IUserDataDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetWhitelistEffectiveDate(CSteamID steam64, CancellationToken token = default)
    {
        ulong s64 = steam64.m_SteamID;

        DateTimeOffset dt = await _dbContext.GlobalBanWhitelists
            .Where(x => x.Steam64 == s64)
            .OrderByDescending(x => x.EffectiveTime)
            .Select(x => x.EffectiveTime)
            .FirstOrDefaultAsync(token);

        return dt == default ? null : dt;
    }
}