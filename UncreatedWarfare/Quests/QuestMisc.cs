using SDG.Framework.Utilities;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests.Types;
using Uncreated.Warfare.Squads;
using UnityEngine;

namespace Uncreated.Warfare.Quests;

[Translatable("Weapon Class", IsPrioritizedTranslation = false)]
public enum WeaponClass : byte
{
    Unknown,
    [Translatable(LanguageAliasSet.CHINESE_SIMPLIFIED, "突击步枪")]
    AssaultRifle,
    [Translatable(LanguageAliasSet.CHINESE_SIMPLIFIED, "战斗步枪")]
    BattleRifle,
    MarksmanRifle,
    [Translatable(LanguageAliasSet.CHINESE_SIMPLIFIED, "狙击步枪")]
    SniperRifle,
    [Translatable(LanguageAliasSet.CHINESE_SIMPLIFIED, "机枪")]
    MachineGun,
    [Translatable(LanguageAliasSet.CHINESE_SIMPLIFIED, "手枪")]
    Pistol,
    [Translatable(LanguageAliasSet.CHINESE_SIMPLIFIED, "霰弹枪")]
    Shotgun,
    [Translatable(LanguageAliasSet.CHINESE_SIMPLIFIED, "火箭筒")]
    [Translatable("Rocket Launcher")]
    Rocket,
    [Translatable(LanguageAliasSet.CHINESE_SIMPLIFIED, "冲锋枪")]
    [Translatable("SMG")]
    SMG
}
[Translatable("Quest Type", Description = "Display names of quests.", IsPrioritizedTranslation = false)]
public enum QuestType : byte
{
    Invalid,
    /// <summary><see cref="KillEnemiesQuest"/></summary>
    KillEnemies,
    /// <summary><see cref="KillEnemiesQuestWeapon"/></summary>
    KillEnemiesWithWeapon,
    /// <summary><see cref="KillEnemiesQuestKit"/></summary>
    KillEnemiesWithKit,
    /// <summary><see cref="KillEnemiesQuestKitClass"/></summary>
    KillEnemiesWithKitClass,
    /// <summary><see cref="KillEnemiesQuestWeaponClass"/></summary>
    KillEnemiesWithWeaponClass,
    /// <summary><see cref="KillEnemiesQuestBranch"/></summary>
    KillEnemiesWithBranch,
    /// <summary><see cref="KillEnemiesQuestTurret"/></summary>
    KillEnemiesWithTurret,
    /// <summary><see cref="KillEnemiesQuestEmplacement"/></summary>
    KillEnemiesWithEmplacement,
    /// <summary><see cref="KillEnemiesQuestSquad"/></summary>
    KillEnemiesInSquad,
    /// <summary><see cref="KillEnemiesQuestFullSquad"/></summary>
    KillEnemiesInFullSquad,
    /// <summary><see cref="KillEnemiesQuestDefense"/></summary>
    [Translatable("Kill Enemies While Defending Point")]
    KillEnemiesOnPointDefense,
    /// <summary><see cref="KillEnemiesQuestAttack"/></summary>
    [Translatable("Kill Enemies While Attacking Point")]
    KillEnemiesOnPointAttack,
    /// <summary><see cref="HelpBuildQuest"/></summary>
    ShovelBuildables,
    /// <summary><see cref="BuildFOBsQuest"/></summary>
    [Translatable("Build FOBs")]
    BuildFOBs,
    /// <summary><see cref="BuildFOBsNearObjQuest"/></summary>
    [Translatable("Build FOBs Near Objectives")]
    BuildFOBsNearObjectives,
    /// <summary><see cref="BuildFOBsOnObjQuest"/></summary>
    [Translatable("Build FOBs Near Current Objective")]
    BuildFOBOnActiveObjective,
    /// <summary><see cref="DeliverSuppliesQuest"/></summary>
    DeliverSupplies,
    /// <summary><see cref="CaptureObjectivesQuest"/></summary>
    CaptureObjectives,
    /// <summary><see cref="DestroyVehiclesQuest"/></summary>
    DestroyVehicles,
    /// <summary><see cref="DriveDistanceQuest"/></summary>
    DriveDistance,
    /// <summary><see cref="TransportPlayersQuest"/></summary>
    TransportPlayers,
    /// <summary><see cref="RevivePlayersQuest"/></summary>
    RevivePlayers,
    /// <summary><see cref="KingSlayerQuest"/></summary>
    [Translatable("King-slayer")]
    KingSlayer,
    /// <summary><see cref="KillStreakQuest"/></summary>
    [Translatable("Killstreak")]
    KillStreak,
    /// <summary><see cref="XPInGamemodeQuest"/></summary>
    [Translatable("Earn XP From Gamemode")]
    XPInGamemode,
    /// <summary><see cref="KillEnemiesRangeQuest"/></summary>
    [Translatable("Kill From Distance")]
    KillFromRange,
    /// <summary><see cref="KillEnemiesRangeQuestWeapon"/></summary>
    [Translatable("Kill From Distance With Weapon")]
    KillFromRangeWithWeapon,
    /// <summary><see cref="KillEnemiesQuestKitClassRange"/></summary>
    [Translatable("Kill From Distance With Class")]
    KillFromRangeWithClass,
    /// <summary><see cref="KillEnemiesQuestKitRange"/></summary>
    [Translatable("Kill From Distance With Kit")]
    KillFromRangeWithKit,
    /// <summary><see cref="RallyUseQuest"/></summary>
    [Translatable("Teammates Use Rallypoint")]
    TeammatesDeployOnRally,
    /// <summary><see cref="FOBUseQuest"/></summary>
    [Translatable("Teammates Use FOB")]
    TeammatesDeployOnFOB,
    /// <summary><see cref="NeutralizeFlagsQuest"/></summary>
    NeutralizeFlags,
    /// <summary><see cref="WinGamemodeQuest"/></summary>
    WinGamemode,
    /// <summary><see cref="DiscordKeySetQuest"/></summary>
    [Translatable("Custom Key")]
    DiscordKeyBinary,
    /// <summary><see cref="PlaceholderQuest"/></summary>
    Placeholder
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class QuestDataAttribute : Attribute
{
    public QuestType Type { get; }
    public QuestDataAttribute(QuestType type)
    {
        Type = type;
    }
}
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class QuestRewardAttribute : Attribute
{
    public QuestRewardType Type { get; }
    public Type ReturnType { get; }
    public QuestRewardAttribute(QuestRewardType type, Type returnType)
    {
        Type = type;
        ReturnType = returnType;
    }
}

public static class QuestJsonEx
{
    public static bool IsWildcardInclusive<TChoice, TVal>(this TChoice choice) where TChoice : IDynamicValue<TVal>.IChoice =>
        choice.ValueType == DynamicValueType.Wildcard && choice.Behavior == ChoiceBehavior.Inclusive;
    public static bool IsWildcardInclusive<TAsset>(this DynamicAssetValue<TAsset>.Choice choice) where TAsset : Asset =>
        choice.ValueType == DynamicValueType.Wildcard && choice.Behavior == ChoiceBehavior.Inclusive;
    public static WeaponClass GetWeaponClass(this Guid item)
    {
        if (Assets.find(item) is ItemGunAsset weapon)
        {
            if (weapon.action == EAction.Pump)
            {
                return WeaponClass.Shotgun;
            }
            if (weapon.action == EAction.Rail)
            {
                return WeaponClass.SniperRifle;
            }
            if (weapon.action == EAction.Minigun)
            {
                return WeaponClass.MachineGun;
            }
            if (weapon.action == EAction.Rocket)
            {
                return WeaponClass.Rocket;
            }
            if (weapon.itemDescription.IndexOf("smg", StringComparison.OrdinalIgnoreCase) != -1 ||
                weapon.itemDescription.IndexOf("sub", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return WeaponClass.SMG;
            }
            if (weapon.itemDescription.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return WeaponClass.Pistol;
            }
            if (weapon.itemDescription.IndexOf("marksman", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return WeaponClass.MarksmanRifle;
            }
            if (weapon.itemDescription.IndexOf("rifle", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return WeaponClass.BattleRifle;
            }
            if (weapon.itemDescription.IndexOf("machine", StringComparison.OrdinalIgnoreCase) != -1 || weapon.itemDescription.IndexOf("lmg", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return WeaponClass.MachineGun;
            }
        }

        return WeaponClass.Unknown;
    }
    // todo rewrite
    public static bool TryReadIntegralValue(this ref Utf8JsonReader reader, out DynamicIntegerValue value)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (string.IsNullOrEmpty(str) || str!.Length < 3)
            {
                if (string.Equals(str, "$*", StringComparison.Ordinal) || string.Equals(str, "#*", StringComparison.Ordinal))
                {
                    value = new DynamicIntegerValue(DynamicValueType.Wildcard, str![0] == '$' ? ChoiceBehavior.Selective : ChoiceBehavior.Inclusive);
                }
                else if (int.TryParse(str, NumberStyles.Any, Data.AdminLocale, out int v1))
                {
                    value = new DynamicIntegerValue(v1);
                }
                else
                {
                    value = new DynamicIntegerValue(0);
                    return false;
                }
                return true;
            }

            L.LogDebug(str);
            bool isInclusive = str[0] == '#';

            if (isInclusive || str[0] == '$')
            {
                int endInd = str.IndexOf(')', 1);
                bool hasRnd = str[str.Length - 1] == '}';
                int stRndInd = endInd + 1;
                if (str[1] == '(' && endInd != -1) // read range
                {
                    int sep = str.IndexOf(':', 2);
                    if (sep != -1)
                    {
                        bool m1 = sep == 2, m2 = sep + 1 == endInd;
                        int v1;
                        int v2;
                        if (m1)
                        {
                            v1 = int.MinValue;
                        }
                        else if (!int.TryParse(str.Substring(2, sep - 2), NumberStyles.Number, Data.AdminLocale, out v1))
                        {
                            v1 = int.MinValue;
                        }
                        if (m2)
                        {
                            v2 = int.MaxValue;
                        }
                        else if (!int.TryParse(str.Substring(sep + 1, endInd - sep - 1), NumberStyles.Number, Data.AdminLocale, out v2))
                        {
                            v2 = int.MaxValue;
                        }

                        if (!m1 && !m2 && v1 > v2)
                        {
                            (v2, v1) = (v1, v2);
                        }

                        int round = 0;
                        if (hasRnd)
                            round = int.Parse(str.Substring(stRndInd + 1, str.Length - stRndInd - 2), NumberStyles.Number, Data.AdminLocale);
                        
                        L.LogDebug($"({v1}:{v2})" + (hasRnd ? $"{{{round}}}" : string.Empty));
                        value = new DynamicIntegerValue(new IntegralRange(v1, v2, round, m1, m2), isInclusive ? ChoiceBehavior.Inclusive : ChoiceBehavior.Selective);
                        return true;
                    }

                    value = new DynamicIntegerValue(0);
                    return false;
                }

                if (str[1] == '[' && str[str.Length - 1] == ']')
                {
                    int arrLen = 1;
                    for (int i = 0; i < str.Length; i++)
                        if (str[i] == ',')
                            arrLen++;
                    int[] res = new int[arrLen];
                    int index = -1;
                    int lastInd = 1;
                    for (int i = 0; i < str.Length; i++)
                    {
                        if (str[i] is ']' or ',')
                        {
                            res[++index] = int.Parse(str.Substring(lastInd + 1, i - lastInd - 1));
                            lastInd = i;
                        }
                    }

                    L.LogDebug($"[{string.Join(",", res)}]");
                    value = new DynamicIntegerValue(new IntegralSet(res), isInclusive ? ChoiceBehavior.Inclusive : ChoiceBehavior.Selective);
                    return true;

                }
                value = new DynamicIntegerValue(0);
                return false;
            }

            if (int.TryParse(str, NumberStyles.Any, Data.AdminLocale, out int v))
            {
                value = new DynamicIntegerValue(v);
                return true;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int v))
        {
            value = new DynamicIntegerValue(v);
            return true;
        }
        value = new DynamicIntegerValue(0);
        return false;
    }
    public static bool TryReadFloatValue(this ref Utf8JsonReader reader, out DynamicFloatValue value)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (string.IsNullOrEmpty(str) || str!.Length < 3)
            {
                if (string.Equals(str, "$*", StringComparison.Ordinal) || string.Equals(str, "#*", StringComparison.Ordinal))
                {
                    value = new DynamicFloatValue(DynamicValueType.Wildcard, str![0] == '$' ? ChoiceBehavior.Selective : ChoiceBehavior.Inclusive);
                }
                else if (float.TryParse(str, NumberStyles.Any, Data.AdminLocale, out float v1))
                {
                    value = new DynamicFloatValue(v1);
                }
                else
                {
                    value = new DynamicFloatValue(0);
                    return false;
                }
                return true;
            }
            L.LogDebug(str);

            bool isInclusive = str[0] == '#';

            if (isInclusive || str[0] == '$')
            {
                int endInd = str.IndexOf(')', 1);
                bool hasRnd = str[str.Length - 1] == '}';
                int stRndInd = endInd + 1;
                if (str[1] == '(' && endInd != -1) // read range
                {
                    int sep = str.IndexOf(':', 2);
                    if (sep != -1)
                    {
                        bool m1 = sep == 2, m2 = sep + 1 == endInd;
                        float v1;
                        float v2;
                        if (m1)
                        {
                            v1 = float.MinValue;
                        }
                        else if (!float.TryParse(str.Substring(2, sep - 2), NumberStyles.Number, Data.AdminLocale, out v1))
                        {
                            v1 = float.MinValue;
                        }

                        if (m2)
                        {
                            v2 = float.MaxValue;
                        }
                        else if (!float.TryParse(str.Substring(sep + 1, endInd - sep - 1), NumberStyles.Number,
                                     Data.AdminLocale, out v2))
                        {
                            v2 = float.MaxValue;
                        }

                        if (!m1 && !m2 && v1 > v2)
                        {
                            (v2, v1) = (v1, v2);
                        }

                        int round = 0;
                        if (hasRnd)
                            round = int.Parse(str.Substring(stRndInd + 1, str.Length - stRndInd - 2), NumberStyles.Number, Data.AdminLocale);
                        

                        L.LogDebug($"({v1}f:{v2}f)" + (hasRnd ? $"{{{round}}}" : string.Empty));
                        value = new DynamicFloatValue(new FloatRange(v1, v2, round, m1, m2),
                            isInclusive ? ChoiceBehavior.Inclusive : ChoiceBehavior.Selective);
                        return true;
                    }

                    value = new DynamicFloatValue(0);
                    return false;
                }


                if (str[1] == '[' && str[str.Length - 1] == ']')
                {
                    int arrLen = 1;
                    for (int i = 0; i < str.Length; i++)
                        if (str[i] == ',')
                            arrLen++;
                    float[] res = new float[arrLen];
                    int index = -1;
                    int lastInd = 1;
                    for (int i = 0; i < str.Length; i++)
                    {
                        if (str[i] is ']' or ',')
                        {
                            res[++index] = float.Parse(str.Substring(lastInd + 1, i - lastInd - 1));
                            lastInd = i;
                        }
                    }

                    L.LogDebug($"[{string.Join("f,", res)}f]");
                    value = new DynamicFloatValue(new FloatSet(res), isInclusive ? ChoiceBehavior.Inclusive : ChoiceBehavior.Selective);
                    return true;

                }
                value = new DynamicFloatValue(0);
                return false;
            }

            if (float.TryParse(str, NumberStyles.Any, Data.AdminLocale, out float v))
            {
                value = new DynamicFloatValue(v);
                return true;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number && reader.TryGetSingle(out float v))
        {
            value = new DynamicFloatValue(v);
            return true;
        }
        value = new DynamicFloatValue(0);
        return false;
    }
    public static unsafe bool TryReadStringValue(this ref Utf8JsonReader reader, out DynamicStringValue value, bool isKitselector)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (string.IsNullOrEmpty(str) || str!.Length < 3)
            {
                if (string.Equals(str, "$*", StringComparison.Ordinal) || string.Equals(str, "#*", StringComparison.Ordinal))
                {
                    value = new DynamicStringValue(isKitselector, DynamicValueType.Wildcard, str![0] == '$' ? ChoiceBehavior.Selective : ChoiceBehavior.Inclusive);
                    return true;
                }

                value = new DynamicStringValue(isKitselector, str!);
                return false;
            }
            bool isInclusive = str[0] == '#';

            if (isInclusive || str[0] == '$')
            {
                if (str[1] == '[' && str[str.Length - 1] == ']')
                {
                    int len = str.Length;
                    int arrLen = 1;
                    string[] res;
                    fixed (char* p = str)
                    {
                        for (int i = 0; i < len; i++)
                            if (str[i] == ',')
                                arrLen++;
                        res = new string[arrLen];
                        int ptrpos = str.Length - 1;
                        int index = arrLen;
                        int endpos = ptrpos;
                        while (endpos > 2 && *(p + endpos - 1) != ',')
                            endpos--;
                        char[] current = new char[ptrpos - endpos];
                        while (ptrpos > 1)
                        {
                            ptrpos--;
                            char c = *(p + ptrpos);
                            if (c == ',' || c == '[')
                            {
                                index--;
                                res[index] = new string(current);
                                endpos = ptrpos - 1;
                                while (endpos > 2 && *(p + endpos - 1) != ',')
                                    endpos--;
                                current = new char[ptrpos - endpos];
                            }
                            else
                                current[ptrpos - endpos] = c;
                        }
                    }
                    value = new DynamicStringValue(isKitselector, new StringSet(res), isInclusive ? ChoiceBehavior.Inclusive : ChoiceBehavior.Selective);
                    return true;

                }

                value = new DynamicStringValue(isKitselector, str);
                return true;
            }

            value = new DynamicStringValue(isKitselector, str);
            return true;
        }
        value = new DynamicStringValue(isKitselector, null!);
        return false;
    }
    public static unsafe bool TryReadEnumValue<TEnum>(this ref Utf8JsonReader reader, out DynamicEnumValue<TEnum> value) where TEnum : struct, Enum
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (string.IsNullOrEmpty(str) || str!.Length < 3)
            {
                if (string.Equals(str, "$*", StringComparison.Ordinal) || string.Equals(str, "#*", StringComparison.Ordinal))
                {
                    value = new DynamicEnumValue<TEnum>(DynamicValueType.Wildcard, str![0] == '$' ? ChoiceBehavior.Selective : ChoiceBehavior.Inclusive);
                    return true;
                }

                value = new DynamicEnumValue<TEnum>(default);
                return false;
            }
            bool isInclusive = str[0] == '#';

