using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("sign", "text"), SubCommandOf(typeof(KitSetCommand))]
internal sealed class KitSetSignTextCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly IKitDataStore _kitDataStore;
    private readonly ILanguageDataStore _languageDataStore;
    public required CommandContext Context { get; init; }

    public KitSetSignTextCommand(IKitDataStore kitDataStore, TranslationInjection<KitCommandTranslations> translations, ILanguageDataStore languageDataStore)
    {
        _kitDataStore = kitDataStore;
        _languageDataStore = languageDataStore;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string? kitId) || !Context.TryGet(1, out string? languageCode) || !Context.TryGetRange(2, out string? value))
        {
            throw Context.SendHelp();
        }

        Kit? kit = await _kitDataStore.QueryKitAsync(kitId, KitInclude.Translations, token);

        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitId);
        }

        kitId = kit.Id;

        LanguageInfo? language = await _languageDataStore.GetInfo(languageCode, true, true, token).ConfigureAwait(false);
        if (language == null)
        {
            throw Context.Reply(_translations.KitLanguageNotFound, languageCode);
        }

        string newName = FormattingUtility.ReplaceNewLineSubstrings(value);

        await _kitDataStore.UpdateKitAsync(kit.Key, KitInclude.Translations | KitInclude.UnlockRequirements, kit =>
        {
            bool found = false;
            for (int i = kit.Translations.Count - 1; i >= 0; --i)
            {
                KitTranslation t = kit.Translations[i];
                if (t.LanguageId != language.Key)
                    continue;

                found = true;
                t.Value = newName;
                break;
            }

            if (!found)
            {
                kit.Translations.Add(new KitTranslation
                {
                    KitId = kit.PrimaryKey,
                    LanguageId = language.Key,
                    Value = newName
                });
            }
        }, Context.CallerId, token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        newName = newName.Replace("\n", "<br>");

        Context.Reply(_translations.KitPropertySet, $"Name ({language.Code})", kit, newName);
        // todo: Context.LogAction(ActionLogType.SetKitProperty, $"{kitId}: SIGN TEXT | \"{language.Code}\" >> \"{newName}\"");
    }
}