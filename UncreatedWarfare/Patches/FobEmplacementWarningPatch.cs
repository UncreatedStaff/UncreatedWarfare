using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using Microsoft.Extensions.Configuration;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Projectiles;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class FobEmplacementWarningPatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(UseableGun).GetMethod("project", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, postfix: Accessor.GetMethod(Postfix));
            logger.LogDebug("Patched {0} for adding warning.", _target);
            UseableGun.onProjectileSpawned += UseableGunOnProjectileSpawned;
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("project")
                .DeclaredIn<UseableGun>(isStatic: false)
                .WithNoParameters()
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Postfix));
        logger.LogDebug("Unpatched {0} for cancelling vanilla reputation.", _target);
        UseableGun.onProjectileSpawned -= UseableGunOnProjectileSpawned;
        _target = null;
    }

    private static void UseableGunOnProjectileSpawned(UseableGun sender, GameObject projectile)
    {
        LastProjectileObject = projectile;
    }

    internal static GameObject? LastProjectileObject;

    // SDG.Unturned.UseableGun
    /// <summary>
    /// Postfix of <see cref="UseableGun.project(Vector3, Vector3, ItemBarrelAsset, ItemMagazineAsset)"/> to predict mortar hits.
    /// </summary>
    [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
    private static void Postfix(Vector3 origin, Vector3 direction, ItemBarrelAsset barrelAsset, ItemMagazineAsset magazineAsset, UseableGun __instance)
    {
        if (!WarfareModule.Singleton.IsLayoutActive())
        {
            LastProjectileObject = null;
            return;
        }

        ILifetimeScope serviceProvider = WarfareModule.Singleton.GetActiveLayout().ServiceProvider;

        if (serviceProvider.TryResolve(out FobManager? fobManager)
            && LastProjectileObject is { activeInHierarchy: true }
            && __instance.equippedGunAsset.isTurret)
        {
            // find emplacement from turret item ID
            ShovelableInfo? shovelableInfo =
                (fobManager.Configuration.GetRequiredSection("Shovelables").Get<IEnumerable<ShovelableInfo>>()
                    ?? Array.Empty<ShovelableInfo>())
                .FirstOrDefault(s => s.Emplacement != null
                                     && (s.Emplacement.ShouldWarnEnemies || s.Emplacement.ShouldWarnFriendlies)
                                     && s.Emplacement.Vehicle.GetAsset() is { } vehicle
                                     && vehicle.turrets.Any(y => y.itemID == __instance.equippedGunAsset.id)
                );

            if (shovelableInfo != null)
            {
                serviceProvider.Resolve<ProjectileSolver>().BeginSolvingProjectile(LastProjectileObject, origin, direction, __instance,
                        (owner, position, time, gun, type) => OnMortarLandingPointFound(shovelableInfo, owner, position, time, gun, type)
                );
            }
        }

        LastProjectileObject = null;
    }

    private static void OnMortarLandingPointFound(
        ShovelableInfo info,
        WarfarePlayer? owner,
        Vector3 position,
        float impactTime,
        ItemGunAsset gun,
        ItemMagazineAsset? ammoType)
    {
        float warnRadius = gun.range;
        if (ammoType != null)
        {
            warnRadius *= ammoType.projectileBlastRadiusMultiplier;
        }

        warnRadius += 5;

        GameObject hitPoint = new GameObject("hit_proj_overlap");
        ColliderProximity proximity = hitPoint.AddComponent<ColliderProximity>();

        Team team = owner?.Team ?? Team.NoTeam;

        LandingZone lz = new LandingZone(info.Emplacement!, team);

        proximity.Initialize(
            new SphereProximity(in position, warnRadius),
            WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>(),
            false,
            validationCheck: lz.ValidationCheck
        );

        proximity.OnObjectEntered += lz.PlayerEntered;
        proximity.OnObjectExited += lz.PlayerExited;

        lz.Proximity = proximity;

        TimeUtility.InvokeAfterDelay(lz.Destroy, impactTime - Time.realtimeSinceStartup);
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