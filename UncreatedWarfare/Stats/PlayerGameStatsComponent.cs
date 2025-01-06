using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Stats;

[PlayerComponent]
public class PlayerGameStatsComponent : IPlayerComponent, IDisposable
{
    private LeaderboardPhase? _phase;

#nullable disable

#if DEBUG
    private ILogger<PlayerGameStatsComponent> _logger;
#endif
    public WarfarePlayer Player { get; private set; }

#nullable restore

    /// <summary>
    /// This array is the stats of the player's active team. This gets switched out to a different array when they change teams.
    /// </summary>
    public double[] Stats { get; internal set; } = Array.Empty<double>();
    public LongestShot LongestShot { get; set; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        Layout layout = serviceProvider.GetRequiredService<Layout>();
#if DEBUG
        _logger = serviceProvider.GetRequiredService<ILogger<PlayerGameStatsComponent>>();
#endif

        _phase = layout.Phases.OfType<LeaderboardPhase>().FirstOrDefault();

        if (!isOnJoin)
            return;

        UseableGun.onBulletHit += OnBulletHit;
        UseableGun.onBulletSpawned += OnBulletSpawned;
        UseableGun.onProjectileSpawned += OnProjectileSpawned;
    }

    private void OnBulletSpawned(UseableGun gun, BulletInfo bullet)
    {
        if (Player.Equals(gun.player))
            AddToStat(KnownStatNames.AmmoConsumed, 1);
    }

    private void OnProjectileSpawned(UseableGun gun, GameObject projectile)
    {
        if (Player.Equals(gun.player))
            AddToStat(KnownStatNames.AmmoConsumed, 1);
    }

    private void OnBulletHit(UseableGun gun, BulletInfo bullet, InputInfo hit, ref bool shouldallow)
    {
        if (!shouldallow || hit.type != ERaycastInfoType.PLAYER || !Player.Equals(gun.player))
            return;

        InteractableVehicle? vehicle = gun.player.movement.getVehicle();
        if (vehicle != null)
        {
            byte seat = gun.player.movement.getSeat();
            if (vehicle.passengers[seat].turret != null && vehicle.passengers[seat].turret.itemID == gun.equippedGunAsset.id)
                return;
        }

        Vector3 gunPoint = gun.player.look.aim.transform.position;

        float sqrDistance = MathUtility.SquaredDistance(in hit.point, in gunPoint, false);
        if (LongestShot.SquaredDistance < sqrDistance)
            LongestShot = new LongestShot(sqrDistance, AssetLink.Create(gun.equippedGunAsset));
    }

    public void AddToStat(string? statName, int value)
    {
        AddToStat(statName, (double)value);
    }

    public void AddToStat(string? statName, double value)
    {
        if (_phase == null)
            return;

        AddToStat(_phase.GetStatIndex(statName), value);
    }

    public void AddToStat(int index, int value)
    {
        AddToStat(index, (double)value);
    }

    public void AddToStat(int index, double value)
    {
        if (index >= Stats.Length || index < 0 || _phase == null)
            return;
        
        Stats[index] += value;
#if DEBUG
        //_logger.LogDebug("Leaderboard stat updated {0}: {1} -> {2} (+{3}) for player {4} on team {5}.", _phase.PlayerStats[index].Name, Stats[index] - value, Stats[index], value, Player, Player.Team);
#endif
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    void IDisposable.Dispose()
    {
        UseableGun.onBulletHit -= OnBulletHit;
        UseableGun.onBulletSpawned -= OnBulletSpawned;
        UseableGun.onProjectileSpawned -= OnProjectileSpawned;
    }
}

public readonly struct LongestShot
{
    public readonly float SquaredDistance;
    public readonly IAssetLink<ItemGunAsset>? Gun;
    public LongestShot(float squaredDistance, IAssetLink<ItemGunAsset> gun)
    {
        SquaredDistance = squaredDistance;
        Gun = gun;
    }

    /// <summary>
    /// {0}m - Gun
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return Gun.TryGetAsset(out ItemGunAsset? asset)
            ? $"{{0}}m - {asset.itemName}"
            : "{0}m";
    }
}