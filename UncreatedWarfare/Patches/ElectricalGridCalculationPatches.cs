using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class ElectricalGridCalculationPatches : IHarmonyPatch
{
    public static bool Failed = true;
    private static MethodInfo? _target1;
    private static MethodInfo? _target2;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        Failed = false;

        _target1 = typeof(InteractablePower).GetMethod("CalculateIsConnectedToPower", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        _target2 = typeof(ObjectManager).GetMethod(nameof(ObjectManager.ReceiveToggleObjectBinaryStateRequest), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        if (_target1 == null)
        {
            Failed = true;
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition("CalculateIsConnectedToPower")
                    .DeclaredIn<InteractablePower>(isStatic: false)
                    .WithNoParameters()
                    .Returning<bool>()
            );
        }
        
        if (_target2 == null)
        {
            Failed = true;
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition(nameof(ObjectManager.ReceiveToggleObjectBinaryStateRequest))
                    .DeclaredIn<ObjectManager>(isStatic: true)
                    .WithParameter<ServerInvocationContext>("context", ByRefTypeMode.In)
                    .WithParameter<byte>("x")
                    .WithParameter<byte>("y")
                    .WithParameter<ushort>("index")
                    .WithParameter<bool>("isUsed")
                    .ReturningVoid()
            );
        }

        if (Failed)
            return;
        
        patcher.Patch(_target1, prefix: Accessor.GetMethod(CalculateIsConnectedToPowerPrefix));
        logger.LogDebug("Patched {0} for calculating grid object power.", _target1);

        try
        {
            patcher.Patch(_target2, prefix: Accessor.GetMethod(ReceiveToggleObjectBinaryStateRequestPrefix));
            logger.LogDebug("Patched {0} for receiving grid object enable requests.", _target2);
        }
        catch
        {
            patcher.Unpatch(_target1, Accessor.GetMethod(CalculateIsConnectedToPowerPrefix));
            throw;
        }
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target1 == null)
            return;

        patcher.Unpatch(_target1, Accessor.GetMethod(CalculateIsConnectedToPowerPrefix));
        logger.LogDebug("Unpatched {0} for calculating grid object power.", _target1);

        patcher.Unpatch(_target2, Accessor.GetMethod(ReceiveToggleObjectBinaryStateRequestPrefix));
        logger.LogDebug("Unpatched {0} for receiving grid object enable requests.", _target2);
        _target1 = null;
    }

    private static bool ReceiveToggleObjectBinaryStateRequestPrefix(in ServerInvocationContext context, byte x, byte y, ushort index, bool isUsed)
    {
        if (!Regions.checkSafe(x, y) || LevelObjects.objects == null)
            return false;

        Player player = context.GetPlayer();
        List<LevelObject> levelObjects = LevelObjects.objects[x, y];
        if (player == null || player.life.isDead || index >= levelObjects.Count)
            return false;

        LevelObject obj = levelObjects[index];
        
        if (!WarfareModule.Singleton.IsLayoutActive() || obj.asset.interactabilityPower == EObjectInteractabilityPower.NONE)
        {
            return true;
        }

        ILifetimeScope serviceProvider = WarfareModule.Singleton.GetActiveLayout().ServiceProvider;

        ElectricalGridService? service = serviceProvider.ResolveOptional<ElectricalGridService>();

        serviceProvider.Resolve<ILogger<ElectricalGridService>>()
            .LogConditional("Received request from {0} for obj {1} @ {2} to state: {3}.", player, obj.asset.FriendlyName, obj.transform.position, isUsed);

        if (service == null || service.IsPowered(obj))
        {
            return true;
        }

        WarfarePlayer wPlayer = serviceProvider.Resolve<IPlayerService>().GetOnlinePlayer(player);
        PlayersTranslations translations = serviceProvider.Resolve<TranslationInjection<PlayersTranslations>>().Value;
        serviceProvider.Resolve<ChatService>().Send(wPlayer, translations.ElectricalGridNotConnected);
        return false;
    }

    private static bool CalculateIsConnectedToPowerPrefix(InteractablePower __instance, ref bool __result)
    {
        ElectricalGridService? handler = WarfareModule.Singleton.ServiceProvider.ResolveOptional<ElectricalGridService>();

        if (handler is not { Enabled: true })
        {
            return true;
        }

        __result = handler.IsPowered(__instance);
        return false;
    }
}