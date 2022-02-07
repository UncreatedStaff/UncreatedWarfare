using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Warfare.Quests.Types;

namespace Uncreated.Warfare.Quests;


public enum EWeaponClass : byte
{
    ASSAULT_RIFLE,
    BATTLE_RIFLE,
    MARKSMAN_RIFLE, // DMR
    SNIPER_RIFLE,
    MACHINE_GUN,
    PISTOL,
    SHOTGUN,
    SMG
}
public enum EQuestType : byte
{
    INVALID,
    KILL_ENEMIES,
    KILL_ENEMIES_WITH_WEAPON,
    KILL_ENEMIES_WITH_KIT,
    KILL_ENEMIES_WITH_KITS,
    KILL_ENEMIES_WITH_KIT_CLASS,
    KILL_ENEMIES_WITH_KIT_CLASSES,
    KILL_ENEMIES_WITH_WEAPON_CLASS,
    KILL_ENEMIES_WITH_BRANCH,
    KILL_ENEMIES_WITH_TURRET,
    KILL_ENEMIES_WITH_EXPLOSIVES,
    KILL_ENEMIES_IN_SQUAD,
    KILL_ENEMIES_IN_FULL_SQUAD,
    BUILD_FORTIFICATIONS,
    BUILD_EMPLACEMENTS,
    BUILD_FOBS,
    BUILD_FOBS_ON_OBJECTIVES,
    BUILD_FOB_ON_ACTIVE_OBJECTIVE,
    DELIVER_SUPPLIES,
    ENTRENCHING_TOOL_HITS,
    CAPTURE_OBJECTIVES,
    DESTROY_CACHE,
    DRIVE_DISTANCE_ANY,
    DRIVE_DISTANCE_SPECIFIED,
    TRANSPORT_PLAYERS,
    REVIVE_PLAYERS,
    KING_SLAYER,
    KILL_VEHICLES,
    KILL_VEHICLE_TYPE,
    KILL_STREAK,
    XP_IN_GAMEMODE,
    KILL_FROM_RANGE,
    KILL_FROM_RANGE_WITH_WEAPON,
    KILL_FROM_RANGE_WITH_CLASS,
    KILL_FROM_RANGE_WITH_KIT,
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class QuestDataAttribute : Attribute
{
    public EQuestType Type => _type;
    private readonly EQuestType _type;
    public QuestDataAttribute(EQuestType type)
    {
        _type = type;
    }
}

public static class QuestJsonEx
{
    /// <summary>Constant float value, random value from a <see cref="IntegralRange"/>, or a random value from a <see cref="IntegralSet"/>.</summary>
    public static unsafe bool TryReadIntegralValue(this ref Utf8JsonReader reader, out DynamicIntegerValue value)
    {
        if (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string str = reader.GetString();
                if (string.IsNullOrEmpty(str))
                {
                    value = new DynamicIntegerValue(0);
                    return false;
                }

                if (str.Length > 3 && str[0] == '$')
                {
                    if (str[1] == '(' && str[str.Length - 1] == ')') // read range
                    {
                        int sep = str.IndexOf(':');
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
                                for (int i = str.Length - 1; i > sep; i--)
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
                            value = new DynamicIntegerValue(new IntegralRange(v1, v2));
                            return true;
                        }
                        else
                        {
                            value = new DynamicIntegerValue(0);
                            return false;
                        }
                    }
                    else if (str[1] == '[' && str[str.Length - 1] == ']')
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
                            while (ptrpos > 1)
                            {
                                ptrpos--;
                                int num = 0;
                                int m = 1;
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
                                else if (c == ',' || c == '[')
                                {
                                    index--;
                                    res[index] = num;
                                }
                            }
                        }

                        value = new DynamicIntegerValue(new IntegralSet(res));
                        return true;

                    }
                    else
                    {
                        value = new DynamicIntegerValue(0);
                        return false;
                    }
                }
                else if (int.TryParse(str, System.Globalization.NumberStyles.Any, Data.Locale, out int v))
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
        }
        value = new DynamicIntegerValue(0);
        return false;
    }
    /// <summary>Constant float value, random value from a <see cref="FloatRange"/>, or a random value from a <see cref="FloatSet"/>.</summary>
    public static unsafe bool TryReadFloatValue(this ref Utf8JsonReader reader, out DynamicFloatValue value)
    {
        if (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string str = reader.GetString();
                if (string.IsNullOrEmpty(str))
                {
                    value = new DynamicFloatValue(0f);
                    return false;
                }

                if (str.Length > 3 && str[0] == '$')
                {
                    if (str[1] == '(' && str[str.Length - 1] == ')') // read range
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
                                for (int i = str.Length - 1; i > sep; i--)
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

                            value = new DynamicFloatValue(new FloatRange(v1f, v2f));
                            return true;
                        }
                        else
                        {
                            value = new DynamicFloatValue(0);
                            return false;
                        }
                    }
                    else if (str[1] == '[' && str[str.Length - 1] == ']')
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
                        value = new DynamicFloatValue(new FloatSet(res));
                        return true;

                    }
                    else
                    {
                        value = new DynamicFloatValue(0);
                        return false;
                    }
                }
                else if (float.TryParse(str, System.Globalization.NumberStyles.Any, Data.Locale, out float v))
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
        }
        value = new DynamicFloatValue(0);
        return false;
    }
    /// <summary>Raw string or a random value from a <see cref="StringSet"/>.</summary>
    public static unsafe bool TryReadStringValue(this ref Utf8JsonReader reader, out DynamicStringValue value)
    {
        if (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string str = reader.GetString();
                if (string.IsNullOrEmpty(str))
                {
                    value = new DynamicStringValue(str);
                    return false;
                }
                if (str.Length > 3 && str[0] == '$')
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
                        value = new DynamicStringValue(new StringSet(res));
                        return true;

                    }
                    else
                    {
                        value = new DynamicStringValue(str);
                        return true;
                    }
                }
                else
                {
                    value = new DynamicStringValue(str);
                    return true;
                }
            }
        }
        value = new DynamicStringValue(null);
        return false;
    }
    public static unsafe bool TryReadEnumValue<TEnum>(this ref Utf8JsonReader reader, out DynamicEnumValue<TEnum> value) where TEnum : unmanaged
    {
        if (!TryReadStringValue(ref reader, out DynamicStringValue v2))
        {
            value = default;
            L.LogWarning("Error reading a value as a dynamic " + typeof(TEnum).Name);
            return false;
        }
        value = new DynamicEnumValue<TEnum>(ref v2);
        return true;
    }
    public static bool TryReadEnumValue<TEnum>(this ref Utf8JsonReader reader, out TEnum value) where TEnum : unmanaged
    {
        if (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string str = reader.GetString();
                return Enum.TryParse(str, true, out value);
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out long v2))
                {
                    try
                    {
                        value = (TEnum)Convert.ChangeType(v2, typeof(TEnum));
                        return true;
                    }
                    catch { }
                }
            }
        }
        value = default;
        return false;
    }
}
public readonly struct DynamicIntegerValue
{
    internal readonly int constant;
    internal readonly IntegralRange range;
    internal readonly IntegralSet set;
    public readonly EDynamicValueType type;
    public DynamicIntegerValue(int constant)
    {
        this.constant = constant;
        this.range = default;
        this.set = default;
        this.type = EDynamicValueType.CONSTANT;
    }
    public DynamicIntegerValue(ref IntegralRange range)
    {
        this.constant = default;
        this.range = range;
        this.set = default;
        this.type = EDynamicValueType.RANGE;
    }
    public DynamicIntegerValue(ref IntegralSet set)
    {
        this.constant = default;
        this.range = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
    }
    public DynamicIntegerValue(IntegralRange range)
    {
        this.constant = default;
        this.range = range;
        this.set = default;
        this.type = EDynamicValueType.RANGE;
    }
    public DynamicIntegerValue(IntegralSet set)
    {
        this.constant = default;
        this.range = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
    }
    public readonly int GetValue()
    {
        if (type == EDynamicValueType.RANGE)
        {
            if (range.Minimum == range.Maximum)
                return range.Minimum;
            return UnityEngine.Random.Range(range.Minimum, range.Maximum + 1);
        }
        else if (type == EDynamicValueType.SET)
        {
            if (set.length == 0)
                return constant;
            else if (set.length == 1)
                return set.Set[0];
            return set.Set[UnityEngine.Random.Range(0, set.length)];
        }
        return constant;
    }
    public readonly int[] GetSetValue()
    {
        if (type == EDynamicValueType.SET)
            return set.Set;
        else if (type == EDynamicValueType.CONSTANT)
            return new int[1] { constant };
        return new int[0];
    }
    public override string ToString() => type switch
    {
        EDynamicValueType.SET => set.ToString(),
        EDynamicValueType.RANGE => range.ToString(),
        _ => constant.ToString()
    };
}
public readonly struct DynamicFloatValue
{
    internal readonly float constant;
    internal readonly FloatRange range;
    internal readonly FloatSet set;
    public readonly EDynamicValueType type;
    public DynamicFloatValue(float constant)
    {
        this.constant = constant;
        this.range = default;
        this.set = default;
        this.type = EDynamicValueType.CONSTANT;
    }
    public DynamicFloatValue(ref FloatRange range)
    {
        this.constant = default;
        this.range = range;
        this.set = default;
        this.type = EDynamicValueType.RANGE;
    }
    public DynamicFloatValue(ref FloatSet set)
    {
        this.constant = default;
        this.range = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
    }
    public DynamicFloatValue(FloatRange range)
    {
        this.constant = default;
        this.range = range;
        this.set = default;
        this.type = EDynamicValueType.RANGE;
    }
    public DynamicFloatValue(FloatSet set)
    {
        this.constant = default;
        this.range = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
    }
    public readonly float GetValue()
    {
        if (type == EDynamicValueType.RANGE)
        {
            if (range.Minimum == range.Maximum)
                return range.Minimum;
            return UnityEngine.Random.Range(range.Minimum, range.Maximum + 1);
        }
        else if (type == EDynamicValueType.SET)
        {
            if (set.length == 0)
                return constant;
            else if (set.length == 1)
                return set.Set[0];
            return set.Set[UnityEngine.Random.Range(0, set.length)];
        }
        return constant;
    }
    public readonly float[] GetSetValue()
    {
        if (type == EDynamicValueType.SET)
            return set.Set;
        else if (type == EDynamicValueType.CONSTANT)
            return new float[1] { constant };
        return new float[0];
    }
    public override string ToString() => type switch
    {
        EDynamicValueType.SET => set.ToString(),
        EDynamicValueType.RANGE => range.ToString(),
        _ => constant.ToString()
    };
}
public readonly struct DynamicStringValue
{
    internal readonly string constant;
    internal readonly StringSet set;
    public readonly EDynamicValueType type;
    public DynamicStringValue(string constant)
    {
        this.constant = constant;
        this.set = default;
        this.type = EDynamicValueType.CONSTANT;
    }
    public DynamicStringValue(ref StringSet set)
    {
        this.constant = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
    }
    public DynamicStringValue(StringSet set)
    {
        this.constant = default;
        this.set = set;
        this.type = EDynamicValueType.SET;
    }
    public readonly string GetValue()
    {
        if (type == EDynamicValueType.SET)
        {
            if (set.length == 0)
                return constant;
            else if (set.length == 1)
                return set.Set[0];
            return set.Set[UnityEngine.Random.Range(0, set.length)];
        }
        return constant;
    }
    public readonly string[] GetSetValue()
    {
        if (type == EDynamicValueType.SET)
            return set.Set;
        else if (type == EDynamicValueType.CONSTANT && constant != null)
            return new string[1] { constant };
        return new string[0];
    }

    public override string ToString() => type switch
    {
        EDynamicValueType.SET => set.ToString(),
        _ => constant ?? string.Empty
    };
}
public readonly struct DynamicEnumValue<TEnum> where TEnum : unmanaged
{
    internal readonly TEnum constant;
    internal readonly TEnum[] set;
    public readonly EDynamicValueType type;
    public DynamicEnumValue(TEnum constant)
    {
        this.constant = constant;
        this.set = default;
        this.type = EDynamicValueType.CONSTANT;
    }
    public DynamicEnumValue(ref DynamicStringValue value)
    {
        if (value.type == EDynamicValueType.CONSTANT)
        {
            if (!Enum.TryParse(value.constant, true, out this.constant))
                L.LogWarning("Error reading " + value.constant + " as " + typeof(TEnum).Name);
            this.set = default;
        }
        else if (value.type == EDynamicValueType.SET)
        {
            this.set = new TEnum[value.set.length];
            for (int i = 0; i < value.set.length; i++)
                if (!Enum.TryParse(value.set.Set[i], true, out this.set[i]))
                    L.LogWarning("Error reading " + value.set.Set[i] + " as " + typeof(TEnum).Name);
            this.constant = default;
        }
        else
        {
            this.set = default;
            this.constant = default;
        }
        this.type = value.type;
    }
    public DynamicEnumValue(ref StringSet set)
    {
        this.constant = default;
        this.set = new TEnum[set.length];
        for (int i = 0; i < set.length; i++)
            if (!Enum.TryParse(set.Set[i], true, out this.set[i]))
                L.LogWarning("Error reading " + set.Set[i] + " as " + typeof(TEnum).Name);
        this.type = EDynamicValueType.SET;
    }
    public DynamicEnumValue(StringSet set)
    {
        this.constant = default;
        this.set = new TEnum[set.length];
        for (int i = 0; i < set.length; i++)
            if (!Enum.TryParse(set.Set[i], true, out this.set[i]))
                L.LogWarning("Error reading " + set.Set[i] + " as " + typeof(TEnum).Name);
        this.type = EDynamicValueType.SET;
    }
    public readonly TEnum GetValue()
    {
        if (type == EDynamicValueType.SET)
        {
            if (set.Length == 0)
                return constant;
            else if (set.Length == 1)
                return set[0];
            return set[UnityEngine.Random.Range(0, set.Length)];
        }
        return constant;
    }
    public readonly TEnum[] GetSetValue()
    {
        if (type == EDynamicValueType.SET)
            return set;
        else if (type == EDynamicValueType.CONSTANT)
            return new TEnum[1] { constant };
        return new TEnum[0];
    }

    public override string ToString() => type switch
    {
        EDynamicValueType.SET => set.ToString(),
        _ => constant.ToString() ?? string.Empty
    };
}

