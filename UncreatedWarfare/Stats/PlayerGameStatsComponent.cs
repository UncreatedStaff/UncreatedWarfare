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

    public double[] Stats { get; private set; } = Array.Empty<double>();
    public WarfarePlayer Player { get; private set; }
    public LongestShot LongestShot { get; set; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        Layout layout = serviceProvider.GetRequiredService<Layout>();

        _phase = layout.Phases.OfType<LeaderboardPhase>().FirstOrDefault();
        Stats = _phase == null ? Array.Empty<double>() : new double[_phase.PlayerStats.Length];

        if (isOnJoin)
            UseableGun.onBulletHit += OnBulletHit;
    }

    private void OnBulletHit(UseableGun gun, BulletInfo bullet, InputInfo hit, ref bool shouldallow)
    {
        if (!shouldallow || hit.type != ERaycastInfoType.PLAYER)
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

    public void AddToStat(string statName, int value)
    {
        AddToStat(statName, (double)value);
    }

    public void AddToStat(string statName, double value)
    {
        if (_phase == null)
            return;

        int index = _phase.GetStatIndex(statName);
        if (index < 0 || index >= Stats.Length)
            return;

        Stats[index] += value;
    }

    public double GetStat(string statName)
    {
        if (_phase == null)
            return 0;

        int index = _phase.GetStatIndex(statName);
        if (index < 0 || index >= Stats.Length)
            return 0;

        return Stats[index];
    }

    public double GetStat(int statIndex)
    {
        if (statIndex < 0 || statIndex >= Stats.Length)
            return 0;

        return Stats[statIndex];
    }

    public void TryAddToStat(int index, int value)
    {
        TryAddToStat(index, (double)value);
    }

    public void TryAddToStat(int index, double value)
    {
        if (index >= Stats.Length)
            return;

        Stats[index] += value;
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    void IDisposable.Dispose()
    {
        UseableGun.onBulletHit -= OnBulletHit;
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

    public override string ToString()
    {
        return Gun.TryGetAsset(out ItemGunAsset? asset)
            ? $"{{0}}m - {asset.itemName}"
            : "{0}m";
    }
}