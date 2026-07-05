using System;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;

/// <summary>
/// Handles the <see cref="IMapTackUIHandler.OnSuppliesUpdated"/> event.
/// </summary>
/// <param name="type">The type of supply that was updated.</param>
/// <param name="amount">The new amount of that supply.</param>
public delegate void SuppliesUpdated(SupplyType type, int amount);

/// <summary>
/// Handles the <see cref="IMapTackUIHandler.OnVehicleUpdated"/> event.
/// </summary>
/// <param name="type">Type of vehicle count that was updated.</param>
/// <param name="amount">New number of that type of vehicle.</param>
public delegate void VehicleUpdated(MapTackVehicleType type, int amount);

/// <summary>
/// Handles the <see cref="IMapTackUIHandler.OnHealthUpdated"/> event.
/// </summary>
/// <param name="health">Percentage of the current health (0 to 1).</param>
public delegate void HealthUpdated(double? health);

/// <summary>
/// Handles the <see cref="IMapTackUIHandler.OnAttributesUpdated"/> event.
/// </summary>
/// <param name="attributes">The current attributes.</param>
public delegate void AttributesUpdated(MapTackAttributes attributes);

/// <summary>
/// Defines an API for customizing the display of the <see cref="MapTackInfoUI"/>.
/// </summary>
public interface IMapTackUIHandler
{
    /// <summary>
    /// Invoked when a supply count is updated.
    /// </summary>
    event SuppliesUpdated? OnSuppliesUpdated;

    /// <summary>
    /// Invoked when a vehicle gets an updated count.
    /// </summary>
    event VehicleUpdated? OnVehicleUpdated;

    /// <summary>
    /// Invoked when this tack's buildable's health changes.
    /// </summary>
    event HealthUpdated? OnHealthUpdated;

    /// <summary>
    /// Invoked when this tack's buildable's attributes change.
    /// </summary>
    event AttributesUpdated? OnAttributesUpdated;

    /// <summary>
    /// Gets the title for the given set of players.
    /// </summary>
    /// <returns>The localized title to show.</returns>
    string GetTitle(in LanguageSet languageSet);

    /// <summary>
    /// Gets the name of the closest location.
    /// </summary>
    /// <returns>The closest location, or <see langword="null"/> to not show the location.</returns>
    string? GetLocation(in LanguageSet languageSet);

    /// <summary>
    /// Gets the total number of supplies for the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">Type of supplies to check.</param>
    /// <returns>The number of supplies, or <see langword="null"/> to not show this supply type.</returns>
    int? GetSupplyCount(SupplyType type);

    /// <summary>
    /// Gets the current health percentage (0 to 1) of the tack's buildable.
    /// </summary>
    /// <returns>The current health, or <see langword="null"/> to not show the health.</returns>
    double? GetHealth();

    /// <summary>
    /// Gets the current attributes for this tack's buildable.
    /// </summary>
    MapTackAttributes GetAttributes();

    /// <summary>
    /// Count vehicles and ouput them to <paramref name="vehicleCounts"/>.
    /// </summary>
    void CountVehicles(IList<KeyValuePair<MapTackVehicleType, int>> vehicleCounts);
}

/// <summary>
/// Defines the various attributes that can show at the top of the UI.
/// </summary>
[Flags]
public enum MapTackAttributes
{
    /// <summary>
    /// A FOB or CACHE was destroyed completely.
    /// </summary>
    Destroyed = 1,

    /// <summary>
    /// A FOB or CACHE is being proxied by nearby enemies.
    /// </summary>
    Proxied = 2,

    /// <summary>
    /// A FOB or CACHE is low on building supplies.
    /// </summary>
    LowBuild = 4,

    /// <summary>
    /// A FOB or CACHE is low on ammo supplies.
    /// </summary>
    LowAmmo = 8,

    /// <summary>
    /// A FOB has yet to be shoveled.
    /// </summary>
    NotBuilt = 16
}

