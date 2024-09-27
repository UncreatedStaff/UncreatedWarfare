using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Lobby;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("teams", "team")]
public class TeamsCommand : IExecutableCommand
{
    private readonly CooldownManager _cooldownManager;
    private readonly ZoneStore _zoneStore;
    private readonly LobbyZoneManager _lobbyManager;
    private readonly TeamsCommandTranslations _translations;

    private static readonly PermissionLeaf PermissionShuffle = new PermissionLeaf("commands.teams.shuffle", unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionInstantLobby = new PermissionLeaf("features.instant_lobby", unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Switch teams without rejoining the server.",
            Parameters =
            [
                new CommandParameter("Shuffle")
                {
                    Aliases = [ "sh" ],
                    Description = "Force the teams to be shuffled next game.",
                    Permission = PermissionShuffle,
                    IsOptional = true
                }
            ]
        };
    }

    public TeamsCommand(CooldownManager cooldownManager, ZoneStore zoneStore, LobbyZoneManager lobbyManager, TranslationInjection<TeamsCommandTranslations> translations)
    {
        _cooldownManager = cooldownManager;
        _zoneStore = zoneStore;
        _lobbyManager = lobbyManager;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        //if (Context.MatchParameter(0, "shuffle", "sh"))
        //{
        //    await Context.AssertPermissions(PermissionShuffle, token);
        //    await UniTask.SwitchToMainThread(token);

        //    throw Context.Reply(T.TeamsShuffleQueued);
        //} todo probably removing this

        if (_cooldownManager.HasCooldown(Context.Player, CooldownType.ChangeTeams, out Cooldown cooldown) && !await Context.HasPermission(PermissionInstantLobby, token))
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

        _cooldownManager.StartCooldown(Context.Player, CooldownType.ChangeTeams, /* todo */ 2000f);
        Context.Player.UnturnedPlayer.teleportToLocationUnsafe(lobbyZone.Spawn, lobbyZone.SpawnYaw);
        throw Context.Defer();
    }
}

public class TeamsCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Teams Command";

    [TranslationData("Sent when changing teams is on cooldown", "Amount of time left on cooldown")]
    public readonly Translation<Cooldown> TeamsCooldown = new Translation<Cooldown>("<#ff8c69>You can't use /teams for another {0}.", arg0Fmt: Cooldown.FormatTimeLong);
}