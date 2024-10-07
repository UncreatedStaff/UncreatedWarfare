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

    internal static bool PatchMethod(MethodInfo? original, ILogger logger, MethodInfo? prefix = null, MethodInfo? postfix = null, MethodInfo? transpiler = null, MethodInfo? finalizer = null)
    {
        bool fail = false;
        PatchMethod(original, ref fail, logger, prefix, postfix, transpiler, finalizer);
        return fail;
    }
    internal static void PatchMethod(MethodInfo? original, ref bool fail, ILogger logger, MethodInfo? prefix = null, MethodInfo? postfix = null, MethodInfo? transpiler = null, MethodInfo? finalizer = null)
    {
        if ((prefix is null && postfix is null && transpiler is null && finalizer is null))
        {
            fail = true;
            return;
        }
        if (original is null)
        {
            MethodInfo m = prefix ?? postfix ?? transpiler ?? finalizer!;
            logger.LogError("Failed to find original method for patch {0}.", m);
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
            logger.LogError(ex, "Error patching {0}.", original);
            fail = true;
        }
    }
}
