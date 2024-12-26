using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Layouts.UI;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.Flags;

public class DefaultCaptureUIFlagEvents :
    IEventListener<FlagContestPointsChanged>,
    IEventListener<FlagContestStateChanged>,
    IEventListener<PlayerEnteredFlagRegion>,
    IEventListener<PlayerExitedFlagRegion>,
    ILayoutPhaseListener<ActionPhase>
{
    private readonly CaptureUI _ui;
    private readonly ITranslationService _translationService;
    private readonly Layout _layout;
    private readonly FlagUITranslations _translations;

    public DefaultCaptureUIFlagEvents(
        CaptureUI ui,
        ITranslationService translationService,
        TranslationInjection<FlagUITranslations> translations,
        Layout layout)
    {
        _ui = ui;
        _translationService = translationService;
        _layout = layout;
        _translations = translations.Value;
    }

    private void UpdateUIForPlayers(FlagObjective flag)
    {
        foreach (LanguageSet set in _translationService.SetOf.PlayersIn(flag.Players))
        {
            if (!set.Team.IsValid)
            {
                while (set.MoveNext())
                    _ui.HideCaptureUI(set.Next);
                continue;
            }

            CaptureUIState state = EvaluateCaptureUI(flag, set);

            _ui.UpdateCaptureUI(set, in state);
        }
    }

    protected virtual CaptureUIState EvaluateCaptureUI(FlagObjective flag, LanguageSet languageSet)
    {
        Team team = languageSet.Team;

        SingleLeaderContest contest = flag.Contest;

        float progress = contest.LeaderPoints / (float)contest.MaxPossiblePoints;

        FlagContestState flagContestState = flag.CurrentContestState;

        string location = flag.Name;

        if (!team.IsValid)
        {
            return CaptureUIState.Ineffective(_translations, location);
        }

        if (flagContestState.State == FlagContestState.ContestState.Contested)
        {
            return CaptureUIState.Contesting(_translations, progress, location);
        }

        Team contester = flagContestState.Winner is { IsValid: true } ? flagContestState.Winner : Team.NoTeam;
        // if score == 0 then contester will be leader
        Team scoreLeader = contest.Leader is { IsValid: true } ? contest.Leader : contester;

        if (contester.IsValid)
        {
            if (contester.IsFriendly(team))
            {
                if (scoreLeader == contester && contest.LeaderPoints == contest.MaxPossiblePoints)
                {
                    return CaptureUIState.Secured(_translations, location);
                }

                if (flagContestState.State != FlagContestState.ContestState.NotObjective)
                {
                    return scoreLeader == contester
                        ? CaptureUIState.Capturing(_translations, progress, contester, location)
                        : CaptureUIState.Clearing(_translations, progress, scoreLeader, location);
                }
            }

            if (contester.IsOpponent(team))
            {
                if (contest.LeaderPoints == contest.MaxPossiblePoints)
                {
                    return CaptureUIState.Lost(_translations, flag.Owner, location);
                }

                return CaptureUIState.Losing(_translations, progress, contester, location);
            }
        }

        return CaptureUIState.Ineffective(_translations, location);
    }

    void IEventListener<FlagContestPointsChanged>.HandleEvent(FlagContestPointsChanged e, IServiceProvider serviceProvider)
    {
        if (_layout.ActivePhase is not ActionPhase)
            return;

        UpdateUIForPlayers(e.Flag);
    }

    void IEventListener<FlagContestStateChanged>.HandleEvent(FlagContestStateChanged e, IServiceProvider serviceProvider)
    {
        if (_layout.ActivePhase is not ActionPhase)
            return;

        UpdateUIForPlayers(e.Flag);
    }

    void IEventListener<PlayerEnteredFlagRegion>.HandleEvent(PlayerEnteredFlagRegion e, IServiceProvider serviceProvider)
    {
        if (_layout.ActivePhase is not ActionPhase)
            return;

        LanguageSet set = new LanguageSet(e.Player);
        CaptureUIState state = EvaluateCaptureUI(e.Flag, set);
        _ui.UpdateCaptureUI(set, in state);
    }

    void IEventListener<PlayerExitedFlagRegion>.HandleEvent(PlayerExitedFlagRegion e, IServiceProvider serviceProvider)
    {
        if (_layout.ActivePhase is not ActionPhase)
            return;

        _ui.HideCaptureUI(e.Player);
    }

    UniTask ILayoutPhaseListener<ActionPhase>.OnPhaseStarted(ActionPhase phase, CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    UniTask ILayoutPhaseListener<ActionPhase>.OnPhaseEnded(ActionPhase phase, CancellationToken token)
    {
        foreach (LanguageSet set in _translations.TranslationService.SetOf.AllPlayers())
        {
            _ui.HideCaptureUI(set);
        }

        return UniTask.CompletedTask;
    }
}