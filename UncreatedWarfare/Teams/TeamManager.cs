using HarmonyLib;
using Microsoft.EntityFrameworkCore;
using SDG.Framework.Landscapes;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Localization;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Uncreated.Warfare.Teams;

public delegate void PlayerTeamDelegate(UCPlayer player, ulong team);
public static class TeamManager
{
    private static TeamConfig _data;
    private static List<FactionInfo> _factions;
    public const ulong ZombieTeamID = ulong.MaxValue;
    internal static readonly FactionInfo[] DefaultFactions =
    {
        new FactionInfo(FactionInfo.Admins, "Admins", "ADMIN", "Admins", "0099ff", "default")
        {
            PrimaryKey = 1,
            NameTranslations = new TranslationList(4)
            {
                { Languages.Russian, "Администрация" },
                { Languages.ChineseSimplified, "管理员" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.ChineseSimplified, "管理员" }
            },
            TMProSpriteIndex = 0
        },
        new FactionInfo(FactionInfo.USA, "United States", "USA", "USA", "78b2ff", "usunarmed", @"https://i.imgur.com/P4JgkHB.png")
        {
            PrimaryKey = 2,
            Build = "a70978a0b47e4017a0261e676af57042",
            Ammo = "51e1e372bf5341e1b4b16a0eacce37eb",
            FOBRadio = "7715ad81f1e24f60bb8f196dd09bd4ef",
            RallyPoint = "5e1db525179341d3b0c7576876212a81",
            DefaultHat = "0cd25f11b5864c0e99c1ad7ca4f8ad7d",
            DefaultShirt = "ee5ecff41ebd4ee082bea183db01193c",
            DefaultBackpack = "83075cc3512f4f209a0b32d309c22f56",
            DefaultVest = "b5c9c2284ac547b59bad4bf7ad23b602",
            DefaultPants = "ad3740ed150040edafef80594b89357d",
            DefaultGlasses = "588933b9da0043d6896d3f6d3f2105b4",
            DefaultMask = "3a7ff1898393450187e970abfc3efbf1",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Russian, "США" },
                { Languages.Romanian, "Statele Unite ale Americi" },
                { Languages.Swedish, "Förenta Staterna" },
                { Languages.ChineseSimplified, "美利坚合众国" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.ChineseSimplified, "美利坚" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.Russian, "США" },
                { Languages.ChineseSimplified, "美国" }
            },
            TMProSpriteIndex = 1,
            Emoji = "🇺🇸"
        },
        new FactionInfo(FactionInfo.Russia, "Russia", "RU", "Russia", "f53b3b", "ruunarmed", @"https://i.imgur.com/YMWSUZC.png")
        {
            PrimaryKey = 3,
            Build = "6a8b8b3c79604aeea97f53c235947a1f",
            Ammo = "8dd66da5affa480ba324e270e52a46d7",
            FOBRadio = "fb910102ad954169abd4b0cb06a112c8",
            RallyPoint = "0d7895360c80440fbe4a45eba28b2007",
            DefaultHat = "e495734ebe274a0085d8b299b5897cb4",
            DefaultShirt = "f5c88106d5324175815e730b3b1b897e",
            DefaultBackpack = "21f6dd73c756470d9be43aaf694a3632",
            DefaultVest = "8bcb7b352fe841d88cf421f2d7aa760e",
            DefaultPants = "cede4da725eb4749b66b9d138b0e557d",
            DefaultMask = "9d849c3f75ac405ca471fd65af4010b6",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Russian, "РОССИЯ" },
                { Languages.Romanian, "Rusia" },
                { Languages.Swedish, "Ryssland" },
                { Languages.ChineseSimplified, "俄罗斯联邦" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Russian, "РОССИЯ" },
                { Languages.Romanian, "Rusia" },
                { Languages.Swedish, "Ryssland" },
                { Languages.ChineseSimplified, "俄罗斯" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.Russian, "РФ" },
                { Languages.Swedish, "RY" },
                { Languages.ChineseSimplified, "俄国" }
            },
            TMProSpriteIndex = 2,
            Emoji = "🇷🇺"
        },
        new FactionInfo(FactionInfo.MEC, "Middle Eastern Coalition", "MEC", "MEC", "ffcd8c", "meunarmed", @"https://i.imgur.com/rPmpNzz.png")
        {
            PrimaryKey = 4,
            Build = "9c7122f7e70e4a4da26a49b871087f9f",
            Ammo = "bfc9aed75a3245acbfd01bc78fcfc875",
            FOBRadio = "c7754ac78083421da73006b12a56811a",
            RallyPoint = "c03352d9e6bb4e2993917924b604ee76",
            DefaultHat = "f10b4420b7c74fa49e09c69ec27709f6",
            DefaultShirt = "16d972440c704ad284155369cd5f1e13",
            DefaultBackpack = "2f077bfd25074bad9d8e24d5af29fab4",
            DefaultVest = "b9b61f2d8b1d472d8430991e08e9450e",
            DefaultPants = "3c0e787a6f034545800023ac3aa589e4",
            TMProSpriteIndex = 3,
            Emoji = "938653900913901598|938654469518950410",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Coalitia Orientului Mijlociu" },
                { Languages.Swedish, "Mellanöstern Koalition" },
                { Languages.ChineseSimplified, "中东联盟" },
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "MK" },
                { Languages.ChineseSimplified, "中东国" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "MK" },
                { Languages.ChineseSimplified, "中东" }
            },
        },
        new FactionInfo(FactionInfo.Germany, "Germany", "DE", "Germany", "ffcc00", "geunarmed", @"https://i.imgur.com/91Apxc5.png")
        {
            PrimaryKey = 5,
            Build = "35eabf178e4e4d82aac34fcbf8e690e3",
            Ammo = "15857c3f693b4209b7b92a0b8438be34",
            FOBRadio = "439c32cced234f358e101294ea0ce3e4",
            RallyPoint = "49663078b594410b98b8a51e8eff3609",
            DefaultHat = "835dc9e72f46431a9bed591bcbbfb081",
            DefaultShirt = "fc4a2a49f335489a84e294ca03031a82",
            DefaultBackpack = "d77a232ad1fb4cf78dde280fd7c14a0b",
            DefaultVest = "2499cebdfc6646c59103a48f06c4838a",
            DefaultPants = "31ed5cd8918e4693bc7431483b130e05",
            TMProSpriteIndex = 4,
            Emoji = "🇩🇪",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Germania" },
                { Languages.Swedish, "Tyskland" },
                { Languages.ChineseSimplified, "德意志联邦共和国" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Germania" },
                { Languages.Swedish, "Tyskland" },
                { Languages.ChineseSimplified, "德意志国" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "TY" },
                { Languages.ChineseSimplified, "德国" }
            }
        },
        new FactionInfo(FactionInfo.China, "China", "CN", "China", "ee1c25", "chunarmed", @"https://i.imgur.com/Yns89Yk.png")
        {
            PrimaryKey = 6,
            Build = "de7c4cafd0304848a7141e3860b2248a",
            Ammo = "2f3cfa9c6bb645fbab8f49ce556d1a1a",
            FOBRadio = "7bde55f70c494418bdd81926fb7d6359",
            RallyPoint = "7720ced42dba4c1eac16d14453cd8bc4",
            DefaultHat = "4dbefaaad6fd4e728912bd929c16c2c6",
            DefaultShirt = "2c1a9c62b30a49e7bda2ef6a2727eb8c",
            DefaultBackpack = "5ac771b71bb7496bb2042d3e8cc2015c",
            DefaultVest = "b74265e7af1c4d52866907e489206f86",
            DefaultPants = "f3a1a4f1f333486480716c42cd5471e9",
            DefaultMask = "5df6ed112bb7430e86f19c30403ebacb",
            TMProSpriteIndex = 5,
            Emoji = "🇨🇳",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "Kina" },
                { Languages.ChineseSimplified, "中华人民共和国" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "Kina" },
                { Languages.ChineseSimplified, "中国" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "KN" },
                { Languages.ChineseSimplified, "中国" }
            }
        },
        new FactionInfo(FactionInfo.USMC, "US Marine Corps", "USMC", "U.S.M.C.", "004481", null, @"https://i.imgur.com/MO9nPmf.png")
        {
            PrimaryKey = 7,
            DefaultHat = "9b14747d30c94b168898b14b3b03cbdd",
            DefaultShirt = "1d8c612e186b4f1588099c663d9d7a44",
            DefaultBackpack = "7971e03a140149f5bbad7d1c51bc7731",
            DefaultVest = "5a7753b4801948c6b875d6589a2c4398",
            DefaultPants = "1a1c1a0065f64543b069e3784f58d5a7",
            DefaultGlasses = "588933b9da0043d6896d3f6d3f2105b4",
            DefaultMask = "3a7ff1898393450187e970abfc3efbf1",
            TMProSpriteIndex = 6,
            Emoji = "989069549817171978|989032657834885150",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "US Marinkår" },
                { Languages.ChineseSimplified, "美利坚合众国海军陆战队" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "U.S.M." },
                { Languages.ChineseSimplified, "海军陆战队" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "USM" },
                { Languages.ChineseSimplified, "海军陆战队" }
            }
        },
        new FactionInfo(FactionInfo.Soviet, "Soviet", "SOV", "Soviet", "cc0000", null, @"https://i.imgur.com/vk8gBBm.png")
        {
            PrimaryKey = 8,
            DefaultHat = "d8c9b02f6ad74216ae25ddd4a98d721c",
            DefaultShirt = "157148a3ebfb447e948b04cdd83d9335",
            DefaultBackpack = "118c5783814847e7bfe6eac1caa11568",
            DefaultVest = "b9b61f2d8b1d472d8430991e08e9450e",
            DefaultPants = "ef9852b99d9e4591904fb42ab9f46134",
            TMProSpriteIndex = 7,
            Emoji = "989037438972334091|989037438972334091",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Sovietic" },
                { Languages.Swedish, "Sovjet" },
                { Languages.ChineseSimplified, "苏维埃社会主义共和国联盟" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Sovietic" },
                { Languages.Swedish, "Sovjet" },
                { Languages.ChineseSimplified, "苏联" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "SOV" },
                { Languages.ChineseSimplified, "苏联" }
            }
        },
        new FactionInfo(FactionInfo.Poland, "Poland", "PL", "Poland", "dc143c", null, @"https://i.imgur.com/fu3nCS3.png")
        {
            PrimaryKey = 9,
            DefaultHat = "ece14052a9d64994a3ef2ab1dc27a073",
            DefaultShirt = "71d35bb681f34b7196bb0e6685106ec4",
            DefaultBackpack = "90f7aa3817834edd82c6458fffbc2780",
            DefaultVest = "44bc4c4333564c61a2e86bd4c2809203",
            DefaultPants = "bf302a8dda994fc08897ed372d8c8cd7",
            DefaultMask = "9d849c3f75ac405ca471fd65af4010b6",
            TMProSpriteIndex = 8,
            Emoji = "🇵🇱",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Polonia" },
                { Languages.Swedish, "Polen" },
                { Languages.ChineseSimplified, "波兰共和国" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Polonia" },
                { Languages.Swedish, "Polen" },
                { Languages.ChineseSimplified, "波兰" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "PL" },
                { Languages.ChineseSimplified, "波兰" }
            }
        },
        new FactionInfo(FactionInfo.Militia, "Militia", "MIL", "Militia", "526257", null)
        {
            PrimaryKey = 10,
            TMProSpriteIndex = 9,
            NameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Militie" },
                { Languages.Swedish, "Milis" },
                { Languages.ChineseSimplified, "民兵组织" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Militie" },
                { Languages.Swedish, "Milis" },
                { Languages.ChineseSimplified, "民兵" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.ChineseSimplified, "民兵" }
            }
        },
        new FactionInfo(FactionInfo.Israel, "Israel Defense Forces", "IDF", "IDF", "005eb8", null, @"https://i.imgur.com/Wzdspd3.png")
        {
            PrimaryKey = 11,
            DefaultHat = "6fa1828a5db147bca1c598e5b41fa319",
            DefaultShirt = "77dc77768d8f4d6b921bbe9a876432d0",
            DefaultBackpack = "67e14c9892b4459bb0d5b7f394f7f91d",
            DefaultVest = "5fbd2fdc5b454606993afff708244e20",
            DefaultPants = "bc16600f78d248c7b108c912ee6a759f",
            DefaultMask = "9d849c3f75ac405ca471fd65af4010b6",
            TMProSpriteIndex = 10,
            Emoji = "🇮🇱",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Forta de aparare a Israelului" },
                { Languages.Swedish, "Israelsk Försvarsmakt" },
                { Languages.ChineseSimplified, "以色列国防军" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "IDF" },
                { Languages.Swedish, "IF" },
                { Languages.ChineseSimplified, "以色列" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "IF" },
                { Languages.ChineseSimplified, "以色列" }
            }
        },
        new FactionInfo(FactionInfo.France, "France", "FR", "France", "002654", null, @"https://i.imgur.com/TYY0kwp.png")
        {
            PrimaryKey = 12,
            DefaultHat = "b53b694277184045a01ce82c55f81029",
            DefaultShirt = "e301b323c52d4feba57fe31e8dea2bca",
            DefaultBackpack = "a5d911ba6c464f89a9913cf198316c53",
            DefaultVest = "5ead83aa50984bc085e1dcf34afc606c",
            DefaultPants = "af4625a9a5e04aa8b9105e08c869998f",
            TMProSpriteIndex = 11,
            Emoji = "🇫🇷",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Franta" },
                { Languages.Swedish, "Frankrike" },
                { Languages.ChineseSimplified, "法兰西共和国" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Franta" },
                { Languages.Swedish, "Frankrike" },
                { Languages.ChineseSimplified, "法兰西" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "FR" },
                { Languages.ChineseSimplified, "法国" }
            }
        },
        new FactionInfo(FactionInfo.Canada, "Canadian Armed Forces", "CAF", "Canada", "d80621", null, @"https://i.imgur.com/zs81UMe.png")
        {
            PrimaryKey = 13,
            DefaultHat = "6e25bcbc24f047698a26d1da3831068f",
            DefaultShirt = "ae976b9a82ba48a488ae71e4ca3cee55",
            DefaultBackpack = "efb51b45aca34676a5d45ce8f28b7ed7",
            DefaultVest = "4626fb373ab648d0b2a67d3fe58017cc",
            DefaultPants = "573275f5925c452c96805e9fc5e52d37",
            DefaultGlasses = "588933b9da0043d6896d3f6d3f2105b4",
            TMProSpriteIndex = 12,
            Emoji = "🇨🇦",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Forta armata canadiene" },
                { Languages.Swedish, "Kanadas Försvarsmakt" },
                { Languages.ChineseSimplified, "加拿大军队" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Canada" },
                { Languages.Swedish, "Kanada" },
                { Languages.ChineseSimplified, "加拿大" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "KF" },
                { Languages.ChineseSimplified, "加拿大" }
            }
        },
        new FactionInfo(FactionInfo.SouthAfrica, "South Africa", "ZA", "S. Africa", "007749", null, @"https://i.imgur.com/2orfzTh.png")
        {
            PrimaryKey = 14,
            DefaultHat = "1fb9ad79c8d14168bdbcdcb33ed50064",
            DefaultShirt = "760f1e854d904bcf902b42c22015aa2a",
            DefaultBackpack = "0cd247d2c01643e49945ab37b16a6a0a",
            DefaultVest = "060cc097e5a642ff85bedaca7a46c188",
            DefaultPants = "b1ca137776964c1f9bb2cd4f19b4d7b5",
            DefaultMask = "9c2b4e15517e434fac0cf0f4bdf0c278",
            TMProSpriteIndex = 13,
            Emoji = "🇿🇦",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Africa de Sud" },
                { Languages.Swedish, "Sydafrika" },
                { Languages.ChineseSimplified, "南非共和国" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "S. Africa" },
                { Languages.Swedish, "S. Afrika" },
                { Languages.ChineseSimplified, "南非" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.ChineseSimplified, "南非" }
            }
        },
        new FactionInfo(FactionInfo.Mozambique, "Mozambique", "MZ", "Mozambique", "ffd100", null, @"https://i.imgur.com/9nXhlMH.png")
        {
            PrimaryKey = 15,
            DefaultHat = "8f30d92410f94318912b8a09f3ccdb9d",
            DefaultShirt = "b9d5f63ed6f84a5c8c339a86828e0642",
            DefaultBackpack = "68170172cf2a4dff8ecbd83964a0c13f",
            DefaultVest = "5ead83aa50984bc085e1dcf34afc606c",
            DefaultPants = "3f0ad0fd305f4deea96a84d4c9ebaae0",
            TMProSpriteIndex = 14,
            Emoji = "🇲🇿",
            NameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Mozambic" },
                { Languages.Swedish, "Mocambique" },
                { Languages.ChineseSimplified, "莫桑比克共和国" }
            },
            ShortNameTranslations = new TranslationList(4)
            {
                { Languages.Romanian, "Mozambic" },
                { Languages.Swedish, "Mocambique" },
                { Languages.ChineseSimplified, "莫桑比克" }
            },
            AbbreviationTranslations = new TranslationList(4)
            {
                { Languages.Swedish, "MC" },
                { Languages.ChineseSimplified, "莫桑比克" }
            }
        }
    };
    internal static Dictionary<ulong, byte>? PlayerBaseStatus;
    public static ushort Team1Tickets;
    public static ushort Team2Tickets;
    private static SqlItem<Zone>? _t1Main;
    private static SqlItem<Zone>? _t1AMC;
    private static SqlItem<Zone>? _t2Main;
    private static SqlItem<Zone>? _t2AMC;
    private static SqlItem<Zone>? _lobbyZone;
    private static Color? _t1Clr;
    private static Color? _t2Clr;
    private static Color? _t3Clr;
    private static FactionInfo? _t1Faction;
    private static FactionInfo? _t2Faction;
    private static FactionInfo? _t3Faction;
    private static Vector3 _lobbySpawn;
    public static event PlayerTeamDelegate? OnPlayerEnteredMainBase;
    public static event PlayerTeamDelegate? OnPlayerLeftMainBase;
    public const ulong Team1ID = 1;
    public const ulong Team2ID = 2;
    public const ulong AdminID = 3;
    private static IReadOnlyList<FactionInfo> _factionsReadonly;
    public static IReadOnlyList<FactionInfo> Factions => _factionsReadonly ?? throw new NullReferenceException("Factions have not been loaded yet.");
    public static bool FactionsLoaded => _factionsReadonly != null;
    public static TeamConfigData Config => _data.Data;
    public static string Team1Name => Team1Faction.Name;
    public static string Team2Name => Team2Faction.Name;
    public static string AdminName => AdminFaction.Name;
    public static string Team1Code => Team1Faction.Abbreviation;
    public static string Team2Code => Team2Faction.Abbreviation;
    public static string AdminCode => AdminFaction.Abbreviation;
    public static Color Team1Color
    {
        get
        {
            if (_t1Clr.HasValue)
                return _t1Clr.Value;
            _t1Clr = Team1ColorHex.Hex();
            return _t1Clr.Value;
        }
    }
    public static Color Team2Color
    {
        get
        {
            if (_t2Clr.HasValue)
                return _t2Clr.Value;
            _t2Clr = Team2ColorHex.Hex();
            return _t2Clr.Value;
        }
    }
    public static Color AdminColor
    {
        get
        {
            if (_t3Clr.HasValue)
                return _t3Clr.Value;
            _t3Clr = AdminColorHex.Hex();
            return _t3Clr.Value;
        }
    }
    public static Color NeutralColor => UCWarfare.GetColor("neutral");
    public static FactionInfo Team1Faction
    {
        get
        {
            if (_t1Faction is not null)
                return _t1Faction;
            for (int i = 0; i < _factions.Count; ++i)
            {
                if (_factions[i].FactionId.Equals(_data.Data.Team1FactionId.Value))
                {
                    _t1Faction = _factions[i];
                    return _t1Faction;
                }
            }

            throw new Exception("Team 1 Faction not selected.");
        }
    }
    public static FactionInfo Team2Faction
    {
        get
        {
            if (_t2Faction is not null)
                return _t2Faction;
            for (int i = 0; i < _factions.Count; ++i)
            {
                if (_factions[i].FactionId.Equals(_data.Data.Team2FactionId.Value))
                {
                    _t2Faction = _factions[i];
                    return _t2Faction;
                }
            }

            throw new Exception("Team 2 Faction not selected.");
        }
    }
    public static FactionInfo AdminFaction
    {
        get
        {
            if (_t3Faction is not null)
                return _t3Faction;
            for (int i = 0; i < _factions.Count; ++i)
            {
                if (_factions[i].FactionId.Equals(_data.Data.AdminFactionId.Value))
                {
                    _t3Faction = _factions[i];
                    return _t3Faction;
                }
            }

            throw new Exception("Admin Faction not selected.");
        }
    }
    public static string Team1ColorHex => Team1Faction.HexColor;
    public static string Team2ColorHex => Team2Faction.HexColor;
    public static string AdminColorHex => AdminFaction.HexColor;
    public static string NeutralColorHex
    {
        get
        {
            if (Data.Colors != default)
                return UCWarfare.GetColorHex("neutral_color");
            else return "ffffff";
        }
    }
    public static string? Team1UnarmedKit => Team1Faction.UnarmedKit;
    public static string? Team2UnarmedKit => Team2Faction.UnarmedKit;
    public static float Team1SpawnAngle => _data.Data.Team1SpawnYaw;
    public static float Team2SpawnAngle => _data.Data.Team2SpawnYaw;
    public static float LobbySpawnAngle => _data.Data.LobbySpawnpointYaw;
    public static float TeamSwitchCooldown => _data.Data.TeamSwitchCooldown;
    public static string DefaultKit => _data.Data.DefaultKit;
    public static Zone Team1Main
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            if (Level.isLoaded)
            {
                Zone? zone = TryGetTeamZone(1ul, false);
                if (zone is not null)
                    return zone;

                L.LogWarning("There is no defined Team 1 main base. Using default instead.");
                for (int i = 0; i < JSONMethods.DefaultZones.Count; ++i)
                {
                    if (JSONMethods.DefaultZones[i].UseCase == ZoneUseCase.Team1Main)
                    {
                        return JSONMethods.DefaultZones[i].GetZone();
                    }
                }
            }
            else
            {
                L.LogWarning(new StackFrame(1).GetMethod()?.FullDescription() + " trying to get Team1Main before level load.");
            }

            return null!;
        }
    }
    public static Zone Team2Main
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            if (Level.isLoaded)
            {
                Zone? zone = TryGetTeamZone(2ul, false);
                if (zone is not null)
                    return zone;

                L.LogWarning("There is no defined Team 2 main base. Using default instead.");
                for (int i = 0; i < JSONMethods.DefaultZones.Count; ++i)
                {
                    if (JSONMethods.DefaultZones[i].UseCase == ZoneUseCase.Team2Main)
                    {
                        return JSONMethods.DefaultZones[i].GetZone();
                    }
                }
            }
            else
            {
                L.LogWarning(new StackFrame(1).GetMethod()?.FullDescription() + " trying to get Team2Main before level load.");
            }

            return null!;
        }
    }
    public static Zone Team1AMC
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            if (Level.isLoaded)
            {
                Zone? zone = TryGetTeamZone(1ul, true);
                if (zone is not null)
                    return zone;

                L.LogWarning("There is no defined Team 1 AMC. Using default instead.");
                for (int i = 0; i < JSONMethods.DefaultZones.Count; ++i)
                {
                    if (JSONMethods.DefaultZones[i].UseCase == ZoneUseCase.Team1Main)
                    {
                        return JSONMethods.DefaultZones[i].GetZone();
                    }
                }
            }
            else
            {
                L.LogWarning(new StackFrame(1).GetMethod()?.FullDescription() + " trying to get Team1AMC before level load.");
            }

            return null!;
        }
    }
    public static Zone Team2AMC
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            if (Level.isLoaded)
            {
                Zone? zone = TryGetTeamZone(2ul, true);
                if (zone is not null)
                    return zone;

                L.LogWarning("There is no defined Team 2 AMC. Using default instead.");
                for (int i = 0; i < JSONMethods.DefaultZones.Count; ++i)
                {
                    if (JSONMethods.DefaultZones[i].UseCase == ZoneUseCase.Team2Main)
                    {
                        return JSONMethods.DefaultZones[i].GetZone();
                    }
                }
            }
            else
            {
                L.LogWarning(new StackFrame(1).GetMethod()?.FullDescription() + " trying to get Team2AMC before level load.");
            }


            return null!;
        }
    }
    public static Zone LobbyZone
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            if (Level.isLoaded)
            {
                Zone? zone = TryGetLobbyZone();
                if (zone is not null)
                    return zone;

                L.LogWarning("There is no defined Lobby zone. Using default instead.");
                for (int i = 0; i < JSONMethods.DefaultZones.Count; ++i)
                {
                    if (JSONMethods.DefaultZones[i].UseCase == ZoneUseCase.Lobby)
                    {
                        return JSONMethods.DefaultZones[i].GetZone();
                    }
                }
            }
            else
            {
                L.LogWarning(new StackFrame(1).GetMethod()?.FullDescription() + " trying to get LobbyZone before level load.");
            }

            return null!;
        }
    }
    public static Vector3 LobbySpawn
    {
        get
        {
            if (_lobbySpawn == default && (Data.ExtraPoints == null || !Data.ExtraPoints.TryGetValue("lobby_spawn", out _lobbySpawn)))
                _lobbySpawn = JSONMethods.DefaultExtraPoints.FirstOrDefault(x => x.Name == "lobby_spawn").Vector3;
            return _lobbySpawn;
        }
    }
    public static FactionInfo GetFaction(ulong team)
    {
        return team switch
        {
            1 => Team1Faction,
            2 => Team2Faction,
            3 => AdminFaction,
            _ => throw new ArgumentOutOfRangeException(nameof(team))
        };
    }
    public static FactionInfo? GetFactionSafe(ulong team)
    {
        return team switch
        {
            1 => Team1Faction,
            2 => Team2Faction,
            3 => AdminFaction,
            _ => null
        };
    }
    public static FactionInfo? GetFactionInfo(uint? id) => id.HasValue ? GetFactionInfo(id.Value) : null;
    public static FactionInfo? GetFactionInfo(PrimaryKey id)
    {
        uint pk = id.Key;
        if (pk == 0) return null;
        if (_factions.Count > pk && _factions[(int)pk].PrimaryKey.Key == pk)
            return _factions[(int)pk];
        for (int i = 0; i < _factions.Count; ++i)
        {
            if (_factions[i].PrimaryKey.Key == pk)
                return _factions[i];
        }

        return null;
    }
    public static FactionInfo? GetFactionInfo(string id)
    {
        for (int i = 0; i < _factions.Count; ++i)
        {
            if (_factions[i].FactionId.Equals(id, StringComparison.OrdinalIgnoreCase))
                return _factions[i];
        }

        return null;
    }
    public static FactionInfo? GetFactionInfo(Faction? faction)
    {
        if (faction == null) return null;
        if (faction.Key > 0)
        {
            FactionInfo? info = GetFactionInfo(faction.Key);
            if (info != null)
                return info;
        }

        return !string.IsNullOrEmpty(faction.InternalName) ? GetFactionInfo(faction.InternalName) : null;
    }
    public static IEnumerable<UCPlayer> EnumerateTeam(ulong team) => PlayerManager.OnlinePlayers.Where(x => x.GetTeam() == team);
    /// <summary>Advanced search using name, abbreviation, and short name.</summary>
    /// <remarks>Exact matches for Id are prioritized.</remarks>
    public static FactionInfo? FindFactionInfo(string search)
    {
        FactionInfo? faction = GetFactionInfo(search);
        if (faction != null) return faction;
        int index = F.StringIndexOf(_factions, x => x.Name, search);
        if (index != -1) return _factions[index];
        index = F.StringIndexOf(_factions, x => x.Abbreviation, search);
        if (index != -1) return _factions[index];
        index = F.StringIndexOf(_factions, x => x.ShortName, search);
        return index != -1 ? _factions[index] : null;
    }
    public static Zone? TryGetLobbyZone()
    {
        SqlItem<Zone>? lobby = _lobbyZone;
        ZoneUseCase uc = ZoneUseCase.Lobby;
        Zone? item = null;
        if (lobby is not { Item: { } item3 })
        {
            ZoneList? singleton = Data.Singletons.GetSingleton<ZoneList>();
            if (singleton != null)
            {
                singleton.WriteWait();
                try
                {
                    for (int i = 0; i < singleton.Items.Count; ++i)
                    {
                        SqlItem<Zone> s = singleton.Items[i];
                        if (s is { Item: { } item2 } && item2.Data.UseCase == uc)
                        {
                            _lobbyZone = s;
                            item = item2;
                            break;
                        }
                    }
                }
                finally
                {
                    singleton.WriteRelease();
                }
            }
        }
        else item = item3;
        return item;
    }
    public static Zone? TryGetTeamZone(ulong team, bool amc = false)
    {
        if (team is not 1ul and not 2ul)
            return null;

        SqlItem<Zone>? main = amc
            ? (team == 1ul ? _t1AMC : _t2AMC)
            : (team == 1ul ? _t1Main : _t2Main);
        ZoneUseCase uc = amc
            ? (team == 1ul ? ZoneUseCase.Team1MainCampZone : ZoneUseCase.Team2MainCampZone)
            : (team == 1ul ? ZoneUseCase.Team1Main : ZoneUseCase.Team2Main);
        Zone? item = null;
        if (main is not { Item: { } item3 })
        {
            ZoneList? singleton = Data.Singletons.GetSingleton<ZoneList>();
            if (singleton != null)
            {
                singleton.WriteWait();
                try
                {
                    for (int i = 0; i < singleton.Items.Count; ++i)
                    {
                        SqlItem<Zone> s = singleton.Items[i];
                        if (s is { Item: { } item2 } && item2.Data.UseCase == uc)
                        {
                            if (amc)
                            {
                                if (team == 1ul)
                                    _t1AMC = s;
                                else
                                    _t2AMC = s;
                            }
                            else
                            {
                                if (team == 1ul)
                                    _t1Main = s;
                                else
                                    _t2Main = s;
                            }

                            item = item2;
                            break;
                        }
                    }
                }
                finally
                {
                    singleton.WriteRelease();
                }
            }
        }
        else item = item3;
        return item;

    }
    internal static void ResetLocations()
    {
        _t1Main = null;
        _t2Main = null;
        _t1AMC = null;
        _t2AMC = null;
        _lobbyZone = null;
        _lobbySpawn = default;
    }
    internal static void SaveConfig() => _data.Save();
    internal static void OnReloadFlags()
    {
        ResetLocations();

        // cache them all
        _ = LobbyZone;
        _ = Team1Main;
        _ = Team2Main;
        _ = Team1AMC;
        _ = Team2AMC;
    }
    public static Zone? GetMain(ulong team)
    {
        if (!Data.Is<ITeams>(out _))
        {
            return null;
        }

        return team switch
        {
            1 => Team1Main,
            2 => Team2Main,
            _ => null
        };
    }
    public static float GetMainYaw(ulong team)
    {
        if (!Data.Is<ITeams>(out _))
        {
            return 0f;
        }

        return team switch
        {
            1 => Team1SpawnAngle,
            2 => Team2SpawnAngle,
            _ => LobbySpawnAngle
        };
    }
    public static bool JoinTeam(UCPlayer player, ulong team, bool teleport = false, bool announce = false)
    {
        ThreadUtil.assertIsGameThread();
        if (team is 1 or 2)
        {
            GroupInfo? groupInfo = GroupManager.getGroupInfo(new CSteamID(GetGroupID(team)));
            if (groupInfo is not null && player.Player.quests.ServerAssignToGroup(groupInfo.groupID, EPlayerGroupRank.MEMBER, true))
            {
                if (teleport)
                    TeleportToMain(player, team);

                if (announce && UCWarfare.Config.EnablePlayerJoinLeaveTeamMessages)
                {
                    ulong id = player.Steam64;
                    Chat.Broadcast(LanguageSet.Where(x => x.GetTeam() == team && x.Steam64 != id), T.TeamJoinAnnounce, GetFactionSafe(team)!, player);
                }
            }
            else return false;
        }
        else
        {
            if (Data.Gamemode is ITeams { UseTeamSelector: true, TeamSelector: { IsLoaded: true } ts })
            {
                if (ts.IsSelecting(player))
                    return false;
                ts.JoinSelectionMenu(player, TeamSelector.JoinTeamBehavior.KeepTeam);
            }
            else
            {
                if (teleport) TeleportToMain(player, 0ul);
                player.Player.quests.leaveGroup(true);
            }
        }

        return true;
    }
    public static void TeleportToMain(UCPlayer player) => TeleportToMain(player, player.GetTeam());
    public static void TeleportToMain(UCPlayer player, ulong team)
    {
        ThreadUtil.assertIsGameThread();
        Vector3 pos = team switch
        {
            1ul => Team1Main.Spawn3D,
            2ul => Team2Main.Spawn3D,
            _ => LobbySpawn
        };
        float angle = team switch
        {
            1ul => Team1SpawnAngle,
            2ul => Team2SpawnAngle,
            _ => LobbySpawnAngle
        };
        player.Player.teleportToLocationUnsafe(pos, angle);
    }
    public static void CheckGroups()
    {
        object? val = typeof(GroupManager).GetField("knownGroups", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
        if (val is Dictionary<CSteamID, GroupInfo> val2)
        {
            bool ft1 = false, ft2 = false, ft3 = false;
            foreach (KeyValuePair<CSteamID, GroupInfo> kv in val2.ToList())
            {
                if (kv.Key.m_SteamID == Team1ID)
                {
                    ft1 = true;
                    if (kv.Value.name != Team1Name)
                    {
                        L.Log("Renamed T1 group " + kv.Value.name + " to " + Team1Name, ConsoleColor.Magenta);
                        kv.Value.name = Team1Name;
                    }
                }
                else if (kv.Key.m_SteamID == Team2ID)
                {
                    ft2 = true;
                    if (kv.Value.name != Team2Name)
                    {
                        L.Log("Renamed T2 group " + kv.Value.name + " to " + Team2Name, ConsoleColor.Magenta);
                        kv.Value.name = Team2Name;
                    }
                }
                else if (kv.Key.m_SteamID == AdminID)
                {
                    ft3 = true;
                    if (kv.Value.name != AdminName)
                    {
                        L.Log("Renamed Admin group " + kv.Value.name + " to " + AdminName, ConsoleColor.Magenta);
                        kv.Value.name = AdminName;
                    }
                }
                else if (kv.Key.m_SteamID > AdminID || kv.Key.m_SteamID < Team1ID)
                    val2.Remove(kv.Key);
            }

            if (!ft1)
            {
                CSteamID gid = new CSteamID(Team1ID);
                val2.Add(gid, new GroupInfo(gid, Team1Name, 0));
                L.Log("Created group " + Team1ID + ": " + Team1Name + ".", ConsoleColor.Magenta);
            }
            if (!ft2)
            {
                CSteamID gid = new CSteamID(Team2ID);
                val2.Add(gid, new GroupInfo(gid, Team2Name, 0));
                L.Log("Created group " + Team2ID + ": " + Team2Name + ".", ConsoleColor.Magenta);
            }
            if (!ft3)
            {
                CSteamID gid = new CSteamID(AdminID);
                val2.Add(gid, new GroupInfo(gid, AdminName, 0));
                L.Log("Created group " + AdminID + ": " + AdminName + ".", ConsoleColor.Magenta);
            }
            GroupManager.save();
        }
    }
    public static ulong Other(ulong team)
    {
        if (team == 1) return 2;
        else if (team == 2) return 1;
        else return team;
    }
    public static bool IsTeam1(this ulong group) => group == Team1ID;
    public static bool IsTeam2(this ulong group) => group == Team2ID;
    public static bool IsInMain(Player player)
    {
        if (player.life.isDead) return false;
        ulong team = player.GetTeam();
        if (team == 1)
        {
            return Team1Main.IsInside(player.transform.position);
        }
        if (team == 2)
        {
            return Team2Main.IsInside(player.transform.position);
        }
        return false;
    }
    public static bool IsInMain(ulong team, Vector3 position)
    {
        if (team == 1)
            return Team1Main.IsInside(position);
        if (team == 2)
            return Team2Main.IsInside(position);

        return false;
    }
    public static bool IsInMainOrAMC(ulong team, Vector3 position)
    {
        if (team == 1)
            return Team1Main.IsInside(position) || Team1AMC.IsInside(position);
        if (team == 2)
            return Team2Main.IsInside(position) || Team2AMC.IsInside(position);

        return false;
    }
    public static bool IsInMainOrLobby(Player player)
    {
        if (player.life.isDead) return false;
        ulong team = player.GetTeam();
        if (LobbyZone.IsInside(player.transform.position))
            return true;
        if (team == 1)
        {
            return Team1Main.IsInside(player.transform.position);
        }
        if (team == 2)
        {
            return Team2Main.IsInside(player.transform.position);
        }
        return false;
    }
    public static bool IsInAnyMainOrLobby(Player player)
    {
        if (player.life.isDead) return false;
        return LobbyZone.IsInside(player.transform.position) || Team1Main.IsInside(player.transform.position) || Team2Main.IsInside(player.transform.position);
    }
    public static bool IsInAnyMain(Vector3 player)
    {
        return Team1Main.IsInside(player) || Team2Main.IsInside(player);
    }
    public static bool IsInAnyMainOrAMCOrLobby(Vector3 player)
    {
        return LobbyZone.IsInside(player) || Team1Main.IsInside(player) || Team2Main.IsInside(player) || Team1AMC.IsInside(player) || Team2AMC.IsInside(player);
    }
    public static string TranslateName(ulong team, bool colorize = false) => TranslateName(team, (LanguageInfo?)null, colorize);
    public static string TranslateName(ulong team, UCPlayer? player, bool colorize = false) => TranslateName(team, player?.Locale.LanguageInfo, colorize);
    public static string TranslateName(ulong team, LanguageInfo? language, bool colorize = false)
    {
        string uncolorized;
        if (team == 1) uncolorized = Team1Faction.GetName(language);
        else if (team == 2) uncolorized = Team2Faction.GetName(language);
        else if (team == 3) uncolorized = AdminFaction.GetName(language);
        else if (team == 0) uncolorized = T.Neutral.Translate(language);
        else uncolorized = team.ToString(Localization.GetCultureInfo(language));
        if (!colorize) return uncolorized;
        return F.ColorizeName(uncolorized, team);
    }
    public static string TranslateShortName(ulong team, bool colorize = false) => TranslateShortName(team, (LanguageInfo?)null, colorize);
    public static string TranslateShortName(ulong team, UCPlayer? player, bool colorize = false) => TranslateShortName(team, player?.Locale.LanguageInfo, colorize);
    public static string TranslateShortName(ulong team, LanguageInfo? language, bool colorize = false)
    {
        string uncolorized;
        if (team == 1) uncolorized = Team1Faction.GetName(language);
        else if (team == 2) uncolorized = Team2Faction.GetName(language);
        else if (team == 3) uncolorized = AdminFaction.GetName(language);
        else if (team == 0) uncolorized = T.Neutral.Translate(language);
        else uncolorized = team.ToString(Localization.GetCultureInfo(language));
        if (!colorize) return uncolorized;
        return F.ColorizeName(uncolorized, team);
    }
    public static string GetTeamHexColor(ulong team)
    {
        return team switch
        {
            1 => Team1ColorHex,
            2 => Team2ColorHex,
            3 => AdminColorHex,
            _ => NeutralColorHex,
        };
    }
    public static Color GetTeamColor(ulong team)
    {
        return team switch
        {
            1 => Team1Color,
            2 => Team2Color,
            3 => AdminColor,
            _ => NeutralColor,
        };
    }
    public static ulong GetGroupID(ulong team)
    {
        if (team == 1) return Team1ID;
        else if (team == 2) return Team2ID;
        else if (team == 3) return AdminID;
        else return 0;
    }
    public static ulong GetTeamNumber(FactionInfo? faction)
    {
        if (faction is not null)
        {
            if (faction == Team1Faction)
                return 1ul;
            if (faction == Team2Faction)
                return 2ul;
            if (faction == AdminFaction)
                return 3ul;
        }
        return 0ul;
    }
    public static ulong GetTeamNumber(Faction? faction)
    {
        if (faction is not null)
        {
            if (faction.InternalName.Equals(Team1Faction.FactionId, StringComparison.Ordinal))
                return 1ul;
            if (faction.InternalName.Equals(Team2Faction.FactionId, StringComparison.Ordinal))
                return 2ul;
            if (faction.InternalName.Equals(AdminFaction.FactionId, StringComparison.Ordinal))
                return 3ul;
        }
        return 0ul;
    }
    public static bool HasTeam(Player player)
    {
        ulong t = player.GetTeam();
        return t is 1 or 2;
    }
    public static bool IsFriendly(Player player, ulong team) => team is 1 or 2 && player.quests.groupID.m_SteamID == (team == 1 ? Team1ID : Team2ID);
    public static bool CanJoinTeam(UCPlayer player, ulong team)
    {
        ulong cteam = player.GetTeam();
        GetTeamCounts(out int t1, out int t2);
        if (cteam == 1)
            --t1;
        else if (cteam == 2)
            --t2;
        return CanJoinTeam(team, t1, t2);
    }
    public static bool CanJoinTeam(ulong team)
    {
        if (!_data.Data.BalanceTeams)
            return true;
        GetTeamCounts(out int t1, out int t2);
        return CanJoinTeam(team, t1, t2);
    }
    public static void GetTeamCounts(out int t1Count, out int t2Count, bool includeSelectors = true)
    {
        t1Count = 0; t2Count = 0;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            if (includeSelectors && player.TeamSelectorData is { IsSelecting: true })
            {
                ulong team3 = player.TeamSelectorData.SelectedTeam;
                if (team3 == 2)
                    ++t2Count;
                else if (team3 == 1)
                    ++t1Count;
                continue;
            }
            int team2 = player.Player.quests.groupID.m_SteamID.GetTeamByte();
            if (team2 == 2)
                ++t2Count;
            else if (team2 == 1)
                ++t1Count;
        }
    }
    public static bool CanJoinTeam(ulong team, int t1Count, int t2Count)
    {
        if (!_data.Data.BalanceTeams)
            return true;

        if (t1Count == t2Count)
            return true;

        // joining team is at 0
        if (team == 1 && t1Count <= 0 || team == 2 && t2Count <= 0)
            return true;

        // joining team is not zero and other team is zero
        if (team == 2 && t1Count <= 0 && t2Count > 0 || team == 1 && t2Count <= 0 && t1Count > 0)
            return false;

        int maxDiff = Mathf.Max(2, Mathf.CeilToInt((t1Count + t2Count) * 0.10f));

        return team switch
        {
            2 => t2Count - maxDiff <= t1Count,
            1 => t1Count - maxDiff <= t2Count,
            _ => false
        };
    }
    public static void RubberbandPlayer(UCPlayer player, ulong team)
    {
        Zone? main = GetMain(team);
        if (main == null)
            return;

        L.LogDebug($"{player} left main while in staging phase.");
        InteractableVehicle? veh = player.CurrentVehicle;
        if (veh != null)
        {
            player.Player.movement.forceRemoveFromVehicle();
            if (veh.gameObject.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForce(-rb.velocity * 4 + Vector3.one);
            }
        }

        Vector3 pos = main.GetClosestPointOnBorder(player.Position);
        Vector3 pos2 = 2 * (pos - player.Position) + player.Position;
        if (!main.IsInside(pos2))
            pos2 = pos;
        Landscape.getWorldHeight(pos2, out float height);
        height += 0.01f;

        if (pos2.y < height)
            pos2.y = height;

        player.Player.teleportToLocationUnsafe(pos2, player.Yaw);
    }
    public static bool InMainCached(UCPlayer player) => PlayerBaseStatus != null && PlayerBaseStatus.TryGetValue(player.Steam64, out byte team) && team != 0;
    public static void EvaluateBases()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        PlayerBaseStatus ??= new Dictionary<ulong, byte>();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            Vector3 pos = pl.Position;
            if (Team1Main.IsInside(pos))
            {
                if (PlayerBaseStatus.TryGetValue(pl.Steam64, out byte x))
                {
                    if (x != 1)
                    {
                        PlayerBaseStatus[pl.Steam64] = 1;
                        InvokeOnLeftMain(pl, x);
                        InvokeOnEnterMain(pl, 1ul);
                    }
                }
                else
                {
                    PlayerBaseStatus.Add(pl.Steam64, 1);
                    InvokeOnEnterMain(pl, 1ul);
                }
            }
            else if (Team2Main.IsInside(pos))
            {
                if (PlayerBaseStatus.TryGetValue(pl.Steam64, out byte x))
                {
                    if (x != 2)
                    {
                        PlayerBaseStatus[pl.Steam64] = 2;
                        InvokeOnLeftMain(pl, x);
                        InvokeOnEnterMain(pl, 2ul);
                    }
                }
                else
                {
                    PlayerBaseStatus.Add(pl.Steam64, 2);
                    InvokeOnEnterMain(pl, 2ul);
                }
            }
            else if (PlayerBaseStatus.TryGetValue(pl.Steam64, out byte x))
            {
                PlayerBaseStatus.Remove(pl.Steam64);
                InvokeOnLeftMain(pl, x);
            }
        }
    }
    private static void InvokeOnLeftMain(UCPlayer player, ulong team)
    {
        if (Data.Gamemode is not TeamGamemode { State: State.Staging } tg || tg.CanLeaveMainInStaging(team) || player.OnDuty())
        {
            player.SendChat(T.LeftMain, GetFaction(team));
            ActionLog.Add(ActionLogType.LeftMain, "Team: " + TranslateName(player.GetTeam(), (LanguageInfo?)null) + ", Base: " + TranslateName(team, (LanguageInfo?)null) +
                                                  ", Position: " + player.Position.ToString("F0", Data.AdminLocale), player);
        }
        OnPlayerLeftMainBase?.Invoke(player, team);
    }
    private static void InvokeOnEnterMain(UCPlayer player, ulong team)
    {
        if (Data.Gamemode is not TeamGamemode { State: State.Staging } tg || tg.CanLeaveMainInStaging(team) || player.OnDuty())
        {
            player.SendChat(T.EnteredMain, GetFaction(team));
            ActionLog.Add(ActionLogType.EnterMain, "Team: " + TranslateName(player.GetTeam(), (LanguageInfo?)null) + ", Base: " + TranslateName(team, (LanguageInfo?)null) +
                                                   ", Position: " + player.Position.ToString("F0", Data.AdminLocale), player);
        }
        OnPlayerEnteredMainBase?.Invoke(player, team);
    }
    public static float GetAMCDamageMultiplier(ulong team, Vector3 position)
    {
        if (team is not 1ul and not 2ul)
            return 1f;

        Zone? amc = TryGetTeamZone(team, true);
        if (amc == null)
            return 1f;

        Zone? main = TryGetTeamZone(team, false);
        if (main == null)
            return 1f;

        if (!amc.IsInside(position))
            return 1f;
        if (main.IsInside(position))
            return 0f;

        Vector3 mainPt = main.GetClosestPointOnBorder(position);
        Vector3 amcPt = amc.GetClosestPointOnBorder(position);

        double exponent = Gamemode.Config.GeneralAMCDamageMultiplierPower / 2d;

        if (exponent == 0d)
            exponent = 1d;

        float mainDist = (mainPt - position).sqrMagnitude;
        float amcDist = (amcPt - position).sqrMagnitude;

        if (exponent - 1d is > 0.0001 or < -0.0001)
        {
            mainDist = (float)Math.Pow(mainDist, exponent);
            amcDist = (float)Math.Pow(amcDist, exponent);
        }

        return mainDist / (mainDist + amcDist);
    }
    public static float GetAMCDamageMultiplier(ulong team, Vector2 position)
    {
        if (team is not 1ul and not 2ul)
            return 1f;

        Zone? amc = TryGetTeamZone(team, true);
        if (amc == null)
            return 1f;

        Zone? main = TryGetTeamZone(team, false);
        if (main == null)
            return 1f;

        if (!amc.IsInside(position))
            return 1f;
        if (main.IsInside(position))
            return 0f;

        Vector2 mainPt = main.GetClosestPointOnBorder(position);
        Vector2 amcPt = amc.GetClosestPointOnBorder(position);

        double exponent = Gamemode.Config.GeneralAMCDamageMultiplierPower / 2d;

        if (exponent == 0d)
            exponent = 1d;

        float mainDist = (mainPt - position).sqrMagnitude;
        float amcDist = (amcPt - position).sqrMagnitude;

        if (exponent - 1d is > 0.0001 or < -0.0001)
        {
            mainDist = (float)Math.Pow(mainDist, exponent);
            amcDist = (float)Math.Pow(amcDist, exponent);
        }

        return mainDist / (mainDist + amcDist);
    }
    internal static void OnConfigReload()
    {
        _t1Clr = null;
        _t2Clr = null;
        _t3Clr = null;
        _t1Faction = null;
        _t2Faction = null;
        _t3Faction = null;
    }
    internal static void SetupConfig()
    {
        if (_data == null)
            _data = new TeamConfig();
        else
            _data.Reload();
    }

    public static RedirectType GetRedirectInfo(Guid input, out FactionInfo? faction, out string? variant, bool clothingOnly = false)
    {
        FactionInfo team1 = Team1Faction;
        FactionInfo team2 = Team2Faction;
        variant = null;
        for (int i = -2; i < _factions.Count; ++i)
        {
            faction = i == -2 ? team2 : (i == -1 ? team1 : _factions[i]);
            if (i > -1 && (faction == team1 || faction == team2))
                continue;
            if (faction.DefaultBackpack.ValidReference(out Guid guid) && guid == input)
                return RedirectType.Backpack;
            if (faction.DefaultVest.ValidReference(out guid) && guid == input)
                return RedirectType.Vest;
            if (faction.DefaultShirt.ValidReference(out guid) && guid == input)
                return RedirectType.Shirt;
            if (faction.DefaultPants.ValidReference(out guid) && guid == input)
                return RedirectType.Pants;
            if (faction.DefaultHat.ValidReference(out guid) && guid == input)
                return RedirectType.Hat;
            if (faction.DefaultMask.ValidReference(out guid) && guid == input)
                return RedirectType.Mask;
            if (faction.DefaultGlasses.ValidReference(out guid) && guid == input)
                return RedirectType.Glasses;
            if (clothingOnly) continue;
            if (faction.RallyPoint.ValidReference(out guid) && guid == input)
                return RedirectType.RallyPoint;
            if (faction.FOBRadio.ValidReference(out guid) && guid == input)
                return RedirectType.Radio;
            if (faction.Build.ValidReference(out guid) && guid == input)
                return RedirectType.BuildSupply;
            if (faction.Ammo.ValidReference(out guid) && guid == input)
                return RedirectType.AmmoSupply;
        }
        faction = null;
        if (!clothingOnly)
        {
            if (Gamemode.Config.BarricadeAmmoBag.AnyMapsContainGuid(input))
                return RedirectType.AmmoBag;
            if (Gamemode.Config.BarricadeFOBBunkerBase.AnyMapsContainGuid(input))
                return RedirectType.Bunker;
            if (Gamemode.Config.BarricadeFOBBunker.AnyMapsContainGuid(input))
                return RedirectType.BunkerBuilt;
            if (Gamemode.Config.BarricadeAmmoCrateBase.AnyMapsContainGuid(input))
                return RedirectType.AmmoCrate;
            if (Gamemode.Config.BarricadeRepairStationBase.AnyMapsContainGuid(input))
                return RedirectType.RepairStation;
            if (Gamemode.Config.BarricadeAmmoCrate.AnyMapsContainGuid(input))
                return RedirectType.AmmoCrateBuilt;
            if (Gamemode.Config.BarricadeRepairStation.AnyMapsContainGuid(input))
                return RedirectType.RepairStationBuilt;
            if (Gamemode.Config.BarricadeUAV.AnyMapsContainGuid(input))
                return RedirectType.UAV;
            if (Gamemode.Config.BarricadeInsurgencyCache.AnyMapsContainGuid(input))
                return RedirectType.Cache;
            if (Gamemode.Config.ItemEntrenchingTool.AnyMapsContainGuid(input))
                return RedirectType.EntrenchingTool;
            if (Gamemode.Config.ItemLaserDesignator.AnyMapsContainGuid(input))
                return RedirectType.LaserDesignator;
            if (Gamemode.Config.BarricadeFOBRadioDamaged.AnyMapsContainGuid(input))
                return RedirectType.RadioDamaged;
        }
        
        return RedirectType.None;
    }
    public static ItemAsset? GetRedirectInfo(RedirectType type, /* TODO */ string variant, FactionInfo? kitFaction, FactionInfo? requesterTeam, out byte[] state, out byte amount)
    {
        if (requesterTeam == null)
            requesterTeam = kitFaction;
        else if (kitFaction == null)
            kitFaction = requesterTeam;
        state = null!;
        byte amt2 = 0;
        ItemAsset? rtn;
        switch (type)
        {
            case RedirectType.Shirt:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    kitFaction.DefaultShirt.ValidReference(out ItemShirtAsset sasset);
                    if (sasset == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.DefaultShirt.ValidReference(out sasset);
                    rtn = sasset;
                }
                break;
            case RedirectType.Pants:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    kitFaction.DefaultPants.ValidReference(out ItemPantsAsset passet);
                    if (passet == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.DefaultPants.ValidReference(out passet);
                    rtn = passet;
                }
                break;
            case RedirectType.Vest:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    kitFaction.DefaultVest.ValidReference(out ItemVestAsset vasset);
                    if (vasset == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.DefaultVest.ValidReference(out vasset);
                    rtn = vasset;
                }
                break;
            case RedirectType.Backpack:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    kitFaction.DefaultBackpack.ValidReference(out ItemBackpackAsset bkasset);
                    if (bkasset == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.DefaultBackpack.ValidReference(out bkasset);
                    rtn = bkasset;
                }
                break;
            case RedirectType.Glasses:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    kitFaction.DefaultGlasses.ValidReference(out ItemGlassesAsset gasset);
                    if (gasset == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.DefaultGlasses.ValidReference(out gasset);
                    rtn = gasset;
                }
                break;
            case RedirectType.Mask:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    kitFaction.DefaultMask.ValidReference(out ItemMaskAsset masset);
                    if (masset == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.DefaultMask.ValidReference(out masset);
                    rtn = masset;
                }
                break;
            case RedirectType.Hat:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    kitFaction.DefaultHat.ValidReference(out ItemHatAsset hasset);
                    if (hasset == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.DefaultHat.ValidReference(out hasset);
                    rtn = hasset;
                }
                break;
            case RedirectType.BuildSupply:
                if (requesterTeam == null)
                    rtn = null;
                else
                {
                    requesterTeam.Build.ValidReference(out ItemAsset iasset);
                    rtn = iasset;
                }
                break;
            case RedirectType.AmmoSupply:
                if (requesterTeam == null)
                    rtn = null;
                else
                {
                    requesterTeam.Ammo.ValidReference(out ItemAsset iasset);
                    rtn = iasset;
                }
                break;
            case RedirectType.RallyPoint:
                if (requesterTeam == null)
                    rtn = null;
                else
                {
                    requesterTeam.RallyPoint.ValidReference(out ItemBarricadeAsset rasset);
                    rtn = rasset;
                }
                break;
            case RedirectType.Radio:
                if (requesterTeam == null)
                    rtn = null;
                else
                {
                    requesterTeam.FOBRadio.ValidReference(out ItemBarricadeAsset rasset);
                    rtn = rasset;
                }
                break;
            case RedirectType.RadioDamaged:
                rtn = Gamemode.Config.BarricadeFOBRadioDamaged.GetAsset();
                break;
            case RedirectType.AmmoBag:
                rtn = Gamemode.Config.BarricadeAmmoBag.GetAsset();
                break;
            case RedirectType.AmmoCrate:
                rtn = Gamemode.Config.BarricadeAmmoCrateBase.GetAsset();
                break;
            case RedirectType.AmmoCrateBuilt:
                rtn = Gamemode.Config.BarricadeAmmoCrate.GetAsset();
                break;
            case RedirectType.RepairStation:
                rtn = Gamemode.Config.BarricadeRepairStationBase.GetAsset();
                break;
            case RedirectType.RepairStationBuilt:
                rtn = Gamemode.Config.BarricadeRepairStation.GetAsset();
                break;
            case RedirectType.Bunker:
                rtn = Gamemode.Config.BarricadeFOBBunkerBase.GetAsset();
                break;
            case RedirectType.BunkerBuilt:
                rtn = Gamemode.Config.BarricadeFOBBunker.GetAsset();
                break;
            case RedirectType.UAV:
                rtn = Gamemode.Config.BarricadeUAV.GetAsset();
                break;
            case RedirectType.Cache:
                rtn = Gamemode.Config.BarricadeInsurgencyCache.GetAsset();
                break;
            case RedirectType.VehicleBay:
                rtn = Gamemode.Config.StructureVehicleBay.GetAsset();
                break;
            case RedirectType.LaserDesignator:
                rtn = Gamemode.Config.ItemLaserDesignator.GetAsset();
                break;
            case RedirectType.EntrenchingTool:
                rtn = Gamemode.Config.ItemEntrenchingTool.GetAsset();
                break;
            default:
                L.LogWarning("Unknown redirect: " + type + ".");
                goto case RedirectType.None;
            case RedirectType.None:
                rtn = null;
                break;
        }
        if (rtn != null)
        {
            amount = amt2 == 0 ? rtn.amount : amt2;
            state ??= rtn.getState(EItemOrigin.ADMIN);
        }
        else
        {
            state ??= Array.Empty<byte>();
            amount = amt2 == 0 ? (byte)1 : amt2;
        }

        return rtn;
    }
    internal static RedirectType GetClothingRedirect(Guid input, out string? variant, FactionInfo faction)
    {
        RedirectType type = GetRedirectInfo(input, out FactionInfo? foundFaction, out variant, true);
        if (type == RedirectType.None || foundFaction != faction)
            return RedirectType.None;

        return type;
    }

    internal static RedirectType GetItemRedirect(Guid input) => GetRedirectInfo(input, out _, out _, false);