/// <summary>
/// Defines the various types of vehicles that can be shown on the map tack UI.
/// </summary>
public enum MapTackVehicleType
{
    /// <summary>
    /// Doesn't fall into a category.
    /// </summary>
    Other,

    /// <summary>
    /// Players.
    /// </summary>
    Infantry,

    /// <summary>
    /// Anti-Air or Anti-Tank. Corresponds to the <see cref="VehicleType.AA"/> and <see cref="VehicleType.ATGM"/> vehicle types.
    /// </summary>
    AA,

    /// <summary>
    /// Mortars. Corresponds to the <see cref="VehicleType.Mortar"/> vehicle type.
    /// </summary>
    Mortar,

    /// <summary>
    /// Heavy machine guns. Corresponds to the <see cref="VehicleType.HMG"/> vehicle type.
    /// </summary>
    HMG,

    /// <summary>
    /// Attack helicopters. Corresponds to the <see cref="VehicleType.AttackHeli"/> vehicle type.
    /// </summary>
    AttackHeli,

    /// <summary>
    /// Fighter jets. Corresponds to the <see cref="VehicleType.Jet"/> vehicle type.
    /// </summary>
    Jet,

    /// <summary>
    /// Transport helicopters. Corresponds to the <see cref="VehicleType.TransportHeli"/> vehicle type.
    /// </summary>
    TransportHeli,

    /// <summary>
    /// Armored personnel carriers. Corresponds to the <see cref="VehicleType.APC"/> vehicle type.
    /// </summary>
    APC,

    /// <summary>
    /// High-mobility vehicles. Corresponds to the <see cref="VehicleType.Humvee"/> vehicle type.
    /// </summary>
    Humvee,

    /// <summary>
    /// Infantry fighting vehicle. Corresponds to the <see cref="VehicleType.IFV"/> vehicle type.
    /// </summary>
    IFV,

    /// <summary>
    /// Battle tanks. Corresponds to the <see cref="VehicleType.MBT"/> vehicle type.
    /// </summary>
    MBT,

    /// <summary>
    /// Lightly armored mobility vehicles. Corresponds to the <see cref="VehicleType.ScoutCar"/> vehicle type.
    /// </summary>
    ScoutCar,

    /// <summary>
    /// Transport and logistic trucks. Corresponds to the <see cref="VehicleType.TransportGround"/> and <see cref="VehicleType.LogisticsGround"/> vehicle types.
    /// </summary>
    Truck
}

/// <summary>
/// Extensions for <see cref="MapTackVehicleType"/>.
/// </summary>
public static class MapTackVehicleTypeExtensions
{
    extension(MapTackVehicleType)
    {
        /// <summary>
        /// Get the corresponding <see cref="MapTackVehicleType"/> for a <see cref="VehicleType"/>.
        /// </summary>
        public static MapTackVehicleType FromVehicleType(VehicleType type)
        {
            return type switch
            {
                VehicleType.Humvee => MapTackVehicleType.Humvee,
                VehicleType.TransportGround => MapTackVehicleType.Truck,
                VehicleType.ScoutCar => MapTackVehicleType.ScoutCar,
                VehicleType.LogisticsGround => MapTackVehicleType.Truck,
                VehicleType.APC => MapTackVehicleType.APC,
                VehicleType.IFV => MapTackVehicleType.IFV,
                VehicleType.MBT => MapTackVehicleType.MBT,
                VehicleType.TransportHeli => MapTackVehicleType.TransportHeli,
                VehicleType.AttackHeli => MapTackVehicleType.AttackHeli,
                VehicleType.Jet => MapTackVehicleType.Jet,
                VehicleType.AA => MapTackVehicleType.AA,
                VehicleType.HMG => MapTackVehicleType.HMG,
                VehicleType.ATGM => MapTackVehicleType.AA,
                VehicleType.Mortar => MapTackVehicleType.Mortar,
                _ => MapTackVehicleType.Other
            };
        }
        
        /// <summary>
        /// Maximum value + 1.
        /// </summary>
        public static int Count => (int)MapTackVehicleType.Truck + 1;
    }
}