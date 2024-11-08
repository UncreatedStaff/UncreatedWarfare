using Microsoft.Extensions.DependencyInjection;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Kits;

[UnturnedUI(BasePath = "Background/BkgrMask")]
public class KitMenuUI : UnturnedUI
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPlayerService _playerService;
    private readonly ITranslationValueFormatter _valueFormatter;
    private readonly IFactionDataStore _factionDataStore;
    private readonly ItemIconProvider _itemIconProvider;
    private readonly ZoneStore _zoneStore;
    private readonly LanguageService _languageService;
    private readonly WarfareModule _warfare;
    private readonly KitMenuUITranslations _translations;

    private const bool RichIcons = true;
    private const EPluginWidgetFlags DisabledWidgets = EPluginWidgetFlags.ShowLifeMeters |
                                                       EPluginWidgetFlags.ShowCenterDot |
                                                       EPluginWidgetFlags.ShowInteractWithEnemy |
                                                       EPluginWidgetFlags.ShowVehicleStatus |
                                                       EPluginWidgetFlags.ShowUseableGunStatus |
                                                       EPluginWidgetFlags.ShowDeathMenu;

    private const EPluginWidgetFlags EnabledWidgets = EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur;

    public const int KitListCount = 40;
    public const int TabCount = 4;
    public const int IncludedItemsCount = 17;
    /* LOGIC */

    // this doesn't actually exist but is used to store UI data.
    public readonly UnturnedUIElement Parent = new UnturnedUIElement("~/root");

    // enable these to clear all, will disable themselves
    public readonly UnturnedUIElement LogicClearAll     = new UnturnedUIElement("~/anim_logic_clear_all");
    public readonly UnturnedUIElement LogicClearList    = new UnturnedUIElement("~/anim_logic_clear_list");
    public readonly UnturnedUIElement LogicClearKit     = new UnturnedUIElement("~/anim_logic_clear_kit");
    public readonly UnturnedUIElement LogicClearFilter  = new UnturnedUIElement("~/anim_logic_clear_filter");
    public readonly UnturnedUIElement LogicSetOpenState = new UnturnedUIElement("~/anim_logic_state_open");

    // set these as active/inactive to enable or disable target
    public readonly UnturnedUIElement LogicActionButton = new UnturnedUIElement("~/anim_logic_state_btn_action");
    public readonly UnturnedUIElement LogicStaff1Button = new UnturnedUIElement("~/anim_logic_state_btn_staff_1");
    public readonly UnturnedUIElement LogicStaff2Button = new UnturnedUIElement("~/anim_logic_state_btn_staff_2");
    public readonly UnturnedUIElement LogicStaff3Button = new UnturnedUIElement("~/anim_logic_state_btn_staff_3");

    /* TABS */

    // labels
    public readonly UnturnedLabel  LblTabBaseKits    = new UnturnedLabel("Tabs/Tab0/tab_base_kits");
    public readonly UnturnedLabel  LblTabEliteKits   = new UnturnedLabel("Tabs/Tab1/tab_elite_kits");
    public readonly UnturnedLabel  LblTabLoadouts    = new UnturnedLabel("Tabs/Tab2/tab_loadouts");
    public readonly UnturnedLabel  LblTabSpecialKits = new UnturnedLabel("Tabs/Tab3/tab_special_kits");

    // buttons
    public readonly UnturnedButton BtnTabBaseKits    = new UnturnedButton("Tabs/Tab0");
    public readonly UnturnedButton BtnTabEliteKits   = new UnturnedButton("Tabs/Tab1");
    public readonly UnturnedButton BtnTabLoadouts    = new UnturnedButton("Tabs/Tab2");
    public readonly UnturnedButton BtnTabSpecialKits = new UnturnedButton("Tabs/Tab3");
    public readonly UnturnedButton BtnTabClose       = new UnturnedButton("Tabs/Tab4");

    /* FILTER */
    
    // labels
    public readonly UnturnedLabel LblFilterTitle                = new UnturnedLabel("KitList/filter_title");

    // name of the selected class in the filter
    public readonly UnturnedLabel DropdownSelectedName          = new UnturnedLabel("KitList/dropdown_open_btn/dropdown_selected");
    public readonly UnturnedLabel DropdownSelectedClass         = new UnturnedLabel("KitList/dropdown_open_btn/dropdown_selected_class");

    // dropdown selection buttons
    public readonly UnturnedButton BtnDropdownNone              = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_none");
    public readonly UnturnedButton BtnDropdownAPRifleman        = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_aprifleman");
    public readonly UnturnedButton BtnDropdownAutomaticRifleman = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_automaticrifleman");
    public readonly UnturnedButton BtnDropdownBreacher          = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_breacher");
    public readonly UnturnedButton BtnDropdownCombatEngineer    = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_combatengineer");
    public readonly UnturnedButton BtnDropdownCrewman           = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_crewman");
    public readonly UnturnedButton BtnDropdownGrenadier         = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_grenadier");
    public readonly UnturnedButton BtnDropdownHAT               = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_hat");
    public readonly UnturnedButton BtnDropdownLAT               = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_lat");
    public readonly UnturnedButton BtnDropdownMachineGunner     = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_machinegunner");
    public readonly UnturnedButton BtnDropdownMarksman          = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_marksman");
    public readonly UnturnedButton BtnDropdownMedic             = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_medic");
    public readonly UnturnedButton BtnDropdownPilot             = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_pilot");
    public readonly UnturnedButton BtnDropdownRifleman          = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_rifleman");
    public readonly UnturnedButton BtnDropdownSniper            = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_sniper");
    public readonly UnturnedButton BtnDropdownSpecOps           = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_specops");
    public readonly UnturnedButton BtnDropdownSquadleader       = new UnturnedButton("KitList/dropdown_open_btn/filter_dropdown/dropdown_squadleader");

    /* KIT INFO */

    // labels
    public readonly UnturnedLabel LblInfoTitle   = new UnturnedLabel("KitInfo/kit_info_title");
    public readonly UnturnedLabel LblInfoFaction = new UnturnedLabel("KitInfo/kit_info_title/kit_info_layout/kit_info_faction/kit_info_faction_lbl");
    public readonly UnturnedLabel LblInfoClass   = new UnturnedLabel("KitInfo/kit_info_title/kit_info_layout/kit_info_class/kit_info_class_lbl");

    // values
    public readonly UnturnedLabel ValInfoFaction = new UnturnedLabel("KitInfo/kit_info_title/kit_info_layout/kit_info_faction/kit_info_faction_value");
    public readonly UnturnedLabel ValInfoClass   = new UnturnedLabel("KitInfo/kit_info_title/kit_info_layout/kit_info_class/kit_info_class_value");
    public readonly UnturnedLabel ValInfoType    = new UnturnedLabel("KitInfo/kit_info_title/kit_info_layout/kit_info_type");

    // icons
    public readonly UnturnedLabel LblInfoFactionFlag = new UnturnedLabel("KitInfo/kit_info_title/kit_info_layout/kit_info_faction/kit_info_faction_lbl/kit_info_faction_flag");
    public readonly UnturnedLabel LblInfoClassIcon   = new UnturnedLabel("KitInfo/kit_info_title/kit_info_layout/kit_info_class/kit_info_class_lbl/kit_info_class_icon");

    // Included Items
    public readonly UnturnedLabel LblInfoIncludedItems = new UnturnedLabel("KitInfo/kit_info_title/kit_info_layout/kit_info_included_items/kit_info_included_lbl");

    /* STATS */

    // labels
    public readonly UnturnedLabel LblStatsTitle                  = new UnturnedLabel("Stats/kit_stats_title");
    public readonly UnturnedLabel LblStatsPlaytime               = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_playtime/kit_info_playtime_lbl");
    public readonly UnturnedLabel LblStatsKills                  = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_kills/kit_info_kills_lbl");
    public readonly UnturnedLabel LblStatsDeaths                 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_deaths/kit_info_deaths_lbl");
    public readonly UnturnedLabel LblStatsPrimaryKills           = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_primary_kills/kit_info_primary_kills_lbl");
    public readonly UnturnedLabel LblStatsPrimaryAverageDistance = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_primary_avg_dist/kit_info_primary_avg_dist_lbl");
    public readonly UnturnedLabel LblStatsSecondaryKills         = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_secondary_kills/kit_info_secondary_kills_lbl");
    public readonly UnturnedLabel LblStatsDBNO                   = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_dbnos/kit_info_dbnos_lbl");
    public readonly UnturnedLabel LblStatsDistanceTraveled       = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_distance_traveled/kit_info_distance_traveled_lbl");
    public readonly UnturnedLabel LblStatsTicketsLost            = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_tickets_used/kit_info_tickets_used_lbl");
    public readonly UnturnedLabel LblStatsTicketsGained          = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_tickets_gained/kit_info_tickets_gained_lbl");
    public readonly UnturnedLabel LblStatsClass1                 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_class_1/kit_info_class_1_lbl");
    public readonly UnturnedLabel LblStatsClass2                 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_class_2/kit_info_class_2_lbl");
    public readonly UnturnedLabel LblStatsClass3                 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_class_3/kit_info_class_3_lbl");

    // values
    public readonly UnturnedLabel ValStatsPlaytime               = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_playtime/kit_info_playtime_value");
    public readonly UnturnedLabel ValStatsKills                  = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_kills/kit_info_kills_value");
    public readonly UnturnedLabel ValStatsDeaths                 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_deaths/kit_info_deaths_value");
    public readonly UnturnedLabel ValStatsPrimaryKills           = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_primary_kills/kit_info_primary_kills_value");
    public readonly UnturnedLabel ValStatsPrimaryAverageDistance = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_primary_avg_dist/kit_info_primary_avg_dist_value");
    public readonly UnturnedLabel ValStatsSecondaryKills         = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_secondary_kills/kit_info_secondary_kills_value");
    public readonly UnturnedLabel ValStatsDBNO                   = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_dbnos/kit_info_dbnos_value");
    public readonly UnturnedLabel ValStatsDistanceTravelled      = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_distance_traveled/kit_info_distance_traveled_value");
    public readonly UnturnedLabel ValStatsTicketsLost            = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_tickets_used/kit_info_tickets_used_value");
    public readonly UnturnedLabel ValStatsTicketsGained          = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_tickets_gained/kit_info_tickets_gained_value");
    public readonly UnturnedLabel ValStatsClass1                 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_class_1/kit_info_class_1_value");
    public readonly UnturnedLabel ValStatsClass2                 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_class_2/kit_info_class_2_value");
    public readonly UnturnedLabel ValStatsClass3                 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_class_3/kit_info_class_3_value");

    // parents
    public readonly UnturnedUIElement ObjStatsClass1 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_class_1");
    public readonly UnturnedUIElement ObjStatsClass2 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_class_2");
    public readonly UnturnedUIElement ObjStatsClass3 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_class_3");

    // separators
    public readonly UnturnedUIElement SeparatorStatsClass1 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_sep_10");
    public readonly UnturnedUIElement SeparatorStatsClass2 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_sep_11");
    public readonly UnturnedUIElement SeparatorStatsClass3 = new UnturnedLabel("Stats/kit_stats_title/kit_stats_layout/kit_stats_sep_12");

    /* ACTIONS */

    // labels
    public readonly UnturnedLabel LblActionsTitle = new UnturnedLabel("Actions/kit_actions_title");
    public readonly UnturnedLabel LblActionsActionButton = new UnturnedLabel("Actions/kit_actions_title/kit_actions_layout/kit_actions_action_btn/kit_actions_action_lbl");
    public readonly UnturnedLabel LblActionsStaff1Button = new UnturnedLabel("Actions/kit_actions_title/kit_actions_layout/kit_actions_staff_1/kit_actions_staff_1_text");
    public readonly UnturnedLabel LblActionsStaff2Button = new UnturnedLabel("Actions/kit_actions_title/kit_actions_layout/kit_actions_staff_2/kit_actions_staff_2_text");
    public readonly UnturnedLabel LblActionsStaff3Button = new UnturnedLabel("Actions/kit_actions_title/kit_actions_layout/kit_actions_staff_3/kit_actions_staff_3_text");

    // buttons
    public readonly UnturnedButton BtnActionsAction = new UnturnedButton("Actions/kit_actions_title/kit_actions_layout/kit_actions_action_btn");
    public readonly UnturnedButton BtnActionsStaff1 = new UnturnedButton("Actions/kit_actions_title/kit_actions_layout/kit_actions_staff_1");
    public readonly UnturnedButton BtnActionsStaff2 = new UnturnedButton("Actions/kit_actions_title/kit_actions_layout/kit_actions_staff_2");
    public readonly UnturnedButton BtnActionsStaff3 = new UnturnedButton("Actions/kit_actions_title/kit_actions_layout/kit_actions_staff_3");

    /* ARRAYS */

    // kit info
    public readonly KitInfoIncludedItem[] IncludedItems = ElementPatterns.CreateArray<KitInfoIncludedItem>("KitInfo/kit_info_title/kit_info_layout/kit_info_included_items/kit_included_{0}", 0, length: IncludedItemsCount);

    // kit list
    public readonly ListedKit[] Kits = ElementPatterns.CreateArray<ListedKit>("KitList/ScrollBox/Viewport/Content/kit_{0}", 1, length: KitListCount);
    public readonly UnturnedUIElement[] LogicSetTabs = ElementPatterns.CreateArray<UnturnedUIElement>("~/anim_logic_set_tab_{0}", 1, length: TabCount);

    public readonly UnturnedButton[] DropdownButtons;
    public readonly string[] DefaultClassCache;
    public readonly string[] ClassIconCache;

    public string[]? DefaultLanguageCache;

    public KitMenuUI(IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<ILoggerFactory>(), serviceProvider.GetRequiredService<AssetConfiguration>().GetAssetLink<EffectAsset>("UI:KitMenu"))
    {
        _serviceProvider = serviceProvider;
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _valueFormatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();
        _zoneStore = serviceProvider.GetRequiredService<ZoneStore>();
        _languageService = serviceProvider.GetRequiredService<LanguageService>();
        _factionDataStore = serviceProvider.GetRequiredService<IFactionDataStore>();
        _itemIconProvider = serviceProvider.GetRequiredService<ItemIconProvider>();
        _warfare = serviceProvider.GetRequiredService<WarfareModule>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitMenuUITranslations>>().Value;

        DropdownButtons = new UnturnedButton[(int)ClassConverter.MaxClass + 1];
        DefaultClassCache = new string[DropdownButtons.Length];
        ClassIconCache = new string[DropdownButtons.Length];
        DropdownButtons[(int)Class.None] = BtnDropdownNone;
        DropdownButtons[(int)Class.Unarmed] = BtnDropdownNone;
        DropdownButtons[(int)Class.APRifleman] = BtnDropdownAPRifleman;
        DropdownButtons[(int)Class.AutomaticRifleman] = BtnDropdownAutomaticRifleman;
        DropdownButtons[(int)Class.Breacher] = BtnDropdownBreacher;
        DropdownButtons[(int)Class.CombatEngineer] = BtnDropdownCombatEngineer;
        DropdownButtons[(int)Class.Crewman] = BtnDropdownCrewman;
        DropdownButtons[(int)Class.Grenadier] = BtnDropdownGrenadier;
        DropdownButtons[(int)Class.HAT] = BtnDropdownHAT;
        DropdownButtons[(int)Class.LAT] = BtnDropdownLAT;
        DropdownButtons[(int)Class.MachineGunner] = BtnDropdownMachineGunner;
        DropdownButtons[(int)Class.Marksman] = BtnDropdownMarksman;
        DropdownButtons[(int)Class.Medic] = BtnDropdownMedic;
        DropdownButtons[(int)Class.Pilot] = BtnDropdownPilot;
        DropdownButtons[(int)Class.Rifleman] = BtnDropdownRifleman;
        DropdownButtons[(int)Class.Sniper] = BtnDropdownSniper;
        DropdownButtons[(int)Class.SpecOps] = BtnDropdownSpecOps;
        DropdownButtons[(int)Class.Squadleader] = BtnDropdownSquadleader;
        for (int i = 1; i < DropdownButtons.Length; ++i)
        {
            DefaultClassCache[i] = (Class)i == Class.APRifleman ? "AP Rifleman" /* too long */ : _valueFormatter.FormatEnum((Class)i, null);
            ClassIconCache[i] = ((Class)i).GetIcon().ToString();
            if (DropdownButtons[i] is not { } btn)
                Logger!.LogWarning("DropdownButtons[{0}] was not initialized (class: {1}).", i, (Class)i);
            else
            {
                btn.OnClicked += OnClassButtonClicked;
            }
        }

        BtnActionsAction.OnClicked += OnActionButtonClicked;
        BtnActionsStaff1.OnClicked += OnStaffButton1Clicked;
        BtnActionsStaff2.OnClicked += OnStaffButton2Clicked;
        BtnActionsStaff3.OnClicked += OnStaffButton3Clicked;

        BtnTabBaseKits.OnClicked += OnTabClickedBaseKits;
        BtnTabEliteKits.OnClicked += OnTabClickedEliteKits;
        BtnTabLoadouts.OnClicked += OnTabClickedLoadouts;
        BtnTabSpecialKits.OnClicked += OnTabClickedSpecialKits;
        BtnTabClose.OnClicked += OnClosed;

        ElementPatterns.SubscribeAll(Kits, listedKit => listedKit.FavoriteButton, FavoriteClickedIntl);
        ElementPatterns.SubscribeAll(Kits, listedKit => listedKit.Root, KitClickedIntl);

        // Translation.OnReload += OnReload;
        CacheLanguages();
    }

    private KitManager GetKitManager()
    {
        return _warfare.ScopedProvider.Resolve<KitManager>();
    }

    private KitMenuUIData GetData(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        KitMenuUIData? data = UnturnedUIDataSource.GetData<KitMenuUIData>(player.Steam64, Parent);
        if (data != null)
            return data;

        data = new KitMenuUIData(this, Parent, player, _serviceProvider);
        UnturnedUIDataSource.AddData(data);
        return data;
    }

    private void KitClickedIntl(UnturnedButton button, Player player)
    {
        WarfarePlayer? ucp = _playerService.GetOnlinePlayer(player);
        if (ucp != null)
        {
            int index = Array.FindIndex(Kits, kit => kit.Root == button);
            if (index != -1)
                OnKitClicked(index, ucp);
        }
    }
    internal void OnFavoritesRefreshed(WarfarePlayer player)
    {
        if (!GetData(player).IsOpen)
            return;
        RefreshList(player);
    }
    private void FavoriteClickedIntl(UnturnedButton button, Player player)
    {
        WarfarePlayer? ucp = _playerService.GetOnlinePlayer(player);
        if (ucp == null)
            return;

        int index = Array.FindIndex(Kits, listedKit => listedKit.FavoriteButton == button);
        if (index != -1)
            OnFavoriteToggled(index, ucp);
    }
    /*
    private string GetClassIcon(Class @class)
    {
        byte c = (byte)@class;
        if (c < ClassIconCache.Length)
            return ClassIconCache[c];
        return new string(@class.GetIcon(), 1);
    }*/
    private void OnClosed(UnturnedButton button, Player player)
    {
        if (_playerService.GetOnlinePlayer(player) is { } pl)
        {
            GetData(pl).IsOpen = false;
        }

        player.enablePluginWidgetFlag(DisabledWidgets);
        player.disablePluginWidgetFlag(EnabledWidgets);
    }

    public void OpenUI(WarfarePlayer player)
    {
        ITransportConnection c = player.Connection;

        KitMenuUIData data = GetData(player);

        data.ActiveTeam = player.Team;
        if (!data.IsAlive)
        {
            SendToPlayer(c);
            if (player.Locale.LanguageInfo.IsDefault)
                SetCachedValues(c);
            else
                SetCachedValuesToOther(c, player.Locale.LanguageInfo);
            LogicSetTabs[0].SetVisibility(c, true);
            SwitchToTab(player, 0);
        }
        else
        {
            SwitchToTab(player, 0);
        }

        data.Tab = 0;
        data.IsOpen = true;

        // todo proper widget management
        player.UnturnedPlayer.disablePluginWidgetFlag(DisabledWidgets);
        player.UnturnedPlayer.enablePluginWidgetFlag(EnabledWidgets);
    }

    private string TranslateClass(Class @class, WarfarePlayer player)
    {
        int cl = (int)@class;
        if (player.Locale.LanguageInfo.IsDefault && DefaultClassCache.Length > cl)
            return DefaultClassCache[cl];

        return _valueFormatter.FormatEnum(@class, player.Locale.LanguageInfo);
    }

    private void OnReload()
    {
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            KitMenuUIData data = GetData(player);
            if (!data.IsAlive)
                continue;

            if (player.Locale.LanguageInfo.IsDefault)
                SetCachedValues(player.Connection);
            else
                SetCachedValuesToOther(player.Connection, player.Locale.LanguageInfo);

            if (!data.IsOpen)
                continue;

            LogicSetTabs[data.Tab].SetVisibility(player.Connection, true);
            SwitchToTab(player, data.Tab);
        }
    }

    private void SetCachedValuesToOther(ITransportConnection c, LanguageInfo language)
    {
        LblTabBaseKits.SetText(c, _translations.KitMenuUITabBaseKits.Translate(language));
        LblTabEliteKits.SetText(c, _translations.KitMenuUITabEliteKits.Translate(language));
        LblTabLoadouts.SetText(c, _translations.KitMenuUITabLoadouts.Translate(language));
        LblTabSpecialKits.SetText(c, _translations.KitMenuUITabSpecialKits.Translate(language));

        LblFilterTitle.SetText(c, _translations.KitMenuUIFilterLabel.Translate(language));
        LblInfoFaction.SetText(c, _translations.KitMenuUIFactionLabel.Translate(language));
        LblInfoClass.SetText(c, _translations.KitMenuUIClassLabel.Translate(language));
        LblInfoIncludedItems.SetText(c, _translations.KitMenuUIIncludedItemsLabel.Translate(language));

        LblStatsPlaytime.SetText(c, _translations.KitMenuUIPlaytimeLabel.Translate(language));
        LblStatsKills.SetText(c, _translations.KitMenuUIKillsLabel.Translate(language));
        LblStatsDeaths.SetText(c, _translations.KitMenuUIDeathsLabel.Translate(language));
        LblStatsPrimaryKills.SetText(c, _translations.KitMenuUIPrimaryKillsLabel.Translate(language));
        LblStatsPrimaryAverageDistance.SetText(c, _translations.KitMenuUIPrimaryAvgDstLabel.Translate(language));
        LblStatsSecondaryKills.SetText(c, _translations.KitMenuUISecondaryKillsLabel.Translate(language));
        LblStatsDBNO.SetText(c, _translations.KitMenuUIDBNOLabel.Translate(language));
        LblStatsDistanceTraveled.SetText(c, _translations.KitMenuUIDistanceTraveledLabel.Translate(language));
        LblStatsTicketsLost.SetText(c, _translations.KitMenuUITicketsLostLabel.Translate(language));
        LblStatsTicketsGained.SetText(c, _translations.KitMenuUITicketsGainedLabel.Translate(language));

        LblStatsTitle.SetText(c, _translations.KitMenuUIStatsLabel.Translate(language));
        LblActionsTitle.SetText(c, _translations.KitMenuUIActionsLabel.Translate(language));
    }
    private void SetCachedValues(ITransportConnection c)
    {
        if (DefaultLanguageCache == null)
            CacheLanguages();
        LblTabBaseKits.SetText(c, DefaultLanguageCache![0]);
        LblTabEliteKits.SetText(c, DefaultLanguageCache[1]);
        LblTabLoadouts.SetText(c, DefaultLanguageCache[2]);
        LblTabSpecialKits.SetText(c, DefaultLanguageCache[3]);

        LblFilterTitle.SetText(c, DefaultLanguageCache[4]);
        LblInfoFaction.SetText(c, DefaultLanguageCache[5]);
        LblInfoClass.SetText(c, DefaultLanguageCache[6]);
        LblInfoIncludedItems.SetText(c, DefaultLanguageCache[7]);

        LblStatsPlaytime.SetText(c, DefaultLanguageCache[12]);
        LblStatsKills.SetText(c, DefaultLanguageCache[13]);
        LblStatsDeaths.SetText(c, DefaultLanguageCache[14]);
        LblStatsPrimaryKills.SetText(c, DefaultLanguageCache[15]);
        LblStatsPrimaryAverageDistance.SetText(c, DefaultLanguageCache[16]);
        LblStatsSecondaryKills.SetText(c, DefaultLanguageCache[17]);
        LblStatsDBNO.SetText(c, DefaultLanguageCache[18]);
        LblStatsDistanceTraveled.SetText(c, DefaultLanguageCache[19]);
        LblStatsTicketsLost.SetText(c, DefaultLanguageCache[20]);
        LblStatsTicketsGained.SetText(c, DefaultLanguageCache[21]);

        LblStatsTitle.SetText(c, DefaultLanguageCache[22]);
        LblActionsTitle.SetText(c, DefaultLanguageCache[23]);
    }

    private void SwitchToTab(WarfarePlayer player, byte tab)
    {
        Logger!.LogDebug("Switching to tab {0}.", tab);
        if (tab >= TabCount) return;

        KitMenuUIData data = GetData(player);

        data.IsOpen = true;
        data.Tab = tab;
        RefreshList(player);

        if (data.Filter == Class.None)
        {
            DropdownSelectedName.SetText(player.Connection, string.Empty);
            DropdownSelectedClass.SetText(player.Connection, string.Empty);
        }
        else
        {
            DropdownSelectedName.SetText(player.Connection, TranslateClass(data.Filter, player));
            DropdownSelectedClass.SetText(player.Connection, data.Filter.GetIcon().ToString());
        }
    }

    private void RefreshSelected(WarfarePlayer player)
    {
        uint? selected = GetData(player).SelectedKit;
        if (!selected.HasValue)
        {
            LogicClearKit.SetVisibility(player.Connection, true);
            return;
        }

        Task.Run(async () =>
        {
            Kit? kit = await GetKitManager().GetKit(selected.Value, player.DisconnectToken);

            await UniTask.SwitchToMainThread(player.DisconnectToken);

            if (kit == null)
            {
                LogicClearKit.SetVisibility(player.Connection, true);
            }
            else
            {
                OpenKit(player, kit);
            }
        });
    }

    private void OpenKit(WarfarePlayer player, Kit kit)
    {
        if (DefaultLanguageCache == null)
            CacheLanguages();
        Logger!.LogDebug("Opening kit: {0}.", kit.InternalName);
        ITransportConnection c = player.Connection;
        LblInfoTitle.SetText(c, kit.GetDisplayName(_languageService, player.Locale.LanguageInfo).Replace('\n', ' ').Replace("\r", string.Empty));

        FactionInfo? faction = kit.FactionId.HasValue ? _factionDataStore.FindFaction(kit.FactionId.Value) : null;

        ValInfoFaction.SetText(c, faction?.GetShortName(player.Locale.LanguageInfo) ?? (DefaultLanguageCache != null && player.Locale.LanguageInfo.IsDefault
                ? DefaultLanguageCache[29]
                : _translations.KitMenuUINoFaction.Translate(player)));

        LblInfoFactionFlag.SetText(c, faction.GetFlagIcon());
        ValInfoClass.SetText(c, TranslateClass(kit.Class, player));
        LblInfoClassIcon.SetText(c, kit.Class.GetIcon().ToString());
        
        ValInfoType.SetText(c, GetTypeString(player, kit.Type));

        List<SimplifiedItemListEntry> groups = kit.SimplifiedItemList;
        int index = 0;
        for (int i = 0; i < groups.Count; ++i)
        {
            if (index >= IncludedItemsCount)
                break;
            SimplifiedItemListEntry grp = groups[i];
            string icon;
            string name;
            int amt = grp.Count;
            if (grp.Asset != null)
            {
                if (!_itemIconProvider.TryGetIcon(grp.Asset, out icon, RichIcons, true))
                {
                    if (grp.Asset is ItemMagazineAsset)
                        icon = _itemIconProvider.GetIcon(RedirectType.StandardAmmoIcon, RichIcons, true);
                    else if (grp.Asset is ItemMeleeAsset)
                        icon = _itemIconProvider.GetIcon(RedirectType.StandardMeleeIcon, RichIcons, true);
                    else if (grp.Asset is ItemThrowableAsset throwable)
                    {
                        if (throwable.isExplosive)
                            icon = _itemIconProvider.GetIcon(RedirectType.StandardGrenadeIcon, RichIcons, true);
                        else if (throwable.itemName.IndexOf("smoke", StringComparison.InvariantCultureIgnoreCase) != -1)
                            icon = _itemIconProvider.GetIcon(RedirectType.StandardSmokeGrenadeIcon, RichIcons, true);
                    }
                    else if (grp.Asset is ItemClothingAsset cloth)
                    {
                        RedirectType type = cloth.type switch
                        {
                            EItemType.HAT => RedirectType.Hat,
                            EItemType.PANTS => RedirectType.Pants,
                            EItemType.SHIRT => RedirectType.Shirt,
                            EItemType.MASK => RedirectType.Mask,
                            EItemType.BACKPACK => RedirectType.Backpack,
                            EItemType.VEST => RedirectType.Vest,
                            EItemType.GLASSES => RedirectType.Glasses,
                            _ => RedirectType.None
                        };
                        if (type != RedirectType.None)
                            icon = _itemIconProvider.GetIcon(type, RichIcons, true);
                    }
                }
                name = grp.Asset.FriendlyName;
            }
            else if (grp.RedirectType != RedirectType.None)
            {
                icon = _itemIconProvider.GetIcon(grp.RedirectType, RichIcons, true);
                name = _valueFormatter.FormatEnum(grp.RedirectType, player.Locale.LanguageInfo);
            }
            else if (grp.ClothingSetName != null)
            {
                icon = _itemIconProvider.GetIcon(RedirectType.Shirt, RichIcons, true);
                name = grp.ClothingSetName + " Set";
                amt = 1;
            }
            else continue;

            KitInfoIncludedItem includedItem = IncludedItems[index];
            includedItem.Root.SetVisibility(c, true);
            includedItem.Icon.SetText(c, icon);
            includedItem.Text.SetText(c, name);
            if (amt != 1)
            {
                includedItem.Amount.SetText(c, "x" + amt.ToString(player.Locale.CultureInfo));
                includedItem.Amount.SetVisibility(c, true);
            }
            else
            {
                includedItem.Amount.SetVisibility(c, false);
            }
            ++index;
        }
        for (; index < IncludedItems.Length; ++index)
        {
            IncludedItems[index].Root.SetVisibility(c, false);
        }

        // WarfareStats? stats = StatsManager.OnlinePlayers.FirstOrDefault(x => x.Steam64 == player.Steam64);
        // if (stats != null && stats.Kits.FirstOrDefault(x => x.KitID.Equals(kit.InternalName, StringComparison.Ordinal)) is { } kitStats)
        // {
        //     ValStatsKills.SetText(c, kitStats.Kills.ToString(player.Locale.CultureInfo));
        //     ValStatsDeaths.SetText(c, kitStats.Deaths.ToString(player.Locale.CultureInfo));
        //     ValStatsPrimaryAverageDistance.SetText(c, kitStats.AverageGunKillDistance.ToString("0.#", player.Locale.CultureInfo) + " m");
        //     ValStatsPlaytime.SetText(c, Localization.GetTimeFromMinutes(((int)kitStats.PlaytimeMinutes), player));
        // }

        if (_zoneStore.IsInMainBase(player))
        {
            if (kit.IsRequestable(player.Team.Faction))
            {
                if (kit is { IsPublicKit: true, CreditCost: <= 0 } || (GetKitManager().HasAccessQuick(kit, player)))
                    LblActionsActionButton.SetText(c, DefaultLanguageCache![24]);
                else
                    LblActionsActionButton.SetText(c, DefaultLanguageCache![30]);
            }
            else
                LblActionsActionButton.SetText(c, DefaultLanguageCache![30]);
            LogicActionButton.SetVisibility(c, true);
        }
        else
        {
            LogicActionButton.SetVisibility(c, false);
            LblActionsActionButton.SetText(c, DefaultLanguageCache![25]);
        }

        if (true /* player.OnDuty() */)
        {
            BtnActionsStaff1.SetVisibility(c, true);
            LblActionsStaff1Button.SetText(c, DefaultLanguageCache![26]);
        }
        else
        {
            BtnActionsStaff1.SetVisibility(c, false);
        }
    }

    private string GetTypeString(WarfarePlayer player, KitType type)
    {
        if (DefaultLanguageCache != null && player.Locale.LanguageInfo.IsDefault)
            return DefaultLanguageCache[
                type switch { KitType.Public => 8, KitType.Elite => 9, KitType.Loadout => 10, _ => 11 }
            ];
        return (type switch
        {
            KitType.Public  => _translations.KitMenuUIKitTypeLabelPublic,
            KitType.Elite   => _translations.KitMenuUIKitTypeLabelElite,
            KitType.Loadout => _translations.KitMenuUIKitTypeLabelLoadout,
            _               => _translations.KitMenuUIKitTypeLabelSpecial
        }).Translate(player.Locale.LanguageInfo);
    }

    private void RefreshList(WarfarePlayer player)
    {
        GetData(player).RefreshKitList();
        SendKitList(player);
        RefreshSelected(player);
    }

    private void SendKitList(WarfarePlayer player)
    {
        LogicClearList.SetVisibility(player.Connection, true);
        KitMenuUIData data = GetData(player);
        for (int i = 0; i < data.Kits.Length; ++i)
        {
            if (data.Kits[i] is { } kit)
            {
                SendKit(player, i, kit, data.Favorited[i]);
            }
        }
    }

    private void SendKit(WarfarePlayer player, int index, Kit kit, bool favorited)
    {
        ITransportConnection c = player.Connection;

        bool hasAccess = GetKitManager().HasAccessQuick(kit, player);
        ListedKit kitUi = Kits[index];
        if (kit.Type != KitType.Loadout)
            kitUi.FavoriteIcon.SetText(c, favorited ? "<#fd0>¼" : "¼");
        kitUi.Weapon.SetText(c, kit.WeaponText ?? string.Empty);
        kitUi.Id.SetText(c, kit.InternalName);
        kitUi.Name.SetText(c, kit.GetDisplayName(_languageService, player.Locale.LanguageInfo).Replace('\n', ' ').Replace("\r", string.Empty));
        kitUi.Status.SetText(c, /* hasAccess ? Gamemode.Config.UIIconPlayer.ToString() : */string.Empty);
        kitUi.Class.SetText(c, kit.Class.GetIcon().ToString());
        kitUi.Flag.SetText(c, kit.Faction.GetFlagIcon());
        kitUi.FavoriteButton.SetVisibility(c, kit.Type != KitType.Loadout);
        
        Kits[index].Root.SetVisibility(c, true);
    }
    private void CacheLanguages()
    {
        LanguageInfo lang = _languageService.GetDefaultLanguage();

        const int length = 31;
        if (DefaultLanguageCache is not { Length: length })
            DefaultLanguageCache = new string[length];
        for (int i = 0; i < DefaultClassCache.Length; ++i)
            DefaultClassCache[i] = _valueFormatter.FormatEnum((Class)i, lang);
        
        DefaultLanguageCache[0]  = _translations.KitMenuUITabBaseKits.Translate(lang);
        DefaultLanguageCache[1]  = _translations.KitMenuUITabEliteKits.Translate(lang);
        DefaultLanguageCache[2]  = _translations.KitMenuUITabLoadouts.Translate(lang);
        DefaultLanguageCache[3]  = _translations.KitMenuUITabSpecialKits.Translate(lang);
                                 
        DefaultLanguageCache[4]  = _translations.KitMenuUIFilterLabel.Translate(lang);
        DefaultLanguageCache[5]  = _translations.KitMenuUIFactionLabel.Translate(lang);
        DefaultLanguageCache[6]  = _translations.KitMenuUIClassLabel.Translate(lang);
        DefaultLanguageCache[7]  = _translations.KitMenuUIIncludedItemsLabel.Translate(lang);
        DefaultLanguageCache[8]  = _translations.KitMenuUIKitTypeLabelPublic.Translate(lang);
        DefaultLanguageCache[9]  = _translations.KitMenuUIKitTypeLabelElite.Translate(lang);
        DefaultLanguageCache[10] = _translations.KitMenuUIKitTypeLabelSpecial.Translate(lang);
        DefaultLanguageCache[11] = _translations.KitMenuUIKitTypeLabelLoadout.Translate(lang);

        DefaultLanguageCache[12] = _translations.KitMenuUIPlaytimeLabel.Translate(lang);
        DefaultLanguageCache[13] = _translations.KitMenuUIKillsLabel.Translate(lang);
        DefaultLanguageCache[14] = _translations.KitMenuUIDeathsLabel.Translate(lang);
        DefaultLanguageCache[15] = _translations.KitMenuUIPrimaryKillsLabel.Translate(lang);
        DefaultLanguageCache[16] = _translations.KitMenuUIPrimaryAvgDstLabel.Translate(lang);
        DefaultLanguageCache[17] = _translations.KitMenuUISecondaryKillsLabel.Translate(lang);
        DefaultLanguageCache[18] = _translations.KitMenuUIDBNOLabel.Translate(lang);
        DefaultLanguageCache[19] = _translations.KitMenuUIDistanceTraveledLabel.Translate(lang);
        DefaultLanguageCache[20] = _translations.KitMenuUITicketsLostLabel.Translate(lang);
        DefaultLanguageCache[21] = _translations.KitMenuUITicketsGainedLabel.Translate(lang);

        DefaultLanguageCache[22] = _translations.KitMenuUIStatsLabel.Translate(lang);
        DefaultLanguageCache[23] = _translations.KitMenuUIActionsLabel.Translate(lang);
        DefaultLanguageCache[24] = _translations.KitMenuUIActionRequestKitLabel.Translate(lang);
        DefaultLanguageCache[25] = _translations.KitMenuUIActionNotInMainKitLabel.Translate(lang);
        DefaultLanguageCache[26] = _translations.KitMenuUIActionGiveKitLabel.Translate(lang);
        DefaultLanguageCache[27] = _translations.KitMenuUIActionEditKitLabel.Translate(lang);
        DefaultLanguageCache[28] = _translations.KitMenuUIActionSetLoadoutItemsLabel.Translate(lang);
        DefaultLanguageCache[29] = _translations.KitMenuUINoFaction.Translate(lang);
        DefaultLanguageCache[30] = _translations.KitMenuUIActionNoAccessLabel.Translate(lang);
    }
    private void OnClassButtonClicked(UnturnedButton button, Player player)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);

        Class @class = (Class)Array.IndexOf(DropdownButtons, button);
        if (@class > ClassConverter.MaxClass)
            return;
        ITransportConnection tc = player.channel.owner.transportConnection;

        GetData(ucp).Filter = @class;
        RefreshList(ucp);
        if (@class == Class.None)
        {
            DropdownSelectedName.SetText(tc, string.Empty);
            DropdownSelectedClass.SetText(tc, string.Empty);
            return;
        }

        // update dropdown text to translated value
        string text = ucp.Locale.LanguageInfo.IsDefault ? DefaultClassCache[(int)@class] : _valueFormatter.FormatEnum(@class, ucp.Locale.LanguageInfo);

        DropdownSelectedName.SetText(tc, text);
        DropdownSelectedClass.SetText(tc, new string(@class.GetIcon(), 1));
    }
    private void OnKitClicked(int kitIndex, WarfarePlayer player)
    {
        KitMenuUIData data = GetData(player);
        if (data.Kits.Length > kitIndex && data.Kits[kitIndex] is { } kit)
            OpenKit(player, kit);
    }
    private void OnFavoriteToggled(int kitIndex, WarfarePlayer player)
    {
        KitMenuUIData data = GetData(player);
        if (data.Kits.Length <= kitIndex || data.Kits[kitIndex] is not { } kit || kit.Type == KitType.Loadout)
            return;

        bool fav = data.Favorited[kitIndex] = !data.Favorited[kitIndex];
        Kits[kitIndex].FavoriteIcon.SetText(player.Connection, fav ? "<#fd0>¼" : "¼");
        data.FavoritesDirty = true;
        Logger!.LogDebug((fav ? "Favorited " : "Unfavorited ") + kit.InternalName);
        if (fav)
            (data.FavoriteKits ??= new List<uint>(8)).Add(kit.PrimaryKey);
        else
            data.FavoriteKits?.RemoveAll(x => x == kit.PrimaryKey);
    }
    private void OnActionButtonClicked(UnturnedButton button, Player player)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);

        if (!_zoneStore.IsInMainBase(ucp))
        {
            LogicActionButton.SetVisibility(ucp.Connection, false);
            return;
        }

        uint? selectedKitPk = GetData(ucp).SelectedKit;

        if (!selectedKitPk.HasValue)
            return;

        CancellationToken tkn = ucp.DisconnectToken;
        Task.Run(async () =>
        {
            Kit? kit = null;
            try
            {
                KitManager kitManager = GetKitManager();
                kit = await kitManager.GetKit(selectedKitPk.Value, tkn, x => KitManager.RequestableSet(x, true)).ConfigureAwait(false);
                if (kit == null)
                    return;

                await kitManager.Requests.RequestKit(kit, CommandContext.CreateTemporary(ucp, _serviceProvider), tkn);
            }
            catch (Exception ex)
            {
                Logger!.LogError(ex, "Error requesting kit {0} ({1}).", selectedKitPk, kit?.InternalName ?? "unknown kit");
            }
        });
    }

    private void OnStaffButton1Clicked(UnturnedButton button, Player player)
    {
        // todo check permission, determine action, perform action
    }
    private void OnStaffButton2Clicked(UnturnedButton button, Player player)
    {
        // todo check permission, determine action, perform action
    }
    private void OnStaffButton3Clicked(UnturnedButton button, Player player)
    {
        // todo check permission, determine action, perform action
    }
    private void OnTabClickedBaseKits(UnturnedButton button, Player player)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        SwitchToTab(ucp, 0);
    }
    private void OnTabClickedEliteKits(UnturnedButton button, Player player)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        SwitchToTab(ucp, 1);
    }
    private void OnTabClickedLoadouts(UnturnedButton button, Player player)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        SwitchToTab(ucp, 2);
    }
    private void OnTabClickedSpecialKits(UnturnedButton button, Player player)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        SwitchToTab(ucp, 3);
    }

    public class ListedKit
    {
        [Pattern(Root = true)]
        public UnturnedButton Root { get; set; }

        [Pattern("flag_", Mode = FormatMode.Prefix)]
        public UnturnedLabel Flag { get; set; }

        [Pattern("name_", Mode = FormatMode.Prefix)]
        public UnturnedLabel Name { get; set; }

        [Pattern("weapon_", Mode = FormatMode.Prefix)]
        public UnturnedLabel Weapon { get; set; }

        [Pattern("id_", Mode = FormatMode.Prefix)]
        public UnturnedLabel Id { get; set; }

        [Pattern("btn_fav_", Mode = FormatMode.Prefix)]
        public UnturnedButton FavoriteButton { get; set; }

        [Pattern("txt_fav_", AdditionalPath = "btn_fav_kit_{0}", Mode = FormatMode.Prefix)]
        public UnturnedLabel FavoriteIcon { get; set; }

        [Pattern("class_", Mode = FormatMode.Prefix)]
        public UnturnedLabel Class { get; set; }

        [Pattern("status_", Mode = FormatMode.Prefix)]
        public UnturnedLabel Status { get; set; }
    }

    public class KitInfoIncludedItem
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("kit_included_text_{0}")]
        public UnturnedLabel Text { get; set; }

        [Pattern("kit_included_icon_{0}")]
        public UnturnedLabel Icon { get; set; }

        [Pattern("kit_included_amt_{0}")]
        public UnturnedLabel Amount { get; set; }
    }
}

