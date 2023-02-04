using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uncreated.Framework;
using Uncreated.Framework.UI;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Kits;
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
    public readonly UnturnedUIElement LogicClearAll     = new UnturnedUIElement("anim_logic_clear_all");
    public readonly UnturnedUIElement LogicClearList    = new UnturnedUIElement("anim_logic_clear_list");
    public readonly UnturnedUIElement LogicClearKit     = new UnturnedUIElement("anim_logic_clear_kit");
    public readonly UnturnedUIElement LogicClearFilter  = new UnturnedUIElement("anim_logic_clear_filter");
    public readonly UnturnedUIElement LogicSetOpenState = new UnturnedUIElement("anim_logic_state_open");

    // set these as active/inactive to enable or disable target
    public readonly UnturnedUIElement LogicActionButton = new UnturnedUIElement("anim_logic_state_btn_action");
    public readonly UnturnedUIElement LogicStaff1Button = new UnturnedUIElement("anim_logic_state_btn_staff_1");
    public readonly UnturnedUIElement LogicStaff2Button = new UnturnedUIElement("anim_logic_state_btn_staff_2");
    public readonly UnturnedUIElement LogicStaff3Button = new UnturnedUIElement("anim_logic_state_btn_staff_3");

    /* TABS */

    // labels
    public readonly UnturnedLabel  LblTabBaseKits    = new UnturnedLabel ("tab_base_kits");
    public readonly UnturnedLabel  LblTabEliteKits   = new UnturnedLabel ("tab_elite_kits");
    public readonly UnturnedLabel  LblTabLoadouts    = new UnturnedLabel ("tab_loadouts");
    public readonly UnturnedLabel  LblTabSpecialKits = new UnturnedLabel ("tab_special_kits");

    // buttons
    public readonly UnturnedButton BtnTabBaseKits    = new UnturnedButton("Tab0");
    public readonly UnturnedButton BtnTabEliteKits   = new UnturnedButton("Tab1");
    public readonly UnturnedButton BtnTabLoadouts    = new UnturnedButton("Tab2");
    public readonly UnturnedButton BtnTabSpecialKits = new UnturnedButton("Tab3");
    public readonly UnturnedButton BtnTabClose       = new UnturnedButton("Tab4");

    /* FILTER */
    
    // labels
    public readonly UnturnedLabel LblFilterTitle                = new UnturnedLabel("filter_title");

    // name of the selected class in the filter
    public readonly UnturnedLabel DropdownSelectedName          = new UnturnedLabel("dropdown_selected");
    public readonly UnturnedLabel DropdownSelectedClass         = new UnturnedLabel("dropdown_selected_class");

    // dropdown selection buttons
    public readonly UnturnedButton BtnDropdownNone              = new UnturnedButton("dropdown_none");
    public readonly UnturnedButton BtnDropdownAPRifleman        = new UnturnedButton("dropdown_aprifleman");
    public readonly UnturnedButton BtnDropdownAutomaticRifleman = new UnturnedButton("dropdown_automaticrifleman");
    public readonly UnturnedButton BtnDropdownBreacher          = new UnturnedButton("dropdown_breacher");
    public readonly UnturnedButton BtnDropdownCombatEngineer    = new UnturnedButton("dropdown_combatengineer");
    public readonly UnturnedButton BtnDropdownCrewman           = new UnturnedButton("dropdown_crewman");
    public readonly UnturnedButton BtnDropdownGrenadier         = new UnturnedButton("dropdown_grenadier");
    public readonly UnturnedButton BtnDropdownHAT               = new UnturnedButton("dropdown_hat");
    public readonly UnturnedButton BtnDropdownLAT               = new UnturnedButton("dropdown_lat");
    public readonly UnturnedButton BtnDropdownMachineGunner     = new UnturnedButton("dropdown_machinegunner");
    public readonly UnturnedButton BtnDropdownMarksman          = new UnturnedButton("dropdown_marksman");
    public readonly UnturnedButton BtnDropdownMedic             = new UnturnedButton("dropdown_medic");
    public readonly UnturnedButton BtnDropdownPilot             = new UnturnedButton("dropdown_pilot");
    public readonly UnturnedButton BtnDropdownRifleman          = new UnturnedButton("dropdown_rifleman");
    public readonly UnturnedButton BtnDropdownSniper            = new UnturnedButton("dropdown_sniper");
    public readonly UnturnedButton BtnDropdownSpecOps           = new UnturnedButton("dropdown_specops");
    public readonly UnturnedButton BtnDropdownSquadleader       = new UnturnedButton("dropdown_squadleader");

    /* KIT INFO */

    // labels
    public readonly UnturnedLabel LblInfoTitle   = new UnturnedLabel("kit_info_title");
    public readonly UnturnedLabel LblInfoFaction = new UnturnedLabel("kit_info_faction_lbl");
    public readonly UnturnedLabel LblInfoClass   = new UnturnedLabel("kit_info_class_lbl");

    // values
    public readonly UnturnedLabel ValInfoFaction = new UnturnedLabel("kit_info_faction_value");
    public readonly UnturnedLabel ValInfoClass   = new UnturnedLabel("kit_info_class_value");
    public readonly UnturnedLabel ValInfoType    = new UnturnedLabel("kit_info_type");

    // icons
    public readonly UnturnedLabel LblInfoFactionFlag = new UnturnedLabel("kit_info_faction_flag");
    public readonly UnturnedLabel LblInfoClassIcon   = new UnturnedLabel("kit_info_class_icon");

    // Included Items
    public readonly UnturnedLabel LblInfoIncludedItems = new UnturnedLabel("kit_info_included_lbl");

    /* STATS */

    // labels
    public readonly UnturnedLabel LblStatsTitle                  = new UnturnedLabel("kit_stats_title");
    public readonly UnturnedLabel LblStatsPlaytime               = new UnturnedLabel("kit_info_playtime_lbl");
    public readonly UnturnedLabel LblStatsKills                  = new UnturnedLabel("kit_info_kills_lbl");
    public readonly UnturnedLabel LblStatsDeaths                 = new UnturnedLabel("kit_info_deaths_lbl");
    public readonly UnturnedLabel LblStatsPrimaryKills           = new UnturnedLabel("kit_info_primary_kills_lbl");
    public readonly UnturnedLabel LblStatsPrimaryAverageDistance = new UnturnedLabel("kit_info_primary_avg_dist_lbl");
    public readonly UnturnedLabel LblStatsSecondaryKills         = new UnturnedLabel("kit_info_secondary_kills_lbl");
    public readonly UnturnedLabel LblStatsDBNO                   = new UnturnedLabel("kit_info_dbnos_lbl");
    public readonly UnturnedLabel LblStatsDistanceTraveled       = new UnturnedLabel("kit_info_distance_traveled_lbl");
    public readonly UnturnedLabel LblStatsTicketsLost            = new UnturnedLabel("kit_info_tickets_used_lbl");
    public readonly UnturnedLabel LblStatsTicketsGained          = new UnturnedLabel("kit_info_tickets_gained_lbl");
    public readonly UnturnedLabel LblStatsClass1                 = new UnturnedLabel("kit_info_class_1_lbl");
    public readonly UnturnedLabel LblStatsClass2                 = new UnturnedLabel("kit_info_class_2_lbl");
    public readonly UnturnedLabel LblStatsClass3                 = new UnturnedLabel("kit_info_class_3_lbl");

    // values
    public readonly UnturnedLabel ValStatsPlaytime               = new UnturnedLabel("kit_info_playtime_value");
    public readonly UnturnedLabel ValStatsKills                  = new UnturnedLabel("kit_info_kills_value");
    public readonly UnturnedLabel ValStatsDeaths                 = new UnturnedLabel("kit_info_deaths_value");
    public readonly UnturnedLabel ValStatsPrimaryKills           = new UnturnedLabel("kit_info_primary_kills_value");
    public readonly UnturnedLabel ValStatsPrimaryAverageDistance = new UnturnedLabel("kit_info_primary_avg_dist_value");
    public readonly UnturnedLabel ValStatsSecondaryKills         = new UnturnedLabel("kit_info_secondary_kills_value");
    public readonly UnturnedLabel ValStatsDBNO                   = new UnturnedLabel("kit_info_dbnos_value");
    public readonly UnturnedLabel ValStatsDistanceTravelled      = new UnturnedLabel("kit_info_distance_traveled_value");
    public readonly UnturnedLabel ValStatsTicketsLost            = new UnturnedLabel("kit_info_tickets_used_value");
    public readonly UnturnedLabel ValStatsTicketsGained          = new UnturnedLabel("kit_info_tickets_gained_value");
    public readonly UnturnedLabel ValStatsClass1                 = new UnturnedLabel("kit_info_class_1_value");
    public readonly UnturnedLabel ValStatsClass2                 = new UnturnedLabel("kit_info_class_2_value");
    public readonly UnturnedLabel ValStatsClass3                 = new UnturnedLabel("kit_info_class_3_value");

    // parents
    public readonly UnturnedUIElement ObjStatsClass1 = new UnturnedLabel("kit_stats_class_1");
    public readonly UnturnedUIElement ObjStatsClass2 = new UnturnedLabel("kit_stats_class_2");
    public readonly UnturnedUIElement ObjStatsClass3 = new UnturnedLabel("kit_stats_class_3");

    // separators
    public readonly UnturnedUIElement SeparatorStatsClass1 = new UnturnedLabel("kit_stats_sep_10");
    public readonly UnturnedUIElement SeparatorStatsClass2 = new UnturnedLabel("kit_stats_sep_11");
    public readonly UnturnedUIElement SeparatorStatsClass3 = new UnturnedLabel("kit_stats_sep_12");

    /* ACTIONS */

    // labels
    public readonly UnturnedLabel LblActionsTitle = new UnturnedLabel("kit_actions_title");
    public readonly UnturnedLabel LblActionsActionButton = new UnturnedLabel("kit_actions_action_lbl");
    public readonly UnturnedLabel LblActionsStaff1Button = new UnturnedLabel("kit_actions_staff_1_text");
    public readonly UnturnedLabel LblActionsStaff2Button = new UnturnedLabel("kit_actions_staff_2_text");
    public readonly UnturnedLabel LblActionsStaff3Button = new UnturnedLabel("kit_actions_staff_3_text");

    // buttons
    public readonly UnturnedButton BtnActionsAction = new UnturnedButton("kit_actions_action_btn");
    public readonly UnturnedButton BtnActionsStaff1 = new UnturnedButton("kit_actions_staff_1");
    public readonly UnturnedButton BtnActionsStaff2 = new UnturnedButton("kit_actions_staff_2");
    public readonly UnturnedButton BtnActionsStaff3 = new UnturnedButton("kit_actions_staff_3");

    /* ARRAYS */

    // kit info
    public readonly UnturnedUIElement[] IncludedItems = UnturnedUIElement.GetPattern("kit_included_{0}", IncludedItemsCount, 0);
    public readonly UnturnedLabel[] IncludedItemsText = UnturnedLabel.GetPattern("kit_included_text_{0}", IncludedItemsCount, 0);
    public readonly UnturnedLabel[] IncludedItemsIcons = UnturnedLabel.GetPattern("kit_included_icon_{0}", IncludedItemsCount, 0);
    public readonly UnturnedLabel[] IncludedItemsAmounts = UnturnedLabel.GetPattern("kit_included_amt_{0}", IncludedItemsCount, 0);

    // kit list
    public readonly UnturnedButton[] Kits = UnturnedButton.GetPattern("kit_{0}", KitListCount);
    public readonly UnturnedLabel[] FlagLabels = UnturnedLabel.GetPattern("flag_kit_{0}", KitListCount);
    public readonly UnturnedLabel[] NameLabels = UnturnedLabel.GetPattern("name_kit_{0}", KitListCount);
    public readonly UnturnedLabel[] WeaponLabels = UnturnedLabel.GetPattern("weapon_kit_{0}", KitListCount);
    public readonly UnturnedLabel[] IdLabels = UnturnedLabel.GetPattern("id_kit_{0}", KitListCount);
    public readonly UnturnedLabel[] FavoriteLabels = UnturnedLabel.GetPattern("txt_fav_kit_{0}", KitListCount);
    public readonly UnturnedLabel[] ClassLabels = UnturnedLabel.GetPattern("class_kit_{0}", KitListCount);
    public readonly UnturnedLabel[] StatusLabels = UnturnedLabel.GetPattern("status_kit_{0}", KitListCount);
    public readonly UnturnedButton[] FavoriteButtons = UnturnedButton.GetPattern("btn_fav_kit_{0}", KitListCount);

    public readonly UnturnedUIElement[] LogicSetTabs = UnturnedUIElement.GetPattern("anim_logic_set_tab_{0}", TabCount);

    public readonly UnturnedButton[] DropdownButtons;
    public readonly string[] DefaultClassCache;
    public readonly string[] ClassIconCache;

    public string[]? DefaultLanguageCache;
    public KitMenuUI() : base(12014, Gamemode.Config.UIKitMenu)
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
        for (int i = 0; i < DropdownButtons.Length; ++i)
        {
            DefaultClassCache[i] = Localization.TranslateEnum((Class)i, L.Default);
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
        
        for (int i = 0; i < FavoriteButtons.Length; ++i)
        {
            FavoriteButtons[i].OnClicked += FavoriteClickedIntl;
        }
        for (int i = 0; i < Kits.Length; ++i)
        {
            Kits[i].OnClicked += KitClickedIntl;
        }

        Translation.OnReload += OnReload;
        CacheLanguages();
    }

    private void KitClickedIntl(UnturnedButton button, Player player)
    {
        UCPlayer? ucp = UCPlayer.FromPlayer(player);
        if (ucp != null)
        {
            int index = Array.IndexOf(Kits, button);
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
        if (ucp != null)
        {
            int index = Array.IndexOf(FavoriteButtons, button);
            if (index != -1)
                OnFavoriteToggled(index, ucp);
        }
    }
    private string GetClassIcon(Class @class)
    {
        byte c = (byte)@class;
        if (c < ClassIconCache.Length)
            return ClassIconCache[c];
        return new string(@class.GetIcon(), 1);
    }
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
            if (player.Language.Equals(L.Default, StringComparison.Ordinal))
                SetCachedValues(c);
            else
                SetCachedValuesToOther(c, player.Language);
            LogicSetTabs[0].SetVisibility(c, true);
            SwitchToTab(player, 0);
        }
        else
        {
            SwitchToTab(player, player.KitMenuData.Tab);
        }
        player.KitMenuData.IsOpen = true;
        player.Player.disablePluginWidgetFlag(DisabledWidgets);
        player.Player.enablePluginWidgetFlag(EnabledWidgets);
    }
    private string TranslateClass(Class @class, UCPlayer player)
    {
        int cl = (int)@class;
        if (player.Language.Equals(L.Default, StringComparison.Ordinal) && DefaultClassCache.Length > cl)
            return DefaultClassCache[cl];

        return Localization.TranslateEnum(@class, player.Steam64);
    }
    private void OnReload()
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.KitMenuData.IsAlive)
            {
                if (pl.Language.Equals(L.Default, StringComparison.Ordinal))
                    SetCachedValues(pl.Connection);
                else
                    SetCachedValuesToOther(pl.Connection, pl.Language);
                if (pl.KitMenuData.IsOpen)
                {
                    LogicSetTabs[pl.KitMenuData.Tab].SetVisibility(pl.Connection, true);
                    SwitchToTab(pl, pl.KitMenuData.Tab);
                }
            }
        }
    }
    private void SetCachedValuesToOther(ITransportConnection c, string lang)
    {
        LblTabBaseKits.SetText(c, T.KitMenuUITabBaseKits.Translate(lang));
        LblTabEliteKits.SetText(c, T.KitMenuUITabEliteKits.Translate(lang));
        LblTabLoadouts.SetText(c, T.KitMenuUITabLoadouts.Translate(lang));
        LblTabSpecialKits.SetText(c, T.KitMenuUITabSpecialKits.Translate(lang));

        LblFilterTitle.SetText(c, T.KitMenuUIFilterLabel.Translate(lang));
        LblInfoFaction.SetText(c, T.KitMenuUIFactionLabel.Translate(lang));
        LblInfoClass.SetText(c, T.KitMenuUIClassLabel.Translate(lang));
        LblInfoIncludedItems.SetText(c, T.KitMenuUIIncludedItemsLabel.Translate(lang));

        LblStatsPlaytime.SetText(c, T.KitMenuUIPlaytimeLabel.Translate(lang));
        LblStatsKills.SetText(c, T.KitMenuUIKillsLabel.Translate(lang));
        LblStatsDeaths.SetText(c, T.KitMenuUIDeathsLabel.Translate(lang));
        LblStatsPrimaryKills.SetText(c, T.KitMenuUIPrimaryKillsLabel.Translate(lang));
        LblStatsPrimaryAverageDistance.SetText(c, T.KitMenuUIPrimaryAvgDstLabel.Translate(lang));
        LblStatsSecondaryKills.SetText(c, T.KitMenuUISecondaryKillsLabel.Translate(lang));
        LblStatsDBNO.SetText(c, T.KitMenuUIDBNOLabel.Translate(lang));
        LblStatsDistanceTraveled.SetText(c, T.KitMenuUIDistanceTraveledLabel.Translate(lang));
        LblStatsTicketsLost.SetText(c, T.KitMenuUITicketsLostLabel.Translate(lang));
        LblStatsTicketsGained.SetText(c, T.KitMenuUITicketsGainedLabel.Translate(lang));

        LblStatsTitle.SetText(c, T.KitMenuUIStatsLabel.Translate(lang));
        LblActionsTitle.SetText(c, T.KitMenuUIActionsLabel.Translate(lang));

        int len = Math.Min(DefaultClassCache.Length, ClassLabels.Length);
        for (int i = 0; i < len; ++i)
            ClassLabels[i].SetText(c, Localization.TranslateEnum((Class)i, lang));
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

        int len = Math.Min(DefaultClassCache.Length, ClassLabels.Length);
        for (int i = 0; i < len; ++i)
            ClassLabels[i].SetText(c, DefaultClassCache[i]);
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
        SqlItem<Kit>? selected = player.KitMenuData.SelectedKit;
        if (selected?.Item is not { } kit)
        {
            LogicClearKit.SetVisibility(player.Connection, true);
            return;
        }

        OpenKit(player, kit);
    }
    private void OpenKit(UCPlayer player, Kit kit)
    {
        L.LogDebug("Opening kit: " + kit.Id);
        FactionInfo? plFaction = TeamManager.GetFactionSafe(player.GetTeam());
        ITransportConnection c = player.Connection;
        string lang = player.Language;
        LblInfoTitle.SetText(c, kit.GetDisplayName(lang).Replace('\n', ' ').Replace("\r", string.Empty));
        FactionInfo? faction = kit.Faction;
        ValInfoFaction.SetText(c, faction?.GetShortName(lang) ?? (
            DefaultLanguageCache != null &&
            player.Language.Equals(L.Default, StringComparison.Ordinal)
                ? DefaultLanguageCache[29]
                : T.KitMenuUINoFaction.Translate(player)
                ));
        LblInfoFactionFlag.SetText(c, faction.GetFlagIcon());
        ValInfoClass.SetText(c, TranslateClass(kit.Class, player));
        LblInfoClassIcon.SetText(c, kit.Class.GetIcon().ToString());
        
        ValInfoType.SetText(c, GetTypeString(player, kit.Type));

        List<KeyValuePair<KeyValuePair<ItemAsset?, RedirectType>, int>>? groups = kit.ItemListCache;
        if (groups == null)
        {
            kit.ItemListCache = groups = new List<KeyValuePair<KeyValuePair<ItemAsset?, RedirectType>, int>>();
            List<IKitItem> items = new List<IKitItem>(kit.Items.OrderBy(x => x is not IItemJar jar || jar.Page > Page.Secondary));
            items.Sort((a, b) => a.CompareTo(b));
            string? clothSet = null;
            for (int i = 0; i < items.Count; ++i)
            {
                IKitItem item = items[i];
                if (item is IAssetRedirect redir)
                {
                    if (groups.Exists(x => x.Key.Value == redir.RedirectType))
                        continue;
                    if (redir.RedirectType <= RedirectType.Glasses)
                    {
                        ItemAsset? asset = TeamManager.GetRedirectInfo(redir.RedirectType, faction, null, out _, out _);
                        if (asset != null)
                        {
                            if (redir.RedirectType is RedirectType.Shirt or RedirectType.Pants && clothSet == null)
                            {
                                if (asset != null)
                                {
                                    int index3 = asset.name.IndexOf(redir.RedirectType == RedirectType.Shirt ? "_Top" : "_Bottom", StringComparison.Ordinal);
                                    if (index3 != -1)
                                    {
                                        clothSet = asset.name.Substring(0, index3).Replace('_', ' ');
                                        continue;
                                    }
                                }
                            }
                            else if (clothSet != null && asset.name.StartsWith(clothSet, StringComparison.Ordinal))
                            {
                                continue;
                            }
                        }
                    }
                    groups.Add(
                        new KeyValuePair<KeyValuePair<ItemAsset?, RedirectType>, int>(
                            new KeyValuePair<ItemAsset?, RedirectType>(null, redir.RedirectType), 1));
                }
                else
                {
                    ItemAsset? asset = item.GetItem(kit, plFaction, out _, out _);
                    if (asset != null)
                    {
                        if (asset.id > 30000 && asset is ItemClothingAsset)
                        {
                            if (clothSet == null && (asset is ItemShirtAsset || asset is ItemPantsAsset))
                            {
                                int index3 = asset.name.IndexOf(asset is ItemShirtAsset ? "_Top" : "_Bottom", StringComparison.Ordinal);
                                if (index3 != -1)
                                {
                                    clothSet = asset.name.Substring(0, index3).Replace('_', ' ');
                                    continue;
                                }
                            }
                            else if (clothSet != null && asset.name.StartsWith(clothSet, StringComparison.Ordinal))
                            {
                                continue;
                            }
                        }
                        int index2 = groups.FindLastIndex(x => x.Key.Key == asset);
                        if (index2 == -1)
                        {
                            groups.Add(new KeyValuePair<KeyValuePair<ItemAsset?, RedirectType>, int>(
                                new KeyValuePair<ItemAsset?, RedirectType>(asset, RedirectType.None), 1));
                        }
                        else
                        {
                            KeyValuePair<KeyValuePair<ItemAsset?, RedirectType>, int> grp = groups[index2];
                            groups[index2] = new KeyValuePair<KeyValuePair<ItemAsset?, RedirectType>, int>(grp.Key, grp.Value + 1);
                        }
                    }
                }
            }

            if (clothSet != null)
            {
                int ind = 0;
                for (int i = 0; i < groups.Count; ++i)
                {
                    if (groups[i].Key.Key is ItemGunAsset)
                        ind = i + 1;
                    else break;
                }
                groups.Insert(ind, new KeyValuePair<KeyValuePair<ItemAsset?, RedirectType>, int>(
                    new KeyValuePair<ItemAsset?, RedirectType>(null, RedirectType.None), 255));
            }

            kit.ClothingSetCache = clothSet;
        }
        int index = 0;
        IFormatProvider locale = Localization.GetLocale(lang);
        for (int i = 0; i < groups.Count; ++i)
        {
            if (index >= IncludedItemsCount)
                break;
            KeyValuePair<KeyValuePair<ItemAsset?, RedirectType>, int> grp = groups[i];
            string icon;
            string name;
            int amt = grp.Value;
            if (grp.Key.Key != null)
            {
                if (!ItemIconProvider.TryGetIcon(grp.Key.Key, out icon, RichIcons, true))
                {
                    if (grp.Key.Key is ItemMagazineAsset)
                        icon = ItemIconProvider.GetIcon(RedirectType.StandardAmmoIcon, RichIcons, true);
                    else if (grp.Key.Key is ItemMeleeAsset)
                        icon = ItemIconProvider.GetIcon(RedirectType.StandardMeleeIcon, RichIcons, true);
                    else if (grp.Key.Key is ItemThrowableAsset throwable)
                    {
                        if (throwable.isExplosive)
                            icon = ItemIconProvider.GetIcon(RedirectType.StandardGrenadeIcon, RichIcons, true);
                        else if (throwable.itemName.IndexOf("smoke", StringComparison.InvariantCultureIgnoreCase) != -1)
                            icon = ItemIconProvider.GetIcon(RedirectType.StandardSmokeGrenadeIcon, RichIcons, true);
                    }
                    else if (grp.Key.Key is ItemClothingAsset cloth)
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
                name = grp.Key.Key.FriendlyName;
            }
            else if (grp.Key.Value != RedirectType.None)
            {
                icon = ItemIconProvider.GetIcon(grp.Key.Value, RichIcons, true);
                name = Localization.TranslateEnum(grp.Key.Value, lang);
            }
            else if (grp.Value == 255)
            {
                icon = ItemIconProvider.GetIcon(RedirectType.Shirt, RichIcons, true);
                if (kit.ClothingSetCache != null)
                    name = kit.ClothingSetCache + " Set";
                else if (faction != null)
                    name = "Default " + faction.GetAbbreviation(lang) + " Set.";
                else
                    name = "Default Set";
                amt = 1;
            }
            else continue;
            IncludedItems[index].SetVisibility(c, true);
            IncludedItemsIcons[index].SetText(c, icon);
            IncludedItemsText[index].SetText(c, name);
            IncludedItemsAmounts[index].SetVisibility(c, amt != 1);
            if (amt != 1)
                IncludedItemsAmounts[index].SetText(c, "x" + amt.ToString(locale));
            ++index;
        }
        for (; index < IncludedItemsText.Length; ++index)
        {
            IncludedItems[index].SetVisibility(c, false);
        }

        WarfareStats? stats = StatsManager.OnlinePlayers.FirstOrDefault(x => x.Steam64 == player.Steam64);
        if (stats != null && stats.Kits.FirstOrDefault(x => x.KitID.Equals(kit.Id, StringComparison.Ordinal)) is { } kitStats)
        {
            ValStatsKills.SetText(c, kitStats.Kills.ToString(locale));
            ValStatsDeaths.SetText(c, kitStats.Deaths.ToString(locale));
            ValStatsPrimaryAverageDistance.SetText(c, kitStats.AverageGunKillDistance.ToString("0.#", locale) + " m");
            ValStatsPlaytime.SetText(c, ((int)kitStats.PlaytimeMinutes).GetTimeFromMinutes(lang));
        }

        if (TeamManager.IsInMain(player) && Data.Is<IKitRequests>())
        {
            LogicActionButton.SetVisibility(c, true);
        }
        else
        {
            LogicActionButton.SetVisibility(c, false);
        }

    }
    private string GetTypeString(UCPlayer player, KitType type)
    {
        if (DefaultLanguageCache != null && player.Language.Equals(L.Default, StringComparison.Ordinal))
            return DefaultLanguageCache[
                type switch { KitType.Public => 8, KitType.Elite => 9, KitType.Loadout => 10, _ => 11 }
            ];
        return (type switch
        {
            KitType.Public  => T.KitMenuUIKitTypeLabelPublic,
            KitType.Elite   => T.KitMenuUIKitTypeLabelElite,
            KitType.Loadout => T.KitMenuUIKitTypeLabelLoadout,
            _               => T.KitMenuUIKitTypeLabelSpecial
        }).Translate(player.Language);
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
            if (player.KitMenuData.Kits[i].Item is { } kit)
            {
                SendKit(player, i, kit, player.KitMenuData.Favorited[i]);
            }
        }
    }
    private void SendKit(UCPlayer player, int index, Kit kit, bool favorited)
    {
        ITransportConnection c = player.Connection;
        bool hasAccess = KitManager.HasAccessQuick(kit, player);
        if (kit.Type != KitType.Loadout)
            FavoriteLabels[index].SetText(c, favorited ? "<#fd0>¼" : "¼");
        WeaponLabels[index].SetText(c, kit.WeaponText ?? string.Empty);
        IdLabels[index].SetText(c, kit.Id);
        NameLabels[index].SetText(c, kit.GetDisplayName(player.Language).Replace('\n', ' ').Replace("\r", string.Empty));
        StatusLabels[index].SetText(c, hasAccess ? Gamemode.Config.UIIconPlayer.ToString() : string.Empty);
        ClassLabels[index].SetText(c, kit.Class.GetIcon().ToString());
        FlagLabels[index].SetText(c, kit.Faction.GetFlagIcon());
        FavoriteButtons[index].SetVisibility(c, kit.Type != KitType.Loadout);
        
        Kits[index].SetVisibility(c, true);
    }
    private void CacheLanguages()
    {
        const int length = 30;
        if (DefaultLanguageCache is not { Length: length })
            DefaultLanguageCache = new string[length];
        for (int i = 0; i < DefaultClassCache.Length; ++i)
            DefaultClassCache[i] = Localization.TranslateEnum((Class)i, L.Default);
        
        DefaultLanguageCache[0]  = T.KitMenuUITabBaseKits.Translate(L.Default);
        DefaultLanguageCache[1]  = T.KitMenuUITabEliteKits.Translate(L.Default);
        DefaultLanguageCache[2]  = T.KitMenuUITabLoadouts.Translate(L.Default);
        DefaultLanguageCache[3]  = T.KitMenuUITabSpecialKits.Translate(L.Default);
                                 
        DefaultLanguageCache[4]  = T.KitMenuUIFilterLabel.Translate(L.Default);
        DefaultLanguageCache[5]  = T.KitMenuUIFactionLabel.Translate(L.Default);
        DefaultLanguageCache[6]  = T.KitMenuUIClassLabel.Translate(L.Default);
        DefaultLanguageCache[7]  = T.KitMenuUIIncludedItemsLabel.Translate(L.Default);
        DefaultLanguageCache[8]  = T.KitMenuUIKitTypeLabelPublic.Translate(L.Default);
        DefaultLanguageCache[9]  = T.KitMenuUIKitTypeLabelElite.Translate(L.Default);
        DefaultLanguageCache[10] = T.KitMenuUIKitTypeLabelSpecial.Translate(L.Default);
        DefaultLanguageCache[11] = T.KitMenuUIKitTypeLabelLoadout.Translate(L.Default);

        DefaultLanguageCache[12] = T.KitMenuUIPlaytimeLabel.Translate(L.Default);
        DefaultLanguageCache[13] = T.KitMenuUIKillsLabel.Translate(L.Default);
        DefaultLanguageCache[14] = T.KitMenuUIDeathsLabel.Translate(L.Default);
        DefaultLanguageCache[15] = T.KitMenuUIPrimaryKillsLabel.Translate(L.Default);
        DefaultLanguageCache[16] = T.KitMenuUIPrimaryAvgDstLabel.Translate(L.Default);
        DefaultLanguageCache[17] = T.KitMenuUISecondaryKillsLabel.Translate(L.Default);
        DefaultLanguageCache[18] = T.KitMenuUIDBNOLabel.Translate(L.Default);
        DefaultLanguageCache[19] = T.KitMenuUIDistanceTraveledLabel.Translate(L.Default);
        DefaultLanguageCache[20] = T.KitMenuUITicketsLostLabel.Translate(L.Default);
        DefaultLanguageCache[21] = T.KitMenuUITicketsGainedLabel.Translate(L.Default);

        DefaultLanguageCache[22] = T.KitMenuUIStatsLabel.Translate(L.Default);
        DefaultLanguageCache[23] = T.KitMenuUIActionsLabel.Translate(L.Default);
        DefaultLanguageCache[24] = T.KitMenuUIActionRequestKitLabel.Translate(L.Default);
        DefaultLanguageCache[25] = T.KitMenuUIActionNotInMainKitLabel.Translate(L.Default);
        DefaultLanguageCache[26] = T.KitMenuUIActionGiveKitLabel.Translate(L.Default);
        DefaultLanguageCache[27] = T.KitMenuUIActionEditKitLabel.Translate(L.Default);
        DefaultLanguageCache[28] = T.KitMenuUIActionSetLoadoutItemsLabel.Translate(L.Default);
        DefaultLanguageCache[29] = T.KitMenuUINoFaction.Translate(L.Default);
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
            string lang = Localization.GetLang(player.channel.owner.playerID.steamID.m_SteamID);
            if (lang.Equals(L.Default, StringComparison.Ordinal))
                lang = DefaultClassCache[(int)@class];
            else
                lang = Localization.TranslateEnum(@class, lang);

            DropdownSelectedName.SetText(tc, lang);
            DropdownSelectedClass.SetText(tc, new string(@class.GetIcon(), 1));
        }
    }
    private void OnKitClicked(int kitIndex, UCPlayer player)
    {
        if (player.KitMenuData.Kits.Length > kitIndex && player.KitMenuData.Kits[kitIndex].Item is { } kit)
            OpenKit(player, kit);
    }
    private void OnFavoriteToggled(int kitIndex, UCPlayer player)
    {
        if (player.KitMenuData.Kits.Length > kitIndex && player.KitMenuData.Kits[kitIndex].Item is { } kit && kit.Type != KitType.Loadout)
        {
            bool fav = player.KitMenuData.Favorited[kitIndex] = !player.KitMenuData.Favorited[kitIndex];
            FavoriteLabels[kitIndex].SetText(player.Connection, fav ? "<#fd0>¼" : "¼");
            player.KitMenuData.FavoritesDirty = true;
            L.LogDebug((fav ? "Favorited " : "Unfavorited ") + kit.Id);
            if (fav)
                (player.KitMenuData.FavoriteKits ??= new List<PrimaryKey>(8)).Add(kit.PrimaryKey);
            else
                player.KitMenuData.FavoriteKits?.RemoveAll(x => x.Key == kit.PrimaryKey.Key);
        }
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
        if (pl.KitMenuData.SelectedKit is { Item: { } } proxy)
        {
            CancellationToken tkn = pl.DisconnectToken;
            tkn.CombineIfNeeded(Data.Gamemode.UnloadToken);
            UCWarfare.RunTask(async token =>
            {
                KitManager? manager = KitManager.GetSingletonQuick();
                if (manager == null)
                    return;
                await manager.RequestKit(proxy, CommandInteraction.CreateTemporary(pl), token);
            }, tkn, ctx: "Request kit from kit UI.");
        }
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
}

