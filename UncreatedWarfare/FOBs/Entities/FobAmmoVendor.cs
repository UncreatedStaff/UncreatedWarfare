using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.FOBs.Entities;

public class FobAmmoVendor : BuildableFobEntity<ShovelableInfo>, IAmmoStorage
{
    private readonly FobManager _fobManager;

    public FobAmmoVendor(ShovelableInfo? info, Team team, IBuildable buildable, FobManager fobManager, IServiceProvider serviceProvider, string? iconOverride = null) : base(info, buildable, team, serviceProvider, iconOverride)
    {
        _fobManager = fobManager;
    }

    public bool CanChangeKit => true;
    public float AmmoCount => _fobManager.FindNearestBunkerFob(Team, Point)?.AmmoCount ?? 0;
    public CSteamID Owner => Buildable.Owner;
    public Vector3 Point => Buildable.Position;
    public float InteractRange => 5;
    public event Action? AmmoCountUpdated; // FOB ammo vendors don't directly store ammo so they don't need anything to happen when their ammo count changes
    public void SubtractAmmo(float ammoCount)
    {
        _fobManager.FindNearestBunkerFob(Team, Point)?.ChangeAmmo(-ammoCount, SupplyChangeReason.ConsumeGeneral);
    }
}