using System.Threading;
using System.Threading.Tasks;
using SDG.Unturned;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands;
public class AbandonCommand : AsyncCommand
{
    private const string SYNTAX = "/abandon | /av";
    private const string HELP = "If you no longer want to use your vehicle, you can return it to the vehicle pool.";

    public AbandonCommand() : base("abandon", EAdminType.MEMBER)
    {
        AddAlias("av");
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByPlayer();

        ctx.AssertGamemode<IVehicles>();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);
        VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
        if (bay == null || !bay.IsLoaded || !VehicleSpawnerOld.Loaded)
            throw ctx.SendGamemodeError();

        if (!TeamManager.IsInMain(ctx.Caller))
            throw ctx.Reply(T.AbandonNotInMain);
        if (ctx.TryGetTarget(out InteractableVehicle vehicle))
        {
            SqlItem<VehicleData>? vehicleData = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
            if (vehicleData?.Item == null)
                throw ctx.Reply(T.AbandonNoTarget);
            await vehicleData.Enter(token).ConfigureAwait(false);
            try
            {
                await UCWarfare.ToUpdate();
                
                if (vehicleData.Item.DisallowAbandons)
                    throw ctx.Reply(T.AbandonNotAllowed);

                if (vehicle.lockedOwner.m_SteamID != ctx.Caller.Steam64)
                    throw ctx.Reply(T.AbandonNotOwned, vehicle);

                if ((float)vehicle.health / vehicle.asset.health < 0.9f)
                    throw ctx.Reply(T.AbandonDamaged, vehicle);

                if ((float)vehicle.fuel / vehicle.asset.fuel < 0.9f)
                    throw ctx.Reply(T.AbandonNeedsFuel, vehicle);

                if (!VehicleSpawnerOld.HasLinkedSpawn(vehicle.instanceID, out VehicleSpawn spawn))
                    throw ctx.Reply(T.AbandonNoSpace, vehicle);

                bay.AbandonVehicle(vehicle, vehicleData.Item, spawn, true);

                ctx.Reply(T.AbandonSuccess, vehicle);
            }
            finally
            {
                vehicleData.Release();
            }
        }
        else throw ctx.Reply(T.AbandonNoTarget);
    }
}