public sealed class KitMenuUIData : IPlayerComponent
{
    public UCPlayer Player { get; set; }
    public ulong ActiveTeam { get; set; }
    public byte Tab { get; set; }
    public Class Filter { get; set; }
    public SqlItem<Kit>? SelectedKit { get; set; }
    public bool IsAlive { get; set; }
    public bool IsOpen { get; set; }
    public bool FavoritesDirty { get; set; }
    public SqlItem<Kit>[] Kits { get; set; } = Array.Empty<SqlItem<Kit>>();
    public List<PrimaryKey>? FavoriteKits { get; set; }
    public bool[] Favorited { get; set; } = Array.Empty<bool>();
    private FactionInfo? _faction;
    public void Init()
    {

    }
    internal void RefreshKitList()
    {
        KitManager? manager = KitManager.GetSingletonQuick();
        if (manager == null)
        {
            Kits = Array.Empty<SqlItem<Kit>>();
            return;
        }
        manager.WriteWait();
        try
        {
            _faction = Player.Faction;
            Func<SqlItem<Kit>, bool> predicate = Tab switch
            {
                0 => KitListBasePredicate,
                1 => KitListElitePredicate,
                2 => KitListLoadoutPredicate,
                3 => KitListSpecialPredicate,
                _ => x => x.Item != null,
            };
            L.LogDebug(manager.Items.Count + " searched... ");
            Kits = manager.Items.Where(predicate)
                .OrderByDescending(x => FavoriteKits?.Contains(x.LastPrimaryKey))
                .ThenBy(x =>
                {
                    string dn = x.Item?.GetDisplayName(L.Default) ?? x.PrimaryKey.Key.ToString();
                    if (dn.Length <= 0 || char.IsDigit(dn[0]))
                        dn = "ZZ" + dn;
                    return dn;
                }).ToArray();
            L.LogDebug(Kits.Length + " selected for tab " + Tab + "... ");
            if (Kits.Length > KitMenuUI.KitListCount)
            {
                SqlItem<Kit>[] old = Kits;
                Kits = new SqlItem<Kit>[KitMenuUI.KitListCount];
                Array.Copy(old, Kits, KitMenuUI.KitListCount);
            }
            if (Favorited.Length != Kits.Length)
                Favorited = new bool[Kits.Length];
            for (int i = 0; i < Kits.Length; ++i)
                Favorited[i] = FavoriteKits != null && FavoriteKits.Contains(Kits[i].LastPrimaryKey);
            using IDisposable disp = L.IndentLog(1);
        }
        finally
        {
            manager.WriteRelease();
        }
    }

    private bool KitListBasePredicate(SqlItem<Kit> x)
        => x.Item is { Type: KitType.Public } kit && (Filter == Class.None || kit.Class == Filter) && kit.Class > Class.Unarmed && kit.IsRequestable(_faction);
    private bool KitListElitePredicate(SqlItem<Kit> x)
        => x.Item is { Type: KitType.Elite } kit && (Filter == Class.None || kit.Class == Filter) && kit.Class > Class.Unarmed && kit.IsRequestable(_faction);
    private bool KitListLoadoutPredicate(SqlItem<Kit> x)
        => x.Item is { Type: KitType.Loadout } kit && (Filter == Class.None || kit.Class == Filter) && kit.Requestable &&
           (Player.OnDuty() && kit.Creator == Player.Steam64 || KitManager.HasAccessQuick(x, Player));
    private bool KitListSpecialPredicate(SqlItem<Kit> x)
        => x.Item is { Type: KitType.Special } kit && (Filter == Class.None || kit.Class == Filter) && kit.IsRequestable(_faction) && (Player.OnDuty() || KitManager.HasAccessQuick(x, Player));

    void OnDestroy()
    {

    }
}