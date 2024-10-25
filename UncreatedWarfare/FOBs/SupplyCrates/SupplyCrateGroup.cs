using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.FOBs.SupplyCrates;
public class SupplyCrateGroup
{
    private TrackingList<SupplyCrate> _supplyCrates;
    private readonly FobManager _fobManager;
    private readonly Vector3 _requiredSupplyPoint;
    private readonly Team _team;

    public int AmmoCount { get; private set; }
    public int BuildCount { get; private set; }
    public SupplyCrateGroup(FobManager fobManager, Vector3 requiredSupplyPoint, Team team)
    {
        _supplyCrates = fobManager.FloatingItems
            .Where(t => t is SupplyCrate c && c.IsWithinRadius(requiredSupplyPoint))
            .Cast<SupplyCrate>()
            .ToTrackingList();

        AmmoCount = _supplyCrates.Where(c => c.Type == SupplyType.Ammo).Sum(c => c.SupplyCount);
        BuildCount = _supplyCrates.Where(c => c.Type == SupplyType.Build).Sum(c => c.SupplyCount);
        _fobManager = fobManager;
        _requiredSupplyPoint = requiredSupplyPoint;
        _team = team;
    }
    private void ChangeSupplies(int amount, SupplyType type, SupplyChangeReason changeReason)
    {
        if (type == SupplyType.Ammo)
            AmmoCount = Mathf.Max(AmmoCount + amount, 0);
        if (type == SupplyType.Build)
            BuildCount = Mathf.Max(BuildCount + amount, 0);

        foreach (var fob in _fobManager.Fobs)
        {
            if (fob is not BasePlayableFob bpf || bpf.Team != _team || !bpf.IsWithinRadius(_requiredSupplyPoint))
                continue;

            // event or update fob ui
        }
    }
    public void SubstractSupplies(int amount, SupplyType type, SupplyChangeReason changeReason)
    {
        int originalAmount = amount;

        // subtract from crates
        foreach (SupplyCrate crate in _supplyCrates)
        {
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
}
