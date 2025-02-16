using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Quests.Parameters;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Quests;

public class QuestRewardExpression : RewardExpression
{
    public Type QuestType { get; }
    public Type StateType { get; }
    public Type RewardType { get; }

    private readonly ConstructorInfo _createCtor;

    /// <inheritdoc />
    public QuestRewardExpression(Type rewardType, Type questType, string expression, ILogger logger)
        : base($"EvaluateReward_{questType.Name}_{rewardType.Name}", typeof(IQuestReward),
            GetReturnType(rewardType, questType, out ConstructorInfo createCtor, out Type? stateType, out IEmittableVariable[] variables),
            rewardType, questType, variables, expression, logger)
    {
        _createCtor = createCtor;

        QuestType = questType;
        StateType = stateType;
        RewardType = rewardType;
    }

    private static Type GetReturnType(Type rewardType, Type questType, out ConstructorInfo createCtor, out Type stateType, out IEmittableVariable[] variables)
    {
        if (!typeof(IQuestReward).IsAssignableFrom(rewardType) || rewardType.IsInterface || rewardType.IsAbstract)
        {
            throw new ArgumentException("Reward type must be a non-abstract class or struct that implements IQuestReward.");
        }

        if (!questType.IsSubclassOf(typeof(QuestTemplate)) || rewardType.IsAbstract)
        {
            throw new ArgumentException("Quest type must be a non-abstract class that inherits QuestTemplate.");
        }

        createCtor = null!;
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
            createCtor = ctor;
            break;
        }

        if (returnType == null)
        {
            throw new ArgumentException("No valid construtors found with a single parameter of type string or a primitive number.", nameof(rewardType));
        }

        Type? tempStateType = null;
        questType.ForEachBaseType((type, _) =>
        {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(QuestTemplate<,,>))
                return true;

            tempStateType = type.GetGenericArguments()[2];
            return false;
        });

        if (tempStateType == null)
        {
            Type[] ts = questType.GetNestedTypes();
            for (int i = 0; i < ts.Length; ++i)
            {
                Type t = ts[i];
                if (!typeof(IQuestState).IsAssignableFrom(t))
                    continue;

                tempStateType = t;
                break;
            }

            if (tempStateType == null)
            {
                throw new ArgumentException($"Unable to identify the state type of quest type {Accessor.ExceptionFormatter.Format(questType)}.");
            }
        }

        stateType = tempStateType;

        FieldInfo[] fields = stateType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        PropertyInfo[] props = stateType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        List<IEmittableVariable> vars = new List<IEmittableVariable>();
        foreach (MemberInfo member in fields.Concat<MemberInfo>(props).OrderByDescending(Accessor.GetPriority))
        {
            if (member.TryGetAttributeSafe(out RewardVariableAttribute reward) && reward.DisallowVariableUsage)
                continue;

            vars.Add(new QuestStateVariable(Variables.AsVariable(member), reward));
        }

        variables = vars.ToArray();
        return typeof(IQuestReward);
    }

    /// <inheritdoc />
    protected override void TransformResult(IOpCodeEmitter emit, ref int stackSize)
    {
        Type rtnType = _createCtor.GetParameters()[0].ParameterType;

        if (rtnType != typeof(double))
        {
            if (rtnType == typeof(int))
                emit.ConvertToInt32Checked();
            else if (rtnType == typeof(uint))
                emit.ConvertToUInt32Checked();
            else if (rtnType == typeof(short))
                emit.ConvertToInt16Checked();
            else if (rtnType == typeof(ushort))
                emit.ConvertToUInt16Checked();
            else if (rtnType == typeof(sbyte))
                emit.ConvertToInt8Checked();
            else if (rtnType == typeof(byte))
                emit.ConvertToUInt8Checked();
            else if (rtnType == typeof(ulong))
                emit.ConvertToUInt64Checked();
            else if (rtnType == typeof(long))
                emit.ConvertToInt64Checked();
            else if (rtnType == typeof(nuint))
                emit.ConvertToNativeUIntChecked();
            else if (rtnType == typeof(nint))
                emit.ConvertToNativeIntChecked();
            else if (rtnType == typeof(float))
                emit.ConvertToSingle();
            else if (rtnType == typeof(string))
            {
                emit.PopToLocal<double>(out LocalBuilder lcl)
                    .LoadLocalAddress(lcl)
                    .Invoke(typeof(double).GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)!);
            }
        }

        emit.CreateObject(_createCtor);

        if (_createCtor.DeclaringType!.IsValueType)
            emit.Box(_createCtor.DeclaringType);
    }

    public IQuestReward? GetReward(IQuestState state)
    {
        object? result = TryEvaluate(state);
        return (IQuestReward?)result;
    }

    private class QuestStateVariable : IEmittableVariable
    {
        private readonly IVariable _variable;

        public string[] Names { get; }

        public QuestStateVariable(IVariable variable, RewardVariableAttribute? attribute)
        {
            _variable = variable;
            Names = attribute?.Name != null ? [ attribute.Name, variable.Member.Name ] : [ variable.Member.Name ];
        }

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
                il.Invoke(((PropertyInfo)_variable.Member).GetMethod);
            }

            MethodInfo? getSingleValueMethod = _variable.MemberType.GetMethod(nameof(QuestParameterValue<object>.GetSingleValue),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (getSingleValueMethod != null)
            {
                if (!getSingleValueMethod.ReturnType.IsPrimitive || getSingleValueMethod.ReturnType == typeof(char) || getSingleValueMethod.ReturnType == typeof(bool))
                {
                    il.PopFromStack();
                    logger.LogError("Invalid variable type: {0}, {1}", _variable.Member.Name, _variable.MemberType);
                    throw new InvalidOperationException($"Invalid variable type: {_variable.Member.Name}, {Accessor.ExceptionFormatter.Format(_variable.MemberType)}.");
                }


                il.Invoke(getSingleValueMethod);
                if (getSingleValueMethod.ReturnType != typeof(double))
                {
                    il.ConvertToDouble();
                }
            }
            else if (_variable.MemberType != typeof(double))
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