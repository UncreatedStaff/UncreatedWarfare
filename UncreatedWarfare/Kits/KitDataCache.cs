using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SDG.Unturned;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare.Kits;

/// <summary>
/// Caches kits based on who's online.
/// </summary>
public class KitDataCache(KitManager manager) : IPlayerConnectListener, IPlayerDisconnectListener, IQuestCompletedHandler
{
    public KitManager Manager { get; } = manager;
    internal ConcurrentDictionary<string, Kit> KitDataById { get; } = new ConcurrentDictionary<string, Kit>(StringComparer.OrdinalIgnoreCase);
    internal ConcurrentDictionary<uint, Kit> KitDataByKey { get; } = [];
    public Kit? GetKit(string id) => KitDataById.GetValueOrDefault(id);
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

    public void OnKitUpdated(Kit kit)
    {
        KitDataByKey[kit.PrimaryKey] = kit;
        KitDataById[kit.InternalName] = kit;
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

                if (Assets.find(req.QuestID) is QuestAsset quest)
                    QuestManager.TryAddQuest(player, quest);
                else
                    L.LogWarning("Unknown quest id " + req.QuestID + " in kit requirement for " + kit.InternalName);

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
            tkn.CombineIfNeeded(UCWarfare.UnloadCancel);

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

            KitDataById.Remove(kit.InternalName, out _);
            KitDataByKey.Remove(kit.PrimaryKey, out _);
        }
    }

    internal void OnNitroUpdated(UCPlayer player, byte state)
    {
        foreach (Kit kit in KitDataByKey.Values)
        {
            if (kit is { RequiresNitro: true })
                Signs.UpdateKitSigns(player, kit.InternalName);
        }

        Kit? activeKit = player.GetActiveKitNoWriteLock();

        if (state == 0 && activeKit is { RequiresNitro: true })
        {
            UCWarfare.RunTask(Manager.TryGiveRiflemanKit, player, true, true, Data.Gamemode.UnloadToken,
                ctx: "Giving rifleman kit to " + player + " after losing nitro boost.");
        }
    }
}
