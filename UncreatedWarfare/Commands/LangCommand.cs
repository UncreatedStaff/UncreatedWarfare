using System.Text;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Commands;

[Command("lang", "language", "foreign")]
public class LangCommand : IExecutableCommand
{
    private readonly ICachableLanguageDataStore _languageDataStore;
    private readonly LanguageService _languageService;
    private readonly LanguageCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Switch your language to some of our supported languages or see a list.",
            Parameters =
            [
                new CommandParameter("Current")
                {
                    IsOptional = true,
                    Description = "See your current language."
                },
                new CommandParameter("Reset")
                {
                    IsOptional = true,
                    Description = "Changes your language back to default."
                },
                new CommandParameter("Language", typeof(string))
                {
                    IsOptional = true,
                    Description = "Changes your language to your choice of supported language."
                }
            ]
        };
    }

    public LangCommand(ICachableLanguageDataStore languageDataStore, LanguageService languageService, TranslationInjection<LanguageCommandTranslations> translations)
    {
        _languageDataStore = languageDataStore;
        _languageService = languageService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (Context.HasArgsExact(0))
        {
            int i = -1;

            await _languageDataStore.WriteWaitAsync(token);

            StringBuilder sb = new StringBuilder();
            try
            {
                foreach (LanguageInfo info in _languageDataStore.Languages)
                {
                    if (!info.HasTranslationSupport)
                        continue;

                    if (++i != 0) sb.Append(", ");
                    sb.Append(info.Code);
                    sb.Append(" : ").Append(info.DisplayName);
                }
            }
            finally
            {
                _languageDataStore.WriteRelease();
            }

            Context.Reply(_translations.LanguageList, sb.ToString());
        }
        else if (Context.MatchParameter(0, "refersh", "reload", "update"))
        {
            Context.Player.Locale.Preferences = await _languageDataStore.GetLanguagePreferences(Context.CallerId.m_SteamID, token);
            Context.Reply(_translations.LanguageRefreshed);
        }
        else if (Context.MatchParameter(0, "current"))
        {
            Context.AssertRanByPlayer();
            Context.Reply(_translations.LanguageCurrent, Context.Player.Locale.LanguageInfo);
        }
        else if (Context.MatchParameter(0, "reset"))
        {
            Context.AssertRanByPlayer();
            
            if (Context.Player.Locale.IsDefaultLanguage)
                throw Context.Reply(_translations.LangAlreadySet, Context.Player.Locale.LanguageInfo);

            LanguageInfo defaultInfo = _languageService.GetDefaultLanguage();

            await Context.Player.Locale.Update(defaultInfo.Code, Data.LocalLocale, token: token).ConfigureAwait(false);
            Context.Reply(_translations.ResetLanguage, defaultInfo);
            CheckIMGUIRequirements(defaultInfo);
        }
        else if (Context.TryGetRange(0, out string? input) && !string.IsNullOrWhiteSpace(input))
        {
            Context.AssertRanByPlayer();

            LanguageInfo? newSet = _languageDataStore.GetInfoCached(input, false);

            if (newSet == null)
                throw Context.Reply(_translations.LanguageNotFound, input);

            LanguageInfo oldSet = Context.Player.Locale.LanguageInfo;
            if (newSet == oldSet)
                throw Context.Reply(_translations.LangAlreadySet, oldSet);

            await Context.Player.Locale.Update(newSet.Code, null, token: token).ConfigureAwait(false);
            CheckIMGUIRequirements(newSet);
            Context.Reply(_translations.ChangedLanguage, newSet);
        }
        else throw Context.Reply(_translations.ResetLanguageHow);
    }
    private void CheckIMGUIRequirements(LanguageInfo newSet)
    {
        if (Context.Player.Save.IMGUI && !newSet.RequiresIMGUI)
        {
            Context.Reply(_translations.NoIMGUITip1, newSet);
            Context.Reply(_translations.NoIMGUITip2);
        }
        else if (!Context.Player.Save.IMGUI && newSet.RequiresIMGUI)
        {
            Context.Reply(_translations.IMGUITip1, newSet);
            Context.Reply(_translations.IMGUITip2);
        }
    }
}

