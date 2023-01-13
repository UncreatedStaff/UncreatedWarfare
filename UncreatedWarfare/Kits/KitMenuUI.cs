using SDG.NetTransport;
using SDG.Unturned;
using System;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Kits;
public class KitMenuUI : UnturnedUI
{
    /* LOGIC */

    // enable these to clear all, will disable themselves
    public readonly UnturnedUIElement LogicClearAll     = new UnturnedUIElement("anim_logic_clear_all");
    public readonly UnturnedUIElement LogicClearList    = new UnturnedUIElement("anim_logic_clear_list");
    public readonly UnturnedUIElement LogicClearFilter  = new UnturnedUIElement("anim_logic_clear_filter");
    public readonly UnturnedUIElement LogicSetTab1      = new UnturnedUIElement("anim_logic_set_tab_1");
    public readonly UnturnedUIElement LogicSetTab2      = new UnturnedUIElement("anim_logic_set_tab_2");
    public readonly UnturnedUIElement LogicSetTab3      = new UnturnedUIElement("anim_logic_set_tab_3");
    public readonly UnturnedUIElement LogicSetTab4      = new UnturnedUIElement("anim_logic_set_tab_4");
    public readonly UnturnedUIElement LogicClose        = new UnturnedUIElement("anim_logic_close");

    // set these as active/inactive to enable or disable target
    public readonly UnturnedUIElement LogicActionButton = new UnturnedUIElement("anim_logic_state_btn_action");
    public readonly UnturnedUIElement LogicStaff1Button = new UnturnedUIElement("anim_logic_state_btn_staff_1");
    public readonly UnturnedUIElement LogicStaff2Button = new UnturnedUIElement("anim_logic_state_btn_staff_2");
    public readonly UnturnedUIElement LogicStaff3Button = new UnturnedUIElement("anim_logic_state_btn_staff_3");

    /* FILTER */
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

    public readonly UnturnedLabel LblInfoIncludedItemText1    = new UnturnedLabel("kit_included_icon_0");
    public readonly UnturnedLabel LblInfoIncludedItemAmount1  = new UnturnedLabel("kit_included_amt_0");
    public readonly UnturnedLabel LblInfoIncludedItemIcon1    = new UnturnedLabel("kit_included_icon_0");
                                                              
    public readonly UnturnedLabel LblInfoIncludedItemText2    = new UnturnedLabel("kit_included_icon_1");
    public readonly UnturnedLabel LblInfoIncludedItemAmount2  = new UnturnedLabel("kit_included_amt_1");
    public readonly UnturnedLabel LblInfoIncludedItemIcon2    = new UnturnedLabel("kit_included_icon_1");
                                                              
    public readonly UnturnedLabel LblInfoIncludedItemText3    = new UnturnedLabel("kit_included_icon_2");
    public readonly UnturnedLabel LblInfoIncludedItemAmount3  = new UnturnedLabel("kit_included_amt_2");
    public readonly UnturnedLabel LblInfoIncludedItemIcon3    = new UnturnedLabel("kit_included_icon_2");
                                                              
    public readonly UnturnedLabel LblInfoIncludedItemText4    = new UnturnedLabel("kit_included_icon_3");
    public readonly UnturnedLabel LblInfoIncludedItemAmount4  = new UnturnedLabel("kit_included_amt_3");
    public readonly UnturnedLabel LblInfoIncludedItemIcon4    = new UnturnedLabel("kit_included_icon_3");
                                                              
    public readonly UnturnedLabel LblInfoIncludedItemText5    = new UnturnedLabel("kit_included_icon_4");
    public readonly UnturnedLabel LblInfoIncludedItemAmount5  = new UnturnedLabel("kit_included_amt_4");
    public readonly UnturnedLabel LblInfoIncludedItemIcon5    = new UnturnedLabel("kit_included_icon_4");
                                                              
    public readonly UnturnedLabel LblInfoIncludedItemText6    = new UnturnedLabel("kit_included_icon_5");
    public readonly UnturnedLabel LblInfoIncludedItemAmount6  = new UnturnedLabel("kit_included_amt_5");
    public readonly UnturnedLabel LblInfoIncludedItemIcon6    = new UnturnedLabel("kit_included_icon_5");
                                                              
    public readonly UnturnedLabel LblInfoIncludedItemText7    = new UnturnedLabel("kit_included_icon_6");
    public readonly UnturnedLabel LblInfoIncludedItemAmount7  = new UnturnedLabel("kit_included_amt_6");
    public readonly UnturnedLabel LblInfoIncludedItemIcon7    = new UnturnedLabel("kit_included_icon_6");
                                                              
