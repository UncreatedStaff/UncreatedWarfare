using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("ipwhitelist", "whitelistip", "whip", "ipwh", "iw"), MetadataFile]
internal sealed class IPWhitelistCommand : IExecutableCommand
{
    private readonly IUserDataService _userDataService;
    private readonly DatabaseInterface _moderationSql;
    private readonly IPWhitelistCommandTranslations _translations;

    private static readonly string[] RemArgs = [ "remove", "delete", "rem", "blacklist" ];

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public IPWhitelistCommand(TranslationInjection<IPWhitelistCommandTranslations> translations, IUserDataService userDataService, DatabaseInterface moderationSql)
    {
        _userDataService = userDataService;
        _moderationSql = moderationSql;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        bool remove = Context.MatchParameter(0, RemArgs);
        if (!remove && !Context.MatchParameter(0, "add", "whitelist", "create"))
            throw Context.SendHelp();
        
        IPv4Range range = default;

        (CSteamID? player, _) = await Context.TryGetPlayer(1).ConfigureAwait(false);

        if (!player.HasValue || Context.TryGet(2, out string? str) && !IPv4Range.TryParse(str, out range) && !IPv4Range.TryParseIPv4(str, out range))
        {
            throw Context.SendHelp();
        }

        PlayerNames names = await _userDataService.GetUsernamesAsync(player.Value.m_SteamID, token).ConfigureAwait(false);

        if (await _moderationSql.WhitelistIP(player.Value, Context.CallerId, range, !remove, DateTimeOffset.UtcNow, token).ConfigureAwait(false))
        {
            Context.Reply(remove ? _translations.IPUnwhitelistSuccess : _translations.IPWhitelistSuccess, names, range);
        }
        else
        {
            Context.Reply(_translations.IPWhitelistNotFound, names, range);
        }
    }
}

public class IPWhitelistCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/IP Whitelist";

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, IPv4Range> IPWhitelistSuccess = new Translation<IPlayer, IPv4Range>("<#00ffff>Whitelisted the IP range: <#9cffb3>{1}</color> for {0}.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, IPv4Range> IPUnwhitelistSuccess = new Translation<IPlayer, IPv4Range>("<#00ffff>Unwhitelisted the IP range: <#9cffb3>{1}</color> for {0}.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, IPv4Range> IPWhitelistNotFound = new Translation<IPlayer, IPv4Range>("<#b3a6a2>The IP range: <#9cffb3>{1}</color> is not whitelisted for {0}.");
}