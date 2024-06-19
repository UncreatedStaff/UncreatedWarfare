using HarmonyLib;
using SDG.Framework.Debug;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Events;
public static class EventDispatcher
{
    private static readonly List<ISalvageInfo> WorkingSalvageInfo = new List<ISalvageInfo>(4);

    public static event EventDelegate<ExitVehicleRequested> ExitVehicleRequested;
    public static event EventDelegate<EnterVehicleRequested> EnterVehicleRequested;
    public static event EventDelegate<VehicleSwapSeatRequested> VehicleSwapSeatRequested;
    public static event EventDelegate<VehicleSpawned> VehicleSpawned;
    public static event EventDelegate<ExitVehicle> ExitVehicle;
    public static event EventDelegate<EnterVehicle> EnterVehicle;
    public static event EventDelegate<VehicleSwapSeat> VehicleSwapSeat;
    public static event EventDelegate<VehicleDestroyed> VehicleDestroyed;
    public static event EventDelegate<VehicleLockChangeRequested> VehicleLockChangeRequested;

    public static event EventDelegate<BarricadeDestroyed> BarricadeDestroyed;
    public static event EventDelegate<PlaceBarricadeRequested> BarricadePlaceRequested;
    public static event EventDelegate<SalvageBarricadeRequested> SalvageBarricadeRequested;
    public static event EventDelegate<BarricadePlaced> BarricadePlaced;
    public static event EventDelegate<LandmineExploding> LandmineExploding;
    public static event EventDelegate<SignTextChanged> SignTextChanged;

    public static event EventDelegate<StructureDestroyed> StructureDestroyed;
    public static event EventDelegate<SalvageStructureRequested> SalvageStructureRequested;
    public static event EventDelegate<DamageStructureRequested> DamageStructureRequested;

    public static event EventDelegate<ItemDropRequested> ItemDropRequested;
    public static event EventDelegate<ItemDropped> ItemDropped;
    public static event EventDelegate<InventoryItemRemoved> InventoryItemRemoved;
    public static event EventDelegate<ItemPickedUp> ItemPickedUp;
    public static event EventDelegate<CraftRequested> CraftRequested;
    public static event EventDelegate<ItemMoveRequested> ItemMoveRequested;
    public static event EventDelegate<ItemMoved> ItemMoved;
    public static event EventDelegate<SwapClothingRequested> SwapClothingRequested;

    public static event EventDelegate<ThrowableSpawned> ThrowableSpawned;
    public static event EventDelegate<ThrowableSpawned> ThrowableDespawning;

    public static event EventDelegate<ProjectileSpawned> ProjectileSpawned;
    public static event EventDelegate<ProjectileSpawned> ProjectileExploded;

    public static event EventDelegate<PlayerPending> PlayerPending;
    public static event AsyncEventDelegate<PlayerPending> PlayerPendingAsync;
    public static event EventDelegate<PlayerJoined> PlayerJoined;
    public static event EventDelegate<PlayerEvent> PlayerLeaving;
    public static event EventDelegate<PlayerEvent> PlayerLeft;
    public static event EventDelegate<BattlEyeKicked> PlayerBattlEyeKicked;
    public static event EventDelegate<PlayerDied> PlayerDied;
    public static event EventDelegate<GroupChanged> GroupChanged;
    public static event EventDelegate<PlayerInjured> PlayerInjuring;
    public static event EventDelegate<PlayerEvent> PlayerInjured;
    public static event EventDelegate<PlayerAided> PlayerAidRequested;
    public static event EventDelegate<PlayerAided> PlayerAided;
    public static event EventDelegate<PlayerEvent> UIRefreshRequested;

