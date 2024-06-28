using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.NewQuests;
using Uncreated.Warfare.NewQuests.Parameters;

namespace Uncreated.Warfare.Quests;

public class RewardExpression
{
    private static readonly char[] TokenSplits = [ '*', '/', '+', '-', '%', '(', ')', '[', ']', '{', '}', ',', '^' ];
    private readonly string _expression;
    private readonly string _expression2;
    private readonly EvaluateDelegate _method;
    private static MethodInfo[]? _mathMethods;
    private readonly ConstructorInfo _createCtor;
    private const bool DebugLogging = false;
    public Type QuestType { get; }
    public Type StateType { get; }
    public Type RewardType { get; }
    public RewardExpression(Type rewardType, Type questType, string expression)
    {
        if (!typeof(IQuestReward).IsAssignableFrom(rewardType) || rewardType.IsInterface || rewardType.IsAbstract)
        {
            throw new ArgumentException("Reward type must be a non-abstract class or struct that implements IQuestReward.");
        }

        if (!questType.IsSubclassOf(typeof(QuestTemplate)) || rewardType.IsAbstract)
        {
            throw new ArgumentException("Quest type must be a non-abstract class that inherits QuestTemplate.");
        }

        Type? returnType = null;
        foreach (ConstructorInfo ctor in rewardType.GetConstructors())
        {
            ParameterInfo[] parameters = ctor.GetParameters();
            if (parameters.Length != 1)
                continue;

            Type paramType = parameters[0].ParameterType;
            if ((!paramType.IsPrimitive || paramType == typeof(char) || paramType == typeof(bool)) && paramType != typeof(string))
                continue;

            returnType = paramType;
            _createCtor = ctor;
            break;
        }


        if (returnType == null)
        {
            throw new ArgumentException("No valid construtors found with a single parameter of type string or a primitive number.", nameof(rewardType));
        }

        Type? stateType = null;
        questType.ForEachBaseType((type, _) =>
        {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(QuestTemplate<,,>))
                return true;

            stateType = type.GetGenericArguments()[2];
            return false;
        });

        if (stateType == null)
        {
            Type[] ts = questType.GetNestedTypes();
            for (int i = 0; i < ts.Length; ++i)
            {
                Type t = ts[i];
                if (!typeof(IQuestState).IsAssignableFrom(t))
                    continue;

                stateType = t;
                break;
            }

            if (stateType == null)
            {
                throw new ArgumentException($"Unable to identify the state type of quest type {Accessor.ExceptionFormatter.Format(questType)}.");
            }
        }

        QuestType = questType;
        StateType = stateType;
        RewardType = rewardType;
        _expression = expression;

