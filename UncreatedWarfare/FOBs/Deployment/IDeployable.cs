using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.FOBs.Deployment;
public interface IDeployable : ITranslationArgument
{
    /// <summary>
    /// The position the player will spawn.
    /// </summary>
    Vector3 SpawnPosition { get; }

    /// <summary>
    /// The angle the player will spawn along the Y-axis.
    /// </summary>
    float Yaw { get; }

    /// <summary>
    /// Get the deployment delay in seconds.
    /// </summary>
    TimeSpan GetDelay(WarfarePlayer player);

    /// <summary>
    /// Initial check to see if a player can deploy to the location.
    /// </summary>
    bool CheckDeployableTo(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings);
    
    /// <summary>
    /// Initial check to see if a player can deploy from the location.
    /// </summary>
    bool CheckDeployableFrom(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings, IDeployable deployingTo);

    /// <summary>
    /// Periodic checks over time to see if a player can deploy to the location. This also runs just before the teleport.
    /// </summary>
    bool CheckDeployableToTick(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings);

    /// <summary>
    /// Periodic checks over time to see if a player can deploy from this location. This also runs just before the teleport.
    /// </summary>
    bool CheckDeployableFromTick(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings, IDeployable deployingTo) => false;

    /// <summary>
    /// Invoked after deployment to this location.
    /// </summary>
    void OnDeployTo(WarfarePlayer player, in DeploySettings settings) { }

    /// <summary>
    /// Invoked after deployment from this location.
    /// </summary>
    void OnDeployFrom(WarfarePlayer player, in DeploySettings settings) { }
}