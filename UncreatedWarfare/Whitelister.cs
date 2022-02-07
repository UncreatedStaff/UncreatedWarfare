using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
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
            BarricadeManager.onDamageBarricadeRequested += OnBarricadeDamageRequested;
            StructureManager.onDamageStructureRequested += OnStructureDamageRequested;
            Reload();
        }
        private void OnStructureDamageRequested(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (F.IsInMain(structureTransform.position))
            {
                shouldAllow = false;
            }
        }
        private void OnBarricadeDamageRequested(CSteamID instigatorSteamID, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (F.IsInMain(barricadeTransform.position))
            {
                shouldAllow = false;
            }
        }
        private void OnItemPickup(Player P, byte x, byte y, uint instanceID, byte to_x, byte to_y, byte to_rot, byte to_page, SDG.Unturned.ItemData itemData, ref bool shouldAllow)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            UCPlayer player = UCPlayer.FromPlayer(P);

            if (player.OnDuty())
            {
                return;
            }
            WhitelistItem whitelistedItem;
            bool isWhitelisted;
            if (Assets.find(EAssetType.ITEM, itemData.item.id) is not ItemAsset a)
            {
                whitelistedItem = null;
                isWhitelisted = false;
                L.LogError("Unknown asset on item " + itemData.item.id.ToString());
                shouldAllow = false;
                return;
            }
            else
            {
                isWhitelisted = IsWhitelisted(a.GUID, out whitelistedItem);
            }
            if (to_page == PlayerInventory.STORAGE && !isWhitelisted)
            {
                shouldAllow = false;
                return;
            }

            if (KitManager.HasKit(player.CSteamID, out Kit kit))
            {
                int itemCount = UCInventoryManager.CountItems(player.Player, itemData.item.id);

                int allowedItems = kit.Items.Count(k => k.id == a.GUID);
                if (allowedItems == 0)
                    allowedItems = kit.Clothes.Count(k => k.id == a.GUID);

                if (allowedItems == 0)
                {
                    if (!isWhitelisted)
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
                    if (!isWhitelisted)
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
            if (!shouldAllow) return;
            using IDisposable profiler = ProfilingUtils.StartTracking();

            UCPlayer player = UCPlayer.FromSteamPlayer(instigatorClient);

            bool isFOB = barricade.model.TryGetComponent(out Components.FOBComponent f);

            if (player.OnDuty() && isFOB)
            {
                f.parent.IsWipedByAuthority = true;
            }
            else
            {
                if (!player.OnDuty() && (!IsWhitelisted(barricade.asset.GUID, out _) || isFOB))
                {
                    player.Message("whitelist_nosalvage");
                    shouldAllow = false;
                    return;
                }
            }

            if (barricade.model.TryGetComponent(out Components.BuildableComponent b))
            {
                b.IsSalvaged = true;
            }

            //if (KitManager.KitExists(player.KitName, out var kit))
            //{
            //    if (kit.Items.Exists(i => i.ID == data.barricade.id))
            //        return;
            //}


        }
        private void OnStructureSalvageRequested(StructureDrop structure, SteamPlayer instigatorClient, ref bool shouldAllow)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            UCPlayer player = UCPlayer.FromSteamPlayer(instigatorClient);
            if (player.OnDuty())
                return;
            SDG.Unturned.StructureData data = structure.GetServersideData();
            if (IsWhitelisted(data.structure.asset.GUID, out _))
                return;

            player.Message("whitelist_nosalvage");
            shouldAllow = false;
        }
        private void OnEditSignRequest(CSteamID steamID, InteractableSign sign, ref string text, ref bool shouldAllow)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            UCPlayer player = UCPlayer.FromCSteamID(steamID);
            if (!player.OnDuty())
            {
                shouldAllow = false;
                player.Message("whitelist_noeditsign");
            }
        }
        internal void OnBarricadePlaceRequested(
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
            using IDisposable profiler = ProfilingUtils.StartTracking();
            try
            {
                UCPlayer player = UCPlayer.FromID(owner);
                if (player == null || player.Player == null || player.OnDuty()) return;
                if (TeamManager.IsInAnyMain(point))
                {
                    shouldAllow = false;
                    player.Message("whitelist_noplace");
                    return;
                }
                if (KitManager.HasKit(player.CSteamID, out Kit kit))
                {
                    if (IsWhitelisted(barricade.asset.GUID, out _))
                    {
                        return;
                    }
                    else
                    {
                        int allowedCount = kit.Items.Where(k => k.id == barricade.asset.GUID).Count();

                        if (allowedCount > 0)
                        {
                            int placedCount = UCBarricadeManager.CountBarricadesWhere(b => b.asset.GUID == barricade.asset.GUID && b.GetServersideData().owner == player.Steam64);

                            if (placedCount >= allowedCount)
                            {
                                shouldAllow = false;
                                player.Message("whitelist_toomanyplaced", allowedCount.ToString());
                                return;
                            }
                            else
                                return;
                        }
                    }
                }

                shouldAllow = false;
                player.Message("whitelist_noplace");
            }
            catch (Exception ex)
            {
                L.LogError("Error verifying barricade place with the whitelist: ");
                L.LogError(ex);
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
            using IDisposable profiler = ProfilingUtils.StartTracking();
            try
            {
                UCPlayer player = UCPlayer.FromID(owner);
                if (player == null || player.Player == null || player.OnDuty()) return;
                if (TeamManager.IsInAnyMainOrAMCOrLobby(point))
                {
                    shouldAllow = false;
                    player.Message("whitelist_noplace");
                    return;
                }
                if (KitManager.HasKit(player.CSteamID, out Kit kit))
                {
                    if (kit.Items.Exists(k => k.id == structure.asset.GUID))
                    {
                        return;
                    }
                    else if (IsWhitelisted(structure.asset.GUID, out _))
                    {
                        return;
                    }
                }

                shouldAllow = false;
                player.Message("whitelist_noplace");
            }
            catch (Exception ex)
            {
                L.LogError("Error verifying structure place with the whitelist: ");
                L.LogError(ex);
            }
        }
        public static void AddItem(Guid ID) => AddObjectToSave(new WhitelistItem(ID, 255));
        public static void RemoveItem(Guid ID) => RemoveWhere(i => i.itemID == ID);
        public static void SetAmount(Guid ID, ushort newAmount) => UpdateObjectsWhere(i => i.itemID == ID, i => i.amount = newAmount);
        public static bool IsWhitelisted(Guid itemID, out WhitelistItem item) => ObjectExists(w => w.itemID == itemID, out item);
        public void Dispose()
        {
            ItemManager.onTakeItemRequested -= OnItemPickup;
            BarricadeDrop.OnSalvageRequested_Global -= OnBarricadeSalvageRequested;
            StructureDrop.OnSalvageRequested_Global -= OnStructureSalvageRequested;
            StructureManager.onDeployStructureRequested -= OnStructurePlaceRequested;
            BarricadeManager.onModifySignRequested -= OnEditSignRequest;
        }
    }
    public class WhitelistItem
    {
        public Guid itemID;
        [JsonSettable]
        public int amount;

        public WhitelistItem(Guid itemID, ushort amount)
        {
            this.itemID = itemID;
            this.amount = amount;
        }
        public WhitelistItem()
        {
            this.itemID = Guid.Empty;
            this.amount = 1;
        }
    }
}
