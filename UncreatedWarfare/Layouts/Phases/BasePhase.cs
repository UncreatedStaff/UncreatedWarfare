using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.Phases;

public abstract class BasePhase<TTeamSettings> : ILayoutPhase where TTeamSettings : PhaseTeamSettings
{
    private readonly ILoggerFactory _loggerFactory;
    protected readonly ITeamManager<Team> TeamManager;
    protected readonly Layout Layout;

    [field: MaybeNull, AllowNull]
    protected ILogger Logger => field ??= _loggerFactory.CreateLogger(GetType());

    public bool IsActive { get; private set; }

    [UsedImplicitly]
    public TimeSpan Duration { get; set; } = TimeSpan.MinValue;

    /// <summary>
    /// Display name of the phase on the popup toast for all teams.
    /// </summary>
    [UsedImplicitly]
    public TranslationList? Name { get; set; }

    /// <summary>
    /// Per-team behavior of the phase.
    /// </summary>
    [UsedImplicitly]
    public IReadOnlyList<TTeamSettings>? Teams { get; set; }

    /// <inheritdoc />
    public IConfiguration Configuration { get; }

    protected BasePhase(IServiceProvider serviceProvider, IConfiguration config)
    {
        Configuration = config;
        TeamManager = serviceProvider.GetRequiredService<ITeamManager<Team>>();
        _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        Layout = serviceProvider.GetRequiredService<Layout>();
    }

    public virtual UniTask InitializePhaseAsync(CancellationToken token = default)
    {
        if (Teams is not { Count: > 0 })
            return UniTask.CompletedTask;

        int i = 0;
        foreach (TTeamSettings settings in Teams)
        {
            settings.TeamInfo = TeamManager.FindTeam(settings.Team);
            settings.Configuration = Configuration.GetSection($"Teams:{i.ToString(CultureInfo.InvariantCulture)}");
            ++i;
        }

        return UniTask.CompletedTask;
    }

    public virtual UniTask BeginPhaseAsync(CancellationToken token = default)
    {
        IsActive = true;
        return UniTask.CompletedTask;
    }

    public virtual UniTask EndPhaseAsync(CancellationToken token = default)
    {
        IsActive = false;
        return UniTask.CompletedTask;
    }
}