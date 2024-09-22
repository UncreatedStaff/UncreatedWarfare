#if DEBUG
#endif
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("load")]
public class LoadCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Loads supplies into a logistics truck. If no amount is given, it fills the vehicle. If 'half' is supplied, it fills half the empty slots.",
            Parameters =
            [
                new CommandParameter("Type", "Build", "Ammo")
                {
                    Parameters =
                    [
                        new CommandParameter("Amount", typeof(float))
                        {
                            IsOptional = true,
                            Description = "Loads your vehicle's trunk with a set amount of supplies (or until it's full)."
                        },
                        new CommandParameter("Half")
                        {
                            Aliases = [ "1/2", ".5", "0.5", ",5", "0,5" ],
                            IsOptional = true,
                            Description = "Loads half of the empty space in your vehicle's trunk."
                        }
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
#if false
        Context.AssertRanByPlayer();

        Context.AssertHelpCheck(0, T.LoadUsage);

        if (!Context.MatchParameter(0, "build", "ammo"))
            throw Context.Reply(T.LoadUsage);

        if (!Context.TryGetVehicleTarget(out InteractableVehicle? vehicle) || vehicle.lockedOwner == CSteamID.Nil || vehicle.lockedGroup == CSteamID.Nil)
        {
            throw Context.Reply(T.LoadNoTarget);
        }

        VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
        if (bay is not { IsLoaded: true })
        {
            throw Context.SendGamemodeError();
        }

        SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        if (data is null || data.Item == null || !VehicleData.IsLogistics(data.Item.Type))
            throw Context.Reply(T.LoadNotLogisticsVehicle);

        if (!F.IsInMain(vehicle.transform.position))
            throw Context.Reply(T.LoadNotInMain);

        if (!(vehicle.speed >= -1) || !(vehicle.speed <= 1))
            throw Context.Reply(T.LoadSpeed);

        int amount = 0;
        bool half = true;

        if (!Context.MatchParameter(1, "half", "1/2"))
        {
            if (!Context.MatchParameter(1, "max", "full") && !(Context.TryGet(1, out amount) && amount > 0) && Context.HasArgs(2))
            {
                throw Context.Reply(T.LoadInvalidAmount, Context.Get(1)!);
            }

            half = false;
        }

        SupplyType type;

        if (Context.MatchParameter(0, "build"))
            type = SupplyType.Build;
        else if (Context.MatchParameter(0, "ammo"))
            type = SupplyType.Ammo;
        else
            throw Context.Reply(T.LoadUsage);

        if (amount == 0)
        {
            if (vehicle.trunkItems == null)
            {
                throw Context.Reply(T.LoadInvalidAmount, Context.Get(1)!);
            }

            ItemAsset? supplyAsset;
            ulong team = vehicle.lockedGroup.m_SteamID.GetTeam();
            if (type == SupplyType.Build)
                TeamManager.GetFaction(team).Build.TryGetAsset(out supplyAsset);
            else
                TeamManager.GetFaction(team).Ammo.TryGetAsset(out supplyAsset);

            if (supplyAsset == null)
            {
                throw Context.ReplyString($"Unknown asset: {type}.");
            }

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
            throw Context.Reply(T.LoadInvalidAmount, Context.Get(1)!);

        if (!vehicle.transform.TryGetComponent(out VehicleComponent vehicleComponent))
        {
            vehicleComponent = vehicle.transform.gameObject.AddComponent<VehicleComponent>();
            vehicleComponent.Initialize(vehicle);
        }

        if (vehicleComponent.ForceSupplyLoop != null)
            throw Context.Reply(T.LoadAlreadyLoading);

        vehicleComponent.StartForceLoadSupplies(Context.Player, type, amount);

        Context.LogAction(ActionLogType.LoadSupplies, type + " x" + amount);
        Context.Defer();
#endif
    }
}