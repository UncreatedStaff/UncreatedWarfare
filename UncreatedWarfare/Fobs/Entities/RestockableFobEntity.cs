using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.FOBs.StateStorage;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Fobs.Entities;

/// <summary>
/// Buildable fob entity that periodically refills it's inventory from a <see cref="BarricadeStateSave"/>.
/// </summary>
public abstract class RestockableBuildableFobEntity<TInfo> : BuildableFobEntity<TInfo> where TInfo : IBuildableFobEntityInfo
{
    private readonly byte[]? _refillState;

    private ILoopTicker? _loopTicker;

    /// <summary>
    /// Invoked after this buildable gets restocked.
    /// </summary>
    public event Action? OnRestock;

    public override bool PreventItemDrops => true;

    protected RestockableBuildableFobEntity(
        IBuildable buildable,
        IServiceProvider serviceProvider,
        bool enableAutoRestock,
        TInfo? info,
        Team team,
        TimeSpan refillInterval = default)
        : base(info, buildable, team, serviceProvider)
    {
        if (buildable.IsStructure || !enableAutoRestock)
            return;

        TimeSpan interval = refillInterval == default ? TimeSpan.FromSeconds(60d) : refillInterval;

        BarricadeStateStore? barricadeStateStore = serviceProvider.GetService<BarricadeStateStore>();

        // try to get a valid state, falling back to the buildable state save if it exists
        BarricadeStateSave? save = barricadeStateStore?.FindBarricadeSave(Buildable.Asset, Team.Faction);

        if (save != null)
        {
            _refillState = Convert.FromBase64String(save.Base64State);
        }
        else
        {
            _refillState = buildable.GetItem<Barricade>().state;
        }

        try
        {
            BarricadeUtility.VerifyState(_refillState, (ItemBarricadeAsset)Buildable.Asset);
        }
        catch (InvalidBarricadeStateException ex)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning(ex, $"{GetType()} failed to get a valid state, resetting to default. Original state");
            _refillState = Buildable.Asset.getState(EItemOrigin.ADMIN);
        }

        BarricadeUtility.WriteOwnerAndGroup(
            _refillState,
            Buildable.GetDrop<BarricadeDrop>(),
            Buildable.Owner.m_SteamID,
            Buildable.Group.m_SteamID
        );

        _loopTicker = serviceProvider.GetRequiredService<ILoopTickerFactory>()
            .CreateTicker(
                interval,
                invokeImmediately: true,
                queueOnGameThread: true,
                onTick: (_, _, _) => Restock()
            );
    }

    /// <summary>
    /// Restock the inventory of this buildable.
    /// </summary>
    public void Restock()
    {
        if (_refillState == null)
            return;

        GameThread.AssertCurrent();

        BarricadeDrop drop = Buildable.GetDrop<BarricadeDrop>();
        if (drop.GetServersideData().barricade.state.SequenceEqual(_refillState))
        {
            return;
        }

        BarricadeUtility.SetState(drop, _refillState);

        HandleRestock();

        try
        {
            OnRestock?.Invoke();
        }
        catch (Exception ex)
        {
            WarfareModule.Singleton.GlobalLogger.LogError(ex, $"Error invoking RestockableFobEntity.OnRestock ({GetType()}).");
        }
    }

    protected virtual void HandleRestock() { }

    public override void Dispose()
    {
        base.Dispose();
        Interlocked.Exchange(ref _loopTicker, null)?.Dispose();
        if (!Buildable.IsStructure && PreventItemDrops)
        {
            BarricadeUtility.PreventItemDrops(Buildable.GetDrop<BarricadeDrop>());
        }
    }
}