            if (isInclusive || str[0] == '$')
            {
                if (str[1] == '[' && str[str.Length - 1] == ']')
                {
                    int len = str.Length;
                    int arrLen = 1;
                    string[] res;
                    fixed (char* p = str)
                    {
                        for (int i = 0; i < len; i++)
                            if (str[i] == ',')
                                arrLen++;
                        res = new string[arrLen];
                        int ptrpos = str.Length - 1;
                        int index = arrLen;
                        int endpos = ptrpos;
                        while (endpos > 2 && *(p + endpos - 1) != ',')
                            endpos--;
                        char[] current = new char[ptrpos - endpos];
                        while (ptrpos > 1)
                        {
                            ptrpos--;
                            char c = *(p + ptrpos);
                            if (c == ',' || c == '[')
                            {
                                index--;
                                res[index] = new string(current);
                                endpos = ptrpos - 1;
                                while (endpos > 2 && *(p + endpos - 1) != ',')
                                    endpos--;
                                current = new char[ptrpos - endpos];
                            }
                            else
                                current[ptrpos - endpos] = c;
                        }
                    }
                    TEnum[] enums = new TEnum[arrLen];
                    int i2 = 0;
                    for (int i = 0; i < arrLen; i++)
                    {
                        if (Enum.TryParse(res[i], true, out TEnum te))
                        {
                            enums[i2] = te;
                            i2++;
                        }
                        else
                        {
                            L.LogWarning("[QUEST PARSER] Couldn't interpret " + res[i] + " as an " + typeof(TEnum).Name);
                        }
                    }
                    // get rid of ones that failed to parse
                    if (i2 != arrLen)
                    {
                        TEnum[] old = enums;
                        enums = new TEnum[i2];
                        Array.Copy(old, 0, enums, 0, i2);
                    }
                    value = new DynamicEnumValue<TEnum>(new EnumSet<TEnum>(enums), isInclusive ? ChoiceBehavior.Inclusive : ChoiceBehavior.Selective);
                    return true;

                }
                if (str[1] == '(' && str[str.Length - 1] == ')') // read range
                {
                    int sep = str.IndexOf(':');
                    if (sep != -1)
                    {
                        bool m1 = sep == 2, m2 = str.Length <= sep + 1 || str[sep + 1] == ')';
                        string p1 = str.Substring(2, sep - 2);
                        string p2 = str.Substring(sep + 1, str.Length - sep - 2);
                        TEnum e1 = default;
                        TEnum e2 = default;
                        if (!m1 && !Enum.TryParse(p1, true, out e1))
                        {
                            L.LogWarning("[QUEST PARSER] Couldn't interpret \"" + p1 + "\" (lower range) as an " + typeof(TEnum).Name);
                            value = new DynamicEnumValue<TEnum>(default);
                            return false;
                        }
                        if (!m2 && !Enum.TryParse(p2, true, out e2))
                        {
                            L.LogWarning("[QUEST PARSER] Couldn't interpret \"" + p2 + "\" (higher range) as an " + typeof(TEnum).Name);
                            value = new DynamicEnumValue<TEnum>(default);
                            return false;
                        }

                        int a;
                        if (!m1 && !m2)
                        {
                            a = e1.CompareTo(e2);
                            if (a > 0)
                            {
                                (e2, e1) = (e1, e2);
                                (m2, m1) = (m1, m2);
                            }
                        }
                        else a = -1;
                        value = a == 0
                            ? new DynamicEnumValue<TEnum>(e1)
                            : new DynamicEnumValue<TEnum>(new EnumRange<TEnum>(e1, e2, m1, m2), isInclusive
                                ? ChoiceBehavior.Inclusive
                                : ChoiceBehavior.Selective);
                        return true;
                    }

                    value = new DynamicEnumValue<TEnum>(default);
                    return false;
                }
                if (Enum.TryParse(str, out TEnum res2))
                {
                    value = new DynamicEnumValue<TEnum>(res2);
                    return true;
                }
            }
            else if (Enum.TryParse(str, out TEnum res))
            {
                value = new DynamicEnumValue<TEnum>(res);
                return true;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt64(out long v2))
            {
                try
                {
                    value = new DynamicEnumValue<TEnum>((TEnum)Convert.ChangeType(v2, typeof(TEnum)));
                    return true;
                }
                catch (InvalidCastException) { }
            }
        }
        value = new DynamicEnumValue<TEnum>(default);
        return false;
    }
    public static bool TryReadEnumValue<TEnum>(this ref Utf8JsonReader reader, out TEnum value) where TEnum : struct, Enum
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (str == null)
            {
                value = default;
                return false;
            }
            return Enum.TryParse(str, true, out value);
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt64(out long v2))
            {
                try
                {
                    value = (TEnum)Convert.ChangeType(v2, typeof(TEnum));
                    return true;
                }
                catch (InvalidCastException) { }
            }
        }
        value = default;
        return false;
    }
    public static unsafe bool TryReadAssetValue<TAsset>(this ref Utf8JsonReader reader, out DynamicAssetValue<TAsset> value) where TAsset : Asset
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (string.IsNullOrEmpty(str) || str!.Length < 3)
            {
                if (string.Equals(str, "$*", StringComparison.Ordinal) || string.Equals(str, "#*", StringComparison.Ordinal))
                {
                    value = new DynamicAssetValue<TAsset>(DynamicValueType.Wildcard, str![0] == '$' ? ChoiceBehavior.Selective : ChoiceBehavior.Inclusive);
                    return true;
                }

                value = new DynamicAssetValue<TAsset>(default);
                return false;
            }
            bool isInclusive = str[0] == '#';

            if (isInclusive || str[0] == '$')
            {
                if (str[1] == '[' && str[str.Length - 1] == ']')
                {
                    int len = str.Length;
                    int arrLen = 1;
                    string[] res;
                    fixed (char* p = str)
                    {
                        for (int i = 0; i < len; i++)
                            if (str[i] == ',')
                                arrLen++;
                        res = new string[arrLen];
                        int ptrpos = str.Length - 1;
                        int index = arrLen;
                        int endpos = ptrpos;
                        while (*(p + endpos) != ',')
                            endpos--;
                        char[] current = new char[ptrpos - endpos];
                        while (ptrpos > 1)
                        {
                            ptrpos--;
                            char c = *(p + ptrpos);
                            if (c == ',' || c == '[')
                            {
                                index--;
                                res[index] = new string(current);
                                endpos = ptrpos - 1;
                                while (endpos > 2 && *(p + endpos) != ',')
                                    endpos--;
                                current = new char[ptrpos - endpos];
                            }
                            else
                                current[ptrpos - endpos] = c;
                        }
                    }
                    Guid[] guids = new Guid[arrLen];
                    int i2 = 0;
                    for (int i = 0; i < arrLen; i++)
                    {
                        if (Guid.TryParse(res[i], out Guid te) && Assets.find(te) is TAsset)
                        {
                            guids[i2] = te;
                            i2++;
                        }
                        else
                        {
                            L.LogWarning("[QUEST PARSER] Couldn't interpret " + res[i] + " as a " + typeof(TAsset).Name);
                        }
                    }
                    // get rid of ones that failed to parse or aren't a valid asset
                    if (i2 != arrLen)
                    {
                        Guid[] old = guids;
                        guids = new Guid[i2];
                        Array.Copy(old, 0, guids, 0, i2);
                    }

                    value = new DynamicAssetValue<TAsset>(new GuidSet(guids), isInclusive ? ChoiceBehavior.Inclusive : ChoiceBehavior.Selective);
                    return true;
                }

                L.LogWarning("[QUEST PARSER] Couldn't interpret " + str + " as a " + typeof(TAsset).Name);
                value = new DynamicAssetValue<TAsset>(default);
                return false;
            }

            if (Guid.TryParse(str, out Guid guid))
            {
                value = new DynamicAssetValue<TAsset>(guid);
                return true;
            }

            L.LogWarning("[QUEST PARSER] Couldn't interpret " + str + " as a " + typeof(TAsset).Name);
        }
        value = new DynamicAssetValue<TAsset>(default);
        return false;
    }
    public static void WriteProperty<T>(this Utf8JsonWriter writer, string property, IDynamicValue<T>.IChoice choice)
    {
        writer.WritePropertyName(property);
        choice.Write(writer);
    }
    public static int RoundNumber(int min, int round, int value)
    {
        if (round > 0)
        {
            int val2 = value - min;
            int mod = val2 % round;
            if (mod == 0)
                return value;
            if (mod > round / 2f)
                return value + (round - mod);
            return value - mod;
        }
        return value;
    }
    public static float RoundNumber(float min, int round, float value)
    {
        if (round == 0) return value;
        if (round > 0)
        {
            float val2 = value - min;
            float mod = val2 % round;
            if (mod == 0)
                return value;
            if (mod > round / 2f)
                return value + (round - mod);
            return value - mod;
        }
        else
        {
            int val = -round;
            while (val > 0)
            {
                val /= 10;
            }
            int t = (int)Mathf.Pow(10, (val - 1) * 2 + 1);
            float m = (-round % t) / (float)t;
            float val2 = value - min;
            float mod = val2 % m;
            if (mod == 0)
                return value;
            if (mod > m / 2f)
                return value + (m - mod);
            return value - mod;
        }
    }
}
/// <summary>Datatype storing either a constant <see cref="int"/>, a <see cref="IntegralRange"/> or a <see cref="IntegralSet"/>.</summary>
public readonly struct DynamicIntegerValue : IDynamicValue<int>, IEquatable<DynamicIntegerValue>
{
    private readonly int _constant;
    private readonly IntegralRange _range;
    private readonly IntegralSet _set;
    private readonly DynamicValueType _type;
    private readonly ChoiceBehavior _choiceBehavior = ChoiceBehavior.Selective;
    public static readonly IDynamicValue<int>.IChoice Zero = new Choice(new DynamicIntegerValue(0, ChoiceBehavior.Selective));
    public static readonly IDynamicValue<int>.IChoice One = new Choice(new DynamicIntegerValue(1, ChoiceBehavior.Selective));
    public static readonly IDynamicValue<int>.IChoice Any = new Choice(new DynamicIntegerValue(DynamicValueType.Wildcard, ChoiceBehavior.Inclusive));
    public int Constant => _constant;
    public IDynamicValue<int>.IRange Range => _range;
    public IDynamicValue<int>.ISet Set => _set;
    public DynamicValueType ValueType => _type;
    public ChoiceBehavior SelectionBehavior => _choiceBehavior;

    /// <remarks>Don't use this unless you know what you're doing.</remarks>
    internal DynamicIntegerValue(DynamicValueType type, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = default;
        _range = default;
        _type = type;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicIntegerValue(int constant, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = constant;
        _range = default;
        _set = default;
        _type = DynamicValueType.Constant;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicIntegerValue(ref IntegralRange range, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _range = range;
        _set = default;
        _type = DynamicValueType.Range;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicIntegerValue(ref IntegralSet set, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _range = default;
        _set = set;
        _type = DynamicValueType.Set;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicIntegerValue(IntegralRange range, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _range = range;
        _set = default;
        _type = DynamicValueType.Range;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicIntegerValue(IntegralSet set, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _range = default;
        _set = set;
        _type = DynamicValueType.Set;
        _choiceBehavior = choiceBehavior;
    }
    public static IDynamicValue<int>.IChoice ReadChoice(ref Utf8JsonReader reader)
    {
        Choice choice = new Choice();
        choice.Read(ref reader);
        return choice;
    }
    public IDynamicValue<int>.IChoice GetValue()
    {
        return new Choice(this);
    }
    public override bool Equals(object obj) => obj is DynamicIntegerValue c && Equals(in c);
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = _constant;
            hashCode = (hashCode * 397) ^ _range.GetHashCode();
            hashCode = (hashCode * 397) ^ _set.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)_type;
            hashCode = (hashCode * 397) ^ (int)_choiceBehavior;
            return hashCode;
        }
    }
    bool IEquatable<DynamicIntegerValue>.Equals(DynamicIntegerValue other) => Equals(in other);
    public bool Equals(in DynamicIntegerValue other)
    {
        if (other._choiceBehavior != _choiceBehavior || other._type != _type)
            return false;

        if (_type == DynamicValueType.Wildcard)
            return true;

        if (_type == DynamicValueType.Set)
        {
            if (_set.Length != other._set.Length)
                return false;
            for (int i = 0; i < _set.Length; ++i)
            {
                if (_set.Set[i] == other._set.Set[i])
                    return false;
            }

            return true;
        }
        if (_type == DynamicValueType.Range)
        {
            if (_range.IsInfiniteMax != other._range.IsInfiniteMax || _range.IsInfiniteMin != other._range.IsInfiniteMin)
                return false;
            if (!_range.IsInfiniteMax && _range.Maximum != other._range.Maximum)
                return false;
            return _range.IsInfiniteMin || _range.Minimum == other._range.Minimum;
        }
        if (_type == DynamicValueType.Constant)
            return _constant == other.Constant;

        return false;
    }
    public override string ToString()
    {
        if (_type == DynamicValueType.Constant)
            return _constant.ToString(Data.AdminLocale);
        if (_type == DynamicValueType.Range)
            return (_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#") + "(" + _range.Minimum.ToString(Data.AdminLocale) +
                   ":" + _range.Maximum.ToString(Data.AdminLocale) + ")";
        if (_type == DynamicValueType.Wildcard)
            return _choiceBehavior == ChoiceBehavior.Selective ? "$*" : "#*";
        if (_type == DynamicValueType.Set)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < _set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(_set.Set[i].ToString(Data.AdminLocale));
            }
            sb.Append(']');
            return sb.ToString();
        }
        return _constant.ToString(Data.AdminLocale);
    }
    public void Write(Utf8JsonWriter writer)
    {
        if (_type == DynamicValueType.Constant)
        {
            writer.WriteNumberValue(_constant);
        }
        else if (_type == DynamicValueType.Range)
        {
            writer.WriteStringValue((_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#") + "(" + _range.Minimum.ToString(Data.AdminLocale) +
                                    ":" + _range.Maximum.ToString(Data.AdminLocale) + ")");
        }
        else if (_type == DynamicValueType.Wildcard)
        {
            writer.WriteStringValue(_choiceBehavior == ChoiceBehavior.Selective ? "$*" : "#*");
        }
        else if (_type == DynamicValueType.Set)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < _set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(_set.Set[i].ToString(Data.AdminLocale));
            }
            sb.Append(']');
            writer.WriteStringValue(sb.ToString());
        }
        else
        {
            writer.WriteNumberValue(_constant);
        }
    }
    private struct Choice : IDynamicValue<int>.IChoice
    {
        private ChoiceBehavior _behavior;
        private DynamicValueType _type;
        private int _value;
        private int[] _values;
        private int _minVal;
        private int _maxVal;
        public ChoiceBehavior Behavior => _behavior;
        public DynamicValueType ValueType => _type;
        public Choice(DynamicIntegerValue value)
        {
            _value = default;
            _minVal = default;
            _maxVal = default;
            _values = null!;
            _type = default;
            _behavior = default;
            FromValue(ref value);
        }
        public override bool Equals(object obj) => obj is Choice c && Equals(in c);
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)_behavior;
                hashCode = (hashCode * 397) ^ (int)_type;
                hashCode = (hashCode * 397) ^ _value;
                hashCode = (hashCode * 397) ^ _values.GetHashCode();
                hashCode = (hashCode * 397) ^ _minVal;
                hashCode = (hashCode * 397) ^ _maxVal;
                return hashCode;
            }
        }

        public bool Equals(in Choice other)
        {
            if (other._behavior != _behavior || other._type != _type)
                return false;

            if (_type == DynamicValueType.Wildcard)
                return true;

            if (_behavior == ChoiceBehavior.Selective || _type == DynamicValueType.Constant)
            {
                return _value == other._value;
            }

            if (_type == DynamicValueType.Range)
            {
                return _minVal == other._minVal && _maxVal == other._maxVal;
            }

            if (_type == DynamicValueType.Set && _values != null && other._values != null)
            {
                if (_values.Length != other._values.Length)
                    return false;
                for (int i = 0; i < _values.Length; ++i)
                {
                    if (_values[i] != other._values[i])
                        return false;
                }

                return true;
            }

            return false;
        }
        private void FromValue(ref DynamicIntegerValue value)
        {
            _type = value._type;
            _behavior = value._choiceBehavior;
            if (value._type == DynamicValueType.Constant)
            {
                _value = value._constant;
            }
            else if (value._type == DynamicValueType.Set)
            {
                if (_behavior == ChoiceBehavior.Selective)
                {
                    _value = value._set.Length == 1 ? value._set.Set[0] : value._set.Set[UnityEngine.Random.Range(0, value._set.Length)];
                }
                else
                {
                    _values = value._set.Set;
                }
            }
            else if (value._type == DynamicValueType.Range)
            {
                if (_behavior == ChoiceBehavior.Selective)
                {
                    _value = UnityEngine.Random.Range(value._range.Minimum, value._range.Maximum + 1);
                    _value = QuestJsonEx.RoundNumber(value._range.Minimum, value._range.Round, _value);
                }
                else
                {
                    _minVal = value._range.Minimum;
                    _maxVal = value._range.Maximum;
                }
            }
            else
            {
                _value = value._constant;
            }
        }
        /// <summary>Will return <see langword="default"/> if <see cref="DynamicIntegerValue.SelectionBehavior"/> is <see cref=ChoiceBehavior.InclusiveL"/> and the value is not of type <see cref=DynamicValueType.ConstantT"/>.</summary>
        public int InsistValue() => _value;
        public bool IsMatch(int value)
        {
            if (_type == DynamicValueType.Wildcard) return true;
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                return _value == value;
            if (_type == DynamicValueType.Range)
                return value >= _minVal && value <= _maxVal;
            if (_type == DynamicValueType.Set)
            {
                for (int i = 0; i < _values.Length; i++)
                    if (_values[i] == value) return true;
            }

            return false;
        }
        public bool Read(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? str = reader.GetString();
                if (str == null) return false;
                if (str.Length > 1 && (str[0] == '$' || str[0] == '#'))
                {
                    if (string.Equals(str, "$*", StringComparison.Ordinal) || string.Equals(str, "#*", StringComparison.Ordinal))
                    {
                        if (str[0] == '#')
                            _behavior = ChoiceBehavior.Inclusive;
                        else
                        {
                            _behavior = ChoiceBehavior.Selective;
                            L.LogWarning("Possibly unintended int singular choice expression in quest choice: $*");
                        }
                        _type = DynamicValueType.Wildcard;
                        return true;
                    }

                    if (reader.TryReadIntegralValue(out DynamicIntegerValue v))
                    {
                        FromValue(ref v);
                        if (_behavior == ChoiceBehavior.Selective && _type != DynamicValueType.Constant)
                            L.LogWarning("Possibly unintended int singular choice expression in quest choice: " + str);
                        return true;
                    }
                }
                else if (int.TryParse(str, NumberStyles.Any, Data.AdminLocale, out _value))
                {
                    _type = DynamicValueType.Constant;
                    _behavior = ChoiceBehavior.Selective;
                    return true;
                }
                return false;
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out _value))
            {
                _type = DynamicValueType.Constant;
                _behavior = ChoiceBehavior.Selective;
                return true;
            }
            return false;
        }
        public void Write(Utf8JsonWriter writer)
        {
            if (_behavior == ChoiceBehavior.Selective || _type == DynamicValueType.Constant)
            {
                writer.WriteNumberValue(_value);
            }
            else if (_type == DynamicValueType.Range)
            {
                writer.WriteStringValue((_behavior == ChoiceBehavior.Selective ? "$" : "#") + "(" + _minVal.ToString(Data.AdminLocale) + ":" + _maxVal.ToString(Data.AdminLocale) + ")");
            }
            else if (_type == DynamicValueType.Wildcard)
            {
                writer.WriteStringValue(_behavior == ChoiceBehavior.Selective ? "$*" : "#*");
            }
            else if (_type == DynamicValueType.Set)
            {
                StringBuilder sb = new StringBuilder(_behavior == ChoiceBehavior.Selective ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(_values[i].ToString(Data.AdminLocale));
                }
                sb.Append(']');
                writer.WriteStringValue(sb.ToString());
            }
            else
            {
                writer.WriteNumberValue(_value);
            }
        }
        public override string ToString()
        {
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                return _value.ToString(Data.AdminLocale);
            if (_type == DynamicValueType.Range)
                return (_behavior == ChoiceBehavior.Selective ? "$" : "#") + "(" + _minVal.ToString(Data.AdminLocale) +
                       ":" + _maxVal.ToString(Data.AdminLocale) + ")";
            if (_type == DynamicValueType.Wildcard)
                return _behavior == ChoiceBehavior.Selective ? "$*" : "#*";
            if (_type == DynamicValueType.Set)
            {
                StringBuilder sb = new StringBuilder(_behavior == ChoiceBehavior.Selective ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(_values[i].ToString(Data.AdminLocale));
                }
                sb.Append(']');
                return sb.ToString();
            }
            return _value.ToString(Data.AdminLocale);
        }
    }
}

