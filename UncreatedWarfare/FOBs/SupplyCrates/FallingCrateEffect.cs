using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class FallingCrateEffect : FallingBuildableEffect
{
#nullable disable
    private FobManager _fobManager;

#nullable restore
    
    private SupplyCrateStack? _stack;

    private int _level = -1, _index = -1;

    /// <inheritdoc />
    protected internal override void Initialize(ref FallingBuildableArgs args, in FallingEffectInstantiationArgs config, Action? onSettle)
    {
        base.Initialize(ref args, in config, onSettle);
        _fobManager = ServiceProvider.GetRequiredService<FobManager>();
    }

    protected override void OnConverted(IBuildable buildable)
    {
        SupplyCrateInfo? crateInfo = _fobManager.Configuration.SupplyCrates.FirstOrDefault(x => x.SupplyItemAsset.MatchAsset(buildable.Asset));
        if (crateInfo == null)
        {
            // this shouldn't really happen since the SupplyCrateInfo has to exist for this to spawn in the first place
            WarfareModule.Singleton.GlobalLogger.LogError("Unable to find CrateInfo for FallingCrateEffect after it was spawned before.");;
            return;
        }

        Team team = ServiceProvider.GetRequiredService<ITeamManager<Team>>().GetTeam(buildable.Group);

        SupplyCrate supplyCrate = new SupplyCrate(
            crateInfo,
            buildable,
            ServiceProvider,
            team,
            _stack,
            _level,
            _index,
            // while saving buildable state we don't want it to reset
            Owner is not { IsOnDuty: true }
        );

        _fobManager.RegisterFobEntity(supplyCrate);

        base.OnConverted(buildable);
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject @object = other.gameObject;
        if (@object.layer != LayerMasks.LOGIC)
            return;

        SupplyStackComponent? stack = @object.GetComponentInParent<SupplyStackComponent>();
        if (stack == null || stack.Stack.Asset.GUID != Buildable.GUID || !stack.Stack.TryGetNextCratePosition(out int level, out int index, out Vector3 position))
        {
            return;
        }

        _level = level;
        _index = index;
        _stack = stack.Stack;

        transform.SetPositionAndRotation(position, _stack.Rotation);
        SkipWaitForSettle();
    }
}