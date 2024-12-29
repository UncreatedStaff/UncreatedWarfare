using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.NewQuests;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Unlocks;

namespace Uncreated.Warfare.Kits;

/// <summary>
/// Caches kits based on who's online.
/// </summary>
public class KitDataCache(KitManager manager, IServiceProvider serviceProvider) : IAsyncEventListener<PlayerJoined>, IEventListener<PlayerLeft>, IEventListener<QuestCompleted>
{
    private readonly IPlayerService _playerService = serviceProvider.GetRequiredService<IPlayerService>();
    private readonly ILogger<KitDataCache> _logger = serviceProvider.GetRequiredService<ILogger<KitDataCache>>();
    public KitManager Manager { get; } = manager;
    internal ConcurrentDictionary<string, Kit> KitDataById { get; } = new ConcurrentDictionary<string, Kit>(StringComparer.OrdinalIgnoreCase);
    internal ConcurrentDictionary<uint, Kit> KitDataByKey { get; } = [];
    public Kit? GetKit(string id) => id == null ? null : KitDataById.GetValueOrDefault(id);
    public Kit? GetKit(uint pk) => KitDataByKey.GetValueOrDefault(pk);
    public bool TryGetKit(string id, out Kit kit) => KitDataById.TryGetValue(id, out kit);
    public bool TryGetKit(uint pk, out Kit kit) => KitDataByKey.TryGetValue(pk, out kit);
    public static IQueryable<Kit> IncludeCachedKitData(IQueryable<Kit> set)
    {
        return set
            .Include(x => x.UnlockRequirementsModels)
            .Include(x => x.FactionFilter)
            .Include(x => x.MapFilter)
            .Include(x => x.Translations);
    }

    public void OnKitUpdated(Kit kit, IKitsDbContext dbContext)
    {
        EntityEntry<Kit> kitEntry = dbContext.Entry(kit);

        bool ur = kitEntry.Collection(x => x.UnlockRequirementsModels).IsLoaded,
             ff = kitEntry.Collection(x => x.FactionFilter).IsLoaded,
             mf = kitEntry.Collection(x => x.MapFilter).IsLoaded,
             tr = kitEntry.Collection(x => x.Translations).IsLoaded;

        if (ur && ff && mf && tr)
        {
            KitDataByKey[kit.PrimaryKey] = kit;
            KitDataById[kit.InternalName] = kit;
        }
        else if (KitDataByKey.TryRemove(kit.PrimaryKey, out Kit existing))
        {
            existing.CopyFrom(kit, false, false);
            if (ur)
                existing.UnlockRequirementsModels = kit.UnlockRequirementsModels;
            if (ff)
                existing.FactionFilter = kit.FactionFilter;
            if (mf)
                existing.MapFilter = kit.MapFilter;
            if (tr)
                existing.Translations = kit.Translations;
            _ = KitDataByKey.GetOrAdd(kit.PrimaryKey, existing);
        }
    }
    public void Clear()
    {
        KitDataById.Clear();
        KitDataByKey.Clear();
    }
    public async Task ReloadCache(CancellationToken token)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        await using WarfareDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarfareDbContext>();
        ulong[] online = _playerService.GetOnlinePlayerSteam64Array();

        List<Kit> kits = await IncludeCachedKitData(dbContext.Kits)
            .Where(x =>
                x.Type != KitType.Loadout ||
                x.Access.Any(y => online.Any(z => y.Steam64 == z)))
            .ToListAsync(token);

