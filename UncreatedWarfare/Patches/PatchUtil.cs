using HarmonyLib;
using SDG.Unturned;
using System;
using System.Reflection;

namespace Uncreated.Warfare.Harmony;
internal static class PatchUtil
{
    internal delegate void ReceiveMethodDelegate(in ClientInvocationContext ctx);
    internal delegate void ReceiveMethodDelegate<in T>(in ClientInvocationContext ctx, T arg1);
    internal delegate void ReceiveMethodDelegate<in T1, in T2>(in ClientInvocationContext ctx, T1 arg1, T2 arg2);
    internal delegate void ReceiveMethodDelegate<in T1, in T2, in T3>(in ClientInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3);
    internal delegate void ReceiveMethodDelegate<in T1, in T2, in T3, in T4>(in ClientInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    internal delegate void ReceiveMethodDelegate<in T1, in T2, in T3, in T4, in T5>(in ClientInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    internal delegate void ReceiveMethodDelegate<in T1, in T2, in T3, in T4, in T5, in T6>(in ClientInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    internal delegate void ReceiveMethodDelegate<in T1, in T2, in T3, in T4, in T5, in T6, in T7>(in ClientInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    internal delegate void ReceiveMethodDelegate<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8>(in ClientInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    internal delegate void ReceiveMethodDelegate<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9>(in ClientInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    internal delegate void ReceiveMethodDelegate<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10>(in ClientInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
    internal static MethodInfo GetMethodInfo(Delegate method)
    {
        try
        {
            return method.GetMethodInfo();
        }
        catch (MemberAccessException)
        {
            L.LogWarning("Was unable to get a method info from a delegate.");
            return null!;
        }
    }
    internal static void PatchMethod(Delegate original, Delegate? prefix = null, Delegate? postfix = null, Delegate? transpiler = null, Delegate? finalizer = null)
    {
        if (original is null || (prefix is null && postfix is null && transpiler is null && finalizer is null)) return;
        try
        {
            MethodInfo? originalInfo = original.Method;
            MethodInfo? prefixInfo = prefix?.Method;
            MethodInfo? postfixInfo = prefix?.Method;
            MethodInfo? transpilerInfo = prefix?.Method;
            MethodInfo? finalizerInfo = prefix?.Method;
            if (originalInfo is null)
            {
                L.LogError("Error getting method info for patching.");
                return;
            }
            if (prefixInfo is null && postfixInfo is null && transpilerInfo is null && finalizerInfo is null)
            {
                L.LogError("Error getting method info for patching " + originalInfo.FullDescription());
                return;
            }
            if (prefix is not null && prefixInfo is null)
                L.LogError("Error getting prefix info for patching " + originalInfo.FullDescription());
            if (postfix is not null && postfixInfo is null)
                L.LogError("Error getting postfix info for patching " + originalInfo.FullDescription());
            if (transpiler is not null && transpilerInfo is null)
                L.LogError("Error getting transpiler info for patching " + originalInfo.FullDescription());
            if (finalizer is not null && finalizerInfo is null)
                L.LogError("Error getting finalizer info for patching " + originalInfo.FullDescription());
            PatchMethod(originalInfo, prefixInfo, postfixInfo, transpilerInfo, finalizerInfo);
        }
        catch (MemberAccessException ex)
        {
            L.LogError("Error getting method info for patching.");
            L.LogError(ex);
        }
    }
    internal static void PatchMethod(MethodInfo original, MethodInfo? prefix = null, MethodInfo? postfix = null, MethodInfo? transpiler = null, MethodInfo? finalizer = null)
    {
        if (original is null || (prefix is null && postfix is null && transpiler is null && finalizer is null)) return;

        HarmonyMethod? prfx2 = prefix is null ? null : new HarmonyMethod(prefix);
        HarmonyMethod? pofx2 = postfix is null ? null : new HarmonyMethod(postfix);
        HarmonyMethod? tplr2 = transpiler is null ? null : new HarmonyMethod(transpiler);
        HarmonyMethod? fnlr2 = finalizer is null ? null : new HarmonyMethod(finalizer);
        try
        {
            Patches.Patcher.Patch(original, prefix: prfx2, postfix: pofx2, transpiler: tplr2, finalizer: fnlr2);
        }
        catch (Exception ex)
        {
            L.LogError("Error patching " + original.FullDescription());
            L.LogError(ex);
        }
    }
}
