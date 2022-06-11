using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uncreated.Framework;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare;
public class Whitelister : ListSingleton<WhitelistItem>
{
    private static Whitelister Singleton;
    public static bool Loaded => Singleton.IsLoaded<Whitelister, WhitelistItem>();
    public Whitelister() : base("whitelist", Path.Combine(Data.KitsStorage, "whitelist.json"))
    {
    }
    protected override string LoadDefaults() => "[]";
    public override void Load()
    {
        ItemManager.onTakeItemRequested += OnItemPickup;
        BarricadeDrop.OnSalvageRequested_Global += OnBarricadeSalvageRequested;
        StructureDrop.OnSalvageRequested_Global += OnStructureSalvageRequested;
        StructureManager.onDeployStructureRequested += OnStructurePlaceRequested;
        BarricadeManager.onModifySignRequested += OnEditSignRequest;
        Singleton = this;
    }

    public override void Unload()
    {
        Singleton = null!;
        BarricadeManager.onModifySignRequested -= OnEditSignRequest;
        StructureManager.onDeployStructureRequested -= OnStructurePlaceRequested;
        StructureDrop.OnSalvageRequested_Global -= OnStructureSalvageRequested;
        BarricadeDrop.OnSalvageRequested_Global -= OnBarricadeSalvageRequested;
        ItemManager.onTakeItemRequested -= OnItemPickup;
    }
    internal void OnStructureDamageRequested(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (F.IsInMain(structureTransform.position))
        {
            shouldAllow = false;
        }
    }
    internal void OnBarricadeDamageRequested(CSteamID instigatorSteamID, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (F.IsInMain(barricadeTransform.position))
        {
            shouldAllow = false;
        }
    }
    private void OnItemPickup(Player P, byte x, byte y, uint instanceID, byte to_x, byte to_y, byte to_rot, byte to_page, ItemData itemData, ref bool shouldAllow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? player = UCPlayer.FromPlayer(P);

        if (player is null || player.OnDuty())
            return;
        WhitelistItem whitelistedItem;
        bool isWhitelisted;
        if (Assets.find(EAssetType.ITEM, itemData.item.id) is not ItemAsset a)
        {
            L.LogError("Unknown asset on item " + itemData.item.id.ToString());
            shouldAllow = false;
            return;
        }
        else
        isWhitelisted = IsWhitelisted(a.GUID, out whitelistedItem);
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

            int max = isWhitelisted ? Math.Max(allowedItems, whitelistedItem.Amount) : allowedItems;

            if (allowedItems == 0)
            {
                if (!isWhitelisted)
                {
                    shouldAllow = false;
                    player.Message("whitelist_notallowed");
                }
                else if (itemCount >= whitelistedItem.Amount)
                {
                    shouldAllow = false;
                    player.Message("whitelist_maxamount");
                }
            }
            else if (itemCount >= max)
            {
                if (!isWhitelisted)
                {
                    shouldAllow = false;
                    player.Message("whitelist_kit_maxamount");
                }
                else if (itemCount >= max)
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        UCPlayer? player = UCPlayer.FromSteamPlayer(instigatorClient);

        bool isFOB = barricade.model.TryGetComponent(out Components.FOBComponent f);

        if (player == null || player.OnDuty() && isFOB)
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
    }
    private void OnStructureSalvageRequested(StructureDrop structure, SteamPlayer instigatorClient, ref bool shouldAllow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? player = UCPlayer.FromSteamPlayer(instigatorClient);
        if (player == null || player.OnDuty())
            return;
        SDG.Unturned.StructureData data = structure.GetServersideData();
        if (IsWhitelisted(data.structure.asset.GUID, out _))
            return;

        player.Message("whitelist_nosalvage");
        shouldAllow = false;
    }
    private void OnEditSignRequest(CSteamID steamID, InteractableSign sign, ref string text, ref bool shouldAllow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? player = UCPlayer.FromCSteamID(steamID);
        if (player != null && !player.OnDuty())
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            UCPlayer? player = UCPlayer.FromID(owner);
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            UCPlayer? player = UCPlayer.FromID(owner);
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
    public static void AddItem(Guid ID)
    {
        Singleton.AssertLoaded<Whitelister, WhitelistItem>();
        Singleton.AddObjectToSave(new WhitelistItem(ID, 255));
    }
    public static void RemoveItem(Guid ID)
    {
        Singleton.AssertLoaded<Whitelister, WhitelistItem>();
        Singleton.RemoveWhere(i => i.Item == ID);
    }

    public static void SetAmount(Guid ID, ushort newAmount)
    {
        Singleton.AssertLoaded<Whitelister, WhitelistItem>();
        Singleton.UpdateObjectsWhere(i => i.Item == ID, i => i.Amount = newAmount);
    }

    public static bool IsWhitelisted(Guid itemID, out WhitelistItem item)
    {
        Singleton.AssertLoaded<Whitelister, WhitelistItem>();
        return Singleton.ObjectExists(w => w.Item == itemID, out item);
    }
}
public class WhitelistItem
{
    public Guid Item;
    [JsonSettable]
    public int Amount;

    public WhitelistItem()
    {
        Item = default;
        Amount = 255;
    }
    public WhitelistItem(Guid itemID, ushort amount)
    {
        this.Item = itemID;
        this.Amount = amount;
    }
}
