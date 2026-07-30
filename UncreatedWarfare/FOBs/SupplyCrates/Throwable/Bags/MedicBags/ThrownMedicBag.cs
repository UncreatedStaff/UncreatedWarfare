using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Bags;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Bags.MedicBags;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Bags.MedicBags;

public class ThrownMedicBag : ThrownBag<PlacedMedicBagComponent>
{
    private readonly float _startingHealingPoints;
    public ThrownMedicBag(
        GameObject throwable,
        WarfarePlayer thrower,
        ItemThrowableAsset thrownAsset,
        ItemBarricadeAsset placedBagAsset,
        IServiceProvider serviceProvider,
        bool isInMain,
        float startingHealingPoints
        )
        : base(throwable, thrower, thrownAsset, placedBagAsset, serviceProvider, isInMain)
    {
        _startingHealingPoints = startingHealingPoints;
    }
    
    protected override void InitPlacedBagComponent(PlacedMedicBagComponent placedAmmoBagComponent, WarfarePlayer owner, IBuildable placedBag, Team team, IServiceProvider serviceProvider)
    {
        placedAmmoBagComponent.Init(Thrower, placedBag, team, serviceProvider, _startingHealingPoints);
    }
}