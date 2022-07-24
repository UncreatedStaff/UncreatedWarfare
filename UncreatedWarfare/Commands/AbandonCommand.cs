using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands;
public class BlankCommand : Command
{
    private const string SYNTAX = "/abandon | /av";
    private const string HELP = "If you no longer want to use your vehicle, you can return it to the vehicle pool.";

    public BlankCommand() : base("abandon", EAdminType.MEMBER)
    {
        AddAlias("av");
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();

        ctx.AssertGamemode<IVehicles>();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (!VehicleBay.Loaded || !VehicleSpawner.Loaded)
            throw ctx.SendGamemodeError();

        if (!TeamManager.IsInMain(ctx.Caller))
            throw ctx.Reply("abandon_not_in_main");

        if (ctx.TryGetTarget(out InteractableVehicle vehicle) && VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData vehicleData))
        {
            if (vehicleData.DisallowAbandons)
                throw ctx.Reply("abandon_not_allowed");

            if (vehicle.lockedOwner.m_SteamID != ctx.Caller.Steam64)
                throw ctx.Reply("abandon_not_owned", vehicle.asset.vehicleName);

            if ((float)vehicle.health / vehicle.asset.health < 0.9f)
                throw ctx.Reply("abandon_damaged", vehicle.asset.vehicleName);

            if ((float)vehicle.fuel / vehicle.asset.fuel < 0.9f)
                throw ctx.Reply("abandon_needs_fuel", vehicle.asset.vehicleName);

            if (!VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out VehicleSpawn spawn))
                throw ctx.Reply("abandon_no_space", vehicle.asset.vehicleName);

            VehicleBay.AbandonVehicle(vehicle, vehicleData, spawn, true);

            ctx.Reply("abandon_success", vehicle.asset.vehicleName);
        }
        else throw ctx.Reply("abandon_no_target");
    }
}