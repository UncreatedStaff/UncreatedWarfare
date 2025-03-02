using SDG.Framework.Utilities;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using MathUtility = Uncreated.Warfare.Util.MathUtility;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable;

public abstract class ThrownSupplyCrate
{
    protected readonly GameObject _throwable;
    protected readonly WarfarePlayer _thrower;
    protected readonly ItemThrowableAsset _thrownAsset;

    private ThrownComponent _thrownComponent;

    public ThrownSupplyCrate(GameObject throwable, ItemThrowableAsset thrownAsset, WarfarePlayer thrower)
    {
        _throwable = throwable;
        _thrownAsset = thrownAsset;
        _thrower = thrower;
    }
    protected void RespawnThrowableItem()
    {
        ItemManager.dropItem(new Item(_thrownAsset, EItemOrigin.CRAFT), _throwable.transform.position, false, true, false);
    }
}