using HarmonyLib;
using System;
using System.Reflection;
using Uncreated.Warfare.Logging;

namespace Uncreated.Warfare.Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
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

    private static bool GetIsLoading() => false;// todo Data.Gamemode is null || Data.Gamemode.IsLoading;

    [UsedImplicitly]
    private static bool DisallowOnLoadingPrefix()
    {
        return !GetIsLoading();
    }
}
