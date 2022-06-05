using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Events;
public static class EventDispatcher
{
    public static event EventDelegate<ExitVehicleRequested> OnExitVehicleRequested;
    public static event EventDelegate<EnterVehicleRequested> OnEnterVehicleRequested;
    public static event EventDelegate<VehicleSwapSeatRequested> OnVehicleSwapSeatRequested;
    public static event EventDelegate<VehicleSpawned> OnVehicleSpawned;
    public static event EventDelegate<ExitVehicle> OnExitVehicle;
    public static event EventDelegate<EnterVehicle> OnEnterVehicle;
    public static event EventDelegate<VehicleSwapSeat> OnVehicleSwapSeat;

    public static event EventDelegate<BarricadeDestroyed> OnBarricadeDestroyed;
    public static event EventDelegate<PlaceBarricadeRequested> OnBarricadePlaceRequested;
    public static event EventDelegate<BarricadePlaced> OnBarricadePlaced;
    public static event EventDelegate<LandmineExploding> OnLandmineExploding;

    public static event EventDelegate<ThrowableSpawned> OnThrowableSpawned;
    public static event EventDelegate<ThrowableSpawned> OnThrowableDespawning;

    public static event EventDelegate<PlayerPending> OnPlayerPending;
    public static event EventDelegate<PlayerJoined> OnPlayerJoined;
    public static event EventDelegate<PlayerEvent> OnPlayerLeaving;
    public static event EventDelegate<BattlEyeKicked> OnPlayerBattlEyeKicked;
    public static event EventDelegate<PlayerDied> OnPlayerDied;
    public static event EventDelegate<GroupChanged> OnGroupChanged;
    internal static void SubscribeToAll()
    {
        EventPatches.TryPatchAll();
        VehicleManager.onExitVehicleRequested += VehicleManagerOnExitVehicleRequested;
        VehicleManager.onEnterVehicleRequested += VehicleManagerOnEnterVehicleRequested;
        VehicleManager.onSwapSeatRequested += VehicleManagerOnSwapSeatRequested;
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
    }
    internal static void UnsubscribeFromAll()
    {
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
        VehicleManager.onSwapSeatRequested -= VehicleManagerOnSwapSeatRequested;
        VehicleManager.onEnterVehicleRequested -= VehicleManagerOnEnterVehicleRequested;
        VehicleManager.onExitVehicleRequested -= VehicleManagerOnExitVehicleRequested;
    }
    private static void TryInvoke<T>(EventDelegate<T> @delegate, T request, string name) where T : EventState
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
        if (OnExitVehicleRequested == null || !shouldAllow) return;
        ExitVehicleRequested request = new ExitVehicleRequested(player, vehicle, shouldAllow, pendingLocation, pendingYaw);
        foreach (EventDelegate<ExitVehicleRequested> inv in OnExitVehicleRequested.GetInvocationList().Cast<EventDelegate<ExitVehicleRequested>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(OnExitVehicleRequested));
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
        if (OnEnterVehicleRequested == null || !shouldAllow || vehicle == null || player == null) return;
        EnterVehicleRequested request = new EnterVehicleRequested(player, vehicle, shouldAllow);
        foreach (EventDelegate<EnterVehicleRequested> inv in OnEnterVehicleRequested.GetInvocationList().Cast<EventDelegate<EnterVehicleRequested>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(OnEnterVehicleRequested));
        }
        if (!request.CanContinue)
            shouldAllow = false;
    }
    private static void VehicleManagerOnSwapSeatRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow, byte fromSeatIndex, ref byte toSeatIndex)
    {
        if (OnVehicleSwapSeatRequested == null || !shouldAllow) return;
        VehicleSwapSeatRequested request = new VehicleSwapSeatRequested(player, vehicle, shouldAllow, fromSeatIndex, toSeatIndex);
        foreach (EventDelegate<VehicleSwapSeatRequested> inv in OnVehicleSwapSeatRequested.GetInvocationList().Cast<EventDelegate<VehicleSwapSeatRequested>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(OnVehicleSwapSeatRequested));
        }
        if (!request.CanContinue) shouldAllow = false;
        else toSeatIndex = request.FinalSeat;
    }
    private static void InteractableVehicleOnPassengerChangedSeats(InteractableVehicle vehicle, int fromSeatIndex, int toSeatIndex)
    {
        if (OnVehicleSwapSeat == null) return;
        Passenger? pl = vehicle.passengers[toSeatIndex];
        if (pl is null || pl.player is null || pl.player.player == null) return;
        UCPlayer? pl2 = UCPlayer.FromPlayer(pl.player.player);
        if (pl2 is null) return;
        VehicleSwapSeat request = new VehicleSwapSeat(pl2, vehicle, (byte)fromSeatIndex, (byte)toSeatIndex);
        foreach (EventDelegate<VehicleSwapSeat> inv in OnVehicleSwapSeat.GetInvocationList().Cast<EventDelegate<VehicleSwapSeat>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(OnVehicleSwapSeat));
        }
    }
    private static void InteractableVehicleOnPassengerRemoved(InteractableVehicle vehicle, int seatIndex, Player player)
    {
        if (OnExitVehicle == null || player == null) return;
        UCPlayer? pl2 = UCPlayer.FromPlayer(player);
        if (pl2 is null) return;
        ExitVehicle request = new ExitVehicle(pl2, vehicle, (byte)seatIndex);
        foreach (EventDelegate<ExitVehicle> inv in OnExitVehicle.GetInvocationList().Cast<EventDelegate<ExitVehicle>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(OnExitVehicle));
        }
    }
    private static void InteractableVehicleOnPassengerAdded(InteractableVehicle vehicle, int seatIndex)
    {
        if (OnEnterVehicle == null || vehicle == null) return;
        Passenger? pl = vehicle.passengers[seatIndex];
        if (pl is null || pl.player is null || pl.player.player == null) return;
        UCPlayer? pl2 = UCPlayer.FromPlayer(pl.player.player);
        if (pl2 is null) return;
        EnterVehicle request = new EnterVehicle(pl2, vehicle, (byte)seatIndex);
        foreach (EventDelegate<EnterVehicle> inv in OnEnterVehicle.GetInvocationList().Cast<EventDelegate<EnterVehicle>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(OnEnterVehicle));
        }
    }
    internal static void InvokeOnVehicleSpawned(InteractableVehicle result)
    {
        if (OnVehicleSpawned == null) return;
        VehicleSpawned args = new VehicleSpawned(result);
        foreach (EventDelegate<VehicleSpawned> inv in OnVehicleSpawned.GetInvocationList().Cast<EventDelegate<VehicleSpawned>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(OnVehicleSpawned));
        }
    }
    internal static void InvokeOnBarricadeDestroyed(BarricadeDrop barricade, BarricadeData barricadeData, BarricadeRegion region, byte x, byte y, ushort plant)
    {
        if (OnBarricadeDestroyed == null) return;
        UCPlayer? instigator = barricade.model.TryGetComponent(out BarricadeComponent component) ? UCPlayer.FromID(component.LastDamager) : null;
        BarricadeDestroyed args = new BarricadeDestroyed(instigator, barricade, barricadeData, region, x, y, plant);
        foreach (EventDelegate<BarricadeDestroyed> inv in OnBarricadeDestroyed.GetInvocationList().Cast<EventDelegate<BarricadeDestroyed>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(OnBarricadeDestroyed));
        }
    }
    internal static void InvokeOnPlayerDied(PlayerDied e)
    {
        if (OnPlayerDied == null) return;
        foreach (EventDelegate<PlayerDied> inv in OnPlayerDied.GetInvocationList().Cast<EventDelegate<PlayerDied>>())
        {
            if (!e.CanContinue) break;
            TryInvoke(inv, e, nameof(OnPlayerDied));
        }
    }
    private static void ProviderOnServerDisconnected(CSteamID steamID)
    {
        if (OnPlayerLeaving == null) return;
        UCPlayer? player = UCPlayer.FromCSteamID(steamID);
        if (player is null) return;
        PlayerEvent args = new PlayerEvent(player);
        foreach (EventDelegate<PlayerEvent> inv in OnPlayerLeaving.GetInvocationList().Cast<EventDelegate<PlayerEvent>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(OnPlayerLeaving));
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
        if (OnPlayerJoined == null) return;
        try
        {
            Player pl = PlayerTool.getPlayer(steamID);
            if (pl is null)
                goto error;
            PlayerManager.InvokePlayerConnected(pl);
        }
        catch (Exception ex)
        {
            L.LogError("Error in EventDispatcher.ProviderOnServerConnected loading player into OnlinePlayers:");
            L.LogError(ex);
            goto error;
        }
        UCPlayer? player = UCPlayer.FromCSteamID(steamID);
        if (player is null)
            goto error;
        PlayerSave.TryReadSaveFile(steamID.m_SteamID, out PlayerSave? save);
        PlayerJoined args = new PlayerJoined(player, save);
        foreach (EventDelegate<PlayerJoined> inv in OnPlayerJoined.GetInvocationList().Cast<EventDelegate<PlayerJoined>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(OnPlayerJoined));
        }
        return;
    error:
        Provider.kick(steamID, "There was a fatal error connecting you to the server.");
    }
    private static void ProviderOnCheckValidWithExplanation(ValidateAuthTicketResponse_t callback, ref bool isValid, ref string explanation)
    {
        if (OnPlayerPending == null || !isValid) return;
        SteamPending? pending = null;
        for (int i = 0; i < Provider.pending.Count; ++i)
        {
            if (Provider.pending[i].playerID.steamID.m_SteamID == callback.m_SteamID.m_SteamID)
                pending = Provider.pending[i];
        }
        if (pending is null) return;
        PlayerSave.TryReadSaveFile(callback.m_SteamID.m_SteamID, out PlayerSave? save);
        PlayerPending args = new PlayerPending(pending, save, isValid, explanation);
        foreach (EventDelegate<PlayerPending> inv in OnPlayerPending.GetInvocationList().Cast<EventDelegate<PlayerPending>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(OnPlayerPending));
        }
        if (!args.CanContinue)
        {
            isValid = false;
            explanation = args.RejectReason;
        }
    }
    private static void ProviderOnBattlEyeKick(SteamPlayer client, string reason)
    {
        if (OnPlayerBattlEyeKicked == null) return;
        UCPlayer? player = UCPlayer.FromSteamPlayer(client);
        if (player is null) return;
        BattlEyeKicked args = new BattlEyeKicked(player, reason);
        foreach (EventDelegate<BattlEyeKicked> inv in OnPlayerBattlEyeKicked.GetInvocationList().Cast<EventDelegate<BattlEyeKicked>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(OnPlayerBattlEyeKicked));
        }
    }
    private static void BarricadeManagerOnDeployBarricadeRequested(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point, ref float angle_x, ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
    {
        if (OnBarricadePlaceRequested == null || !shouldAllow) return;
        UCPlayer? player = UCPlayer.FromID(owner);
        bool isVehicle = hit != null && hit.CompareTag("Vehicle");
        InteractableVehicle? vehicle = null;
        if (isVehicle)
        {
            vehicle = BarricadeManager.FindVehicleRegionByTransform(hit)?.vehicle;
            if (vehicle == null) isVehicle = false;
        }
        Vector3 rotation = new Vector3(angle_x, angle_y, angle_z);
        PlaceBarricadeRequested args = new PlaceBarricadeRequested(player, vehicle, barricade, asset, hit, point, rotation, owner, group, shouldAllow);
        foreach (EventDelegate<PlaceBarricadeRequested> inv in OnBarricadePlaceRequested.GetInvocationList().Cast<EventDelegate<PlaceBarricadeRequested>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(OnBarricadePlaceRequested));
        }
        if (!args.CanContinue)
            shouldAllow = false;
        else
        {
            point = args.Position;
            angle_x = args.Rotation.x;
            angle_y = args.Rotation.y;
            angle_z = args.Rotation.z;
            owner = args.Owner;
            group = args.GroupOwner;
        }
    }
    private static void BarricadeManagerOnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
    {
        if (OnBarricadePlaced == null) return;
        BarricadeData data = drop.GetServersideData();
        UCPlayer? owner = UCPlayer.FromID(data.owner);
        if (owner is null) return;
        BarricadePlaced args = new BarricadePlaced(owner, drop, data, region);
        foreach (EventDelegate<BarricadePlaced> inv in OnBarricadePlaced.GetInvocationList().Cast<EventDelegate<BarricadePlaced>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(OnBarricadePlaced));
        }
    }
    private static void UseableThrowableOnThrowableSpawned(UseableThrowable useable, GameObject throwable)
    {
        ThrowableComponent c = throwable.AddComponent<ThrowableComponent>();
        c.Throwable = useable.equippedThrowableAsset.GUID;
        c.Owner = useable.player.channel.owner.playerID.steamID.m_SteamID;
        c.IsExplosive = useable.equippedThrowableAsset.isExplosive;
        if (OnThrowableSpawned == null) return;
        UCPlayer? owner = UCPlayer.FromPlayer(useable.player);
        if (owner is null) return;
        if (owner.Player.TryGetPlayerData(out UCPlayerData comp))
            comp.ActiveThrownItems.Add(c);
        ThrowableSpawned args = new ThrowableSpawned(owner, useable.equippedThrowableAsset, throwable);
        foreach (EventDelegate<ThrowableSpawned> inv in OnThrowableSpawned.GetInvocationList().Cast<EventDelegate<ThrowableSpawned>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(OnThrowableSpawned));
        }
    }
    internal static void InvokeOnThrowableDespawning(ThrowableComponent throwableComponent)
    {
        if (OnThrowableDespawning == null) return;
        UCPlayer? owner = UCPlayer.FromID(throwableComponent.Owner);
        if (owner is null || Assets.find(throwableComponent.Throwable) is not ItemThrowableAsset asset) return;
        ThrowableSpawned args = new ThrowableSpawned(owner, asset, throwableComponent.gameObject);
        foreach (EventDelegate<ThrowableSpawned> inv in OnThrowableDespawning.GetInvocationList().Cast<EventDelegate<ThrowableSpawned>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(OnThrowableDespawning));
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
    internal static void InvokeOnLandmineExploding(UCPlayer? owner, BarricadeDrop barricade, InteractableTrap trap, UCPlayer triggerer, GameObject triggerObject, ref bool shouldExplode)
    {
        if (OnLandmineExploding == null || !shouldExplode) return;
        LandmineExploding request = new LandmineExploding(owner, barricade, trap, triggerer, triggerObject, shouldExplode);
        foreach (EventDelegate<LandmineExploding> inv in OnLandmineExploding.GetInvocationList().Cast<EventDelegate<LandmineExploding>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(OnLandmineExploding));
        }
        if (!request.CanContinue) shouldExplode = false;
    }
    internal static void InvokeOnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup)
    {
        if (OnGroupChanged == null || player is null) return;
        GroupChanged args = new GroupChanged(player, oldGroup, newGroup);
        foreach (EventDelegate<GroupChanged> inv in OnGroupChanged.GetInvocationList().Cast<EventDelegate<GroupChanged>>())
        {
            if (!args.CanContinue) break;
            TryInvoke(inv, args, nameof(OnGroupChanged));
        }
    }
}
public delegate void EventDelegate<T>(T e) where T : EventState;
