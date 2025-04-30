using DanielWillett.ReflectionTools;
using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Commands;

[Command("vehicle", "v", "veh"), Priority(1), MetadataFile]
internal sealed class VehicleCommand : IExecutableCommand
{
    private static readonly PermissionLeaf SpawnPermission = new PermissionLeaf("warfare::commands.vehicle.spawn");

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        // players have access to /v by default
        await Context.AssertPermissions(SpawnPermission, token);

        bool enter = Context.MatchFlag('e', "enter");

        if (!Context.TryGet(0, out VehicleAsset? asset, out _, true, allowMultipleResults: true))
            throw Context.ReplyString("<color=#8f9494>Unable to find a vehicle by the name or id: <color=#dddddd>" + Context.GetRange(0) + "</color>.</color>");

        Vector3 ppos = Context.Player.Position;
        Vector3 v = Context.Player.UnturnedPlayer.look.aim.forward.normalized with { y = 0 };
        Vector3 targetPos = ppos + v * 6.5f;
        RaycastHit hit;
        targetPos.y += 500f;
        while (!Physics.Raycast(targetPos, Vector3.down, out hit, 500f, RayMasks.GROUND
                                                                       | RayMasks.BARRICADE
                                                                       | RayMasks.STRUCTURE
                                                                       | RayMasks.LARGE
                                                                       | RayMasks.MEDIUM
                                                                       | RayMasks.SMALL
                                                                       | RayMasks.ENVIRONMENT
                                                                       | RayMasks.RESOURCE) && Mathf.Abs(targetPos.y - ppos.y) < 500f)
        {
            targetPos.y -= 50f;
        }

        targetPos.y = (hit.transform == null ? ppos.y : hit.point.y) + 3f;

        // fill all turrets
        byte[][] turrets = new byte[asset.turrets.Length][];

        for (int i = 0; i < asset.turrets.Length; ++i)
        {
            if (Assets.find(EAssetType.ITEM, asset.turrets[i].itemID) is ItemAsset iasset)
                turrets[i] = iasset.getState(true);
            else
                turrets[i] = Array.Empty<byte>();
        }

        InteractableVehicle vehicle = VehicleManager.SpawnVehicleV3(
            asset,
            0,
            0,
            0f,
            targetPos,
            Quaternion.Euler(Context.Player.UnturnedPlayer.look.aim.rotation.eulerAngles with { x = 0, z = 0 }),
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
            255);
        
        if (enter)
            VehicleManager.ServerForcePassengerIntoVehicle(Context.Player.UnturnedPlayer, vehicle);

        Context.ReplyString($"Spawned a <color=#dddddd>{vehicle.asset.vehicleName}</color> (<color=#aaaaaa>{vehicle.asset.id}</color>).", "bfb9ac");
    }
}