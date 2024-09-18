using Uncreated.Warfare.Injures;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Commands;

[Command("heal")]
[MetadataFile(nameof(GetHelpMetadata))]
public class HealCommand : IExecutableCommand
{
    private readonly ChatService _chatService;

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

    public HealCommand(ChatService chatService)
    {
        _chatService = chatService;
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

        _chatService.Send(onlinePlayer, T.HealSelf);

        if (onlinePlayer.Steam64.m_SteamID != Context.CallerId.m_SteamID)
            Context.Reply(T.HealPlayer, onlinePlayer);
        else
            Context.Defer();

        return UniTask.CompletedTask;
    }
}