using Microsoft.Extensions.Configuration;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("discord", "dicsord"), MetadataFile]
internal sealed class DiscordCommand : IExecutableCommand
{
    private readonly string? _discordInviteCode;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public DiscordCommand(IConfiguration configuration)
    {
        _discordInviteCode = configuration["discord_invite_code"];
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        if (_discordInviteCode == null)
        {
            throw Context.SendNotImplemented();
        }

        if (Context.Player != null)
        {
            Context.ReplyUrl("Join our Discord Server", "https://discord.gg/" + _discordInviteCode);
        }
        else
        {
            Context.ReplyString("https://discord.gg/" + _discordInviteCode);
        }

        return UniTask.CompletedTask;
    }
}