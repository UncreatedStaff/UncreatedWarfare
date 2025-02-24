using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Kits;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits.Requests;

public class KitRequestService : IRequestHandler<KitSignInstanceProvider, Kit>, IRequestHandler<Kit, Kit>, IDisposable
{
    public const string DefaultKitId = "default";

    private readonly IKitDataStore _kitDataStore;
    private readonly ITranslationValueFormatter _valueFormatter;
    private readonly LoadoutService _loadoutService;
    private readonly MapScheduler _mapScheduler;
    private readonly CooldownManager _cooldownManager;
    private readonly PlayerNitroBoostService _boostService;
    private readonly IKitAccessService _kitAccessService;
    private readonly KitBestowService _kitBestowService;
    private readonly IKitsDbContext _kitDbContext;
    private readonly EventDispatcher _eventDispatcher;
    private readonly DroppedItemTracker _droppedItemTracker;
    private readonly AssetRedirectService _assetRedirectService;
    private readonly PointsService _pointsService;
    private readonly RequestKitsTranslations _kitReqTranslations;
    private readonly IPlayerService _playerService;
    private readonly ChatService _chatService;

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public KitRequestService(
        IKitDataStore kitDataStore,
        ITranslationValueFormatter valueFormatter,
        IPlayerService playerService,
        LoadoutService loadoutService,
        TranslationInjection<RequestKitsTranslations> translations,
        MapScheduler mapScheduler,
        CooldownManager cooldownManager,
        PlayerNitroBoostService boostService,
        IKitAccessService kitAccessService,
        KitBestowService kitBestowService,
        IKitsDbContext kitDbContext,
        EventDispatcher eventDispatcher,
        DroppedItemTracker droppedItemTracker,
        AssetRedirectService assetRedirectService,
        PointsService pointsService,
        ChatService chatService)
    {
        _kitDataStore = kitDataStore;
        _loadoutService = loadoutService;
        _playerService = playerService;
        _mapScheduler = mapScheduler;
        _cooldownManager = cooldownManager;
        _boostService = boostService;
        _kitAccessService = kitAccessService;
        _kitBestowService = kitBestowService;
        _kitDbContext = kitDbContext;
        _kitDbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _eventDispatcher = eventDispatcher;
        _droppedItemTracker = droppedItemTracker;
        _assetRedirectService = assetRedirectService;
        _pointsService = pointsService;
        _valueFormatter = valueFormatter;
        _kitReqTranslations = translations.Value;
        _chatService = chatService;
    }