public class LanguageCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Language";

    [TranslationData("Output from /lang, lists all languages.", "Comma-serparated list of languages")]
    public readonly Translation<string> LanguageList = new Translation<string>("<#f53b3b>Languages: <#e6e3d5>{0}</color>.");

    [TranslationData("Fallback usage output from /lang, explains /lang reset.")]
    public readonly Translation ResetLanguageHow = new Translation("<#f53b3b>Do <#e6e3d5>/lang reset</color> to reset back to default language.");

    [TranslationData("Result from using /lang refresh, reloads ui with updated text.")]
    public readonly Translation LanguageRefreshed = new Translation("<#f53b3b>Refreshed all signs and UI.");

    [TranslationData("Output from /lang current, tells the player their selected language.", "Current Language")]
    public readonly Translation<LanguageInfo> LanguageCurrent = new Translation<LanguageInfo>("<#f53b3b>Current language: <#e6e3d5>{0}</color>.", arg0Fmt: LanguageInfo.FormatDisplayName);

    [TranslationData("Output from /lang <language>, tells the player their new language.", "New Language")]
    public readonly Translation<LanguageInfo> ChangedLanguage = new Translation<LanguageInfo>("<#f53b3b>Changed your language to <#e6e3d5>{0}</color>.", arg0Fmt: LanguageInfo.FormatDisplayName);

    [TranslationData("Output from /lang <language> when the player is using already that language.", "Current Language")]
    public readonly Translation<LanguageInfo> LangAlreadySet = new Translation<LanguageInfo>("<#ff8c69>You are already set to <#e6e3d5>{0}</color>.", arg0Fmt: LanguageInfo.FormatDisplayName);

    [TranslationData("Output from /lang reset, tells the player their language changed to the default language.", "Default Language")]
    public readonly Translation<LanguageInfo> ResetLanguage = new Translation<LanguageInfo>("<#f53b3b>Reset your language to <#e6e3d5>{0}</color>.", arg0Fmt: LanguageInfo.FormatDisplayName);

    [TranslationData("Output from /lang reset when the player is using already that language.", "Default Language")]
    public readonly Translation<LanguageInfo> ResetCurrent = new Translation<LanguageInfo>("<#ff8c69>You are already on the default language: <#e6e3d5>{0}</color>.", arg0Fmt: LanguageInfo.FormatDisplayName);

    [TranslationData("Output from /lang <language> when the language isn't found.", "Input language")]
    public readonly Translation<string> LanguageNotFound = new Translation<string>("<#dd1111>We don't have translations for <#e6e3d5>{0}</color> yet. If you are fluent and want to help, feel free to ask us about submitting translations.");

    [TranslationData("Tells the player that IMGUI is recommended for this language and how to enable it (part 1).", "Language id")]
    public readonly Translation<LanguageInfo> IMGUITip1 = new Translation<LanguageInfo>("<#f53b3b>{0} recommends using IMGUI mode. do <#fff>/options imgui true</color>...", arg0Fmt: LanguageInfo.FormatDisplayName);

    [TranslationData("Tells the player that IMGUI is recommended for this language and how to enable it (part 2).")]
    public readonly Translation IMGUITip2 = new Translation("<#f53b3b>... go to your steam launch options and add <#fff>-Glazier IMGUI</color> to them.");

    [TranslationData("Tells the player that IMGUI is not recommended for this language and how to enable it (part 1).", "Language id")]
    public readonly Translation<LanguageInfo> NoIMGUITip1 = new Translation<LanguageInfo>("<#f53b3b>{0} recommends not using IMGUI mode. do <#fff>/options imgui false</color>...", arg0Fmt: LanguageInfo.FormatDisplayName);

    [TranslationData("Tells the player that IMGUI is not recommended for this language and how to enable it (part 2).")]
    public readonly Translation NoIMGUITip2 = new Translation("<#f53b3b>... go to your steam launch options and remove <#fff>-Glazier IMGUI</color>.");
}