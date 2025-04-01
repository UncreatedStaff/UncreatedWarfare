using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Players.PendingTasks;

[PlayerTask]
internal class DownloadKitDataPlayerTask : IPlayerPendingTask
{
    private readonly IKitsDbContext _dbContext;
    private readonly LoadoutService _loadoutService;

    private List<uint>? _access;
    private List<uint>? _favorites;
    private IReadOnlyList<Kit>? _loadouts;

    public DownloadKitDataPlayerTask(IKitsDbContext dbContext, LoadoutService loadoutService)
    {
        _dbContext = dbContext;
        _loadoutService = loadoutService;
        
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public async Task<bool> RunAsync(PlayerPending e, CancellationToken token)
    {
        await DownloadAccess(e, token).ConfigureAwait(false);
        await DownloadFavorites(e, token).ConfigureAwait(false);
        await DownloadLoadouts(e, token).ConfigureAwait(false);

        return true;
    }

    private async Task DownloadFavorites(PlayerPending e, CancellationToken token)
    {
        ulong s64 = e.Steam64.m_SteamID;

        _favorites = await _dbContext.KitFavorites
            .Where(x => x.Steam64 == s64)
            .Select(x => x.KitId)
            .ToListAsync(token)
            .ConfigureAwait(false);
    }

    private async Task DownloadAccess(PlayerPending e, CancellationToken token)
    {
        ulong s64 = e.Steam64.m_SteamID;

        _access = await _dbContext.KitAccess
            .Where(x => x.Steam64 == s64)
            .Select(x => x.KitId)
            .ToListAsync(token)
            .ConfigureAwait(false);
    }

    private async Task DownloadLoadouts(PlayerPending e, CancellationToken token)
    {
        ulong s64 = e.Steam64.m_SteamID;

        _loadouts = await _loadoutService.GetLoadouts(e.Steam64, KitInclude.Cached, token)
                                         .ConfigureAwait(false);
    }

    public void Apply(WarfarePlayer player)
    {
        if (_access == null || _favorites == null || _loadouts == null)
            return;

        KitPlayerComponent component = player.Component<KitPlayerComponent>();
        foreach (uint kit in _access)
        {
            component.AddAccessibleKit(kit);
        }

        foreach (uint kit in _favorites)
        {
            component.AddFavoriteKit(kit);
        }

        component.UpdateLoadouts(_loadouts);
    }

    bool IPlayerPendingTask.CanReject => false;
}