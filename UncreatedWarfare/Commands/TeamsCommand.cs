using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Lobby;
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("teams", "team"), MetadataFile]
internal sealed class TeamsCommand : IExecutableCommand
{
    private readonly CooldownManager _cooldownManager;
    private readonly ZoneStore _zoneStore;
    private readonly LobbyZoneManager _lobbyManager;
    private readonly ITeamManager<Team> _teamManager;
    private readonly TeamsCommandTranslations _translations;

    private static readonly PermissionLeaf PermissionInstantLobby = new PermissionLeaf("features.instant_lobby", unturned: false, warfare: true);

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public TeamsCommand(
        CooldownManager cooldownManager,
        ZoneStore zoneStore,
        LobbyZoneManager lobbyManager,
        TranslationInjection<TeamsCommandTranslations> translations,
        ITeamManager<Team> teamManager
        )
    {
        _cooldownManager = cooldownManager;
        _zoneStore = zoneStore;
        _lobbyManager = lobbyManager;
        _teamManager = teamManager;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (_cooldownManager.HasCooldown(Context.Player, KnownCooldowns.ChangeTeams, out Cooldown cooldown) && !await Context.HasPermission(PermissionInstantLobby, token))
        {
            throw Context.Reply(_translations.TeamsCooldown, cooldown);
        }

        if (!_zoneStore.IsInMainBase(Context.Player, Context.Player.Team.Faction))
        {
            throw Context.Reply(Context.CommonTranslations.NotInMain);
        }

        Zone? lobbyZone = _lobbyManager.GetLobbyZone();

        if (lobbyZone == null)
            throw Context.SendUnknownError();

        _cooldownManager.StartCooldown(Context.Player, KnownCooldowns.ChangeTeams);
        await _lobbyManager.JoinLobbyAsync(Context.Player, token);
        throw Context.Defer();
    }
}

public class TeamsCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Teams Command";

    [TranslationData("Sent when changing teams is on cooldown", "Amount of time left on cooldown")]
    public readonly Translation<Cooldown> TeamsCooldown = new Translation<Cooldown>("<#ff8c69>You can't use /teams for another {0}.", arg0Fmt: Cooldown.FormatTimeLong);
}