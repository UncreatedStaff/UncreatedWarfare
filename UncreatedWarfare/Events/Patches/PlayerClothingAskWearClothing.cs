using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class PlayerClothingAskWearClothing : IHarmonyPatch
{
    private static MethodInfo?[]? _targets;
    private static MethodInfo[]? _patches;

    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _targets =
        [
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.askWearShirt),    BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ typeof(ItemShirtAsset),    typeof(byte), typeof(byte[]), typeof(bool) ], null),
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.askWearPants),    BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ typeof(ItemPantsAsset),    typeof(byte), typeof(byte[]), typeof(bool) ], null),
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.askWearVest),     BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ typeof(ItemVestAsset),     typeof(byte), typeof(byte[]), typeof(bool) ], null),
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.askWearHat),      BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ typeof(ItemHatAsset),      typeof(byte), typeof(byte[]), typeof(bool) ], null),
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.askWearMask),     BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ typeof(ItemMaskAsset),     typeof(byte), typeof(byte[]), typeof(bool) ], null),
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.askWearBackpack), BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ typeof(ItemBackpackAsset), typeof(byte), typeof(byte[]), typeof(bool) ], null),
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.askWearGlasses),  BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ typeof(ItemGlassesAsset),  typeof(byte), typeof(byte[]), typeof(bool) ], null)
        ];

        _patches =
        [
            Accessor.GetMethod(OnSwappedShirt)!,
            Accessor.GetMethod(OnSwappedPants)!,
            Accessor.GetMethod(OnSwappedVest)!,
            Accessor.GetMethod(OnSwappedHat)!,
            Accessor.GetMethod(OnSwappedMask)!,
            Accessor.GetMethod(OnSwappedBackpack)!,
            Accessor.GetMethod(OnSwappedGlasses)!
        ];

        for (int i = 0; i < _targets.Length; ++i)
        {
            MethodInfo? target = _targets[i];
            if (target != null)
            {
                patcher.Patch(target, postfix: _patches[i]);
                logger.LogDebug("Patched {0} for clothing swapped event.", target);
                continue;
            }

            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition("askWear" + (ClothingType)i)
                    .DeclaredIn<PlayerClothing>(isStatic: false)
                    .WithParameter<byte>("page")
                    .WithParameter<byte>("x")
                    .WithParameter<byte>("y")
                    .ReturningVoid()
            );
        }
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_targets == null)
            return;

        for (int i = 0; i < _targets.Length; ++i)
        {
            MethodInfo? target = _targets[i];
            if (target == null)
                continue;

            patcher.Unpatch(target, _patches![i]);
            logger.LogDebug("Unpatched {0} for clothing swapped event.", target);
        }

        _targets = null;
    }

    private static void OnSwappedShirt(PlayerClothing __instance, ItemShirtAsset asset, byte quality, byte[] state, bool playEffect) =>
        InvokeSwappedClothing(ClothingType.Shirt,    WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>().GetOnlinePlayer(__instance.player), asset, quality, state, playEffect);
    private static void OnSwappedPants(PlayerClothing __instance, ItemPantsAsset asset, byte quality, byte[] state, bool playEffect) =>
        InvokeSwappedClothing(ClothingType.Pants,    WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>().GetOnlinePlayer(__instance.player), asset, quality, state, playEffect);
    private static void OnSwappedVest(PlayerClothing __instance, ItemVestAsset asset, byte quality, byte[] state, bool playEffect) =>
        InvokeSwappedClothing(ClothingType.Vest,     WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>().GetOnlinePlayer(__instance.player), asset, quality, state, playEffect);
    private static void OnSwappedHat(PlayerClothing __instance, ItemHatAsset asset, byte quality, byte[] state, bool playEffect) =>
        InvokeSwappedClothing(ClothingType.Hat,      WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>().GetOnlinePlayer(__instance.player), asset, quality, state, playEffect);
    private static void OnSwappedMask(PlayerClothing __instance, ItemMaskAsset asset, byte quality, byte[] state, bool playEffect) =>
        InvokeSwappedClothing(ClothingType.Mask,     WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>().GetOnlinePlayer(__instance.player), asset, quality, state, playEffect);
    private static void OnSwappedBackpack(PlayerClothing __instance, ItemBackpackAsset asset, byte quality, byte[] state, bool playEffect) =>
        InvokeSwappedClothing(ClothingType.Backpack, WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>().GetOnlinePlayer(__instance.player), asset, quality, state, playEffect);
    private static void OnSwappedGlasses(PlayerClothing __instance, ItemGlassesAsset asset, byte quality, byte[] state, bool playEffect) =>
        InvokeSwappedClothing(ClothingType.Glasses,  WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>().GetOnlinePlayer(__instance.player), asset, quality, state, playEffect);

    private static void InvokeSwappedClothing(ClothingType type, WarfarePlayer player, ItemClothingAsset asset, byte quality, byte[] state, bool playEffect)
    {
        ClothingSwapped args = new ClothingSwapped
        {
            Player = player,
            Type = type,
            State = state,
            Asset = asset,
            EffectPlayed = playEffect,
            Quality = quality
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, CancellationToken.None);
    }
}