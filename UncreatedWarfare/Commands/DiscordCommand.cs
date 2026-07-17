using Microsoft.Extensions.Configuration;
using System;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("discord", "dicsord"), MetadataFile]
internal sealed class DiscordCommand : IExecutableCommand
{
    private readonly IConfiguration _configuration;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public DiscordCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    internal static string GetDiscordJoinUrl(IConfiguration configuration)
    {
        string? discordInviteCode = configuration["discord_invite_code"];

        string url;
        if (string.IsNullOrEmpty(discordInviteCode))
        {
            url = "https://uncreated.network/discord";
        }
        else if (discordInviteCode.StartsWith("http", StringComparison.Ordinal))
        {
            url = discordInviteCode;
        }
        else
        {
            url = "https://discord.gg/" + discordInviteCode;
        }

        return url;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        string url = GetDiscordJoinUrl(_configuration);

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