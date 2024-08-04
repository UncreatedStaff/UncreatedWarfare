using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;

[UnturnedUI(BasePath = "Background/BkgrMask")]
public class KitMenuUI : UnturnedUI
{
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
    public KitMenuUI() : base(Gamemode.Config.UIKitMenu.GetId())
    {
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
            DefaultClassCache[i] = (Class)i == Class.APRifleman ? "AP Rifleman" /* too long */ : Localization.TranslateEnum((Class)i, null);
            ClassIconCache[i] = ((Class)i).GetIcon().ToString();
            if (DropdownButtons[i] is not { } btn)
                L.LogWarning("DropdownButtons[" + i + "] was not initialized (class: " + (Class)i + ").", method: "KIT MENU UI");
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

        Translation.OnReload += OnReload;
        CacheLanguages();
    }

    private void KitClickedIntl(UnturnedButton button, Player player)
    {
        UCPlayer? ucp = UCPlayer.FromPlayer(player);
        if (ucp != null)
        {
            int index = Array.FindIndex(Kits, kit => kit.Root == button);
            if (index != -1)
                OnKitClicked(index, ucp);
        }
    }
    internal void OnFavoritesRefreshed(UCPlayer player)
    {
        if (!player.KitMenuData.IsOpen)
            return;
        RefreshList(player);
    }
    private void FavoriteClickedIntl(UnturnedButton button, Player player)
    {
        UCPlayer? ucp = UCPlayer.FromPlayer(player);
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
        if (UCPlayer.FromPlayer(player) is { } pl)
        {
            pl.KitMenuData.IsOpen = false;
        }

        player.enablePluginWidgetFlag(DisabledWidgets);
        player.disablePluginWidgetFlag(EnabledWidgets);
    }

