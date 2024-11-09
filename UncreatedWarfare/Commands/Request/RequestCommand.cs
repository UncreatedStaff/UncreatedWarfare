using System.Linq;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Translations;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("request", "req", "r"), SynchronizedCommand, MetadataFile]
public sealed class RequestCommand : ICompoundingCooldownCommand
{
    private readonly SignInstancer _signInstancer;
    private readonly VehicleSpawnerStore _vehicleSpawners;
    private readonly KitManager _kitManager;
    private readonly RequestTranslations _translations;
    public float CompoundMultiplier => 2f;
    public float MaxCooldown => 900f; // 15 mins

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public RequestCommand(
        TranslationInjection<RequestTranslations> translations,
        SignInstancer signInstancer,
        VehicleSpawnerStore vehicleSpawners,
        KitManager kitManager)
    {
        _signInstancer = signInstancer;
        _vehicleSpawners = vehicleSpawners;
        _kitManager = kitManager;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        BarricadeDrop? sign = null;
        InteractableVehicle? vehicle = null;

        if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
        {
            if (barricade.interactable is InteractableSign)
                sign = barricade;
        }

        if (sign == null && !Context.TryGetVehicleTarget(out vehicle, tryCallersVehicleFirst: false))
        {
            throw Context.Reply(_translations.RequestNoTarget);
        }

        int? loadoutId = null;
        string? kitId = null;
        VehicleSpawnInfo? spawn = null;

        if (sign != null)
        {
            ISignInstanceProvider? provider = _signInstancer.GetSignProvider(sign);
            switch (provider)
            {
                case KitSignInstanceProvider kit:
                    loadoutId = kit.LoadoutNumber > 0 ? kit.LoadoutNumber : null;
                    kitId = kit.KitId;
                    break;

                // todo vehicle bay sign
            }
        }
        else if (vehicle != null)
        {
            spawn = _vehicleSpawners.Spawns.FirstOrDefault(x => x.LinkedVehicle == vehicle);
        }

        if (loadoutId.HasValue)
        {
            await _kitManager.Requests.RequestLoadout(loadoutId.Value, Context, token);
        }
        else if (kitId != null)
        {
            Kit? kit = await _kitManager.FindKit(kitId, token, exactMatchOnly: true, static x => KitManager.RequestableSet(x, true));

            if (kit == null)
                throw Context.Reply(_translations.RequestKitNotRegistered);

            await _kitManager.Requests.RequestKit(kit, Context, token);
        }
        else if (spawn != null)
        {
            // todo
            throw Context.SendNotImplemented();
        }
        else
        {
            throw Context.Reply(_translations.RequestNoTarget);
        }
    }
}