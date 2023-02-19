using SDG.Unturned;
using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands.VanillaRework;
public class VCommand : Command
{
    private const string SYNTAX = "/v <vehicle>";
    private const string HELP = "Spawns a vehicle in front of you.";

    public VCommand() : base("vehicle", EAdminType.ADMIN, 1)
    {
        AddAlias("v");
        AddAlias("veh");
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        bool enter = false;
        if (ctx.MatchParameter(0, "-e"))
        {
            enter = true;
            ctx.Offset = 1;
        }

        ctx.AssertArgs(1, SYNTAX);

        ctx.AssertOnDuty();

        ctx.AssertRanByPlayer();

        if (!ctx.TryGet(0, out VehicleAsset asset, out bool mutiple, true, allowMultipleResults: true))
            throw ctx.ReplyString("<color=#8f9494>Unable to find a vehicle by the name or id: <color=#dddddd>" + ctx.GetRange(0) + "</color>.</color>");

        Vector3 ppos = ctx.Caller.Position;
        Vector3 v = ctx.Caller.Player.look.aim.forward.normalized with { y = 0 };
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
            Quaternion.Euler(ctx.Caller.Player.look.aim.rotation.eulerAngles with { x = 0, z = 0 }),
            false,
            false,
            false,
            false,
            asset.fuel,
            asset.health,
            10000,
            ctx.CallerCSteamID,
            ctx.Caller.Player.quests.groupID,
            true,
            turrets,
            255);
        if (enter)
            VehicleManager.ServerForcePassengerIntoVehicle(ctx.Caller.Player, vehicle);
        ctx.ReplyString($"Spawned a <color=#dddddd>{vehicle.asset.vehicleName}</color> (<color=#aaaaaa>{vehicle.asset.id}</color>).", "bfb9ac");
    }
}
