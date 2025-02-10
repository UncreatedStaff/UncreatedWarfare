using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Players.PendingTasks;

[PlayerTask]
internal class DownloadKitDataPlayerTask : IPlayerPendingTask
{
    private readonly IKitsDbContext _dbContext;

    private List<uint>? _access;
    private List<uint>? _favorites;

    public DownloadKitDataPlayerTask(IKitsDbContext dbContext)
    {
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _dbContext = dbContext;
    }

    public async Task<bool> RunAsync(PlayerPending e, CancellationToken token)
    {
        Task favTask = DownloadFavorites(e, token);
        await DownloadAccess(e, token).ConfigureAwait(false);
        await favTask.ConfigureAwait(false);

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

    public void Apply(WarfarePlayer player)
    {
        if (_access == null || _favorites == null)
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
    }

    bool IPlayerPendingTask.CanReject => false;
}