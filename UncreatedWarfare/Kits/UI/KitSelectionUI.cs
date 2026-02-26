using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Moderation.Discord;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.UI;

[UnturnedUI(BasePath = "Background")]
public sealed partial class KitSelectionUI : UnturnedUI, IEventListener<PlayerLocaleUpdated>
{
    private readonly IKitDataStore _kitDataStore;
    private readonly IKitItemResolver _kitItemResolver;
    private readonly IKitFavoriteService _kitFavoriteService;
    private readonly ItemIconProvider _iconProvider;
    private readonly KitWeaponTextService _weaponTextService;
    private readonly KitRequestService _kitRequestService;
    private readonly IKitsDbContext _kitsDbContext;
    private readonly IPlayerService _playerService;
    private readonly TranslationInjection<RequestTranslations> _requestTranslations;
    private readonly ChatService _chatService;
    private readonly ITranslationService _translationService;
    private readonly KitRequirementManager _kitRequirements;
    private readonly ITeamManager<Team> _teamManager;
    private readonly SquadManager? _squadManager;
    private readonly PlayerNitroBoostService? _nitroBoostService;
    private readonly AccountLinkingService? _acountLinkingService;
    private readonly SemaphoreSlim _dbSemaphore;
    private readonly KitSelectionUITranslations _translations;

    // maps AttachmentType -> UI array index
    private readonly int[] _attachmentMap =
    [
        1, -1,
        4, -1,
        3, -1,
        2, -1,
        0
    ];
    private readonly AttachmentType[] _inverseAttachmentMap =
    [
        AttachmentType.Magazine,
        AttachmentType.Sight,
        AttachmentType.Barrel,
        AttachmentType.Grip,
        AttachmentType.Tactical
    ];

    private Kit[]? _cachedPublicKits;
    private readonly Func<CSteamID, KitSelectionUIData> _getDataFunc;

    private readonly KitRequirementVisitor _kitRequirementVisitor;


    public KitSelectionUI(
        ILoggerFactory loggerFactory,
        AssetConfiguration assetConfig,
        IKitDataStore kitDataStore,
        IKitItemResolver kitItemResolver,
        IKitFavoriteService kitFavoriteService,
        ItemIconProvider iconProvider,
        IKitsDbContext kitsDbContext,
        IPlayerService playerService,
        TranslationInjection<KitSelectionUITranslations> translations,
        TranslationInjection<RequestTranslations> requestTranslations,
        ChatService chatService,
        ITranslationService translationService,
        KitRequirementManager kitRequirements,
        KitWeaponTextService weaponTextService,
        KitRequestService kitRequestService,
        ITeamManager<Team> teamManager,
        SquadManager? squadManager = null,
        PlayerNitroBoostService? nitroBoostService = null,
        AccountLinkingService? acountLinkingService = null)
        : base(
            loggerFactory,
            assetConfig.GetAssetLink<EffectAsset>("UI:KitSelectionUI"),
            staticKey: true
        )
    {
        _translations = translations.Value;
        _kitsDbContext = kitsDbContext;
        _playerService = playerService;
        _requestTranslations = requestTranslations;
        _chatService = chatService;
        _translationService = translationService;
        _kitRequirements = kitRequirements;
        _teamManager = teamManager;
        _squadManager = squadManager;
        _nitroBoostService = nitroBoostService;
        _weaponTextService = weaponTextService;
        _kitRequestService = kitRequestService;

        if (_nitroBoostService != null)
        {
            _nitroBoostService.OnNitroBoostStatusUpdated += HandleNitroBoostStatusUpdated;
        }

        _acountLinkingService = acountLinkingService;
        if (_acountLinkingService != null)
        {
            _acountLinkingService.OnLinkUpdated += HandleAccountLinkUpdated;
            _acountLinkingService.OnGuildStatusUpdated += HandleGuildStatusUpdated;
        }

        _dbSemaphore = new SemaphoreSlim(1, 1);

        _kitDataStore = kitDataStore;
        _kitItemResolver = kitItemResolver;
        _kitFavoriteService = kitFavoriteService;
        _kitFavoriteService.OnFavoriteStatusUpdated += HandleFavoriteStatusUpdated;
        _iconProvider = iconProvider;
        kitDataStore.KitUpdated += KitUpdated;
        kitDataStore.KitAdded += KitUpdated;
        kitDataStore.KitRemoved += KitRemoved;

        _getDataFunc = id => new KitSelectionUIData(id, this);

        _kitNameFilter.OnTextUpdated += HandleKitFilterUpdated;

        _close.OnClicked += HandleCloseUI;
        _listNextPage.OnClicked += HandleNextPage;
        _listPreviousPage.OnClicked += HandlePreviousPage;
        _listPage.OnTextUpdated += HandlePageTyped;

        _switchBackToPanel.OnClicked += (_, player) =>
        {
            KitSelectionUIData data = GetOrAddData(player.channel.owner.playerID.steamID);
            if (data.Page == KitPage.Public)
                return;

            data.Page = KitPage.Public;
            _switchToPanelLogic.Show(player);
        };

        ElementPatterns.SubscribeAll(_classButtons, HandleClassFilterChosen);
        ElementPatterns.SubscribeAll(_favoriteKits, HandleFavoriteKitClicked);
        ElementPatterns.SubscribeAll(_favoriteKits, f => f.UnfavoriteButton, HandleFavoriteKitUnfavoriteClicked);
        ElementPatterns.SubscribeAll(_favoriteKits, f => f.RequestButton, HandleFavoriteKitRequestClicked);

        foreach (KitPanel panel in _panels)
        {
            ElementPatterns.SubscribeAll(panel.Kits, k => k.FavoriteButton, HandleButtonFavoriteKitClicked);
            ElementPatterns.SubscribeAll(panel.Kits, k => k.UnfavoriteButton, HandleButtonUnfavoriteKitClicked);
            ElementPatterns.SubscribeAll(panel.Kits, k => k.RequestButton, HandleButtonRequestKitClicked);
            ElementPatterns.SubscribeAll(panel.Kits, k => k.PreviewButton, HandleButtonPreviewKitClicked);
        }

        ElementPatterns.SubscribeAll(_listResults, k => k.FavoriteButton, HandleButtonFavoriteKitClicked);
        ElementPatterns.SubscribeAll(_listResults, k => k.UnfavoriteButton, HandleButtonUnfavoriteKitClicked);
        ElementPatterns.SubscribeAll(_listResults, k => k.RequestButton, HandleButtonRequestKitClicked);
        ElementPatterns.SubscribeAll(_listResults, k => k.PreviewButton, HandleButtonPreviewKitClicked);

        _kitRequirementVisitor = new KitRequirementVisitor(this);
    }

    /// <summary>
    /// Update the kit for players (or <paramref name="player"/>) that has the UI open.
    /// </summary>
    /// <param name="kit">The kit to update.</param>
    /// <param name="player">Optional player to update it for. If <see langword="null"/>, updates for all players.</param>
    public async UniTask UpdateKitAsync(Kit kit, WarfarePlayer? player = null, CancellationToken token = default)
    {
        try
        {
            _ = kit.Items;
            _ = kit.UnlockRequirements;
            _ = kit.FactionFilter;
            _ = kit.MapFilter;
            _ = kit.Delays;
        }
        catch (NotIncludedException)
        {
            Kit? fullKit = await _kitDataStore.QueryKitAsync(kit.Key, KitInclude.UI, token);
            if (fullKit == null)
                return;

            kit = fullKit;
        }

        if (player == null)
        {
            foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
            {
                await UniTask.SwitchToMainThread(token);
                KitSelectionUIData? playerData = GetData<KitSelectionUIData>(pl.Steam64);
                if (playerData is not { HasUI: true })
                    continue;

                await UpdateKitAsync(kit, player, token);
            }

            return;
        }


        await UniTask.SwitchToMainThread(token);

        KitSelectionUIData? data = GetData<KitSelectionUIData>(player.Steam64);
        if (data is not { HasUI: true })
            return;

        KitPlayerComponent playerComp = player.Component<KitPlayerComponent>();
        // public kits
        for (int panelIndex = 0; panelIndex < _panels.Length; ++panelIndex)
        {
            KitPanel panel = _panels[panelIndex];
            Class cl = GetPanelClass(panelIndex);
            for (int kitIndex = 0; kitIndex < panel.Kits.Length; ++kitIndex)
            {
                ref KitCacheInformation info = ref data.GetCachedState(cl, kitIndex);
                if (info.Kit == null)
                    break;

                if (info.Kit.Key == kit.Key)
                {
                    SendKitInfo(panel.Kits[kitIndex], player, kit, playerComp, data, false, kitIndex, cl);
                }
            }
        }

        if (data.Page == KitPage.List)
        {
            // search list
            for (int kitIndex = 0; kitIndex < _listResults.Length; ++kitIndex)
            {
                ref KitCacheInformation info = ref data.GetCachedState(kitIndex);
                if (info.Kit == null)
                    break;

                if (info.Kit.Key == kit.Key)
                {
                    SendKitInfo(_listResults[kitIndex], player, kit, playerComp, data, false, kitIndex);
                }
            }
        }

        if (!playerComp.IsKitFavorited(kit.Key))
        {
            return;
        }

        // kit favorites
        for (int i = 0; i < _favoriteKits.Length; ++i)
        {
            Kit? fav = data.FavoriteKitsCache[i];
            if (fav == null)
                break;

            if (fav.Key == kit.Key)
            {
                SendFavoriteKit(i, kit, player, data);
            }
        }
    }

