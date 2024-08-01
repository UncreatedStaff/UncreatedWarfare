using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Players.Management.Legacy;

namespace Uncreated.Warfare.Kits;

/// <summary>
/// Caches kits based on who's online.
/// </summary>
public class KitDataCache(KitManager manager) : IPlayerConnectListener, IPlayerDisconnectListener, IQuestCompletedHandler
{
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
        await using WarfareDbContext dbContext = new WarfareDbContext();
        ulong[] online = PlayerManager.GetOnlinePlayersArray();

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
        await using WarfareDbContext dbContext = new WarfareDbContext();

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

    async void IPlayerConnectListener.OnPlayerConnecting(UCPlayer player)
    {
        foreach (Kit kit in KitDataById.Values)
        {
            if (kit == null || kit.Type == KitType.Loadout || kit.UnlockRequirementsModels is not { Count: > 0 } || kit.Disabled)
                continue;

            for (int j = 0; j < kit.UnlockRequirements.Length; j++)
            {
                if (kit.UnlockRequirements[j] is not QuestUnlockRequirement { UnlockPresets.Length: > 0 } req || req.CanAccess(player))
                    continue;

                if (Assets.find(req.QuestId) is QuestAsset quest)
                    QuestManager.TryAddQuest(player, quest);
                else
                    L.LogWarning("Unknown quest id " + req.QuestId + " in kit requirement for " + kit.InternalName);

                for (int r = 0; r < req.UnlockPresets.Length; r++)
                {
                    BaseQuestTracker? tracker = QuestManager.CreateTracker(player, req.UnlockPresets[r]);
                    if (tracker == null)
                        L.LogWarning("Failed to create tracker for kit " + kit.InternalName + ", player " + player.Name.PlayerName);
                }
            }
        }

        try
        {
            await using WarfareDbContext dbContext = new WarfareDbContext();
            ulong steam64 = player.Steam64;

            CancellationToken tkn = player.DisconnectToken;
            using CombinedTokenSources tokens = tkn.CombineTokensIfNeeded(UCWarfare.UnloadCancel);

            List<Kit> kits = await IncludeCachedKitData(dbContext.Kits)
                .Where(x =>
                    x.Type == KitType.Loadout &&
                    x.Access.Any(y => y.Steam64 == steam64))
                .ToListAsync(tkn);

            if (!player.IsOnline)
                return;
            
            await UCWarfare.ToUpdate(player.DisconnectToken);

            foreach (Kit kit in kits)
            {
                KitDataById[kit.InternalName] = kit;
                KitDataByKey[kit.PrimaryKey] = kit;
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error updating kit cache.");
            L.LogError(ex);
        }
    }

    void IQuestCompletedHandler.OnQuestCompleted(QuestCompleted e)
    {
        if (!e.Player.IsOnline)
            return;

        foreach (Kit kit in KitDataById.Values)
        {
            if (kit.Type == KitType.Loadout || kit.UnlockRequirements == null || kit.Disabled)
                continue;

            for (int j = 0; j < kit.UnlockRequirements.Length; j++)
            {
                if (kit.UnlockRequirements[j] is not QuestUnlockRequirement { UnlockPresets.Length: > 0 } req || req.CanAccess(e.Player))
                    continue;

                for (int r = 0; r < req.UnlockPresets.Length; r++)
                {
                    if (req.UnlockPresets[r] != e.PresetKey)
                        continue;
                    
                    e.Break();
                    return;
                }
            }
        }
    }

    void IPlayerDisconnectListener.OnPlayerDisconnecting(UCPlayer player)
    {
        if (player.AccessibleKits == null)
            return;

        foreach (Kit kit in KitDataByKey.Values)
        {
            if (kit.Type != KitType.Loadout || !player.AccessibleKits.Contains(kit.PrimaryKey))
                continue;

            KitDataById.TryRemove(kit.InternalName, out _);
            KitDataByKey.TryRemove(kit.PrimaryKey, out _);
        }
    }

    internal void OnNitroUpdated(UCPlayer player, byte state)
    {
        foreach (Kit kit in KitDataByKey.Values)
        {
            if (kit is { RequiresNitro: true })
                Signs.UpdateKitSigns(player, kit.InternalName);
        }

        if (state == 0)
        {
            UCWarfare.RunTask(static async (mngr, player, tkn) =>
            {
                Kit? activeKit = await player.GetActiveKit(tkn).ConfigureAwait(false);

                if (activeKit is { RequiresNitro: true })
                {
                    await mngr.TryGiveRiflemanKit(player, true, true, player.DisconnectToken);
                }
            }, Manager, player, player.DisconnectToken, ctx: $"Checking for removing kit from {player} after losing nitro boost.");
        }
    }
}
