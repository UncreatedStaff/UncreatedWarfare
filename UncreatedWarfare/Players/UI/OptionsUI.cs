using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations;

// ReSharper disable ClassNeverInstantiated.Local

namespace Uncreated.Warfare.Players.UI;

[UnturnedUI(BasePath = "Container")]
public class OptionsUI : UnturnedUI
{
    private readonly OptionsUITranslations _translations;
    private readonly Func<CSteamID, OptionsUIData> _createUiData;

    private readonly IReadOnlyCollection<TimeZoneInfo> _allTimeZones;
    private readonly CultureInfo[] _allCultures;

    private readonly UnturnedLabel _optionsTitle = new UnturnedLabel("Title");
    private readonly UnturnedLabel _internationalizationTitle = new UnturnedLabel("Localization/Viewport/Content/Title");

    private readonly PlaceholderTextBox _searchLanguageBox = new PlaceholderTextBox("Localization/Viewport/Content/SearchL10N/L10N_SearchBox", "./Viewport/Placeholder");
    private readonly LabeledButton _searchLanguageButton = new LabeledButton("Localization/Viewport/Content/SearchL10N/L10N_Search", "./Label");

    private readonly PlaceholderTextBox _searchCultureBox = new PlaceholderTextBox("Localization/Viewport/Content/SearchI14N/I14N_SearchBox", "./Viewport/Placeholder");
    private readonly LabeledButton _searchCultureButton = new LabeledButton("Localization/Viewport/Content/SearchI14N/I14N_Search", "./Label");

    private readonly PlaceholderTextBox _searchTimeZoneBox = new PlaceholderTextBox("Localization/Viewport/Content/SearchTZ/TZ_SearchBox", "./Viewport/Placeholder");
    private readonly LabeledButton _searchTimeZoneButton = new LabeledButton("Localization/Viewport/Content/SearchTZ/TZ_Search", "./Label");

    private readonly UnturnedLabel _noLanguagesFound = new UnturnedLabel("Localization/Viewport/Content/L10N_1/NoFound");
    private readonly UnturnedLabel _noCulturesFound = new UnturnedLabel("Localization/Viewport/Content/I14N_1/NoFound");
    private readonly UnturnedLabel _noTimeZonesFound = new UnturnedLabel("Localization/Viewport/Content/TZ_1/NoFound");

    private readonly LanguageResult[] _languageResults = ElementPatterns.CreateArray<LanguageResult>("Localization/Viewport/Content/L10N_{0}", 1, to: 6);
    private readonly CultureResult[] _cultureResults = ElementPatterns.CreateArray<CultureResult>("Localization/Viewport/Content/L10N_{0}", 1, to: 25);
    private readonly TimeZoneResult[] _timeZoneResults = ElementPatterns.CreateArray<TimeZoneResult>("Localization/Viewport/Content/TZ_{0}", 1, to: 25);

    private readonly LabeledUnturnedToggle _imguiOption = new LabeledUnturnedToggle(false, "Options/Viewport/Content/Btn_IMGUI_Toggle", "./ToggleState", "../Label_IMGUI", null);
    private readonly LabeledUnturnedToggle _trackQuestsOption = new LabeledUnturnedToggle(true, "Options/Viewport/Content/Btn_TrackQuests_Toggle", "./ToggleState", "../Label_TrackQuests", null);
    private readonly LabeledUnturnedToggle _useCultureForCommandInput = new LabeledUnturnedToggle(false, "Localization/Viewport/Content/SearchI14N/I14N_CustomInputToggle", "./State", "../CustomInputTitle", null);
    private readonly UnturnedLabel _useCultureForCommandInputDescription = new UnturnedLabel("Localization/Viewport/Content/SearchI14N/CultureDescription");

    private readonly UnturnedLabel _imguiDescription = new UnturnedLabel("Options/Viewport/Content/Description_IMGUI");
    private readonly UnturnedLabel _trackQuestsDescription = new UnturnedLabel("Options/Viewport/Content/Description_TrackQuests");

    public OptionsUI(ILoggerFactory loggerFactory, AssetConfiguration assetConfiguration, TranslationInjection<OptionsUITranslations> translations)
        : base(loggerFactory, assetConfiguration.GetAssetLink<EffectAsset>("UI:Options"), staticKey: true, reliable: true)
    {
        _translations = translations.Value;
        _allTimeZones = TimeZoneInfo.GetSystemTimeZones();
        _allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

        _createUiData = steam64 => new OptionsUIData(steam64, this);

        _searchLanguageBox.OnTextUpdated += OnSearchLanguages;
        _searchCultureBox.OnTextUpdated += OnSearchCultures;
        _searchTimeZoneBox.OnTextUpdated += OnSearchTimeZones;
    }

    private OptionsUIData GetOrCreateData(CSteamID player)
    {
        return GetOrAddData(player, _createUiData);
    }

