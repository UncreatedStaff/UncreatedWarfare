using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Squads.Spotted;

[Priority(1)]
internal sealed class SpottedService : ILayoutHostedService
{
    private readonly List<SpottableObjectComponent> _allSpottableObjects;
    
    private readonly ILogger<SpottedService> _logger;


    private readonly WarfareModule _module;
    private readonly IPlayerService _playerService;
    private readonly IAssetLink<ItemGunAsset> _laserDesignator;
    private readonly ITranslationValueFormatter _formatter;
    private readonly SpottedTranslations _translations;
    private readonly ChatService _chatService;
    private readonly FobConfiguration _fobConfiguration;
    private ITeamManager<Team>? _teamManager;

    public IReadOnlyList<SpottableObjectComponent> AliveSpottableObjects { get; }

    public SpottedService(IServiceProvider serviceProvider, ILogger<SpottedService> logger)
    {
        _logger = logger;

        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _module = serviceProvider.GetRequiredService<WarfareModule>();
        _formatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<SpottedTranslations>>().Value;
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _fobConfiguration = serviceProvider.GetRequiredService<FobConfiguration>();
        _laserDesignator = serviceProvider.GetRequiredService<AssetConfiguration>()
                                          .GetAssetLink<ItemGunAsset>("Items:LaserDesignator");

        _allSpottableObjects = new List<SpottableObjectComponent>(64);
        AliveSpottableObjects = new ReadOnlyCollection<SpottableObjectComponent>(_allSpottableObjects);
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        UseableGun.onBulletHit += UseableGunOnBulletHit;
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        UseableGun.onBulletHit -= UseableGunOnBulletHit;

        foreach (SpottableObjectComponent comp in _allSpottableObjects)
        {
            comp.RemoveAllSpotters();
        }

        _teamManager = null;
        return UniTask.CompletedTask;
    }

    private void UseableGunOnBulletHit(UseableGun gun, BulletInfo bullet, InputInfo hit, ref bool shouldAllow)
    {
        if (!_laserDesignator.MatchAsset(gun.equippedGunAsset) || !shouldAllow)
        {
            return;
        }

        if (hit.transform == null)
        {
            shouldAllow = false;
            return;
        }

        WarfarePlayer spotter = _playerService.GetOnlinePlayer(gun.player);

        if (_teamManager == null && _module.IsLayoutActive())
            _teamManager = _module.GetActiveLayout().TeamManager;

        IBuildable? buildable = null;
        switch (hit.type)
        {
            // infantry
            case ERaycastInfoType.PLAYER:
                WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(hit.player);
                if (player == null || !spotter.Team.IsOpponent(player.Team))
                {
                    _logger.LogDebug("Invalid spot: no player team: {0}.", player);
                    break;
                }

                SpottableObjectComponent? spotted = SpottableObjectComponent.GetOrAddIfValid(player);
                if (spotted == null)
                    break;

                _logger.LogConditional("Spotting player {0}", player);
                Spot(spotter, player.Team, spotted, _translations.SpottedTargetPlayer.Translate());
                break;

            // vehicles
            case ERaycastInfoType.VEHICLE:
                InteractableVehicle? vehicle = hit.vehicle;
                if (vehicle == null || vehicle.isDead || vehicle.isDrowned || vehicle.isExploded)
                {
                    _logger.LogDebug("Invalid spot: no vehicle found.");
                    break;
                }

                Team? team = _teamManager?.GetTeam(vehicle.lockedGroup);
                if (team is null || !team.IsValid || !team.IsOpponent(spotter.Team))
                {
                    _logger.LogDebug("Invalid spot: no vehicle team: {0}.", buildable);
                    break;
                }

                spotted = SpottableObjectComponent.GetOrAddIfValid(vehicle);
                if (spotted == null)
                    break;

                _logger.LogConditional("Spotting vehicle {0}.", vehicle.asset.vehicleName);
                Spot(spotter, team, spotted, vehicle.transform.TryGetComponent(out WarfareVehicleComponent vc)
                    ? _formatter.FormatEnum(vc.WarfareVehicle.Info.Type, null)
                    : vehicle.asset.vehicleName
                );
                break;

            // buildables
            case ERaycastInfoType.STRUCTURE:
                StructureDrop? structure = StructureManager.FindStructureByRootTransform(hit.transform);
                if (structure == null)
                {
                    _logger.LogDebug("Invalid spot: no structure found.");
                    break;
                }

                buildable = new BuildableStructure(structure);
                goto case ERaycastInfoType.BARRICADE;

            case ERaycastInfoType.BARRICADE:
                if (buildable == null)
                {
                    BarricadeDrop? barricade = BarricadeManager.FindBarricadeByRootTransform(hit.transform);
                    if (barricade == null)
                    {
                        _logger.LogDebug("Invalid spot: no barricade found.");
                        break;
                    }

                    buildable = new BuildableBarricade(barricade);
                }

                team = _teamManager?.GetTeam(buildable.Group);
                if (team is null || !team.IsValid || !team.IsOpponent(spotter.Team))
                {
                    _logger.LogDebug("Invalid spot: no team on buildable: {0}.", buildable);
                    break;
                }

                ShovelableInfo? shovelable = _fobConfiguration.Shovelables.FirstOrDefault(x => x.CompletedStructure.MatchAsset(buildable.Asset));
                if (shovelable is not { ConstuctionType: ShovelableType.Fob })
                {
                    _logger.LogDebug("Invalid spot: not FOB: {0}.", buildable);
                    break;
                }

                spotted = SpottableObjectComponent.GetOrAddIfValid(buildable);
                if (spotted == null)
                    break;

                _logger.LogConditional("Spotting buildable {0}.", buildable.Asset.itemName);
                Spot(spotter, team, spotted, _translations.SpottedTargetFOB.Translate());
                break;
        }

        shouldAllow = false;
    }

    private void Spot(WarfarePlayer spotter, Team targetTeam, SpottableObjectComponent spottedComp, string targetName)
    {
        if (!spottedComp.TryAddSpotter(spotter))
        {
            _logger.LogConditional(" - already spotted.");
            return;
        }

        spotter.SendToast(new ToastMessage(ToastMessageStyle.Mini, _translations.SpottedToast.Translate(spotter)));

        Team t = spotter.Team;
        Color t1 = t.Faction.Color;

        targetName = TranslationFormattingUtility.Colorize(targetName, targetTeam.Faction.Color);

        _chatService.Broadcast(_formatter.TranslationService.SetOf.PlayersOnTeam(t), _translations.SpottedMessage, t1, targetName, spotter);
    }

    internal void AddSpottableObject(SpottableObjectComponent comp)
    {
        _allSpottableObjects.Add(comp);
    }

    internal void RemoveSpottableObject(SpottableObjectComponent comp)
    {
        _allSpottableObjects.Remove(comp);
    }
}