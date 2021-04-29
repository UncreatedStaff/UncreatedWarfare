using Rocket.API.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Flags;

namespace UncreatedWarfare
{
    partial class JSONMethods
    {
        public static readonly Dictionary<string, string> DefaultTranslations = new Dictionary<string, string>
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
            { "flag_neutralized", "<color=#{1}>{0}</color> has been neutralized!" },
            { "team_1", "USA" },
            { "team_2", "Russia" },
            { "ui_capturing", "CAPTURING" },
            { "ui_losing", "LOSING" },
            { "ui_clearing", "CLEARING" },
            { "ui_contested", "CONTESTED" },
            { "ui_secured", "SECURED" },
            { "ui_nocap", "NOT OBJECTIVE" },
            { "current_zone", "You are in flag zone: {0}, at position ({1}, {2}, {3})." },
            { "not_in_zone", "No flag zone found at position ({0}, {1}, {2}), out of {3} registered flags." },
            { "player_connected", "<color=#{1}>{0}</color> joined the server!" },
            { "player_disconnected", "<color=#{1}>{0}</color> left the server." },

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
        };
        public static readonly List<FlagData> DefaultFlags = new List<FlagData>
        {
            new FlagData(1, "AmmoHill", -89, 297, new ZoneData("rectangle", "86,68")),
            new FlagData(2, "Hilltop", 258, 497, new ZoneData("rectangle", "66,72")),
            new FlagData(3, "Papanov", 754, 759, new ZoneData("polygon", "635,738,713,873,873,780,796,645")),
            new FlagData(4, "Verto", 624, 469, new ZoneData("polygon", "500,446,514,527,710,492,748,466,710,411")),
            new FlagData(5, "Hill123", 631, 139, new ZoneData("rectangle", "44,86")),
            new FlagData(6, "Hill13", 338, -15, new ZoneData("circle", "35")),
            new FlagData(7, "Mining", 52.5f, -215, new ZoneData("polygon", "7,-283,-6,-270,-6,-160,7,-147,72,-147,111,-160,111,-257,104,-264,40,-283"))
        };
        public static readonly List<ColorData> DefaultColors = new List<ColorData>
        {
            new ColorData("default", "ffffff"),
            new ColorData("join_message_background", "e6e3d5"),
            new ColorData("join_message_name", "ffff1a"),
            new ColorData("leave_message_background", "e6e3d5"),
            new ColorData("leave_message_name", "ffff1a"),

            // Team Colors
            new ColorData("team_1_color", "4785ff"),
            new ColorData("team_2_color", "f53b3b"),
            new ColorData("neutral_color", "c2c2c2"),

            // Team 1 Circle
            new ColorData("capturing_team_1", "4785ff"),
            new ColorData("losing_team_1", "f53b3b"),
            new ColorData("clearing_team_1", "4785ff"),
            new ColorData("contested_team_1", "ffff1a"),
            new ColorData("secured_team_1", "00ff00"),
            new ColorData("nocap_team_1", "ff0000"),

            // Team 1 Background Circle
            new ColorData("capturing_team_1_bkgr", "002266"),
            new ColorData("losing_team_1_bkgr", "610505"),
            new ColorData("clearing_team_1_bkgr", "002266"),
            new ColorData("contested_team_1_bkgr", "666600"),
            new ColorData("secured_team_1_bkgr", "006600"),
            new ColorData("nocap_team_1_bkgr", "660000"),

            // Team 1 Words
            new ColorData("capturing_team_1_words", "4785ff"),
            new ColorData("losing_team_1_words", "f53b3b"),
            new ColorData("clearing_team_1_words", "4785ff"),
            new ColorData("contested_team_1_words", "ffff1a"),
            new ColorData("secured_team_1_words", "00ff00"),
            new ColorData("nocap_team_1_words", "ff0000"),
            new ColorData("entered_cap_radius", "e6e3d5"),

            // Team 2 Circle
            new ColorData("capturing_team_2", "f53b3b"),
            new ColorData("losing_team_2", "4785ff"),
            new ColorData("clearing_team_2", "f53b3b"),
            new ColorData("contested_team_2", "ffff1a"),
            new ColorData("secured_team_2", "00ff00"),
            new ColorData("nocap_team_2", "ff0000"),

            // Team 2 Background Circle
            new ColorData("capturing_team_2_bkgr", "610505"),
            new ColorData("losing_team_2_bkgr", "002266"),
            new ColorData("clearing_team_2_bkgr", "610505"),
            new ColorData("contested_team_2_bkgr", "666600"),
            new ColorData("secured_team_2_bkgr", "006600"),
            new ColorData("nocap_team_2_bkgr", "660000"),

            // Team 2 Words
            new ColorData("capturing_team_2_words", "f53b3b"),
            new ColorData("losing_team_2_words", "4785ff"),
            new ColorData("clearing_team_2_words", "f53b3b"),
            new ColorData("contested_team_2_words", "ffff1a"),
            new ColorData("secured_team_2_words", "00ff00"),
            new ColorData("nocap_team_2_words", "ff0000"),

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

            // Team 2 Chat
            new ColorData("capturing_team_2_chat", "e6e3d5"),
            new ColorData("losing_team_2_chat", "e6e3d5"),
            new ColorData("clearing_team_2_chat", "e6e3d5"),
            new ColorData("contested_team_2_chat", "e6e3d5"),
            new ColorData("secured_team_2_chat", "e6e3d5"),
            new ColorData("nocap_team_2_chat", "e6e3d5"),

            // Other Flag Chats
            new ColorData("flag_neutralized", "e6e3d5")

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
    }
}
