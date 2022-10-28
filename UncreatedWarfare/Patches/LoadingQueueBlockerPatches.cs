using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare;
internal static class LoadingQueueBlockerPatches
{
    public static void Patch()
    {
        Type thisType = typeof(LoadingQueueBlockerPatches);
        Type provider = typeof(Provider);
        MethodInfo? kickInactivePendingMethod = provider.GetMethod("KickClientsBlockingUpQueue", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo? verifyNextPlayerInQueueMethod = provider.GetMethod("verifyNextPlayerInQueue", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo? disallowWhenLoadingPrefix = thisType.GetMethod(nameof(DisallowOnLoadingPrefix), BindingFlags.NonPublic | BindingFlags.Static);
        
        if (kickInactivePendingMethod == null || disallowWhenLoadingPrefix == null)
            L.LogWarning("Unable to prefix Provider.KickClientsBlockingUpQueue, players could be kicked for holding up queue.");
        else
            Patches.Patcher.Patch(kickInactivePendingMethod, prefix: new HarmonyMethod(disallowWhenLoadingPrefix));
        
        if (verifyNextPlayerInQueueMethod == null || disallowWhenLoadingPrefix == null)
            L.LogWarning("Unable to prefix Provider.verifyNextPlayerInQueue, players can join while loading.");
        else
            Patches.Patcher.Patch(verifyNextPlayerInQueueMethod, prefix: new HarmonyMethod(disallowWhenLoadingPrefix));
    }
    public static void Unpatch()
    {
        Type thisType = typeof(LoadingQueueBlockerPatches);
        Type provider = typeof(Provider);
        MethodInfo? kickInactivePendingMethod = provider.GetMethod("KickClientsBlockingUpQueue", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo? verifyNextPlayerInQueueMethod = provider.GetMethod("verifyNextPlayerInQueue", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo? disallowWhenLoadingPrefix = thisType.GetMethod(nameof(DisallowOnLoadingPrefix), BindingFlags.NonPublic | BindingFlags.Static);
        
        if (kickInactivePendingMethod == null || disallowWhenLoadingPrefix == null)
            L.LogWarning("Unable to transpile Provider.KickClientsBlockingUpQueue, players could be kicked for holding up queue.");
        else
            Patches.Patcher.Unpatch(kickInactivePendingMethod, disallowWhenLoadingPrefix);
        
        if (verifyNextPlayerInQueueMethod == null || disallowWhenLoadingPrefix == null)
            L.LogWarning("Unable to transpile Provider.KickClientsBlockingUpQueue, players could be kicked for holding up queue.");
        else
            Patches.Patcher.Unpatch(verifyNextPlayerInQueueMethod, disallowWhenLoadingPrefix);
    }
    /*
    private static MethodInfo IsLoadingMethod =
        typeof(LoadingQueueBlockerPatches).GetMethod(nameof(GetIsLoading),
            BindingFlags.NonPublic | BindingFlags.Static)!;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]*/
    private static bool GetIsLoading() => Data.Gamemode is null || Data.Gamemode.IsLoading;

    [UsedImplicitly]
    private static bool DisallowOnLoadingPrefix()
    {
        return !GetIsLoading();
    }
}
