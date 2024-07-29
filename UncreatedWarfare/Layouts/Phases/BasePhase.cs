using Cysharp.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.Phases;
public class BasePhase<TTeamSettings> : ILayoutPhase where TTeamSettings : PhaseTeamSettings
{
    private readonly ITeamManager<Team> _teamManager;
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
        _teamManager = serviceProvider.GetRequiredService<ITeamManager<Team>>();
    }

    public virtual UniTask InitializePhaseAsync(CancellationToken token = default)
    {
        if (Teams is not { Length: > 0 })
            return UniTask.CompletedTask;

        for (int i = 0; i < Teams.Length; i++)
        {
            TTeamSettings settings = Teams[i];
            settings.TeamInfo = _teamManager.FindTeam(settings.Team);
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