/// <summary>Datatype storing either a constant <see cref="float"/>, a <see cref="FloatRange"/> or a <see cref="FloatSet"/>.</summary>
public readonly struct DynamicFloatValue : IDynamicValue<float>, IEquatable<DynamicFloatValue>
{
    private readonly float _constant;
    public float Constant => _constant;

    private readonly FloatRange _range;
    public IDynamicValue<float>.IRange Range => _range;

    private readonly ChoiceBehavior _choiceBehavior = ChoiceBehavior.Selective;
    public ChoiceBehavior SelectionBehavior { get => _choiceBehavior; }

    private readonly FloatSet _set;
    public IDynamicValue<float>.ISet Set => _set;

    private readonly DynamicValueType _type;
    public DynamicValueType ValueType => _type;

    /// <remarks>Don't use this unless you know what you're doing.</remarks>
    internal DynamicFloatValue(DynamicValueType type, ChoiceBehavior anyBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = default;
        _range = default;
        _type = type;
        _choiceBehavior = anyBehavior;
    }
    public DynamicFloatValue(float constant)
    {
        _constant = constant;
        _range = default;
        _set = default;
        _type = DynamicValueType.Constant;
        _choiceBehavior = ChoiceBehavior.Selective;
    }
    public DynamicFloatValue(ref FloatRange range, ChoiceBehavior anyBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _range = range;
        _set = default;
        _type = DynamicValueType.Range;
        _choiceBehavior = anyBehavior;
    }
    public DynamicFloatValue(ref FloatSet set, ChoiceBehavior anyBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _range = default;
        _set = set;
        _type = DynamicValueType.Set;
        _choiceBehavior = anyBehavior;
    }
    public DynamicFloatValue(FloatRange range, ChoiceBehavior anyBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _range = range;
        _set = default;
        _type = DynamicValueType.Range;
        _choiceBehavior = anyBehavior;
    }
    public DynamicFloatValue(FloatSet set, ChoiceBehavior anyBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _range = default;
        _set = set;
        _type = DynamicValueType.Set;
        _choiceBehavior = anyBehavior;
    }
    public IDynamicValue<float>.IChoice GetValue()
    {
        return new Choice(this);
    }
    public static IDynamicValue<float>.IChoice ReadChoice(ref Utf8JsonReader reader)
    {
        Choice choice = new Choice();
        choice.Read(ref reader);
        return choice;
    }
    bool IEquatable<DynamicFloatValue>.Equals(DynamicFloatValue other) => Equals(in other);
    public override bool Equals(object obj) => obj is DynamicFloatValue c && Equals(in c);
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = _constant.GetHashCode();
            hashCode = (hashCode * 397) ^ _range.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)_choiceBehavior;
            hashCode = (hashCode * 397) ^ _set.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)_type;
            return hashCode;
        }
    }

    public bool Equals(in DynamicFloatValue other)
    {
        if (other._choiceBehavior != _choiceBehavior || other._type != _type)
            return false;

        if (_type == DynamicValueType.Wildcard)
            return true;

        if (_type == DynamicValueType.Set)
        {
            if (_set.Length != other._set.Length)
                return false;
            for (int i = 0; i < _set.Length; ++i)
            {
                if (!_set.Set[i].AlmostEquals(other._set.Set[i], 0.005f))
                    return false;
            }

            return true;
        }
        if (_type == DynamicValueType.Range)
        {
            if (_range.IsInfiniteMax != other._range.IsInfiniteMax || _range.IsInfiniteMin != other._range.IsInfiniteMin)
                return false;
            if (!_range.IsInfiniteMax && !_range.Maximum.AlmostEquals(other._range.Maximum, 0.005f))
                return false;
            return _range.IsInfiniteMin || _range.Minimum.AlmostEquals(other._range.Minimum, 0.005f);
        }
        if (_type == DynamicValueType.Constant)
            return _constant.AlmostEquals(other.Constant, 0.005f);

        return false;
    }
    public override string ToString()
    {
        if (_type == DynamicValueType.Constant)
            return _constant.ToString(Data.AdminLocale);
        if (_type == DynamicValueType.Range)
            return (_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#") + "(" + _range.Minimum.ToString(Data.AdminLocale) +
                   ":" + _range.Maximum.ToString(Data.AdminLocale) + ")";
        if (_type == DynamicValueType.Wildcard)
            return _choiceBehavior == ChoiceBehavior.Selective ? "$*" : "#*";
        if (_type == DynamicValueType.Set)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < _set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(_set.Set[i].ToString(Data.AdminLocale));
            }
            sb.Append(']');
            return sb.ToString();
        }
        return _constant.ToString(Data.AdminLocale);
    }
    public void Write(Utf8JsonWriter writer)
    {
        if (_type == DynamicValueType.Constant)
        {
            writer.WriteNumberValue(_constant);
        }
        else if (_type == DynamicValueType.Range)
        {
            writer.WriteStringValue((_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#") + "(" + _range.Minimum.ToString(Data.AdminLocale) +
                                    ":" + _range.Maximum.ToString(Data.AdminLocale) + ")");
        }
        else if (_type == DynamicValueType.Wildcard)
        {
            writer.WriteStringValue(_choiceBehavior == ChoiceBehavior.Selective ? "$*" : "#*");
        }
        else if (_type == DynamicValueType.Set)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < _set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(_set.Set[i].ToString(Data.AdminLocale));
            }
            sb.Append(']');
            writer.WriteStringValue(sb.ToString());
        }
        else
        {
            writer.WriteNumberValue(_constant);
        }
    }
    private struct Choice : IDynamicValue<float>.IChoice
    {
        private ChoiceBehavior _behavior;
        private DynamicValueType _type;
        private float _value;
        private float[]? _values;
        private float _minVal;
        private float _maxVal;
        public ChoiceBehavior Behavior => _behavior;
        public DynamicValueType ValueType => _type;
        public Choice(DynamicFloatValue value)
        {
            _value = default;
            _minVal = default;
            _maxVal = default;
            _values = null;
            _type = default;
            _behavior = default;
            FromValue(ref value);
        }
        public override bool Equals(object obj) => obj is Choice c && Equals(in c);
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)_behavior;
                hashCode = (hashCode * 397) ^ (int)_type;
                hashCode = (hashCode * 397) ^ _value.GetHashCode();
                hashCode = (hashCode * 397) ^ (_values != null ? _values.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _minVal.GetHashCode();
                hashCode = (hashCode * 397) ^ _maxVal.GetHashCode();
                return hashCode;
            }
        }
        public bool Equals(in Choice other)
        {
            if (other._behavior != _behavior || other._type != _type)
                return false;

            if (_type == DynamicValueType.Wildcard)
                return true;

            if (_behavior == ChoiceBehavior.Selective || _type == DynamicValueType.Constant)
            {
                return _value.AlmostEquals(other._value, 0.005f);
            }

            if (_type == DynamicValueType.Range)
            {
                return _minVal.AlmostEquals(other._minVal, 0.005f) && _maxVal.AlmostEquals(other._maxVal, 0.005f);
            }

            if (_type == DynamicValueType.Set && _values != null && other._values != null)
            {
                if (_values.Length != other._values.Length)
                    return false;
                for (int i = 0; i < _values.Length; ++i)
                {
                    if (!_values[i].AlmostEquals(other._values[i], 0.005f))
                        return false;
                }

                return true;
            }

            return false;
        }
        private void FromValue(ref DynamicFloatValue value)
        {
            _type = value._type;
            _behavior = value._choiceBehavior;
            if (value._type == DynamicValueType.Constant)
            {
                _value = value._constant;
            }
            else if (value._type == DynamicValueType.Set)
            {
                if (_behavior == ChoiceBehavior.Selective)
                {
                    _value = value._set.Length == 1 ? value._set.Set[0] : value._set.Set[UnityEngine.Random.Range(0, value._set.Length)];
                }
                else
                {
                    _values = value._set.Set;
                }
            }
            else if (value._type == DynamicValueType.Range)
            {
                if (_behavior == ChoiceBehavior.Selective)
                {
                    _value = UnityEngine.Random.Range(value._range.Minimum, value._range.Maximum);
                    _value = QuestJsonEx.RoundNumber(value._range.Minimum, value._range.Round, _value);
                }
                else
                {
                    _minVal = value._range.Minimum;
                    _maxVal = value._range.Maximum;
                }
            }
            else
            {
                _value = value._constant;
            }
        }
        /// <summary>Will return <see langword="default"/> if <see cref="DynamicFloatValue.SelectionBehavior"/> is <see cref=ChoiceBehavior.InclusiveL"/> and the value is not of type <see cref=DynamicValueType.ConstantT"/>.</summary>
        public float InsistValue() => _value;
        public bool IsMatch(float value)
        {
            if (_type == DynamicValueType.Wildcard) return true;
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                return Math.Abs(_value - value) < 0.005f;
            if (_type == DynamicValueType.Range)
                return value >= _minVal && value <= _maxVal;
            if (_type == DynamicValueType.Set)
            {
                for (int i = 0; i < _values!.Length; i++)
                    if (Math.Abs(_values[i] - value) < 0.005f) return true;
            }

            return false;
        }
        public bool Read(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? str = reader.GetString();
                if (str == null) return false;
                if (str.Length > 1 && (str[0] == '$' || str[0] == '#'))
                {
                    if (string.Equals(str, "$*", StringComparison.Ordinal) || string.Equals(str, "#*", StringComparison.Ordinal))
                    {
                        if (str[0] == '#')
                            _behavior = ChoiceBehavior.Inclusive;
                        else
                        {
                            _behavior = ChoiceBehavior.Selective;
                            L.LogWarning("Possibly unintended float singular choice expression in quest choice: $*");
                        }
                        _type = DynamicValueType.Wildcard;
                        return true;
                    }

                    if (reader.TryReadFloatValue(out DynamicFloatValue v))
                    {
                        FromValue(ref v);
                        if (_behavior == ChoiceBehavior.Selective && _type != DynamicValueType.Constant)
                            L.LogWarning("Possibly unintended float singular choice expression in quest choice: " + str);
                        return true;
                    }
                }
                else if (float.TryParse(str, NumberStyles.Any, Data.AdminLocale, out _value))
                {
                    _type = DynamicValueType.Constant;
                    _behavior = ChoiceBehavior.Selective;
                    return true;
                }
                return false;
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetSingle(out _value))
            {
                _type = DynamicValueType.Constant;
                _behavior = ChoiceBehavior.Selective;
                return true;
            }
            return false;
        }
        public void Write(Utf8JsonWriter writer)
        {
            if (_behavior == ChoiceBehavior.Selective || _type == DynamicValueType.Constant)
            {
                writer.WriteNumberValue(_value);
            }
            else if (_type == DynamicValueType.Range)
            {
                writer.WriteStringValue((_behavior == ChoiceBehavior.Selective ? "$" : "#") + "(" + _minVal.ToString(Data.AdminLocale) + ":" + _maxVal.ToString(Data.AdminLocale) + ")");
            }
            else if (_type == DynamicValueType.Wildcard)
            {
                writer.WriteStringValue(_behavior == ChoiceBehavior.Selective ? "$*" : "#*");
            }
            else if (_type == DynamicValueType.Set)
            {
                StringBuilder sb = new StringBuilder(_behavior == ChoiceBehavior.Selective ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values!.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(_values[i].ToString(Data.AdminLocale));
                }
                sb.Append(']');
                writer.WriteStringValue(sb.ToString());
            }
            else
            {
                writer.WriteNumberValue(_value);
            }
        }
        public override string ToString()
        {
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                return _value.ToString(Data.AdminLocale);
            if (_type == DynamicValueType.Range)
                return (_behavior == ChoiceBehavior.Selective ? "$" : "#") + "(" + _minVal.ToString(Data.AdminLocale) +
                       ":" + _maxVal.ToString(Data.AdminLocale) + ")";
            if (_type == DynamicValueType.Wildcard)
                return _behavior == ChoiceBehavior.Selective ? "$*" : "#*";
            if (_type == DynamicValueType.Set)
            {
                StringBuilder sb = new StringBuilder(_behavior == ChoiceBehavior.Selective ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values!.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(_values[i].ToString(Data.AdminLocale));
                }
                sb.Append(']');
                return sb.ToString();
            }
            return _value.ToString(Data.AdminLocale);
        }
    }
}
/// <summary>Datatype storing either a constant <see cref="string"/> or a <see cref="StringSet"/>.</summary>
public readonly struct DynamicStringValue : IDynamicValue<string>, IEquatable<DynamicStringValue>
{
    private readonly string? _constant;
    private readonly StringSet _set;
    private readonly DynamicValueType _type;
    private readonly ChoiceBehavior _choiceBehavior = ChoiceBehavior.Selective;
    public bool IsKitSelector { get; }
    public string Constant => _constant!;
    public IDynamicValue<string>.ISet Set => _set;
    public DynamicValueType ValueType => _type;
    public IDynamicValue<string>.IRange? Range => null;
    public ChoiceBehavior SelectionBehavior => _choiceBehavior;

    /// <remarks>Don't use this unless you know what you're doing.</remarks>
    internal DynamicStringValue(bool isKitSelector, DynamicValueType type, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = default;
        _type = type;
        _choiceBehavior = choiceBehavior;
        IsKitSelector = isKitSelector;
    }
    public DynamicStringValue(bool isKitSelector, string constant)
    {
        _constant = constant;
        _set = default;
        _type = DynamicValueType.Constant;
        IsKitSelector = isKitSelector;
    }
    public DynamicStringValue(bool isKitSelector, ref StringSet set, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = set;
        _type = DynamicValueType.Set;
        _choiceBehavior = choiceBehavior;
        IsKitSelector = isKitSelector;
    }
    public DynamicStringValue(bool isKitSelector, StringSet set, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = set;
        _type = DynamicValueType.Set;
        _choiceBehavior = choiceBehavior;
        IsKitSelector = isKitSelector;
    }
    IDynamicValue<string>.IChoice IDynamicValue<string>.GetValue() => GetValue();
    internal Choice GetValue()
    {
        return new Choice(this);
    }
    public static IDynamicValue<string>.IChoice ReadChoice(ref Utf8JsonReader reader) => ReadChoiceIntl(ref reader);
    internal static Choice ReadChoiceIntl(ref Utf8JsonReader reader)
    {
        Choice choice = new Choice();
        choice.Read(ref reader);
        return choice;
    }
    bool IEquatable<DynamicStringValue>.Equals(DynamicStringValue other) => Equals(in other);
    public override bool Equals(object obj) => obj is DynamicStringValue c && Equals(in c);
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = (_constant != null ? _constant.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ _set.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)_type;
            hashCode = (hashCode * 397) ^ (int)_choiceBehavior;
            hashCode = (hashCode * 397) ^ IsKitSelector.GetHashCode();
            return hashCode;
        }
    }
    public bool Equals(in DynamicStringValue other)
    {
        if (other._choiceBehavior != _choiceBehavior || other._type != _type || other.IsKitSelector != IsKitSelector)
            return false;

        if (_type == DynamicValueType.Wildcard)
            return true;

        if (_type == DynamicValueType.Set)
        {
            if (_set.Length != other._set.Length)
                return false;
            for (int i = 0; i < _set.Length; ++i)
            {
                if (!_set.Set[i].Equals(other._set.Set[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        if (_type == DynamicValueType.Constant)
            return _constant!.Equals(other._constant!, StringComparison.Ordinal);

        return false;
    }
    public override string ToString()
    {
        if (_type == DynamicValueType.Constant)
            return _constant!;
        if (_type == DynamicValueType.Wildcard)
            return _choiceBehavior == ChoiceBehavior.Selective ? "$*" : "#*";
        if (_type == DynamicValueType.Set)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#");
            sb.Append('[');
            string[] arr = _set.Set;
            for (int i = 0; i < _set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                if (arr[i] != null)
                    sb.Append(arr[i].Replace(',', '_'));
            }
            sb.Append(']');
            return sb.ToString();
        }
        return _constant!;
    }
    public void Write(Utf8JsonWriter writer)
    {
        if (_type == DynamicValueType.Constant)
        {
            writer.WriteStringValue(_constant);
        }
        else if (_type == DynamicValueType.Wildcard)
        {
            writer.WriteStringValue(_choiceBehavior == ChoiceBehavior.Selective ? "$*" : "#*");
        }
        else if (_type == DynamicValueType.Set)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#");
            sb.Append('[');
            string[] arr = _set.Set;
            for (int i = 0; i < _set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                if (arr[i] != null)
                    sb.Append(arr[i].Replace(',', '_'));
            }
            sb.Append(']');
            writer.WriteStringValue(sb.ToString());
        }
        else
        {
            writer.WriteStringValue(_constant);
        }
    }
    internal struct Choice : IDynamicValue<string>.IChoice
    {
        private ChoiceBehavior _behavior;
        private DynamicValueType _type;
        private string? _value;
        private string[]? _values;
        private string? _kitName;
        private string[]? _kitNames;
        private bool _isKitSelector;
        public bool IsKitSelector => _isKitSelector;
        public ChoiceBehavior Behavior => _behavior;
        public DynamicValueType ValueType => _type;
        public Choice(DynamicStringValue value)
        {
            _value = default;
            _values = null;
            _type = default;
            _behavior = default;
            _isKitSelector = default;
            _kitName = null;
            _kitNames = null;
            FromValue(ref value);
        }
        public override bool Equals(object obj) => obj is Choice c && Equals(in c);
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)_behavior;
                hashCode = (hashCode * 397) ^ (int)_type;
                hashCode = (hashCode * 397) ^ (_value != null ? _value.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_values != null ? _values.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _isKitSelector.GetHashCode();
                return hashCode;
            }
        }
        public bool Equals(in Choice other)
        {
            if (other._behavior != _behavior || other._type != _type || _isKitSelector != other._isKitSelector)
                return false;

            if (_type == DynamicValueType.Wildcard)
                return true;

            if ((_behavior == ChoiceBehavior.Selective || _type == DynamicValueType.Constant) && _value != null && other._value != null)
            {
                return _value.Equals(other._value, StringComparison.Ordinal);
            }

            if (_type == DynamicValueType.Set && _values != null && other._values != null)
            {
                if (_values.Length != other._values.Length)
                    return false;
                for (int i = 0; i < _values.Length; ++i)
                {
                    if (!_values[i].Equals(other._values[i], StringComparison.Ordinal))
                        return false;
                }

                return true;
            }

            return false;
        }
        private void FromValue(ref DynamicStringValue value)
        {
            _type = value._type;
            _behavior = value._choiceBehavior;
            _isKitSelector = value.IsKitSelector;
            if (value._type == DynamicValueType.Constant)
            {
                _value = value._constant;
            }
            else if (value._type == DynamicValueType.Set)
            {
                if (_behavior == ChoiceBehavior.Selective)
                {
                    _value = value._set.Length == 1 ? value._set.Set[0] : value._set.Set[UnityEngine.Random.Range(0, value._set.Length)];
                }
                else
                {
                    _values = value._set.Set;
                }
            }
            else if (value._type == DynamicValueType.Wildcard && _isKitSelector)
            {
                SqlItem<Kit>? rand = KitManager.GetSingletonQuick()?.GetRandomPublicKit();
                _value = rand?.Item is null ? value._constant : rand.Item.Id;
            }
            else
            {
                _value = value._constant;
            }
        }
        /// <summary>Will return <see langword="null"/> if <see cref="DynamicStringValue.SelectionBehavior"/> is <see cref="ChoiceBehavior.Inclusive"/> and the value is not of type <see cref="DynamicValueType.Constant"/>.</summary>
        public readonly string InsistValue() => _value!;
        public readonly bool IsMatch(string value)
        {
            if (_type == DynamicValueType.Wildcard && _behavior == ChoiceBehavior.Inclusive) return true;
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                return _value!.Equals(value, StringComparison.OrdinalIgnoreCase);
            if (_type == DynamicValueType.Set)
            {
                for (int i = 0; i < _values!.Length; i++)
                    if (_values[i].Equals(value, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
        public bool Read(ref Utf8JsonReader reader)
        {
            string? str = reader.GetString();
            if (str == null) return false;
            if (str.Length > 1 && (str[0] == '$' || str[0] == '#'))
            {
                if (string.Equals(str, "$*", StringComparison.Ordinal) || string.Equals(str, "#*", StringComparison.Ordinal))
                {
                    if (str[0] == '#')
                        _behavior = ChoiceBehavior.Inclusive;
                    else
                    {
                        _behavior = ChoiceBehavior.Selective;
                        L.LogWarning("Possibly unintended " + (_isKitSelector ? "kit" : "string") + " singular choice expression in quest choice: $*");
                    }
                    _type = DynamicValueType.Wildcard;
                    return true;
                }

                if (reader.TryReadStringValue(out DynamicStringValue v, _isKitSelector))
                {
                    FromValue(ref v);
                    if (_behavior == ChoiceBehavior.Selective && _type != DynamicValueType.Constant)
                        L.LogWarning("Possibly unintended " + (_isKitSelector ? "kit" : "string") + " singular choice expression in quest choice: " + str);
                    return true;
                }
            }
            else
            {
                _value = str;
                _type = DynamicValueType.Constant;
                _behavior = ChoiceBehavior.Selective;
                return true;
            }
            return false;
        }
        public readonly void Write(Utf8JsonWriter writer)
        {
            if (_behavior == ChoiceBehavior.Selective || _type == DynamicValueType.Constant)
            {
                writer.WriteStringValue(_value);
            }
            else if (_type == DynamicValueType.Wildcard)
            {
                writer.WriteStringValue(_behavior == ChoiceBehavior.Selective ? "$*" : "#*");
            }
            else if (_type == DynamicValueType.Set)
            {
                StringBuilder sb = new StringBuilder(_behavior == ChoiceBehavior.Selective ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values!.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    if (_values[i] != null)
                        sb.Append(_values[i].Replace(',', '_'));
                }
                sb.Append(']');
                writer.WriteStringValue(sb.ToString());
            }
            else
            {
                writer.WriteStringValue(_value);
            }
        }
        public override string ToString()
        {
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                return _value!;
            if (_type == DynamicValueType.Wildcard)
                return _behavior == ChoiceBehavior.Selective ? "$*" : "#*";
            if (_type == DynamicValueType.Set)
            {
                StringBuilder sb = new StringBuilder(_behavior == ChoiceBehavior.Selective ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values!.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    if (_values[i] != null)
                        sb.Append(_values[i].Replace(',', '_'));
                }
                sb.Append(']');
                return sb.ToString();
            }
            return _value!;
        }
        public string GetKitNames(ulong player = 0)
        {
            if (_isKitSelector)
            {
                if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                {
                    if (_kitName == null && !string.IsNullOrEmpty(_value))
                    {
                        SqlItem<Kit>? find = KitManager.GetSingletonQuick()?.FindKitNoLock(_value!, true);
                        Kit? item = find?.Item;
                        if (item != null)
                        {
                            _kitName = GetKitName(item, player);
                            // adds the faction name so kits named 'Rifleman #1', etc show the right team
                            if (item.Type == KitType.Public && item.Faction is { } faction)
                                _kitName = faction.ShortName + " " + _kitName;
                        }
                        else _kitName = _value;
                        _kitName = find?.Item != null ? GetKitName(find.Item, player) : _value!;
                    }
                    else _kitName = _value!;
                    return _kitName;
                }

                if (_type == DynamicValueType.Wildcard)
                {
                    return "any";
                }
                if (_type == DynamicValueType.Set)
                {
                    if (_kitNames == null)
                    {
                        _kitNames = new string[_values!.Length];
                        KitManager? m = KitManager.GetSingletonQuick();
                        if (m != null)
                        {
                            for (int i = 0; i < _values.Length; ++i)
                            {
                                SqlItem<Kit>? find = m.FindKitNoLock(_value!, true);
                                Kit? item = find?.Item;
                                if (item != null)
                                {
                                    _kitNames[i] = GetKitName(item, player);
                                    if (item.Type == KitType.Public && item.Faction is { } faction)
                                        _kitNames[i] = faction.ShortName + " " + _kitNames[i];
                                }
                                else _kitNames[i] = _values[i];
                            }
                        }
                        else
                        {
                            for (int i = 0; i < _values.Length; ++i)
                                _kitNames[i] = _values[i];
                        }
                    }

                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < _kitNames.Length; ++i)
                    {
                        if (i != 0)
                        {
                            if (i != _kitNames.Length - 1)
                                builder.Append(", ");
                            else if (_kitNames.Length > 2)
                                builder.Append(", " + (_behavior == ChoiceBehavior.Selective ? "or" : "and") + " ");
                            else if (_kitNames.Length == 2)
                                builder.Append(" " + (_behavior == ChoiceBehavior.Selective ? "or" : "and") + " ");
                        }

                        builder.Append(_kitNames[i]);
                    }

                    return builder.ToString();
                }
                return _value!;
            }

            return ToString();
        }
        private static string GetKitName(Kit kit, ulong player)
        {
            if (player == 0 || !Data.Languages.TryGetValue(player, out string language))
                language = L.Default;
            return kit.GetDisplayName(language);
        }
    }
}
/// <summary>Datatype storing either a constant <seealso cref="Guid"/> value or a set of <seealso cref="Guid"/> values.
/// <para>Formatted like <see cref="DynamicStringValue"/> with <seealso cref="Guid"/>s.</para>
/// <para>Can also be formatted as "$*" to act as a wildcard, only applicable for some quests.</para></summary>
/// <typeparam name="TAsset">Any <see cref="Asset"/>.</typeparam>
public readonly struct DynamicAssetValue<TAsset> : IDynamicValue<Guid>, IEquatable<DynamicAssetValue<TAsset>> where TAsset : Asset
{
    private readonly Guid _constant;
    private readonly GuidSet _set;
    private readonly DynamicValueType _type;
    private readonly EAssetType _assetType;
    private readonly ChoiceBehavior _choiceBehavior = ChoiceBehavior.Selective;
    public Guid Constant => _constant;
    public IDynamicValue<Guid>.IRange? Range => null;
    public IDynamicValue<Guid>.ISet Set => _set;
    public DynamicValueType ValueType => _type;
    public ChoiceBehavior SelectionBehavior => _choiceBehavior;

    /// <remarks>Don't use this unless you know what you're doing.</remarks>
    internal DynamicAssetValue(DynamicValueType type, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = default;
        _type = type;
        _assetType = GetAssetType();
        _choiceBehavior = choiceBehavior;
    }
    public DynamicAssetValue(Guid constant)
    {
        _constant = constant;
        _set = default;
        _type = DynamicValueType.Constant;
        _assetType = GetAssetType();

    }
    public DynamicAssetValue(GuidSet set, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = set;
        _type = DynamicValueType.Set;
        _assetType = GetAssetType();
        _choiceBehavior = choiceBehavior;
    }
    public DynamicAssetValue(ref GuidSet set, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = set;
        _type = DynamicValueType.Set;
        _assetType = GetAssetType();
        _choiceBehavior = choiceBehavior;
    }
    bool IEquatable<DynamicAssetValue<TAsset>>.Equals(DynamicAssetValue<TAsset> other) => Equals(in other);
    public override bool Equals(object obj) => obj is DynamicAssetValue<TAsset> c && Equals(in c);
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = _constant.GetHashCode();
            hashCode = (hashCode * 397) ^ _set.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)_type;
            hashCode = (hashCode * 397) ^ (int)_assetType;
            hashCode = (hashCode * 397) ^ (int)_choiceBehavior;
            return hashCode;
        }
    }
    public bool Equals(in DynamicAssetValue<TAsset> other)
    {
        if (other._choiceBehavior != _choiceBehavior || other._type != _type)
            return false;

        if (_type == DynamicValueType.Wildcard)
            return true;

        if (_type == DynamicValueType.Set)
        {
            if (_set.Length != other._set.Length)
                return false;
            for (int i = 0; i < _set.Length; ++i)
            {
                if (_set.Set[i] != other._set.Set[i])
                    return false;
            }

            return true;
        }
        if (_type == DynamicValueType.Constant)
            return _constant == other._constant;

        return false;
    }
    public Choice GetValue()
    {
        return new Choice(this);
    }
    public static Choice ReadChoice(ref Utf8JsonReader reader)
    {
        Choice choice = new Choice();
        choice.Read(ref reader);
        return choice;
    }
    private static EAssetType GetAssetType() => AssetTypeHelper<TAsset>.Type;
    IDynamicValue<Guid>.IChoice IDynamicValue<Guid>.GetValue() => GetValue();

    public override string ToString()
    {
        if (_type == DynamicValueType.Constant)
            return _constant.ToString("N");
        if (_type == DynamicValueType.Wildcard)
            return _choiceBehavior == ChoiceBehavior.Selective ? "$*" : "#*";
        if (_type == DynamicValueType.Set)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < _set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(_set.Set[i].ToString("N"));
            }
            sb.Append(']');
            return sb.ToString();
        }
        return _constant.ToString("N");
    }
    public void Write(Utf8JsonWriter writer)
    {
        if (_type == DynamicValueType.Constant)
        {
            writer.WriteStringValue(_constant.ToString("N"));
        }
        else if (_type == DynamicValueType.Wildcard)
        {
            writer.WriteStringValue(_choiceBehavior == ChoiceBehavior.Selective ? "$*" : "#*");
        }
        else if (_type == DynamicValueType.Set)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < _set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(_set.Set[i].ToString("N"));
            }
            sb.Append(']');
            writer.WriteStringValue(sb.ToString());
        }
        else
        {
            writer.WriteStringValue(_constant.ToString("N"));
        }
    }
    public struct Choice : IDynamicValue<Guid>.IChoice
    {
        private ChoiceBehavior _behavior;
        private DynamicValueType _type;
        private EAssetType _assetType;
        private Guid _value;
        private TAsset _valueCache;
        private Guid[]? _values;
        private TAsset[]? _valuesCache;
        private bool _areValuesCached;
        public ChoiceBehavior Behavior => _behavior;
        public DynamicValueType ValueType => _type;
        public Choice(DynamicAssetValue<TAsset> value)
        {
            _value = default;
            _valueCache = null!;
            _values = null;
            _type = default;
            _behavior = default;
            _valuesCache = null;
            _areValuesCached = false;
            _assetType = default;
            FromValue(ref value);
        }
        public override bool Equals(object obj) => obj is Choice c && Equals(in c);
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)_behavior;
                hashCode = (hashCode * 397) ^ (int)_type;
                hashCode = (hashCode * 397) ^ (int)_assetType;
                hashCode = (hashCode * 397) ^ _value.GetHashCode();
                hashCode = (hashCode * 397) ^ (_values != null ? _values.GetHashCode() : 0);
                return hashCode;
            }
        }
        public bool Equals(in Choice other)
        {
            if (other._behavior != _behavior || other._type != _type)
                return false;

            if (_type == DynamicValueType.Wildcard)
                return true;

            if (_behavior == ChoiceBehavior.Selective || _type == DynamicValueType.Constant)
            {
                return _value == other._value;
            }

            if (_type == DynamicValueType.Set && _values != null && other._values != null)
            {
                if (_values.Length != other._values.Length)
                    return false;
                for (int i = 0; i < _values.Length; ++i)
                {
                    if (_values[i] != other._values[i])
                        return false;
                }

                return true;
            }

            return false;
        }
        private void FromValue(ref DynamicAssetValue<TAsset> value)
        {
            _type = value._type;
            _behavior = value._choiceBehavior;
            _assetType = value._assetType;
            _areValuesCached = false;
            if (value._type == DynamicValueType.Constant)
            {
                _value = value._constant;
                if (Level.isLoading)
                {
                    _valueCache = Assets.find<TAsset>(_value);
                    _areValuesCached = _valueCache is not null;
                }
                else
                {
                    _areValuesCached = false;
                }
            }
            else if (value._type == DynamicValueType.Set)
            {
                if (_behavior == ChoiceBehavior.Selective)
                {
                    _value = value._set.Length == 1 ? value._set.Set[0] : value._set.Set[UnityEngine.Random.Range(0, value._set.Length)];
                    if (Level.isLoading)
                    {
                        _valueCache = Assets.find<TAsset>(_value);
                        _areValuesCached = _valueCache is not null;
                    }
                    else
                    {
                        _areValuesCached = false;
                    }
                }
                else
                {
                    _values = value._set.Set;
                    _valuesCache = new TAsset[_values.Length];
                    bool hasNull = false;
                    for (int i = 0; i < _values.Length; ++i)
                    {
                        TAsset? asset = Assets.find<TAsset>(_values[i]);
                        if (asset == null)
                            hasNull = true;
                        else _valuesCache[i] = asset;
                    }
                    _areValuesCached = !hasNull || Level.isLoading;
                }
            }
            else if (value._type == DynamicValueType.Wildcard && value._choiceBehavior == ChoiceBehavior.Selective)
            {
                if (Level.isLoading)
                {
                    List<TAsset> assets = ListPool<TAsset>.claim();
                    try
                    {
                        Assets.find(assets);
                        if (assets.Count == 1)
                            _valueCache = assets[0];
                        else
                            _valueCache = assets[UnityEngine.Random.Range(0, assets.Count)];
                        _value = _valueCache.GUID;
                    }
                    finally
                    {
                        ListPool<TAsset>.release(assets);
                    }
                    _areValuesCached = true;
                }
                else
                {
                    _areValuesCached = false;
                }
            }
            else
            {
                _value = value._constant;
                if (Level.isLoading)
                {
                    _valueCache = Assets.find<TAsset>(_value);
                    _areValuesCached = _valueCache is not null;
                }
                else
                {
                    _areValuesCached = false;
                }
            }
        }
        /// <summary>Will return <see langword="null"/> if <see cref="DynamicAssetValue{TAsset}.SelectionBehavior"/> is <see cref="ChoiceBehavior.Inclusive"/> and the value is not of type <see cref="DynamicValueType.Constant"/>.</summary>
        public readonly Guid InsistValue() => _value;
        public readonly TAsset InsistAssetValue() => (_valueCache ?? (Assets.isLoading ? null : Assets.find<TAsset>(_value)))!;
        public readonly TAsset[] GetAssetValueSet()
        {
            if (_areValuesCached) return _valuesCache!;
            if (_type == DynamicValueType.Set)
            {
                TAsset[] values = new TAsset[_values!.Length];
                for (int i = 0; i < values.Length; i++)
                    values[i] = (!Level.isLoading ? null : Assets.find<TAsset>(_values[i]))!;
                return values;
            }
            return _valuesCache!;
        }
        public readonly string GetCommaList()
        {
            if (_valueCache != null)
                return _valueCache?.FriendlyName ?? "null";
            if (_type == DynamicValueType.Wildcard && _behavior == ChoiceBehavior.Inclusive)
            {
                if (typeof(TAsset) == typeof(ItemAsset))
                    return "any item";
                if (typeof(TAsset) == typeof(VehicleAsset))
                    return "any vehicle";
                if (typeof(TAsset) == typeof(Asset))
                    return "any asset";

                if (_assetType == EAssetType.ITEM)
                    return " any " + typeof(TAsset).Name.Replace("Item", string.Empty).Replace("Asset", string.Empty).ToLower();
                return " any " + typeof(TAsset).Name.Replace("Asset", string.Empty).ToLower();
            }
            if (!_areValuesCached) GetAssetValueSet();
            if (_valuesCache != null)
            {
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < _valuesCache.Length; i++)
                {
                    if (i != 0)
                    {
                        if (i != _valuesCache.Length - 1)
                            builder.Append(", ");
                        else if (_valuesCache.Length > 2)
                            builder.Append(", " + (_behavior == ChoiceBehavior.Selective ? "or" : "and") + " ");
                        else if (_valuesCache.Length == 2)
                            builder.Append(" " + (_behavior == ChoiceBehavior.Selective ? "or" : "and") + " ");
                    }
                    builder.Append(_valuesCache[i]?.FriendlyName ?? "null");
                }
                return builder.ToString();
            }
            return string.Empty;
        }
        public readonly bool IsMatch(Guid value)
        {
            if (_type == DynamicValueType.Wildcard) return true;
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                return _value == value;
            if (_type == DynamicValueType.Set)
            {
                for (int i = 0; i < _values!.Length; i++)
                    if (_values[i] == value) return true;
            }
            return false;
        }
        public readonly bool IsMatch(ushort value)
        {
            if (_type == DynamicValueType.Wildcard) return true;
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                return (_valueCache ?? Assets.find<TAsset>(_value))?.id == value;
            if (_type == DynamicValueType.Set)
            {
                TAsset[] assets = GetAssetValueSet();
                for (int i = 0; i < assets.Length; i++)
                    if (assets[i]?.id == value) return true;
            }
            return false;
        }
        public readonly bool IsMatch(TAsset value)
        {
            if (value == null) return false;
            if (_type == DynamicValueType.Wildcard) return true;
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                return _value == value.GUID;
            if (_type == DynamicValueType.Set)
            {
                for (int i = 0; i < _values!.Length; i++)
                    if (_values[i] == value.GUID) return true;
            }
            return false;
        }
        public bool Read(ref Utf8JsonReader reader)
        {
            string? str = reader.GetString();
            if (str == null) return false;
            if (str.Length > 1 && (str[0] == '$' || str[0] == '#'))
            {
                if (string.Equals(str, "$*", StringComparison.Ordinal) || string.Equals(str, "#*", StringComparison.Ordinal))
                {
                    if (str[0] == '#')
                        _behavior = ChoiceBehavior.Inclusive;
                    else
                    {
                        _behavior = ChoiceBehavior.Selective;
                        L.LogWarning("Possibly unintended " + typeof(TAsset).Name + " singular choice expression in quest choice: $*");
                    }
                    _type = DynamicValueType.Wildcard;
                    return true;
                }

                if (reader.TryReadAssetValue(out DynamicAssetValue<TAsset> v))
                {
                    FromValue(ref v);
                    if (_behavior == ChoiceBehavior.Selective && _type != DynamicValueType.Constant)
                        L.LogWarning("Possibly unintended " + typeof(TAsset).Name + " singular choice expression in quest choice: " + str);
                    return true;
                }
            }
            else if (reader.TryGetGuid(out _value))
            {
                _type = DynamicValueType.Constant;
                _behavior = ChoiceBehavior.Selective;
                return true;
            }
            return false;
        }
        public readonly void Write(Utf8JsonWriter writer)
        {
            if (_behavior == ChoiceBehavior.Selective || _type == DynamicValueType.Constant)
            {
                writer.WriteStringValue(_value);
            }
            else if (_type == DynamicValueType.Wildcard)
            {
                writer.WriteStringValue(_behavior == ChoiceBehavior.Selective ? "$*" : "#*");
            }
            else if (_type == DynamicValueType.Set)
            {
                StringBuilder sb = new StringBuilder(_behavior == ChoiceBehavior.Selective ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values!.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(_values[i].ToString("N"));
                }
                sb.Append(']');
                writer.WriteStringValue(sb.ToString());
            }
            else
            {
                writer.WriteStringValue(_value);
            }
        }
        public readonly override string ToString()
        {
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                return _value.ToString("N");
            if (_type == DynamicValueType.Wildcard)
                return _behavior == ChoiceBehavior.Selective ? "$*" : "#*";
            if (_type == DynamicValueType.Set)
            {
                StringBuilder sb = new StringBuilder(_behavior == ChoiceBehavior.Selective ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values!.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(_values[i].ToString("N"));
                }
                sb.Append(']');
                return sb.ToString();
            }
            return _value.ToString("N");
        }
        public readonly string? ToStringAssetNames()
        {
            if (_type == DynamicValueType.Constant)
                return Assets.find(_value)?.FriendlyName;
            if (_type == DynamicValueType.Wildcard)
                return _behavior == ChoiceBehavior.Selective ? "$*" : "#*";
            if (_type == DynamicValueType.Set)
            {
                StringBuilder sb = new StringBuilder(_behavior == ChoiceBehavior.Selective ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values!.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(Assets.find(_values[i])?.FriendlyName);
                }
                sb.Append(']');
                return sb.ToString();
            }

            return Assets.find(_value)?.FriendlyName;
        }
    }
}
/// <summary>Datatype storing either a constant enum value or a set of enum values.
/// <para>Formatted like <see cref="DynamicStringValue"/>.</para></summary>
/// <typeparam name="TEnum">Any enumeration.</typeparam>
public readonly struct DynamicEnumValue<TEnum> : IDynamicValue<TEnum>, IEquatable<DynamicEnumValue<TEnum>> where TEnum : struct, Enum
{
    private readonly TEnum _constant;
    private readonly EnumSet<TEnum> _set;
    private readonly EnumRange<TEnum> _range;
    private readonly DynamicValueType _type;
    private readonly ChoiceBehavior _choiceBehavior = ChoiceBehavior.Selective;
    public ChoiceBehavior SelectionBehavior => _choiceBehavior;
    public TEnum Constant => _constant;
    public IDynamicValue<TEnum>.IRange Range => _range;
    public IDynamicValue<TEnum>.ISet Set => _set;
    public DynamicValueType ValueType => _type;
    /// <remarks>Don't use this unless you know what you're doing.</remarks>
    internal DynamicEnumValue(DynamicValueType type, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = default;
        _range = default;
        _type = type;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicEnumValue(TEnum constant)
    {
        _constant = constant;
        _set = default;
        _range = default;
        _type = DynamicValueType.Constant;
    }
    public DynamicEnumValue(EnumSet<TEnum> set, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = set;
        _range = default;
        _type = DynamicValueType.Set;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicEnumValue(ref EnumSet<TEnum> set, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = set;
        _range = default;
        _type = DynamicValueType.Set;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicEnumValue(EnumRange<TEnum> range, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = default;
        _range = range;
        _type = DynamicValueType.Range;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicEnumValue(ref EnumRange<TEnum> range, ChoiceBehavior choiceBehavior = ChoiceBehavior.Selective)
    {
        _constant = default;
        _set = default;
        _range = range;
        _type = DynamicValueType.Range;
        _choiceBehavior = choiceBehavior;
    }

    bool IEquatable<DynamicEnumValue<TEnum>>.Equals(DynamicEnumValue<TEnum> other) => Equals(in other);
    public override bool Equals(object obj) => obj is DynamicEnumValue<TEnum> c && Equals(in c);
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = _constant.GetHashCode();
            hashCode = (hashCode * 397) ^ _set.GetHashCode();
            hashCode = (hashCode * 397) ^ _range.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)_type;
            hashCode = (hashCode * 397) ^ (int)_choiceBehavior;
            return hashCode;
        }
    }
    public bool Equals(in DynamicEnumValue<TEnum> other)
    {
        if (other._choiceBehavior != _choiceBehavior || other._type != _type)
            return false;

        if (_type == DynamicValueType.Wildcard)
            return true;

        if (_type == DynamicValueType.Set)
        {
            if (_set.Length != other._set.Length)
                return false;
            for (int i = 0; i < _set.Length; ++i)
            {
                if (_set.Set[i].CompareTo(other._set.Set[i]) != 0)
                    return false;
            }

            return true;
        }
        if (_type == DynamicValueType.Range)
        {
            if (_range.IsInfiniteMax != other._range.IsInfiniteMax || _range.IsInfiniteMin != other._range.IsInfiniteMin)
                return false;
            if (!_range.IsInfiniteMax && _range.Maximum.CompareTo(other._range.Maximum) != 0)
                return false;
            return _range.IsInfiniteMin || _range.Minimum.CompareTo(other._range.Minimum) == 0;
        }
        if (_type == DynamicValueType.Constant)
            return _constant.CompareTo(other._constant) == 0;

        return false;
    }
    public IDynamicValue<TEnum>.IChoice GetValue() => GetValueIntl();
    internal Choice GetValueIntl()
    {
        return new Choice(this);
    }

    public static IDynamicValue<TEnum>.IChoice ReadChoice(ref Utf8JsonReader reader) => ReadChoiceIntl(ref reader);
    internal static Choice ReadChoiceIntl(ref Utf8JsonReader reader)
    {
        Choice choice = new Choice();
        choice.Read(ref reader);
        return choice;
    }
    public override string ToString()
    {
        if (_type == DynamicValueType.Constant)
            return _constant.ToString();
        if (_type == DynamicValueType.Range)
            return (_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#") + "(" + _range.Minimum.ToString() +
                   ":" + _range.Maximum.ToString() + ")";
        if (_type == DynamicValueType.Wildcard)
            return _choiceBehavior == ChoiceBehavior.Selective ? "$*" : "#*";
        if (_type == DynamicValueType.Set)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < _set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(_set.Set[i].ToString());
            }
            sb.Append(']');
            return sb.ToString();
        }
        return _constant.ToString();
    }
    public void Write(Utf8JsonWriter writer)
    {
        if (_type == DynamicValueType.Constant)
        {
            writer.WriteStringValue(_constant.ToString());
        }
        else if (_type == DynamicValueType.Range)
        {
            writer.WriteStringValue((_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#") + "(" + _range.Minimum.ToString() +
                                    ":" + _range.Maximum.ToString() + ")");
        }
        else if (_type == DynamicValueType.Wildcard)
        {
            writer.WriteStringValue(_choiceBehavior == ChoiceBehavior.Selective ? "$*" : "#*");
        }
        else if (_type == DynamicValueType.Set)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == ChoiceBehavior.Selective ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < _set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(_set.Set[i].ToString());
            }
            sb.Append(']');
            writer.WriteStringValue(sb.ToString());
        }
        else
        {
            writer.WriteStringValue(_constant.ToString());
        }
    }

    internal struct Choice : IDynamicValue<TEnum>.IChoice
    {
        private ChoiceBehavior _behavior;
        private DynamicValueType _type;
        private TEnum _value;
        private TEnum[]? _values;
        private TEnum _minVal;
        private TEnum _maxVal;
        private int _minValUnderlying;
        private int _maxValUnderlying;
        public ChoiceBehavior Behavior => _behavior;
        public DynamicValueType ValueType => _type;

        public Choice(DynamicEnumValue<TEnum> value)
        {
            Type t = typeof(TEnum);
            if (t.IsEnum)
            {
                t = t.GetEnumUnderlyingType();
                if (t == typeof(long) || t == typeof(ulong) || t == typeof(uint))
                    throw new ArgumentException(nameof(TEnum), "Type " + typeof(TEnum).Name +
                                                               " derives from an underlying type too large to convert: " + t.Name);
            }
            else throw new ArgumentException(nameof(TEnum), "Type " + typeof(TEnum).Name + " is not an enum type.");
            _value = default;
            _values = null;
            _type = default;
            _behavior = default;
            _minVal = default;
            _maxVal = default;
            _minValUnderlying = default;
            _maxValUnderlying = default;
            FromValue(ref value);
        }
        public override bool Equals(object obj) => obj is Choice c && Equals(in c);
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)_behavior;
                hashCode = (hashCode * 397) ^ (int)_type;
                hashCode = (hashCode * 397) ^ _value.GetHashCode();
                hashCode = (hashCode * 397) ^ (_values != null ? _values.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _minVal.GetHashCode();
                hashCode = (hashCode * 397) ^ _maxVal.GetHashCode();
                hashCode = (hashCode * 397) ^ _minValUnderlying;
                hashCode = (hashCode * 397) ^ _maxValUnderlying;
                return hashCode;
            }
        }
        public bool Equals(in Choice other)
        {
            if (other._behavior != _behavior || other._type != _type)
                return false;

            if (_type == DynamicValueType.Wildcard)
                return true;

            if (_behavior == ChoiceBehavior.Selective || _type == DynamicValueType.Constant)
            {
                return _value.CompareTo(other._value) == 0;
            }

            if (_type == DynamicValueType.Set && _values != null && other._values != null)
            {
                if (_values.Length != other._values.Length)
                    return false;
                for (int i = 0; i < _values.Length; ++i)
                {
                    if (_values[i].CompareTo(other._values[i]) != 0)
                        return false;
                }

                return true;
            }

            return false;
        }
        private void FromValue(ref DynamicEnumValue<TEnum> value)
        {
            _type = value._type;
            _behavior = value._choiceBehavior;
            if (value._type == DynamicValueType.Constant)
            {
                _value = value._constant;
            }
            else if (value._type == DynamicValueType.Set)
            {
                if (_behavior == ChoiceBehavior.Selective)
                {
                    _value = value._set.Length == 1 ? value._set.Set[0] : value._set.Set[UnityEngine.Random.Range(0, value._set.Length)];
                }
                else
                {
                    _values = value._set.Set;
                }
            }
            else if (value._type == DynamicValueType.Range)
            {
                if (_behavior == ChoiceBehavior.Selective)
                {

                    int r1 = ((IConvertible)value.Range.Minimum).ToInt32(Data.AdminLocale);
                    int r2 = ((IConvertible)value.Range.Maximum).ToInt32(Data.AdminLocale);
                    int r3 = UnityEngine.Random.Range(r1, r2 + 1);
                    _value = (TEnum)Enum.ToObject(typeof(TEnum), r3);
                }
                else
                {
                    _minValUnderlying = ((IConvertible)value.Range.Minimum).ToInt32(Data.AdminLocale);
                    _minVal = value.Range.Minimum;
                    _maxValUnderlying = ((IConvertible)value.Range.Maximum).ToInt32(Data.AdminLocale);
                    _maxVal = value.Range.Maximum;
                }
            }
            else if (value._type == DynamicValueType.Wildcard && _behavior == ChoiceBehavior.Selective)
            {
                Array arr = Enum.GetValues(typeof(TEnum));
                int r = UnityEngine.Random.Range(0, arr.Length);
                _value = (TEnum)arr.GetValue(r);
            }
            else
            {
                _value = value._constant;
            }
        }
        /// <summary>Will return <see langword="null"/> if <see cref="DynamicEnumValue{TEnum}.SelectionBehavior"/> is <see cref=ChoiceBehavior.InclusiveL"/> and the value is not of type <see cref=DynamicValueType.ConstantT"/>.</summary>
        public TEnum InsistValue() => _value;
        public readonly bool IsMatch(TEnum value)
        {
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                return _value.Equals(value);
            if (_type == DynamicValueType.Wildcard) return true;
            if (_type == DynamicValueType.Range)
            {
                int r = (int)(object)value;
                return r >= _minValUnderlying && r <= _maxValUnderlying;
            }
            if (_type == DynamicValueType.Set)
            {
                for (int i = 0; i < _values!.Length; i++)
                    if (_value.Equals(value)) return true;
            }

            return false;
        }
        public bool Read(ref Utf8JsonReader reader)
        {
            string? str = reader.GetString();
            if (str == null) return false;
            if (str.Length > 1 && (str[0] == '$' || str[0] == '#'))
            {
                if (string.Equals(str, "$*", StringComparison.Ordinal) || string.Equals(str, "#*", StringComparison.Ordinal))
                {
                    if (str[0] == '#')
                        _behavior = ChoiceBehavior.Inclusive;
                    else
                    {
                        _behavior = ChoiceBehavior.Selective;
                        L.LogWarning("Possibly unintended " + typeof(TEnum).Name + " singular choice expression in quest choice: $*");
                    }
                    _type = DynamicValueType.Wildcard;
                    return true;
                }

                if (reader.TryReadEnumValue(out DynamicEnumValue<TEnum> v))
                {
                    FromValue(ref v);
                    if (_behavior == ChoiceBehavior.Selective && _type != DynamicValueType.Constant)
                        L.LogWarning("Possibly unintended " + typeof(TEnum).Name + " singular choice expression in quest choice: " + str);
                    return true;
                }
            }
            else if (Enum.TryParse(str, true, out _value))
            {
                _type = DynamicValueType.Constant;
                _behavior = ChoiceBehavior.Selective;
                return true;
            }
            return false;
        }
        public readonly void Write(Utf8JsonWriter writer)
        {
            if (_behavior == ChoiceBehavior.Selective || _type == DynamicValueType.Constant)
            {
                writer.WriteStringValue(_value.ToString());
            }
            else if (_type == DynamicValueType.Wildcard)
            {
                writer.WriteStringValue(_behavior == ChoiceBehavior.Selective ? "$*" : "#*");
            }
            else if (_type == DynamicValueType.Range)
            {
                writer.WriteStringValue((_behavior == ChoiceBehavior.Selective ? "$(" : "#(") + _minVal.ToString() + ":" + _maxVal + ")");
            }
            else if (_type == DynamicValueType.Set)
            {
                StringBuilder sb = new StringBuilder(_behavior == ChoiceBehavior.Selective ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values!.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(_values[i].ToString());
                }
                sb.Append(']');
                writer.WriteStringValue(sb.ToString());
            }
            else
            {
                writer.WriteStringValue(_value.ToString());
            }
        }
        public readonly override string ToString()
        {
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
                return Localization.TranslateEnum(_value, 0).ToLower();
            if (_type == DynamicValueType.Range)
                return Localization.TranslateEnum(_minVal, 0).ToLower() + " to " + Localization.TranslateEnum(_maxVal, 0).ToLower();
            
            if (_type == DynamicValueType.Wildcard)
                return "any " + Localization.TranslateEnumName(typeof(TEnum), 0).ToLower();

            if (_type == DynamicValueType.Set)
            {
                StringBuilder sb = new StringBuilder(_values!.Length * 12);
                for (int i = 0; i < _values.Length; i++)
                {
                    if (i != 0) sb.Append(", ");
                    sb.Append(Localization.TranslateEnum(_values[i], 0).ToLower());
                }
                return sb.ToString();
            }
            return _value.ToString();
        }
        public string GetCommaList(ulong player)
        {
            if (_type == DynamicValueType.Constant || _behavior == ChoiceBehavior.Selective)
            {
                return Localization.TranslateEnum(_value, player);
            }
            if (_type == DynamicValueType.Wildcard)
            {
                return "any";
            }
            if (_type == DynamicValueType.Set)
            {
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < _values!.Length; ++i)
                {
                    if (i != 0)
                    {
                        if (i != _values.Length - 1)
                            builder.Append(", ");
                        else if (_values.Length > 2)
                            builder.Append(", " + (_behavior == ChoiceBehavior.Selective ? "or" : "and") + " ");
                        else if (_values.Length == 2)
                            builder.Append(" " + (_behavior == ChoiceBehavior.Selective ? "or" : "and") + " ");
                    }

                    builder.Append(Localization.TranslateEnum(_values[i], player));
                }

                return builder.ToString();
            }
            return Localization.TranslateEnum(_value, player);
        }
    }
}

