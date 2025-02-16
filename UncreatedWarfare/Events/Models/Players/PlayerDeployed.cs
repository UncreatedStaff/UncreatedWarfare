using System;
using Uncreated.Warfare.FOBs.Deployment;

namespace Uncreated.Warfare.Events.Models.Players;

public class PlayerDeployed : PlayerEvent
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
}
