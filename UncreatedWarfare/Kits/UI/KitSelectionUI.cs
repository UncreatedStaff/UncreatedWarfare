using Humanizer;
using Stripe;
using System;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits.UI;

[UnturnedUI(BasePath = "Background")]
public sealed class KitSelectionUI : UnturnedUI, IHudUIListener
{
    private readonly IKitDataStore _kitDataStore;
    private readonly IKitAccessService _kitAccessService;
    private readonly IPlayerService _playerService;

    private Kit[]? _cachedPublicKits;
    private readonly Func<CSteamID, KitSelectionUIData> _getDataFunc;

    private bool _isHidden;

    private readonly UnturnedUIElement _root = new UnturnedUIElement("~/Background");
    private readonly UnturnedUIElement _switchToListLogic = new UnturnedUIElement("~/Logic_SwitchToList");
    private readonly UnturnedUIElement _switchToPanelLogic = new UnturnedUIElement("~/Logic_SwitchToPanel");

    private readonly PlaceholderTextBox _kitNameFilter = new PlaceholderTextBox("Filters/Viewport/Content/Kit_Search");

    private readonly UnturnedButton[] _classButtons = ElementPatterns.CreateArray<UnturnedButton>(
        i => new UnturnedButton($"Filters/Viewport/Content/Classes_Grid/Kits_Class_{EnumUtility.GetName((Class)i)}"),
        (int)Class.Squadleader,
        to: (int)Class.SpecOps
    );

    private readonly UnturnedButton _switchBackToPanel = new UnturnedButton("Kits_Back_To_Panel");
    private readonly UnturnedButton _close = new UnturnedButton("Kits_Close");

    private readonly KitPanel[] _panels = ElementPatterns.CreateArray<KitPanel>(
        "Public_Kit_Layout/Viewport/Content/Kit_Panel_{0}", (int)Class.Squadleader, to: (int)Class.SpecOps
    );

    public KitSelectionUI(
        ILoggerFactory loggerFactory,
        AssetConfiguration assetConfig,
        IKitDataStore kitDataStore,
        IKitAccessService kitAccessService,
        IPlayerService playerService
    )
        : base(
            loggerFactory,
            assetConfig.GetAssetLink<EffectAsset>("UI:KitSelectionUI"),
            staticKey: true
        )
    {
        _kitDataStore = kitDataStore;
        _kitAccessService = kitAccessService;
        _playerService = playerService;
        kitDataStore.KitUpdated += KitUpdated;
        kitDataStore.KitAdded += KitUpdated;
        kitDataStore.KitRemoved += KitModelUpdated;

        _getDataFunc = id => new KitSelectionUIData(id, this);

        _kitNameFilter.OnTextUpdated += OnKitFilterUpdated;

        _switchBackToPanel.OnClicked += (_, player) =>
        {
            // todo: prevent double click
            _switchToPanelLogic.Show(player);
        };

        ElementPatterns.SubscribeAll(_classButtons, OnClassFilterChosen);

        for (int i = 0; i < _panels.Length; ++i)
        {
            _panels[i].Class = (Class)(i + (int)Class.Squadleader);
        }
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
        _kitDataStore.KitRemoved -= KitModelUpdated;
    }

    private void KitUpdated(Kit _)
    {
        _cachedPublicKits = null;
    }

    private void KitModelUpdated(KitModel _)
    {
        _cachedPublicKits = null;
    }

    public async Task OpenAsync(WarfarePlayer player, uint factionId, CancellationToken token = default)
    {
        if (factionId == 0)
            factionId = player.Team.Faction.PrimaryKey;

        Kit[] publicKits = _cachedPublicKits ??= await _kitDataStore
            .QueryKitsAsync(
                KitInclude.UI,
                q => q.Where(k => k.Type == KitType.Public && k.FactionId == factionId && !k.Disabled && k.Class != Class.Unarmed && k.Class != Class.None)
                      .OrderBy(k => k.Class).ThenBy(k => k.Id),
                token
            )
            .ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        KitSelectionUIData data = GetOrAddData(player);
        if (_isHidden || data.IsHidden)
            return;

        if (!data.HasUI)
        {
            data.HasUI = true;
            SendToPlayer(player.SteamPlayer);
        }

        Class prevClass = Class.Unarmed;
        int panelIndex = -1;
        int kitIndex = 0;
        KitPlayerComponent kitComp = player.Component<KitPlayerComponent>();
        for (int i = 0; i < publicKits.Length; ++i)
        {
            Kit kit = publicKits[i];
            if (kit.Class != prevClass)
            {
                if ((int)prevClass % 3 == 0)
                {
                    await UniTask.NextFrame();
                }

                ++panelIndex;
                kitIndex = 0;
                prevClass = kit.Class;
            }

            if (panelIndex >= _panels.Length)
                break;

            KitPanel panel = _panels[panelIndex];
            ++kitIndex;
            if (kitIndex >= panel.Kits.Length)
                continue;

            KitInfo info = panel.Kits[kitIndex];
            SendKitInfo(info, player, kit, kitComp, fresh: true);
        }
    }