public enum DynamicValueType
{
    /// <summary>Entered as a constant value with no extra symbols.<code>3</code></summary>
    Constant,
    /// <summary>Entered with parenthesis.<code>$(12:16)</code></summary>
    Range,
    /// <summary>Entered with square brackets.<code>#[usrif1,usrif2,usrif3]</code></summary>
    Set,
    /// <summary>Represented by an asterisk.<code>$*</code></summary>
    Wildcard
}
// ReSharper disable once TypeParameterCanBeVariant (literally just incorrect)
public interface IDynamicValue<T>
{
    public T Constant { get; }
    public IRange? Range { get; }
    public interface IRange
    {
        public T Minimum { get; }
        public T Maximum { get; }
        public bool IsInfiniteMin { get; }
        public bool IsInfiniteMax { get; }
    }
    public ISet? Set { get; }
    public interface ISet
    {
        public T[] Set { get; }
        public int Length { get; }
    }
    public DynamicValueType ValueType { get; }
    public ChoiceBehavior SelectionBehavior { get; }
    public IChoice GetValue();
    public void Write(Utf8JsonWriter writer);
    public interface IChoice
    {
        public ChoiceBehavior Behavior { get; }
        public DynamicValueType ValueType { get; }
        public bool IsMatch(T value);
        public T InsistValue();
        public void Write(Utf8JsonWriter writer);
        public bool Read(ref Utf8JsonReader reader);
        public string ToString();
    }
}
public enum ChoiceBehavior : byte
{
    /// <summary>Represented by a <see langword='$'/>. Selects one item from the dynamic value.</summary>
    Selective,
    /// <summary>Represented by a <see langword='#'/>. Selects any item that matches the dynamic value.</summary>
    Inclusive
}

