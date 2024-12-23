using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.FOBs.Deployment;

namespace Uncreated.Warfare.Events.Models.Deployment;

/// <summary>
/// Event listener args which fires after a player deploys to any type of <see cref="IDeployable"/>.
/// </summary>
internal class PlayerDeployed : PlayerEvent
{
    /// <summary>
    /// The <see cref="IDeployable"/> that the player deployed to.
    /// </summary>
    public required IDeployable Deployable { get; init; }
}