        if (returnType == typeof(string))
        {
            DynamicMethod strMethod = new DynamicMethod($"EvaluateReward_{QuestType.Name}_{RewardType.Name}", typeof(IQuestReward), [ typeof(IQuestState) ], typeof(QuestRewards), true);
            strMethod.DefineParameter(0, ParameterAttributes.None, "state");

            IOpCodeEmitter strIl = strMethod.AsEmitter(debuggable: DebugLogging);
            strIl.Emit(OpCodes.Ldstr, _expression);
            strIl.Emit(OpCodes.Newobj, _createCtor!);

            if (rewardType.IsValueType)
                strIl.Emit(OpCodes.Box, rewardType);

            strIl.Emit(OpCodes.Ret);

            _method = (EvaluateDelegate)strMethod.CreateDelegate(typeof(EvaluateDelegate));
            _expression2 = expression;

            return;
        }

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
            for (int j = 0; j < TokenSplits.Length; ++j)
            {
                if (TokenSplits[j] == c)
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
        int pLvl;
        for (int i = 0; i < tokens.Count; ++i)
        {
            if (tokens[i][0] is '*' or '/' or '^')
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
                                for (int k = 0; k < TokenSplits.Length; ++k)
                                {
                                    if (TokenSplits[k] == v)
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
                for (int k = 0; k < TokenSplits.Length; ++k)
                {
                    if (TokenSplits[k] == v)
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
        if (!was0Once && tokens[0][0] is '(' or '[' or '{' && tokens[^1][0] is ')' or ']' or '}')
        {
            tokens.RemoveAt(0);
            tokens.RemoveAt(tokens.Count - 1);
        }


        _expression2 = string.Join(string.Empty, tokens);

        Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars = new Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>>(tokens.Count);
        FieldInfo[] fields = stateType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        DynamicMethod method = new DynamicMethod($"EvaluateReward_{QuestType.Name}_{RewardType.Name}", typeof(IQuestReward), [ typeof(IQuestState) ], typeof(QuestRewards), true);
        method.DefineParameter(0, ParameterAttributes.None, "state");

        IOpCodeEmitter il = method.AsEmitter(debuggable: DebugLogging);
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

                if (!name.Equals(tokens[i], StringComparison.OrdinalIgnoreCase))
                    continue;
                
                foreach (KeyValuePair<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> pair in vars)
                {
                    if (pair.Value.Key.Key != field)
                        continue;
                    
                    vars.Add(i, new KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>(pair.Value.Key, pair.Value.Value));
                    goto skip;
                }

                MethodInfo? m2 = field.FieldType.GetMethod(nameof(QuestParameterValue<object>.GetSingleValue), BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (m2 != null)
                {
                    if (!m2.ReturnType.IsPrimitive || m2.ReturnType == typeof(char) || m2.ReturnType == typeof(bool))
                    {
                        L.LogError("Invalid variable type: " + tokens[i] + ", " + m2.ReturnType.Name);
                        goto error;
                    }

                    il.DeclareLocal(typeof(double));
                    if (DebugLogging)
                    {
                        il.Comment("Variable #" + (lcl + 1) + " " + field.Name);
                    }

                    vars.Add(i, new KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>(new KeyValuePair<FieldInfo, MethodInfo>(field, m2), lcl));
                    il.Emit(OpCodes.Ldarg_0);
                    if (field.DeclaringType is { IsValueType: true })
                    {
                        il.Emit(OpCodes.Unbox, field.DeclaringType);
                    }

                    il.Emit(OpCodes.Ldfld, field);
                    il.Emit(OpCodes.Callvirt, m2);
                    if (m2.ReturnType != typeof(double))
                    {
                        il.Emit(OpCodes.Conv_R8);
                    }
                    if (lcl < byte.MaxValue)
                    {
                        il.Emit(OpCodes.Stloc_S, (byte)lcl);
                    }
                    else
                    {
                        il.Emit(OpCodes.Stloc, lcl);
                    }
                    ++lcl;
                }
                skip:;
            }
        }
        int stackSize = 0;
        int index = 0;

        if (!Evaluate(ref index, il, tokens, vars, ref stackSize))
            goto error;

        if (returnType == typeof(int))
        {
            il.Emit(OpCodes.Conv_I4);
        }
        else if (returnType == typeof(float))
        {
            il.Emit(OpCodes.Conv_R4);
        }
        else if (returnType == typeof(uint))
        {
            il.Emit(OpCodes.Conv_U4);
        }
        else if (returnType == typeof(short))
        {
            il.Emit(OpCodes.Conv_I2);
        }
        else if (returnType == typeof(ushort))
        {
            il.Emit(OpCodes.Conv_U2);
        }
        else if (returnType == typeof(long))
        {
            il.Emit(OpCodes.Conv_I8);
        }
        else if (returnType == typeof(ulong))
        {
            il.Emit(OpCodes.Conv_U8);
        }
        else if (returnType == typeof(byte))
        {
            il.Emit(OpCodes.Conv_U1);
        }
        else if (returnType == typeof(sbyte))
        {
            il.Emit(OpCodes.Conv_I1);
        }

        if (stackSize < 1)
            goto error;

        il.Emit(OpCodes.Newobj, _createCtor!);

        if (rewardType.IsValueType)
            il.Emit(OpCodes.Box, rewardType);

        il.Emit(OpCodes.Ret);

        _method = (EvaluateDelegate)method.CreateDelegate(typeof(EvaluateDelegate));
        return;
    error:
        throw new ArgumentException("Invalid exrpession: \"" + (_expression2 ?? _expression) + "\"", nameof(expression));
    }
    private bool Evaluate(ref int stPos, IOpCodeEmitter il, List<string> tokens, Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars, ref int stackSize)
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
                    case '^':
                        if (!last)
                        {
                            if (!PowerTwoCall(il, tokens, vars, ref i, ref stackSize)) return false;
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
            {
                if (tokens.Count < i + 2 && tokens[i + 1][0] != '(' || !LoadMethod(ref i, il, tokens, vars, ref stackSize))
                {
                    L.LogWarning("Unknown token: '" + tokens[i] + "'.");
                    return false;
                }
            }
            emitted = true;
        }
        return emitted;
    }
    private bool TwoCodeCall(OpCode code, IOpCodeEmitter il, List<string> tokens, Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars, ref int i, ref int stackSize)
    {
        if (stackSize < 1)
            return false;
        int l = i + 1;
        if (Load(ref l, il, tokens, vars, ref stackSize))
        {
            i = l;
            il.Emit(code);
            --stackSize;
            return true;
        }
        else if (tokens.Count > i + 3 && tokens[i + 2][0] is '(' or '[' or '{')
        {
            LoadMethod(ref l, il, tokens, vars, ref stackSize);
            i = l;
            il.Emit(code);
            --stackSize;
            return true;
        }

        return false;
    }
    private bool PowerTwoCall(IOpCodeEmitter il, List<string> tokens, Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars, ref int i, ref int stackSize)
    {
        if (stackSize < 1)
            return false;
        int l = i + 1;
        MethodInfo? pow = GetMethod("Pow", 2, _mathMethods ??= typeof(Math).GetMethods(BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public));
        if (pow == null)
        {
            L.LogError("Unable to find Math.Pow method!");
            return false;
        }
        if (Load(ref l, il, tokens, vars, ref stackSize))
        {
            i = l;
            il.Emit(OpCodes.Call, pow);
            --stackSize;
            return true;
        }
        else if (tokens.Count > i + 3 && tokens[i + 2][0] is '(' or '[' or '{')
        {
            LoadMethod(ref l, il, tokens, vars, ref stackSize);
            i = l;
            il.Emit(OpCodes.Call, pow);
            --stackSize;
            return true;
        }

        return false;
    }
    private bool Load(ref int index, IOpCodeEmitter il, List<string> tokens, Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars, ref int stackSize)
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
    private bool LoadConstant(int index, IOpCodeEmitter il, List<string> tokens, ref int stackSize)
    {
        if (index >= tokens.Count)
            return false;
        string tkn = tokens[index];
        if (tkn[^1] is 'f' or 'F' or 'd' or 'D')
            tkn = tkn.Substring(0, tkn.Length - 1);
        if (double.TryParse(tkn.Replace('_', '-'), NumberStyles.Number, CultureInfo.InvariantCulture, out double res))
        {
            LoadConstant(il, res);
            ++stackSize;
            return true;
        }
        return false;
    }
    private bool LoadVariable(int index, IOpCodeEmitter il, List<string> tokens, Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars, ref int stackSize)
    {
        if (index >= tokens.Count)
            return false;
        if (vars.TryGetValue(index, out KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int> mi))
        {
            if (mi.Value < byte.MaxValue)
            {
                il.Emit(OpCodes.Ldloc_S, (byte)mi.Value);
            }
            else
            {
                il.Emit(OpCodes.Ldloc, mi.Value);
            }
            if (mi.Key.Value.ReturnType != typeof(void))
                ++stackSize;
            return true;
        }
        else
        {
            string tkn = tokens[index];
            FieldInfo? fi = typeof(Math).GetField(tkn, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static) ??
                typeof(Math).GetField(tkn, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static);
            if (fi != null)
            {
                if (fi.IsLiteral)
                {
                    if (fi.FieldType == typeof(int))
                        LoadConstant(il, (int)fi.GetValue(null));
                    else if (fi.FieldType == typeof(float))
                        LoadConstant(il, (float)fi.GetValue(null));
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

                    ++stackSize;
                    return true;
                }
                else
                {
                    il.Emit(OpCodes.Ldsfld, fi);
                    if (fi.FieldType != typeof(double))
                    {
                        il.Emit(OpCodes.Conv_R8);
                    }

                    ++stackSize;
                    return true;
                }
            }
        }
        return false;
    }
    private static void LoadConstant(IOpCodeEmitter il, double c)
    {
        il.Emit(OpCodes.Ldc_R8, c);
    }
    private bool LoadMethod(ref int index, IOpCodeEmitter il, List<string> tokens, Dictionary<int, KeyValuePair<KeyValuePair<FieldInfo, MethodInfo>, int>> vars, ref int stackSize)
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
        if (tkn.Equals("ceil", StringComparison.OrdinalIgnoreCase))
            tkn = "Ceiling";
        MethodInfo? f = GetMethod(tkn, pCt, _mathMethods ??= typeof(Math).GetMethods(BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public));

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
                        il.Emit(OpCodes.Conv_I4);
                    }
                    else if (p[i].ParameterType == typeof(float))
                    {
                        il.Emit(OpCodes.Conv_R4);
                    }
                    else if (p[i].ParameterType == typeof(uint))
                    {
                        il.Emit(OpCodes.Conv_U4);
                    }
                    else if (p[i].ParameterType == typeof(short))
                    {
                        il.Emit(OpCodes.Conv_I2);
                    }
                    else if (p[i].ParameterType == typeof(ushort))
                    {
                        il.Emit(OpCodes.Conv_U2);
                    }
                    else if (p[i].ParameterType == typeof(long))
                    {
                        il.Emit(OpCodes.Conv_I8);
                    }
                    else if (p[i].ParameterType == typeof(ulong))
                    {
                        il.Emit(OpCodes.Conv_U8);
                    }
                    else if (p[i].ParameterType == typeof(byte))
                    {
                        il.Emit(OpCodes.Conv_U1);
                    }
                    else if (p[i].ParameterType == typeof(sbyte))
                    {
                        il.Emit(OpCodes.Conv_I1);
                    }
                }
            }
            stackSize -= pCt;
            il.Emit(OpCodes.Call, f);
            ++stackSize;
            if (f.ReturnType != typeof(double))
            {
                il.Emit(OpCodes.Conv_R8);
            }
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
    public IQuestReward? TryEvaluate(IQuestState state)
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

    private delegate IQuestReward EvaluateDelegate(IQuestState state);
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class RewardFieldAttribute : Attribute
{
    public string? Name { get; }
    public bool DisallowVariableUsage { get; }
    public RewardFieldAttribute(string nameOverride)
    {
        Name = nameOverride;
        DisallowVariableUsage = false;
    }
    public RewardFieldAttribute(bool disableVariableUsage)
    {
        Name = null;
        DisallowVariableUsage = disableVariableUsage;
    }
}