    public readonly UnturnedLabel LblInfoIncludedItemText8    = new UnturnedLabel("kit_included_icon_7");
    public readonly UnturnedLabel LblInfoIncludedItemAmount8  = new UnturnedLabel("kit_included_amt_7");
    public readonly UnturnedLabel LblInfoIncludedItemIcon8    = new UnturnedLabel("kit_included_icon_7");
                                                              
    public readonly UnturnedLabel LblInfoIncludedItemText9    = new UnturnedLabel("kit_included_icon_8");
    public readonly UnturnedLabel LblInfoIncludedItemAmount9  = new UnturnedLabel("kit_included_amt_8");
    public readonly UnturnedLabel LblInfoIncludedItemIcon9    = new UnturnedLabel("kit_included_icon_8");

    public readonly UnturnedLabel LblInfoIncludedItemText10   = new UnturnedLabel("kit_included_icon_9");
    public readonly UnturnedLabel LblInfoIncludedItemAmount10 = new UnturnedLabel("kit_included_amt_9");
    public readonly UnturnedLabel LblInfoIncludedItemIcon10   = new UnturnedLabel("kit_included_icon_9");

    public readonly UnturnedLabel LblInfoIncludedItemText11   = new UnturnedLabel("kit_included_icon_10");
    public readonly UnturnedLabel LblInfoIncludedItemAmount11 = new UnturnedLabel("kit_included_amt_10");
    public readonly UnturnedLabel LblInfoIncludedItemIcon11   = new UnturnedLabel("kit_included_icon_10");

    public readonly UnturnedLabel LblInfoIncludedItemText12   = new UnturnedLabel("kit_included_icon_11");
    public readonly UnturnedLabel LblInfoIncludedItemAmount12 = new UnturnedLabel("kit_included_amt_11");
    public readonly UnturnedLabel LblInfoIncludedItemIcon12   = new UnturnedLabel("kit_included_icon_11");

    public readonly UnturnedLabel LblInfoIncludedItemText13   = new UnturnedLabel("kit_included_icon_12");
    public readonly UnturnedLabel LblInfoIncludedItemAmount13 = new UnturnedLabel("kit_included_amt_12");
    public readonly UnturnedLabel LblInfoIncludedItemIcon13   = new UnturnedLabel("kit_included_icon_12");

    public readonly UnturnedLabel LblInfoIncludedItemText14   = new UnturnedLabel("kit_included_icon_13");
    public readonly UnturnedLabel LblInfoIncludedItemAmount14 = new UnturnedLabel("kit_included_amt_13");
    public readonly UnturnedLabel LblInfoIncludedItemIcon14   = new UnturnedLabel("kit_included_icon_13");

    public readonly UnturnedLabel LblInfoIncludedItemText15   = new UnturnedLabel("kit_included_icon_14");
    public readonly UnturnedLabel LblInfoIncludedItemAmount15 = new UnturnedLabel("kit_included_amt_14");
    public readonly UnturnedLabel LblInfoIncludedItemIcon15   = new UnturnedLabel("kit_included_icon_14");

    public readonly UnturnedLabel LblInfoIncludedItemText16   = new UnturnedLabel("kit_included_icon_15");
    public readonly UnturnedLabel LblInfoIncludedItemAmount16 = new UnturnedLabel("kit_included_amt_15");
    public readonly UnturnedLabel LblInfoIncludedItemIcon16   = new UnturnedLabel("kit_included_icon_15");

    public readonly UnturnedLabel LblInfoIncludedItemText17   = new UnturnedLabel("kit_included_icon_16");
    public readonly UnturnedLabel LblInfoIncludedItemAmount17 = new UnturnedLabel("kit_included_amt_16");
    public readonly UnturnedLabel LblInfoIncludedItemIcon17   = new UnturnedLabel("kit_included_icon_16");

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

    /* LIST */
    public readonly UnturnedUIElement Kit1         = new UnturnedUIElement("kit_1");
    public readonly UnturnedLabel LblFlagKit1      = new UnturnedLabel("flag_kit_1");
    public readonly UnturnedLabel LblNameKit1      = new UnturnedLabel("name_kit_1");
    public readonly UnturnedLabel LblWeaponKit1    = new UnturnedLabel("weapon_kit_1");
    public readonly UnturnedLabel LblIdKit1        = new UnturnedLabel("id_kit_1");
    public readonly UnturnedLabel LblFavoriteKit1  = new UnturnedLabel("txt_fav_kit_1");
    public readonly UnturnedLabel LblClassKit1     = new UnturnedLabel("class_kit_1");
    public readonly UnturnedLabel LblStatusKit1    = new UnturnedLabel("status_kit_1");
    public readonly UnturnedLabel BtnFavoriteKit1  = new UnturnedLabel("btn_fav_kit_1");
                                                   
