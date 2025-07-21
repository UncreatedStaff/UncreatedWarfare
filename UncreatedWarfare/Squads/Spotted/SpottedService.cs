using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Events.Models.Vehicles;
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
internal sealed class SpottedService : ILayoutHostedService, IEventListener<VehicleExploded>
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
    private readonly EventDispatcher _eventDispatcher;
    private ITeamManager<Team>? _teamManager;

    public IReadOnlyList<SpottableObjectComponent> AllSpottableObjects { get; }

    public SpottedService(IServiceProvider serviceProvider, ILogger<SpottedService> logger)
    {
        _logger = logger;

        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _module = serviceProvider.GetRequiredService<WarfareModule>();
        _eventDispatcher = serviceProvider.GetRequiredService<EventDispatcher>();
        _formatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<SpottedTranslations>>().Value;
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _fobConfiguration = serviceProvider.GetRequiredService<FobConfiguration>();
        _laserDesignator = serviceProvider.GetRequiredService<AssetConfiguration>()
                                          .GetAssetLink<ItemGunAsset>("Items:LaserDesignator");

        _allSpottableObjects = new List<SpottableObjectComponent>(64);
        AllSpottableObjects = new ReadOnlyCollection<SpottableObjectComponent>(_allSpottableObjects);
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

        shouldAllow = false;

        if (hit.transform == null)
            return;

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
                    _logger.LogDebug($"Invalid spot: no player team: {player}.");
                    break;
                }

                SpottableObjectComponent? spotted = SpottableObjectComponent.GetOrAddIfValid(player);
                if (spotted == null)
                    break;

                _logger.LogConditional($"Spotting player {player}");
                _ = Spot(spotter, player.Team, spotted, _translations.SpottedTargetPlayer.Translate());
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
                    _logger.LogDebug($"Invalid spot: no vehicle team: {buildable}.");
                    break;
                }

                spotted = SpottableObjectComponent.GetOrAddIfValid(vehicle);
                if (spotted == null)
                    break;

                _logger.LogConditional($"Spotting vehicle {vehicle.asset.vehicleName}.");
                _ = Spot(spotter, team, spotted, vehicle.transform.TryGetComponent(out WarfareVehicleComponent vc)
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
                    _logger.LogDebug($"Invalid spot: no team on buildable: {buildable}.");
                    break;
                }

                ShovelableInfo? shovelable = _fobConfiguration.Shovelables.FirstOrDefault(x => x.CompletedStructure.MatchAsset(buildable.Asset));
                if (shovelable is not { ConstuctionType: ShovelableType.Fob })
                {
                    _logger.LogDebug($"Invalid spot: not FOB: {buildable}.");
                    break;
                }

                spotted = SpottableObjectComponent.GetOrAddIfValid(buildable);
                if (spotted == null)
                    break;

                _logger.LogConditional($"Spotting buildable {buildable.Asset.itemName}.");
                _ = Spot(spotter, team, spotted, _translations.SpottedTargetFOB.Translate());
                break;
        }
    }

    public async UniTask<bool> Spot(ISpotter spotter, Team targetTeam, SpottableObjectComponent spottedComp, string targetName, CancellationToken token = default)
    {
        WarfarePlayer? player = spotter as WarfarePlayer;

        SpotTargetRequested requestArgs = new SpotTargetRequested
        {
            Spotter = spotter,
            Player = player,
            Target = spottedComp,
            Team = spotter.Team,
            TargetTeam = targetTeam,
            TargetName = targetName
        };

        if (!await _eventDispatcher.DispatchEventAsync(requestArgs, token))
        {
            return false;
        }

        await UniTask.SwitchToMainThread(token);

        token.ThrowIfCancellationRequested();

        if (!spotter.Alive)
            return false;
        
        if (!spottedComp.TryAddSpotter(spotter))
        {
            _logger.LogConditional(" - already spotted.");
            return false;
        }

        player?.SendToast(new ToastMessage(ToastMessageStyle.Mini, _translations.SpottedToast.Translate(player)));

        Team t = spotter.Team;
        Color t1 = t.Faction.Color;

        targetName = TranslationFormattingUtility.Colorize(targetName, targetTeam.Faction.Color);

        _chatService.Broadcast(_formatter.TranslationService.SetOf.PlayersOnTeam(t), _translations.SpottedMessage, t1, targetName, player);

        _ = _eventDispatcher.DispatchEventAsync(new TargetSpotted
        {
            Spotter = spotter,
            Player = player,
            Target = spottedComp,
            Team = spotter.Team,
            TargetTeam = targetTeam,
            TargetName = targetName
        }, CancellationToken.None);
        return true;
    }

    internal void AddSpottableObject(SpottableObjectComponent comp)
    {
        _allSpottableObjects.Add(comp);
    }

    internal void RemoveSpottableObject(SpottableObjectComponent comp)
    {
        _allSpottableObjects.Remove(comp);
    }

    public void HandleEvent(VehicleExploded e, IServiceProvider serviceProvider)
    {
        SpottableObjectComponent? spotted = AllSpottableObjects.FirstOrDefault(f => f.Vehicle == e.Vehicle.Vehicle);
        if (spotted == null)
            return;
        
        Object.Destroy(spotted);
    }
}