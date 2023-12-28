using HarmonyLib;
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
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Localization;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Uncreated.Warfare.Teams;

public delegate void PlayerTeamDelegate(UCPlayer player, ulong team);
public static class TeamManager
{
    private static TeamConfig _data;
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
    private static readonly List<FactionInfo> FactionsIntl = [.. DefaultFactions];
    public static IReadOnlyList<FactionInfo> Factions { get; } = FactionsIntl.AsReadOnly();
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
            lock (FactionsIntl)
            {
                for (int i = 0; i < FactionsIntl.Count; ++i)
                {
                    if (FactionsIntl[i].FactionId.Equals(_data.Data.Team1FactionId.Value))
                    {
                        _t1Faction = FactionsIntl[i];
                        return _t1Faction;
                    }
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

            lock (FactionsIntl)
            {
                for (int i = 0; i < FactionsIntl.Count; ++i)
                {
                    if (FactionsIntl[i].FactionId.Equals(_data.Data.Team2FactionId.Value))
                    {
                        _t2Faction = FactionsIntl[i];
                        return _t2Faction;
                    }
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

            lock (FactionsIntl)
            {
                for (int i = 0; i < FactionsIntl.Count; ++i)
                {
                    if (FactionsIntl[i].FactionId.Equals(_data.Data.AdminFactionId.Value))
                    {
                        _t3Faction = FactionsIntl[i];
                        return _t3Faction;
                    }
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
        lock (FactionsIntl)
        {
            uint pk = id.Key;
            if (pk == 0) return null;
            if (FactionsIntl.Count >= pk && FactionsIntl[(int)pk - 1].PrimaryKey.Key == pk)
                return FactionsIntl[(int)pk - 1];
            for (int i = 0; i < FactionsIntl.Count; ++i)
            {
                if (FactionsIntl[i].PrimaryKey.Key == pk)
                    return FactionsIntl[i];
            }
        }

        return null;
    }
    public static FactionInfo? GetFactionInfo(string id)
    {
        lock (FactionsIntl)
        {
            for (int i = 0; i < FactionsIntl.Count; ++i)
            {
                if (FactionsIntl[i].FactionId.Equals(id, StringComparison.OrdinalIgnoreCase))
                    return FactionsIntl[i];
            }
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
        lock (FactionsIntl)
        {
            int index = F.StringIndexOf(FactionsIntl, x => x.Name, search);
            if (index != -1) return FactionsIntl[index];
            index = F.StringIndexOf(FactionsIntl, x => x.Abbreviation, search);
            if (index != -1) return FactionsIntl[index];
            index = F.StringIndexOf(FactionsIntl, x => x.ShortName, search);
            return index != -1 ? FactionsIntl[index] : null;
        }
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
        lock (FactionsIntl)
        {
            for (int i = -2; i < FactionsIntl.Count; ++i)
            {
                faction = i == -2 ? team2 : (i == -1 ? team1 : FactionsIntl[i]);
                if (i > -1 && (faction == team1 || faction == team2))
                    continue;
                if (faction.Backpacks.TryMatchVariant(input, out variant))
                    return RedirectType.Backpack;
                if (faction.Vests.TryMatchVariant(input, out variant))
                    return RedirectType.Vest;
                if (faction.Shirts.TryMatchVariant(input, out variant))
                    return RedirectType.Shirt;
                if (faction.Pants.TryMatchVariant(input, out variant))
                    return RedirectType.Pants;
                if (faction.Hats.TryMatchVariant(input, out variant))
                    return RedirectType.Hat;
                if (faction.Masks.TryMatchVariant(input, out variant))
                    return RedirectType.Mask;
                if (faction.Glasses.TryMatchVariant(input, out variant))
                    return RedirectType.Glasses;
                if (clothingOnly) continue;
                if (faction.RallyPoint.ValidReference(out Guid guid) && guid == input)
                    return RedirectType.RallyPoint;
                if (faction.FOBRadio.ValidReference(out guid) && guid == input)
                    return RedirectType.Radio;
                if (faction.Build.ValidReference(out guid) && guid == input)
                    return RedirectType.BuildSupply;
                if (faction.Ammo.ValidReference(out guid) && guid == input)
                    return RedirectType.AmmoSupply;
            }
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
                    ItemShirtAsset sasset = kitFaction.Shirts.Resolve(variant);
                    if (sasset == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.Shirts.Resolve(variant);
                    rtn = sasset;
                }
                break;
            case RedirectType.Pants:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    ItemPantsAsset passet = kitFaction.Pants.Resolve(variant);
                    if (passet == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.Pants.Resolve(variant);
                    rtn = passet;
                }
                break;
            case RedirectType.Vest:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    ItemVestAsset vasset = kitFaction.Vests.Resolve(variant);
                    if (vasset == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.Vests.Resolve(variant);
                    rtn = vasset;
                }
                break;
            case RedirectType.Backpack:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    ItemBackpackAsset bkasset = kitFaction.Backpacks.Resolve(variant);
                    if (bkasset == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.Backpacks.Resolve(variant);
                    rtn = bkasset;
                }
                break;
            case RedirectType.Glasses:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    ItemGlassesAsset gasset = kitFaction.Glasses.Resolve(variant);
                    if (gasset == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.Glasses.Resolve(variant);
                    rtn = gasset;
                }
                break;
            case RedirectType.Mask:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    ItemMaskAsset masset = kitFaction.Masks.Resolve(variant);
                    if (masset == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.Masks.Resolve(variant);
                    rtn = masset;
                }
                break;
            case RedirectType.Hat:
                if (kitFaction == null)
                    rtn = null;
                else
                {
                    ItemHatAsset hasset = kitFaction.Hats.Resolve(variant);
                    if (hasset == null && requesterTeam != null && requesterTeam != kitFaction)
                        requesterTeam.Hats.Resolve(variant);
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
    public static async Task ReloadFactions(CancellationToken token)
    {
        await using IFactionDbContext dbContext = new WarfareDbContext();

        await ReloadFactions(dbContext, UCWarfare.IsLoaded, token).ConfigureAwait(false);
    }

    public static Task ReloadFactions(IFactionDbContext db, bool uploadDefaultIfMissing, CancellationToken token)
    {
        lock (FactionsIntl)
            return FactionInfo.DownloadFactions(db, FactionsIntl, uploadDefaultIfMissing, token);
    }
    public static void WriteFactionLocalization(LanguageInfo language, string path, bool writeMising)
    {
        using FileStream str = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using StreamWriter writer = new StreamWriter(str, System.Text.Encoding.UTF8);
        writer.WriteLine("# Kit Name Translations");
        writer.WriteLine("#  <br> = new line on signs");
        writer.WriteLine();
        lock (FactionsIntl)
        {
            for (int i = 0; i < FactionsIntl.Count; i++)
            {
                if (WriteFactionIntl(FactionsIntl[i], language, writer, writeMising) && i != FactionsIntl.Count - 1)
                    writer.WriteLine();
            }
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
