using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Projectiles;
public class ProjectileSpawned
{
    required public WarfarePlayer Player { get; init; }
    required public ItemGunAsset Asset { get; init; }
    required public Rocket RocketComponent { get; init; }
    required public GameObject Object { get; init; }
}
