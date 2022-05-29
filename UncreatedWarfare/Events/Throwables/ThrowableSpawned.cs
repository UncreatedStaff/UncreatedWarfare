using SDG.Unturned;
using UnityEngine;

namespace Uncreated.Warfare.Events.Vehicles;
public class ThrowableSpawned : PlayerEvent
{
    private readonly GameObject _throwable;
    private readonly ItemThrowableAsset _asset;
    public ItemThrowableAsset Asset => _asset;
    public GameObject Object => _throwable;
    public ThrowableSpawned(UCPlayer player, ItemThrowableAsset asset, GameObject @object) : base(player)
    {
        this._throwable = @object;
        this._asset = asset;
    }
}
