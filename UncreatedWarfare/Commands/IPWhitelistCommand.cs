using System;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players.Management.Legacy;

namespace Uncreated.Warfare.Commands;

[Command("ipwhitelist", "whitelistip", "whip", "ipwh", "iw")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class IPWhitelistCommand : IExecutableCommand
{
    private const string Syntax = "/ipwhitelist <add|remove> <steam64> [ip = any]";
    private const string Help = "Whitelist a player's IP to not be IP banned.";
    private static readonly string[] RemArgs = { "remove", "delete", "rem", "blacklist" };

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help,
            Parameters =
            [
                new CommandParameter("Add")
                {
                    Aliases = [ "whitelist", "create" ],
                    Description = "Add an IP range to the IP whitelist.",
                    Parameters =
                    [
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            Parameters =
                            [
                                new CommandParameter("IP", typeof(IPv4Range))
                                {
                                    IsOptional = true
                                }
                            ]
                        }
                    ]
                },
                new CommandParameter("Remove")
                {
                    Aliases = [ "delete", "rem", "blacklist" ],
                    Description = "Remove an ip range from the IP whitelist.",
                    Parameters =
                    [
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            Parameters =
                            [
                                new CommandParameter("IP", typeof(IPv4Range))
                                {
                                    IsOptional = true
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        bool remove = Context.MatchParameter(0, RemArgs);
        if (remove || Context.MatchParameter(0, "add", "whitelist", "create"))
        {
            IPv4Range range = default;
            if (!Context.TryGet(1, out ulong player, out _) || Context.TryGet(2, out string str) && !IPv4Range.TryParse(str, out range) && !IPv4Range.TryParseIPv4(str, out range))
                throw Context.SendCorrectUsage(Syntax);

            PlayerNames names = await F.GetPlayerOriginalNamesAsync(player, token).ConfigureAwait(false);
            if (await Data.ModerationSql.WhitelistIP(player, Context.CallerId.m_SteamID, range, !remove, DateTimeOffset.UtcNow, token).ConfigureAwait(false) == StandardErrorCode.Success)
                Context.Reply(remove ? T.IPUnwhitelistSuccess : T.IPWhitelistSuccess, names, range);
            else
                Context.Reply(T.IPWhitelistNotFound, names, range);
        }
        else Context.SendCorrectUsage(Syntax);
    }
}