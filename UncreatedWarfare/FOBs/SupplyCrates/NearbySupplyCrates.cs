using System.Linq;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Fobs;
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

    public static NearbySupplyCrates FindNearbyCrates(Vector3 supplyPoint, CSteamID team, FobManager fobManager)
    {
        var supplyCrates = fobManager.Entities
            .OfType<SupplyCrate>()
            .Where(e => e.Buildable.Group == team && e.IsWithinRadius(supplyPoint))
            .ToTrackingList();  

        return new NearbySupplyCrates(supplyCrates, supplyPoint, team, fobManager);
    }
    
    public static NearbySupplyCrates FromSingleCrate(SupplyCrate existing, FobManager fobManager) // todo: team should be a team.
    {
        return new NearbySupplyCrates(new TrackingList<SupplyCrate>(1) { existing }, existing.Buildable.Position, existing.Buildable.Group, fobManager);
    }

    private void ChangeSupplies(float amount, SupplyType type, SupplyChangeReason changeReason)
    {
        if (type == SupplyType.Ammo)
            AmmoCount = Mathf.Max(AmmoCount + amount, 0);
        else if(type == SupplyType.Build)
            BuildCount = Mathf.Max(BuildCount + amount, 0);

        NotifyChanged(type, amount, changeReason);
    }

    public void SubstractSupplies(float amount, SupplyType type, SupplyChangeReason changeReason)
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
                // move on and try to substract the remainder from the next crate
                amount = -remainder;
            }

            if (remainder >= 0) // no need to substract from any further crates
                break;
        }
        ChangeSupplies(-originalAmount, type, changeReason);
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

    public void NotifyChanged(SupplyType type, float amountDelta, SupplyChangeReason reason, WarfarePlayer? resupplier = null)
    {
        foreach (var fob in _fobManager.Fobs)
        {
            if (fob is not ResourceFob bpf || bpf.Team.GroupId != _team || !bpf.IsWithinRadius(_requiredSupplyPoint))
                continue;

            bpf.ChangeSupplies(type, amountDelta);
            FobSuppliesChanged args = new FobSuppliesChanged {
                Fob = bpf,
                AmountDelta = amountDelta,
                SupplyType = type,
                ChangeReason = reason,
                Resupplier = resupplier
            };

            _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
        }
    }
}