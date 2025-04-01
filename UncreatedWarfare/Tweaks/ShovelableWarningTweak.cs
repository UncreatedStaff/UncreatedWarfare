using SDG.Framework.Utilities;
using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Projectiles;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Projectiles;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Tweaks;

internal class ShovelableWarningTweak : IEventListener<ProjectileSpawned>
{
    private readonly FobManager _fobManager;

    public ShovelableWarningTweak(FobManager fobManager)
    {
        _fobManager = fobManager;
    }

    /// <inheritdoc />
    public void HandleEvent(ProjectileSpawned e, IServiceProvider serviceProvider)
    {
        if (e.Vehicle == null)
        {
            return;
        }

        // find emplacement from vehicle ID if it needs to warn enemies or friendlies
        ShovelableInfo? shovelableInfo = _fobManager.Configuration.Shovelables
            .FirstOrDefault(s => s.Emplacement != null
                                 && (s.Emplacement.ShouldWarnEnemies || s.Emplacement.ShouldWarnFriendlies)
                                 && s.Emplacement.Vehicle.MatchAsset(e.Vehicle.asset)
            );

        if (shovelableInfo == null)
        {
            return;
        }

        e.Projectile.OnProjectileSolved += (projectile, projectedHitPosition, projectedImpactTime)
            => OnSolved(shovelableInfo, projectile, projectedHitPosition, projectedImpactTime);
    }

    private static void OnSolved(ShovelableInfo info, WarfareProjectile projectile, Vector3 projectedHitPosition, DateTime projectedImpactTime)
    {
        float warnRadius = projectile.Asset.range;
        if (projectile.Ammo != null)
        {
            warnRadius *= projectile.Ammo.projectileBlastRadiusMultiplier;
        }

        warnRadius += 7.5f;

        GameObject hitPoint = new GameObject("hit_proj_overlap");
        ColliderProximity proximity = hitPoint.AddComponent<ColliderProximity>();

        Team team = projectile.Team;

        LandingZone lz = new LandingZone(info.Emplacement!, team);

        proximity.Initialize(
            new SphereProximity(in projectedHitPosition, warnRadius),
            WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>(),
            false,
            validationCheck: lz.ValidationCheck
        );

        proximity.OnObjectEntered += lz.PlayerEntered;
        proximity.OnObjectExited += lz.PlayerExited;

        lz.Proximity = proximity;

        TimeUtility.InvokeAfterDelay(lz.Destroy, (float)(DateTime.UtcNow - projectedImpactTime).TotalSeconds);
    }

    [PlayerComponent]
    private class LandingZonesComponent : IPlayerComponent
    {
        public int NumLandingZones;
        public required WarfarePlayer Player { get; set; }
        public void Init(IServiceProvider serviceProvider, bool isOnJoin) { }
    }

    private class LandingZone
    {
        public ColliderProximity? Proximity;

        private readonly EmplacementInfo _emplacement;
        private readonly Team _friendlyTeam;

        public LandingZone(EmplacementInfo emplacement, Team friendlyTeam)
        {
            _emplacement = emplacement;
            _friendlyTeam = friendlyTeam;
        }

        public void PlayerEntered(WarfarePlayer player)
        {
            LandingZonesComponent zones = player.Component<LandingZonesComponent>();

            // can be in multiple at once
            if (Interlocked.Increment(ref zones.NumLandingZones) == 1)
            {
                player.SendToast(new ToastMessage(ToastMessageStyle.FlashingWarning,
                    WarfareModule.Singleton.ServiceProvider
                        .Resolve<TranslationInjection<PlayersTranslations>>().Value.MortarWarning.Translate(player))
                );
            }
        }

        public void PlayerExited(WarfarePlayer player)
        {
            LandingZonesComponent zones = player.Component<LandingZonesComponent>();

            // can be in multiple at once
            if (Interlocked.Decrement(ref zones.NumLandingZones) <= 0)
            {
                player.Component<ToastManager>().SkipExpiration(ToastMessageStyle.FlashingWarning);
            }
        }

        public bool ValidationCheck(WarfarePlayer player)
        {
            bool friendly = player.Team.IsFriendly(_friendlyTeam);
            return !friendly && _emplacement.ShouldWarnEnemies ||
                   friendly && _emplacement.ShouldWarnFriendlies;
        }

        public void Destroy()
        {
            Proximity?.Dispose();
        }
    }
}
