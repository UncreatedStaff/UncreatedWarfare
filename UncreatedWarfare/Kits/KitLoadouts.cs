using DanielWillett.ModularRpcs.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;

[RpcClass]
public class KitLoadouts<TDbContext>(KitManager manager, IServiceProvider serviceProvider) where TDbContext : IKitsDbContext, new()
{
    private readonly PlayerService _playerService = serviceProvider.GetRequiredService<PlayerService>();
    public KitManager Manager { get; } = manager;

    /// <summary>Indexed from 1.</summary>
    public Task<Kit?> GetLoadout(UCPlayer player, int loadoutId, CancellationToken token = default)
    {
        return GetLoadout(player.Steam64, loadoutId, token);
    }

    private async Task<List<Kit>> GetLoadouts(IKitsDbContext dbContext, ulong steam64, CancellationToken token = default)
    {
        List<Kit> kits = await dbContext.Kits
            .Where(x => x.Type == KitType.Loadout && x.Access.Any(y => y.Steam64 == steam64))
            .ToListAsync(token);

        if (_playerService.GetOnlinePlayerOrNull(steam64) is { } player)
        {
            await UniTask.SwitchToMainThread(token);

            return kits
                .OrderByDescending(x => Manager.IsFavoritedQuick(x.PrimaryKey, player))
                .ThenBy(x => x.InternalName ?? string.Empty)
                .ToList();
        }

        return kits;
    }

    private async Task<List<Kit>> GetLoadouts(ulong steam64, CancellationToken token = default)
    {
        await using IKitsDbContext dbContext = new TDbContext();

        return await GetLoadouts(dbContext, steam64, token).ConfigureAwait(false);
    }

    /// <summary>Indexed from 1. Use with purchase sync.</summary>
    public async Task<Kit?> GetLoadoutQuick(ulong steam64, int loadoutId, CancellationToken token = default)
    {
        if (loadoutId <= 0)
            throw new ArgumentOutOfRangeException(nameof(loadoutId));

        IEnumerable<Kit> kits = Manager.Cache.KitDataByKey.Values
            .Where(x => x.Type == KitType.Loadout &&
                        x.InternalName.Length >= 17 &&
                        KitEx.ParseStandardLoadoutId(x.InternalName, out ulong player) != -1 &&
                        player == steam64);

        if (_playerService.GetOnlinePlayerOrNull(steam64) is { } player)
        {
            await UniTask.SwitchToMainThread(token);

            kits = kits
                .OrderByDescending(x => Manager.IsFavoritedQuick(x.PrimaryKey, player))
                .ThenBy(x => x.InternalName ?? string.Empty);
        }

        foreach (Kit kit in kits)
        {
            if (--loadoutId <= 0)
                return kit;
        }

        return null;
    }

    /// <summary>Indexed from 1.</summary>
    public async Task<Kit?> GetLoadout(ulong steam64, int loadoutId, CancellationToken token = default)
    {
        if (loadoutId <= 0)
            throw new ArgumentOutOfRangeException(nameof(loadoutId));

        List<Kit> loadouts = await GetLoadouts(steam64, token).ConfigureAwait(false);

        return loadoutId > loadouts.Count ? null : loadouts[loadoutId - 1];
    }

    public async Task<string> GetFreeLoadoutName(ulong playerId)
    {
        return KitEx.GetLoadoutName(playerId, await GetFreeLoadoutId(playerId));
    }

    /// <summary>Indexed from 1.</summary>
    public async Task<int> GetFreeLoadoutId(ulong playerId)
    {
        await using IKitsDbContext dbContext = serviceProvider.GetRequiredService<WarfareDbContext>();

        return await GetFreeLoadoutId(dbContext, playerId).ConfigureAwait(false);
    }

    /// <summary>Indexed from 1.</summary>
    public async Task<int> GetFrGetFreeLoadoutIdeeLoadoutId(ulong playerId, CancellationToken token = default)
    {
        await using IKitsDbContext dbContext = serviceProvider.GetRequiredService<WarfareDbContext>();

        return await GetFreeLoadoutId(dbContext, playerId, token).ConfigureAwait(false);
    }

