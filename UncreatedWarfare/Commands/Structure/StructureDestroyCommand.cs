using System;
using System.Globalization;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("destroy", "pop"), SubCommandOf(typeof(StructureCommand))]
internal sealed class StructureDestroyCommand : IExecutableCommand
{
    private readonly BuildableSaver _saver;
    private readonly VehicleService _vehicleService;
    private readonly EventDispatcher _eventDispatcher;
    private readonly StructureTranslations _translations;
    private readonly BuildableSaver _buildableSaver;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public StructureDestroyCommand(VehicleService vehicleService, BuildableSaver saver, EventDispatcher eventDispatcher, TranslationInjection<StructureTranslations> translations, BuildableSaver buildableSaver)
    {
        _saver = saver;
        _vehicleService = vehicleService;
        _eventDispatcher = eventDispatcher;
        _buildableSaver = buildableSaver;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
        {
            await _vehicleService.DeleteVehicleAsync(vehicle, token);

            Context.LogAction(ActionLogType.PopStructure,
                $"VEHICLE: {vehicle.asset.vehicleName} / {vehicle.asset.id} /" +
                $" {vehicle.asset.GUID:N} at {vehicle.transform.position:N2} ({vehicle.instanceID})");
            Context.Reply(_translations.StructureDestroyed, vehicle.asset);
        }
        else if (Context.TryGetStructureTarget(out StructureDrop? structure))
        {
            bool removedSave = await _saver.DiscardStructureAsync(structure.instanceID, token);
            await UniTask.SwitchToMainThread(token);
            if (removedSave)
            {
                Context.LogAction(ActionLogType.UnsaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
                                                                 $"at {structure.GetServersideData().point} ({structure.instanceID})");
                Context.Reply(_translations.StructureUnsaved, structure.asset);
            }

            await UnsaveBuildable(new BuildableStructure(structure), token);
            await DestroyStructure(structure, Context.Player, CancellationToken.None);
            Context.LogAction(ActionLogType.PopStructure,
                $"STRUCTURE: {structure.asset.itemName} / {structure.asset.id} /" +
                $" {structure.asset.GUID:N} at {structure.model.transform.position.ToString("N2", CultureInfo.InvariantCulture)} ({structure.instanceID})");
        }
        else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
        {
            bool removedSave = await _saver.DiscardStructureAsync(barricade.instanceID, token);
            await UniTask.SwitchToMainThread(token);
            if (removedSave)
            {
                Context.LogAction(ActionLogType.UnsaveStructure, $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} " +
                                                                 $"at {barricade.GetServersideData().point} ({barricade.instanceID})");
                Context.Reply(_translations.StructureUnsaved, barricade.asset);
            }

            await UnsaveBuildable(new BuildableBarricade(barricade), token);
            await DestroyBarricade(barricade, Context.Player, CancellationToken.None);
            Context.LogAction(ActionLogType.PopStructure,
                $"BARRICADE: {barricade.asset.itemName} / {barricade.asset.id} /" +
                $" {barricade.asset.GUID:N} at {barricade.model.transform.position.ToString("N2", CultureInfo.InvariantCulture)} ({barricade.instanceID})");
            Context.Defer();
        }
        else
        {
            Context.Reply(_translations.StructureNoTarget);
        }
    }

    private async UniTask UnsaveBuildable(IBuildable buildable, CancellationToken token = default)
    {
        if (await _buildableSaver.DiscardBuildableAsync(buildable, token))
        {
            Context.Reply(_translations.StructureUnsaved, buildable.Asset);
        }
    }

    private async UniTask DestroyBarricade(BarricadeDrop bDrop, WarfarePlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (bDrop == null || !BarricadeManager.tryGetRegion(bDrop.model, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
        {
            Context.Reply(_translations.StructureNotDestroyable);
            return;
        }

        // simulate salvaging the barricade
        SalvageBarricadeRequested args = new SalvageBarricadeRequested
        {
            Player = player,
            Region = region,
            InstanceId = bDrop.instanceID,
            Barricade = bDrop,
            ServersideData = bDrop.GetServersideData(),
            RegionPosition = new RegionCoord(x, y),
            VehicleRegionIndex = plant,
            InstigatorTeam = player.Team
        };

        BuildableExtensions.SetDestroyInfo(bDrop.model, args, null);
        bool shouldAllow = true;
        try
        {
            bool shouldAllowTemp = shouldAllow;
            BuildableExtensions.SetSalvageInfo(bDrop.model, EDamageOrigin.Unknown, Context.CallerId, true, salvageInfo =>
            {
                if (salvageInfo is not ISalvageListener listener)
                    return true;

                listener.OnSalvageRequested(args);

                if (args.IsActionCancelled)
                    shouldAllowTemp = false;

                return !args.IsCancelled;
            });

            shouldAllow = shouldAllowTemp;

            EventContinuations.Dispatch(args, _eventDispatcher, token, out shouldAllow, continuation: args =>
            {
                if (args.ServersideData.barricade.isDead)
                    return;

                // simulate BarricadeDrop.ReceiveSalvageRequest
                ItemBarricadeAsset asset = args.Barricade.asset;
                if (asset.isUnpickupable)
                    return;

                // re-apply ISalvageInfo components
                BuildableExtensions.SetSalvageInfo(args.Transform, EDamageOrigin.Unknown, args.Steam64, true, null);

                if (!BarricadeManager.tryGetRegion(args.Barricade.model, out byte x, out byte y, out ushort plant, out _))
                {
                    x = args.RegionPosition.x;
                    y = args.RegionPosition.y;
                    plant = args.VehicleRegionIndex;
                }

                BarricadeManager.destroyBarricade(args.Barricade, x, y, plant);
                Context.Reply(_translations.StructureDestroyed, bDrop.asset);
                RemoveBuiladble(null, bDrop);
            });
        }
        finally
        {
            // undo setting this if the task needs continuing, it'll be re-set later
            if (!shouldAllow)
            {
                BuildableExtensions.SetSalvageInfo(bDrop.model, EDamageOrigin.Unknown, null, false, null);
            }
        }

        BarricadeManager.destroyBarricade(bDrop, x, y, ushort.MaxValue);
        Context.Reply(_translations.StructureDestroyed, bDrop.asset);
        RemoveBuiladble(null, bDrop);
    }

    private async UniTask DestroyStructure(StructureDrop sDrop, WarfarePlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (sDrop == null || !StructureManager.tryGetRegion(sDrop.model, out byte x, out byte y, out StructureRegion region))
        {
            Context.Reply(_translations.StructureNotDestroyable);
            return;
        }

        // simulate salvaging the structure
        SalvageStructureRequested args = new SalvageStructureRequested
        {
            Player = player,
            Region = region,
            InstanceId = sDrop.instanceID,
            Structure = sDrop,
            ServersideData = sDrop.GetServersideData(),
            RegionPosition = new RegionCoord(x, y),
            InstigatorTeam = player.Team
        };

        BuildableExtensions.SetDestroyInfo(sDrop.model, args, null);

        bool shouldAllow = true;
        try
        {
            BuildableExtensions.SetSalvageInfo(sDrop.model, EDamageOrigin.Unknown, Context.CallerId, true, salvageInfo =>
            {
                if (salvageInfo is not ISalvageListener listener)
                    return true;

                listener.OnSalvageRequested(args);

                if (args.IsActionCancelled)
                    shouldAllow = false;

                return !args.IsCancelled;
            });

            EventContinuations.Dispatch(args, _eventDispatcher, token, out shouldAllow, continuation: args =>
            {
                if (args.ServersideData.structure.isDead)
                    return;

                // simulate StructureDrop.ReceiveSalvageRequest
                ItemStructureAsset? asset = args.Structure.asset;
                if (asset is { isUnpickupable: true })
                    return;

                // re-apply ISalvageInfo components
                BuildableExtensions.SetSalvageInfo(args.Transform, EDamageOrigin.Unknown, args.Steam64, true, null);

                if (!StructureManager.tryGetRegion(args.Structure.model, out byte x, out byte y, out _))
                {
                    x = args.RegionPosition.x;
                    y = args.RegionPosition.y;
                }

                StructureManager.destroyStructure(sDrop, x, y, Vector3.Reflect(sDrop.GetServersideData().point - player.Position, Vector3.up).normalized * 4);
                Context.Reply(_translations.StructureDestroyed, sDrop.asset);
                RemoveBuiladble(sDrop, null);
            });
        }
        finally
        {
            if (!shouldAllow)
            {
                BuildableExtensions.SetSalvageInfo(sDrop.model, EDamageOrigin.Unknown, null, false, null);
            }
        }

        if (!shouldAllow)
            return;

        StructureManager.destroyStructure(sDrop, x, y, Vector3.Reflect(sDrop.GetServersideData().point - player.Position, Vector3.up).normalized * 4);
        Context.Reply(_translations.StructureDestroyed, sDrop.asset);
        RemoveBuiladble(sDrop, null);
    }

    private void RemoveBuiladble(StructureDrop? structure, BarricadeDrop? barricade)
    {
        _ = Task.Run(async () =>
        {
            bool success;
            try
            {
                success = structure != null
                    ? await _buildableSaver.DiscardStructureAsync(structure.instanceID, CancellationToken.None)
                    : await _buildableSaver.DiscardBarricadeAsync(barricade!.instanceID, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Context.Logger.LogError(ex, "Error unsaving buildable.");
                return;
            }

            if (success)
                Context.Reply(_translations.StructureUnsaved, (ItemPlaceableAsset?)structure?.asset ?? barricade!.asset);
        }, CancellationToken.None);
    }
}