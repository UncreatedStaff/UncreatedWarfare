using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Costs;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;

namespace Uncreated.Warfare.Vehicles;

/// <summary>
/// Handles abandoning vehicles.
/// </summary>
public class AbandonService
{
    private readonly PlayerService _playerService;
    private readonly VehicleService _vehicleService;

    public AbandonService(PlayerService playerService, VehicleService vehicleService)
    {
        _playerService = playerService;
        _vehicleService = vehicleService;
    }
    public async UniTask AbandonAllVehiclesAsync(bool respawn, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        List<InteractableVehicle> candidates = new List<InteractableVehicle>(16);
        for (int i = 0; i < VehicleManager.vehicles.Count; ++i)
        {
            InteractableVehicle vehicle = VehicleManager.vehicles[i];

            if (vehicle.isDead || vehicle.isExploded || vehicle.isDrowned || !vehicle.TryGetComponent(out VehicleComponent vehicleComponent))
            {
                continue;
            }

            if (vehicleComponent.Spawn == null)
                continue;

            ulong t = vehicle.lockedGroup.m_SteamID.GetTeam();
            if (t == 1ul && TeamManager.Team1Main.IsInside(vehicle.transform.position) ||
                t == 2ul && TeamManager.Team2Main.IsInside(vehicle.transform.position))
            {
                candidates.Add(vehicle);
            }
        }

        foreach (InteractableVehicle vehicle in candidates)
        {
            await AbandonVehicle(vehicle, respawn, token);
        }
    }

    /// <summary>
    /// Try to abandon the given vehicle.
    /// </summary>
    /// <returns><see langword="true"/> if all the info is found about the vehicle and its deleted, <see langword="false"/> if it's just deleted (or is already dead).</returns>
    public async UniTask<bool> AbandonVehicle(InteractableVehicle vehicle, bool respawn, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (vehicle == null || vehicle.isDead || vehicle.isExploded || vehicle.isDrowned)
            return false;

        if (!vehicle.TryGetComponent(out VehicleComponent vehicleComponent))
            return false;

        WarfarePlayer? owner = _playerService.FromID(vehicle.lockedOwner.m_SteamID);
        bool found = false;
        VehicleSpawnInfo? originalSpawn = vehicleComponent.Spawn;
        if (originalSpawn != null)
        {
            VehicleBayComponent? component =
                originalSpawn.Spawner?.Model == null
                ? null
                : originalSpawn.Spawner.Model.GetComponent<VehicleBayComponent>();

            CreditUnlockCost? creditCost = vehicleComponent.VehicleData?.UnlockCosts.OfType<CreditUnlockCost>().FirstOrDefault();

            found = owner != null && component != null && creditCost != null;

            if (found && creditCost!.Credits > 0
                      && component!.RequestTime != 0
                      && vehicleComponent.VehicleData!.Abandon.AllowAbandon
                      && vehicleComponent.OwnerHistory.Count < 2)
            {
                int creditReward = creditCost.Credits - Mathf.Min(creditCost.Credits,
                    (int)Math.Floor(vehicleComponent.VehicleData.Abandon.ValueLossSpeed * (Time.realtimeSinceStartup - component.RequestTime)));

                Points.AwardCredits(owner!, creditReward, T.AbandonCompensationToast.Translate(owner), false, false);
            }
            else
            {
                found = false;
            }
        }
        else if (owner != null)
        {
            ToastMessage.QueueMessage(owner, new ToastMessage(ToastMessageStyle.Mini, T.AbandonCompensationToastTransferred.Translate(owner).Colorize("adadad")));
        }

        await _vehicleService.DeleteVehicle(vehicle, token);

        if (respawn && originalSpawn != null)
        {
            _vehicleService.SpawnVehicle(originalSpawn);
        }

        return found;
    }
}
