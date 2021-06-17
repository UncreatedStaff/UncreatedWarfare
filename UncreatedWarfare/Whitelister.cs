using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using UnityEngine;

namespace Uncreated.Warfare
{
    public class Whitelister : JSONSaver<WhitelistItem>, IDisposable
    {
        protected override string LoadDefaults() => "[]";

        public Whitelister()
            : base(Data.KitsStorage + "whitelist.json")
        {
            ItemManager.onTakeItemRequested += OnItemPickup;
            BarricadeManager.onSalvageBarricadeRequested += OnBarricadeSalvageRequested;
            StructureManager.onSalvageStructureRequested += OnStructureSalvageRequested;
            StructureManager.onDeployStructureRequested += OnStructurePlaceRequested;
            BarricadeManager.onModifySignRequested += OnEditSignRequest;
            Reload();
        }
        private void OnItemPickup(Player P, byte x, byte y, uint instanceID, byte to_x, byte to_y, byte to_rot, byte to_page, ItemData itemData, ref bool shouldAllow)
        {
            var player = UnturnedPlayer.FromPlayer(P);

            if (F.OnDuty(player))
            {
                return;
            }

            if (KitManager.HasKit(player.CSteamID, out var kit))
            {
                int itemCount = UCInventoryManager.CountItems(player, itemData.item.id);

                int allowedItems = kit.Items.Where(k => k.ID == itemData.item.id).Count();
                if (allowedItems == 0)
                    allowedItems = kit.Clothes.Where(k => k.ID == itemData.item.id).Count();

                if (allowedItems == 0)
                {
                    if (!IsWhitelisted(itemData.item.id, out var whitelistedItem))
                    {
                        shouldAllow = false;
                        player.Message($"whitelist_notallowed");
                    }
                    else if (itemCount >= whitelistedItem.amount)
                    {
                        shouldAllow = false;
                        player.Message($"whitelist_maxamount");
                    }
                }
                else if (itemCount >= allowedItems)
                {
                    shouldAllow = false;
                    player.Message($"whitelist_kit_maxamount");
                }
            }
            else
            {
                shouldAllow = false;
                player.Message($"whitelist_nokit");
            }
        }
        private void OnBarricadeSalvageRequested(CSteamID steamID, byte x, byte y, ushort plant, ushort index, ref bool shouldAllow)
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
        private void OnStructureSalvageRequested(CSteamID steamID, byte x, byte y, ushort index, ref bool shouldAllow)
        {
            var player = UnturnedPlayer.FromCSteamID(steamID);
            if (player.OnDuty())
            {
                return;
            }
            if (StructureManager.tryGetRegion(x, y, out var region))
            {
                if (IsWhitelisted(region.structures[index].structure.id, out var whitelistedItem))
                {
                    return;
                }
            }

            player.Message("whitelist_nosalvage");
            shouldAllow = false;
        }
        private void OnEditSignRequest(CSteamID steamID, InteractableSign sign, ref string text, ref bool shouldAllow)
        {
            var player = UnturnedPlayer.FromCSteamID(steamID);
            if (!player.OnDuty())
            {
                shouldAllow = false;
                player.Message("whitelist_noeditsign");
            }
        }
        private void OnBarricadePlaceRequested(
            Barricade barricade,
            ItemBarricadeAsset asset,
            Transform hit,
            ref Vector3 point,
            ref float angle_x,
            ref float angle_y,
            ref float angle_z,
            ref ulong owner,
            ref ulong group,
            ref bool shouldAllow)
        {
            var player = UnturnedPlayer.FromCSteamID(new CSteamID(owner));

            if (F.OnDuty(player))
                return;

            if (KitManager.HasKit(player.CSteamID, out var kit))
            {
                if (kit.Items.Exists(k => k.ID == barricade.id))
                {
                    return;
                }
                else if (IsWhitelisted(barricade.id, out _))
                {
                    return;
                }
            }

            shouldAllow = false;
            player.Message($"whitelist_noplace");
        }
        private void OnStructurePlaceRequested(
            Structure structure,
            ItemStructureAsset asset,
            ref Vector3 point,
            ref float angle_x,
            ref float angle_y,
            ref float angle_z,
            ref ulong owner,
            ref ulong group,
            ref bool shouldAllow
            )
        {
            var player = UnturnedPlayer.FromCSteamID(new CSteamID(owner));

            if (F.OnDuty(player))
                return;

            if (KitManager.HasKit(player.CSteamID, out var kit))
            {
                if (kit.Items.Exists(k => k.ID == structure.id))
                {
                    return;
                }
                else if (IsWhitelisted(structure.id, out _))
                {
                    return;
                }
            }

            shouldAllow = false;
            player.Message($"whitelist_noplace");
        }
        public static void AddItem(ushort ID) => AddObjectToSave(new WhitelistItem(ID, 255));
        public static void RemoveItem(ushort ID) => RemoveWhere(i => i.itemID == ID);
        public static void SetAmount(ushort ID, ushort newAmount) => UpdateObjectsWhere(i => i.itemID == ID, i => i.amount = newAmount);
        public static bool IsWhitelisted(ushort itemID, out WhitelistItem item) => ObjectExists(w => w.itemID == itemID, out item);
        public void Dispose()
        {
            ItemManager.onTakeItemRequested -= OnItemPickup;
            BarricadeManager.onSalvageBarricadeRequested -= OnBarricadeSalvageRequested;
            BarricadeManager.onDeployBarricadeRequested -= OnBarricadePlaceRequested;
        }
    }
    public class WhitelistItem
    {
        public ushort itemID;
        [JsonSettable]
        public ushort amount;

        public WhitelistItem(ushort itemID, ushort amount)
        {
            this.itemID = itemID;
            this.amount = amount;
        }
    }
}
