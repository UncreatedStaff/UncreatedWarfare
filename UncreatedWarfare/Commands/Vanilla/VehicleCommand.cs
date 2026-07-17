using DanielWillett.ReflectionTools;
using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("vehicle", "v", "veh"), Priority(1), MetadataFile]
internal sealed class VehicleCommand : IExecutableCommand
{
    private static readonly PermissionLeaf SpawnPermission = new PermissionLeaf("warfare::commands.vehicle.spawn");
    
    private readonly VehicleService _vehicleService;

    private static readonly List<Collider> AllColliders = new List<Collider>(12);

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public VehicleCommand(VehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        // players have access to /v by default
        await Context.AssertPermissions(SpawnPermission, token);

        bool enter = Context.MatchFlag('e', "enter");

        if (!Context.TryGet(0, out VehicleAsset? asset, out _, true, allowMultipleResults: true))
            throw Context.ReplyString("<color=#8f9494>Unable to find a vehicle by the name or id: <color=#dddddd>" + Context.GetRange(0) + "</color>.</color>");

        GameObject model = asset.GetOrLoadModel();

        float sizeToCenter = 0f;

        if (model != null)
        {
            try
            {
                model.GetComponentsInChildren(AllColliders);
                if (AllColliders.Count > 0)
                {
                    Bounds bounds = AllColliders[0].bounds;
                    for (int i = 1; i < AllColliders.Count; ++i)
                    {
                        bounds.Encapsulate(AllColliders[i].bounds);
                    }

                    sizeToCenter = Mathf.Sqrt(
                        Math.Max(
                            (model.transform.position - bounds.max).sqrMagnitude,
                            (model.transform.position - bounds.min).sqrMagnitude
                        )
                    );
                }
            }
            finally
            {
                AllColliders.Clear();
            }
        }

        sizeToCenter = Math.Max(6.5f, sizeToCenter);

        Quaternion rotation = Quaternion.Euler(0f, Context.Player.UnturnedPlayer.look.yaw, 0f);

        Vector3 origin = Context.Player.Position;
        Vector3 direction = rotation * Vector3.forward;
        Vector3 targetPos = origin + direction * sizeToCenter;

        RaycastHit hit;
        targetPos.y += 500f;
        while (!Physics.Raycast(targetPos, Vector3.down, out hit, 500f, RayMasks.GROUND
                                                                       | RayMasks.BARRICADE
                                                                       | RayMasks.STRUCTURE
                                                                       | RayMasks.LARGE
                                                                       | RayMasks.MEDIUM
                                                                       | RayMasks.SMALL
                                                                       | RayMasks.ENVIRONMENT
                                                                       | RayMasks.RESOURCE) && Mathf.Abs(targetPos.y - origin.y) < 500f)
        {
            targetPos.y -= 50f;
        }

        targetPos.y = (hit.transform == null ? origin.y : hit.point.y) + 3f;

        // fill all turrets
        byte[][] turrets = new byte[asset.turrets.Length][];

        for (int i = 0; i < asset.turrets.Length; ++i)
        {
            if (Assets.find(EAssetType.ITEM, asset.turrets[i].itemID) is ItemAsset iasset)
                turrets[i] = iasset.getState(true);
            else
                turrets[i] = Array.Empty<byte>();
        }

        // apply helicopter rotation offset
        _vehicleService.ApplyUpwardsRotationOffset(asset, ref rotation);

        InteractableVehicle vehicle = VehicleManager.SpawnVehicleV3(
            asset,
            0,
            0,
            0f,
            targetPos,
            rotation,
            false,
            false,
            false,
            false,
            asset.fuel,
            asset.health,
            10000,
            Context.CallerId,
            Context.Player.UnturnedPlayer.quests.groupID,
            true,
            turrets,
            255,
            asset.SupportsPaintColor ? (Color32)Context.Player.Team.Faction.Color : default
        );
        
        if (enter)
            VehicleManager.ServerForcePassengerIntoVehicle(Context.Player.UnturnedPlayer, vehicle);

        Context.ReplyString($"Spawned a <color=#dddddd>{vehicle.asset.vehicleName}</color> (<color=#aaaaaa>{vehicle.asset.id}</color>).", "bfb9ac");
    }
}