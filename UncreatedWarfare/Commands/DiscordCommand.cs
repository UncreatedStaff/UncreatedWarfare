using Microsoft.Extensions.Configuration;
using System;
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
        string url;
        if (string.IsNullOrEmpty(_discordInviteCode))
        {
            url = "https://uncreated.network/discord";
        }
        else if (_discordInviteCode.StartsWith("http", StringComparison.Ordinal))
        {
            url = _discordInviteCode;
        }
        else
        {
            url = "https://discord.gg/" + _discordInviteCode;
        }

        if (Context.Player != null)
        {
            Context.ReplyUrl("Join our Discord Server", url);
        }
        else
        {
            Context.ReplyString(url);
        }

        return UniTask.CompletedTask;
    }
}