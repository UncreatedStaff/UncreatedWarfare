using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;

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

    public AbandonService(IPlayerService playerService, VehicleService vehicleService, TranslationInjection<AbandonTranslations> translations, ITranslationValueFormatter formatter)
    {
        _playerService = playerService;
        _vehicleService = vehicleService;
        _formatter = formatter;
        _translations = translations.Value;
    }
    public async UniTask AbandonAllVehiclesAsync(bool respawn, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        List<InteractableVehicle> candidates = new List<InteractableVehicle>(16);
#if false
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
#endif

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

        WarfarePlayer? owner = _playerService.GetOnlinePlayer(vehicle.lockedOwner.m_SteamID);
        bool found = false;
        VehicleSpawnInfo? originalSpawn = vehicleComponent.Spawn;
        if (originalSpawn != null)
        {
#if false
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

                Points.AwardCredits(owner!, creditReward, _translations.AbandonCompensationToast.Translate(owner!), redmessage: false, isPurchase: false);
            }
            else
            {
                found = false;
            }
#endif
        }
        else if (owner != null)
        {
            owner.SendToast(new ToastMessage(ToastMessageStyle.Mini, _formatter.Colorize(_translations.AbandonCompensationToastTransferred.Translate(owner), new Color32(173, 173, 173, 255), TranslationOptions.TMProUI)));
        }

        await _vehicleService.DeleteVehicleAsync(vehicle, token);

        if (respawn && originalSpawn != null)
        {
            await _vehicleService.SpawnVehicleAsync(originalSpawn, token);
        }

        return found;
    }
}

public class AbandonTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Abandon";

    [TranslationData(Description = "Sent when a player isn't looking at a vehicle when doing /abandon.")]
    public readonly Translation AbandonNoTarget = new Translation("<#ff8c69>You must be looking at a vehicle.");

    [TranslationData(Description = "Sent when a player is looking at a vehicle they didn't request.")]
    public readonly Translation<InteractableVehicle> AbandonNotOwned = new Translation<InteractableVehicle>("<#ff8c69>You did not request that {0}.");

    [TranslationData(Description = "Sent when a player does /abandon while not in main.")]
    public readonly Translation AbandonNotInMain = new Translation("<#ff8c69>You must be in main to abandon a vehicle.");

    [TranslationData(Description = "Sent when a player tries to abandon a damaged vehicle.")]
    public readonly Translation<InteractableVehicle> AbandonDamaged = new Translation<InteractableVehicle>("<#ff8c69>Your <#cedcde>{0}</color> is damaged, repair it before returning it to the yard.");

    [TranslationData(Description = "Sent when a player tries to abandon a vehicle with low fuel.")]
    public readonly Translation<InteractableVehicle> AbandonNeedsFuel = new Translation<InteractableVehicle>("<#ff8c69>Your <#cedcde>{0}</color> is not fully fueled.");

    [TranslationData(Description = "Sent when a player tries to abandon a vehicle and all the bays for that vehicle are already full, theoretically should never happen.")]
    public readonly Translation<InteractableVehicle> AbandonNoSpace = new Translation<InteractableVehicle>("<#ff8c69>There's no space for <#cedcde>{0}</color> in the yard.", arg0Fmt: PluralAddon.Always());

    [TranslationData(Description = "Sent when a player tries to abandon a vehicle that isn't allowed to be abandoned.")]
    public readonly Translation<InteractableVehicle> AbandonNotAllowed = new Translation<InteractableVehicle>("<#ff8c69><#cedcde>{0}</color> can not be abandoned.", arg0Fmt: PluralAddon.Always());

    [TranslationData(Description = "Sent when a player abandons a vehicle.")]
    public readonly Translation<InteractableVehicle> AbandonSuccess = new Translation<InteractableVehicle>("<#a0ad8e>Your <#cedcde>{0}</color> was returned to the yard.");

    [TranslationData(Description = "Credits toast for returning a vehicle soon after requesting it.")]
    public readonly Translation AbandonCompensationToast = new Translation("RETURNED VEHICLE", TranslationOptions.TMProUI);

    [TranslationData(Description = "Credits toast for returning a vehicle soon after requesting it, but not getting anything because the vehicle was transferred.")]
    public readonly Translation AbandonCompensationToastTransferred = new Translation("+0 <color=#b8ffc1>C</color> [GIVEN]\nRETURNED VEHICLE", TranslationOptions.TMProUI);
}