    public readonly UnturnedUIElement Kit2         = new UnturnedUIElement("kit_2");
    public readonly UnturnedLabel LblFlagKit2      = new UnturnedLabel("flag_kit_2");
    public readonly UnturnedLabel LblNameKit2      = new UnturnedLabel("name_kit_2");
    public readonly UnturnedLabel LblWeaponKit2    = new UnturnedLabel("weapon_kit_2");
    public readonly UnturnedLabel LblIdKit2        = new UnturnedLabel("id_kit_2");
    public readonly UnturnedLabel LblFavoriteKit2  = new UnturnedLabel("txt_fav_kit_2");
    public readonly UnturnedLabel LblClassKit2     = new UnturnedLabel("class_kit_2");
    public readonly UnturnedLabel LblStatusKit2    = new UnturnedLabel("status_kit_2");
    public readonly UnturnedLabel BtnFavoriteKit2  = new UnturnedLabel("btn_fav_kit_2");
                                                   
    public readonly UnturnedUIElement Kit3         = new UnturnedUIElement("kit_3");
    public readonly UnturnedLabel LblFlagKit3      = new UnturnedLabel("flag_kit_3");
    public readonly UnturnedLabel LblNameKit3      = new UnturnedLabel("name_kit_3");
    public readonly UnturnedLabel LblWeaponKit3    = new UnturnedLabel("weapon_kit_3");
    public readonly UnturnedLabel LblIdKit3        = new UnturnedLabel("id_kit_3");
    public readonly UnturnedLabel LblFavoriteKit3  = new UnturnedLabel("txt_fav_kit_3");
    public readonly UnturnedLabel LblClassKit3     = new UnturnedLabel("class_kit_3");
    public readonly UnturnedLabel LblStatusKit3    = new UnturnedLabel("status_kit_3");
    public readonly UnturnedLabel BtnFavoriteKit3  = new UnturnedLabel("btn_fav_kit_3");
                                                   
    public readonly UnturnedUIElement Kit4         = new UnturnedUIElement("kit_4");
    public readonly UnturnedLabel LblFlagKit4      = new UnturnedLabel("flag_kit_4");
    public readonly UnturnedLabel LblNameKit4      = new UnturnedLabel("name_kit_4");
    public readonly UnturnedLabel LblWeaponKit4    = new UnturnedLabel("weapon_kit_4");
    public readonly UnturnedLabel LblIdKit4        = new UnturnedLabel("id_kit_4");
    public readonly UnturnedLabel LblFavoriteKit4  = new UnturnedLabel("txt_fav_kit_4");
    public readonly UnturnedLabel LblClassKit4     = new UnturnedLabel("class_kit_4");
    public readonly UnturnedLabel LblStatusKit4    = new UnturnedLabel("status_kit_4");
    public readonly UnturnedLabel BtnFavoriteKit4  = new UnturnedLabel("btn_fav_kit_4");
                                                   
    public readonly UnturnedUIElement Kit5         = new UnturnedUIElement("kit_5");
    public readonly UnturnedLabel LblFlagKit5      = new UnturnedLabel("flag_kit_5");
    public readonly UnturnedLabel LblNameKit5      = new UnturnedLabel("name_kit_5");
    public readonly UnturnedLabel LblWeaponKit5    = new UnturnedLabel("weapon_kit_5");
    public readonly UnturnedLabel LblIdKit5        = new UnturnedLabel("id_kit_5");
    public readonly UnturnedLabel LblFavoriteKit5  = new UnturnedLabel("txt_fav_kit_5");
    public readonly UnturnedLabel LblClassKit5     = new UnturnedLabel("class_kit_5");
    public readonly UnturnedLabel LblStatusKit5    = new UnturnedLabel("status_kit_5");
    public readonly UnturnedLabel BtnFavoriteKit5  = new UnturnedLabel("btn_fav_kit_5");
                                                   
    public readonly UnturnedUIElement Kit6         = new UnturnedUIElement("kit_6");
    public readonly UnturnedLabel LblFlagKit6      = new UnturnedLabel("flag_kit_6");
    public readonly UnturnedLabel LblNameKit6      = new UnturnedLabel("name_kit_6");
    public readonly UnturnedLabel LblWeaponKit6    = new UnturnedLabel("weapon_kit_6");
    public readonly UnturnedLabel LblIdKit6        = new UnturnedLabel("id_kit_6");
    public readonly UnturnedLabel LblFavoriteKit6  = new UnturnedLabel("txt_fav_kit_6");
    public readonly UnturnedLabel LblClassKit6     = new UnturnedLabel("class_kit_6");
    public readonly UnturnedLabel LblStatusKit6    = new UnturnedLabel("status_kit_6");
    public readonly UnturnedLabel BtnFavoriteKit6  = new UnturnedLabel("btn_fav_kit_6");
                                                   
