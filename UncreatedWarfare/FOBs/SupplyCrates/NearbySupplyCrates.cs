using System;
using System.Linq;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.FOBs.SupplyCrates;
public class NearbySupplyCrates
{
    private readonly TrackingList<SupplyCrate> _supplyCrates;
    private readonly FobManager _fobManager;
    private readonly Vector3 _requiredSupplyPoint;
    private readonly CSteamID _team;

    public float AmmoCount { get; private set; }
    public float BuildCount { get; private set; }
    public float NumberOfSupplyCrates => _supplyCrates.Count;
    private NearbySupplyCrates(TrackingList<SupplyCrate> supplyCrates, Vector3 requiredSupplyPoint, CSteamID team, FobManager fobManager)
    {
        _supplyCrates = supplyCrates;

        AmmoCount = _supplyCrates.Where(c => c.Type == SupplyType.Ammo).Sum(c => c.SupplyCount);
        BuildCount = _supplyCrates.Where(c => c.Type == SupplyType.Build).Sum(c => c.SupplyCount);
        _fobManager = fobManager;
        _requiredSupplyPoint = requiredSupplyPoint;
        _team = team;
    }

    /// <summary>
    /// Checks to see if there are nearby crates of each type of buildable without allocating a list for them.
    /// </summary>
    public static bool HasNearbySupplyCrates(Vector3 supplyPoint, CSteamID team, FobManager fobManager, out bool hasAmmoCrates, out bool hasBuildCrates)
    {
        hasAmmoCrates = false;
        hasBuildCrates = false;

        bool hasAny = false;
        foreach (IFobEntity entity in fobManager.Entities)
        {
            if (entity is not SupplyCrate crate || crate.Buildable.Group != team || !crate.Buildable.IsAlive || !crate.IsWithinRadius(supplyPoint))
                continue;

            hasAny = true;

            if (crate.Type == SupplyType.Build)
            {
                hasBuildCrates = true;
                if (hasAmmoCrates) return true;
            }
            else if (crate.Type == SupplyType.Ammo)
            {
                hasAmmoCrates = true;
                if (hasBuildCrates) return true;
            }
        }

        return hasAny;
    }

    /// <summary>
    /// Compiles a list of all nearby supply crates.
    /// </summary>
    public static NearbySupplyCrates FindNearbyCrates(Vector3 supplyPoint, CSteamID team, FobManager fobManager)
    {
        var supplyCrates = fobManager.Entities
            .OfType<SupplyCrate>()
            .Where(e => e.Buildable.Group == team && e.IsWithinRadius(supplyPoint))
            .OrderByDescending(x => x.Buildable.Position.y) // top ones are used first so less likely for them to be floating
            .ToTrackingList();

        return new NearbySupplyCrates(supplyCrates, supplyPoint, team, fobManager);
    }
    
    public static NearbySupplyCrates FromSingleCrate(SupplyCrate existing, FobManager fobManager) // todo: team should be a team.
    {
        return new NearbySupplyCrates(new TrackingList<SupplyCrate>(1) { existing }, existing.Buildable.Position, existing.Buildable.Group, fobManager);
    }

    private void ChangeSupplies(float amount, SupplyType type, SupplyChangeReason changeReason, WarfarePlayer? instigator = null)
    {
        if (type == SupplyType.Ammo)
        {
            amount = Math.Max(-AmmoCount, amount);
            AmmoCount += amount;
        }
        else if (type == SupplyType.Build)
        {
            amount = Math.Max(-BuildCount, amount);
            BuildCount += amount;
        }

        NotifyChanged(type, amount, changeReason, instigator);
    }

    public void SubtractSupplies(float amount, SupplyType type, SupplyChangeReason changeReason, WarfarePlayer? instigator = null)
    {
        float originalAmount = amount;

        // subtract from crates
        foreach (SupplyCrate crate in _supplyCrates)
        {
            if (crate.Type != type) // do not subtract supplies from the wrong crate type
                continue;

            float remainder = crate.SupplyCount - amount;
            float toSubstract = Mathf.Clamp(crate.SupplyCount - amount, 0, crate.SupplyCount);
            crate.SupplyCount = toSubstract;

            if (remainder <= 0)
            {
                crate.Buildable.Destroy();
                
                // move on and try to subtract the remainder from the next crate
                amount = -remainder;
            }

            if (remainder >= 0) // no need to subtract from any further crates
                break;
        }

        ChangeSupplies(-originalAmount, type, changeReason, instigator);
    }

    public void RefundSupplies(float amount, SupplyType type)
    {
        float totalAmountToAdd = amount;
        // subtract from crates
        foreach (SupplyCrate crate in _supplyCrates)
        {
            if (crate.Type != type) // do not subtract supplies from the wrong crate type
                continue;

            float amountBeforeAdd = crate.SupplyCount;
            float newAmount = Mathf.Clamp(crate.SupplyCount + totalAmountToAdd, 0, crate.MaxSupplyCount);
            crate.SupplyCount = newAmount;

            float amountAddedToCrate = newAmount - amountBeforeAdd;
            totalAmountToAdd -= amountAddedToCrate;
        }

        ChangeSupplies(amount, type, SupplyChangeReason.ResupplyShoveableSalvaged);
    }

    public void NotifyChanged(SupplyType type, float amountDelta, SupplyChangeReason reason, WarfarePlayer? instigator = null)
    {
        foreach (var fob in _fobManager.Fobs)
        {
            if (fob is not ResourceFob bpf || bpf.Team.GroupId != _team || !bpf.IsWithinRadius(_requiredSupplyPoint))
                continue;

            bpf.ChangeSupplies(type, amountDelta, reason, instigator);
        }
    }
}