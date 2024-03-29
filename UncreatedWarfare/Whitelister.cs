﻿using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uncreated.Framework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Structures;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare;
public class Whitelister : ListSingleton<WhitelistItem>
{
    private static Whitelister _singleton;
    public static bool Loaded => _singleton.IsLoaded<Whitelister, WhitelistItem>();
    public Whitelister() : base("whitelist", Path.Combine(Data.Paths.KitsStorage, "whitelist.json"))
    {
    }
    protected override string LoadDefaults() => "[]";
    public override void Load()
    {
        ItemManager.onTakeItemRequested += OnItemPickup;
        EventDispatcher.SalvageBarricadeRequested += OnBarricadeSalvageRequested;
        EventDispatcher.SalvageStructureRequested += OnStructureSalvageRequested;
        StructureManager.onDeployStructureRequested += OnStructurePlaceRequested;
        BarricadeManager.onModifySignRequested += OnEditSignRequest;
        _singleton = this;
    }

    public override void Unload()
    {
        _singleton = null!;
        BarricadeManager.onModifySignRequested -= OnEditSignRequest;
        StructureManager.onDeployStructureRequested -= OnStructurePlaceRequested;
        EventDispatcher.SalvageStructureRequested -= OnStructureSalvageRequested;
        EventDispatcher.SalvageBarricadeRequested -= OnBarricadeSalvageRequested;
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
    private void OnItemPickup(Player pl, byte x, byte y, uint instanceID, byte toX, byte toY, byte toRot, byte toPage, ItemData itemData, ref bool shouldAllow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? player = UCPlayer.FromPlayer(pl);

        if (player is null || player.OnDuty())
            return;
        if (Assets.find(EAssetType.ITEM, itemData.item.id) is not ItemAsset a)
        {
            L.LogError("Unknown asset on item " + itemData.item.id.ToString());
            shouldAllow = false;
            return;
        }
        bool isWhitelisted = IsWhitelisted(a.GUID, out WhitelistItem whitelistedItem);

        if (toPage == PlayerInventory.STORAGE && !isWhitelisted)
        {
            shouldAllow = false;
            return;
        }

        Kit? kit = player.CachedActiveKitInfo;
        if (kit != null)
        {
            int itemCount = UCInventoryManager.CountItems(player.Player, a.GUID);

            int allowedItems = kit.CountItems(a.GUID, true);

            int max = isWhitelisted ? Math.Max(allowedItems, whitelistedItem.Amount) : allowedItems;

            if (allowedItems == 0)
            {
                if (!isWhitelisted)
                {
                    shouldAllow = false;
                    player.SendChat(T.WhitelistProhibitedPickup, a);
                }
                else if (itemCount >= whitelistedItem.Amount)
                {
                    shouldAllow = false;
                    player.SendChat(T.WhitelistProhibitedPickupAmt, a);
                }
            }
            else if (itemCount >= max)
            {
                if (!isWhitelisted)
                {
                    shouldAllow = false;
                    player.SendChat(T.WhitelistProhibitedPickupAmt, a);
                }
                else if (itemCount >= max)
                {
                    shouldAllow = false;
                    player.SendChat(T.WhitelistProhibitedPickupAmt, a);
                }
            }
        }
        else
        {
            shouldAllow = false;
            player.SendChat(T.WhitelistNoKit);
        }
        if (EventFunctions.DroppedItems.TryGetValue(pl.channel.owner.playerID.steamID.m_SteamID, out List<uint> instances))
        {
            if (instances != null)
                instances.Remove(instanceID);
        }
    }
    private void OnBarricadeSalvageRequested(SalvageBarricadeRequested e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!(e.Player.OnDuty() || IsWhitelisted(e.Barricade.asset.GUID, out _) || e.Player.IsSquadLeader() && RallyManager.IsRally(e.Barricade.asset)))
        {
            Kit? kit = e.Player.CachedActiveKitInfo;
            if (e.ServersideData.owner != e.Player.Steam64 || kit == null || !kit.ContainsItem(e.Barricade.asset.GUID, e.ServersideData.group.GetTeam()))
            {
                e.Player.SendChat(T.WhitelistProhibitedSalvage, e.Barricade.asset);
                e.Break();
            }
        }
    }
    private void OnStructureSalvageRequested(SalvageStructureRequested e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (e.Player.OnDuty() || IsWhitelisted(e.Structure.asset.GUID, out _))
            return;

        Kit? kit = e.Player.CachedActiveKitInfo;
        if (e.ServersideData.owner != e.Player.Steam64 || kit == null || !kit.ContainsItem(e.Structure.asset.GUID, e.ServersideData.group.GetTeam()))
        {
            e.Player.SendChat(T.WhitelistProhibitedSalvage, e.Structure.asset);
            e.Break();
        }
    }
    private void OnEditSignRequest(CSteamID steamID, InteractableSign sign, ref string text, ref bool shouldAllow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? player = UCPlayer.FromCSteamID(steamID);
        if (player == null || !player.OnDuty())
        {
            shouldAllow = false;
            player?.SendChat(T.ProhibitedSignEditing);
        }
    }
    // ReSharper disable InconsistentNaming
    internal void OnBarricadePlaceRequested(
        Barricade barricade,
        ItemBarricadeAsset asset,
        Transform? hit,
        ref Vector3 point,
        ref float angle_x,
        ref float angle_y,
        ref float angle_z,
        ref ulong owner,
        ref ulong group,
        ref bool shouldAllow)
    {
        // ReSharper restore InconsistentNaming
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
                player.SendChat(T.WhitelistProhibitedPlace, asset);
                return;
            }