    public readonly UnturnedUIElement Kit7         = new UnturnedUIElement("kit_7");
    public readonly UnturnedLabel LblFlagKit7      = new UnturnedLabel("flag_kit_7");
    public readonly UnturnedLabel LblNameKit7      = new UnturnedLabel("name_kit_7");
    public readonly UnturnedLabel LblWeaponKit7    = new UnturnedLabel("weapon_kit_7");
    public readonly UnturnedLabel LblIdKit7        = new UnturnedLabel("id_kit_7");
    public readonly UnturnedLabel LblFavoriteKit7  = new UnturnedLabel("txt_fav_kit_7");
    public readonly UnturnedLabel LblClassKit7     = new UnturnedLabel("class_kit_7");
    public readonly UnturnedLabel LblStatusKit7    = new UnturnedLabel("status_kit_7");
    public readonly UnturnedLabel BtnFavoriteKit7  = new UnturnedLabel("btn_fav_kit_7");
                                                   
    public readonly UnturnedUIElement Kit8         = new UnturnedUIElement("kit_8");
    public readonly UnturnedLabel LblFlagKit8      = new UnturnedLabel("flag_kit_8");
    public readonly UnturnedLabel LblNameKit8      = new UnturnedLabel("name_kit_8");
    public readonly UnturnedLabel LblWeaponKit8    = new UnturnedLabel("weapon_kit_8");
    public readonly UnturnedLabel LblIdKit8        = new UnturnedLabel("id_kit_8");
    public readonly UnturnedLabel LblFavoriteKit8  = new UnturnedLabel("txt_fav_kit_8");
    public readonly UnturnedLabel LblClassKit8     = new UnturnedLabel("class_kit_8");
    public readonly UnturnedLabel LblStatusKit8    = new UnturnedLabel("status_kit_8");
    public readonly UnturnedLabel BtnFavoriteKit8  = new UnturnedLabel("btn_fav_kit_8");
                                                   
    public readonly UnturnedUIElement Kit9         = new UnturnedUIElement("kit_9");
    public readonly UnturnedLabel LblFlagKit9      = new UnturnedLabel("flag_kit_9");
    public readonly UnturnedLabel LblNameKit9      = new UnturnedLabel("name_kit_9");
    public readonly UnturnedLabel LblWeaponKit9    = new UnturnedLabel("weapon_kit_9");
    public readonly UnturnedLabel LblIdKit9        = new UnturnedLabel("id_kit_9");
    public readonly UnturnedLabel LblFavoriteKit9  = new UnturnedLabel("txt_fav_kit_9");
    public readonly UnturnedLabel LblClassKit9     = new UnturnedLabel("class_kit_9");
    public readonly UnturnedLabel LblStatusKit9    = new UnturnedLabel("status_kit_9");
    public readonly UnturnedLabel BtnFavoriteKit9  = new UnturnedLabel("btn_fav_kit_9");

    public readonly UnturnedUIElement Kit10        = new UnturnedUIElement("kit_10");
    public readonly UnturnedLabel LblFlagKit10     = new UnturnedLabel("flag_kit_10");
    public readonly UnturnedLabel LblNameKit10     = new UnturnedLabel("name_kit_10");
    public readonly UnturnedLabel LblWeaponKit10   = new UnturnedLabel("weapon_kit_10");
    public readonly UnturnedLabel LblIdKit10       = new UnturnedLabel("id_kit_10");
    public readonly UnturnedLabel LblFavoriteKit10 = new UnturnedLabel("txt_fav_kit_10");
    public readonly UnturnedLabel LblClassKit10    = new UnturnedLabel("class_kit_10");
    public readonly UnturnedLabel LblStatusKit10   = new UnturnedLabel("status_kit_10");
    public readonly UnturnedLabel BtnFavoriteKit10 = new UnturnedLabel("btn_fav_kit_10");

    public readonly UnturnedUIElement Kit11        = new UnturnedUIElement("kit_11");
    public readonly UnturnedLabel LblFlagKit11     = new UnturnedLabel("flag_kit_11");
    public readonly UnturnedLabel LblNameKit11     = new UnturnedLabel("name_kit_11");
    public readonly UnturnedLabel LblWeaponKit11   = new UnturnedLabel("weapon_kit_11");
    public readonly UnturnedLabel LblIdKit11       = new UnturnedLabel("id_kit_11");
    public readonly UnturnedLabel LblFavoriteKit11 = new UnturnedLabel("txt_fav_kit_11");
    public readonly UnturnedLabel LblClassKit11    = new UnturnedLabel("class_kit_11");
    public readonly UnturnedLabel LblStatusKit11   = new UnturnedLabel("status_kit_11");
    public readonly UnturnedLabel BtnFavoriteKit11 = new UnturnedLabel("btn_fav_kit_11");

