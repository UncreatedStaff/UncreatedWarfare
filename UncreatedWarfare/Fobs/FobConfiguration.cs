using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable.AmmoBags;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Vehicle;

namespace Uncreated.Warfare.Fobs;

/// <summary>
/// Home for storing FOB and buildable data.
/// </summary>
public sealed class FobConfiguration : BaseAlternateConfigurationFile
{
    public IReadOnlyList<SupplyCrateInfo> SupplyCrates { get; private set; } = null!;
    public IReadOnlyList<ThrownAmmoBagInfo> ThrowableAmmoBags { get; private set; } = null!;
    public IReadOnlyList<ThrownVehicleCrateInfo> ThrowableVehicleSupplyCrates { get; private set; } = null!;
    public IReadOnlyList<ShovelableInfo> Shovelables { get; private set; } = null!;

    /// <summary>
    /// Home for storing FOB and buildable data.
    /// </summary>
    public FobConfiguration() : base("Fobs.yml")
    {
        HandleChange();
    }

    /// <inheritdoc />
    protected override void HandleChange()
    {
        List<SupplyCrateInfo>? supplyCrates = UnderlyingConfiguration.GetSection("SupplyCrates").Get<List<SupplyCrateInfo>>();
        supplyCrates?.ForEach(crate =>
        {
            crate.SupplyItemAsset ??= AssetLink.Empty<ItemPlaceableAsset>();
            crate.PlacementEffect ??= AssetLink.Empty<EffectAsset>();
        });
        SupplyCrates = new ReadOnlyCollection<SupplyCrateInfo>((IList<SupplyCrateInfo>?)supplyCrates ?? Array.Empty<SupplyCrateInfo>());

        List<ThrownVehicleCrateInfo>? vehicleCrates = UnderlyingConfiguration.GetSection("ThrowableVehicleSupplyCrates").Get<List<ThrownVehicleCrateInfo>>();
        vehicleCrates?.ForEach(crate =>
        {
            crate.ThrowableItemAsset ??= AssetLink.Empty<ItemAsset>();
            crate.ResupplyEffect ??= AssetLink.Empty<EffectAsset>();
        });
        ThrowableVehicleSupplyCrates = new ReadOnlyCollection<ThrownVehicleCrateInfo>((IList<ThrownVehicleCrateInfo>?)vehicleCrates ?? Array.Empty<ThrownVehicleCrateInfo>());

        List<ThrownAmmoBagInfo>? ammoBags = UnderlyingConfiguration.GetSection("ThrowableAmmoBags").Get<List<ThrownAmmoBagInfo>>();
        ammoBags?.ForEach(crate =>
        {
            crate.ThrowableItemAsset ??= AssetLink.Empty<ItemAsset>();
            crate.AmmoBagBarricadeAsset ??= AssetLink.Empty<ItemBarricadeAsset>();
        });
        ThrowableAmmoBags = new ReadOnlyCollection<ThrownAmmoBagInfo>((IList<ThrownAmmoBagInfo>?)ammoBags ?? Array.Empty<ThrownAmmoBagInfo>());
        
        List<ShovelableInfo>? shovelables = UnderlyingConfiguration.GetSection("Shovelables").Get<List<ShovelableInfo>>();
        shovelables?.ForEach(info =>
        {
            info.Foundation ??= AssetLink.Empty<ItemPlaceableAsset>();
            if (info.Emplacement is { Vehicle: null })
                info.Emplacement = null;
        });
        Shovelables = new ReadOnlyCollection<ShovelableInfo>((IList<ShovelableInfo>?)shovelables ?? Array.Empty<ShovelableInfo>());
    }
}