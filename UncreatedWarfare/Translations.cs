using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Kits;
using UnityEngine;

namespace Uncreated.Warfare;

partial class JSONMethods
{
    public static void CreateDefaultTranslations()
    {
        DefaultTranslations = new Dictionary<string, string>
        {
            { Localization.Common.NOT_IMPLEMENTED,        "<color=#ff8c69>This command hasn't been implemented yet.</color>" },
            { Localization.Common.CORRECT_USAGE,          "<color=#ff8c69>Correct usage: {0}</color>" },
            { Localization.Common.CONSOLE_ONLY,           "<color=#ff8c69>This command can not be called from console.</color>" },
            { Localization.Common.PLAYERS_ONLY,           "<color=#ff8c69>This command can only called from console.</color>" },
            { Localization.Common.PLAYER_NOT_FOUND,       "<color=#ff8c69>Player not found.</color>" },
            { Localization.Common.UNKNOWN_ERROR,          "<color=#ff8c69>We ran into an unknown error executing that command.</color>" },
            { Localization.Common.GAMEMODE_ERROR,         "<color=#ffa238>This command is not enabled in this gamemode.</color>" },
            { Localization.Common.NO_PERMISSIONS,         "<color=#ff8c69>You do not have permission to use this command.</color>" },
            { Localization.Common.NOT_ENABLED,            "<color=#ff8c69>This feature is not currently enabled.</color>" },
            { Localization.Common.NO_PERMISSIONS_ON_DUTY, "<color=#ff8c69>You must be on duty to execute that command.</color>" },
            { "gamemode_not_flag_gamemode", "<color=#ff8c69>Current gamemode <color=#ff758f>{0}</color> is not a <color=#ff758f>FLAG GAMEMODE</color>.</color>" },
            { "gamemode_flag_not_on_cap_team", "<color=#ff8c69>You're not on a team that can capture flags.</color>" },
            { "gamemode_flag_not_on_cap_team_console", "That team can not capture flags." },
            { "entered_main", "<color=#e6e3d5>You have entered the safety of {0} headquarters!</color>" },
            { "left_main", "<color=#e6e3d5>You have left the safety of {0} headquarters.</color>" },
            { "entered_cap_radius", "You have entered the capture radius of <color=#{1}>{0}</color>." },
            { "left_cap_radius", "You have left the cap radius of <color=#{1}>{0}</color>." },
            { "capturing", "Your team is capturing this point!" },
            { "losing", "Your team is losing this point!" },
            { "contested", "<color=#{1}>{0}</color> is contested! Eliminate all enemies to secure it." },
            { "clearing", "Your team is busy clearing this point." },
            { "secured", "This point is secure for now. Keep up the defense." },
            { "nocap", "This point is not your objective, check the right of your screen to see which points to attack and defend." },
            { "notowned", "This point is owned by the enemies. Get more players to capture it." },
            { "locked", "This point has already been captured, try to protect the objective to win." },
            { "flag_neutralized", "<color=#{1}>{0}</color> has been neutralized!" },
            { "team_1", "USA" },
            { "team_2", "Middle Eastern Coalition" },
            { "team_3", "Admins" },
            { "teams_join_success", "<color=#a0ad8e>You've joined {0}.</color>" },
            { "teams_join_announce", "<color=#a0ad8e>{0} joined <color=#{2}>{1}</color>!</color>" },
            { "join_player_joined_console", "{0} ({1}) changed group: {3} >> {2}" },
            { "neutral", "Neutral" },
            { "undiscovered_flag", "unknown" },
            { "ui_capturing", "CAPTURING" },
            { "ui_losing", "LOSING" },
            { "ui_clearing", "CLEARING" },
            { "ui_contested", "CONTESTED" },
            { "ui_secured", "SECURED" },
            { "ui_nocap", "NOT OBJECTIVE" },
            { "ui_notowned", "TAKEN" },
            { "ui_locked", "LOCKED" },
            { "ui_in_vehicle", "IN VEHICLE" },
            { "team_win", "<color=#{1}>{0}</color> has won the battle!" },
            { "team_capture", "<color=#{1}>{0}</color> captured <color=#{3}>{2}</color>!" },
            { "player_connected", "<color=#e6e3d5><color=#ffff1a>{0}</color> joined the server!</color>" },
            { "player_disconnected", "<color=#e6e3d5><color=#ffff1a>{0}</color> left the server.</color>" },
            { "flag_header", "Flags" },
            { "null_transform_kick_message", "Your character is bugged, which messes up our zone plugin. Rejoin or contact a Director if this continues. (discord.gg/{0})." },
            { "text_chat_feedback_chat_filter", "<color=#ff8c69>Our chat filter flagged <color=#fdfdfd>{0}</color>, so the message wasn't sent." },

            #region Leaderboard
            // universal
            { "lb_next_game", "Starting soon..." },
            { "lb_next_game_shut_down", "<color=#94cbff>Shutting Down Because: \"{0}\"</color>" },
            { "lb_next_game_time_format", "{0:mm\\:ss}" },
            { "lb_warstats_header", "<color=#{1}>{0}</color> vs <color=#{3}>{2}</color>" },
            { "lb_playerstats_header", "<color=#{1}>{0}</color> - {2:n0}% presence" },
            { "lb_playerstats_header_backup", "<color=#{1}>{0}</color>" },
            { "lb_winner_title", "<color=#{1}>{0}</color> Wins!" },
            { "lb_longest_shot", "{0}m - {1}\n{2}" },

            // ctf
            { "ctf_lb_playerstats_0", "Kills: " },
            { "ctf_lb_playerstats_1", "Deaths: " },
            { "ctf_lb_playerstats_2", "K/D Ratio: " },
            { "ctf_lb_playerstats_3", "Kills on Point: " },
            { "ctf_lb_playerstats_4", "Time Deployed: " },
            { "ctf_lb_playerstats_5", "XP Gained: " },
            { "ctf_lb_playerstats_6", "Time on Point: " },
            { "ctf_lb_playerstats_7", "Captures: " },
            { "ctf_lb_playerstats_8", "Time in Vehicle: " },
            { "ctf_lb_playerstats_9", "Teamkills: " },
            { "ctf_lb_playerstats_10", "FOBs Destroyed: " },
            { "ctf_lb_playerstats_11", "Credits Gained: " },

            { "ctf_lb_warstats_0", "Duration: " },
            { "ctf_lb_warstats_1", "US Casualties: " },
            { "ctf_lb_warstats_2", "MEC Casualties: " },
            { "ctf_lb_warstats_3", "Flag Captures: " },
            { "ctf_lb_warstats_4", "US Average Army: " },
            { "ctf_lb_warstats_5", "MEC Average Army: " },
            { "ctf_lb_warstats_6", "US FOBs Placed: " },
            { "ctf_lb_warstats_7", "MEC FOBs Placed: " },
            { "ctf_lb_warstats_8", "US FOBs Destroyed: " },
            { "ctf_lb_warstats_9", "MEC FOBs Destroyed: " },
            { "ctf_lb_warstats_10", "Teamkill Casualties: " },
            { "ctf_lb_warstats_11", "Longest Shot: " },

            { "ctf_lb_header_0", "Kills" },
            { "ctf_lb_header_1", "Deaths" },
            { "ctf_lb_header_2", "XP" },
            { "ctf_lb_header_3", "Credits" },
            { "ctf_lb_header_4", "Caps" },
            { "ctf_lb_header_5", "Damage" },

            // insurgency
            { "ins_lb_playerstats_0", "Kills: " },
            { "ins_lb_playerstats_1", "Deaths: " },
            { "ins_lb_playerstats_2", "Damage Done: " },
            { "ins_lb_playerstats_3", "Objective Kills: " },
            { "ins_lb_playerstats_4", "Time Deployed: " },
            { "ins_lb_playerstats_5", "XP Gained: " },
            { "ins_lb_playerstats_6", "Intelligence Gathered: " },
            { "ins_lb_playerstats_7", "Caches Discovered: " },
            { "ins_lb_playerstats_8", "Caches Destroyed: " },
            { "ins_lb_playerstats_9", "Teamkills: " },
            { "ins_lb_playerstats_10", "FOBs Destroyed: " },
            { "ins_lb_playerstats_11", "Credits Gained: " },

            { "ins_lb_warstats_0", "Duration: " },
            { "ins_lb_warstats_1", "US Casualties: " },
            { "ins_lb_warstats_2", "MEC Casualties: " },
            { "ins_lb_warstats_3", "Intelligence Gathered: " },
            { "ins_lb_warstats_4", "US Average Army: " },
            { "ins_lb_warstats_5", "MEC Average Army: " },
            { "ins_lb_warstats_6", "US FOBs Placed: " },
            { "ins_lb_warstats_7", "MEC FOBs Placed: " },
            { "ins_lb_warstats_8", "US FOBs Destroyed: " },
            { "ins_lb_warstats_9", "MEC FOBs Destroyed: " },
            { "ins_lb_warstats_10", "Teamkill Casualties: " },
            { "ins_lb_warstats_11", "Longest Shot: " },

            { "ins_lb_header_0", "Kills" },
            { "ins_lb_header_1", "Deaths" },
            { "ins_lb_header_2", "XP" },
            { "ins_lb_header_3", "Credits" },
            { "ins_lb_header_4", "KDR" },
            { "ins_lb_header_5", "Damage" },

            // conquest
            { "cqt_lb_playerstats_0", "Kills: " },
            { "cqt_lb_playerstats_1", "Deaths: " },
            { "cqt_lb_playerstats_2", "Damage Done: " },
            { "cqt_lb_playerstats_3", "Objective Kills: " },
            { "cqt_lb_playerstats_4", "Time Deployed: " },
            { "cqt_lb_playerstats_5", "XP Gained: " },
            { "cqt_lb_playerstats_6", "Revives: " },
            { "cqt_lb_playerstats_7", "Flags Captured: " },
            { "cqt_lb_playerstats_8", "Time on Flag: " },
            { "cqt_lb_playerstats_9", "Teamkills: " },
            { "cqt_lb_playerstats_10", "FOBs Destroyed: " },
            { "cqt_lb_playerstats_11", "Credits Gained: " },

            { "cqt_lb_warstats_0", "Duration: " },
            { "cqt_lb_warstats_1", "US Casualties: " },
            { "cqt_lb_warstats_2", "MEC Casualties: " },
            { "cqt_lb_warstats_3", "Flag Captures: " },
            { "cqt_lb_warstats_4", "US Average Army: " },
            { "cqt_lb_warstats_5", "MEC Average Army: " },
            { "cqt_lb_warstats_6", "US FOBs Placed: " },
            { "cqt_lb_warstats_7", "MEC FOBs Placed: " },
            { "cqt_lb_warstats_8", "US FOBs Destroyed: " },
            { "cqt_lb_warstats_9", "MEC FOBs Destroyed: " },
            { "cqt_lb_warstats_10", "Teamkill Casualties: " },
            { "cqt_lb_warstats_11", "Longest Shot: " },

            { "cqt_lb_header_0", "Kills" },
            { "cqt_lb_header_1", "Deaths" },
            { "cqt_lb_header_2", "XP" },
            { "cqt_lb_header_3", "Credits" },
            { "cqt_lb_header_4", "KDR" },
            { "cqt_lb_header_5", "Damage" },
            #endregion

            #region GroupCommand
            { "group_usage", "<color=#ff8c69>Syntax: <i>/group [ join [id] | create [name] ].</i></color>" },
            { "current_group", "<color=#e6e3d5>Group <color=#4785ff>{0}</color>: <color=#4785ff>{1}</color>.</color>" },
            { "cant_create_group", "<color=#ff8c69>You can't create a group right now.</color>" },
            { "created_group", "<color=#e6e3d5>Created group <color=#4785ff>{0}</color>: <color=#4785ff>{1}</color>.</color>" },
            { "created_group_console", "{0} ({1}) created group \"{2}\": \"{3}\"" },
            { "not_in_group", "<color=#ff8c69>You aren't in a group.</color>" },
            { "joined_group", "<color=#e6e3d5>You have joined group {0}: <color=#4785ff>{1}</color>.</color>" },
            { "joined_already_in_group", "<color=#ff8c69>You are already in that group.</color>" },
            { "joined_group_not_found", "<color=#ff8c69>Could not find group <color=#4785ff>{0}</color>.</color>" },
            { "joined_group_console", "{0} ({1}) joined group \"{2}\": \"{3}\"." },
            #endregion

            #region LangCommand
            { "language_list", "<color=#f53b3b>Languages: <color=#e6e3d5>{0}</color>.</color>" },
            { "language_current", "<color=#f53b3b>Current language: <color=#e6e3d5>{0}</color>.</color>" },
            { "changed_language", "<color=#f53b3b>Changed your language to <color=#e6e3d5>{0}</color>.</color>" },
            { "change_language_not_needed", "<color=#f53b3b>You are already set to <color=#e6e3d5>{0}</color>.</color>" },
            { "reset_language", "<color=#f53b3b>Reset your language to <color=#e6e3d5>{0}</color>.</color>" },
            { "reset_language_how", "<color=#f53b3b>Do <color=#e6e3d5>/lang reset</color> to reset back to default language.</color>" },
            { "dont_have_language", "<color=#dd1111>We don't have translations for <color=#e6e3d5>{0}</color> yet. If you are fluent and want to help, feel free to ask us about submitting translations.</color>" },
            { "reset_language_not_needed", "<color=#dd1111>You are already on the default language: <color=#e6e3d5>{0}</color>.</color>" },
            #endregion

            #region Toasts
            { "welcome_message", "Thanks for playing <color=#{0}>Uncreated Warfare</color>!\nWelcome back <color=#{2}>{1}</color>." },
            { "welcome_message_first_time", "Welcome to <color=#{0}>Uncreated Warfare</color>!\nTalk to the NPCs to get started." },
            #endregion
            
            #region KitCommand
            { "kit_created", "<color=#a0ad8e>Created kit: <color=#ffffff>{0}</color></color>" },
            { "kit_search_results", "<color=#a0ad8e>Matches: <i>{0}</i>.</color>" },
            { "kit_overwritten", "<color=#a0ad8e>Overwritten items for kit: <color=#ffffff>{0}</color></color>" },
            { "kit_copied", "<color=#a0ad8e>Copied data from <color=#c7b197>{0}</color></color> into new kit: <color=#ffffff>{1}</color></color>" },
            { "kit_deleted", "<color=#a0ad8e>Deleted kit: <color=#ffffff>{0}</color></color>" },
            { "kit_setprop", "<color=#a0ad8e>Set <color=#8ce4ff>{0}</color> for kit <color=#ffb89c>{1}</color> to: <color=#ffffff>{2}</color></color>" },
            { "kit_accessgiven", "<color=#a0ad8e>Allowed player: <color=#e06969>{0}</color> to access the kit: <color=#ffffff>{1}</color></color>" },
            { "kit_accessgiven_dm", "<color=#a0ad8e>You were just given access to the kit: <color=#ffffff>{0}</color>.</color>" },
            { "kit_accessremoved_dm", "<color=#a0ad8e>You were just denied access to the kit: <color=#ffffff>{0}</color>.</color>" },
            { "kit_accessremoved", "<color=#a0ad8e>Disallowed player: <color=#e06969>{0}</color> to access the kit: <color=#ffffff>{1}</color></color>" },
            { "kit_e_exist", "<color=#ff8c69>A kit called {0} already exists.</color>" },
            { "kit_e_noexist", "<color=#ff8c69>A kit called {0} does not exist.</color>" },
            { "kit_e_invalidprop", "<color=#ff8c69>{0} isn't a valid a kit property. Try putting 'Class', 'Cost', 'IsPremium', etc.</color>" },
            { "kit_e_invalidarg", "<color=#ff8c69>{0} is not a valid value for kit property: {1}</color>" },
            { "kit_e_invalidarg_not_allowed", "<color=#ff8c69>{0} is not a valid property, or it cannot be changed.</color>" },
            { "kit_e_noplayer", "<color=#ff8c69>No player found by the name of '{0}'.</color>" },
            { "kit_e_alreadyaccess", "<color=#dbc48f>Player {0} already has access to the kit: {1}.</color>" },
            { "kit_e_noaccess", "<color=#dbc48f>Player {0} already does not have access to that: {1}.</color>" },
            { "kit_e_cooldown", "<color=#c2b39b>You can request this kit again in: <color=#bafeff>{0}</color></color>" },
            { "kit_e_cooldownglobal", "<color=#c2b39b>You can request another kit in: <color=#bafeff>{0}</color></color>" },
            { "kit_l_e_playernotfound", "<color=#ff8c69>Could not find player with the Steam64 ID: {0}</color>" },
            { "kit_l_e_kitexists", "<color=#ff8c69>Something went wrong and this loadout could not be created (loadout already exists)</color>" },
            { "kit_l_created", "<color=#a0ad8e>Created <color=#c4c9bb>{0}</color> loadout for <color=#deb692>{1}</color> (<color=#968474>{2}</color>). Kit name: <color=#ffffff>{3}</color></color>" },
            #endregion
            
            #region RangeCommand
            { "range", "<color=#9e9c99>The range to your squad's marker is: <color=#8aff9f>{0}m</color></color>" },
            { "range_nomarker", "<color=#9e9c99>You squad has no marker.</color>" },
            { "range_notsquadleader", "<color=#9e9c99>Only <color=#cedcde>SQUAD LEADERS</color> can place markers.</color>" },
            { "range_notinsquad", "<color=#9e9c99>You must JOIN A SQUAD in order to do /range.</color>" },
            #endregion
            
            #region SquadCommand
            { "squad_not_in_team", "<color=#a89791>You can't join a squad unless you're on a team.</color>" },
            { "squad_created", "<color=#a0ad8e>You created the squad <color=#ffffff>{0}</color></color>" },
            { "squad_ui_reloaded", "<color=#a0ad8e>Squad UI has been reloaded.</color>" },
            { "squad_joined", "<color=#a0ad8e>You joined <color=#ffffff>{0}</color>.</color>" },
            { "squad_left", "<color=#a7a8a5>You left your squad.</color>" },
            { "squad_disbanded", "<color=#a7a8a5>Your squad was disbanded.</color>" },
            { "squad_locked", "<color=#a7a8a5>You <color=#6be888>locked</color> your squad.</color>" },
            { "squad_unlocked", "<color=#999e90>You <color=#ffffff>unlocked</color> your squad.</color>" },
            { "squad_promoted", "<color=#b9bdb3><color=#ffc94a>{0}</color> was promoted to squad leader.</color>" },
            { "squad_kicked", "<color=#b9bdb3>You were kicked from Squad <color=#6be888>{0}</color>.</color>" },
            { "squad_e_noexist", "<color=#a89791>Could not find a squad called '{0}'.</color>" },
            { "squad_e_insquad", "<color=#ff8c69>You are already in a squad!</color>" },
            { "squad_e_notinsquad", "<color=#a89791>You are not in a squad!</color>" },
            { "squad_e_notsquadleader", "<color=#ff8c69>You are not a squad leader!</color>" },
            { "squad_e_locked", "<color=#a89791>That squad is locked.</color>" },
            { "squad_e_full", "<color=#a89791>That squad is full.</color>" },
            { "squad_e_playernotinsquad", "<color=#a89791>That player is not in your squad.</color>" },
            { "squad_e_playernotfound", "<color=#a89791>Could not find player: '{0}'.</color>" },
            { "squad_player_joined", "<color=#b9bdb3><color=#8df0c5>{0}</color> joined your squad.</color>" },
            { "squad_player_left", "<color=#b9bdb3><color=#f0c08d>{0}</color> left your squad.</color>" },
            { "squad_player_promoted", "<color=#b9bdb3><color=#ffc94a>{0}</color> was promoted to squad leader.</color>" },
            { "squad_player_kicked", "<color=#b9bdb3><color=#d68f81>{0}</color> was kicked from your squad.</color>" },
            { "squad_squadleader", "<color=#b9bdb3>You are now the <color=#ffc94a>squad leader</color>.</color>" },
            { "squads_disabled", "<color=#ff8c69>Squads are disabled.</color>" },
            { "squad_too_many", "<color=#ff8c69>There can not be more than 8 squads on a team at once.</color>" },

            { "squad_ui_player_name", "{0}" },
            { "squad_ui_player_count", "<color=#bd6b5b>{0}</color> {1}/6" },
            { "squad_ui_player_count_small", "{0}/6" },
            { "squad_ui_player_count_small_locked", "<color=#969696>{0}/6</color>" },
            { "squad_ui_header_name", "<color=#bd6b5b>{0}</color> {1}/6" },
            { "squad_ui_leader_name", "{0}" },
            { "squad_ui_expanded", "..." },
            #endregion
            
            #region OrderCommand
            { "order_usage_1", "<color=#9fa1a6>To give orders: <color=#9dbccf>/order <squad> <type></color>. Type <color=#d1bd90>/order actions</color> to see a list of actions.</color>" },
            { "order_actions", "<color=#9fa1a6>Order actions: <color=#9dbccf>{0}</color></color>" },
            { "order_usage_2", "<color=#9fa1a6>Try typing: <color=#9dbccf>/order <b>{0}</b> <i>action</i></color>" },
            { "order_usage_3", "<color=#9fa1a6>Try typing: <color=#9dbccf>/order {0} <b><action></b></color>. Type <color=#d1bd90>/order actions</color> to see a list of actions.</color>" },
            { "order_e_squadnoexist", "<color=#9fa1a6>There is no friendly squad called '{0}'.</color>" },
            { "order_e_not_squadleader", "<color=#9fa1a6>You must be the leader of a squad to give orders.</color>" },
            { "order_e_actioninvalid", "<color=#9fa1a6>'{0}' is not a valid action. Try one of these: {1}</color>" },
            { "order_e_attack_marker", "<color=#9fa1a6>Place a map marker on a <color=#d1bd90>position</color> or <color=#d1bd90>flag</color> where you want {0} to attack.</color>" },
            { "order_e_attack_marker_ins", "<color=#9fa1a6>Place a map marker on a <color=#d1bd90>position</color> or <color=#d1bd90>cache</color> where you want {0} to attack.</color>" },
            { "order_e_defend_marker", "<color=#9fa1a6>Place a map marker on the <color=#d1bd90>position</color> or <color=#d1bd90>flag</color> where you want {0} to attack.</color>" },
            { "order_e_defend_marker_ins", "<color=#9fa1a6>Place a map marker on the <color=#d1bd90>position</color> or <color=#d1bd90>cache</color> where you want {0} to attack.</color>" },
            { "order_e_buildfob_marker", "<color=#9fa1a6>Place a map marker where you want {0} to build a <color=#d1bd90>FOB</color>.</color>" },
            { "order_e_move_marker", "<color=#9fa1a6>Place a map marker where you want {0} to move to.</color>" },
            { "order_e_buildfob_fobexists", "<color=#9fa1a6>There is already a friendly FOB near that marker.</color>" },
            { "order_e_buildfob_foblimit", "<color=#9fa1a6>The max FOB limit has been reached.</color>" },
            { "order_e_squadtooclose", "<color=#9fa1a6>{0} is already near that marker. Try placing it further away.</color>" },
            { "order_e_raycast", "<color=#b58b86>.</color>" },
            { "order_s_sent", "<color=#9fa1a6>Order sent to {0}: <color=#9dbccf>{1}</color></color>" },
            { "order_s_received", "<color=#9fa1a6><color=#9dbccf>{0}</color> has given your squad new orders:\n<color=#d4d4d4>{1}</color></color>" },
            { "order_ui_commander", "Orders from <color=#a7becf>{0}</color>:" },
            { "order_ui_text", "{0}" },
            { "order_ui_time", "- {0}m left" },
            { "order_ui_reward", "- Reward: {0}" },
            { "order_attack_objective", "Attack your objective: {0}." },
            { "order_attack_flag", "Attack {0}." },
            { "order_defend_objective", "Defend your objective: {0}." },
            { "order_defend_flag", "Defend {0}." },
            { "order_attack_cache", "Attack {0}." },
            { "order_defend_cache", "Defend {0}." },
            { "order_attack_area", "Attack near {0}." },
            { "order_defend_area", "Defend near {0}." },
            { "order_buildfob_flag", "Build a FOB on {0}." },
            { "order_buildfob_cache", "Build a FOB near {0}." },
            { "order_buildfob_area", "Build a FOB in {0}." },
            #endregion
            
            #region RallyCommand
            { "rally_success", "<color=#959c8c>You have <color=#5eff87>rallied</color> with your squad.</color>" },
            { "rally_active", "<color=#959c8c>Squad <color=#5eff87>RALLY POINT</color> is now active. Do '<color=#bfbfbf>/rally</color>' to rally with your squad.</color>" },
            { "rally_wait", "<color=#959c8c>Standby for <color=#5eff87>RALLY</color> in: <color=#ffe4b5>{0}s</color>. Do '<color=#a3b4c7>/rally cancel</color>' to abort.</color>" },
            { "rally_aborted", "<color=#a1a1a1>Cancelled rally deployment.</color>" },
            { "rally_cancelled", "<color=#959c8c><color=#bfbfbf>RALLY</color> is no longer available - there are enemies nearby.</color>" },
            { "rally_e_unavailable", "<color=#ad9990>Rally is unavailable right now.</color>" },
            { "rally_e_enemies", "<color=#ad9990>There are enemies nearby.</color>" },
            { "rally_e_nosquadmember", "<color=#99918d>You need at least <color=#cedcde>1 SQUAD MEMBER</color> near you in order to place this.</color>" },
            { "rally_e_notsquadleader", "<color=#99918d>You must be a <color=#cedcde>SQUAD LEADER</color> in order to place this.</color>" },
            { "rally_e_alreadywaiting", "<color=#959c8c>You are already waiting on rally deployment. Do '<color=#a3b4c7>/rally cancel</color>' to abort.</color>" },
            { "rally_e_notwaiting", "<color=#959c8c>You are not awaiting rally deployment.</color>" },
            { "rally_e_notinsquad", "<color=#959c8c>You are not in a squad.</color>" },
            { "rally_e_obstructed", "<color=#959c8c>This rally point is obstructed, find a more open place to put it.</color>" },
            { "rally_ui", "<color=#5eff87>RALLY</color> {0}" },
            { "rally_time_value", " {0:mm\\:ss}" },
            #endregion
            
            #region Time Formatting
            { "time_second", "second" },
            { "time_seconds", "seconds" },
            { "time_minute", "minute" },
            { "time_minutes", "minutes" },
            { "time_hour", "hour" },
            { "time_hours", "hours" },
            { "time_day", "day" },
            { "time_days", "days" },
            { "time_month", "month" },
            { "time_months", "months" },
            { "time_year", "year" },
            { "time_years", "years" },
            { "time_and", "and" },
            #endregion
            
            #region FOB System
            { "build_error_notinradius", "<color=#ffab87>This can only be placed inside FOB RADIUS.</color>" },
            { "build_error_tick_notinradius", "<color=#ffab87>There's no longer a friendly FOB nearby.</color>" },
            { "build_error_radiustoosmall", "<color=#ffab87>This can only be placed within {0}m of this FOB Radio right now. Expand this range by building a FOB BUNKER.</color>" },
            { "build_error_noradio", "<color=#ffab87>This can only be placed within {0}m of a friendly FOB RADIO.</color>" },
            { "build_error_structureexists", "<color=#ffab87>This FOB already has {0} {1}.</color>" },
            { "build_error_tick_structureexists", "<color=#ffab87>Too many {0}s have already been built on this FOB.</color>" },
            { "build_error_tooclosetoenemybunker", "<color=#ffab87>You may not build on top of an enemy FOB bunker.</color>" },
            { "build_error_notenoughbuild", "<color=#fae69c>You are missing nearby build! <color=#d1c597>Building Supplies: </color><color=#d1c597>{0}/{1}</color></color>" },
            { "build_error_too_many_fobs", "<color=#ffab87>The max number of FOBs has been reached.</color>" },
            { "build_error_invalid_collision", "<color=#ffab87>A {0} can't be built here.</color>" },
            { "no_placement_fobs_underwater", "<color=#ffab87>You can't build a FOB underwater.</color>" },
            { "no_placement_fobs_too_high", "<color=#ffab87>You can't build a FOB more than {0}m above the ground.</color>" },
            { "no_placement_fobs_too_near_base", "<color=#ffab87>You can't build a FOB this close to main base.</color>" },
            { "fob_error_nologi", "<color=#ffab87>You must be near a friendly LOGISTICS TRUCK to place that FOB radio.</color>" },
            { "fob_error_fobtooclose", "<color=#ffa238>You are too close to an existing FOB Radio ({0}m away). You must be at least {1}m away to place a new radio.</color>" },
            { "fob_ui", "{0}  <color=#d6d2c7>{1}</color>  {2}" },
            { "cache_destroyed_attack", "<color=#e8d1a7>WEAPONS CACHE HAS BEEN ELIMINATED</color>" },
            { "cache_destroyed_defense", "<color=#deadad>WEAPONS CACHE HAS BEEN DESTROYED</color>" },
            { "cache_discovered_attack", "<color=#dbdbdb>NEW WEAPONS CACHE DISCOVERED NEAR <color=#e3c59a>{0}</color></color>" },
            { "cache_discovered_defense", "<color=#d9b9a7>WEAPONS CACHE HAS BEEN COMPROMISED, DEFEND IT</color>" },
            { "cache_spawned_defense", "<color=#a8e0a4>NEW WEAPONS CACHE IS NOW ACTIVE</color>" },
            #endregion
            
            #region DeployCommand
            { "deploy_s", "<color=#fae69c>You have arrived at <color=#{0}>{1}</color>.</color>" },
            { "deploy_c_notspawnable", "<color=#ffa238>The FOB you were deploying is no longer active.</color>" },
            { "deploy_c_cachedead", "<color=#ffa238>The Cache you were deploying to was destroyed!</color>" },
            { "deploy_c_moved", "<color=#ffa238>You moved and can no longer deploy!</color>" },
            { "deploy_c_enemiesNearby", "<color=#ffa238>You no longer deploy to that location - there are enemies nearby.</color>" },
            { "deploy_c_notactive", "<color=#ffa238>The point you were deploying to is no longer active.</color>" },
            { "deploy_c_dead", "<color=#ffa238>You died and can no longer deploy!</color>" },
            { "deploy_e_fobnotfound", "<color=#b5a591>There is no location or FOB by the name of '{0}'.</color>" },
            { "deploy_e_notnearfob", "<color=#b5a591>You must be on an active friendly FOB or at main in order to deploy again.</color>" },
            { "deploy_e_notnearfob_ins", "<color=#b5a591>You must be on an active friendly FOB, Cache, or at main in order to deploy again.</color>" },
            { "deploy_e_cooldown", "<color=#b5a591>You can deploy again in: <color=#e3c27f>{0}</color>.</color>" },
            { "deploy_e_alreadydeploying", "<color=#b5a591>You are already deploying somewhere.</color>" },
            { "deploy_e_incombat", "<color=#ffaa42>You are in combat, soldier! You can deploy in another: <color=#e3987f>{0}</color>.</color>" },
            { "deploy_e_injured", "<color=#ffaa42>You can not deploy while injured, get a medic to revive you or give up.</color>" },
            { "deploy_e_enemiesnearby", "<color=#ffa238>You cannot deploy to that FOB - there are enemies nearby.</color>" },
            { "deploy_e_nobunker", "<color=#ffa238>That FOB has no bunker. Your team must build a FOB BUNKER before you can deploy to it.</color>" },
            { "deploy_e_damaged", "<color=#ffa238>That FOB radio is damaged. Repair it with your ENTRENCHING TOOL.</color>" },
            { "deploy_fob_standby", "<color=#fae69c>Now deploying to <color=#{0}>{1}</color>. You will arrive in <color=#eeeeee>{2} seconds</color>.</color>" },
            { "deploy_lobby_removed", "<color=#fae69c>The lobby has been removed, use  <i>/teams</i> to switch teams instead.</color>" },
            #endregion
            
            #region AmmoCommand
            { "ammo_error_nocrate", "<color=#ffab87>Look at an AMMO CRATE, AMMO BAG or VEHICLE in order to resupply.</color>" },
            { "ammo_success", "<color=#d1bda7>Resupplied kit. Consumed: <color=#d97568>{0} AMMO</color> <color=#948f8a>({1} left)</color></color>" },
            { "ammo_success_vehicle", "<color=#d1bda7>Resupplied vehicle. Consumed: <color=#d97568>{0} AMMO</color> <color=#948f8a>({1} left)</color></color>" },
            { "ammo_success_main", "<color=#d1bda7>Resupplied kit. Consumed: <color=#d97568>{0} AMMO</color></color>" },
            { "ammo_success_vehicle_main", "<color=#d1bda7>Resupplied vehicle. Consumed: <color=#d97568>{0} AMMO</color></color>" },
            { "ammo_vehicle_cant_rearm", "<color=#b3a6a2>This vehicle can't be resupplied.</color>" },
            { "ammo_auto_resupply", "<color=#b3a6a2>This vehicle will AUTO RESUPPLY when in main. You can also use '<color=#c9bfad>/load <color=#d4c49d>build</color>|<color=#d97568>ammo</color> <amount></color>'.</color>" }, 
            { "ammo_vehicle_full_already", "<color=#b3a6a2>This vehicle does not need to be resupplied.</color>" },
            { "ammo_not_near_fob", "<color=#b3a6a2>This ammo crate is not built on a friendly FOB.</color>" },
            { "ammo_not_near_repair_station", "<color=#b3a6a2>Your vehicle must be next to a <color=#e3d5ba>REPAIR STATION</color> in order to rearm.</color>" },
            { "ammo_not_in_team", "<color=#b3a6a2>You must be on a team to use this feature.</color>" },
            { "ammo_not_enough_stock", "<color=#b3a6a2>Insufficient ammo. Required: <color=#d97568>{0}/{1} AMMO</color></color>" },
            { "vehicle_staging", "<color=#b3a6a2>You cannot enter this vehicle during the staging phase.</color>" },
            { "ammo_no_kit", "<color=#b3a6a2>You don't have a kit yet. Go and request one at main.</color>" },
            { "ammo_cooldown", "<color=#b7bab1>More AMMO arriving in: <color=#de95a8>{0}</color></color>" },
            { "ammo_not_rifleman", "<color=#b3a6a2>You must be a RIFLEMAN in order to place this Ammo Bag.</color>" },
            #endregion
            
            #region LoadCommand
            { "load_e_novehicle", "<color=#b3a6a2>Look at a friendly LOGISTICS TRUCK or HELICOPTER to load it.</color>" },
            { "load_e_usage", "<color=#b3a6a2>Try typing: '<color=#e6d1b3>/load ammo <amount></color>' or '<color=#e6d1b3>/load build <amount></color>'.</color>" },
            { "load_e_invalidamount", "<color=#b3a6a2>'{0}' is not a valid amount of supplies.</color>" },
            { "load_e_notinmain", "<color=#b3a6a2>You must be in MAIN to load up this vehicle.</color>" },
            { "load_e_notlogi", "<color=#b3a6a2>Only LOGISTICS TRUCKS and TRANSPORT HELICOPTERS can be loaded with supplies.</color>" },
            { "load_e_toofast", "<color=#b3a6a2>Vehicle is moving too fast.</color>" },
            { "load_e_itemassetnotfound", "<color=#b3a6a2>The item required to resupply does not exist. Please report this to the admins.</color>" },
            { "load_s_build", "<color=#d1bda7>Loading complete. <color=#d4c49d>{0} BUILD</color> loaded.</color>" },
            { "load_s_ammo", "<color=#d1bda7>Loading complete. <color=#d97568>{0} AMMO</color> loaded.</color>" },
            #endregion
            
            #region Custom Signs
            { "sign_rules", "Rules\nNo suicide vehicles.\netc." },
            { "sign_kitdelay", "<color=#e6e6e6>All <color=#3bede1>Elite Kits</color> and <color=#32a852>Loadouts</color> are locked for the two weeks of the season.\nThey will be available again after <color=#d8addb>April 1st</color></color>" },
            { "sign_class_desc_squadleader", "\n\n<color=#cecece>Help your squad by supplying them with <color=#f0a31c>rally points</color> and placing <color=#f0a31c>FOB radios</color>.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_rifleman", "\n\n<color=#cecece>Resupply your teammates in the field with an <color=#f0a31c>Ammo Bag</color>.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_medic", "\n\n<color=#cecece><color=#f0a31c>Revive</color> your teammates after they've been injured.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_breacher", "\n\n<color=#cecece>Use <color=#f0a31c>high-powered explosives</color> to take out <color=#f01f1c>enemy FOBs</color>.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_autorifleman", "\n\n<color=#cecece>Equipped with a high-capacity and powerful <color=#f0a31c>LMG</color> to spray-and-pray your enemies.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_machinegunner", "\n\n<color=#cecece>Equipped with a powerful <color=#f0a31c>Machine Gun</color> to shred the enemy team in combat.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_lat", "\n\n<color=#cecece>A balance between an anti-tank and combat loadout, used to conveniently destroy armored enemy vehicles.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_hat", "\n\n<color=#cecece>Equipped with multiple powerful <color=#f0a31c>anti-tank shells</color> to take out any vehicles.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_grenadier", "\n\n<color=#cecece>Equipped with a <color=#f0a31c>grenade launcher</color> to take out enemies behind cover or in light-armored vehicles.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_marksman", "\n\n<color=#cecece>Equipped with a <color=#f0a31c>marksman rifle</color> to take out enemies from medium to high distances.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_sniper", "\n\n<color=#cecece>Equipped with a high-powered <color=#f0a31c>sniper rifle</color> to take out enemies from great distances.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_aprifleman", "\n\n<color=#cecece>Equipped with <color=#f0a31c>explosive traps</color> to cover entry-points and entrap enemy vehicles.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_engineer", "\n\n<color=#cecece>Features 200% <color=#f0a31c>build speed</color> and are equipped with <color=#f0a31c>fortifications</color> and traps to help defend their team's FOBs.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_crewman", "\n\n<color=#cecece>The only kits than can man <color=#f0a31c>armored vehicles</color>.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_pilot", "\n\n<color=#cecece>The only kits that can fly <color=#f0a31c>aircraft</color>.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_specops", "\n\n<color=#cecece>Equipped with <color=#f0a31c>night-vision</color> to help see at night.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_bundle_misc", "<color=#f0a31c>Misc.</color>" },
            { "sign_bundle_caf", "<color=#f0a31c>Canadian Bundle</color>" },
            { "sign_bundle_fr", "<color=#f0a31c>French Bundle</color>" },
            { "sign_bundle_ger", "<color=#f0a31c>German Bundle</color>" },
            { "sign_bundle_usmc", "<color=#f0a31c>USMC Bundle</color>" },
            { "sign_bundle_usa", "<color=#f0a31c>USA Bundle</color>" },
            { "sign_bundle_pl", "<color=#f0a31c>Polish Bundle</color>" },
            { "sign_bundle_idf", "<color=#f0a31c>IDF Bundle</color>" },
            { "sign_bundle_militia", "<color=#f0a31c>Militia Bundle</color>" },
            { "sign_bundle_ru", "<color=#f0a31c>Russia Bundle</color>" },
            { "sign_bundle_soviet", "<color=#f0a31c>Soviet Bundle</color>" },
            { "sign_loadout_info", "<color=#cecece>Loadouts and elite kits can be purchased\nin our <color=#7483c4>Discord</color> server.\n\n<color=#7483c4>/discord</color>" },
            #endregion
            
            #region KickOverrideCommand
            { "kick_syntax", "<color=#9cffb3>Syntax: <i>/kick <player> <reason ...></i>.</color>" },
            { "kick_no_reason_provided", "<color=#9cffb3>You must provide a reason.</color>" },
            { "kick_no_player_found", "<color=#9cffb3>No player found from <color=#d8addb>{0}</color>.</color>" },
            { "kick_kicked_feedback", "<color=#00ffff>You kicked <color=#d8addb>{0}</color>.</color>" },
            { "kick_kicked_broadcast", "<color=#00ffff><color=#d8addb>{0}</color> was kicked by <color=#00ffff>{1}</color>.</color>" },
            { "kick_kicked_broadcast_operator", "<color=#00ffff><color=#d8addb>{0}</color> was kicked by an operator.</color>" },
            { "kick_kicked_console_operator", "{0} ({1}) was kicked by an operator because: {2}." },
            { "kick_kicked_console", "{0} ({1}) was kicked by {2} ({3}) because: {4}." },
            { "kick_autokick_namefilter", "Your name does not contain enough alphanumeric characters in succession (5), please change your name and rejoin." },
            #endregion
            
            #region BanOverrideCommand
            { "ban_syntax", "<color=#9cffb3>Syntax: <i>/ban <player> <duration minutes> <reason ...></i>.</color>" },
            { "ban_permanent_feedback", "<color=#00ffff><color=#d8addb>{0}</color> was <b>permanently</b> banned.</color>" },
            { "ban_permanent_broadcast", "<color=#00ffff><color=#d8addb>{0}</color> was <b>permanently</b> banned by <color=#00ffff>{1}</color>.</color>" },
            { "ban_permanent_broadcast_operator", "<color=#00ffff><color=#d8addb>{0}</color> was <b>permanently</b> banned by an operator.</color>" },
            { "ban_permanent_console_operator", "{0} ({1}) was permanently banned by an operator because: {2}." },
            { "ban_permanent_console", "{0} ({1}) was permanently banned by {2} ({3}) because: {4}." },
            { "ban_feedback", "<color=#00ffff><color=#d8addb>{0}</color> was banned for <color=#9cffb3>{1}</color>.</color>" },
            { "ban_broadcast", "<color=#00ffff><color=#d8addb>{0}</color> was banned by <color=#00ffff>{1}</color> for <color=#9cffb3>{2}</color>.</color>" },
            { "ban_broadcast_operator", "<color=#00ffff><color=#d8addb>{0}</color> was banned by an operator for <color=#9cffb3>{2}</color>.</color>" },
            { "ban_console_operator", "{0} ({1}) was banned by an operator for {3} because: {2}." },
            { "ban_console", "{0} ({1}) was banned by {2} ({3}) for {5} because: {4}." },
            { "ban_no_player_found", "<color=#9cffb3>No player found from <color=#d8addb>{0}</color>.</color>" },
            { "ban_invalid_number", "<color=#9cffb3><color=#9cffb3>{0}</color> should be a whole number between <color=#00ffff>1</color> and <color=#00ffff>2147483647</color>.</color>" },
            { "ban_no_reason_provided", "<color=#9cffb3>You must provide a reason.</color>" },
            #endregion
            
            #region WarnCommand
            { "warn_syntax", "<color=#9cffb3>Syntax: <i>/warn <player> <reason ...></i>.</color>" },
            { "warn_no_player_found", "<color=#9cffb3>No player found from <color=#d8addb>{0}</color>.</color>" },
            { "warn_no_reason_provided", "<color=#9cffb3>You must provide a reason.</color>" },
            { "warn_warned_private_operator", "<color=#ffff00>An operator warned you for: <b>{0}</b>.</color>" },
            { "warn_warned_console_operator", "Warned {0} ({1}) for: {2}" },
            { "warn_warned_broadcast_operator", "<color=#ffff00><color=#d8addb>{0}</color> was warned by an operator.</color>" },
            { "warn_warned_feedback", "<color=#ffff00>You warned <color=#d8addb>{0}</color>.</color>" },
            { "warn_warned_private", "<color=#ffff00><color=#00ffff>{0}</color> warned you for: <b>{1}</b>.</color>" },
            { "warn_warned_console", "{0} ({1}) was warned by {2} ({3}) for: {4}" },
            { "warn_warned_broadcast", "<color=#ffff00><color=#d8addb>{0}</color> was warned by <color=#00ffff>{1}</color>.</color>" },
            #endregion

            #region MuteCommand
            { "mute_syntax", "<color=#9cffb3>Syntax: /mute <voice|text|both> <name or steam64> <permanent | duration in minutes> <reason...></color>" },
            { "mute_no_player_found", "<color=#9cffb3>No online players found with the name <color=#d8addb>{0}</color>. To mute someone that's offline, use their Steam64 ID.</color>" },
            { "mute_cant_read_duration", "<color=#9cffb3>The given value for duration must be a positive number or 'permanent'.</color>" },
            { "mute_feedback", "<color=#00ffff><color=#d8addb>{0}</color> ({1}) was {3} muted for <color=#9cffb3>{2}</color>.</color>" },
            { "mute_feedback_permanent", "<color=#00ffff><color=#d8addb>{0}</color> ({1}) was {2} muted <color=#9cffb3>permanently</color>.</color>" },
            { "mute_broadcast", "<color=#00ffff><color=#d8addb>{0}</color> was {3} muted by <color=#00ffff>{1}</color> for <color=#9cffb3>{2}</color>.</color>" },
            { "mute_broadcast_operator", "<color=#00ffff><color=#d8addb>{0}</color> was {1} muted by an operator for <color=#9cffb3>{2}</color>.</color>" },
            { "mute_broadcast_permanent", "<color=#00ffff><color=#d8addb>{0}</color> was <color=#9cffb3>permanently</color> {2} muted by <color=#00ffff>{1}</color>.</color>" },
            { "mute_broadcast_operator_permanent", "<color=#00ffff><color=#d8addb>{0}</color> was <color=#9cffb3>permanently</color> {1} muted by an operator.</color>" },
            { "mute_dm", "<color=#ffff00><color=#00ffff>{0}</color> {3} muted you for <color=#9cffb3>{2}</color> because: <color=#9cffb3><b>{1}</b></color>.</color>" },
            { "mute_dm_permanent", "<color=#ffff00><color=#00ffff>{0}</color> <color=#9cffb3>permanently</color> {2} muted you for: <color=#9cffb3><b>{1}</b></color>.</color>" },
            { "mute_dm_operator", "<color=#ffff00>An operator {2} muted you for <color=#9cffb3>{1}</color> because: <color=#9cffb3><b>{0}</b></color>.</color>" },
            { "mute_dm_operator_permanent", "<color=#ffff00>An operator <color=#9cffb3>permanently</color> {1} muted you for: <color=#9cffb3><b>{0}</b></color>.</color>" },
            { "text_chat_feedback_muted_permanent", "<color=#ffff00>You're permanently muted in text chat because: {0}.</color>" },
            { "text_chat_feedback_muted", "<color=#ffff00>You're muted in text chat until {0} because {1}.</color>" },
            #endregion

            #region UnmuteCommnad
            { "unmute_not_found", "<color=#9cffb3>\"{0}\" doesn't match a player. To unmute an offline player use their Steam64 ID.</color>" },
            { "unmute_not_muted", "<color=#9cffb3>{0} is not currently muted.</color>" },
            { "unmute_unmuted_broadcast", "<color=#ffff00><color=#d8addb>{0}</color> was unmuted by <color=#00ffff>{1}</color>.</color>" },
            { "unmute_unmuted_broadcast_operator", "<color=#ffff00><color=#d8addb>{0}</color> was unmuted by an operator.</color>" },
            { "unmute_unmuted_dm", "<color=#ffff00><color=#00ffff>{0}</color> has lifted your mute.</color>" },
            { "unmute_unmuted_dm_operator", "<color=#ffff00>Your mute has been lifted.</color>" },
            { "unmute_unmuted", "<color=#ffff00><color=#d8addb>{0}</color> was successfully unmuted.</color>" },
            #endregion

            #region Anti-Main-Camp
            { "amc_reverse_damage", "<color=#f53b3b>Stop <b><color=#ff3300>main-camping</color></b>! Damage is <b>reversed</b> back on you.</color>" },
            #endregion

            #region UnbanCommand
            { "unban_syntax", "<color=#9cffb3>Syntax: <i>/unban <player id></i>.</color>" },
            { "unban_no_player_found", "<color=#9cffb3>No player ID found from <color=#d8addb>{0}</color>.</color>" },
            { "unban_player_not_banned_console", "Player \"{0}\" is not banned. You must use Steam64's for /unban." },
            { "unban_unbanned_console_name_operator", "Sucessfully unbanned {0} ({1})." },
            { "unban_unbanned_console_id_operator", "Sucessfully unbanned {0}." },
            { "unban_unbanned_broadcast_name_operator", "<color=#00ffff><color=#d8addb>{0}</color> was unbanned by an operator.</color>" },
            { "unban_unbanned_broadcast_id_operator", "<color=#00ffff><color=#d8addb>{0}</color> was unbanned by an operator.</color>" },
            { "unban_unbanned_console_name", "{0} ({1}) was unbanned by {2} ({3})." },
            { "unban_unbanned_console_id", "{0} was unbanned by {1} ({2})." },
            { "unban_unbanned_broadcast_name", "<color=#00ffff><color=#d8addb>{0}</color> was unbanned by <color=#00ffff>{1}</color>.</color>" },
            { "unban_unbanned_broadcast_id", "<color=#00ffff><color=#d8addb>{0}</color> was unbanned by <color=#00ffff>{1}</color>.</color>" },
            { "unban_unbanned_feedback_name", "<color=#00ffff>You unbanned <color=#d8addb>{0}</color>.</color>" },
            { "unban_unbanned_feedback_id", "<color=#00ffff>You unbanned <color=#d8addb>{0}</color>.</color>" },
            #endregion
            
            #region LoadBansCommand
            { "loadbans_NoBansErrorText", "There are no banned players." },
            { "loadbans_LogBansDisabled", "Can't upload, Logging bans is disabled." },
            #endregion

            #region DutyCommand
            { "duty_admin_on_console", "{0} ({1}) went on duty." },
            { "duty_admin_off_console", "{0} ({1}) went off duty." },
            { "duty_intern_on_console", "{0} ({1}) went on duty." },
            { "duty_intern_off_console", "{0} ({1}) went off duty." },
            { "duty_on_feedback", "<color=#c6d4b8>You are now <color=#95ff4a>on duty</color>.</color>" },
            { "duty_off_feedback", "<color=#c6d4b8>You are now <color=#ff8c4a>off duty</color>.</color>" },
            { "duty_on_broadcast", "<color=#c6d4b8><color=#d9e882>{0}</color> is now <color=#95ff4a>on duty</color>.</color>" },
            { "duty_off_broadcast", "<color=#c6d4b8><color=#d9e882>{0}</color> is now <color=#ff8c4a>off duty</color>.</color>" },
            #endregion

            #region Teamkills
            { "teamkilled_console_log", "{0} ({1}) teamkilled {2} ({3})!!" },
            #endregion

            #region Restrictions
            { "no_placement_on_vehicle", "<color=#f53b3b>You can't place a{1} <color=#d9e882>{0}</color> on a vehicle!</color>" },
            { "no_place_trap", "<color=#f53b3b>You're not allowed to place a{1} <color=#d9e882>{0}</color> here.</color>" },
            { "cant_steal_batteries", "<color=#f53b3b>Stealing batteries is not allowed.</color>" },
            { "cant_leave_group", "<color=#f53b3b>You are not allowed to manually change groups.</color>" },
            { "cant_store_this_item", "<color=#f53b3b>You are not allowed to store <color=#d9e882>{0}</color>.</color>" },
            { "marker_not_in_squad", "<color=#f53b3b>Only your squad can see markers, join a squad with <color=#d9e882>/squad join <name></color> or <color=#d9e882>/squad create <name></color> to use this feature.</color>" },
            { "entered_enemy_territory", "Too close to enemy base! You will die in {0} second{1}!" },
            #endregion

            #region OnVehicleEnterRequested
            { "vehicle_wait_for_owner", "<color=#bda897>Only the owner (<color=#cedcde>{0}</color>) can enter the driver's seat right now.</color>" },
            { "vehicle_wait_for_owner_or_squad", "<color=#bda897>Only the owner (<color=#cedcde>{0}</color>) and/or members of squad <color=#cedcde>{1}</color> can enter the driver's seat right now.</color>" },
            { "vehicle_no_kit", "<color=#bda897>You cannot get in a vehicle without a kit.</color>" },
            { "vehicle_too_high", "<color=#ff684a>Vehicle is too high off the ground!</color>" },
            { "vehicle_not_valid_kit", "<color=#bda897>You need a <color=#cedcde>{0}</color> kit in order to man this vehicle.</color>" },
            { "vehicle_need_driver", "<color=#bda897>Your vehicle needs a <color=#cedcde>DRIVER</color> before you can switch to the gunner's seat on the battlefield.</color>" },
            { "vehicle_cannot_abandon_driver", "<color=#bda897>You cannot abandon the driver's seat on the battlefield.</color>" },
            { "vehicle_no_passenger_seats", "<color=#bda897>There are no free passenger seats in this vehicle.</color>" },
            #endregion
            
            #region Warnings
            { "friendly_mortar_incoming", "FRIENDLY MORTAR STRIKE INCOMING" },
            { "afk_warning", "<color=#f53b3b>You will be AFK-Kicked in {0} if you don't move.</color>" },
            #endregion
            
            #region BattlEye
            { "battleye_kick_console", "{0} ({1}) was kicked by BattlEye because: \"{2}\"" },
            { "battleye_kick_broadcast", "<color=#00ffff>{0} was kicked by <color=#feed00>BattlEye</color>.</color>" },
            #endregion
            
            #region RequestCommand
            { "request_saved_sign", "<color=#a4baa9>Saved kit: <color=#ffebbd>{0}</color>.</color>" },
            { "request_removed_sign", "<color=#a4baa9>Removed kit sign: <color=#ffebbd>{0}</color>.</color>" },
            { "request_not_looking", "<color=#a8918a>You must be looking at a request sign or vehicle.</color>" },
            { "request_already_saved", "<color=#a8918a>That sign is already saved.</color>" },
            { "request_already_removed", "<color=#a8918a>That sign has already been removed.</color>" },
            { "request_kit_given", "<color=#99918d>You have been allocated a <color=#cedcde>{0}</color> kit!</color>" },
            { "request_kit_boughtcredits", "<color=#c4a36a>Kit bought for <color=#b8ffc1>C </color><color=#ffffff>{0}</color>. Request it with '<color=#b3b0ab>/request</color>'.</color>" },
            { "request_kit_e_kitnoexist", "<color=#a8918a>This kit has not been created yet.</color>" },
            { "request_kit_e_alreadyhaskit", "<color=#a8918a>You already have this kit.</color>" },
            { "request_kit_e_notallowed", "<color=#a8918a>You do not have access to this kit.</color>" },
            { "request_kit_e_notboughtcredits", "<color=#99918d>Look at this sign and type '<color=#ffe2ab>/buy</color>' to unlock this kit permanently for <color=#b8ffc1>C </color><color=#ffffff>{0}</color></color>" },
            { "request_kit_e_notenoughcredits", "<color=#a8918a>You are missing <color=#b8ffc1>C </color><color=#ffffff>{0}</color> needed to unlock this kit.</color>" },
            { "request_kit_e_notbuyablecredits", "<color=#a8918a>This kit cannot be purchased with credits.</color>" },
            { "request_kit_e_limited", "<color=#a8918a>Your team already has a max of {0} players using this kit. Try again later.</color>" },
            { "request_kit_e_wronglevel", "<color=#b3ab9f>You must be rank <color=#ffc29c>{0}</color> to use this kit.</color>" },
            { "request_kit_e_wrongrank", "<color=#b3ab9f>You must be a <color=#{1}>{0}</color> to request this kit.</color>" },
            { "request_kit_e_quest_incomplete", "<color=#b3ab9f>Complete the <color=#ffc29c>{0}</color> quest to request this kit.</color>" },
            { "request_kit_e_notsquadleader", "<color=#b3ab9f>You must be a <color=#cedcde>SQUAD LEADER</color> in order to get this kit.</color>" },
            { "request_loadout_e_notallowed", "<color=#a8918a>You do not own this loadout.</color>" },
            { "request_vehicle_e_notenoughcredits", "<color=#a8918a>You are missing <color=#b8ffc1>C </color><color=#ffffff>{0}</color> needed to request this vehicle.</color>" },
            { "request_vehicle_e_cooldown", "<color=#b3ab9f>This vehicle can be requested in: <color=#ffe2ab>{0}</color>.</color>" },
            { "request_vehicle_e_time_delay", "<color=#b3ab9f>This vehicle is delayed for another: <color=#94cfff>{0}</color>.</color>" },
            { "request_vehicle_e_cache_delay_atk_1", "<color=#b3ab9f>Destroy <color=#94cfff>{0}</color> to request this vehicle.</color>" },
            { "request_vehicle_e_cache_delay_def_1", "<color=#b3ab9f>Lose <color=#94cfff>{0}</color> to request this vehicle.</color>" },
            { "request_vehicle_e_cache_delay_atk_undiscovered_1", "<color=#b3ab9f><color=#94cfff>Discover and Destroy</color> the next cache to request this vehicle.</color>" },
            { "request_vehicle_e_cache_delay_def_undiscovered_1", "<color=#b3ab9f><color=#94cfff>Discover and Lose</color> the next cache to request this vehicle.</color>" },
            { "request_vehicle_e_cache_delay_atk_2+", "<color=#b3ab9f>Destroy <color=#94cfff>{0} more caches</color> to request this vehicle.</color>" },
            { "request_vehicle_e_cache_delay_def_2+", "<color=#b3ab9f>Lose <color=#94cfff>{0} more caches</color> to request this vehicle.</color>" },
            { "request_vehicle_e_flag_delay_1", "<color=#b3ab9f>Capture <color=#94cfff>{0}</color> to request this vehicle.</color>" },
            { "request_vehicle_e_flag_lose_delay_1", "<color=#b3ab9f>Lose <color=#94cfff>{0}</color> to request this vehicle.</color>" },
            { "request_vehicle_e_flag_delay_2+", "<color=#b3ab9f>Capture <color=#94cfff>{0} more flags</color> to request this vehicle.</color>" },
            { "request_vehicle_e_flag_lose_delay_2+", "<color=#b3ab9f>Lose <color=#94cfff>{0} more flags</color> to request this vehicle.</color>" },
            { "request_vehicle_e_staging_delay", "<color=#a6918a>This vehicle can only be requested after the game starts.</color>" },
            { "request_vehicle_e_notinsquad", "<color=#b3ab9f>You must be <color=#cedcde>IN A SQUAD</color> in order to request this vehicle.</color>" },
            { "request_vehicle_e_nokit", "<color=#a8918a>Get a kit before you request vehicles.</color>" },
            { "request_vehicle_e_notinteam", "<color=#a8918a>You must be on the other team to request this vehicle.</color>" },
            { "request_vehicle_e_wrongkit", "<color=#b3ab9f>You need a {0} kit in order to request this vehicle.</color>" },
            { "request_vehicle_e_wronglevel", "<color=#b3ab9f>You must be rank <color=#ffc29c>{0}</color> to request this vehicle.</color>" },
            { "request_vehicle_e_wrongrank", "<color=#b3ab9f>You must be a <color=#{1}>{0}</color> to request this vehicle.</color>" },
            { "request_vehicle_e_quest_incomplete", "<color=#b3ab9f>Complete the <color=#ffc29c>{0}</color> quest to request this vehicle.</color>" },
            { "request_vehicle_e_alreadyrequested", "<color=#a8918a>This vehicle has already been requested.</color>" },
            { "request_vehicle_e_already_owned", "<color=#a8918a>You have already requested a nearby vehicle.</color>" },
            { "request_vehicle_e_unknown_delay", "<color=#b3ab9f>This vehicle is delayed because: <color=#94cfff>{0}</color>.</color>" },
            { "request_vehicle_given", "<color=#b3a591>This <color=#ffe2ab>{0}</color> is now yours to take into battle.</color>" },
            #endregion
            
            #region StructureCommand
            { "structure_not_looking", "<color=#ff8c69>You must be looking at a barricade, structure, or vehicle.</color>" },
            { "structure_saved", "<color=#e6e3d5>Saved <color=#c6d4b8>{0}</color>.</color>" },
            { "structure_saved_already", "<color=#e6e3d5><color=#c6d4b8>{0}</color> is already saved.</color>" },
            { "structure_unsaved", "<color=#e6e3d5><color=#e6e3d5>Removed <color=#c6d4b8>{0}</color> save.</color>" },
            { "structure_unsaved_already", "<color=#ff8c69><color=#c6d4b8>{0}</color> is not saved.</color>" },
            { "structure_popped", "<color=#e6e3d5>Destroyed <color=#c6d4b8>{0}</color>.</color>" },
            { "structure_pop_not_poppable", "<color=#ff8c69>That object can not be destroyed.</color>" },
            { "structure_examine_not_examinable", "<color=#ff8c69>That object can not be examined.</color>" },
            { "structure_examine_not_locked", "<color=#ff8c69>This vehicle is not locked.</color>" },
            { "structure_last_owner_web_prompt", "Last owner of {0}: {1}, Team: {2}." },
            { "structure_last_owner_chat", "<color=#c6d4b8>Last owner of <color=#e6e3d5>{0}</color>: <color=#{3}>{1} <i>({2})</i></color>, Team: <color=#{5}>{4}</color>.</color>" },
            #endregion
            
            #region WhitelistCommand
            { "whitelist_added", "<color=#a0ad8e>Whitelisted item: <color=#ffffff>{0}</color></color>" },
            { "whitelist_removed", "<color=#a0ad8e>Un-whitelisted item: <color=#ffffff>{0}</color></color>" },
            { "whitelist_e_exist", "<color=#ff8c69>That item is already whitelisted.</color>" },
            { "whitelist_e_noexist", "<color=#ff8c69>That item is not yet whitelisted.</color>" },
            { "whitelist_e_invalidid", "<color=#ff8c69>{0} is not a valid item ID.</color> " },
            { "whitelist_e_invalidamount", "<color=#ff8c69>{0} is not a valid number.</color> " },
            { "whitelist_notallowed", "<color=#ff8c69>The item is not allowed to be picked up.</color> " },
            { "whitelist_maxamount", "<color=#ff8c69>You are not allowed to carry any more of this item.</color> " },
            { "whitelist_kit_maxamount", "<color=#ff8c69>Your kit does not allow you to have any more of this item.</color> " },
            { "whitelist_nokit", "<color=#ff8c69>Get a kit first before you can pick up items.</color> " },
            { "whitelist_nosalvage", "<color=#ff8c69>You are not allowed to salvage that.</color> " },
            { "whitelist_noplace", "<color=#ff8c69>You are not allowed to place that.</color> " },
            { "whitelist_toomanyplaced", "<color=#ff8c69>You cannot place more than {0} of those.</color> " },
            { "whitelist_noeditsign", "<color=#ff8c69>You are not allowed to edit that sign.</color> " },
            #endregion
            
            #region VehiclebayCommand
            { "vehiclebay_added", "<color=#a0ad8e>Added requestable vehicle to the vehicle bay: <color=#ffffff>{0}</color></color>" },
            { "vehiclebay_removed", "<color=#a0ad8e>Removed requestable vehicle from the vehicle bay: <color=#ffffff>{0}</color></color>" },
            { "vehiclebay_setprop", "<color=#a0ad8e>Set <color=#8ce4ff>{0}</color> for vehicle <color=#ffb89c>{1}</color> to: <color=#ffffff>{2}</color></color>" },
            { "vehiclebay_setitems", "<color=#a0ad8e>Successfuly set the rearm list for vehicle <color=#ffffff>{0}</color> from your inventory. It will now drop <color=#8ce4ff>{1}</color> items with /ammo.</color>" },
            { "vehiclebay_savemeta", "<color=#a0ad8e>Successfully saved all metadata for vehicle <color=#ffffff>{0}</color>.</color>" },
            { "vehiclebay_cleareditems", "<color=#a0ad8e>Successfuly cleared the rearm list for this vehicle.</color>" },
            { "vehiclebay_seatadded", "<color=#a0ad8e>Made seat <color=#ffffff>{0}</color> a crewman seat for this vehicle.</color>" },
            { "vehiclebay_seatremoved", "<color=#a0ad8e>Seat <color=#ffffff>{0}</color> is no longer a crewman seat for this vehicle.</color>" },
            { "vehiclebay_e_novehicle", "<color=#ff8c69>Look at a vehicle or spawner barricade to use this command.</color>" },
            { "vehiclebay_e_exist", "<color=#ff8c69>That vehicle is already added to the vehicle bay.</color>" },
            { "vehiclebay_e_noexist", "<color=#ff8c69>That vehicle has not been added to the vehicle bay.</color>" },
            { "vehiclebay_e_invalidprop", "<color=#ff8c69>{0} isn't a valid a vehicle property. Try putting 'level', 'team', 'rearmcost' etc.</color>" },
            { "vehiclebay_e_invalidarg", "<color=#ff8c69>{0} isn't a valid value for vehicle property: {1}</color>" },
            { "vehiclebay_e_not_settable", "<color=#ff8c69>{0} is not marked as settable.</color>" },
            { "vehiclebay_e_not_added", "<color=#ff8c69><color=#ffffff>{0}</color> has not been added to the vehicle bay yet. Look at one and do /vb add.</color>" },
            { "vehiclebay_e_invalidseat", "<color=#ff8c69>{0} isn't a valid value for vehicle property: {1}</color>" },
            { "vehiclebay_e_seatexist", "<color=#ff8c69>This vehicle already has a crew seat with index: {0}</color>" },
            { "vehiclebay_e_seatnoexist", "<color=#ff8c69>This vehicle does not have a crew seat with index: {0}</color>" },
            { "vehiclebay_e_gamemode_not_active", "<color=#ff8c69>You may not enter a vehicle right now as the game has not started.</color>" },
            { "vehiclebay_delay_added", "<color=#a0ad8e>Added delay of type {0}:{1} during {2} gamemode.</color>" },
            { "vehiclebay_delay_removed", "<color=#a0ad8e>Removed {0} matching delays.</color>" },
            { "vehiclebay_spawn_registered", "<color=#a0ad8e>Successfully registered spawn. <color=#ffffff>{0}s</color> will spawn here.</color>" },
            { "vehiclebay_spawn_deregistered", "<color=#a0ad8e>Successfully deregistered spawn.</color>" },
            { "vehiclebay_link_started", "<color=#a0ad8e>Started linking, do /vb link on the sign now.</color>" },
            { "vehiclebay_link_finished", "<color=#a0ad8e>Successfully registered vehicle sign link.</color>" },
            { "vehiclebay_unlink_success", "<color=#a0ad8e>Successfully unlinked vehicle sign.</color>" },
            { "vehiclebay_link_not_started", "<color=#ff8c69>You must do /vb link on a vehicle bay first.</color>" },
            { "vehiclebay_spawn_forced", "<color=#a0ad8e>Skipped timer for <color=#ffffff>{0}</color>.</color>" },
            { "vehiclebay_e_invalidid", "<color=#ff8c69>{0} is not a valid vehicle ID.</color>" },
            { "vehiclebay_e_invalidbayid", "<color=#ff8c69>{0} is not a valid vehicle bay item.</color>" },
            { "vehiclebay_e_idnotfound", "<color=#ff8c69>Could not find vehicle with ID: {0}</color>" },
            { "vehiclebay_e_spawnexist", "<color=#ff8c69>This spawn is already registered to <color=#8ce4ff>{0}</color>. Unregister it first.</color>" },
            { "vehiclebay_e_spawnnoexist", "<color=#ff8c69>This spawn is not registered.</color>" },
            { "vehiclebay_check_registered", "<color=#a0ad8e>This spawn (<color=#8ce4ff>{0}</color>) is registered with vehicle: <color=#ffffff>{1} - {2}</color></color>" },
            { "vehiclebay_check_notregistered", "<color=#a0ad8e>This spawn is not registered.</color>" },
            #endregion

            #region Vehicle Death Messages
            { "VEHICLE_DESTROYED", "{0} took out a {1} with a {2} from {3}m away." },
            { "VEHICLE_DESTROYED_UNKNOWN", "{0} took out a {1}." },
            { "VEHICLE_TEAMKILLED", "{0} blew up a friendly {1}." },
            #endregion
            
            #region OfficerCommand
            { "officer_promoted", "<color=#9e9788>Congratulations, you have been <color=#e3b552>PROMOTED</color> to <color=#e05353>{0}</color> of <color=#baccca>{1}</color>!</color>" },
            { "officer_demoted", "<color=#9e9788>You have been <color=#c47f5c>DEMOTED</color> to <color=#e05353>{0}</color> of <color=#baccca>{1}</color>.</color>" },
            { "officer_discharged", "<color=#9e9788>You have been <color=#ab2e2e>DISCHARGED</color> from the officer ranks for unacceptable behaviour.</color>" },
            { "officer_announce_promoted", "<color=#9e9788><color=#c4daff>{0}</color> has been <color=#e3b552>PROMOTED</color> to <color=#e05353>{1}</color> of <color=#baccca>{2}</color>!</color>" },
            { "officer_announce_demoted", "<color=#9e9788><color=#c4daff>{0}</color> has been <color=#c47f5c>DEMOTED</color> to <color=#e05353>{1}</color> of <color=#baccca>{2}</color>.</color>" },
            { "officer_announce_discharged", "<color=#9e9788><color=#c4daff>{0}</color> has been <color=#ab2e2e>DISCHARGED</color> from the rank of <color=#e05353>{1}s</color> for unacceptable behaviour.</color>" },
            { "officer_e_playernotfound", "<color=#b08989>'{0}' is not a valid online player or Steam64 ID.</color>" },
            { "officer_e_invalidrank", "<color=#b08989>'{0}' is not a valid officer level. Try numbers 1 - 5.</color>" },
            { "officer_s_changedrank", "<color=#c6d6c1>{0}'s officer rank was successfully changed to {1} of {2}.</color>" },
            { "officer_s_discharged", "<color=#c6d6c1>{0} was successfully discharged.</color>" },
            #endregion
            
            #region ClearCommand
            { "clear_inventory_console_identity", "Specify a player name when clearing from console." }, // runs from console only, no color needed.
            { "clear_inventory_player_not_found", "<color=#ff8c69>A player was not found from <color=#8ce4ff>\"{0}\"</color>.</color>" },
            { "clear_inventory_self", "<color=#e6e3d5>Cleared your inventory.</color>" },
            { "clear_inventory_others", "<color=#e6e3d5>Cleared <color=#8ce4ff>{0}</color>'s inventory.</color>" },
            { "clear_items_cleared", "<color=#e6e3d5>Cleared all dropped items.</color>" },
            { "clear_structures_cleared", "<color=#e6e3d5>Cleared all placed structures and barricades.</color>" },
            { "clear_vehicles_cleared", "<color=#e6e3d5>Cleared all vehicles.</color>" },
            #endregion
            
            #region UCDeaths
            { "zombie", "a zombie" },
            #endregion
            
            #region ShutdownOverrideCommand
            { "shutdown_syntax", "<color=#9cffb3>Corrent syntax: /shutdown <aftergame|*seconds*|instant> <reason>.</color>" },
            { "shutdown_broadcast_after_game", "<color=#00ffff>A shutdown has been scheduled after this game because: \"<color=#6699ff>{0}</color>\".</color>" },
            { "shutdown_broadcast_after_game_daily", "<color=#00ffff>A daily restart will occur after this game. Down-time estimate: <color=#6699ff>2 minutes</color>.</color>" },
            { "shutdown_broadcast_after_game_canceled", "<color=#00ffff>The scheduled shutdown has been canceled.</color>" },
            { "shutdown_broadcast_after_game_canceled_console", "The scheduled shutdown was canceled." },
            { "shutdown_broadcast_after_game_canceled_console_player", "The scheduled shutdown was canceled by {0}." },
            { "shutdown_broadcast_after_time", "<color=#00ffff>A shutdown has been scheduled in {0} because: \"<color=#6699ff>{1}</color>\".</color>" },
            { "shutdown_broadcast_after_game_console", "A shutdown has been scheduled after this game because: \"{0}\"." },
            { "shutdown_broadcast_after_game_reminder", "<color=#00ffff>A shutdown is scheduled to occur after this game because: \"<color=#6699ff>{0}</color>\".</color>" },
            { "shutdown_broadcast_after_game_console_player", "A shutdown has been scheduled after this game by {0} because: \"{1}\"." },
            { "shutdown_broadcast_after_time_console", "A shutdown has been scheduled in {0} because: \"{1}\"." },
            #endregion
            
            #region RequestSigns
            { "kit_name", "<b>{0}</b>" },
            { "kit_weapons", "<b>{0}</b>" },
            { "kit_price_dollars", "$ {0:N2}" },
            { "kit_premium_exclusive", "EXCLUSIVE" },
            { "kit_required_level", "<color=#{1}>{0}</color>" }, // {0} = level number
            { "kit_required_rank", "<color=#{1}>Rank: {0}</color>" },
            { "kit_required_quest", "<color=#{1}>Quest: <color=#ffffff>{0}</color></color>" },
            { "kit_required_quest_unknown", "<color=#{1}>Finish <color=#ffffff>{0}</color> quest{2}</color>" },
            { "kit_required_quest_done", "<color=#ff974d>Kit Unlocked</color>" },
            { "kit_premium_owned", "OWNED" },
            { "kit_cost", "<color=#b8ffc1>C</color> <color=#ffffff>{0}</color>" },
            { "kit_unlimited", "unlimited" },
            { "kit_player_count", "{0}/{1}" },
            { "sign_kit_request", "{0}\n{1}\n{2}\n{3}" },
            { "loadout_name", "LOADOUT {0}\n" },
            { "loadout_name_owned", "" },
            #endregion
            
            #region Vehiclebay Signs
            { "vbs_tickets_postfix", "Tickets" },
            { "vbs_state_ready", "Ready!  <b>/request</b>" },
            { "vbs_state_dead", "{0}:{1}" },
            { "vbs_state_active", "{0}" },
            { "vbs_state_idle", "Idle: {0}:{1}" },
            { "vbs_state_delay_staging", "Locked Until Start" },
            { "vbs_state_delay_time", "Locked: {0}:{1}" },
            { "vbs_state_delay_flags_1", "Capture {0}" },
            { "vbs_state_delay_flags_lose_1", "Lose {0}" },
            { "vbs_state_delay_caches_atk_1", "Destroy {0}" },
            { "vbs_state_delay_caches_atk_undiscovered_1", "Discover Next Cache" },
            { "vbs_state_delay_caches_def_1", "Lose {0}" },
            { "vbs_state_delay_caches_def_undiscovered_1", "Lose Next Cache" },
            { "vbs_state_delay_flags_lose_2+", "Lose {0} more flags" },
            { "vbs_state_delay_flags_2+", "Capture {0} more flags" },
            { "vbs_state_delay_caches_atk_2+", "Destroy {0} more caches" },
            { "vbs_state_delay_caches_def_2+", "Lose {0} more caches" },
            #endregion

            #region ReviveManager
            { "heal_e_notmedic", "<color=#bdae9d>Only a <color=#ff758f>MEDIC</color> can heal or revive teammates.</color>" },
            { "heal_e_enemy", "<color=#bdae9d>You cannot aid enemy soldiers.</color>" },
            #endregion
            
            #region ReloadCommand
            { "reload_syntax", "<color=#ff8c69>Syntax: /reload [help|module].</color>" },
            { "reload_reloaded_all", "<color=#e6e3d5>Reloaded all Uncreated Warfare components.</color>" },
            { "reload_reloaded_translations", "<color=#e6e3d5>Reloaded all translation files.</color>" },
            { "reload_reloaded_flags", "<color=#e6e3d5>Reloaded flag data.</color>" },
            { "reload_reloaded_flags_gm", "<color=#ff8c69>You must be on a flag gamemode to use this command!</color>" },
            { "reload_reloaded_permissions", "<color=#e6e3d5>Reloaded the permission saver file.</color>" },
            { "reload_reloaded_generic", "<color=#e6e3d5>Reloaded the '{0}' config file.</color>" },
            { "reload_reloaded_tcp", "<color=#e6e3d5>Tried to close any existing TCP connection to UCDiscord and re-open it.</color>" },
            { "reload_reloaded_sql", "<color=#e6e3d5>Reopened the MySql Connection.</color>" },
            #endregion
            
            #region Debug Commands
            { "test_no_method", "<color=#ff8c69>No method found called <color=#ff758f>{0}</color>.</color>" },
            { "test_error_executing", "<color=#ff8c69>Ran into an error while executing: <color=#ff758f>{0} - {1}</color>.</color>" },
            { "test_multiple_matches", "<color=#ff8c69>Multiple methods match <color=#ff758f>{0}</color>.</color>" },

            { "test_zonearea_syntax", "<color=#ff8c69>Syntax: <i>/test zonearea [active|all] <show extra zones: true|false> <show path: true|false> <show range: true|false></i>.</color>" },
            { "test_zonearea_started", "<color=#e6e3d5>Picture has to generate, wait around a minute.</color>" },

            { "test_givexp_player_not_found", "<color=#ff8c69>Could not find player named <color=#ff758f>{0}</color></color>" },
            { "test_givexp_success", "<color=#e6e3d5>Given {0} XP to {1}.</color>" },
            { "test_givexp_invalid_amount", "<color=#ff8c69><color=#ff758f>{0}</color> is not a valid amount (Int32).</color>" },

            { "test_givecredits_player_not_found", "<color=#ff8c69>Could not find player named <color=#ff758f>{0}</color></color>" },
            { "test_givecredits_success", "<color=#e6e3d5>Given {0} credits to {1}.</color>" },
            { "test_givecredits_invalid_amount", "<color=#ff8c69><color=#ff758f>{0}</color> is not a valid amount (Int32).</color>" },

            { "test_zone_not_in_zone", "<color=#e6e3d5>No flag zone found at position <color=#4785ff>({0}, {1}, {2})</color> - <color=#4785ff>{3}°</color>, out of <color=#4785ff>{4}</color> registered flags.</color>" },
            { "test_zone_current_zone", "<color=#e6e3d5>You are in flag zone: <color=#4785ff>{0}</color>, at position <color=#4785ff>({1}, {2}, {3})</color>.</color>" },

            { "test_time_enabled_console", "Enabled coroutine timing." },

            { "test_down_success", "<color=#e6e3d5>Applied <color=#8ce4ff>{0}</color> damage to player.</color>" },

            { "test_sign_no_sign", "<color=#ff8c69>No sign found.</color>" },
            { "test_sign_success", "<color=#e6e3d5>Sign text: <color=#8ce4ff>\"{0}\"</color>.</color>" },

            { "test_gamemode_skipped_staging", "<color=#e6e3d5>The staging phase was skipped.</color>" },
            { "test_gamemode_loaded_gamemode", "<color=#e6e3d5>Loaded gamemode: {0}.</color>" },
            { "test_gamemode_failed_loading_gamemode", "<color=#ff8c69>Failed to load gamemode \"{0}\". Check Console.</color>" },
            { "test_gamemode_type_not_found", "<color=#ff8c69>There is no gamemode with the name \"{0}\".</color>" },

            { "test_trackstats_enabled", "<color=#e6e3d5>Tracking stats has been enabled.</color>" },
            { "test_trackstats_disabled", "<color=#e6e3d5>Tracking stats has been disabled.</color>" },

            { "test_destroyblocker_failure", "<color=#ff8c69>Found no zone blockers to destroy.</color>" },
            { "test_destroyblocker_success", "<color=#e6e3d5>Destroyed {0} zone blocker{1}.</color>" },

            { "test_resetlobby_success", "<color=#e6e3d5>Reset {0}'s lobby state.</color>" },

            { "test_instid_not_found", "<color=#ff8c69>An object with an Instance ID was not found.</color>" },
            { "test_instid_found_barricade", "<color=#e6e3d5>Found barricade with instance id {0}.</color>" },
            { "test_instid_found_structure", "<color=#e6e3d5>Found structure with instance id {0}.</color>" },
            { "test_instid_found_vehicle", "<color=#e6e3d5>Found vehicle with instance id {0}.</color>" },
            { "test_instid_found_object", "<color=#e6e3d5>Found level object with instance id {0}.</color>" },

            { "test_playersave_success", "<color=#e6e3d5>Successfully set {1} in {0}'s playersave to {2}.</color>" },
            { "test_playersave_field_not_found", "<color=#ff8c69>Couldn't find a field by the name {0} in PlayerSave.</color>" },
            { "test_playersave_field_protected", "<color=#ff8c69>The field {0} in PlayerSave must have the JsonSettable attribute applied to it to set it.</color>" },
            { "test_playersave_couldnt_parse", "<color=#ff8c69>Couldn't convert {0} to a value {1} can use.</color>" },
            { "test_playersave_not_found", "<color=#ff8c69>A player with that ID has not joined.</color>" },

            #endregion

            #region Phases
            { "phases_briefing", "BRIEFING PHASE" },
            { "phases_preparation", "PREPARATION PHASE" },
            { "phases_invasion_attack", "BRIEFING PHASE" },
            { "phases_invasion_defense", "PREPARATION PHASE\nFORTIFY {0}" },
            #endregion
            
            #region XP Toasts
            { "xp_from_operator", "FROM OPERATOR" },
            { "xp_from_player", "FROM {0}" },
            { "xp_healed_teammate", "HEALED {0}" },
            { "xp_enemy_downed", "<color=#e3e3e3>DOWNED</color>" },
            { "xp_friendly_downed", "<color=#e3e3e3>DOWNED FRIENDLY</color>" },
            { "xp_enemy_killed", "KILLED ENEMY" },
            { "xp_kill_assist", "ASSIST" },
            { "xp_vehicle_assist", "VEHICLE ASSIST" },
            { "xp_driver_assist", "DRIVER ASSIST" },
            { "xp_spotted_assist", "SPOTTER" },
            { "xp_friendly_killed", "TEAMKILLED" },
            { "xp_fob_killed", "FOB DESTROYED" },
            { "xp_fob_teamkilled", "FRIENDLY FOB DESTROYED" },
            { "xp_fob_in_use", "FOB IN USE" },
            { "xp_supplies_unloaded", "RESUPPLIED FOB" },
            { "xp_resupplied_teammate", "RESUPPLIED TEAMMATE" },
            { "xp_repaired_vehicle", "REPAIRED VEHICLE" },
            { "xp_fob_repaired_vehicle", "FOB REPAIRED VEHICLE" },
            { "xp_vehicle_destroyed", "{0} DESTROYED" },
            { "xp_aircraft_destroyed", "{0} SHOT DOWN" },

            { "xp_flag_captured", "FLAG CAPTURED" },
            { "xp_flag_neutralized", "FLAG NEUTRALIZED" },
            { "xp_flag_attack", "ATTACK" },
            { "xp_flag_defend", "DEFENSE" },
            { "xp_cache_killed", "CACHE DESTROYED" },
            { "xp_cache_teamkilled", "FRIENDLY CACHE DESTROYED" },

            { "xp_squad_bonus", "SQUAD BONUS" },
            { "xp_on_duty", "ON DUTY" },

            { "xp_transporting_players", "TRANSPORTING PLAYERS" },

            { "gain_xp", "+{0} XP" },
            { "loss_xp", "-{0} XP" },
            { "gain_credits", "+{0} <color=#b8ffc1>C</color>" },
            { "subtract_credits", "-{0} <color=#b8ffc1>C</color>" },
            { "loss_credits", "-{0} <color=#d69898>C</color>" },
            { "promoted_xp_1", "YOU HAVE BEEN <color=#ffbd8a>PROMOTED</color> TO" },
            { "promoted_xp_2", "{0}" },
            { "demoted_xp_1", "YOU HAVE BEEN <color=#e86868>DEMOTED</color> TO" },
            { "demoted_xp_2", "{0}" },
            #endregion

            #region Injured UI
            { "injured_ui_header", "You are injured" },
            { "injured_ui_give_up", "Press <b>'/'</b> to give up.\n " },
            { "injured_chat", "<color=#ff8c69>You were injured, press <color=#cedcde><plugin_2/></color> to give up.</color>" },
            #endregion
            
            #region Insurgency
            { "insurgency_ui_unknown_attack", "<color=#696969>Undiscovered</color>" },
            { "insurgency_ui_unknown_defense", "<color=#696969>Unknown</color>" },
            { "insurgency_ui_destroyed_attack", "<color=#5a6e5c>Destroyed</color>" },
            { "insurgency_ui_destroyed_defense", "<color=#6b5858>Lost</color>" },
            { "insurgency_ui_cache_attack", "<color=#ffca61>{0}</color> <color=#c2c2c2>{1}</color>" },
            { "insurgency_ui_cache_defense_undiscovered", "<color=#b780d9>{0}</color> <color=#c2c2c2>{1}</color>" },
            { "insurgency_ui_cache_defense_discovered", "<color=#555bcf>{0}</color> <color=#c2c2c2>{1}</color>" },
            { "caches_header", "Caches" },
            #endregion
            
            #region ReportCommand
            { "report_syntax", "<color=#9cffb3>Corrent syntax: /report <player> [\"report reason\"] [custom message...]</color>" },
            { "report_reasons", "<color=#9cffb3>Report reasons: -none-, \"chat abuse\", \"voice chat abuse\", \"soloing vehicles\", \"wasteing assets\", \"teamkilling\", \"fob greifing\".</color>" },
            { "report_discord_not_linked", "<color=#9cffb3>Your account must be linked in our Discord server to use this command. Type <color=#7483c4>/discord</color> then type <color=#ffffff>-link {0}</color> in <color=#c480d9>#warfare-stats</color>.</color>" },
            { "report_player_not_found", "<color=#9cffb3>Unable to find a player with that name, you can use their <color=#ffffff>Steam64 ID</color> instead, as names are only stored until they've been offline for 20 minutes.</color>" },
            { "report_unknown_error", "<color=#9cffb3>Unable to generate a report for an unknown reason, check your syntax again with <color=#ffffff>/report help</color>.</color>" },
            { "report_success_p1", "<color=#c480d9>Successfully reported {0} for <color=#ffffff>{1}</color> as a <color=#00ffff>{2}</color> report.</color>" },
            { "report_success_p2", "<color=#c480d9>If possible please post evidence in <color=#ffffff>#player-reports</color> in our <color=#7483c4>Discord</color> server.</color>" },
            { "report_notify_admin", "<color=#c480d9>{0} reported {1} for <color=#ffffff>{2}</color> as a <color=#00ffff>{3}</color> report.\nCheck <color=#c480d9>#player-reports</color> for more information.</color>" },
            { "report_notify_violator", "<color=#c480d9>You've been reported for <color=#00ffff>{0}</color>.\nCheck <color=#ffffff>#player-reports</color> in our <color=#7483c4>Discord</color> (/discord) for more information and to defend yourself.</color>" },
            { "report_notify_violator_chat_p1", "<color=#c480d9>You've been reported for <color=#00ffff>{0} - {1}</color>.</color>" },
            { "report_notify_violator_chat_p2", "<color=#c480d9>Check <color=#ffffff>#player-reports</color> in our <color=#7483c4>Discord</color> (/discord) for more information and to defend yourself.</color>" },
            { "report_console", "{0} ({1}) reported {2} ({3}) for \"{4}\" as a {5} report." },
            { "report_console_record", "Report against {0} ({1}) record: \"{2}\"" },
            { "report_console_record_failed", "Report against {0} ({1}) failed to send to UCDB." },
            { "report_cooldown", "You've already reported {0} in the past hour." },
            { "report_cancelled", "You did not confirm your report in time." },
            { "report_confirm", "Did you mean to report {1} <i><color=#444444>{0}</color></i>? Type <color=#ff8c69>/confirm</color> to continue." },
            { "report_not_connected", "<color=#ff8c69>The report system is not available right now, please try again later.</color>" },
            #endregion
            
            #region Tips
            { "tip_place_radio", "Place a <color=#ababab>FOB RADIO</color>." },
            { "tip_place_bunker", "Build a <color=#a5c3d9>FOB BUNKER</color> so that your team can spawn." },
            { "tip_unload_supplies", "<color=#d9c69a>DROP SUPPLIES</color> onto the FOB." },
            { "tip_help_build", "<color=#d9c69a>{0} needs help building!</color>" },
            { "tip_logi_resupplied", "Your {0} has been auto resupplied." },
            #endregion

            #region ZoneCommand
            { "zone_syntax", "<color=#ff8c69>Syntax: /zone <visualize|go|edit|list|create|util></color>" },
            { "zone_visualize_no_results", "<color=#ff8c69>You aren't in any existing zone.</color>" },
            { "zone_go_no_results", "<color=#ff8c69>Couldn't find a zone by that name.</color>" },
            { "zone_go_success", "<color=#e6e3d5>Teleported to <color=#5a6e5c>{0}</color>.</color>" },
            { "zone_visualize_success", "<color=#e6e3d5>Spawned {0} particles around <color=#cedcde>{1}</color>.</color>" },
            { "enter_zone_test", "<color=#e6e3d5>You've entered the zone <color=#cedcde>{0}</color>.</color>" },
            { "exit_zone_test", "<color=#e6e3d5>You've exited the zone <color=#cedcde>{0}</color>.</color>" },

            // zone delete
            { "delete_zone_badvalue_self", "<color=#ff8c69>You must be standing in 1 zone (not 0 or multiple). Alternatively, provide a zone name as another argument.</color>" },
            { "delete_zone_badvalue", "<color=#ff8c69>Failed to find a zone named \"{0}\".</color>" },
            { "delete_zone_confirm", "Did you mean to delete <color=#666666>{0}</color>? Type <color=#ff8c69>/confirm</color> to continue." },
            { "delete_zone_success", "<color=#e6e3d5>Deleted <color=#666666>{0}</color>.</color>" },
            { "delete_zone_deleted_working_zone", "<color=#ff8c69>Someone deleted the zone you're working on, saving this will create a new one.</color>" },

            // zone create
            { "create_zone_syntax", "<color=#ff8c69>Syntax: /zone create <polygon|rectangle|circle> <name>.</color>" },
            { "create_zone_success", "<color=#e6e3d5>Started zone builder for {0}, a {1} zone.</color>" },
            { "create_zone_name_taken", "<color=#ff8c69>\"{0}\" is already in use by another zone.</color>" },
            { "create_zone_name_taken_2", "<color=#ff8c69>\"{0}\" is already in use by another zone being created by {1}.</color>" },

            // zone edit
            { "edit_zone_syntax", "<color=#ff8c69>Syntax: /zone edit <existing|maxheight|minheight|finalize|cancel|addpoint|delpoint|clearpoints|setpoint|orderpoint|radius|sizex|sizez|center|name|shortname|type> [value]</color>" },
            { "edit_zone_not_started", "<color=#ff8c69>Start creating a zone with <color=#ffffff>/zone create <polygon|rectangle|circle> <name></color>.</color>" },
            { "edit_zone_finalize_exists", "<color=#ff8c69>There's already a zone saved with that id.</color>" },
            { "edit_zone_finalize_success", "<color=#e6e3d5>Successfully finalized and saved {0}.</color>" },
            { "edit_zone_finalize_failure", "<color=#ff8c69>The provided zone data was invalid because: <color=#ffffff>{0}</color></color>" },
            { "edit_zone_finalize_use_case", "<color=#ff8c69>Before saving you must set a use case with /zone edit use case <type>: \"flag\", \"lobby\", \"t1_main\", \"t2_main\", \"t1_amc\", or \"t2_amc\".</color>" },
            { "edit_zone_finalize_success_overwrite", "<color=#e6e3d5>Successfully overwrote {0}.</color>" },
            { "edit_zone_cancel_success", "<color=#e6e3d5>Successfully cancelled making {0}.</color>" },
            { "edit_zone_finalize_error", "<color=#ff8c69>There was a problem finalizing your zone: \"{0}\".</color>" },
            { "edit_zone_maxheight_badvalue", "<color=#ff8c69>Maximum Height must be a decimal or whole number, or leave it blank to use the player's current height.</color>" },
            { "edit_zone_maxheight_success", "<color=#e6e3d5>Set maximum height to {0}.</color>" },
            { "edit_zone_minheight_badvalue", "<color=#ff8c69>Minimum Height must be a decimal or whole number, or leave it blank to use the player's current height.</color>" },
            { "edit_zone_minheight_success", "<color=#e6e3d5>Set minimum height to {0}.</color>" },
            { "edit_zone_type_badvalue", "<color=#ff8c69>Type must be rectangle, circle, or polygon.</color>" },
            { "edit_zone_type_already_set", "<color=#ff8c69>This zone is already a {0}.</color>" },
            { "edit_zone_type_success", "<color=#e6e3d5>Set type to {0}.</color>" },
            { "edit_zone_addpoint_badvalues", "<color=#ff8c69>Adding a point requires either: blank (appends, current pos), <index> (current pos), <x> <z> (appends), or <index> <x> <z> parameters.</color>" },
            { "edit_zone_addpoint_success", "<color=#e6e3d5>Added point #{0} at {1}.</color>" },
            { "edit_zone_delpoint_badvalues", "<color=#ff8c69>Deleting a point requires either: nearby X and Z parameters, a point number, or leave them blank to use the player's current position.</color>" },
            { "edit_zone_point_number_not_point", "<color=#ff8c69>Point #{0} is not defined.</color>" },
            { "edit_zone_point_none_nearby", "<color=#ff8c69>There is no point near {0}.</color>" },
            { "edit_zone_delpoint_success", "<color=#e6e3d5>Removed point #{0} at {1}.</color>" },
            { "edit_zone_setpoint_badvalues", "<color=#ff8c69>Moving a point requires either: blank (move nearby closer), <nearby src x> <nearby src z> <dest x> <dest z>, <pt num> (destination is player position }, <pt num> <dest x> <dest z>, or <nearby src x> <nearby src z> (destination is nearby player).</color>" },
            { "edit_zone_setpoint_success", "<color=#e6e3d5>Moved point #{0} from {1} to {2}.</color>" },
            { "edit_zone_orderpoint_success", "<color=#e6e3d5>Moved point #{0} to index #{1}.</color>" },
            { "edit_zone_orderpoint_badvalue", "<color=#ff8c69>Ordering a point requires either: <from-index> <to-index>, <to-index> (from is nearby player), or <src x> <src z> <to-index>.</color>" },
            { "edit_zone_radius_badvalue", "<color=#ff8c69>Radius must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.</color>" },
            { "edit_zone_radius_success", "<color=#e6e3d5>Set radius to {0}.</color>" },
            { "edit_zone_sizex_badvalue", "<color=#ff8c69>Size X must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.</color>" },
            { "edit_zone_sizex_success", "<color=#e6e3d5>Set size x to {0}.</color>" },
            { "edit_zone_sizez_badvalue", "<color=#ff8c69>Size Z must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.</color>" },
            { "edit_zone_sizez_success", "<color=#e6e3d5>Set size z to {0}.</color>" },
            { "edit_zone_center_badvalue", "<color=#ff8c69>To set center you must provide two decimal or whole numbers, or leave them blank to use the player's current position.</color>" },
            { "edit_zone_center_success", "<color=#e6e3d5>Set center position to {0}.</color>" },
            { "edit_zone_clearpoints_success", "<color=#e6e3d5>Cleared all polygon points.</color>" },
            { "edit_zone_clearpoints_uncleared", "<color=#e6e3d5>Restored {0} point{1}.</color>" },
            { "edit_zone_name_badvalue", "<color=#ff8c69>Name requires one string argument. Quotation marks aren't required.</color>" },
            { "edit_zone_name_success", "<color=#e6e3d5>Set name to \"{0}\".</color>" },
            { "edit_zone_short_name_badvalue", "<color=#ff8c69>Short name requires one string argument. Quotation marks aren't required.</color>" },
            { "edit_zone_short_name_success", "<color=#e6e3d5>Set short name to \"{0}\".</color>" },
            { "edit_zone_short_name_removed", "<color=#e6e3d5>Removed short name.</color>" },
            { "edit_zone_existing_badvalue", "<color=#ff8c69>Edit existing zone requires the zone name as a parameter. Alternatively stand in the zone (without overlapping another).</color>" },
            { "edit_zone_existing_in_progress", "<color=#ff8c69>Cancel or finalize the zone you're currently editing first.</color>" },
            { "edit_zone_existing_success", "<color=#e6e3d5>Started editing zone {0}, a {1} zone.</color>" },
            { "edit_zone_use_case_badvalue", "<color=#ff8c69>Use case requires one string argument: \"flag\", \"lobby\", \"t1_main\", \"t2_main\", \"t1_amc\", or \"t2_amc\".</color>" },
            { "edit_zone_use_case_success", "<color=#e6e3d5>Set use case to \"{0}\".</color>" },
            { "edit_zone_undo_failure", "<color=#ff8c69>There is nothing to undo.</color>" },
            { "edit_zone_redo_failure", "<color=#ff8c69>There is nothing to redo.</color>" },


            // edit zone ui
            { "edit_zone_ui_suggested_command_1", "/ze maxheight [value]" },
            { "edit_zone_ui_suggested_command_2", "/ze minheight [value]" },
            { "edit_zone_ui_suggested_command_3", "/ze finalize" },
            { "edit_zone_ui_suggested_command_4", "/ze cancel" },
            { "edit_zone_ui_suggested_command_5_p", "/ze addpt [x z]" },
            { "edit_zone_ui_suggested_command_6_p", "/ze delpt [number | x z]" },
            { "edit_zone_ui_suggested_command_7_p", "/ze setpt <number | src: x z | number dest: x z | src: x z dest: x z>" },
            { "edit_zone_ui_suggested_command_8_p", "/ze orderpt <from-index to-index | to-index | src: x z to-index>" },
            { "edit_zone_ui_suggested_command_9_c", "/ze radius [value]" },
            { "edit_zone_ui_suggested_command_10_r", "/ze sizex [value]" },
            { "edit_zone_ui_suggested_command_11_r", "/ze sizez [value]" },
            { "edit_zone_ui_suggested_command_12", "/zone util location" },
            { "edit_zone_ui_suggested_command_13", "/ze type <rectangle | circle | polygon>" },
            { "edit_zone_ui_suggested_command_14_p", "/ze clearpoints" },
            { "edit_zone_ui_suggested_commands", "Suggested Commands" },
            { "edit_zone_ui_y_limits", "Y: {0} - {1}" },
            { "edit_zone_ui_y_limits_infinity", "∞" },

            // zone util
            { "util_zone_syntax", "<color=#ff8c69>Syntax: /zone util <location></color>" },
            { "util_zone_location", "<color=#e6e3d5>Location: {0}, {1}, {2} | Yaw: {3}°.</color>" },
            #endregion

            #region Teams
            { "teams_e_cooldown", "<color=#ff8c69>You can't use /teams for another {0}.</color>" },
            { "team_ui_header", "Choose a Team" },
            { "team_ui_click_to_join", "CLICK TO JOIN" },
            { "team_ui_click_to_join_donor", "<color=#e3b552>CLICK TO JOIN</color>" },
            { "team_ui_joined", "JOINED" },
            { "team_ui_joined_donor", "<color=#e3b552>JOINED</color>" },
            { "team_ui_full", "<color=#bf6363>FULL</color>" },
            { "team_ui_confirm", "<color=#888888>CONFIRM</color>" },
            { "team_ui_joining", "<color=#999999>JOINING...</color>" },
            #endregion

            #region Spotting
            { "spotted", "<color=#b9ffaa>SPOTTED</color>" },
            #endregion

            #region VehicleTypes
            { "HUMVEE", "Humvee" },
            { "TRANSPORT", "Transport Truck" },
            { "LOGISTICS", "Logistics Truck" },
            { "SCOUT_CAR", "Scout Car" },
            { "APC", "APC" },
            { "IFV", "IFV" },
            { "MBT", "Tank" },
            { "HELI_TRANSPORT", "Transport Heli" },
            { "HELI_ATTACK", "Attack Heli" },
            { "JET", "Jet" },
            { "EMPLACEMENT", "Emplacement" },
            #endregion

            #region TeleportCommand
            { "tp_target_dead", "<color=#8f9494><color=#{1}>{0}</color> is not alive.</color>" },
            { "tp_entered_vehicle", "<color=#bfb9ac>You were put in <color=#{2}>{1}</color>'s <color=#dddddd>{0}</color>.</color>" },
            { "tp_teleported_player", "<color=#bfb9ac>You were teleported to <color=#{1}>{0}</color>.</color>" },
            { "tp_obstructed_player", "<color=#8f9494>Failed to teleport you to <color=#{1}>{0}</color>, their position is obstructed.</color>" },
            { "tp_location_not_found", "<color=#8f9494>Failed to find a location similar to <color=#dddddd>{0}</color>.</color>" },
            { "tp_teleported_location", "<color=#bfb9ac>You were teleported to <color=#dddddd>{0}</color>.</color>" },
            { "tp_obstructed_location", "<color=#8f9494>Failed to teleport you to <color=#dddddd>{0}</color>, it's position is obstructed.</color>" },
            { "tp_entered_vehicle_other", "<color=#bfb9ac><color=#{4}>{3}</color> was put in <color=#{2}>{1}</color>'s <color=#dddddd>{0}</color>.</color>" },
            { "tp_teleported_player_other", "<color=#bfb9ac><color=#{3}>{2}</color> was teleported to <color=#{1}>{0}</color>.</color>" },
            { "tp_obstructed_player_other", "<color=#8f9494>Failed to teleport <color=#{3}>{2}</color> to <color=#{1}>{0}</color>, their position is obstructed.</color>" },
            { "tp_teleported_location_other", "<color=#bfb9ac><color=#{2}>{1}</color> was teleported to <color=#dddddd>{0}</color>.</color>" },
            { "tp_obstructed_location_other", "<color=#8f9494>Failed to teleport <color=#{2}>{1}</color> to <color=#dddddd>{0}</color>, it's position is obstructed.</color>" },
            { "tp_target_not_found", "<color=#8f9494>Failed to find a player from <color=#dddddd>{0}</color></color>" },
            { "tp_invalid_coordinates", "<color=#8f9494>Use of coordinates should look like: <color=#eeeeee>/tp [player] <x y z></color>.</color>" },
            { "tp_teleported_player_location", "<color=#bfb9ac>You were teleported to <color=#eeeeee>{0}</color>.</color>" },
            { "tp_obstructed_player_location", "<color=#8f9494>Failed to teleport you to <color=#eeeeee>{0}</color>, that point is obstructed.</color>" },
            { "tp_teleported_player_location_other", "<color=#bfb9ac><color=#{2}>{1}</color> was teleported to <color=#eeeeee>{0}</color>.</color>" },
            { "tp_obstructed_player_location_other", "<color=#8f9494>Failed to teleport <color=#{2}>{1}</color> to <color=#eeeeee>{0}</color>, that point is obstructed.</color>" },
            #endregion

            #region HealCommand
            { "heal_player", "<color=#ff9966>You healed <color=#{1}>{0}</color>.</color>" },
            { "heal_self", "<color=#ff9966>You we're healed.</color>" },
            #endregion

            #region GodCommand
            { "god_mode_enabled", "<color=#bfb9ac>God mode enabled.</color>" },
            { "god_mode_disabled", "<color=#bfb9ac>God mode disabled.</color>" },
            #endregion

            #region VanishCommand
            { "vanish_mode_enabled", "<color=#bfb9ac>Vanish mode enabled.</color>" },
            { "vanish_mode_disabled", "<color=#bfb9ac>Vanish mode disabled.</color>" },
            #endregion

            #region PermissionCommand
            { "permissions_current", "<color=#bfb9ac>Current permisions: <color=#ffdf91>{0}</color>.</color>" },
            { "permissions_grant_success", "<color=#bfb9ac><color=#7f8182>{1}</color> <color=#dddddd>({2})</color> is now a <color=#ffdf91>{0}</color>.</color>" },
            { "permissions_grant_already", "<color=#bfb9ac><color=#7f8182>{1}</color> <color=#dddddd>({2})</color> is already at the <color=#ffdf91>{0}</color> level.</color>" },
            { "permissions_revoke_already", "<color=#bfb9ac><color=#7f8182>{0}</color> <color=#dddddd>({1})</color> is already a <color=#ffdf91>member</color>.</color>" },
            { "permissions_revoke_success", "<color=#bfb9ac><color=#7f8182>{0}</color> <color=#dddddd>({1})</color> is now a <color=#ffdf91>member</color>.</color>" },
            #endregion

            #region Win UI
            { "win_ui_value_tickets", "{0} Tickets" },
            { "win_ui_value_caches", "{0} Caches Left" },
            { "win_ui_header_winner", "{0}\r\nhas won the battle!" },
            #endregion
        };
    }