    public readonly UnturnedUIElement Kit12        = new UnturnedUIElement("kit_12");
    public readonly UnturnedLabel LblFlagKit12     = new UnturnedLabel("flag_kit_12");
    public readonly UnturnedLabel LblNameKit12     = new UnturnedLabel("name_kit_12");
    public readonly UnturnedLabel LblWeaponKit12   = new UnturnedLabel("weapon_kit_12");
    public readonly UnturnedLabel LblIdKit12       = new UnturnedLabel("id_kit_12");
    public readonly UnturnedLabel LblFavoriteKit12 = new UnturnedLabel("txt_fav_kit_12");
    public readonly UnturnedLabel LblClassKit12    = new UnturnedLabel("class_kit_12");
    public readonly UnturnedLabel LblStatusKit12   = new UnturnedLabel("status_kit_12");
    public readonly UnturnedLabel BtnFavoriteKit12 = new UnturnedLabel("btn_fav_kit_12");

    public readonly UnturnedUIElement Kit13        = new UnturnedUIElement("kit_13");
    public readonly UnturnedLabel LblFlagKit13     = new UnturnedLabel("flag_kit_13");
    public readonly UnturnedLabel LblNameKit13     = new UnturnedLabel("name_kit_13");
    public readonly UnturnedLabel LblWeaponKit13   = new UnturnedLabel("weapon_kit_13");
    public readonly UnturnedLabel LblIdKit13       = new UnturnedLabel("id_kit_13");
    public readonly UnturnedLabel LblFavoriteKit13 = new UnturnedLabel("txt_fav_kit_13");
    public readonly UnturnedLabel LblClassKit13    = new UnturnedLabel("class_kit_13");
    public readonly UnturnedLabel LblStatusKit13   = new UnturnedLabel("status_kit_13");
    public readonly UnturnedLabel BtnFavoriteKit13 = new UnturnedLabel("btn_fav_kit_13");

    public readonly UnturnedUIElement Kit14        = new UnturnedUIElement("kit_14");
    public readonly UnturnedLabel LblFlagKit14     = new UnturnedLabel("flag_kit_14");
    public readonly UnturnedLabel LblNameKit14     = new UnturnedLabel("name_kit_14");
    public readonly UnturnedLabel LblWeaponKit14   = new UnturnedLabel("weapon_kit_14");
    public readonly UnturnedLabel LblIdKit14       = new UnturnedLabel("id_kit_14");
    public readonly UnturnedLabel LblFavoriteKit14 = new UnturnedLabel("txt_fav_kit_14");
    public readonly UnturnedLabel LblClassKit14    = new UnturnedLabel("class_kit_14");
    public readonly UnturnedLabel LblStatusKit14   = new UnturnedLabel("status_kit_14");
    public readonly UnturnedLabel BtnFavoriteKit14 = new UnturnedLabel("btn_fav_kit_14");

    public readonly UnturnedUIElement Kit15        = new UnturnedUIElement("kit_15");
    public readonly UnturnedLabel LblFlagKit15     = new UnturnedLabel("flag_kit_15");
    public readonly UnturnedLabel LblNameKit15     = new UnturnedLabel("name_kit_15");
    public readonly UnturnedLabel LblWeaponKit15   = new UnturnedLabel("weapon_kit_15");
    public readonly UnturnedLabel LblIdKit15       = new UnturnedLabel("id_kit_15");
    public readonly UnturnedLabel LblFavoriteKit15 = new UnturnedLabel("txt_fav_kit_15");
    public readonly UnturnedLabel LblClassKit15    = new UnturnedLabel("class_kit_15");
    public readonly UnturnedLabel LblStatusKit15   = new UnturnedLabel("status_kit_15");
    public readonly UnturnedLabel BtnFavoriteKit15 = new UnturnedLabel("btn_fav_kit_15");

