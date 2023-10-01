using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;

namespace Uncreated.Warfare.Commands;
public class IPWhitelistCommand : AsyncCommand
{
    private const string SYNTAX = "/ipwhitelist <add|remove> <steam64> [ip = any]";
    private const string HELP = "Whitelist a player's IP to not be IP banned.";
    private static readonly string[] RemArgs = { "remove", "delete", "rem", "blacklist" };

    public IPWhitelistCommand() : base("ipwhitelist", EAdminType.STAFF)
    {
        AddAlias("whitelistip");
        AddAlias("whip");
        AddAlias("ipwh");
        AddAlias("iw");
        Structure = new CommandStructure
        {
            Description = HELP,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Add")
                {
                    Aliases = new string[] { "whitelist", "create" },
                    Description = "Add an IP range to the IP whitelist.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("IP", typeof(IPv4Range))
                                {
                                    IsOptional = true
                                }
                            }
                        }
                    }
                },
                new CommandParameter("Remove")
                {
                    Aliases = new string[] { "delete", "rem", "blacklist" },
                    Description = "Remove an ip range from the IP whitelist.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("IP", typeof(IPv4Range))
                                {
                                    IsOptional = true
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        bool remove = ctx.MatchParameter(0, RemArgs);
        if (remove || ctx.MatchParameter(0, "add", "whitelist", "create"))
        {
            IPv4Range range = default;
            if (!ctx.TryGet(1, out ulong player, out _) || ctx.TryGet(2, out string str) && !IPv4Range.TryParse(str, out range) && !IPv4Range.TryParseIPv4(str, out range))
                throw ctx.SendCorrectUsage(SYNTAX);

            PlayerNames names = await F.GetPlayerOriginalNamesAsync(player, token).ConfigureAwait(false);
            if (await Data.ModerationSql.WhitelistIP(player, ctx.CallerID, range, !remove, DateTimeOffset.UtcNow, token).ConfigureAwait(false) == StandardErrorCode.Success)
                ctx.Reply(remove ? T.IPUnwhitelistSuccess : T.IPWhitelistSuccess, names, range);
            else
                ctx.Reply(T.IPWhitelistNotFound, names, range);
        }
        else ctx.SendCorrectUsage(SYNTAX);
    }
}