    public void OpenUI(UCPlayer player)
    {
        ITransportConnection c = player.Connection;
        player.KitMenuData.ActiveTeam = player.GetTeam();
        if (!player.KitMenuData.IsAlive)
        {
            SendToPlayer(c);
            if (player.Locale.LanguageInfo.IsDefault)
                SetCachedValues(c);
            else
                SetCachedValuesToOther(c, player.Locale.LanguageInfo, player.Locale.CultureInfo);
            LogicSetTabs[0].SetVisibility(c, true);
            SwitchToTab(player, 0);
        }
        else
        {
            SwitchToTab(player, 0);
        }

        player.KitMenuData.Tab = 0;
        player.KitMenuData.IsOpen = true;
        player.Player.disablePluginWidgetFlag(DisabledWidgets);
        player.Player.enablePluginWidgetFlag(EnabledWidgets);
    }
    private string TranslateClass(Class @class, UCPlayer player)
    {
        int cl = (int)@class;
        if (player.Locale.LanguageInfo.IsDefault && DefaultClassCache.Length > cl)
            return DefaultClassCache[cl];

        return Localization.TranslateEnum(@class, player.Locale.LanguageInfo);
    }
    private void OnReload()
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.KitMenuData.IsAlive)
            {
                if (pl.Locale.LanguageInfo.IsDefault)
                    SetCachedValues(pl.Connection);
                else
                    SetCachedValuesToOther(pl.Connection, pl.Locale.LanguageInfo, pl.Locale.CultureInfo);
                if (pl.KitMenuData.IsOpen)
                {
                    LogicSetTabs[pl.KitMenuData.Tab].SetVisibility(pl.Connection, true);
                    SwitchToTab(pl, pl.KitMenuData.Tab);
                }
            }
        }
    }
    private void SetCachedValuesToOther(ITransportConnection c, LanguageInfo language, CultureInfo culture)
    {
        LblTabBaseKits.SetText(c, T.KitMenuUITabBaseKits.Translate(language, culture));
        LblTabEliteKits.SetText(c, T.KitMenuUITabEliteKits.Translate(language, culture));
        LblTabLoadouts.SetText(c, T.KitMenuUITabLoadouts.Translate(language, culture));
        LblTabSpecialKits.SetText(c, T.KitMenuUITabSpecialKits.Translate(language, culture));

        LblFilterTitle.SetText(c, T.KitMenuUIFilterLabel.Translate(language, culture));
        LblInfoFaction.SetText(c, T.KitMenuUIFactionLabel.Translate(language, culture));
        LblInfoClass.SetText(c, T.KitMenuUIClassLabel.Translate(language, culture));
        LblInfoIncludedItems.SetText(c, T.KitMenuUIIncludedItemsLabel.Translate(language, culture));

        LblStatsPlaytime.SetText(c, T.KitMenuUIPlaytimeLabel.Translate(language, culture));
        LblStatsKills.SetText(c, T.KitMenuUIKillsLabel.Translate(language, culture));
        LblStatsDeaths.SetText(c, T.KitMenuUIDeathsLabel.Translate(language, culture));
        LblStatsPrimaryKills.SetText(c, T.KitMenuUIPrimaryKillsLabel.Translate(language, culture));
        LblStatsPrimaryAverageDistance.SetText(c, T.KitMenuUIPrimaryAvgDstLabel.Translate(language, culture));
        LblStatsSecondaryKills.SetText(c, T.KitMenuUISecondaryKillsLabel.Translate(language, culture));
        LblStatsDBNO.SetText(c, T.KitMenuUIDBNOLabel.Translate(language, culture));
        LblStatsDistanceTraveled.SetText(c, T.KitMenuUIDistanceTraveledLabel.Translate(language, culture));
        LblStatsTicketsLost.SetText(c, T.KitMenuUITicketsLostLabel.Translate(language, culture));
        LblStatsTicketsGained.SetText(c, T.KitMenuUITicketsGainedLabel.Translate(language, culture));

        LblStatsTitle.SetText(c, T.KitMenuUIStatsLabel.Translate(language, culture));
        LblActionsTitle.SetText(c, T.KitMenuUIActionsLabel.Translate(language, culture));
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
    private void SwitchToTab(UCPlayer player, byte tab)
    {
        L.LogDebug("Switching to tab " + tab);
        if (tab >= TabCount) return;
        player.KitMenuData.IsOpen = true;
        player.KitMenuData.Tab = tab;
        RefreshList(player);

        if (player.KitMenuData.Filter == Class.None)
        {
            DropdownSelectedName.SetText(player.Connection, string.Empty);
            DropdownSelectedClass.SetText(player.Connection, string.Empty);
        }
        else
        {
            DropdownSelectedName.SetText(player.Connection, TranslateClass(player.KitMenuData.Filter, player));
            DropdownSelectedClass.SetText(player.Connection, player.KitMenuData.Filter.GetIcon().ToString());
        }
    }
    private void RefreshSelected(UCPlayer player)
    {
        Kit? selected = player.KitMenuData.SelectedKit;
        if (selected is null)
        {
            LogicClearKit.SetVisibility(player.Connection, true);
            return;
        }

        OpenKit(player, selected);
    }
    private void OpenKit(UCPlayer player, Kit kit)
    {
        if (DefaultLanguageCache == null)
            CacheLanguages();
        L.LogDebug("Opening kit: " + kit.InternalName);
        ITransportConnection c = player.Connection;
        LblInfoTitle.SetText(c, kit.GetDisplayName(player.Locale.LanguageInfo).Replace('\n', ' ').Replace("\r", string.Empty));
        FactionInfo? faction = TeamManager.GetFactionInfo(kit.FactionId);

        ValInfoFaction.SetText(c, faction?.GetShortName(player.Locale.LanguageInfo) ?? (DefaultLanguageCache != null && player.Locale.LanguageInfo.IsDefault
                ? DefaultLanguageCache[29]
                : T.KitMenuUINoFaction.Translate(player)));

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
                if (!ItemIconProvider.TryGetIcon(grp.Asset, out icon, RichIcons, true))
                {
                    if (grp.Asset is ItemMagazineAsset)
                        icon = ItemIconProvider.GetIcon(RedirectType.StandardAmmoIcon, RichIcons, true);
                    else if (grp.Asset is ItemMeleeAsset)
                        icon = ItemIconProvider.GetIcon(RedirectType.StandardMeleeIcon, RichIcons, true);
                    else if (grp.Asset is ItemThrowableAsset throwable)
                    {
                        if (throwable.isExplosive)
                            icon = ItemIconProvider.GetIcon(RedirectType.StandardGrenadeIcon, RichIcons, true);
                        else if (throwable.itemName.IndexOf("smoke", StringComparison.InvariantCultureIgnoreCase) != -1)
                            icon = ItemIconProvider.GetIcon(RedirectType.StandardSmokeGrenadeIcon, RichIcons, true);
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
                            icon = ItemIconProvider.GetIcon(type, RichIcons, true);
                    }
                }
                name = grp.Asset.FriendlyName;
            }
            else if (grp.RedirectType != RedirectType.None)
            {
                icon = ItemIconProvider.GetIcon(grp.RedirectType, RichIcons, true);
                name = Localization.TranslateEnum(grp.RedirectType, player.Locale.LanguageInfo);
            }
            else if (grp.ClothingSetName != null)
            {
                icon = ItemIconProvider.GetIcon(RedirectType.Shirt, RichIcons, true);
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

        KitManager? manager = KitManager.GetSingletonQuick();

        if (TeamManager.IsInMain(player) && Data.Is<IKitRequests>())
        {
            if (kit.IsRequestable(player.Faction))
            {
                if (kit is { IsPublicKit: true, CreditCost: <= 0 } || (manager != null && manager.HasAccessQuick(kit, player)))
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

        if (player.OnDuty())
        {
            BtnActionsStaff1.SetVisibility(c, true);
            LblActionsStaff1Button.SetText(c, DefaultLanguageCache![26]);
        }
        else
        {
            BtnActionsStaff1.SetVisibility(c, false);
        }
    }
    private string GetTypeString(UCPlayer player, KitType type)
    {
        if (DefaultLanguageCache != null && player.Locale.LanguageInfo.IsDefault)
            return DefaultLanguageCache[
                type switch { KitType.Public => 8, KitType.Elite => 9, KitType.Loadout => 10, _ => 11 }
            ];
        return (type switch
        {
            KitType.Public  => T.KitMenuUIKitTypeLabelPublic,
            KitType.Elite   => T.KitMenuUIKitTypeLabelElite,
            KitType.Loadout => T.KitMenuUIKitTypeLabelLoadout,
            _               => T.KitMenuUIKitTypeLabelSpecial
        }).Translate(player.Locale.LanguageInfo);
    }
    private void RefreshList(UCPlayer player)
    {
        player.KitMenuData.RefreshKitList();
        SendKitList(player);
        RefreshSelected(player);
    }
    private void SendKitList(UCPlayer player)
    {
        LogicClearList.SetVisibility(player.Connection, true);
        for (int i = 0; i < player.KitMenuData.Kits.Length; ++i)
        {
            if (player.KitMenuData.Kits[i] is { } kit)
            {
                SendKit(player, i, kit, player.KitMenuData.Favorited[i]);
            }
        }
    }
    private void SendKit(UCPlayer player, int index, Kit kit, bool favorited)
    {
        ITransportConnection c = player.Connection;
        KitManager? manager = KitManager.GetSingletonQuick();
        bool hasAccess = manager != null && manager.HasAccessQuick(kit, player);
        ListedKit kitUi = Kits[index];
        if (kit.Type != KitType.Loadout)
            kitUi.FavoriteIcon.SetText(c, favorited ? "<#fd0>¼" : "¼");
        kitUi.Weapon.SetText(c, kit.WeaponText ?? string.Empty);
        kitUi.Id.SetText(c, kit.InternalName);
        kitUi.Name.SetText(c, kit.GetDisplayName(player.Locale.LanguageInfo).Replace('\n', ' ').Replace("\r", string.Empty));
        kitUi.Status.SetText(c, hasAccess ? Gamemode.Config.UIIconPlayer.ToString() : string.Empty);
        kitUi.Class.SetText(c, kit.Class.GetIcon().ToString());
        kitUi.Flag.SetText(c, kit.FactionInfo.GetFlagIcon());
        kitUi.FavoriteButton.SetVisibility(c, kit.Type != KitType.Loadout);
        
        Kits[index].Root.SetVisibility(c, true);
    }
    private void CacheLanguages()
    {
        LanguageInfo lang = Localization.GetDefaultLanguage();

        const int length = 31;
        if (DefaultLanguageCache is not { Length: length })
            DefaultLanguageCache = new string[length];
        for (int i = 0; i < DefaultClassCache.Length; ++i)
            DefaultClassCache[i] = Localization.TranslateEnum((Class)i, lang);
        
        DefaultLanguageCache[0]  = T.KitMenuUITabBaseKits.Translate(lang);
        DefaultLanguageCache[1]  = T.KitMenuUITabEliteKits.Translate(lang);
        DefaultLanguageCache[2]  = T.KitMenuUITabLoadouts.Translate(lang);
        DefaultLanguageCache[3]  = T.KitMenuUITabSpecialKits.Translate(lang);
                                 
        DefaultLanguageCache[4]  = T.KitMenuUIFilterLabel.Translate(lang);
        DefaultLanguageCache[5]  = T.KitMenuUIFactionLabel.Translate(lang);
        DefaultLanguageCache[6]  = T.KitMenuUIClassLabel.Translate(lang);
        DefaultLanguageCache[7]  = T.KitMenuUIIncludedItemsLabel.Translate(lang);
        DefaultLanguageCache[8]  = T.KitMenuUIKitTypeLabelPublic.Translate(lang);
        DefaultLanguageCache[9]  = T.KitMenuUIKitTypeLabelElite.Translate(lang);
        DefaultLanguageCache[10] = T.KitMenuUIKitTypeLabelSpecial.Translate(lang);
        DefaultLanguageCache[11] = T.KitMenuUIKitTypeLabelLoadout.Translate(lang);

        DefaultLanguageCache[12] = T.KitMenuUIPlaytimeLabel.Translate(lang);
        DefaultLanguageCache[13] = T.KitMenuUIKillsLabel.Translate(lang);
        DefaultLanguageCache[14] = T.KitMenuUIDeathsLabel.Translate(lang);
        DefaultLanguageCache[15] = T.KitMenuUIPrimaryKillsLabel.Translate(lang);
        DefaultLanguageCache[16] = T.KitMenuUIPrimaryAvgDstLabel.Translate(lang);
        DefaultLanguageCache[17] = T.KitMenuUISecondaryKillsLabel.Translate(lang);
        DefaultLanguageCache[18] = T.KitMenuUIDBNOLabel.Translate(lang);
        DefaultLanguageCache[19] = T.KitMenuUIDistanceTraveledLabel.Translate(lang);
        DefaultLanguageCache[20] = T.KitMenuUITicketsLostLabel.Translate(lang);
        DefaultLanguageCache[21] = T.KitMenuUITicketsGainedLabel.Translate(lang);

        DefaultLanguageCache[22] = T.KitMenuUIStatsLabel.Translate(lang);
        DefaultLanguageCache[23] = T.KitMenuUIActionsLabel.Translate(lang);
        DefaultLanguageCache[24] = T.KitMenuUIActionRequestKitLabel.Translate(lang);
        DefaultLanguageCache[25] = T.KitMenuUIActionNotInMainKitLabel.Translate(lang);
        DefaultLanguageCache[26] = T.KitMenuUIActionGiveKitLabel.Translate(lang);
        DefaultLanguageCache[27] = T.KitMenuUIActionEditKitLabel.Translate(lang);
        DefaultLanguageCache[28] = T.KitMenuUIActionSetLoadoutItemsLabel.Translate(lang);
        DefaultLanguageCache[29] = T.KitMenuUINoFaction.Translate(lang);
        DefaultLanguageCache[30] = T.KitMenuUIActionNoAccessLabel.Translate(lang);
    }
    private void OnClassButtonClicked(UnturnedButton button, Player player)
    {
        if (UCPlayer.FromPlayer(player) is { } ucp)
        {
            Class @class = (Class)Array.IndexOf(DropdownButtons, button);
            if (@class > ClassConverter.MaxClass)
                return;
            ITransportConnection tc = player.channel.owner.transportConnection;

            ucp.KitMenuData.Filter = @class;
            RefreshList(ucp);
            if (@class == Class.None)
            {
                DropdownSelectedName.SetText(tc, string.Empty);
                DropdownSelectedClass.SetText(tc, string.Empty);
                return;
            }

            // update dropdown text to translated value
            string text = ucp.Locale.LanguageInfo.IsDefault ? DefaultClassCache[(int)@class] : Localization.TranslateEnum(@class, ucp.Locale.LanguageInfo);

            DropdownSelectedName.SetText(tc, text);
            DropdownSelectedClass.SetText(tc, new string(@class.GetIcon(), 1));
        }
    }
    private void OnKitClicked(int kitIndex, UCPlayer player)
    {
        if (player.KitMenuData.Kits.Length > kitIndex && player.KitMenuData.Kits[kitIndex] is { } kit)
            OpenKit(player, kit);
    }
    private void OnFavoriteToggled(int kitIndex, UCPlayer player)
    {
        if (player.KitMenuData.Kits.Length <= kitIndex || player.KitMenuData.Kits[kitIndex] is not { } kit || kit.Type == KitType.Loadout)
            return;

        bool fav = player.KitMenuData.Favorited[kitIndex] = !player.KitMenuData.Favorited[kitIndex];
        Kits[kitIndex].FavoriteIcon.SetText(player.Connection, fav ? "<#fd0>¼" : "¼");
        player.KitMenuData.FavoritesDirty = true;
        L.LogDebug((fav ? "Favorited " : "Unfavorited ") + kit.InternalName);
        if (fav)
            (player.KitMenuData.FavoriteKits ??= new List<uint>(8)).Add(kit.PrimaryKey);
        else
            player.KitMenuData.FavoriteKits?.RemoveAll(x => x == kit.PrimaryKey);
    }
    private void OnActionButtonClicked(UnturnedButton button, Player player)
    {
        if (UCPlayer.FromPlayer(player) is not { } pl)
            return;

        if (!TeamManager.IsInMain(pl) || !Data.Is<IKitRequests>())
        {
            LogicActionButton.SetVisibility(pl.Connection, false);
            return;
        }

        if (pl.KitMenuData.SelectedKit is not { } proxy)
            return;

        CancellationToken tkn = pl.DisconnectToken;
        CombinedTokenSources tokens = tkn.CombineTokensIfNeeded(Data.Gamemode.UnloadToken);
        UCWarfare.RunTask(async tokens =>
        {
            try
            {
                KitManager? manager = KitManager.GetSingletonQuick();

                if (manager == null)
                    return;

                proxy = await manager.GetKit(proxy.PrimaryKey, tokens.Token, x => KitManager.RequestableSet(x, true)).ConfigureAwait(false);
                if (proxy == null)
                    return;

                await manager.Requests.RequestKit(proxy, CommandContext.CreateTemporary(pl), tokens.Token);
            }
            finally
            {
                tokens.Dispose();
            }
        }, tokens, ctx: "Request kit from kit UI.");
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
        if (UCPlayer.FromPlayer(player) is { } pl)
            SwitchToTab(pl, 0);
    }
    private void OnTabClickedEliteKits(UnturnedButton button, Player player)
    {
        if (UCPlayer.FromPlayer(player) is { } pl)
            SwitchToTab(pl, 1);
    }
    private void OnTabClickedLoadouts(UnturnedButton button, Player player)
    {
        if (UCPlayer.FromPlayer(player) is { } pl)
            SwitchToTab(pl, 2);
    }
    private void OnTabClickedSpecialKits(UnturnedButton button, Player player)
    {
        if (UCPlayer.FromPlayer(player) is { } pl)
            SwitchToTab(pl, 3);
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

public sealed class KitMenuUIData : IPlayerComponent
{
    public UCPlayer Player { get; set; }
    public ulong ActiveTeam { get; set; }
    public byte Tab { get; set; }
    public Class Filter { get; set; }
    public Kit? SelectedKit { get; set; }
    public bool IsAlive { get; set; }
    public bool IsOpen { get; set; }
    public bool FavoritesDirty { get; set; }
    public Kit[] Kits { get; set; } = Array.Empty<Kit>();
    public List<uint>? FavoriteKits { get; set; }
    public bool[] Favorited { get; set; } = Array.Empty<bool>();
    private FactionInfo? _faction;
    private KitManager? _manager;
    private UCPlayer? _viewLensPlayer;
    public void Init()
    {

    }
    internal void RefreshKitList()
    {
        KitManager? manager = KitManager.GetSingletonQuick();
        if (manager == null)
        {
            Kits = Array.Empty<Kit>();
            return;
        }

        _viewLensPlayer = Player;
        UCPlayer.TryApplyViewLens(ref _viewLensPlayer);

        _faction = _viewLensPlayer.Faction;
        _manager = manager;

        Func<Kit, bool> predicate = Tab switch
        {
            0 => KitListBasePredicate,
            1 => KitListElitePredicate,
            2 => KitListLoadoutPredicate,
            3 => KitListSpecialPredicate,
            _ => _ => true
        };
#if DEBUG
        using IDisposable disp = L.IndentLog(1);
#endif

        L.LogDebug(manager.Cache.KitDataByKey.Count + " searched... ");
        Kits = manager.Cache.KitDataByKey.Values.Where(predicate)
            .OrderByDescending(x => FavoriteKits?.Contains(x.PrimaryKey))
            .ThenBy(x =>
            {
                string dn = x.GetDisplayName(null);
                if (dn.Length <= 0 || char.IsDigit(dn[0]))
                    dn = "ZZ" + dn;
                return dn;
            }).ToArray();

        L.LogDebug(Kits.Length + " selected for tab " + Tab + "... ");

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
        _viewLensPlayer = null;

        _manager = null;
        _faction = null;
    }

    private bool KitListBasePredicate(Kit x)
        => x is { Type: KitType.Public } && (Filter == Class.None || x.Class == Filter) && x.Class > Class.Unarmed && x.IsRequestable(_faction);
    private bool KitListElitePredicate(Kit x)
        => x is { Type: KitType.Elite } && (Filter == Class.None || x.Class == Filter) && x.Class > Class.Unarmed && x.IsRequestable(_faction);
    private bool KitListLoadoutPredicate(Kit x)
        => x is { Type: KitType.Loadout } && (Filter == Class.None || x.Class == Filter) && x is { Class: > Class.Unarmed, Requestable: true } &&
           (Player.OnDuty() && x.Creator == Player.Steam64 || _manager!.HasAccessQuick(x, _viewLensPlayer!));
    private bool KitListSpecialPredicate(Kit x)
        => x is { Type: KitType.Special } && (Filter == Class.None || x.Class == Filter) && x.Class > Class.Unarmed && x.IsRequestable(_faction)
           && (Player.OnDuty() || _manager!.HasAccessQuick(x, _viewLensPlayer!));
}