    public readonly UnturnedUIElement Kit16        = new UnturnedUIElement("kit_16");
    public readonly UnturnedLabel LblFlagKit16     = new UnturnedLabel("flag_kit_16");
    public readonly UnturnedLabel LblNameKit16     = new UnturnedLabel("name_kit_16");
    public readonly UnturnedLabel LblWeaponKit16   = new UnturnedLabel("weapon_kit_16");
    public readonly UnturnedLabel LblIdKit16       = new UnturnedLabel("id_kit_16");
    public readonly UnturnedLabel LblFavoriteKit16 = new UnturnedLabel("txt_fav_kit_16");
    public readonly UnturnedLabel LblClassKit16    = new UnturnedLabel("class_kit_16");
    public readonly UnturnedLabel LblStatusKit16   = new UnturnedLabel("status_kit_16");
    public readonly UnturnedLabel BtnFavoriteKit16 = new UnturnedLabel("btn_fav_kit_16");

    public readonly UnturnedUIElement Kit17        = new UnturnedUIElement("kit_17");
    public readonly UnturnedLabel LblFlagKit17     = new UnturnedLabel("flag_kit_17");
    public readonly UnturnedLabel LblNameKit17     = new UnturnedLabel("name_kit_17");
    public readonly UnturnedLabel LblWeaponKit17   = new UnturnedLabel("weapon_kit_17");
    public readonly UnturnedLabel LblIdKit17       = new UnturnedLabel("id_kit_17");
    public readonly UnturnedLabel LblFavoriteKit17 = new UnturnedLabel("txt_fav_kit_17");
    public readonly UnturnedLabel LblClassKit17    = new UnturnedLabel("class_kit_17");
    public readonly UnturnedLabel LblStatusKit17   = new UnturnedLabel("status_kit_17");
    public readonly UnturnedLabel BtnFavoriteKit17 = new UnturnedLabel("btn_fav_kit_17");

    public readonly UnturnedUIElement Kit18        = new UnturnedUIElement("kit_18");
    public readonly UnturnedLabel LblFlagKit18     = new UnturnedLabel("flag_kit_18");
    public readonly UnturnedLabel LblNameKit18     = new UnturnedLabel("name_kit_18");
    public readonly UnturnedLabel LblWeaponKit18   = new UnturnedLabel("weapon_kit_18");
    public readonly UnturnedLabel LblIdKit18       = new UnturnedLabel("id_kit_18");
    public readonly UnturnedLabel LblFavoriteKit18 = new UnturnedLabel("txt_fav_kit_18");
    public readonly UnturnedLabel LblClassKit18    = new UnturnedLabel("class_kit_18");
    public readonly UnturnedLabel LblStatusKit18   = new UnturnedLabel("status_kit_18");
    public readonly UnturnedLabel BtnFavoriteKit18 = new UnturnedLabel("btn_fav_kit_18");

    public readonly UnturnedUIElement Kit19        = new UnturnedUIElement("kit_19");
    public readonly UnturnedLabel LblFlagKit19     = new UnturnedLabel("flag_kit_19");
    public readonly UnturnedLabel LblNameKit19     = new UnturnedLabel("name_kit_19");
    public readonly UnturnedLabel LblWeaponKit19   = new UnturnedLabel("weapon_kit_19");
    public readonly UnturnedLabel LblIdKit19       = new UnturnedLabel("id_kit_19");
    public readonly UnturnedLabel LblFavoriteKit19 = new UnturnedLabel("txt_fav_kit_19");
    public readonly UnturnedLabel LblClassKit19    = new UnturnedLabel("class_kit_19");
    public readonly UnturnedLabel LblStatusKit19   = new UnturnedLabel("status_kit_19");
    public readonly UnturnedLabel BtnFavoriteKit19 = new UnturnedLabel("btn_fav_kit_19");

    public readonly UnturnedUIElement Kit20        = new UnturnedUIElement("kit_20");
    public readonly UnturnedLabel LblFlagKit20     = new UnturnedLabel("flag_kit_20");
    public readonly UnturnedLabel LblNameKit20     = new UnturnedLabel("name_kit_20");
    public readonly UnturnedLabel LblWeaponKit20   = new UnturnedLabel("weapon_kit_20");
    public readonly UnturnedLabel LblIdKit20       = new UnturnedLabel("id_kit_20");
    public readonly UnturnedLabel LblFavoriteKit20 = new UnturnedLabel("txt_fav_kit_20");
    public readonly UnturnedLabel LblClassKit20    = new UnturnedLabel("class_kit_20");
    public readonly UnturnedLabel LblStatusKit20   = new UnturnedLabel("status_kit_20");
    public readonly UnturnedLabel BtnFavoriteKit20 = new UnturnedLabel("btn_fav_kit_20");

    /* ARRAYS */

    // kit info
    public readonly UnturnedLabel[] IncludedItemsText;
    public readonly UnturnedLabel[] IncludedItemsIcons;
    public readonly UnturnedLabel[] IncludedItemsAmounts;