    private void HandleNextPage(UnturnedButton button, Player unturnedPlayer)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);
        KitSelectionUIData data = GetOrAddData(player);

        if (data.Page != KitPage.List)
            return;

        UpdatePage(player, data, data.SearchPage + 1);
    }

    private void HandlePageTyped(UnturnedTextBox textBox, Player unturnedPlayer, string text)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);
        KitSelectionUIData data = GetOrAddData(player);

        if (data.Page != KitPage.List)
            return;

        if (!int.TryParse(text, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowThousands, player.Locale.ParseFormat, out int pageNumber)
            || pageNumber <= 0)
        {
            textBox.SetText(unturnedPlayer, data.SearchPage.ToString(player.Locale.ParseFormat));
            return;
        }

        UpdatePage(player, data, pageNumber - 1);
    }

    private void HandlePreviousPage(UnturnedButton button, Player unturnedPlayer)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);
        KitSelectionUIData data = GetOrAddData(player);

        if (data.Page != KitPage.List || data.SearchPage <= 0)
            return;

        UpdatePage(player, data, data.SearchPage - 1);
    }

    private void HandleCloseUI(UnturnedButton button, Player player)
    {
        _ = CloseAsync(_playerService.GetOnlinePlayer(player));
    }

    private void HandleKitFilterUpdated(UnturnedTextBox textBox, Player unturnedPlayer, string text)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);
        KitSelectionUIData data = GetOrAddData(player);

        UpdateKitFilter(player, data, null, text);
    }

    private void HandleClassFilterChosen(UnturnedButton button, Player unturnedPlayer)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);
        KitSelectionUIData data = GetOrAddData(player);

        int classIndex = Array.IndexOf(_classButtons, button);
        if (classIndex < 0)
            return;

        Class @class = GetPanelClass(classIndex);
        UpdateKitFilter(player, data, @class == data.ClassFilter ? Class.None : @class, null);
    }

    private void HandleFavoriteStatusUpdated(CSteamID steam64, uint kitPk, bool isFavorite)
    {
        if (_playerService.GetOnlinePlayerOrNullThreadSafe(steam64) is not { } player)
        {
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await UniTask.SwitchToMainThread();

                if (!player.IsOnline)
                    return;

                KitSelectionUIData data = GetOrAddData(player);
                if (!data.HasUI)
                    return;

                Kit? kit = await _kitDataStore.QueryKitAsync(kitPk, KitInclude.UI, player.DisconnectToken);
                if (kit == null)
                    return;

                Kit[] favoriteKits = await GetFavoriteKits(player, player.DisconnectToken).ConfigureAwait(false);
                await UniTask.SwitchToMainThread();
                if (!player.IsOnline || !data.HasUI)
                    return;

                UpdateFavoriteList(player, data, favoriteKits, false);
                await UpdateKitAsync(kit, player, player.DisconnectToken);
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, "Error updating favorites for a player who's favoites changed.");
            }
        });
    }

    private KitSelectionUIData GetOrAddData(WarfarePlayer player)
    {
        return GetOrAddData(player.Steam64, _getDataFunc);
    }

    private KitSelectionUIData GetOrAddData(CSteamID steam64)
    {
        return GetOrAddData(steam64, _getDataFunc);
    }

    protected override void OnDisposing()
    {
        _kitDataStore.KitUpdated -= KitUpdated;
        _kitDataStore.KitAdded -= KitUpdated;
        _kitDataStore.KitRemoved -= KitRemoved;

        if (_nitroBoostService != null)
        {
            _nitroBoostService.OnNitroBoostStatusUpdated -= HandleNitroBoostStatusUpdated;
        }
        if (_acountLinkingService != null)
        {
            _acountLinkingService.OnLinkUpdated -= HandleAccountLinkUpdated;
            _acountLinkingService.OnGuildStatusUpdated -= HandleGuildStatusUpdated;
        }

        _kitFavoriteService.OnFavoriteStatusUpdated -= HandleFavoriteStatusUpdated;
    }

    private void KitUpdated(Kit _)
    {
        _cachedPublicKits = null;
    }

    private void KitRemoved(KitModel _)
    {
        _cachedPublicKits = null;
    }

    void IEventListener<PlayerLocaleUpdated>.HandleEvent(PlayerLocaleUpdated e, IServiceProvider serviceProvider)
    {
        KitSelectionUIData? data = GetData<KitSelectionUIData>(e.Player.Steam64);
        if (data is not { HasUI: true })
            return;

        UpdateConstantText(e.Language.IsDefault, e.Connection, data, e.Player);
    }

    /// <summary>
    /// Close the UI for a player, waiting until it fully closes.
    /// </summary>
    /// <param name="instant">The UI will instantly close instead of animating.</param>
    public async UniTask CloseAsync(WarfarePlayer player, bool instant = false, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        KitSelectionUIData? data = GetData<KitSelectionUIData>(player.Steam64);
        if (data == null || data.IsClosing || !data.HasUI)
            return;

        data.IsClosing = !instant;
        data.HasUI = false;
        data.ResetCache();
        player.UnturnedPlayer.enablePluginWidgetFlag(EPluginWidgetFlags.Default);

        data.ModalHandle.Dispose();
        if (instant)
        {
            ClearFromPlayer(player.Connection);
        }
        else
        {
            _startCloseAnimationLogic.Show(player);

            // animation is actually 0.5 but add a little extra time to account for ping
            await UniTask.Delay(TimeSpan.FromSeconds(0.6), cancellationToken: token);

            if (player.IsOnline && data.IsClosing)
            {
                ClearFromPlayer(player.Connection);
                data.IsClosing = false;
            }
        }
    }

    /// <summary>
    /// Open the UI for a player.
    /// </summary>
    public Task OpenAsync(WarfarePlayer player, CancellationToken token = default)
    {
        return OpenAsync(player, 0u, token);
    }

    /// <summary>
    /// Open the UI for a player for a specific faction.
    /// </summary>
    public async Task OpenAsync(WarfarePlayer player, uint factionId, CancellationToken token = default)
    {
        Team team = player.Team;
        if (factionId == 0u)
            factionId = team.Faction.PrimaryKey;

        await _dbSemaphore.WaitAsync(token);
        try
        {
            await player
                .Component<KitPlayerComponent>()
                .ReloadCacheAsync(_kitsDbContext, token)
                .ConfigureAwait(false);
        }
        finally
        {
            _dbSemaphore.Release();
        }

        // download public kits
        Kit[]? publicKits = _cachedPublicKits;
        if (publicKits == null)
        {
            IReadOnlyList<uint> factionIds = _teamManager.Factions;

            publicKits = _cachedPublicKits ??= await _kitDataStore
                .QueryKitsAsync(
                    KitInclude.UI,
                    q => q.Where(k => k.Type == KitType.Public && k.FactionId != null && factionIds.Contains(k.FactionId.Value) && !k.Disabled && k.Class != Class.Unarmed && k.Class != Class.None)
                        .OrderBy(k => k.Class).ThenBy(k => k.Id),
                    token: token
                )
                .ConfigureAwait(false);
        }

        Kit[] favoriteKits = await GetFavoriteKits(player, token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        KitSelectionUIData data = GetOrAddData(player);

        ModalHandle.TryGetModalHandle(player, ref data.ModalHandle);
        player.UnturnedPlayer.disablePluginWidgetFlag(EPluginWidgetFlags.Default);
        data.Team = team;
        data.Page = KitPage.Public;
        data.SearchMaxSize = null;

        if (!data.HasUI)
        {
            data.HasUI = true;
            data.IsClosing = false;
            data.ResetCache();
            SendToPlayer(player.SteamPlayer);
        }

        bool isDefaultLang = player.Locale.IsDefaultLanguage;
        data.HasDefaultText = isDefaultLang;

        ITransportConnection c = player.Connection;

        UpdateConstantText(isDefaultLang, c, data, player);

        if (!isDefaultLang)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.Yield();
        }

        Class prevClass = Class.Unarmed;
        int panelIndex = -1;
        int kitIndex = -1;
        KitPlayerComponent kitComp = player.Component<KitPlayerComponent>();
        bool needsNitroBoostRequest = false;
        foreach (Kit kit in publicKits)
        {
            if (kit.Faction.PrimaryKey != factionId)
                continue;

            if (kit.Class != prevClass)
            {
                // only send 1 class per frame, after AR it should be off the screen for most people so we can delay longer
                if (prevClass > Class.AutomaticRifleman)
                {
                    await UniTask.Delay(125, cancellationToken: token);
                }
                else
                {
                    await UniTask.NextFrame();
                }

                token.ThrowIfCancellationRequested();

                if (panelIndex >= 0)
                {
                    KitPanel panelToClear = _panels[panelIndex];
                    for (int i = kitIndex + 1; i < panelToClear.Kits.Length; ++i)
                    {
                        panelToClear.Kits[i].Root.Hide(c);
                    }
                }

                while (true)
                {
                    ++panelIndex;
                    if (panelIndex >= _panels.Length)
                    {
                        panelIndex = -1;
                        break;
                    }

                    kitIndex = -1;
                    prevClass = kit.Class;
                    if (GetPanelClass(panelIndex) != kit.Class)
                    {
                        // no kits in panel
                        _panels[panelIndex].Root.Hide(c);
                    }
                    else
                    {
                        break;
                    }
                }

                if (panelIndex < 0)
                    break;
            }

            KitPanel panel = _panels[panelIndex];
            ++kitIndex;
            if (kitIndex >= panel.Kits.Length)
                continue;

            KitInfo info = panel.Kits[kitIndex];
            SendKitInfo(info, player, kit, kitComp, data, fromDefaultValues: true, kitIndex, prevClass);
            
            if (data.GetCachedState(prevClass, kitIndex).LabelState == StatusState.ServerBoostRequired)
            {
                needsNitroBoostRequest = true;
            }
        }

        if (panelIndex >= 0)
        {
            KitPanel panelToClear = _panels[panelIndex];
            for (int i = kitIndex + 1; i < panelToClear.Kits.Length; ++i)
            {
                panelToClear.Kits[i].Root.Hide(c);
            }
            for (int i = panelIndex + 1; i < _panels.Length; ++i)
            {
                _panels[i].Root.Hide(c);
            }
        }

        token.ThrowIfCancellationRequested();
        await UniTask.NextFrame();

        UpdateFavoriteList(player, data, favoriteKits, true);

        // check if linked and whether or not the player is in the guild to show the right server boost button
        if (needsNitroBoostRequest && _acountLinkingService != null)
        {
            try
            {
                await CheckNitroBoostStatus(player, data, token, publicKits: true, listKits: false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, $"Error fetching discord server boost status for {player}.");
            }
        }
    }

    private Task<Kit[]> GetFavoriteKits(WarfarePlayer player, CancellationToken token)
    {
        ulong s64 = player.Steam64.m_SteamID;
        IReadOnlyList<uint> factions = _teamManager.Factions;
        return _kitDataStore.QueryKitsAsync(
            KitInclude.Default,
            q => q
                .Where(k => k.Favorites.Any(f => f.Steam64 == s64) && (k.Type != KitType.Public || (!k.Disabled && k.Faction != null && factions.Contains(k.Faction.Key) && k.Season >= WarfareModule.Season)))
                .Take(_favoriteKits.Length),
            token: token
        );
    }

    private void UpdateFavoriteList(WarfarePlayer player, KitSelectionUIData data, Kit[] kits, bool fromDefaults)
    {
        ITransportConnection c = player.Connection;

        int i = 0;
        int ct = Math.Min(kits.Length, _favoriteKits.Length);
        if (ct == 0)
        {
            if (!fromDefaults)
                _favoritesLabel.Hide(player);
        }
        else
        {
            _favoritesLabel.Show(player);
        }

        for (; i < ct; ++i)
        {
            SendFavoriteKit(i, kits[i], player, data);
        }

        if (fromDefaults)
            return;

        for (; i < _favoriteKits.Length; ++i)
        {
            if (data.FavoriteKitsCache[i] == null)
                continue;

            data.FavoriteKitsCache[i] = null;
            _favoriteKits[i].Root.Hide(c);
        }
    }

    private void SendFavoriteKit(int index, Kit kit, WarfarePlayer player, KitSelectionUIData data)
    {
        FavoriteKitInfo ui = _favoriteKits[index];
        ITransportConnection c = player.Connection;
        ui.Flag.SetText(c, kit.Faction.Sprite);
        ui.Class.SetText(c, kit.Class.GetIconString());
        string displayName = kit.GetDisplayName(player.Locale.LanguageInfo, useIdFallback: true);
        ui.Name.SetText(c, displayName);
        ui.Id.SetText(c, ReferenceEquals(displayName, kit.Id) ? string.Empty : kit.Id);
        ui.Root.Show(c);
        data.FavoriteKitsCache[index] = kit;
    }

    private void UpdateKitFilter(WarfarePlayer player, KitSelectionUIData data, Class? @class, string? search)
    {
        if (@class.HasValue && search != null)
        {
            if (data.ClassFilter == @class && string.Equals(data.NameFilter, search, StringComparison.OrdinalIgnoreCase))
                return;
        }
        else if (@class.HasValue)
        {
            if (data.ClassFilter == @class)
                return;
        }
        else if (search != null)
        {
            if (string.Equals(data.NameFilter, search, StringComparison.OrdinalIgnoreCase))
                return;
        }
        else return;

        if (@class.HasValue)
        {
            data.ClassFilter = @class.Value;
            _searchResultsTitle.SetText(player.Connection, data.ClassFilter == Class.None
                ? _translations.SearchResultsLabel.Translate(player)
                : _translations.SearchResultsByClassLabel.Translate(data.ClassFilter, player)
            );
        }

        if (search != null)
        {
            data.NameFilter = search;
        }

        if (data.Page != KitPage.List)
        {
            _switchToListLogic.Show(player);
            data.Page = KitPage.List;
        }

        Task.Run(async () =>
        {
            try
            {
                await UpdateSearchAsync(player, data).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, "Error updating search after filter change.");
            }
        });
    }
    
    private void UpdatePage(WarfarePlayer player, KitSelectionUIData data, int page)
    {
        data.SearchPage = Math.Max(0, page);

        Task.Run(async () =>
        {
            try
            {
                await UpdateSearchAsync(player, data).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, "Error updating search after page change.");
            }
        });
    }

    private async Task UpdateSearchAsync(WarfarePlayer player, KitSelectionUIData data, CancellationToken token = default)
    {
        GetLogger().LogTrace("Updating search...");
        string? match = string.IsNullOrEmpty(data.NameFilter) ? null : "%" + data.NameFilter + "%";
        Class classFilter = data.ClassFilter;
        if (string.IsNullOrEmpty(match) && classFilter < Class.Squadleader)
        {
            GetLogger().LogTrace("Invalid search");
            await UniTask.SwitchToMainThread(token);
            _listNoResult.Show(player.Connection);
            HideKits(player, data);
            return;
        }

        bool doSort = false;
        ulong steam64 = player.Steam64.m_SteamID;

        int? searchMaxSize = data.SearchMaxSize;
        if (!searchMaxSize.HasValue)
        {
            searchMaxSize = await QueryFactory(_kitsDbContext.Kits).CountAsync(token);
            data.SearchMaxSize = searchMaxSize;
        }

        int maxPage = (int)Math.Ceiling((double)searchMaxSize.Value / _listResults.Length);
        GetLogger().LogTrace($"Page: {data.SearchPage}, max page: {maxPage}.");
        data.SearchPage = Math.Min(data.SearchPage, maxPage);
            
        doSort = true;
        Kit[] output;
        while (true)
        {
            output = await _kitDataStore.QueryKitsAsync(KitInclude.UI, QueryFactory, pageInfo: new PaginationInfo(data.SearchPage, _listResults.Length), token: token);

            if (output.Length != 0 || data.SearchPage <= 0)
                break;

            GetLogger().LogTrace($"No data on page {data.SearchPage}, moving down.");
            --data.SearchPage;
        }

        await SendKitList(player, data, output, token);
        return;

        IQueryable<KitModel> QueryFactory(IQueryable<KitModel> q)
        {
            uint langId = player.Locale.LanguageInfo.Key;
            uint defaultLangId = _translationService.LanguageService.GetDefaultLanguage().Key;
            if (classFilter >= Class.Squadleader)
            {
                if (match != null)
                {
                    q = q.Where(x => x.Class == classFilter && (EF.Functions.Like(x.Id, match) || x.Translations.Any(x => (x.LanguageId == langId || x.LanguageId == defaultLangId) && EF.Functions.Like(x.Value, match))));
                }
                else
                {
                    q = q.Where(x => x.Class == classFilter);
                }
            }
            else
            {
                // assumption: match != null (guard at beginning of method)
                q = q.Where(x => EF.Functions.Like(x.Id, match) || x.Translations.Any(x => (x.LanguageId == langId || x.LanguageId == defaultLangId) && EF.Functions.Like(x.Value, match)));
            }

            IReadOnlyList<uint> factionIds = _teamManager.Factions;

            // filter out irrelevant loadouts and public kits
            q = q.Where(x =>
                (x.Type != KitType.Loadout || x.Access.Any(x => x.Steam64 == steam64))
                && (x.Type != KitType.Public || x.Faction != null && factionIds.Contains(x.Faction.Key) && x.Season >= WarfareModule.Season)
            );

            if (!doSort)
                return q;
            
            IOrderedQueryable<KitModel> sortQ;
            if (match != null)
                sortQ = q.OrderByDescending(x => x.Id == match).ThenByDescending(x => x.Type != KitType.Special);
            else
                sortQ = q.OrderByDescending(x => x.Type != KitType.Special);

            sortQ = sortQ.ThenByDescending(x => x.Type == KitType.Loadout).ThenBy(x => x.Id);
            return sortQ;
        }
    }

    private async Task SendKitList(WarfarePlayer player, KitSelectionUIData data, Kit[] kits, CancellationToken token)
    {
        await UniTask.SwitchToMainThread(token);

        ITransportConnection c = player.Connection;
        if (kits.Length == 0)
        {
            GetLogger().LogTrace("No results");
            _listNoResult.Show(c);
            HideKits(player, data);
            _listPage.SetText(c, string.Empty);
            _listPreviousPage.Disable(c);
            _listNextPage.Disable(c);
            return;
        }

        _listNoResult.Hide(c);

        _listPage.SetText(c, (data.SearchPage + 1).ToString(player.Locale.CultureInfo));
        GetLogger().LogTrace($"Setting button states for page {data.SearchPage} ({kits.Length}/{_listResults.Length}) results), moving down.");
        _listPreviousPage.SetState(c, data.SearchPage > 0);
        _listNextPage.SetState(c, kits.Length > _listResults.Length);

        bool needsNitroBoostRequest = false;

        int i = 0;
        int ct = Math.Min(kits.Length, _listResults.Length);
        KitPlayerComponent comp = player.Component<KitPlayerComponent>();
        for (; i < ct; ++i)
        {
            Kit kit = kits[i];
            KitInfo ui = _listResults[i];
            SendKitInfo(ui, player, kit, comp, data, false, i);

            if (data.GetCachedState(i).LabelState == StatusState.ServerBoostRequired)
            {
                needsNitroBoostRequest = true;
            }
            ui.Root.Show(c);
        }

        // check if linked and whether or not the player is in the guild to show the right server boost button
        if (needsNitroBoostRequest && _acountLinkingService != null)
        {
            try
            {
                await CheckNitroBoostStatus(player, data, token, publicKits: false, listKits: true);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, $"Error fetching discord server boost status for {player}.");
            }
        }

        HideKits(player, data, i);
    }

    private void HideKits(WarfarePlayer player, KitSelectionUIData data, int startIndex = 0)
    {
        for (int i = startIndex; i < _listResults.Length; ++i)
        {
            ref KitCacheInformation cache = ref data.GetCachedState(i);
            if (cache.Kit == null)
                break;

            _listResults[i].Root.Hide(player.Connection);
            cache.Kit = null;
        }
    }

    private void SendKitInfo(KitInfo ui, WarfarePlayer player, Kit kit, KitPlayerComponent kitAccessComp, KitSelectionUIData data, bool fromDefaultValues, int index, Class @class = Class.None)
    {
        ITransportConnection c = player.Connection;
        ui.Flag.SetText(c, kit.Faction.Sprite);
        ui.Class.SetText(c, kit.Class.GetIconString());
        ui.Name.SetText(c, kit.GetDisplayName(player.Locale.LanguageInfo, useIdFallback: true));

        string id = kit.Id;
        if (kit.Type == KitType.Loadout)
        {
            int loadoutId = LoadoutIdHelper.Parse(id, out CSteamID steam64);
            if (loadoutId >= 0 && player.Equals(steam64))
            {
                id = _translations.LoadoutIdLabel.Translate(LoadoutIdHelper.GetLoadoutLetter(loadoutId).ToUpperInvariant());
            }
        }

        ui.Id.SetText(c, id);
        
        if (kitAccessComp.IsKitAccessible(kit.Key))
        {
            ui.PreviewButtonParent.Hide(c);
            ui.RequestButtonParent.Show(c);
        }
        else if (!fromDefaultValues)
        {
            ui.RequestButtonParent.Hide(c);
            ui.PreviewButtonParent.Show(c);
        }

        if (kitAccessComp.IsKitFavorited(kit.Key))
        {
            ui.FavoriteButtonParent.Hide(c);
            ui.UnfavoriteButtonParent.Show(c);
        }
        else if (!fromDefaultValues)
        {
            ui.UnfavoriteButtonParent.Hide(c);
            ui.FavoriteButtonParent.Show(c);
        }

        ImmutableArray<ItemDescriptor> itemDescriptors = kit.GetItemDescriptors(data.Team ?? Team.NoTeam, _kitItemResolver, _iconProvider, _weaponTextService);
        int i = 0;
        int itemCt = Math.Min(itemDescriptors.Length, ui.IncludeLabels.Length);
        for (; i < itemCt; ++i)
        {
            ItemDescriptor desc = itemDescriptors[i];
         
            CountIncludeLabel lbl = ui.IncludeLabels[i];

            lbl.Name.SetText(c, desc.ItemName);
            if (desc.Amount > 1)
            {
                lbl.Count.SetText(c, desc.Amount.ToString(player.Locale.CultureInfo));
                lbl.Count.Show(c);
            }
            else if (!fromDefaultValues)
            {
                lbl.Count.Hide(c);
            }

            lbl.Icon.SetText(c, desc.Icon);
            lbl.Show(c);
            ImmutableArray<ItemDescriptorAttachment> attachments = desc.Attachments;
            if (i < 3)
            {
                IncludeLabel[] attachmentLabels = i switch
                {
                    0 => ui.PrimaryAttachments,
                    1 => ui.SecondaryAttachments,
                    _ => ui.TertiaryAttachments,
                };
                int attachmentCt = attachments.IsDefaultOrEmpty ? 0 : attachments.Length;
                int mask = 0;
                for (int j = 0; j < attachmentCt; ++j)
                {
                    ItemDescriptorAttachment attachment = attachments[j];
                    IncludeLabel attachmentLabel = attachmentLabels[_attachmentMap[(int)attachment.AttachmentType]];

                    mask |= 1 << ((int)attachment.AttachmentType / 2);

                    if (attachment.Icon != null)
                    {
                        attachmentLabel.Icon.SetText(c, attachment.Icon);
                    }
                    else if (!fromDefaultValues)
                    {
                        attachmentLabel.Icon.SetText(c, GetAttachmentIcon(j));
                    }

                    attachmentLabel.Name.SetText(c, attachment.ItemName);
                    attachmentLabel.Show(c);
                }

                if (!fromDefaultValues)
                {
                    for (int j = 0; j < 5; ++j)
                    {
                        if ((mask & (1 << ((int)_inverseAttachmentMap[j] / 2))) != 0)
                            continue;

                        attachmentLabels[j].Hide(c);
                    }
                }
            }
        }

        if (!fromDefaultValues)
        {
            for (; i < ui.IncludeLabels.Length; ++i)
            {
                ui.IncludeLabels[i].Hide(c);
            }
        }

        ref KitCacheInformation cachedInfo = ref data.GetCachedState(@class, index);
        cachedInfo.Kit = kit;

        UpdateStatusLabels(ui, c, fromDefaultValues, data, @class, index, player, kit, kitAccessComp);
    }

    private void UpdateStatusLabels(KitInfo ui, ITransportConnection c, bool fromDefaultValues, KitSelectionUIData data, Class @class, int index, WarfarePlayer player, Kit kit, KitPlayerComponent kitAccessComp)
    {
        KitRequirementsState state;
        state.UI = ui;
        state.Connection = c;
        state.FromDefaults = fromDefaultValues;
        state.Data = data;
        state.Class = @class;
        state.Index = index;

        if (kitAccessComp.ActiveKitKey == kit.Key)
        {
            ui.StatusLabel.SetText(c, _translations.StatusEquipped.Translate(player));
            if (!fromDefaultValues)
                ui.UnlockSection.Hide(c);

            data.SetCacheState(in state, PurchaseButtonState.None, StatusState.Available);
            return;
        }

        KitRequirementResolutionContext<KitRequirementsState> ctx = new KitRequirementResolutionContext<KitRequirementsState>(player, data.Team ?? Team.NoTeam, kit, kitAccessComp.CachedKit, kitAccessComp, state);

        bool anyNo = false;
        foreach (IKitRequirement requirement in _kitRequirements.Request)
        {
            if (requirement.AcceptCached(_kitRequirementVisitor, in ctx) == KitRequirementResult.No)
            {
                anyNo = true;
                break;
            }
        }

        if (!anyNo)
        {
            ui.StatusLabel.SetText(c, _translations.StatusUnlocked.Translate(player));
            if (!fromDefaultValues)
                ui.UnlockSection.Hide(c);

            data.SetCacheState(in state, PurchaseButtonState.None, StatusState.Available);
        }
    }

    internal static int GetClassPanelIndex(Class @class)
    {
        return @class - Class.Squadleader;
    }

    internal static Class GetPanelClass(int panelIndex)
    {
        return (Class)(panelIndex + (int)Class.Squadleader);
    }

    internal static string GetAttachmentIcon(AttachmentType attachmentType)
    {
        return attachmentType switch
        {
            AttachmentType.Sight => "ˆ",
            AttachmentType.Tactical => "ˇ",
            AttachmentType.Grip => "ˈ",
            AttachmentType.Barrel => "ˉ",
            _ => "ˊ"
        };
    }

    internal static string GetAttachmentIcon(int attachmentRowIndex)
    {
        return attachmentRowIndex switch
        {
            0 => "ˊ",
            1 => "ˆ",
            2 => "ˉ",
            3 => "ˈ",
            _ => "ˇ"
        };
    }

    private void UpdateConstantText(bool isDefaultLang, ITransportConnection c, KitSelectionUIData data, WarfarePlayer player)
    {
        if (!data.HasDefaultText)
            isDefaultLang = false;

        if (!isDefaultLang || !_translations.PublicKitsLabel.HasDefaultValue)
            _publicKitsTitle.SetText(c, _translations.PublicKitsLabel.Translate(player));

        if (!isDefaultLang || !_translations.KitNameFilterPlaceholder.HasDefaultValue)
            _kitNameFilter.SetPlaceholder(c, _translations.KitNameFilterPlaceholder.Translate(player));

        if (!isDefaultLang || !_translations.SearchButtonLabel.HasDefaultValue)
            _kitNameFilterSearchLabel.SetText(c, _translations.SearchButtonLabel.Translate(player));

        if (!isDefaultLang || !_translations.ClassesLabel.HasDefaultValue)
            _classFilterLabel.SetText(c, _translations.ClassesLabel.Translate(player));

        if (!isDefaultLang || !_translations.FavoritesLabel.HasDefaultValue)
            _favoritesLabel.SetText(c, _translations.FavoritesLabel.Translate(player));

        if (data.ClassFilter == Class.None)
        {
            if (!isDefaultLang || !_translations.SearchResultsLabel.HasDefaultValue)
                _searchResultsTitle.SetText(c, _translations.SearchResultsLabel.Translate(player));
        }
        else
        {
            _searchResultsTitle.SetText(c, _translations.SearchResultsByClassLabel.Translate(data.ClassFilter, player));
        }

        if (!isDefaultLang || !_translations.SearchResultsNoResults.HasDefaultValue)
            _listNoResultLabel.SetText(c, _translations.SearchResultsNoResults.Translate(player));

        if (!isDefaultLang || !_translations.SearchResultsPreviousPage.HasDefaultValue)
            _listPreviousPage.SetText(c, _translations.SearchResultsPreviousPage.Translate(player));

        if (!isDefaultLang || !_translations.SearchResultsNextPage.HasDefaultValue)
            _listNextPage.SetText(c, _translations.SearchResultsNextPage.Translate(player));

        if (!isDefaultLang || !_translations.SearchResultsPageInputPlaceholder.HasDefaultValue)
            _listPage.SetPlaceholder(c, _translations.SearchResultsPageInputPlaceholder.Translate(player));

        if (!isDefaultLang || !_translations.ToPublicButtonLabel.HasDefaultValue)
            _switchBackToPanel.SetText(c, _translations.ToPublicButtonLabel.Translate(player));

        if (!isDefaultLang || !_translations.CloseButtonLabel.HasDefaultValue)
            _close.SetText(c, _translations.CloseButtonLabel.Translate(player));

        for (Class cl = Class.Squadleader; cl <= Class.SpecOps; ++cl)
        {
            Translation t = _translations.DescriptionOfClass(cl)!;
            if (!isDefaultLang || !t.HasDefaultValue)
                _panels[GetClassPanelIndex(cl)].Description.SetText(c, t.Translate(player));

            if (isDefaultLang)
                continue;

            string className = _translationService.ValueFormatter.FormatEnum(cl, player.Locale.LanguageInfo);
            _panels[GetClassPanelIndex(cl)].Title.SetText(c, className);
        }

        data.HasDefaultText = isDefaultLang;
    }

    private enum KitPage
    {
        Public,
        List
    }

    private class KitSelectionUIData : IUnturnedUIData
    {
        internal bool HasUI;
        internal bool HasDefaultText;
        internal bool IsClosing;
        internal Team? Team;
        internal KitPage Page;
        internal IDisposable? HudHandler;
        internal int Operations;

        internal Class ClassFilter
        {
            get;
            set
            {
                field = value;
                HandleFilterUpdated();
            }
        }

        internal string? NameFilter
        {
            get;
            set
            {
                field = value;
                HandleFilterUpdated();
            }
        }

        internal int SearchPage;
        internal int? SearchMaxSize;
        public readonly Kit?[] FavoriteKitsCache;

        internal void HandleFilterUpdated()
        {
            SearchMaxSize = null;
        }

        private KitCacheInformation[] _publicKitCache;
        private KitCacheInformation[] _listKitCache;
        public ModalHandle ModalHandle;

        public void SetCacheState(in KitRequirementsState state, PurchaseButtonState buttonState, StatusState labelState)
        {
            ref KitCacheInformation info = ref GetCachedState(in state);

            info.ButtonState = buttonState;
            info.LabelState = labelState;
        }

        public ref KitCacheInformation GetCachedState(in KitRequirementsState state)
        {
            if (state.Class >= Class.Squadleader)
            {
                return ref GetCachedState(state.Class, state.Index);
            }

            return ref GetCachedState(state.Index);
        }

        public ref KitCacheInformation GetCachedState(Class @class, int ind)
        {
            if (@class < Class.Squadleader)
            {
                return ref GetCachedState(ind);
            }

            int index = GetClassPanelIndex(@class) * 3 + ind;
            if (index >= _publicKitCache.Length)
                throw new ArgumentOutOfRangeException(nameof(ind));

            return ref _publicKitCache[index];
        }

        public ref KitCacheInformation GetCachedState(int listResultIndex)
        {
            if (listResultIndex < 0 || listResultIndex >= _listKitCache.Length)
                throw new ArgumentOutOfRangeException(nameof(listResultIndex));

            return ref _listKitCache[listResultIndex];
        }

        public CSteamID Player { get; private set; }
        public UnturnedUI Owner { get; private set; }
        UnturnedUIElement? IUnturnedUIData.Element => null;

        public KitSelectionUIData(CSteamID player, KitSelectionUI owner)
        {
            Player = player;
            Owner = owner;

            _publicKitCache = new KitCacheInformation[owner._panels.Length * owner._panels[0].Kits.Length];
            _listKitCache = new KitCacheInformation[owner._listResults.Length];
            FavoriteKitsCache = new Kit[owner._favoriteKits.Length];
        }

        public void ResetCache()
        {
            Array.Clear(_listKitCache, 0, _listKitCache.Length);
            Array.Clear(_publicKitCache, 0, _publicKitCache.Length);
            SearchPage = 0;
        }
    }

    private struct KitCacheInformation
    {
        public Kit? Kit;

        public PurchaseButtonState ButtonState;
        public StatusState LabelState;
    }

    private enum PurchaseButtonState
    {
        None,
        PremiumCost,
        CreditCost,
        ViewLoadoutTicket,
        OpenLoadoutTicket,
        JoinSquad,
        CreateSquad,
        OpenDiscordForBoosts,
        JoinDiscordGuild,
        BeginLinkDiscordAccount
    }

    private enum StatusState
    {
        Loading,
        Generic,
        PremiumCost,
        CreditCost,
        Exclusive,
        LoadoutLocked,
        LoadoutOutOfDate,
        Disabled,
        ServerBoostRequired,
        MapFilter,
        FactionFilter,
        Squad,
        SquadLeader,
        SquadMemberTaken,
        TooManySquadMember,
        ClassLimited,
        GlobalCooldown,
        PremiumCooldown,
        UnlockRequirement,
        Available
    }

    private struct KitRequirementsState
    {
        public ITransportConnection Connection;
        public KitInfo UI;
        public bool FromDefaults;
        public KitSelectionUIData Data;
        public int Index;
        public Class Class;
    }

    private class KitRequirementVisitor(KitSelectionUI @this) : IKitRequirementVisitor<KitRequirementsState>
    {
        private readonly KitSelectionUI _this = @this;

        public void AcceptGenericRequirementNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx, string message)
        {
            ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, TranslationFormattingUtility.Colorize(message, new Color32(194, 96, 62, 255)));
            if (!ctx.State.FromDefaults)
                ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.None, StatusState.Generic);
        }

        public void AcceptPremiumCostNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx, decimal cost)
        {
            ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, _this._translations.StatusNotPurchased.Translate(ctx.Player));
            ctx.State.UI.UnlockButton.SetText(ctx.State.Connection, _this._translations.PurchaseButtonCurrency.Translate(decimal.Round(ctx.Kit.PremiumCost, 2), ctx.Player));
            ctx.State.UI.UnlockSection.Show(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.PremiumCost, StatusState.PremiumCost);
        }

        public void AcceptCreditCostNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx, double cost, double current)
        {
            if (cost > current)
            {
                ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, _this._translations.StatusCreditsCantAfford.Translate(cost - current, ctx.Player));
                if (!ctx.State.FromDefaults)
                    ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);

                ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.None, StatusState.CreditCost);
            }
            else
            {
                ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, _this._translations.StatusNotPurchased.Translate(ctx.Player));
                ctx.State.UI.UnlockButton.SetText(ctx.State.Connection, _this._translations.PurchaseButtonCredits.Translate(cost, current, current - cost, ctx.Player));
                ctx.State.UI.UnlockSection.Show(ctx.State.Connection);

                ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.CreditCost, StatusState.CreditCost);
            }
        }

        public void AcceptExclusiveKitNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx)
        {
            ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, _this._translations.StatusExclusiveNotOwned.Translate(ctx.Player));
            if (!ctx.State.FromDefaults)
                ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.None, StatusState.Exclusive);
        }

        public void AcceptLoadoutLockedNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx)
        {
            ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, _this._translations.StatusLoadoutLocked.Translate(ctx.Player));
            ctx.State.UI.UnlockButton.SetText(ctx.State.Connection, _this._translations.PurchaseButtonViewLoadoutTicket.Translate(ctx.Player));
            ctx.State.UI.UnlockSection.Show(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.ViewLoadoutTicket, StatusState.LoadoutLocked);
        }

        public void AcceptLoadoutOutOfDateNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx, int season)
        {
            ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, _this._translations.StatusLoadoutNeedsUpgraded.Translate(season, ctx.Player));
            ctx.State.UI.UnlockButton.SetText(ctx.State.Connection, _this._translations.PurchaseButtonOpenLoadoutTicket.Translate(ctx.Player));
            ctx.State.UI.UnlockSection.Show(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.OpenLoadoutTicket, StatusState.LoadoutOutOfDate);
        }

        public void AcceptDisabledNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx)
        {
            ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, _this._translations.StatusDisabled.Translate(ctx.Player));
            if (!ctx.State.FromDefaults)
                ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.None, StatusState.Disabled);
        }

        public void AcceptNitroBoostRequirementNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx)
        {
            ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, _this._translations.StatusServerBoostRequired.Translate(ctx.Player));
            if (!ctx.State.FromDefaults)
                ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.None, StatusState.ServerBoostRequired);
        }

        public void AcceptMapFilterNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx, string mapName)
        {
            ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, _this._translations.StatusFailsMapFilter.Translate(mapName, ctx.Player));
            if (!ctx.State.FromDefaults)
                ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.None, StatusState.MapFilter);
        }

        public void AcceptFactionFilterNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx, FactionInfo faction)
        {
            ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, _this._translations.StatusFailsFactionFilter.Translate(faction, ctx.Player));
            if (!ctx.State.FromDefaults)
                ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.None, StatusState.FactionFilter);
        }

        public void AcceptRequiresSquadNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx, bool needsSquadLead)
        {
            Squad? squad = ctx.Player.GetSquad();
            PurchaseButtonState button = PurchaseButtonState.None;
            if (squad == null && _this._squadManager != null && (!needsSquadLead || _this._squadManager.CanCreateNewSquad(ctx.Team)))
            {
                ctx.State.UI.UnlockButton.SetText(
                    ctx.State.Connection,
                    (needsSquadLead ? _this._translations.PurchaseButtonCreateSquad : _this._translations.PurchaseButtonJoinSquad).Translate(ctx.Player)
                );
                ctx.State.UI.UnlockSection.Show(ctx.State.Connection);
                button = needsSquadLead ? PurchaseButtonState.CreateSquad : PurchaseButtonState.JoinSquad;
            }
            else if (!ctx.State.FromDefaults)
            {
                ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);
            }

            ctx.State.UI.StatusLabel.SetText(
                ctx.State.Connection,
                (needsSquadLead ? _this._translations.StatusSquadLeaderRequired : _this._translations.StatusSquadMemberRequired).Translate(ctx.Player)
            );

            ctx.State.Data.SetCacheState(in ctx.State, button, needsSquadLead ? StatusState.SquadLeader : StatusState.Squad);
        }

        public void AcceptMinRequiredSquadMembersNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx,
            WarfarePlayer? playerTakingKit, int squadMemberCount, int minimumSquadMembers)
        {
            if (playerTakingKit != null)
                ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, _this._translations.StatusTakenBySquadMember.Translate(playerTakingKit, ctx.Player));
            else
                ctx.State.UI.StatusLabel.SetText(ctx.State.Connection, _this._translations.StatusSquadMembersRequired.Translate(squadMemberCount, minimumSquadMembers, ctx.Player));

            if (!ctx.State.FromDefaults)
                ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.None, playerTakingKit != null ? StatusState.SquadMemberTaken : StatusState.TooManySquadMember);
        }

        public void AcceptClassesAllowedPerXTeammatesRequirementNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx,
            int allowedPerXUsers, int currentUsers, int teammates, int kitsAllowed)
        {
            ctx.State.UI.StatusLabel.SetText(
                ctx.State.Connection,
                _this._translations.StatusTooManyTeammatesWithClass.Translate(ctx.Kit.Class, currentUsers, currentUsers + allowedPerXUsers, ctx.Player)
            );

            if (!ctx.State.FromDefaults)
                ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.None, StatusState.ClassLimited);
        }

        public void AcceptGlobalCooldownNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx, in Cooldown requestCooldown)
        {
            ctx.State.UI.StatusLabel.SetText(
                ctx.State.Connection,
                _this._translations.StatusRequestCooldown.Translate(requestCooldown.GetTimeLeft(), ctx.Player)
            );

            if (!ctx.State.FromDefaults)
                ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.None, StatusState.GlobalCooldown);
        }

        public void AcceptPremiumCooldownNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx, in Cooldown requestCooldown)
        {
            ctx.State.UI.StatusLabel.SetText(
                ctx.State.Connection,
                _this._translations.StatusRequestCooldown.Translate(requestCooldown.GetTimeLeft(), ctx.Player)
            );

            if (!ctx.State.FromDefaults)
                ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.None, StatusState.PremiumCooldown);
        }

        public void AcceptKitSpecificUnlockRequirementNotMet(in KitRequirementResolutionContext<KitRequirementsState> ctx, UnlockRequirement requirement)
        {
            ctx.State.UI.StatusLabel.SetText(
                ctx.State.Connection,
                requirement.GetSignText(ctx.Player, ctx.Player.Locale.LanguageInfo, ctx.Player.Locale.CultureInfo)
            );

            if (!ctx.State.FromDefaults)
                ctx.State.UI.UnlockSection.Hide(ctx.State.Connection);

            ctx.State.Data.SetCacheState(in ctx.State, PurchaseButtonState.None, StatusState.UnlockRequirement);
        }
    }
}

