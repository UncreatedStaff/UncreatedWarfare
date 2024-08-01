using Cysharp.Threading.Tasks;
using System.Threading;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Vehicles;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands;

[Command("abandon", "av")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class AbandonCommand : IExecutableCommand
{
    private const string Syntax = "/abandon | /av";
    private const string Help = "If you no longer want to use your vehicle, you can return it to the vehicle pool.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "If you no longer want to use your vehicle, you can return it to the vehicle pool."
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertGamemode(out IVehicles vgm);

        Context.AssertHelpCheck(0, Syntax + " - " + Help);
        VehicleBay bay = vgm.VehicleBay;
        VehicleSpawner spawner = vgm.VehicleSpawner;

        if (!TeamManager.IsInMain(Context.Player))
            throw Context.Reply(T.AbandonNotInMain);

        if (!Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
            throw Context.Reply(T.AbandonNoTarget);
        
        SqlItem<VehicleData>? vehicleData = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
        
        if (vehicleData?.Item == null)
            throw Context.Reply(T.AbandonNoTarget);

        await vehicleData.Enter(token).ConfigureAwait(false);
        try
        {
            await UniTask.SwitchToMainThread(token);

            if (vehicleData.Item.DisallowAbandons)
                throw Context.Reply(T.AbandonNotAllowed);

            if (vehicle.lockedOwner.m_SteamID != Context.CallerId.m_SteamID)
                throw Context.Reply(T.AbandonNotOwned, vehicle);

            if ((float)vehicle.health / vehicle.asset.health < 0.9f)
                throw Context.Reply(T.AbandonDamaged, vehicle);

            if ((float)vehicle.fuel / vehicle.asset.fuel < 0.9f)
                throw Context.Reply(T.AbandonNeedsFuel, vehicle);

            if (!spawner.TryGetSpawn(vehicle, out SqlItem<VehicleSpawn> spawn))
                throw Context.Reply(T.AbandonNoSpace, vehicle);

            if (spawner.AbandonVehicle(vehicle, vehicleData, spawn, true))
                Context.Reply(T.AbandonSuccess, vehicle);
            else
                throw Context.SendUnknownError();
        }
        finally
        {
            vehicleData.Release();
        }
    }
}