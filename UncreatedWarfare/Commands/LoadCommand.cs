using SDG.Unturned;
using Steamworks;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Vehicles;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class LoadCommand : Command
{
    private const string SYNTAX = "/load <build|ammo> <amount>";
    private const string HELP = "Loads supplies into a logistics truck.";

    public LoadCommand() : base("load", EAdminType.MEMBER) { }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, "load_e_usage", Array.Empty<string>());

        if (ctx.MatchParameter(0, "build", "ammo"))
        {
            if (ctx.TryGet(1, out int amount) && amount > 0)
            {
                if (ctx.TryGetTarget(out InteractableVehicle vehicle))
                {
                    if (vehicle.lockedOwner == CSteamID.Nil || vehicle.lockedGroup == CSteamID.Nil)
                        throw ctx.Reply(T.LoadNoTarget);

                    if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data) && data.Type == EVehicleType.LOGISTICS)
                    {
                        if (F.IsInMain(vehicle.transform.position))
                        {
                            if (vehicle.speed >= -1 && vehicle.speed <= 1)
                            {
                                if (!vehicle.transform.TryGetComponent(out VehicleComponent c))
                                {
                                    c = vehicle.transform.gameObject.AddComponent<VehicleComponent>();
                                    c.Initialize(vehicle);
                                }

                                ESupplyType type = ESupplyType.BUILD;
                                if (ctx.MatchParameter(0, "build")) type = ESupplyType.BUILD;
                                else if (ctx.MatchParameter(0, "ammo")) type = ESupplyType.AMMO;
                                else throw ctx.Reply(T.LoadUsage);

                                if (c.forceSupplyLoop == null)
                                    c.StartForceLoadSupplies(ctx.Caller, type, amount);

                                ctx.LogAction(EActionLogType.LOAD_SUPPLIES, type.ToString() + " x" + amount);
                                ctx.Defer();
                            }
                            else throw ctx.Reply(T.LoadSpeed);
                        }
                        else throw ctx.Reply(T.LoadNotInMain);
                    }
                    else throw ctx.Reply(T.LoadNotLogisticsVehicle);
                }
                else throw ctx.Reply(T.LoadNoTarget);
            }
            else throw ctx.Reply(T.LoadInvalidAmount);
        }
        else throw ctx.Reply(T.LoadUsage);
    }
}