public sealed class KitMenuUIData : IUnturnedUIData
{
    private readonly IServiceProvider _serviceProvider;
    private FactionInfo? _faction;
    private KitManager? _manager;

    public WarfarePlayer Player { get; set; }
    public UnturnedUI Owner { get; set; }
    public UnturnedUIElement Element { get; set; }
    CSteamID IUnturnedUIData.Player => Player.Steam64;

    public Team ActiveTeam { get; set; }
    public byte Tab { get; set; }
    public Class Filter { get; set; }
    public uint? SelectedKit { get; set; }
    public bool IsAlive { get; set; }
    public bool IsOpen { get; set; }
    public bool FavoritesDirty { get; set; }
    public Kit[] Kits { get; set; } = Array.Empty<Kit>();
    public List<uint>? FavoriteKits { get; set; }
    public bool[] Favorited { get; set; } = Array.Empty<bool>();

    public KitMenuUIData(UnturnedUI owner, UnturnedUIElement element, WarfarePlayer player, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Owner = owner;
        Element = element;
        Player = player;
    }
    internal void RefreshKitList()
    {
        KitManager manager = _serviceProvider.GetRequiredService<KitManager>();

        _faction = Player.Team.Faction;
        _manager = manager;

        Func<Kit, bool> predicate = Tab switch
        {
            0 => KitListBasePredicate,
            1 => KitListElitePredicate,
            2 => KitListLoadoutPredicate,
            3 => KitListSpecialPredicate,
            _ => _ => true
        };
//#if DEBUG
//        using IDisposable disp = L.IndentLog(1);
//#endif

        //L.LogDebug(manager.Cache.KitDataByKey.Count + " searched... ");
        Kits = manager.Cache.KitDataByKey.Values.Where(predicate)
            .OrderByDescending(x => FavoriteKits?.Contains(x.PrimaryKey))
            .ThenBy(x =>
            {
                string dn = x.GetDisplayName(null);
                if (dn.Length <= 0 || char.IsDigit(dn[0]))
                    dn = "ZZ" + dn;
                return dn;
            }).ToArray();

        //L.LogDebug(Kits.Length + " selected for tab " + Tab + "... ");

        if (Kits.Length > KitMenuUI.KitListCount)
        {
            Kit[] old = Kits;
            Kits = new Kit[KitMenuUI.KitListCount];
            Array.Copy(old, Kits, KitMenuUI.KitListCount);
        }

        if (Favorited.Length != Kits.Length)
            Favorited = new bool[Kits.Length];
        for (int i = 0; i < Kits.Length; ++i)
            Favorited[i] = FavoriteKits != null && FavoriteKits.Contains(Kits[i].PrimaryKey);

        _manager = null;
        _faction = null;
    }

