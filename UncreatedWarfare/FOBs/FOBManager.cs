using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Fobs;

public class FobManager : ILayoutHostedService, IEventListener<BarricadePlaced>
{
    private readonly FobConfiguration _config;

    private readonly List<IFobItem> _floatingItems;
    private readonly List<IFob> _fobs;

    /// <summary>
    /// Items placed by players that aren't linked to a specific FOB.
    /// </summary>
    public IReadOnlyList<IFobItem> FloatingItems { get; private set; }
    
    /// <summary>
    /// List of all FOBs in the world.
    /// </summary>
    public IReadOnlyList<IFob> Fobs { get; private set; }

    public FobManager(FobConfiguration config)
    {
        _config = config;

        _floatingItems = new List<IFobItem>(32);
        _fobs = new List<IFob>(24);

        FloatingItems = new ReadOnlyCollection<IFobItem>(_floatingItems);
        Fobs = new ReadOnlyCollection<IFob>(_fobs);
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    void IEventListener<BarricadePlaced>.HandleEvent(BarricadePlaced e, IServiceProvider serviceProvider)
    {
        
    }
}