using System;
using System.Collections.Immutable;
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
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.UI;

[UnturnedUI(BasePath = "Background")]
public sealed class KitSelectionUI : UnturnedUI, IHudUIListener
{
    private readonly IKitDataStore _kitDataStore;
    private readonly IKitAccessService _kitAccessService;
    private readonly IPlayerService _playerService;
    private readonly IKitItemResolver _kitItemResolver;
    private readonly ItemIconProvider _iconProvider;
    private readonly IKitsDbContext _kitsDbContext;
    private readonly SemaphoreSlim _dbSemaphore;

    // maps AttachmentType -> UI array index
    private readonly int[] _attachmentMap =
    [
        1, -1,
        4, -1,
        3, -1,
        2, -1,
        0
    ];

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
        IPlayerService playerService,
        IKitItemResolver kitItemResolver,
        ItemIconProvider iconProvider,
        IKitsDbContext kitsDbContext
    )
        : base(
            loggerFactory,
            assetConfig.GetAssetLink<EffectAsset>("UI:KitSelectionUI"),
            staticKey: true
        )
    {
        _kitsDbContext = kitsDbContext;
        _dbSemaphore = new SemaphoreSlim(1, 1);

        _kitDataStore = kitDataStore;
        _kitAccessService = kitAccessService;
        _playerService = playerService;
        _kitItemResolver = kitItemResolver;
        _iconProvider = iconProvider;
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
                // only send 3 classes per frame
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
            SendKitInfo(info, player, kit, kitComp, fromDefaultValues: true);
        }
    }

    private void SendKitInfo(KitInfo ui, WarfarePlayer player, Kit kit, KitPlayerComponent kitAccessComp, bool fromDefaultValues)
    {
        ITransportConnection c = player.Connection;
        ui.Flag.SetText(c, kit.Faction.Sprite);
        ui.Class.SetText(c, kit.Class.GetIconString());
        ui.Name.SetText(c, kit.GetDisplayName(player.Locale.LanguageInfo, useIdFallback: true));
        ui.Playtime.SetText(c, "Playtime: ..."); // todo
        
        if (kitAccessComp.IsKitAccessible(kit.Key))
        {
            ui.PreviewButton.Hide(c);
            ui.RequestButton.Show(c);
        }
        else if (!fromDefaultValues)
        {
            ui.RequestButton.Hide(c);
            ui.PreviewButton.Show(c);
        }

        if (kitAccessComp.IsKitFavorited(kit.Key))
        {
            ui.FavoriteButton.Hide(c);
            ui.UnfavoriteButton.Show(c);
        }
        else if (!fromDefaultValues)
        {
            ui.UnfavoriteButton.Hide(c);
            ui.FavoriteButton.Show(c);
        }

        ImmutableArray<ItemDescriptor> itemDescriptors = kit.GetItemDescriptors(player.Team, _kitItemResolver, _iconProvider);
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
                        if ((mask & (1 << j)) != 0)
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

        if (!kitAccessComp.IsKitAccessible(kit.Key))
        {

        }


        ui.StatusLabel.SetText(c, "");
    }

    private string GetAttachmentIcon(AttachmentType attachmentType)
    {
        return GetAttachmentIcon(_attachmentMap[(int)attachmentType]);
    }

    private static string GetAttachmentIcon(int attachmentRowIndex)
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

    private class KitInfo : PatternRoot
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
        [ArrayPattern(1, To = 5)]
        public IncludeLabel[] PrimaryAttachments { get; set; }

        [Pattern("Include_2_{0}")]
        [ArrayPattern(1, To = 5)]
        public IncludeLabel[] SecondaryAttachments { get; set; }

        [Pattern("Include_3_{0}")]
        [ArrayPattern(1, To = 5)]
        public IncludeLabel[] TertiaryAttachments { get; set; }

        [Pattern("Status")]
        public UnturnedLabel StatusLabel { get; set; }

        [Pattern("Unlock")]
        public UnturnedUIElement UnlockButton { get; set; }
    }

    private class IncludeLabel : PatternRoot
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

internal sealed class KitSelectionUITranslations : PropertiesTranslationCollection
{
    protected override string FileName => "UI/Kit Selection";

    [TranslationData("Label for the page with all the public kits.")]
    public readonly Translation PublicKitsLabel = new Translation("Public Kits", TranslationOptions.TMProUI);

    [TranslationData("Default label for the page with kit search results.")]
    public readonly Translation SearchResultsLabel = new Translation("Search Results", TranslationOptions.TMProUI);

    [TranslationData("Default label for the page with kit search results when sorting by class.", "The class being filtered by.")]
    public readonly Translation<Class> SearchResultsByClassLabel = new Translation<Class>("Search Results - {0} kits", TranslationOptions.TMProUI);

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
    
    [TranslationData("Label for when the kit needs to be bought with credits or real money.")]
    public readonly Translation StatusNotPurchased = new Translation("Not Purchased", TranslationOptions.TMProUI);
    
    [TranslationData("Label for the purchase button shown when the kit needs to be bought with credits.")]
    public readonly Translation PurchaseButtonCredits = new Translation("Buy for <#b8ffc1>C</color> <#fff>0</color>\n<#b8ffc1>C</color> <#fff>0</color> - <#b8ffc1>C</color> <#fff>0</color> = <#b8ffc1>C</color> <#fff>0</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label for the purchase button shown when the kit needs to be bought with credits.")]
    public readonly Translation PurchaseButtonCurrency = new Translation("Buy for <#b8ffc1>C</color> <#fff>0</color>\n<#b8ffc1>C</color> <#fff>0</color> - <#b8ffc1>C</color> <#fff>0</color> = <#b8ffc1>C</color> <#fff>0</color>", TranslationOptions.TMProUI);

    
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