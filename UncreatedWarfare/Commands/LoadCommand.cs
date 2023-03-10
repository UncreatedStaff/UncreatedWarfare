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
            
        if (ctx.MatchParameter(0, "build", "ammo"))
        {
            if (ctx.TryGetTarget(out InteractableVehicle vehicle))
            {
                if (vehicle.lockedOwner == CSteamID.Nil || vehicle.lockedGroup == CSteamID.Nil)
                    throw ctx.Reply(T.LoadNoTarget);
                VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
                if (bay != null && bay.IsLoaded)
                {
                    SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token);
                    await UCWarfare.ToUpdate();
                    if (data is null || data.Item == null || !VehicleData.IsLogistics(data.Item.Type))
                        throw ctx.Reply(T.LoadNotLogisticsVehicle);
                    if (F.IsInMain(vehicle.transform.position))
                    {
                        if (vehicle.speed >= -1 && vehicle.speed <= 1)
                        {
                            int amount = 0;
                            bool half = true;
                            if (!ctx.MatchParameter(1, "half", "1/2"))
                            {
                                if (!(ctx.TryGet(1, out amount) && amount > 0) && ctx.HasArg(1))
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
                                    amount =
                                        (vehicle.trunkItems.width - vehicle.trunkItems.width % supplyAsset.size_x) *
                                        (vehicle.trunkItems.height - vehicle.trunkItems.height % supplyAsset.size_y) -
                                        c;
                                amount /= (supplyAsset.size_x * supplyAsset.size_y);
                                if (half)
                                    amount /= 2;
                            }

                            if (amount > 0)
                            {
                                if (!vehicle.transform.TryGetComponent(out VehicleComponent c))
                                {
                                    c = vehicle.transform.gameObject.AddComponent<VehicleComponent>();
                                    c.Initialize(vehicle);
                                }

                                if (c.ForceSupplyLoop == null)
                                {
                                    c.StartForceLoadSupplies(ctx.Caller, type, amount);

                                    ctx.LogAction(ActionLogType.LoadSupplies, type.ToString() + " x" + amount);
                                    ctx.Defer();
                                }
                                else throw ctx.Reply(T.LoadAlreadyLoading);
                            }
                            else throw ctx.Reply(T.LoadInvalidAmount, ctx.Get(1)!);
                        }
                        else throw ctx.Reply(T.LoadSpeed);
                    }
                    else throw ctx.Reply(T.LoadNotInMain);
                }
                else throw ctx.SendGamemodeError();
            }
            else throw ctx.Reply(T.LoadNoTarget);
        }
        else throw ctx.Reply(T.LoadUsage);
    }
}
