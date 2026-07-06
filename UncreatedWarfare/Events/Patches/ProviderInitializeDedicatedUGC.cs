using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Server;
using Uncreated.Warfare.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class ProviderInitializeDedicatedUGC : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(Provider).GetMethod("initializeDedicatedUGC",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, CallingConventions.Any, Type.EmptyTypes, null);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for workshop initializing event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("initializeDedicatedUGC")
                .DeclaredIn<Provider>(isStatic: true)
                .WithNoParameters()
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for workshop initializing event.", _target);
        _target = null;
    }

    // SDG.Unturned.Provider.initializeDedicatedUGC
    /// <summary>
    /// Allows us to pause workshop downloads until after an event processes.
    /// </summary>
    private static bool Prefix(SteamPending __instance)
    {
        WorkshopDownloadConfig config = WorkshopDownloadConfig.getOrLoad();

        ServerWorkshopLoading args = new ServerWorkshopLoading
        {
            Items = new HashSet<PublishedFileId_t>(config.File_IDs.Select(x => new PublishedFileId_t(x))),
            IgnoredChildren = new HashSet<PublishedFileId_t>(config.File_IDs.Select(x => new PublishedFileId_t(x)))
        };

        EventContinuations.DispatchNoCancel(
            args,
            WarfareModule.EventDispatcher,
            WarfareModule.Singleton.UnloadToken,
            out bool shouldAllow,
            static args =>
            {
                Apply(args);

                // Provider.initializeDedicatedUGC();
                _target!.Invoke(null, Array.Empty<object>());
            }
        );

        if (shouldAllow)
            Apply(args);

        return shouldAllow;

        static void Apply(ServerWorkshopLoading args)
        {
            WorkshopDownloadConfig config = WorkshopDownloadConfig.get();

            config.File_IDs = [ ..args.Items.Select(x => x.m_PublishedFileId) ];
            config.Ignore_Children_File_IDs = [ ..args.IgnoredChildren.Select(x => x.m_PublishedFileId) ];
        }
    }
}