public sealed class KitSelectionUITranslations : PropertiesTranslationCollection
{
    protected override string FileName => "UI/Kit Selection";

    [TranslationData("Label for the page with all the public kits.")]
    public readonly Translation PublicKitsLabel = new Translation("Public Kits", TranslationOptions.TMProUI);

    [TranslationData("Default label for the page with kit search results.")]
    public readonly Translation SearchResultsLabel = new Translation("Search Results", TranslationOptions.TMProUI);

    [TranslationData("Default label for the page with kit search results when sorting by class.", "The class being filtered by")]
    public readonly Translation<Class> SearchResultsByClassLabel = new Translation<Class>("Search Results - {0} kits", TranslationOptions.TMProUI);

    [TranslationData("Text shown when there were no search results.")]
    public readonly Translation SearchResultsNoResults = new Translation("No results\n<#b4b4b4>Try adjusting your search parameters.</color>", TranslationOptions.TMProUI);

    [TranslationData("Previous page button text.")]
    public readonly Translation SearchResultsPreviousPage = new Translation("Previous", TranslationOptions.TMProUI);

    [TranslationData("Next page button text.")]
    public readonly Translation SearchResultsNextPage = new Translation("Next", TranslationOptions.TMProUI);

    [TranslationData("Page text box placeholder text.")]
    public readonly Translation SearchResultsPageInputPlaceholder = new Translation("Page", TranslationOptions.TMProUI);

