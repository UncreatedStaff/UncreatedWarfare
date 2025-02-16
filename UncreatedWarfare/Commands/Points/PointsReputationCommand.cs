using System;
using System.Linq;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("add", "give", "award"), SubCommandOf(typeof(PointsReputationCommand))]
internal sealed class PointsAddReputationCommand : IExecutableCommand
{
    private readonly PointsService _pointsService;
    private readonly LanguageService _languageService;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public PointsAddReputationCommand(PointsService pointsService, LanguageService languageService)
    {
        _pointsService = pointsService;
        _languageService = languageService;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        int argSt = 1;
        (CSteamID? playerId, WarfarePlayer? onlinePlayer) = await Context.TryGetPlayer(0).ConfigureAwait(false);

        if (!playerId.HasValue)
        {
            playerId = Context.CallerId;
            onlinePlayer = Context.Player;

            if (!Context.HasArgs(0))
                throw Context.SendHelp();

            argSt = 0;
        }

        if (!playerId.Value.IsIndividual())
            throw Context.SendPlayerNotFound();

        if (!Context.TryGet(argSt, out double value))
        {
            Context.ReplyString("Invalid number.");
            throw Context.SendHelp();
        }

        LanguageSet set = onlinePlayer != null
            ? new LanguageSet(onlinePlayer)
            : new LanguageSet(_languageService.GetDefaultLanguage(), _languageService.GetDefaultCulture(), TimeZoneInfo.Utc, false, Team.NoTeam);

        await _pointsService.ApplyEvent(playerId.Value, 0, _pointsService.GetAdminEvent(in set, null, null, value), token);

        Context.ReplyString($"Awarded {value.ToString(Context.Culture)} reputation to {playerId.Value.m_SteamID.ToString("D17", Context.Culture)}.");
    }
}