using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.Entities;

public class FobAmmoVendor : BuildableFobEntity<ShovelableInfo>, IAmmoStorage
{
    private readonly FobManager _fobManager;
    private readonly SemaphoreSlim _intxSemaphore;

    private ResourceFob? _resourceFob;

    private event Action<float>? AmmoCountUpdated;

    event Action<float>? IAmmoStorage.AmmoCountUpdated
    {
        add
        {
            _ = GetOrFindRelevantFob();
            AmmoCountUpdated += value;
        }
        remove => AmmoCountUpdated -= value;
    }

    public FobAmmoVendor(ShovelableInfo? info, Team team, IBuildable buildable, FobManager fobManager, IServiceProvider serviceProvider, string? iconOverride = null, bool iconVisible = true)
        : base(info, buildable, team, serviceProvider, iconOverride, iconVisible)
    {
        _fobManager = fobManager;
        _intxSemaphore = new SemaphoreSlim(1, 1);
    }

    public bool CanChangeKit => true;
    public bool AllowDiscountedRearm => false;
    public float AmmoCount
    {
        get
        {
            IResourceFob? relevantFob = GetOrFindRelevantFob();
            return relevantFob?.AmmoCount ?? 0;
        }
    }

    public CSteamID Owner => Buildable.Owner;
    public Vector3 Point => Buildable.Position;
    public float InteractRange => 6;

    /// <inheritdoc />
    public void SubtractAmmo(float ammoCount)
    {
        GetOrFindRelevantFob()?.ChangeSupplies(0f, ammoAmount: -ammoCount, SupplyChangeReason.ConsumeGeneral);
    }

    private IResourceFob? GetOrFindRelevantFob()
    {
        GameThread.AssertCurrent();

        if (_resourceFob is { IsRegistered: true })
        {
            return _resourceFob;
        }

        if (_resourceFob != null)
        {
            _resourceFob.OnSuppliesUpdated -= OnFobSuppliesUpdated;
            _resourceFob.Deregistered -= OnFobDeregistered;
        }

        ResourceFob? fob = _fobManager.FindNearestBunkerFob(Team, Point);
        if (fob != null)
        {
            fob.OnSuppliesUpdated += OnFobSuppliesUpdated;
            fob.Deregistered += OnFobDeregistered;
        }

        _resourceFob = fob;
        return fob;
    }

    private void OnFobDeregistered()
    {
        try
        {
            AmmoCountUpdated?.Invoke(0);
        }
        catch (Exception ex)
        {
            WarfareModule.Singleton.GlobalLogger.LogError(
                ex,
                "Event handler threw an exception while invoking IAmmoStorage.AmmoCountUpdated (OnFobDeregistered)."
            );
        }

        _ = GetOrFindRelevantFob();
    }

    private void OnFobSuppliesUpdated(SupplyType type, int amount)
    {
        if (type != SupplyType.Ammo)
            return;

        try
        {
            AmmoCountUpdated?.Invoke(amount);
        }
        catch (Exception ex)
        {
            WarfareModule.Singleton.GlobalLogger.LogError(
                ex,
                "Event handler threw an exception while invoking IAmmoStorage.AmmoCountUpdated (OnFobSuppliesUpdated)."
            );
        }
    }

    SemaphoreSlim IAmmoStorage.InteractSemaphore => _intxSemaphore;

    public override void Dispose()
    {
        _intxSemaphore.Dispose();
        base.Dispose();
    }
}