    [TranslationData("Label for the class list on the left panel.")]
    public readonly Translation ClassesLabel = new Translation("Classes", TranslationOptions.TMProUI);

    [TranslationData("Label for the favorite kits list on the left panel.")]
    public readonly Translation FavoritesLabel = new Translation("Favorites", TranslationOptions.TMProUI);

    [TranslationData("Label for the included items list on each kit.")]
    public readonly Translation IncludedItemsLabel = new Translation("Includes", TranslationOptions.TMProUI);

    [TranslationData("Label for the playtime on each kit.", "Total playtime duration.")]
    public readonly Translation<TimeSpan> PlayTimeLabel = new Translation<TimeSpan>("Playtime: {0}", TranslationOptions.TMProUI, TimeAddon.Create(TimeSpanFormatType.Short));

    [TranslationData("Placeholder text for the kit search text box.")]
    public readonly Translation KitNameFilterPlaceholder = new Translation("Kit Name Filter", TranslationOptions.TMProUI);

    [TranslationData("Label for the search button used in conjunction with the kit name filter.")]
    public readonly Translation SearchButtonLabel = new Translation("Search", TranslationOptions.TMProUI);
    
    [TranslationData("Label for the button which switches from search results back to the main public kit page.")]
    public readonly Translation ToPublicButtonLabel = new Translation("Return to Public Kits", TranslationOptions.TMProUI);
    
