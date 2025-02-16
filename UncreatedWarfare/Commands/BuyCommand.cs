using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("buy"), MetadataFile]
internal sealed class BuyCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _kitTranslations;
    private readonly KitCommandLookResolver _lookResolver;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public BuyCommand(TranslationInjection<KitCommandTranslations> translations, KitCommandLookResolver lookResolver)
    {
        _lookResolver = lookResolver;
        _kitTranslations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        KitCommandLookResult result = await _lookResolver.ResolveFromArgumentsOrLook(Context, 0, 0, KitInclude.Buyable, token).ConfigureAwait(false);

        if (!result.IsSign)
        {
            throw Context.SendHelp();
        }

        throw Context.SendNotImplemented();
        //await _kitManager.Requests.BuyKit(Context, kit, drop.model.position, token).ConfigureAwait(false);
    }
}
