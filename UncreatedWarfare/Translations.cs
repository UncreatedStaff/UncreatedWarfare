﻿using SDG.Unturned;
using System.Collections.Generic;
using Uncreated.Warfare.Kits;
using FlagData = Uncreated.Warfare.Gamemodes.Flags.FlagData;

namespace Uncreated.Warfare
{
    partial class JSONMethods
    {
        public static void CreateDefaultTranslations()
        {
            DefaultTranslations = new Dictionary<string, string>
            {
                { "correct_usage", "<color=#ff8c69>Correct usage: {0}</color>" },
                { "correct_usage_c", "Correct usage: {0}" },
                { "entered_main", "<color=#e6e3d5>You have entered the safety of {0} headquarters!</color>" },
                { "left_main", "<color=#e6e3d5>You have left the safety of {0} headquarters.</color>" },
                { "entered_cap_radius", "You have entered the capture radius of <color=#{1}>{0}</color>." },
                { "left_cap_radius", "You have left the cap radius of <color=#{1}>{0}</color>." },
                { "capturing", "Your team is capturing this point!" },
                { "team_capturing", "<color=#{1}>{0}</color> is capturing <color=#{3}>{2}</color>: <color=#{1}>{4}/{5}</color>" },
                { "team_clearing", "<color=#{1}>{0}</color> is clearing <color=#{3}>{2}</color>: <color=#{1}>{4}/{5}</color>" },
                { "losing", "Your team is losing this point!" },
                { "contested", "<color=#{1}>{0}</color> is contested! Eliminate all enemies to secure it." },
                { "clearing", "Your team is busy clearing this point." },
                { "secured", "This point is secure for now. Keep up the defense." },
                { "nocap", "This point is not your objective, check the right of your screen to see which points to attack and defend." },
                { "notowned", "This point is owned by the enemies. Get more players to capture it." },
                { "locked", "This point has already been captured, try to protect the objective to win." },
                { "flag_neutralized", "<color=#{1}>{0}</color> has been neutralized!" },
                { "server_desc", "<color=#9cb6a4>Uncreated Warfare</color> {0}." },
                { "team_1", "USA" },
                { "team_2", "Russia" },
                { "team_3", "Admins" },
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
                { "team_win", "<color=#{1}>{0}</color> won the game!" },
                { "team_capture", "<color=#{1}>{0}</color> captured <color=#{3}>{2}</color>!" },
                { "player_connected", "<color=#e6e3d5><color=#ffff1a>{0}</color> joined the server!</color>" },
                { "player_disconnected", "<color=#e6e3d5><color=#ffff1a>{0}</color> left the server.</color>" },
                { "command_e_gamemode", "<color=#ffa238>This command is not enabled in this gamemode.</color>" },
                { "force_loaded_gamemode", "<color=#e6e3d5>Loaded gamemode <b>{0}</b>, everyone was returned to lobby.</color>" },
                { "flag_header", "Flags" },
                { "null_transform_kick_message", "Your character is bugged, which messes up our zone plugin. Rejoin or contact a Director if this continues. (discord.gg/{0})." },

                // group
                { "group_usage", "<color=#ff8c69>Syntax: <i>/group [ join [id] | create [name] ].</i></color>" },
                { "current_group", "<color=#e6e3d5>Group <color=#4785ff>{0}</color>: <color=#4785ff>{1}</color>.</color>" },
                { "cant_create_group", "<color=#ff8c69>You can't create a group right now.</color>" },
                { "created_group", "<color=#e6e3d5>Created group <color=#4785ff>{0}</color>: <color=#4785ff>{1}</color>.</color>" },
                { "created_group_console", "{0} ({1}) created group \"{2}\": \"{3}\"" },
                { "rename_not_in_group", "<color=#ff8c69>You must be in a group to rename it.</color>" },
                { "renamed_group", "<color=#e6e3d5>Renamed group <color=#4785ff>{0}</color>: <color=#f53b3b>{1}</color> -> <color=#4785ff>{2}</color>.</color>" },
                { "renamed_group_already_named_that", "<color=#ff8c69>The group is already named that.</color>" },
                { "renamed_group_console", "{0} ({1}) renamed group \"{2}\": \"{3}\" -> \"{4}\"." },
                { "group_not_found", "<color=#ff8c69>A group with that ID was not found. Are you sure you entered an existing group ID?</color>" },
                { "not_in_group", "<color=#ff8c69>You aren't in a group.</color>" },
                { "joined_group", "<color=#e6e3d5>You have joined group {0}: <color=#4785ff>{1}</color>.</color>" },
                { "joined_already_in_group", "<color=#ff8c69>You are already in that group.</color>" },
                { "joined_group_not_found", "<color=#ff8c69>Could not find group <color=#4785ff>{0}</color>.</color>" },
                { "joined_group_console", "{0} ({1}) joined group \"{2}\": \"{3}\"." },
                { "deleted_group", "<color=#e6e3d5>Deleted group <color=#4785ff>{0}</color>: <color=#4785ff>{1}</color>.</color>" },
                { "deleted_group_console", "{0} ({1}) deleted group \"{2}\": \"{3}\"" },

                { "no_permissions", "<color=#ff8c69>You do not have permission to use this command.</color>" },

                // join
                { "teams_join_success", "<color=#d1c5b0>You have successfully joined {0}! Welcome to the battlefield.</color>" },
                { "teams_join_announce", "<color=#d1c5b0>{0} joined {1}!</color>" },
                { "teams_e_notinmain", "<color=#a8a194>You must be in <color=#fce5bb>Main</color> to switch teams." },
                { "teams_e_cooldown", "<color=#a8a194>You can switch teams in: <color=#fce5bb>{0}</color> seconds." },
                { "teams_join_e_groupnoexist", "<color=#a8a194>The team you tried to join is not set up correctly. Contact an admin to fix it.</color>" },
                { "teams_join_e_teamfull", "<color=#a8a194>The group you tried to join is full! Contact an admin to remove the server's group limit.</color>" },
                { "join_player_joined_console", "\"{0}\" ({1}) joined team \"{2}\" from \"{3}\"." },

                // Lang
                { "language_list", "<color=#f53b3b>Languages: <color=#e6e3d5>{0}</color>.</color>" },
                { "language_current", "<color=#f53b3b>Current language: <color=#e6e3d5>{0}</color>.</color>" },
                { "changed_language", "<color=#f53b3b>Changed your language to <color=#e6e3d5>{0}</color>.</color>" },
                { "change_language_not_needed", "<color=#f53b3b>You are already set to <color=#e6e3d5>{0}</color>.</color>" },
                { "reset_language", "<color=#f53b3b>Reset your language to <color=#e6e3d5>{0}</color>.</color>" },
                { "reset_language_how", "<color=#f53b3b>Do <color=#e6e3d5>/lang reset</color> to reset back to default language.</color>" },
                { "dont_have_language", "<color=#dd1111>We don't have translations for <color=#e6e3d5>{0}</color> yet. If you are fluent and want to help, feel free to ask us about submitting translations.</color>" },
                { "reset_language_not_needed", "<color=#dd1111>You are already on the default language: <color=#e6e3d5>{0}</color>.</color>" },
                
                // Toasts
                { "welcome_message", "Thanks for playing <color=#{0}>Uncreated Warfare</color>!\nWelcome back <color=#{2}>{1}</color>." },
                { "welcome_message_first_time", "Welcome to <color=#{0}>Uncreated Warfare</color>!\nTalk to the NPCs to get started." },

                // Kits
                { "kit_created", "<color=#a0ad8e>Created kit: <color=#ffffff>{0}</color></color>" },
                { "kit_search_results", "<color=#a0ad8e>Matches: <i>{0}</i>.</color>" },
                { "kit_given", "<color=#a0ad8e>Received kit: <color=#ffffff>{0}</color></color>" },
                { "kit_overwritten", "<color=#a0ad8e>Overwritten items for kit: <color=#ffffff>{0}</color></color>" },
                { "kit_copied", "<color=#a0ad8e>Copied new kit <color=#ffffff>{0}</color></color> from existing kit: <color=#c7b197>{1}</color></color>" },
                { "kit_deleted", "<color=#a0ad8e>Deleted kit: <color=#ffffff>{0}</color></color>" },
                { "kit_setprop", "<color=#a0ad8e>Set <color=#8ce4ff>{0}</color> for kit <color=#ffb89c>{1}</color> to: <color=#ffffff>{2}</color></color>" },
                { "kit_accessgiven", "<color=#a0ad8e>Allowed player: <color=#e06969>{0}</color> to access the kit: <color=#ffffff>{1}</color></color>" },
                { "kit_accessremoved", "<color=#a0ad8e>Disallowed player: <color=#e06969>{0}</color> to access the kit: <color=#ffffff>{1}</color></color>" },
                { "kit_e_exist", "<color=#ff8c69>A kit called {0} already exists.</color>" },
                { "kit_e_noexist", "<color=#ff8c69>A kit called {0} does not exist.</color>" },
                { "kit_e_set_sign_syntax", "<color=#ff8c69>Syntax: /kit set sign <kitname> <language> <sign text...></color>" },
                { "kit_e_invalidprop", "<color=#ff8c69>{0} isn't a valid a kit property. Try putting 'Class', 'Cost', 'IsPremium', etc.</color>" },
                { "kit_e_invalidarg", "<color=#ff8c69>{0} is not a valid value for kit property: {1}</color>" },
                { "kit_e_invalidarg_not_allowed", "<color=#ff8c69>You are not allowed to change {0}.</color>" },
                { "kit_e_noplayer", "<color=#ff8c69>No player found by the name of '{0}'.</color>" },
                { "kit_e_alreadyaccess", "<color=#dbc48f>Player {0} already has access to the kit: {1}.</color>" },
                { "kit_e_noaccess", "<color=#dbc48f>Player {0} already does not have access to that: {1}.</color>" },
                { "kit_e_notallowed", "<color=#ab8b82>You are not allowed to access that kit.</color>" },
                { "kit_e_wrongteam", "<color=#ab8b82>You cannot request this kit while on the wrong team.</color>" },
                { "kit_e_cooldown", "<color=#c2b39b>You can request this kit again in: <color=#bafeff>{0}</color></color>" },
                { "kit_e_cooldownglobal", "<color=#c2b39b>You can request another kit in: <color=#bafeff>{0}</color></color>" },
                { "kits_heading", "<color=#b0a99d>You have access to the following <color=#ffd494>PREMIUM</color> kits:</color>" },
                { "kits_nokits", "<color=#b0a99d>You have no premium kits. Go and <b><color=#e6c6b3>request a regular kit at the <color=#99a38e>ARMORY</color></b></color></color>" },
                { "kits_notonduty", "<color=#a6918a>You must be on duty to execute that command.</color>" },
                { "kit_l_e_invalid_steamid", "<color=#ff8c69>Invalid Steam64 ID: {0}</color>" },
                { "kit_l_e_playernotfound", "<color=#ff8c69>Could not find player with the Steam64 ID: {0}</color>" },
                { "kit_l_e_invalid_team", "<color=#ff8c69>Invalid team: {0} - try either '1' or '2'.</color>" },
                { "kit_l_e_invalid_class", "<color=#ff8c69>Invalid kit class: {0} - try 'rifleman', 'medic', 'breacher', etc.</color>" },
                { "kit_l_e_kitexists", "<color=#ff8c69>Something went wrong and this loadout could not be created (loadout already exists)</color>" },
                { "kit_l_created", "<color=#a0ad8e>Created <color=#c4c9bb>{0}</color> loadout for <color=#deb692>{1}</color> (<color=#968474>{2}</color>). Kit name: <color=#ffffff>{3}</color></color>" },
                

                // Range
                { "range", "<color=#9e9c99>The range to your squad's marker is: <color=#8aff9f>{0}m</color></color>" },
                { "range_nomarker", "<color=#9e9c99>You squad has no marker.</color>" },
                { "range_notsquadleader", "<color=#9e9c99>Only <color=#cedcde>SQUAD LEADERS</color> can place markers.</color>" },
                { "range_notinsquad", "<color=#9e9c99>You must JOIN A SQUAD in order to do /range.</color>" },

                // Squads
                { "squad_created", "<color=#a0ad8e>You created the squad <color=#ffffff>{0}</color></color>" },
                { "squad_ui_reloaded", "<color=#a0ad8e>Squad UI has been reloaded.</color>" },
                { "squad_joined", "<color=#a0ad8e>You joined <color=#ffffff>{0}</color>.</color>" },
                { "squad_left", "<color=#a7a8a5>You left your squad.</color>" },
                { "squad_disbanded", "<color=#a7a8a5>Your squad was disbanded.</color>" },
                { "squad_locked", "<color=#a7a8a5>You <color=#6be888>locked</color> your squad.</color>" },
                { "squad_unlocked", "<color=#999e90>You <color=#ffffff>unlocked</color> your squad.</color>" },
                { "squad_promoted", "<color=#b9bdb3><color=#ffc94a>{0}</color> was promoted to squad leader.</color>" },
                { "squad_kicked", "<color=#b9bdb3>You were kicked from Squad <color=#6be888>{0}</color>.</color>" },
                { "squad_e_exist", "<color=#a89791>A squad with a similar name to '{0}' already exists.</color>" },
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
                { "squad_no_no_words", "<color=#ff8c69>You can't name a squad that.</color>" },
                { "squads_disabled", "<color=#ff8c69>Squads are disabled.</color>" },
                { "squad_too_many", "<color=#ff8c69>There can not be more than 8 squads on a team at once.</color>" },

                { "squad_ui_name", "{0}" },
                { "squad_ui_player_name", "{0}" },
                { "squad_ui_squad_name", "{0}" },
                { "squad_ui_header_name", "{0} <color=#8c8c8c>{1}/6</color>" },
                { "squad_ui_player_count", "<color=#bd6b5b>{0}</color>{1}/6" },
                { "squad_ui_player_count_small", "{0}/6" },
                { "squad_ui_player_count_small_locked", "<color=#969696>{0}/6</color>" },
                { "squad_ui_locked_symbol", "<color=#bd6b5b>{0}</color>" },
                { "squad_ui_leader_name", "{0}" },
                { "squad_ui_expanded", "..." },

                // orders   

                { "order_usage_1", "<color=#9fa1a6>To give orders: <color=#9dbccf>/order <i>squad_name action</i></color'. Type <color=#d1bd90>/order actions</color> to see a list of actions.</color>" },
                { "order_actions", "<color=#9fa1a6>Order actions: <color=#9dbccf>{0}</color></color>" },
                { "order_usage_2", "<color=#9fa1a6>Try typing: <color=#9dbccf>/order <b>{0}</b> <i>action</i></color>" },
                { "order_usage_3", "<color=#9fa1a6>Try typing: <color=#9dbccf>/order {0} <b>action</b></color>. Type <color=#d1bd90>/order actions</color> to see a list of actions.</color>" },
                { "order_e_squadnoexist", "<color=#9fa1a6>There is no friendly squad called '{0}'.</color>" },
                { "order_e_actioninvalid", "<color=#9fa1a6>'{0}' is not a valid action. Try one of these: {1}</color>" },
                { "order_e_alreadyhasorder_marker", "<color=#9fa1a6>{0} already has orders from <color=#d1ac90>{1}</color></color>" },
                { "order_e_attack_marker", "<color=#9fa1a6>Place a map marker on a <color=#d1bd90>position</color> or <color=#d1bd90>flag</color> where you want {0} to attack.</color>" },
                { "order_e_defend_marker", "<color=#9fa1a6>Place a map marker on the <color=#d1bd90>position</color> or <color=#d1bd90>flag</color> where you want {0} to attack.</color>" },
                { "order_e_buildfob_marker", "<color=#9fa1a6>Place a map marker where you want {0} to build a <color=#d1bd90>FOB</color>.</color>" },
                { "order_e_buildfob_fobexists", "<color=#9fa1a6>There is already a friendly FOB near that marker.</color>" },
                { "order_e_buildfob_foblimit", "<color=#9fa1a6>The max FOB limit has been reached.</color>" },
                { "order_e_squadtooclose", "<color=#9fa1a6>{0} is already near that marker. Try placing it further away.</color>" },
                { "order_e_raycast", "<color=#b58b86>Something went wrong while raycasting. Please contact the devs/admins about this.</color>" },
                { "order_s_sent", "<color=#9fa1a6>Order sent to {0}: <color=#9dbccf>{1}</color></color>" },
                { "order_s_received", "<color=#9fa1a6><color=#9dbccf>{0}</color> has given your squad new orders:\n<color=#d4d4d4>{1}</color></color>" },

                // rally
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

                // fobs
                { "time_left", "<color=#ffaa42>Time left: <color=#ffe4b5>{0}</color>.</color>" },
                { "build_error_notinradius", "<color=#ffab87>This can only be placed inside FOB RADIUS.</color>" },
                { "build_error_radiustoosmall", "<color=#ffab87>This can only be placed within {0}m of this FOB Radio right now. Expand this range by building a FOB BUNKER.</color>" },
                { "build_error_noradio", "<color=#ffab87>This can only be placed within {0}m of a friendly FOB RADIO.</color>" },
                { "build_error_structureexists", "<color=#ffab87>This FOB already has {0} {1}.</color>" },
                { "build_error_notenoughbuild", "<color=#fae69c>You are missing nearby build! <color=#d1c597>Building Supplies: </color><color=#d1c597>{0}/{1}</color></color>" },
                { "build_error_tooclosetomain", "<color=#fae69c>You cannot build too close to main.</color>" },
                { "build_error_maxemplacements", "<color=#d1c597>This FOB already has {0} {1}s.</color>" },
                { "build_error_notbuildable", "<color=#d1c597>That barricade is not buildable.</color>" },
                { "build_error_too_many_fobs", "<color=#ffab87>The max number of FOBs has been reached.</color>" },
                { "no_placement_fobs_underwater", "<color=#ffab87>You can't build a FOB underwater.</color>" },
                { "no_placement_fobs_too_high", "<color=#ffab87>You can't build a FOB more than {0}m above the ground.</color>" },
                { "no_placement_fobs_too_near_base", "<color=#ffab87>You can't build a FOB this close to main base.</color>" },
                { "fob_nofobs", "<color=#b5a591>Your team has no active FOBs. Take a Logi Truck and go and build some!</color>" },
                { "fob_built", "<color=#b0ffa8>Successfully built FOB! Your team may now spawn on it.</color>" },
                { "fob_teleported", "<color=#fae69c>You have been deployed to <color=#54e3ff>{0}</color>.</color>" },
                { "fob_error_nologi", "<color=#ffab87>You must be near a friendly LOGISTICS TRUCK to place that FOB radio.</color>" },
                { "fob_error_fobtooclose", "<color=#ffa238>You are too close to an existing FOB Radio ({0}m away). You must be at least {1}m away to place a new radio.</color>" },
                { "fob_error_limitreached", "<color=#ffa238>The number of FOBs allowed on the map has been reached.</color>" },
                { "ammocrate_built", "<color=#b0ffa8>Successfully built ammo crate. Your team may now resupply from it.</color>" },
                { "ammocrate_error_alreadyexists", "<color=#ffa238>This FOB already has an ammo crate.</color>" },
                { "repairstation_built", "<color=#b0ffa8>Successfully built repair station. Your team may now repair damaged vehicles at this FOB.</color>" },
                { "repairstation_error_alreadyexists", "<color=#ffa238>This FOB already has a repair station.</color>" },
                { "emplacement_built", "<color=#b0ffa8>Successfully built {0}. Do /ammo on it to resupply.</color>" },
                { "fortification_built", "<color=#b0ffa8>Successfully built {0}.</color>" },
                { "fob_ui", "{0}   {1}" },
                { "cache_destroyed_attack", "<color=#e8d1a7>WEAPONS CACHE HAS BEEN ELIMINATED</color>" },
                { "cache_destroyed_defense", "<color=#deadad>WEAPONS CACHE HAS BEEN DESTROYED</color>" },
                { "cache_discovered_attack", "<color=#dbdbdb>NEW WEAPONS CACHE DISCOVERED NEAR <color=#e3c59a>{0}</color></color>" },
                { "cache_discovered_defense", "<color=#d9b9a7>WEAPONS CACHE HAS BEEN COMPROMISED, DEFEND IT</color>" },
                { "cache_spawned_defense", "<color=#a8e0a4>NEW WEAPONS CACHE IS NOW ACTIVE</color>" },

                // deployment
                { "deploy_s", "<color=#fae69c>You have arrived at <color=#{0}>{1}</color>.</color>" },
                { "deploy_c_notspawnable", "<color=#ffa238>The FOB you were deploying is no longer active.</color>" },
                { "deploy_c_cachedead", "<color=#ffa238>The Cache you were deploying to was destroyed!</color>" },
                { "deploy_c_damaged", "<color=#ffa238>You were damaged while trying to deploy!</color>" },
                { "deploy_c_moved", "<color=#ffa238>You moved and can no longer deploy!</color>" },
                { "deploy_c_enemiesNearby", "<color=#ffa238>You no longer deploy to that location - there are enemies nearby.</color>" },
                { "deploy_c_notactive", "<color=#ffa238>The point you were deploying to is no longer active.</color>" },
                { "deploy_c_dead", "<color=#ffa238>You died and can no longer deploy!</color>" },
                { "deploy_e_fobnotfound", "<color=#b5a591>There is no location or FOB by the name of '{0}'.</color>" },
                { "deploy_e_notnearfob", "<color=#b5a591>You must be on an active friendly FOB or at main in order to deploy again.</color>" },
                { "deploy_e_cooldown", "<color=#b5a591>You can deploy again in: <color=#e3c27f>{0}</color>.</color>" },
                { "deploy_e_alreadydeploying", "<color=#b5a591>You are already deploying somewhere.</color>" },
                { "deploy_e_incombat", "<color=#ffaa42>You are in combat, soldier! You can deploy in another: <color=#e3987f>{0}</color>.</color>" },
                { "deploy_e_injured", "<color=#ffaa42>You can not deploy while injured, get a medic to revive you or give up.</color>" },
                { "deploy_e_enemiesnearby", "<color=#ffa238>You cannot deploy to that FOB - there are enemies nearby.</color>" },
                { "deploy_e_nobunker", "<color=#ffa238>That FOB has no bunker. Your team must build a FOB BUNKER before you can deploy to it.</color>" },
                { "deploy_standby", "<color=#fae69c>Now deploying to <color=#54e3ff>{0}</color>. You will arrive in <color=#eeeeee>{1} seconds</color>.</color>" },
                { "deploy_fob_standby", "<color=#fae69c>Now deploying to <color=#{0}>{1}</color>. You will arrive in <color=#eeeeee>{2} seconds</color>.</color>" },
                { "deploy_standby_nomove", "<color=#fae69c>Now deploying to <color=#54e3ff>{0}</color>. Stand still for <color=#eeeeee>{1} seconds</color>.</color>" },
                { "deploy_lobby_removed", "<color=#fae69c>The lobby has been removed, use  <i>/teams</i> to switch teams instead.</color>" },

                // /ammo
                { "ammo_error_nocrate", "<color=#ffab87>Look at an Ammo Crate, Ammo Bag or Vehicle in order to resupply.</color>" },
                { "ammo_success", "<color=#ffab87>Your kit has been resupplied. <color=#dea095>-1x AMMO BOX</color></color>" },
                { "ammo_success_bag", "<color=#ffab87>Your kit has been resupplied. <color=#d1c597>{0} uses remaining</color></color>" },
                { "ammo_success_bag_finished", "<color=#ffab87>Your kit has been resupplied.</color>" },
                { "ammo_success_vehicle", "<color=#ffab87>Your vehicle has been resupplied. <color=#d1c597>-{0}x AMMO BOX{1}</color></color>" },
                { "ammo_vehicle_cant_rearm", "<color=#b3a6a2>This vehicle can't be resupplied.</color>" },
                { "ammo_vehicle_out_of_main", "<color=#b3a6a2>This vehicle can only be resupplied at main base.</color>" },
                { "ammo_vehicle_full_already", "<color=#b3a6a2>This vehicle does not need to be resupplied.</color>" },
                { "ammo_not_near_fob", "<color=#b3a6a2>This repair station is not built on a friendly FOB.</color>" },
                { "ammo_not_near_repair_station", "<color=#b3a6a2>Your vehicle must be next to a <color=#e3d5ba>REPAIR STATION</color> in order to rearm.</color>" },
                { "ammo_not_in_team", "<color=#b3a6a2>You must be on a team to use this feature.</color>" },
                { "ammo_not_enough_stock", "<color=#b3a6a2>This FOB is missing ammo! Required: <color=#dea095>{0}/{1}</color></color>" },
                { "vehicle_staging", "<color=#b3a6a2>You cannot enter this vehicle during the staging phase.</color>" },
                { "ammo_no_kit", "<color=#b3a6a2>You don't have a kit yet. Go and request one at main.</color>" },
                { "ammo_cooldown", "<color=#b3a6a2>You can not refill your kit for another {0} seconds.</color>" },
                { "ammo_vehicle_cooldown", "<color=#b3a6a2>You can not refill your vehicle for another {0} seconds.</color>" },
                { "ammo_not_rifleman", "<color=#b3a6a2>You must be a RIFLEMAN in order to place this Ammo Bag.</color>" },
                { "ammo_bag_already_resupplied", "<color=#b3a6a2>You have already resupplied from this ammo bag.</color>" },

                // End UI
                { "game_over", "Game Over!" },
                { "winner", "<color=#{1}>{0}</color> Won!" },
                { "lb_header_1", "Top Squad: <color=#{1}>{0}</color>" },
                { "lb_header_1_no_squad", "No Squads Participated." },
                { "lb_header_2", "Most Kills" },
                { "lb_header_3", "Time On Point" },
                { "lb_header_4", "Most XP Gained" },
                { "lb_player_name", "<color=#{1}>{0}</color>" },
                { "lb_player_value", "<color=#{1}>{0}</color>" },
                { "lb_float_player_value", "<color=#{1}>{0:0.00}</color>" },
                { "lb_time_player_value", "<color=#{1}>{0:hh\\:mm\\:ss}</color>" },
                { "stats_player_value", "<color=#{1}>{0}</color>" },
                { "stats_war_value", "<color=#{1}>{0}</color>" },
                { "stats_player_time_value", "<color=#{1}>{0:hh\\:mm\\:ss}</color>" },
                { "stats_war_time_value", "<color=#{1}>{0:hh\\:mm\\:ss}</color>" },
                { "stats_player_float_value", "<color=#{1}>{0:0.00}</color>" },
                { "stats_war_float_value", "<color=#{1}>{0:0.00}</color>" },
                { "player_name_header", "<color=#{1}>{0}</color> - {2:n0}% presence." },
                { "war_name_header", "<color=#{1}>{0}</color> vs <color=#{3}>{2}</color>" },
                { "longest_shot_format", "{0}m - {1}\n{2}" },
                { "lblKills", "Kills: " },
                { "lblDeaths", "Deaths: " },
                { "lblKDR", "K/D Ratio: " },
                { "lblKillsOnPoint", "Kills on Flag: " },
                { "lblTimeDeployed", "Time Deployed: " },
                { "lblXpGained", "XP Gained: " },
                { "lblTimeOnPoint", "Time on Flag: " },
                { "lblCaptures", "Captures: " },
                { "lblTimeInVehicle", "Damage Dealt: " },
                { "lblTeamkills", "Teamkills: " },
                { "lblFOBsDestroyed", "FOBs Destroyed: " },
                { "lblOfficerPointsGained", "Officer Points Gained: " },
                { "lblDuration", "Duration: " },
                { "lblDeathsT1", "US Casualties: " },
                { "lblDeathsT2", "RU Casualties: " },
                { "lblOwnerChangeCount", "Total Flag Swaps: " }, // amount of times the flag changed owners or got captured from neutral
                { "lblAveragePlayerCountT1", "US Average Army: " },
                { "lblAveragePlayerCountT2", "RU Average Army: " },
                { "lblFOBsPlacedT1", "US FOBs Built: " },
                { "lblFOBsPlacedT2", "RU FOBs Built: " },
                { "lblFOBsDestroyedT1", "US FOBs Destroyed: " },
                { "lblFOBsDestroyedT2", "RU FOBs Destroyed: " },
                { "lblTeamkillingCasualties", "Teamkill Casualties: " },
                { "lblTopRankingOfficer", "Longest Shot: " },
                { "next_game_start_label", "Next Game Starting In" },
                { "next_game_start_label_shutting_down", "<color=#00ffff>Shutting Down Because: \"{0}\"</color>" },
                { "next_game_starting_format", "{0:mm\\:ss}" },

                // SIGNS - must prefix with "sign_" for them to work
                { "sign_rules", "Rules\nNo suicide vehicles.\netc." },

                // Admin Commands
                { "server_not_running", "<color=#9cffb3>This is not a server.</color>" },
                // kick
                { "kick_syntax", "<color=#9cffb3>Syntax: <i>/kick <player> <reason ...></i>.</color>" },
                { "kick_no_reason_provided", "<color=#9cffb3>You must provide a reason.</color>" },
                { "kick_no_player_found", "<color=#9cffb3>No player found from <color=#d8addb>{0}</color>.</color>" },
                { "kick_no_player_found_console", "No player found from \"{0}\"." },
                { "kick_kicked_feedback", "<color=#00ffff>You kicked <color=#d8addb>{0}</color>.</color>" },
                { "kick_kicked_broadcast", "<color=#00ffff><color=#d8addb>{0}</color> was kicked by <color=#00ffff>{1}</color>.</color>" },
                { "kick_kicked_broadcast_operator", "<color=#00ffff><color=#d8addb>{0}</color> was kicked by an operator.</color>" },
                { "kick_kicked_console_operator", "{0} ({1}) was kicked by an operator because: {2}." },
                { "kick_kicked_console", "{0} ({1}) was kicked by {2} ({3}) because: {4}." },
                { "kick_autokick_namefilter", "Your name does not contain enough alphanumeric characters in succession (5), please change your name and rejoin." },
                // ban
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
                { "ban_no_player_found_console", "No player found from \"{0}\"." },
                { "ban_invalid_number", "<color=#9cffb3><color=#9cffb3>{0}</color> should be a whole number between <color=#00ffff>1</color> and <color=#00ffff>2147483647</color>.</color>" },
                { "ban_invalid_number_console", "Failed to cast \"{0}\" to a Int32 (1 to 2147483647)." },
                { "ban_no_reason_provided", "<color=#9cffb3>You must provide a reason.</color>" },
                // ip ban
                { "ip_ban_syntax", "<color=#9cffb3>Syntax: <i>/ipban <player> <duration minutes> <reason ...></i>.</color>" },
                { "ip_ban_permanent_feedback", "<color=#00ffff><color=#d8addb>{0}</color> was <b>permanently</b> ip-banned.</color>" },
                { "ip_ban_permanent_feedback_noip", "<color=#00ffff><color=#d8addb>{0}</color> was <b>permanently</b> banned but was unable to find their ip.</color>" },
                { "ip_ban_permanent_console_operator", "{0} ({1}) was permanently ip-banned by an operator because: {2}." },
                { "ip_ban_permanent_console", "{0} ({1}) was permanently ip-banned by {2} ({3}) because: {4}." },
                { "ip_ban_permanent_console_noip", "{0} ({1}) was permanently banned by {2} ({3}) because: {4}, but their IP couldn't be found." },
                { "ip_ban_feedback", "<color=#00ffff><color=#d8addb>{0}</color> was ip-banned for <color=#9cffb3>{1}</color>.</color>" },
                { "ip_ban_feedback_noip", "<color=#00ffff><color=#d8addb>{0}</color> was banned for <color=#9cffb3>{1}</color>, but their IP couldn't be found.</color>" },
                { "ip_ban_console_operator", "{0} ({1}) was ip-banned by an operator for {3} because: {2}." },
                { "ip_ban_console", "{0} ({1}) was ip-banned by {2} ({3}) for {5} because: {4}." },
                { "ip_ban_console_noip", "{0} ({1}) was banned by {2} ({3}) for {5} because: {4}, but their IP couldn't be found." },
                { "ip_ban_no_player_found", "<color=#9cffb3>No player found from <color=#d8addb>{0}</color>.</colo  r>" },
                { "ip_ban_no_player_found_console", "No player found from \"{0}\"." },
                { "ip_ban_invalid_number", "<color=#9cffb3><color=#9cffb3>{0}</color> should be a whole number between <color=#00ffff>0</color> and <color=#00ffff>2147483647</color>.</color>" },
                { "ip_ban_invalid_number_console", "Failed to cast \"{0}\" to a Int32 (0 to 2147483647)." },
                { "ip_ban_no_reason_provided", "<color=#9cffb3>You must provide a reason.</color>" },
                // warn
                { "warn_syntax", "<color=#9cffb3>Syntax: <i>/warn <player> <reason ...></i>.</color>" },
                { "warn_no_player_found", "<color=#9cffb3>No player found from <color=#d8addb>{0}</color>.</color>" },
                { "warn_no_player_found_console", "No player found from \"{0}\"." },
                { "warn_no_reason_provided", "<color=#9cffb3>You must provide a reason.</color>" },
                { "warn_warned_private_operator", "<color=#ffff00>An operator warned you for: <b>{0}</b>.</color>" },
                { "warn_warned_console_operator", "Warned {0} ({1}) for: {2}" },
                { "warn_warned_broadcast_operator", "<color=#ffff00><color=#d8addb>{0}</color> was warned by an operator.</color>" },
                { "warn_warned_feedback", "<color=#ffff00>You warned <color=#d8addb>{0}</color>.</color>" },
                { "warn_warned_private", "<color=#ffff00><color=#00ffff>{0}</color> warned you for: <b>{1}</b>.</color>" },
                { "warn_warned_console", "{0} ({1}) was warned by {2} ({3}) for: {4}" },
                { "warn_warned_broadcast", "<color=#ffff00><color=#d8addb>{0}</color> was warned by <color=#00ffff>{1}</color>.</color>" },
                // mute
                { "mute_syntax", "<color=#9cffb3>Syntax: /mute <voice|text|both> <name or steam64> <permanent | duration in minutes> <reason...></color>" },
                { "mute_no_player_found", "<color=#9cffb3>No online players found with the name <color=#d8addb>{0}</color>. To mute someone that's offline, use their Steam64 ID.</color>" },
                { "mute_cant_read_duration", "<color=#9cffb3>The given value for duration must be a positive number or 'permanent'.</color>" },

                // amc
                { "amc_reverse_damage", "<color=#f53b3b>Stop <b><color=#ff3300>main-camping</color></b>! Damage is <b>reversed</b> back on you.</color>" },
                // unban
                { "unban_syntax", "<color=#9cffb3>Syntax: <i>/unban <player id></i>.</color>" },
                { "unban_no_player_found", "<color=#9cffb3>No player ID found from <color=#d8addb>{0}</color>.</color>" },
                { "unban_no_player_found_console", "No Steam64 ID found from \"{0}\"." },
                { "unban_player_not_banned", "<color=#9cffb3>Player <color=#d8addb>{0}</color> is not banned. You must use Steam64's for /unban.</color>" },
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
                // loadbans
                { "loadbans_NoBansErrorText", "There are no banned players." },
                { "loadbans_LogBansDisabled", "Can't upload, Logging bans is disabled." },
                { "loadbans_UploadedBans", "Uploaded {0} ban{1} to the MySQL database and logged them." },
                // duty
                { "duty_admin_on_console", "{0} ({1}) went on duty." },
                { "duty_admin_off_console", "{0} ({1}) went off duty." },
                { "duty_intern_on_console", "{0} ({1}) went on duty." },
                { "duty_intern_off_console", "{0} ({1}) went off duty." },
                { "duty_on_feedback", "<color=#c6d4b8>You are now <color=#95ff4a>on duty</color>.</color>" },
                { "duty_off_feedback", "<color=#c6d4b8>You are now <color=#ff8c4a>off duty</color>.</color>" },
                { "duty_on_broadcast", "<color=#c6d4b8><color=#d9e882>{0}</color> is now <color=#95ff4a>on duty</color>.</color>" },
                { "duty_off_broadcast", "<color=#c6d4b8><color=#d9e882>{0}</color> is now <color=#ff8c4a>off duty</color>.</color>" },
                { "duty_while_on_duty", "<color=#cdff42>{0}</color> killed someone while <color=#ff6d24>on duty</color>! Perhaps they are abusing?" },
                { "duty_while_on_duty_console", "{0} ({1}) killed {2} ({3}) while on duty!!" },
                // tk system
                { "teamkilled_console_log", "{0} ({1}) teamkilled {2} ({3})!!" },
                // restrictions
                { "no_placement_on_vehicle", "<color=#f53b3b>You can't place a{1} <color=#d9e882>{0}</color> on a vehicle!</color>" },
                { "cant_steal_batteries", "<color=#f53b3b>Stealing batteries is not allowed.</color>" },
                { "cant_leave_group", "<color=#f53b3b>You are not allowed to manually change groups.</color>" },
                { "cant_store_this_item", "<color=#f53b3b>You are not allowed to store <color=#d9e882>{0}</color>.</color>" },
                { "marker_not_in_squad", "<color=#f53b3b>Only your squad can see markers, join a squad with <color=#d9e882>/squad join <name></color> or <color=#d9e882>/squad create <name></color> to use this feature.</color>" },
                { "entered_enemy_territory", "Too close to enemy base! You will die in {0} second{1}!     " },

                { "afk_warning", "<color=#f53b3b>You will be AFK-Kicked in {0} if you don't move.</color>" },

                { "vehicle_owner_not_in_vehicle", "Wait for the owner {0} to get in." },
                { "vehicle_owner_not_in_vehicle_squad", "Wait for the owner {0} to get in, or join the squad {1}." },
                { "vehicle_no_kit", "You can not get in a vehicle without a kit." },
                { "vehicle_too_high", "<color=#ff684a>Vehicle is too high off the ground!</color>" },
                { "vehicle_not_valid_kit", "You need a <color=#cedcde>{0}</color> kit in order to main this vehicle." },
                { "vehicle_need_another_person_with_kit", "You must have another <color=#cedcde>{0}</color> in this vehicle before you can enter the gunner's seat." },
                { "vehicle_need_driver", "Your vehicle needs a DRIVER before you can switch to the gunner's seat." },
                { "vehicle_need_crew", "Wait for this vehicle's CREW to get in first." },
                { "vehicle_cannot_switch", "You cannot switch from driver's seat to gunner's seat in this vehicle." },
                
                // other
                { "friendly_mortar_incoming", "FRIENDLY MORTAR STRIKE INCOMING" },
                // battleye
                { "battleye_kick_console", "{0} ({1}) was kicked by BattlEye because: \"{2}\"" },
                { "battleye_kick_broadcast", "<color=#00ffff>{0} was kicked by <color=#feed00>BattlEye</color>.</color>" },
                // request
                { "request_saved_sign", "<color=#a4baa9>Saved kit: <color=#ffebbd>{0}</color>.</color>" },
                { "request_removed_sign", "<color=#a4baa9>Removed kit sign: <color=#ffebbd>{0}</color>.</color>" },
                { "request_sign_exists", "<color=#a8918a>A sign is already registered at that location, remove it with /request remove.</color>" },
                { "request_not_looking", "<color=#a8918a>You must be looking at a request sign or vehicle.</color>" },
                { "request_already_saved", "<color=#a8918a>That sign is already saved.</color>" },
                { "request_already_removed", "<color=#a8918a>That sign has already been removed.</color>" },
                { "request_kit_given", "<color=#99918d>You have been allocated a <color=#cedcde>{0}</color> kit!</color>" },
                { "request_kit_e_signnoexist", "<color=#a8918a>This is not a request sign.</color>" },
                { "request_kit_e_kitnoexist", "<color=#a8918a>This kit has not been created yet.</color>" },
                { "request_kit_e_alreadyhaskit", "<color=#a8918a>You already have this kit.</color>" },
                { "request_kit_e_notallowed", "<color=#a8918a>You do not have access to this kit.</color>" },
                { "request_kit_e_limited", "<color=#a8918a>Your team already has a max of {0} players using this kit. Try again later.</color>" },
                { "request_kit_e_wronglevel", "<color=#b3ab9f>You must be <color=#ff8f8f>{0}</color> - <color=#ffc29c>Level {1}</color> to request this kit.</color>" },
                { "request_kit_e_wrongbranch", "<color=#a8918a>You must be a different branch.</color>" },
                { "request_kit_e_notsquadleader", "<color=#b3ab9f>You must be a <color=#cedcde>SQUAD LEADER</color> in order to get this kit.</color>" },
                { "request_loadout_e_notallowed", "<color=#a8918a>You do not own this loadout.</color>" },
                { "request_vehicle_e_notrequestable", "<color=#a8918a>This vehicle cannot be reqested.</color>" },
                { "request_vehicle_e_cooldown", "<color=#b3ab9f>This vehicle can be requested in: <color=#ffe2ab>{0}</color>.</color>" },
                { "request_vehicle_e_delay", "<color=#b3ab9f>This vehicle is delayed for another: <color=#94cfff>{0}</color>.</color>" },
                { "request_vehicle_e_notinsquad", "<color=#b3ab9f>You must be <color=#cedcde>IN A SQUAD</color> in order to request this vehicle.</color>" },
                { "request_vehicle_e_nokit", "<color=#a8918a>Get a kit before you request vehicles.</color>" },
                { "request_vehicle_e_notinteam", "<color=#a8918a>You must be on the other team to request this vehicle.</color>" },
                { "request_vehicle_e_wrongkit", "<color=#b3ab9f>You need a {0} kit in order to request this vehicle.</color>" },
                { "request_vehicle_e_wronglevel", "<color=#b3ab9f>You must be <color=#ff8f8f>{0}</color> - <color=#ffc29c>Level {1}</color> to request this vehicle.</color>" },
                { "request_vehicle_e_wrongbranch", "<color=#b3ab9f>You must be a part of <color=#fcbda4>{0}</color> to request this vehicle.</color>" },
                { "request_vehicle_e_alreadyrequested", "<color=#a8918a>This vehicle has already been requested.</color>" },
                { "request_vehicle_e_already_owned", "<color=#a8918a>You have already requested a nearby vehicle.</color>" },
                { "request_vehicle_e_staging", "<color=#a6918a>This vehicle can only be requested after the game starts.</color>" },
                { "request_vehicle_given", "<color=#b3a591>This <color=#ffe2ab>{0}</color> is now yours to take into battle.</color>" },


                // structure
                { "structure_not_looking", "<color=#ff8c69>You must be looking at a barricade, structure, or vehicle.</color>" },
                { "structure_saved", "<color=#e6e3d5>Saved <color=#c6d4b8>{0}</color>.</color>" },
                { "structure_saved_already", "<color=#e6e3d5><color=#c6d4b8>{0}</color> is already saved.</color>" },
                { "structure_saved_not_vehicle", "<color=#ff8c69>You can not save a vehicle.</color>" },
                { "structure_saved_not_bush", "<color=#ff8c69>Why are you trying to save a bush?</color>" },
                { "structure_unsaved_not_bush", "<color=#ff8c69>Why are you trying to unsave a bush?</color>" },
                { "structure_unsaved", "<color=#e6e3d5><color=#e6e3d5>Removed <color=#c6d4b8>{0}</color> save.</color>" },
                { "structure_unsaved_already", "<color=#ff8c69><color=#c6d4b8>{0}</color> is not saved.</color>" },
                { "structure_unsaved_not_vehicle", "<color=#ff8c69>You can not save or remove a vehicle.</color>" },
                { "structure_popped", "<color=#e6e3d5>Destroyed <color=#c6d4b8>{0}</color>.</color>" },
                { "structure_pop_not_poppable", "<color=#ff8c69>That object can not be destroyed.</color>" },
                { "structure_examine_not_examinable", "<color=#ff8c69>That object can not be examined.</color>" },
                { "structure_examine_not_locked", "<color=#ff8c69>This vehicle is not locked.</color>" },
                { "structure_last_owner_web_prompt", "Last owner of {0}: {1}, Team: {2}." },
                { "structure_last_owner_chat", "<color=#c6d4b8>Last owner of <color=#e6e3d5>{0}</color>: <color=#{3}>{1} <i>({2})</i></color>, Team: <color=#{5}>{4}</color>.</color>" },

                // whitelist
                { "whitelist_added", "<color=#a0ad8e>Whitelisted item: <color=#ffffff>{0}</color></color>" },
                { "whitelist_removed", "<color=#a0ad8e>Un-whitelisted item: <color=#ffffff>{0}</color></color>" },
                { "whitelist_setamount", "<color=#a0ad8e>Set max allowed amount for item <color=#ffffff>{1}</color> to: <color=#8ce4ff>{2}</color></color>" },
                { "whitelist_setsalvagable", "<color=#a0ad8e>Set salvagable property for item <color=#ffffff>{1}</color> to: <color=#8ce4ff>{2}</color></color>" },
                { "whitelist_e_exist", "<color=#ff8c69>That item is already whitelisted.</color>" },
                { "whitelist_e_noexist", "<color=#ff8c69>That item is not yet whitelisted.</color>" },
                { "whitelist_e_invalidid", "<color=#ff8c69>{0} is not a valid item ID.</color> " },
                { "whitelist_e_invalidamount", "<color=#ff8c69>{0} is not a valid number.</color> " },
                { "whitelist_e_invalidsalvagable", "<color=#ff8c69>{0} is not a valid true or false value.</color> " },
                { "whitelist_notallowed", "<color=#ff8c69>The item is not allowed to be picked up.</color> " },
                { "whitelist_maxamount", "<color=#ff8c69>You are not allowed to carry any more of this item.</color> " },
                { "whitelist_kit_maxamount", "<color=#ff8c69>Your kit does not allow you to have any more of this item.</color> " },
                { "whitelist_nokit", "<color=#ff8c69>Get a kit first before you can pick up items.</color> " },
                { "whitelist_nosalvage", "<color=#ff8c69>You are not allowed to salvage that.</color> " },
                { "whitelist_noplace", "<color=#ff8c69>You are not allowed to place that.</color> " },
                { "whitelist_toomanyplaced", "<color=#ff8c69>You cannot place more than {0} of those.</color> " },
                { "whitelist_noeditsign", "<color=#ff8c69>You are not allowed to edit that sign.</color> " },
                
                //vehiclebay
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

                // vehiclebay spawns
                { "vehiclebay_spawn_registered", "<color=#a0ad8e>Successfully registered spawn. <color=#ffffff>{0}s</color> will spawn here.</color>" },
                { "vehiclebay_spawn_deregistered", "<color=#a0ad8e>Successfully deregistered spawn.</color>" },
                { "vehiclebay_link_started", "<color=#a0ad8e>Started linking, do /vb link on the sign now.</color>" },
                { "vehiclebay_link_finished", "<color=#a0ad8e>Successfully registered vehicle sign link.</color>" },
                { "vehiclebay_link_not_started", "<color=#ff8c69>You must do /vb link on a vehicle bay first.</color>" },
                { "vehiclebay_spawn_removed", "<color=#a0ad8e>Vehicle spawn was deregistered it's barricade was salvaged.</color>" },
                { "vehiclebay_spawn_forced", "<color=#a0ad8e>Skipped timer for <color=#ffffff>{0}</color>.</color>" },
                { "vehiclebay_e_invalidid", "<color=#ff8c69>{0} is not a valid vehicle ID.</color>" },
                { "vehiclebay_e_idnotfound", "<color=#ff8c69>Could not find vehicle with ID: {0}</color>" },
                { "vehiclebay_e_spawnexist", "<color=#ff8c69>This spawn is already registered. Unregister it first.</color>" },
                { "vehiclebay_e_spawnnoexist", "<color=#ff8c69>This spawn is not registered.</color>" },
                { "vehiclebay_check_registered", "<color=#a0ad8e>This spawn (<color=#8ce4ff>{0}</color>) is registered with vehicle: <color=#ffffff>{1} - {2}</color></color>" },
                { "vehiclebay_check_notregistered", "<color=#a0ad8e>This spawn is not registered.</color>" },

                { "VEHICLE_DESTROYED", "{0} took out {1}'s {2}." },
                { "VEHICLE_DESTROYED_UNKNOWN", "{0} took out a {2}." },
                { "VEHICLE_TEAMKILLED", "{0} blew up a friendly {2}." },
                
                // Officers
                { "officer_promoted", "<color=#9e9788>Congratulations, you have been <color=#e3b552>PROMOTED</color> to <color=#e05353>{0}</color> of <color=#baccca>{1}</color>!</color>" },
                { "officer_demoted", "<color=#9e9788>You have been <color=#c47f5c>DEMOTED</color> to <color=#e05353>{0}</color> of <color=#baccca>{1}</color>.</color>" },
                { "officer_discharged", "<color=#9e9788>You have been <color=#ab2e2e>DISCHARGED</color> from the officer ranks for unacceptable behaviour.</color>" },
                { "officer_announce_promoted", "<color=#9e9788><color=#c4daff>{0}</color> has been <color=#e3b552>PROMOTED</color> to <color=#e05353>{1}</color> of <color=#baccca>{2}</color>!</color>" },
                { "officer_announce_demoted", "<color=#9e9788><color=#c4daff>{0}</color> has been <color=#c47f5c>DEMOTED</color> to <color=#e05353>{1}</color> of <color=#baccca>{2}</color>.</color>" },
                { "officer_announce_discharged", "<color=#9e9788><color=#c4daff>{0}</color> has been <color=#ab2e2e>DISCHARGED</color> from the rank of <color=#e05353>{1}s</color> for unacceptable behaviour.</color>" },
                { "officer_ui_no_stars", "no medals" },
                { "officer_ui_stars", "{0} medal{1}" },
                { "officer_e_playernotfound", "<color=#b08989>'{0}' is not a valid online player or Steam64 ID.</color>" },
                { "officer_e_invalidrank", "<color=#b08989>'{0}' is not a valid officer level. Try numbers 1 - 5.</color>" },
                { "officer_e_invalidteam", "<color=#b08989>'{0}' is not a valid team. Try either '1' or '2'.</color>" },
                { "officer_s_changedrank", "<color=#c6d6c1>{0}'s officer rank was successfully changed to {1} of {2}.</color>" },
                { "officer_s_discharged", "<color=#c6d6c1>{0} was successfully discharged.</color>" },

                // promotions

                { "xp_announce_promoted", "<color=#9e9788><color=#a1c4ff>{0}</color> was <color=#e3b552>promoted</color> to <color=#ffffff>{1}</color>." },
                { "xp_announce_demoted", "<color=#9e9788><color=#a1c4ff>{0}</color> was <color=#c47f5c>demoted</color> to <color=#e05353>{1}</color>.</color>" },
                { "ofp_announce_gained", "<color=#9e9788><color=#a1c4ff>{0}</color> has been awarded <color=#ffc569>{1}</color> for good leadership.</color>" },


                // Clear
                { "clear_not_enough_args", "<color=#ff8c69>The clear command requires 1 argument.</color>" },
                { "clear_inventory_console_identity", "Specify a player name when clearing from console." }, // runs from console only, no color needed.
                { "clear_inventory_player_not_found", "<color=#ff8c69>A player was not found from <color=#8ce4ff>\"{0}\"</color>.</color>" },
                { "clear_inventory_self", "<color=#e6e3d5>Cleared your inventory.</color>" },
                { "clear_inventory_others", "<color=#e6e3d5>Cleared <color=#8ce4ff>{0}</color>'s inventory.</color>" },
                { "clear_items_cleared", "<color=#e6e3d5>Cleared all dropped items.</color>" },
                { "clear_structures_cleared", "<color=#e6e3d5>Cleared all placed structures and barricades.</color>" },
                { "clear_vehicles_cleared", "<color=#e6e3d5>Cleared all vehicles.</color>" },

                // Deaths
                { "no_murderer_name", "Unapplicable" },
                { "zombie", "a zombie" },

                // Shutdown
                { "shutdown_not_server", "<color=#9cffb3>This is not a server.</color>" },
                { "shutdown_syntax", "<color=#9cffb3>Corrent syntax: /shutdown <aftergame|*seconds*|instant> <reason>.</color>" },
                { "shutdown_broadcast_after_game", "<color=#00ffff>A shutdown has been scheduled after this game because: \"<color=#6699ff>{0}</color>\".</color>" },
                { "shutdown_broadcast_after_game_canceled", "<color=#00ffff>The scheduled shutdown has been canceled.</color>" },
                { "shutdown_broadcast_after_game_canceled_console", "The scheduled shutdown was canceled." },
                { "shutdown_broadcast_after_game_canceled_console_player", "The scheduled shutdown was canceled by {0}." },
                { "shutdown_broadcast_after_time", "<color=#00ffff>A shutdown has been scheduled in {0} because: \"<color=#6699ff>{1}</color>\".</color>" },
                { "shutdown_broadcast_after_game_console", "A shutdown has been scheduled after this game because: \"{0}\"." },
                { "shutdown_broadcast_after_game_reminder", "<color=#00ffff>A shutdown is scheduled to occur after this game because: \"<color=#6699ff>{0}</color>\".</color>" },
                { "shutdown_broadcast_after_game_console_player", "A shutdown has been scheduled after this game by {0} because: \"{1}\"." },
                { "shutdown_broadcast_after_time_console", "A shutdown has been scheduled in {0} because: \"{1}\"." },
                { "shutdown_broadcast_after_time_console_player", "A shutdown has been scheduled in {0} by {1} because: \"{2}\"." },

                // Tickets
                { "enemy_controlling", "The enemy is in control!" },
                { "enemy_dominating", "The enemy is dominating!" },
                { "defeated", "You are defeated!" },
                { "victorious", "You are victorious!" },
                { "controlling", "Your team is in control!" },
                { "dominating", "Your team is dominating!" },

                // Branch names
                { "team1_infantry", "Infantry Division" },
                { "team2_infantry", "Infantry Division" },
                { "team1_armor", "Armor Division" },
                { "team2_armor", "Armor Division" },
                { "team1_airforce", "Airforce" },
                { "team2_airforce", "Airforce" },
                { "team1_specops", "Navy Seals" },
                { "team2_specops", "Spetsnaz" },

                // Kit Signs
                { "kit_name", "<b>{0}</b>" },
                { "kit_weapons", "<b>{0}</b>" },
                { "kit_price_tickets", "{0} Tickets" },
                { "kit_price_dollars", "$ {0:N2}" },
                { "kit_price_exclusive", "EXCLUSIVE" },
                { "kit_required_level", "<color=#{3}>{2}</color> <color=#{1}>lvl {0}</color>" }, // {0} = level number
                { "kit_owned", "OWNED" },
                { "kit_unlimited", "unlimited" },
                { "kit_not_owned", "NOT OWNED" },
                { "kit_player_count", "{0}/{1}" },
                { "sign_kit_request", "{0}\n{1}\n{2}\n{3}" },
                { "loadout_name", "LOADOUT {0}\n" },
                { "loadout_name_owned", "" },

                // Vehicle bay signs
                { "vbs_tickets_postfix", "Tickets" },
                { "vbs_state_ready", "Ready!  <b>/request</b>" },
                { "vbs_state_dead", "{0}:{1}" },
                { "vbs_state_active", "{0}" },
                { "vbs_state_idle", "Idle: {0}:{1}" },
                { "vbs_level_prefix", "Lvl" },
                { "vbs_branch_default", "" },
                { "vbs_branch_infantry", "INFANTRY DIVISION" },
                { "vbs_branch_armor", "ARMOR DIVISION" },
                { "vbs_branch_airforce", "AIRFORCE DIVISION" },
                { "vbs_branch_specops", "SPEC-OPS DIVISION" },
                { "vbs_branch_navy", "NAVAL DIVISION" },


                // revives
                { "heal_e_notmedic", "<color=#bdae9d>Only a <color=#ff758f>MEDIC</color> can heal or revive teammates.</color>" },
                { "heal_e_enemy", "<color=#bdae9d>You cannot aid enemy soldiers.</color>" },

                // reloads
                { "reload_reloaded_all", "<color=#e6e3d5>Reloaded all Uncreated Warfare components.</color>" },
                { "reload_reloaded_config", "<color=#e6e3d5>Reloaded all the config files.</color>" },
                { "reload_reloaded_lang", "<color=#e6e3d5>Reloaded all translations.</color>" },
                { "reload_reloaded_flags", "<color=#e6e3d5>Re-read flags from file and begain to start a new game.</color>" },
                { "reload_reloaded_tcp", "<color=#e6e3d5>Tried to close any existing TCP connection to UCDiscord and re-open it.</color>" },
                { "reload_reloaded_kits", "<color=#e6e3d5>Reloaded all kits and request signs.</color>" },
                { "reload_reloaded_slots", "<color=#e6e3d5>Reset the slots plugin to max.</color>" },
                { "reload_reloaded_slots_not_enabled", "<color=#e6e3d5>{0} must be enabled to use this command operation.</color>" },
                { "reload_reloaded_sql", "<color=#e6e3d5>Reopened the MySql Connection.</color>" },
                { "reload_reloaded_gameconfig", "<color=#e6e3d5>Reloaded Gamemode Config.</color>" },

                //test
                { "test_no_method", "<color=#ff8c69>No method found called <color=#ff758f>{0}</color>.</color>" },
                { "test_error_executing", "<color=#ff8c69>Ran into an error while executing: <color=#ff758f>{0} - {1}</color>.</color>" },
                { "test_multiple_matches", "<color=#ff8c69>Multiple methods match <color=#ff758f>{0}</color>.</color>" },
                { "test_no_players_console", "No player found." },
                { "test_check_console", "<color=#e6e3d5>Check the console log.</color>" },

                { "test_zonearea_syntax", "<color=#ff8c69>Syntax: <i>/test zonearea [active|all] <show extra zones: true|false> <show path: true|false> <show range: true|false></i>.</color>" },
                { "test_zonearea_started", "<color=#e6e3d5>Picture has to generate, wait around a minute.</color>" },

                { "test_givexp_syntax", "<color=#ff8c69>Syntax: <i>/test givexp <name> <amount></i>.</color>" },
                { "test_givexp_player_not_found", "<color=#ff8c69>Could not find player named <color=#ff758f>{0}</color></color>" },
                { "test_givexp_success", "<color=#e6e3d5>Given {0} XP to {1}.</color>" },
                { "test_givexp_invalid_amount", "<color=#ff8c69><color=#ff758f>{0}</color> is not a valid amount (Int32).</color>" },

                { "test_giveof_syntax", "<color=#ff8c69>Syntax: <i>/test giveof <name> <amount></i>.</color>" },
                { "test_giveof_player_not_found", "<color=#ff8c69>Could not find player named <color=#ff758f>{0}</color></color>" },
                { "test_giveof_success", "<color=#e6e3d5>Given {0} Officer Point{1} to {2}.</color>" },
                { "test_giveof_invalid_amount", "<color=#ff8c69><color=#ff758f>{0}</color> is not a valid amount (Int32).</color>" },

                { "test_zone_not_in_zone", "<color=#e6e3d5>No flag zone found at position <color=#4785ff>({0}, {1}, {2})</color> - <color=#4785ff>{3}°</color>, out of <color=#4785ff>{4}</color> registered flags.</color>" },
                { "test_zone_current_zone", "<color=#e6e3d5>You are in flag zone: <color=#4785ff>{0}</color>, at position <color=#4785ff>({1}, {2}, {3})</color>.</color>" },

                { "test_visualize_syntax", "<color=#ff8c69>Syntax: <i>/test visualize [spacing] [perline]</i>. Specifying perline will disregard spacing.</color>" },
                { "test_visualize_success", "<color=#e6e3d5>Spawned {0} particles around zone <color=#{2}>{1}</color>. They will despawn in 1 minute.</color>" },

                { "test_go_syntax", "<color=#ff8c69>Syntax: <i>/test go <flag name|zone name|flag id|zone id|obj1|obj2|t1main|t2main></i>. Specifying perline will disregard spacing.</color>" },
                { "test_go_no_zone", "<color=#ff8c69>No zone or flag found from search terms: <color=#8ce4ff>{0}</color>.</color>" },
                { "test_go_success_zone", "<color=#e6e3d5>Teleported to extra zone <color=#8ce4ff>{0}</color>.</color>" },
                { "test_go_success_flag", "<color=#e6e3d5>Teleported to flag <color=#{1}>{0}</color>.</color>" },

                { "test_time_enabled", "<color=#e6e3d5><color=#8ce4ff>Enabled</color> coroutine timing.</color>" },
                { "test_time_disabled", "<color=#e6e3d5><color=#8ce4ff>Disabled</color> coroutine timing.</color>" },
                { "test_time_enabled_console", "Enabled coroutine timing." },
                { "test_time_disabled_console", "Disabled coroutine timing." },

                { "test_down_success", "<color=#e6e3d5>Applied <color=#8ce4ff>{0}</color> damage to player.</color>" },

                { "test_sign_no_sign", "<color=#ff8c69>No sign found.</color>" },
                { "test_sign_success", "<color=#e6e3d5>Sign text: <color=#8ce4ff>\"{0}\"</color>.</color>" },

                { "gamemode_not_flag_gamemode", "<color=#ff8c69>Current gamemode <color=#ff758f>{0}</color> is not a <color=#ff758f>FLAG GAMEMODE</color>.</color>" },
                { "gamemode_flag_not_on_cap_team", "<color=#ff8c69>You're not on a team that can capture flags.</color>" },
                { "gamemode_flag_not_on_cap_team_console", "That team can not capture flags." },
                { "phases_briefing", "BRIEFING PHASE" },
                { "phases_preparation", "PREPARATION PHASE" },

                // xp toast messages
                { "xp_built_emplacement", "BUILT EMPLACEMENT" },
                { "xp_built_fortification", "BUILT FORTIFICATION" },
                { "xp_built_fob", "BUILT FOB" },
                { "xp_built_ammo_crate", "BUILT AMMO CRATE" },
                { "xp_built_repair_station", "BUILT REPAIR STATION" },
                { "xp_from_operator", "FROM OPERATOR" },
                { "xp_from_player", "FROM {0}" },
                { "xp_healed_teammate", "HEALED {0}" },
                { "xp_enemy_downed", "DOWNED ENEMY" },
                { "xp_friendly_downed", "DOWNED FRIENDLY" },
                { "xp_enemy_killed", "KILLED ENEMY" },
                { "xp_kill_assist", "ASSIST" },
                { "xp_driver_assist", "DRIVER ASSIST" },
                { "xp_friendly_killed", "TEAMKILLED {0}" },
                { "xp_fob_killed", "FOB DESTROYED" },
                { "xp_fob_teamkilled", "FRIENDLY FOB DESTROYED" },
                { "xp_fob_in_use", "FOB IN USE" },
                { "xp_resupplied_teammate", "RESUPPLIED TEAMMATE" },

                { "xp_victory", "VICTORY" },
                { "xp_handicap", "HARD FOUGHT" },
                { "xp_flag_captured", "FLAG CAPTURED" },
                { "xp_flag_neutralized", "FLAG NEUTRALIZED" },
                { "xp_flag_attack", "ATTACK" },
                { "xp_flag_defend", "DEFENCE" },
                { "xp_cache_killed", "CACHE DESTROYED" },
                { "xp_cache_teamkilled", "FRIENDLY CACHE DESTROYED" },

                { "xp_squad_bonus", "SQUAD BONUS" },
                { "xp_on_duty", "ON DUTY" },

                { "xp_humvee_destroyed", "HUMVEE DESTROYED" },
                { "xp_transport_destroyed", "TRANSPORT DESTROYED" },
                { "xp_logistics_destroyed", "LOGISTICS DESTROYED" },
                { "xp_apc_destroyed", "APC DESTROYED" },
                { "xp_ifv_destroyed", "IFV DESTROYED" },
                { "xp_tank_destroyed", "TANK DESTROYED" },
                { "xp_transheli_destroyed", "HELI SHOT DOWN" },
                { "xp_attackheli_destroyed", "ATTACK HELI SHOT DOWN" },
                { "xp_jet_destroyed", "JET SHOT DOWN" },
                { "xp_emplacement_destroyed", "EMPLACEMENT DESTROYED" },

                { "xp_friendly_humvee_destroyed", "FRIENDLY HUMVEE DESTROYED" },
                { "xp_friendly_transport_destroyed", "FRIENDLY TRANSPORT DESTROYED" },
                { "xp_friendly_logistics_destroyed", "FRIENDLY LOGISTICS DESTROYED" },
                { "xp_friendly_apc_destroyed", "FRIENDLY APC DESTROYED" },
                { "xp_friendly_ifv_destroyed", "FRIENDLY IFV DESTROYED" },
                { "xp_friendly_tank_destroyed", "FRIENDLY TANK DESTROYED" },
                { "xp_friendly_helicopter_destroyed", "FRIENDLY HELICOPTER DESTROYED" },
                { "xp_friendly_emplacement_destroyed", "FRIENDLY EMPLACEMENT DESTROYED" },

                { "xp_transporting_players", "TRANSPORTING PLAYERS" },

                { "gain_xp", "+{0} XP" },
                { "loss_xp", "-{0} XP" },
                { "level_up_xp_1", "YOU HAVE REACHED" },
                { "level_up_xp_2", "{1} - <color=#f7b88b>Level {2}</color>" },
                { "level_down_xp", "<color=#e08675>LEVEL LOST</color>" },
                { "promoted_xp", "YOU HAVE BEEN <color=#ffbd8a>PROMOTED</color> TO" },
                { "demoted_xp", "YOU HAVE BEEN <color=#e86868>DEMOTED</color> TO" },

                { "branch_changed", "<color=#ffffff>{0}</color>\n<color=#9e9e9e>{1} - <color=#f7b88b>lvl {2}</color></color>" },
                { "branch_changed_recruit", "<color=#ffffff>{0}</color>\n<color=#9e9e9e>{1}</color>" },

                { "ui_xp_level", "lvl {0}" },
                { "ui_xp_next_level", "lvl {0}" },

                // officer point toast messages
                { "ofp_squad_built_emplacement", "SQUAD BUILT EMPLACEMENT" },
                { "ofp_squad_built_fortification", "SQUAD BUILT FORTIFICATION" },
                { "ofp_squad_built_fob", "SQUAD BUILT FOB" },
                { "ofp_squad_built_ammo_crate", "SQUAD BUILT AMMO CRATE" },
                { "ofp_squad_built_repair_station", "SQUAD BUILT REPAIR STATION" },
                { "ofp_from_operator", "FROM OPERATOR" },
                { "ofp_from_player", "FROM {0}" },
                { "ofp_vehicle_eliminated", "VEHICLE ELIMINATED" },
                { "ofp_deployed_fob", "SQUAD DEPLOYED" },

                { "ofp_squad_victory", "VICTORY" },
                { "ofp_squad_flag_captured", "SQUAD CAPTURED FLAG" },
                { "ofp_squad_flag_neutralized", "SQUAD NEUTRALIZED FLAG" },

                { "ofp_transporting_players", "TRANSPORTING PLAYERS" },

                { "ofp_rally_used", "RALLY USED" },

                { "ofp_supplies_unloaded", "RESUPPLIED FOB" },

                { "gain_ofp", "+{0} TW" },
                { "loss_ofp", "-{0} TW" },
                { "gain_star", "FOR GOOD TEAMWORK, YOU HAVE BEEN AWARDED" },

                { "injured_ui_header", "You are injured" },
                { "injured_ui_give_up", "Press <b>'/'</b> to give up.\n " },
                { "injured_chat", "<color=#ff8c69>You were injured, press <color=#cedcde><plugin_2/></color> to give up.</color>" },

                // Insurgency

                { "insurgency_ui_unknown_attack", "<color=#696969>Undiscovered</color>" },
                { "insurgency_ui_unknown_defense", "<color=#696969>Unknown</color>" },
                { "insurgency_ui_destroyed_attack", "<color=#5a6e5c>Destroyed</color>" },
                { "insurgency_ui_destroyed_defense", "<color=#6b5858>Lost</color>" },
                { "insurgency_ui_cache_attack", "<color=#ffca61>{0}</color> <color=#c2c2c2>{1}</color>" },
                { "insurgency_ui_cache_defense_undiscovered", "<color=#84d980>{0}</color> <color=#c2c2c2>{1}</color>" },
                { "insurgency_ui_cache_defense_discovered", "<color=#c480d9>{0}</color> <color=#c2c2c2>{1}</color>" },
                { "caches_header", "Caches" },

                // Report Command
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
                { "report_popup", "You've been reported for {0}. More info available here (Discord must be installed):" },
                { "report_console", "{0} ({1}) reported {2} ({3}) for \"{4}\" as a {5} report." },
                { "report_console_record", "Report against {0} ({1}) record: \"{2}\"" },
                { "report_console_record_failed", "Report against {0} ({1}) failed to send to UCDB." },
            };
        }
        public static Dictionary<string, string> DefaultTranslations;
        public static readonly List<FlagData> DefaultFlags = new List<FlagData>
        {
            new FlagData(1, "AmmoHill", -89, 297, new ZoneData("rectangle", "86,68"), true, -1, -1),
            new FlagData(2, "Hilltop", 258, 497, new ZoneData("rectangle", "66,72"), true, -1, -1),
            new FlagData(3, "Papanov", 754, 759, new ZoneData("polygon", "635,738,713,873,873,780,796,645"), true, -1, -1),
            new FlagData(4, "Verto", 624, 469, new ZoneData("polygon", "500,446,514,527,710,492,748,466,710,411"), true, -1, -1),
            new FlagData(5, "Hill123", 631, 139, new ZoneData("rectangle", "44,86"), true, -1, -1),
            new FlagData(6, "Hill13", 338, -15, new ZoneData("circle", "35"), true, -1, -1),
            new FlagData(7, "Mining", 52.5f, -215, new ZoneData("polygon", "7,-283,-6,-270,-6,-160,7,-147,72,-147,111,-160,111,-257,104,-264,40,-283"), true, -1, -1),
            new FlagData(8, "Fortress", -648.5f, 102.5f, new ZoneData("rectangle", "79,47"), true, -1, -1)
        };
        public static List<FlagData> DefaultExtraZones = new List<FlagData>
        {
            new FlagData(-69, "lobby", 713.1f, -991, new ZoneData("rectangle", "12.2,12"), false, -1, -1),
            new FlagData(1, "T1Main", 823, -880.5f, new ZoneData("rectangle", "120,189"), true, -1, -1),
            new FlagData(101, "T1AMC", 717.5f, -697.5f, new ZoneData("rectangle", "613,653"), true, -1, -1),
            new FlagData(2, "T2Main", -823, 876.5f, new ZoneData("rectangle", "120,189"), true, -1, -1),
            new FlagData(102, "T2AMC", -799, 744.5f, new ZoneData("rectangle", "450,559"), true, -1, -1),
        };
        public static List<Point3D> DefaultExtraPoints = new List<Point3D>
        {
            new Point3D("lobby_spawn", 713.1f, 39f, -991)
        };
        public static readonly Dictionary<string, string> DefaultColors = new Dictionary<string, string>()
        {
            { "default", "ffffff" },
            { "defaulterror", "ff8c69" },
            { "uncreated", "9cb6a4" },
            { "attack_icon_color", "ffca61" },
            { "defend_icon_color", "ba70cc" },
            { "locked_icon_color", "c2c2c2" },
            { "undiscovered_flag", "696969" },
            { "team_count_ui_color_team_1", "ffffff" },
            { "team_count_ui_color_team_2", "ffffff" },
            { "team_count_ui_color_team_1_icon", "4785ff" },
            { "team_count_ui_color_team_2_icon", "f53b3b" },
            { "default_fob_color", "54e3ff" },
            { "no_bunker_fob_color", "696969" },
            { "enemy_nearby_fob_color", "ff8754" },
            { "bleeding_fob_color", "d45555" },
            { "invasion_special_fob", "5482ff" },
            { "insurgency_cache_undiscovered_color", "b780d9" },
            { "insurgency_cache_discovered_color", "696ed1" },

            // Team Colors
            { "team_1_color", "4785ff" },
            { "team_2_color", "f53b3b" },
            { "team_3_color", "0099ff" },
            { "neutral_color", "c2c2c2" },

            // Team 1 Circle
            { "capturing_team_1", "4785ff" },
            { "losing_team_1", "f53b3b" },
            { "clearing_team_1", "4785ff" },
            { "contested_team_1", "ffff1a" },
            { "secured_team_1", "00ff00" },
            { "nocap_team_1", "ff8c69" },
            { "notowned_team_1", "ff8c69" },
            { "locked_team_1", "ff8c69" },
            { "in_vehicle_team_1", "ff8c69" },

            // Team 1 Background Circle
            { "capturing_team_1_bkgr", "002266" },
            { "losing_team_1_bkgr", "610505" },
            { "clearing_team_1_bkgr", "002266" },
            { "contested_team_1_bkgr", "666600" },
            { "secured_team_1_bkgr", "006600" },
            { "nocap_team_1_bkgr", "660000" },
            { "notowned_team_1_bkgr", "660000" },
            { "locked_team_1_bkgr", "660000" },
            { "in_vehicle_team_1_bkgr", "660000" },

            // Team 1 Words
            { "capturing_team_1_words", "4785ff" },
            { "losing_team_1_words", "f53b3b" },
            { "clearing_team_1_words", "4785ff" },
            { "contested_team_1_words", "ffff1a" },
            { "secured_team_1_words", "00ff00" },
            { "nocap_team_1_words", "ff8c69" },
            { "notowned_team_1_words", "ff8c69" },
            { "locked_team_1_words", "ff8c69" },
            { "in_vehicle_team_1_words", "ff8c69" },

            // Team 2 Circle
            { "capturing_team_2", "f53b3b" },
            { "losing_team_2", "4785ff" },
            { "clearing_team_2", "f53b3b" },
            { "contested_team_2", "ffff1a" },
            { "secured_team_2", "00ff00" },
            { "nocap_team_2", "ff8c69" },
            { "notowned_team_2", "ff8c69" },
            { "locked_team_2", "ff8c69" },
            { "in_vehicle_team_2", "ff8c69" },

            // Team 2 Background Circle
            { "capturing_team_2_bkgr", "610505" },
            { "losing_team_2_bkgr", "002266" },
            { "clearing_team_2_bkgr", "610505" },
            { "contested_team_2_bkgr", "666600" },
            { "secured_team_2_bkgr", "006600" },
            { "nocap_team_2_bkgr", "660000" },
            { "notowned_team_2_bkgr", "660000" },
            { "locked_team_2_bkgr", "660000" },
            { "in_vehicle_team_2_bkgr", "660000" },

            // Team 2 Words
            { "capturing_team_2_words", "f53b3b" },
            { "losing_team_2_words", "4785ff" },
            { "clearing_team_2_words", "f53b3b" },
            { "contested_team_2_words", "ffff1a" },
            { "secured_team_2_words", "00ff00" },
            { "nocap_team_2_words", "ff8c69" },
            { "notowned_team_2_words", "ff8c69" },
            { "locked_team_2_words", "ff8c69" },
            { "in_vehicle_team_2_words", "ff8c69" },

            // Flag Chats
            { "entered_cap_radius_team_1", "e6e3d5" },
            { "entered_cap_radius_team_2", "e6e3d5" },
            { "left_cap_radius_team_1", "e6e3d5" },
            { "left_cap_radius_team_2", "e6e3d5" },

            // Team 1 Chat
            { "capturing_team_1_chat", "e6e3d5" },
            { "losing_team_1_chat", "e6e3d5" },
            { "clearing_team_1_chat", "e6e3d5" },
            { "contested_team_1_chat", "e6e3d5" },
            { "secured_team_1_chat", "e6e3d5" },
            { "nocap_team_1_chat", "e6e3d5" },
            { "notowned_team_1_chat", "e6e3d5" },
            { "locked_team_1_chat", "e6e3d5" },
            { "in_vehicle_team_1_chat", "e6e3d5" },

            // Team 2 Chat
            { "capturing_team_2_chat", "e6e3d5" },
            { "losing_team_2_chat", "e6e3d5" },
            { "clearing_team_2_chat", "e6e3d5" },
            { "contested_team_2_chat", "e6e3d5" },
            { "secured_team_2_chat", "e6e3d5" },
            { "nocap_team_2_chat", "e6e3d5" },
            { "notowned_team_2_chat", "e6e3d5" },
            { "locked_team_2_chat", "e6e3d5" },
            { "in_vehicle_team_2_chat", "e6e3d5" },

            // Other Flag Chats
            { "flag_neutralized", "e6e3d5" },
            { "team_win", "e6e3d5" },
            { "team_capture", "e6e3d5" },

            // Deaths
            { "death_background", "ffffff" },
            { "death_background_teamkill", "ff9999" },
            { "death_zombie_name_color", "788c5a" },

            // Request
            { "kit_public_header", "ffffff" },
            { "kit_level_available", "ff974d" },
            { "kit_level_available_abbr", "999999" },
            { "kit_level_unavailable", "ad9380" },
            { "kit_level_unavailable_abbr", "999999" },
            { "kit_level_dollars", "7878ff" },
            { "kit_level_dollars_owned", "769fb5" },
            { "kit_level_dollars_exclusive", "96ffb2" },
            { "kit_weapon_list", "343434" },
            { "kit_unlimited_players", "111111" },
            { "kit_player_counts_available", "96ffb2" },
            { "kit_player_counts_unavailable", "c2603e" },

            // Vehicle Sign
            { "vbs_level_low_enough", "ff974d" },
            { "vbs_level_too_high", "222222" },
            { "vbs_name", "ccffff" },
            { "vbs_branch", "e6ffff" },
            { "vbs_ticket_number", "ffffff" },
            { "vbs_ticket_label", "f0f0f0" },
            { "vbs_dead", "ff0000" },
            { "vbs_idle", "ffcc00" },
            { "vbs_active", "ff9933" },
            { "vbs_ready", "33cc33" },

            // stars
            { "no_stars", "737373" },
            { "star_color", "ffd683" },
        };
        public static List<Kit> DefaultKits = new List<Kit>
        {
            KitEx.Construct("default",
                new List<KitItem> { },
                new List<KitClothing> {
                new KitClothing(184, 100, "", EClothingType.SHIRT),
                new KitClothing(2, 100, "", EClothingType.PANTS),
                new KitClothing(185, 100, "", EClothingType.MASK)
            }, (k) =>
            {
                k.ShouldClearInventory = true;
                k.UnlockLevel = 0;
                k.UnlockBranch = EBranch.DEFAULT;
                k.Cost = 0;
                k.Team = 0;
                k.Class = EClass.UNARMED;
                k.Branch = EBranch.DEFAULT;
                k.SignTexts = new Dictionary<string, string> {
                    { DefaultLanguage, "Default Kit" },
                    { "ru-ru", "Комплект по умолчанию" }
                };
            }),
            KitEx.Construct("usunarmed",
                new List<KitItem> { },
                new List<KitClothing> {
                new KitClothing(30710, 100, "", EClothingType.SHIRT),
                new KitClothing(30711, 100, "", EClothingType.PANTS),
                new KitClothing(30715, 100, "", EClothingType.HAT),
                new KitClothing(30718, 100, "", EClothingType.BACKPACK),
                new KitClothing(31251, 100, "", EClothingType.GLASSES)
            }, (k) =>
            {
                k.ShouldClearInventory = true;
                k.UnlockLevel = 0;
                k.UnlockBranch = EBranch.DEFAULT;
                k.Cost = 0;
                k.Team = 1;
                k.Class = EClass.UNARMED;
                k.Branch = EBranch.DEFAULT;
                k.SignTexts = new Dictionary<string, string> {
                    { DefaultLanguage, "Unarmed" },
                    { "ru-ru", "Безоружный" }
                };
            }),
            KitEx.Construct("ruunarmed",
                new List<KitItem> { },
                new List<KitClothing> {
                new KitClothing(30700, 100, "", EClothingType.SHIRT),
                new KitClothing(30701, 100, "", EClothingType.PANTS),
                new KitClothing(31123, 100, "", EClothingType.VEST),
                new KitClothing(30704, 100, "", EClothingType.HAT),
                new KitClothing(434, 100, "", EClothingType.MASK),
                new KitClothing(31156, 100, "", EClothingType.BACKPACK)
            }, (k) =>
            {
                k.ShouldClearInventory = true;
                k.UnlockLevel = 0;
                k.UnlockBranch = EBranch.DEFAULT;
                k.Cost = 0;
                k.Team = 2;
                k.Class = EClass.UNARMED;
                k.Branch = EBranch.DEFAULT;
                k.SignTexts = new Dictionary<string, string> {
                    { DefaultLanguage, "Unarmed" } ,
                    { "ru-ru", "Безоружный" }
                };
            }),
            KitEx.Construct("usrif1",
                new List<KitItem> {
                new KitItem(81, 0, 0, 0, 100, "", 1, 3),
                new KitItem(394, 0, 2, 0, 100, "", 1, 2),
                new KitItem(394, 1, 2, 0, 100, "", 1, 2),
                new KitItem(394, 2, 2, 0, 100, "", 1, 2),
                new KitItem(1176, 1, 0, 0, 100, "", 1, 3),
                new KitItem(31343, 0, 0, 0, 100, "", 30, 2),
                new KitItem(31343, 1, 0, 0, 100, "", 30, 2),
                new KitItem(31343, 2, 0, 0, 100, "", 30, 2),
                new KitItem(31343, 3, 0, 0, 100, "", 30, 2),
                new KitItem(31343, 4, 0, 0, 100, "", 30, 2),
                new KitItem(31475, 2, 0, 0, 100, "", 30, 3),
                new KitItem(31477, 3, 2, 0, 100, "", 30, 2),
                new KitItem(32326, 0, 0, 0, 100, "6HoAAO56AABveh4BAWRkZGRk", 1, 0)
            },
                new List<KitClothing> {
                new KitClothing(30710, 100, "", EClothingType.SHIRT),
                new KitClothing(30711, 100, "", EClothingType.PANTS),
                new KitClothing(30715, 100, "", EClothingType.HAT),
                new KitClothing(30718, 100, "", EClothingType.BACKPACK),
                new KitClothing(31251, 100, "", EClothingType.GLASSES)
            }, (k) =>
            {
                k.ShouldClearInventory = true;
                k.UnlockLevel = 0;
                k.UnlockBranch = EBranch.INFANTRY;
                k.Cost = 0;
                k.Team = 1;
                k.Class = EClass.AUTOMATIC_RIFLEMAN;
                k.Branch = EBranch.INFANTRY;
                k.SignTexts = new Dictionary<string, string> {
                    { DefaultLanguage, "Rifleman 1" },
                    { "ru-ru", "Стрелок 1" }
                };
            }),
            KitEx.Construct("rurif1",
                new List<KitItem> {
                new KitItem(81, 0, 0, 0, 100, "", 1, 3),
                new KitItem(394, 0, 2, 0, 100, "", 1, 2),
                new KitItem(394, 1, 2, 0, 100, "", 1, 2),
                new KitItem(394, 2, 2, 0, 100, "", 1, 2),
                new KitItem(1176, 1, 0, 0, 100, "", 1, 3),
                new KitItem(31413, 0, 0, 0, 100, "", 30, 2),
                new KitItem(31413, 1, 0, 0, 100, "", 30, 2),
                new KitItem(31413, 2, 0, 0, 100, "", 30, 2),
                new KitItem(31413, 3, 0, 0, 100, "", 30, 2),
                new KitItem(31413, 4, 0, 0, 100, "", 30, 2),
                new KitItem(31438, 1, 1, 0, 100, "", 8, 3),
                new KitItem(31438, 2, 1, 0, 100, "", 8, 3),
                new KitItem(31438, 3, 1, 0, 100, "", 8, 3),
                new KitItem(31475, 2, 0, 0, 100, "", 1, 3),
                new KitItem(31477, 3, 2, 0, 100, "", 1, 3),
                new KitItem(31477, 3, 2, 0, 100, "", 1, 3),
                new KitItem(31412, 0, 0, 0, 100, "4HsAAAAAAAC1eh4CAWRkZGRk", 1, 0),
                new KitItem(31437, 0, 0, 0, 100, "AAAAAAAAAADOeggBAWRkZGRk", 1, 1)
            },
                new List<KitClothing> {
                new KitClothing(30700, 100, "", EClothingType.SHIRT),
                new KitClothing(30701, 100, "", EClothingType.PANTS),
                new KitClothing(31123, 100, "", EClothingType.VEST),
                new KitClothing(30704, 100, "", EClothingType.HAT),
                new KitClothing(434, 100, "", EClothingType.MASK),
                new KitClothing(31156, 100, "", EClothingType.BACKPACK)
            }, (k) =>
            {
                k.ShouldClearInventory = true;
                k.UnlockLevel = 0;
                k.UnlockBranch = EBranch.INFANTRY;
                k.Cost = 0;
                k.Team = 2;
                k.Class = EClass.AUTOMATIC_RIFLEMAN;
                k.Branch = EBranch.INFANTRY;
                k.SignTexts = new Dictionary<string, string> {
                    { DefaultLanguage, "Rifleman 1" },
                    { "ru-ru", "Стрелок 1" }
                };
            }),
            KitEx.Construct("africa1",
                new List<KitItem> {
                new KitItem(81, 3, 0, 0, 100, "", 1, 3),
                new KitItem(333, 6, 0, 0, 100, "", 1, 3),
                new KitItem(394, 2, 2, 0, 100, "", 1, 2),
                new KitItem(394, 1, 2, 0, 100, "", 1, 2),
                new KitItem(394, 0, 2, 0, 100, "", 1, 2),
                new KitItem(1176, 5, 0, 0, 100, "", 1, 3),
                new KitItem(30505, 2, 0, 0, 100, "", 1, 3),
                new KitItem(30511, 0, 0, 0, 100, "", 1, 3),
                new KitItem(30511, 1, 0, 0, 100, "", 1, 3),
                new KitItem(31312, 0, 0, 0, 100, "", 20, 2),
                new KitItem(31312, 1, 0, 0, 100, "", 20, 2),
                new KitItem(31312, 2, 0, 0, 100, "", 20, 2),
                new KitItem(31312, 3, 0, 0, 100, "", 20, 2),
                new KitItem(31322, 0, 0, 0, 100, "TH4AAAAAAABQehQBAWRkZGRk", 1, 0),
                new KitItem(31479, 3, 2, 0, 100, "", 1, 2),
                new KitItem(31481, 4, 2, 0, 100, "", 1, 2),
                new KitItem(31487, 0, 0, 0, 100, "AAAAAAAAAAABexEBAWRkZGRk", 1, 1),
                new KitItem(31489, 4, 0, 0, 100, "", 17, 3),
                new KitItem(31489, 3, 0, 0, 100, "", 17, 3),
                new KitItem(38310, 0, 2, 0, 100, "", 1, 3),
                new KitItem(38333, 6, 1, 0, 100, "vpW/lQAAAAAAAAABAWRkZGRk", 1, 3)
            },
                new List<KitClothing> {
                new KitClothing(30960, 100, "", EClothingType.SHIRT),
                new KitClothing(30961, 100, "", EClothingType.PANTS),
                new KitClothing(30962, 100, "", EClothingType.VEST),
                new KitClothing(30965, 100, "", EClothingType.HAT),
                new KitClothing(31221, 100, "", EClothingType.MASK),
                new KitClothing(30970, 100, "", EClothingType.BACKPACK)
            }, (k) =>
            {
                k.ShouldClearInventory = true;
                k.UnlockLevel = 0;
                k.UnlockBranch = EBranch.INFANTRY;
                k.Cost = 0;
                k.IsPremium = true;
                k.PremiumCost = 6.00f;
                k.Team = 2;
                k.Class = EClass.AUTOMATIC_RIFLEMAN;
                k.Branch = EBranch.INFANTRY;
                k.SignTexts = new Dictionary<string, string> {
                    { DefaultLanguage, "Africa 1" },
                    { "ru-ru", "Африка 1" }
                };
            })
        };
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
        public static readonly Dictionary<string, string> DefaultDeathTranslations = new Dictionary<string, string> {
            { "ACID", "{0} was burned by an acid zombie." },
            { "ANIMAL", "{0} was attacked by an animal." },
            { "ARENA", "{0} stepped outside the arena boundary." },
            { "BLEEDING", "{0} bled out from {1}." },
            { "BLEEDING_UNKNOWN", "{0} bled out." },
            { "BLEEDING_SUICIDE", "{0} bled out." },
            { "BONES", "{0} fell to their death." },
            { "BOULDER", "{0} was crushed by a mega zombie." },
            { "BREATH", "{0} asphyxiated." },
            { "BURNER", "{0} was burned by a mega zombie." },
            { "BURNING", "{0} burned to death." },
            { "CHARGE", "{1} blew up {0} with a demolition charge." },
            { "CHARGE_SUICIDE", "{0} was blown up by their own demolition charge." },
            { "FOOD", "{0} starved to death." },
            { "FREEZING", "{0} froze to death." },
            { "GRENADE", "{1} blew up {0} with a {3}." },
            { "GRENADE_SUICIDE", "{0} blew themselves up with their {3}." },
            { "GRENADE_SUICIDE_UNKNOWN", "{0} blew themselves up with a grenade." },
            { "GRENADE_UNKNOWN", "{1} blew up {0} with a grenade." },
            { "GUN", "{1} shot {0} in the {2} with a {3} from {4} away." },
            { "GUN_UNKNOWN", "{1} shot {0} in the {2} from {4} away." },
            { "GUN_SUICIDE_UNKNOWN", "{0} shot themselves in the {2}." },
            { "GUN_SUICIDE", "{0} shot themselves in the {2} with a {3}." },
            { "INFECTION", "{0} got infected." },
            { "KILL", "{0} was killed by an admin, {1}." },
            { "KILL_SUICIDE", "{0} killed themselves as an admin." },
            { "LANDMINE", "{1} blew up {0} with a {3}." },
            { "LANDMINE_SUICIDE", "{0} blew themselves up with their {3}." },
            { "LANDMINE_SUICIDE_UNKNOWN", "{0} blew themselves up with a landmine." },
            { "LANDMINE_UNKNOWN", "{1} got blown up by {0} with a landmine." },
            { "LANDMINE_TRIGGERED", "{1} blew up {0} with a {3} set off by {5}." },
            { "LANDMINE_SUICIDE_TRIGGERED", "{0} was blown up by their own {3} set off by {5}." },
            { "LANDMINE_SUICIDE_UNKNOWN_TRIGGERED", "{0} was blown up by their own landmine set off by {5}." },
            { "LANDMINE_UNKNOWN_TRIGGERED", "{1} blew up {0} with a landmine set off by {5}." },
            { "LANDMINE_UNKNOWNKILLER", "{0} was blown up by a {3}." },
            { "LANDMINE_UNKNOWN_UNKNOWNKILLER", "{0} was blown up by a landmine." },
            { "LANDMINE_TRIGGERED_UNKNOWNKILLER", "{0} was blown up by a {3} set off by {5}." },
            { "LANDMINE_UNKNOWN_TRIGGERED_UNKNOWNKILLER", "{0} was blown up by a landmine set off by {5}." },
            { "MELEE", "{1} struck {0} in the {2} with a {3}." },
            { "MELEE_UNKNOWN", "{1} struck {0} in the {2}." },
            { "MISSILE", "{1} blew up {0} with a {3} from {4} away." },
            { "MISSILE_UNKNOWN", "{1} blew up {0} with a missile from {4} away." },
            { "MISSILE_SUICIDE_UNKNOWN", "{0} blew themselves up." },
            { "MISSILE_SUICIDE", "{0} blew themselves up with a {3}." },
            { "PUNCH", "{0} domed {1}." },
            { "ROADKILL", "{0} was ran over by {1}." },
            { "SENTRY", "{0} was killed by a sentry." },
            { "SHRED", "{0} was shredded by barbed wire." },
            { "SPARK", "{0} was shocked by a mega zombie." },
            { "SPIT", "{0} was killed by a spitter zombie." },
            { "SPLASH", "{1} killed {0} with {3} fragmentation." },
            { "SPLASH_UNKNOWN", "{1} killed {0} with fragmentation." },
            { "SPLASH_SUICIDE_UNKNOWN", "{0} fragged themselves." },
            { "SPLASH_SUICIDE", "{0} fragged themselves with a {3}." },
            { "SUICIDE", "{0} committed suicide." },
            { "VEHICLE", "{1} killed {0} with a {3}." },
            { "VEHICLE_SUICIDE", "{0} blew themselves up with a {3}." },
            { "VEHICLE_SUICIDE_UNKNOWN", "{0} blew themselves up with a vehicle." },
            { "VEHICLE_UNKNOWN", "{0} was blown up by {1} with a vehicle." },
            { "VEHICLE_UNKNOWNKILLER", "{0} blown up by a {3}." },
            { "VEHICLE_UNKNOWN_UNKNOWNKILLER", "{0} was blown up by a vehicle." },
            { "WATER", "{0} dehydrated." },
            { "ZOMBIE", "{0} was killed by {1}." },
            { "MAINCAMP", "{0} tried to main-camp {1} from {4} away and died." },
            { "MAINDEATH", "{0} tried to enter enemy territory." },
            { "38328", "{0} was blown up by a mortar by {1} from {4} away." },  // 81mm mortar turret item id
            { "38328_SUICIDE", "{0} blew themselves up with a mortar." }        // 81mm mortar turret item id
        };
        public static readonly Dictionary<ELimb, string> DefaultLimbTranslations = new Dictionary<ELimb, string> {
            { ELimb.LEFT_ARM, "Left Arm" },
            { ELimb.LEFT_BACK, "Left Back" },
            { ELimb.LEFT_FOOT, "Left Foot" },
            { ELimb.LEFT_FRONT, "Left Front" },
            { ELimb.LEFT_HAND, "Left Hand" },
            { ELimb.LEFT_LEG, "Left Leg" },
            { ELimb.RIGHT_ARM, "Right Arm" },
            { ELimb.RIGHT_BACK, "Right Back" },
            { ELimb.RIGHT_FOOT, "Right Foot" },
            { ELimb.RIGHT_FRONT, "Right Front" },
            { ELimb.RIGHT_HAND, "Right Hand" },
            { ELimb.RIGHT_LEG, "Right Leg" },
            { ELimb.SKULL, "Head" },
            { ELimb.SPINE, "Spine" }
        };
        public const string DeathsTranslationDescription = "Translations | Key, space, value with unlimited spaces. " +
            "Formatting: Dead player name, Murderer name when applicable, Limb, Gun name when applicable, distance when applicable. /reload translations to reload";
        public const string DeathsLimbTranslationsDescription = "Translations | Key, space, value with unlimited spaces. " +
            "Must match SDG.Unturned.ELimb enumerator list <LEFT|RIGHT>_<ARM|LEG|BACK|FOOT|FRONT|HAND>, SPINE, SKULL. ex. LEFT_ARM, RIGHT_FOOT";
    }
}