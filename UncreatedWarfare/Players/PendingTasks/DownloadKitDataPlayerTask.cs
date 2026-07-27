using Microsoft.EntityFrameworkCore;
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
    private List<uint>? _favoriteKitIds;
    private IReadOnlyList<Kit>? _loadouts;

    public DownloadKitDataPlayerTask(
        IKitsDbContext dbContext,
        LoadoutService loadoutService)
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

        _favoriteKitIds = await _dbContext.KitFavorites
            .OrderByDescending(x => x.DateFavorited)
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
        _loadouts = await _loadoutService.GetLoadouts(e.Steam64, KitInclude.Cached, token)
                                         .ConfigureAwait(false);
    }

    public void Apply(WarfarePlayer player)
    {
        if (_access == null || _favoriteKitIds == null || _loadouts == null)
            return;

        KitPlayerComponent component = player.Component<KitPlayerComponent>();
        foreach (uint kit in _access)
        {
            component.AddAccessibleKit(kit);
        }

        component.LoadFavoriteKits(_favoriteKitIds);
        component.UpdateLoadouts(_loadouts);
    }

    bool IPlayerPendingTask.CanReject => false;
}