#if DEBUG
    [Obsolete]
    internal static Guid CheckClothingAssetRedirect(Guid input, ulong team)
    {
        if (team is not 1 and not 2) return input;
        if (input == BackpackRedirect)
            GetFaction(team).DefaultBackpack.ValidReference(out input);
        else if (input == ShirtRedirect)
            GetFaction(team).DefaultShirt.ValidReference(out input);
        else if (input == PantsRedirect)
            GetFaction(team).DefaultPants.ValidReference(out input);
        else if (input == VestRedirect)
            GetFaction(team).DefaultVest.ValidReference(out input);

        return input;
    }
    [Obsolete]
    internal static Guid CheckAssetRedirect(Guid input, ulong team)
    {
        if (team is < 1 or > 2) return input;
        if (input == RadioRedirect)
            GetFaction(team).FOBRadio.ValidReference(out input);
        else if (input == RallyPointRedirect)
            GetFaction(team).RallyPoint.ValidReference(out input);
        else if (input == BuildingSuppliesRedirect)
            GetFaction(team).Build.ValidReference(out input);
        else if (input == AmmoSuppliesRedirect)
            GetFaction(team).Ammo.ValidReference(out input);
        return input;
    }
    [Obsolete]
    public static bool GetLegacyRedirect(Guid input, out RedirectType type)
    {
        type = RedirectType.None;
        if (input == RadioRedirect)
            type = RedirectType.Radio;
        else if (input == RallyPointRedirect)
            type = RedirectType.RallyPoint;
        else if (input == BuildingSuppliesRedirect)
            type = RedirectType.BuildSupply;
        else if (input == AmmoSuppliesRedirect)
            type = RedirectType.AmmoSupply;
        else if (input == BackpackRedirect)
            type = RedirectType.Backpack;
        else if (input == ShirtRedirect)
            type = RedirectType.Shirt;
        else if (input == PantsRedirect)
            type = RedirectType.Pants;
        else if (input == VestRedirect)
            type = RedirectType.Vest;
        else return false;

        return true;
    }
    // items
    [Obsolete]
    private static readonly Guid RadioRedirect              = new Guid("dea738f0e4894bd4862fd0c850185a6d");
    [Obsolete]
    private static readonly Guid RallyPointRedirect         = new Guid("60240b23b1604ffbbc1bb3771ea5081f");
    [Obsolete]
    private static readonly Guid BuildingSuppliesRedirect   = new Guid("96e27895c1b34e128121296c14dd9bf5");
    [Obsolete]
    private static readonly Guid AmmoSuppliesRedirect       = new Guid("c4cee82e290b4b26b7a6e2be9cd70df7");

    // clothes
    [Obsolete]
    private static readonly Guid BackpackRedirect           = new Guid("bfc294a392294438b29194abfa9792f9");
    [Obsolete]
    private static readonly Guid ShirtRedirect              = new Guid("bc84a3c778884f38a4804da8ab1ca925");
    [Obsolete]
    private static readonly Guid PantsRedirect              = new Guid("dacac5a5628a44d7b40b16f14be681f4");
    [Obsolete]
    private static readonly Guid VestRedirect               = new Guid("2b22ac1b5de74755a24c2f05219c5e1f");