    private bool KitListBasePredicate(Kit x)
        => x is { Type: KitType.Public } && (Filter == Class.None || x.Class == Filter) && x.Class > Class.Unarmed && x.IsRequestable(_faction);
    private bool KitListElitePredicate(Kit x)
        => x is { Type: KitType.Elite } && (Filter == Class.None || x.Class == Filter) && x.Class > Class.Unarmed && x.IsRequestable(_faction);
    private bool KitListLoadoutPredicate(Kit x)
        => x is { Type: KitType.Loadout } && (Filter == Class.None || x.Class == Filter) && x is { Class: > Class.Unarmed, Requestable: true } &&
           (/* todo Player.OnDuty() && Player.Equals(x.Creator) || */ _manager!.HasAccessQuick(x, Player!));
    private bool KitListSpecialPredicate(Kit x)
        => x is { Type: KitType.Special } && (Filter == Class.None || x.Class == Filter) && x.Class > Class.Unarmed && x.IsRequestable(_faction)
           && (/* todo Player.OnDuty() || */_manager!.HasAccessQuick(x, Player!));
}

public class KitMenuUITranslations : PropertiesTranslationCollection
{
    protected override string FileName => "UI/Kit Menu";

    
    [TranslationData("Text that goes on the base kits tab.")]
    public readonly Translation KitMenuUITabBaseKits = new Translation("Base Kits", TranslationOptions.TMProUI);
    