    // kit list
    public readonly UnturnedUIElement[] Kits;
    public readonly UnturnedLabel[] FlagLabels;
    public readonly UnturnedLabel[] NameLabels;
    public readonly UnturnedLabel[] WeaponLabels;
    public readonly UnturnedLabel[] IdLabels;
    public readonly UnturnedLabel[] FavoriteLabels;
    public readonly UnturnedLabel[] ClassLabels;
    public readonly UnturnedLabel[] StatusLabels;
    public readonly UnturnedLabel[] FavoriteButtons;

    public readonly UnturnedButton[] DropdownButtons;
    public KitMenuUI() : base(12013, Gamemode.Config.UIKitMenu)
    {
        DropdownButtons = new UnturnedButton[(int)ClassConverter.MaxClass + 1];
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
            if (DropdownButtons[i] is not { } btn)
                L.LogWarning("DropdownButtons[" + i + "] was not initialized (class: " + (Class)i + ").", method: "KIT MENU UI");
            else
            {
                btn.OnClicked += OnClassButtonClicked;
            }
        }

        IncludedItemsText    = new UnturnedLabel[]
        {
            LblInfoIncludedItemText1,
            LblInfoIncludedItemText2,
            LblInfoIncludedItemText3,
            LblInfoIncludedItemText4,
            LblInfoIncludedItemText5,
            LblInfoIncludedItemText6,
            LblInfoIncludedItemText7,
            LblInfoIncludedItemText8,
            LblInfoIncludedItemText9,
            LblInfoIncludedItemText10,
            LblInfoIncludedItemText11,
            LblInfoIncludedItemText12,
            LblInfoIncludedItemText13,
            LblInfoIncludedItemText14,
            LblInfoIncludedItemText15,
            LblInfoIncludedItemText16,
            LblInfoIncludedItemText17
        };
        IncludedItemsIcons   = new UnturnedLabel[]
        {
            LblInfoIncludedItemIcon1,
            LblInfoIncludedItemIcon2,
            LblInfoIncludedItemIcon3,
            LblInfoIncludedItemIcon4,
            LblInfoIncludedItemIcon5,
            LblInfoIncludedItemIcon6,
            LblInfoIncludedItemIcon7,
            LblInfoIncludedItemIcon8,
            LblInfoIncludedItemIcon9,
            LblInfoIncludedItemIcon10,
            LblInfoIncludedItemIcon11,
            LblInfoIncludedItemIcon12,
            LblInfoIncludedItemIcon13,
            LblInfoIncludedItemIcon14,
            LblInfoIncludedItemIcon15,
            LblInfoIncludedItemIcon16,
            LblInfoIncludedItemIcon17
        };
        IncludedItemsAmounts = new UnturnedLabel[]
        {
            LblInfoIncludedItemAmount1,
            LblInfoIncludedItemAmount2,
            LblInfoIncludedItemAmount3,
            LblInfoIncludedItemAmount4,
            LblInfoIncludedItemAmount5,
            LblInfoIncludedItemAmount6,
            LblInfoIncludedItemAmount7,
            LblInfoIncludedItemAmount8,
            LblInfoIncludedItemAmount9,
            LblInfoIncludedItemAmount10,
            LblInfoIncludedItemAmount11,
            LblInfoIncludedItemAmount12,
            LblInfoIncludedItemAmount13,
            LblInfoIncludedItemAmount14,
            LblInfoIncludedItemAmount15,
            LblInfoIncludedItemAmount16,
            LblInfoIncludedItemAmount17
        };
        Kits                 = new UnturnedUIElement[]
        {
            Kit1, Kit2, Kit3, Kit4, Kit5, Kit6, Kit7, Kit8, Kit9,
            Kit10, Kit11, Kit12, Kit13, Kit14, Kit15, Kit16, Kit17,
            Kit18, Kit19, Kit20
        };
        FlagLabels           = new UnturnedLabel[]
        {
            LblFlagKit1, LblFlagKit2, LblFlagKit3, LblFlagKit4, LblFlagKit5, LblFlagKit6, LblFlagKit7, LblFlagKit8,
            LblFlagKit9,
            LblFlagKit10, LblFlagKit11, LblFlagKit12, LblFlagKit13, LblFlagKit14, LblFlagKit15, LblFlagKit16,
            LblFlagKit17,
            LblFlagKit18, LblFlagKit19, LblFlagKit20
        };
        NameLabels           = new UnturnedLabel[]
        {
            LblNameKit1, LblNameKit2, LblNameKit3, LblNameKit4, LblNameKit5, LblNameKit6, LblNameKit7, LblNameKit8,
            LblNameKit9,
            LblNameKit10, LblNameKit11, LblNameKit12, LblNameKit13, LblNameKit14, LblNameKit15, LblNameKit16,
            LblNameKit17,
            LblNameKit18, LblNameKit19, LblNameKit20
        };
        WeaponLabels         = new UnturnedLabel[]
        {
            LblWeaponKit1, LblWeaponKit2, LblWeaponKit3, LblWeaponKit4, LblWeaponKit5, LblWeaponKit6, LblWeaponKit7,
            LblWeaponKit8, LblWeaponKit9,
            LblWeaponKit10, LblWeaponKit11, LblWeaponKit12, LblWeaponKit13, LblWeaponKit14, LblWeaponKit15,
            LblWeaponKit16, LblWeaponKit17,
            LblWeaponKit18, LblWeaponKit19, LblWeaponKit20
        };
        IdLabels             = new UnturnedLabel[]
        {
            LblIdKit1, LblIdKit2, LblIdKit3, LblIdKit4, LblIdKit5, LblIdKit6, LblIdKit7, LblIdKit8, LblIdKit9,
            LblIdKit10, LblIdKit11, LblIdKit12, LblIdKit13, LblIdKit14, LblIdKit15, LblIdKit16, LblIdKit17,
            LblIdKit18, LblIdKit19, LblIdKit20
        };
        FavoriteLabels       = new UnturnedLabel[]
        {
            LblFavoriteKit1, LblFavoriteKit2, LblFavoriteKit3, LblFavoriteKit4, LblFavoriteKit5, LblFavoriteKit6,
            LblFavoriteKit7, LblFavoriteKit8, LblFavoriteKit9,
            LblFavoriteKit10, LblFavoriteKit11, LblFavoriteKit12, LblFavoriteKit13, LblFavoriteKit14, LblFavoriteKit15,
            LblFavoriteKit16, LblFavoriteKit17,
            LblFavoriteKit18, LblFavoriteKit19, LblFavoriteKit20
        };
        ClassLabels          = new UnturnedLabel[]
        {
            LblClassKit1, LblClassKit2, LblClassKit3, LblClassKit4, LblClassKit5, LblClassKit6, LblClassKit7,
            LblClassKit8, LblClassKit9,
            LblClassKit10, LblClassKit11, LblClassKit12, LblClassKit13, LblClassKit14, LblClassKit15, LblClassKit16,
            LblClassKit17,
            LblClassKit18, LblClassKit19, LblClassKit20
        };
        StatusLabels         = new UnturnedLabel[]
        {
            LblStatusKit1, LblStatusKit2, LblStatusKit3, LblStatusKit4, LblStatusKit5, LblStatusKit6, LblStatusKit7,
            LblStatusKit8, LblStatusKit9,
            LblStatusKit10, LblStatusKit11, LblStatusKit12, LblStatusKit13, LblStatusKit14, LblStatusKit15,
            LblStatusKit16, LblStatusKit17,
            LblStatusKit18, LblStatusKit19, LblStatusKit20
        };
        FavoriteButtons      = new UnturnedLabel[]
        {
            BtnFavoriteKit1, BtnFavoriteKit2, BtnFavoriteKit3, BtnFavoriteKit4, BtnFavoriteKit5, BtnFavoriteKit6,
            BtnFavoriteKit7, BtnFavoriteKit8, BtnFavoriteKit9,
            BtnFavoriteKit10, BtnFavoriteKit11, BtnFavoriteKit12, BtnFavoriteKit13, BtnFavoriteKit14, BtnFavoriteKit15,
            BtnFavoriteKit16, BtnFavoriteKit17,
            BtnFavoriteKit18, BtnFavoriteKit19, BtnFavoriteKit20
        };

        BtnActionsAction.OnClicked += OnActionButtonClicked;
        BtnActionsStaff1.OnClicked += OnStaffButton1Clicked;
        BtnActionsStaff2.OnClicked += OnStaffButton2Clicked;
        BtnActionsStaff3.OnClicked += OnStaffButton3Clicked;
    }

    private void OnClassButtonClicked(UnturnedButton button, Player player)
    {
        Class @class = (Class)Array.IndexOf(DropdownButtons, button);
        if (@class > ClassConverter.MaxClass)
            return;
        ITransportConnection tc = player.channel.owner.transportConnection;

        // update dropdown text to translated value
        DropdownSelectedName.SetText(tc, Localization.TranslateEnum(@class, player.channel.owner.playerID.steamID.m_SteamID));
        DropdownSelectedClass.SetText(tc, new string(@class.GetIcon(), 1));

        // todo update list
    }
    private void OnActionButtonClicked(UnturnedButton button, Player player)
    {
        // todo determine action, perform action
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
}
