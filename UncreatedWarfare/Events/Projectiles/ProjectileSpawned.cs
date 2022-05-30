using SDG.Unturned;
using UnityEngine;

namespace Uncreated.Warfare.Events.Vehicles;
public class ProjectileSpawned : EventState
{
    private readonly UCPlayer? _player;
    private readonly GameObject _projectile;
    private readonly ItemGunAsset _asset;
    private readonly SDG.Unturned.Rocket _rocketComponent;
    public UCPlayer? Player => _player;
    public ItemGunAsset Asset => _asset;
    public SDG.Unturned.Rocket RocketComponent => _rocketComponent;
    public GameObject Object => _projectile;
    public ProjectileSpawned(UCPlayer? player, ItemGunAsset asset, GameObject @object, SDG.Unturned.Rocket rocketComponent)
    {
        this._player = player;
        this._projectile = @object;
        this._asset = asset;
        this._rocketComponent = rocketComponent;
    }
}
