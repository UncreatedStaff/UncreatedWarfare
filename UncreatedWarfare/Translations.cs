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
                { "current_zone", "You are in flag zone: {0}, at position ({1}, {2}, {3})." },
                { "team_win", "<color=#{1}>{0}</color> won the game!" },
                { "team_capture", "<color=#{1}>{0}</color> captured <color=#{3}>{2}</color>!" },
                { "not_in_zone", "No flag zone found at position ({0}, {1}, {2}) - {3}°, out of {4} registered flags." },
                { "player_connected", "<color=#{1}>{0}</color> joined the server!" },
                { "player_disconnected", "<color=#{1}>{0}</color> left the server." },
                { "current_group", "Group <color=#{1}>{0}</color>: <color=#{3}>{2}</color>" },
                { "cant_create_group", "You can't create a group right now." },
                { "created_group", "Created group <color=#{1}>{0}</color>: <color=#{3}>{2}</color>" },
                { "created_group_console", "{0} ({1}) created group \"{2}\": \"{3}\"" },
                { "rename_not_in_group", "You must be in a group to rename it." },
                { "renamed_group", "Renamed group <color=#{1}>{0}</color>: <color=#{3}>{2}</color> -> <color=#{5}>{4}</color>." },
                { "renamed_group_already_named_that", "The group is already named that." },
                { "renamed_group_console", "{0} ({1}) renamed group \"{2}\": \"{3}\" -> \"{4}\"." },
                { "group_not_found", "A group with that ID was not found. Are you sure you entered an existing group ID?" },
                { "not_in_group", "You aren't in a group." },
                { "joined_group", "You have joined group <color=#{1}>{0}</color>: <color=#{3}>{2}</color>" },
                { "joined_already_in_group", "You are already in that group." },
                { "joined_group_not_found", "Could not find group <color=#{1}>{0}</color>." },
                { "joined_group_console", "{0} ({1}) joined group \"{2}\": \"{3}\"." },
                { "deleted_group", "Deleted group <color=#{1}>{0}</color>: <color=#{3}>{2}</color>" },
                { "deleted_group_console", "{0} ({1}) deleted group \"{2}\": \"{3}\"" },
                { "join_not_in_lobby", "You must be in the lobby to join a new team: <color={0}>/deploy lobby</color>." },
                { "joined_team_must_rejoin", "You have joined <color=#{1}>{0}</color>. You must rejoin the server to update your name." },
                { "joined_team", "You have joined <color=#{1}>{0}</color>. Deploying you to main base." },
                { "join_already_in_team", "You are already a member of <color=#{1}>{0}</color>." },
                { "join_auto_balance_cant_switch", "<color=#{1}>{0}</color> has too many players on it to switch." },
                { "join_group_has_no_space", "<color=#{1}>{0}</color> has surpassed the server's max group size. This should be tweaked by an admin." },
                { "join_command_no_args_provided", "Do <b>/join <color=#{1}>{0}</color></b> or <b>/join <color=#{3}>{2}</color></b>." },
                { "join_group_not_found", "Could not find group <color=#{1}>{0}</color> (ID: <color=#{3}>{2}</color>). Tell an admin about this." },
                { "player_switched_groups_console_must_rejoin", "{0} ({1}) joined {2} and must rejoin." },
                { "player_switched_groups_console", "{0} ({1}) joined {2}." },
                { "from_lobby_teleport_failed", "Failed to teleport you to your main base. Do <color=#{0}>/deploy main</color> to try again." },
                { "no_permissions", "You do not have permission to use this command." },
                { "group_usage", "/group [create <ID> <Name> | rename <ID> <NewName> | join <ID> | delete <ID>]" },

                // Lang
                { "language_list", "Languages: <color=#{1}>{0}</color>." },
                { "language_current", "Current language: <color=#{1}>{0}</color>." },
                { "changed_language", "Changed your language to <color=#{1}>{0}</color>" },
                { "change_language_not_needed", "You are already set to <color=#{1}>{0}</color>." },
                { "reset_language", "Reset your language to <color=#{1}>{0}</color>" },
                { "reset_language_how", "Do <color=#{0}>/lang reset</color> to reset back to default language." },
                { "dont_have_language", "We don't have translations for <color=#{1}>{0}</color> yet. If you are fluent and want to help, feel free to ask us about submitting translations." },
                { "reset_language_not_needed", "You are already on the default language: <color=#{1}>{0}</color>." },

                // Toasts
                { "welcome_message", "Thank's for playing <color=#{0}>Uncreated Warfare</color>!\nWelcome back <color=#{2}>{1}</color>." },
                { "welcome_message_first_time", "Welcome to <color=#{0}>Uncreated Warfare</color>!\nTalk to the NPCs to get started." },

                // Kits
                { "kit_created", "<color=#a0ad8e>Created kit: <color=#ffffff>{0}</color></color>" },
                { "kit_given", "<color=#a0ad8e>Received kit: <color=#ffffff>{0}</color></color>" },
                { "kit_overwritten", "<color=#a0ad8e>Overwritten items for kit: <color=#ffffff>{0}</color></color>" },
                { "kit_deleted", "<color=#a0ad8e>Deleted kit: <color=#ffffff>{0}</color></color>" },
                { "kit_setprop", "<color=#a0ad8e>Set {0} for kit <color=#ffffff>{1}</color> to: <color=#8ce4ff>{2}</color></color>" },
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

                // Squads
                { "squad_created", "<color=#a0ad8e>You created the squad <color=#ffffff>{0}</color></color>" },
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

                // fobs
                { "time_left", "<color=#FFAA42>Time left: <color=#FFE4B5>{0}</color><color=#FFAA42></color>" },
                { "build_error_noteam", "<color=#FFAB87>You must be looking at a friendly structure base in order to build it.</color>" },
                { "build_error_notfriendly", "<color=#FFAB87>That FOB foundation is not friendly.</color>" },
                { "build_error_nofoundation", "<color=#FFAB87>You must be looking at a friendly structure base in order to build it.</color>" },
                { "build_error_notenoughbuild", "<color=#FAE69C>You are missing build! <color=#d1c597>Nearby Build: </color><color=#d1c597>{0}/{1}</color></color>" },
                { "build_error_fobtoofar", "<color=#FAE69C>You must be next to a friendly FOB to build this structure.</color>" },
                { "build_error_tooclosetomain", "<color=#FAE69C>You cannot build too close to main.</color>" },
                { "build_error_maxemplacements", "<color=#d1c597>This FOB already has {0} {1}s.</color>" },
                { "build_error_notbuildable", "<color=#d1c597>That barricade is not buildable.</color>" },
                { "fob_nofobs", "<color=#b5a591>Your team has no active FOBs. Take a Logi Truck and go and build some!</color>" },
                { "fob_built", "<color=#b0ffa8>Successfully built FOB! Your team may now spawn on it.</color>" },
                { "fob_teleported", "<color=#FAE69C>You have been deployed to <color=#54e3ff>{0}</color>.</color>" },
                { "fob_error_nologi", "<color=#FFAB87>You need to be near a friendly logistics truck in order to build a FOB!</color>" },
                { "fob_error_fobtooclose", "<color=#ffa238>You are too close to an existing friendly fob! You need to be 300m away from it to construct a new fob.</color>" },
                { "fob_error_limitreached", "<color=#ffa238>The number of FOBs allowed on the map has been reached.</color>" },
                { "ammocrate_built", "<color=#b0ffa8>Successfully built ammo crate. Your team may now resupply from it.</color>" },
                { "ammocrate_error_alreadyexists", "<color=#ffa238>This FOB already has an ammo crate.</color>" },
                { "repairstation_built", "<color=#b0ffa8>Successfully built repair station. Your team may now repair damaged vehicles at this FOB.</color>" },
                { "repairstation_error_alreadyexists", "<color=#ffa238>This FOB already has a repair station.</color>" },
                { "emplacement_built", "<color=#b0ffa8>Successfully built {0}. Do /ammo on it to resupply.</color>" },

                // deployment
                { "deploy_error_routine_fobdamaged", "<color=#ffa238>The FOB you were deploying to is now too damaged!</color>" },
                { "deploy_error_routine_fobdead", "<color=#ffa238>The FOB you were deploying to was detroyed!</color>" },
                { "deploy_error_routine_moved", "<color=#ffa238>You moved and can no longer deploy!</color>" },
                { "deploy_error_routine_combat", "<color=#ffa238>You are now in combat and can no longer deploy!</color>" },
                { "deploy_error_routine_dead", "<color=#ffa238>You died and can no longer deploy!</color>!" },
                { "deploy_error_fobnotfound", "<color=#b5a591>There is no location or FOB by the name of '{0}'.</color>" },
                { "deploy_error_notnearfob", "<color=#b5a591>You must be on an active friendly FOB or at main in order to redeploy to another location.</color>" },
                { "deploy_error_fobnotbuilt", "<color=#ffa238>That FOB is not built! Your team must build it first before it can become spawnable.</color>" },
                { "deploy_error_fobdamaged", "<color=#ffa238>That FOB is damaged! Your team must repair it first before it can become spawnable again.</color>" },
                { "deploy_error_cooldown", "<color=#b5a591>You can't redeploy again so quickly! Time left to deploy: <color=#e3c27f>{0}</color></color>" },
                { "deploy_error_incombat", "<color=#ffaa42>You are in combat, soldier! Wait until you are safe before you can redploy.</color>" },
                { "deploy_standby", "<color=#FAE69C>Now deploying to <color=#54e3ff>{0}</color>. You will arrive in <color=#EEEEEE>{1} seconds</color>. </color>" },
                { "deploy_standby_nomove", "<color=#FAE69C>Now deploying to <color=#54e3ff>{0}</color>. Stand still for <color=#EEEEEE>{1} seconds</color>. </color>" },
                { "mainbase_standby", "<color=#FAE69C>Now deploying to <color=#bdbab1>{0}</color>. You will arrive in <color=#EEEEEE>{1} seconds</color>. </color>" },
                { "mainbase_standby_nomove", "<color=#FAE69C>Now deploying to <color=#bdbab1>{0}</color>. Stand still for <color=#EEEEEE>{1} seconds</color>. </color>" },
                { "mainbase_teleported", "<color=#FAE69C>You have arrived at <color=#bdbab1>{0}</color>.</color>" },
                { "mainbase_created", "<color=#dbcfb6>Successfully created the main base '<color=#ffac40>{0}</color>'.</color>" },
                { "mainbase_removed", "<color=#dbcfb6>Successfully removed the main base <color=#ffac40>{0}</color></color>" },
                { "mainbase_clear", "<color=#dbcfb6>Successfully removed all existing main bases.</color>" },
                { "mainbase_error_noexist", "<color=#FFAB87>There is no existing main bases by the name of '{0}'!</color>" },
                { "mainbase_error_exists", "<color=#FFAB87>A main base by the name of '{0}' already exists!</color>" },
                { "mainbase_error_noexistteam", "<color=#FFAB87>Your team does not have a main base!</color>" },
                { "mainbase_error_nolobby", "<color=#FFAB87>There is no lobby to deploy to!</color>" },

                // /ammo
                { "ammo_error_nocrate", "<color=#FFAB87>Look at a placed Ammo Crate or vehicle in order to resupply.</color>" },
                { "ammo_error_nokit", "<color=#FFAB87>You don't have a kit yet. Go and request one at main.</color>" },
                { "ammo_success", "<color=#FFAB87>Your kit has been resupplied. <color=#d1c597>-1x Ammo crate</color>.</color>" },

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
                { "player_name_header", "<color=#{1}>{0}</color>" },
                { "war_name_header", "<color=#{1}>{0}</color> vs <color=#{3}>{2}</color>" },
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
                { "lblTopRankingOfficer", "Highest Ranker: " },
                { "next_game_start_label", "Next Game Starting In" },
                { "next_game_start_label_shutting_down", "<color=#00ffff>Shutting Down Because: \"{0}\"</color>" },
                { "next_game_starting_format", "{0:mm\\:ss}" },

                // SIGNS - must prefix with "sign_" for them to work
                { "sign_test", "<color=#ff00ff>This is the english translation for that sign.</color>" },
                { "sign_rules", "Rules\nNo suicide vehicles.\netc." },

                // Admin Commands
                { "NotRunningErrorText", "This is not a server." },
                { "InvalidParameterErrorText", "Invalid parameter count. Quotes should be around any parameters with spaces." },
                // kick
                { "kick_ErrorNoReasonProvided_Console", "You must provide a reason." },
                { "kick_ErrorNoReasonProvided", "You must provide a reason." },
                { "kick_NoPlayerErrorText", "No player found from <color=#d8addb>{0}</color>." },
                { "kick_NoPlayerErrorText_Console", "No player found from \"{0}\"." },
                { "kick_KickedPlayer", "You kicked <color=#d8addb>{0}</color>." },
                { "kick_KickedPlayer_Broadcast", "<color=#d8addb>{0}</color> was kicked by <color=#00ffff>{1}</color>." },
                { "kick_KickedPlayerFromConsole_Broadcast", "<color=#d8addb>{0}</color> was kicked by an operator." },
                { "kick_KickedPlayerFromConsole_Console", "{0} ({1}) was kicked by an operator because: {2}." },
                { "kick_KickedPlayer_Console", "{0} ({1}) was kicked by {2} ({3}) because: {4}." },
                // ban
                { "ban_BanTextPermanent", "<color=#d8addb>{0}</color> was <b>permanently</b> banned." },
                { "ban_BanTextPermanent_Broadcast", "<color=#d8addb>{0}</color> was <b>permanently</b> banned by <color=#00ffff>{1}</color>." },
                { "ban_BanTextPermanentFromConsole_Broadcast", "<color=#d8addb>{0}</color> was <b>permanently</b> banned by an operator." },
                { "ban_BanTextPermanentFromConsole_Console", "{0} ({1}) was permanently banned by an operator because: {2}." },
                { "ban_BanTextPermanent_Console", "{0} ({1}) was permanently banned by {2} ({3}) because: {4}." },
                { "ban_BanText", "<color=#d8addb>{0}</color> was banned for <color=#9cffb3>{2}</color>." },
                { "ban_BanText_Broadcast", "<color=#d8addb>{0}</color> was banned by <color=#00ffff>{1}</color> for <color=#9cffb3>{2}</color>." },
                { "ban_BanTextFromConsole_Broadcast", "<color=#d8addb>{0}</color> was banned by an operator for <color=#9cffb3>{1}</color>." },
                { "ban_BanTextFromConsole_Console", "{0} ({1}) was banned by an operator for {3} because: {2}." },
                { "ban_BanText_Console", "{0} ({1}) was banned by {2} ({3}) for {5} because: {4}." },
                { "ban_NoPlayerErrorText", "No player found from <color=#d8addb>{0}</color>." },
                { "ban_NoPlayerErrorText_Console", "No player found from \"{0}\"." },
                { "ban_InvalidNumberErrorText", "<color=#9cffb3>{0}</color> should be between <color=#9cffb3>0</color> and <color=#9cffb3>4294967295</color>." },
                { "ban_InvalidNumberErrorText_Console", "Failed to cast \"{0}\" to a UInt32 (0 to 4294967295)." },
                { "ban_ErrorNoReasonProvided_Console", "You must provide a reason." },
                { "ban_ErrorNoReasonProvided", "You must provide a reason." },
                // warn
                { "warn_NoPlayerErrorText", "No player found from <color=#d8addb>{0}</color>." },
                { "warn_NoPlayerErrorText_Console", "No player found from \"{0}\"." },
                { "warn_ErrorNoReasonProvided_Console", "You must provide a reason." },
                { "warn_ErrorNoReasonProvided", "You must provide a reason." },
                { "warn_WarnedPlayerFromConsole_DM", "An operator warned you for: <b>{0}</b>." },
                { "warn_WarnedPlayerFromConsole_Console", "Warned {0} ({1}) for: {2}" },
                { "warn_WarnedPlayerFromConsole_Broadcast", "<color=#d8addb>{0}</color> was warned by an operator." },
                { "warn_WarnedPlayer_Feedback", "You warned <color=#d8addb>{0}</color>." },
                { "warn_WarnedPlayer_DM", "<color=#00ffff>{0}</color> warned you for: <b>{1}</b>" },
                { "warn_WarnedPlayer_Console", "{0} ({1}) was warned by {2} ({3}) for: {4}" },
                { "warn_WarnedPlayer_Broadcast", "<color=#d8addb>{0}</color> was warned by <color=#00ffff>{1}</color>." },
                // amc
                { "amc_ReverseDamage", "Stop <b><color=#ff3300>main-camping</color></b>! Damage is <b>reversed</b> back on you." },
                { "amc_MainCampLogged", "Stop <b><color=#ff3300>main-camping</color></b>!" },
                // unban
                { "unban_NoPlayerErrorText", "No player ID found from <color=#d8addb>{0}</color>." },
                { "unban_NoPlayerErrorText_Console", "No player ID found from \"{0}\"." },
                { "unban_PlayerIsNotBanned", "Player <color=#d8addb>{0}</color> is not banned. You must use Steam64's for /unban." },
                { "unban_PlayerIsNotBanned_Console", "Player \"{0}\" is not banned. You must use Steam64's for /unban." },
                { "unban_UnbanTextFromConsole_WithName_Console", "Sucessfully unbanned {0} ({1})." },
                { "unban_UnbanTextFromConole_NoName_Console", "Sucessfully unbanned {0}." },
                { "unban_UnbanTextFromConsole_WithName_Broadcast", "<color=#d8addb>{0}</color> was unbanned by an operator." },
                { "unban_UnbanTextFromConsole_NoName_Broadcast", "<color=#d8addb>{0}</color> was unbanned by an operator." },
                { "unban_UnbanText_WithName_Console", "{0} ({1}) was unbanned by {2} ({3})." },
                { "unban_UnbanText_NoName_Console", "{0} was unbanned by {1} ({2})." },
                { "unban_UnbanText_WithName_Broadcast", "<color=#d8addb>{0}</color> was unbanned by <color=#00ffff>{1}</color>." },
                { "unban_UnbanText_NoName_Broadcast", "<color=#d8addb>{0}</color> was unbanned by <color=#00ffff>{1}</color>." },
                { "unban_UnbanText_WithName_Feedback", "You unbanned <color=#d8addb>{0}</color>." },
                { "unban_UnbanText_NoName_Feedback", "You unbanned <color=#d8addb>{0}</color>." },
                // loadbans
                { "loadbans_NoBansErrorText", "There are no banned players." },
                { "loadbans_LogBansDisabled", "Can't upload, Logging bans is disabled." },
                { "loadbans_UploadedBans", "Uploaded {0} ban{1} to the MySQL database and logged them." },
                // duty
                { "duty_GoOnDuty_Console", "{0} ({1}) went on duty." },
                { "duty_GoOffDuty_Console", "{0} ({1}) went off duty." },
                { "duty_GoOnDuty_Feedback", "You are now <color=#95ff4a>on duty</color>." },
                { "duty_GoOffDuty_Feedback", "You are now <color=#ff8c4a>off duty</color>." },
                { "duty_GoOnDuty_Broadcast", "<color=#d9e882>{0}</color> is now <color=#95ff4a>on duty</color>." },
                { "duty_GoOffDuty_Broadcast", "<color=#d9e882>{0}</color> is now <color=#ff8c4a>off duty</color>." },
                { "duty_KilledOnDuty_Broadcast", "<color=#cdff42>{0}</color> killed someone while <color=#ff6d24>on duty</color>! Perhaps they are abusing?" },
                { "duty_KilledOnDuty_Console", "{0} ({1}) killed {2} ({3}) while on duty!!" },
                // tk system
                { "tk_Teamkilled_Console", "{0} ({1}) teamkilled {2} ({3})!!" },
                // restrictions
                { "no_placement_on_vehicle", "You can't place a{1} <color=#d9e882>{0}</color> on a vehicle!" },
                { "cant_steal_batteries", "Stealing batteries is not allowed." },
                { "cant_leave_group", "You are not allowed to manually change groups." },
                { "cant_store_this_item", "You are not allowed to store <color=#{1}>{0}</color>." },
                // battleye
                { "battleye_kicked_Console", "{0} ({1}) was kicked by BattlEye because: \"{2}\"" },
                { "battleye_kicked_Broadcast", "<color=#d8addb>{0}</color> was kicked by <color=#feed00>BattlEye</color>." },
                // request
                { "request_saved_sign", "Saved kit: <color=#{1}>{0}</color>." },
                { "request_removed_sign", "Removed kit sign: <color=#{1}>{0}</color>." },
                { "request_sign_exists", "A sign is already registered at that location, remove it with /request remove." },
                { "request_kit_given_free", "Kit requested: <color=#{1}>{0}</color>." },
                { "request_kit_given_credits", "Kit requested: <color=#{1}>{0}</color>. <color=#{3}>-{2}</color> credits." },
                { "request_kit_given_credits_cant_afford", "You do not have <color=#{1}>{0}</color> credits." },
                { "request_kit_given_not_owned", "You do not own <color=#{1}>{0}</color>." },
                { "request_not_looking", "You must be looking at a request sign or vehicle." },
                { "request_kit_e_signnoexist", "This is not a request sign." },                             // FILL IN FORMATTING HERE \/
                { "request_kit_e_kitnoexist", "This kit has not been created yet." },
                { "request_kit_e_alreadyhaskit", "You already have this kit." },
                { "request_kit_e_notallowed", "You do not have access to this kit." },
                { "request_kit_e_limited", "Too many players are already using this kit, try again when there are less than {1} players." },
                { "request_kit_e_wronglevel", "You must be a higher level to use this kit." },
                { "request_kit_e_wrongbranch", "You must be a different branch." },
                { "request_vehicle_e_notrequestable", "This vehicle is not available." },
                { "request_vehicle_e_cooldown", "This vehicle is not available for another {0}." },
                { "request_vehicle_e_wronglevel", "You must be a higher level" },
                { "request_vehicle_e_wrongbranch", "You must be in the <color=#{1}>{0}</color> branch to use this vehicle." },
                { "request_vehicle_e_alreadyrequested", "This vehicle has already been requested by someone." },
                { "request_vehicle_given", "Unlocked <color=#{1}>{0}</color>." },
                { "kit_free", "FREE" },
                { "kit_owned", "OWNED" },
                { "kit_price_dollars", "$ {0}" },
                { "kit_price_credits", "C {0}" },
                // structure
                { "structure_not_looking", "You must be looking at a barricade, structure, or vehicle." },
                { "structure_saved", "Saved <color=#{1}>{0}</color>." },
                { "structure_saved_already", "<color=#{1}>{0}</color> is already saved." },
                { "structure_saved_not_vehicle", "You can not save a vehicle." },
                { "structure_saved_not_bush", "Why are you trying to save a bush?" },
                { "structure_unsaved_not_bush", "Why are you trying to unsave a bush?" },
                { "structure_unsaved", "Removed <color=#{1}>{0}</color> save." },
                { "structure_unsaved_already", "<color=#{1}>{0}</color> is not saved." },
                { "structure_unsaved_not_vehicle", "You can not save or remove a vehicle." },
                { "structure_remove", "Discarded <color=#{1}>{0}</color>." },
                { "structure_popped", "Destroyed <color=#{1}>{0}</color>." },
                { "structure_pop_not_poppable", "That object can not be destroyed." },
                { "structure_examine_not_examinable", "That object can not be examined." },
                { "structure_examine", "<color=#{2}>{0} <i>({1})</i></color> placed this <color=#{4}>{3}</color>." },
                { "structure_examine_not_locked", "This vehicle is not locked." },
                { "structure_last_owner_web_prompt", "Last owner of {0}: {1}, Team: {2}." },
                { "structure_last_owner_chat", "Last owner of <color=#{1}>{0}</color>: <color=#{4}>{2} <i>({3})</i></color>, Team: <color=#{6}>{5}</color>." },

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
                { "vehiclebay_setprop", "<color=#a0ad8e>Set {0} for the <color=#ffffff>{1}</color> to: <color=#8ce4ff>{2}</color></color>" },
                { "vehiclebay_setitems", "<color=#a0ad8e>Successfuly set the rearm list for the <color=#ffffff>{1}</color> from your inventory. It will now drop <color=#8ce4ff>{2}</color> items with /ammo.</color>" },
                { "vehiclebay_cleareditems", "<color=#a0ad8e>Successfuly set the rearm list for the <color=#ffffff>{1}</color> from your inventory. It will <color=#8ce4ff>no longer</color> drop any items with /ammo.</color>" },
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

                // vehicle bay signs
                { "vehiclebay_sign_no_min_level", "<color=#{4}><color=#{1}>{0}</color>\nTickets: <color=#{3}>{2}</color></color>" }, // 0: vehicle name, 1: vehicle color, 2: Ticket cost, 3: Ticket cost color , 4: background color
                { "vehiclebay_sign_min_level", "<color=#{6}><color=#{1}>{0}</color>\n<color=#{3}>{2}</color>\nTickets: <color=#{5}>{4}</color></color>" }, // 0: vehicle name, 1: vehicle color, 2: rank, 3: rank color, 4: Ticket cost, 5: Ticket cost color, 6: background color

                // Officers
                { "officer_promoted", "<color=#9e9788>Congratulations, you have been <color=#e3b552>PROMOTED</color> to <color=#e05353>{0}</color> of <color=#baccca>{1}</color>!</color>" },
                { "officer_demoted", "<color=#9e9788>You have been <color=#c47f5c>DEMOTED</color> to <color=#e05353>{0}s</color>.</color>" },
                { "officer_discharged", "<color=#9e9788>You have been <color=#ab2e2e>DISCHARGED</color> from the officer ranks for unacceptable behaviour.</color>" },

                // Clear
                { "clear_not_enough_args", "The clear command requires 1 argument." },
                { "clear_inventory_console_identity", "Specify a player name when clearing from console." },
                { "clear_inventory_player_not_found", "A player was not found from <color=#{1}>\"{0}\"</color>." },
                { "clear_inventory_self", "Cleared your inventory." },
                { "clear_inventory_others", "Cleared <color=#{1}>{0}</color>'s inventory." },
                { "clear_items_cleared", "Cleared all dropped items." },
                { "clear_structures_cleared", "Cleared all placed structures and barricades." },
                { "clear_vehicles_cleared", "Cleared all vehicles." },

                // Deaths
                { "no_murderer_name", "Unapplicable" },
                { "zombie", "a zombie" },

                // Shutdown
                { "shutdown_not_server", "This is not a server." },
                { "shutdown_syntax", "Corrent syntax: /shutdown <aftergame|*seconds*|instant> <reason>." },
                { "shutdown_broadcast_after_game", "A shutdown has been scheduled after this game because: \"<color=#{1}>{0}</color>\"" },
                { "shutdown_broadcast_after_game_canceled", "The scheduled shutdown has been canceled." },
                { "shutdown_broadcast_after_game_canceled_console", "The scheduled shutdown was canceled." },
                { "shutdown_broadcast_after_game_canceled_console_player", "The scheduled shutdown was canceled by {0}." },
                { "shutdown_broadcast_after_time", "A shutdown has been scheduled in {0} because: \"<color=#{2}>{1}</color>\"" },
                { "shutdown_broadcast_after_game_console", "A shutdown has been scheduled after this game because: \"{0}\"" },
                { "shutdown_broadcast_after_game_reminder", "A shutdown is scheduled to occur after this game because: \"<color=#{1}>{0}</color>\"" },
                { "shutdown_broadcast_after_game_console_player", "A shutdown has been scheduled after this game by {0} because: \"{1}\"" },
                { "shutdown_broadcast_after_time_console", "A shutdown has been scheduled in {0} because: \"{1}\"" },
                { "shutdown_broadcast_after_time_console_player", "A shutdown has been scheduled in {0} by {1} because: \"{2}\"" },

                // Tickets
                { "enemy_controlling", "The enemy is in control!" },
                { "enemy_dominating", "The enemy is dominating!" },
                { "defeated", "You are defeated!" },
                { "victorious", "You are victorious!" },
                { "controlling", "Your team is in control!" },
                { "dominating", "Your team is dominating!" }

            };
        }
        public static Dictionary<string, string> DefaultTranslations;
        public static readonly List<FlagData> DefaultFlags = new List<FlagData>
        {
            new FlagData(1, "AmmoHill", -89, 297, new ZoneData("rectangle", "86,68"), true, 4),
            new FlagData(2, "Hilltop", 258, 497, new ZoneData("rectangle", "66,72"), true, 3),
            new FlagData(3, "Papanov", 754, 759, new ZoneData("polygon", "635,738,713,873,873,780,796,645"), true, 3),
            new FlagData(4, "Verto", 624, 469, new ZoneData("polygon", "500,446,514,527,710,492,748,466,710,411"), true, 2),
            new FlagData(5, "Hill123", 631, 139, new ZoneData("rectangle", "44,86"), true, 0),
            new FlagData(6, "Hill13", 338, -15, new ZoneData("circle", "35"), true, 1),
            new FlagData(7, "Mining", 52.5f, -215, new ZoneData("polygon", "7,-283,-6,-270,-6,-160,7,-147,72,-147,111,-160,111,-257,104,-264,40,-283"), true, 0),
            new FlagData(8, "Fortress", -648.5f, 102.5f, new ZoneData("rectangle", "79,47"), true, 0)
        };
        public static List<FlagData> DefaultExtraZones = new List<FlagData>
        {
            new FlagData(-69, "lobby", 713.1f, -991, new ZoneData("rectangle", "12.2,12"), false, 0),
            new FlagData(1, "T1Main", 823, -880.5f, new ZoneData("rectangle", "120,189"), true, 0),
            new FlagData(101, "T1AMC", 717.5f, -697.5f, new ZoneData("rectangle", "613,653"), true, 0),
            new FlagData(2, "T2Main", -823, 876.5f, new ZoneData("rectangle", "120,189"), true, 0),
            new FlagData(102, "T2AMC", -799, 744.5f, new ZoneData("rectangle", "450,559"), true, 0),
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
            new ColorData("correct_usage_c", "ff8c69"),
            new ColorData("join_message_background", "e6e3d5"),
            new ColorData("join_message_name", "ffff1a"),
            new ColorData("leave_message_background", "e6e3d5"),
            new ColorData("leave_message_name", "ffff1a"),
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
            new ColorData("team_3_color", "00ffff"),
            new ColorData("neutral_color", "c2c2c2"),

            // Team 1 Circle
            new ColorData("capturing_team_1", "4785ff"),
            new ColorData("losing_team_1", "f53b3b"),
            new ColorData("clearing_team_1", "4785ff"),
            new ColorData("contested_team_1", "ffff1a"),
            new ColorData("secured_team_1", "00ff00"),
            new ColorData("nocap_team_1", "ff8c69"),
            new ColorData("notowned_team_1", "ff8c69"),

            // Team 1 Background Circle
            new ColorData("capturing_team_1_bkgr", "002266"),
            new ColorData("losing_team_1_bkgr", "610505"),
            new ColorData("clearing_team_1_bkgr", "002266"),
            new ColorData("contested_team_1_bkgr", "666600"),
            new ColorData("secured_team_1_bkgr", "006600"),
            new ColorData("nocap_team_1_bkgr", "660000"),
            new ColorData("notowned_team_1_bkgr", "660000"),

            // Team 1 Words
            new ColorData("capturing_team_1_words", "4785ff"),
            new ColorData("losing_team_1_words", "f53b3b"),
            new ColorData("clearing_team_1_words", "4785ff"),
            new ColorData("contested_team_1_words", "ffff1a"),
            new ColorData("secured_team_1_words", "00ff00"),
            new ColorData("nocap_team_1_words", "ff8c69"),
            new ColorData("notowned_team_1_words", "ff8c69"),

            // Team 2 Circle
            new ColorData("capturing_team_2", "f53b3b"),
            new ColorData("losing_team_2", "4785ff"),
            new ColorData("clearing_team_2", "f53b3b"),
            new ColorData("contested_team_2", "ffff1a"),
            new ColorData("secured_team_2", "00ff00"),
            new ColorData("nocap_team_2", "ff8c69"),
            new ColorData("notowned_team_2", "ff8c69"),

            // Team 2 Background Circle
            new ColorData("capturing_team_2_bkgr", "610505"),
            new ColorData("losing_team_2_bkgr", "002266"),
            new ColorData("clearing_team_2_bkgr", "610505"),
            new ColorData("contested_team_2_bkgr", "666600"),
            new ColorData("secured_team_2_bkgr", "006600"),
            new ColorData("nocap_team_2_bkgr", "660000"),
            new ColorData("notowned_team_2_bkgr", "660000"),

            // Team 2 Words
            new ColorData("capturing_team_2_words", "f53b3b"),
            new ColorData("losing_team_2_words", "4785ff"),
            new ColorData("clearing_team_2_words", "f53b3b"),
            new ColorData("contested_team_2_words", "ffff1a"),
            new ColorData("secured_team_2_words", "00ff00"),
            new ColorData("nocap_team_2_words", "ff8c69"),
            new ColorData("notowned_team_2_words", "ff8c69"),

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

            // Team 2 Chat
            new ColorData("capturing_team_2_chat", "e6e3d5"),
            new ColorData("losing_team_2_chat", "e6e3d5"),
            new ColorData("clearing_team_2_chat", "e6e3d5"),
            new ColorData("contested_team_2_chat", "e6e3d5"),
            new ColorData("secured_team_2_chat", "e6e3d5"),
            new ColorData("nocap_team_2_chat", "e6e3d5"),
            new ColorData("notowned_team_2_chat", "e6e3d5"),

            // Other Flag Chats
            new ColorData("flag_neutralized", "e6e3d5"),
            new ColorData("team_win", "e6e3d5"),
            new ColorData("team_capture", "e6e3d5"),

            // Group Command
            new ColorData("not_in_group", "e6e3d5"),
            new ColorData("current_group", "e6e3d5"),
            new ColorData("current_group_id", "4785ff"),
            new ColorData("current_group_name", "4785ff"),
            new ColorData("created_group", "e6e3d5"),
            new ColorData("created_group_id", "4785ff"),
            new ColorData("created_group_name", "4785ff"),
            new ColorData("cant_create_group", "ff8c69"),
            new ColorData("rename_not_in_group", "ff8c69"),
            new ColorData("group_not_found", "ff8c69"),
            new ColorData("renamed_group", "e6e3d5"),
            new ColorData("renamed_group_already_named_that", "ff8c69"),
            new ColorData("renamed_group_id", "4785ff"),
            new ColorData("renamed_group_old_name", "f53b3b"),
            new ColorData("renamed_group_new_name", "4785ff"),
            new ColorData("joined_group", "e6e3d5"),
            new ColorData("joined_already_in_group", "ff8c69"),
            new ColorData("joined_group_not_found", "ff8c69"),
            new ColorData("joined_group_not_found_group_id", "4785ff"),
            new ColorData("joined_group_id", "4785ff"),
            new ColorData("joined_group_name", "4785ff"),
            new ColorData("deleted_group", "e6e3d5"),
            new ColorData("deleted_group_id", "4785ff"),
            new ColorData("deleted_group_name", "4785ff"),
            new ColorData("join_not_in_lobby", "ff8c69"),
            new ColorData("join_not_in_lobby_command", "e6e3d5"),
            new ColorData("joined_team_must_rejoin", "e6e3d5"),
            new ColorData("joined_team", "e6e3d5"),
            new ColorData("join_already_in_team", "f53b3b"),
            new ColorData("join_auto_balance_cant_switch", "f53b3b"),
            new ColorData("join_auto_balance_cant_switch_queue_command", "e6e3d5"),
            new ColorData("join_group_has_no_space", "f53b3b"),
            new ColorData("join_group_not_found", "f53b3b"),
            new ColorData("join_group_not_found_group_id", "4785ff"),
            new ColorData("from_lobby_teleport_failed", "ff8c69"),
            new ColorData("from_lobby_teleport_failed_command", "4785ff"),
            new ColorData("no_permissions", "ff8c69"),

            // Lang Command
            new ColorData("language_list", "f53b3b"),
            new ColorData("language_list_list", "e6e3d5"),
            new ColorData("language_current", "f53b3b"),
            new ColorData("language_current_language", "e6e3d5"),
            new ColorData("changed_language", "f53b3b"),
            new ColorData("changed_language_language", "e6e3d5"),
            new ColorData("change_language_not_needed", "f53b3b"),
            new ColorData("change_language_not_needed_language", "e6e3d5"),
            new ColorData("reset_language", "f53b3b"),
            new ColorData("reset_language_language", "e6e3d5"),
            new ColorData("reset_language_not_needed", "f53b3b"),
            new ColorData("reset_language_not_needed_language", "e6e3d5"),
            new ColorData("reset_language_how", "f53b3b"),
            new ColorData("reset_language_how_command", "e6e3d5"),
            new ColorData("dont_have_language", "dd1111"),
            new ColorData("dont_have_language_language", "e6e3d5"),

            // Ban
            new ColorData("ban_broadcast", "00ffff"),
            new ColorData("ban_feedback", "00ffff"),

            // Unban
            new ColorData("unban_broadcast", "00ffff"),
            new ColorData("unban_feedback", "00ffff"),

            // Warn
            new ColorData("warn_broadcast", "ffff00"),
            new ColorData("warn_feedback", "ffff00"),
            new ColorData("warn_message", "ffff00"),

            // Kick
            new ColorData("kick_broadcast", "00ffff"),
            new ColorData("kick_feedback", "00ffff"),

            // Duty
            new ColorData("duty_broadcast", "c6d4b8"),
            new ColorData("duty_feedback", "c6d4b8"),

            // Deaths
            new ColorData("death_background", "ffffff"),
            new ColorData("death_zombie_name_color", "788c5a"),

            // Request
            new ColorData("request_saved_sign", "00ffff"),
            new ColorData("request_removed_sign", "00ffff"),
            new ColorData("request_sign_exists", "00ffff"),
            new ColorData("request_kit_given_free", "00fffff"),
            new ColorData("request_kit_given_credits", "00fffff"),
            new ColorData("request_kit_given_credits_credits", "c6d4b8"),
            new ColorData("request_kit_given_credits_cant_afford", "00fffff"),
            new ColorData("request_kit_given_credits_cant_afford_credits", "c6d4b8"),
            new ColorData("request_kit_given_not_owned", "00fffff"),
            new ColorData("request_not_looking", "ff8c69"),
            new ColorData("request_kit_e_signnoexist", "ff8c69"),
            new ColorData("request_kit_e_kitnoexist", "ff8c69"),
            new ColorData("request_kit_e_alreadyhaskit", "ff8c69"),
            new ColorData("request_kit_e_notallowed", "ff8c69"),
            new ColorData("request_kit_e_limited", "ff8c69"),
            new ColorData("request_kit_e_wronglevel", "ff8c69"),
            new ColorData("request_kit_e_wrongbranch", "ff8c69"),
            new ColorData("request_vehicle_e_notrequestable", "ff8c69"),
            new ColorData("request_vehicle_e_cooldown", "ff8c69"),
            new ColorData("request_vehicle_e_wronglevel", "ff8c69"),
            new ColorData("request_vehicle_e_wrongbranch", "ff8c69"),
            new ColorData("request_vehicle_e_wrongbranch_branch", "c6d4b8"),
            new ColorData("request_vehicle_e_alreadyrequested", "ff8c69"),
            new ColorData("request_vehicle_given", "00fffff"),
            new ColorData("request_vehicle_given_vehicle_name", "c6d4b8"),
            new ColorData("kit_price_free", "f53b3b"),
            new ColorData("kit_price_credits", "f53b3b"),
            new ColorData("kit_price_dollars", "f53b3b"),
            new ColorData("kit_price_owned", "f53b3b"),

            // Vehicle Sign
            new ColorData("vbs_background", "222222"),
            new ColorData("vbs_vehicle_name_color", "a0ad8e"),
            new ColorData("vbs_locked_vehicle_color", "800000"),
            new ColorData("vbs_rank_color", "e6e3d5"),
            new ColorData("vbs_ticket_cost", "e6e3d5"),

            // Structure
            new ColorData("structure_saved", "e6e3d5"),
            new ColorData("structure_saved_already", "e6e3d5"),
            new ColorData("structure_saved_not_vehicle", "ff8c69"),
            new ColorData("structure_saved_not_bush", "ff8c69"),
            new ColorData("structure_unsaved", "e6e3d5"),
            new ColorData("structure_unsaved_already", "e6e3d5"),
            new ColorData("structure_unsaved_not_vehicle", "ff8c69"),
            new ColorData("structure_unsaved_not_bush", "ff8c69"),
            new ColorData("structure_not_looking", "ff8c69"),
            new ColorData("structure_examine_not_locked", "ff8c69"),
            new ColorData("structure_popped", "e6e3d5"),
            new ColorData("structure_popped_structure", "c6d4b8"),
            new ColorData("structure_saved_structure", "c6d4b8"),
            new ColorData("structure_unsaved_structure", "c6d4b8"),
            new ColorData("structure_saved_already_structure", "c6d4b8"),
            new ColorData("structure_unsaved_already_structure", "c6d4b8"),
            new ColorData("structure_pop_not_poppable", "ff8c69"),
            new ColorData("structure_examine_not_examinable", "ff8c69"),
            new ColorData("structure_last_owner_chat", "c6d4b8"),
            new ColorData("structure_last_owner_chat_structure", "e6e3d5"),

            // Clear
            new ColorData("clear_not_enough_args", "ff8c69"),
            new ColorData("clear_inventory_console_identity", "ff8c69"),
            new ColorData("clear_inventory_player_not_found", "ff8c69"),
            new ColorData("clear_inventory_player_not_found_player", "8ce4ff"),
            new ColorData("clear_inventory_self", "e6e3d5"),
            new ColorData("clear_inventory_others", "e6e3d5"),
            new ColorData("clear_inventory_others_name", "8ce4ff"),
            new ColorData("clear_items_cleared", "e6e3d5"),
            new ColorData("clear_structures_cleared", "e6e3d5"),
            new ColorData("clear_vehicles_cleared", "e6e3d5"),
            
            // Shutdown
            new ColorData("shutdown_broadcast_after_game", "00ffff"),
            new ColorData("shutdown_broadcast_after_game_canceled", "00ffff"),
            new ColorData("shutdown_broadcast_after_game_reason", "6699ff"),
            new ColorData("shutdown_broadcast_after_time", "00ffff"),
            new ColorData("shutdown_broadcast_after_time_reason", "6699ff"),
            new ColorData("shutdown_broadcast_after_game_reminder", "00ffff"),
            new ColorData("shutdown_broadcast_after_game_reminder_reason", "6699ff"),

            // Restrictions
            new ColorData("cant_steal_batteries", "f53b3b"),
            new ColorData("cant_place_structures_on_vehicles", "f53b3b"),
            new ColorData("cant_leave_group", "f53b3b"),
            new ColorData("cant_store_this_item", "f53b3b"),
            new ColorData("cant_store_this_item_item", "e6e3d5"),
        };
        public static readonly List<XPData> DefaultXPData = new List<XPData>
        {
            new XPData(EXPGainType.OFFENCE_KILL, 30),
            new XPData(EXPGainType.DEFENCE_KILL, 15),
            new XPData(EXPGainType.CAPTURE, 500),
            new XPData(EXPGainType.WIN, 800),
            new XPData(EXPGainType.CAPTURE_KILL, 25),
            new XPData(EXPGainType.KILL, 10),
            new XPData(EXPGainType.CAP_INCREASE, 30),
            new XPData(EXPGainType.HOLDING_POINT, 10)
        };
        public static readonly List<CreditsData> DefaultCreditData = new List<CreditsData>
        {
            new CreditsData(ECreditsGainType.CAPTURE, 250),
            new CreditsData(ECreditsGainType.WIN, 600)
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
            new LanguageAliasSet("zh-cn", "Chinese (Simplified)", new List<string> { "chinese", "simplified chinese", "chinese simplified", "simple chinese", "chinese simple", 
                "zh", "zh-s", "s-zh", "zh-hk", "zh-mo", "zh-sg", "中国人", "zhōngguó rén", "zhongguo ren", "简体中文", "jiǎntǐ zhōngwén", "jianti zhongwen", "中国人", "zhōngguó rén", "zhongguo ren",
                "zhs", "szh", "zhhk", "zhmo", "zhsg", }),
            new LanguageAliasSet("zh-tw", "Chinese (Traditional)", new List<string> { "traditional chinese", "chinese traditional",
                "zhtw", "zh-t", "t-zh", "zht", "tzh", "中國傳統的", "zhōngguó chuántǒng de", "zhongguo chuantong de", "繁體中文", "fántǐ zhōngwén", "fanti zhongwen", "中國人" }),
            new LanguageAliasSet("en-gb", "Bri'ish", new List<string> { "british", "great british", "gb", "engb"})
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
            { "CHARGE", "{0} was blown up by {1}'s demolition charge." },
            { "CHARGE_SUICIDE", "{0} was blown up by their own demolition charge." },
            { "FOOD", "{0} starved to death." },
            { "FREEZING", "{0} froze to death." },
            { "GRENADE", "{0} was blown up by {1}'s {3}." },
            { "GRENADE_SUICIDE", "{0} blew themselves up with their {3}." },
            { "GRENADE_SUICIDE_UNKNOWN", "{0} blew themselves up with a grenade." },
            { "GRENADE_UNKNOWN", "{0} was blown up by {1}'s grenade." },
            { "GUN", "{0} was shot by {1} in the {2} with a {3} from {4} away." },
            { "GUN_UNKNOWN", "{0} was shot by {1} in the {2} from {4} away." },
            { "GUN_SUICIDE_UNKNOWN", "{0} shot themselves in the {2}." },
            { "GUN_SUICIDE", "{0} shot themselves with a {3} in the {2}." },
            { "INFECTION", "{0} got infected." },
            { "KILL", "{0} was killed by an admin, {1}." },
            { "KILL_SUICIDE", "{0} killed themselves as an admin." },
            { "LANDMINE", "{0} got blown up by {1}'s {3}." },
            { "LANDMINE_SUICIDE", "{0} blew themselves up with their {3}." },
            { "LANDMINE_SUICIDE_UNKNOWN", "{0} blew themselves up with a landmine." },
            { "LANDMINE_UNKNOWN", "{0} got blown up by {1}'s landmine." },
            { "LANDMINE_TRIGGERED", "{0} got blown up by {1}'s {3} that triggered from {5}." },
            { "LANDMINE_SUICIDE_TRIGGERED", "{0} was blown up by their {3} that triggered from {5}." },
            { "LANDMINE_SUICIDE_UNKNOWN_TRIGGERED", "{0} was blown up by their landmine that triggered from {5}." },
            { "LANDMINE_UNKNOWN_TRIGGERED", "{0} got blown up by {1}'s landmine that triggered from {5}." },
            { "LANDMINE_UNKNOWNKILLER", "{0} got blown up by a {3}." },
            { "LANDMINE_UNKNOWN_UNKNOWNKILLER", "{0} got blown up by a landmine." },
            { "LANDMINE_TRIGGERED_UNKNOWNKILLER", "{0} got blown up by a {3} that triggered from {5}." },
            { "LANDMINE_UNKNOWN_TRIGGERED_UNKNOWNKILLER", "{0} got blown up by a landmine that triggered from {5}." },
            { "MELEE", "{0} was meleed by {1} with a {3} in the {2}." },
            { "MELEE_UNKNOWN", "{0} was meleed by {1} in the {2}." },
            { "MISSILE", "{0} was blown up by {1}'s {3} from {4} away." },
            { "MISSILE_UNKNOWN", "{0} was blown up by {1}'s missile from {4} away." },
            { "MISSILE_SUICIDE_UNKNOWN", "{0} blew themselves up." },
            { "MISSILE_SUICIDE", "{0} blew themselves up with a {3}." },
            { "PUNCH", "{0} was punched by {1}." },
            { "ROADKILL", "{0} was ran over by {1}." },
            { "SENTRY", "{0} was killed by a sentry." },
            { "SHRED", "{0} was shredded by barbed wire." },
            { "SPARK", "{0} was shocked by a mega zombie." },
            { "SPIT", "{0} was killed by a spitter zombie." },
            { "SPLASH", "{0} died to splash damage by {1} with a {3}." },
            { "SPLASH_UNKNOWN", "{0} died to splash damage by {1}." },
            { "SPLASH_SUICIDE_UNKNOWN", "{0} killed theirself with splash damage." },
            { "SPLASH_SUICIDE", "{0} killed theirself with splash damage from a {3}." },
            { "SUICIDE", "{0} committed suicide." },
            { "VEHICLE", "{0} was blown up by {1} with a {3}." },
            { "VEHICLE_SUICIDE", "{0} blew themselves up with a {3}." },
            { "VEHICLE_SUICIDE_UNKNOWN", "{0} blew themselves up with a vehicle." },
            { "VEHICLE_UNKNOWN", "{0} was blown up by {1} with a vehicle." },
            { "VEHICLE_UNKNOWNKILLER", "{0} blown up by a {3}." },
            { "VEHICLE_UNKNOWN_UNKNOWNKILLER", "{0} was blown up by a vehicle." },
            { "WATER", "{0} dehydrated." },
            { "ZOMBIE", "{0} was killed by {1}." },
            { "MAINCAMP", "{0} tried to main-camp {1} from {2} away and died." },
            { "38328", "{0} was blown up by a mortar by {1} from {4} away." },  // 81mm mortar turret item id
            { "38328_SUICIDE", "{0} blew themselves up with a mortar." },       // 81mm mortar turret item id
            { "v38310", "{0} was blown up by {1}'s bradley (brad is uncool)." },
            { "v38310_SUICIDE", "{0} was blown up by their bradley (brad is uncool)." }
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