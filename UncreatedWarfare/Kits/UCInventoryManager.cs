using Rocket.Unturned.Player;
using SDG.Unturned;
using System;

namespace Uncreated.Warfare.Kits
{
    public static class UCInventoryManager
    {
        public static void GiveKitToPlayer(UnturnedPlayer player, Kit kit)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (kit != null)
            {
                ClearInventory(player);
                foreach (KitClothing clothing in kit.Clothes)
                {
                    if (Assets.find(clothing.id) is ItemAsset asset)
                    {
                        if (clothing.type == EClothingType.SHIRT)
                            player.Player.clothing.askWearShirt(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                        if (clothing.type == EClothingType.PANTS)
                            player.Player.clothing.askWearPants(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                        if (clothing.type == EClothingType.VEST)
                            player.Player.clothing.askWearVest(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                        if (clothing.type == EClothingType.HAT)
                            player.Player.clothing.askWearHat(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                        if (clothing.type == EClothingType.MASK)
                            player.Player.clothing.askWearMask(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                        if (clothing.type == EClothingType.BACKPACK)
                            player.Player.clothing.askWearBackpack(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                        if (clothing.type == EClothingType.GLASSES)
                            player.Player.clothing.askWearGlasses(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                    }
                }

                foreach (KitItem k in kit.Items)
                {
                    if (Assets.find(k.id) is ItemAsset asset)
                    {
                        Item item = new Item(asset.id, k.amount, 100);
                        item.metadata = Convert.FromBase64String(k.metadata);

                        if (!player.Inventory.tryAddItem(item, k.x, k.y, k.page, k.rotation))
                            player.Inventory.tryAddItem(item, true);
                    }
                }
            }
        }

        public static void ClearInventory(UCPlayer player) => ClearInventory(player.SteamPlayer);
        public static void ClearInventory(UnturnedPlayer player) => ClearInventory(player.Player.channel.owner);
        public static void ClearInventory(SteamPlayer player)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            player.player.equipment.dequip();

            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                if (page == PlayerInventory.AREA)
                    continue;

                byte count = player.player.inventory.getItemCount(page);

                for (byte index = 0; index < count; index++)
                {
                    player.player.inventory.removeItem(page, 0);
                }
            }


            byte[] blank = new byte[0];
            player.player.clothing.askWearBackpack(0, 0, blank, true);
            player.player.inventory.removeItem(2, 0);

            player.player.clothing.askWearGlasses(0, 0, blank, true);
            player.player.inventory.removeItem(2, 0);

            player.player.clothing.askWearHat(0, 0, blank, true);
            player.player.inventory.removeItem(2, 0);

            player.player.clothing.askWearPants(0, 0, blank, true);
            player.player.inventory.removeItem(2, 0);

            player.player.clothing.askWearMask(0, 0, blank, true);
            player.player.inventory.removeItem(2, 0);

            player.player.clothing.askWearShirt(0, 0, blank, true);
            player.player.inventory.removeItem(2, 0);

            player.player.clothing.askWearVest(0, 0, blank, true);
            player.player.inventory.removeItem(2, 0);

            byte handcount = player.player.inventory.getItemCount(2);
            for (byte i = 0; i < handcount; i++)
            {
                player.player.inventory.removeItem(2, 0);
            }
        }

        public static void RemoveNumberOfItemsFromStorage(InteractableStorage storage, ushort itemID, int amount)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            int counter = 0;

            for (byte i = (byte)(storage.items.getItemCount() - 1); i >= 0; i--)
            {
                if (storage.items.getItem(i).item.id == itemID)
                {
                    counter++;
                    storage.items.removeItem(i);

                    if (counter == amount)
                        return;
                }
            }
        }

        public static int CountItems(Player player, ushort itemID)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            int count = 0;

            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                byte pageCount = player.inventory.getItemCount(page);

                for (byte index = 0; index < pageCount; index++)
                {
                    if (player.inventory.getItem(page, index).item.id == itemID)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public static void RemoveSingleItem(UCPlayer player, ushort itemID)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                byte pageCount = player.Player.inventory.getItemCount(page);

                for (byte index = 0; index < pageCount; index++)
                {
                    if (player.Player.inventory.getItem(page, index).item.id == itemID)
                    {
                        player.Player.inventory.removeItem(page, index);
                        return;
                    }
                }
            }
        }
    }
}