    /// <summary>Indexed from 1.</summary>
    public async Task<int> GetFreeLoadoutId(IKitsDbContext dbContext, ulong playerId, CancellationToken token = default)
    {
        List<Kit> loadouts = await GetLoadouts(dbContext, playerId, token).ConfigureAwait(false);
        List<int> taken = new List<int>(loadouts.Count);
        foreach (Kit kit in loadouts)
        {
            if (kit.InternalName.Length < 19)
                continue;
            int id = KitEx.ParseStandardLoadoutId(kit.InternalName);
            if (id > 0)
                taken.Add(id);
        }
        int maxId = 0;
        int lowestGap = int.MaxValue;
        int last = -1;
        taken.Sort();
        for (int i = 0; i < taken.Count; ++i)
        {
            int c = taken[i];
            if (i != 0)
            {
                if (last + 1 != c && lowestGap > last + 1)
                    lowestGap = last + 1;
            }

            last = c;

            if (maxId < c)
                maxId = c;
        }

        return lowestGap == int.MaxValue ? maxId + 1 : lowestGap;
    }

    /// <summary>
    /// Start upgrading a loadout to the current season.
    /// </summary>
    /// <remarks>
    /// The kit's <see cref="Kit.Disabled"/> property will be set to <see langword="true"/>, and the sign will change to 'being set up by admin', etc.
    /// The kit should be 'unlocked' when it's done using <see cref="UnlockLoadout"/>.
    /// </remarks>
    /// <param name="adminInstigator">The admin that handled the ticket.</param>
    /// <param name="player">The owner of the loadout.</param>
    /// <param name="class">The new class to set for the loadout.</param>
    /// <param name="loadoutInternalId">The internal name of the kit, ex. '76500000000000000_a'.</param>
    /// <returns>The upgraded kit.</returns>
    /// <exception cref="KitNotFoundException"/>
    /// <exception cref="InvalidOperationException">Kit is already up to date.</exception>
    [RpcReceive]
    public async Task<Kit> UpgradeLoadout(CSteamID adminInstigator, CSteamID player, Class @class, string loadoutInternalId, CancellationToken token = default)
    {
        Kit? kit = await Manager.FindKit(loadoutInternalId, token, true, KitManager.FullSet).ConfigureAwait(false);
        
        if (kit == null)
        {
            throw new KitNotFoundException(loadoutInternalId);
        }

        if (kit.Season >= UCWarfare.Season)
        {
            throw new InvalidOperationException($"Kit is already up to date for season {UCWarfare.Season}.");
        }

        await using IKitsDbContext dbContext = serviceProvider.GetRequiredService<WarfareDbContext>();

        Class oldClass = kit.Class;
        FactionInfo? oldFaction = kit.FactionInfo;
        kit.FactionFilterIsWhitelist = false;
        kit.FactionFilter.Clear();
        kit.Class = @class;
        kit.Faction = null;
        kit.FactionId = null;
        kit.UpdateLastEdited(adminInstigator.m_SteamID);
        kit.SetItemArray(KitDefaults<WarfareDbContext>.GetDefaultLoadoutItems(@class), dbContext);
        kit.SetUnlockRequirementArray(Array.Empty<UnlockRequirement>(), dbContext);
        kit.RequiresNitro = false;
        kit.WeaponText = string.Empty;
        kit.Disabled = true;
        kit.Season = UCWarfare.Season;
        kit.MapFilterIsWhitelist = false;
        kit.MapFilter.Clear();
        kit.Branch = KitDefaults<WarfareDbContext>.GetDefaultBranch(@class);
        kit.TeamLimit = KitDefaults<WarfareDbContext>.GetDefaultTeamLimit(@class);
        kit.RequestCooldown = KitDefaults<WarfareDbContext>.GetDefaultRequestCooldown(@class);
        kit.SquadLevel = SquadLevel.Member;
        kit.CreditCost = 0;
        kit.Type = KitType.Loadout;
        kit.PremiumCost = 0m;

        dbContext.Update(kit);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        ActionLog.Add(ActionLogType.UpgradeLoadout, $"ID: {loadoutInternalId} (#{kit.PrimaryKey}). Class: {oldClass} -> {@class}. Old Faction: {oldFaction?.FactionId ?? "none"}", adminInstigator);

        if (!await Manager.HasAccess(kit, player.m_SteamID, token))
        {
            await Manager.GiveAccess(kit, player.m_SteamID, KitAccessType.Purchase, token).ConfigureAwait(false);
            KitSync.OnAccessChanged(player.m_SteamID);
            ActionLog.Add(ActionLogType.ChangeKitAccess, player.m_SteamID.ToString(Data.AdminLocale) + " GIVEN ACCESS TO " + loadoutInternalId + ", REASON: " + KitAccessType.Purchase, adminInstigator);
        }

        await UniTask.SwitchToMainThread(token);

        WarfarePlayer? pl = _playerService.GetOnlinePlayerOrNull(player);
        if (pl != null)
            Manager.Signs.UpdateSigns(pl);

        await Manager.Distribution.DequipKit(kit, true, token).ConfigureAwait(false);

        return kit;
    }