#endif
    public static Task ReloadFactions(CancellationToken token) => ReloadFactions(WarfareDatabases.Factions, UCWarfare.IsLoaded, token);
    public static Task ReloadFactions(IFactionDbContext db, bool uploadDefaultIfMissing, CancellationToken token)
    {
        if (_factions == null)
        {
            _factions = new List<FactionInfo>(DefaultFactions);
            _factionsReadonly = _factions.AsReadOnly();
        }
        return FactionInfo.DownloadFactions(db, _factions, uploadDefaultIfMissing, token);
    }
    public static void WriteFactionLocalization(LanguageInfo language, string path, bool writeMising)
    {
        using FileStream str = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using StreamWriter writer = new StreamWriter(str, System.Text.Encoding.UTF8);
        writer.WriteLine("# Kit Name Translations");
        writer.WriteLine("#  <br> = new line on signs");
        writer.WriteLine();
        for (int i = 0; i < _factions.Count; i++)
        {
            if (WriteFactionIntl(_factions[i], language, writer, writeMising) && i != _factions.Count - 1)
                writer.WriteLine();
        }
    }
    private static bool WriteFactionIntl(FactionInfo faction, LanguageInfo language, StreamWriter writer, bool writeMising)
    {
        FactionInfo? defaultFaction = Array.Find(DefaultFactions, x => x.PrimaryKey.Key == faction.PrimaryKey.Key);

        GetValue(faction.NameTranslations, defaultFaction?.NameTranslations, out string? nameValue, out bool isNameValueDefault);
        GetValue(faction.ShortNameTranslations, defaultFaction?.ShortNameTranslations, out string? shortNameValue, out bool isShortNameValueDefault);
        GetValue(faction.AbbreviationTranslations, defaultFaction?.AbbreviationTranslations, out string? abbreviationValue, out bool isAbbreviationNameValueDefault);

        if (!writeMising && isNameValueDefault && isShortNameValueDefault && isAbbreviationNameValueDefault)
            return false;

        writer.WriteLine("# " + faction.GetName(null) + " (ID: " + faction.FactionId + ", #" + faction.PrimaryKey.Key.ToString(CultureInfo.InvariantCulture) + ")");
        if (faction.Name != null)
            writer.WriteLine("#  Name:         " + faction.Name);
        if (faction.ShortName != null)
            writer.WriteLine("#  Short Name:   " + faction.ShortName);
        if (faction.Abbreviation != null)
            writer.WriteLine("#  Abbreviation: " + faction.Abbreviation);
        if (!string.IsNullOrEmpty(faction.FlagImageURL))
            writer.WriteLine("#  Flag:         " + faction.FlagImageURL);

        if (writeMising || !isNameValueDefault)
        {
            if (!isNameValueDefault)
                writer.WriteLine("# Default: " + faction.GetName(null));
            writer.WriteLine("Name: " + (nameValue ?? faction.Name ?? defaultFaction?.Name ?? faction.FactionId));
        }
        if (writeMising || !isShortNameValueDefault)
        {
            if (!isShortNameValueDefault)
                writer.WriteLine("# Default: " + faction.GetShortName(null));
            writer.WriteLine("ShortName: " + (shortNameValue ?? faction.ShortName ?? defaultFaction?.ShortName ?? faction.FactionId));
        }
        if (writeMising || !isAbbreviationNameValueDefault)
        {
            if (!isAbbreviationNameValueDefault)
                writer.WriteLine("# Default: " + faction.GetAbbreviation(null));
            writer.WriteLine("Abbreviation: " + (abbreviationValue ?? faction.Abbreviation ?? defaultFaction?.Abbreviation ?? faction.FactionId));
        }
        return true;

        void GetValue(Dictionary<string, string>? loaded, Dictionary<string, string>? @default, out string? value, out bool isDefaultValue)
        {
            value = null;
            if (loaded != null)
            {
                if (loaded.TryGetValue(language.Code, out value))
                    isDefaultValue = language.IsDefault;
                else if (!language.IsDefault && loaded.TryGetValue(L.Default, out value))
                    isDefaultValue = true;
                else if (@default != null && @default.TryGetValue(language.Code, out value))
                    isDefaultValue = language.IsDefault;
                else if (@default != null && !language.IsDefault && @default.TryGetValue(L.Default, out value))
                    isDefaultValue = true;
                else
                {
                    value = faction.Name ?? faction.FactionId;
                    isDefaultValue = true;
                }
            }
            else if (@default != null && @default.TryGetValue(language.Code, out value))
                isDefaultValue = language.IsDefault;
            else if (@default != null && !language.IsDefault && @default.TryGetValue(L.Default, out value))
                isDefaultValue = true;
            else
                isDefaultValue = true;
        }
    }
}
public class FactionInfo : ITranslationArgument, IListItem, ICloneable
{
    public const string UnknownTeamImgURL = @"https://i.imgur.com/z0HE5P3.png";
    public const int FactionIDMaxCharLimit = 16;
    public const int FactionNameMaxCharLimit = 32;
    public const int FactionShortNameMaxCharLimit = 24;
    public const int FactionAbbreviationMaxCharLimit = 6;
    public const int FactionImageLinkMaxCharLimit = 128;

