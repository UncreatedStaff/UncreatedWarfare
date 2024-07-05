using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players.Costs;
using Uncreated.Warfare.Players.Unlocks;

namespace Uncreated.Warfare.Vehicles;

[CannotApplyEqualityOperator]
public class WarfareVehicleInfo : IEquatable<WarfareVehicleInfo>
{
    /// <summary>
    /// Since info objects are re-created when they're updated, the old objects still need to stay updated.
    /// </summary>
    internal WeakReference<WarfareVehicleInfo>? DependantInfo;
    public IConfigurationRoot Configuration { get; internal set; }

    public IAssetLink<VehicleAsset> Vehicle { get; set; }
    public VehicleType Type { get; set; }
    public Branch Branch { get; set; }

    public CrewInfo Crew { get; set; }
    public RearmInfo Rearm { get; set; }
    public AbandonInfo Abandon { get; set; }

    public IReadOnlyList<UnlockRequirement> UnlockRequirements { get; set; } = Array.Empty<UnlockRequirement>();
    public IReadOnlyList<UnlockCost> UnlockCosts { get; set; } = Array.Empty<UnlockCost>();
    public IReadOnlyList<TrunkItem> Trunk { get; set; } = Array.Empty<TrunkItem>();
    public IReadOnlyList<Delay> Delays { get; set; } = Array.Empty<Delay>();

    public class CrewInfo
    {
        public IReadOnlyList<byte> Seats { get; set; } = Array.Empty<byte>();
        public bool Invincible { get; set; }
        public bool PassengersInvincible { get; set; }
    }

    public class RearmInfo
    {
        public int AmmoConsumed { get; set; }
        public IReadOnlyList<IAssetLink<ItemAsset>> Items { get; set; } = Array.Empty<IAssetLink<ItemAsset>>();
    }

    public class AbandonInfo
    {
        public bool AllowAbandon { get; set; } = true;
        public double AbandonValueLossSpeed { get; set; } = 0.125d;
    }

    public class TrunkItem
    {
        public IAssetLink<ItemAsset> Item { get; set; }
        public byte X { get; set; }
        public byte Y { get; set; }
        public byte Rotation { get; set; }
        public byte[]? State { get; set; }
    }

    // helper functions are in WarfareVehicleExtensions

    internal void UpdateFrom(WarfareVehicleInfo other)
    {
        if (!other.Vehicle.Equals(Vehicle))
            throw new ArgumentException("Not same vehicle.", nameof(other));

        Type = other.Type;
        Branch = other.Branch;

        Crew.Seats = other.Crew.Seats;
        Crew.Invincible = other.Crew.Invincible;
        Crew.PassengersInvincible = other.Crew.PassengersInvincible;

        Rearm.AmmoConsumed = other.Rearm.AmmoConsumed;
        Rearm.Items = other.Rearm.Items;
        
        Abandon.AbandonValueLossSpeed = other.Abandon.AbandonValueLossSpeed;
        Abandon.AllowAbandon = other.Abandon.AllowAbandon;

        UnlockRequirements = other.UnlockRequirements;
        UnlockCosts = other.UnlockCosts;
        Trunk = other.Trunk;
        Delays = other.Delays;

        if (DependantInfo != null && DependantInfo.TryGetTarget(out WarfareVehicleInfo? info))
        {
            info.UpdateFrom(other);
        }
    }

    public bool Equals(WarfareVehicleInfo? other)
    {
        return other is not null && Vehicle.Equals(other.Vehicle);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as WarfareVehicleInfo);
    }

    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return Vehicle.GetHashCode();
    }
}