    internal static void SubscribeToAll()
    {
        EventPatches.TryPatchAll();
        VehicleManager.onExitVehicleRequested += VehicleManagerOnExitVehicleRequested;
        VehicleManager.onEnterVehicleRequested += InvokeVehicleManagerOnEnterVehicleRequested;
        VehicleManager.onSwapSeatRequested += InvokeVehicleManagerOnSwapSeatRequested;
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
        BarricadeDrop.OnSalvageRequested_Global += BarricadeDropOnSalvageRequested;
        StructureManager.onDamageStructureRequested += StructureManagerOnDamageStructureRequested;
        PlayerQuests.onGroupChanged += PlayerQuestsOnGroupChanged;
        VehicleManager.OnToggleVehicleLockRequested += VehicleManagerOnOnToggleVehicleLockRequested;
        EventPatches.OnStartVerifying += IntlOnStartVerifyingPlayerConnection;
    }
    internal static void UnsubscribeFromAll()
    {
        EventPatches.OnStartVerifying -= IntlOnStartVerifyingPlayerConnection;
        VehicleManager.OnToggleVehicleLockRequested -= VehicleManagerOnOnToggleVehicleLockRequested;
        PlayerQuests.onGroupChanged -= PlayerQuestsOnGroupChanged;
        StructureManager.onDamageStructureRequested -= StructureManagerOnDamageStructureRequested;
        StructureDrop.OnSalvageRequested_Global -= StructureDropOnSalvageRequested;
        BarricadeDrop.OnSalvageRequested_Global -= BarricadeDropOnSalvageRequested;
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
        VehicleManager.onSwapSeatRequested -= InvokeVehicleManagerOnSwapSeatRequested;
        VehicleManager.onEnterVehicleRequested -= InvokeVehicleManagerOnEnterVehicleRequested;
        VehicleManager.onExitVehicleRequested -= VehicleManagerOnExitVehicleRequested;
    }
    private static void TryInvoke<TState>(EventDelegate<TState> @delegate, TState request, string name) where TState : EventState
    {
        try
        {
            @delegate.Invoke(request);
        }
        catch (ControlException) { }
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
    private static async Task TryInvoke<TState>(AsyncEventDelegate<TState> @delegate, TState request, string name, CancellationToken token = default, bool mainThread = true) where TState : EventState
    {
        try
        {
            if (mainThread && !UCWarfare.IsMainThread)
            {
                await UniTask.SwitchToMainThread(token);
                ThreadUtil.assertIsGameThread();
            }

            await @delegate.Invoke(request, token).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (ControlException) { }
        catch (Exception ex)
        {
            try
            {
                MethodInfo? i = @delegate.Method;
                if (i is not null)
                    name = i.FullDescription();
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
    internal static void InvokeVehicleManagerOnEnterVehicleRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow)
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
    internal static void InvokeVehicleManagerOnSwapSeatRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow, byte fromSeatIndex, ref byte toSeatIndex)
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

        VehicleDestroyed request = new VehicleDestroyed(vehicle, spotted);
        if (request.Instigator != null && request.Instigator.Player.TryGetPlayerData(out UCPlayerData data))
            data.LastExplodedVehicle = request.Vehicle.asset.GUID;
        if (VehicleDestroyed != null)
        {
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
    private static void VehicleManagerOnOnToggleVehicleLockRequested(InteractableVehicle vehicle, ref bool shouldallow)
    {
        if (VehicleLockChangeRequested == null || vehicle == null || !vehicle.isDriven) return;
        UCPlayer? pl2 = UCPlayer.FromSteamPlayer(vehicle.passengers[0].player);
        if (pl2 is null) return;
        VehicleLockChangeRequested request = new VehicleLockChangeRequested(pl2, vehicle, shouldallow);
        foreach (EventDelegate<VehicleLockChangeRequested> inv in VehicleLockChangeRequested.GetInvocationList().Cast<EventDelegate<VehicleLockChangeRequested>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(VehicleLockChangeRequested));
        }

        if (!request.CanContinue)
            shouldallow = false;
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
    internal static void InvokeOnBarricadeDestroyed(BarricadeDrop barricade, BarricadeData barricadeData, ulong instigator, BarricadeRegion region, byte x, byte y, ushort plant, EDamageOrigin origin)
    {
        UCPlayer? player = UCPlayer.FromID(instigator);
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        SqlItem<SavedStructure>? barricadeSave = saver?.GetSaveItemSync(barricade.instanceID, StructType.Barricade);
        // todo add item tracking
        BarricadeDestroyed args = new BarricadeDestroyed(player, instigator, barricade, barricadeData, region, x, y, plant, barricadeSave, origin, default, default);

        if (barricade.model.TryGetComponent(out ShovelableComponent shovelableComponent))
            shovelableComponent.DestroyInfo = args;

        if (BarricadeDestroyed == null || Data.Gamemode is not { State: State.Active or State.Staging })
            return;
        foreach (EventDelegate<BarricadeDestroyed> inv in BarricadeDestroyed.GetInvocationList().Cast<EventDelegate<BarricadeDestroyed>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(BarricadeDestroyed));
        }
    }
    internal static void InvokeOnPlayerDied(PlayerDied e)
    {
        if (PlayerDied == null) return;
        if (e.Player.Player.TryGetPlayerData(out UCPlayerData data))
            data.CancelDeployment();
        
        foreach (EventDelegate<PlayerDied> inv in PlayerDied.GetInvocationList().Cast<EventDelegate<PlayerDied>>())
        {
            if (!e.CanContinue) break;
            TryInvoke(inv, e, nameof(PlayerDied));
        }
        e.ActiveVehicle = null;
    }
    private static void ProviderOnServerDisconnected(CSteamID steamID)
    {
        UCPlayer? player = UCPlayer.FromCSteamID(steamID);
        if (player is null) return;
        lock (player)
            player.IsLeaving = true;
        PlayerEvent args = new PlayerEvent(player);
        if (PlayerLeaving != null)
        {
            foreach (EventDelegate<PlayerEvent> inv in PlayerLeaving.GetInvocationList().Cast<EventDelegate<PlayerEvent>>())
            {
                if (!args.CanContinue) break;
                TryInvoke(inv, args, nameof(PlayerLeaving));
            }
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
        
        if (PlayerLeft == null) return;
        foreach (EventDelegate<PlayerEvent> inv in PlayerLeft.GetInvocationList().Cast<EventDelegate<PlayerEvent>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(PlayerLeft));
        }

        GC.Collect(2, GCCollectionMode.Optimized, false, false);
    }

    private static List<PendingAsyncData> _pendingAsyncData = new InspectableList<PendingAsyncData>(4);
    private static void ProviderOnServerConnected(CSteamID steamID)
    {
        if (PlayerJoined == null) return;
        UCPlayer player;
        bool newPlayer;
        try
        {
            Player pl = PlayerTool.getPlayer(steamID);
            if (pl is null)
                goto error;
            int index = _pendingAsyncData.FindIndex(x => x.Steam64 == steamID.m_SteamID);
            if (index == -1)
            {
                Provider.kick(steamID, "Unable to find your async data.");
                return;
            }

            PendingAsyncData data = _pendingAsyncData[index];

            _pendingAsyncData.RemoveAt(index);
            _pendingAsyncData.RemoveAll(x => !Provider.pending.Exists(y => y.playerID.steamID.m_SteamID == x.Steam64));

            player = PlayerManager.InvokePlayerConnected(pl, data, out newPlayer);


            if (player is null)
                goto error;
        }
        catch (Exception ex)
        {
            L.LogError("Error in EventDispatcher.ProviderOnServerConnected loading player into OnlinePlayers:");
            L.LogError(ex);
            goto error;
        }
        PlayerJoined args = new PlayerJoined(player, newPlayer);
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
            if (Provider.pending[i].playerID.steamID.m_SteamID != callback.m_SteamID.m_SteamID)
                continue;
            
            pending = Provider.pending[i];
            break;
        }
        if (pending is null) return;
        PlayerSave.TryReadSaveFile(callback.m_SteamID.m_SteamID, out PlayerSave? save);
        PlayerPending args = new PlayerPending(pending, save, null, isValid, explanation);
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

            c.GunId = gun.equippedGunAsset.GUID;
            c.Owner = gun.player.channel.owner.playerID.steamID.m_SteamID;
            if (ProjectileSpawned == null) continue;
            UCPlayer? owner = UCPlayer.FromPlayer(gun.player);
            if (owner is null) continue;
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
            VehicleDamageCalculator.RegisterForAdvancedDamage(vehicle, VehicleDamageCalculator.GetComponentDamageMultiplier(projectileComponent, other));

        if (ProjectileExploded == null) return;
        UCPlayer? owner = UCPlayer.FromID(projectileComponent.Owner);
        if (Assets.find(projectileComponent.GunId) is not ItemGunAsset asset) return;
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
            VehicleDamageCalculator.RegisterForAdvancedDamage(input.vehicle, VehicleDamageCalculator.GetComponentDamageMultiplier(input));
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
    private static void PlayerQuestsOnGroupChanged(PlayerQuests sender, CSteamID oldgroupid, EPlayerGroupRank oldgrouprank, CSteamID newgroupid, EPlayerGroupRank newgrouprank)
    {
        if (GroupChanged == null || sender == null || UCPlayer.FromPlayer(sender.player) is not { IsOnline: true } player) return;
        GroupChanged args = new GroupChanged(player, oldgroupid.m_SteamID, oldgrouprank, newgroupid.m_SteamID, newgrouprank);
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
        if (instigatorClient != null) DestroyerComponent.AddOrUpdate(structure.model.gameObject, instigatorClient.playerID.steamID.m_SteamID, EDamageOrigin.Unknown);
        else return;

        if (SalvageStructureRequested == null) return;
        UCPlayer? player = UCPlayer.FromSteamPlayer(instigatorClient);
        if (player == null) return;
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        SqlItem<SavedStructure>? save = saver?.GetSaveItemSync(structure.instanceID, StructType.Structure);
        if (!StructureManager.tryGetRegion(structure.model, out byte x, out byte y, out StructureRegion region))
            return;
        SalvageStructureRequested args = new SalvageStructureRequested(player, structure, structure.GetServersideData(), region!, x, y, save, default, default);
        structure.model.GetComponents(WorkingSalvageInfo);
        try
        {
            for (int i = 0; i < WorkingSalvageInfo.Count; ++i)
            {
                ISalvageInfo salvageInfo = WorkingSalvageInfo[i];
                salvageInfo.Salvager = instigatorClient.playerID.steamID.m_SteamID;
                salvageInfo.IsSalvaged = true;
                if (salvageInfo is ISalvageListener listener)
                {
                    listener.OnSalvageRequested(args);
                    if (!args.CanContinue)
                    {
                        shouldAllow = false;
                        break;
                    }

                }
            }
            if (args.CanContinue)
            {
                foreach (EventDelegate<SalvageStructureRequested> inv in SalvageStructureRequested.GetInvocationList().Cast<EventDelegate<SalvageStructureRequested>>())
                {
                    if (!args.CanContinue) break;
                    TryInvoke(inv, args, nameof(SalvageStructureRequested));
                }
                if (!args.CanContinue)
                    shouldAllow = false;
            }
        }
        finally
        {
            try
            {
                if (!shouldAllow)
                {
                    for (int i = 0; i < WorkingSalvageInfo.Count; ++i)
                    {
                        ISalvageInfo salvageInfo = WorkingSalvageInfo[i];
                        salvageInfo.Salvager = instigatorClient.playerID.steamID.m_SteamID;
                        salvageInfo.IsSalvaged = false;
                    }
                }
            }
            finally
            {
                WorkingSalvageInfo.Clear();
            }
        }
    }
    private static void BarricadeDropOnSalvageRequested(BarricadeDrop barricade, SteamPlayer instigatorClient, ref bool shouldAllow)
    {
        if (!shouldAllow) return;
        if (instigatorClient != null)
            DestroyerComponent.AddOrUpdate(barricade.model.gameObject, instigatorClient.playerID.steamID.m_SteamID, EDamageOrigin.Unknown);
        else
        {
            DestroyerComponent.AddOrUpdate(barricade.model.gameObject, 0ul, EDamageOrigin.Unknown);
            return;
        }

        if (SalvageBarricadeRequested == null) return;
        UCPlayer? player = UCPlayer.FromSteamPlayer(instigatorClient);
        if (player == null) return;
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        SqlItem<SavedStructure>? save = saver?.GetSaveItemSync(barricade.instanceID, StructType.Barricade);
        if (!BarricadeManager.tryGetRegion(barricade.model, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
            return;
        SalvageBarricadeRequested args = new SalvageBarricadeRequested(player, barricade, barricade.GetServersideData(), region!, x, y, plant, save, default, default);
        barricade.model.GetComponents(WorkingSalvageInfo);
        try
        {
            for (int i = 0; i < WorkingSalvageInfo.Count; ++i)
            {
                ISalvageInfo salvageInfo = WorkingSalvageInfo[i];
                salvageInfo.Salvager = instigatorClient.playerID.steamID.m_SteamID;
                salvageInfo.IsSalvaged = true;
                if (salvageInfo is ISalvageListener listener)
                {
                    listener.OnSalvageRequested(args);
                    if (!args.CanContinue)
                    {
                        shouldAllow = false;
                        break;
                    }
                }
            }
            if (args.CanContinue)
            {
                foreach (EventDelegate<SalvageBarricadeRequested> inv in SalvageBarricadeRequested.GetInvocationList().Cast<EventDelegate<SalvageBarricadeRequested>>())
                {
                    if (!args.CanContinue) break;
                    TryInvoke(inv, args, nameof(SalvageBarricadeRequested));
                }
                if (!args.CanContinue)
                    shouldAllow = false;
            }
        }
        finally
        {
            try
            {
                if (!shouldAllow)
                {
                    for (int i = 0; i < WorkingSalvageInfo.Count; ++i)
                    {
                        ISalvageInfo salvageInfo = WorkingSalvageInfo[i];
                        salvageInfo.Salvager = instigatorClient.playerID.steamID.m_SteamID;
                        salvageInfo.IsSalvaged = false;
                    }
                }
            }
            finally
            {
                WorkingSalvageInfo.Clear();
            }
        }
    }
    private static void StructureManagerOnDamageStructureRequested(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (!shouldAllow) return;
        try
        {
            if (DamageStructureRequested == null) return;
            StructureDrop drop = StructureManager.FindStructureByRootTransform(structureTransform);
            if (drop == null) return;
            UCPlayer? player = UCPlayer.FromCSteamID(instigatorSteamID);
            StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
            SqlItem<SavedStructure>? save = saver?.GetSaveItemSync(drop.instanceID, StructType.Structure);
            if (!StructureManager.tryGetRegion(structureTransform, out byte x, out byte y, out StructureRegion region))
                return;

            // todo add item tracking
            DamageStructureRequested args = new DamageStructureRequested(player, instigatorSteamID.m_SteamID, drop, drop.GetServersideData(), region!, x, y, save, damageOrigin, pendingTotalDamage, default, default);
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
        finally
        {
            if (shouldAllow)
            {
                if (OffenseManager.IsValidSteam64Id(instigatorSteamID))
                    DestroyerComponent.AddOrUpdate(structureTransform.gameObject, instigatorSteamID.m_SteamID, damageOrigin);
                else
                    DestroyerComponent.AddOrUpdate(structureTransform.gameObject, 0ul, EDamageOrigin.Unknown);
            }
        }

    }

    private static readonly List<IManualOnDestroy> WorkingOnDestroy = new List<IManualOnDestroy>(2);
    internal static void InvokeOnStructureDestroyed(StructureDrop drop, ulong instigator, Vector3 ragdoll, bool wasPickedUp, StructureRegion region, byte x, byte y, EDamageOrigin origin)
    {
        UCPlayer? player = UCPlayer.FromID(instigator);
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        SqlItem<SavedStructure>? save = saver?.GetSaveItemSync(drop.instanceID, StructType.Structure);

        // todo add item tracking
        StructureDestroyed args = new StructureDestroyed(player, instigator, drop, drop.GetServersideData(), region, x, y, save, ragdoll, wasPickedUp, origin, default, default);

        if (drop.model.TryGetComponent(out ShovelableComponent shovelableComponent))
            shovelableComponent.DestroyInfo = args;

        if (StructureDestroyed != null)
        {
            foreach (EventDelegate<StructureDestroyed> inv in StructureDestroyed.GetInvocationList().Cast<EventDelegate<StructureDestroyed>>())
            {
                if (!args.CanContinue) break;
                TryInvoke(inv, args, nameof(StructureDestroyed));
            }
        }

        drop.model.GetComponents(WorkingOnDestroy);
        try
        {
            for (int i = 0; i < WorkingOnDestroy.Count; ++i)
                WorkingOnDestroy[i].ManualOnDestroy();
        }
        finally
        {
            WorkingOnDestroy.Clear();
        }
    }
    internal static void InvokeOnInjuringPlayer(PlayerInjuring args)
    {
        if (PlayerInjuring == null) return;
        foreach (EventDelegate<PlayerInjuring> inv in PlayerInjuring.GetInvocationList().Cast<EventDelegate<PlayerInjuring>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(PlayerInjuring));
        }
    }
    internal static void InvokeOnPlayerAided(UCPlayer medic, UCPlayer player, ItemConsumeableAsset asset, bool isRevive, ref bool shouldAllow)
    {
        if (!shouldAllow || PlayerAidRequested == null && PlayerAided == null)
            return;

        PlayerAided args = new PlayerAided(medic, player, asset, isRevive, shouldAllow);
        if (PlayerAidRequested != null)
        {
            foreach (EventDelegate<PlayerAided> inv in PlayerAidRequested.GetInvocationList().Cast<EventDelegate<PlayerAided>>())
            {
                if (!args.CanContinue) break;
                TryInvoke(inv, args, nameof(PlayerAidRequested));
            }

            if (!args.CanContinue)
            {
                shouldAllow = false;
                return;
            }
        }

        foreach (EventDelegate<PlayerAided> inv in PlayerAided.GetInvocationList().Cast<EventDelegate<PlayerAided>>())
        {
            TryInvoke(inv, args, nameof(PlayerAided));
        }
    }
    internal static void InvokeOnPlayerInjured(PlayerInjured args)
    {
        if (PlayerInjured == null) return;
        foreach (EventDelegate<PlayerInjured> inv in PlayerInjured.GetInvocationList().Cast<EventDelegate<PlayerInjured>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(PlayerInjured));
        }
    }
    internal static void IntlOnStartVerifyingPlayerConnection(SteamPending player, ref bool shouldDeferContinuation)
    {
        if (PlayerPendingAsync == null)
            return;

        ulong s64 = player.playerID.steamID.m_SteamID;
        PlayerSave.TryReadSaveFile(s64, out PlayerSave? save);

        PendingAsyncData data = new PendingAsyncData(player);
        CancellationTokenSource? src = null;

        for (int i = 0; i < PlayerManager.PlayerConnectCancellationTokenSources.Count; ++i)
        {
            KeyValuePair<ulong, CancellationTokenSource> kvp = PlayerManager.PlayerConnectCancellationTokenSources[i];
            if (kvp.Key == s64)
            {
                src = kvp.Value;
                break;
            }
        }

        PlayerPending args = new PlayerPending(player, save, data, true, string.Empty);
        Task task = InvokePrePlayerConnectAsync(args, src == null ? CancellationToken.None : src.Token);
        if (task.IsCompleted)
        {
            if (args.CanContinue)
                return;

            EventPatches.RemovePlayer(player, args.Rejection, args.RejectReason);
            return;
        }

        UCWarfare.RunTask(task, ctx: "Player connecting: {" + player.playerID.steamID.m_SteamID.ToString(Data.AdminLocale) + "} [" + player.playerID.playerName + "].");
        shouldDeferContinuation = true;
    }
    private static async Task InvokePrePlayerConnectAsync(PlayerPending args, CancellationToken token = default)
    {
        await UCWarfare.I.PlayerJoinLock.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (PlayerPendingAsync == null)
                return;

            foreach (AsyncEventDelegate<PlayerPending> inv in PlayerPendingAsync.GetInvocationList().Cast<AsyncEventDelegate<PlayerPending>>())
            {
                if (!args.CanContinue)
                    break;

                await TryInvoke(inv, args, nameof(PlayerPendingAsync), token).ConfigureAwait(true);
            }

            await UniTask.SwitchToMainThread(token);

            if (args.CanContinue)
            {
                _pendingAsyncData.Add(args.AsyncData);
                EventPatches.ContinueSendingVerifyPacket(args.PendingPlayer);
            }
            else
                EventPatches.RemovePlayer(args.PendingPlayer, args.Rejection, args.RejectReason ?? "An unknown error occured.");
        }
        catch (Exception ex)
        {
            L.LogError(ex);
            await UniTask.SwitchToMainThread(token);
            EventPatches.ContinueSendingVerifyPacket(args.PendingPlayer);
        }
        finally
        {
            UCWarfare.I.PlayerJoinLock.Release();
        }
    }
    internal static void InvokeOnSignTextChanged(InteractableSign sign)
    {
        if (SignTextChanged == null) return;
        BarricadeDrop? drop = UCBarricadeManager.GetSignFromInteractable(sign);
        if (drop == null)
            return;
        UCPlayer? player = null;
        if (drop.model.TryGetComponent(out BarricadeComponent comp) && comp.EditTick >= UCWarfare.I.Debugger.Updates)
            player = UCPlayer.FromID(comp.LastEditor);
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        SqlItem<SavedStructure>? save = saver?.GetSaveItemSync(drop.instanceID, StructType.Structure);
        BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out BarricadeRegion region);
        SignTextChanged args = new SignTextChanged(player, drop, region, x, y, plant, save);
        foreach (EventDelegate<SignTextChanged> inv in SignTextChanged.GetInvocationList().Cast<EventDelegate<SignTextChanged>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(SignTextChanged));
        }
    }
    internal static bool OnDraggingOrSwappingItem(PlayerInventory playerInv, byte pageFrom, ref byte pageTo, byte xFrom, ref byte xTo, byte yFrom, ref byte yTo, ref byte rotTo, bool swap)
    {
        if (ItemMoveRequested != null && UCPlayer.FromPlayer(playerInv.player) is { IsOnline: true } pl)
        {
            if (pageTo < PlayerInventory.SLOTS)
                rotTo = 0;
            ItemJar? jar = playerInv.getItem(pageFrom, playerInv.getIndex(pageFrom, xFrom, yFrom));
            ItemJar? swapping = !swap ? null : playerInv.getItem(pageTo, playerInv.getIndex(pageTo, xTo, yTo));
            ItemMoveRequested args = new ItemMoveRequested(pl, (Page)pageFrom, (Page)pageTo, xFrom, xTo, yFrom, yTo, rotTo, swap, jar, swapping);
            foreach (EventDelegate<ItemMoveRequested> inv in ItemMoveRequested.GetInvocationList().Cast<EventDelegate<ItemMoveRequested>>())
            {
                if (!args.CanContinue) break;
                TryInvoke(inv, args, nameof(ItemMoveRequested));
            }

            if (!args.CanContinue)
                return false;
            pageTo = (byte)args.NewPage;
            xTo = args.NewX;
            yTo = args.NewY;
            rotTo = args.NewRotation;
        }

        return true;
    }
    internal static void OnDraggedOrSwappedItem(PlayerInventory playerInv, byte pageFrom, byte pageTo, byte xFrom, byte xTo, byte yFrom, byte yTo, byte rotFrom, byte rotTo, bool swap)
    {
        if (ItemMoved != null && UCPlayer.FromPlayer(playerInv.player) is { IsOnline: true } pl)
        {
            ItemJar? jar = playerInv.getItem(pageTo, playerInv.getIndex(pageTo, xTo, yTo));
            if (!swap) rotFrom = byte.MaxValue;
            ItemJar? swapped = playerInv.getItem(pageFrom, playerInv.getIndex(pageFrom, xFrom, yFrom));
            ItemMoved args = new ItemMoved(pl, (Page)pageFrom, (Page)pageTo, xFrom, xTo, yFrom, yTo, rotFrom, rotTo, swap, jar, swapped);
            foreach (EventDelegate<ItemMoved> inv in ItemMoved.GetInvocationList().Cast<EventDelegate<ItemMoved>>())
            {
                if (!args.CanContinue) break;
                TryInvoke(inv, args, nameof(ItemMoved));
            }
        }
    }

    internal static void OnPickedUpItem(Player player, byte regionX, byte regionY, uint instanceId, byte toX, byte toY, byte toRot, byte toPage, ItemData? data)
    {
        if (ItemPickedUp != null && UCPlayer.FromPlayer(player) is { IsOnline: true } pl)
        {
            PlayerInventory playerInv = player.inventory;
            ItemRegion? region = null;
            ItemJar? jar = null;
            if (ItemManager.regions != null && Regions.checkSafe(regionX, regionY))
                region = ItemManager.regions[regionX, regionY];
            if (data != null)
            {
                if (toPage != byte.MaxValue)
                    jar = playerInv.getItem(toPage, playerInv.getIndex(toPage, toX, toY));
                if (jar == null || jar.item != data.item)
                {
                    for (int page = 0; page < PlayerInventory.AREA; ++page)
                    {
                        SDG.Unturned.Items p = playerInv.items[page];
                        for (int index = 0; index < p.items.Count; ++index)
                        {
                            if (p.items[index].item == data.item)
                            {
                                jar = p.items[index];
                                toX = jar.x;
                                toY = jar.y;
                                toPage = (byte)page;
                                toRot = jar.rot;
                                goto d;
                            }
                        }
                    }
                }
            }
            d:

            ItemPickedUp args = new ItemPickedUp(pl, (Page)toPage, toX, toY, toRot, regionX, regionY, instanceId, region, data, jar, data?.item);
            foreach (EventDelegate<ItemPickedUp> inv in ItemPickedUp.GetInvocationList().Cast<EventDelegate<ItemPickedUp>>())
            {
                if (!args.CanContinue) break;
                TryInvoke(inv, args, nameof(ItemPickedUp));
            }
        }
    }
    internal static void OnDroppedItem(PlayerInventory playerInv, byte page, byte x, byte y, byte rot, Item? item)
    {
        if (ItemDropped != null && UCPlayer.FromPlayer(playerInv.player) is { IsOnline: true } pl)
        {
            ItemData? data = null;
            if (ItemManager.regions != null && item != null)
            {
                if (Regions.tryGetCoordinate(playerInv.player.transform.position, out byte x2, out byte y2))
                {
                    ItemRegion r = ItemManager.regions[x2, y2];
                    for (int i = 0; i < r.items.Count; ++i)
                    {
                        if (r.items[i].item == item)
                        {
                            data = r.items[i];
                            break;
                        }
                    }
                }
                if (data == null)
                {
                    for (x2 = 0; x2 < Regions.WORLD_SIZE; ++x2)
                    {
                        for (y2 = 0; y2 < Regions.WORLD_SIZE; ++y2)
                        {
                            ItemRegion r = ItemManager.regions[x2, y2];
                            for (int i = 0; i < r.items.Count; ++i)
                            {
                                if (r.items[i].item == item)
                                {
                                    data = r.items[i];
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            ItemDropped args = new ItemDropped(pl, item, data, (Page)page, x, y, rot);
            foreach (EventDelegate<ItemDropped> inv in ItemDropped.GetInvocationList().Cast<EventDelegate<ItemDropped>>())
            {
                if (!args.CanContinue) break;
                TryInvoke(inv, args, nameof(ItemDropped));
            }
        }
    }

    public static void InvokeOnItemRemoved(UCPlayer player, byte page, byte index, ItemJar jar)
    {
        if (InventoryItemRemoved == null) return;

        InventoryItemRemoved args = new InventoryItemRemoved(player, (Page)page, jar.x, jar.y, index, jar);
        foreach (EventDelegate<InventoryItemRemoved> inv in InventoryItemRemoved.GetInvocationList().Cast<EventDelegate<InventoryItemRemoved>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(InventoryItemRemoved));
        }
    }

    internal static bool InvokeSwapClothingRequest(ClothingType type, UCPlayer player, byte page, byte x, byte y)
    {
        if (SwapClothingRequested == null) return true;
        ItemJar? jar = page < player.Player.inventory.items.Length ? player.Player.inventory.items[page].getItem(player.Player.inventory.items[page].getIndex(x, y)) : null;
        SwapClothingRequested args = new SwapClothingRequested(player, type, jar, (Page)page, x, y);
        foreach (EventDelegate<SwapClothingRequested> inv in SwapClothingRequested.GetInvocationList().Cast<EventDelegate<SwapClothingRequested>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(SwapClothingRequested));
        }

        return args.CanContinue;
    }
}
public delegate void EventDelegate<in TState>(TState e) where TState : EventState;
public delegate Task AsyncEventDelegate<in TState>(TState e, CancellationToken token = default) where TState : EventState;

/// <summary>Meant purely to break execution.</summary>
public class ControlException : Exception
{
    public ControlException() { }
    public ControlException(string message) : base(message) { }
}