//#define LOG_OP_CODES
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using UnityEngine;

namespace Uncreated.Warfare.Quests;

public class RewardExpression
{
    private static readonly char[] TOKEN_SPLITS = new char[] { '*', '/', '+', '-', '%', '(', ')', '[', ']', '{', '}', ',' };
    private readonly Type _rtnType;
    private readonly Type? _questStateType;
    private readonly string _expression;
    private readonly string _expression2;
    private readonly EvaluateDelegate _method = null!;
    public EQuestType QuestType { get; internal set; }
    public EQuestRewardType RewardType { get; private set; }
    private RewardExpression(EQuestRewardType type, EQuestType questType, EvaluateDelegate method)
    {
        RewardType = type;
        QuestType = questType;
        _method = method;
        _expression = "0";
        _expression2 = _expression;
    }
    public RewardExpression(EQuestRewardType type, EQuestType questType, string expression)
    {
        RewardType = type;
        QuestType = questType;
        _expression = expression;
        if (!QuestRewards.QuestRewardTypes.TryGetValue(type, out _rtnType))
            throw new ArgumentException("Invalid reward type: \"" + type.ToString() + "\"");
        _rtnType = (Attribute.GetCustomAttribute(_rtnType, typeof(QuestRewardAttribute)) as QuestRewardAttribute)?.ReturnType!;

        if (_rtnType == null || !_rtnType.IsPrimitive || _rtnType == typeof(char) || _rtnType == typeof(bool))
            throw new ArgumentException((_rtnType?.Name ?? "<unknown-type>") + " is not a valid return type for RewardExpressions");

        if (!QuestManager.QuestTypes.TryGetValue(questType, out _questStateType))
            throw new ArgumentException("Invalid quest type: \"" + type.ToString() + "\"");

        Type[] ts = _questStateType.GetNestedTypes();
        for (int i = 0; i < ts.Length; ++i)
        {
            Type t = ts[i];
            if (typeof(IQuestState).IsAssignableFrom(t))
            {
                _questStateType = t;
                goto validate;
            }
        }

        throw new ArgumentException("Quest type " + _questStateType.Name + " is missing a nested IQuestState type");

    validate:
        List<string> tokens = new List<string>(32);
        int tknSt = 0;
        for (int i = 0; i < _expression.Length; ++i)
        {
            char c = _expression[i];
            if (c == ' ')
            {
                if (i != 0 && i != tknSt)
                    tokens.Add(_expression.Substring(tknSt, i - tknSt));
                tknSt = i + 1;
                continue;
            }
            for (int j = 0; j < TOKEN_SPLITS.Length; ++j)
            {
                if (TOKEN_SPLITS[j] == c)
                {
                    if (i != 0 && i != tknSt)
                        tokens.Add(_expression.Substring(tknSt, i - tknSt));
                    tokens.Add(new string(c, 1));
                    tknSt = i + 1;
                    break;
                }
            }
        }
        if (tknSt < _expression.Length)
            tokens.Add(_expression.Substring(tknSt));
        int pLvl = 0;
        for (int i = 0; i < tokens.Count; ++i)
        {
            if (tokens[i][0] is '*' or '/')
            {
                int spanSt = i - 1;
                int spanEnd = i + 1;
                if (spanSt < 0 || spanEnd > tokens.Count - 1 || (spanSt > 0 && tokens[spanSt - 1][0] is '(' or '[' or '{' && spanEnd < tokens.Count - 1 && tokens[spanEnd + 1][0] is ')' or ']' or '}'))
                    continue;
                bool next = false;
                char v;
                if (tokens[spanSt][0] is ')' or ']' or '}')
                {
                    pLvl = 1;
                    for (int j = spanSt - 1; j >= 0; --j)
                    {
                        if (next || tokens[j][0] is '(' or '[' or '{')
                        {
                            if (!next && j > 0)
                            {
                                v = tokens[j - 1][0];
                                for (int k = 0; k < TOKEN_SPLITS.Length; ++k)
                                {
                                    if (TOKEN_SPLITS[k] == v)
                                        goto f;
                                }
                                next = true;
                                continue;
                            }
                        f:
                            if (--pLvl <= 0)
                            {
                                spanSt = j;
                                break;
                            }
                            next = false;
                        }
                        else if (tokens[j][0] is ')' or ']' or '}')
                            ++pLvl;
                    }
                }
                bool endIsTs = false;
                v = tokens[spanEnd][0];
                for (int k = 0; k < TOKEN_SPLITS.Length; ++k)
                {
                    if (TOKEN_SPLITS[k] == v)
                    {
                        endIsTs = true;
                        break;
                    }
                }
                if (!endIsTs || v is '(' or '[' or '{')
                {
                    next = false;
                    pLvl = !endIsTs ? 0 : 1;
                    for (int j = spanEnd + 1; j < tokens.Count; ++j)
                    {
                        if (next || tokens[j][0] is ')' or ']' or '}')
                        {
                            if (--pLvl <= 0)
                            {
                                spanEnd = j;
                                break;
                            }
                        }
                        else if (tokens[j][0] is '(' or '[' or '{')
                            ++pLvl;
                    }
                }
                tokens.Insert(spanEnd + 1, ")");
                tokens.Insert(spanSt, "(");
                i = spanEnd + 2;
            }
        }

        pLvl = 0;
        bool was0Once = false;
        for (int i = 0; i < tokens.Count; ++i)
        {
            if (i != 0)
                was0Once |= pLvl == 0;

            if (tokens[i][0] is '(' or '[' or '{')
                ++pLvl;
            else if (tokens[i][0] is ')' or ']' or '}')
                --pLvl;
        }
        if (!was0Once && tokens[0][0] is '(' or '[' or '{' && tokens[tokens.Count - 1][0] is ')' or ']' or '}')
        {
            tokens.RemoveAt(0);
            tokens.RemoveAt(tokens.Count - 1);
        }


        _expression2 = string.Join(string.Empty, tokens);

        Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars = new Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>>(tokens.Count);
        FieldInfo[] fields = _questStateType!.GetFields(BindingFlags.Public | BindingFlags.Instance);
        DynamicMethod method = new DynamicMethod("EvaluateReward", typeof(object), new Type[] { typeof(IQuestState) }, typeof(QuestRewards), true);
        method.DefineParameter(0, ParameterAttributes.None, "state");

#if LOG_OP_CODES
        L.Log("object QuestRewards.EvaluateReward(IQuestState state)");
        L.Log("{");
#endif
        ILGenerator il = method.GetILGenerator();
        int lcl = 0;
        for (int i = 0; i < tokens.Count; ++i)
        {
            for (int f = 0; f < fields.Length; ++f)
            {
                FieldInfo field = fields[f];
                string name = field.Name;
                if (Attribute.GetCustomAttribute(field, typeof(RewardFieldAttribute)) is RewardFieldAttribute rfa)
                {
                    if (rfa.DisallowVariableUsage)
                        continue;
                    else if (rfa.Name is not null)
                        name = rfa.Name;
                }
                if (name.Equals(tokens[i], StringComparison.OrdinalIgnoreCase))
                {
                    foreach (KeyValuePair<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> pair in vars)
                    {
                        if (pair.Value.Key.Key == field)
                        {
                            vars.Add(i, new KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>(pair.Value.Key, pair.Value.Value));
                            goto skip;
                        }
                    }
                    MethodInfo m2 = field.FieldType.GetMethod(nameof(IDynamicValue<object>.IChoice.InsistValue), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (!m2.ReturnType.IsPrimitive || m2.ReturnType == typeof(char) || m2.ReturnType == typeof(bool))
                    {
                        L.LogError("Invalid variable type: " + tokens[i] + ", " + m2.ReturnType.Name);
                        goto error;
                    }
                    if (m2 != null)
                    {
                        il.DeclareLocal(typeof(double));
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        LogOpCode("// Variable #" + (lcl + 1) + " " + field.Name, ConsoleColor.Green);
                        Console.ResetColor();
                        vars.Add(i, new KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>(new KeyValuePair<FieldInfo, MethodInfo>(field, m2), lcl));
                        LogOpCode("ldarg.0");
                        il.Emit(OpCodes.Ldarg_0);
                        if (field.DeclaringType.IsValueType)
                        {
                            LogOpCode("unbox " + field.DeclaringType.FullName);
                            il.Emit(OpCodes.Unbox, field.DeclaringType);
                        }
                        LogOpCode("ldfld " + field.Name);
                        il.Emit(OpCodes.Ldfld, field);
                        LogOpCode("call " + m2.Name);
                        il.Emit(OpCodes.Call, m2);
                        if (m2.ReturnType != typeof(double))
                        {
                            LogOpCode("conv.r8");
                            il.Emit(OpCodes.Conv_R8);
                        }
                        LogOpCode("stloc " + lcl);
                        LogOpCode();
                        il.Emit(OpCodes.Stloc, lcl);
                        ++lcl;
                    }
                skip:;
                }
            }
        }
        int stackSize = 0;
        int index = 0;
        if (!Evaluate(ref index, il, tokens, vars, ref stackSize))
            goto error;
        if (_rtnType == typeof(int))
        {
            LogOpCode("conv.i4");
            il.Emit(OpCodes.Conv_I4);
        }
        else if (_rtnType == typeof(float))
        {
            LogOpCode("conv.r4");
            il.Emit(OpCodes.Conv_R4);
        }
        else if (_rtnType == typeof(uint))
        {
            LogOpCode("conv.u4");
            il.Emit(OpCodes.Conv_U4);
        }
        else if (_rtnType == typeof(short))
        {
            LogOpCode("conv.i2");
            il.Emit(OpCodes.Conv_I2);
        }
        else if (_rtnType == typeof(ushort))
        {
            LogOpCode("conv.u2");
            il.Emit(OpCodes.Conv_U2);
        }
        else if (_rtnType == typeof(long))
        {
            LogOpCode("conv.i8");
            il.Emit(OpCodes.Conv_I8);
        }
        else if (_rtnType == typeof(ulong))
        {
            LogOpCode("conv.u8");
            il.Emit(OpCodes.Conv_U8);
        }
        else if (_rtnType == typeof(byte))
        {
            LogOpCode("conv.u1");
            il.Emit(OpCodes.Conv_U1);
        }
        else if (_rtnType == typeof(sbyte))
        {
            LogOpCode("conv.i1");
            il.Emit(OpCodes.Conv_I1);
        }
        if (stackSize < 1)
            goto error;
        LogOpCode("box");
        il.Emit(OpCodes.Box, _rtnType);
        LogOpCode("ret");
        il.Emit(OpCodes.Ret);

#if LOG_OP_CODES
        L.Log("}");
#endif
        _method = (EvaluateDelegate)method.CreateDelegate(typeof(EvaluateDelegate));
        return;
    error:
        throw new ArgumentException("Invalid exrpession: \"" + (_expression2 ?? _expression) + "\"", nameof(expression));
    }
    private bool Evaluate(ref int stPos, ILGenerator il, List<string> tokens, Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars, ref int stackSize)
    {
        bool emitted = false;
        for (int i = stPos; i < tokens.Count;)
        {
            bool last = i == tokens.Count - 1;
            if (tokens[i].Length == 1)
            {
                switch (tokens[i][0])
                {
                    case ')':
                    case ']':
                    case '}':
                    case ',':
                        stPos = i + 1;
                        return true;
                    case '(':
                    case '[':
                    case '{':
                        if (!last)
                        {
                            int leftOff = i + 1;
                            if (!Evaluate(ref leftOff, il, tokens, vars, ref stackSize)) return false;
                            i = leftOff;
                        }
                        else return true;
                        emitted = true;
                        continue;
                    case '+':
                        if (!last)
                        {
                            if (!TwoCodeCall(OpCodes.Add, il, tokens, vars, ref i, ref stackSize)) return false;
                        }
                        else return true;
                        emitted = true;
                        continue;
                    case '-':
                        if (!last)
                        {
                            if (!TwoCodeCall(OpCodes.Sub, il, tokens, vars, ref i, ref stackSize)) return false;
                        }
                        else return true;
                        emitted = true;
                        continue;
                    case '/':
                        if (!last)
                        {
                            if (!TwoCodeCall(OpCodes.Div, il, tokens, vars, ref i, ref stackSize)) return false;
                        }
                        else return true;
                        emitted = true;
                        continue;
                    case '*':
                        if (!last)
                        {
                            if (!TwoCodeCall(OpCodes.Mul, il, tokens, vars, ref i, ref stackSize)) return false;
                        }
                        else return true;
                        emitted = true;
                        continue;
                    case '%':
                        if (!last)
                        {
                            if (!TwoCodeCall(OpCodes.Rem, il, tokens, vars, ref i, ref stackSize)) return false;
                        }
                        else return true;
                        emitted = true;
                        continue;
                }
            }
            if (!Load(ref i, il, tokens, vars, ref stackSize))
                if (tokens.Count < i + 2 && tokens[i + 1][0] != '(' || !LoadMethod(ref i, il, tokens, vars, ref stackSize)) return false;
            emitted = true;
        }
        return emitted;
    }
    private bool TwoCodeCall(OpCode code, ILGenerator il, List<string> tokens, Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars, ref int i, ref int stackSize)
    {
        if (stackSize < 1)
            return false;
        int l = i + 1;
        if (Load(ref l, il, tokens, vars, ref stackSize))
        {
            i = l;
            il.Emit(code);
            LogOpCode(code.Name);
            LogOpCode();
            --stackSize;
            return true;
        }
        else if (tokens.Count > i + 3 && tokens[i + 2][0] is '(' or '[' or '{')
        {
            LoadMethod(ref l, il, tokens, vars, ref stackSize);
            i = l;
            il.Emit(code);
            LogOpCode(code.Name);
            LogOpCode();
            --stackSize;
            return true;
        }

        return false;
    }
    private bool Load(ref int index, ILGenerator il, List<string> tokens, Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars, ref int stackSize)
    {
        if (tokens[index][0] is '(' or '[' or '{')
        {
            int l = index + 1;
            if (Evaluate(ref l, il, tokens, vars, ref stackSize))
            {
                index = l;
                return true;
            }
            return false;
        }

        if (!LoadConstant(index, il, tokens, ref stackSize) && !LoadVariable(index, il, tokens, vars, ref stackSize))
        {
            return false;
        }
        else ++index;
        return true;
    }
    private bool LoadConstant(int index, ILGenerator il, List<string> tokens, ref int stackSize)
    {
        if (index >= tokens.Count)
            return false;
        string tkn = tokens[index];
        if (tkn[tkn.Length - 1] is 'f' or 'F' or 'd' or 'D')
            tkn = tkn.Substring(0, tkn.Length - 1);
        if (double.TryParse(tkn.Replace('_', '-'), NumberStyles.Number, CultureInfo.InvariantCulture, out double res))
        {
            LoadConstant(il, res);
            ++stackSize;
            return true;
        }
        return false;
    }
    private bool LoadVariable(int index, ILGenerator il, List<string> tokens, Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars, ref int stackSize)
    {
        if (index >= tokens.Count)
            return false;
        if (vars.TryGetValue(index, out KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int> mi))
        {
            LogOpCode("ldloc " + mi.Value);
            il.Emit(OpCodes.Ldloc, mi.Value);
            if (mi.Key.Value.ReturnType != typeof(void))
                ++stackSize;
            return true;
        }
        else
        {
            string tkn = tokens[index];
            FieldInfo? fi = typeof(Mathf).GetField(tkn, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static);
            if (fi != null)
            {
                if (fi.IsLiteral)
                {
                    if (fi.FieldType == typeof(int))
                        LoadConstant(il, (int)fi.GetValue(null));
                    else if (fi.FieldType == typeof(float))
                        LoadConstant(il, (double)(float)fi.GetValue(null));
                    else if (fi.FieldType == typeof(double))
                        LoadConstant(il, (double)fi.GetValue(null));
                    else if (fi.FieldType == typeof(uint))
                        LoadConstant(il, (uint)fi.GetValue(null));
                    else if (fi.FieldType == typeof(long))
                        LoadConstant(il, (long)fi.GetValue(null));
                    else if (fi.FieldType == typeof(ulong))
                        LoadConstant(il, (ulong)fi.GetValue(null));
                    else if (fi.FieldType == typeof(short))
                        LoadConstant(il, (short)fi.GetValue(null));
                    else if (fi.FieldType == typeof(ushort))
                        LoadConstant(il, (ushort)fi.GetValue(null));
                    else if (fi.FieldType == typeof(byte))
                        LoadConstant(il, (byte)fi.GetValue(null));
                    else if (fi.FieldType == typeof(sbyte))
                        LoadConstant(il, (sbyte)fi.GetValue(null));
                    else return false;
                    LogOpCode();
                    ++stackSize;
                    return true;
                }
                else
                {
                    il.Emit(OpCodes.Ldsfld, fi);
                    LogOpCode("ldsfld " + fi.Name);
                    if (fi.FieldType != typeof(double))
                    {
                        il.Emit(OpCodes.Conv_R8);
                        LogOpCode("conv.r8");
                    }
                    LogOpCode();
                    ++stackSize;
                    return true;
                }
            }
        }
        return false;
    }
    private static void LoadConstant(ILGenerator il, double c)
    {
        il.Emit(OpCodes.Ldc_R8, c);
        LogOpCode("ldc.r8 " + c.ToString(Data.Locale));
    }
    private bool LoadMethod(ref int index, ILGenerator il, List<string> tokens, Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars, ref int stackSize)
    {
        if (index >= tokens.Count - 2)
            return false;
        string tkn = tokens[index];
        int ind2 = -1;
        int ind1 = index + 2;
        int pCt = 0;
        int pLvl = 1;
        for (int i = ind1; i < tokens.Count; ++i)
        {
            if (tokens[i][0] == '(')
                ++pLvl;
            if (tokens[i][0] == ')')
            {
                if (--pLvl < 1)
                {
                    ind2 = i - 1;
                    break;
                }
            }
            else if (pLvl == 1 && tokens[i][0] == ',')
                ++pCt;
        }

        if (ind2 + 1 != ind1)
            ++pCt;

        MethodInfo? f = //GetMethod(tkn, pCt, typeof(Mathf).GetMethods(BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public)) ?? 
                        GetMethod(tkn, pCt, typeof(Math).GetMethods(BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public));

        if (f != null)
        {
            ParameterInfo[] p = f.GetParameters();
            int ctoken = ind1;
            for (int i = 0; i < pCt; ++i)
            {
                if (!Evaluate(ref ctoken, il, tokens, vars, ref stackSize))
                    return false;
                if (p[i].ParameterType != typeof(double))
                {
                    if (p[i].ParameterType == typeof(int))
                    {
                        LogOpCode("conv.i4");
                        il.Emit(OpCodes.Conv_I4);
                    }
                    else if (p[i].ParameterType == typeof(float))
                    {
                        LogOpCode("conv.r4");
                        il.Emit(OpCodes.Conv_R4);
                    }
                    else if (p[i].ParameterType == typeof(uint))
                    {
                        LogOpCode("conv.u4");
                        il.Emit(OpCodes.Conv_U4);
                    }
                    else if (p[i].ParameterType == typeof(short))
                    {
                        LogOpCode("conv.i2");
                        il.Emit(OpCodes.Conv_I2);
                    }
                    else if (p[i].ParameterType == typeof(ushort))
                    {
                        LogOpCode("conv.u2");
                        il.Emit(OpCodes.Conv_U2);
                    }
                    else if (p[i].ParameterType == typeof(long))
                    {
                        LogOpCode("conv.i8");
                        il.Emit(OpCodes.Conv_I8);
                    }
                    else if (p[i].ParameterType == typeof(ulong))
                    {
                        LogOpCode("conv.u8");
                        il.Emit(OpCodes.Conv_U8);
                    }
                    else if (p[i].ParameterType == typeof(byte))
                    {
                        LogOpCode("conv.u1");
                        il.Emit(OpCodes.Conv_U1);
                    }
                    else if (p[i].ParameterType == typeof(sbyte))
                    {
                        LogOpCode("conv.i1");
                        il.Emit(OpCodes.Conv_I1);
                    }
                }
            }
            stackSize -= pCt;
            il.Emit(OpCodes.Call, f);
            LogOpCode("call " + f.Name);
            ++stackSize;
            if (f.ReturnType != typeof(double))
            {
                LogOpCode("conv.r8");
                il.Emit(OpCodes.Conv_R8);
            }
            LogOpCode();
        }

        index = ind2 != -1 ? (ind2 + 2) : tokens.Count;
        return true;
    }
    private static MethodInfo? GetMethod(string name, int pCt, MethodInfo[] ms)
    {
        MethodInfo? f = null;
        for (int i = 0; i < ms.Length; ++i)
        {
            if (ms[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                if (ms[i].ReturnType == typeof(void) || !ms[i].ReturnType.IsPrimitive || ms[i].ReturnType == typeof(char) || ms[i].ReturnType == typeof(bool))
                    continue;
                ParameterInfo[] p = ms[i].GetParameters();
                if (p.Length != pCt)
                    continue;
                for (int j = 0; j < p.Length; ++j)
                {
                    if (!p[j].ParameterType.IsPrimitive || p[j].ParameterType == typeof(char) || p[j].ParameterType == typeof(bool))
                        goto next;
                }

                f = ms[i];
                break;
            }
        next:;
        }
        return f;
    }
    [Conditional("LOG_OP_CODES")]
    private static void LogOpCode(string? str = null, ConsoleColor color = ConsoleColor.Gray)
    {
#if LOG_OP_CODES
        L.Log(str is null ? string.Empty : (" " + str), color);
#endif
    }
    public object? Evaluate(IQuestState state)
    {
        if (_method == null)
        {
            L.LogError("Tried to evaluate an invalid RewardExpression.");
            return null;
        }
        try
        {
            return _method(state);
        }
        catch (Exception e)
        {
            L.LogError("Failed to execute expression: " + (_expression2 ?? _expression));
            L.LogError(e);
            return null;
        }
    }
    public static RewardExpression? ReadFromJson(ref Utf8JsonReader reader, EQuestType quest)
    {
        EQuestRewardType reward = EQuestRewardType.NONE;
        string? expression = null;
        do
        {
            if (reader.TokenType == JsonTokenType.StartObject) continue;
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? prop = reader.GetString()!;
                if (reader.Read())
                {
                    if (reward == EQuestRewardType.NONE && prop.Equals("type", StringComparison.OrdinalIgnoreCase))
                    {
                        prop = reader.GetString()!;
                        if (!Enum.TryParse(prop, true, out EQuestRewardType type))
                            L.LogWarning("Invalid quest type " + prop + " in " + quest + " RewardExpression read.");
                        else
                            reward = type;
                    }
                    else if (expression is null && prop.Equals("expression", StringComparison.OrdinalIgnoreCase))
                    {
                        expression = reader.GetString()!;
                    }
                }
            }
        } while (reader.Read());

        if (expression != null && reward != EQuestRewardType.NONE)
        {
            try
            {
                return new RewardExpression(reward, quest, expression);
            }
            catch (ArgumentException ex)
            {
                L.LogWarning(ex.Message + " in " + quest + " RewardExpression read.");
            }
            catch (Exception ex)
            {
                L.LogWarning(ex.GetType().Name + " in " + quest + " RewardExpression read.");
                L.LogError(ex);
            }
        }
        else if (expression is null)
            L.LogWarning("Missing expression in quest reward for " + quest.ToString());
        else if (reward == EQuestRewardType.NONE)
            L.LogWarning("Missing reward type in quest reward for " + quest.ToString());

        return null;
    }
    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("type");
        writer.WriteStringValue(RewardType.ToString());
        writer.WritePropertyName("expression");
        writer.WriteStringValue(_expression);
        writer.WriteEndObject();
    }

    private delegate object EvaluateDelegate(IQuestState state);
}

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class RewardFieldAttribute : Attribute
{
    readonly string? _name;
    readonly bool _dvu;

    public RewardFieldAttribute(string nameOverride)
    {
        this._name = nameOverride;
        _dvu = false;
    }
    public RewardFieldAttribute(bool disableVariableUsage)
    {
        this._name = null;
        _dvu = disableVariableUsage;
    }

    public string? Name => _name;
    public bool DisallowVariableUsage => _dvu;
}