    public void Open(WarfarePlayer player)
    {
        OptionsUIData data = GetOrCreateData(player.Steam64);

        ITransportConnection c = player.Connection;
        if (!data.HasUI)
        {
            SendToPlayer(c);
            data.HasUI = true;
        }

        if (!player.Locale.LanguageInfo.IsDefault)
            UpdateText(player);

        _imguiOption.Set(player.UnturnedPlayer, player.Save.IMGUI);
        _trackQuestsOption.Set(player.UnturnedPlayer, player.Save.TrackQuests);
        _useCultureForCommandInput.Set(player.UnturnedPlayer, player.Locale.Preferences.UseCultureForCommandInput);

        WarfarePlayerLocale locale = player.Locale;
        
        _searchCultureBox.SetText(c, locale.CultureInfo.EnglishName);
        _searchLanguageBox.SetText(c, locale.LanguageInfo.DisplayName);
        _searchTimeZoneBox.SetText(c, locale.TimeZone.Id);
    }

    public void UpdateText(WarfarePlayer player)
    {
        ITransportConnection c = player.Connection;

        _optionsTitle.SetText(c, _translations.Title.Translate(player));

        _imguiOption.SetText(c, _translations.IMGUIOptionLabel.Translate(player));
        _imguiDescription.SetText(c, _translations.IMGUIDescription.Translate(player));

        _trackQuestsOption.SetText(c, _translations.TrackQuestsOptionLabel.Translate(player));
        _trackQuestsDescription.SetText(c, _translations.TrackQuestsDescription.Translate(player));

        _internationalizationTitle.SetText(c, _translations.InternationalizationTitle.Translate(player));

        _searchLanguageBox.SetPlaceholder(c, _translations.PlaceholderLanguageName.Translate(player));
        _searchCultureBox.SetPlaceholder(c, _translations.PlaceholderCulture.Translate(player));
        _searchTimeZoneBox.SetPlaceholder(c, _translations.PlaceholderTimeZone.Translate(player));

        string search = _translations.ButtonSearch.Translate(player);
        _searchLanguageButton.SetText(c, search);
        _searchCultureButton.SetText(c, search);
        _searchTimeZoneButton.SetText(c, search);

        _useCultureForCommandInputDescription.SetText(c, _translations.CultureDescription.Translate(player));

        _noLanguagesFound.SetText(c, _translations.NoResultsTimeZone.Translate(player));
        _noCulturesFound.SetText(c, _translations.NoResultsTimeZone.Translate(player));
        _noTimeZonesFound.SetText(c, _translations.NoResultsTimeZone.Translate(player));
    }

    private void OnSearchTimeZones(UnturnedTextBox textbox, Player player, string text)
    {

    }

    private void OnSearchCultures(UnturnedTextBox textbox, Player player, string text)
    {

    }

    private void OnSearchLanguages(UnturnedTextBox textbox, Player player, string text)
    {

    }

    private class LanguageResult : PatternRoot
    {
        [Pattern("Name")]
        public required UnturnedLabel Name { get; init; }

        [Pattern("Code")]
        public required UnturnedLabel Code { get; init; }

        [Pattern("ContributorsTitle")]
        public required UnturnedLabel ContributorsTitle { get; init; }

        [Pattern("Contributors")]
        public required UnturnedLabel Contributors { get; init; }

        [Pattern("Btn_L10N_Apply_{0}", PresetPaths = [ "./Label", "./ButtonState" ])]
        public required LabeledStateButton ApplyButton { get; init; }
    }

    private class CultureResult : PatternRoot
    {
        [Pattern("Name")]
        public required UnturnedLabel Name { get; init; }

        [Pattern("Code")]
        public required UnturnedLabel Code { get; init; }

        [Pattern("Btn_I14N_Apply_{0}", PresetPaths = [ "./Label", "./ButtonState" ])]
        public required LabeledStateButton ApplyButton { get; init; }
    }

    private class TimeZoneResult : PatternRoot
    {
        [Pattern("Name")]
        public required UnturnedLabel Name { get; init; }

        [Pattern("Code")]
        public required UnturnedLabel Code { get; init; }

        [Pattern("Btn_TZ_Apply_{0}", PresetPaths = [ "./Label", "./ButtonState" ])]
        public required LabeledStateButton ApplyButton { get; init; }
    }

    private class OptionsUIData : IUnturnedUIData
    {
        public CSteamID Player { get; }
        public UnturnedUI Owner { get; }
        UnturnedUIElement? IUnturnedUIData.Element => null;

        public bool HasUI { get; set; }

        public OptionsUIData(CSteamID player, UnturnedUI owner)
        {
            Player = player;
            Owner = owner;
        }
    }
}

public class OptionsUITranslations : PropertiesTranslationCollection
{
    protected override string FileName => "UI/Options";

