using Microsoft.Extensions.Configuration;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("discord", "dicsord")]
[MetadataFile(nameof(GetHelpMetadata))]
public class DiscordCommand : IExecutableCommand
{
    private readonly string? _discordInviteCode;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public DiscordCommand(IConfiguration configuration)
    {
        _discordInviteCode = configuration["discord_invite_code"];
    }

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
        if (Context.Player != null)
        {
            Context.ReplyUrl("Join our Discord Server", "https://discord.gg/" + _discordInviteCode);
        }
        else
        {
            Context.ReplyString("https://discord.gg/" + _discordInviteCode);
        }

        return default;
    }
}
