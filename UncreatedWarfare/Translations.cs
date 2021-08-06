using Rocket.API.Collections;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Stats;
using FlagData = Uncreated.Warfare.Gamemodes.Flags.FlagData;
using Kit = Uncreated.Warfare.Kits.Kit;

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
                { "flag_neutralized", "<color=#{1}>{0}</color> has been neutralized!" },
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
                { "ui_in_vehicle", "IN VEHICLE" },
                { "team_win", "<color=#{1}>{0}</color> won the game!" },
                { "team_capture", "<color=#{1}>{0}</color> captured <color=#{3}>{2}</color>!" },
                { "player_connected", "<color=#e6e3d5><color=#ffff1a>{0}</color> joined the server!</color>" },
                { "player_disconnected", "<color=#e6e3d5><color=#ffff1a>{0}</color> left the server.</color>" },

                // group
                { "group_usage", "<color=#ff8c69>Syntax: <i>/group [ join [id] | create [name] ].</color>" },
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
                { "join_s", "<color=#d1c5b0>You have successfully joined {0}! Welcome to the battlefield.</color>" },
                { "join_announce", "<color=#d1c5b0>{0} joined {1}!</color>" },
                { "joined_standby", "<color=#d1c5b0>Joining team...</color>" },
                { "join_e_alreadyonteam", "<color=#a8a194>You are already on that team.</color>" },
                { "join_e_notinlobby", "<color=#a8a194>You must be in the <color=#fce5bb>lobby</color> to switch teams. Do <color=#fce5bb>/deploy lobby</color>.</color>" },
                { "join_correctusage", "<color=#a8a194>Do <color=#fce5bb>/join us</color> or <color=#fce5bb>/join ru</color> to join a team.</color>" },
                { "join_e_groupnoexist", "<color=#a8a194>That group is not set up correctly. Contact an admin to fix it.</color>" },
                { "join_e_teamfull", "<color=#a8a194>That group is full! Contact an admin to remove the server's group limit.</color>" },
                { "join_e_autobalance", "<color=#a8a194>That group currently has too many players. Please try again later.</color>" },
                { "join_e_badname", "<color=#a8a194>You have left your team. Please <b>rejoin</b> to confirm your team switch.</color>" },
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
                { "kit_deleted", "<color=#a0ad8e>Deleted kit: <color=#ffffff>{0}</color></color>" },
                { "kit_setprop", "<color=#a0ad8e>Set <color=#8ce4ff>{0}</color> for kit <color=#ffb89c>{1}</color> to: <color=#ffffff>{2}</color></color>" },
                { "kit_accessgiven", "<color=#a0ad8e>Allowed player: <color=#e06969>{0}</color> to access the kit: <color=#ffffff>{1}</color></color>" },
                { "kit_accessremoved", "<color=#a0ad8e>Disallowed player: <color=#e06969>{0}</color> to access the kit: <color=#ffffff>{1}</color></color>" },
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

                // Range
                { "range", "<color=#9e9c99>The range to your marker is: <color=#8aff9f>{0}m</color></color>" },
                { "range_nomarker", "<color=#9e9c99>Place a marker first.</color>" },
                { "range_notsquadleader", "<color=#9e9c99>You must be a <color=#cedcde>SQUAD LEADER</color>.</color>" },

                // Squads
                { "squad_created", "<color=#a0ad8e>You created the squad <color=#ffffff>{0}</color></color>" },
                { "squad_ui_reloaded", "<color=#a0ad8e>Squad UI has been reloaded.</color>" },
                { "squad_joined", "<color=#a0ad8e>You joined <color=#ffffff>{0}</color>.</color>" },
                { "squad_left", "<color=#a7a8a5>You left your squad.</color>" },
                { "squad_disbanded", "<color=#a7a8a5>Your squad was disbanded.</color>" },
                { "squad_locked", "<color=#a7a8a5>You <color=#6be888>locked</color> your squad.</color>" },
                { "squad_unlocked", "<color=#999e90>You <color=#ffffff>unlocked</color> your squad.</color>" },
                { "squad_promoted", "<color=#b9bdb3><color=#ffc94a>{0}</color> was promoted to squad leader.</color>" },
                { "squad_kicked", "<color=#b9bdb3><color=#a6705b>{0}</color> was kicked from the squad.</color>" },
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
                { "squad_player_kicked", "<color=#b9bdb3><color=#d68f81>{0}</color> was kicked from the squad.</color>" },
                { "squad_squadleader", "<color=#b9bdb3>You are now the <color=#ffc94a>squad leader</color>.</color>" },
                { "squad_no_no_words", "<color=#ff8c69>You can't name a squad that.</color>" },

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
                { "rally_ui", "<color=#5eff87>RALLY</color>{0}" },
                { "rally_time_value", " {0:mm\\:ss}" },

                { "time_second", "second" },
                { "time_seconds", "seconds" },
                { "time_minute", "minute" },
                { "time_minutes", "minute" },
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
                { "build_error_noteam", "<color=#ffab87>You must be looking at a friendly structure base in order to build it.</color>" },
                { "build_error_notfriendly", "<color=#ffab87>That FOB foundation is not friendly.</color>" },
                { "build_error_nofoundation", "<color=#ffab87>You must be looking at a friendly structure base in order to build it.</color>" },
                { "build_error_notenoughbuild", "<color=#fae69c>You are missing build! <color=#d1c597>Nearby Build: </color><color=#d1c597>{0}/{1}</color></color>" },
                { "build_error_fobtoofar", "<color=#fae69c>You must be next to a friendly FOB to build this structure.</color>" },
                { "build_error_tooclosetomain", "<color=#fae69c>You cannot build too close to main.</color>" },
                { "build_error_maxemplacements", "<color=#d1c597>This FOB already has {0} {1}s.</color>" },
                { "build_error_notbuildable", "<color=#d1c597>That barricade is not buildable.</color>" },
                { "build_error_too_many_fobs", "<color=#d1c597>There can not be more than 10 fobs at a time.</color>" },
                { "fob_nofobs", "<color=#b5a591>Your team has no active FOBs. Take a Logi Truck and go and build some!</color>" },
                { "fob_built", "<color=#b0ffa8>Successfully built FOB! Your team may now spawn on it.</color>" },
                { "fob_teleported", "<color=#fae69c>You have been deployed to <color=#54e3ff>{0}</color>.</color>" },
                { "fob_error_nologi", "<color=#ffab87>You need to be near a friendly logistics truck in order to build a FOB!</color>" },
                { "fob_error_fobtooclose", "<color=#ffa238>You are too close to an existing friendly fob! You need to be 300m away from it to construct a new fob.</color>" },
                { "fob_error_limitreached", "<color=#ffa238>The number of FOBs allowed on the map has been reached.</color>" },
                { "ammocrate_built", "<color=#b0ffa8>Successfully built ammo crate. Your team may now resupply from it.</color>" },
                { "ammocrate_error_alreadyexists", "<color=#ffa238>This FOB already has an ammo crate.</color>" },
                { "repairstation_built", "<color=#b0ffa8>Successfully built repair station. Your team may now repair damaged vehicles at this FOB.</color>" },
                { "repairstation_error_alreadyexists", "<color=#ffa238>This FOB already has a repair station.</color>" },
                { "emplacement_built", "<color=#b0ffa8>Successfully built {0}. Do /ammo on it to resupply.</color>" },
                { "fob_ui", "<color=#54e3ff>{0}</color>{1}" },

                // deployment
                { "deploy_s", "<color=#fae69c>You have arrived at <color=#bdbab1>{0}</color>.</color>" },
                { "deploy_c_fobdead", "<color=#ffa238>The FOB you were deploying to was detroyed!</color>" },
                { "deploy_c_moved", "<color=#ffa238>You moved and can no longer deploy!</color>" },
                { "deploy_c_damaged", "<color=#ffa238>You are now in combat and can no longer deploy!</color>" },
                { "deploy_c_dead", "<color=#ffa238>You died and can no longer deploy!</color>" },
                { "deploy_e_fobnotfound", "<color=#b5a591>There is no location or FOB by the name of '{0}'.</color>" },
                { "deploy_e_notnearfob", "<color=#b5a591>You must be on an active friendly FOB or at main in order to deploy again.</color>" },
                { "deploy_e_cooldown", "<color=#b5a591>You can deploy again in: <color=#e3c27f>{0}</color>.</color>" },
                { "deploy_e_alreadydeploying", "<color=#b5a591>You are already deploying somewhere.</color>" },
                { "deploy_e_incombat", "<color=#ffaa42>You are in combat, soldier! You can deploy in another: <color=#e3987f>{0}</color>.</color>" },
                { "deploy_standby", "<color=#fae69c>Now deploying to <color=#54e3ff>{0}</color>. You will arrive in <color=#eeeeee>{1} seconds</color>.</color>" },
                { "deploy_standby_nomove", "<color=#fae69c>Now deploying to <color=#54e3ff>{0}</color>. Stand still for <color=#eeeeee>{1} seconds</color>.</color>" },

                // /ammo
                { "ammo_error_nocrate", "<color=#ffab87>Look at an Ammo Crate or Vehicle in order to resupply or place a rifleman's <color=#cedcde>AMMO BAG</color> in your inventory.</color>" },
                { "ammo_success", "<color=#ffab87>Your kit has been resupplied. <color=#d1c597>-1x AMMO BOX</color>.</color>" },
                { "ammo_success_bag", "<color=#ffab87>Your kit has been resupplied. <color=#d1c597>-1x AMMO BAG</color>.</color>" },
                { "ammo_success_vehicle", "<color=#ffab87>Your vehicle has been resupplied. <color=#d1c597>-{0}x AMMO BOX{1}</color>.</color>" },
                { "ammo_vehicle_cant_rearm", "<color=#9cffb3>This vehicle can't be refilled.</color>" },
                { "ammo_vehicle_out_of_main", "<color=#9cffb3>This vehicle can only be refilled at main base.</color>" },
                { "ammo_vehicle_full_already", "<color=#9cffb3>This vehicle does not need to be refilled.</color>" },
                { "ammo_vehicle_not_near_ammo_crate", "<color=#9cffb3>Your vehicle must be next to an Ammo Crate in order to rearm.</color>" },
                { "ammo_not_in_team", "<color=#9cffb3>You must be on a team to use this feature.</color>" },
                { "ammo_no_stock", "<color=#9cffb3>This ammo crate has no ammo. Fill it up with <color=#cedcde>AMMO BOXES</color> in order to resupply.</color>" },
                { "ammo_not_enough_stock", "<color=#9cffb3>This Ammo Crate is missing <color=#cedcde>AMMO BOXES</color>. <color=#d1c597>{0}/{1}</color>.</color>" },
                { "ammo_no_kit", "<color=#9cffb3>You don't have a kit yet. Go and request one at main.</color>" },
                { "ammo_crate_has_no_storage", "<color=#9cffb3>This is an ammo crate according to the server's config, but it has no storage. The admins may have messed something up.</color>" },
                { "ammo_cooldown", "<color=#9cffb3>You can not refill your kit for another {0} seconds.</color>" },
                { "ammo_vehicle_cooldown", "<color=#9cffb3>You can not refill your vehicle for another {0} seconds.</color>" },

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
                { "longest_shot_format", "{0}m\n{1}" },
                { "lblKills", "Kills: " },
                { "lblDeaths", "Deaths: " },
                { "lblKDR", "K/D Ratio: " },
                { "lblKillsOnPoint", "Kills on Flag: " },
                { "lblTimeDeployed", "Time Deployed: " },
                { "lblXpGained", "XP Gained: " },
                { "lblTimeOnPoint", "Time on Flag: " },
                { "lblCaptures", "Captures: " },
                { "lblTimeInVehicle", "Time Driving: " },
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
                // ban
                { "ban_syntax", "<color=#9cffb3>Syntax: <i>/ban <player> <duration minutes> <reason ...></i>.</color>" },
                { "ban_permanent_feedback", "<color=#00ffff><color=#d8addb>{0}</color> was <b>permanently</b> banned.</color>" },
                { "ban_permanent_broadcast", "<color=#00ffff><color=#d8addb>{0}</color> was <b>permanently</b> banned by <color=#00ffff>{1}</color>.</color>" },
                { "ban_permanent_broadcast_operator", "<color=#00ffff><color=#d8addb>{0}</color> was <b>permanently</b> banned by an operator.</color>" },
                { "ban_permanent_console_operator", "{0} ({1}) was permanently banned by an operator because: {2}." },
                { "ban_permanent_console", "{0} ({1}) was permanently banned by {2} ({3}) because: {4}." },
                { "ban_feedback", "<color=#00ffff><color=#d8addb>{0}</color> was banned for <color=#9cffb3>{2}</color>.</color>" },
                { "ban_broadcast", "<color=#00ffff><color=#d8addb>{0}</color> was banned by <color=#00ffff>{1}</color> for <color=#9cffb3>{2}</color>.</color>" },
                { "ban_broadcast_operator", "<color=#00ffff><color=#d8addb>{0}</color> was banned by an operator for <color=#9cffb3>{1}</color>.</color>" },
                { "ban_console_operator", "{0} ({1}) was banned by an operator for {3} because: {2}." },
                { "ban_console", "{0} ({1}) was banned by {2} ({3}) for {5} because: {4}." },
                { "ban_no_player_found", "<color=#9cffb3>No player found from <color=#d8addb>{0}</color>.</color>" },
                { "ban_no_player_found_console", "No player found from \"{0}\"." },
                { "ban_invalid_number", "<color=#9cffb3><color=#9cffb3>{0}</color> should be a whole number between <color=#00ffff>0</color> and <color=#00ffff>4294967295</color>.</color>" },
                { "ban_invalid_number_console", "Failed to cast \"{0}\" to a UInt32 (0 to 4294967295)." },
                { "ban_no_reason_provided", "<color=#9cffb3>You must provide a reason.</color>" },
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
                { "vehicle_not_valid_kit", "You need a <color=#cedcde>{0}</color> kit in order to man this vehicle." },
                { "vehicle_need_another_person_with_kit", "You must have another <color=#cedcde>{0}</color> in this vehicle before you can enter the gunner's seat." },
                
                // other
                { "friendly_mortar_incoming", "FRIENDLY MORTAR STRIKE INCOMING" },
                // battleye
                { "battleye_kick_console", "{0} ({1}) was kicked by BattlEye because: \"{2}\"" },
                { "battleye_kick_broadcast", "<color=#00ffff><color=#d8addb>{0}</color> was kicked by <color=#feed00>BattlEye</color>.</color>" },
                // request
                { "request_saved_sign", "<color=#a4baa9>Saved kit: <color=#ffebbd>{0}</color>.</color>" },
                { "request_removed_sign", "<color=#a4baa9>Removed kit sign: <color=#ffebbd>{0}</color>.</color>" },
                { "request_sign_exists", "<color=#a8918a>A sign is already registered at that location, remove it with /request remove.</color>" },
                { "request_not_looking", "<color=#a8918a>You must be looking at a request sign or vehicle.</color>" },
                { "request_kit_given", "<color=#99918d>You have been allocated a <color=#cedcde>{0}</color> kit!</color>" },
                { "request_kit_e_signnoexist", "<color=#a8918a>This is not a request sign.</color>" },
                { "request_kit_e_kitnoexist", "<color=#a8918a>This kit has not been created yet.</color>" },
                { "request_kit_e_alreadyhaskit", "<color=#a8918a>You already have this kit.</color>" },
                { "request_kit_e_notallowed", "<color=#a8918a>You do not have access to this kit.</color>" },
                { "request_kit_e_limited", "<color=#a8918a>Your team already has a max of {0} players using this kit. Try again later.</color>" },
                { "request_kit_e_wronglevel", "<color=#a8918a>You must be <color=#ffc29c>Level {0}</color> to request this kit.</color>" },
                { "request_kit_e_wrongbranch", "<color=#a8918a>You must be a different branch.</color>" },
                { "request_kit_e_notsquadleader", "<color=#b3ab9f>You must be a <color=#cedcde>SQUAD LEADER</color> in order to get this kit.</color>" },
                { "request_vehicle_e_notrequestable", "<color=#a8918a>This vehicle cannot be reqested.</color>" },
                { "request_vehicle_e_cooldown", "<color=#b3ab9f>This vehicle can be requested in: <color=#ffe2ab>{0}</color>.</color>" },
                { "request_vehicle_e_notinsquad", "<color=#b3ab9f>You must be <color=#cedcde>IN A SQUAD</color> in order to request this vehicle.</color>" },
                { "request_vehicle_e_nokit", "<color=#a8918a>Get a kit before you request vehicles.</color>" },
                { "request_vehicle_e_wrongkit", "<color=#b3ab9f>You need a {0} kit in order to request this vehicle.</color>" },
                { "request_vehicle_e_wronglevel", "<color=#b3ab9f>You must be <color=#ffc29c>Level {0}</color> to request this vehicle.</color>" },
                { "request_vehicle_e_wrongbranch", "<color=#b3ab9f>You must be a part of <color=#fcbda4>{0}</color> to request this vehicle.</color>" },
                { "request_vehicle_e_alreadyrequested", "<color=#a8918a>This vehicle has already been requested.</color>" },
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

                // vehicle bay signs
                { "vehiclebay_sign_no_min_level", "<color=#{4}><color=#{1}>{0}</color>\nTickets: <color=#{3}>{2}</color></color>" }, // 0: vehicle name, 1: vehicle color, 2: Ticket cost, 3: Ticket cost color , 4: background color
                { "vehiclebay_sign_min_level", "<color=#{6}><color=#{1}>{0}</color>\n<color=#{3}>{2}</color>\nTickets: <color=#{5}>{4}</color></color>" }, // 0: vehicle name, 1: vehicle color, 2: rank, 3: rank color, 4: Ticket cost, 5: Ticket cost color, 6: background color

                // Officers
                { "officer_promoted", "<color=#9e9788>Congratulations, you have been <color=#e3b552>PROMOTED</color> to <color=#e05353>{0}</color> of the <color=#baccca>{1}</color>!</color>" },
                { "officer_demoted", "<color=#9e9788>You have been <color=#c47f5c>DEMOTED</color> to <color=#e05353>{0}</color>.</color>" },
                { "officer_discharged", "<color=#9e9788>You have been <color=#ab2e2e>DISCHARGED</color> from the officer ranks for unacceptable behaviour.</color>" },
                { "officer_announce_promoted", "<color=#9e9788><color=#c4daff>{0}</color> has been <color=#e3b552>PROMOTED</color> to <color=#e05353>{1}</color> of the <color=#baccca>{2}</color>!</color>" },
                { "officer_announce_demoted", "<color=#9e9788><color=#c4daff>{0}</color> has been <color=#c47f5c>DEMOTED</color> to <color=#e05353>{0}s</color>.</color>" },
                { "officer_announce_discharged", "<color=#9e9788><color=#c4daff>{0}</color> has been <color=#ab2e2e>DISCHARGED</color> from the rank of <color=#e05353>{1}s</color> for unacceptable behaviour.</color>" },
                { "officer_ui_no_stars", "no stars" },
                { "officer_ui_stars", "{0} star{1}" },

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
                { "branch_changed", "<color=#ccb89f>You have joined the <color=#ff9182>{0}</color>.</color>" },
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
                { "kit_required_level", "<color=#{1}>L {0}</color><color=#{3}> - {2}</color>" }, // {0} = level number
                { "kit_owned", "OWNED" },
                { "kit_unlimited", "unlimited" },
                { "kit_not_owned", "NOT OWNED" },
                { "kit_player_count", "{0}/{1}" },
                { "sign_kit_request", "{0}\n{1}\n{2}\n{3}" },  
                // {0} = name, {1} = Lvl __ or '\n' if lvl == 0 or if premium cost, {2} = weapon list, {3} player count on team

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
                { "xp_friendly_killed", "TEAMKILLED {0}" },
                { "xp_fob_killed", "DESTROYED FOB" },
                { "xp_deployed_fob", "TEAMMATE DEPLOYED" },

                { "xp_victory", "VICTORY" },
                { "xp_flag_captured", "FLAG CAPTURED" },
                { "xp_flag_neutralized", "FLAG NEUTRALIZED" },
                { "xp_flag_attack", "ATTACK" },
                { "xp_flag_defend", "DEFENCE" },

                { "xp_squad_bonus", "SQUAD BONUS" },

                { "xp_humvee_destroyed", "HUMVEE DESTROYED" },
                { "xp_transport_destroyed", "TRANSPORT DESTROYED" },
                { "xp_logistics_destroyed", "LOGISTICS DESTROYED" },
                { "xp_apc_destroyed", "APC DESTROYED" },
                { "xp_ifv_destroyed", "IFV DESTROYED" },
                { "xp_tank_destroyed", "TANK DESTROYED" },
                { "xp_helicopter_destroyed", "HELICOPTER DESTROYED" },
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
                { "promoted_xp", "YOU HAVE BEEN <color=#ffbd8a>PROMOTED</color> TO" },
                { "demoted_xp", "YOU HAVE BEEN <color=#ff8a8a>DEMOTED</color> TO" },

                { "ui_xp_level", "L {0}" },
                { "ui_ofp_level", "O {0}" },
                { "ui_ofp_equivalent", "L {0} equivalent" },
                { "ui_xp_next_level", "{0}  L {1}" },

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

                { "gain_ofp", "+{0} OF" },
                { "loss_ofp", "-{0} OF" },
                { "gain_star", "FOR EXCELLENT LEADERSHIP, YOU HAVE BEEN AWARDED" },

                { "injured_ui_header", "You are injured" },
                { "injured_ui_give_up", "Press <b>'/'</b> to give up.\n " },
                { "injured_chat", "<color=#ff8c69>You were injured, press <color=#cedcde><plugin_2/></color> to give up.</color>" }
            };
        }
        public static Dictionary<string, string> DefaultTranslations;
        public static readonly List<FlagData> DefaultFlags = new List<FlagData>
        { 
            new FlagData(1, "AmmoHill", -89, 297, new ZoneData("rectangle", "86,68"), true, 4, -1, -1),
            new FlagData(2, "Hilltop", 258, 497, new ZoneData("rectangle", "66,72"), true, 3, -1, -1),
            new FlagData(3, "Papanov", 754, 759, new ZoneData("polygon", "635,738,713,873,873,780,796,645"), true, 3, -1, -1),
            new FlagData(4, "Verto", 624, 469, new ZoneData("polygon", "500,446,514,527,710,492,748,466,710,411"), true, 2, -1, -1),
            new FlagData(5, "Hill123", 631, 139, new ZoneData("rectangle", "44,86"), true, 0, -1, -1),
            new FlagData(6, "Hill13", 338, -15, new ZoneData("circle", "35"), true, 1, -1, -1),
            new FlagData(7, "Mining", 52.5f, -215, new ZoneData("polygon", "7,-283,-6,-270,-6,-160,7,-147,72,-147,111,-160,111,-257,104,-264,40,-283"), true, 0, -1, -1),
            new FlagData(8, "Fortress", -648.5f, 102.5f, new ZoneData("rectangle", "79,47"), true, 0, -1, -1)
        };
        public static List<FlagData> DefaultExtraZones = new List<FlagData>
        {
            new FlagData(-69, "lobby", 713.1f, -991, new ZoneData("rectangle", "12.2,12"), false, 0, -1, -1),
            new FlagData(1, "T1Main", 823, -880.5f, new ZoneData("rectangle", "120,189"), true, 0, -1, -1),
            new FlagData(101, "T1AMC", 717.5f, -697.5f, new ZoneData("rectangle", "613,653"), true, 0, -1, -1),
            new FlagData(2, "T2Main", -823, 876.5f, new ZoneData("rectangle", "120,189"), true, 0, -1, -1),
            new FlagData(102, "T2AMC", -799, 744.5f, new ZoneData("rectangle", "450,559"), true, 0, -1, -1),
        };
        public static List<Point3D> DefaultExtraPoints = new List<Point3D>
        {
            new Point3D("lobby_spawn", 713.1f, 39f, -991)
        };
        public static readonly List<ColorData> DefaultColors = new List<ColorData>
        {
            new ColorData("default", "ffffff"),
            new ColorData("defaulterror", "ff8c69"),
            new ColorData("uncreated", "9cb6a4"),
            new ColorData("attack_icon_color", "ffca61"),
            new ColorData("defend_icon_color", "ba70cc"),
            new ColorData("undiscovered_flag", "696969"),
            new ColorData("team_count_ui_color_team_1", "ffffff"),
            new ColorData("team_count_ui_color_team_2", "ffffff"),
            new ColorData("team_count_ui_color_team_1_icon", "4785ff"),
            new ColorData("team_count_ui_color_team_2_icon", "f53b3b"),

            // Team Colors
            new ColorData("team_1_color", "4785ff"),
            new ColorData("team_2_color", "f53b3b"),
            new ColorData("team_3_color", "0099ff"),
            new ColorData("neutral_color", "c2c2c2"),

            // Team 1 Circle
            new ColorData("capturing_team_1", "4785ff"),
            new ColorData("losing_team_1", "f53b3b"),
            new ColorData("clearing_team_1", "4785ff"),
            new ColorData("contested_team_1", "ffff1a"),
            new ColorData("secured_team_1", "00ff00"),
            new ColorData("nocap_team_1", "ff8c69"),
            new ColorData("notowned_team_1", "ff8c69"),
            new ColorData("in_vehicle_team_1", "ff8c69"),

            // Team 1 Background Circle
            new ColorData("capturing_team_1_bkgr", "002266"),
            new ColorData("losing_team_1_bkgr", "610505"),
            new ColorData("clearing_team_1_bkgr", "002266"),
            new ColorData("contested_team_1_bkgr", "666600"),
            new ColorData("secured_team_1_bkgr", "006600"),
            new ColorData("nocap_team_1_bkgr", "660000"),
            new ColorData("notowned_team_1_bkgr", "660000"),
            new ColorData("in_vehicle_team_1_bkgr", "660000"),

            // Team 1 Words
            new ColorData("capturing_team_1_words", "4785ff"),
            new ColorData("losing_team_1_words", "f53b3b"),
            new ColorData("clearing_team_1_words", "4785ff"),
            new ColorData("contested_team_1_words", "ffff1a"),
            new ColorData("secured_team_1_words", "00ff00"),
            new ColorData("nocap_team_1_words", "ff8c69"),
            new ColorData("notowned_team_1_words", "ff8c69"),
            new ColorData("in_vehicle_team_1_words", "ff8c69"),

            // Team 2 Circle
            new ColorData("capturing_team_2", "f53b3b"),
            new ColorData("losing_team_2", "4785ff"),
            new ColorData("clearing_team_2", "f53b3b"),
            new ColorData("contested_team_2", "ffff1a"),
            new ColorData("secured_team_2", "00ff00"),
            new ColorData("nocap_team_2", "ff8c69"),
            new ColorData("notowned_team_2", "ff8c69"),
            new ColorData("in_vehicle_team_2", "ff8c69"),

            // Team 2 Background Circle
            new ColorData("capturing_team_2_bkgr", "610505"),
            new ColorData("losing_team_2_bkgr", "002266"),
            new ColorData("clearing_team_2_bkgr", "610505"),
            new ColorData("contested_team_2_bkgr", "666600"),
            new ColorData("secured_team_2_bkgr", "006600"),
            new ColorData("nocap_team_2_bkgr", "660000"),
            new ColorData("notowned_team_2_bkgr", "660000"),
            new ColorData("in_vehicle_team_2_bkgr", "660000"),

            // Team 2 Words
            new ColorData("capturing_team_2_words", "f53b3b"),
            new ColorData("losing_team_2_words", "4785ff"),
            new ColorData("clearing_team_2_words", "f53b3b"),
            new ColorData("contested_team_2_words", "ffff1a"),
            new ColorData("secured_team_2_words", "00ff00"),
            new ColorData("nocap_team_2_words", "ff8c69"),
            new ColorData("notowned_team_2_words", "ff8c69"),
            new ColorData("in_vehicle_team_2_words", "ff8c69"),

            // Flag Chats
            new ColorData("entered_cap_radius_team_1", "e6e3d5"),
            new ColorData("entered_cap_radius_team_2", "e6e3d5"),
            new ColorData("left_cap_radius_team_1", "e6e3d5"),
            new ColorData("left_cap_radius_team_2", "e6e3d5"),

            // Team 1 Chat
            new ColorData("capturing_team_1_chat", "e6e3d5"),
            new ColorData("losing_team_1_chat", "e6e3d5"),
            new ColorData("clearing_team_1_chat", "e6e3d5"),
            new ColorData("contested_team_1_chat", "e6e3d5"),
            new ColorData("secured_team_1_chat", "e6e3d5"),
            new ColorData("nocap_team_1_chat", "e6e3d5"),
            new ColorData("notowned_team_1_chat", "e6e3d5"),
            new ColorData("in_vehicle_team_1_chat", "e6e3d5"),

            // Team 2 Chat
            new ColorData("capturing_team_2_chat", "e6e3d5"),
            new ColorData("losing_team_2_chat", "e6e3d5"),
            new ColorData("clearing_team_2_chat", "e6e3d5"),
            new ColorData("contested_team_2_chat", "e6e3d5"),
            new ColorData("secured_team_2_chat", "e6e3d5"),
            new ColorData("nocap_team_2_chat", "e6e3d5"),
            new ColorData("notowned_team_2_chat", "e6e3d5"),
            new ColorData("in_vehicle_team_2_chat", "e6e3d5"),

            // Other Flag Chats
            new ColorData("flag_neutralized", "e6e3d5"),
            new ColorData("team_win", "e6e3d5"),
            new ColorData("team_capture", "e6e3d5"),

            // Deaths
            new ColorData("death_background", "ffffff"),
            new ColorData("death_zombie_name_color", "788c5a"),

            // Request
            new ColorData("kit_public_header", "ffffff"),
            new ColorData("kit_level_available", "ff974d"),
            new ColorData("kit_level_available_abbr", "999999"),
            new ColorData("kit_level_unavailable", "c4785e"),
            new ColorData("kit_level_unavailable_abbr", "999999"),
            new ColorData("kit_level_dollars", "7878ff"),
            new ColorData("kit_level_dollars_owned", "769fb5"),
            new ColorData("kit_weapon_list", "343434"),
            new ColorData("kit_unlimited_players", "111111"),
            new ColorData("kit_player_counts_available", "96ffb2"),
            new ColorData("kit_player_counts_unavailable", "c2603e"),

            // Vehicle Sign
            new ColorData("vbs_background", "222222"),
            new ColorData("vbs_vehicle_name_color", "a0ad8e"),
            new ColorData("vbs_locked_vehicle_color", "800000"),
            new ColorData("vbs_rank_color", "e6e3d5"),
            new ColorData("vbs_ticket_cost", "e6e3d5"),

            // stars
            new ColorData("no_stars", "737373"),
            new ColorData("star_color", "ffd683"),
        };
        public static readonly List<MySqlTableData> DefaultMySQLTableData = new List<MySqlTableData>
        {
            new MySqlTableData("discord_accounts", "discord_accounts", new List<MySqlColumnData> {
                new MySqlColumnData("Steam64","Steam64"),
                new MySqlColumnData("DiscordID","DiscordID")
            }),
            new MySqlTableData("usernames", "usernames", new List<MySqlColumnData> {
                new MySqlColumnData("Steam64","Steam64"),
                new MySqlColumnData("PlayerName","PlayerName"),
                new MySqlColumnData("CharacterName","CharacterName"),
                new MySqlColumnData("NickName","NickName")
            }),
            new MySqlTableData("logindata", "logindata", new List<MySqlColumnData> {
                new MySqlColumnData("Steam64","Steam64"),
                new MySqlColumnData("IP","IP"),
                new MySqlColumnData("LastLoggedIn","LastLoggedIn")
            }),
            new MySqlTableData("levels", "levels", new List<MySqlColumnData> {
                new MySqlColumnData("Steam64", "Steam64"),
                new MySqlColumnData("Team", "Team"),
                new MySqlColumnData("OfficerPoints", "OfficerPoints"),
                new MySqlColumnData("XP", "XP")
            }),
            new MySqlTableData("playerstats", "playerstats", new List<MySqlColumnData> {
                new MySqlColumnData("Steam64","Steam64"),
                new MySqlColumnData("Team","Team"),
                new MySqlColumnData("Username","Username"),
                new MySqlColumnData("Kills","Kills"),
                new MySqlColumnData("Deaths","Deaths"),
                new MySqlColumnData("Teamkills","Teamkills")
            })
        };
        public static List<Kit> DefaultKits = new List<Kit>
        {
            new Kit("default",
                new List<KitItem> { },
                new List<KitClothing> {
                new KitClothing(184, 100, "", KitClothing.EClothingType.SHIRT),
                new KitClothing(2, 100, "", KitClothing.EClothingType.PANTS),
                new KitClothing(185, 100, "", KitClothing.EClothingType.MASK)
            })
            {
                ShouldClearInventory = true,
                RequiredLevel = 0,
                Cost = 0,
                Team = 0,
                Class = Kit.EClass.UNARMED,
                Branch = EBranch.DEFAULT,
                SignTexts = new Dictionary<string, string> { 
                    { DefaultLanguage, "<color=#{0}>Default Kit</color>\n<color=#{2}>{1}</color>" },
                    { "ru-ru", "<color=#{0}>Комплект по умолчанию</color>\n<color=#{2}>{1}</color>" }
                }
            },
            new Kit("usunarmed",
                new List<KitItem> { },
                new List<KitClothing> {
                new KitClothing(30710, 100, "", KitClothing.EClothingType.SHIRT),
                new KitClothing(30711, 100, "", KitClothing.EClothingType.PANTS),
                new KitClothing(30715, 100, "", KitClothing.EClothingType.HAT),
                new KitClothing(30718, 100, "", KitClothing.EClothingType.BACKPACK),
                new KitClothing(31251, 100, "", KitClothing.EClothingType.GLASSES)
            })
            {
                ShouldClearInventory = true,
                RequiredLevel = 0,
                Cost = 0,
                Team = 1,
                Class = Kit.EClass.UNARMED,
                Branch = EBranch.DEFAULT,
                SignTexts = new Dictionary<string, string> { 
                    { DefaultLanguage, "<color=#{0}>Unarmed</color>\n<color=#{2}>{1}</color>" }, 
                    { "ru-ru", "<color=#{0}>Безоружный</color>\n<color=#{2}>{1}</color>" } 
                }
            },
            new Kit("ruunarmed",
                new List<KitItem> { },
                new List<KitClothing> {
                new KitClothing(30700, 100, "", KitClothing.EClothingType.SHIRT),
                new KitClothing(30701, 100, "", KitClothing.EClothingType.PANTS),
                new KitClothing(31123, 100, "", KitClothing.EClothingType.VEST),
                new KitClothing(30704, 100, "", KitClothing.EClothingType.HAT),
                new KitClothing(434, 100, "", KitClothing.EClothingType.MASK),
                new KitClothing(31156, 100, "", KitClothing.EClothingType.BACKPACK)
            })
            {
                ShouldClearInventory = true,
                RequiredLevel = 0,
                Cost = 0,
                Team = 2,
                Class = Kit.EClass.UNARMED,
                Branch = EBranch.DEFAULT,
                SignTexts = new Dictionary<string, string> { 
                    { DefaultLanguage, "<color=#{0}>Unarmed</color>\n<color=#{2}>{1}</color>" } ,
                    { "ru-ru", "<color=#{0}>Безоружный</color>\n<color=#{2}>{1}</color>" }
                }
            },
            new Kit("usrif1",
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
                new KitClothing(30710, 100, "", KitClothing.EClothingType.SHIRT),
                new KitClothing(30711, 100, "", KitClothing.EClothingType.PANTS),
                new KitClothing(30715, 100, "", KitClothing.EClothingType.HAT),
                new KitClothing(30718, 100, "", KitClothing.EClothingType.BACKPACK),
                new KitClothing(31251, 100, "", KitClothing.EClothingType.GLASSES)
            })
            {
                ShouldClearInventory = true,
                RequiredLevel = 0,
                Cost = 0,
                Team = 1,
                Class = Kit.EClass.AUTOMATIC_RIFLEMAN,
                Branch = EBranch.INFANTRY,
                SignTexts = new Dictionary<string, string> { 
                    { DefaultLanguage, "<color=#{0}>Rifleman 1</color>\n<color=#{2}>{1}</color>" },
                    { "ru-ru", "<color=#{0}>Стрелок 1</color>\n<color=#{2}>{1}</color>" }
                }
            },
            new Kit("rurif1",
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
                new KitClothing(30700, 100, "", KitClothing.EClothingType.SHIRT),
                new KitClothing(30701, 100, "", KitClothing.EClothingType.PANTS),
                new KitClothing(31123, 100, "", KitClothing.EClothingType.VEST),
                new KitClothing(30704, 100, "", KitClothing.EClothingType.HAT),
                new KitClothing(434, 100, "", KitClothing.EClothingType.MASK),
                new KitClothing(31156, 100, "", KitClothing.EClothingType.BACKPACK)
            })
            {
                ShouldClearInventory = true,
                RequiredLevel = 0,
                Cost = 0,
                Team = 2,
                Class = Kit.EClass.AUTOMATIC_RIFLEMAN,
                Branch = EBranch.INFANTRY,
                SignTexts = new Dictionary<string, string> { 
                    { DefaultLanguage, "<color=#{0}>Rifleman 1</color>\n<color=#{2}>{1}</color>" },
                    { "ru-ru", "<color=#{0}>Стрелок 1</color>\n<color=#{2}>{1}</color>" }
                }
            },
            new Kit("africa1",
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
                new KitClothing(30960, 100, "", KitClothing.EClothingType.SHIRT),
                new KitClothing(30961, 100, "", KitClothing.EClothingType.PANTS),
                new KitClothing(30962, 100, "", KitClothing.EClothingType.VEST),
                new KitClothing(30965, 100, "", KitClothing.EClothingType.HAT),
                new KitClothing(31221, 100, "", KitClothing.EClothingType.MASK),
                new KitClothing(30970, 100, "", KitClothing.EClothingType.BACKPACK)
            })
            {
                ShouldClearInventory = true,
                RequiredLevel = 0,
                Cost = 0,
                IsPremium = true,
                PremiumCost = 6.00f,
                Team = 2,
                Class = Kit.EClass.AUTOMATIC_RIFLEMAN,
                Branch = EBranch.INFANTRY,
                SignTexts = new Dictionary<string, string> { 
                    { DefaultLanguage, "<color=#{0}>Africa 1</color>\n<color=#{2}>{1}</color>" },
                    { "ru-ru", "<color=#{0}>Африка 1</color>\n<color=#{2}>{1}</color>" }
                }
            }
        };
        public static readonly List<LanguageAliasSet> DefaultLanguageAliasSets = new List<LanguageAliasSet>
        {
            new LanguageAliasSet("en-us", "English", new List<string> { "english", "enus", "en", "us", "inglés", "inglesa", "ingles", 
                "en-au", "en-bz", "en-ca", "en-cb", "en-ie", "en-jm", "en-nz", "en-ph", "en-tt", "en-za", "en-zw", 
                "enau", "enbz", "enca", "encb", "enie", "enjm", "ennz", "enph", "entt", "enza", "enzw" } ),
            new LanguageAliasSet("ru-ru", "Russian", new List<string> { "russian", "ruru", "ru", "russia", "cyrillic", "русский", "russkiy", "российский" } ),
            new LanguageAliasSet("es-es", "Spanish", new List<string> { "spanish", "español", "española", "espanol", "espanola", "es", "eses",
                "es-ar", "es-bo", "es-cl", "es-co", "es-cr", "es-do", "es-ec", "es-gt", "es-hn", "es-mx", "es-ni", "es-pa", "es-pe", "es-pr", "es-py", "es-sv", "es-uy", "es-ve",
                "esar", "esbo", "escl", "esco", "escr", "esdo", "esec", "esgt", "eshn", "esmx", "esni", "espa", "espe", "espr", "espy", "essv", "esuy", "esve" } ),
            new LanguageAliasSet("de-de", "German", new List<string> { "german", "deutsche", "de", "de-at", "de-ch", "de-li", "de-lu", "deat", "dech", "deli", "delu", "dede" } ),
            new LanguageAliasSet("ar-sa", "Arabic", new List<string> { "arabic", "ar", "arab", "عربى", "eurbaa",
                "ar-ae", "ar-bh", "ar-dz", "ar-eg", "ar-iq", "ar-jo", "ar-kw", "ar-lb", "ar-ly", "ar-ma", "ar-om", "ar-qa", "ar-sy", "ar-tn", "ar-ye",
                "arae", "arbh", "ardz", "areg", "ariq", "arjo", "arkw", "arlb", "arly", "arma", "arom", "arqa", "arsy", "artn", "arye"}),
            new LanguageAliasSet("fr-fr", "French", new List<string> { "french", "fr", "française", "français", "francaise", "francais", 
                "fr-be", "fr-ca", "fr-ch", "fr-lu", "fr-mc", 
                "frbe", "frca", "frch", "frlu", "frmc" }),
            new LanguageAliasSet("pl-pl", "Polish", new List<string> { "polish", "plpl", "polskie", "pol", "pl" }),
            new LanguageAliasSet("zh-cn", "Chinese (Simplified)", new List<string> { "chinese", "simplified chinese", "chinese simplified", "simple chinese", "chinese simple", 
                "zh", "zh-s", "s-zh", "zh-hk", "zh-mo", "zh-sg", "中国人", "zhōngguó rén", "zhongguo ren", "简体中文", "jiǎntǐ zhōngwén", "jianti zhongwen", "中国人", "zhōngguó rén", "zhongguo ren",
                "zhs", "szh", "zhhk", "zhmo", "zhsg", }),
            new LanguageAliasSet("zh-tw", "Chinese (Traditional)", new List<string> { "traditional chinese", "chinese traditional",
                "zhtw", "zh-t", "t-zh", "zht", "tzh", "中國傳統的", "zhōngguó chuántǒng de", "zhongguo chuantong de", "繁體中文", "fántǐ zhōngwén", "fanti zhongwen", "中國人" }),
            new LanguageAliasSet("pt-pt", "Portuguese", new List<string> { "portuguese", "pt", "pt-pt", "pt-br", "ptbr", "ptpt", "português", "a língua portuguesa", "o português" }),
            new LanguageAliasSet("fil", "Filipino", new List<string> { "pilipino", "fil", "pil", "tagalog", "filipino", "tl", "tl-ph", "fil-ph", "pil-ph" }),
            new LanguageAliasSet("nb-no", "Norwegian", new List<string> { "norwegian", "norway", "bokmål", "bokmal", "norsk", "nb-no", "nb", "no", "nbno" }),
            new LanguageAliasSet("ro-ro", "Romanian", new List<string> { "română", "romanian", "ro", "roro", "ro-ro", "romania" })
        };
        public static readonly Dictionary<string, string> DefaultDeathTranslations = new Dictionary<string, string> {
            { "ACID", "{0} was burned by an acid zombie." },
            { "ANIMAL", "{0} was attacked by an animal." },
            { "ARENA", "{0} stepped outside the arena boundary." },
            { "BLEEDING", "{0} bled out from {1}." },
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
            { "ROADKILL", "{1} was ran over {0}." },
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