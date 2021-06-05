using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare
{
    public class Whitelister : JSONSaver<WhitelistItem>, IDisposable
    {
        private static List<WhitelistItem> items;

        protected override string LoadDefaults() => "[]";

        public Whitelister()
            : base(Data.KitsStorage + "whitelist.json")
        {
            ItemManager.onTakeItemRequested += OnItemPickup;
            BarricadeManager.onSalvageBarricadeRequested += OnBarricadeSalvageRequested;
            Reload();
        }

        private void OnItemPickup(Player P, byte x, byte y, uint instanceID, byte to_x, byte to_y, byte to_rot, byte to_page, ItemData itemData, ref bool shouldAllow)
        {
            var player = UnturnedPlayer.FromPlayer(P);

            if (F.OnDuty(player))
            {
                return;
            }

            if (IsWhitelisted(itemData.item.id, out var whitelistedItem))
            {
                if (KitManager.HasKit(player.CSteamID, out var kit))
                {
                    int allowedItems = kit.Items.Count(k => k.ID == itemData.item.id);
                    if (allowedItems == 0)
                        allowedItems = kit.Clothes.Count(k => k.ID == itemData.item.id);

                    if (allowedItems == 0)
                    {
                        shouldAllow = false;
                        player.Message($"whitelist_notallowed");
                    }
                    else if (UCInventoryManager.CountItems(player, itemData.item.id) >= allowedItems)
                    {
                        shouldAllow = false;
                        player.Message($"whitelist_maxamount");
                    }
                }
                else
                {
                    shouldAllow = false;
                    player.Message($"whitelist_nokit");
                }
            }
            else
            {
                shouldAllow = false;
                player.Message($"whitelist_notwhitelisted");
            }
        }
        private void OnBarricadeSalvageRequested(Steamworks.CSteamID steamID, byte x, byte y, ushort plant, ushort index, ref bool shouldAllow)
        {
            var player = UnturnedPlayer.FromCSteamID(steamID);
            if (player.OnDuty())
            {
                return;
            }
            if (BarricadeManager.tryGetRegion(x, y, plant, out var region))
            {
                if (IsWhitelisted(region.barricades[index].barricade.id, out var whitelistedItem))
                {
                    return;
                }
            }

            player.Message("whitelist_nosalvage");
            shouldAllow = false;
        }

        public static void Reload() => items = GetExistingObjects();
        public static void Save() => OverwriteSavedList(items);
        public static void AddItem(ushort ID)
        {
            items.Add(new WhitelistItem(ID, 255));
            Save();
        }
        public static void RemoveItem(ushort ID)
        {
            items.RemoveAll(i => i.itemID == ID);
            Save();
        }
        public static void SetAmount(ushort ID, ushort newAmount)
        {
            items.Where(i => i.itemID == ID).ToList().ForEach(i => i.amount = newAmount);
            Save();
        }
        public static bool IsWhitelisted(ushort itemID, out WhitelistItem item)
        {
            item = items.Find(w => w.itemID == itemID);
            return item != null;
        }
        public void Dispose()
        {
            ItemManager.onTakeItemRequested -= OnItemPickup;
            BarricadeManager.onSalvageBarricadeRequested -= OnBarricadeSalvageRequested;
        }
    }
    public class WhitelistItem
    {
        public ushort itemID;
        public ushort amount;

        public WhitelistItem(ushort itemID, ushort amount)
        {
            this.itemID = itemID;
            this.amount = amount;
        }
    }
}
