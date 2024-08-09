using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using static Uncreated.Warfare.Harmony.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class PlayerClothingReceiveSwapClothingRequest : IHarmonyPatch
{
    private static MethodInfo?[]? _targets;
    private static MethodInfo[]? _patches;

    void IHarmonyPatch.Patch(ILogger logger)
    {
        _targets =
        [
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapShirtRequest), BindingFlags.Public | BindingFlags.Instance),
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapPantsRequest), BindingFlags.Public | BindingFlags.Instance),
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapVestRequest), BindingFlags.Public | BindingFlags.Instance),
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapHatRequest), BindingFlags.Public | BindingFlags.Instance),
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapMaskRequest), BindingFlags.Public | BindingFlags.Instance),
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapBackpackRequest), BindingFlags.Public | BindingFlags.Instance),
            typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapGlassesRequest), BindingFlags.Public | BindingFlags.Instance)
        ];

        _patches =
        [
            Accessor.GetMethod(OnReceiveSwapShirtRequest),
            Accessor.GetMethod(OnReceiveSwapPantsRequest),
            Accessor.GetMethod(OnReceiveSwapVestRequest),
            Accessor.GetMethod(OnReceiveSwapHatRequest),
            Accessor.GetMethod(OnReceiveSwapMaskRequest),
            Accessor.GetMethod(OnReceiveSwapBackpackRequest),
            Accessor.GetMethod(OnReceiveSwapGlassesRequest)
        ];

        for (int i = 0; i < _targets.Length; ++i)
        {
            MethodInfo? target = _targets[i];
            if (target != null)
            {
                Patcher.Patch(target, prefix: _patches[i]);
                logger.LogDebug("Patched {0} for swap clothing event.", Accessor.Formatter.Format(target));
                continue;
            }

            logger.LogError("Failed to find method: {0}.",
                Accessor.Formatter.Format(new MethodDefinition("ReceiveSwap" + (ClothingType)i + "Request")
                    .DeclaredIn<PlayerClothing>(isStatic: false)
                    .WithParameter<byte>("page")
                    .WithParameter<byte>("x")
                    .WithParameter<byte>("y")
                    .ReturningVoid()
                )
            );
        }
    }

    void IHarmonyPatch.Unpatch(ILogger logger)
    {
        if (_targets == null)
            return;

        for (int i = 0; i < _targets.Length; ++i)
        {
            MethodInfo? target = _targets[i];
            if (target == null)
                continue;

            Patcher.Unpatch(target, _patches![i]);
            logger.LogDebug("Unpatched {0} for swap clothing event.", Accessor.Formatter.Format(target));
        }

        _targets = null;
    }

    private static bool OnReceiveSwapShirtRequest(PlayerClothing __instance, ref byte page, ref byte x, ref byte y) =>
        InvokeSwapClothingRequest(ClothingType.Shirt,    WarfareModule.Singleton.ServiceProvider.GetRequiredService<PlayerService>().GetOnlinePlayer(__instance.player), ref page, ref x, ref y);
    private static bool OnReceiveSwapPantsRequest(PlayerClothing __instance, ref byte page, ref byte x, ref byte y) =>
        InvokeSwapClothingRequest(ClothingType.Pants,    WarfareModule.Singleton.ServiceProvider.GetRequiredService<PlayerService>().GetOnlinePlayer(__instance.player), ref page, ref x, ref y);
    private static bool OnReceiveSwapVestRequest(PlayerClothing __instance, ref byte page, ref byte x, ref byte y) =>
        InvokeSwapClothingRequest(ClothingType.Vest,     WarfareModule.Singleton.ServiceProvider.GetRequiredService<PlayerService>().GetOnlinePlayer(__instance.player), ref page, ref x, ref y);
    private static bool OnReceiveSwapHatRequest(PlayerClothing __instance, ref byte page, ref byte x, ref byte y) =>
        InvokeSwapClothingRequest(ClothingType.Hat,      WarfareModule.Singleton.ServiceProvider.GetRequiredService<PlayerService>().GetOnlinePlayer(__instance.player), ref page, ref x, ref y);
    private static bool OnReceiveSwapMaskRequest(PlayerClothing __instance, ref byte page, ref byte x, ref byte y) =>
        InvokeSwapClothingRequest(ClothingType.Mask,     WarfareModule.Singleton.ServiceProvider.GetRequiredService<PlayerService>().GetOnlinePlayer(__instance.player), ref page, ref x, ref y);
    private static bool OnReceiveSwapBackpackRequest(PlayerClothing __instance, ref byte page, ref byte x, ref byte y) =>
        InvokeSwapClothingRequest(ClothingType.Backpack, WarfareModule.Singleton.ServiceProvider.GetRequiredService<PlayerService>().GetOnlinePlayer(__instance.player), ref page, ref x, ref y);
    private static bool OnReceiveSwapGlassesRequest(PlayerClothing __instance, ref byte page, ref byte x, ref byte y) =>
        InvokeSwapClothingRequest(ClothingType.Glasses,  WarfareModule.Singleton.ServiceProvider.GetRequiredService<PlayerService>().GetOnlinePlayer(__instance.player), ref page, ref x, ref y);

    private static bool InvokeSwapClothingRequest(ClothingType type, WarfarePlayer player, ref byte page, ref byte x, ref byte y)
    {
        PlayerInventory playerInv = player.UnturnedPlayer.inventory;
        PlayerClothing playerClothing = player.UnturnedPlayer.clothing;

        ItemJar? jar = null;
        ItemClothingAsset? asset = null;
        if (page != byte.MaxValue)
        {
            byte index = playerInv.getIndex(page, x, y);
            if (index == byte.MaxValue)
                return false;

            jar = playerInv.getItem(page, index);
            asset = jar.GetAsset<ItemClothingAsset>();
            if (asset == null || asset.type != type.GetItemType())
                return false;
        }

        ItemClothingAsset? equipped = null;
        byte[]? equippedState = null;
        byte equippedQuality = 100;

        switch (type)
        {
            case ClothingType.Shirt:
                equipped = playerClothing.shirtAsset;
                equippedState = playerClothing.shirtState;
                equippedQuality = playerClothing.shirtQuality;
                break;

            case ClothingType.Pants:
                equipped = playerClothing.pantsAsset;
                equippedState = playerClothing.pantsState;
                equippedQuality = playerClothing.pantsQuality;
                break;

            case ClothingType.Vest:
                equipped = playerClothing.vestAsset;
                equippedState = playerClothing.vestState;
                equippedQuality = playerClothing.vestQuality;
                break;

            case ClothingType.Hat:
                equipped = playerClothing.hatAsset;
                equippedState = playerClothing.hatState;
                equippedQuality = playerClothing.hatQuality;
                break;

            case ClothingType.Mask:
                equipped = playerClothing.maskAsset;
                equippedState = playerClothing.maskState;
                equippedQuality = playerClothing.maskQuality;
                break;

            case ClothingType.Backpack:
                equipped = playerClothing.backpackAsset;
                equippedState = playerClothing.backpackState;
                equippedQuality = playerClothing.backpackQuality;
                break;

            case ClothingType.Glasses:
                equipped = playerClothing.glassesAsset;
                equippedState = playerClothing.glassesState;
                equippedQuality = playerClothing.glassesQuality;
                break;
        }

        // empty to empty
        if (equipped == null && jar == null)
        {
            return false;
        }

        SwapClothingRequested args = new SwapClothingRequested((Page)page, x, y, jar, asset)
        {
            Player = player,
            Type = type,
            CurrentClothing = equipped,
            CurrentClothingState = equippedState,
            CurrentClothingQuality = equippedQuality
        };

        EventContinuations.Dispatch(args, WarfareModule.EventDispatcher, WarfareModule.Singleton.UnloadToken, out bool shouldAllow, continuation: args =>
        {
            if (!args.Player.IsOnline)
                return;

            byte page = byte.MaxValue, x = byte.MaxValue, y = byte.MaxValue;

            if (!args.IsRemoving)
            {
                // track if the item was moved in-between when the event started and finished
                if (!args.Player.Component<ItemTrackingPlayerComponent>().TryGetCurrentItemPosition(args.EquippingOriginalPage, args.EquippingOriginalX, args.EquippingOriginalY, out Page newPage, out byte newX, out byte newY, out bool isDropped))
                {
                    // check if item has somehow moved but didn't get tracked (kit changed and transformations were cleared, etc.)
                    byte existingIndex = args.Player.UnturnedPlayer.inventory.getIndex((byte)args.EquippingPage, args.EquippingX, args.EquippingY);
                    if (existingIndex == byte.MaxValue)
                        return;

                    if (args.EquippingItem != args.Player.UnturnedPlayer.inventory.getItem((byte)args.EquippingPage, existingIndex).item)
                        return;

                    page = (byte)args.EquippingPage;
                    x = args.EquippingX;
                    y = args.EquippingY;
                }
                else if (isDropped)
                {
                    return;
                }
                else
                {
                    page = (byte)newPage;
                    x = newX;
                    y = newY;
                }
            }

            PlayerClothing playerClothing = args.Player.UnturnedPlayer.clothing;
            switch (type)
            {
                case ClothingType.Shirt:
                    playerClothing.ReceiveSwapShirtRequest(page, x, y);
                    break;

                case ClothingType.Pants:
                    playerClothing.ReceiveSwapPantsRequest(page, x, y);
                    break;

                case ClothingType.Vest:
                    playerClothing.ReceiveSwapVestRequest(page, x, y);
                    break;

                case ClothingType.Hat:
                    playerClothing.ReceiveSwapHatRequest(page, x, y);
                    break;

                case ClothingType.Mask:
                    playerClothing.ReceiveSwapMaskRequest(page, x, y);
                    break;

                case ClothingType.Backpack:
                    playerClothing.ReceiveSwapBackpackRequest(page, x, y);
                    break;

                case ClothingType.Glasses:
                    playerClothing.ReceiveSwapGlassesRequest(page, x, y);
                    break;
            }
        });

        if (!shouldAllow)
            return false;

        page = (byte)args.EquippingPage;
        x = args.EquippingX;
        y = args.EquippingY;
        return true;
    }
}