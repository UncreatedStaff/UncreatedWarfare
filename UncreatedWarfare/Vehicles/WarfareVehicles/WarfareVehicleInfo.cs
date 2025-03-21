using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players.Costs;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles;

[CannotApplyEqualityOperator]
public class WarfareVehicleInfo : IEquatable<WarfareVehicleInfo>, ITranslationArgument
{
    /// <summary>
    /// Since info objects are re-created when they're updated, the old objects still need to stay updated.
    /// </summary>
    internal WeakReference<WarfareVehicleInfo>? DependantInfo;
    public IConfigurationRoot Configuration { get; internal set; }

    public IAssetLink<VehicleAsset> VehicleAsset { get; set; }
    public VehicleType Type { get; set; }
    public Branch Branch { get; set; } = Branch.Default;
    public Class Class { get; set; } = Class.None;
    public int TicketCost { get; set; } = 0;
    public TimeSpan RespawnTime { get; set; } = TimeSpan.Zero;
    public TimeSpan Cooldown { get; set; } = TimeSpan.Zero;
    public Color32 PaintColor { get; set; }

    /// <remarks>
    /// Some vehicles like the F15-E have names that are too long and result in the final sign having too many characters.
    /// </remarks>
    public string? ShortName { get; set; }

    public required CrewInfo Crew { get; set; }
    public required RearmInfo Rearm { get; set; }
    public required AbandonInfo Abandon { get; set; }

    public IReadOnlyList<UnlockRequirement> UnlockRequirements { get; set; } = Array.Empty<UnlockRequirement>();
    public required int CreditCost { get; set; }
    public required bool WipeTrunkOnDestroyed { get; set; } = true;
    public IReadOnlyList<TrunkItem> Trunk { get; set; } = Array.Empty<TrunkItem>();
    public IReadOnlyList<IAssetLink<ItemAsset>> StartingItems { get; set; } = Array.Empty<IAssetLink<ItemAsset>>();

    public static void EnsureInitialized(WarfareVehicleInfo v)
    {
        v.VehicleAsset ??= AssetLink.Empty<VehicleAsset>();
        v.Configuration ??= new ConfigurationBuilder().Build();

        v.Crew ??= new CrewInfo();
        v.Abandon ??= new AbandonInfo();
        v.Rearm ??= new RearmInfo();

        v.UnlockRequirements ??= Array.Empty<UnlockRequirement>();
        v.Trunk ??= Array.Empty<TrunkItem>();

        v.Crew.Seats ??= Array.Empty<int>();
        v.Rearm.Items ??= Array.Empty<IAssetLink<ItemAsset>>();
        v.StartingItems ??= Array.Empty<IAssetLink<ItemAsset>>();
    }

    public class CrewInfo
    {
        public IReadOnlyList<int> Seats { get; set; } = Array.Empty<int>();
        public bool Invincible { get; set; }
        public bool PassengersInvincible { get; set; }
        public int? MaxAllowedCrew { get; set; }
    }

    public class RearmInfo
    {
        public int AmmoConsumed { get; set; } = 1;
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

    public class TrunkItem
    {
        public required IAssetLink<ItemAsset> Item { get; set; }
        public required byte X { get; set; }
        public required byte Y { get; set; }
        public required byte Rotation { get; set; }
        public byte[]? State { get; set; }
    }

    // helper functions are in WarfareVehicleExtensions

    internal void UpdateFrom(WarfareVehicleInfo other)
    {
        if (!other.VehicleAsset.Equals(VehicleAsset))
            throw new ArgumentException("Not same vehicle.", nameof(other));

        Type = other.Type;
        Branch = other.Branch;
        Class = other.Class;
        TicketCost = other.TicketCost;
        RespawnTime = other.RespawnTime;
        Cooldown = other.Cooldown;

        Crew.Seats = other.Crew.Seats;
        Crew.Invincible = other.Crew.Invincible;
        Crew.PassengersInvincible = other.Crew.PassengersInvincible;

        Rearm.AmmoConsumed = other.Rearm.AmmoConsumed;
        Rearm.Items = other.Rearm.Items;

        Abandon.ValueLossSpeed = other.Abandon.ValueLossSpeed;
        Abandon.AllowAbandon = other.Abandon.AllowAbandon;

        UnlockRequirements = other.UnlockRequirements;
        CreditCost = other.CreditCost;
        WipeTrunkOnDestroyed = other.WipeTrunkOnDestroyed;
        Trunk = other.Trunk;
        StartingItems = other.StartingItems;
        // todo Delays = other.Delays;

        WeakReference<WarfareVehicleInfo>? dependent = DependantInfo;
        if (dependent != null && dependent.TryGetTarget(out WarfareVehicleInfo? info))
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
        return other is not null && VehicleAsset.Equals(other.VehicleAsset);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as WarfareVehicleInfo);
    }

    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return VehicleAsset.GetHashCode();
    }


    public static readonly SpecialFormat FormatNameColored = new SpecialFormat("Colored Vehicle Name", "cn");

    public static readonly SpecialFormat FormatName = new SpecialFormat("Vehicle Name", "n");

    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        // todo
        return ToString();
    }

    public static WarfareVehicleInfo CreateDefault(VehicleAsset vehicleAsset)
    {
        return new WarfareVehicleInfo
        {
            DependantInfo = null,
            Configuration = new ConfigurationBuilder().Build(),
            VehicleAsset = AssetLink.Create(vehicleAsset),
            Type = VehicleType.None,
            Branch = Branch.Default,
            Class = Class.None,
            TicketCost = 0,
            CreditCost = 0,
            WipeTrunkOnDestroyed = true,
            RespawnTime = TimeSpan.Zero,
            Cooldown = TimeSpan.Zero,
            PaintColor = default,
            ShortName = null,
            Crew = new CrewInfo(),
            Rearm = new RearmInfo(),
            Abandon = new AbandonInfo(),
            StartingItems = Array.Empty<IAssetLink<ItemAsset>>(),
        };
    }
}