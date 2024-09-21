using Uncreated.Warfare.Injures;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("heal")]
[MetadataFile(nameof(GetHelpMetadata))]
public class HealCommand : IExecutableCommand
{
    private readonly ChatService _chatService;
    private readonly HealCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Heal yourself or someone else to max health and revive them if they're injured.",
            Parameters =
            [
                new CommandParameter("Player", typeof(IPlayer))
                {
                    IsOptional = true,
                    IsRemainder = true
                }
            ]
        };
    }

    public HealCommand(ChatService chatService, TranslationInjection<HealCommandTranslations> translations)
    {
        _chatService = chatService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertOnDuty();

        if (!Context.TryGet(0, out _, out WarfarePlayer? onlinePlayer) || onlinePlayer == null)
        {
            if (Context.HasArgs(1))
                throw Context.SendPlayerNotFound();

            onlinePlayer = Context.Player;
        }

        onlinePlayer.UnturnedPlayer.life.sendRevive();

        PlayerInjureComponent? injureComponent = onlinePlayer.ComponentOrNull<PlayerInjureComponent>();
        if (injureComponent != null)
            injureComponent.Revive();

        _chatService.Send(onlinePlayer, _translations.HealSelf);

        if (onlinePlayer.Steam64.m_SteamID != Context.CallerId.m_SteamID)
            Context.Reply(_translations.HealPlayer, onlinePlayer);
        else
            Context.Defer();

        return UniTask.CompletedTask;
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