using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("rename", "rname", "name"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitRenameCommand : IExecutableCommand
{
    private readonly KitCommandLookResolver _lookResolver;
    private readonly KitCommandTranslations _translations;
    private readonly LanguageService _languageService;
    private readonly IKitDataStore _kitDataStore;
    private readonly IKitAccessService _kitAccessService;

    public required CommandContext Context { get; init; }

    public KitRenameCommand(TranslationInjection<KitCommandTranslations> translations,
        KitCommandLookResolver lookResolver,
        IKitAccessService kitAccessService,
        LanguageService languageService,
        IKitDataStore kitDataStore)
    {
        _translations = translations.Value;
        _lookResolver = lookResolver;
        _kitAccessService = kitAccessService;
        _languageService = languageService;
        _kitDataStore = kitDataStore;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();
        
        KitCommandLookResult result = await _lookResolver.ResolveFromArgumentsOrLook(Context, 0, 1, KitInclude.Translations, token).ConfigureAwait(false);

        Kit kit = result.Kit;
        if (kit.Type != KitType.Loadout)
        {
            throw Context.Reply(_translations.KitRenameNotLoadout);
        }

        string? name = Context.GetRange(0);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw Context.SendHelp();
        }

        if (ChatFilterHelper.GetChatFilterViolation(name) is { } filterViolation)
        {
            throw Context.Reply(_translations.KitRenameFilterVoilation, filterViolation);
        }

        if (kit.IsLocked || kit.Season < WarfareModule.Season || !await _kitAccessService.HasAccessAsync(Context.CallerId, kit.Key, token).ConfigureAwait(false))
        {
            throw Context.Reply(_translations.KitRenameNoAccess, kit);
        }

        LanguageInfo defaultLanguage = _languageService.GetDefaultLanguage();

        string oldName = kit.GetDisplayName(defaultLanguage, true, removeNewLine: false);
        string newName = FormattingUtility.ReplaceNewLineSubstrings(name);

        await _kitDataStore.UpdateKitAsync(kit.Key, KitInclude.Translations, kit =>
        {
            bool found = false;
            for (int i = kit.Translations.Count - 1; i >= 0; --i)
            {
                KitTranslation t = kit.Translations[i];
                if (found || t.LanguageId != defaultLanguage.Key)
                {
                    kit.Translations.RemoveAt(i);
                    continue;
                }

                found = true;
                t.Value = newName;
            }

            if (!found)
            {
                kit.Translations.Add(new KitTranslation
                {
                    KitId = kit.PrimaryKey,
                    LanguageId = defaultLanguage.Key,
                    Value = newName
                });
            }

        }, Context.CallerId, token).ConfigureAwait(false);

        oldName = oldName.Replace("\n", "<br>");
        newName = newName.Replace("\n", "<br>");

        int ldId = LoadoutIdHelper.Parse(kit.Id);
        string ldIdStr = ldId == -1 ? "???" : LoadoutIdHelper.GetLoadoutLetter(ldId).ToUpperInvariant();
        // todo: Context.LogAction(ActionLogType.SetKitProperty, $"{kit.Id}: SIGN TEXT | \"{defaultLanguage.Code}\" >> \"{newName}\" (using /kit rename)");
        Context.Reply(_translations.KitRenamed, ldIdStr, oldName, newName);
    }
}
