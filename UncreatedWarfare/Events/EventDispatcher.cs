using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Events;
public static class EventDispatcher
{
    public static event EventDelegate<ExitVehicleRequested> OnExitVehicleRequested;
    public static event EventDelegate<EnterVehicleRequested> OnEnterVehicleRequested;
    public static event EventDelegate<VehicleSwapSeatRequested> OnVehicleSwapSeatRequested;
    public static event EventDelegate<ExitVehicle> OnExitVehicle;
    public static event EventDelegate<EnterVehicle> OnEnterVehicle;
    public static event EventDelegate<VehicleSwapSeat> OnVehicleSwapSeat;

    public static event EventDelegate<BarricadeDestroyed> OnBarricadeDestroyed;
    public static event EventDelegate<PlaceBarricadeRequested> OnBarricadePlaceRequested;
    public static event EventDelegate<BarricadePlaced> OnBarricadePlaced;

    public static event EventDelegate<PlayerPending> OnPlayerPending;
    public static event EventDelegate<PlayerJoined> OnPlayerJoined;
    public static event EventDelegate<PlayerEvent> OnPlayerLeaving;
    public static event EventDelegate<BattlEyeKicked> OnPlayerBattlEyeKicked;
    internal static void SubscribeToAll()
    {
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
    }
    internal static void UnsubscribeFromAll()
    {
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
        EventDelegate<ExitVehicleRequested>[] inv = (EventDelegate<ExitVehicleRequested>[])OnExitVehicleRequested.GetInvocationList();
        for (int i = 0; i < inv.Length && request.CanContinue; ++i)
            TryInvoke(inv[i], request, nameof(OnExitVehicleRequested));
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
        EventDelegate<EnterVehicleRequested>[] inv = (EventDelegate<EnterVehicleRequested>[])OnEnterVehicleRequested.GetInvocationList();
        for (int i = 0; i < inv.Length && request.CanContinue; ++i)
            TryInvoke(inv[i], request, nameof(OnEnterVehicleRequested));
        if (!request.CanContinue)
            shouldAllow = false;
    }
    private static void VehicleManagerOnSwapSeatRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow, byte fromSeatIndex, ref byte toSeatIndex)
    {
        if (OnVehicleSwapSeatRequested == null || !shouldAllow) return;
        VehicleSwapSeatRequested request = new VehicleSwapSeatRequested(player, vehicle, shouldAllow, fromSeatIndex, toSeatIndex);
        EventDelegate<VehicleSwapSeatRequested>[] inv = (EventDelegate<VehicleSwapSeatRequested>[])OnVehicleSwapSeatRequested.GetInvocationList();
        for (int i = 0; i < inv.Length && request.CanContinue; ++i)
            TryInvoke(inv[i], request, nameof(OnVehicleSwapSeatRequested));
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
        EventDelegate<VehicleSwapSeat>[] inv = (EventDelegate<VehicleSwapSeat>[])OnVehicleSwapSeat.GetInvocationList();
        for (int i = 0; i < inv.Length && request.CanContinue; ++i)
            TryInvoke(inv[i], request, nameof(OnVehicleSwapSeat));
    }
    private static void InteractableVehicleOnPassengerRemoved(InteractableVehicle vehicle, int seatIndex, Player player)
    {
        if (OnExitVehicle == null || player == null) return;
        UCPlayer? pl2 = UCPlayer.FromPlayer(player);
        if (pl2 is null) return;
        ExitVehicle request = new ExitVehicle(pl2, vehicle, (byte)seatIndex);
        EventDelegate<ExitVehicle>[] inv = (EventDelegate<ExitVehicle>[])OnExitVehicle.GetInvocationList();
        for (int i = 0; i < inv.Length && request.CanContinue; ++i)
            TryInvoke(inv[i], request, nameof(OnExitVehicle));
    }
    private static void InteractableVehicleOnPassengerAdded(InteractableVehicle vehicle, int seatIndex)
    {
        if (OnEnterVehicle == null || vehicle == null) return;
        Passenger? pl = vehicle.passengers[seatIndex];
        if (pl is null || pl.player is null || pl.player.player == null) return;
        UCPlayer? pl2 = UCPlayer.FromPlayer(pl.player.player);
        if (pl2 is null) return;
        EnterVehicle request = new EnterVehicle(pl2, vehicle, (byte)seatIndex);
        EventDelegate<EnterVehicle>[] inv = (EventDelegate<EnterVehicle>[])OnEnterVehicle.GetInvocationList();
        for (int i = 0; i < inv.Length && request.CanContinue; ++i)
            TryInvoke(inv[i], request, nameof(OnEnterVehicle));
    }
    internal static void InvokeOnBarricadeDestroyed(BarricadeDrop barricade, BarricadeData barricadeData, BarricadeRegion region, byte x, byte y, ushort plant)
    {
        if (OnBarricadeDestroyed == null) return;
        UCPlayer? instigator = barricade.model.TryGetComponent(out BarricadeComponent component) ? UCPlayer.FromID(component.LastDamager) : null;
        BarricadeDestroyed args = new BarricadeDestroyed(instigator, barricade, barricadeData, region, x, y, plant);
        EventDelegate<BarricadeDestroyed>[] inv = (EventDelegate<BarricadeDestroyed>[])OnBarricadeDestroyed.GetInvocationList();
        for (int i = 0; i < inv.Length && args.CanContinue; ++i)
            TryInvoke(inv[i], args, nameof(OnBarricadeDestroyed));
    }
    private static void ProviderOnServerDisconnected(CSteamID steamID)
    {
        if (OnPlayerLeaving == null) return;
        UCPlayer? player = UCPlayer.FromCSteamID(steamID);
        if (player is null) return;
        PlayerEvent args = new PlayerEvent(player);
        EventDelegate<PlayerEvent>[] inv = (EventDelegate<PlayerEvent>[])OnPlayerLeaving.GetInvocationList();
        for (int i = 0; i < inv.Length && args.CanContinue; ++i)
            TryInvoke(inv[i], args, nameof(OnPlayerLeaving));
    }
    private static void ProviderOnServerConnected(CSteamID steamID)
    {
        if (OnPlayerJoined == null) return;
        UCPlayer? player = UCPlayer.FromCSteamID(steamID);
        if (player is null) return;
        PlayerSave.TryReadSaveFile(steamID.m_SteamID, out PlayerSave? save);
        PlayerJoined args = new PlayerJoined(player, save);
        EventDelegate<PlayerJoined>[] inv = (EventDelegate<PlayerJoined>[])OnPlayerJoined.GetInvocationList();
        for (int i = 0; i < inv.Length && args.CanContinue; ++i)
            TryInvoke(inv[i], args, nameof(OnPlayerJoined));
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
        EventDelegate<PlayerPending>[] inv = (EventDelegate<PlayerPending>[])OnPlayerPending.GetInvocationList();
        for (int i = 0; i < inv.Length && args.CanContinue; ++i)
            TryInvoke(inv[i], args, nameof(OnPlayerPending));
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
        EventDelegate<BattlEyeKicked>[] inv = (EventDelegate<BattlEyeKicked>[])OnPlayerBattlEyeKicked.GetInvocationList();
        for (int i = 0; i < inv.Length && args.CanContinue; ++i)
            TryInvoke(inv[i], args, nameof(OnPlayerBattlEyeKicked));
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
        EventDelegate<PlaceBarricadeRequested>[] inv = (EventDelegate<PlaceBarricadeRequested>[])OnBarricadePlaceRequested.GetInvocationList();
        for (int i = 0; i < inv.Length && args.CanContinue; ++i)
            TryInvoke(inv[i], args, nameof(OnBarricadePlaceRequested));
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
        EventDelegate<BarricadePlaced>[] inv = (EventDelegate<BarricadePlaced>[])OnBarricadePlaced.GetInvocationList();
        for (int i = 0; i < inv.Length && args.CanContinue; ++i)
            TryInvoke(inv[i], args, nameof(OnBarricadePlaced));
    }
}
public delegate void EventDelegate<T>(T e) where T : EventState;
