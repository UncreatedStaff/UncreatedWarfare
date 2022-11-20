﻿using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Items;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events.Structures;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Events;
public static class EventDispatcher
{
    public static event EventDelegate<ExitVehicleRequested> ExitVehicleRequested;
    public static event EventDelegate<EnterVehicleRequested> EnterVehicleRequested;
    public static event EventDelegate<VehicleSwapSeatRequested> VehicleSwapSeatRequested;
    public static event EventDelegate<VehicleSpawned> VehicleSpawned;
    public static event EventDelegate<ExitVehicle> ExitVehicle;
    public static event EventDelegate<EnterVehicle> EnterVehicle;
    public static event EventDelegate<VehicleSwapSeat> VehicleSwapSeat;
    public static event EventDelegate<VehicleDestroyed> VehicleDestroyed;

    public static event EventDelegate<BarricadeDestroyed> BarricadeDestroyed;
    public static event EventDelegate<PlaceBarricadeRequested> BarricadePlaceRequested;
    public static event EventDelegate<BarricadePlaced> BarricadePlaced;
    public static event EventDelegate<LandmineExploding> LandmineExploding;

    public static event EventDelegate<StructureDestroyed> StructureDestroyed;
    public static event EventDelegate<SalvageStructureRequested> SalvageStructureRequested;
    public static event EventDelegate<DamageStructureRequested> DamageStructureRequested;

    public static event EventDelegate<ItemDropRequested> ItemDropRequested;
    public static event EventDelegate<CraftRequested> CraftRequested;

    public static event EventDelegate<ThrowableSpawned> ThrowableSpawned;
    public static event EventDelegate<ThrowableSpawned> ThrowableDespawning;

    public static event EventDelegate<ProjectileSpawned> ProjectileSpawned;
    public static event EventDelegate<ProjectileSpawned> ProjectileExploded;