/// <summary>Datatype storing a range of integers.
/// <para>Formatted like this: </para><code>"$(3:182)"</code></summary>
public readonly struct IntegralRange : IDynamicValue<int>.IRange
{
    private readonly int _min;
    private readonly int _round;
    private readonly int _max;
    private readonly bool _isInfiniteMin;
    private readonly bool _isInfiniteMax;
    public int Minimum => _min;
    public int Maximum => _max;
    public int Round => _round;
    public bool IsInfiniteMin => _isInfiniteMin;
    public bool IsInfiniteMax => _isInfiniteMax;
    public IntegralRange(int min, int max, int round)
    {
        _min = min;
        _max = max;
        _round = round;
        _isInfiniteMin = _isInfiniteMax = false;
    }
    public IntegralRange(int min, int max, int round, bool isInfiniteMin, bool isInfiniteMax)
    {
        _min = min;
        _max = max;
        _round = round;
        _isInfiniteMin = isInfiniteMin;
        _isInfiniteMax = isInfiniteMax;
    }
    public override string ToString() => "$(" + (_isInfiniteMin ? string.Empty : _min.ToString(Data.AdminLocale)) + ":" + (_isInfiniteMax ? string.Empty : _max.ToString(Data.AdminLocale)) + ")" + (_round == 0 ? string.Empty : ("{" + _round.ToString(Data.AdminLocale) + "}"));
}
/// <summary>Datatype storing a range of floats.
/// <para>Formatted like this: </para><code>"$(3.2:182.1)"</code></summary>
public readonly struct FloatRange : IDynamicValue<float>.IRange
{
    private readonly float _min;
    private readonly float _max;
    private readonly int _round;
    private readonly bool _isInfiniteMin;
    private readonly bool _isInfiniteMax;
    public float Minimum => _min;
    public float Maximum => _max;
    public int Round => _round;
    public bool IsInfiniteMin => _isInfiniteMin;
    public bool IsInfiniteMax => _isInfiniteMax;
    public FloatRange(float min, float max, int round)
    {
        _min = min;
        _max = max;
        _round = round;
        _isInfiniteMin = _isInfiniteMax = false;
    }
    public FloatRange(float min, float max, int round, bool isInfiniteMin, bool isInfiniteMax)
    {
        _min = min;
        _max = max;
        _round = round;
        _isInfiniteMin = isInfiniteMin;
        _isInfiniteMax = isInfiniteMax;
    }
    public override string ToString() => "$(" + (_isInfiniteMin ? string.Empty : _min.ToString(Data.AdminLocale)) + ":" + (_isInfiniteMax ? string.Empty : _max.ToString(Data.AdminLocale)) + ")" + (_round == 0 ? string.Empty : ("{" + _round.ToString(Data.AdminLocale) + "}"));
}
/// <summary>Datatype storing a range of floats.
/// <para>Formatted like this: </para><code>"$(3.2:182.1)"</code></summary>
public readonly struct EnumRange<TEnum> : IDynamicValue<TEnum>.IRange where TEnum : struct, Enum
{
    private readonly TEnum _min;
    private readonly TEnum _max;
    private readonly bool _isInfiniteMin;
    private readonly bool _isInfiniteMax;
    public TEnum Minimum => _min;
    public TEnum Maximum => _max;
    public bool IsInfiniteMin => _isInfiniteMin;
    public bool IsInfiniteMax => _isInfiniteMax;
    public EnumRange(TEnum min, TEnum max)
    {
        _min = min;
        _max = max;
        _isInfiniteMin = _isInfiniteMax = false;
    }
    public EnumRange(TEnum min, TEnum max, bool isInfiniteMin, bool isInfiniteMax)
    {
        _min = min;
        _max = max;
        _isInfiniteMin = isInfiniteMin;
        _isInfiniteMax = isInfiniteMax;
    }
    public override string ToString() => "$(" + (_isInfiniteMin ? string.Empty : _min.ToString()) + ":" + (_isInfiniteMax ? string.Empty : _max.ToString()) + ")";
}
/// <summary>Datatype storing an array or set of integers.
/// <para>Formatted like this: </para><code>"$[1,8,3,9123]"</code></summary>
public readonly struct IntegralSet : IEnumerable<int>, IDynamicValue<int>.ISet
{
    private readonly int[] _set;
    public int[] Set => _set;
    private readonly int _length;
    public int Length => _length;
    public IntegralSet(int[] set)
    {
        _set = set;
        _length = set.Length;
    }
    public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)_set).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder("$[");
        for (int i = 0; i < _length; i++)
        {
            if (i != 0) sb.Append(',');
            sb.Append(_set[i].ToString(Data.AdminLocale));
        }
        sb.Append(']');
        return sb.ToString();
    }
}
/// <summary>Datatype storing an array or set of floats.
/// <para>Formatted like this: </para><code>"$[1.2,8.1,3,9123.10]"</code></summary>
public readonly struct FloatSet : IEnumerable<float>, IDynamicValue<float>.ISet
{
    private readonly float[] _set;
    public float[] Set => _set;
    private readonly int _length;
    public int Length => _length;
    public FloatSet(float[] set)
    {
        _set = set;
        _length = set.Length;
    }
    public IEnumerator<float> GetEnumerator() => ((IEnumerable<float>)_set).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder("$[");
        for (int i = 0; i < _length; i++)
        {
            if (i != 0) sb.Append(',');
            sb.Append(_set[i].ToString(Data.AdminLocale));
        }
        sb.Append(']');
        return sb.ToString();
    }
}
/// <summary>Datatype storing an array or set of strings.
/// <para>Formatted like this: </para><code>"$[string1,ben smells,18,nice]"</code></summary>
public readonly struct StringSet : IEnumerable<string>, IDynamicValue<string>.ISet
{
    private readonly string[] _set;
    public string[] Set => _set;
    private readonly int _length;
    public int Length => _length;
    public StringSet(string[] set)
    {
        _set = set;
        _length = set.Length;
    }
    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)_set).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder("$[");
        for (int i = 0; i < _length; i++)
        {
            if (i != 0) sb.Append(',');
            if (_set[i] != null)
                sb.Append(_set[i].Replace(',', '_'));
        }
        sb.Append(']');
        return sb.ToString();
    }
}
/// <summary>Datatype storing an array or set of enums.
/// <para>Formatted like this: </para><code>"$[string1,ben smells,18,nice]"</code></summary>
public readonly struct EnumSet<TEnum> : IEnumerable<TEnum>, IDynamicValue<TEnum>.ISet where TEnum : struct, Enum
{
    private readonly TEnum[] _set;
    public TEnum[] Set => _set;
    private readonly int _length;
    public int Length => _length;
    public EnumSet(TEnum[] set)
    {
        _set = set;
        _length = set.Length;
    }
    public IEnumerator<TEnum> GetEnumerator() => ((IEnumerable<TEnum>)_set).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder("$[");
        for (int i = 0; i < _length; i++)
        {
            if (i != 0) sb.Append(',');
            sb.Append(_set[i].ToString());
        }
        sb.Append(']');
        return sb.ToString();
    }
}
/// <summary>Datatype storing an array or set of guids that should reference Assets.
/// <para>Formatted like this: </para><code>"$[6cba0660fac347b8a849157eac941411,7c58b04bd8b046ee982cef9880722d22]"</code></summary>
public readonly struct GuidSet : IEnumerable<Guid>, IDynamicValue<Guid>.ISet
{
    private readonly Guid[] _set;
    private readonly int _length;
    public Guid[] Set => _set;
    public int Length => _length;
    public GuidSet(Guid[] set)
    {
        _set = set;
        _length = set.Length;
    }
    public IEnumerator<Guid> GetEnumerator() => ((IEnumerable<Guid>)_set).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder("$[");
        for (int i = 0; i < _length; i++)
        {
            if (i != 0) sb.Append(',');
            sb.Append(_set[i].ToString("N"));
        }
        sb.Append(']');
        return sb.ToString();
    }
}
public interface INotifyTracker
{
    public UCPlayer? Player { get; }
}
public interface IQuestPreset
{
    public Guid Key { get; }
    public IQuestState State { get; }
    public IQuestReward[]? RewardOverrides { get; }
    public ulong Team { get; }
    public ushort Flag { get; }
}

