using Microsoft.Extensions.DependencyInjection;
using Stripe;
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
public class PlayerGameStatsComponent : IPlayerComponent, IDisposable, ILeaderboardRow
{
    private LeaderboardPhase? _phase;
    private PointsUI? _pointsUi;
    private IAssetLink<ItemGunAsset>? _laserDesignator;

#nullable disable

#if DEBUG
    private ILogger<PlayerGameStatsComponent> _logger;
#endif
    public required WarfarePlayer Player { get; init; }

#nullable restore

    /// <summary>
    /// This array is the stats of the player's active team. This gets switched out to a different array when they change teams.
    /// </summary>
    public double[] Stats { get; internal set; } = Array.Empty<double>();
    public LongestShot LongestShot { get; set; }

    Span<double> ILeaderboardRow.Stats => Stats;

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        Layout layout = serviceProvider.GetRequiredService<Layout>();
#if DEBUG
        _logger = serviceProvider.GetRequiredService<ILogger<PlayerGameStatsComponent>>();
#endif

        _phase = layout.Phases.OfType<LeaderboardPhase>().FirstOrDefault();
        _pointsUi = serviceProvider.GetService<PointsUI>();
        _laserDesignator = serviceProvider.GetRequiredService<AssetConfiguration>()
            .GetAssetLink<ItemGunAsset>("Items:LaserDesignator");

        Stats = Array.Empty<double>();

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
        if (!shouldallow || hit.type != ERaycastInfoType.PLAYER || !Player.Equals(gun.player) || _laserDesignator.MatchAsset(gun.equippedGunAsset))
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

    public bool TryGetStatValue(string? statName, out int value)
    {
        if (!TryGetStatValue(statName, out double vDbl))
        {
            value = 0;
            return false;
        }

        value = (int)Math.Round(vDbl);
        return true;
    }

    public bool TryGetStatValue(string? statName, out double value)
    {
        value = 0;
        if (_phase == null)
            return false;

        int index = _phase.GetStatIndex(statName);
        if (index < 0)
            return false;

        value = GetStatValue(_phase.PlayerStats[index]);
        return true;
    }

    public double GetStatValue(LeaderboardPhaseStatInfo stat)
    {
        if (stat.CachedExpression == null)
        {
            if (stat.Index >= Stats.Length || stat.Index < 0 || _phase == null)
                return 0;

            return Stats[stat.Index];
        }

        double calculatedValue = (double)stat.CachedExpression.TryEvaluate(this)!;
        return double.IsFinite(calculatedValue) ? calculatedValue : 0;
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
        if (_pointsUi != null && _pointsUi.IsStatRelevant(_phase.PlayerStats[index]))
        {
            _pointsUi.UpdatePointsUI(Player);
        }
    }

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