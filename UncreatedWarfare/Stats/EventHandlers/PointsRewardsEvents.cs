using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Events.Models.Fobs.Ammo;
using Uncreated.Warfare.Events.Models.Fobs.Shovelables;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Stats.EventHandlers;

internal class PointsRewardsEvents :
    IAsyncEventListener<PlayerDied>,
    IAsyncEventListener<VehicleExploded>,
    IAsyncEventListener<FlagCaptured>,
    IAsyncEventListener<FlagNeutralized>,
    IAsyncEventListener<ObjectiveSlowTick>,
    IAsyncEventListener<FobDeregistered>,
    IAsyncEventListener<FobSuppliesChanged>,
    IAsyncEventListener<ShovelableBuilt>,
    IAsyncEventListener<PlayerRevived>,
    IAsyncEventListener<PlayerAided>,
    IAsyncEventListener<PlayerDeployed>,
    IAsyncEventListener<ExitVehicle>,
    IAsyncEventListener<PlayerRearmedKit>
{
    private const double DriverAssistScaleFactor = 0.5; // 0.5 means both the gunner and driver share the total reward equally
    private readonly PointsService _points;
    private readonly PointsTranslations _translations;

    public PointsRewardsEvents(PointsService points, TranslationInjection<PointsTranslations> translations)
    {
        _points = points;
        _translations = translations.Value;
    }

    public async UniTask HandleEventAsync(PlayerDied e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Killer == null)
            return;

        ResolvedEventInfo resolveEvent;
        Translation translation;
        if (e.Killer.Equals(e.Player))
        {
            resolveEvent = _points.GetEvent("Suicide").Resolve();
            translation = _translations.XPToastSuicide;
        }
        else if (e.Killer.Team == e.Player.Team)
        {
            resolveEvent = _points.GetEvent("FriendlyKilled").Resolve();
            translation = _translations.XPToastFriendlyKilled;
        }
        else
        {
            EventInfo killedEventInfo = _points.GetEvent("EnemyKilled");
            translation = _translations.XPToastEnemyKilled;

            if (ShouldAwardDriverAssist(e.Killer, serviceProvider, out WarfarePlayer? driver))
            {
                resolveEvent = new ResolvedEventInfo(killedEventInfo, DriverAssistScaleFactor);

                await _points.ApplyEvent(
                    driver.Steam64,
                    driver.Team.Faction.PrimaryKey,
                    resolveEvent.WithTranslation(_translations.XPToastKillDriverAssist, driver), token)
                    .ConfigureAwait(false);
            }
            else
            {
                resolveEvent = killedEventInfo.Resolve();
            }
        }

        await _points.ApplyEvent(
            e.Killer.Steam64,
            e.Killer.Team.Faction.PrimaryKey,
            resolveEvent.WithTranslation(translation, e.Killer), token)
            .ConfigureAwait(false);
    }
    private bool ShouldAwardDriverAssist(WarfarePlayer killer, IServiceProvider serviceProvider, [NotNullWhen(true)] out WarfarePlayer? driver)
    {
        InteractableVehicle currentVehicle = killer.UnturnedPlayer.movement.getVehicle();
        if (currentVehicle != null)
        {
            driver = serviceProvider.GetService<IPlayerService>()?.GetOnlinePlayerOrNull(currentVehicle.passengers[0].player);
            if (driver != null && !driver.Equals(killer))
            {
                return true;
            }
        }
        driver = null;
        return false;
    }

    [EventListener(Priority = int.MinValue)]
    public async UniTask HandleEventAsync(VehicleExploded e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Instigator == null)
            return;

        uint faction = e.InstigatorTeam?.Faction.PrimaryKey ?? 0;

        if (faction == 0)
            return;

        IPlayerService? playerService = serviceProvider.GetService<IPlayerService>();
        if (playerService == null)
            return;

        CSteamID instigator = e.InstigatorId;

        if (e.InstigatorTeam!.GroupId.m_SteamID == e.Team)
        {
            EventInfo @event = _points.GetEvent("DestroyFriendlyVehicle:" + e.Vehicle.Info.Type);

            Translation<VehicleType> translation = e.Vehicle.Info.Type.IsAircraft()
                ? _translations.XPToastAircraftDestroyed
                : _translations.XPToastVehicleDestroyed;

            await _points.ApplyEvent(instigator, faction, @event.Resolve().WithTranslation(translation, e.Vehicle.Info.Type, e.Instigator), token).ConfigureAwait(false);
        }
        else
        {
            EventInfo @event = _points.GetEvent("DestroyEnemyVehicle:" + e.Vehicle.Info.Type);

            Translation translation = e.Vehicle.Info.Type.IsAircraft()
                ? _translations.XPToastAircraftDestroyed
                : _translations.XPToastVehicleDestroyed;

            List<Task> tasks = new List<Task>();
            foreach (CSteamID playerId in e.Vehicle.DamageTracker.Contributors)
            {
                float contributionPercentage = e.Vehicle.DamageTracker.GetDamageContributionPercentage(playerId, DateTime.Now.Subtract(TimeSpan.FromMinutes(3)));

                WarfarePlayer? contributor = playerService.GetOnlinePlayerOrNull(playerId);
                if (contributor == null)
                    continue;

                ResolvedEventInfo resolvedEvent;

                if (contributor.Equals(e.Instigator))
                {
                    if (ShouldAwardDriverAssist(e.Instigator, serviceProvider, out WarfarePlayer? driver))
                    {
                        resolvedEvent = new ResolvedEventInfo(@event, contributionPercentage * DriverAssistScaleFactor);

                        Task driverAssistTask = _points.ApplyEvent(
                            driver.Steam64,
                            driver.Team.Faction.PrimaryKey,
                            resolvedEvent.WithTranslation(_translations.XPToastKillDriverAssist, driver), token);
                        tasks.Add(driverAssistTask);
                    }
                    else
                        resolvedEvent = new ResolvedEventInfo(@event, contributionPercentage);
                }
                else if (contributionPercentage < 0.15)
                    continue;
                else
                    resolvedEvent = new ResolvedEventInfo(@event, contributionPercentage);

                Task task = _points.ApplyEvent(contributor.Steam64, faction, resolvedEvent.WithTranslation(translation, contributor), token);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }
    }

    public async UniTask HandleEventAsync(FlagCaptured e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        EventInfo @event = _points.GetEvent("FlagCaptured");
        Translation translation = _translations.XPToastFlagCaptured;

        List<Task> tasks = new List<Task>();
        foreach (WarfarePlayer player in e.Flag.Players)
        {
            if (player.Team != e.Capturer)
                continue;

            Task task = _points.ApplyEvent(
                player.Steam64,
                player.Team.Faction.PrimaryKey,
                @event.Resolve().WithTranslation(translation, player), token);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    public async UniTask HandleEventAsync(FlagNeutralized e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        EventInfo @event = _points.GetEvent("FlagNeutralized");
        Translation translation = _translations.XPToastFlagNeutralized;

        List<Task> tasks = new List<Task>();
        foreach (WarfarePlayer player in e.Flag.Players)
        {
            if (player.Team != e.Neutralizer)
                continue;

            Task task = _points.ApplyEvent(
                player.Steam64,
                player.Team.Faction.PrimaryKey,
                @event.Resolve().WithTranslation(translation, player), token);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    public async UniTask HandleEventAsync(ObjectiveSlowTick e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        EventInfo attackFlagEvent = _points.GetEvent("FlagTickAttack");
        Translation attackFlagTranslation = _translations.XPToastFlagTickAttack;
        EventInfo defendFlagEvent = _points.GetEvent("FlagTickDefend");
        Translation defendFlagTranslation = _translations.XPToastFlagTickDefend;

        List<Task> tasks = new List<Task>();
        foreach (WarfarePlayer player in e.Flag.Players)
        {
            Task task;
            
            if (player.Team == e.Flag.Owner)
            {
                task = _points.ApplyEvent(
                    player.Steam64,
                    player.Team.Faction.PrimaryKey,
                    defendFlagEvent.Resolve().WithTranslation(defendFlagTranslation, player), token
                );
            }
            else
            {
                task = _points.ApplyEvent(
                    player.Steam64,
                    player.Team.Faction.PrimaryKey,
                    attackFlagEvent.Resolve().WithTranslation(attackFlagTranslation, player), token
                );
            }
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);
    }

    public async UniTask HandleEventAsync(FobDeregistered e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Fob is not BunkerFob buildableFob)
            return;

        IPlayerService? playerService = serviceProvider.GetService<IPlayerService>();
        if (playerService == null)
            return;

        WarfarePlayer? instigator = playerService.GetOnlinePlayerOrNull(buildableFob.DamageTracker.LatestDamageInstigator ?? default);
        if (instigator == null)
            return;

        EventInfo @event;
        Translation translation;
        if (instigator.Team == buildableFob.Team)
        {
            @event = _points.GetEvent("FriendlyFobDestroyed");
            translation = _translations.XPToastFriendlyFOBDestroyed;
        }
        else
        {
            @event = _points.GetEvent("EnemyFobDestroyed");
            translation = _translations.XPToastFOBDestroyed;
        }

        await _points.ApplyEvent(
            instigator.Steam64,
            instigator.Team.Faction.PrimaryKey,
            @event.Resolve().WithTranslation(translation, instigator), token)
            .ConfigureAwait(false);
    }


    public async UniTask HandleEventAsync(FobSuppliesChanged e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Resupplier == null || e.AmountDelta <= 0)
            return;

        if (e.SupplyType == SupplyType.Build && e.Fob.BuildCount >= 120)
            return;

        if (e.SupplyType == SupplyType.Ammo && e.Fob.AmmoCount >= 120)
            return;

        EventInfo @event = _points.GetEvent("ResuppliedFob");
        Translation translation = _translations.XPToastResuppliedFob;

        double nominalResupplyAmount = @event.Configuration.GetValue<double>("NominalResupplyAmount", 50);
        ResolvedEventInfo scaledEvent = new ResolvedEventInfo(@event, e.AmountDelta / nominalResupplyAmount);

        await _points.ApplyEvent(
            e.Resupplier.Steam64,
            e.Resupplier.Team.Faction.PrimaryKey,
            scaledEvent.WithTranslation(translation, e.Resupplier), token)
            .ConfigureAwait(false);
    }

    public async UniTask HandleEventAsync(ShovelableBuilt e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        AssetConfiguration? assetConfiguration = serviceProvider.GetService<AssetConfiguration>();
        if (assetConfiguration == null)
            return;

        IPlayerService? playerService = serviceProvider.GetService<IPlayerService>();
        if (playerService == null)
            return;

        EventInfo @event;
        Translation translation;
        if (assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Gameplay:Fob").MatchAsset(e.Shovelable.Info.CompletedStructure))
        {
            @event = _points.GetEvent("FobBuilt");
            translation = _translations.XPToastFOBBuilt;
        }
        else if (assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Gameplay:RepairStation").MatchAsset(e.Shovelable.Info.CompletedStructure))
        {
            @event = _points.GetEvent("RepairStationBuilt");
            translation = _translations.XPToastRepairStationBuilt;
        }
        else if (e.Shovelable.Info.Emplacement != null)
        {
            @event = _points.GetEvent("EmplacementBuilt");
            translation = _translations.XPToastEmplacementBuilt;
        }
        else
        {
            @event = _points.GetEvent("FortificationBuilt");
            translation = _translations.XPToastFortificationBuilt;
        }

        List<Task> tasks = new List<Task>();
        foreach (ulong steam64 in e.Shovelable.Builders.Contributors)
        {
            WarfarePlayer? player = playerService.GetOnlinePlayerOrNull(steam64);
            if (player == null)
                continue;

            float scaleFactor = e.Shovelable.Builders.GetContributionPercentage(player.Steam64);

            ResolvedEventInfo reward = new ResolvedEventInfo(@event, scaleFactor);

            Task task = _points.ApplyEvent(
                player.Steam64,
                player.Team.Faction.PrimaryKey,
                reward.WithTranslation(translation, player), token);
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);
    }
    public async UniTask HandleEventAsync(PlayerRevived e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Medic == null)
            return;

        EventInfo @event = _points.GetEvent("RevivedTeammate");
        Translation translation = _translations.XPToastRevivedTeammate;

        await _points.ApplyEvent(
            e.Medic.Steam64,
            e.Medic.Team.Faction.PrimaryKey,
            @event.Resolve().WithTranslation(translation, e.Medic), token)
            .ConfigureAwait(false);
    }

    public async UniTask HandleEventAsync(PlayerAided e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Medic == null)
            return;

        if (e.Player.UnturnedPlayer.life.health >= 100) // do not award XP for healing players who are already full health
            return;

        EventInfo @event = _points.GetEvent("HealedTeammate");
        Translation translation = _translations.XPToastHealedTeammate;

        await _points.ApplyEvent(
            e.Medic.Steam64,
            e.Medic.Team.Faction.PrimaryKey,
            @event.Resolve().WithTranslation(translation, e.Medic), token)
            .ConfigureAwait(false);
    }

    public async UniTask HandleEventAsync(PlayerDeployed e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Destination is not BunkerFob buildableFob)
            return;

        IPlayerService? playerService = serviceProvider.GetService<IPlayerService>();
        if (playerService == null)
            return;

        WarfarePlayer? fobCreator = playerService.GetOnlinePlayerOrNull(buildableFob.Creator);
        if (fobCreator == null)
            return;

        EventInfo @event = _points.GetEvent("PlayerDeployToFob");
        Translation translation = _translations.XPToastPlayerDeployToFob;

        await _points.ApplyEvent(
            fobCreator.Steam64,
            fobCreator.Team.Faction.PrimaryKey,
            @event.Resolve().WithTranslation(translation, fobCreator), token)
            .ConfigureAwait(false);
    }

    public async UniTask HandleEventAsync(ExitVehicle e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        float distanceTransported = e.Vehicle.TranportTracker.RecordPlayerExit(e.Player.Steam64.m_SteamID, e.Vehicle.Vehicle.transform.position);

        if (e.PassengerIndex == 0 || e.Vehicle.Info.IsCrewSeat(e.PassengerIndex))
            return;

        WarfarePlayer? driver = serviceProvider.GetService<IPlayerService>()?.GetOnlinePlayerOrNull(e.Vehicle.Vehicle.passengers[0].player);
        if (driver == null)
            return;

        EventInfo @event = _points.GetEvent("TransportedPlayer");
        Translation translation = _translations.XPToastTransportedPlayer;

        double nominalTransportDistance = @event.Configuration.GetValue<double>("NominalTransportDistance", 200);
        if (distanceTransported < nominalTransportDistance)
            return;

        ResolvedEventInfo scaledEvent = new ResolvedEventInfo(@event, distanceTransported / nominalTransportDistance);

        await _points.ApplyEvent(
            driver.Steam64,
            driver.Team.Faction.PrimaryKey,
            scaledEvent.WithTranslation(translation, driver), token)
            .ConfigureAwait(false);
    }

    public async UniTask HandleEventAsync(PlayerRearmedKit e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        IPlayerService? playerService = serviceProvider.GetService<IPlayerService>();
        if (playerService == null)
            return;
        
        WarfarePlayer? ammoBagOwner = playerService.GetOnlinePlayerOrNull(e.AmmoStorage.Owner);
        if (ammoBagOwner == null)
            return;
        
        if (ammoBagOwner.Equals(e.Player))
            return;
        
        EventInfo @event = _points.GetEvent("ResuppliedTeammate");
        ResolvedEventInfo scaledEvent = new ResolvedEventInfo(@event, e.AmmoConsumed / @event.Configuration.GetValue<double>("NominalAmmoPerReward", 1));

        await _points.ApplyEvent(
            ammoBagOwner,
            scaledEvent.WithTranslation(_translations.XPToastResuppliedTeammate, ammoBagOwner), token
        ).ConfigureAwait(false);
    }
}