    [TranslationData("Window title.")]
    public readonly Translation Title = new Translation("Options", TranslationOptions.TMProUI);

    [TranslationData("Title of the language, culture, and time zone section.")]
    public readonly Translation InternationalizationTitle = new Translation("Internationalization", TranslationOptions.TMProUI);

    [TranslationData("Placeholder text for the language search text box.")]
    public readonly Translation PlaceholderLanguageName = new Translation("Language Name", TranslationOptions.TMProUI);

    [TranslationData("Placeholder text for the culture search text box.")]
    public readonly Translation PlaceholderCulture = new Translation("International Culture", TranslationOptions.TMProUI);

    [TranslationData("Placeholder text for the time zone search text box.")]
    public readonly Translation PlaceholderTimeZone = new Translation("Time Zone", TranslationOptions.TMProUI);

    [TranslationData("Button text for all the search buttons.")]
    public readonly Translation ButtonSearch = new Translation("Search", TranslationOptions.TMProUI);

    [TranslationData("Button text for all the apply (select) buttons.")]
    public readonly Translation ButtonApply = new Translation("Apply", TranslationOptions.TMProUI);

    [TranslationData("Text displayed when there are no results from a language search.")]
    public readonly Translation NoResultsLanguage = new Translation("No supported languages found.\n" +
                                                                    "If you would like to contribute to a translation pack,\n" +
                                                                    "ping <#aaa>@BlazingFlame</color> in /discord.", TranslationOptions.TMProUI);

    [TranslationData("Text displayed when there are no results from a culture search.")]
    public readonly Translation NoResultsCulture = new Translation("No cultures found, try searching a country name.\n" +
                                                                   "You can also type in any RFC-4646 culture code.", TranslationOptions.TMProUI);

    [TranslationData("Text displayed when there are no results from a time zone search.")]
    public readonly Translation NoResultsTimeZone = new Translation("No time zones found.\n" +
                                                                    "You can also type in any IANA Time Zone ID.", TranslationOptions.TMProUI);

    [TranslationData("Format for a language name and native name.", "Language name in English", "Language name in that language", IsPriorityTranslation = false)]
    public readonly Translation<string, string> LanguageNameFormat = new Translation<string, string>("{0} <#444>(<#eeb>{1}</color>)", TranslationOptions.TMProUI);

    [TranslationData("Format for extra information about a language when it's the selected language.", "Internal language code (en-us, etc)", "Support/implementation percentage")]
    public readonly Translation<string, float> LanguageInfoFormatSelected = new Translation<string, float>("<#0f0>selected</color> <#444>|</color> {0} <#444>|</color> Support: <#fff>{1}", TranslationOptions.TMProUI, arg1Fmt: "P0");

    [TranslationData("Format for extra information about a language when it's not the selected language.", "Internal language code (en-us, etc)", "Support/implementation percentage")]
    public readonly Translation<string, float> LanguageInfoFormatDeselected = new Translation<string, float>("{0} <#444>|</color> Support: <#fff>{1}", TranslationOptions.TMProUI, arg1Fmt: "P0");

    [TranslationData("Title of the contributors section of a language.")]
    public readonly Translation LanguageContributorsTitle = new Translation("Contributors", TranslationOptions.TMProUI);

    [TranslationData("Description of what cultures are used for.")]
    public readonly Translation CultureDescription = new Translation("The selected international culture influences how numbers, dates, percentages, and other locale-specific text is displayed. " +
                                                                     "If 'Use culture for command input' is selected, your command input will be interpreted using your culture. " +
                                                                     "Example: commas instead of periods for decimals, correctly ordered months and days, etc.", TranslationOptions.TMProUI);

    [TranslationData("Label for the IMGUI Mode option.")]
    public readonly Translation IMGUIOptionLabel = new Translation("IMGUI Mode", TranslationOptions.TMProUI);

    [TranslationData("Description of what the IMGUI Mode option does.")]
    public readonly Translation IMGUIDescription = new Translation("Enables chat suppport for the <smallcaps>-Glazier IMGUI</smallcaps> launch option which reverts to the old UI system. " +
                                                                   "Some players prefer launching with this for performance " +
                                                                   "or to allow displaying some characters not supported by uGUI (the default UI option).", TranslationOptions.TMProUI);

    [TranslationData("Label for the Track Daily Missions option.")]
    public readonly Translation TrackQuestsOptionLabel = new Translation("Track Daily Missions", TranslationOptions.TMProUI);

    [TranslationData("Description of what the Track Daily Missions option does.")]
    public readonly Translation TrackQuestsDescription = new Translation("By default, Daily Missions are added to the quest menu when you join. " +
                                                                         "Disabling this option will keep them from being auto-added. " +
                                                                         "Your progress will still be tracked and you can 'track' them in the vanilla quest menu any time.", TranslationOptions.TMProUI);
}