using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Moderation.Discord;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("link"), MetadataFile]
internal sealed class LinkCommand : IExecutableCommand
{
    private readonly AccountLinkingService _linkingService;
    private readonly IUserDataService _userDataService;
    private readonly LinkCommandTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public LinkCommand(
        AccountLinkingService linkingService,
        TranslationInjection<LinkCommandTranslations> translations,
        IUserDataService userDataService)
    {
        _linkingService = linkingService;
        _userDataService = userDataService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (Context.HasArgs(1))
        {
            string tkn = Context.GetRange(0)!;

            bool success = await _linkingService.ResolveLinkFromSteamAsync(tkn, Context.CallerId, CancellationToken.None)
                .ConfigureAwait(false);

            if (!success)
            {
                throw Context.Reply(_translations.TokenNotRecognized, tkn);
            }

            ulong discordId = await _userDataService.GetDiscordIdAsync(Context.CallerId.m_SteamID, CancellationToken.None);
            Context.Reply(_translations.SuccessfullyLinked, discordId);
        }
        else
        {
            SteamDiscordPendingLink link = await _linkingService.BeginLinkFromSteamAsync(Context.CallerId, token: token);

            Context.Reply(_translations.StartedLink, link.Token);

            await UniTask.Delay(1000, cancellationToken: CancellationToken.None);

            if (!Context.Player.IsOnline)
                return;

            Context.ReplyUrl(_translations.CopyTokenPopup.Translate(Context.Caller), "https://uncreated.network/copy-text?text=" + link.Token);
        }
    }
}

public sealed class LinkCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Link";

    [TranslationData("Sent when a player tries to link their Discord and Steam accounts but uses an unknown token.", "The linking token")]
    public readonly Translation<string> TokenNotRecognized = new Translation<string>("<#ffab87>Unknown linking token: <#fff>{0}</color>. Note that tokens are case-sensitive.");

    [TranslationData("Sent when a player link their Discord and Steam accounts using a token.", "Discord ID", "Steam64 ID")]
    public readonly Translation<ulong> SuccessfullyLinked = new Translation<ulong>("<#ccffcc>Linked <#7483c4>Discord</color> account ID <i><#ddd>{0}</color></i> to your Steam account.");

    [TranslationData("Sent when a player starts linking their Discord and Steam accounts and will have to complete the link in Discord.", "The linking token")]
    public readonly Translation<string> StartedLink = new Translation<string>("<#ccffcc>Use <#fff>/link token:<b>{0}</b></color> (case-sensitive) in <#7483c4>Discord</color> (/discord) to finish linking your accounts.");

    [TranslationData("Sent on the vanilla open URL popup.", "The linking token")]
    public readonly Translation<string> CopyTokenPopup = new Translation<string>("Copy Token", TranslationOptions.UnityUI | TranslationOptions.NoRichText);
}