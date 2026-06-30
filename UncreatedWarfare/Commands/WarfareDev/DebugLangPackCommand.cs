using Stripe;
using System;
using System.IO;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Commands;

[Command("langpack"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugLangPackCommand : IExecutableCommand
{
    private readonly ITranslationService _translationService;
    private readonly ILanguageDataStore _languageDataStore;
    private readonly WarfareModule _module;

    public required CommandContext Context { get; init; }

    public DebugLangPackCommand(ITranslationService translationService, ILanguageDataStore languageDataStore, WarfareModule module)
    {
        _translationService = translationService;
        _languageDataStore = languageDataStore;
        _module = module;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(1, "Language");

        LanguageInfo? lang = await _languageDataStore.GetInfo(Context.GetRange(0)!, false, allowCache: true, token);
        if (lang == null)
            throw Context.ReplyString($"Language {Context.GetRange(0)} not found.");

        if (lang.IsDefault)
            throw Context.ReplyString("Can not create a lang-pack for the default language.");

        string fileName = Path.Combine(TranslationService.TranslationsFolder, lang.DisplayName + ".zip");
        string location = Path.Combine(_module.HomeDirectory, fileName);

        await _translationService.ExportAsync(lang, location, token);

        Context.ReplyString($"Exported a lang-pack for {lang.DisplayName} to {fileName}.");
    }
}