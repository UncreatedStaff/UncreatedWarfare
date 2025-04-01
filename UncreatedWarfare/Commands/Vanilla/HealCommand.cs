using Uncreated.Warfare.Injures;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("heal"), MetadataFile]
internal sealed class HealCommand : IExecutableCommand
{
    private readonly ChatService _chatService;
    private readonly HealCommandTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public HealCommand(ChatService chatService, TranslationInjection<HealCommandTranslations> translations)
    {
        _chatService = chatService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        (_, WarfarePlayer? onlinePlayer) = await Context.TryGetPlayer(0, remainder: true).ConfigureAwait(false);

        if (onlinePlayer == null)
        {
            if (Context.HasArgs(1))
                throw Context.SendPlayerNotFound();

            Context.AssertRanByPlayer();
            onlinePlayer = Context.Player;
        }
        
        await UniTask.SwitchToMainThread(token);

        onlinePlayer.UnturnedPlayer.life.sendRevive();

        PlayerInjureComponent? injureComponent = onlinePlayer.ComponentOrNull<PlayerInjureComponent>();
        if (injureComponent != null)
            injureComponent.Revive();

        if (onlinePlayer.Steam64.m_SteamID != Context.CallerId.m_SteamID)
        {
            _chatService.Send(onlinePlayer, _translations.HealSelf);
            Context.Reply(_translations.HealPlayer, onlinePlayer);
        }
        else
        {
            Context.Reply(_translations.HealSelf);
        }
    }
}

public class HealCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Heal";

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer> HealPlayer = new Translation<IPlayer>("<#ff9966>You healed {0}.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData("Sent to a player when they're healed, either by themselves or another player using /heal.")]
    public readonly Translation HealSelf = new Translation("<#ff9966>You were healed.");
}