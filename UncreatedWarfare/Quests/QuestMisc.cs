using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests.Types;
using Uncreated.Warfare.Squads;
using UnityEngine;

namespace Uncreated.Warfare.Quests;

[Translatable("Weapon Class")]
public enum EWeaponClass : byte
{
    UNKNOWN,
    ASSAULT_RIFLE,
    BATTLE_RIFLE,
    MARKSMAN_RIFLE,
    SNIPER_RIFLE,
    MACHINE_GUN,
    PISTOL,
    SHOTGUN,
    [Translatable("Rocket Launcher")]
    ROCKET,
    [Translatable("SMG")]
    SMG
}
[Translatable("Quest Type")]
public enum EQuestType : byte
{
    INVALID,
    /// <summary><see cref="KillEnemiesQuest"/></summary>
    KILL_ENEMIES,
    /// <summary><see cref="KillEnemiesQuestWeapon"/></summary>
    KILL_ENEMIES_WITH_WEAPON,
    /// <summary><see cref="KillEnemiesQuestKit"/></summary>
    KILL_ENEMIES_WITH_KIT,
    /// <summary><see cref="KillEnemiesQuestKitClass"/></summary>
    KILL_ENEMIES_WITH_KIT_CLASS,
    /// <summary><see cref="KillEnemiesQuestWeaponClass"/></summary>
    KILL_ENEMIES_WITH_WEAPON_CLASS,
    /// <summary><see cref="KillEnemiesQuestBranch"/></summary>
    KILL_ENEMIES_WITH_BRANCH,
    /// <summary><see cref="KillEnemiesQuestTurret"/></summary>
    KILL_ENEMIES_WITH_TURRET,
    /// <summary><see cref="KillEnemiesQuestSquad"/></summary>
    KILL_ENEMIES_IN_SQUAD,
    /// <summary><see cref="KillEnemiesQuestFullSquad"/></summary>
    KILL_ENEMIES_IN_FULL_SQUAD,
    /// <summary><see cref="KillEnemiesQuestDefense"/></summary>
    [Translatable("Kill Enemies While Defending Point")]
    KILL_ENEMIES_ON_POINT_DEFENSE,
    /// <summary><see cref="KillEnemiesQuestAttack"/></summary>
    [Translatable("Kill Enemies While Attacking Point")]
    KILL_ENEMIES_ON_POINT_ATTACK,
    /// <summary><see cref="HelpBuildQuest"/></summary>
    SHOVEL_BUILDABLES,
    /// <summary><see cref="BuildFOBsQuest"/></summary>
    [Translatable("Build FOBs")]
    BUILD_FOBS,
    /// <summary><see cref="BuildFOBsNearObjQuest"/></summary>
    [Translatable("Build FOBs Near Objectives")]
    BUILD_FOBS_NEAR_OBJECTIVES,
    /// <summary><see cref="BuildFOBsOnObjQuest"/></summary>
    [Translatable("Build FOBs Near Current Objective")]
    BUILD_FOB_ON_ACTIVE_OBJECTIVE,
    /// <summary><see cref="DeliverSuppliesQuest"/></summary>
    DELIVER_SUPPLIES,
    /// <summary><see cref="CaptureObjectivesQuest"/></summary>
    CAPTURE_OBJECTIVES,
    /// <summary><see cref="DestroyVehiclesQuest"/></summary>
    DESTROY_VEHICLES,
    /// <summary><see cref="DriveDistanceQuest"/></summary>
    DRIVE_DISTANCE,
    /// <summary><see cref="TransportPlayersQuest"/></summary>
    TRANSPORT_PLAYERS,
    /// <summary><see cref="RevivePlayersQuest"/></summary>
    REVIVE_PLAYERS,
    /// <summary><see cref="KingSlayerQuest"/></summary>
    [Translatable("King-slayer")]
    KING_SLAYER,
    /// <summary><see cref="KillStreakQuest"/></summary>
    [Translatable("Killstreak")]
    KILL_STREAK,
    /// <summary><see cref="XPInGamemodeQuest"/></summary>
    [Translatable("Earn XP From Gamemode")]
    XP_IN_GAMEMODE,
    /// <summary><see cref="KillEnemiesRangeQuest"/></summary>
    [Translatable("Kill From Distance")]
    KILL_FROM_RANGE,
    /// <summary><see cref="KillEnemiesRangeQuestWeapon"/></summary>
    [Translatable("Kill From Distance With Weapon")]
    KILL_FROM_RANGE_WITH_WEAPON,
    /// <summary><see cref="KillEnemiesQuestKitClassRange"/></summary>
    [Translatable("Kill From Distance With Class")]
    KILL_FROM_RANGE_WITH_CLASS,
    /// <summary><see cref="KillEnemiesQuestKitRange"/></summary>
    [Translatable("Kill From Distance With Kit")]
    KILL_FROM_RANGE_WITH_KIT,
    /// <summary><see cref="RallyUseQuest"/></summary>
    [Translatable("Teammates Use Rallypoint")]
    TEAMMATES_DEPLOY_ON_RALLY,
    /// <summary><see cref="FOBUseQuest"/></summary>
    [Translatable("Teammates Use FOB")]
    TEAMMATES_DEPLOY_ON_FOB,
    /// <summary><see cref="NeutralizeFlagsQuest"/></summary>
    NEUTRALIZE_FLAGS,
    /// <summary><see cref="WinGamemodeQuest"/></summary>
    WIN_GAMEMODE,
    /// <summary><see cref="DiscordKeySetQuest"/></summary>
    [Translatable("Custom Key")]
    DISCORD_KEY_SET_BOOL,
    /// <summary><see cref="PlaceholderQuest"/></summary>
    PLACEHOLDER
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class QuestDataAttribute : Attribute
{
    public EQuestType Type { get; }
    public QuestDataAttribute(EQuestType type)
    {
        Type = type;
    }
}
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class QuestRewardAttribute : Attribute
{
    public EQuestRewardType Type { get; }
    public Type ReturnType { get; }
    public QuestRewardAttribute(EQuestRewardType type, Type returnType)
    {
        Type = type;
        ReturnType = returnType;
    }
}

public static class QuestJsonEx
{
    public static EWeaponClass GetWeaponClass(this Guid item)
    {
        if (Assets.find(item) is ItemGunAsset weapon)
        {
            if (weapon.action == EAction.Pump)
            {
                return EWeaponClass.SHOTGUN;
            }

            if (weapon.action == EAction.Rail)
            {
                return EWeaponClass.SNIPER_RIFLE;
            }
            if (weapon.action == EAction.Minigun)
            {
                return EWeaponClass.MACHINE_GUN;
            }
            if (weapon.action == EAction.Rocket)
            {
                return EWeaponClass.ROCKET;
            }
            if (weapon.itemDescription.IndexOf("smg", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return EWeaponClass.SMG;
            }
            if (weapon.itemDescription.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return EWeaponClass.PISTOL;
            }
            if (weapon.itemDescription.IndexOf("marksman", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return EWeaponClass.MARKSMAN_RIFLE;
            }
            if (weapon.itemDescription.IndexOf("rifle", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return EWeaponClass.BATTLE_RIFLE;
            }
            if (weapon.itemDescription.IndexOf("machine", StringComparison.OrdinalIgnoreCase) != -1 || weapon.itemDescription.IndexOf("lmg", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return EWeaponClass.MACHINE_GUN;
            }
        }

        return EWeaponClass.UNKNOWN;
    }
    public static unsafe bool TryReadIntegralValue(this ref Utf8JsonReader reader, out DynamicIntegerValue value)
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
                    value = new DynamicIntegerValue(EDynamicValueType.ANY, str![0] == '$' ? EChoiceBehavior.ALLOW_ONE : EChoiceBehavior.ALLOW_ALL);
                }
                else if (int.TryParse(str, NumberStyles.Any, Data.Locale, out int v1))
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

            bool isInclusive = str[0] == '#';

            if (isInclusive || str[0] == '$')
            {
                int endInd = str.IndexOf(')', 1);
                bool hasRnd = str[str.Length - 1] == '}';
                int stRndInd = endInd + 1;
                if (str[1] == '(' && endInd != -1) // read range
                {
                    int sep = str.IndexOf(':', 3);
                    if (sep != -1)
                    {
                        int v1 = 0;
                        int v2 = 0;
                        fixed (char* p = str)
                        {
                            int m = 1;
                            for (int i = sep - 1; i > 1; i--)
                            {
                                char c = *(p + i);
                                if (c >= '0' && c <= '9')
                                {
                                    v1 += (c - 48) * m;
                                    m *= 10;
                                }
                                else if (c == '-')
                                {
                                    v1 = -v1;
                                    break;
                                }
                            }
                        }

                        fixed (char* p = str)
                        {
                            int m = 1;
                            for (int i = endInd; i > sep; i--)
                            {
                                char c = *(p + i);
                                if (c >= '0' && c <= '9')
                                {
                                    v1 += (c - 48) * m;
                                    m *= 10;
                                }
                                else if (c == '-')
                                {
                                    v1 = -v1;
                                    break;
                                }
                            }
                        }

                        if (v1 > v2)
                        {
                            (v2, v1) = (v1, v2);
                        }

                        int round = 0;
                        if (hasRnd)
                        {
                            int.TryParse(str.Substring(stRndInd + 1, str.Length - 1 - stRndInd),
                                NumberStyles.Number, Data.Locale, out round);
                        }
                        value = new DynamicIntegerValue(new IntegralRange(v1, v2, round), isInclusive ? EChoiceBehavior.ALLOW_ALL : EChoiceBehavior.ALLOW_ONE);
                        return true;
                    }

                    value = new DynamicIntegerValue(0);
                    return false;
                }

                if (str[1] == '[' && str[str.Length - 1] == ']')
                {
                    int len = str.Length;
                    int arrLen = 1;
                    int[] res;
                    fixed (char* p = str)
                    {
                        for (int i = 0; i < len; i++)
                            if (str[i] == ',')
                                arrLen++;
                        res = new int[arrLen];
                        int ptrpos = str.Length - 1;
                        int index = arrLen;
                        int num = 0;
                        int m = 1;
                        while (ptrpos > 1)
                        {
                            ptrpos--;
                            char c = *(p + ptrpos);
                            if (c is >= '0' and <= '9')
                            {
                                num += (c - 48) * m;
                                m *= 10;
                            }
                            else if (c == '-')
                            {
                                num = -num;
                            }
                            else if (c == ',' || c == ']')
                            {
                                index--;
                                res[index] = num;
                            }
                        }
                    }

                    value = new DynamicIntegerValue(new IntegralSet(res), isInclusive ? EChoiceBehavior.ALLOW_ALL : EChoiceBehavior.ALLOW_ONE);
                    return true;

                }
                value = new DynamicIntegerValue(0);
                return false;
            }

            if (int.TryParse(str, NumberStyles.Any, Data.Locale, out int v))
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
    public static unsafe bool TryReadFloatValue(this ref Utf8JsonReader reader, out DynamicFloatValue value)
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
                    value = new DynamicFloatValue(EDynamicValueType.ANY, str![0] == '$' ? EChoiceBehavior.ALLOW_ONE : EChoiceBehavior.ALLOW_ALL);
                }
                else if (float.TryParse(str, NumberStyles.Any, Data.Locale, out float v1))
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

            bool isInclusive = str[0] == '#';

            if (isInclusive || str[0] == '$')
            {
                int endInd = str.IndexOf(')', 1);
                bool hasRnd = str[str.Length - 1] == '}';
                int stRndInd = endInd + 1;
                if (str[1] == '(' && endInd != -1) // read range
                {
                    int sep = str.IndexOf(':');
                    if (sep != -1)
                    {
                        int v1 = 0;
                        int v2 = 0;
                        float v1f = 0;
                        float v2f = 0;
                        fixed (char* p = str)
                        {
                            int m = 1;
                            int decPlace = 1;
                            for (int i = sep - 1; i > 1; i--)
                            {
                                char c = *(p + i);
                                if (c >= '0' && c <= '9')
                                {
                                    v1 += (c - 48) * m;
                                    m *= 10;
                                }
                                else if (c == '-')
                                {
                                    v1 = -v1;
                                    break;
                                }
                                else if (c == '.')
                                {
                                    decPlace = m;
                                }
                            }
                            v1f = v1 / (float)decPlace;
                        }

                        fixed (char* p = str)
                        {
                            int m = 1;
                            int decPlace = 1;
                            for (int i = endInd; i > sep; i--)
                            {
                                char c = *(p + i);
                                if (c >= '0' && c <= '9')
                                {
                                    v1 += (c - 48) * m;
                                    m *= 10;
                                }
                                else if (c == '-')
                                {
                                    v1 = -v1;
                                    break;
                                }
                                else if (c == '.')
                                {
                                    decPlace = m;
                                }
                            }
                            v2f = v2 / (float)decPlace;
                        }

                        if (v1f > v2f)
                        {
                            (v2f, v1f) = (v1f, v2f);
                        }

                        int round = 0;
                        if (hasRnd)
                        {
                            int.TryParse(str.Substring(stRndInd + 1, str.Length - 1 - stRndInd),
                                NumberStyles.Number, Data.Locale, out round);
                        }

                        value = new DynamicFloatValue(new FloatRange(v1f, v2f, round), isInclusive ? EChoiceBehavior.ALLOW_ALL : EChoiceBehavior.ALLOW_ONE);
                        return true;
                    }

                    value = new DynamicFloatValue(0);
                    return false;
                }

                if (str[1] == '[' && str[str.Length - 1] == ']')
                {
                    int len = str.Length;
                    int arrLen = 1;
                    float[] res;
                    fixed (char* p = str)
                    {
                        for (int i = 0; i < len; i++)
                            if (str[i] == ',')
                                arrLen++;
                        res = new float[arrLen];
                        int ptrpos = str.Length - 1;
                        int index = arrLen;
                        while (ptrpos > 1)
                        {
                            ptrpos--;
                            int num = 0;
                            int m = 1;
                            int decPlace = 1;
                            char c = *(p + ptrpos);
                            if (c >= '0' && c <= '9')
                            {
                                num += (c - 48) * m;
                                m *= 10;
                            }
                            else if (c == '-')
                            {
                                num = -num;
                                break;
                            }
                            else if (c == '.')
                            {
                                decPlace = m;
                            }
                            else if (c == ',' || c == '[')
                            {
                                index--;
                                res[index] = (float)num / decPlace;
                            }
                        }
                    }
                    value = new DynamicFloatValue(new FloatSet(res), isInclusive ? EChoiceBehavior.ALLOW_ALL : EChoiceBehavior.ALLOW_ONE);
                    return true;

                }
                value = new DynamicFloatValue(0);
                return false;
            }

            if (float.TryParse(str, NumberStyles.Any, Data.Locale, out float v))
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
                    value = new DynamicStringValue(isKitselector, EDynamicValueType.ANY, str![0] == '$' ? EChoiceBehavior.ALLOW_ONE : EChoiceBehavior.ALLOW_ALL);
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
                    value = new DynamicStringValue(isKitselector, new StringSet(res), isInclusive ? EChoiceBehavior.ALLOW_ALL : EChoiceBehavior.ALLOW_ONE);
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
                    value = new DynamicEnumValue<TEnum>(EDynamicValueType.ANY, str![0] == '$' ? EChoiceBehavior.ALLOW_ONE : EChoiceBehavior.ALLOW_ALL);
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
                    value = new DynamicEnumValue<TEnum>(new EnumSet<TEnum>(enums), isInclusive ? EChoiceBehavior.ALLOW_ALL : EChoiceBehavior.ALLOW_ONE);
                    return true;

                }

                {
                    if (str[1] == '(' && str[str.Length - 1] == ')') // read range
                    {
                        int sep = str.IndexOf(':');
                        if (sep != -1)
                        {
                            string p1 = str.Substring(2, sep - 2);
                            string p2 = str.Substring(sep + 1, str.Length - sep - 2);
                            if (!Enum.TryParse(p1, true, out TEnum e1))
                            {
                                L.LogWarning("[QUEST PARSER] Couldn't interpret \"" + p1 + "\" (lower range) as an " + typeof(TEnum).Name);
                                value = new DynamicEnumValue<TEnum>(default);
                                return false;
                            }
                            if (!Enum.TryParse(p2, true, out TEnum e2))
                            {
                                L.LogWarning("[QUEST PARSER] Couldn't interpret \"" + p2 + "\" (higher range) as an " + typeof(TEnum).Name);
                                value = new DynamicEnumValue<TEnum>(default);
                                return false;
                            }
                            int a = e1.CompareTo(e2);
                            if (a > 0)
                                (e2, e1) = (e1, e2);
                            value = a == 0
                                ? new DynamicEnumValue<TEnum>(e1)
                                : new DynamicEnumValue<TEnum>(new EnumRange<TEnum>(e1, e2), isInclusive
                                    ? EChoiceBehavior.ALLOW_ALL
                                    : EChoiceBehavior.ALLOW_ONE);
                            return true;
                        }

                        value = new DynamicEnumValue<TEnum>(default);
                        return false;
                    }
                    if (Enum.TryParse(str, out TEnum res))
                    {
                        value = new DynamicEnumValue<TEnum>(res);
                        return true;
                    }
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
                    value = new DynamicAssetValue<TAsset>(EDynamicValueType.ANY, str![0] == '$' ? EChoiceBehavior.ALLOW_ONE : EChoiceBehavior.ALLOW_ALL);
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

                    value = new DynamicAssetValue<TAsset>(new GuidSet(guids), isInclusive ? EChoiceBehavior.ALLOW_ALL : EChoiceBehavior.ALLOW_ONE);
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
public readonly struct DynamicIntegerValue : IDynamicValue<int>
{
    internal readonly int constant;
    internal readonly IntegralRange range;
    internal readonly IntegralSet set;
    public readonly EDynamicValueType type;
    private readonly EChoiceBehavior _choiceBehavior = EChoiceBehavior.ALLOW_ONE;
    public static readonly IDynamicValue<int>.IChoice Zero = new Choice(new DynamicIntegerValue(0, EChoiceBehavior.ALLOW_ONE));
    public static readonly IDynamicValue<int>.IChoice One = new Choice(new DynamicIntegerValue(1, EChoiceBehavior.ALLOW_ONE));
    public static readonly IDynamicValue<int>.IChoice Any = new Choice(new DynamicIntegerValue(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL));
    public int Constant => constant;
    public IDynamicValue<int>.IRange Range => range;
    public IDynamicValue<int>.ISet Set => set;
    public EDynamicValueType ValueType => type;
    public EChoiceBehavior ChoiceBehavior => _choiceBehavior;

    /// <remarks>Don't use this unless you know what you're doing.</remarks>
    internal DynamicIntegerValue(EDynamicValueType type, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = default;
        this.range = default;
        this.type = type;
        this._choiceBehavior = choiceBehavior;
    }
    public DynamicIntegerValue(int constant, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = constant;
        this.range = default;
        this.set = default;
        this.type = EDynamicValueType.CONSTANT;
        this._choiceBehavior = choiceBehavior;
    }
    public DynamicIntegerValue(ref IntegralRange range, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.range = range;
        this.set = default;
        this.type = EDynamicValueType.RANGE;
        this._choiceBehavior = choiceBehavior;
    }
    public DynamicIntegerValue(ref IntegralSet set, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.range = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
        this._choiceBehavior = choiceBehavior;
    }
    public DynamicIntegerValue(IntegralRange range, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.range = range;
        this.set = default;
        this.type = EDynamicValueType.RANGE;
        this._choiceBehavior = choiceBehavior;
    }
    public DynamicIntegerValue(IntegralSet set, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.range = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
        this._choiceBehavior = choiceBehavior;
    }
    public static IDynamicValue<int>.IChoice ReadChoice(ref Utf8JsonReader reader)
    {
        Choice choice = new Choice();
        choice.Read(ref reader);
        return choice;
    }
    public readonly IDynamicValue<int>.IChoice GetValue()
    {
        return new Choice(this);
    }
    public override string ToString()
    {
        if (type == EDynamicValueType.CONSTANT)
            return constant.ToString(Data.Locale);
        if (type == EDynamicValueType.RANGE)
            return (_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#") + "(" + range.Minimum.ToString(Data.Locale) +
                   ":" + range.Maximum.ToString(Data.Locale) + ")";
        if (type == EDynamicValueType.ANY)
            return _choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*";
        if (type == EDynamicValueType.SET)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(set.Set[i].ToString(Data.Locale));
            }
            sb.Append(']');
            return sb.ToString();
        }
        return constant.ToString(Data.Locale);
    }
    public void Write(Utf8JsonWriter writer)
    {
        if (type == EDynamicValueType.CONSTANT)
        {
            writer.WriteNumberValue(constant);
        }
        else if (type == EDynamicValueType.RANGE)
        {
            writer.WriteStringValue((_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#") + "(" + range.Minimum.ToString(Data.Locale) +
                                    ":" + range.Maximum.ToString(Data.Locale) + ")");
        }
        else if (type == EDynamicValueType.ANY)
        {
            writer.WriteStringValue(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*");
        }
        else if (type == EDynamicValueType.SET)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(set.Set[i].ToString(Data.Locale));
            }
            sb.Append(']');
            writer.WriteStringValue(sb.ToString());
        }
        else
        {
            writer.WriteNumberValue(constant);
        }
    }
    private struct Choice : IDynamicValue<int>.IChoice
    {
        private EChoiceBehavior _behavior;
        private EDynamicValueType _type;
        private int _value;
        private int[] _values;
        private int _minVal;
        private int _maxVal;
        public EChoiceBehavior Behavior => _behavior;
        public EDynamicValueType ValueType => _type;
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
        private void FromValue(ref DynamicIntegerValue value)
        {
            _type = value.type;
            _behavior = value._choiceBehavior;
            if (value.type == EDynamicValueType.CONSTANT)
            {
                _value = value.constant;
            }
            else if (value.type == EDynamicValueType.SET)
            {
                if (_behavior == EChoiceBehavior.ALLOW_ONE)
                {
                    _value = value.set.Length == 1 ? value.set.Set[0] : value.set.Set[UnityEngine.Random.Range(0, value.set.Length)];
                }
                else
                {
                    _values = value.set.Set;
                }
            }
            else if (value.type == EDynamicValueType.RANGE)
            {
                if (_behavior == EChoiceBehavior.ALLOW_ONE)
                {
                    _value = UnityEngine.Random.Range(value.range.Minimum, value.range.Maximum + 1);
                    _value = QuestJsonEx.RoundNumber(value.range.Minimum, value.range.Round, _value);
                }
                else
                {
                    _minVal = value.range.Minimum;
                    _maxVal = value.range.Maximum;
                }
            }
            else
            {
                _value = value.constant;
            }
        }
        /// <summary>Will return <see langword="default"/> if <see cref="ChoiceBehavior"/> is <see cref="EChoiceBehavior.ALLOW_ALL"/> and the value is not of type <see cref="EDynamicValueType.CONSTANT"/>.</summary>
        public int InsistValue() => _value;
        public bool IsMatch(int value)
        {
            if (_type == EDynamicValueType.ANY) return true;
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                return _value == value;
            if (_type == EDynamicValueType.RANGE)
                return value >= _minVal && value <= _maxVal;
            if (_type == EDynamicValueType.SET)
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
                            _behavior = EChoiceBehavior.ALLOW_ALL;
                        else
                        {
                            _behavior = EChoiceBehavior.ALLOW_ONE;
                            L.LogWarning("Possibly unintended int singular choice expression in quest choice: $*");
                        }
                        _type = EDynamicValueType.ANY;
                        return true;
                    }

                    if (reader.TryReadIntegralValue(out DynamicIntegerValue v))
                    {
                        FromValue(ref v);
                        if (_behavior == EChoiceBehavior.ALLOW_ONE && _type != EDynamicValueType.CONSTANT)
                            L.LogWarning("Possibly unintended int singular choice expression in quest choice: " + str);
                        return true;
                    }
                }
                else if (int.TryParse(str, NumberStyles.Any, Data.Locale, out _value))
                {
                    _type = EDynamicValueType.CONSTANT;
                    _behavior = EChoiceBehavior.ALLOW_ONE;
                    return true;
                }
                return false;
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out _value))
            {
                _type = EDynamicValueType.CONSTANT;
                _behavior = EChoiceBehavior.ALLOW_ONE;
                return true;
            }
            return false;
        }
        public void Write(Utf8JsonWriter writer)
        {
            if (_behavior == EChoiceBehavior.ALLOW_ONE || _type == EDynamicValueType.CONSTANT)
            {
                writer.WriteNumberValue(_value);
            }
            else if (_type == EDynamicValueType.RANGE)
            {
                writer.WriteStringValue((_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#") + "(" + _minVal.ToString(Data.Locale) + ":" + _maxVal.ToString(Data.Locale) + ")");
            }
            else if (_type == EDynamicValueType.ANY)
            {
                writer.WriteStringValue(_behavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*");
            }
            else if (_type == EDynamicValueType.SET)
            {
                StringBuilder sb = new StringBuilder(_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(_values[i].ToString(Data.Locale));
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
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                return _value.ToString(Data.Locale);
            if (_type == EDynamicValueType.RANGE)
                return (_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#") + "(" + _minVal.ToString(Data.Locale) +
                       ":" + _maxVal.ToString(Data.Locale) + ")";
            if (_type == EDynamicValueType.ANY)
                return _behavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*";
            if (_type == EDynamicValueType.SET)
            {
                StringBuilder sb = new StringBuilder(_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(_values[i].ToString(Data.Locale));
                }
                sb.Append(']');
                return sb.ToString();
            }
            return _value.ToString(Data.Locale);
        }
    }
}

/// <summary>Datatype storing either a constant <see cref="float"/>, a <see cref="FloatRange"/> or a <see cref="FloatSet"/>.</summary>
public readonly struct DynamicFloatValue : IDynamicValue<float>
{
    private readonly float constant;
    public float Constant => constant;

    private readonly FloatRange range;
    public IDynamicValue<float>.IRange Range => range;

    private readonly EChoiceBehavior _choiceBehavior = EChoiceBehavior.ALLOW_ONE;
    public EChoiceBehavior ChoiceBehavior { get => _choiceBehavior; }

    private readonly FloatSet set;
    public IDynamicValue<float>.ISet Set => set;

    public readonly EDynamicValueType type;
    public EDynamicValueType ValueType => type;

    /// <remarks>Don't use this unless you know what you're doing.</remarks>
    internal DynamicFloatValue(EDynamicValueType type, EChoiceBehavior anyBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = default;
        this.range = default;
        this.type = type;
        this._choiceBehavior = anyBehavior;
    }
    public DynamicFloatValue(float constant)
    {
        this.constant = constant;
        this.range = default;
        this.set = default;
        this.type = EDynamicValueType.CONSTANT;
        _choiceBehavior = EChoiceBehavior.ALLOW_ONE;
    }
    public DynamicFloatValue(ref FloatRange range, EChoiceBehavior anyBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.range = range;
        this.set = default;
        this.type = EDynamicValueType.RANGE;
        _choiceBehavior = anyBehavior;
    }
    public DynamicFloatValue(ref FloatSet set, EChoiceBehavior anyBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.range = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
        _choiceBehavior = anyBehavior;
    }
    public DynamicFloatValue(FloatRange range, EChoiceBehavior anyBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.range = range;
        this.set = default;
        this.type = EDynamicValueType.RANGE;
        _choiceBehavior = anyBehavior;
    }
    public DynamicFloatValue(FloatSet set, EChoiceBehavior anyBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.range = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
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
    public override string ToString()
    {
        if (type == EDynamicValueType.CONSTANT)
            return constant.ToString(Data.Locale);
        if (type == EDynamicValueType.RANGE)
            return (_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#") + "(" + range.Minimum.ToString(Data.Locale) +
                   ":" + range.Maximum.ToString(Data.Locale) + ")";
        if (type == EDynamicValueType.ANY)
            return _choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*";
        if (type == EDynamicValueType.SET)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(set.Set[i].ToString(Data.Locale));
            }
            sb.Append(']');
            return sb.ToString();
        }
        return constant.ToString(Data.Locale);
    }
    public void Write(Utf8JsonWriter writer)
    {
        if (type == EDynamicValueType.CONSTANT)
        {
            writer.WriteNumberValue(constant);
        }
        else if (type == EDynamicValueType.RANGE)
        {
            writer.WriteStringValue((_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#") + "(" + range.Minimum.ToString(Data.Locale) +
                                    ":" + range.Maximum.ToString(Data.Locale) + ")");
        }
        else if (type == EDynamicValueType.ANY)
        {
            writer.WriteStringValue(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*");
        }
        else if (type == EDynamicValueType.SET)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(set.Set[i].ToString(Data.Locale));
            }
            sb.Append(']');
            writer.WriteStringValue(sb.ToString());
        }
        else
        {
            writer.WriteNumberValue(constant);
        }
    }
    private struct Choice : IDynamicValue<float>.IChoice
    {
        private EChoiceBehavior _behavior;
        private EDynamicValueType _type;
        private float _value;
        private float[]? _values;
        private float _minVal;
        private float _maxVal;
        public EChoiceBehavior Behavior => _behavior;
        public EDynamicValueType ValueType => _type;
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
        private void FromValue(ref DynamicFloatValue value)
        {
            _type = value.type;
            _behavior = value._choiceBehavior;
            if (value.type == EDynamicValueType.CONSTANT)
            {
                _value = value.constant;
            }
            else if (value.type == EDynamicValueType.SET)
            {
                if (_behavior == EChoiceBehavior.ALLOW_ONE)
                {
                    if (value.set.Length == 1)
                        _value = value.set.Set[0];
                    else
                        _value = value.set.Set[UnityEngine.Random.Range(0, value.set.Length)];
                }
                else
                {
                    _values = value.set.Set;
                }
            }
            else if (value.type == EDynamicValueType.RANGE)
            {
                if (_behavior == EChoiceBehavior.ALLOW_ONE)
                {
                    _value = UnityEngine.Random.Range(value.range.Minimum, value.range.Maximum);
                    _value = QuestJsonEx.RoundNumber(value.range.Minimum, value.range.Round, _value);
                }
                else
                {
                    _minVal = value.range.Minimum;
                    _maxVal = value.range.Maximum;
                }
            }
            else
            {
                _value = value.constant;
            }
        }
        /// <summary>Will return <see langword="default"/> if <see cref="ChoiceBehavior"/> is <see cref="EChoiceBehavior.ALLOW_ALL"/> and the value is not of type <see cref="EDynamicValueType.CONSTANT"/>.</summary>
        public float InsistValue() => _value;
        public bool IsMatch(float value)
        {
            if (_type == EDynamicValueType.ANY) return true;
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                return Math.Abs(_value - value) < 0.005f;
            if (_type == EDynamicValueType.RANGE)
                return value >= _minVal && value <= _maxVal;
            if (_type == EDynamicValueType.SET)
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
                            _behavior = EChoiceBehavior.ALLOW_ALL;
                        else
                        {
                            _behavior = EChoiceBehavior.ALLOW_ONE;
                            L.LogWarning("Possibly unintended float singular choice expression in quest choice: $*");
                        }
                        _type = EDynamicValueType.ANY;
                        return true;
                    }

                    if (reader.TryReadFloatValue(out DynamicFloatValue v))
                    {
                        FromValue(ref v);
                        if (_behavior == EChoiceBehavior.ALLOW_ONE && _type != EDynamicValueType.CONSTANT)
                            L.LogWarning("Possibly unintended float singular choice expression in quest choice: " + str);
                        return true;
                    }
                }
                else if (float.TryParse(str, System.Globalization.NumberStyles.Any, Data.Locale, out _value))
                {
                    _type = EDynamicValueType.CONSTANT;
                    _behavior = EChoiceBehavior.ALLOW_ONE;
                    return true;
                }
                return false;
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetSingle(out _value))
            {
                _type = EDynamicValueType.CONSTANT;
                _behavior = EChoiceBehavior.ALLOW_ONE;
                return true;
            }
            return false;
        }
        public void Write(Utf8JsonWriter writer)
        {
            if (_behavior == EChoiceBehavior.ALLOW_ONE || _type == EDynamicValueType.CONSTANT)
            {
                writer.WriteNumberValue(_value);
            }
            else if (_type == EDynamicValueType.RANGE)
            {
                writer.WriteStringValue((_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#") + "(" + _minVal.ToString(Data.Locale) + ":" + _maxVal.ToString(Data.Locale) + ")");
            }
            else if (_type == EDynamicValueType.ANY)
            {
                writer.WriteStringValue(_behavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*");
            }
            else if (_type == EDynamicValueType.SET)
            {
                StringBuilder sb = new StringBuilder(_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values!.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(_values[i].ToString(Data.Locale));
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
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                return _value.ToString(Data.Locale);
            if (_type == EDynamicValueType.RANGE)
                return (_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#") + "(" + _minVal.ToString(Data.Locale) +
                       ":" + _maxVal.ToString(Data.Locale) + ")";
            if (_type == EDynamicValueType.ANY)
                return _behavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*";
            if (_type == EDynamicValueType.SET)
            {
                StringBuilder sb = new StringBuilder(_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values!.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(_values[i].ToString(Data.Locale));
                }
                sb.Append(']');
                return sb.ToString();
            }
            return _value.ToString(Data.Locale);
        }
    }
}
/// <summary>Datatype storing either a constant <see cref="string"/> or a <see cref="StringSet"/>.</summary>
public readonly struct DynamicStringValue : IDynamicValue<string>
{
    internal readonly string? constant;
    public string Constant => constant!;

    internal readonly StringSet set;
    public IDynamicValue<string>.ISet Set => set;

    public readonly EDynamicValueType type;
    public EDynamicValueType ValueType => type;

    public IDynamicValue<string>.IRange? Range => null;

    private readonly EChoiceBehavior _choiceBehavior = EChoiceBehavior.ALLOW_ONE;
    public EChoiceBehavior ChoiceBehavior => _choiceBehavior;
    public readonly bool IsKitSelector;

    /// <remarks>Don't use this unless you know what you're doing.</remarks>
    internal DynamicStringValue(bool isKitSelector, EDynamicValueType type, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = default;
        this.type = type;
        this._choiceBehavior = choiceBehavior;
        this.IsKitSelector = isKitSelector;
    }
    public DynamicStringValue(bool isKitSelector, string constant)
    {
        this.constant = constant;
        this.set = default;
        this.type = EDynamicValueType.CONSTANT;
        this.IsKitSelector = isKitSelector;
    }
    public DynamicStringValue(bool isKitSelector, ref StringSet set, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
        this._choiceBehavior = choiceBehavior;
        this.IsKitSelector = isKitSelector;
    }
    public DynamicStringValue(bool isKitSelector, StringSet set, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
        this._choiceBehavior = choiceBehavior;
        this.IsKitSelector = isKitSelector;
    }
    IDynamicValue<string>.IChoice IDynamicValue<string>.GetValue() => GetValue();
    internal readonly Choice GetValue()
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
    public override string ToString()
    {
        if (type == EDynamicValueType.CONSTANT)
            return constant!;
        if (type == EDynamicValueType.ANY)
            return _choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*";
        if (type == EDynamicValueType.SET)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
            sb.Append('[');
            string[] arr = set.Set;
            for (int i = 0; i < set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                if (arr[i] != null)
                    sb.Append(arr[i].Replace(',', '_'));
            }
            sb.Append(']');
            return sb.ToString();
        }
        return constant!;
    }
    public void Write(Utf8JsonWriter writer)
    {
        if (type == EDynamicValueType.CONSTANT)
        {
            writer.WriteStringValue(constant);
        }
        else if (type == EDynamicValueType.ANY)
        {
            writer.WriteStringValue(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*");
        }
        else if (type == EDynamicValueType.SET)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
            sb.Append('[');
            string[] arr = set.Set;
            for (int i = 0; i < set.Length; i++)
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
            writer.WriteStringValue(constant);
        }
    }
    internal struct Choice : IDynamicValue<string>.IChoice
    {
        private EChoiceBehavior _behavior;
        private EDynamicValueType _type;
        private string? _value;
        private string[]? _values;
        private string? _kitName;
        private string[]? _kitNames;
        private bool _isKitSelector;
        public bool IsKitSelector => _isKitSelector;
        public EChoiceBehavior Behavior => _behavior;
        public EDynamicValueType ValueType => _type;
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
        private void FromValue(ref DynamicStringValue value)
        {
            _type = value.type;
            _behavior = value._choiceBehavior;
            _isKitSelector = value.IsKitSelector;
            if (value.type == EDynamicValueType.CONSTANT)
            {
                _value = value.constant;
            }
            else if (value.type == EDynamicValueType.SET)
            {
                if (_behavior == EChoiceBehavior.ALLOW_ONE)
                {
                    if (value.set.Length == 1)
                        _value = value.set.Set[0];
                    else
                        _value = value.set.Set[UnityEngine.Random.Range(0, value.set.Length)];
                }
                else
                {
                    _values = value.set.Set;
                }
            }
            else if (value.type == EDynamicValueType.ANY && _isKitSelector)
            {
                KitOld? rand = KitManager.GetRandomPublicKit();
                if (rand is null)
                {
                    _value = value.constant;
                }
                else
                {
                    _value = rand.Name;
                }
            }
            else
            {
                _value = value.constant;
            }
        }
        /// <summary>Will return <see langword="null"/> if <see cref="ChoiceBehavior"/> is <see cref="EChoiceBehavior.ALLOW_ALL"/> and the value is not of type <see cref="EDynamicValueType.CONSTANT"/>.</summary>
        public string InsistValue() => _value!;
        public bool IsMatch(string value)
        {
            if (_type == EDynamicValueType.ANY && _behavior == EChoiceBehavior.ALLOW_ALL) return true;
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                return _value!.Equals(value, StringComparison.OrdinalIgnoreCase);
            if (_type == EDynamicValueType.SET)
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
                        _behavior = EChoiceBehavior.ALLOW_ALL;
                    else
                    {
                        _behavior = EChoiceBehavior.ALLOW_ONE;
                        L.LogWarning("Possibly unintended " + (_isKitSelector ? "kit" : "string") + " singular choice expression in quest choice: $*");
                    }
                    _type = EDynamicValueType.ANY;
                    return true;
                }

                if (reader.TryReadStringValue(out DynamicStringValue v, _isKitSelector))
                {
                    FromValue(ref v);
                    if (_behavior == EChoiceBehavior.ALLOW_ONE && _type != EDynamicValueType.CONSTANT)
                        L.LogWarning("Possibly unintended " + (_isKitSelector ? "kit" : "string") + " singular choice expression in quest choice: " + str);
                    return true;
                }
            }
            else
            {
                _value = str;
                _type = EDynamicValueType.CONSTANT;
                _behavior = EChoiceBehavior.ALLOW_ONE;
                return true;
            }
            return false;
        }
        public void Write(Utf8JsonWriter writer)
        {
            if (_behavior == EChoiceBehavior.ALLOW_ONE || _type == EDynamicValueType.CONSTANT)
            {
                writer.WriteStringValue(_value);
            }
            else if (_type == EDynamicValueType.ANY)
            {
                writer.WriteStringValue(_behavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*");
            }
            else if (_type == EDynamicValueType.SET)
            {
                StringBuilder sb = new StringBuilder(_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
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
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                return _value!;
            if (_type == EDynamicValueType.ANY)
                return _behavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*";
            if (_type == EDynamicValueType.SET)
            {
                StringBuilder sb = new StringBuilder(_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
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
                if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                {
                    if (_kitName == null)
                    {
                        if (KitManager.KitExists(_value!, out KitOld kit))
                            _kitName = GetKitName(kit, player);
                        else
                            _kitName = _value!;
                    }
                    return _kitName;
                }

                if (_type == EDynamicValueType.ANY)
                {
                    return "any";
                }
                if (_type == EDynamicValueType.SET)
                {
                    if (_kitNames == null)
                    {
                        _kitNames = new string[_values!.Length];
                        for (int i = 0; i < _values.Length; ++i)
                        {
                            if (KitManager.KitExists(_values[i], out KitOld kit))
                                _kitNames[i] = GetKitName(kit, player);
                            else
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
                                builder.Append(", " + (_behavior == EChoiceBehavior.ALLOW_ONE ? "or" : "and") + " ");
                            else if (_kitNames.Length == 2)
                                builder.Append(" " + (_behavior == EChoiceBehavior.ALLOW_ONE ? "or" : "and") + " ");
                        }

                        builder.Append(_kitNames[i]);
                    }

                    return builder.ToString();
                }
                return _value!;
            }

            return ToString();
        }
        private static string GetKitName(KitOld kit, ulong player)
        {
            if (player == 0 || !Data.Languages.TryGetValue(player, out string language))
                language = L.DEFAULT;
            if (kit.SignTexts.TryGetValue(language, out string v))
                return v;
            if (!language.Equals(L.DEFAULT, StringComparison.Ordinal) &&
                kit.SignTexts.TryGetValue(L.DEFAULT, out v))
                return v;
            return kit.SignTexts.Values.FirstOrDefault() ?? kit.Name;
        }
    }
}
/// <summary>Datatype storing either a constant <seealso cref="Guid"/> value or a set of <seealso cref="Guid"/> values.
/// <para>Formatted like <see cref="DynamicStringValue"/> with <seealso cref="Guid"/>s.</para>
/// <para>Can also be formatted as "$*" to act as a wildcard, only applicable for some quests.</para></summary>
/// <typeparam name="TAsset">Any <see cref="Asset"/>.</typeparam>
public readonly struct DynamicAssetValue<TAsset> : IDynamicValue<Guid> where TAsset : Asset
{
    internal readonly Guid constant;
    internal readonly GuidSet set;
    public readonly EDynamicValueType type;
    public readonly EAssetType assetType;
    public readonly EChoiceBehavior _choiceBehavior = EChoiceBehavior.ALLOW_ONE;
    public Guid Constant => constant;
    public IDynamicValue<Guid>.IRange? Range => null;

    public IDynamicValue<Guid>.ISet? Set => set;

    public EDynamicValueType ValueType => type;

    public EChoiceBehavior ChoiceBehavior => _choiceBehavior;

    /// <remarks>Don't use this unless you know what you're doing.</remarks>
    internal DynamicAssetValue(EDynamicValueType type, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = default;
        this.type = type;
        this.assetType = GetAssetType();
        this._choiceBehavior = choiceBehavior;
    }
    public DynamicAssetValue(Guid constant)
    {
        this.constant = constant;
        this.set = default;
        this.type = EDynamicValueType.CONSTANT;
        this.assetType = GetAssetType();

    }
    public DynamicAssetValue(GuidSet set, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
        this.assetType = GetAssetType();
        this._choiceBehavior = choiceBehavior;
    }
    public DynamicAssetValue(ref GuidSet set, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
        this.assetType = GetAssetType();
        this._choiceBehavior = choiceBehavior;
    }
    public readonly Choice GetValue()
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
        if (type == EDynamicValueType.CONSTANT)
            return constant.ToString("N");
        if (type == EDynamicValueType.ANY)
            return _choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*";
        if (type == EDynamicValueType.SET)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(set.Set[i].ToString("N"));
            }
            sb.Append(']');
            return sb.ToString();
        }
        return constant.ToString("N");
    }
    public void Write(Utf8JsonWriter writer)
    {
        if (type == EDynamicValueType.CONSTANT)
        {
            writer.WriteStringValue(constant.ToString("N"));
        }
        else if (type == EDynamicValueType.ANY)
        {
            writer.WriteStringValue(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*");
        }
        else if (type == EDynamicValueType.SET)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(set.Set[i].ToString("N"));
            }
            sb.Append(']');
            writer.WriteStringValue(sb.ToString());
        }
        else
        {
            writer.WriteStringValue(constant.ToString("N"));
        }
    }
    public struct Choice : IDynamicValue<Guid>.IChoice
    {
        private EChoiceBehavior _behavior;
        private EDynamicValueType _type;
        private EAssetType _assetType;
        private Guid _value;
        private TAsset _valueCache;
        private Guid[]? _values;
        private TAsset[]? _valuesCache;
        private bool _areValuesCached;
        public EChoiceBehavior Behavior => _behavior;
        public EDynamicValueType ValueType => _type;
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
        private void FromValue(ref DynamicAssetValue<TAsset> value)
        {
            _type = value.type;
            _behavior = value._choiceBehavior;
            _assetType = value.assetType;
            _areValuesCached = false;
            if (value.type == EDynamicValueType.CONSTANT)
            {
                _value = value.constant;
                if (Level.isLoaded)
                {
                    _valueCache = Assets.find<TAsset>(_value);
                    _areValuesCached = _valueCache is not null;
                }
                else
                {
                    _areValuesCached = false;
                }
            }
            else if (value.type == EDynamicValueType.SET)
            {
                if (_behavior == EChoiceBehavior.ALLOW_ONE)
                {
                    if (value.set.Length == 1)
                        _value = value.set.Set[0];
                    else
                        _value = value.set.Set[UnityEngine.Random.Range(0, value.set.Length)];
                    if (Level.isLoaded)
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
                    _values = value.set.Set;
                    _valuesCache = new TAsset[_values.Length];
                    bool hasNull = false;
                    for (int i = 0; i < _values.Length; ++i)
                    {
                        TAsset? asset = Assets.find<TAsset>(_values[i]);
                        if (asset == null)
                            hasNull = true;
                        else _valuesCache[i] = asset;
                    }
                    _areValuesCached = !hasNull || Level.isLoaded;
                }
            }
            else if (value.type == EDynamicValueType.ANY && value._choiceBehavior == EChoiceBehavior.ALLOW_ONE)
            {
                if (Level.isLoaded)
                {
                    TAsset[] assets = Assets.find(_assetType).OfType<TAsset>().ToArray();
                    if (assets.Length == 1)
                    {
                        _valueCache = assets[0];
                        _value = _valueCache.GUID;
                    }
                    else
                    {
                        _valueCache = assets[UnityEngine.Random.Range(0, assets.Length)];
                        _value = _valueCache.GUID;
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
                _value = value.constant;
                if (Level.isLoaded)
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
        /// <summary>Will return <see langword="null"/> if <see cref="ChoiceBehavior"/> is <see cref="EChoiceBehavior.ALLOW_ALL"/> and the value is not of type <see cref="EDynamicValueType.CONSTANT"/>.</summary>
        public Guid InsistValue() => _value;
        public TAsset InsistAssetValue() => (_valueCache ?? (Assets.isLoading ? null : Assets.find<TAsset>(_value)))!;
        public readonly TAsset[] GetAssetValueSet()
        {
            if (_areValuesCached) return _valuesCache!;
            if (_type == EDynamicValueType.SET)
            {
                TAsset[] values = new TAsset[_values!.Length];
                for (int i = 0; i < values.Length; i++)
                    values[i] = (!Level.isLoaded ? null : Assets.find<TAsset>(_values[i]))!;
                return values;
            }
            return _valuesCache!;
        }
        public readonly string GetCommaList()
        {
            if (_valueCache != null)
                return GetName(_valueCache, _assetType) ?? "null";
            if (_type == EDynamicValueType.ANY && _behavior == EChoiceBehavior.ALLOW_ALL)
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
                            builder.Append(", " + (_behavior == EChoiceBehavior.ALLOW_ONE ? "or" : "and") + " ");
                        else if (_valuesCache.Length == 2)
                            builder.Append(" " + (_behavior == EChoiceBehavior.ALLOW_ONE ? "or" : "and") + " ");
                    }
                    builder.Append(GetName(_valuesCache[i], _assetType) ?? "null");
                }
                return builder.ToString();
            }
            return string.Empty;
        }
        public readonly bool IsMatch(Guid value)
        {
            if (_type == EDynamicValueType.ANY) return true;
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                return _value == value;
            if (_type == EDynamicValueType.SET)
            {
                for (int i = 0; i < _values!.Length; i++)
                    if (_values[i] == value) return true;
            }
            return false;
        }
        public readonly bool IsMatch(ushort value)
        {
            if (_type == EDynamicValueType.ANY) return true;
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                return (_valueCache ?? Assets.find<TAsset>(_value))?.id == value;
            if (_type == EDynamicValueType.SET)
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
            if (_type == EDynamicValueType.ANY) return true;
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                return _value == value.GUID;
            if (_type == EDynamicValueType.SET)
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
                        _behavior = EChoiceBehavior.ALLOW_ALL;
                    else
                    {
                        _behavior = EChoiceBehavior.ALLOW_ONE;
                        L.LogWarning("Possibly unintended " + typeof(TAsset).Name + " singular choice expression in quest choice: $*");
                    }
                    _type = EDynamicValueType.ANY;
                    return true;
                }

                if (reader.TryReadAssetValue(out DynamicAssetValue<TAsset> v))
                {
                    FromValue(ref v);
                    if (_behavior == EChoiceBehavior.ALLOW_ONE && _type != EDynamicValueType.CONSTANT)
                        L.LogWarning("Possibly unintended " + typeof(TAsset).Name + " singular choice expression in quest choice: " + str);
                    return true;
                }
            }
            else if (reader.TryGetGuid(out _value))
            {
                _type = EDynamicValueType.CONSTANT;
                _behavior = EChoiceBehavior.ALLOW_ONE;
                return true;
            }
            return false;
        }
        public readonly void Write(Utf8JsonWriter writer)
        {
            if (_behavior == EChoiceBehavior.ALLOW_ONE || _type == EDynamicValueType.CONSTANT)
            {
                writer.WriteStringValue(_value);
            }
            else if (_type == EDynamicValueType.ANY)
            {
                writer.WriteStringValue(_behavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*");
            }
            else if (_type == EDynamicValueType.SET)
            {
                StringBuilder sb = new StringBuilder(_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
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
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                return _value.ToString("N");
            if (_type == EDynamicValueType.ANY)
                return _behavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*";
            if (_type == EDynamicValueType.SET)
            {
                StringBuilder sb = new StringBuilder(_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
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
            if (_type == EDynamicValueType.CONSTANT)
                return GetName(Assets.find(_value), _assetType);
            if (_type == EDynamicValueType.ANY)
                return _behavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*";
            if (_type == EDynamicValueType.SET)
            {
                StringBuilder sb = new StringBuilder(_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values!.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(GetName(Assets.find(_values[i]), _assetType));
                }
                sb.Append(']');
                return sb.ToString();
            }
            return GetName(Assets.find(_value), _assetType);
        }
        private static string? GetName(Asset asset, EAssetType type)
        {
            if (asset == null) return null;
            return asset.FriendlyName;
        }
    }
}
/// <summary>Datatype storing either a constant enum value or a set of enum values.
/// <para>Formatted like <see cref="DynamicStringValue"/>.</para></summary>
/// <typeparam name="TEnum">Any enumeration.</typeparam>
public readonly struct DynamicEnumValue<TEnum> : IDynamicValue<TEnum> where TEnum : struct, Enum
{
    internal readonly TEnum constant;
    internal readonly EnumSet<TEnum> set;
    internal readonly EnumRange<TEnum> range;
    public readonly EDynamicValueType type;
    private readonly EChoiceBehavior _choiceBehavior = EChoiceBehavior.ALLOW_ONE;
    public EChoiceBehavior ChoiceBehavior => _choiceBehavior;
    public TEnum Constant => constant;
    public IDynamicValue<TEnum>.IRange Range => range;
    public IDynamicValue<TEnum>.ISet Set => set;
    public EDynamicValueType ValueType => type;
    /// <remarks>Don't use this unless you know what you're doing.</remarks>
    internal DynamicEnumValue(EDynamicValueType type, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = default;
        this.range = default;
        this.type = type;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicEnumValue(TEnum constant)
    {
        this.constant = constant;
        this.set = default;
        this.range = default;
        this.type = EDynamicValueType.CONSTANT;
    }
    public DynamicEnumValue(EnumSet<TEnum> set, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = set;
        this.range = default;
        this.type = EDynamicValueType.SET;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicEnumValue(ref EnumSet<TEnum> set, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = set;
        this.range = default;
        this.type = EDynamicValueType.SET;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicEnumValue(EnumRange<TEnum> range, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = default;
        this.range = range;
        this.type = EDynamicValueType.RANGE;
        _choiceBehavior = choiceBehavior;
    }
    public DynamicEnumValue(ref EnumRange<TEnum> range, EChoiceBehavior choiceBehavior = EChoiceBehavior.ALLOW_ONE)
    {
        this.constant = default;
        this.set = default;
        this.range = range;
        this.type = EDynamicValueType.RANGE;
        _choiceBehavior = choiceBehavior;
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
        if (type == EDynamicValueType.CONSTANT)
            return constant.ToString();
        if (type == EDynamicValueType.RANGE)
            return (_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#") + "(" + range.Minimum.ToString() +
                   ":" + range.Maximum.ToString() + ")";
        if (type == EDynamicValueType.ANY)
            return _choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*";
        if (type == EDynamicValueType.SET)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(set.Set[i].ToString());
            }
            sb.Append(']');
            return sb.ToString();
        }
        return constant.ToString();
    }
    public void Write(Utf8JsonWriter writer)
    {
        if (type == EDynamicValueType.CONSTANT)
        {
            writer.WriteStringValue(constant.ToString());
        }
        else if (type == EDynamicValueType.RANGE)
        {
            writer.WriteStringValue((_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#") + "(" + range.Minimum.ToString() +
                                    ":" + range.Maximum.ToString() + ")");
        }
        else if (type == EDynamicValueType.ANY)
        {
            writer.WriteStringValue(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*");
        }
        else if (type == EDynamicValueType.SET)
        {
            StringBuilder sb = new StringBuilder(_choiceBehavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
            sb.Append('[');
            for (int i = 0; i < set.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(set.Set[i].ToString());
            }
            sb.Append(']');
            writer.WriteStringValue(sb.ToString());
        }
        else
        {
            writer.WriteStringValue(constant.ToString());
        }
    }

    internal struct Choice : IDynamicValue<TEnum>.IChoice
    {
        private EChoiceBehavior _behavior;
        private EDynamicValueType _type;
        private TEnum _value;
        private TEnum[]? _values;
        private TEnum _minVal;
        private TEnum _maxVal;
        private int _minValUnderlying;
        private int _maxValUnderlying;
        public EChoiceBehavior Behavior => _behavior;
        public EDynamicValueType ValueType => _type;

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
        private void FromValue(ref DynamicEnumValue<TEnum> value)
        {
            _type = value.type;
            _behavior = value._choiceBehavior;
            if (value.type == EDynamicValueType.CONSTANT)
            {
                _value = value.constant;
            }
            else if (value.type == EDynamicValueType.SET)
            {
                if (_behavior == EChoiceBehavior.ALLOW_ONE)
                {
                    if (value.set.Length == 1)
                        _value = value.set.Set[0];
                    else
                        _value = value.set.Set[UnityEngine.Random.Range(0, value.set.Length)];
                }
                else
                {
                    _values = value.set.Set;
                }
            }
            else if (value.type == EDynamicValueType.RANGE)
            {
                if (_behavior == EChoiceBehavior.ALLOW_ONE)
                {

                    int r1 = ((IConvertible)value.Range.Minimum).ToInt32(Data.Locale);
                    int r2 = ((IConvertible)value.Range.Maximum).ToInt32(Data.Locale);
                    int r3 = UnityEngine.Random.Range(r1, r2 + 1);
                    _value = (TEnum)Enum.ToObject(typeof(TEnum), r3);
                }
                else
                {
                    _minValUnderlying = ((IConvertible)value.Range.Minimum).ToInt32(Data.Locale);
                    _minVal = value.Range.Minimum;
                    _maxValUnderlying = ((IConvertible)value.Range.Maximum).ToInt32(Data.Locale);
                    _maxVal = value.Range.Maximum;
                }
            }
            else if (value.type == EDynamicValueType.ANY && _behavior == EChoiceBehavior.ALLOW_ONE)
            {
                Array arr = Enum.GetValues(typeof(TEnum));
                int r = UnityEngine.Random.Range(0, arr.Length);
                _value = (TEnum)arr.GetValue(r);
            }
            else
            {
                _value = value.constant;
            }
        }
        /// <summary>Will return <see langword="null"/> if <see cref="ChoiceBehavior"/> is <see cref="EChoiceBehavior.ALLOW_ALL"/> and the value is not of type <see cref="EDynamicValueType.CONSTANT"/>.</summary>
        public TEnum InsistValue() => _value;
        public readonly bool IsMatch(TEnum value)
        {
            if (_type == EDynamicValueType.ANY) return true;
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                return _value.Equals(value);
            if (_type == EDynamicValueType.RANGE)
            {
                int r = (int)(object)value;
                return r >= _maxValUnderlying && r <= _maxValUnderlying;
            }
            if (_type == EDynamicValueType.SET)
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
                        _behavior = EChoiceBehavior.ALLOW_ALL;
                    else
                    {
                        _behavior = EChoiceBehavior.ALLOW_ONE;
                        L.LogWarning("Possibly unintended " + typeof(TEnum).Name + " singular choice expression in quest choice: $*");
                    }
                    _type = EDynamicValueType.ANY;
                    return true;
                }

                if (reader.TryReadEnumValue(out DynamicEnumValue<TEnum> v))
                {
                    FromValue(ref v);
                    if (_behavior == EChoiceBehavior.ALLOW_ONE && _type != EDynamicValueType.CONSTANT)
                        L.LogWarning("Possibly unintended " + typeof(TEnum).Name + " singular choice expression in quest choice: " + str);
                    return true;
                }
            }
            else if (Enum.TryParse(str, true, out _value))
            {
                _type = EDynamicValueType.CONSTANT;
                _behavior = EChoiceBehavior.ALLOW_ONE;
                return true;
            }
            return false;
        }
        public readonly void Write(Utf8JsonWriter writer)
        {
            if (_behavior == EChoiceBehavior.ALLOW_ONE || _type == EDynamicValueType.CONSTANT)
            {
                writer.WriteStringValue(_value.ToString());
            }
            else if (_type == EDynamicValueType.ANY)
            {
                writer.WriteStringValue(_behavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*");
            }
            else if (_type == EDynamicValueType.RANGE)
            {
                writer.WriteStringValue((_behavior == EChoiceBehavior.ALLOW_ONE ? "$(" : "#(") + _minVal.ToString() + ":" + _maxVal + ")");
            }
            else if (_type == EDynamicValueType.SET)
            {
                StringBuilder sb = new StringBuilder(_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
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
        public override readonly string ToString()
        {
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
                return _value.ToString();
            if (_type == EDynamicValueType.RANGE)
                return (_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#") + "(" + _minVal.ToString() +
                       ":" + _maxVal.ToString() + ")";
            if (_type == EDynamicValueType.ANY)
                return _behavior == EChoiceBehavior.ALLOW_ONE ? "$*" : "#*";
            if (_type == EDynamicValueType.SET)
            {
                StringBuilder sb = new StringBuilder(_behavior == EChoiceBehavior.ALLOW_ONE ? "$" : "#");
                sb.Append('[');
                for (int i = 0; i < _values!.Length; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(_values[i].ToString());
                }
                sb.Append(']');
                return sb.ToString();
            }
            return _value.ToString();
        }
        public string GetCommaList(ulong player)
        {
            if (_type == EDynamicValueType.CONSTANT || _behavior == EChoiceBehavior.ALLOW_ONE)
            {
                return Localization.TranslateEnum(_value, player);
            }
            if (_type == EDynamicValueType.ANY)
            {
                return "any";
            }
            if (_type == EDynamicValueType.SET)
            {
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < _values!.Length; ++i)
                {
                    if (i != 0)
                    {
                        if (i != _values.Length - 1)
                            builder.Append(", ");
                        else if (_values.Length > 2)
                            builder.Append(", " + (_behavior == EChoiceBehavior.ALLOW_ONE ? "or" : "and") + " ");
                        else if (_values.Length == 2)
                            builder.Append(" " + (_behavior == EChoiceBehavior.ALLOW_ONE ? "or" : "and") + " ");
                    }

                    builder.Append(Localization.TranslateEnum(_values[i], player));
                }

                return builder.ToString();
            }
            return Localization.TranslateEnum(_value!, player);
        }
    }
}

public enum EDynamicValueType
{
    CONSTANT,
    RANGE,
    SET,
    ANY
}
public interface IDynamicValue<T>
{
    public T Constant { get; }
    public IRange? Range { get; }
    public interface IRange
    {
        public T Minimum { get; }
        public T Maximum { get; }
    }
    public ISet? Set { get; }
    public interface ISet
    {
        public T[] Set { get; }
        public int Length { get; }
    }
    public EDynamicValueType ValueType { get; }
    public EChoiceBehavior ChoiceBehavior { get; }
    public IChoice GetValue();
    public void Write(Utf8JsonWriter writer);
    public interface IChoice
    {
        public EChoiceBehavior Behavior { get; }
        public EDynamicValueType ValueType { get; }
        public bool IsMatch(T value);
        public T InsistValue();
        public void Write(Utf8JsonWriter writer);
        public bool Read(ref Utf8JsonReader reader);
        public string ToString();
    }
}
public enum EChoiceBehavior : byte
{
    ALLOW_ONE,
    ALLOW_ALL
}

/// <summary>Datatype storing a range of integers.
/// <para>Formatted like this: </para><code>"$(3:182)"</code></summary>
public readonly struct IntegralRange : IDynamicValue<int>.IRange
{
    private readonly int _min;
    private readonly int _round;
    private readonly int _max;
    public int Minimum => _min;
    public int Maximum => _max;
    public int Round => _round;
    public IntegralRange(int min, int max, int round)
    {
        _min = min;
        _max = max;
        _round = round;
    }
    public override readonly string ToString() => "$(" + _min.ToString(Data.Locale) + ":" + _max.ToString(Data.Locale) + ")" + (_round == 0 ? string.Empty : "{" + _round + "}");
}
/// <summary>Datatype storing a range of floats.
/// <para>Formatted like this: </para><code>"$(3.2:182.1)"</code></summary>
public readonly struct FloatRange : IDynamicValue<float>.IRange
{
    private readonly float _min;
    private readonly float _max;
    private readonly int _round;
    public float Minimum => _min;
    public float Maximum => _max;
    public int Round => _round;
    public FloatRange(float min, float max, int round)
    {
        _min = min;
        _max = max;
        _round = round;
    }
    public override readonly string ToString() => "$(" + _min.ToString(Data.Locale) + ":" + _max.ToString(Data.Locale) + ")" + (_round == 0 ? string.Empty : "{" + _round + "}");
}
/// <summary>Datatype storing a range of floats.
/// <para>Formatted like this: </para><code>"$(3.2:182.1)"</code></summary>
public readonly struct EnumRange<TEnum> : IDynamicValue<TEnum>.IRange where TEnum : struct, Enum
{
    private readonly TEnum _min;
    public TEnum Minimum => _min;
    private readonly TEnum _max;
    public TEnum Maximum => _max;
    public EnumRange(TEnum min, TEnum max)
    {
        _min = min;
        _max = max;
    }
    public override readonly string ToString() => "$(" + _min.ToString() + ":" + _max.ToString() + ")";
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
    public override readonly string ToString()
    {
        StringBuilder sb = new StringBuilder("$[");
        for (int i = 0; i < _length; i++)
        {
            if (i != 0) sb.Append(',');
            sb.Append(_set[i].ToString(Data.Locale));
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
            sb.Append(_set[i].ToString(Data.Locale));
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
    public Guid[] Set => _set;
    private readonly int _length;
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
public enum EPresetType
{
    RANK_UNLOCK,
    KIT_UNLOCK,
    VEHICLE_UNLOCK
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
    public void OnPlayerSpawnedAtBunker(FOBs.BuiltBuildableComponent bunker, FOB fob, UCPlayer spawner);
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
    public void OnBuildableBuilt(UCPlayer player, FOBs.BuildableData buildable);
}
public interface INotifyVehicleDestroyed : INotifyTracker
{
    public void OnVehicleDestroyed(VehicleDestroyed e);
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
public interface IQuestState<TTracker, TDataNew> : IQuestState where TTracker : BaseQuestTracker where TDataNew : BaseQuestData
{
    public void Init(TDataNew data);
    public bool IsEligable(UCPlayer player);
}