    [TranslationData("Label for the button which closes the UI.")]
    public readonly Translation CloseButtonLabel = new Translation("Cancel", TranslationOptions.TMProUI);
    
    [TranslationData("Label for when the kit needs to be bought with credits or real money.")]
    public readonly Translation StatusNotPurchased = new Translation("<#c2603e>Not Owned</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label for when the kit needs to be bought with credits or real money.")]
    public readonly Translation StatusExclusiveNotOwned = new Translation("<#c2603e>Not Owned</color>", TranslationOptions.TMProUI);

    [TranslationData("Label for when the kit needs to be bought with credits or real money and the player doesn't have enough.", "Difference between kit cost and current balance in C")]
    public readonly Translation<double> StatusCreditsCantAfford = new Translation<double>("<#c2603e>Too Expensive – Missing <#b8ffc1>C</color> <#fff>{0}</color></color>", TranslationOptions.TMProUI, "F0");

    [TranslationData("Label for when the kit is a locked loadout, meaning its pending set up by staff.")]
    public readonly Translation StatusLoadoutLocked = new Translation("<#9cb6a4>Pending setup</color> by staff", TranslationOptions.TMProUI);
    
    [TranslationData("Label for when the kit is an expired loadout.")]
    public readonly Translation<int> StatusLoadoutNeedsUpgraded = new Translation<int>("S{0} loadout <#9cb6a4>requires upgrade</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label for when the kit is already equipped by the player.")]
    public readonly Translation StatusEquipped = new Translation("<#827d6d>Equipped</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label for when the kit is disabled, usually because of some exploit or bug.")]
    public readonly Translation StatusDisabled = new Translation("Temporarily Disabled", TranslationOptions.TMProUI);
    
    [TranslationData("Label for when the kit requires a Nitro Server Boost and the player isn't boosting or isn't linked.")]
    public readonly Translation StatusServerBoostRequired = new Translation("<#9b59b6>Nitro Boost Required</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label for when the kit isn't available on the current map.")]
    public readonly Translation<string> StatusFailsMapFilter = new Translation<string>("<#c2603e>Unavailable on</color>\n<#ddd>{0}</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label for when the kit isn't available on the current faction.")]
    public readonly Translation<FactionInfo> StatusFailsFactionFilter = new Translation<FactionInfo>("<#c2603e>Unavailable on</color>\n{0}", TranslationOptions.TMProUI, FactionInfo.FormatColorDisplayName);
    
    [TranslationData("Label for when the kit is able to be requested.")]
    public readonly Translation StatusUnlocked = new Translation("<#96ffb2>Unlocked</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label for when the kit requires the player to be a squad leader.")]
    public readonly Translation StatusSquadLeaderRequired = new Translation("<#c2603e>Squad Leader Required</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label for when the kit requires the player to be in a squad.")]
    public readonly Translation StatusSquadMemberRequired = new Translation("<#c2603e>Squad Required</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label for when the kit can not be used by more than one player.", "Player using the kit")]
    public readonly Translation<IPlayer> StatusTakenBySquadMember = new Translation<IPlayer>("<#c2603e>Taken by {0}</color>", TranslationOptions.TMProUI, WarfarePlayer.FormatColoredNickName);
    
    [TranslationData("Label for when the kit requires the player to be in a squad.", "Current squad member count", "Required squad member count")]
    public readonly Translation<int, int> StatusSquadMembersRequired = new Translation<int, int>("<#c2603e><#ddd>{0}</color> of <#ddd>{1}</color> required squad members</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label for when the kit is on a global or premium kit request cooldown.", "Time left on cooldown")]
    public readonly Translation<TimeSpan> StatusRequestCooldown = new Translation<TimeSpan>("<#c2603e>Request cooldown: <#fff>{0}</color></color>", TranslationOptions.TMProUI, TimeAddon.Create(TimeSpanFormatType.CountdownMinutesSeconds));
    
    [TranslationData("Label for when the kit requires the player to be in a squad.", "Class being limited", "Current squad member count", "Required squad member count")]
    public readonly Translation<Class, int, int> StatusTooManyTeammatesWithClass = new Translation<Class, int, int>(
        "<#c2603e>Too many {0} (<#ddd>{1}</color>)\n<#ddd>{2}</color> more ${p:2:teammate} needed</color>",
        TranslationOptions.TMProUI,
        new ArgumentFormat(LowercaseAddon.Instance, PluralAddon.Always())
    );

    [TranslationData("Label for the purchase button shown when the kit needs to be bought with credits.", "Cost in C", "Current balance in C", "Current balance - Cost")]
    public readonly Translation<double, double, double> PurchaseButtonCredits = new Translation<double, double, double>(
        "Buy for <#b8ffc1>C</color> <#fff>{0}</color>\n<#b8ffc1>C</color> <#fff>{1}</color> - <#b8ffc1>C</color> <#fff>{0}</color> = <#b8ffc1>C</color> <#fff>{2}</color>",
        TranslationOptions.TMProUI, "F0", "F0", "F0"
    );
    
    [TranslationData("Label for the purchase button shown when the kit is a loadout currently waiting for an upgrade.")]
    public readonly Translation PurchaseButtonViewLoadoutTicket = new Translation("View <#9cb6a4>loadout ticket</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label for the purchase button shown when the kit is a loadout that needs an upgrade.")]
    public readonly Translation PurchaseButtonOpenLoadoutTicket = new Translation("Create <#9cb6a4>loadout ticket</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label for the purchase button shown when the kit needs to be bought with credits.")]
    public readonly Translation<decimal> PurchaseButtonCurrency = new Translation<decimal>("Purchase for <#7878ff>$ {0}</color>\non our website.", TranslationOptions.TMProUI, arg0Fmt: "F2");
    
    [TranslationData("Label for the purchase button shown when the player needs to be a squad leader for the kit.")]
    public readonly Translation PurchaseButtonCreateSquad = new Translation("Create a <#f0a31c>Squad</color> to Equip", TranslationOptions.TMProUI);
    
    [TranslationData("Label for the purchase button shown when the player needs to be in a squad for the kit.")]
    public readonly Translation PurchaseButtonJoinSquad = new Translation("Join a <#f0a31c>Squad</color> to Equip", TranslationOptions.TMProUI);

    // https://discord.com/channels/645743633202544643/boosts
    [TranslationData("Label for the purchase button shown when the player is linked to discord but is not boosting.")]
    public readonly Translation PurchaseButtonNotBoostingOpenDiscord = new Translation("Open Discord", TranslationOptions.TMProUI);
    
    [TranslationData("Label for the purchase button shown when the player is not linked to discord.")]
    public readonly Translation PurchaseButtonNotBoostingLinkDiscord = new Translation("Link Discord Account", TranslationOptions.TMProUI);
    
    [TranslationData("Label for the purchase button shown when the player is linked to discord but not in the guild.")]
    public readonly Translation PurchaseButtonNotBoostingJoinDiscord = new Translation("Join Discord Server", TranslationOptions.TMProUI);
    
    [TranslationData("Shown in the ID section for loadouts owned by the viewing player.", "The letter (ex. 'A', 'AF', 'BC', 'F') of the loadout, formatted like an Excel column.")]
    public readonly Translation<string> LoadoutIdLabel = new Translation<string>("Loadout {0}", TranslationOptions.TMProUI | TranslationOptions.NoRichText);


    public Translation? DescriptionOfClass(Class c)
    {
        return c switch
        {
            Class.Squadleader => DescriptionSquadleader,
            Class.Rifleman => DescriptionRifleman,
            Class.Medic => DescriptionMedic,
            Class.Breacher => DescriptionBreacher,
            Class.AutomaticRifleman => DescriptionAutoRifleman,
            Class.Grenadier => DescriptionGrenadier,
            Class.MachineGunner => DescriptionMachineGunner,
            Class.LAT => DescriptionLAT,
            Class.HAT => DescriptionHAT,
            Class.Marksman => DescriptionMarksman,
            Class.Sniper => DescriptionSniper,
            Class.APRifleman => DescriptionAPRifleman,
            Class.CombatEngineer => DescriptionCombatEngineer,
            Class.Crewman => DescriptionCrewman,
            Class.Pilot => DescriptionPilot,
            Class.SpecOps => DescriptionSpecOps,
            _ => null
        };
    }

    
    [TranslationData("Description of the Squadleader class.")]
    public readonly Translation DescriptionSquadleader = new Translation(
        "Help your squad by supplying them with <#f0a31c>rally points</color> and placing <#f0a31c>FOB radios</color>.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Rifleman class.")]
    public readonly Translation DescriptionRifleman = new Translation(
        "Resupply your teammates in the field with an <#f0a31c>Ammo Bag</color>.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Medic class.")]
    public readonly Translation DescriptionMedic = new Translation(
        "<#f0a31c>Revive</color> your teammates after they've been injured.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Breacher class.")]
    public readonly Translation DescriptionBreacher = new Translation(
        "Use <#f0a31c>high-powered explosives</color> to take out <#f01f1c>enemy FOBs</color>.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Automatic Rifleman class.")]
    public readonly Translation DescriptionAutoRifleman = new Translation(
        "Equipped with a high-capacity and powerful <#f0a31c>LMG</color> to spray-and-pray your enemies.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Grenadier class.")]
    public readonly Translation DescriptionGrenadier = new Translation(
        "Equipped with a <#f0a31c>grenade launcher</color> to take out enemies behind cover or in light-armored vehicles.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Machine Gunner class.")]
    public readonly Translation DescriptionMachineGunner = new Translation(
        "Equipped with a powerful <#f0a31c>Machine Gun</color> to shred the enemy team in combat.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Light Anti-Tank class.")]
    public readonly Translation DescriptionLAT = new Translation(
        "A balance between an anti-tank and combat loadout, used to conveniently destroy <#f01f1c>armored enemy vehicles</color>.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Heavy Anti-Tank class.")]
    public readonly Translation DescriptionHAT = new Translation(
        "Equipped with multiple powerful <#f0a31c>anti-tank shells</color> to take out any vehicles.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Marksman class.")]
    public readonly Translation DescriptionMarksman = new Translation(
        "Equipped with a <#f0a31c>marksman rifle</color> to take out enemies from medium to high distances.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Sniper class.")]
    public readonly Translation DescriptionSniper = new Translation(
        "Equipped with a high-powered <#f0a31c>sniper rifle</color> to take out enemies from great distances.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Anti-Personnel Rifleman class.")]
    public readonly Translation DescriptionAPRifleman = new Translation(
        "Equipped with <#f0a31c>explosive traps</color> to cover entry-points and entrap enemy vehicles.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Combat Engineer class.")]
    public readonly Translation DescriptionCombatEngineer = new Translation(
        "Features 200% <#f0a31c>build speed</color> and are equipped with <#f0a31c>fortifications</color> and traps to help defend their team's FOBs.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Crewman class.")]
    public readonly Translation DescriptionCrewman = new Translation(
        "Gives users the ability to operate <#f0a31c>armored vehicles</color>.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Pilot class.")]
    public readonly Translation DescriptionPilot = new Translation(
        "Gives users the ability to fly <#f0a31c>aircraft</color>.",
        TranslationOptions.TMProUI
    );
    
    [TranslationData("Description of the Special Ops class.")]
    public readonly Translation DescriptionSpecOps = new Translation(
        "Equipped with <#f0a31c>night-vision</color> to help see at night.",
        TranslationOptions.TMProUI
    );
}