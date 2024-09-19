using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Projectiles;
public class ProjectileSpawned
{
    private readonly WarfarePlayer? _player;
    private readonly GameObject _projectile;
    private readonly ItemGunAsset _asset;
    private readonly Rocket _rocketComponent;
    public WarfarePlayer? Player => _player;
    public ItemGunAsset Asset => _asset;
    public Rocket RocketComponent => _rocketComponent;
    public GameObject Object => _projectile;
    public ProjectileSpawned(WarfarePlayer? player, ItemGunAsset asset, GameObject @object, Rocket rocketComponent)
    {
        _player = player;
        _projectile = @object;
        _asset = asset;
        _rocketComponent = rocketComponent;
    }
}
