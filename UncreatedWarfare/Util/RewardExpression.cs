using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Uncreated.Warfare.Util;

public class RewardExpression
{
    private static readonly char[] TokenSplits = [ '*', '/', '+', '-', '%', '(', ')', '[', ']', '{', '}', ',', '^' ];
    private readonly string _methodName;
    private readonly Type _methodReturnType;
    private readonly Type _dataReturnType;
    private readonly Type? _inputType;
    private readonly Type _ownerType;
    private readonly IReadOnlyList<IEmittableVariable> _variables;
    private readonly string _expression;
    private string _expression2;
    private EvaluateDelegate? _method;
    private static MethodInfo[]? _mathMethods;
    private readonly ILogger _logger;
    private List<string>? _tokens;
    private const bool DebugLogging = false;
    public RewardExpression(string methodName, Type methodReturnType, Type dataReturnType, Type? inputType, Type ownerType, IReadOnlyList<IEmittableVariable> variables, string expression, ILogger logger)
    {
        _methodName = methodName;
        _methodReturnType = methodReturnType;
        _dataReturnType = dataReturnType;
        _inputType = inputType;
        _ownerType = ownerType;
        _variables = variables;
        _expression = expression;
        _logger = logger;

        if (dataReturnType == typeof(string))
        {
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
                if (spanSt < 0 || spanEnd > tokens.Count - 1 || spanSt > 0 && tokens[spanSt - 1][0] is '(' or '[' or '{' && spanEnd < tokens.Count - 1 && tokens[spanEnd + 1][0] is ')' or ']' or '}')
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
        _tokens = tokens;
    }

    protected virtual void TransformResult(IOpCodeEmitter emit, ref int stackSize)
    {
    }

    private bool Evaluate(ref int stPos, IOpCodeEmitter il, List<string> tokens, Dictionary<int, VariableInfo> vars, ref int stackSize)
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
                    _logger.LogWarning("Unknown token: '{0}'.", tokens[i]);
                    return false;
                }
            }
            emitted = true;
        }
        return emitted;
    }
    private bool TwoCodeCall(OpCode code, IOpCodeEmitter il, List<string> tokens, Dictionary<int, VariableInfo> vars, ref int i, ref int stackSize)
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
    private bool PowerTwoCall(IOpCodeEmitter il, List<string> tokens, Dictionary<int, VariableInfo> vars, ref int i, ref int stackSize)
    {
        if (stackSize < 1)
            return false;
        int l = i + 1;
        MethodInfo? pow = GetMethod("Pow", 2, _mathMethods ??= typeof(Math).GetMethods(BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public));
        if (pow == null)
        {
            _logger.LogError("Unable to find Math.Pow method!");
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
    private bool Load(ref int index, IOpCodeEmitter il, List<string> tokens, Dictionary<int, VariableInfo> vars, ref int stackSize)
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
    private bool LoadVariable(int index, IOpCodeEmitter il, List<string> tokens, Dictionary<int, VariableInfo> vars, ref int stackSize)
    {
        if (index >= tokens.Count)
            return false;
        if (vars.TryGetValue(index, out VariableInfo mi))
        {
            il.LoadLocalValue(mi.Local);
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
    private bool LoadMethod(ref int index, IOpCodeEmitter il, List<string> tokens, Dictionary<int, VariableInfo> vars, ref int stackSize)
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

        index = ind2 != -1 ? ind2 + 2 : tokens.Count;
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
    public object? TryEvaluate(object arg)
    {
        _method ??= CreateMethod();

        try
        {
            return _method(arg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute expression: {0}.", _expression2 ?? _expression);
            return null;
        }
    }

    private struct VariableInfo
    {
        public IEmittableVariable Variable;
        public LocalReference Local;
    }

    private EvaluateDelegate CreateMethod()
    {
        List<string>? tokens = _tokens;

        Type[] parameters = [ typeof(object) ];
        if (_dataReturnType == typeof(string))
        {
            DynamicMethod strMethod = new DynamicMethod(_methodName, typeof(object), parameters, _ownerType, true);

            if (parameters.Length > 0)
                strMethod.DefineParameter(0, ParameterAttributes.None, "arg0");

            IOpCodeEmitter strIl = strMethod.AsEmitter(debuggable: DebugLogging);
            strIl.Emit(OpCodes.Ldstr, _expression);
            int stackSize2 = 1;

            TransformResult(strIl, ref stackSize2);

            if (_methodReturnType.IsValueType)
                strIl.Box(_methodReturnType);

            strIl.Emit(OpCodes.Ret);

            _method = (EvaluateDelegate)strMethod.CreateDelegate(typeof(EvaluateDelegate));
            _expression2 = _expression;
            _tokens = null;
            return _method;
        }

        Dictionary<int, VariableInfo> variables = new Dictionary<int, VariableInfo>();

        DynamicMethod method = new DynamicMethod(_methodName, typeof(object), parameters, _ownerType, true);

        if (parameters.Length > 0)
            method.DefineParameter(0, ParameterAttributes.None, "arg0");

        IOpCodeEmitter il = method.AsEmitter(debuggable: DebugLogging);
        for (int i = 0; i < tokens!.Count; ++i)
        {
            foreach (IEmittableVariable variable in _variables)
            {
                if (!variable.Names.Any(x => x.Equals(tokens[i], StringComparison.OrdinalIgnoreCase)))
                    continue;

                foreach (VariableInfo existing in variables.Values)
                {
                    if (existing.Variable != variable)
                        continue;

                    variables.Add(i, existing);
                    goto skip;
                }

                il.AddLocal<double>(out LocalBuilder local);
                variable.Preload(local, il, _logger);
                if (DebugLogging)
                {
                    il.Comment($"Variable #{local.LocalIndex} {variable.Names[0]}");
                }

                variables.Add(i, new VariableInfo
                {
                    Local = local,
                    Variable = variable
                });

            skip:;
            }
        }
        int stackSize = 0;
        int index = 0;

        if (!Evaluate(ref index, il, tokens, variables, ref stackSize))
            throw new InvalidOperationException("Invalid expression.");

        if (_dataReturnType == typeof(int))
        {
            il.Emit(OpCodes.Conv_I4);
        }
        else if (_dataReturnType == typeof(float))
        {
            il.Emit(OpCodes.Conv_R4);
        }
        else if (_dataReturnType == typeof(uint))
        {
            il.Emit(OpCodes.Conv_U4);
        }
        else if (_dataReturnType == typeof(short))
        {
            il.Emit(OpCodes.Conv_I2);
        }
        else if (_dataReturnType == typeof(ushort))
        {
            il.Emit(OpCodes.Conv_U2);
        }
        else if (_dataReturnType == typeof(long))
        {
            il.Emit(OpCodes.Conv_I8);
        }
        else if (_dataReturnType == typeof(ulong))
        {
            il.Emit(OpCodes.Conv_U8);
        }
        else if (_dataReturnType == typeof(byte))
        {
            il.Emit(OpCodes.Conv_U1);
        }
        else if (_dataReturnType == typeof(sbyte))
        {
            il.Emit(OpCodes.Conv_I1);
        }

        if (stackSize < 1)
            throw new InvalidOperationException("Invalid expression.");

        TransformResult(il, ref stackSize);
        if (_methodReturnType.IsValueType)
        {
            il.Box(_methodReturnType);
        }

        il.Emit(OpCodes.Ret);

        _method = (EvaluateDelegate)method.CreateDelegate(typeof(EvaluateDelegate));
        _tokens = null;
        return _method;
    }

    private delegate object EvaluateDelegate(object state);

    public interface IEmittableVariable
    {
        string[] Names { get; }

        void Preload(LocalReference local, IOpCodeEmitter emitter, ILogger logger);
    }

    public class EmittableVariable : IEmittableVariable
    {
        private readonly IVariable _variable;

        public string[] Names { get; }

        public EmittableVariable(IVariable variable)
        {
            _variable = variable;
            Names = [ _variable.Member.Name ];
        }

        /// <inheritdoc />
        public void Preload(LocalReference local, IOpCodeEmitter il, ILogger logger)
        {
            if (!_variable.IsStatic)
            {
                il.LoadArgument(0);
                if (_variable.DeclaringType is { IsValueType: true })
                {
                    il.LoadUnboxedAddress(_variable.DeclaringType);
                }
                else
                {
                    il.CastReference(_variable.DeclaringType!);
                }
            }

            if (_variable.IsField)
            {
                il.LoadInstanceFieldValue((FieldInfo)_variable.Member);
            }
            else
            {
                il.Invoke(((PropertyInfo)_variable).GetMethod);
            }

            if (_variable.MemberType != typeof(double))
            {
                if (_variable.MemberType == typeof(object))
                {
                    il.LoadUnboxedValue<double>();
                }
                else if (Operators.FindCast(_variable.MemberType, typeof(double), preferCheckedOperator: true) is { } castMethod)
                {
                    il.Invoke(castMethod);
                }
                else
                {
                    il.ConvertToDouble();
                }
            }

            il.SetLocalValue(local);
        }
    }
}


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class RewardVariableAttribute : Attribute
{
    public string? Name { get; }
    public bool DisallowVariableUsage { get; }
    public RewardVariableAttribute(string nameOverride)
    {
        Name = nameOverride;
        DisallowVariableUsage = false;
    }
    public RewardVariableAttribute(bool disableVariableUsage)
    {
        Name = null;
        DisallowVariableUsage = disableVariableUsage;
    }
}