    public static event EventDelegate<PlayerPending> PlayerPending;
    public static event EventDelegate<PlayerJoined> PlayerJoined;
    public static event EventDelegate<PlayerEvent> PlayerLeaving;
    public static event EventDelegate<BattlEyeKicked> PlayerBattlEyeKicked;
    public static event EventDelegate<PlayerDied> PlayerDied;
    public static event EventDelegate<GroupChanged> GroupChanged;
    public static event EventDelegate<PlayerEvent> UIRefreshRequested;
    internal static void SubscribeToAll()
    {
        EventPatches.TryPatchAll();
        VehicleManager.onExitVehicleRequested += VehicleManagerOnExitVehicleRequested;
        VehicleManager.onEnterVehicleRequested += VehicleManagerOnEnterVehicleRequested;
        VehicleManager.onSwapSeatRequested += VehicleManagerOnSwapSeatRequested;
        VehicleManager.OnVehicleExploded += VehicleManagerOnVehicleExploded;
        InteractableVehicle.OnPassengerAdded_Global += InteractableVehicleOnPassengerAdded;
        InteractableVehicle.OnPassengerRemoved_Global += InteractableVehicleOnPassengerRemoved;
        InteractableVehicle.OnPassengerChangedSeats_Global += InteractableVehicleOnPassengerChangedSeats;
        BarricadeManager.onDeployBarricadeRequested += BarricadeManagerOnDeployBarricadeRequested;
        BarricadeManager.onBarricadeSpawned += BarricadeManagerOnBarricadeSpawned;
        Provider.onServerConnected += ProviderOnServerConnected;
        Provider.onServerDisconnected += ProviderOnServerDisconnected;
        Provider.onCheckValidWithExplanation += ProviderOnCheckValidWithExplanation;
        Provider.onBattlEyeKick += ProviderOnBattlEyeKick;
        UseableThrowable.onThrowableSpawned += UseableThrowableOnThrowableSpawned;
        UseableGun.onProjectileSpawned += ProjectileOnProjectileSpawned;
        UseableGun.onBulletHit += OnBulletHit;
        PlayerInput.onPluginKeyTick += OnPluginKeyTick;
        PlayerCrafting.onCraftBlueprintRequested += PlayerCraftingOnCraftRequested;
        StructureDrop.OnSalvageRequested_Global += StructureDropOnSalvageRequested;
        StructureManager.onDamageStructureRequested += StructureManagerOnDamageStructureRequested;
    }
    internal static void UnsubscribeFromAll()
    {
        StructureManager.onDamageStructureRequested -= StructureManagerOnDamageStructureRequested;
        StructureDrop.OnSalvageRequested_Global -= StructureDropOnSalvageRequested;
        PlayerCrafting.onCraftBlueprintRequested -= PlayerCraftingOnCraftRequested;
        UseableGun.onBulletHit -= OnBulletHit;
        PlayerInput.onPluginKeyTick -= OnPluginKeyTick;
        UseableGun.onProjectileSpawned -= ProjectileOnProjectileSpawned;
        UseableThrowable.onThrowableSpawned -= UseableThrowableOnThrowableSpawned;
        Provider.onBattlEyeKick -= ProviderOnBattlEyeKick;
        Provider.onCheckValidWithExplanation -= ProviderOnCheckValidWithExplanation;
        Provider.onServerDisconnected -= ProviderOnServerDisconnected;
        Provider.onServerConnected -= ProviderOnServerConnected;
        BarricadeManager.onBarricadeSpawned += BarricadeManagerOnBarricadeSpawned;
        BarricadeManager.onDeployBarricadeRequested += BarricadeManagerOnDeployBarricadeRequested;
        InteractableVehicle.OnPassengerChangedSeats_Global -= InteractableVehicleOnPassengerChangedSeats;
        InteractableVehicle.OnPassengerRemoved_Global -= InteractableVehicleOnPassengerRemoved;
        InteractableVehicle.OnPassengerAdded_Global -= InteractableVehicleOnPassengerAdded;
        VehicleManager.OnVehicleExploded -= VehicleManagerOnVehicleExploded;
        VehicleManager.onSwapSeatRequested -= VehicleManagerOnSwapSeatRequested;
        VehicleManager.onEnterVehicleRequested -= VehicleManagerOnEnterVehicleRequested;
        VehicleManager.onExitVehicleRequested -= VehicleManagerOnExitVehicleRequested;
    }
    private static void TryInvoke<TState>(EventDelegate<TState> @delegate, TState request, string name) where TState : EventState
    {
        try
        {
            @delegate.Invoke(request);
        }
        catch (Exception ex)
        {
            try
            {
                MethodInfo? i = @delegate.Method;
                if (i is not null)
                    name = i.Name;
            }
            catch (MemberAccessException) { }
            L.LogError("EventDispatcher ran into an error invoking: " + name, method: name);
            L.LogError(ex, method: name);
        }
    }
    private static void VehicleManagerOnExitVehicleRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow, ref Vector3 pendingLocation, ref float pendingYaw)
    {
        if (ExitVehicleRequested == null || !shouldAllow) return;
        ExitVehicleRequested request = new ExitVehicleRequested(player, vehicle, shouldAllow, pendingLocation, pendingYaw);
        foreach (EventDelegate<ExitVehicleRequested> inv in ExitVehicleRequested.GetInvocationList().Cast<EventDelegate<ExitVehicleRequested>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(ExitVehicleRequested));
        }
        if (!request.CanContinue)
            shouldAllow = false;
        else
        {
            pendingLocation = request.ExitLocation;
            pendingYaw = request.ExitLocationYaw;
        }
    }
    private static void VehicleManagerOnEnterVehicleRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow)
    {
        if (EnterVehicleRequested == null || !shouldAllow || vehicle == null || player == null) return;
        EnterVehicleRequested request = new EnterVehicleRequested(player, vehicle, shouldAllow);
        foreach (EventDelegate<EnterVehicleRequested> inv in EnterVehicleRequested.GetInvocationList().Cast<EventDelegate<EnterVehicleRequested>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(EnterVehicleRequested));
        }
        if (!request.CanContinue)
            shouldAllow = false;
    }
    private static void VehicleManagerOnSwapSeatRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow, byte fromSeatIndex, ref byte toSeatIndex)
    {
        if (VehicleSwapSeatRequested == null || !shouldAllow) return;
        VehicleSwapSeatRequested request = new VehicleSwapSeatRequested(player, vehicle, shouldAllow, fromSeatIndex, toSeatIndex);
        foreach (EventDelegate<VehicleSwapSeatRequested> inv in VehicleSwapSeatRequested.GetInvocationList().Cast<EventDelegate<VehicleSwapSeatRequested>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(VehicleSwapSeatRequested));
        }
        if (!request.CanContinue) shouldAllow = false;
        else toSeatIndex = request.FinalSeat;
    }
    private static void VehicleManagerOnVehicleExploded(InteractableVehicle vehicle)
    {
        SpottedComponent spotted = vehicle.transform.GetComponent<SpottedComponent>();

        if (vehicle.gameObject.TryGetComponent(out BuiltBuildableComponent comp))
            UnityEngine.Object.Destroy(comp);

        if (VehicleDestroyed != null)
        {
            VehicleDestroyed request = new VehicleDestroyed(vehicle, spotted);
            foreach (EventDelegate<VehicleDestroyed> inv in VehicleDestroyed.GetInvocationList().Cast<EventDelegate<VehicleDestroyed>>())
            {
                if (!request.CanContinue) break;
                TryInvoke(inv, request, nameof(VehicleDestroyed));
            }
        }
        if (spotted != null)
            UnityEngine.Object.Destroy(spotted);
    }
    private static void InteractableVehicleOnPassengerChangedSeats(InteractableVehicle vehicle, int fromSeatIndex, int toSeatIndex)
    {
        if (VehicleSwapSeat == null) return;
        Passenger? pl = vehicle.passengers[toSeatIndex];
        if (pl is null || pl.player is null || pl.player.player == null) return;
        UCPlayer? pl2 = UCPlayer.FromPlayer(pl.player.player);
        if (pl2 is null) return;
        VehicleSwapSeat request = new VehicleSwapSeat(pl2, vehicle, (byte)fromSeatIndex, (byte)toSeatIndex);
        foreach (EventDelegate<VehicleSwapSeat> inv in VehicleSwapSeat.GetInvocationList().Cast<EventDelegate<VehicleSwapSeat>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(VehicleSwapSeat));
        }
    }
    private static void InteractableVehicleOnPassengerRemoved(InteractableVehicle vehicle, int seatIndex, Player player)
    {
        if (ExitVehicle == null || player == null) return;
        UCPlayer? pl2 = UCPlayer.FromPlayer(player);
        if (pl2 is null) return;
        ExitVehicle request = new ExitVehicle(pl2, vehicle, (byte)seatIndex);
        foreach (EventDelegate<ExitVehicle> inv in ExitVehicle.GetInvocationList().Cast<EventDelegate<ExitVehicle>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(ExitVehicle));
        }
    }
    private static void InteractableVehicleOnPassengerAdded(InteractableVehicle vehicle, int seatIndex)
    {
        if (EnterVehicle == null || vehicle == null) return;
        Passenger? pl = vehicle.passengers[seatIndex];
        if (pl is null || pl.player is null || pl.player.player == null) return;
        UCPlayer? pl2 = UCPlayer.FromPlayer(pl.player.player);
        if (pl2 is null) return;
        EnterVehicle request = new EnterVehicle(pl2, vehicle, (byte)seatIndex);
        foreach (EventDelegate<EnterVehicle> inv in EnterVehicle.GetInvocationList().Cast<EventDelegate<EnterVehicle>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(EnterVehicle));
        }
    }
    internal static void InvokeOnVehicleSpawned(InteractableVehicle result)
    {
        if (VehicleSpawned == null) return;
        VehicleSpawned args = new VehicleSpawned(result);
        foreach (EventDelegate<VehicleSpawned> inv in VehicleSpawned.GetInvocationList().Cast<EventDelegate<VehicleSpawned>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(VehicleSpawned));
        }
    }
    internal static void InvokeOnBarricadeDestroyed(BarricadeDrop barricade, BarricadeData barricadeData, BarricadeRegion region, byte x, byte y, ushort plant)
    {
        if (BarricadeDestroyed == null) return;
        UCPlayer? instigator = barricade.model.TryGetComponent(out BarricadeComponent component) ? UCPlayer.FromID(component.LastDamager) : null;
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        SqlItem<SavedStructure>? barricadeSave = saver?.GetSaveItemSync(barricade.instanceID, EStructType.BARRICADE);

        BarricadeDestroyed args = new BarricadeDestroyed(instigator, barricade, barricadeData, region, x, y, plant, barricadeSave);
        foreach (EventDelegate<BarricadeDestroyed> inv in BarricadeDestroyed.GetInvocationList().Cast<EventDelegate<BarricadeDestroyed>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(BarricadeDestroyed));
        }
    }
    internal static void InvokeOnPlayerDied(PlayerDied e)
    {
        if (PlayerDied == null) return;
        foreach (EventDelegate<PlayerDied> inv in PlayerDied.GetInvocationList().Cast<EventDelegate<PlayerDied>>())
        {
            if (!e.CanContinue) break;
            TryInvoke(inv, e, nameof(PlayerDied));
        }
        e.ActiveVehicle = null;
    }
    private static void ProviderOnServerDisconnected(CSteamID steamID)
    {
        if (PlayerLeaving == null) return;
        UCPlayer? player = UCPlayer.FromCSteamID(steamID);
        if (player is null) return;
        player._isLeaving = true;
        PlayerEvent args = new PlayerEvent(player);
        foreach (EventDelegate<PlayerEvent> inv in PlayerLeaving.GetInvocationList().Cast<EventDelegate<PlayerEvent>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(PlayerLeaving));
        }
        try
        {
            PlayerManager.InvokePlayerDisconnected(player);
        }
        catch (Exception ex)
        {
            L.LogError("Failed to remove a player from player manager.");
            L.LogError(ex);
        }
    }
    private static void ProviderOnServerConnected(CSteamID steamID)
    {
        if (PlayerJoined == null) return;
        UCPlayer player;
        try
        {
            Player pl = PlayerTool.getPlayer(steamID);
            if (pl is null)
                goto error;
            player = PlayerManager.InvokePlayerConnected(pl);
            if (player is null)
                goto error;
        }
        catch (Exception ex)
        {
            L.LogError("Error in EventDispatcher.ProviderOnServerConnected loading player into OnlinePlayers:");
            L.LogError(ex);
            goto error;
        }
        PlayerSave.TryReadSaveFile(steamID.m_SteamID, out PlayerSave? save);
        PlayerJoined args = new PlayerJoined(player, save);
        foreach (EventDelegate<PlayerJoined> inv in PlayerJoined.GetInvocationList().Cast<EventDelegate<PlayerJoined>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(PlayerJoined));
        }
        return;
    error:
        Provider.kick(steamID, "There was a fatal error connecting you to the server.");
    }
    private static void ProviderOnCheckValidWithExplanation(ValidateAuthTicketResponse_t callback, ref bool isValid, ref string explanation)
    {
        if (PlayerPending == null || !isValid) return;
        SteamPending? pending = null;
        for (int i = 0; i < Provider.pending.Count; ++i)
        {
            if (Provider.pending[i].playerID.steamID.m_SteamID == callback.m_SteamID.m_SteamID)
                pending = Provider.pending[i];
        }
        if (pending is null) return;
        PlayerSave.TryReadSaveFile(callback.m_SteamID.m_SteamID, out PlayerSave? save);
        PlayerPending args = new PlayerPending(pending, save, isValid, explanation);
        foreach (EventDelegate<PlayerPending> inv in PlayerPending.GetInvocationList().Cast<EventDelegate<PlayerPending>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(PlayerPending));
        }
        if (!args.CanContinue)
        {
            isValid = false;
            explanation = args.RejectReason;
        }
    }
    private static void ProviderOnBattlEyeKick(SteamPlayer client, string reason)
    {
        if (PlayerBattlEyeKicked == null) return;
        UCPlayer? player = UCPlayer.FromSteamPlayer(client);
        if (player is null) return;
        BattlEyeKicked args = new BattlEyeKicked(player, reason);
        foreach (EventDelegate<BattlEyeKicked> inv in PlayerBattlEyeKicked.GetInvocationList().Cast<EventDelegate<BattlEyeKicked>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(PlayerBattlEyeKicked));
        }
    }
    private static void BarricadeManagerOnDeployBarricadeRequested(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point, ref float angleX, ref float angleY, ref float angleZ, ref ulong owner, ref ulong group, ref bool shouldAllow)
    {
        if (BarricadePlaceRequested == null || !shouldAllow) return;
        UCPlayer? player = UCPlayer.FromID(owner);
        InteractableVehicle? vehicle = null;
        if (hit != null && hit.CompareTag("Vehicle"))
            vehicle = BarricadeManager.FindVehicleRegionByTransform(hit)?.vehicle;
        Vector3 rotation = new Vector3(angleX, angleY, angleZ);
        PlaceBarricadeRequested args = new PlaceBarricadeRequested(player, vehicle, barricade, asset, hit, point, rotation, owner, group, shouldAllow);
        foreach (EventDelegate<PlaceBarricadeRequested> inv in BarricadePlaceRequested.GetInvocationList().Cast<EventDelegate<PlaceBarricadeRequested>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(BarricadePlaceRequested));
        }
        if (!args.CanContinue)
            shouldAllow = false;
        else
        {
            point = args.Position;
            angleX = args.Rotation.x;
            angleY = args.Rotation.y;
            angleZ = args.Rotation.z;
            owner = args.Owner;
            group = args.GroupOwner;
        }
    }
    private static void BarricadeManagerOnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
    {
        if (BarricadePlaced == null) return;
        BarricadeData data = drop.GetServersideData();
        UCPlayer? owner = UCPlayer.FromID(data.owner);
        if (owner is null) return;
        BarricadePlaced args = new BarricadePlaced(owner, drop, data, region);
        foreach (EventDelegate<BarricadePlaced> inv in BarricadePlaced.GetInvocationList().Cast<EventDelegate<BarricadePlaced>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(BarricadePlaced));
        }
    }
    private static void UseableThrowableOnThrowableSpawned(UseableThrowable useable, GameObject throwable)
    {
        ThrowableComponent c = throwable.AddComponent<ThrowableComponent>();
        c.Throwable = useable.equippedThrowableAsset.GUID;
        c.Owner = useable.player.channel.owner.playerID.steamID.m_SteamID;
        c.IsExplosive = useable.equippedThrowableAsset.isExplosive;
        if (ThrowableSpawned == null) return;
        UCPlayer? owner = UCPlayer.FromPlayer(useable.player);
        if (owner is null) return;
        if (owner.Player.TryGetPlayerData(out UCPlayerData comp))
            comp.ActiveThrownItems.Add(c);
        ThrowableSpawned args = new ThrowableSpawned(owner, useable.equippedThrowableAsset, throwable);
        foreach (EventDelegate<ThrowableSpawned> inv in ThrowableSpawned.GetInvocationList().Cast<EventDelegate<ThrowableSpawned>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(ThrowableSpawned));
        }
    }
    internal static void InvokeOnThrowableDespawning(ThrowableComponent throwableComponent)
    {
        if (ThrowableDespawning == null) return;
        UCPlayer? owner = UCPlayer.FromID(throwableComponent.Owner);
        if (owner is null || Assets.find(throwableComponent.Throwable) is not ItemThrowableAsset asset) return;
        ThrowableSpawned args = new ThrowableSpawned(owner, asset, throwableComponent.gameObject);
        foreach (EventDelegate<ThrowableSpawned> inv in ThrowableDespawning.GetInvocationList().Cast<EventDelegate<ThrowableSpawned>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(ThrowableDespawning));
        }
        owner.Player.StartCoroutine(ThrowableDespawnCoroutine(owner, throwableComponent.UnityInstanceID));
    }
    private static IEnumerator ThrowableDespawnCoroutine(UCPlayer player, int instId)
    {
        yield return null;
        if (player.IsOnline && player.Player.TryGetPlayerData(out UCPlayerData component))
        {
            for (int i = component.ActiveThrownItems.Count - 1; i >= 0; --i)
                if (component.ActiveThrownItems[i] == null || component.ActiveThrownItems[i].UnityInstanceID == instId)
                    component.ActiveThrownItems.RemoveAt(i);
        }
    }
    private static void ProjectileOnProjectileSpawned(UseableGun gun, GameObject projectile)
    {
        foreach (Rocket rocket in projectile.GetComponentsInChildren<Rocket>(true))
        {
            ProjectileComponent c = rocket.gameObject.AddComponent<ProjectileComponent>();

            c.GunID = gun.equippedGunAsset.GUID;
            c.Owner = gun.player.channel.owner.playerID.steamID.m_SteamID;
            if (ProjectileSpawned == null) return;
            UCPlayer? owner = UCPlayer.FromPlayer(gun.player);
            if (owner is null) return;
            ProjectileSpawned args = new ProjectileSpawned(owner, gun.equippedGunAsset, rocket.gameObject, rocket);
            foreach (EventDelegate<ProjectileSpawned> inv in ProjectileSpawned.GetInvocationList().Cast<EventDelegate<ProjectileSpawned>>())
            {
                if (!args.CanContinue) break;
                TryInvoke(inv, args, nameof(ProjectileSpawned));
            }
        }
    }
    internal static void InvokeOnProjectileExploded(ProjectileComponent projectileComponent, Collider other)
    {
        InteractableVehicle vehicle = other.GetComponentInParent<InteractableVehicle>();

        if (vehicle != null)
            VehicleDamageCalculator.RegisterForAdvancedDamage(vehicle, VehicleDamageCalculator.GetDamageMultiplier(projectileComponent, other));

        if (ProjectileExploded == null) return;
        UCPlayer? owner = UCPlayer.FromID(projectileComponent.Owner);
        if (Assets.find(projectileComponent.GunID) is not ItemGunAsset asset) return;
        ProjectileSpawned args = new ProjectileSpawned(owner, asset, projectileComponent.gameObject, projectileComponent.RocketComponent);

        foreach (EventDelegate<ProjectileSpawned> inv in ProjectileExploded.GetInvocationList().Cast<EventDelegate<ProjectileSpawned>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(ProjectileExploded));
        }
    }
    internal static void OnBulletHit(UseableGun gun, BulletInfo bullet, InputInfo input, ref bool shouldAllow)
    {
        //L.Log("     Normal: " + input.normal);
        //L.Log("     Bullet Forward: " + bullet.);

        if (input.vehicle != null)
            VehicleDamageCalculator.RegisterForAdvancedDamage(input.vehicle, VehicleDamageCalculator.GetDamageMultiplier(input));
    }
    internal static void InvokeOnLandmineExploding(UCPlayer? owner, BarricadeDrop barricade, InteractableTrap trap, UCPlayer triggerer, GameObject triggerObject, ref bool shouldExplode)
    {
        if (LandmineExploding == null || !shouldExplode) return;
        LandmineExploding request = new LandmineExploding(owner, barricade, trap, triggerer, triggerObject, shouldExplode);
        foreach (EventDelegate<LandmineExploding> inv in LandmineExploding.GetInvocationList().Cast<EventDelegate<LandmineExploding>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(LandmineExploding));
        }
        if (!request.CanContinue) shouldExplode = false;
    }
    private static void PlayerCraftingOnCraftRequested(PlayerCrafting crafting, ref ushort itemID, ref byte blueprintIndex, ref bool shouldAllow)
    {
        if (CraftRequested == null || !shouldAllow) return;
        UCPlayer? pl = UCPlayer.FromPlayer(crafting.player);
        if (pl == null || Assets.find(EAssetType.ITEM, itemID) is not ItemAsset asset || asset.blueprints.Count <= blueprintIndex)
            return;
        Blueprint bp = asset.blueprints[blueprintIndex];
        if (bp is null)
            return;
        CraftRequested request = new CraftRequested(pl, asset, bp, shouldAllow);
        foreach (EventDelegate<CraftRequested> inv in CraftRequested.GetInvocationList().Cast<EventDelegate<CraftRequested>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(CraftRequested));
        }
        itemID = request.ItemId;
        blueprintIndex = request.BlueprintIndex;
        if (!request.CanContinue) shouldAllow = false;
    }
    internal static void InvokeOnDropItemRequested(UCPlayer player, PlayerInventory inventory, Item item, ref bool shouldAllow)
    {
        if (ItemDropRequested == null || !shouldAllow) return;
        ItemJar? jar = null;
        byte pageNum = default, index = default;
        bool found = false;
        for (byte i = 0; i < PlayerInventory.PAGES; ++i)
        {
            SDG.Unturned.Items page = inventory.items[i];
            for (byte j = 0; j < page.items.Count; ++j)
            {
                if (page.items[j].item == item)
                {
                    jar = page.items[j];
                    pageNum = i;
                    index = j;
                    found = true;
                    break;
                }
            }
            if (found)
                break;
        }
        if (found)
        {
            ItemDropRequested request = new ItemDropRequested(player, item, jar!, pageNum, index, shouldAllow);
            foreach (EventDelegate<ItemDropRequested> inv in ItemDropRequested.GetInvocationList().Cast<EventDelegate<ItemDropRequested>>())
            {
                if (!request.CanContinue) break;
                TryInvoke(inv, request, nameof(ItemDropRequested));
            }
            if (!request.CanContinue) shouldAllow = false;
        }
    }
    internal static void InvokeOnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup)
    {
        if (GroupChanged == null || player is null) return;
        GroupChanged args = new GroupChanged(player, oldGroup, newGroup);
        foreach (EventDelegate<GroupChanged> inv in GroupChanged.GetInvocationList().Cast<EventDelegate<GroupChanged>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(GroupChanged));
        }
    }
    internal static void InvokeUIRefreshRequest(UCPlayer player)
    {
        if (UIRefreshRequested == null || player is null) return;
        PlayerEvent args = new PlayerEvent(player);
        foreach (EventDelegate<PlayerEvent> inv in UIRefreshRequested.GetInvocationList().Cast<EventDelegate<PlayerEvent>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(UIRefreshRequested));
        }
    }
    private static void OnPluginKeyTick(Player player, uint simulation, byte key, bool state)
    {
        if (key == 0)
            PlayerManager.FromID(player.channel.owner.playerID.steamID.m_SteamID)?.Keys.Simulate();
    }
    internal static void OnKeyDown(UCPlayer player, PlayerKey key, KeyDown? callback)
    {
        if (callback == null || !player.IsOnline) return;
        bool handled = false;
        foreach (KeyDown inv in callback.GetInvocationList().Cast<KeyDown>())
        {
            inv?.Invoke(player, ref handled);
            if (handled) break;
        }
    }
    internal static void OnKeyUp(UCPlayer player, PlayerKey key, float timeDown, KeyUp? callback)
    {
        if (callback == null || !player.IsOnline) return;
        bool handled = false;
        foreach (KeyUp inv in callback.GetInvocationList().Cast<KeyUp>())
        {
            inv?.Invoke(player, timeDown, ref handled);
            if (handled) break;
        }
    }
    private static void StructureDropOnSalvageRequested(StructureDrop structure, SteamPlayer instigatorClient, ref bool shouldAllow)
    {
        if (!shouldAllow) return;
        if (instigatorClient != null) DestroyerComponent.AddOrUpdate(structure.model.gameObject, instigatorClient.playerID.steamID.m_SteamID);
        else return;

        if (SalvageStructureRequested == null) return;
        UCPlayer? player = UCPlayer.FromSteamPlayer(instigatorClient);
        if (player == null) return;
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        SqlItem<SavedStructure>? save = saver?.GetSaveItemSync(structure.instanceID, EStructType.STRUCTURE);
        StructureRegion? region = null;
        if (Regions.tryGetCoordinate(structure.model.position, out byte x, out byte y))
            StructureManager.tryGetRegion(x, y, out region);

        SalvageStructureRequested args = new SalvageStructureRequested(player, structure, structure.GetServersideData(), region!, x, y, save);
        foreach (EventDelegate<SalvageStructureRequested> inv in SalvageStructureRequested.GetInvocationList().Cast<EventDelegate<SalvageStructureRequested>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(SalvageStructureRequested));
        }
        if (!args.CanContinue)
            shouldAllow = false;
    }
    private static void StructureManagerOnDamageStructureRequested(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (!shouldAllow) return;
        if (OffenseManager.IsValidSteam64ID(instigatorSteamID))
            DestroyerComponent.AddOrUpdate(structureTransform.gameObject, instigatorSteamID.m_SteamID);

        if (DamageStructureRequested == null) return;
        StructureDrop drop = StructureManager.FindStructureByRootTransform(structureTransform);
        if (drop == null) return;
        UCPlayer? player = UCPlayer.FromCSteamID(instigatorSteamID);
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        SqlItem<SavedStructure>? save = saver?.GetSaveItemSync(drop.instanceID, EStructType.STRUCTURE);
        StructureRegion? region = null;
        if (Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
            StructureManager.tryGetRegion(x, y, out region);

        DamageStructureRequested args = new DamageStructureRequested(player, drop, drop.GetServersideData(), region!, x, y, save, damageOrigin, pendingTotalDamage);
        foreach (EventDelegate<DamageStructureRequested> inv in DamageStructureRequested.GetInvocationList().Cast<EventDelegate<DamageStructureRequested>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(DamageStructureRequested));
        }
        if (!args.CanContinue)
            shouldAllow = false;
        else
            pendingTotalDamage = args.PendingDamage;
    }
    internal static void InvokeOnStructureDestroyed(StructureDrop drop, ulong instigator, Vector3 ragdoll, bool wasPickedUp)
    {
        if (StructureDestroyed == null) return;
        UCPlayer? player = UCPlayer.FromID(instigator);
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        SqlItem<SavedStructure>? save = saver?.GetSaveItemSync(drop.instanceID, EStructType.STRUCTURE);
        StructureRegion? region = null;
        if (Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
            StructureManager.tryGetRegion(x, y, out region);

        StructureDestroyed args = new StructureDestroyed(player, drop, drop.GetServersideData(), region!, x, y, save, ragdoll, wasPickedUp);
        foreach (EventDelegate<StructureDestroyed> inv in StructureDestroyed.GetInvocationList().Cast<EventDelegate<StructureDestroyed>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(StructureDestroyed));
        }
    }
}
public delegate void EventDelegate<in TState>(TState e) where TState : EventState;