public enum EDynamicValueType
{
    CONSTANT,
    RANGE,
    SET
}

/// <summary>"$(min:max)"</summary>
public readonly struct IntegralRange
{
    public readonly int Minimum;
    public readonly int Maximum;
    public IntegralRange(int min, int max)
    {
        Minimum = min;
        Maximum = max;
    }
    public override readonly string ToString() => "$(" + Minimum.ToString(Data.Locale) + ":" + Maximum.ToString(Data.Locale) + ")";
}
/// <summary>"$(min:max)"</summary>
public readonly struct FloatRange
{
    public readonly float Minimum;
    public readonly float Maximum;
    public FloatRange(float min, float max)
    {
        Minimum = min;
        Maximum = max;
    }
    public override readonly string ToString() => "$(" + Minimum.ToString(Data.Locale) + ":" + Maximum.ToString(Data.Locale) + ")";
}
/// <summary>"$[1, 2, 3, 4]"</summary>
public readonly struct IntegralSet : IEnumerable<int>
{
    public readonly int[] Set;
    public readonly int length;
    public IntegralSet(int[] set)
    {
        Set = set;
        length = set.Length;
    }
    public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)Set).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Set.GetEnumerator();
    public override readonly string ToString()
    {
        StringBuilder sb = new StringBuilder("$[");
        for (int i = 0; i < length; i++)
        {
            if (i != 0) sb.Append(',');
            sb.Append(Set[i].ToString(Data.Locale));
        }
        sb.Append(']');
        return sb.ToString();
    }
}
/// <summary>"$[1.3, 5.2, 8.3, 12.4]"</summary>
public readonly struct FloatSet : IEnumerable<float>
{
    public readonly float[] Set;
    public readonly int length;
    public FloatSet(float[] set)
    {
        Set = set;
        length = set.Length;
    }
    public IEnumerator<float> GetEnumerator() => ((IEnumerable<float>)Set).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Set.GetEnumerator();
    public override readonly string ToString()
    {
        StringBuilder sb = new StringBuilder("$[");
        for (int i = 0; i < length; i++)
        {
            if (i != 0) sb.Append(',');
            sb.Append(Set[i].ToString(Data.Locale));
        }
        sb.Append(']');
        return sb.ToString();
    }

}
/// <summary>"$[string1, ben smells, 18, nice]"</summary>
public readonly struct StringSet : IEnumerable<string>
{
    public readonly string[] Set;
    public readonly int length;
    public StringSet(string[] set)
    {
        Set = set;
        length = set.Length;
    }
    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)Set).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Set.GetEnumerator();
    public override readonly string ToString()
    {
        StringBuilder sb = new StringBuilder("$[");
        for (int i = 0; i < length; i++)
        {
            if (i != 0) sb.Append(',');
            if (Set[i] != null)
                sb.Append(Set[i].Replace(',', '_'));
        }
        sb.Append(']');
        return sb.ToString();
    }
}

public interface INotifyOnKill
{
    public void OnKill(UCWarfare.KillEventArgs kill);
}
public interface IQuestState<TTracker, TDataNew> where TTracker : BaseQuestTracker where TDataNew : BaseQuestData
{
    public void WriteQuestState(Utf8JsonWriter writer);
    public void ReadQuestState(ref Utf8JsonReader reader);
    public void Init(TDataNew data);
}