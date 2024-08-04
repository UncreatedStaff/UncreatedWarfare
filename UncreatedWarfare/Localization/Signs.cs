using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare;
public class Signs_ : BaseSingleton, ILevelStartListener
{
    public const string Prefix = "sign_";
    public const string VBSPrefix = "vbs_";
    public const string KitPrefix = "kit_";
    public const string TraitPrefix = "trait_";
    public const string LoadoutPrefix = "loadout_";
    public const string LongTranslationPrefix = "l_";
    private static readonly Dictionary<uint, CustomSignComponent> ActiveSigns = new Dictionary<uint, CustomSignComponent>(64);
    public override void Load()
    {
        EventDispatcher.SignTextChanged += OnSignTextChanged;
        EventDispatcher.BarricadePlaced += OnBarricadePlaced;
        EventDispatcher.BarricadeDestroyed += OnBarricadeDestroyed;
    }
    public override void Unload()
    {
        EventDispatcher.BarricadeDestroyed -= OnBarricadeDestroyed;
        EventDispatcher.BarricadePlaced -= OnBarricadePlaced;
        EventDispatcher.SignTextChanged -= OnSignTextChanged;
    }
    private static void OnSignTextChanged(SignTextChanged e) => CheckSign(e.Barricade);
    private static void OnBarricadePlaced(BarricadePlaced e) => CheckSign(e.Barricade);
    private static void OnBarricadeDestroyed(BarricadeDestroyed e)
    {
        if (e.Transform.TryGetComponent(out CustomSignComponent comp))
            UnityEngine.Object.Destroy(comp);
        ActiveSigns.Remove(e.InstanceID);
    }
    public static void InvalidateSign(BarricadeDrop drop)
    {
        if (drop.model.TryGetComponent(out CustomSignComponent comp))
            comp.Init(drop);
    }
    void ILevelStartListener.OnLevelReady() => CheckAllSigns();
    private static CustomSignComponent? GetComponent(BarricadeDrop drop, out bool isNew)
    {
        ThreadUtil.assertIsGameThread();
        if (drop.interactable is not InteractableSign sign)
        {
            isNew = false;
            return null;
        }
        CustomSignComponent comp;
        string text = sign.text;
        if (!text.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            if (ActiveSigns.TryGetValue(drop.instanceID, out comp))
            {
                ActiveSigns.Remove(drop.instanceID);
                UnityEngine.Object.Destroy(comp);
            }
            isNew = true;
            return null;
        }
        if (ActiveSigns.TryGetValue(drop.instanceID, out comp))
        {
            if (comp.CheckStillValid())
            {
                isNew = false;
                return comp;
            }
            if (comp.isActiveAndEnabled)
                UnityEngine.Object.Destroy(comp);
            ActiveSigns.Remove(drop.instanceID);
        }
        if (drop.model.TryGetComponent(out comp))
            UnityEngine.Object.Destroy(comp);

        text = text.Substring(Prefix.Length);
        if (text.StartsWith(VBSPrefix, StringComparison.OrdinalIgnoreCase))
        {
            comp = drop.model.gameObject.AddComponent<VehicleBaySignComponent>();
        }
        else if (text.StartsWith(KitPrefix, StringComparison.OrdinalIgnoreCase))
        {
            comp = drop.model.gameObject.AddComponent<KitSignComponent>();
        }
        else if (text.StartsWith(LoadoutPrefix, StringComparison.OrdinalIgnoreCase))
        {
            comp = drop.model.gameObject.AddComponent<KitSignComponent>();
            ((KitSignComponent)comp).IsLoadout = true;
        }
        else if (text.StartsWith(TraitPrefix, StringComparison.OrdinalIgnoreCase))
        {
            comp = drop.model.gameObject.AddComponent<TraitSignComponent>();
        }
        else if (text.StartsWith(LongTranslationPrefix, StringComparison.OrdinalIgnoreCase))
        {
            comp = drop.model.gameObject.AddComponent<TranlationSignComponent>();
            ((TranlationSignComponent)comp).IsLong = true;
        }
        else
        {
            comp = drop.model.gameObject.AddComponent<TranlationSignComponent>();
        }

        comp.Init(drop);
        ActiveSigns.Add(drop.instanceID, comp);
        isNew = true;
        return comp;
    }
    public static Kit? GetKitFromSign(BarricadeDrop drop, out int loadoutId)
    {
        if (ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent kitsign)
        {
            loadoutId = kitsign.LoadoutIndex;
            return kitsign.IsLoadout ? null : kitsign.Kit;
        }

        loadoutId = -1;
        return null;
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static bool CheckSign(BarricadeDrop drop)
    {
        ThreadUtil.assertIsGameThread();
        if (drop.model == null)
            return false;
        CustomSignComponent? comp = GetComponent(drop, out bool isNew);
        if (comp is not null)
        {
            if (isNew)
                BroadcastSignUpdate(drop, comp);
            return true;
        }
        return false;
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static void CheckAllSigns()
    {
        ThreadUtil.assertIsGameThread();
        foreach (uint id in ActiveSigns.Keys.ToList())
        {
            BarricadeDrop? drop = BarricadeUtility.FindBarricade(id).Drop;
            if (drop != null || !ActiveSigns.TryGetValue(id, out CustomSignComponent comp))
                continue;

            if (comp != null)
                UnityEngine.Object.Destroy(comp);
            ActiveSigns.Remove(id);
        }
        for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                for (int i = 0; i < region.drops.Count; ++i)
                {
                    if (region.drops[i].asset.build is EBuild.SIGN or EBuild.SIGN_WALL)
                        CheckSign(region.drops[i]);
                }
            }
        }
        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
            for (int i = 0; i < region.drops.Count; ++i)
            {
                if (region.drops[i].asset.build is EBuild.SIGN or EBuild.SIGN_WALL)
                    CheckSign(region.drops[i]);
            }
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static void UpdateAllSigns(UCPlayer? player = null, bool updatePlainText = false)
    {
        ThreadUtil.assertIsGameThread();
        bool a = player is null;
        if (!a && !player!.IsOnline)
            return;
        for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                for (int i = 0; i < region.drops.Count; ++i)
                {
                    if (region.drops[i].asset.build is EBuild.SIGN or EBuild.SIGN_WALL)
                    {
                        if (a) BroadcastSignUpdate(region.drops[i], updatePlainText);
                        else SendSignUpdate(region.drops[i], player!, updatePlainText);
                    }
                }
            }
        }
        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
            for (int i = 0; i < region.drops.Count; ++i)
            {
                if (region.drops[i].asset.build is EBuild.SIGN or EBuild.SIGN_WALL)
                {
                    if (a) BroadcastSignUpdate(region.drops[i], updatePlainText);
                    else SendSignUpdate(region.drops[i], player!, updatePlainText);
                }
            }
        }
    }
    public static void UpdateLoadoutSigns(UCPlayer? player)
    {
        ThreadUtil.assertIsGameThread();
        bool b = player is null;
        if (player is null)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent comp2 && comp2.IsLoadout)
                            BroadcastSignUpdate(drop, comp2);
                    }
                }
            }
        }
        else if (!player.IsOnline) return;
        else
        {
            int maxx = Math.Min(player.Player.movement.region_x + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            int maxy = Math.Min(player.Player.movement.region_y + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            for (int x = Math.Max(0, player.Player.movement.region_x - BarricadeManager.BARRICADE_REGIONS); x <= maxx; ++x)
            {
                for (int y = Math.Max(0, player.Player.movement.region_y - BarricadeManager.BARRICADE_REGIONS); y <= maxy; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent comp2 && comp2.IsLoadout)
                            SendSignUpdate(drop, player);
                    }
                }
            }
        }

        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
            for (int i = 0; i < region.drops.Count; ++i)
            {
                BarricadeDrop drop = region.drops[i];
                if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent comp2 && comp2.IsLoadout)
                {
                    if (b)
                        BroadcastSignUpdate(drop, comp2);
                    else
                        SendSignUpdate(drop, player!, comp2);
                }
            }
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static void UpdateKitSigns(UCPlayer? player, string? kitName)
    {
        ThreadUtil.assertIsGameThread();
        bool a = kitName is null;
        if (player is null)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent comp2 && (a || kitName!.Equals(comp2.KitName, StringComparison.OrdinalIgnoreCase)))
                            BroadcastSignUpdate(drop, comp2);
                    }
                }
            }
        }
        else if (!player.IsOnline)
            return;
        else
        {
            int maxx = Math.Min(player.Player.movement.region_x + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            int maxy = Math.Min(player.Player.movement.region_y + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            for (int x = Math.Max(0, player.Player.movement.region_x - BarricadeManager.BARRICADE_REGIONS); x <= maxx; ++x)
            {
                for (int y = Math.Max(0, player.Player.movement.region_y - BarricadeManager.BARRICADE_REGIONS); y <= maxy; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent comp2 && (a || kitName!.Equals(comp2.KitName, StringComparison.OrdinalIgnoreCase)))
                            SendSignUpdate(drop, player, comp2);
                    }
                }
            }
        }

        bool b = player is null;
        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
            for (int i = 0; i < region.drops.Count; ++i)
            {
                BarricadeDrop drop = region.drops[i];
                if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent comp2 && (a || kitName!.Equals(comp2.KitName, StringComparison.OrdinalIgnoreCase)))
                {
                    if (b)
                        BroadcastSignUpdate(drop, comp2);
                    else
                        SendSignUpdate(drop, player!, comp2);
                }
            }
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static void BroadcastSignUpdate(BarricadeDrop drop, bool updatePlainText = false)
    {
        ThreadUtil.assertIsGameThread();
        if (drop.interactable is InteractableSign sign)
        {
            if (ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp))
            {
                BroadcastSignUpdate(drop, comp);
                return;
            }

            if (!updatePlainText) return;
            NetId id = sign.GetNetId();
            bool r = Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y);
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (r && !Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                    continue;
                Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, pl.Connection, sign.text);
            }
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    private static void BroadcastSignUpdate(BarricadeDrop drop, CustomSignComponent comp)
    {
        NetId id = drop.interactable.GetNetId();
        bool r = Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y);
        if (comp.DropIsPlanted)
            r = false;
        if (comp is { OptimizedBroadcast: false, PerPlayer: true } || !r)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (r && !Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                    continue;
                Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, pl.Connection, comp.Translate(pl.Locale.LanguageInfo, pl.Locale.CultureInfo, pl));
            }
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.InRegions(x, y, BarricadeManager.BARRICADE_REGIONS))
            {
                string val = comp.OptimizedBroadcast ? comp.Translate(set.Language, set.CultureInfo) : comp.Translate(set.Language, set.CultureInfo, null);
                while (set.MoveNext())
                    Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, set.Next.Connection, comp.OptimizedBroadcast ? comp.FormatBroadcast(val, set.Language, set.CultureInfo, set.Next) : val);
            }
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static void SendSignUpdate(BarricadeDrop drop, UCPlayer player, bool updatePlainText = false)
    {
        ThreadUtil.assertIsGameThread();
        if (drop.interactable is InteractableSign sign)
        {
            if (ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp))
            {
                SendSignUpdate(drop, player, comp);
                return;
            }
            if (!updatePlainText ||
                Regions.tryGetCoordinate(drop.model.position, out byte x2, out byte y2) &&
                !Regions.checkArea(x2, y2, player.Player.movement.region_x, player.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                return;
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.ViewLens.HasValue && pl.ViewLens!.Value == player.Steam64)
                    Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Unreliable, pl.Connection, sign.text);
            }
            Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Unreliable, player.Connection, sign.text);
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    private static void SendSignUpdate(BarricadeDrop drop, UCPlayer player, CustomSignComponent comp)
    {
        if (!comp.DropIsPlanted && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y) && !Regions.checkArea(x, y, player.Player.movement.region_x, player.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
            return;
        string t = comp.Translate(player.Locale.LanguageInfo, player.Locale.CultureInfo, player);
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.ViewLens.HasValue && pl.ViewLens!.Value == player.Steam64)
                Data.SendChangeText.Invoke(((InteractableSign)drop.interactable).GetNetId(), ENetReliability.Unreliable, pl.Connection, t);
        }
        Data.SendChangeText.Invoke(((InteractableSign)drop.interactable).GetNetId(), ENetReliability.Unreliable, player.Connection, t);
    }
    public static void UpdateTraitSigns(UCPlayer? player, TraitData? data)
    {
        ThreadUtil.assertIsGameThread();
        string n = data?.TypeName!;
        bool a = n is null;
        if (player is null)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is TraitSignComponent comp2 && (a || n!.Equals(comp2.TraitName, StringComparison.OrdinalIgnoreCase)))
                            BroadcastSignUpdate(drop, comp2);
                    }
                }
            }
        }
        else if (!player.IsOnline)
            return;
        else
        {
            int maxx = Math.Min(player.Player.movement.region_x + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            int maxy = Math.Min(player.Player.movement.region_y + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            for (int x = Math.Max(0, player.Player.movement.region_x - BarricadeManager.BARRICADE_REGIONS); x <= maxx; ++x)
            {
                for (int y = Math.Max(0, player.Player.movement.region_y - BarricadeManager.BARRICADE_REGIONS); y <= maxy; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is TraitSignComponent comp2 && (a || n!.Equals(comp2.TraitName, StringComparison.OrdinalIgnoreCase)))
                            SendSignUpdate(drop, player, comp2);
                    }
                }
            }
        }

        bool b = player is null;
        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
            for (int i = 0; i < region.drops.Count; ++i)
            {
                BarricadeDrop drop = region.drops[i];
                if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is TraitSignComponent comp2 && (a || n!.Equals(comp2.TraitName, StringComparison.OrdinalIgnoreCase)))
                {
                    if (b)
                        BroadcastSignUpdate(drop, comp2);
                    else
                        SendSignUpdate(drop, player!, comp2);
                }
            }
        }
    }
    public static void UpdateVehicleBaySigns(UCPlayer? player) => UpdateVehicleBaySigns(player, (VehicleSpawn?)null);
    public static void UpdateVehicleBaySigns(UCPlayer? player, VehicleSpawn? spawn)
    {
        int key = spawn is null ? PrimaryKey.NotAssigned : spawn.PrimaryKey;
        ThreadUtil.assertIsGameThread();
        bool a = key < 0;
        if (player is null)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is VehicleBaySignComponent comp2 && (a || (comp2.Spawn is { } c && key == c.LastPrimaryKey.Key)))
                            BroadcastSignUpdate(drop, comp2);
                    }
                }
            }
        }
        else if (player.IsOnline)
        {
            int maxx = Math.Min(player.Player.movement.region_x + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            int maxy = Math.Min(player.Player.movement.region_y + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            for (int x = Math.Max(0, player.Player.movement.region_x - BarricadeManager.BARRICADE_REGIONS); x <= maxx; ++x)
            {
                for (int y = Math.Max(0, player.Player.movement.region_y - BarricadeManager.BARRICADE_REGIONS); y <= maxy; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is VehicleBaySignComponent comp2 && (a || (comp2.Spawn is { } c && key == c.LastPrimaryKey.Key)))
                            SendSignUpdate(drop, player, comp2);
                    }
                }
            }
        }
    }
    public static void UpdateVehicleBaySigns(UCPlayer? player, VehicleData? data)
    {
        int key = data is null ? PrimaryKey.NotAssigned : data.PrimaryKey;
        ThreadUtil.assertIsGameThread();
        bool a = key < 0;
        if (player is null)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is VehicleBaySignComponent comp2 && (a || (comp2.Spawn?.Item?.Vehicle is { } c && key == c.LastPrimaryKey.Key)))
                            BroadcastSignUpdate(drop, comp2);
                    }
                }
            }
        }
        else if (player.IsOnline)
        {
            int maxx = Math.Min(player.Player.movement.region_x + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            int maxy = Math.Min(player.Player.movement.region_y + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            for (int x = Math.Max(0, player.Player.movement.region_x - BarricadeManager.BARRICADE_REGIONS); x <= maxx; ++x)
            {
                for (int y = Math.Max(0, player.Player.movement.region_y - BarricadeManager.BARRICADE_REGIONS); y <= maxy; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is VehicleBaySignComponent comp2 && (a || (comp2.Spawn?.Item?.Vehicle is { } c && key == c.LastPrimaryKey.Key)))
                            SendSignUpdate(drop, player, comp2);
                    }
                }
            }
        }
    }
    public static void InvalidateTraitSigns(UCPlayer? player, TraitData? data)
    {
        ThreadUtil.assertIsGameThread();
        string n = data?.TypeName!;
        bool a = n is null;
        for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                for (int i = 0; i < region.drops.Count; ++i)
                {
                    BarricadeDrop drop = region.drops[i];
                    if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is TraitSignComponent comp2 && (a || n!.Equals(comp2.TraitName, StringComparison.OrdinalIgnoreCase)))
                        InvalidateSign(drop);
                }
            }
        }
        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
            for (int i = 0; i < region.drops.Count; ++i)
            {
                BarricadeDrop drop = region.drops[i];
                if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is TraitSignComponent comp2 && (a || n!.Equals(comp2.TraitName, StringComparison.OrdinalIgnoreCase)))
                    InvalidateSign(drop);
            }
        }
    }
    public static void InvalidateVehicleBaySigns() => InvalidateVehicleBaySigns((VehicleSpawn?)null);
    public static void InvalidateVehicleBaySigns(VehicleSpawn? spawn)
    {
        int key = spawn is null ? PrimaryKey.NotAssigned : spawn.PrimaryKey;
        ThreadUtil.assertIsGameThread();
        bool a = key < 0;
        for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                for (int i = 0; i < region.drops.Count; ++i)
                {
                    BarricadeDrop drop = region.drops[i];
                    if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is VehicleBaySignComponent comp2 && (a || (comp2.Spawn is { } c && key == c.LastPrimaryKey.Key)))
                        InvalidateSign(drop);
                }
            }
        }
    }
    public static void InvalidateVehicleBaySigns(VehicleData? data)
    {
        int key = data is null ? PrimaryKey.NotAssigned : data.PrimaryKey;
        ThreadUtil.assertIsGameThread();
        bool a = key < 0;
        for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                for (int i = 0; i < region.drops.Count; ++i)
                {
                    BarricadeDrop drop = region.drops[i];
                    if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is VehicleBaySignComponent comp2 && (a || (comp2.Spawn?.Item?.Vehicle is { } c && key == c.LastPrimaryKey.Key)))
                        InvalidateSign(drop);
                }
            }
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static string GetClientText(BarricadeDrop drop, UCPlayer player, out bool isLong)
    {
        if (drop.interactable is InteractableSign sign)
        {
            if (ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp))
            {
                isLong = comp is TranlationSignComponent t && t.IsLong;
                return comp.Translate(player.Locale.LanguageInfo, player.Locale.CultureInfo, player);
            }

            isLong = false;
            return sign.text;
        }

        isLong = false;
        return string.Empty;
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static string GetClientText(BarricadeDrop drop, UCPlayer player)
    {
        if (drop.interactable is InteractableSign sign)
        {
            return ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) ? comp.Translate(player.Locale.LanguageInfo, player.Locale.CultureInfo, player) : sign.text;
        }

        return string.Empty;
    }
    public static void SetSignTextServerOnly(InteractableSign sign, string text)
    {
        ThreadUtil.assertIsGameThread();
        BarricadeDrop barricadeByRootFast = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
        byte[] state = barricadeByRootFast.GetServersideData().barricade.state;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
        if (bytes.Length > byte.MaxValue - 18)
        {
            L.LogWarning(text + " is too long to go on a sign! (SetSignTextServerOnly)");
            return;
        }
        byte[] newState = new byte[17 + bytes.Length];
        Buffer.BlockCopy(state, 0, newState, 0, 16);
        newState[16] = (byte)bytes.Length;
        if (bytes.Length != 0)
            Buffer.BlockCopy(bytes, 0, newState, 17, bytes.Length);
        BarricadeManager.updateState(barricadeByRootFast.model, newState, newState.Length);
        sign.updateState(barricadeByRootFast.asset, newState);
    }
    private abstract class CustomSignComponent : MonoBehaviour
    {
        protected BarricadeDrop Barricade;
        protected InteractableSign Sign;
        public bool DropIsPlanted { get; private set; }
        public virtual bool OptimizedBroadcast => false;
        public abstract bool PerPlayer { get; }
        public abstract bool CheckStillValid();
        public void Init(BarricadeDrop drop)
        {
            Barricade = drop;
            if (BarricadeManager.tryGetRegion(drop.model, out _, out _, out ushort plant, out _) && plant != ushort.MaxValue)
                DropIsPlanted = true;
            Sign = (InteractableSign)drop.interactable;
            Init();
        }
        protected abstract void Init();
        public abstract string Translate(LanguageInfo language, CultureInfo culture, UCPlayer? player);
        public virtual string Translate(LanguageInfo language, CultureInfo culture) => throw new NotImplementedException();
        public virtual string FormatBroadcast(string text, LanguageInfo language, CultureInfo culture, UCPlayer player) => text;
    }
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class TranlationSignComponent : CustomSignComponent
    {
        private Translation? _translation;
        private string? _defCache;
        private bool _warn;
        public override bool PerPlayer => false;
        public override bool CheckStillValid() => Sign != null &&
                                                  Sign.text.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) && 
                                                 !Sign.text.StartsWith(Prefix + KitPrefix, StringComparison.OrdinalIgnoreCase) &&
                                                 !Sign.text.StartsWith(Prefix + TraitPrefix, StringComparison.OrdinalIgnoreCase) &&
                                                 !Sign.text.StartsWith(Prefix + LoadoutPrefix, StringComparison.OrdinalIgnoreCase) &&
                                                 !Sign.text.StartsWith(Prefix + VBSPrefix, StringComparison.OrdinalIgnoreCase);
        public bool IsLong { get; set; }
        public string SignId { get; private set; }

        [UsedImplicitly]
        void OnDisable()
        {
            Translation.OnReload -= OnReload;
        }
        protected override void Init()
        {
            _defCache = null;
            if (Sign.text.Length > Prefix.Length + TraitPrefix.Length)
            {
                SignId = Sign.text.Substring(Prefix.Length);
            }
            else
            {
                SignId = Sign.text;
                if (!_warn)
                {
                    L.LogWarning("Sign at " + gameObject.transform.position + " has an invalid trait id: \"" + Sign.text + "\".");
                    _warn = true;
                }
            }
            Translation.OnReload += OnReload;
            if (!string.IsNullOrEmpty(SignId))
                _translation = Translation.FromSignId(SignId);
        }
        private void OnReload() => _defCache = null;
        public override string Translate(LanguageInfo language, CultureInfo culture, UCPlayer? player)
        {
            if (language.IsDefault)
                return _defCache ??= _translation?.Translate(language) ?? SignId ?? Sign.text.Substring(Prefix.Length);
            
            return _translation?.Translate(language, culture) ?? SignId ?? Sign.text.Substring(Prefix.Length);
        }
    }
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class VehicleBaySignComponent : CustomSignComponent
    {
        private SqlItem<VehicleSpawn>? _spawn;
        private ulong _base;
        public override bool PerPlayer => true;
        public override bool OptimizedBroadcast => true;

        public SqlItem<VehicleSpawn>? Spawn
        {
            get
            {
                if (_spawn is not null && _spawn.Manager != null)
                    return _spawn;
                VehicleSpawner.GetSingletonQuick()?.TryGetSpawn(Barricade, out _spawn);
                return _spawn;
            }
        }
        public override bool CheckStillValid() => Sign != null &&
                                                  Sign.text.StartsWith(Prefix + VBSPrefix, StringComparison.OrdinalIgnoreCase);
        protected override void Init()
        {
            if (TeamManager.Team1Main.IsInside(transform.position))
                _base = 1ul;
            else if (TeamManager.Team2Main.IsInside(transform.position))
                _base = 2ul;
            else
                _base = 0ul;
        }
        public override string Translate(LanguageInfo language, CultureInfo culture, UCPlayer? player)
        {
            VehicleSpawn? spawn = Spawn?.Item;
            VehicleData? data;
            if (spawn != null && (data = spawn.Vehicle?.Item) != null)
            {
                return Util.QuickFormat(Localization.TranslateVBS(spawn, data, player!.Locale.LanguageInfo, player.Locale.CultureInfo,
                    TeamManager.GetFactionSafe(_base) ?? TeamManager.GetFactionInfo(data.Faction)), data.GetCostLine(player));
            }
            return Sign.text;
        }

        public override string Translate(LanguageInfo language, CultureInfo culture)
        {
            VehicleSpawn? spawn = Spawn?.Item;
            VehicleData? data;
            if (spawn != null && (data = spawn.Vehicle?.Item) != null)
            {
                return Localization.TranslateVBS(spawn, data, language, culture, TeamManager.GetFactionSafe(_base) ?? TeamManager.GetFactionInfo(data.Faction));
            }
            return Sign.text;
        }

        public override string FormatBroadcast(string text, LanguageInfo language, CultureInfo culture, UCPlayer player)
        {
            VehicleSpawn? spawn = Spawn?.Item;
            VehicleData? data;
            if (spawn != null && (data = spawn.Vehicle?.Item) != null)
            {
                return Util.QuickFormat(text, data.GetCostLine(player));
            }
            return text;
        }
    }
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class TraitSignComponent : CustomSignComponent
    {
        private bool _warn;
        private TraitData? _trait;
        private string _traitName;

        public override bool PerPlayer => true;
        public string TraitName
        {
            get => _traitName;
            set
            {
                _traitName = value;
                _trait = null;
            }
        }
        public TraitData? Trait
        {
            get
            {
                if (_trait != null || !TraitManager.Loaded)
                    return _trait;
                if (TraitName is null)
                    return null;
                return _trait = TraitManager.FindTrait(TraitName);
            }
        }
        public override bool CheckStillValid() => Sign != null &&
                                                  Sign.text.StartsWith(Prefix + TraitPrefix, StringComparison.OrdinalIgnoreCase);
        protected override void Init()
        {
            if (Sign.text.Length > Prefix.Length + TraitPrefix.Length)
            {
                TraitName = Sign.text.Substring(Prefix.Length + TraitPrefix.Length);
                _ = Trait;
            }
            else
            {
                TraitName = null!;
                if (!_warn)
                {
                    L.LogWarning("Sign at " + gameObject.transform.position + " has an invalid trait id: \"" + Sign.text + "\".");
                    _warn = true;
                }
            }
        }
        public override string Translate(LanguageInfo language, CultureInfo culture, UCPlayer? player)
        {
            TraitData? trait = Trait;
            if (trait != null)
            {
                ulong team = trait.Team is 1 or 2 ? trait.Team : player!.GetTeam();
                string txt = TraitSigns.TranslateTraitSign(player!.Locale.LanguageInfo, player.Locale.CultureInfo, trait, team, out bool fmt);
                return fmt ? TraitSigns.FormatTraitSign(player, trait, txt) : txt;
            }
            return Sign.text;
        }
    }
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class KitSignComponent : CustomSignComponent
    {
        private bool _warn;
        private string _kitName;
        private KitManager? _lastManager;
        public int LoadoutIndex { get; set; } = -1;
        public bool IsLoadout { get; set; }
        public override bool PerPlayer => true;
        public string KitName
        {
            get => _kitName;
            set
            {
                _kitName = value;
                if (_kit is not null && (value is null || !_kitName.Equals(_kit?.InternalName)))
                    _kit = null;
            }
        }

        private Kit? _kit;
        public Kit? Kit
        {
            get
            {
                if (KitName is null)
                    return null;
                if (_kit != null && _lastManager is { IsLoaded: true })
                    return _kit;
                _lastManager = Data.Singletons.GetSingleton<KitManager>();
                if (_lastManager == null)
                    return _kit = null;
                return _kit = _lastManager.Cache.GetKit(KitName);
            }
        }
        public override bool CheckStillValid() => Sign != null &&
                                                  (Sign.text.StartsWith(Prefix + KitPrefix, StringComparison.OrdinalIgnoreCase) ||
                                                   Sign.text.StartsWith(Prefix + LoadoutPrefix, StringComparison.OrdinalIgnoreCase));
        protected override void Init()
        {
            if (IsLoadout)
            {
                if (Sign.text.Length > Prefix.Length + LoadoutPrefix.Length &&
                    byte.TryParse(Sign.text.Substring(Prefix.Length + LoadoutPrefix.Length), NumberStyles.Number, Data.AdminLocale, out byte loadout))
                {
                    LoadoutIndex = loadout;
                }
                else if (!_warn)
                {
                    L.LogWarning("Sign at " + gameObject.transform.position + " has an invalid loadout id: \"" + Sign.text + "\".");
                    _warn = true;
                }
            }
            else
            {
                if (Sign.text.Length > Prefix.Length + KitPrefix.Length)
                {
                    KitName = Sign.text.Substring(Prefix.Length + KitPrefix.Length);
                    _ = Kit;
                }
                else
                {
                    KitName = null!;
                    if (!_warn)
                    {
                        L.LogWarning("Sign at " + gameObject.transform.position + " has an invalid kit id: \"" + Sign.text + "\".");
                        _warn = true;
                    }
                }
            }
        }
        public override string Translate(LanguageInfo language, CultureInfo culture, UCPlayer? player)
        {
            if (IsLoadout)
            {
                return LoadoutIndex > -1 ? Localization.TranslateLoadoutSign((byte)LoadoutIndex, player!) : Sign.text;
            }
            Kit? kit = Kit;
            return kit != null ? Localization.TranslateKitSign(kit, player!) : Sign.text;
        }
    }
}
