using System;
using System.Linq;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Stats;

namespace Uncreated.Warfare.Commands;

[Command("addstat", "stat"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugAddToStatCommand : IExecutableCommand
{
    private readonly LeaderboardPhase _leaderboardPhase;
    public required CommandContext Context { get; init; }

    public DebugAddToStatCommand(Layout layout)
    {
        _leaderboardPhase = layout.Phases.OfType<LeaderboardPhase>().FirstOrDefault()
                                ?? throw new InvalidOperationException("No leaderboard phase active.");
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(1);

        string statId = Context.Get(0)!;

        double amount = 1;
        if (Context.HasArgs(2) && !Context.TryGet(1, out amount) || amount == 0)
        {
            throw Context.ReplyString($"Invalid amount: {Context.Get(1)}.");
        }

        WarfarePlayer target;
        if (Context.HasArgs(3))
        {
            (_, WarfarePlayer? onlinePlayer) = await Context.TryGetPlayer(2, remainder: true, PlayerNameType.PlayerName);
            target = onlinePlayer ?? throw Context.SendPlayerNotFound();
        }
        else
        {
            Context.AssertRanByPlayer();
            target = Context.Player;
        }

        int statIndex = _leaderboardPhase.GetStatIndex(statId);
        if (statIndex == -1)
        {
            throw Context.ReplyString($"Stat {statId} is not currently active.");
        }

        LeaderboardPhaseStatInfo stat = _leaderboardPhase.PlayerStats[statIndex];
        string statName = stat.DisplayName?.Translate(Context.Language, stat.Name) ?? stat.Name;

        PlayerGameStatsComponent stats = target.Component<PlayerGameStatsComponent>();
        string format = stat.NumberFormat ?? "0.##";

        if (stat.CachedExpression != null)
        {
            throw Context.ReplyString($"Stat \"{statName}\" is a cacluated stat and can't be changed. Current value: {stats.GetStatValue(stat).ToString(format, Context.Culture)}.");
        }

        stats.AddToStat(statIndex, amount);

        double newValue = stats.Stats[statIndex];

        if (target.Equals(Context.Player))
            Context.ReplyString($"Added {amount.ToString(format, Context.Culture)} to stat \"{statName}\". New value: {newValue.ToString(format, Context.Culture)}.");
        else
            Context.ReplyString($"Added {amount.ToString(format, Context.Culture)} to stat \"{statName}\" for player {target}. New value: {newValue.ToString(format, Context.Culture)}.");
    }
}