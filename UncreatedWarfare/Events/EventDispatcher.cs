using System;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Components;

namespace Uncreated.Warfare.Events;
public static class EventDispatcher
{
    internal static void SubscribeToAll()
    {
        EventPatches.TryPatchAll();
    }
    internal static void InvokeVehicleManagerOnSwapSeatRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow, byte fromSeatIndex, ref byte toSeatIndex)
    {
#if false
        if (VehicleSwapSeatRequested == null || !shouldAllow) return;
        VehicleSwapSeatRequested request = new VehicleSwapSeatRequested(player, vehicle, shouldAllow, fromSeatIndex, toSeatIndex);
        foreach (EventDelegate<VehicleSwapSeatRequested> inv in VehicleSwapSeatRequested.GetInvocationList().Cast<EventDelegate<VehicleSwapSeatRequested>>())
        {
            if (!request.CanContinue) break;
            TryInvoke(inv, request, nameof(VehicleSwapSeatRequested));
        }
        if (!request.CanContinue) shouldAllow = false;
        else toSeatIndex = request.FinalSeat;
#endif
    }
    internal static void InvokeOnThrowableDespawning(ThrowableComponent throwableComponent)
    {
#if false
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
#endif
    }
    internal static void InvokeOnProjectileExploded(ProjectileComponent projectileComponent, Collider other)
    {
#if false
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
#endif
    }
    internal static bool OnDraggingOrSwappingItem(PlayerInventory playerInv, byte pageFrom, ref byte pageTo, byte xFrom, ref byte xTo, byte yFrom, ref byte yTo, ref byte rotTo, bool swap)
    {
#if false
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
#endif

        return true;
    }
    internal static void OnDraggedOrSwappedItem(PlayerInventory playerInv, byte pageFrom, byte pageTo, byte xFrom, byte xTo, byte yFrom, byte yTo, byte rotFrom, byte rotTo, bool swap)
    {
#if false
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
#endif
    }

    internal static void OnPickedUpItem(Player player, byte regionX, byte regionY, uint instanceId, byte toX, byte toY, byte toRot, byte toPage, ItemData? data)
    {
#if false
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
#endif
    }
}
/// <summary>Meant purely to break execution.</summary>
public class ControlException : Exception
{
    public ControlException() { }
    public ControlException(string message) : base(message) { }
}