    public const string Admins = "admins";
    public const string USA = "usa";
    public const string Russia = "russia";
    public const string MEC = "mec";
    public const string Germany = "germany";
    public const string China = "china";
    public const string USMC = "usmc";
    public const string Soviet = "soviet";
    public const string Poland = "poland";
    public const string Militia = "militia";
    public const string Israel = "israel";
    public const string France = "france";
    public const string Canada = "canada";
    public const string SouthAfrica = "southafrica";
    public const string Mozambique = "mozambique";

    [Obsolete("Africa was split into individual countries.")]
    public const string LegacyAfrica = "africa";
    
    private string _factionId;
    [JsonPropertyName("displayName")]
    public string Name { get; set; }
    [JsonPropertyName("shortName")]
    public string? ShortName { get; set; }
    [JsonPropertyName("nameLocalization")]
    public TranslationList NameTranslations { get; set; }
    [JsonPropertyName("shortNameLocalization")]
    public TranslationList ShortNameTranslations { get; set; }
    [JsonPropertyName("abbreviationLocalization")]
    public TranslationList AbbreviationTranslations { get; set; }
    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; set; }
    [JsonPropertyName("color")]
    public string HexColor { get; set; }
    [JsonPropertyName("unarmed")]
    public string? UnarmedKit { get; set; }
    [JsonPropertyName("flagImg")]
    public string FlagImageURL { get; set; }
    [JsonPropertyName("ammoSupplies")]
    public JsonAssetReference<ItemAsset>? Ammo { get; set; }
    [JsonPropertyName("buildingSupplies")]
    public JsonAssetReference<ItemAsset>? Build { get; set; }
    [JsonPropertyName("rallyPoint")]
    public JsonAssetReference<ItemBarricadeAsset>? RallyPoint { get; set; }
    [JsonPropertyName("radio")]
    public JsonAssetReference<ItemBarricadeAsset>? FOBRadio { get; set; }
    [JsonPropertyName("defaultBackpacks")]
    public AssetVariantDictionary<ItemBackpackAsset> Backpacks { get; set; }
    [JsonPropertyName("defaultShirts")]
    public AssetVariantDictionary<ItemShirtAsset> Shirts { get; set; }
    [JsonPropertyName("defaultPants")]
    public AssetVariantDictionary<ItemPantsAsset> Pants { get; set; }
    [JsonPropertyName("defaultVests")]
    public AssetVariantDictionary<ItemVestAsset> Vests { get; set; }
    [JsonPropertyName("defaultHats")]
    public AssetVariantDictionary<ItemHatAsset> Hats { get; set; }
    [JsonPropertyName("defaultGlasses")]
    public AssetVariantDictionary<ItemGlassesAsset> Glasses { get; set; }
    [JsonPropertyName("defaultMasks")]
    public AssetVariantDictionary<ItemMaskAsset> Masks { get; set; }
    [JsonPropertyName("tmProSpriteIndex")]
    public int? TMProSpriteIndex { get; set; }
    [JsonPropertyName("emoji")]
    public string? Emoji { get; set; }
    [JsonIgnore]
    public PrimaryKey PrimaryKey { get; set; }
    [JsonIgnore]
    public string Sprite => "<sprite index=" + (TMProSpriteIndex.HasValue ? TMProSpriteIndex.Value.ToString(Data.AdminLocale) : "0") + ">";
    [JsonPropertyName("factionId")]
    public string FactionId
    {
        get => _factionId;
        set
        {
            if (value.Length > FactionIDMaxCharLimit)
                throw new ArgumentException("Faction ID must be less than " + FactionIDMaxCharLimit + " characters.", "factionId");
            _factionId = value;
        }
    }

    public FactionInfo()
    {
        Backpacks = new AssetVariantDictionary<ItemBackpackAsset>(1);
        Shirts = new AssetVariantDictionary<ItemShirtAsset>(1);
        Pants = new AssetVariantDictionary<ItemPantsAsset>(1);
        Vests = new AssetVariantDictionary<ItemVestAsset>(1);
        Hats = new AssetVariantDictionary<ItemHatAsset>(1);
        Glasses = new AssetVariantDictionary<ItemGlassesAsset>(0);
        Masks = new AssetVariantDictionary<ItemMaskAsset>(0);

        NameTranslations = new TranslationList(4);
        ShortNameTranslations = new TranslationList(4);
        AbbreviationTranslations = new TranslationList(4);
    }

    public FactionInfo(string factionId, string name, string abbreviation, string? shortName, string hexColor, string? unarmedKit, string flagImage = UnknownTeamImgURL)
        : this()
    {
        FactionId = factionId;
        Name = name;
        Abbreviation = abbreviation;
        ShortName = shortName;
        HexColor = hexColor;
        UnarmedKit = unarmedKit;
        FlagImageURL = flagImage;
    }

    public FactionInfo(Faction model)
    {
        FactionId = model.InternalName;
        Name = model.Name;
        Abbreviation = model.Abbreviation;
        ShortName = model.ShortName;
        HexColor = model.HexColor;
        UnarmedKit = model.UnarmedKit;
        FlagImageURL = model.FlagImageUrl;
        Emoji = model.Emoji;
        TMProSpriteIndex = model.SpriteIndex is < 0 ? null : model.SpriteIndex;
        Backpacks = new AssetVariantDictionary<ItemBackpackAsset>(1);
        Shirts = new AssetVariantDictionary<ItemShirtAsset>(1);
        Pants = new AssetVariantDictionary<ItemPantsAsset>(1);
        Vests = new AssetVariantDictionary<ItemVestAsset>(1);
        Hats = new AssetVariantDictionary<ItemHatAsset>(1);
        Glasses = new AssetVariantDictionary<ItemGlassesAsset>(1);
        Masks = new AssetVariantDictionary<ItemMaskAsset>(1);

        NameTranslations = new TranslationList(4);
        ShortNameTranslations = new TranslationList(4);
        AbbreviationTranslations = new TranslationList(4);

        ApplyAssets(model);
        ApplyTranslations(model);
    }
    public void CloneFrom(FactionInfo model)
    {
        Name = model.Name;
        PrimaryKey = model.PrimaryKey;
        Abbreviation = model.Abbreviation;
        ShortName = model.ShortName;
        HexColor = model.HexColor;
        UnarmedKit = model.UnarmedKit;
        FlagImageURL = model.FlagImageURL;
        Emoji = model.Emoji;
        TMProSpriteIndex = model.TMProSpriteIndex;
        Backpacks = model.Backpacks.Clone();
        Shirts = model.Shirts.Clone();
        Pants = model.Pants.Clone();
        Vests = model.Vests.Clone();
        Hats = model.Hats.Clone();
        Glasses = model.Glasses.Clone();
        Masks = model.Masks.Clone();
        Ammo = model.Ammo;
        Build = model.Build;
        RallyPoint = model.RallyPoint;
        FOBRadio = model.FOBRadio;
    }
    internal Faction CreateModel()
    {
        Faction faction = new Faction
        {
            Key = PrimaryKey,
            InternalName = FactionId,
            Name = Name,
            ShortName = ShortName,
            Abbreviation = Abbreviation,
            HexColor = HexColor,
            UnarmedKit = UnarmedKit,
            FlagImageUrl = FlagImageURL,
            Emoji = Emoji,
            SpriteIndex = TMProSpriteIndex,
            Translations = new List<FactionLocalization>(Math.Max(NameTranslations.Count, Math.Max(ShortNameTranslations.Count, AbbreviationTranslations.Count))),
            Assets = new List<FactionAsset>(32)
        };

        foreach (KeyValuePair<string, string> kvp in NameTranslations)
        {
            FactionLocalization? loc = faction.Translations.FirstOrDefault(x => x.Language.Code.Equals(kvp.Key, StringComparison.Ordinal));
            if (Data.LanguageDataStore.GetInfoCached(kvp.Key) is not { Key: not 0 } lang)
                continue;
            if (loc == null)
            {
                loc = new FactionLocalization
                {
                    Name = kvp.Value,
                    Faction = faction,
                    LanguageId = lang.Key
                };
                faction.Translations.Add(loc);
            }
            else loc.Name = kvp.Value;
        }
        foreach (KeyValuePair<string, string> kvp in ShortNameTranslations)
        {
            FactionLocalization? loc = faction.Translations.FirstOrDefault(x => x.Language.Code.Equals(kvp.Key, StringComparison.Ordinal));
            if (Data.LanguageDataStore.GetInfoCached(kvp.Key) is not { Key: not 0 } lang)
                continue;
            if (loc == null)
            {
                loc = new FactionLocalization
                {
                    ShortName = kvp.Value,
                    Faction = faction,
                    LanguageId = lang.Key
                };
                faction.Translations.Add(loc);
            }
            else loc.ShortName = kvp.Value;
        }
        foreach (KeyValuePair<string, string> kvp in AbbreviationTranslations)
        {
            FactionLocalization? loc = faction.Translations.FirstOrDefault(x => x.Language.Code.Equals(kvp.Key, StringComparison.Ordinal));
            if (Data.LanguageDataStore.GetInfoCached(kvp.Key) is not { Key: not 0 } lang)
                continue;
            if (loc == null)
            {
                loc = new FactionLocalization
                {
                    Abbreviation = kvp.Value,
                    Faction = faction,
                    LanguageId = lang.Key
                };
                faction.Translations.Add(loc);
            }
            else loc.Abbreviation = kvp.Value;
        }

        if (Ammo is not null)
        {
            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromJsonAssetReference(Ammo),
                Faction = faction,
                Redirect = RedirectType.AmmoSupply
            });
        }
        if (Build is not null)
        {
            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromJsonAssetReference(Build),
                Faction = faction,
                Redirect = RedirectType.BuildSupply
            });
        }
        if (RallyPoint is not null)
        {
            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromJsonAssetReference(RallyPoint),
                Faction = faction,
                Redirect = RedirectType.RallyPoint
            });
        }
        if (FOBRadio is not null)
        {
            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromJsonAssetReference(FOBRadio),
                Faction = faction,
                Redirect = RedirectType.Radio
            });
        }
        foreach (KeyValuePair<string, JsonAssetReference<ItemBackpackAsset>> backpack in Backpacks)
        {
            if (faction.Assets.Any(x => x.Redirect == RedirectType.Backpack && string.Equals(backpack.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                continue;

            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromJsonAssetReference(backpack.Value),
                Faction = faction,
                Redirect = RedirectType.Backpack,
                VariantKey = backpack.Key.Length == 0 ? null : backpack.Key
            });
        }
        foreach (KeyValuePair<string, JsonAssetReference<ItemShirtAsset>> shirt in Shirts)
        {
            if (faction.Assets.Any(x => x.Redirect == RedirectType.Shirt && string.Equals(shirt.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                continue;

            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromJsonAssetReference(shirt.Value),
                Faction = faction,
                Redirect = RedirectType.Shirt,
                VariantKey = shirt.Key.Length == 0 ? null : shirt.Key
            });
        }
        foreach (KeyValuePair<string, JsonAssetReference<ItemPantsAsset>> pants in Pants)
        {
            if (faction.Assets.Any(x => x.Redirect == RedirectType.Pants && string.Equals(pants.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                continue;

            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromJsonAssetReference(pants.Value),
                Faction = faction,
                Redirect = RedirectType.Pants,
                VariantKey = pants.Key.Length == 0 ? null : pants.Key
            });
        }
        foreach (KeyValuePair<string, JsonAssetReference<ItemVestAsset>> vest in Vests)
        {
            if (faction.Assets.Any(x => x.Redirect == RedirectType.Vest && string.Equals(vest.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                continue;

            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromJsonAssetReference(vest.Value),
                Faction = faction,
                Redirect = RedirectType.Vest,
                VariantKey = vest.Key.Length == 0 ? null : vest.Key
            });
        }
        foreach (KeyValuePair<string, JsonAssetReference<ItemHatAsset>> hat in Hats)
        {
            if (faction.Assets.Any(x => x.Redirect == RedirectType.Hat && string.Equals(hat.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                continue;

            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromJsonAssetReference(hat.Value),
                Faction = faction,
                Redirect = RedirectType.Hat,
                VariantKey = hat.Key.Length == 0 ? null : hat.Key
            });
        }
        foreach (KeyValuePair<string, JsonAssetReference<ItemGlassesAsset>> glasses in Glasses)
        {
            if (faction.Assets.Any(x => x.Redirect == RedirectType.Glasses && string.Equals(glasses.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                continue;

            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromJsonAssetReference(glasses.Value),
                Faction = faction,
                Redirect = RedirectType.Glasses,
                VariantKey = glasses.Key.Length == 0 ? null : glasses.Key
            });
        }
        foreach (KeyValuePair<string, JsonAssetReference<ItemMaskAsset>> mask in Masks)
        {
            if (faction.Assets.Any(x => x.Redirect == RedirectType.Mask && string.Equals(mask.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                continue;

            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromJsonAssetReference(mask.Value),
                Faction = faction,
                Redirect = RedirectType.Mask,
                VariantKey = mask.Key.Length == 0 ? null : mask.Key
            });
        }

        return faction;
    }
    public void ApplyTranslations(Faction model)
    {
        NameTranslations.Clear();
        ShortNameTranslations.Clear();
        AbbreviationTranslations.Clear();
        
        if (model.Translations == null)
            return;

        foreach (FactionLocalization language in model.Translations)
        {
            if (!string.IsNullOrEmpty(language.Name))
                NameTranslations[language.Language.Code] = language.Name;

            if (!string.IsNullOrEmpty(language.ShortName))
                ShortNameTranslations[language.Language.Code] = language.ShortName;

            if (!string.IsNullOrEmpty(language.Abbreviation))
                AbbreviationTranslations[language.Language.Code] = language.Abbreviation;
        }
    }
    public void ApplyAssets(Faction model)
    {
        Backpacks.Clear();
        Shirts.Clear();
        Pants.Clear();
        Vests.Clear();
        Hats.Clear();
        Glasses.Clear();
        Masks.Clear();

        if (model.Assets == null)
        {
            Ammo = null;
            Build = null;
            RallyPoint = null;
            FOBRadio = null;
            return;
        }

        Ammo = FindAsset(model, RedirectType.AmmoSupply)?.Asset.GetJsonAssetReference<ItemAsset>();
        Build = FindAsset(model, RedirectType.BuildSupply)?.Asset.GetJsonAssetReference<ItemAsset>();
        RallyPoint = FindAsset(model, RedirectType.RallyPoint)?.Asset.GetJsonAssetReference<ItemBarricadeAsset>();
        FOBRadio = FindAsset(model, RedirectType.Radio)?.Asset.GetJsonAssetReference<ItemBarricadeAsset>();

        foreach (FactionAsset asset in model.Assets)
        {
            string key = asset.VariantKey ?? string.Empty;
            switch (asset.Redirect)
            {
                case RedirectType.Backpack:
                    Backpacks[key] = asset.Asset.GetJsonAssetReference<ItemBackpackAsset>();
                    break;
                case RedirectType.Shirt:
                    Shirts[key] = asset.Asset.GetJsonAssetReference<ItemShirtAsset>();
                    break;
                case RedirectType.Pants:
                    Pants[key] = asset.Asset.GetJsonAssetReference<ItemPantsAsset>();
                    break;
                case RedirectType.Vest:
                    Vests[key] = asset.Asset.GetJsonAssetReference<ItemVestAsset>();
                    break;
                case RedirectType.Hat:
                    Hats[key] = asset.Asset.GetJsonAssetReference<ItemHatAsset>();
                    break;
                case RedirectType.Glasses:
                    Glasses[key] = asset.Asset.GetJsonAssetReference<ItemGlassesAsset>();
                    break;
                case RedirectType.Mask:
                    Masks[key] = asset.Asset.GetJsonAssetReference<ItemMaskAsset>();
                    break;
            }
        }
    }
    private static FactionAsset? FindAsset(Faction factionInfo, RedirectType redirect)
    {
        return factionInfo.Assets?.FirstOrDefault(x => x.Redirect == redirect);
    }

    [JsonIgnore]
    public JsonAssetReference<ItemBackpackAsset>? DefaultBackpack
    {
        get => Backpacks.Default;
        set => Backpacks.Default = value;
    }
    [JsonIgnore]
    public JsonAssetReference<ItemShirtAsset>? DefaultShirt
    {
        get => Shirts.Default;
        set => Shirts.Default = value;
    }
    [JsonIgnore]
    public JsonAssetReference<ItemPantsAsset>? DefaultPants
    {
        get => Pants.Default;
        set => Pants.Default = value;
    }
    [JsonIgnore]
    public JsonAssetReference<ItemVestAsset>? DefaultVest
    {
        get => Vests.Default;
        set => Vests.Default = value;
    }
    [JsonIgnore]
    public JsonAssetReference<ItemHatAsset>? DefaultHat
    {
        get => Hats.Default;
        set => Hats.Default = value;
    }
    [JsonIgnore]
    public JsonAssetReference<ItemGlassesAsset>? DefaultGlasses
    {
        get => Glasses.Default;
        set => Glasses.Default = value;
    }
    [JsonIgnore]
    public JsonAssetReference<ItemMaskAsset>? DefaultMask
    {
        get => Masks.Default;
        set => Masks.Default = value;
    }

    [FormatDisplay("ID")]
    public const string FormatId = "i";
    [FormatDisplay("Colored ID")]
    public const string FormatColorId = "ic";
    [FormatDisplay("Short Name")]
    public const string FormatShortName = "s";
    [FormatDisplay("Display Name")]
    public const string FormatDisplayName = "d";
    [FormatDisplay("Abbreviation")]
    public const string FormatAbbreviation = "a";
    [FormatDisplay("Colored Short Name")]
    public const string FormatColorShortName = "sc";
    [FormatDisplay("Colored Display Name")]
    public const string FormatColorDisplayName = "dc";
    [FormatDisplay("Colored Abbreviation")]
    public const string FormatColorAbbreviation = "ac";

    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(FormatColorDisplayName, StringComparison.Ordinal))
                return Localization.Colorize(HexColor, GetName(language), flags);
            if (format.Equals(FormatShortName, StringComparison.Ordinal))
                return GetShortName(language);
            if (format.Equals(FormatColorShortName, StringComparison.Ordinal))
                return Localization.Colorize(HexColor, GetShortName(language), flags);
            if (format.Equals(FormatAbbreviation, StringComparison.Ordinal))
                return GetAbbreviation(language);
            if (format.Equals(FormatColorAbbreviation, StringComparison.Ordinal))
                return Localization.Colorize(HexColor, GetAbbreviation(language), flags);
            if (format.Equals(FormatId, StringComparison.Ordinal) ||
                format.Equals(FormatColorId, StringComparison.Ordinal))
            {
                ulong team = 0;
                if (TeamManager.Team1Faction == this)
                    team = 1;
                else if (TeamManager.Team2Faction == this)
                    team = 2;
                else if (TeamManager.AdminFaction == this)
                    team = 3;
                if (format.Equals(FormatId, StringComparison.Ordinal))
                    return team.ToString(culture ?? Data.LocalLocale);

                return Localization.Colorize(HexColor, team.ToString(culture ?? Data.LocalLocale), flags);
            }
        }
        return GetName(language);
    }
    public string GetName(LanguageInfo? language)
    {
        if (language is null || language.IsDefault || NameTranslations is null || !NameTranslations.TryGetValue(language.Code, out string? val))
            return Name;
        return val ?? Name;
    }
    public string GetShortName(LanguageInfo? language)
    {
        if (language is null || language.IsDefault)
            return ShortName ?? Name;
        if (ShortNameTranslations is null || !ShortNameTranslations.TryGetValue(language.Code, out string? val))
        {
            if (NameTranslations is null || !NameTranslations.TryGetValue(language.Code, out val))
                return ShortName ?? Name;
        }
        return val ?? Name;
    }
    public string GetAbbreviation(LanguageInfo? language)
    {
        if (language is null || language.IsDefault || AbbreviationTranslations is null || !AbbreviationTranslations.TryGetValue(language.Code, out string val))
            return Abbreviation;
        return val;
    }
    public object Clone()
    {
        return new FactionInfo(FactionId, Name, Abbreviation, ShortName, HexColor, UnarmedKit, FlagImageURL)
        {
            PrimaryKey = PrimaryKey,
            Ammo = Ammo?.Clone() as JsonAssetReference<ItemAsset>,
            Build = Build?.Clone() as JsonAssetReference<ItemAsset>,
            RallyPoint = RallyPoint?.Clone() as JsonAssetReference<ItemBarricadeAsset>,
            FOBRadio = FOBRadio?.Clone() as JsonAssetReference<ItemBarricadeAsset>,
            DefaultBackpack = DefaultBackpack?.Clone() as JsonAssetReference<ItemBackpackAsset>,
            DefaultShirt = DefaultShirt?.Clone() as JsonAssetReference<ItemShirtAsset>,
            DefaultPants = DefaultPants?.Clone() as JsonAssetReference<ItemPantsAsset>,
            DefaultVest = DefaultVest?.Clone() as JsonAssetReference<ItemVestAsset>,
            DefaultGlasses = DefaultGlasses?.Clone() as JsonAssetReference<ItemGlassesAsset>,
            DefaultMask = DefaultMask?.Clone() as JsonAssetReference<ItemMaskAsset>,
            DefaultHat = DefaultHat?.Clone() as JsonAssetReference<ItemHatAsset>
        };
    }
    public static async Task DownloadFactions(IFactionDbContext db, List<FactionInfo> list, bool uploadDefaultIfMissing, CancellationToken token = default)
    {
        if (UCWarfare.IsLoaded)
            Localization.ClearSection(TranslationSection.Factions);

        List<Faction> factions = await db.Factions
            .Include(x => x.Assets)
            .Include(x => x.Translations).ThenInclude(x => x.Language)
            .ToListAsync(token).ConfigureAwait(false);

        if (factions.Count == 0 && uploadDefaultIfMissing)
        {
            for (int i = 0; i < TeamManager.DefaultFactions.Length; ++i)
            {
                Faction faction = TeamManager.DefaultFactions[i].CreateModel();
                faction.Key = default;
                factions.Add(faction);
            }

            L.LogDebug($"Adding {factions.Count} factions...");
            await db.Factions.AddRangeAsync(factions, token).ConfigureAwait(false);
            await db.SaveChangesAsync(token).ConfigureAwait(false);
        }

        foreach (Faction faction in factions)
        {
            FactionInfo newFaction = new FactionInfo(faction);
            FactionInfo? existing = list.Find(x => x._factionId.Equals(faction.InternalName, StringComparison.Ordinal));
            if (existing != null)
                existing.CloneFrom(newFaction);
            else
                list.Add(newFaction);

            if (UCWarfare.IsLoaded && faction.Translations != null)
            {
                foreach (FactionLocalization local in faction.Translations)
                {
                    if (local.Language.Code.IsDefault())
                        continue;

                    if (Data.LanguageDataStore.GetInfoCached(local.Language.Code) is { } language)
                        language.IncrementSection(TranslationSection.Factions, (local.Name != null ? 1 : 0) + (local.ShortName != null ? 1 : 0) + (local.Abbreviation != null ? 1 : 0));
                }
            }
        }

        if (UCWarfare.IsLoaded)
        {
            Localization.IncrementSection(TranslationSection.Factions, list.Count * 3);
        }
    }

}

public class TeamConfig : Config<TeamConfigData>
{
    public TeamConfig() : base(Warfare.Data.Paths.BaseDirectory, "teams.json", "teams")
    {
    }
    protected override void OnReload()
    {
        TeamManager.OnConfigReload();
    }
}

public class TeamConfigData : JSONConfigData
{
    [JsonPropertyName("t1Faction")]
    public RotatableConfig<string> Team1FactionId { get; set; }
    [JsonPropertyName("t2Faction")]
    public RotatableConfig<string> Team2FactionId { get; set; }
    [JsonPropertyName("adminFaction")]
    public RotatableConfig<string> AdminFactionId { get; set; }

    [JsonPropertyName("defaultkit")]
    public RotatableConfig<string> DefaultKit { get; set; }
    [JsonPropertyName("team1spawnangle")]
    public RotatableConfig<float> Team1SpawnYaw { get; set; }
    [JsonPropertyName("team2spawnangle")]
    public RotatableConfig<float> Team2SpawnYaw { get; set; }
    [JsonPropertyName("lobbyspawnangle")]
    public RotatableConfig<float> LobbySpawnpointYaw { get; set; }

    [JsonPropertyName("team_switch_cooldown")]
    public float TeamSwitchCooldown { get; set; }
    [JsonPropertyName("allowedTeamGap")]
    public float AllowedDifferencePercent { get; set; }
    [JsonPropertyName("balanceTeams")]
    public bool BalanceTeams { get; set; }
    public override void SetDefaults()
    {
        // don't even think about leaking these
        Team1FactionId = new RotatableConfig<string>(FactionInfo.USA, new RotatableDefaults<string>
        {
            { MapScheduler.FoolsRoad,       FactionInfo.USA },
            { MapScheduler.Nuijamaa,        FactionInfo.USA },
            { MapScheduler.GooseBay,        FactionInfo.USA },
            { MapScheduler.GulfOfAqaba,     FactionInfo.USA },
            { MapScheduler.ChangbaiShan,    FactionInfo.Germany },
        });
        Team2FactionId = new RotatableConfig<string>(FactionInfo.Russia, new RotatableDefaults<string>
        {
            { MapScheduler.FoolsRoad,       FactionInfo.Russia },
            { MapScheduler.Nuijamaa,        FactionInfo.Russia },
            { MapScheduler.GooseBay,        FactionInfo.Russia },
            { MapScheduler.GulfOfAqaba,     FactionInfo.MEC },
            { MapScheduler.ChangbaiShan,    FactionInfo.China },
        });
        AdminFactionId = FactionInfo.Admins;
        DefaultKit = "default";
        Team1SpawnYaw = new RotatableConfig<float>(0f, new RotatableDefaults<float>
        {
            { MapScheduler.FoolsRoad,       180f },
            { MapScheduler.Nuijamaa,        0f },
            { MapScheduler.GooseBay,        0f },
            { MapScheduler.GulfOfAqaba,     90f },
            { MapScheduler.ChangbaiShan,    0f },
        });
        Team2SpawnYaw = new RotatableConfig<float>(0f, new RotatableDefaults<float>
        {
            { MapScheduler.FoolsRoad,       180f },
            { MapScheduler.Nuijamaa,        0f },
            { MapScheduler.GooseBay,        0f },
            { MapScheduler.GulfOfAqaba,     90f },
            { MapScheduler.ChangbaiShan,    0f },
        });
        LobbySpawnpointYaw = new RotatableConfig<float>(0f, new RotatableDefaults<float>
        {
            { MapScheduler.FoolsRoad,       0f },
            { MapScheduler.Nuijamaa,        0f },
            { MapScheduler.GooseBay,        0f },
            { MapScheduler.GulfOfAqaba,     90f },
            { MapScheduler.ChangbaiShan,    0f },
        });
        TeamSwitchCooldown = 1200;
        BalanceTeams = true;
    }
}
