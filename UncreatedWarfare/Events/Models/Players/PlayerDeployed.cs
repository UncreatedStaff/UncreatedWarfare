using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.FOBs.Deployment;

namespace Uncreated.Warfare.Events.Models.Players;

public class PlayerDeployed : PlayerEvent, IActionLoggableEvent
{
    /// <summary>
    /// Location where the player deployed to.
    /// </summary>
    public required IDeployable Destination { get; init;  }

    /// <summary>
    /// Deployment settings influencing deployment behavior.
    /// </summary>
    public required DeploySettings Settings { get; init; }

    /// <summary>
    /// Location the player deployed from.
    /// </summary>
    public required Vector3 FromLocation { get; init; }

    /// <summary>
    /// Location the player deployed to.
    /// </summary>
    public required Quaternion FromRotation { get; init; }

    /// <summary>
    /// Time at which the deployment started in UTC.
    /// </summary>
    public required DateTime DeployStartTime { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.Chat,
            $"Destination: {Destination}, Settings: (combat: {(Settings.AllowCombat ? "T" : "F")}, " +
            $"movement: {(Settings.AllowMovement ? "T" : "F")}, " +
            $"injure: {(Settings.AllowInjured ? "T" : "F")}, " +
            $"enemies: {(Settings.AllowNearbyEnemies ? "T" : "F")}, " +
            $"delay: {Settings.Delay?.ToString() ?? "default"}, " +
            $"yaw: {Settings.YawOverride?.ToString("F2") ?? "default"}, " +
            $"cooldown: \"{Settings.CooldownType}\"), From: {FromLocation:F2}, {FromRotation:F2}, Started at: {DeployStartTime:O}",
            Player.Steam64.m_SteamID
        );
    }
}