        Clear();
        foreach (Kit kit in kits)
        {
            KitDataById[kit.InternalName] = kit;
            KitDataByKey[kit.PrimaryKey] = kit;
        }
    }
    public async Task<Kit?> ReloadCache(uint pk, CancellationToken token)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        await using WarfareDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarfareDbContext>();

        Kit? kit = await IncludeCachedKitData(dbContext.Kits)
            .FirstOrDefaultAsync(x => x.PrimaryKey == pk, token);

        if (kit != null)
        {
            KitDataById[kit.InternalName] = kit;
            KitDataByKey[kit.PrimaryKey] = kit;
        }
        else if (KitDataByKey.TryRemove(pk, out kit) && kit != null)
            KitDataById.TryRemove(kit.InternalName, out _);

        return kit;
    }

    internal void RemoveKit(uint pk, string kitId)
    {
        KitDataById.TryRemove(kitId, out _);
        KitDataByKey.TryRemove(pk, out _);
    }

    async UniTask IAsyncEventListener<PlayerJoined>.HandleEventAsync(PlayerJoined e, IServiceProvider serviceProvider, CancellationToken token)
    {
        foreach (Kit kit in KitDataById.Values)
        {
            if (kit == null || kit.Type == KitType.Loadout || kit.UnlockRequirementsModels is not { Count: > 0 } || kit.Disabled)
                continue;

            for (int j = 0; j < kit.UnlockRequirements.Length; j++)
            {
                if (kit.UnlockRequirements[j] is not QuestUnlockRequirement { UnlockPresets.Length: > 0 } req || req.CanAccessFast(e.Player))
                    continue;

                if (Assets.find(req.QuestId) is QuestAsset quest)
                {
                    // todo QuestManager.TryAddQuest(e.Player, quest);
                }
                else
                    _logger.LogWarning("Unknown quest id {0} in kit requirement for {1}.", req.QuestId, kit.InternalName);

                for (int r = 0; r < req.UnlockPresets.Length; r++)
                {
                    QuestTemplate? tracker = null;// todo QuestManager.CreateTracker(e.Player, req.UnlockPresets[r]);
                    if (tracker == null)
                        _logger.LogWarning("Failed to create tracker for kit {0}, player {1}.", kit.InternalName, e.Player.Names.GetDisplayNameOrPlayerName());
                }
            }
        }

        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            await using WarfareDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarfareDbContext>();
            ulong steam64 = e.Steam64.m_SteamID;

            CancellationToken tkn = e.Player.DisconnectToken;

            List<Kit> kits = await IncludeCachedKitData(dbContext.Kits)
                .Where(x =>
                    x.Type == KitType.Loadout &&
                    x.Access.Any(y => y.Steam64 == steam64))
                .ToListAsync(tkn);

            if (!e.Player.IsOnline)
                return;
            
            await UniTask.SwitchToMainThread(e.Player.DisconnectToken);

            foreach (Kit kit in kits)
            {
                KitDataById[kit.InternalName] = kit;
                KitDataByKey[kit.PrimaryKey] = kit;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating kit cache.");
        }
    }

    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        List<uint>? kits = e.Player.Component<KitPlayerComponent>().AccessibleKits;
        if (kits == null)
            return;

        foreach (Kit kit in KitDataByKey.Values)
        {
            if (kit.Type != KitType.Loadout || !kits.Contains(kit.PrimaryKey))
                continue;

            KitDataById.TryRemove(kit.InternalName, out _);
            KitDataByKey.TryRemove(kit.PrimaryKey, out _);
        }
    }

    void IEventListener<QuestCompleted>.HandleEvent(QuestCompleted e, IServiceProvider serviceProvider)
    {
        if (!e.Player.IsOnline)
            return;

        foreach (Kit kit in KitDataById.Values)
        {
            if (kit.Type == KitType.Loadout || kit.UnlockRequirements == null || kit.Disabled)
                continue;

            for (int j = 0; j < kit.UnlockRequirements.Length; j++)
            {
                if (kit.UnlockRequirements[j] is not QuestUnlockRequirement { UnlockPresets.Length: > 0 } req || req.CanAccessFast(e.Player))
                    continue;

                for (int r = 0; r < req.UnlockPresets.Length; r++)
                {
                    if (req.UnlockPresets[r] != e.PresetKey)
                        continue;

                    e.Cancel();
                    return;
                }
            }
        }
    }

    internal void OnNitroUpdated(WarfarePlayer player, byte state)
    {
        foreach (Kit kit in KitDataByKey.Values)
        {
            if (kit is { RequiresNitro: true })
            {
                Manager.Signs.UpdateSigns(kit.InternalName, player);
            }
        }

        if (state == 0)
        {
            Task.Run(async () =>
            {
                Kit? activeKit = await player.Component<KitPlayerComponent>().GetActiveKitAsync(player.DisconnectToken).ConfigureAwait(false);

                if (activeKit is { RequiresNitro: true })
                {
                    await Manager.TryGiveRiflemanKit(player, true, player.DisconnectToken);
                }
            }, player.DisconnectToken);
        }
    }
}