            bool wh = IsWhitelisted(barricade.asset.GUID, out WhitelistItem item);
            if (wh && (item.Amount >= 255 || item.Amount == -1))
                return;
            

            Kit? kit = player.CachedActiveKitInfo;
            if (wh || kit != null)
            {
                int allowedCount = wh ? item.Amount : kit!.CountItems(barricade.asset.GUID);
                if (allowedCount > 0)
                {
                    int placedCount = UCBarricadeManager.CountBarricadesWhere(b => b.GetServersideData().owner == player.Steam64 && b.asset.GUID == barricade.asset.GUID, allowedCount);
                    if (placedCount >= allowedCount && allowedCount != 255)
                    {
                        StructureSaver? saver = StructureSaver.GetSingletonQuick();
                        int diff = placedCount - allowedCount + 1;
                        foreach (BarricadeDrop drop in UCBarricadeManager.NonPlantedBarricades
                                     .Where(b => b.GetServersideData().owner == player.Steam64 && b.asset.GUID == barricade.asset.GUID)
                                     .OrderBy(b => b.model.TryGetComponent(out BarricadeComponent comp) ? comp.CreateTime : 0)
                                     .ToList())
                        {
                            if (diff <= 0)
                                break;
                            if (Regions.tryGetCoordinate(drop.GetServersideData().point, out byte x, out byte y))
                            {
                                if (saver is not { IsLoaded: true } || saver.GetSaveItemSync(drop.instanceID, StructType.Barricade) is null)
                                {
                                    --diff;
                                    DestroyerComponent.AddOrUpdate(drop.model.gameObject, 0ul, EDamageOrigin.VehicleDecay);
                                    BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                                }
                            }
                        }
                        if (diff > 0)
                        {
                            shouldAllow = false;
                            player.SendChat(T.WhitelistProhibitedPlaceAmt, allowedCount, asset);
                        }
                    }

                    return;
                }
            }

            shouldAllow = false;
            player.SendChat(T.WhitelistProhibitedPlace, asset);
        }
        catch (Exception ex)
        {
            L.LogError("Error verifying barricade place with the whitelist: ");
            L.LogError(ex);
        }
    }
    // ReSharper disable InconsistentNaming
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
        // ReSharper restore InconsistentNaming
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
                player.SendChat(T.WhitelistProhibitedPlace, asset);
                return;
            }
            Kit? kit = player.CachedActiveKitInfo;
            if (kit != null)
            {
                if (IsWhitelisted(structure.asset.GUID, out _))
                    return;

                int allowedCount = kit.CountItems(structure.asset.GUID);

                if (allowedCount > 0)
                {
                    // todo delete old structures not sure what happened to that system
                    int placedCount = UCBarricadeManager.CountStructuresWhere(s => s.GetServersideData().owner == player.Steam64 && s.asset.GUID == structure.asset.GUID, allowedCount);

                    if (placedCount >= allowedCount && allowedCount != 255)
                    {
                        shouldAllow = false;
                        player.SendChat(T.WhitelistProhibitedPlaceAmt, allowedCount, asset);
                    }

                    return;
                }
            }

            shouldAllow = false;
            player.SendChat(T.WhitelistProhibitedPlace, asset);
        }
        catch (Exception ex)
        {
            L.LogError("Error verifying structure place with the whitelist: ");
            L.LogError(ex);
        }
    }
    public static void AddItem(Guid guid, byte amount = 255)
    {
        _singleton.AssertLoaded<Whitelister, WhitelistItem>();
        if (IsWhitelisted(guid, out WhitelistItem item))
        {
            if (amount != item.Amount)
                SetAmount(guid, amount);
            L.Log($"[WHITELISTER] Already whitelisted: {F.AssetToString(guid)}.");
            return;
        }
        _singleton.AddObjectToSave(new WhitelistItem(guid, amount));
        L.Log($"[WHITELISTER] Whitelisted {F.AssetToString(guid)}.");
    }
    public static void RemoveItem(Guid guid)
    {
        _singleton.AssertLoaded<Whitelister, WhitelistItem>();
        _singleton.RemoveWhere(i => i.Item == guid);
        L.Log($"[WHITELISTER] Unwhitelisted {F.AssetToString(guid)}.");
    }

    public static void SetAmount(Guid guid, ushort newAmount)
    {
        _singleton.AssertLoaded<Whitelister, WhitelistItem>();
        _singleton.UpdateObjectsWhere(i => i.Item == guid, i => i.Amount = newAmount);
        L.Log($"[WHITELISTER] Set whitelist amount: {F.AssetToString(guid)}.");
    }

    public static bool IsWhitelisted(Guid guid, out WhitelistItem item)
    {
        _singleton.AssertLoaded<Whitelister, WhitelistItem>();
        return _singleton.ObjectExists(w => w.Item == guid, out item);
    }
    internal static bool IsWhitelistedFast(Guid guid)
    {
        for (int i = 0; i < _singleton.Count; ++i)
        {
            if (_singleton[i].Item == guid)
                return true;
        }
        return false;
    }
}
public class WhitelistItem
{
    public Guid Item { get; set; }
    [CommandSettable]
    public int Amount { get; set; }

    public WhitelistItem()
    {
        Item = default;
        Amount = 255;
    }
    public WhitelistItem(Guid itemID, ushort amount)
    {
        Item = itemID;
        Amount = amount;
    }
}
