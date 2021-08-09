using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
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
            BarricadeDrop.OnSalvageRequested_Global += OnBarricadeSalvageRequested;
            StructureDrop.OnSalvageRequested_Global += OnStructureSalvageRequested;
            StructureManager.onDeployStructureRequested += OnStructurePlaceRequested;
            BarricadeManager.onModifySignRequested += OnEditSignRequest;
            BarricadeManager.onDeployBarricadeRequested += OnBarricadePlaceRequested;
            Reload();
        }
        private void OnItemPickup(Player P, byte x, byte y, uint instanceID, byte to_x, byte to_y, byte to_rot, byte to_page, ItemData itemData, ref bool shouldAllow)
        {
            UCPlayer player = UCPlayer.FromPlayer(P);

            if (F.OnDuty(player))
            {
                return;
            }

            if (KitManager.HasKit(player.CSteamID, out Kit kit))
            {
                int itemCount = UCInventoryManager.CountItems(player.Player, itemData.item.id);

                int allowedItems = kit.Items.Count(k => k.ID == itemData.item.id);
                if (allowedItems == 0)
                    allowedItems = kit.Clothes.Count(k => k.ID == itemData.item.id);

                if (allowedItems == 0)
                {
                    if (!IsWhitelisted(itemData.item.id, out WhitelistItem whitelistedItem))
                    {
                        shouldAllow = false;
                        player.Message("whitelist_notallowed");
                    }
                    else if (itemCount >= whitelistedItem.amount)
                    {
                        shouldAllow = false;
                        player.Message("whitelist_maxamount");
                    }
                }
                else if (itemCount >= allowedItems)
                {
                    if (!IsWhitelisted(itemData.item.id, out WhitelistItem whitelistedItem))
                    {
                        shouldAllow = false;
                        player.Message("whitelist_kit_maxamount");
                    }
                    else if (itemCount >= whitelistedItem.amount)
                    {
                        shouldAllow = false;
                        player.Message("whitelist_maxamount");
                    }
                }
            }
            else
            {
                shouldAllow = false;
                player.Message("whitelist_nokit");
            }
            if (EventFunctions.droppeditems.TryGetValue(P.channel.owner.playerID.steamID.m_SteamID, out List<uint> instances)) 
            {
                if (instances != null)
                    instances.Remove(instanceID);
            }
        }
        private void OnBarricadeSalvageRequested(BarricadeDrop barricade, SteamPlayer instigatorClient, ref bool shouldAllow)
        {
            UCPlayer player = UCPlayer.FromSteamPlayer(instigatorClient);
            if (player.OnDuty())
                return;
            BarricadeData data = barricade.GetServersideData();
            if (data.owner == instigatorClient.playerID.steamID.m_SteamID || IsWhitelisted(data.barricade.id, out _))
                return;

            player.Message("whitelist_nosalvage");
            shouldAllow = false;
        }
        private void OnStructureSalvageRequested(StructureDrop structure, SteamPlayer instigatorClient, ref bool shouldAllow)
        {
            UCPlayer player = UCPlayer.FromSteamPlayer(instigatorClient);
            if (player.OnDuty())
                return;
            StructureData data = structure.GetServersideData();
            if (data.owner == instigatorClient.playerID.steamID.m_SteamID || IsWhitelisted(data.structure.id, out _))
                return;

            player.Message("whitelist_nosalvage");
            shouldAllow = false;
        }
        private void OnEditSignRequest(CSteamID steamID, InteractableSign sign, ref string text, ref bool shouldAllow)
        {
            UCPlayer player = UCPlayer.FromCSteamID(steamID);
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
            try
            {
                UCPlayer player = UCPlayer.FromID(owner);
                if (player == null || player.Player == null || F.OnDuty(player)) return;
                if (player == null || TeamManager.IsInAnyMain(player.Player.transform.position) && !player.OnDutyOrAdmin())
                {
                    shouldAllow = false;
                    player.Message("whitelist_noplace");
                    return;
                }
                if (player == null || player.Player == null || F.OnDuty(player)) return;
                if (player.OnDuty()) return;
                if (KitManager.HasKit(player.CSteamID, out Kit kit))
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
                player.Message("whitelist_noplace");
            }
            catch (Exception ex)
            {
                F.LogError("Error verifying barricade place with the whitelist: ");
                F.LogError(ex);
            }
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
            try
            {
                UCPlayer player = UCPlayer.FromID(owner);
                if (player == null || player.Player == null || F.OnDuty(player)) return;
                if (player == null || TeamManager.IsInAnyMain(player.Player.transform.position) && !player.OnDutyOrAdmin())
                {
                    shouldAllow = false;
                    player.Message("whitelist_noplace");
                    return;
                }
                if (KitManager.HasKit(player.CSteamID, out Kit kit))
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
                player.Message("whitelist_noplace");
            }
            catch (Exception ex)
            {
                F.LogError("Error verifying structure place with the whitelist: ");
                F.LogError(ex);
            }
        }
        public static void AddItem(ushort ID) => AddObjectToSave(new WhitelistItem(ID, 255));
        public static void RemoveItem(ushort ID) => RemoveWhere(i => i.itemID == ID);
        public static void SetAmount(ushort ID, ushort newAmount) => UpdateObjectsWhere(i => i.itemID == ID, i => i.amount = newAmount);
        public static bool IsWhitelisted(ushort itemID, out WhitelistItem item) => ObjectExists(w => w.itemID == itemID, out item);
        public void Dispose()
        {
            ItemManager.onTakeItemRequested -= OnItemPickup;
            BarricadeDrop.OnSalvageRequested_Global -= OnBarricadeSalvageRequested;
            StructureDrop.OnSalvageRequested_Global -= OnStructureSalvageRequested;
            StructureManager.onDeployStructureRequested -= OnStructurePlaceRequested;
            BarricadeManager.onModifySignRequested -= OnEditSignRequest;
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
        public WhitelistItem()
        {
            this.itemID = 0;
            this.amount = 1;
        }
    }
}
