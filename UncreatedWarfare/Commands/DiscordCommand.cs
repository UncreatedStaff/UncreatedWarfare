using Uncreated.Warfare.Commands.Dispatch;

namespace Uncreated.Warfare.Commands;

[Command("discord", "dicsord")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class DiscordCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Sends the Discord link to the Uncreated Network server."
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        if (Context.Caller is not null)
        {
            Context.Player.UnturnedPlayer.channel.owner.SendURL("Join our Discord Server", "https://discord.gg/" + UCWarfare.Config.DiscordInviteCode);
            Context.Defer();
        }
        else
        {
            Context.ReplyString("https://discord.gg/" + UCWarfare.Config.DiscordInviteCode);
        }

        return default;
    }
}
