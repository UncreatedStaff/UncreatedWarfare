using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Fobs;

public partial class FobManager : ILayoutHostedService
{
    private readonly FobConfiguration _configuration;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly TranslationInjection<FobTranslations> _translations;
    private readonly ChatService _chatService;
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
        _translations = serviceProvider.GetRequiredService<TranslationInjection<FobTranslations>>();
        _chatService = serviceProvider.GetRequiredService<ChatService>();
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
        string fobName = $"FOB {griddy.LetterXPhoneticName}-{griddy.Y + 1}";

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