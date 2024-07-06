using System.Collections.Generic;

namespace Uncreated.Warfare;

partial class JSONMethods
{
    private const string Team1ColorPlaceholder = "%t1%";
    private const string Team2ColorPlaceholder = "%t2%";
    private const string Team3ColorPlaceholder = "%t3%";
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
        { "team_count_ui_color_team_1_icon", Team1ColorPlaceholder },
        { "team_count_ui_color_team_2_icon", Team2ColorPlaceholder },
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
        { "points", "f0a31c" },
        { "commander", "f0a31c" },

        // capture ui
        { "contested", "ffdc8a" },
        { "secured", "80ff80" },
        { "neutral", "b5b5b5" },
        { "nocap", "8c8582" },
        { "locked", "8c8582" },
        { "invehicle", "8c8582" },

        // Other Flag Chats
        { "flag_neutralized", "e6e3d5" },
        { "team_win", "e6e3d5" },
        { "team_capture", "e6e3d5" },

        // FOBs
        { "build", "d1c597" },
        { "ammo", "d97568" },

        // Deaths
        { "death_background", "ffffff" },
        { "death_background_teamkill", "ff9999" },

        // Traits
        { "trait", "99ff99" },
        { "trait_desc", "cccccc" },

        // Request
        { "kit_public_header", "ffffff" },
        { "kit_public_header_fav", "ffff99" },
        { "kit_public_commander_header", "f0a31c" },
        { "kit_level_available", "ff974d" },
        { "kit_level_unavailable", "917663" },
        { "kit_level_dollars", "7878ff" },
        { "kit_free", "66ffcc" },
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
}