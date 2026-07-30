using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Bags;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Bags.AmmoBags;

public class ThrownAmmoBag : ThrownBag<PlacedAmmoBagComponent>
{
    private readonly float _startingAmmo;
    public ThrownAmmoBag(
        GameObject throwable,
        WarfarePlayer thrower,
        ItemThrowableAsset thrownAsset,
        ItemBarricadeAsset placedBagAsset,
        IServiceProvider serviceProvider,
        bool isInMain,
        float startingAmmo
        )
        : base(throwable, thrower, thrownAsset, placedBagAsset, serviceProvider, isInMain)
    {
        _startingAmmo = startingAmmo;
    }

    protected override void InitPlacedBagComponent(PlacedAmmoBagComponent placedAmmoBagComponent, WarfarePlayer owner, IBuildable placedBag, Team team, IServiceProvider serviceProvider)
    {
        placedAmmoBagComponent.Init(Thrower, placedBag, team, serviceProvider, _startingAmmo);
    }
}