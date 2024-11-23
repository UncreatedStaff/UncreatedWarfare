using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.Phases;
public class BasePhase<TTeamSettings> : ILayoutPhase where TTeamSettings : PhaseTeamSettings
{
    protected ITeamManager<Team> TeamManager;
    public bool IsActive { get; private set; }
    public TimeSpan Duration { get; set; } = TimeSpan.MinValue;

    /// <summary>
    /// Display name of the phase on the popup toast for all teams.
    /// </summary>
    public TranslationList? Name { get; set; }

    /// <summary>
    /// Per-team behavior of the phase.
    /// </summary>
    public IReadOnlyList<TTeamSettings>? Teams { get; set; }

    /// <inheritdoc />
    public IConfiguration Configuration { get; }

    public BasePhase(IServiceProvider serviceProvider, IConfiguration config)
    {
        Configuration = config;
        TeamManager = serviceProvider.GetRequiredService<ITeamManager<Team>>();
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

    public virtual UniTask BeginPhaseAsync(object[] dataFromPreviousPhase, CancellationToken token = default)
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