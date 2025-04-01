using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// Transpiles a method to add logging for each instruction.
    /// </summary>
    public static bool AddFunctionStepthrough(Harmony patcher, MethodBase method)
    {
        patcher.Patch(method, transpiler: new HarmonyMethod(AddFunctionStepthroughTranspiler));
        return true;
    }

    private static IEnumerable<CodeInstruction> AddFunctionStepthroughTranspiler([InstantHandle] IEnumerable<CodeInstruction> instructions, MethodBase method)
    {
        List<CodeInstruction> ins = [.. instructions];
        AddFunctionStepthrough(ins, method);
        return ins;
    }

    private static void LogInfo(string str, ConsoleColor color)
    {
        ConsoleColor old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(str);
        Console.ForegroundColor = old;
    }

    private static void AddFunctionStepthrough(List<CodeInstruction> ins, MethodBase method)
    {
        ins.Insert(0, new CodeInstruction(OpCodes.Ldstr, "Stepping through Method: " + Accessor.Formatter.Format(method) + ":"));
        ins.Insert(1, new CodeInstruction(OpCodes.Ldc_I4, (int)ConsoleColor.Green));
        ins.Insert(2, new CodeInstruction(OpCodes.Call, Accessor.GetMethod(LogInfo)));
        ins[0].WithStartBlocksFrom(ins[3]);
        for (int i = 3; i < ins.Count; i++)
        {
            CodeInstruction instr = ins[i];
            CodeInstruction? start = null;
            foreach (ExceptionBlock block in instr.blocks)
            {
                CodeInstruction blockInst = new CodeInstruction(OpCodes.Ldstr, "  " + block.blockType);
                start ??= blockInst;
                ins.Insert(i, blockInst);
                ins.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_I4, (int)ConsoleColor.Green));
                ins.Insert(i + 2, new CodeInstruction(OpCodes.Call, Accessor.GetMethod(LogInfo)));
                i += 3;
            }

            foreach (Label label in instr.labels)
            {
                CodeInstruction lblInst = new CodeInstruction(OpCodes.Ldstr, "  " + Unsafe.As<Label, int>(ref Unsafe.AsRef(in label)) + ":");
                start ??= lblInst;
                ins.Insert(i, lblInst);
                ins.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_I4, (int)ConsoleColor.Green));
                ins.Insert(i + 2, new CodeInstruction(OpCodes.Call, Accessor.GetMethod(LogInfo)));
                i += 3;
            }

            CodeInstruction mainInst = new CodeInstruction(OpCodes.Ldstr, "  " + PatchUtility.CodeInstructionFormatter.FormatCodeInstruction(instr, OpCodeFormattingContext.List));
            start ??= mainInst;
            ins.Insert(i, mainInst);
            ins.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_I4, (int)ConsoleColor.Green));
            ins.Insert(i + 2, new CodeInstruction(OpCodes.Call, Accessor.GetMethod(LogInfo)));
            i += 3;

            start.WithStartBlocksFrom(instr);
        }
    }
}