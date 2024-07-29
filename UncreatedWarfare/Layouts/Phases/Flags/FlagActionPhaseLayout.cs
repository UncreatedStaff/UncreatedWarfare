using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;
using Uncreated.Warfare.Zones.Pathing;

namespace Uncreated.Warfare.Layouts.Phases.Flags;
public class FlagActionPhaseLayout : ILayoutPhase
{
    private readonly ILogger<FlagActionPhaseLayout> _logger;
    private readonly IServiceProvider _serviceProvider;
    private IList<Zone>? _pathingResult;
    public bool IsActive { get; private set; }

    public FlagPhaseSettings Flags { get; set; } = new FlagPhaseSettings();

    /// <inheritdoc />
    public IConfigurationSection Configuration { get; }

    public FlagActionPhaseLayout(ILogger<FlagActionPhaseLayout> logger, IServiceProvider serviceProvider, IConfigurationSection config)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        Configuration = config;
    }

    public async UniTask InitializePhaseAsync(CancellationToken token = default)
    {
        if (!ContextualTypeResolver.TryResolveType(Flags.Pathing, out Type? pathingProviderType, typeof(IZonePathingProvider)))
        {
            _logger.LogError("Unknown or invalid pathing provider type: {0}.", Flags.Pathing);

            throw new LayoutConfigurationException(this, "Invalid pathing provider type.");
        }

        // create zone providers from config
        IReadOnlyList<Type> zoneProviderTypes = Flags.GetFlagPoolTypes(_logger);

        List<IZoneProvider> zoneProviders = new List<IZoneProvider>(zoneProviderTypes.Count);
        foreach (Type zoneProviderType in zoneProviderTypes)
        {
            zoneProviders.Add((IZoneProvider)ActivatorUtilities.CreateInstance(_serviceProvider, zoneProviderType, this));
        }

        ZoneStore zoneStore = new ZoneStore(zoneProviders);

        IConfigurationSection config = Configuration.GetSection("PathingData");
        IZonePathingProvider pathingProvider = (IZonePathingProvider)ActivatorUtilities.CreateInstance(_serviceProvider, pathingProviderType, this, zoneStore, config);

        config.Bind(pathingProvider);

        _pathingResult = await pathingProvider.CreateZonePathAsync(token);
    }

    public virtual UniTask BeginPhaseAsync(CancellationToken token = default)
    {
        IsActive = true;

        // todo
        // IList<Zone> zonePath = _pathingResult ?? throw new LayoutConfigurationException(this, "Unable to create zone path."); // shouldn't happen

        return UniTask.CompletedTask;
    }

    public virtual UniTask EndPhaseAsync(CancellationToken token = default)
    {
        IsActive = false;
        return UniTask.CompletedTask;
    }
}
