using System;

namespace Uncreated.Warfare.FOBs.Deployment;
public struct DeploySettings
{
    // note that some of the booleans are flipped weirdly (disabled instead of enabled)
    // so the default value of the struct can be the defaults

    /// <summary>
    /// Amount of time to wait until deploying.
    /// </summary>
    /// <remarks>Defaults to using <see cref="IDeployable.GetDelay"/>.</remarks>
    public TimeSpan? Delay { get; set; }

    /// <summary>
    /// If the player can move while waiting.
    /// </summary>
    public bool AllowMovement { get; set; }

    /// <summary>
    /// If the player can be damaged while waiting.
    /// </summary>
    public bool AllowDamage { get; set; }

    /// <summary>
    /// If the player can be injured before or while waiting.
    /// </summary>
    public bool AllowInjured { get; set; }

    /// <summary>
    /// If the player can be on combat cooldown before or while waiting.
    /// </summary>
    public bool AllowCombat { get; set; }

    /// <summary>
    /// If the player can deploy while enemies are nearby.
    /// </summary>
    public bool AllowNearbyEnemies { get; set; }

    /// <summary>
    /// Optionally change the distance to check for nearby enemies from <see cref="DeploymentService.DefaultNearbyEnemyRange"/>.
    /// </summary>
    public float? NearbyEnemyRange { get; set; }

    /// <summary>
    /// If a cooldown should be started on successful deployment.
    /// </summary>
    public bool DisableStartingCooldown { get; set; }

    /// <summary>
    /// If cooldowns should be checked for on deployment.
    /// </summary>
    public bool DisableCheckingForCooldown { get; set; }

    /// <summary>
    /// Optionally change the cooldown type from <see cref="CooldownType.Deploy"/>.
    /// </summary>
    public CooldownType? CooldownType { get; set; }

    /// <summary>
    /// If chat interaction with the player should be used for the initial check.
    /// </summary>
    public bool DisableInitialChatUpdates { get; set; }

    /// <summary>
    /// If chat interaction with the player should be used after the initial check.
    /// </summary>
    public bool DisableTickingChatUpdates { get; set; }
}