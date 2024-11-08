using DanielWillett.ModularRpcs.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;

/// <summary>
/// Handles operations specific to loadouts.
/// </summary>
[RpcClass]
public class KitLoadouts(KitManager manager, IServiceProvider serviceProvider) : IDisposable
{
    private readonly IPlayerService _playerService = serviceProvider.GetRequiredService<IPlayerService>();
    private readonly ChatService _chatService = serviceProvider.GetRequiredService<ChatService>();
    private readonly KitCommandTranslations _kitTranslations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
    private readonly LanguageService _languageService = serviceProvider.GetRequiredService<LanguageService>();
    
    /// <summary>
    /// Ensures <see cref="GetFreeLoadoutId"/> can be used without worrying about the ID being stolen.
    /// </summary>
    private readonly SemaphoreSlim _loadoutCreationSemaphore = new SemaphoreSlim(1, 1);

    public KitManager Manager { get; } = manager;

    void IDisposable.Dispose()
    {
        _loadoutCreationSemaphore.Dispose();
    }

    /// <summary>
    /// Indexed from 1.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    /// <exception cref="ArgumentOutOfRangeException">Loadout ID less than or equal to zero.</exception>
    [Pure]
    public Kit? GetLoadoutQuick(CSteamID steam64, int loadoutId, CancellationToken token = default)
    {
        GameThread.AssertCurrent();

        if (loadoutId <= 0)
            throw new ArgumentOutOfRangeException(nameof(loadoutId));

        IEnumerable<Kit> kits = Manager.Cache.KitDataByKey.Values
            .Where(x => x.Type == KitType.Loadout
                        && LoadoutIdHelper.Parse(x.InternalName, out CSteamID player) > 0
                        && player.m_SteamID == steam64.m_SteamID
                    );

        if (_playerService.GetOnlinePlayerOrNull(steam64) is { } player)
        {
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

    /// <summary>
    /// Get the loadout for a sign with the given index.
    /// </summary>
    /// <remarks>Indexed from 1.</remarks>
    [Pure]
    public async Task<Kit?> GetLoadout(CSteamID steam64, int loadoutIndex, CancellationToken token = default)
    {
        if (loadoutIndex <= 0)
            throw new ArgumentOutOfRangeException(nameof(loadoutIndex));

        List<Kit> loadouts = await GetLoadouts(steam64, token, true).ConfigureAwait(false);

        return loadoutIndex > loadouts.Count ? null : loadouts[loadoutIndex - 1];
    }

    /// <summary>
    /// Get the loadout for a sign with the given index.
    /// </summary>
    /// <remarks>Indexed from 1.</remarks>
    [Pure]
    public async Task<Kit?> GetLoadout(IKitsDbContext dbContext, CSteamID steam64, int loadoutIndex, CancellationToken token = default)
    {
        if (loadoutIndex <= 0)
            throw new ArgumentOutOfRangeException(nameof(loadoutIndex));

        List<Kit> loadouts = await GetLoadouts(dbContext, steam64, token, true).ConfigureAwait(false);

        return loadoutIndex > loadouts.Count ? null : loadouts[loadoutIndex - 1];
    }

    /// <summary>
    /// Get a list of all loadouts for a player in the order they would appear on signs.
    /// </summary>
    [Pure]
    public async Task<List<Kit>> GetLoadouts(CSteamID steam64, CancellationToken token = default, bool doLock = true)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        await using IKitsDbContext dbContext = scope.ServiceProvider.GetRequiredService<IKitsDbContext>();
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        return await GetLoadouts(dbContext, steam64, token, doLock).ConfigureAwait(false);
    }

    /// <summary>
    /// Get a list of all loadouts for a player in the order they would appear on signs.
    /// </summary>
    [Pure]
    public async Task<List<Kit>> GetLoadouts(IKitsDbContext dbContext, CSteamID steam64, CancellationToken token = default, bool doLock = true)
    {
        if (doLock)
            await _loadoutCreationSemaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong s64 = steam64.m_SteamID;
            List<Kit> kits = await dbContext.Kits
                .Where(x => x.Type == KitType.Loadout && x.Access.Any(y => y.Steam64 == s64))
                .OrderByDescending(x => dbContext.KitFavorites.Any(y => y.KitId == x.PrimaryKey && y.Steam64 == s64))
                .ThenBy(y => y.InternalName)
                .ToListAsync(token)
                .ConfigureAwait(false);

            return kits;
        }
        finally
        {
            if (doLock)
                _loadoutCreationSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets the first free loadout ID for a player.
    /// </summary>
    /// <remarks>Indexed from 1.</remarks>
    [Pure]
    public async Task<string> GetFreeLoadoutName(CSteamID playerId, CancellationToken token = default, bool doLock = true)
    {
        return LoadoutIdHelper.GetLoadoutName(playerId, await GetFreeLoadoutId(playerId, token, doLock));
    }

    /// <summary>
    /// Gets the first free loadout ID for a player.
    /// </summary>
    /// <remarks>Indexed from 1.</remarks>
    [Pure]
    public async Task<string> GetFreeLoadoutName(IKitsDbContext dbContext, CSteamID playerId, CancellationToken token = default, bool doLock = true)
    {
        return LoadoutIdHelper.GetLoadoutName(playerId, await GetFreeLoadoutId(dbContext, playerId, token, doLock));
    }

    /// <summary>
    /// Gets the first free loadout number for a player.
    /// </summary>
    /// <remarks>Indexed from 1.</remarks>
    [Pure]
    public async Task<int> GetFreeLoadoutId(CSteamID playerId, CancellationToken token = default, bool doLock = true)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        await using IKitsDbContext dbContext = scope.ServiceProvider.GetRequiredService<IKitsDbContext>();
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        return await GetFreeLoadoutId(dbContext, playerId, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the first free loadout number for a player.
    /// </summary>
    /// <remarks>Indexed from 1.</remarks>
    [Pure]
    public async Task<int> GetFreeLoadoutId(IKitsDbContext dbContext, CSteamID playerId, CancellationToken token = default, bool doLock = true)
    {
        if (doLock)
            await _loadoutCreationSemaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong s64 = playerId.m_SteamID;
            string likeExpr = s64.ToString(CultureInfo.InvariantCulture) + "\\_%"; // s64_%

            // not using GetLoadouts because we need to check by name, not by access, just in case something gets corrupted
            // plus order doesn't matter so may as well not take the extra processing power

            List<Kit> loadouts = await dbContext.Kits
                    .Where(x => x.Type == KitType.Loadout && EF.Functions.Like(x.InternalName, likeExpr))
                    .ToListAsync(token)
                    .ConfigureAwait(false);

            List<int> taken = new List<int>(loadouts.Count);
            foreach (Kit kit in loadouts)
            {
                int id = LoadoutIdHelper.Parse(kit.InternalName);
                if (id > 0)
                    taken.Add(id);
            }

            // find first open number
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
        finally
        {
            if (doLock)
                _loadoutCreationSemaphore.Release();
        }
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
    public async Task<Kit> UpgradeLoadout(CSteamID adminInstigator, CSteamID player, Class @class, string loadoutInternalId, CancellationToken token = default)
    {
        Kit? kit;
        Class oldClass;
        Faction? oldFaction;

        using (IServiceScope scope = serviceProvider.CreateScope())
        await using (IKitsDbContext dbContext = scope.ServiceProvider.GetRequiredService<IKitsDbContext>())
        {
            kit = await Manager.FindKit(dbContext, loadoutInternalId, token, true, KitManager.FullSet).ConfigureAwait(false);

            if (kit == null)
            {
                throw new KitNotFoundException(loadoutInternalId);
            }

            if (kit.Season >= WarfareModule.Season)
            {
                throw new InvalidOperationException($"Kit is already up to date for season {WarfareModule.Season}.");
            }

            oldClass = kit.Class;
            oldFaction = kit.Faction;
            kit.FactionFilterIsWhitelist = false;
            kit.FactionFilter.Clear();
            kit.Class = @class;
            kit.Faction = null;
            kit.FactionId = null;
            kit.UpdateLastEdited(adminInstigator);
            kit.SetItemArray(KitDefaults.GetDefaultLoadoutItems(@class), dbContext);
            kit.SetUnlockRequirementArray(Array.Empty<UnlockRequirement>(), dbContext);
            kit.RequiresNitro = false;
            kit.WeaponText = string.Empty;
            kit.Disabled = true;
            kit.Season = WarfareModule.Season;
            kit.MapFilterIsWhitelist = false;
            kit.MapFilter.Clear();
            kit.Branch = KitDefaults.GetDefaultBranch(@class);
            kit.TeamLimit = KitDefaults.GetDefaultTeamLimit(@class);
            kit.RequestCooldown = KitDefaults.GetDefaultRequestCooldown(@class);
            kit.SquadLevel = SquadLevel.Member;
            kit.CreditCost = 0;
            kit.Type = KitType.Loadout;
            kit.PremiumCost = 0m;

            dbContext.Update(kit);
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }

        ActionLog.Add(ActionLogType.UpgradeLoadout, $"ID: {loadoutInternalId} (#{kit.PrimaryKey}). Class: {oldClass} -> {@class}. Old Faction: {oldFaction?.InternalName ?? "none"}", adminInstigator);

        if (!await Manager.HasAccess(kit, player, CancellationToken.None))
        {
            await Manager.GiveAccess(kit, player, KitAccessType.Purchase, CancellationToken.None).ConfigureAwait(false);
            // todo KitSync.OnAccessChanged(player.m_SteamID);
            ActionLog.Add(ActionLogType.ChangeKitAccess, player.m_SteamID.ToString(CultureInfo.InvariantCulture) + " GIVEN ACCESS TO " + loadoutInternalId + ", REASON: " + KitAccessType.Purchase, adminInstigator);
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);

        WarfarePlayer? pl = _playerService.GetOnlinePlayerOrNull(player);
        if (pl != null)
            Manager.Signs.UpdateSigns(pl);

        await Manager.Distribution.DequipKit(kit, true, CancellationToken.None).ConfigureAwait(false);

        return kit;
    }

    /// <summary>
    /// Enables a loadout, usually after upgrading a kit.
    /// </summary>
    /// <remarks>
    /// The kit's <see cref="Kit.Disabled"/> property will be set to <see langword="false"/>.
    /// The kit can be re-locked later using <see cref="LockLoadout"/>.
    /// </remarks>
    /// <param name="adminInstigator">The admin that handled the ticket.</param>
    /// <param name="loadoutInternalId">The internal name of the kit, ex. '76500000000000000_a'.</param>
    /// <returns>The unlocked kit.</returns>
    /// <exception cref="KitNotFoundException"/>
    /// <exception cref="InvalidOperationException">Kit is already unlocked.</exception>
    public async Task<Kit> UnlockLoadout(CSteamID adminInstigator, string loadoutInternalId, CancellationToken token = default)
    {
        Kit? existing;
        CSteamID player;

        using (IServiceScope scope = serviceProvider.CreateScope())
        await using (IKitsDbContext dbContext = scope.ServiceProvider.GetRequiredService<IKitsDbContext>())
        {
            existing = await Manager.FindKit(dbContext, loadoutInternalId, token, true, KitManager.FullSet).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KitNotFoundException(loadoutInternalId);
            }

            if (!existing.Disabled)
            {
                throw new InvalidOperationException("Kit is already unlocked.");
            }

            ActionLog.Add(ActionLogType.UnlockLoadout, loadoutInternalId, adminInstigator);

            existing.UpdateLastEdited(adminInstigator);
            existing.Disabled = false;
            existing.MarkRemoteItemsDirty();
            existing.MarkRemoteUnlockRequirementsDirty();
            if (string.IsNullOrEmpty(existing.WeaponText))
                existing.WeaponText = Manager.GetWeaponText(existing);

            LoadoutIdHelper.Parse(existing.InternalName, out player);

            dbContext.Update(existing);
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);

        WarfarePlayer? onlinePlayer = _playerService.GetOnlinePlayerOrNull(player);
        if (onlinePlayer != null)
        {
            Manager.Signs.UpdateSigns(onlinePlayer);
            _chatService.Send(onlinePlayer, _kitTranslations.DMLoadoutUnlocked);
        }

        await Manager.Distribution.DequipKit(existing, true, CancellationToken.None).ConfigureAwait(false);
        return existing;
    }

    /// <summary>
    /// Disables a loadout, usually after upgrading a kit.
    /// </summary>
    /// <remarks>
    /// The kit's <see cref="Kit.Disabled"/> property will be set to <see langword="true"/>, and the sign will change to 'being set up by admin', etc.
    /// The kit should be 'unlocked' when it's done using <see cref="UnlockLoadout"/>.
    /// </remarks>
    /// <param name="adminInstigator">The admin that handled the ticket.</param>
    /// <param name="loadoutInternalId">The internal name of the kit, ex. '76500000000000000_a'.</param>
    /// <returns>The unlocked kit.</returns>
    /// <exception cref="KitNotFoundException"/>
    /// <exception cref="InvalidOperationException">Kit is already locked.</exception>
    public async Task<Kit> LockLoadout(CSteamID adminInstigator, string loadoutInternalId, CancellationToken token = default)
    {
        Kit? existing;
        CSteamID player;

        using (IServiceScope scope = serviceProvider.CreateScope())
        await using (IKitsDbContext dbContext = scope.ServiceProvider.GetRequiredService<IKitsDbContext>())
        {
            existing = await Manager.FindKit(dbContext, loadoutInternalId, token, true, KitManager.FullSet).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KitNotFoundException(loadoutInternalId);
            }

            if (existing.Disabled)
            {
                throw new InvalidOperationException("Kit is already locked.");
            }

            ActionLog.Add(ActionLogType.UnlockLoadout, loadoutInternalId, adminInstigator);

            existing.UpdateLastEdited(adminInstigator);
            existing.Disabled = true;

            LoadoutIdHelper.Parse(existing.InternalName, out player);

            dbContext.Update(existing);
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);

        WarfarePlayer? onlinePlayer = _playerService.GetOnlinePlayerOrNull(player);
        if (onlinePlayer != null)
        {
            Manager.Signs.UpdateSigns(onlinePlayer);
            _chatService.Send(onlinePlayer, _kitTranslations.DMLoadoutUnlocked);
        }

        await Manager.Distribution.DequipKit(existing, true, CancellationToken.None).ConfigureAwait(false);
        return existing;
    }

    /// <summary>
    /// Creates a new loadout with a given class and name.
    /// </summary>
    /// <remarks>
    /// The kit's <see cref="Kit.Disabled"/> property will be set to <see langword="true"/>, and the sign will show 'being set up by admin'.
    /// The kit should be 'unlocked' when it's done using <see cref="UnlockLoadout"/>.
    /// </remarks>
    /// <param name="adminInstigator">The admin that handled the ticket.</param>
    public async Task<Kit> CreateLoadout(CSteamID adminInstigator, CSteamID forPlayer, Class @class, string? displayName, CancellationToken token = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        await using IKitsDbContext dbContext = scope.ServiceProvider.GetRequiredService<IKitsDbContext>();
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        return await CreateLoadout(dbContext, adminInstigator, forPlayer, @class, displayName, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new loadout with a given class and name.
    /// </summary>
    /// <remarks>
    /// The kit's <see cref="Kit.Disabled"/> property will be set to <see langword="true"/>, and the sign will show 'being set up by admin'.
    /// The kit should be 'unlocked' when it's done using <see cref="UnlockLoadout"/>.
    /// </remarks>
    /// <param name="adminInstigator">The admin that handled the ticket.</param>
    public async Task<Kit> CreateLoadout(IKitsDbContext dbContext, CSteamID adminInstigator, CSteamID forPlayer, Class @class, string? displayName, CancellationToken token = default)
    {
        await _loadoutCreationSemaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            string loadoutName = await GetFreeLoadoutName(dbContext, forPlayer, token, doLock: false).ConfigureAwait(false);

            IKitItem[] items = KitDefaults.GetDefaultLoadoutItems(@class);
            Kit kit = new Kit(loadoutName, @class, KitDefaults.GetDefaultBranch(@class), KitType.Loadout, SquadLevel.Member, null)
            {
                Creator = adminInstigator.m_SteamID,
                WeaponText = string.Empty,
                Disabled = true
            };

            kit.SetItemArray(items, dbContext);
            kit.SetSignText(dbContext, adminInstigator, displayName ?? ("Loadout " + LoadoutIdHelper.GetLoadoutLetter(LoadoutIdHelper.ParseNumber(loadoutName))), _languageService.GetDefaultLanguage());
            dbContext.Add(kit);
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

            ActionLog.Add(ActionLogType.CreateKit, loadoutName, adminInstigator);

            await Manager.GiveAccess(kit, forPlayer, KitAccessType.Purchase, CancellationToken.None).ConfigureAwait(false);

            ActionLog.Add(ActionLogType.ChangeKitAccess, forPlayer.m_SteamID.ToString(CultureInfo.InvariantCulture) + " GIVEN ACCESS TO " + loadoutName + ", REASON: " + KitAccessType.Purchase, adminInstigator);

            // todo KitSync.OnAccessChanged(forPlayer.m_SteamID);

            await UniTask.SwitchToMainThread(CancellationToken.None);

            WarfarePlayer? onlinePlayer = _playerService.GetOnlinePlayerOrNull(adminInstigator);
            if (onlinePlayer != null)
            {
                Manager.Signs.UpdateSigns(onlinePlayer);
            }

            return kit;
        }
        finally
        {
            _loadoutCreationSemaphore.Release();
        }
    }
}