using SDG.Unturned;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands;
public class AbandonCommand : Command
{
    private const string SYNTAX = "/abandon | /av";
    private const string HELP = "If you no longer want to use your vehicle, you can return it to the vehicle pool.";

    public AbandonCommand() : base("abandon", EAdminType.MEMBER)
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
            throw ctx.Reply(T.AbandonNotInMain);

        if (ctx.TryGetTarget(out InteractableVehicle vehicle) && VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData vehicleData))
        {
            if (vehicleData.DisallowAbandons)
                throw ctx.Reply(T.AbandonNotAllowed);

            if (vehicle.lockedOwner.m_SteamID != ctx.Caller.Steam64)
                throw ctx.Reply(T.AbandonNotOwned, vehicle);

            if ((float)vehicle.health / vehicle.asset.health < 0.9f)
                throw ctx.Reply(T.AbandonDamaged, vehicle);

            if ((float)vehicle.fuel / vehicle.asset.fuel < 0.9f)
                throw ctx.Reply(T.AbandonNeedsFuel, vehicle);

            if (!VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out VehicleSpawn spawn))
                throw ctx.Reply(T.AbandonNoSpace, vehicle);

            VehicleBay.AbandonVehicle(vehicle, vehicleData, spawn, true);

            ctx.Reply(T.AbandonSuccess, vehicle);
        }
        else throw ctx.Reply(T.AbandonNoTarget);
    }
}