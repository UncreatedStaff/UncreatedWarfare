using Microsoft.EntityFrameworkCore;
using System;
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
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Squads.UI;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Kits.Requests;

public class KitRequestService : IRequestHandler<KitSignInstanceProvider, Kit>, IRequestHandler<Kit, Kit>, IDisposable
{
    public const string DefaultKitId = "default";

    private readonly IKitDataStore _kitDataStore;
    private readonly ITranslationValueFormatter _valueFormatter;
    private readonly LoadoutService _loadoutService;
    private readonly CooldownManager _cooldownManager;
    private readonly IKitAccessService _kitAccessService;
    private readonly KitBestowService _kitBestowService;
    private readonly IKitsDbContext _kitDbContext;
    private readonly IKitItemResolver _kitItemResolver;
    private readonly EventDispatcher _eventDispatcher;
    private readonly DroppedItemTracker _droppedItemTracker;
    private readonly AssetRedirectService _assetRedirectService;
    private readonly PointsService _pointsService;
    private readonly SquadMenuUI _squadMenuUI;
    private readonly RequestKitsTranslations _kitReqTranslations;
    private readonly ChatService _chatService;
    private readonly ILogger<KitRequestService> _logger;
    private readonly KitRequirementManager _kitRequirements;
    private readonly ZoneStore _zoneStore;

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private readonly KitRequirementVisitor _requestRequirementVisitor;

