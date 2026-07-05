using System;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("main", "base", "home", "mainbase", "homebase", "spawn"), SubCommandOf(typeof(DeployCommand)), MetadataFile]
internal sealed class DeployMainCommand : IExecutableCommand
{
    private readonly DeploymentTranslations _translations;
    private readonly DeploymentService _deploymentService;
    private readonly ZoneStore _zoneStore;
    private readonly FobManager _fobManager;
    private readonly FobConfiguration _fobConfig;
    private readonly IDeployMainHandler? _deployMainHandler;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public DeployMainCommand(
        TranslationInjection<DeploymentTranslations> translations,
        DeploymentService deploymentService,
        ZoneStore zoneStore,
        FobManager fobManager,
        FobConfiguration fobConfig,
        IDeployMainHandler? deployMainHandler = null)
    {
        _deploymentService = deploymentService;
        _zoneStore = zoneStore;
        _fobManager = fobManager;
        _fobConfig = fobConfig;
        _deployMainHandler = deployMainHandler;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.Player.Team.IsValid)
        {
            throw Context.Reply(_translations.DeployNotOnTeam);
        }

        if (_zoneStore.IsInMainBase(Context.Player, Context.Player.Team.Faction))
        {
            throw Context.Reply(_translations.DeployAlreadyInMain);
        }

        if (Context.Player.Component<DeploymentComponent>().CurrentDeployment is { } deployment)
        {
            throw Context.Reply(_translations.DeployAlreadyActive, deployment);
        }

        Zone? mainBase = _zoneStore.SearchZone(ZoneType.MainBase, Context.Player.Team.Faction);

        if (mainBase == null)
        {
            throw Context.ReplyString("No main bases configured. Contact a dev.");
        }

        if (_deployMainHandler != null)
        {
            // this is used for insurgency to allow for deploying from caches.
            if (!await _deployMainHandler.CanDeployToMainAsync(Context.Player.Team, mainBase, Context, token))
            {
                if (Context.Responded)
                    return;

                throw Context.Reply(_translations.DeployCancelled);
            }
        }
        else
        {
            BunkerFob? nearestFob = _fobManager.FindNearestBunkerFob(Context.Player.Team, Context.Player.Position, includeUnbuilt: false);

            if (nearestFob == null
                || !MathUtility.WithinRange(
                        nearestFob.SpawnPosition,
                        Context.Player.Position,
                        Math.Min(nearestFob.EffectiveRadius, _fobConfig.MaximumDistanceFromFobToDeployToMain
                    )
                ))
            {
                throw Context.Reply(_translations.DeployNotNearFOB);
            }
        }

        DeploySettings parameters = new DeploySettings
        {
            AllowCombat = false,
            AllowDamage = false,
            AllowInjured = false,
            AllowMovement = false,
            AllowNearbyEnemies = false,
            NearbyEnemyRange = _fobConfig.MaximumDistanceFromEnemiesToDeployToMain,
            // dont really need a cooldown since it'd be hard to get from a FOB to main and back
            DisableCheckingForCooldown = true,
            Delay = _fobConfig.DeployFobToMainDelay
        };

        _deploymentService.TryStartDeployment(Context.Player, mainBase, in parameters);
        
        // DeploymentService uses ChatService
        Context.Defer();
    }
}

/// <summary>
/// Allows overriding the nearby-FOB requirement of /deploy main.
/// </summary>
public interface IDeployMainHandler
{
    /// <summary>
    /// Attempts to deploy to main.
    /// </summary>
    UniTask<bool> CanDeployToMainAsync(Team team, Zone mainBase, CommandContext? context, CancellationToken token = default);
}