using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Phases.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Layouts.UI;
using Uncreated.Warfare.Moderation.Records;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.AdvanceAndSecure.Logic;
public abstract class DualSideFlagService : 
    BaseFlagService,
    IEventListener<FlagCaptured>,
    IEventListener<FlagNeutralized>
{
    // todo: this class is not actually two-sided. Maybe combine it with BaseFlagService?
    private readonly ILoopTickerFactory _loopTickerFactory;
    private ILoopTicker? _loopTicker;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ITranslationService _translationService;
    protected readonly CaptureUITranslations _translations;
    protected readonly Layout _layout;

    public DualSideFlagService(
        IConfiguration configuration,
        IServiceProvider serviceProvider
    )
        : base(serviceProvider, configuration)
    {
        _serviceProvider = serviceProvider;
        _layout = serviceProvider.GetRequiredService<Layout>();
        _loopTickerFactory = serviceProvider.GetRequiredService<ILoopTickerFactory>();
        _translationService = serviceProvider.GetRequiredService<ITranslationService>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<CaptureUITranslations>>().Value;
    }
    public override async UniTask StartAsync(CancellationToken token)
    {
        await base.StartAsync(token);

        TimeSpan tickSpeed = Configuration.GetValue("TickSpeed", TimeSpan.FromSeconds(4d));

        RecalculateObjectives();

        _loopTicker = _loopTickerFactory.CreateTicker(tickSpeed, tickSpeed, true, OnTick);
    }
    public override async UniTask StopAsync(CancellationToken token)
    {
        await base.StopAsync(token);

        Interlocked.Exchange(ref _loopTicker, null)?.Dispose();
    }
    protected void TriggerVictory(Team winner)
    {
        _ = _layout.MoveToNextPhase(token: default, winner);
    }
    private void OnTick(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        foreach (FlagObjective flag in ActiveFlags)
        {
            FlagContestResult contestResult = GetContestResult(flag, _layout.TeamManager.AllTeams);
            Logger.LogInformation("contest result: " + contestResult.State.ToString());
            if (contestResult.State == FlagContestResult.ContestState.OneTeamIsLeading)
            {
                flag.MarkContested(false);
                flag.Contest.AwardPoints(contestResult.Leader!, 12);
            }
            else if (contestResult.State == FlagContestResult.ContestState.Contested)
                flag.MarkContested(true);
        }
    }
    protected abstract void RecalculateObjectives();
    protected abstract FlagContestResult GetContestResult(FlagObjective flag, IEnumerable<Team> possibleContestingTeams);

    void IEventListener<FlagNeutralized>.HandleEvent(FlagNeutralized e, IServiceProvider serviceProvider)
    {
        RecalculateObjectives();
    }

    void IEventListener<FlagCaptured>.HandleEvent(FlagCaptured e, IServiceProvider serviceProvider)
    {
        RecalculateObjectives();

        foreach (Team team in _layout.TeamManager.AllTeams)
        {
            if (ActiveFlags.All(f => f.Owner == team))
            {
                TriggerVictory(team);
                return;
            }
        }
    }
}