    public KitRequestService(
        IKitDataStore kitDataStore,
        ITranslationValueFormatter valueFormatter,
        LoadoutService loadoutService,
        TranslationInjection<RequestKitsTranslations> translations,
        CooldownManager cooldownManager,
        IKitAccessService kitAccessService,
        KitBestowService kitBestowService,
        IKitsDbContext kitDbContext,
        IKitItemResolver kitItemResolver,
        EventDispatcher eventDispatcher,
        DroppedItemTracker droppedItemTracker,
        AssetRedirectService assetRedirectService,
        PointsService pointsService,
        SquadMenuUI squadMenuUI,
        ChatService chatService,
        ZoneStore zoneStore,
        ILogger<KitRequestService> logger,
        KitRequirementManager kitRequirements)
    {
        _kitDataStore = kitDataStore;
        _loadoutService = loadoutService;
        _cooldownManager = cooldownManager;
        _kitAccessService = kitAccessService;
        _kitBestowService = kitBestowService;
        _kitDbContext = kitDbContext;
        _kitItemResolver = kitItemResolver;
        _kitDbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _eventDispatcher = eventDispatcher;
        _droppedItemTracker = droppedItemTracker;
        _assetRedirectService = assetRedirectService;
        _pointsService = pointsService;
        _squadMenuUI = squadMenuUI;
        _valueFormatter = valueFormatter;
        _kitReqTranslations = translations.Value;
        _chatService = chatService;
        _zoneStore = zoneStore;
        _logger = logger;
        _kitRequirements = kitRequirements;

        _requestRequirementVisitor = new KitRequirementVisitor(this);
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

    private struct RequestState
    {
        public IRequestResultHandler Handler;
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
            Kit? activeKit = await player.Component<KitPlayerComponent>().GetActiveKitAsync(KitInclude.Default, token).ConfigureAwait(false);

            await UniTask.SwitchToMainThread(token);

            Team team = player.Team;

            // already requested
            if (activeKit != null && activeKit.Key == kit.Key)
            {
                resultHandler.MissingRequirement(player, kit, _kitReqTranslations.AlreadyEquipped.Translate(player));
                return false;
            }

            KitPlayerComponent component = player.Component<KitPlayerComponent>();

            RequestState state;
            state.Handler = resultHandler;

            KitRequirementResolutionContext<RequestState> context = new KitRequirementResolutionContext<RequestState>(player, team, kit, activeKit, component, state);

            foreach (IKitRequirement requirement in _kitRequirements.Request)
            {
                KitRequirementResult result = await requirement.AcceptAsync(_requestRequirementVisitor, in context, token);
                if (result == KitRequirementResult.No)
                    return false;
            }

            if (!player.IsOnline)
            {
                throw new OperationCanceledException("Player disconnected before kit could be granted.");
            }

            if (team != player.Team)
            {
                throw new OperationCanceledException("Changed teams before kit could be granted.");
            }

            if (kit.Type == KitType.Loadout && LoadoutIdHelper.Parse(kit.Id, out CSteamID intendedPlayer) > 0 && intendedPlayer.m_SteamID == player.Steam64.m_SteamID)
            {
                // todo: ActionLog.Add(ActionLogType.RequestKit, $"Loadout {LoadoutIdHelper.GetLoadoutLetter(LoadoutIdHelper.Parse(kit.Id))}: {kit.Id}, Team {team}, Class: {_valueFormatter.FormatEnum(kit.Class, null)}", player.Steam64);
            }
            else
            {
                // todo: ActionLog.Add(ActionLogType.RequestKit, $"Kit {kit.Id}, Team {player.Team.Faction.Name}, Class: {_valueFormatter.FormatEnum(kit.Class, null)}", player.Steam64);
            }

            await GrantKitRequest(player, kit, resultHandler, token).ConfigureAwait(false);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Purchase a kit for a player.
    /// </summary>
    /// <param name="target">Where to play the purchase SFX. If <see langword="null"/> the effect will be played at the player's position.</param>
    /// <returns><see langword="false"/> if the kit's credit cost is 0 or if the kit can't be found, otherwise <see langword="true"/>.</returns>
    public async Task<bool> BuyKitAsync(WarfarePlayer player, Kit kit, Vector3? target = null, CancellationToken token = default)
    {
        if (kit.CreditCost <= 0)
            return false;

        using CombinedTokenSources srcComb = token.CombineTokensIfNeeded(player.DisconnectToken);

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (await _kitAccessService.GetAccessAsync(player.Steam64, kit.Key, token) != null)
            {
                return false;
            }

            // give access to the kit
            if (!await _kitAccessService.UpdateAccessAsync(player.Steam64, kit.Key, KitAccessType.Credits, CSteamID.Nil, token))
            {
                return false;
            }
            
            try
            {
                // purchase the kit
                await _pointsService.ApplyEvent(player, _pointsService.GetPurchaseEvent(player, kit.CreditCost), token);
            }
            catch
            {
                // if purchase somehow failed, remove access before rethrowing
                try
                {
                    await _kitAccessService.UpdateAccessAsync(player.Steam64, kit.Key, null, CSteamID.Nil, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to remove access after removing credits failed.");
                }
                throw;
            }

            await UniTask.SwitchToMainThread(CancellationToken.None);

            if (!player.IsOnline)
                return true;

            // "cash register" sound effect
            IAssetLink<EffectAsset> purchaseEffect = AssetLink.Create<EffectAsset>("5e2a0073025849d39322932d88609777");
            EffectUtility.TriggerEffect(purchaseEffect, EffectManager.SMALL, target ?? player.Position, true);

            _chatService.Send(player, _kitReqTranslations.KitPurchaseSuccess, kit, kit.CreditCost);
        }
        finally
        {
            _semaphore.Release();
        }

        return true;
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

    public async Task<bool> GiveAvailableFreeKitAsync(WarfarePlayer player, bool silent = false, bool isLowAmmo = false, CancellationToken token = default)
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
                    await GiveKitIntlAsync(player, new KitBestowData(defaultKit) { Silent = silent, IsLowAmmo = isLowAmmo }, false, token).ConfigureAwait(false);
                    return true;
                }
            }

            ulong steam64 = player.Steam64.m_SteamID;
            List<uint> kits = await _kitDataStore.QueryListAsync(kits => kits
                .OrderByDescending(x => x.Class == Class.Rifleman)
                .Where(x => x.Type == KitType.Public
                            && x.Season == WarfareModule.Season
                            && x.PremiumCost == 0
                            && !x.RequiresNitro
                            && x.Delays.Count == 0
                            && x.SquadLevel == SquadLevel.Member
                            && !x.Disabled
                            && x.Class != Class.Unarmed
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
                await GiveKitIntlAsync(player, new KitBestowData(kit) { Silent = silent, IsLowAmmo = isLowAmmo }, false, token).ConfigureAwait(false);
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

    public async Task RestockKitAsync(WarfarePlayer player, bool resupplyAmmoBags = false, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            KitPlayerComponent kitComp = player.Component<KitPlayerComponent>();
            if (!kitComp.HasKit)
            {
                await GiveUnarmedKitAsyncIntl(player, true, token).ConfigureAwait(false);
                return;
            }

            Kit? kit = await kitComp.GetActiveKitAsync(KitInclude.Giveable, token);
            await UniTask.SwitchToMainThread(token);
            kitComp.CachedKit = kit;
            if (kit != null && resupplyAmmoBags && NeedsToFullRestock(player, kit))
            {
                await GiveKitIntlAsync(player, new KitBestowData(kit) { Silent = true }, false, token);
                return;
            }

            _kitBestowService.RestockKit(player, resupplyAmmoBags);

            ApplyHotkeys(player);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private bool NeedsToFullRestock(WarfarePlayer player, Kit kit)
    {
        if (player.Component<KitPlayerComponent>().HasLowAmmo)
            return true;

        // check if any clothes are missing or the wrong item
        IKitItem[] items = kit.Items;
        foreach (IKitItem item in items)
        {
            if (item is not IClothingItem cl)
                continue;

            ClothingItem clothingItem = new ClothingItem(player.UnturnedPlayer.clothing, cl.ClothingType);
            if (!clothingItem.HasStorage)
                continue;

            if (cl is IConcreteItem concrete)
            {
                if (!concrete.Item.MatchAsset(clothingItem.Asset))
                    return true;
            }
            else
            {
                KitItemResolutionResult result = _kitItemResolver.ResolveKitItem(item, kit, player.Team);
                if (result.Asset != null && (clothingItem.Asset == null || clothingItem.Asset.GUID != result.Asset.GUID))
                    return true;
            }
        }

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
        
        await GiveKitIntlAsync(player, new KitBestowData(kit) { IsLowAmmo = _zoneStore.IsInWarRoom(player) && !player.Save.IsFirstLife }, true, token).ConfigureAwait(false);

        resultHandler.Success(player, kit);

        if (player.IsOnDuty)
            return;

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
        GiveKitMainThread(player, kitBestowData, layouts, hotkeys, false);

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


    /// <summary>
    /// Used to give all players the unarmed kit when the game is over. It doesn't check layouts or hotkeys.
    /// </summary>
    public void GiveKitMainThread(WarfarePlayer player, KitBestowData kitBestowData)
    {
        GiveKitMainThread(player, kitBestowData, null, null, false);
    }

    private void GiveKitMainThread(WarfarePlayer player, KitBestowData kitBestowData, List<KitLayoutTransformation>? layouts, List<KitHotkey>? hotkeys, bool invokeEvent)
    {
        if (!player.IsOnline)
            throw new OperationCanceledException("Player disconnected.");

        Kit kit = kitBestowData.Kit;
        HotkeyPlayerComponent hotkeyComponent = player.Component<HotkeyPlayerComponent>();
        hotkeyComponent.HotkeyBindings = null;

        _kitBestowService.BestowKit(player, layouts == null ? kitBestowData : kitBestowData.Copy(layouts));

        hotkeyComponent.HotkeyBindings = hotkeys;
        ApplyHotkeys(player);

        if (!invokeEvent)
        {
            _ = _eventDispatcher.DispatchEventAsync(
                new PlayerKitChanged
                {
                    Player = player,
                    Class = kit.Class,
                    Kit = kit,
                    KitId = kit.Key,
                    KitName = kit.Id,
                    WasRequested = false
                }, CancellationToken.None);
        }
    }

    private void ApplyHotkeys(WarfarePlayer player)
    {
        HotkeyPlayerComponent hotkeyComponent = player.Component<HotkeyPlayerComponent>();

        if (hotkeyComponent.HotkeyBindings == null)
            return;

        ItemTrackingPlayerComponent itemTracker = player.Component<ItemTrackingPlayerComponent>();
        foreach (KitHotkey hotkey in hotkeyComponent.HotkeyBindings)
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
    }

    void IDisposable.Dispose()
    {
        _semaphore.Dispose();
    }

    /// <summary>
    /// Translation layer between <see cref="IKitRequirementVisitor{TState}"/> and <see cref="IRequestResultHandler"/>.
    /// </summary>
    private class KitRequirementVisitor : IKitRequirementVisitor<RequestState>
    {
        private readonly KitRequestService _this;

        public KitRequirementVisitor(KitRequestService @this)
        {
            _this = @this;
        }

        public void AcceptGenericRequirementNotMet(in KitRequirementResolutionContext<RequestState> ctx, string message)
        {
            ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, message);
        }

        public void AcceptPremiumCostNotMet(in KitRequirementResolutionContext<RequestState> ctx, decimal cost)
        {
            ctx.State.Handler.MissingDonorOwnership(ctx.Player, ctx.Kit, cost);
        }

        public void AcceptCreditCostNotMet(in KitRequirementResolutionContext<RequestState> ctx, double cost, double current)
        {
            WarfarePlayer player = ctx.Player;

            // check enough credits
            if (current < cost)
            {
                ctx.State.Handler.MissingCreditsOwnership(player, ctx.Kit, cost);
                return;
            }

            // get the position the player is looking at to play the effect at
            Physics.Raycast(player.UnturnedPlayer.look.aim.position, player.UnturnedPlayer.look.aim.forward, out RaycastHit hit, 4f,
                RayMasks.PLAYER_INTERACT & ~RayMasks.ENEMY, QueryTriggerInteraction.Ignore);

            if (hit.transform == null || hit.transform.gameObject.layer != LayerMasks.BARRICADE)
                hit = default;

            Kit kit = ctx.Kit;
            // confirm purchase kit modal
            ToastMessage message = ToastMessage.Popup(
                _this._kitReqTranslations.ModalConfirmPurchaseKitHeading.Translate(player),
                _this._kitReqTranslations.ModalConfirmPurchaseKitDescription.Translate(kit, (int)Math.Ceiling(cost), player),
                _this._kitReqTranslations.ModalConfirmPurchaseKitAcceptButton.Translate(player),
                _this._kitReqTranslations.ModalConfirmPurchaseKitCancelButton.Translate(player),
                callbacks: new PopupCallbacks((WarfarePlayer player, int _, in ToastMessage _, ref bool _, ref bool _) =>
                {
                    _ = _this.BuyKitAsync(player, kit, hit.transform?.position, player.DisconnectToken);
                }, null)
            );


            player.SendToast(message);
        }

        public void AcceptExclusiveKitNotMet(in KitRequirementResolutionContext<RequestState> ctx)
        {
            ctx.State.Handler.MissingExclusiveOwnership(ctx.Player, ctx.Kit);
        }

        public void AcceptLoadoutLockedNotMet(in KitRequirementResolutionContext<RequestState> ctx)
        {
            ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, _this._kitReqTranslations.NeedsSetup.Translate(ctx.Player));
        }

        public void AcceptLoadoutOutOfDateNotMet(in KitRequirementResolutionContext<RequestState> ctx, int season)
        {
            ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, _this._kitReqTranslations.NeedsUpgrade.Translate(ctx.Player));
        }

        public void AcceptDisabledNotMet(in KitRequirementResolutionContext<RequestState> ctx)
        {
            ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, _this._kitReqTranslations.KitDisabled.Translate(ctx.Player));
        }

        public void AcceptNitroBoostRequirementNotMet(in KitRequirementResolutionContext<RequestState> ctx)
        {
            ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, _this._kitReqTranslations.RequiresNitroBoost.Translate(ctx.Player));
        }

        public void AcceptMapFilterNotMet(in KitRequirementResolutionContext<RequestState> ctx, string mapName)
        {
            ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, _this._kitReqTranslations.KitMapNotAllowed.Translate(ctx.Player));
        }

