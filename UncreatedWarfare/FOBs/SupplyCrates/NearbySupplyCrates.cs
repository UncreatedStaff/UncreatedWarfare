using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.FOBs.SupplyCrates;
public class NearbySupplyCrates
{
    private TrackingList<SupplyCrate> _supplyCrates;
    private readonly FobManager _fobManager;
    private readonly Vector3 _requiredSupplyPoint;
    private readonly CSteamID _team;

    public int AmmoCount { get; private set; }
    public int BuildCount { get; private set; }
    private NearbySupplyCrates(TrackingList<SupplyCrate> supplyCrates, Vector3 requiredSupplyPoint, CSteamID team, FobManager fobManager)
    {
        _supplyCrates = supplyCrates.ToTrackingList();

        AmmoCount = _supplyCrates.Where(c => c.Type == SupplyType.Ammo).Sum(c => c.SupplyCount);
        BuildCount = _supplyCrates.Where(c => c.Type == SupplyType.Build).Sum(c => c.SupplyCount);
        _fobManager = fobManager;
        _requiredSupplyPoint = requiredSupplyPoint;
        _team = team;
    }
    public static NearbySupplyCrates FindNearbyCrates(Vector3 supplyPoint, CSteamID team, FobManager fobManager)
    {
        var supplyCrates = fobManager.FloatingItems
            .Where(t => t is SupplyCrate c && c.IsWithinRadius(supplyPoint))
            .Cast<SupplyCrate>()
            .ToTrackingList();

        return new NearbySupplyCrates(supplyCrates, supplyPoint, team, fobManager);
    }
    public static NearbySupplyCrates FromSingleCrate(SupplyCrate existing, FobManager fobManager) // todo: team should be a team.
    {
        return new NearbySupplyCrates(new TrackingList<SupplyCrate>() { existing }, existing.Buildable.Position, existing.Buildable.Group, fobManager);
    }
    private void ChangeSupplies(int amount, SupplyType type, SupplyChangeReason changeReason)
    {
        if (type == SupplyType.Ammo)
            AmmoCount = Mathf.Max(AmmoCount + amount, 0);
        else if(type == SupplyType.Build)
            BuildCount = Mathf.Max(BuildCount + amount, 0);

        NotifyChanged(type, amount, changeReason);
    }
    public void SubstractSupplies(int amount, SupplyType type, SupplyChangeReason changeReason)
    {
        int originalAmount = amount;

        // subtract from crates
        foreach (SupplyCrate crate in _supplyCrates)
        {
            if (crate.Type != type) // do not subtract supplies from the wrong crate type
                continue;

            int remainder = crate.SupplyCount - amount;
            int toSubstract = Mathf.Clamp(crate.SupplyCount - amount, 0, crate.SupplyCount);
            crate.SupplyCount = toSubstract;

            if (remainder <= 0)
            {
                crate.Buildable.Destroy();
                // move on and try to substract the remainder from the next crate
                amount = -remainder;
            }

            if (remainder >= 0) // no need to substract from any further crates
                break;
        }
        ChangeSupplies(-originalAmount, type, changeReason);
    }
    public void RefundSupplies(int amount, SupplyType type)
    {
        int totalAmountToAdd = amount;
        // subtract from crates
        foreach (SupplyCrate crate in _supplyCrates)
        {
            if (crate.Type != type) // do not subtract supplies from the wrong crate type
                continue;

            int amountBeforeAdd = crate.SupplyCount;
            int newAmount = Mathf.Clamp(crate.SupplyCount + totalAmountToAdd, 0, crate.MaxSupplyCount);
            crate.SupplyCount = newAmount;

            int amountAddedToCrate = newAmount - amountBeforeAdd;
            totalAmountToAdd -= amountAddedToCrate;
        }
        ChangeSupplies(amount, type, SupplyChangeReason.ResupplyShoveableSalvaged);
    }
    public void NotifyChanged(SupplyType type, int amountDelta, SupplyChangeReason reason)
    {
        foreach (var fob in _fobManager.Fobs)
        {
            if (fob is not BasePlayableFob bpf || bpf.Team.GroupId != _team || !bpf.IsWithinRadius(_requiredSupplyPoint))
                continue;

            Console.WriteLine($"Fob supplies changed by " + amountDelta);

            bpf.ChangeSupplies(type, amountDelta);
            _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobSuppliesChanged { Fob = bpf, AmountDelta = amountDelta, SupplyType = type, ChangeReason = reason });
        }
    }
}
