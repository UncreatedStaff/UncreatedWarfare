using System;
using System.Collections.Generic;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Vehicles;

/// <summary>
/// Handles abandoning vehicles.
/// </summary>
public class AbandonService
{
    private readonly IPlayerService _playerService;
    private readonly VehicleService _vehicleService;
    private readonly ITranslationValueFormatter _formatter;
    private readonly AbandonTranslations _translations;
    private readonly ZoneStore _zoneStore;
    private readonly ITeamManager<Team> _teamManager;
    private readonly PointsService _pointsService;

    public AbandonService(
        IPlayerService playerService,
        VehicleService vehicleService,
        TranslationInjection<AbandonTranslations> translations,
        ITranslationValueFormatter formatter,
        ZoneStore zoneStore,
        ITeamManager<Team> teamManager,
        PointsService pointsService)
    {
        _playerService = playerService;
        _vehicleService = vehicleService;
        _formatter = formatter;
        _zoneStore = zoneStore;
        _teamManager = teamManager;
        _pointsService = pointsService;
        _translations = translations.Value;
    }

    public async UniTask AbandonAllVehiclesAsync(bool respawn, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        List<InteractableVehicle> candidates = new List<InteractableVehicle>(16);
        for (int i = 0; i < VehicleManager.vehicles.Count; ++i)
        {
            InteractableVehicle vehicle = VehicleManager.vehicles[i];

            if (vehicle.isDead || vehicle.isExploded || vehicle.isDrowned)
            {
                continue;
            }

            WarfareVehicle warfareVehicle = _vehicleService.GetVehicle(vehicle);
            if (warfareVehicle.Spawn == null)
                continue;

            Team t = _teamManager.GetTeam(vehicle.lockedGroup);
            if (t.IsValid && _zoneStore.IsInsideZone(vehicle.transform.position, ZoneType.MainBase, t.Faction))
            {
                candidates.Add(vehicle);
            }
        }

        UniTask<bool>[] tasks = new UniTask<bool>[candidates.Count];

        for (int i = 0; i < candidates.Count; i++)
        {
            InteractableVehicle vehicle = candidates[i];
            tasks[i] = AbandonVehicleAsync(vehicle, respawn, token);
        }

        await UniTask.WhenAll(tasks);
    }

    /// <summary>
    /// Try to abandon the given vehicle.
    /// </summary>
    /// <returns><see langword="true"/> if all the info is found about the vehicle and its deleted, <see langword="false"/> if it's just deleted (or is already dead).</returns>
    public async UniTask<bool> AbandonVehicleAsync(InteractableVehicle vehicle, bool respawn, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (vehicle == null || vehicle.isDead || vehicle.isExploded || vehicle.isDrowned)
            return false;

        WarfareVehicle warfareVehicle = _vehicleService.GetVehicle(vehicle);
        Team team = _teamManager.GetTeam(vehicle.lockedGroup);
        if (!team.IsValid)
        {
            //Console.WriteLine($"invalid team {vehicle.lockedGroup}.");
            return false;
        }

        CSteamID owner = warfareVehicle.OriginalOwner;
        if (owner.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
        {
            //Console.WriteLine($"invalid owner {warfareVehicle.OriginalOwner}.");
            return false;
        }

        WarfarePlayer? ownerPlayer = _playerService.GetOnlinePlayerOrNull(owner);

        WarfarePlayer? currentOwner = _playerService.GetOnlinePlayerOrNull(warfareVehicle.Vehicle.lockedOwner);
        VehicleSpawner? originalSpawn = warfareVehicle.Spawn;
        if (originalSpawn != null)
        {
            WarfareVehicleInfo info = warfareVehicle.Info;
            int creditCost = info.CreditCost;

            if (creditCost > 0
                 && originalSpawn.RequestTime != DateTime.MinValue
                 && info.Abandon.AllowAbandon)
            {
                int creditReward = creditCost - Mathf.Min(
                    creditCost,
                    Mathf.FloorToInt((float)(info.Abandon.ValueLossSpeed * (DateTime.UtcNow - originalSpawn.RequestTime).TotalSeconds))
                );

                if (creditReward > 0)
                {
                    ResolvedEventInfo e = _pointsService
                                          .GetEvent("VehicleAbandon")
                                          .Resolve(overrideCredits: creditReward);

                    if (ownerPlayer != null)
                    {
                        e = e.WithTranslation(_translations.AbandonCompensationToast, ownerPlayer);
                    }

                    await _pointsService.ApplyEvent(owner, team.Faction.PrimaryKey, e, token);

                    await UniTask.SwitchToMainThread(token);
                }
            }
        }
        if (originalSpawn == null || !Equals(ownerPlayer, currentOwner))
        {
            currentOwner?.SendToast(
                new ToastMessage(ToastMessageStyle.Mini,
                                 _formatter.Colorize(_translations.AbandonCompensationToastTransferred.Translate(currentOwner),
                                                     new Color32(173, 173, 173, 255), TranslationOptions.TMProUI))
                );
        }

        await _vehicleService.DeleteVehicleAsync(vehicle, token);

        await UniTask.SwitchToMainThread(token);

        if (respawn && originalSpawn != null && originalSpawn.LinkedVehicle == null)
        {
            await _vehicleService.SpawnVehicleAsync(originalSpawn, token);
        }

        return true;
    }
}

public class AbandonTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Abandon";