        public void AcceptFactionFilterNotMet(in KitRequirementResolutionContext<RequestState> ctx, FactionInfo faction)
        {
            ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, _this._kitReqTranslations.KitTeamNotAllowed.Translate(ctx.Player));
        }

        public void AcceptRequiresSquadNotMet(in KitRequirementResolutionContext<RequestState> ctx, bool needsSquadLead)
        {
            if (!ctx.Player.IsInSquad())
            {
                UniTask.Create(ctx.Player, async player =>
                {
                    await UniTask.SwitchToMainThread();
                    _this._squadMenuUI.OpenUI(player);
                });
                return;
            }

            if (needsSquadLead)
            {
                ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, _this._kitReqTranslations.RequestKitNotSquadleader.Translate(ctx.Player));
            }
        }

        public void AcceptMinRequiredSquadMembersNotMet(in KitRequirementResolutionContext<RequestState> ctx,
            WarfarePlayer? playerTakingKit, int squadMemberCount, int minimumSquadMembers)
        {
            if (playerTakingKit != null)
            {
                ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, _this._kitReqTranslations.RequestKitTakenInSquad.Translate(ctx.Player));
            }
            else
            {
                ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, _this._kitReqTranslations.RequestKitNotEnoughSquadMembers.Translate(minimumSquadMembers, ctx.Player));
            }
        }

        public void AcceptClassesAllowedPerXTeammatesRequirementNotMet(in KitRequirementResolutionContext<RequestState> ctx,
            int allowedPerXUsers, int currentUsers, int teammates, int kitsAllowed)
        {
            ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, _this._kitReqTranslations.RequestKitClassLimited.Translate(currentUsers, ctx.Kit.Class, ctx.Player));
        }

        public void AcceptGlobalCooldownNotMet(in KitRequirementResolutionContext<RequestState> ctx, in Cooldown requestCooldown)
        {
            ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, _this._kitReqTranslations.OnGlobalCooldown.Translate(requestCooldown, ctx.Player));
        }

        public void AcceptPremiumCooldownNotMet(in KitRequirementResolutionContext<RequestState> ctx, in Cooldown requestCooldown)
        {
            ctx.State.Handler.MissingRequirement(ctx.Player, ctx.Kit, _this._kitReqTranslations.OnCooldown.Translate(requestCooldown, ctx.Player));
        }

        public void AcceptKitSpecificUnlockRequirementNotMet(in KitRequirementResolutionContext<RequestState> ctx, UnlockRequirement requirement)
        {
            ctx.State.Handler.MissingUnlockRequirement(ctx.Player, ctx.Kit, requirement);
        }
    }

}