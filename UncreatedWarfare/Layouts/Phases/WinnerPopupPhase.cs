using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Layouts.Tickets;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Layouts.Phases;

public class WinnerPopupPhase : BasePhase<PhaseTeamSettings>
{
    private readonly ITranslationService _translationService;
    private readonly ILoopTickerFactory _loopTickerFactory;
    private readonly GameOverUITranslations _translations;
    private readonly ITeamManager<Team> _teamManager;
    private readonly ITicketTracker? _ticketTracker;

    private ILoopTicker? _ticker;

    public WinnerPopupPhase(IServiceProvider serviceProvider, IConfiguration config) : base(serviceProvider, config)
    {
        _translationService = serviceProvider.GetRequiredService<ITranslationService>();
        _loopTickerFactory = serviceProvider.GetRequiredService<ILoopTickerFactory>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<GameOverUITranslations>>().Value;
        _teamManager = serviceProvider.GetRequiredService<ITeamManager<Team>>();

        _ticketTracker = serviceProvider.GetService<ITicketTracker>();
    }

    /// <inheritdoc />
    public override async UniTask BeginPhaseAsync(CancellationToken token = default)
    {
        await base.BeginPhaseAsync(token);

        if (!Layout.Data.TryGetValue(KnownLayoutDataKeys.WinnerTeam, out object teamWinnerBox) || teamWinnerBox is not Team winningTeam)
        {
            Logger.LogWarning("Previous phase did not set {0} tag in layout data.", KnownLayoutDataKeys.WinnerTeam);
            await Layout.MoveToNextPhase(token);
            return;
        }

        await UniTask.SwitchToMainThread(token);

        TimeSpan duration = Duration.Ticks > 0 ? Duration : TimeSpan.FromSeconds(10d);

        _ticker = _loopTickerFactory.CreateTicker(duration, Timeout.InfiniteTimeSpan, queueOnGameThread: false, OnTick);
        SendUIToAll(winningTeam);
    }

    private void SendUIToAll(Team winner)
    {
        foreach (LanguageSet set in _translationService.SetOf.AllPlayers())
        {
            SendUI(set, winner);
        }
    }

    protected virtual void SendUI(LanguageSet languageSet, Team winner)
    {
        int teamCount = _teamManager.AllTeams.Count;

        string[] args = new string[teamCount * 2 + 1];
        args[0] = _translations.Title.Translate(winner.Faction, in languageSet);

        int index = 0;
        foreach (Team team in _teamManager.AllTeams)
        {
            if (_ticketTracker != null)
            {
                args[++index] = _translations.TicketsLeft.Translate(_ticketTracker.GetTickets(team), in languageSet);
            }
            else
            {
                args[++index] = string.Empty;
            }

            args[teamCount + index] = team.Faction.FlagImageURL;
        }

        languageSet.SendToasts(new ToastMessage(ToastMessageStyle.GameOver, args));
    }

    /// <inheritdoc />
    public override UniTask EndPhaseAsync(CancellationToken token = default)
    {
        EndTicker();
        return base.EndPhaseAsync(token);
    }

    private void OnTick(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        EndTicker();
        UniTask.Create(async () =>
        {
            try
            {
                await Layout.MoveToNextPhase();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error moving to next phase.");
            }
        });
    }

    private void EndTicker()
    {
        Interlocked.Exchange(ref _ticker, null)?.Dispose();
    }
}

public class GameOverUITranslations : PropertiesTranslationCollection
{
    /// <inheritdoc />
    protected override string FileName => "UI/Game Over";

    [TranslationData("Shows at the end of a game to show who won the game.")]
    public readonly Translation<FactionInfo> Title = new Translation<FactionInfo>("{0}\r\nhas won the battle!", TranslationOptions.UnityUI, FactionInfo.FormatColorDisplayName);

    [TranslationData("The text that shows how many tickets a team had left at the end of the game.")]
    public readonly Translation<int> TicketsLeft = new Translation<int>("{0} ${p:0:Ticket}", TranslationOptions.UnityUI);

    [TranslationData("The text that shows how many caches a team had left at the end of the game.")]
    public readonly Translation<int> CachesLeft = new Translation<int>("{0} ${p:0:Cache} Left", TranslationOptions.UnityUI);
}