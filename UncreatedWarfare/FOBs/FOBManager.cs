using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Fobs;

public partial class FobManager : ILayoutHostedService
{
    private readonly FobConfiguration _configuration;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FobManager> _logger;
    private readonly TrackingList<IFobItem> _floatingItems;
    private readonly TrackingList<IFob> _fobs;

    /// <summary>
    /// Items placed by players that aren't linked to a specific FOB.
    /// </summary>
    public IReadOnlyList<IFobItem> FloatingItems { get; }

    /// <summary>
    /// List of all FOBs in the world.
    /// </summary>
    public IReadOnlyList<IFob> Fobs { get; }

    public FobManager(IServiceProvider serviceProvider, ILogger<FobManager> logger)
    {
        _configuration = serviceProvider.GetRequiredService<FobConfiguration>();
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _serviceProvider = serviceProvider;
        _logger = logger;
        _fobs = new TrackingList<IFob>(24);
        _floatingItems = new TrackingList<IFobItem>(32);

        Fobs = new ReadOnlyTrackingList<IFob>(_fobs);
        FloatingItems = new ReadOnlyTrackingList<IFobItem>(_floatingItems);
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    public BuildableFob RegisterFob(IBuildable fobBuildable)
    {
        GridLocation griddy = new GridLocation(fobBuildable.Position);
        string fobName = $"FOB {NATOPhoneticAlphabetHelper.GetProperCase(griddy.LetterX)}-{griddy.Y + 1}";

        BuildableFob fob = new BuildableFob(_serviceProvider, fobName, fobBuildable);
        _fobs.Add(fob);
        _logger.LogDebug("Registered new FOB: " + fob);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobRegistered { Fob = fob });
        return fob;
    }
    public bool DeregisterFob(IFob fob)
    {
        IFob? existing = _fobs.FindAndRemove(f => f == fob);
        if (existing == null)
            return false;
        _logger.LogDebug("Deregistered FOB: " + fob);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobDeregistered { Fob = fob });
        fob.DestroyAsync();
        return true;
    }
}