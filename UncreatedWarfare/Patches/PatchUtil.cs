using DanielWillett.ReflectionTools.Emit;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Uncreated.Warfare.Patches;
internal static class PatchUtil
{
    internal static int FindLocalOfType<T>(this MethodBase method)
    {
        MethodBody? body = method.GetMethodBody();
        if (body == null)
            return -1;
        Type t = typeof(T);
        LocalVariableInfo? v = body.LocalVariables.FirstOrDefault(x => x.LocalType == t);
        return v == null ? -1 : v.LocalIndex;
    }
    
    /// <summary>
    /// Get the local builder or index of the instruction.
    /// </summary>
    [Pure]
    public static LocalReference GetLocal(CodeInstruction code, bool set)
    {
        if (code.opcode.OperandType == OperandType.ShortInlineVar &&
            (set && code.opcode == OpCodes.Stloc_S ||
             !set && code.opcode == OpCodes.Ldloc_S || !set && code.opcode == OpCodes.Ldloca_S))
        {
            return new LocalReference((LocalBuilder)code.operand);
        }
        if (code.opcode.OperandType == OperandType.InlineVar &&
            (set && code.opcode == OpCodes.Stloc ||
             !set && code.opcode == OpCodes.Ldloc || !set && code.opcode == OpCodes.Ldloca))
        {
            return new LocalReference((LocalBuilder)code.operand);
        }
        if (set)
        {
            if (code.opcode == OpCodes.Stloc_0)
            {
                return new LocalReference(0);
            }
            if (code.opcode == OpCodes.Stloc_1)
            {
                return new LocalReference(1);
            }
            if (code.opcode == OpCodes.Stloc_2)
            {
                return new LocalReference(2);
            }
            if (code.opcode == OpCodes.Stloc_3)
            {
                return new LocalReference(3);
            }
        }
        else
        {
            if (code.opcode == OpCodes.Ldloc_0)
            {
                return new LocalReference(0);
            }
            if (code.opcode == OpCodes.Ldloc_1)
            {
                return new LocalReference(1);
            }
            if (code.opcode == OpCodes.Ldloc_2)
            {
                return new LocalReference(2);
            }
            if (code.opcode == OpCodes.Ldloc_3)
            {
                return new LocalReference(3);
            }
        }

        return default;
    }
}