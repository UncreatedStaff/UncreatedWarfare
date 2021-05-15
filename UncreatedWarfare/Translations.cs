using Rocket.API.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Flags;
using UncreatedWarfare.Kits;
using UncreatedWarfare.Stats;

namespace UncreatedWarfare
{
    partial class JSONMethods
    {
        public static readonly List<CallData> DefaultNodeCalls = new List<CallData>
        {
            new CallData(ECall.SEND_PLAYER_LIST, "sendPlayerList"),
            new CallData(ECall.SEND_PLAYER_JOINED, "sendPlayerJoin"),
            new CallData(ECall.SEND_PLAYER_LEFT, "sendPlayerLeave"),
            new CallData(ECall.GET_PLAYER_LIST, "getPlayerList"),
            new CallData(ECall.GET_USERNAME, "getUsername"),
            new CallData(ECall.PING_SERVER, "ping" ),
            new CallData(ECall.SEND_PLAYER_LOCATION_DATA, "sendPlayerLocationData"),
            new CallData(ECall.INVOKE_BAN, "invokeBan"),
            new CallData(ECall.SEND_VEHICLE_DATA, "sendVehicleData"),
            new CallData(ECall.SEND_ITEM_DATA, "sendItemData"),
            new CallData(ECall.SEND_SKIN_DATA, "sendSkinData"),
            new CallData(ECall.REPORT_VEHICLE_ERROR, "reportVehicleError"),
            new CallData(ECall.REPORT_ITEM_ERROR, "reportItemError"),
            new CallData(ECall.REPORT_SKIN_ERROR, "reportSkinError"),
            new CallData(ECall.SEND_UPDATED_USERNAME, "sendUpdatedUsername"),

        };
        public static void CreateDefaultTranslations()
        {
            DefaultTranslations = new Dictionary<string, string>
            {
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
                { "notowned", "This point is owned by <color=#{1}>{0}</color>. Get more players to capture it." },
                { "flag_neutralized", "<color=#{1}>{0}</color> has been neutralized!" },
                { "team_1", "USA" },
                { "team_2", "Russia" },
                { "team_3", "Admin" },
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
                { "not_in_zone", "No flag zone found at position ({0}, {1}, {2}), out of {3} registered flags." },
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
                { "changed_language", "Changed your language to <color=#{1}>{0}</color>" },
                { "change_language_not_needed", "You are already set to <color=#{1}>{0}</color>." },
                { "reset_language", "Reset your language to <color=#{1}>{0}</color>" },
                { "reset_language_how", "Do <color=#{0}>/lang reset</color> to reset back to default language." },
                { "dont_have_language", "We don't have translations for <color=#{1}>{0}</color> yet. If you are fluent and want to help, feel free to ask us about submitting translations." },
                { "reset_language_not_needed", "You are already on the default language: <color=#{1}>{0}</color>." },

                // Kits
                { "kit_created", "<color=#a0ad8e>Created kit: <color=#ffffff>{0}</color></color>" },
                { "kit_overwritten", "<color=#a0ad8e>Overwritten items for kit: <color=#ffffff>{0}</color></color>" },
                { "kit_deleted", "<color=#a0ad8e>Deleted kit: <color=#ffffff>{0}</color></color>" },
                { "kit_setprop", "<color=#a0ad8e>Set {0} for kit <color=#ffffff>{1}</color> to: <color=#8ce4ff>{2}</color></color>" },
                { "kit_accessgiven", "<color=#a0ad8e>Allowed player: <color=#e06969>{0}</color> to access the kit: <color=#ffffff>{1}</color></color>" },
                { "kit_accessremoved", "<color=#a0ad8e>Allowed player: <color=#e06969>{0}</color> to access the kit: <color=#ffffff>{1}</color></color>" },
                { "kit_e_noexist", "<color=#ff8c69>A kit called {0} does not exist.</color>" },
                { "kit_e_invalidprop", "<color=#ff8c69>{0} isn't a valid a kit property. Try putting 'class', 'cost', 'clearinv' etc.</color>" },
                { "kit_e_invalidarg", "<color=#ff8c69>{0} is not a valid value for kit property: {1}</color>" },
                { "kit_e_noplayer", "<color=#ff8c69>No player found by the name of '{0}'.</color>" },
                { "kit_e_alreadyaccess", "<color=#dbc48f>Player {0} already has access to the kit: {1}.</color>" },
                { "kit_e_noaccess", "<color=#dbc48f>Player {0} already does not have access to that: {1}.</color>" },

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
                { "winner", "<color=#{1}>{0}</color>" },
                { "lb_header_1", "Most Kills" },
                { "lb_header_2", "K/D Ratio" },
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
                { "lblCreditsGained", "Credits Gained: " },
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
                { "next_game_start_label", "Next Game Starting In" },
                { "next_game_starting_format", "{0:mm\\:ss}" },

                // SIGNS - must prefix with "sign_" for them to work
                { "sign_test", "<color=#ff00ff>This is the english translation for that sign.</color>" }
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
            new FlagData(7, "Mining", 52.5f, -215, new ZoneData("polygon", "7,-283,-6,-270,-6,-160,7,-147,72,-147,111,-160,111,-257,104,-264,40,-283"), true, 0)
        };
        public static List<FlagData> DefaultExtraZones = new List<FlagData>
        {
            new FlagData(-69, "Lobby", 713.1f, -991, new ZoneData("rectangle", "12.2,12"), false, 0),
            new FlagData(-1, "USMain", 823, -880.5f, new ZoneData("rectangle", "120,189"), true, 0),
            new FlagData(-101, "USAMC", 717.5f, -697.5f, new ZoneData("rectangle", "613,653"), true, 0),
            new FlagData(-2, "RUMain", -823, 876.5f, new ZoneData("rectangle", "120,189"), true, 0),
            new FlagData(-102, "RUAMC", -799, 744.5f, new ZoneData("rectangle", "450,559"), true, 0),
        };
        public static List<Point3D> DefaultExtraPoints = new List<Point3D>
        {
            new Point3D("lobby_spawn", 713.1f, 39f, -991)
        };
        public static readonly List<ColorData> DefaultColors = new List<ColorData>
        {
            new ColorData("default", "ffffff"),
            new ColorData("defaulterror", "ff0000"),
            new ColorData("join_message_background", "e6e3d5"),
            new ColorData("join_message_name", "ffff1a"),
            new ColorData("leave_message_background", "e6e3d5"),
            new ColorData("leave_message_name", "ffff1a"),

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
            new ColorData("nocap_team_1", "ff0000"),
            new ColorData("notowned_team_1", "ff0000"),

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
            new ColorData("nocap_team_1_words", "ff0000"),
            new ColorData("notowned_team_1_words", "ff0000"),

            // Team 2 Circle
            new ColorData("capturing_team_2", "f53b3b"),
            new ColorData("losing_team_2", "4785ff"),
            new ColorData("clearing_team_2", "f53b3b"),
            new ColorData("contested_team_2", "ffff1a"),
            new ColorData("secured_team_2", "00ff00"),
            new ColorData("nocap_team_2", "ff0000"),
            new ColorData("notowned_team_2", "ff0000"),

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
            new ColorData("nocap_team_2_words", "ff0000"),
            new ColorData("notowned_team_2_words", "ff0000"),

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
            new ColorData("cant_create_group", "ff0000"),
            new ColorData("rename_not_in_group", "ff0000"),
            new ColorData("group_not_found", "ff0000"),
            new ColorData("renamed_group", "e6e3d5"),
            new ColorData("renamed_group_already_named_that", "ff0000"),
            new ColorData("renamed_group_id", "4785ff"),
            new ColorData("renamed_group_old_name", "f53b3b"),
            new ColorData("renamed_group_new_name", "4785ff"),
            new ColorData("joined_group", "e6e3d5"),
            new ColorData("joined_already_in_group", "ff0000"),
            new ColorData("joined_group_not_found", "ff0000"),
            new ColorData("joined_group_not_found_group_id", "4785ff"),
            new ColorData("joined_group_id", "4785ff"),
            new ColorData("joined_group_name", "4785ff"),
            new ColorData("deleted_group", "e6e3d5"),
            new ColorData("deleted_group_id", "4785ff"),
            new ColorData("deleted_group_name", "4785ff"),
            new ColorData("join_not_in_lobby", "ff0000"),
            new ColorData("join_not_in_lobby_command", "e6e3d5"),
            new ColorData("joined_team_must_rejoin", "e6e3d5"),
            new ColorData("joined_team", "e6e3d5"),
            new ColorData("join_already_in_team", "f53b3b"),
            new ColorData("join_auto_balance_cant_switch", "f53b3b"),
            new ColorData("join_auto_balance_cant_switch_queue_command", "e6e3d5"),
            new ColorData("join_group_has_no_space", "f53b3b"),
            new ColorData("join_group_not_found", "f53b3b"),
            new ColorData("join_group_not_found_group_id", "4785ff"),
            new ColorData("from_lobby_teleport_failed", "ff0000"),
            new ColorData("from_lobby_teleport_failed_command", "4785ff"),
            new ColorData("no_permissions", "ff0000"),

            // Lang Command
            new ColorData("language_list", "f53b3b"),
            new ColorData("language_list_list", "e6e3d5"),
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
            new ColorData("dont_have_language_language", "e6e3d5")
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
            new MySqlTableData("xp", "xp", new List<MySqlColumnData> {
                new MySqlColumnData("Steam64","Steam64"),
                new MySqlColumnData("Team","Team"),
                new MySqlColumnData("Username","Username"),
                new MySqlColumnData("Balance","Balance")
            }),
            new MySqlTableData("credits", "credits", new List<MySqlColumnData> {
                new MySqlColumnData("Steam64","Steam64"),
                new MySqlColumnData("Team","Team"),
                new MySqlColumnData("Username","Username"),
                new MySqlColumnData("Balance","Balance")
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
        /*
        public static readonly List<TeamData> DefaultTeamData = new List<TeamData>
        {
            new TeamData(1, "US", new List<ulong>(), 777.5f, 33.5f, -800f),
            new TeamData(2, "Russia", new List<ulong>(), -782f, 50f, 850f),
            new TeamData(3, "Admins", new List<ulong>(), 719f, 39f, -1017f) //admin group for structures.
        };
        */
        public static List<Kit> DefaultKits = new List<Kit>
        {
            new Kit("usrif1", new List<KitItem> {
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
                Class = Kit.EClass.AUTOMATIC_RIFLEMAN,
                Branch = Kit.EBranch.INFANTRY
            },
            new Kit("rurif1", new List<KitItem> {
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
                Class = Kit.EClass.AUTOMATIC_RIFLEMAN,
                Branch = Kit.EBranch.INFANTRY
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

    }
}