#region Notification Interfaces
public interface INotifyOnKill : INotifyTracker
{
    public void OnKill(PlayerDied e);
}
public interface INotifyOnDeath : INotifyTracker
{
    public void OnDeath(PlayerDied e);
}
public interface INotifyOnObjectiveCaptured : INotifyTracker
{
    public void OnObjectiveCaptured(ulong[] participants);
}
public interface INotifyOnFlagNeutralized : INotifyTracker
{
    public void OnFlagNeutralized(ulong[] participants, ulong neutralizer);
}
public interface INotifyGameOver : INotifyTracker
{
    public void OnGameOver(ulong winner);
}
public interface INotifyRallyActive : INotifyTracker
{
    public void OnRallyActivated(RallyPoint rally);
}
public interface INotifyBunkerSpawn : INotifyTracker
{
    public void OnPlayerSpawnedAtBunker(BunkerComponent component, UCPlayer spawner);
}
public interface INotifyGainedXP : INotifyTracker
{
    public void OnGainedXP(UCPlayer player, int amtGained, int total, int gameTotal);
}
public interface INotifyFOBBuilt : INotifyTracker
{
    public void OnFOBBuilt(UCPlayer constructor, FOB fob);
}
public interface INotifySuppliesConsumed : INotifyTracker
{
    public void OnSuppliesConsumed(FOB fob, ulong player, int amount);
}
public interface INotifyOnRevive : INotifyTracker
{
    public void OnPlayerRevived(UCPlayer reviver, UCPlayer revived);
}
public interface INotifyBuildableBuilt : INotifyTracker
{
    public void OnBuildableBuilt(UCPlayer player, BuildableData buildable);
}
public interface INotifyVehicleDestroyed : INotifyTracker
{
    public void OnVehicleDestroyed(VehicleDestroyed e, UCPlayer instigator);
}
public interface INotifyVehicleDistanceUpdates : INotifyTracker
{
    public void OnDistanceUpdated(ulong lastDriver, float totalDistance, float newDistance, VehicleComponent vehicle);
}
#endregion
/// <summary>Stores information about the values of variations of <see cref="BaseQuestData"/>.</summary>
public interface IQuestState
{
    public IDynamicValue<int>.IChoice FlagValue { get; }
    public void WriteQuestState(Utf8JsonWriter writer);
    public void OnPropertyRead(ref Utf8JsonReader reader, string property);
}
/// <inheritdoc/>
/// <typeparam name="TTracker">Class deriving from <see cref="BaseQuestTracker"/> used to track progress.</typeparam>
/// <typeparam name="TDataNew">Class deriving from <see cref="BaseQuestData"/> used as a template for random variations to be created.</typeparam>
public interface IQuestState<TTracker, in TDataNew> : IQuestState where TTracker : BaseQuestTracker where TDataNew : BaseQuestData
{
    public void Init(TDataNew data);
    public bool IsEligable(UCPlayer player);
}