    [TranslationData(Description = "Sent when a player isn't looking at a vehicle when doing /abandon.")]
    public readonly Translation AbandonNoTarget = new Translation("<#ff8c69>You must be looking at a vehicle.");

    [TranslationData(Description = "Sent when a player is looking at a vehicle they didn't request.")]
    public readonly Translation<VehicleAsset> AbandonNotOwned = new Translation<VehicleAsset>("<#ff8c69>You did not request that {0}.");

    [TranslationData(Description = "Sent when a player does /abandon while not in main.")]
    public readonly Translation AbandonNotInMain = new Translation("<#ff8c69>You must be in main to abandon a vehicle.");

    [TranslationData(Description = "Sent when a player tries to abandon a damaged vehicle.")]
    public readonly Translation<VehicleAsset> AbandonDamaged = new Translation<VehicleAsset>("<#ff8c69>Your <#cedcde>{0}</color> is damaged, repair it before returning it to the yard.");

    [TranslationData(Description = "Sent when a player tries to abandon a vehicle with low fuel.")]
    public readonly Translation<VehicleAsset> AbandonNeedsFuel = new Translation<VehicleAsset>("<#ff8c69>Your <#cedcde>{0}</color> is not fully fueled.");

    [TranslationData(Description = "Sent when a player tries to abandon a vehicle and all the bays for that vehicle are already full, theoretically should never happen.")]
    public readonly Translation<VehicleAsset> AbandonNoSpace = new Translation<VehicleAsset>("<#ff8c69>There's no space for <#cedcde>{0}</color> in the yard.", arg0Fmt: PluralAddon.Always());

    [TranslationData(Description = "Sent when a player tries to abandon a vehicle that isn't allowed to be abandoned.")]
    public readonly Translation<VehicleAsset> AbandonNotAllowed = new Translation<VehicleAsset>("<#ff8c69><#cedcde>{0}</color> can not be abandoned.", arg0Fmt: PluralAddon.Always());

    [TranslationData(Description = "Sent when a player abandons a vehicle.")]
    public readonly Translation<VehicleAsset> AbandonSuccess = new Translation<VehicleAsset>("<#a0ad8e>Your <#cedcde>{0}</color> was returned to the yard.");

    [TranslationData(Description = "Credits toast for returning a vehicle soon after requesting it.")]
    public readonly Translation AbandonCompensationToast = new Translation("RETURNED VEHICLE", TranslationOptions.TMProUI);

    [TranslationData(Description = "Credits toast for returning a vehicle soon after requesting it, but not getting anything because the vehicle was transferred.")]
    public readonly Translation AbandonCompensationToastTransferred = new Translation("+0 <color=#b8ffc1>C</color> [TRANSFERRED]\nRETURNED VEHICLE", TranslationOptions.TMProUI);
}