using SDG.Unturned;
using Steamworks;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;
public class LoadCommand : AsyncCommand
{
    private const string Syntax = "/load <build|ammo> <amount|'half'>";
    private const string Help = "Loads supplies into a logistics truck. If no amount is given, it fills the vehicle. If 'half' is supplied, it fills half the empty slots.";

    public LoadCommand() : base("load", EAdminType.MEMBER)
    {
        Structure = new CommandStructure
        {
            Description = "Loads supplies into a logistics truck.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Type", "Build", "Ammo")
                {
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Amount", typeof(float))
                        {
                            IsOptional = true,
                            Description = "Loads your vehicle's trunk with a set amount of supplies (or until it's full)."
                        },
                        new CommandParameter("Half")
                        {
                            Aliases = new string[] { "1/2" },
                            IsOptional = true,
                            Description = "Loads half of the empty space in your vehicle's trunk."
                        }
                    }
                }
            }
        };
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, T.LoadUsage);

        if (!ctx.MatchParameter(0, "build", "ammo"))
            throw ctx.Reply(T.LoadUsage);

        if (!ctx.TryGetTarget(out InteractableVehicle vehicle) || vehicle.lockedOwner == CSteamID.Nil || vehicle.lockedGroup == CSteamID.Nil)
            throw ctx.Reply(T.LoadNoTarget);

        VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
        if (bay is not { IsLoaded: true })
            throw ctx.SendGamemodeError();

        SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
        await UCWarfare.ToUpdate();
        if (data is null || data.Item == null || !VehicleData.IsLogistics(data.Item.Type))
            throw ctx.Reply(T.LoadNotLogisticsVehicle);

        if (!F.IsInMain(vehicle.transform.position))
            throw ctx.Reply(T.LoadNotInMain);

        if (!(vehicle.ReplicatedSpeed >= -1) || !(vehicle.ReplicatedSpeed <= 1))
            throw ctx.Reply(T.LoadSpeed);

        int amount = 0;
        bool half = true;

        if (!ctx.MatchParameter(1, "half", "1/2"))
        {
            if (!ctx.MatchParameter(1, "max", "full") && !(ctx.TryGet(1, out amount) && amount > 0) && ctx.HasArg(1))
                throw ctx.Reply(T.LoadInvalidAmount, ctx.Get(1)!);

            half = false;
        }

        SupplyType type;
        if (ctx.MatchParameter(0, "build")) type = SupplyType.Build;
        else if (ctx.MatchParameter(0, "ammo")) type = SupplyType.Ammo;
        else throw ctx.Reply(T.LoadUsage);

        if (amount == 0)
        {
            if (vehicle.trunkItems == null)
                throw ctx.Reply(T.LoadInvalidAmount, ctx.Get(1)!);
            ItemAsset? supplyAsset;
            ulong team = vehicle.lockedGroup.m_SteamID.GetTeam();
            if (type is SupplyType.Build)
                TeamManager.GetFaction(team).Build.ValidReference(out supplyAsset);
            else
                TeamManager.GetFaction(team).Ammo.ValidReference(out supplyAsset);
            byte c = vehicle.trunkItems.getItemCount();
            // estimate the amount that can fit
            amount = (vehicle.trunkItems.width - vehicle.trunkItems.width % supplyAsset.size_x) *
                     (vehicle.trunkItems.height - vehicle.trunkItems.height % supplyAsset.size_y);
            for (int i = 0; i < c; i++)
            {
                ItemJar ij = vehicle.trunkItems.items[i];
                amount -= ij.size_x * ij.size_y;
            }

            if (amount < 0)
            {
                amount =
                    (vehicle.trunkItems.width - vehicle.trunkItems.width % supplyAsset.size_x) *
                    (vehicle.trunkItems.height - vehicle.trunkItems.height % supplyAsset.size_y) -
                    c;
            }
            amount /= (supplyAsset.size_x * supplyAsset.size_y);
            if (half)
                amount /= 2;
        }

        int max = vehicle.trunkItems != null
            ? vehicle.trunkItems.width * vehicle.trunkItems.height
            : 255;

        if (amount >= max)
            amount = 0;

        if (amount <= 0)
            throw ctx.Reply(T.LoadInvalidAmount, ctx.Get(1)!);

        if (!vehicle.transform.TryGetComponent(out VehicleComponent vehicleComponent))
        {
            vehicleComponent = vehicle.transform.gameObject.AddComponent<VehicleComponent>();
            vehicleComponent.Initialize(vehicle);
        }

        if (vehicleComponent.ForceSupplyLoop != null)
            throw ctx.Reply(T.LoadAlreadyLoading);

        vehicleComponent.StartForceLoadSupplies(ctx.Caller, type, amount);

        ctx.LogAction(ActionLogType.LoadSupplies, type + " x" + amount);
        ctx.Defer();
    }
}