    public static Dictionary<string, string> DefaultTranslations;
    public static readonly List<ZoneModel> DefaultZones;
    static JSONMethods()
    {
        DefaultZones = new List<ZoneModel>(8);
        ZoneModel mdl = new ZoneModel()
        {
            Id = 1,
            Name = "Ammo Hill",
            X = -82.4759521f,
            Z = 278.999451f,
            ZoneType = EZoneType.RECTANGLE,
            UseCase = EZoneUseCase.FLAG
        };
        mdl.ZoneData.SizeX = 97.5f;
        mdl.ZoneData.SizeZ = 70.3125f;
        mdl.Adjacencies = new AdjacentFlagData[]
        {
            new AdjacentFlagData(8, 1f),
            new AdjacentFlagData(2, 1f),
        };
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 2,
            Name = "Hilltop Encampment",
            ShortName = "Hilltop",
            X = 241.875f,
            Z = 466.171875f,
            ZoneType = EZoneType.POLYGON,
            UseCase = EZoneUseCase.FLAG
        };
        mdl.ZoneData.Points = new Vector2[]
        {
            new Vector2(272.301117f, 498.742401f),
            new Vector2(212.263733f, 499.852478f),
            new Vector2(211.238708f, 433.756653f),
            new Vector2(271.106445f, 432.835083f)
        };
        mdl.Adjacencies = new AdjacentFlagData[]
        {
            new AdjacentFlagData(4, 0.5f),
            new AdjacentFlagData(3, 1f),
        };
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 3,
            Name = "FOB Papanov",
            ShortName = "Papanov",
            X = 706.875f,
            Z = 711.328125f,
            ZoneType = EZoneType.POLYGON,
            UseCase = EZoneUseCase.FLAG
        };
        mdl.ZoneData.Points = new Vector2[]
        {
            new Vector2(669.994995f, 817.746216f),
            new Vector2(818.528564f, 731.983521f),
            new Vector2(745.399902f, 605.465942f),
            new Vector2(596.919312f, 691.226624f)
        };
        mdl.Adjacencies = new AdjacentFlagData[]
        {
            new AdjacentFlagData(2, 1f)
        };
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 4,
            Name = "Verto",
            X = 1649,
            Z = 559,
            ZoneType = EZoneType.POLYGON,
            UseMapCoordinates = true,
            UseCase = EZoneUseCase.FLAG
        };
        mdl.ZoneData.Points = new Vector2[]
        {
            new Vector2(1539.5f, 494),
            new Vector2(1722.5f, 529),
            new Vector2(1769.5f, 558),
            new Vector2(1741, 599),
            new Vector2(1695.5f, 574),
            new Vector2(1665, 568),
            new Vector2(1658, 608.5f),
            new Vector2(1608.5f, 598.5f),
            new Vector2(1602.5f, 624),
            new Vector2(1562.5f, 614.5f),
            new Vector2(1577.5f, 554),
            new Vector2(1528.5f, 545)
        };
        mdl.Adjacencies = new AdjacentFlagData[]
        {
            new AdjacentFlagData(2, 0.5f),
            new AdjacentFlagData(3, 1f)
        };
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 5,
            Name = "Hill 123",
            X = 1657.5f,
            Z = 885.5f,
            ZoneType = EZoneType.CIRCLE,
            UseMapCoordinates = true,
            UseCase = EZoneUseCase.FLAG
        };
        mdl.ZoneData.Radius = 43.5f;
        mdl.Adjacencies = new AdjacentFlagData[]
        {
            new AdjacentFlagData(4, 1f)
        };
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 6,
            Name = "Hill 13",
            X = 1354,
            Z = 1034.5f,
            ZoneType = EZoneType.CIRCLE,
            UseMapCoordinates = true,
            UseCase = EZoneUseCase.FLAG
        };
        mdl.ZoneData.Radius = 47;
        mdl.Adjacencies = new AdjacentFlagData[]
        {
            new AdjacentFlagData(2, 1f),
            new AdjacentFlagData(5, 1f),
            new AdjacentFlagData(1, 2f)
        };
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 7,
            Name = "Mining Headquarters",
            ShortName = "Mining HQ",
            X = 49.21875f,
            Z = -202.734375f,
            ZoneType = EZoneType.POLYGON,
            UseCase = EZoneUseCase.FLAG
        };
        mdl.ZoneData.Points = new Vector2[]
        {
            new Vector2(-5.02727556f, -138.554886f),
            new Vector2(72.9535751f, -138.59877f),
            new Vector2(103.024361f, -138.548294f),
            new Vector2(103.59375f, -151.40625f),
            new Vector2(103.048889f, -246.603363f),
            new Vector2(72.9691391f, -246.541885f),
            new Vector2(53.1518631f, -257.577393f),
            new Vector2(53.9740639f, -258.832581f),
            new Vector2(43.0496025f, -264.54364f),
            new Vector2(-4.99750614f, -264.539978f),
        };
        mdl.Adjacencies = new AdjacentFlagData[]
        {
            new AdjacentFlagData(6, 1f)
        };
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 8,
            Name = "OP Fortress",
            ShortName = "Fortress",
            X = 375.5f,
            Z = 913f,
            ZoneType = EZoneType.CIRCLE,
            UseMapCoordinates = true,
            UseCase = EZoneUseCase.FLAG
        };
        mdl.ZoneData.Radius = 47;
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 9,
            Name = "Dylym",
            X = 1849f,
            Z = 1182.5f,
            ZoneType = EZoneType.POLYGON,
            UseMapCoordinates = true,
            UseCase = EZoneUseCase.FLAG
        };
        mdl.ZoneData.Points = new Vector2[]
        {
            new Vector2(1818.5f, 1132.5f),
            new Vector2(1907.5f, 1121.5f),
            new Vector2(1907.5f, 1243.5f),
            new Vector2(1829.5f, 1243.5f),
            new Vector2(1829.5f, 1229.5f),
            new Vector2(1790.5f, 1229.5f),
            new Vector2(1790.5f, 1192.5f),
            new Vector2(1818.5f, 1190.5f)
        };
        mdl.Adjacencies = new AdjacentFlagData[]
        {
            new AdjacentFlagData(5, 1f),
            new AdjacentFlagData(6, 1f)
        };
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 990,
            Name = "Lobby",
            X = 713.1f,
            Z = -991,
            ZoneType = EZoneType.RECTANGLE,
            UseMapCoordinates = false,
            UseCase = EZoneUseCase.LOBBY
        };
        mdl.ZoneData.SizeX = 12.2f;
        mdl.ZoneData.SizeZ = 12;
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 991,
            Name = "USA Main Base",
            ShortName = "US Main",
            X = 1853,
            Z = 1874,
            ZoneType = EZoneType.POLYGON,
            UseMapCoordinates = true,
            UseCase = EZoneUseCase.T1_MAIN
        };
        mdl.ZoneData.Points = new Vector2[]
        {
            new Vector2(1788.5f, 1811.5f),
            new Vector2(1906f, 1811.5f),
            new Vector2(1906f, 1998f),
            new Vector2(1788.5f, 1998f),
            new Vector2(1788.5f, 1904.5f),
            new Vector2(1774.5f, 1904.5f),
            new Vector2(1774.5f, 1880.5f),
            new Vector2(1788.5f, 1880.5f),
        };
        mdl.Adjacencies = new AdjacentFlagData[]
        {
            new AdjacentFlagData(7, 0.8f),
            new AdjacentFlagData(9, 1f)
        };
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 992,
            Name = "USA AMC",
            ShortName = "US AMC",
            X = 1692f,
            Z = 1825.3884f,
            ZoneType = EZoneType.RECTANGLE,
            UseMapCoordinates = true,
            UseCase = EZoneUseCase.T1_AMC
        };
        mdl.ZoneData.SizeX = 712;
        mdl.ZoneData.SizeZ = 443.2332f;
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 993,
            Name = "Russian Main Base",
            ShortName = "RU Main",
            X = 196,
            Z = 113,
            ZoneType = EZoneType.POLYGON,
            UseMapCoordinates = true,
            UseCase = EZoneUseCase.T2_MAIN
        };
        mdl.ZoneData.Points = new Vector2[]
        {
            new Vector2(142.5f, 54f),
            new Vector2(259.5f, 54f),
            new Vector2(259.5f, 120f),
            new Vector2(275f, 120f),
            new Vector2(275f, 144f),
            new Vector2(259.5f, 144f),
            new Vector2(259.5f, 240f),
            new Vector2(142.5f, 240f)
        };
        mdl.Adjacencies = new AdjacentFlagData[]
        {
            new AdjacentFlagData(8, 0.5f),
            new AdjacentFlagData(2, 0.5f),
            new AdjacentFlagData(3, 0.5f)
        };
        mdl.ValidateRead();
        DefaultZones.Add(mdl);

        mdl = new ZoneModel()
        {
            Id = 994,
            Name = "Russian AMC Zone",
            ShortName = "RU AMC",
            X = 275,
            Z = 234.6833f,
            ZoneType = EZoneType.RECTANGLE,
            UseMapCoordinates = true,
            UseCase = EZoneUseCase.T2_AMC
        };
        mdl.ZoneData.SizeX = 550;
        mdl.ZoneData.SizeZ = 469.3665f;
        mdl.ValidateRead();
        DefaultZones.Add(mdl);
    }
    public static List<Point3D> DefaultExtraPoints = new List<Point3D>
    {
        new Point3D("lobby_spawn", 713.1f, 39f, -991)
    };

    private const string T1_COLOR_PH = "%t1%";
    private const string T2_COLOR_PH = "%t2%";
    private const string T3_COLOR_PH = "%t3%";
    public static readonly Dictionary<string, string> DefaultColors = new Dictionary<string, string>()
    {
        { "default", "ffffff" },
        { "uncreated", "9cb6a4" },
        { "attack_icon_color", "ffca61" },
        { "defend_icon_color", "ba70cc" },
        { "locked_icon_color", "c2c2c2" },
        { "undiscovered_flag", "696969" },
        { "team_count_ui_color_team_1", "ffffff" },
        { "team_count_ui_color_team_2", "ffffff" },
        { "team_count_ui_color_team_1_icon", T1_COLOR_PH },
        { "team_count_ui_color_team_2_icon", T2_COLOR_PH },
        { "default_fob_color", "54e3ff" },
        { "no_bunker_fob_color", "696969" },
        { "enemy_nearby_fob_color", "ff8754" },
        { "bleeding_fob_color", "d45555" },
        { "invasion_special_fob", "5482ff" },
        { "insurgency_cache_undiscovered_color", "b780d9" },
        { "insurgency_cache_discovered_color", "555bcf" },
        { "neutral_color", "c2c2c2" },
        { "credits", "b8ffc1" },
        { "rally", "5eff87" },

        // capture ui
        { "contested", "ffdc8a" },
        { "secured", "80ff80" },
        { "nocap", "855a5a" },
        { "locked", "855a5a" },
        { "invehicle", "855a5a" },

        // Other Flag Chats
        { "flag_neutralized", "e6e3d5" },
        { "team_win", "e6e3d5" },
        { "team_capture", "e6e3d5" },

        // Deaths
        { "death_background", "ffffff" },
        { "death_background_teamkill", "ff9999" },

        // Request
        { "kit_public_header", "ffffff" },
        { "kit_level_available", "ff974d" },
        { "kit_level_unavailable", "917663" },
        { "kit_level_dollars", "7878ff" },
        { "kit_level_dollars_owned", "769fb5" },
        { "kit_level_dollars_exclusive", "96ffb2" },
        { "kit_weapon_list", "343434" },
        { "kit_unlimited_players", "111111" },
        { "kit_player_counts_available", "96ffb2" },
        { "kit_player_counts_unavailable", "c2603e" },

        // Vehicle Sign
        { "vbs_branch", "9babab" },
        { "vbs_ticket_number", "ffffff" },
        { "vbs_ticket_label", "f0f0f0" },
        { "vbs_dead", "ff0000" },
        { "vbs_idle", "ffcc00" },
        { "vbs_delay", "94cfff" },
        { "vbs_active", "ff9933" },
        { "vbs_ready", "33cc33" },
    };
    public static List<Kit> DefaultKits = new List<Kit> { };
    public static readonly List<LanguageAliasSet> DefaultLanguageAliasSets = new List<LanguageAliasSet>
    {
        new LanguageAliasSet("en-us", "English", new string[] { "english", "enus", "en", "us", "inglés", "inglesa", "ingles",
            "en-au", "en-bz", "en-ca", "en-cb", "en-ie", "en-jm", "en-nz", "en-ph", "en-tt", "en-za", "en-zw",
            "enau", "enbz", "enca", "encb", "enie", "enjm", "ennz", "enph", "entt", "enza", "enzw" } ),
        new LanguageAliasSet("ru-ru", "Russian", new string[] { "russian", "ruru", "ru", "russia", "cyrillic", "русский", "russkiy", "российский" } ),
        new LanguageAliasSet("es-es", "Spanish", new string[] { "spanish", "español", "española", "espanol", "espanola", "es", "eses",
            "es-ar", "es-bo", "es-cl", "es-co", "es-cr", "es-do", "es-ec", "es-gt", "es-hn", "es-mx", "es-ni", "es-pa", "es-pe", "es-pr", "es-py", "es-sv", "es-uy", "es-ve",
            "esar", "esbo", "escl", "esco", "escr", "esdo", "esec", "esgt", "eshn", "esmx", "esni", "espa", "espe", "espr", "espy", "essv", "esuy", "esve" } ),
        new LanguageAliasSet("de-de", "German", new string[] { "german", "deutsche", "de", "de-at", "de-ch", "de-li", "de-lu", "deat", "dech", "deli", "delu", "dede" } ),
        new LanguageAliasSet("ar-sa", "Arabic", new string[] { "arabic", "ar", "arab", "عربى", "eurbaa",
            "ar-ae", "ar-bh", "ar-dz", "ar-eg", "ar-iq", "ar-jo", "ar-kw", "ar-lb", "ar-ly", "ar-ma", "ar-om", "ar-qa", "ar-sy", "ar-tn", "ar-ye",
            "arae", "arbh", "ardz", "areg", "ariq", "arjo", "arkw", "arlb", "arly", "arma", "arom", "arqa", "arsy", "artn", "arye"}),
        new LanguageAliasSet("fr-fr", "French", new string[] { "french", "fr", "française", "français", "francaise", "francais",
            "fr-be", "fr-ca", "fr-ch", "fr-lu", "fr-mc",
            "frbe", "frca", "frch", "frlu", "frmc" }),
        new LanguageAliasSet("pl-pl", "Polish", new string[] { "polish", "plpl", "polskie", "pol", "pl" }),
        new LanguageAliasSet("zh-cn", "Chinese (Simplified)", new string[] { "chinese", "simplified chinese", "chinese simplified", "simple chinese", "chinese simple",
            "zh", "zh-s", "s-zh", "zh-hk", "zh-mo", "zh-sg", "中国人", "zhōngguó rén", "zhongguo ren", "简体中文", "jiǎntǐ zhōngwén", "jianti zhongwen", "中国人", "zhōngguó rén", "zhongguo ren",
            "zhs", "szh", "zhhk", "zhmo", "zhsg", }),
        new LanguageAliasSet("zh-tw", "Chinese (Traditional)", new string[] { "traditional chinese", "chinese traditional",
            "zhtw", "zh-t", "t-zh", "zht", "tzh", "中國傳統的", "zhōngguó chuántǒng de", "zhongguo chuantong de", "繁體中文", "fántǐ zhōngwén", "fanti zhongwen", "中國人" }),
        new LanguageAliasSet("pt-pt", "Portuguese", new string[] { "portuguese", "pt", "pt-pt", "pt-br", "ptbr", "ptpt", "português", "a língua portuguesa", "o português" }),
        new LanguageAliasSet("fil", "Filipino", new string[] { "pilipino", "fil", "pil", "tagalog", "filipino", "tl", "tl-ph", "fil-ph", "pil-ph" }),
        new LanguageAliasSet("nb-no", "Norwegian", new string[] { "norwegian", "norway", "bokmål", "bokmal", "norsk", "nb-no", "nb", "no", "nbno" }),
        new LanguageAliasSet("ro-ro", "Romanian", new string[] { "română", "romanian", "ro", "roro", "ro-ro", "romania" })
    };
}