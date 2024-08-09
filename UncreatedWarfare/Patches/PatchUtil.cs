using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Logging;

namespace Uncreated.Warfare.Harmony;
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

    [Obsolete("Use Accessor.GetMethod instead.")]
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

    internal static bool PatchMethod(Delegate original, Delegate? prefix = null, Delegate? postfix = null, Delegate? transpiler = null, Delegate? finalizer = null)
    {
        if (original is null || (prefix is null && postfix is null && transpiler is null && finalizer is null)) return false;
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
                return false;
            }
            if (prefixInfo is null && postfixInfo is null && transpilerInfo is null && finalizerInfo is null)
            {
                L.LogError("Error getting method info for patching " + originalInfo.FullDescription());
                return false;
            }
            if (prefix is not null && prefixInfo is null)
                L.LogError("Error getting prefix info for patching " + originalInfo.FullDescription());
            if (postfix is not null && postfixInfo is null)
                L.LogError("Error getting postfix info for patching " + originalInfo.FullDescription());
            if (transpiler is not null && transpilerInfo is null)
                L.LogError("Error getting transpiler info for patching " + originalInfo.FullDescription());
            if (finalizer is not null && finalizerInfo is null)
                L.LogError("Error getting finalizer info for patching " + originalInfo.FullDescription());
            return PatchMethod(originalInfo, prefixInfo, postfixInfo, transpilerInfo, finalizerInfo);
        }
        catch (MemberAccessException ex)
        {
            L.LogError("Error getting method info for patching.");
            L.LogError(ex);
            return false;
        }
    }
    internal static bool PatchMethod(MethodInfo? original, MethodInfo? prefix = null, MethodInfo? postfix = null, MethodInfo? transpiler = null, MethodInfo? finalizer = null)
    {
        bool fail = false;
        PatchMethod(original, ref fail, prefix, postfix, transpiler, finalizer);
        return fail;
    }
    internal static void PatchMethod(MethodInfo? original, ref bool fail, MethodInfo? prefix = null, MethodInfo? postfix = null, MethodInfo? transpiler = null, MethodInfo? finalizer = null)
    {
        if ((prefix is null && postfix is null && transpiler is null && finalizer is null))
        {
            fail = true;
            return;
        }
        if (original is null)
        {
            MethodInfo m = prefix ?? postfix ?? transpiler ?? finalizer!;
            L.LogError("Failed to find original method for patch " + m.FullDescription() + ".");
            fail = true;
            return;
        }

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
            fail = true;
        }
    }
}
