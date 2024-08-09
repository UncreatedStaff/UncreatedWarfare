using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.Phases;
public class BasePhase<TTeamSettings> : ILayoutPhase where TTeamSettings : PhaseTeamSettings
{
    protected ITeamManager<Team> TeamManager;
    public bool IsActive { get; private set; }
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Display name of the phase on the popup toast for all teams.
    /// </summary>
    public TranslationList? Name { get; set; }

    /// <summary>
    /// Per-team behavior of the phase.
    /// </summary>
    public TTeamSettings[]? Teams { get; set; }

    /// <inheritdoc />
    public IConfigurationSection Configuration { get; }

    public BasePhase(IServiceProvider serviceProvider, IConfigurationSection config)
    {
        Configuration = config;
        TeamManager = serviceProvider.GetRequiredService<ITeamManager<Team>>();
    }

    public virtual UniTask InitializePhaseAsync(CancellationToken token = default)
    {
        if (Teams is not { Length: > 0 })
            return UniTask.CompletedTask;

        for (int i = 0; i < Teams.Length; i++)
        {
            TTeamSettings settings = Teams[i];
            settings.TeamInfo = TeamManager.FindTeam(settings.Team);
            settings.Configuration = Configuration.GetSection($"Teams:{i.ToString(CultureInfo.InvariantCulture)}");
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