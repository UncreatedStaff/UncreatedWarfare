using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.Deployment;
public class DeploymentService : ILayoutHostedService
{
    public const float DefaultNearbyEnemyRange = 35;

    private readonly IPlayerService _playerService;
    public DeploymentService(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            player.Component<DeploymentComponent>().CancelDeployment(false);
        }

        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Begin deployment to a location.
    /// </summary>
    /// <returns>Whether or not the initial check was successful. Future tick checks may still fail.</returns>
    public bool TryStartDeployment(WarfarePlayer player, IDeployable location, in DeploySettings settings)
    {
        return player.Component<DeploymentComponent>().TryStartDeployment(location, settings);
    }

    /// <summary>
    /// Cancel all deployments to a given <paramref name="location"/>.
    /// </summary>
    public void CancelDeploymentsTo(IDeployable location, bool chat)
    {
        GameThread.AssertCurrent();

        if (location == null)
            throw new ArgumentNullException(nameof(location));

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            DeploymentComponent component = player.Component<DeploymentComponent>();
            if (Equals(component.CurrentDeployment, location))
            {
                component.CancelDeployment(chat);
            }
        }
    }
}
