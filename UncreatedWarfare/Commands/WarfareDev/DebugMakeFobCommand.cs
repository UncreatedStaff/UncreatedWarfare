using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("fob"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugMakeFobCommand : IExecutableCommand
{
    private readonly FobManager _fobManager;
    private readonly DroppedItemTracker _itemTracker;
    private readonly AssetConfiguration _assetConfig;
    public required CommandContext Context { get; init; }

    public DebugMakeFobCommand(FobManager fobManager, DroppedItemTracker itemTracker, AssetConfiguration assetConfig)
    {
        _fobManager = fobManager;
        _itemTracker = itemTracker;
        _assetConfig = assetConfig;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGetTargetInfo(out RaycastInfo? raycast, distance: 16f))
        {
            throw Context.ReplyString("Look at something.");
        }

        if (!Context.Player.Team.IsValid)
        {
            throw Context.ReplyString("Join a team.");
        }

        ItemPlaceableAsset unbuiltFob = _assetConfig.GetAssetLink<ItemPlaceableAsset>("Buildables:Gameplay:FobUnbuilt").GetAssetOrFail();

        Quaternion rotation = Quaternion.Euler(-90, Context.Player.UnturnedPlayer.look.yaw, 0);

        IBuildable @base = BuildableExtensions.DropBuildable(unbuiltFob, raycast.point, rotation, Context.Player.Steam64, Context.Player.GroupId);

        await UniTask.NextFrame();
        await UniTask.NextFrame();

        BunkerFob? fob = _fobManager.FindBuildableFob<BunkerFob>(@base);
        if (fob == null)
        {
            throw Context.ReplyString("Failed to place FOB.");
        }

        if (fob.Shovelable == null)
        {
            throw Context.ReplyString("Failed to find shovelable for FOB.");
        }

        // drop supplies
        if (Context.MatchFlag('a', "--ammo"))
        {
            SupplyCrateInfo? ammoSupply = _fobManager.Configuration.SupplyCrates
                .OrderByDescending(x => x.StartingSupplies)
                .FirstOrDefault(x => x.Type == SupplyType.Ammo);

            if (ammoSupply == null)
            {
                Context.ReplyString("No ammo supplies configured.");
            }
            else
            {
                Context.Logger.LogInformation("Dropping ammo supplies.");
                Item item = new Item(ammoSupply.SupplyItemAsset.GetAssetOrFail(), EItemOrigin.ADMIN);
                float startingSupplies = fob.AmmoCount;
                _itemTracker.SimulateDroppingItem(Context.Player, item, Context.Player.Position);
                for (int i = 0; i < 50; ++i)
                {
                    // wait for ammo supplies to apply up to 5 sec
                    await UniTask.Delay(100, cancellationToken: token);
                    if (!Mathf.Approximately(fob.AmmoCount, startingSupplies))
                        break;
                }
            }
        }

        if (Context.MatchFlag('b', "--build"))
        {
            SupplyCrateInfo? buildSupply = _fobManager.Configuration.SupplyCrates
                .OrderByDescending(x => x.StartingSupplies)
                .FirstOrDefault(x => x.Type == SupplyType.Build);

            if (buildSupply == null)
            {
                Context.ReplyString("No build supplies configured.");
            }
            else
            {
                Context.Logger.LogInformation("Dropping build supplies.");
                Item item = new Item(buildSupply.SupplyItemAsset.GetAssetOrFail(), EItemOrigin.ADMIN);
                float startingSupplies = fob.BuildCount;
                _itemTracker.SimulateDroppingItem(Context.Player, item, Context.Player.Position);
                for (int i = 0; i < 50; ++i)
                {
                    // wait for build supplies to apply up to 5 sec
                    await UniTask.Delay(100, cancellationToken: token);
                    if (!Mathf.Approximately(fob.BuildCount, startingSupplies))
                        break;
                }
            }
        }

        if (Context.MatchFlag('q', "--quickbuild"))
        {
            await UniTask.Delay(500, cancellationToken: token);

            Context.Logger.LogInformation("Quick-building FOB.");
            fob.Shovelable.Shovel(Context.Player, raycast.point, fob.Shovelable.HitsRemaining);
        }

        Context.ReplyString($"Created FOB at <#ddd>{fob.ClosestLocation}</color>.");
    }
}