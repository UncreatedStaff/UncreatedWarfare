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

[Command("add", "give", "award"), SubCommandOf(typeof(PointsExperienceCommand))]
internal sealed class PointsAddExperienceCommand : IExecutableCommand
{
    private readonly PointsService _pointsService;
    private readonly LanguageService _languageService;
    private readonly IFactionDataStore _factionDataStore;
    private readonly ITeamManager<Team> _teamManager;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public PointsAddExperienceCommand(PointsService pointsService, LanguageService languageService, IFactionDataStore factionDataStore, ITeamManager<Team> teamManager)
    {
        _pointsService = pointsService;
        _languageService = languageService;
        _factionDataStore = factionDataStore;
        _teamManager = teamManager;
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
            throw Context.ReplyString("Invalid number.");
        }

        if (!Context.TryGet(argSt + 1, out string? factionStr)
            || _factionDataStore.FindFaction(factionStr) is not { } faction
            || _teamManager.AllTeams.FirstOrDefault(x => x.Faction.FactionId.Equals(faction.FactionId, StringComparison.Ordinal)) is not { } team)
        {
            throw Context.ReplyString("Invalid faction.");
        }

        LanguageSet set = onlinePlayer != null
            ? new LanguageSet(onlinePlayer)
            : new LanguageSet(_languageService.GetDefaultLanguage(), _languageService.GetDefaultCulture(), TimeZoneInfo.Utc, false, team);

        await _pointsService.ApplyEvent(playerId.Value, faction.PrimaryKey, _pointsService.GetAdminEvent(in set, value, null, null), token);

        Context.ReplyString($"Awarded {value.ToString(Context.Culture)} XP to {playerId.Value.m_SteamID.ToString("D17", Context.Culture)} on {faction.Name}.");
    }
}