    private void SendKitInfo(KitInfo ui, WarfarePlayer player, Kit kit, KitPlayerComponent kitAccessComp, bool fresh)
    {
        ITransportConnection c = player.Connection;
        ui.Flag.SetText(c, kit.Faction.Sprite);
        ui.Class.SetText(c, kit.Class.GetIconString());
        ui.Name.SetText(c, kit.GetDisplayName(player.Locale.LanguageInfo, useIdFallback: true));
        ui.Playtime.SetText(c, "Playtime: 0hr"); // todo
        
        if (kitAccessComp.IsKitAccessible(kit.Key))
        {
            ui.PreviewButton.Hide(c);
            ui.RequestButton.Show(c);
        }
        else if (!fresh)
        {
            ui.RequestButton.Hide(c);
            ui.PreviewButton.Show(c);
        }

        if (kitAccessComp.IsKitFavorited(kit.Key))
        {
            ui.FavoriteButton.Hide(c);
            ui.UnfavoriteButton.Show(c);
        }
        else if (!fresh)
        {
            ui.UnfavoriteButton.Hide(c);
            ui.FavoriteButton.Show(c);
        }

        //kit.Items
    }

    private void OnKitFilterUpdated(UnturnedTextBox textBox, Player player, string text)
    {
        
    }

    private void OnClassFilterChosen(UnturnedButton button, Player player)
    {

    }

    public void Hide(WarfarePlayer? player)
    {
        if (player == null)
        {
            if (_isHidden)
                return;

            _isHidden = true;
            foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
            {
                KitSelectionUIData data = GetOrAddData(pl);
                if (data is { IsHidden: false, HasUI: true })
                    _root.Hide(pl);
            }
            return;
        }

        KitSelectionUIData plData = GetOrAddData(player);
        if (plData is { IsHidden: false, HasUI: true })
        {
            plData.IsHidden = true;
            _root.Hide(player);
        }
    }

    public void Restore(WarfarePlayer? player)
    {
        if (player == null)
        {
            if (!_isHidden)
                return;

            _isHidden = false;
            foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
            {
                KitSelectionUIData data = GetOrAddData(pl);
                if (data is { IsHidden: false, HasUI: true })
                    _root.Show(pl);
            }
            return;
        }

        KitSelectionUIData plData = GetOrAddData(player);
        if (plData is { IsHidden: true, HasUI: true })
        {
            plData.IsHidden = false;
            _root.Show(player);
        }
    }

    private class KitSelectionUIData : IUnturnedUIData
    {
        internal bool IsHidden;
        internal bool HasUI;

        public CSteamID Player { get; private set; }
        public UnturnedUI Owner { get; private set; }
        UnturnedUIElement? IUnturnedUIData.Element => null;

        public KitSelectionUIData(CSteamID player, KitSelectionUI owner)
        {
            Player = player;
            Owner = owner;
        }
    }

#nullable disable

    private class KitPanel
    {
        internal Class Class;

        [Pattern("Title", AdditionalPath = "Viewport/Content")]
        public UnturnedLabel Title { get; set; }

        [Pattern("Desc", AdditionalPath = "Viewport/Content")]
        public UnturnedLabel Description { get; set; }

        [Pattern("Kit_{0}", AdditionalPath = "Viewport/Content")]
        [ArrayPattern(1, To = 3)]
        public KitInfo[] Kits { get; set; }
    }

    private class KitInfo
    {
        [Pattern("Flag")]
        public UnturnedLabel Flag { get; set; }

        [Pattern("Class")]
        public UnturnedLabel Class { get; set; }

        [Pattern("Name")]
        public UnturnedLabel Name { get; set; }

        [Pattern("Playtime")]
        public UnturnedLabel Playtime { get; set; }

        [Pattern("Kit_Panel_{1}_Kit_{0}_Favorite", AdditionalPath = "Buttons")]
        public UnturnedButton FavoriteButton { get; set; }

        [Pattern("Kit_Panel_{1}_Kit_{0}_Unfavorite", AdditionalPath = "Buttons")]
        public UnturnedButton UnfavoriteButton { get; set; }

        [Pattern("Kit_Panel_{1}_Kit_{0}_Request", AdditionalPath = "Buttons")]
        public UnturnedButton RequestButton { get; set; }

        [Pattern("Kit_Panel_{1}_Kit_{0}_Preview", AdditionalPath = "Buttons")]
        public UnturnedButton PreviewButton { get; set; }

        [Pattern("Include_{0}")]
        [ArrayPattern(1, To = 15)]
        public CountIncludeLabel[] IncludeLabels { get; set; }

        [Pattern("Include_1_{0}")]
        [ArrayPattern(1, To = 4)]
        public UnturnedLabel[] PrimaryAttachments { get; set; }

        [Pattern("Include_2_{0}")]
        [ArrayPattern(1, To = 4)]
        public UnturnedLabel[] SecondaryAttachments { get; set; }

        [Pattern("Include_3_{0}")]
        [ArrayPattern(1, To = 4)]
        public UnturnedLabel[] TertiaryAttachments { get; set; }

        [Pattern("Status")]
        public UnturnedLabel StatusLabel { get; set; }

        [Pattern("Unlock")]
        public UnturnedUIElement UnlockButton { get; set; }
    }

    private class IncludeLabel
    {
        [Pattern("Icon")]
        public UnturnedLabel Icon { get; set; }

        [Pattern("Name")]
        public UnturnedLabel Name { get; set; }
    }

    private class CountIncludeLabel : IncludeLabel
    {
        [Pattern("Count")]
        public UnturnedLabel Count { get; set; }
    }

#nullable restore
}
