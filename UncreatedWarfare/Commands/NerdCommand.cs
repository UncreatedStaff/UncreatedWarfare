using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("nerd", "nred"), MetadataFile]
internal sealed class NerdCommand : IExecutableCommand
{
    private readonly NerdService _nerdService;
    private readonly IPlayerService _playerService;
    private readonly IUserDataService _userDataService;
    private readonly NerdTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public NerdCommand(
        NerdService nerdService,
        IPlayerService playerService,
        IUserDataService userDataService,
        TranslationInjection<NerdTranslations> translations)
    {
        _nerdService = nerdService;
        _playerService = playerService;
        _userDataService = userDataService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(2);

        (CSteamID? steam64, _) = await Context.TryGetPlayer(1, remainder: true).ConfigureAwait(false);
        if (!steam64.HasValue)
        {
            throw Context.SendPlayerNotFound();
        }

        bool add = Context.MatchParameter(0, "add", "nerd", "set");
        if (!add && !Context.MatchParameter(0, "remove", "cancel", "reset"))
        {
            throw Context.SendHelp();
        }

        IPlayer player = await _playerService.GetOfflinePlayer(steam64.Value, _userDataService, token);

        if (!await _nerdService.SetNerdnessAsync(steam64.Value, Context.CallerId, add, token))
        {
            throw Context.Reply(add ? _translations.AlreadyNerd : _translations.AlreadyChad, player);
        }

        Context.Reply(add ? _translations.AddedNerd : _translations.RemovedNerd, player);
    }
}

public sealed class NerdTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Nerd";

    [TranslationData("Sent when a player tries to nerd someone that's already a nerd.", IsPriorityTranslation = false)]
    public Translation<IPlayer> AlreadyNerd = new Translation<IPlayer>("{0} is already a nerd.", arg0Fmt: WarfarePlayer.FormatColoredDisplayOrPlayerName);

    [TranslationData("Sent when a player tries to un-nerd someone that's isn't a nerd.", IsPriorityTranslation = false)]
    public Translation<IPlayer> AlreadyChad = new Translation<IPlayer>("{0} is not a nerd.", arg0Fmt: WarfarePlayer.FormatColoredDisplayOrPlayerName);

    [TranslationData("Sent when a player adds someone as a nerd.", IsPriorityTranslation = false)]
    public Translation<IPlayer> AddedNerd = new Translation<IPlayer>("{0} is now a nerd.", arg0Fmt: WarfarePlayer.FormatColoredDisplayOrPlayerName);

    [TranslationData("Sent when a player removed someone as a nerd.", IsPriorityTranslation = false)]
    public Translation<IPlayer> RemovedNerd = new Translation<IPlayer>("{0} is no longer a nerd.", arg0Fmt: WarfarePlayer.FormatColoredDisplayOrPlayerName);
}