    /// <inheritdoc />
    public async Task<bool> RequestAsync(WarfarePlayer player, [NotNullWhen(true)] KitSignInstanceProvider? sign, IRequestResultHandler resultHandler, CancellationToken token = default)
    {
        if (sign == null)
        {
            resultHandler.NotFoundOrRegistered(player);
            return false;
        }

        if (sign.LoadoutNumber < 0)
        {
            Kit? foundKit = await _kitDataStore.QueryKitAsync(sign.KitId, KitInclude.Verifiable | KitInclude.Giveable, token).ConfigureAwait(false);
            return await RequestAsync(player, foundKit, resultHandler, token).ConfigureAwait(false);
        }

        Kit? loadout = await _loadoutService.GetLoadoutFromNumber(player.Steam64, sign.LoadoutNumber, KitInclude.Verifiable | KitInclude.Giveable, token).ConfigureAwait(false);
        return await RequestAsync(player, loadout, resultHandler, token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RequestAsync(WarfarePlayer player, [NotNullWhen(true)] Kit? kit, IRequestResultHandler resultHandler, CancellationToken token = default)
    {
        if (kit == null)
        {
            resultHandler.NotFoundOrRegistered(player);
            return false;
        }

        // one player can request a kit at a time.
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await UniTask.SwitchToMainThread(token);

            Team team = player.Team;

            // already requested
            uint? activeKit = player.Component<KitPlayerComponent>().ActiveKitKey;
            if (activeKit.HasValue && activeKit.Value == kit.Key)
            {
                resultHandler.MissingRequirement(player, kit, _kitReqTranslations.AlreadyEquipped.Translate(player));
                return false;
            }

            if (kit.Type == KitType.Loadout && kit.Season < WarfareModule.Season)
            {
                resultHandler.MissingRequirement(player, kit, _kitReqTranslations.NeedsUpgrade.Translate(player));
                return false;
            }

            if (kit is { Type: KitType.Loadout, IsLocked: true })
            {
                resultHandler.MissingRequirement(player, kit, _kitReqTranslations.NeedsSetup.Translate(player));
                return false;
            }

            // outdated kit
            if (kit.IsLocked || kit.Season != WarfareModule.Season && kit.Season > 0)
            {
                resultHandler.MissingRequirement(player, kit, _kitReqTranslations.KitDisabled.Translate(player));
                return false;
            }

            if (!IsCurrentMapAllowed(kit))
            {
                resultHandler.MissingRequirement(player, kit, _kitReqTranslations.KitMapNotAllowed.Translate(player));
                return false;
            }

            if (!IsCurrentFactionAllowed(kit, team))
            {
                resultHandler.MissingRequirement(player, kit, _kitReqTranslations.KitTeamNotAllowed.Translate(player));
                return false;
            }

            // kit is purchasable and player does not own yet
            if (kit is { Type: KitType.Public, CreditCost: > 0 } && !player.Component<KitPlayerComponent>().IsKitAccessible(kit.Key))
            {
                // check enough credits
                if (player.CachedPoints.Credits < kit.CreditCost)
                {
                    resultHandler.MissingCreditsOwnership(player, kit, kit.CreditCost);
                    return false;
                }
                
                // confirm purchase kit modal
                ToastMessage message = ToastMessage.Popup(
                    _kitReqTranslations.ModalConfirmPurchaseKitHeading.Translate(player),
                    _kitReqTranslations.ModalConfirmPurchaseKitDescription.Translate(kit, kit.CreditCost, player),
                    _kitReqTranslations.ModalConfirmPurchaseKitAcceptButton.Translate(player),
                    _kitReqTranslations.ModalConfirmPurchaseKitCancelButton.Translate(player),
                    callbacks: new PopupCallbacks((WarfarePlayer warfarePlayer, int button,
                        in ToastMessage toastMessage, ref bool consume, ref bool window) =>
                    {
                        // purchase kit
                        _kitAccessService.UpdateAccessAsync(player.Steam64, kit.Key, KitAccessType.Purchase, CSteamID.Nil, player.DisconnectToken);
                        _pointsService.ApplyEvent(player, new ResolvedEventInfo(default, null, -kit.CreditCost, null), player.DisconnectToken);

                        // "cash register" sound effect
                        IAssetLink<EffectAsset> purchaseEffect = AssetLink.Create<EffectAsset>("5e2a0073025849d39322932d88609777");
                        EffectUtility.TriggerEffect(purchaseEffect, EffectManager.SMALL, player.Position, true);
                        
                        _chatService.Send(player, _kitReqTranslations.KitPurchaseSuccess, kit, kit.CreditCost);
                    }, null)
                );
                

                player.SendToast(message);
                return false;
            }

            // elite access
            if (kit is { Type: not KitType.Public, RequiresServerBoost: false } && !player.Component<KitPlayerComponent>().IsKitAccessible(kit.Key))
            {
                if (kit.PremiumCost > 0)
                    resultHandler.MissingDonorOwnership(player, kit, kit.PremiumCost);
                else
                    resultHandler.MissingExclusiveOwnership(player, kit);
                return false;
            }

            // team limits
            if (kit.Type != KitType.Loadout
                    ? IsLimited(kit, out _, out int allowedPlayers, team)
                    : IsClassLimited(kit, out _, out allowedPlayers, team))
            {
                resultHandler.MissingRequirement(player, kit,
                    kit.Type != KitType.Loadout
                        ? _kitReqTranslations.RequestKitLimited.Translate(allowedPlayers, player)
                        : _kitReqTranslations.RequestKitClassLimited.Translate(allowedPlayers, kit.Class, player)
                );
                return false;
            }

            // cooldowns
            if (_cooldownManager.HasCooldown(player, KnownCooldowns.RequestKit, out Cooldown requestCooldown) && kit.Class is not Class.Crewman and not Class.Pilot)
            {
                resultHandler.MissingRequirement(player, kit, _kitReqTranslations.OnGlobalCooldown.Translate(requestCooldown, player));
                return false;
            }

            if (kit is { IsPaid: true, RequestCooldown.Ticks: > 0 } && _cooldownManager.HasCooldown(player, KnownCooldowns.RequestPremiumKit, out Cooldown premiumCooldown, kit.Id))
            {
                resultHandler.MissingRequirement(player, kit, _kitReqTranslations.OnCooldown.Translate(premiumCooldown, player));
                return false;
            }

            // unlock requirements
            if (kit.UnlockRequirements != null)
            {
                for (int i = 0; i < kit.UnlockRequirements.Length; i++)
                {
                    UnlockRequirement req = kit.UnlockRequirements[i];
                    await UniTask.SwitchToMainThread(token);
                    if (req == null || await req.CanAccessAsync(player, token))
                        continue;

                    await UniTask.SwitchToMainThread(token);
                    resultHandler.MissingUnlockRequirement(player, kit, req);
                    return false;
                }

                await UniTask.SwitchToMainThread(token);
            }

            if (kit is not { CreditCost: 0, Type: KitType.Public })
            {
                // double check access against database
                bool hasAccess = await _kitAccessService.HasAccessAsync(player.Steam64, kit.Key, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                if (!hasAccess)
                {
                    // check nitro boost status
                    if (kit.RequiresServerBoost)
                    {
                        bool nitroBoosting;
                        try
                        {
                            nitroBoosting = await _boostService.IsBoosting(player.Steam64, true, token).ConfigureAwait(false);
                            await UniTask.SwitchToMainThread(token);
                        }
                        catch
                        {
                            nitroBoosting = player.Save.WasNitroBoosting;
                        }

                        if (!nitroBoosting)
                        {
                            resultHandler.MissingRequirement(player, kit, _kitReqTranslations.RequiresNitroBoost.Translate(requestCooldown, player));
                            return false;
                        }
                    }
                    else if (kit.IsPaid)
                    {
                        if (kit.PremiumCost > 0)
                            resultHandler.MissingDonorOwnership(player, kit, kit.PremiumCost);
                        else
                            resultHandler.MissingExclusiveOwnership(player, kit);
                        return false;
                    }
                    else
                    {
                        resultHandler.MissingCreditsOwnership(player, kit, kit.CreditCost);
                        return false;
                    }
                }
            }

            if (!player.IsOnline)
            {
                throw new OperationCanceledException("Player disconnected before kit could be granted.");
            }

            if (team != player.Team)
            {
                throw new OperationCanceledException("Changed teams before kit could be granted.");
            }

            // recheck limits to make sure people can't request at the same time to avoid limits.
            if (kit.Type != KitType.Loadout
                    ? IsLimited(kit, out _, out allowedPlayers, team)
                    : IsClassLimited(kit, out _, out allowedPlayers, team))
            {
                resultHandler.MissingRequirement(player, kit,
                    kit.Type != KitType.Loadout
                        ? _kitReqTranslations.RequestKitLimited.Translate(allowedPlayers, player)
                        : _kitReqTranslations.RequestKitClassLimited.Translate(allowedPlayers, kit.Class, player)
                );
                return false;
            }

            if (kit.Type == KitType.Loadout && LoadoutIdHelper.Parse(kit.Id, out CSteamID intendedPlayer) > 0 && intendedPlayer.m_SteamID == player.Steam64.m_SteamID)
            {
                ActionLog.Add(ActionLogType.RequestKit, $"Loadout {LoadoutIdHelper.GetLoadoutLetter(LoadoutIdHelper.Parse(kit.Id))}: {kit.Id}, Team {team}, Class: {_valueFormatter.FormatEnum(kit.Class, null)}", player.Steam64);
            }
            else
            {
                ActionLog.Add(ActionLogType.RequestKit, $"Kit {kit.Id}, Team {player.Team.Faction.Name}, Class: {_valueFormatter.FormatEnum(kit.Class, null)}", player.Steam64);
            }

            // todo: make a tweak for KitRequests.CheckOrCreateSquadForSquadleaderKit

            await GrantKitRequest(player, kit, resultHandler, token).ConfigureAwait(false);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> GiveUnarmedKitAsync(WarfarePlayer player, bool silent = false, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await GiveUnarmedKitAsyncIntl(player, silent, token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> GiveAvailableFreeKitAsync(WarfarePlayer player, bool silent = false, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            uint factionId = player.Team.Faction.PrimaryKey;
            if (factionId == 0)
            {
                Kit? defaultKit = await _kitDataStore.QueryKitAsync(DefaultKitId, KitInclude.Giveable, token);
                if (defaultKit != null)
                {
                    await GiveKitIntlAsync(player, new KitBestowData(defaultKit) { Silent = silent }, false, token).ConfigureAwait(false);
                    return true;
                }
            }

            ulong steam64 = player.Steam64.m_SteamID;
            List<uint> kits = await _kitDataStore.QueryListAsync(kits => kits
                .Where(x => x.Type == KitType.Public
                            && x.Season == WarfareModule.Season
                            && x.PremiumCost == 0
                            && !x.RequiresNitro
                            && x.Delays.Count == 0
                            && x.SquadLevel == SquadLevel.Member
                            && !x.Disabled
                            && x.FactionId == factionId
                            && (x.CreditCost == 0 || x.Access.Any(a => a.Steam64 == steam64))).Select(x => x.PrimaryKey),
                token: token
            ).ConfigureAwait(false);


            Kit? kit = null;
            if (kits.Count != 0)
            {
                kit = await _kitDataStore.QueryKitAsync(kits[kits.GetRandomIndex()], KitInclude.Giveable, token).ConfigureAwait(false);
            }

            if (kit != null)
            {
                await GiveKitIntlAsync(player, new KitBestowData(kit) { Silent = silent }, false, token).ConfigureAwait(false);
                return true;
            }
            else
            {
                return await GiveUnarmedKitAsyncIntl(player, silent, token).ConfigureAwait(false);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<bool> GiveUnarmedKitAsyncIntl(WarfarePlayer player, bool silent = false, CancellationToken token = default)
    {
        Kit? kit = null;
        uint unarmedKit = player.Team.Faction.UnarmedKit.GetValueOrDefault();
        if (unarmedKit != 0)
            kit = await _kitDataStore.QueryKitAsync(unarmedKit, KitInclude.Giveable, token).ConfigureAwait(false);

        kit ??= await _kitDataStore.QueryKitAsync(DefaultKitId, KitInclude.Giveable, token).ConfigureAwait(false);

        if (!player.IsOnline)
            return false;

        KitPlayerComponent comp = player.Component<KitPlayerComponent>();
        if (kit != null)
        {
            if (comp.ActiveKitKey != kit.Key)
                await GiveKitIntlAsync(player, new KitBestowData(kit) { Silent = silent }, false, token).ConfigureAwait(false);
            return true;
        }

        if (comp.ActiveKitKey.HasValue)
            await RemoveKitAsync(player, token);
        return false;
    }

    public async Task GiveKitAsync(WarfarePlayer player, KitBestowData kitBestowData, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await GiveKitIntlAsync(player, kitBestowData, false, token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async UniTask RemoveKitAsync(WarfarePlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        // update hotkey list
        player.Component<HotkeyPlayerComponent>().HotkeyBindings = null;

        // clear hotkeys, todo: see if needed
        // for (int i = 0; i <= 9; ++i)
        //     player.UnturnedPlayer.equipment.ServerClearItemHotkey(KitItemUtility.GetHotkeyIndex((byte)i));

        _kitBestowService.BestowEmptyKit(player);

        await _eventDispatcher.DispatchEventAsync(
            new PlayerKitChanged
            {
                Player = player,
                Class = Class.None,
                Kit = null,
                KitId = 0,
                KitName = null,
                WasRequested = false
            }, CancellationToken.None);
    }

    private async Task GrantKitRequest(WarfarePlayer player, Kit kit, IRequestResultHandler resultHandler, CancellationToken token)
    {
        // assumes _semaphore is locked

        await UniTask.SwitchToMainThread(token);

        await _droppedItemTracker.DestroyItemsDroppedByPlayerAsync(player.Steam64, false, token);
        
        await GiveKitIntlAsync(player, new KitBestowData(kit) { IsLowAmmo = !player.Save.IsFirstLife }, true, token).ConfigureAwait(false);

        resultHandler.Success(player, kit);

        if (kit is { IsPaid: true, RequestCooldown.Ticks: > 0 })
        {
            _cooldownManager.StartCooldown(player, KnownCooldowns.RequestPremiumKit, kit.RequestCooldown, kit.Id);
        }

        _cooldownManager.StartCooldown(player, KnownCooldowns.RequestKit);
    }

    private async Task GiveKitIntlAsync(WarfarePlayer player, KitBestowData kitBestowData, bool isRequest, CancellationToken token = default)
    {
        // assumes _semaphore is locked

        ulong steam64 = player.Steam64.m_SteamID;
        uint kitId = kitBestowData.Kit.Key;

        List<KitLayoutTransformation> layouts = await _kitDbContext.KitLayoutTransformations
            .Where(x => x.Steam64 == steam64 && x.KitId == kitId)
            .AsNoTracking()
            .ToListAsync(token)
            .ConfigureAwait(false);

        List<KitHotkey> hotkeys = await _kitDbContext.KitHotkeys
            .Where(x => x.Steam64 == steam64 && x.KitId == kitId)
            .AsNoTracking()
            .ToListAsync(token)
            .ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        // update hotkey list
        HotkeyPlayerComponent hotkeyComponent = player.Component<HotkeyPlayerComponent>();
        hotkeyComponent.HotkeyBindings = null;

        // clear hotkeys, todo: see if needed
        // for (int i = 0; i <= 9; ++i)
        //     player.UnturnedPlayer.equipment.ServerClearItemHotkey(KitItemUtility.GetHotkeyIndex((byte)i));

        _kitBestowService.BestowKit(player, kitBestowData.Copy(layouts));

        ItemTrackingPlayerComponent itemTracker = player.Component<ItemTrackingPlayerComponent>();

        hotkeyComponent.HotkeyBindings = hotkeys;
        foreach (KitHotkey hotkey in hotkeys)
        {
            byte index = KitItemUtility.GetHotkeyIndex(hotkey.Slot);
            if (index == byte.MaxValue)
                continue;

            if (!itemTracker.TryGetCurrentItemPosition(hotkey.Page, hotkey.X, hotkey.Y, out Page page, out byte x, out byte y, out bool isDropped, out Item? item))
                continue;
            
            if (isDropped)
            {
                // find suitable item after original was dropped
                hotkeyComponent.HandleItemDropped(item, hotkey.X, hotkey.Y, hotkey.Page);
                continue;
            }

            ItemAsset itemAsset = item.GetAsset();
            if (itemAsset == null)
                continue;

            if (hotkey.Item.HasValue && !hotkey.Item.Value.Equals(itemAsset))
            {
                // hotkey item mismatch (concrete)
                continue;
            }

            if (hotkey.Redirect.HasValue && _assetRedirectService.TryFindRedirectType(itemAsset, out RedirectType redirect, out _, out _) && hotkey.Redirect.Value != redirect)
            {
                // hotkey item mismatch (redirect)
                continue;
            }

            if (KitItemUtility.CanBindHotkeyTo(itemAsset, page))
            {
                player.UnturnedPlayer.equipment.ServerBindItemHotkey(index, itemAsset, (byte)page, x, y);
            }
        }

        Kit kit = kitBestowData.Kit;
        await _eventDispatcher.DispatchEventAsync(
            new PlayerKitChanged
            {
                Player = player,
                Class = kit.Class,
                Kit = kit,
                KitId = kit.Key,
                KitName = kit.Id,
                WasRequested = isRequest
            }, CancellationToken.None);
    }

    internal bool IsLimited(Kit kit, out int currentPlayers, out int allowedPlayers, Team team)
    {
        currentPlayers = 0;

        int ttl = 0;
        foreach (WarfarePlayer player in _playerService.OnlinePlayersOnTeam(team))
        {
            ++ttl;
            if (player.Component<KitPlayerComponent>().ActiveKitKey is { } pk && pk == kit.Key)
                ++currentPlayers;
        }

        allowedPlayers = Mathf.CeilToInt(kit.TeamLimit * ttl);
        if (kit.TeamLimit >= 1f)
        {
            return false;
        }

        return currentPlayers + 1 > allowedPlayers;
    }

    internal bool IsClassLimited(Kit kit, out int currentPlayers, out int allowedPlayers, Team team)
    {
        currentPlayers = 0;

        int ttl = 0;
        foreach (WarfarePlayer player in _playerService.OnlinePlayersOnTeam(team))
        {
            ++ttl;
            if (player.Component<KitPlayerComponent>().ActiveClass == kit.Class)
                ++currentPlayers;
        }

        allowedPlayers = Mathf.CeilToInt(kit.TeamLimit * ttl);
        if (kit.TeamLimit >= 1f)
        {
            return false;
        }

        return currentPlayers + 1 > allowedPlayers;
    }

    private bool IsCurrentMapAllowed(Kit kit)
    {
        if (kit.MapFilter.IsNullOrEmpty())
            return true;
        int map = _mapScheduler.Current;
        if (map != -1)
        {
            for (int i = 0; i < kit.MapFilter.Length; ++i)
            {
                if (kit.MapFilter[i] == map)
                    return kit.MapFilterIsWhitelist;
            }
        }

        return !kit.MapFilterIsWhitelist;
    }

    private static bool IsCurrentFactionAllowed(Kit kit, Team team)
    {
        if (!kit.Faction.IsDefaultFaction)
        {
            if (team.Opponents.Any(x => x.Faction.Equals(kit.Faction)))
                return false;
        }

        if (kit.FactionFilter.IsNullOrEmpty())
            return true;

        for (int i = 0; i < kit.FactionFilter.Length; ++i)
        {
            if (kit.FactionFilter[i].Equals(team.Faction))
                return kit.FactionFilterIsWhitelist;
        }

        return !kit.FactionFilterIsWhitelist;
    }

    void IDisposable.Dispose()
    {
        _semaphore.Dispose();
    }
}