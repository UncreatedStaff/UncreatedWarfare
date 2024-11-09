﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players.Costs;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.Vehicles;

[CannotApplyEqualityOperator]
public class WarfareVehicleInfo : IEquatable<WarfareVehicleInfo>, ITranslationArgument
{
    /// <summary>
    /// Since info objects are re-created when they're updated, the old objects still need to stay updated.
    /// </summary>
    internal WeakReference<WarfareVehicleInfo>? DependantInfo;
    public IConfigurationRoot Configuration { get; internal set; }

    public IAssetLink<VehicleAsset> Vehicle { get; set; }
    public VehicleType Type { get; set; }
    public Branch Branch { get; set; }
    public Class Class { get; set; }
    public int TicketCost { get; set; }
    public TimeSpan RespawnTime { get; set; }
    public TimeSpan Cooldown { get; set; }

    /// <remarks>
    /// Some vehicles like the F15-E have names that are too long and result in the final sign having too many characters.
    /// </remarks>
    public string? ShortName { get; set; }

    public CrewInfo Crew { get; set; }
    public RearmInfo Rearm { get; set; }
    public AbandonInfo Abandon { get; set; }

    public IReadOnlyList<UnlockRequirement> UnlockRequirements { get; set; } = Array.Empty<UnlockRequirement>();
    public IReadOnlyList<UnlockCost> UnlockCosts { get; set; } = Array.Empty<UnlockCost>();
    public IReadOnlyList<TrunkItem> Trunk { get; set; } = Array.Empty<TrunkItem>();
    public IReadOnlyList<RequestItem> RequestItems { get; set; } = Array.Empty<RequestItem>();
    //public IReadOnlyList<Delay> Delays { get; set; } = Array.Empty<Delay>();

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

        /// <summary>
        /// Amount of credits lost per second of vehicle life.
        /// </summary>
        public double ValueLossSpeed { get; set; } = 0.125d;
    }

    public class RequestItem
    {
        public IAssetLink<ItemAsset> Item { get; set; }
        public byte[]? State { get; set; }
        public int Amount { get; set; } = 1;
    }

    public class TrunkItem
    {
        public byte X { get; set; }
        public byte Y { get; set; }
        public byte Rotation { get; set; }
        public IAssetLink<ItemAsset> Item { get; set; }
        public byte[]? State { get; set; }
    }

    // helper functions are in WarfareVehicleExtensions

    internal void UpdateFrom(WarfareVehicleInfo other)
    {
        if (!other.Vehicle.Equals(Vehicle))
            throw new ArgumentException("Not same vehicle.", nameof(other));

        Type = other.Type;
        Branch = other.Branch;
        Class = other.Class;
        TicketCost = other.TicketCost;
        RespawnTime = other.RespawnTime;
        Cooldown = other.Cooldown;

        RequestItems = other.RequestItems;

        Crew.Seats = other.Crew.Seats;
        Crew.Invincible = other.Crew.Invincible;
        Crew.PassengersInvincible = other.Crew.PassengersInvincible;

        Rearm.AmmoConsumed = other.Rearm.AmmoConsumed;
        Rearm.Items = other.Rearm.Items;
        
        Abandon.ValueLossSpeed = other.Abandon.ValueLossSpeed;
        Abandon.AllowAbandon = other.Abandon.AllowAbandon;

        UnlockRequirements = other.UnlockRequirements;
        UnlockCosts = other.UnlockCosts;
        Trunk = other.Trunk;
        // todo Delays = other.Delays;

        if (DependantInfo != null && DependantInfo.TryGetTarget(out WarfareVehicleInfo? info))
        {
            info.UpdateFrom(other);
        }
        else
        {
            DependantInfo = null;
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


    public static readonly SpecialFormat FormatNameColored = new SpecialFormat("Colored Vehicle Name", "cn");

    public static readonly SpecialFormat FormatName = new SpecialFormat("Vehicle Name", "n");

    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        // todo
        return ToString();
    }
}