    [TranslationData("Text that goes on the elite kits tab.")]
    public readonly Translation KitMenuUITabEliteKits = new Translation("Elite Kits", TranslationOptions.TMProUI);
    
    [TranslationData("Text that goes on the loadouts tab.")]
    public readonly Translation KitMenuUITabLoadouts = new Translation("Loadouts", TranslationOptions.TMProUI);
    
    [TranslationData("Text that goes on the special kits tab.")]
    public readonly Translation KitMenuUITabSpecialKits = new Translation("Special Kits", TranslationOptions.TMProUI);

    
    [TranslationData("Label that goes in front of the filter dropdown.")]
    public readonly Translation KitMenuUIFilterLabel = new Translation("Filter", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of the faction in kit info.")]
    public readonly Translation KitMenuUIFactionLabel = new Translation("Faction", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of the class in kit info.")]
    public readonly Translation KitMenuUIClassLabel = new Translation("Class", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of the included items list in kit info.")]
    public readonly Translation KitMenuUIIncludedItemsLabel = new Translation("Included Items", TranslationOptions.TMProUI);
    
    [TranslationData("Value for kit type (KitType.Public).")]
    public readonly Translation KitMenuUIKitTypeLabelPublic = new Translation("Public Kit", TranslationOptions.TMProUI);
    
    [TranslationData("Value for kit type (KitType.Elite).")]
    public readonly Translation KitMenuUIKitTypeLabelElite = new Translation("Elite Kit", TranslationOptions.TMProUI);
    
    [TranslationData("Value for kit type (KitType.Special).")]
    public readonly Translation KitMenuUIKitTypeLabelSpecial = new Translation("Special/Event Kit", TranslationOptions.TMProUI);
    
    [TranslationData("Value for kit type (KitType.Loadout).")]
    public readonly Translation KitMenuUIKitTypeLabelLoadout = new Translation("Custom Loadout", TranslationOptions.TMProUI);

    
    [TranslationData("Label that goes in front of playtime in kit stats.")]
    public readonly Translation KitMenuUIPlaytimeLabel = new Translation("Playtime", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of total kills in kit stats.")]
    public readonly Translation KitMenuUIKillsLabel = new Translation("Total Kills", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of total deaths in kit stats.")]
    public readonly Translation KitMenuUIDeathsLabel = new Translation("Total Deaths", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of primary kills in kit stats.")]
    public readonly Translation KitMenuUIPrimaryKillsLabel = new Translation("Primary Kills", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of primary average kill distance in kit stats.")]
    public readonly Translation KitMenuUIPrimaryAvgDstLabel = new Translation("Primary Avg. Dst.", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of secondary kills in kit stats.")]
    public readonly Translation KitMenuUISecondaryKillsLabel = new Translation("Secondary Kills", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of DBNO states in kit stats.")]
    public readonly Translation KitMenuUIDBNOLabel = new Translation("Injures Without Kill", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of distance traveled in kit stats.")]
    public readonly Translation KitMenuUIDistanceTraveledLabel = new Translation("Distance Traveled", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of tickets lost in kit stats.")]
    public readonly Translation KitMenuUITicketsLostLabel = new Translation("Tickets Lost", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of tickets gained in kit stats.")]
    public readonly Translation KitMenuUITicketsGainedLabel = new Translation("Tickets Recovered", TranslationOptions.TMProUI);

    
    [TranslationData("Label for kit stats title.")]
    public readonly Translation KitMenuUIStatsLabel = new Translation("Stats", TranslationOptions.TMProUI);
    
    [TranslationData("Label for kit actions title.")]
    public readonly Translation KitMenuUIActionsLabel = new Translation("Actions", TranslationOptions.TMProUI);
    
    [TranslationData("Label actions button action 1, request kit.")]
    public readonly Translation KitMenuUIActionRequestKitLabel = new Translation("Request Kit", TranslationOptions.TMProUI);
    
    [TranslationData("Label actions button action 2, buy kit (can afford).", "Credit cost")]
    public readonly Translation<int> KitMenuUIActionBuyPublicKitCanAffordLabel = new Translation<int>("<#ccffff>Buy Kit <#b8ffc1>C</color> <#fff>{0}</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label actions button action 2, buy kit (can't afford).", "Credit cost")]
    public readonly Translation<int> KitMenuUIActionBuyPublicKitCantAffordLabel = new Translation<int>("<#ff6666>Requires <#b8ffc1>C</color> <#fff>{0}</color>", TranslationOptions.TMProUI);
    
    [TranslationData("Label actions button action 3, order kit.", "Price", "Currency Prefix")]
    public readonly Translation<decimal, string> KitMenuUIActionBuyPremiumKitLabel = new Translation<decimal, string>("<#ccffff>Open Ticket <#c$kit_level_dollars$>{1}</color> <#fff>{0}</color>", TranslationOptions.TMProUI, "C");
    
    [TranslationData("Label actions button action not in main.")]
    public readonly Translation KitMenuUIActionNotInMainKitLabel = new Translation("<#ff6666>Not in Main", TranslationOptions.TMProUI);
    
    [TranslationData("Label actions button action premium not linked.", "Price", "Currency Prefix")]
    public readonly Translation<decimal, string> KitMenuUIActionBuyPremiumKitNotLinkedLabel = new Translation<decimal, string>("<#ccffff>Premium kit: <#c$kit_level_dollars$>{1}</color> <#fff>{0}</color>", TranslationOptions.TMProUI, "C");
    
    [TranslationData("Label actions button action premium unlock requirement not met.", "Price", "Currency Prefix")]
    public readonly Translation<decimal, string> KitMenuUIActionBuyPublicUnlockReqNotMetLabel = new Translation<decimal, string>("<#ccffff>Premium kit: <#c$kit_level_dollars$>{1}</color> <#fff>{0}</color>", TranslationOptions.TMProUI, "C");
    
    [TranslationData("Label actions button staff give kit.")]
    public readonly Translation KitMenuUIActionGiveKitLabel = new Translation("<#0099ff>Give Kit", TranslationOptions.TMProUI);
    
    [TranslationData("Label actions button staff edit kit.")]
    public readonly Translation KitMenuUIActionEditKitLabel = new Translation("<#0099ff>Edit Kit</color> (Coming Soon)", TranslationOptions.TMProUI);
    
    [TranslationData("Label actions button staff set loadout items kit.")]
    public readonly Translation KitMenuUIActionSetLoadoutItemsLabel = new Translation("<#0099ff>Set Loadout Items", TranslationOptions.TMProUI);
    
    [TranslationData("Shown when a player doesn't have access to a kit.")]
    public readonly Translation KitMenuUIActionNoAccessLabel = new Translation("No Access", TranslationOptions.TMProUI);
    
    [TranslationData("Shown when a kit's faction is not assigned.")]
    public readonly Translation KitMenuUINoFaction = new Translation("Unaffiliated", TranslationOptions.TMProUI);

    /* CLASS STATS */
    // squadleader
    
    [TranslationData("Label that goes in front of FOBs started for Squadleaders in kit stats.")]
    public readonly Translation KitMenuUISquadLeaderFOBsStartedLabel = new Translation("FOBs Started", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of UAVs requested for Squadleaders in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUISquadLeaderUAVsRequestedLabel = new Translation("UAVs Requested", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of targets spotted for Squadleaders in kit stats.")]
    public readonly Translation KitMenuUISquadLeaderTargetsSpottedLabel = new Translation("Targets Spotted", TranslationOptions.TMProUI);

    // rifleman
    
    [TranslationData("Label that goes in front of self restocked for Riflemen in kit stats.")]
    public readonly Translation KitMenuUIRiflemanSelfRestockedLabel = new Translation("Self Restocked", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of teammates restocked for Riflemen in kit stats.")]
    public readonly Translation KitMenuUIRiflemanTeammatesRestockedLabel = new Translation("Teammates Restocked", TranslationOptions.TMProUI);

    // medic
    
    [TranslationData("Label that goes in front of teammates healed for Medics in kit stats.")]
    public readonly Translation KitMenuUIMedicTeammatesHealedLabel = new Translation("Teammates Healed", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of teammates revived for Medics in kit stats.")]
    public readonly Translation KitMenuUIMedicTeammatesRevivedLabel = new Translation("Teammates Revived", TranslationOptions.TMProUI);

    // breacher
    
    [TranslationData("Label that goes in front of structures destroyed for Breachers in kit stats.")]
    public readonly Translation KitMenuUIBreacherStructuresDestroyedLabel = new Translation("Structures Destroyed", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of radios destroyed for Breachers in kit stats.")]
    public readonly Translation KitMenuUIBreacherRadiosDestroyedLabel = new Translation("Radios Destroyed", TranslationOptions.TMProUI);

    // auto-rifleman
    
    [TranslationData("Label that goes in front of spray n pray streak (most kills without reloading) for Automatic Riflemen in kit stats.")]
    public readonly Translation KitMenuUIAutoRiflemanStructuresDestroyedLabel = new Translation("Spray n Pray Streak", TranslationOptions.TMProUI);

    // grenadier
    
    [TranslationData("Label that goes in front of grenade kills for Grenadiers in kit stats.")]
    public readonly Translation KitMenuUIGrenadierGrenadeKillsLabel = new Translation("Grenade Kills", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of vehicle kills for Grenadiers in kit stats.")]
    public readonly Translation KitMenuUIGrenadierVehicleKillsLabel = new Translation("Vehicle Kills", TranslationOptions.TMProUI);

    // machine gunner
    
    [TranslationData("Label that goes in front of spray n pray streak (most kills without reloading) for Machine Gunners in kit stats.")]
    public readonly Translation KitMenuUIMachineGunnerStructuresDestroyedLabel = new Translation("Spray n Pray Streak", TranslationOptions.TMProUI);

    // LAT
    
    [TranslationData("Label that goes in front of vehicle kills for LATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUILATVehicleKillsLabel = new Translation("Vehicle Kills", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of LAT player kills for LATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUILATPlayerKillsLabel = new Translation("LAT Player Kills", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of structure kills for LATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUILATStructuresDestroyedLabel = new Translation("Structures Destroyed", TranslationOptions.TMProUI);

    // HAT
    
    [TranslationData("Label that goes in front of vehicle kills for HATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUIHATVehicleKillsLabel = new Translation("Vehicle Kills", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of HAT player kills for HATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUIHATPlayerKillsLabel = new Translation("HAT Player Kills", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of structure kills for HATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUIHATStructuresDestroyedLabel = new Translation("Structures Destroyed", TranslationOptions.TMProUI);

    // marksman
    
    [TranslationData("Label that goes in front of primary kills from 150m to 250m away for Marksmen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUIMarksmanKills100mLabel = new Translation("Kills 150m-250m", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of primary kills from 250m to 350m away for Marksmen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUIMarksmanKills200mLabel = new Translation("Kills 250m-350m", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of primary kills over 350m away for Marksmen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUIMarksmanKills300mLabel = new Translation("Kills 350m+", TranslationOptions.TMProUI);

    // sniper
    
    [TranslationData("Label that goes in front of primary kills from 200m to 300m away for Snipers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUISniperKills200mLabel = new Translation("Kills 200m-300m", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of primary kills from 300m to 400m away for Snipers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUISniperKills300mLabel = new Translation("Kills 300m-400m", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of primary kills over 400m away for Snipers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUISniperKills400mLabel = new Translation("Kills 400m+", TranslationOptions.TMProUI);

    // ap-rifleman
    
    [TranslationData("Label that goes in front of vehicle kills for AP Riflemen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUIAPRiflemanVehicleKillsLabel = new Translation("Vehicle Kills", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of player trap kills for AP Riflemen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUIAPRiflemanTrapKillsLabel = new Translation("Trap Kills", TranslationOptions.TMProUI);

    // combat engineer
    
    [TranslationData("Label that goes in front of shovel points for Combat Engineers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUICombatEngineerShovelsLabel = new Translation("Shovel Points", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of fortifications built for Combat Engineers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUICombatEngineerFortificationsBuiltLabel = new Translation("Fortifications Built", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of emplacements built for Combat Engineers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public readonly Translation KitMenuUICombatEngineerEmplacementsBuiltLabel = new Translation("Emplacements Built", TranslationOptions.TMProUI);

    // crewman
    
    [TranslationData("Label that goes in front of km driven for Crewmen in kit stats.")]
    public readonly Translation KitMenuUICrewmanKmDrivenLabel = new Translation("Distance Driven (km)", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of driver assists for Crewmen in kit stats.")]
    public readonly Translation KitMenuUICrewmanDriverAssistsLabel = new Translation("Driver Assists", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of total transport distance built for Crewmen in kit stats.")]
    public readonly Translation KitMenuUICrewmanTransportDistanceLabel = new Translation("Ttl. Transport Dst.", TranslationOptions.TMProUI);

    // pilot
    
    [TranslationData("Label that goes in front of km flown for Pilots in kit stats.")]
    public readonly Translation KitMenuUIPilotKmDrivenLabel = new Translation("Distance Flown (km)", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of pilot assists for Pilots in kit stats.")]
    public readonly Translation KitMenuUIPilotDriverAssistsLabel = new Translation("Pilot Assists", TranslationOptions.TMProUI);
    
    [TranslationData("Label that goes in front of total transport distance built for Pilots in kit stats.")]
    public readonly Translation KitMenuUIPilotTransportDistanceLabel = new Translation("Ttl. Transport Dst.", TranslationOptions.TMProUI);

    // spec ops
    
    [TranslationData("Label that goes in front of night vision kills for Special Ops in kit stats.")]
    public readonly Translation KitMenuUISpecOpsNVGKillsLabel = new Translation("NVG Kills (Night)", TranslationOptions.TMProUI);
}