    public async Task<Kit> UnlockLoadout(ulong fromPlayer, string loadoutName, CancellationToken token = default)
    {
        Kit? existing = await Manager.FindKit(loadoutName, token, true, KitManager.FullSet).ConfigureAwait(false);
        if (existing is null)
            return (existing, StandardErrorCode.NotFound);
        ulong player = 0;

        await using IKitsDbContext dbContext = serviceProvider.GetRequiredService<WarfareDbContext>();

        ActionLog.Add(ActionLogType.UnlockLoadout, loadoutName, fromPlayer);

        existing.UpdateLastEdited(fromPlayer);
        existing.Disabled = false;
        existing.MarkRemoteItemsDirty();
        existing.MarkRemoteUnlockRequirementsDirty();
        if (string.IsNullOrEmpty(existing.WeaponText))
            existing.WeaponText = Manager.GetWeaponText(existing);

        if (existing.InternalName.Length >= 17)
            ulong.TryParse(existing.InternalName.AsSpan(0, 17), NumberStyles.Number, Data.AdminLocale, out player);

        dbContext.Update(existing);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);
        if (UCPlayer.FromID(player) is { } pl)
        {
            Signs.UpdateLoadoutSigns(pl);
            pl.SendChat(T.DMLoadoutUnlocked, existing);
        }

        await Manager.Distribution.DequipKit(existing, true, token).ConfigureAwait(false);

        return (existing, StandardErrorCode.Success);
    }

    public async Task<(Kit?, StandardErrorCode)> LockLoadout(ulong fromPlayer, string loadoutName, CancellationToken token = default)
    {
        ulong player = 0;

        await using IKitsDbContext dbContext = serviceProvider.GetRequiredService<WarfareDbContext>();

        Kit? kit = await Manager.FindKit(loadoutName, token, exactMatchOnly: true, KitManager.FullSet).ConfigureAwait(false);
        if (kit == null)
            return (kit, StandardErrorCode.NotFound);

        ActionLog.Add(ActionLogType.UnlockLoadout, loadoutName, fromPlayer);

        kit.UpdateLastEdited(fromPlayer);
        kit.Disabled = true;
        kit.MarkRemoteItemsDirty();
        kit.MarkRemoteUnlockRequirementsDirty();
        if (string.IsNullOrEmpty(kit.WeaponText))
            kit.WeaponText = Manager.GetWeaponText(kit);

        if (kit.InternalName.Length >= 17)
            ulong.TryParse(kit.InternalName.AsSpan(0, 17), NumberStyles.Number, Data.AdminLocale, out player);

        await UniTask.SwitchToMainThread(token);
        if (UCPlayer.FromID(player) is { } pl)
            Signs.UpdateLoadoutSigns(pl);

        await Manager.Distribution.DequipKit(kit, true, token).ConfigureAwait(false);

        return (kit, StandardErrorCode.Success);
    }

    public async Task<(Kit, StandardErrorCode)> CreateLoadout(ulong fromPlayer, ulong player, Class @class, string displayName, CancellationToken token = default)
    {
        string loadoutName = await GetFreeLoadoutName(player).ConfigureAwait(false);
        Kit? kit = await Manager.FindKit(loadoutName, token, true, x => x.Kits).ConfigureAwait(false);
        if (kit != null)
            return (kit, StandardErrorCode.GenericError);

        await using IKitsDbContext dbContext = serviceProvider.GetRequiredService<WarfareDbContext>();

        IKitItem[] items = KitDefaults<WarfareDbContext>.GetDefaultLoadoutItems(@class);
        kit = new Kit(loadoutName, @class, KitDefaults<WarfareDbContext>.GetDefaultBranch(@class), KitType.Loadout, SquadLevel.Member, null)
        {
            Creator = fromPlayer,
            WeaponText = string.Empty,
            Disabled = true
        };

        kit.SetItemArray(items, dbContext);
        kit.SetSignText(dbContext, fromPlayer, kit, displayName);
        dbContext.Add(kit);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        ActionLog.Add(ActionLogType.CreateKit, loadoutName, fromPlayer);

        await Manager.GiveAccess(kit, player, KitAccessType.Purchase, token).ConfigureAwait(false);
        ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(Data.AdminLocale) + " GIVEN ACCESS TO " + loadoutName + ", REASON: " + KitAccessType.Purchase, fromPlayer);
        KitSync.OnAccessChanged(player);
        await UniTask.SwitchToMainThread(token);
        if (UCPlayer.FromID(player) is { } pl)
            Signs.UpdateLoadoutSigns(pl);
        return (kit, StandardErrorCode.Success);
    }
}