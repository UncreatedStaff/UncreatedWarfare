using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Kits;
using UnityEngine;

namespace Uncreated.Warfare;

partial class JSONMethods
{
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

        // Traits
        { "trait", "99ff99" },
        { "trait_desc", "cccccc" },

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
        new LanguageAliasSet(LanguageAliasSet.ENGLISH, "English", new string[] { "english", "enus", "en", "us", "inglés", "inglesa", "ingles",
            "en-au", "en-bz", "en-ca", "en-cb", "en-ie", "en-jm", "en-nz", "en-ph", "en-tt", "en-za", "en-zw",
            "enau", "enbz", "enca", "encb", "enie", "enjm", "ennz", "enph", "entt", "enza", "enzw" } ),
        new LanguageAliasSet(LanguageAliasSet.RUSSIAN, "Russian", new string[] { "russian", "ruru", "ru", "russia", "cyrillic", "русский", "russkiy", "российский" } ),
        new LanguageAliasSet(LanguageAliasSet.SPANISH, "Spanish", new string[] { "spanish", "español", "española", "espanol", "espanola", "es", "eses",
            "es-ar", "es-bo", "es-cl", "es-co", "es-cr", "es-do", "es-ec", "es-gt", "es-hn", "es-mx", "es-ni", "es-pa", "es-pe", "es-pr", "es-py", "es-sv", "es-uy", "es-ve",
            "esar", "esbo", "escl", "esco", "escr", "esdo", "esec", "esgt", "eshn", "esmx", "esni", "espa", "espe", "espr", "espy", "essv", "esuy", "esve" } ),
        new LanguageAliasSet(LanguageAliasSet.GERMAN, "German", new string[] { "german", "deutsche", "de", "de-at", "de-ch", "de-li", "de-lu", "deat", "dech", "deli", "delu", "dede" } ),
        new LanguageAliasSet(LanguageAliasSet.ARABIC, "Arabic", new string[] { "arabic", "ar", "arab", "عربى", "eurbaa",
            "ar-ae", "ar-bh", "ar-dz", "ar-eg", "ar-iq", "ar-jo", "ar-kw", "ar-lb", "ar-ly", "ar-ma", "ar-om", "ar-qa", "ar-sy", "ar-tn", "ar-ye",
            "arae", "arbh", "ardz", "areg", "ariq", "arjo", "arkw", "arlb", "arly", "arma", "arom", "arqa", "arsy", "artn", "arye"}),
        new LanguageAliasSet(LanguageAliasSet.FRENCH, "French", new string[] { "french", "fr", "française", "français", "francaise", "francais",
            "fr-be", "fr-ca", "fr-ch", "fr-lu", "fr-mc",
            "frbe", "frca", "frch", "frlu", "frmc" }),
        new LanguageAliasSet(LanguageAliasSet.POLISH, "Polish", new string[] { "polish", "plpl", "polskie", "pol", "pl" }),
        new LanguageAliasSet(LanguageAliasSet.CHINESE_SIMPLIFIED, "Chinese (Simplified)", new string[] { "chinese", "simplified chinese", "chinese simplified", "simple chinese", "chinese simple",
            "zh", "zh-s", "s-zh", "zh-hk", "zh-mo", "zh-sg", "中国人", "zhōngguó rén", "zhongguo ren", "简体中文", "jiǎntǐ zhōngwén", "jianti zhongwen", "中国人", "zhōngguó rén", "zhongguo ren",
            "zhs", "szh", "zhhk", "zhmo", "zhsg", }),
        new LanguageAliasSet(LanguageAliasSet.CHINESE_TRADITIONAL, "Chinese (Traditional)", new string[] { "traditional chinese", "chinese traditional",
            "zhtw", "zh-t", "t-zh", "zht", "tzh", "中國傳統的", "zhōngguó chuántǒng de", "zhongguo chuantong de", "繁體中文", "fántǐ zhōngwén", "fanti zhongwen", "中國人" }),
        new LanguageAliasSet(LanguageAliasSet.PORTUGUESE, "Portuguese", new string[] { "portuguese", "pt", "pt-pt", "pt-br", "ptbr", "ptpt", "português", "a língua portuguesa", "o português" }),
        new LanguageAliasSet(LanguageAliasSet.FILIPINO, "Filipino", new string[] { "pilipino", "fil", "pil", "tagalog", "filipino", "tl", "tl-ph", "fil-ph", "pil-ph" }),
        new LanguageAliasSet(LanguageAliasSet.NORWEGIAN, "Norwegian", new string[] { "norwegian", "norway", "bokmål", "bokmal", "norsk", "nb-no", "nb", "no", "nbno" }),
        new LanguageAliasSet(LanguageAliasSet.DUTCH, "Dutch", new string[] { "dutch", "nederlands", "nl-nl", "nl", "dutch", "nlnl" }),
        new LanguageAliasSet(LanguageAliasSet.ROMANIAN, "Romanian", new string[] { "română", "romanian", "ro", "roro", "ro-ro", "romania" })
    };
}