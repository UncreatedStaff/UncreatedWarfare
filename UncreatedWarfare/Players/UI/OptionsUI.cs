using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Steam;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
// ReSharper disable UnusedAutoPropertyAccessor.Local

// ReSharper disable ClassNeverInstantiated.Local

namespace Uncreated.Warfare.Players.UI;

[UnturnedUI(BasePath = "Container")]
public class OptionsUI : UnturnedUI
{
    private readonly List<LanguageInfo> _languageSearch;
    private readonly List<CultureInfo> _cultureSearch;
    private readonly List<TimeZoneInfo> _timeZoneSearch;

    private readonly IPlayerService _playerService;
    private ILoopTicker? _loopTicker;
    private readonly LanguageService _languageService;
    private readonly TimeZoneRegionalDatabase _timeZoneDb;
    private readonly ILoopTickerFactory _loopTickerFactory;
    private readonly ICachableLanguageDataStore _languageDataStore;

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
    private readonly CultureResult[] _cultureResults = ElementPatterns.CreateArray<CultureResult>("Localization/Viewport/Content/I14N_{0}", 1, to: 25);
    private readonly TimeZoneResult[] _timeZoneResults = ElementPatterns.CreateArray<TimeZoneResult>("Localization/Viewport/Content/TZ_{0}", 1, to: 25);

    private readonly LabeledUnturnedToggle _imguiOption = new LabeledUnturnedToggle(false, "Options/Viewport/Content/Btn_IMGUI_Toggle", "./ToggleState", "../Label_IMGUI", null);
    private readonly LabeledUnturnedToggle _trackQuestsOption = new LabeledUnturnedToggle(true, "Options/Viewport/Content/Btn_TrackQuests_Toggle", "./ToggleState", "../Label_TrackQuests", null);
    private readonly LabeledUnturnedToggle _useCultureForCommandInput = new LabeledUnturnedToggle(false, "Localization/Viewport/Content/SearchI14N/I14N_CustomInputToggle", "./State", "../CustomInputTitle", null);
    private readonly UnturnedLabel _useCultureForCommandInputDescription = new UnturnedLabel("Localization/Viewport/Content/SearchI14N/CultureDescription");

    private readonly UnturnedLabel _imguiDescription = new UnturnedLabel("Options/Viewport/Content/Description_IMGUI");
    private readonly UnturnedLabel _trackQuestsDescription = new UnturnedLabel("Options/Viewport/Content/Description_TrackQuests");

    private readonly LabeledButton _buttonSave = new LabeledButton("Button_Options_Save", "./Label");
    private readonly LabeledButton _buttonCancel = new LabeledButton("Button_Options_Cancel", "./Label");

    public OptionsUI(ILoggerFactory loggerFactory,
        AssetConfiguration assetConfiguration,
        TranslationInjection<OptionsUITranslations> translations,
        IPlayerService playerService,
        LanguageService languageService,
        ILanguageDataStore languageDataStore,
        TimeZoneRegionalDatabase timeZoneDb,
        ILoopTickerFactory loopTickerFactory)
        : base(loggerFactory, assetConfiguration.GetAssetLink<EffectAsset>("UI:Options"), staticKey: true, reliable: true)
    {
        _playerService = playerService;
        _languageService = languageService;
        _timeZoneDb = timeZoneDb;
        _loopTickerFactory = loopTickerFactory;
        _languageDataStore = languageDataStore as ICachableLanguageDataStore ?? throw new InvalidOperationException("Expected cachable language data store.");
        _translations = translations.Value;
        _allTimeZones = TimeZoneInfo.GetSystemTimeZones();
        _allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

        _languageSearch = new List<LanguageInfo>(_languageResults.Length);
        _cultureSearch = new List<CultureInfo>(_cultureResults.Length);
        _timeZoneSearch = new List<TimeZoneInfo>(_timeZoneResults.Length);

        _createUiData = steam64 => new OptionsUIData(steam64, this);

        _searchLanguageBox.OnTextUpdated += OnSearchLanguages;
        _searchCultureBox.OnTextUpdated += OnSearchCultures;
        _searchTimeZoneBox.OnTextUpdated += OnSearchTimeZones;

        _buttonSave.OnClicked += OnClickedSave;
        _buttonCancel.OnClicked += OnClickedCancel;

        ElementPatterns.SubscribeAll(_languageResults.Select(x => x.ApplyButton), OnLanguageApplyButtonClicked);
        ElementPatterns.SubscribeAll(_cultureResults.Select(x => x.ApplyButton), OnCultureApplyButtonClicked);
        ElementPatterns.SubscribeAll(_timeZoneResults.Select(x => x.ApplyButton), OnTimeZoneApplyButtonClicked);

        DateTime now = DateTime.UtcNow;

        // https://stackoverflow.com/questions/7029353/how-can-i-round-up-the-time-to-the-nearest-x-minutes
        DateTime nextMinute = new DateTime((now.Ticks + TimeSpan.TicksPerMinute - 1) / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute, DateTimeKind.Utc);

        _loopTicker = _loopTickerFactory.CreateTicker(nextMinute - now, false, queueOnGameThread: true, UpdateClocks);
    }

    private void OnClickedCancel(UnturnedButton button, Player player)
    {
        Close(_playerService.GetOnlinePlayer(player));
    }

    private void OnClickedSave(UnturnedButton button, Player uPlayer)
    {
        OptionsUIData data = GetOrCreateData(uPlayer.channel.owner.playerID.steamID);
        if (data.IsSaving)
            return;

        data.IsSaving = true;
        UniTask.Create(async () =>
        {
            WarfarePlayer player = _playerService.GetOnlinePlayer(uPlayer);

            try
            {
                bool saveUpdated = false;
                if (_imguiOption.TryGetValue(player.UnturnedPlayer, out bool value) && player.Save.IMGUI != value)
                {
                    player.Save.IMGUI = value;
                    saveUpdated = true;
                }

                if (_trackQuestsOption.TryGetValue(player.UnturnedPlayer, out value) && player.Save.TrackQuests != value)
                {
                    player.Save.TrackQuests = value;
                    saveUpdated = true;
                }

                if (saveUpdated)
                {
                    player.Save.Save();
                }

                bool localeUpdated = false;
                if (data.SelectedLanguage != null && data.SelectedLanguage != player.Locale.LanguageInfo)
                {
                    player.Locale.Preferences.LanguageId = data.SelectedLanguage.Key;
                    localeUpdated = true;
                }

                if (data.SelectedCulture != null && !data.SelectedCulture.Name.Equals(player.Locale.CultureInfo.Name, StringComparison.Ordinal))
                {
                    player.Locale.Preferences.Culture = data.SelectedCulture.Name;
                    localeUpdated = true;
                }

                if (data.SelectedTimeZone != null && !data.SelectedTimeZone.Equals(player.Locale.TimeZone))
                {
                    player.Locale.Preferences.TimeZone = data.SelectedTimeZone.Id;
                    localeUpdated = true;
                }

                if (_useCultureForCommandInput.TryGetValue(player.UnturnedPlayer, out value) && value != player.Locale.Preferences.UseCultureForCommandInput)
                {
                    player.Locale.Preferences.UseCultureForCommandInput = value;
                    localeUpdated = true;
                }

                if (localeUpdated)
                {
                    await player.Locale.Apply();
                }
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                data.IsSaving = false;
                Close(player);
            }
        });
    }

    protected override void OnDisposing()
    {
        if (_loopTicker == null)
            return;

        _loopTicker.Dispose();
        _loopTicker = null;
    }

    private void UpdateClocks(ILoopTicker ticker, TimeSpan timesincestart, TimeSpan deltatime)
    {
        // correct the < 1minute delay from the first tick
        TimeSpan oneMinute = TimeSpan.FromMinutes(1d);
        if (ticker.PeriodicDelay != oneMinute)
        {
            ticker.Dispose();
            _loopTicker = _loopTickerFactory.CreateTicker(oneMinute, false, queueOnGameThread: true, UpdateClocks);
        }

        DateTime now = DateTime.UtcNow;
        
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            OptionsUIData? data = GetData<OptionsUIData>(player.Steam64);
            if (data is not { HasUI: true, TimeZoneResults.Length: > 0 })
                continue;

            int ct = Math.Min(_timeZoneResults.Length, data.TimeZoneResults.Length);
            for (int i = 0; i < ct; ++i)
            {
                TimeZoneInfo timeZone = data.TimeZoneResults[i];
                bool selected = timeZone.Equals(data.SelectedTimeZone);
                _timeZoneResults[i].Code.SetText(player, (selected ? _translations.TimeZoneInfoFormatSelected : _translations.TimeZoneInfoFormatDeselected)
                    .Translate(timeZone.Id, TimeZoneInfo.ConvertTime(now, timeZone), player));
            }
        }
    }

    private void OnLanguageApplyButtonClicked(UnturnedButton button, Player uPlayer)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(uPlayer);
        OptionsUIData data = GetOrCreateData(player.Steam64);

        int buttonIndex = Array.FindIndex(_languageResults, x => ReferenceEquals(x.ApplyButton.Button, button));
        if (data.LanguageResults == null || buttonIndex < 0 || buttonIndex >= data.LanguageResults.Length)
            return;

        OnLanguageSelected(data, player, buttonIndex);
    }

    private void OnCultureApplyButtonClicked(UnturnedButton button, Player uPlayer)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(uPlayer);
        OptionsUIData data = GetOrCreateData(player.Steam64);

        int buttonIndex = Array.FindIndex(_cultureResults, x => ReferenceEquals(x.ApplyButton.Button, button));
        if (data.CultureResults == null || buttonIndex < 0 || buttonIndex >= data.CultureResults.Length)
            return;

        OnCultureSelected(data, player, buttonIndex);
    }

    private void OnTimeZoneApplyButtonClicked(UnturnedButton button, Player uPlayer)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(uPlayer);
        OptionsUIData data = GetOrCreateData(player.Steam64);

        int buttonIndex = Array.FindIndex(_timeZoneResults, x => ReferenceEquals(x.ApplyButton.Button, button));
        if (data.TimeZoneResults == null || buttonIndex < 0 || buttonIndex >= data.TimeZoneResults.Length)
            return;

        OnTimeZoneSelected(data, player, buttonIndex);
    }

    private OptionsUIData GetOrCreateData(CSteamID player)
    {
        return GetOrAddData(player, _createUiData);
    }

    public void Close(WarfarePlayer player)
    {
        OptionsUIData data = GetOrCreateData(player.Steam64);
        data.Modal.Dispose();
        if (data.IsSaving)
            return;

        data.HasUI = false;

        ClearFromPlayer(player.Connection);
    }

    public void Open(WarfarePlayer player)
    {
        OptionsUIData data = GetOrCreateData(player.Steam64);
        if (data.IsSaving)
            return;

        ITransportConnection c = player.Connection;
        if (!data.HasUI)
        {
            SendToPlayer(c);
            data.HasUI = true;
        }

        ModalHandle.TryGetModalHandle(player, ref data.Modal);

        if (!player.Locale.LanguageInfo.IsDefault)
            UpdateText(player);

        _imguiOption.Set(player.UnturnedPlayer, player.Save.IMGUI);
        _trackQuestsOption.Set(player.UnturnedPlayer, player.Save.TrackQuests);
        _useCultureForCommandInput.Set(player.UnturnedPlayer, player.Locale.Preferences.UseCultureForCommandInput);

        WarfarePlayerLocale locale = player.Locale;
        
        _searchLanguageBox.SetText(c, data.LanguageSearch = locale.LanguageInfo.Code);
        _searchCultureBox.SetText(c, data.CultureSearch = string.IsNullOrEmpty(locale.CultureInfo.Name) ? "invariant" : locale.CultureInfo.Name);
        _searchTimeZoneBox.SetText(c, data.TimeZoneSearch = locale.TimeZone.Id);

        data.SelectedLanguage = locale.LanguageInfo;
        data.SelectedCulture = locale.CultureInfo;
        data.SelectedTimeZone = locale.TimeZone;

        data.LanguageResults = null;
        data.CultureResults = null;
        data.TimeZoneResults = null;

        SendLanguageList(player);
        SendCultureList(player);
        SendTimeZoneList(player);
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

        _buttonSave.SetText(c, _translations.ButtonSave.Translate(player));
        _buttonCancel.SetText(c, _translations.ButtonCancel.Translate(player));

        string applyButton = _translations.ButtonApply.Translate(player);
        string contributorsTitle = _translations.LanguageContributorsTitle.Translate(player);

        for (int i = 0; i < _languageResults.Length; ++i)
        {
            LanguageResult ui = _languageResults[i];
            ui.ApplyButton.SetText(c, applyButton);
            ui.ContributorsTitle.SetText(c, contributorsTitle);
        }
        for (int i = 0; i < _cultureResults.Length; ++i)
        {
            _cultureResults[i].ApplyButton.SetText(c, applyButton);
        }
        for (int i = 0; i < _timeZoneResults.Length; ++i)
        {
            _timeZoneResults[i].ApplyButton.SetText(c, applyButton);
        }
    }

    private void OnLanguageSelected(OptionsUIData data, WarfarePlayer player, int buttonIndex)
    {
        LanguageInfo language = data.LanguageResults![buttonIndex];

        data.SelectedLanguage = language;
        data.LanguageSearch = language.Code;
        data.LanguageResults = null;
        SendLanguageList(player);

        data.CultureResults = null;
        data.CultureSearch = null;
        _searchCultureBox.SetText(player, string.Empty);
        SendCultureList(player);
    }

    private void OnCultureSelected(OptionsUIData data, WarfarePlayer player, int buttonIndex)
    {
        CultureInfo culture = data.CultureResults![buttonIndex];

        data.SelectedCulture = culture;
        data.CultureSearch = culture.Name;
        data.CultureResults = null;
        SendCultureList(player);
        
        data.TimeZoneResults = null;
        data.TimeZoneSearch = null;
        _searchTimeZoneBox.SetText(player, string.Empty);
        SendTimeZoneList(player);
    }

    private void OnTimeZoneSelected(OptionsUIData data, WarfarePlayer player, int buttonIndex)
    {
        TimeZoneInfo timeZone = data.TimeZoneResults![buttonIndex];

        data.SelectedTimeZone = timeZone;
        data.TimeZoneSearch = timeZone.Id;
        data.TimeZoneResults = null;
        SendTimeZoneList(player);
    }

    private void OnSearchLanguages(UnturnedTextBox textbox, Player player, string text)
    {
        OptionsUIData data = GetOrCreateData(player.channel.owner.playerID.steamID);

        data.LanguageSearch = text;
        data.LanguageResults = null;

        SendLanguageList(_playerService.GetOnlinePlayer(player));
    }

    private void OnSearchCultures(UnturnedTextBox textbox, Player player, string text)
    {
        OptionsUIData data = GetOrCreateData(player.channel.owner.playerID.steamID);

        data.CultureSearch = text;
        data.CultureResults = null;

        SendCultureList(_playerService.GetOnlinePlayer(player));
    }

    private void OnSearchTimeZones(UnturnedTextBox textbox, Player player, string text)
    {
        OptionsUIData data = GetOrCreateData(player.channel.owner.playerID.steamID);

        data.TimeZoneSearch = text;
        data.TimeZoneResults = null;

        SendTimeZoneList(_playerService.GetOnlinePlayer(player));
    }

    private void SendLanguageList(WarfarePlayer player)
    {
        OptionsUIData data = GetOrCreateData(player.Steam64);

        string? search = data.LanguageSearch;
        data.LanguageResults ??= SearchLanguages(search, player);

        ITransportConnection c = player.Connection;

        int ct = Math.Min(data.LanguageResults.Length, _languageResults.Length);

        int i = 0;
        if (ct == 0)
        {
            _noLanguagesFound.SetVisibility(c, true);
            LanguageResult ui = _languageResults[0];
            ui.Name.Hide(c);
            ui.Code.Hide(c);
            ui.Contributors.Hide(c);
            ui.ContributorsTitle.Hide(c);
            ui.ApplyButton.Hide(c);
            i = 1;
        }

        for (; i < ct; ++i)
        {
            LanguageResult ui = _languageResults[i];

            if (i == 0)
            {
                _noLanguagesFound.SetVisibility(c, false);
                ui.Name.Show(c);
                ui.Code.Show(c);
                ui.Contributors.Show(c);
                ui.ContributorsTitle.Show(c);
                ui.ApplyButton.Show(c);
            }
            else
                ui.Show(c);

            LanguageInfo language = data.LanguageResults[i];

            float support = language.Support;

            bool selected = language == data.SelectedLanguage;

            string name = language.NativeName != null && !language.DisplayName.Equals(language.NativeName, StringComparison.Ordinal)
                ? _translations.LanguageNameFormat.Translate(language.DisplayName, language.NativeName, player)
                : language.DisplayName;

            ui.Name.SetText(c, name);
            ui.Code.SetText(c, (selected ? _translations.LanguageInfoFormatSelected : _translations.LanguageInfoFormatDeselected).Translate(language.Code, support, player));
            ui.ApplyButton.SetState(c, !selected);

            if (language.IsDefault)
            {
                ui.Contributors.SetText(c, "Uncreated Warfare Developers");
                ui.ContributorsTitle.Show(c);
            }
            else if (language.Contributors.Count > 0)
            {
                ui.ContributorsTitle.Show(c);
                string contributors = string.Join(Environment.NewLine, language.Contributors.Select(x => x.ContributorData.PlayerName));
                ui.Contributors.SetText(c, contributors);
            }
            else
            {
                ui.ContributorsTitle.Hide(c);
                ui.Contributors.SetText(c, string.Empty);
            }
        }

        for (; i < _languageResults.Length; ++i)
        {
            _languageResults[i].Hide(c);
        }
    }

    private void SendCultureList(WarfarePlayer player)
    {
        OptionsUIData data = GetOrCreateData(player.Steam64);

        string? search = data.CultureSearch;
        data.CultureResults ??= SearchCultures(search, data, player);

        ITransportConnection c = player.Connection;

        int ct = Math.Min(data.CultureResults.Length, _cultureResults.Length);

        int i = 0;
        if (ct == 0)
        {
            _noCulturesFound.SetVisibility(c, true);
            CultureResult ui = _cultureResults[0];
            ui.Name.Hide(c);
            ui.Code.Hide(c);
            ui.ApplyButton.Hide(c);
            i = 1;
        }

        for (; i < ct; ++i)
        {
            CultureResult ui = _cultureResults[i];

            if (i == 0)
            {
                _noCulturesFound.SetVisibility(c, false);
                ui.Name.Show(c);
                ui.Code.Show(c);
                ui.ApplyButton.Show(c);
            }
            else
                ui.Show(c);

            CultureInfo culture = data.CultureResults[i];

            bool selected = culture.Name.Equals(data.SelectedCulture?.Name, StringComparison.Ordinal);

            ui.Name.SetText(c, culture.DisplayName);
            ui.Code.SetText(c, (selected ? _translations.CultureInfoFormatSelected : _translations.CultureInfoFormatDeselected).Translate(culture.Name, player));
            ui.ApplyButton.SetState(c, !selected);
        }

        for (; i < _cultureResults.Length; ++i)
        {
            _cultureResults[i].Hide(c);
        }
    }

    private void SendTimeZoneList(WarfarePlayer player)
    {
        OptionsUIData data = GetOrCreateData(player.Steam64);

        string? search = data.TimeZoneSearch;
        data.TimeZoneResults ??= SearchTimeZones(search, data, player);

        ITransportConnection c = player.Connection;

        int ct = Math.Min(data.TimeZoneResults.Length, _timeZoneResults.Length);

        int i = 0;
        if (ct == 0)
        {
            _noTimeZonesFound.SetVisibility(c, true);
            TimeZoneResult ui = _timeZoneResults[0];
            ui.Name.Hide(c);
            ui.Code.Hide(c);
            ui.ApplyButton.Hide(c);
            i = 1;
        }

        for (; i < ct; ++i)
        {
            TimeZoneResult ui = _timeZoneResults[i];

            if (i == 0)
            {
                _noTimeZonesFound.SetVisibility(c, false);
                ui.Name.Show(c);
                ui.Code.Show(c);
                ui.ApplyButton.Show(c);
            }
            else
                ui.Show(c);

            TimeZoneInfo timeZone = data.TimeZoneResults[i];

            bool selected = timeZone.Equals(data.SelectedTimeZone);

            ui.Name.SetText(c, timeZone.DisplayName);
            ui.Code.SetText(c, (selected ? _translations.TimeZoneInfoFormatSelected : _translations.TimeZoneInfoFormatDeselected)
                .Translate(timeZone.Id, TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone), player));
            ui.ApplyButton.SetState(c, !selected);
        }

        for (; i < _timeZoneResults.Length; ++i)
        {
            _timeZoneResults[i].Hide(c);
        }
    }

    private LanguageInfo[] SearchLanguages(string? search, WarfarePlayer player)
    {
        _languageSearch.Clear();
        if (string.IsNullOrWhiteSpace(search))
        {
            _languageSearch.Add(_languageService.GetDefaultLanguage());
            string? steamLanguage = player.SteamPlayer.language;
            if (steamLanguage != null)
            {
                LanguageInfo? match = _languageDataStore.Languages.FirstOrDefault(x => string.Equals(x.SteamLanguageName, steamLanguage, StringComparison.OrdinalIgnoreCase));
                if (match != null && !match.IsDefault)
                    _languageSearch.Add(match);
            }
            using IEnumerator<LanguageInfo> langs = _languageDataStore.Languages.GetEnumerator();
            for (int uiCount = _languageSearch.Count; uiCount < _languageResults.Length && langs.MoveNext();)
            {
                if (_languageSearch.Contains(langs.Current))
                    continue;

                ++uiCount;
                _languageSearch.Add(langs.Current);
            }

            return _languageSearch.ToArray();
        }

        search = search.Trim();

        LanguageInfo? exactMatch = _languageDataStore.Languages.FirstOrDefault(x => x.Code.Equals(search, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
        {
            return [ exactMatch ];
        }

        const LevenshteinOptions options = LevenshteinOptions.AutoComplete | LevenshteinOptions.IgnoreCase | LevenshteinOptions.IgnorePunctuation | LevenshteinOptions.IgnoreWhitespace;

        // sort by fuzzy match to display name or native name
        foreach (LanguageInfo fuzzyMatch in _languageDataStore.Languages.OrderBy(x => Math.Min(
            StringUtility.LevenshteinDistance(x.DisplayName, search, player.Locale.CultureInfo, options),
            string.IsNullOrWhiteSpace(x.NativeName) ? int.MaxValue : StringUtility.LevenshteinDistance(x.NativeName, search, player.Locale.CultureInfo, options)
        )))
        {
            if (_languageSearch.Contains(fuzzyMatch))
                continue;

            _languageSearch.Add(fuzzyMatch);
            if (_languageSearch.Count >= _languageResults.Length)
                break;
        }

        return _languageSearch.ToArray();
    }

    private CultureInfo[] SearchCultures(string? search, OptionsUIData data, WarfarePlayer player)
    {
        _cultureSearch.Clear();
        if (string.IsNullOrWhiteSpace(search))
        {
            if (data.SelectedLanguage != null)
            {
                // current culture by steam country code
                string code = data.SelectedLanguage.Code;
                if (player.SteamSummary.CountryCode != null && code.Length > 2 && code[2] == '-')
                {
                    string bestCulture = code.AsSpan(0, 3).Concat(player.SteamSummary.CountryCode);
                    if (_languageService.TryGetCultureInfo(bestCulture, out CultureInfo? bestCultureMatch) && !_cultureSearch.Exists(x => x.Name.Equals(bestCultureMatch.Name, StringComparison.OrdinalIgnoreCase)))
                        _cultureSearch.Add(bestCultureMatch);
                }

                // supported cultures for selected language
                foreach (LanguageCulture supportedCulture in data.SelectedLanguage.SupportedCultures)
                {
                    if (_languageService.TryGetCultureInfo(supportedCulture.CultureCode, out CultureInfo? bestCultureMatch)
                        && !_cultureSearch.Exists(x => x.Name.Equals(bestCultureMatch.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        _cultureSearch.Add(bestCultureMatch);
                        if (_cultureSearch.Count >= _cultureResults.Length)
                            break;
                    }
                }
            }

            // default culture
            CultureInfo defaultCulture = _languageService.GetDefaultCulture();
            if (_cultureSearch.Count < _cultureResults.Length && !_cultureSearch.Exists(x => x.Name.Equals(defaultCulture.Name, StringComparison.OrdinalIgnoreCase)))
                _cultureSearch.Add(defaultCulture);

            // fill with remaining cultures
            if (_cultureSearch.Count < _cultureResults.Length)
            {
                foreach (CultureInfo culture in _allCultures)
                {
                    if (_cultureSearch.Exists(x => x.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    _cultureSearch.Add(culture);
                    if (_cultureSearch.Count >= _cultureResults.Length)
                        break;
                }
            }

            return _cultureSearch.ToArray();
        }

        search = search.Trim();

        CultureInfo? exactMatch = _allCultures.FirstOrDefault(x => x.Name.Equals(search, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
        {
            return [ exactMatch ];
        }

        const LevenshteinOptions options = LevenshteinOptions.AutoComplete | LevenshteinOptions.IgnoreCase | LevenshteinOptions.IgnorePunctuation | LevenshteinOptions.IgnoreWhitespace;

        // sort by fuzzy match to native name, english name, and ID name (en-US)
        foreach (CultureInfo fuzzyMatch in _allCultures.OrderBy(x => Math.Min(
            StringUtility.LevenshteinDistance(x.NativeName, search, player.Locale.CultureInfo, options),
            Math.Min(
                StringUtility.LevenshteinDistance(x.EnglishName, search, player.Locale.CultureInfo, options),
                StringUtility.LevenshteinDistance(x.Name, search, player.Locale.CultureInfo, options)
            )
        )))
        {
            if (_cultureSearch.Contains(fuzzyMatch))
                continue;

            _cultureSearch.Add(fuzzyMatch);
            if (_cultureSearch.Count >= _cultureResults.Length)
                break;
        }

        return _cultureSearch.ToArray();
    }

    private TimeZoneInfo[] SearchTimeZones(string? search, OptionsUIData data, WarfarePlayer player)
    {
        _timeZoneSearch.Clear();
        if (string.IsNullOrWhiteSpace(search))
        {
            IReadOnlyList<TimeZoneInfo> timeZones;
            // selected culture
            if (data.SelectedCulture != null)
            {
                string code = data.SelectedCulture.Name;
                int dashIndex = code.IndexOf('-');
                if (dashIndex > 0 && dashIndex < code.Length - 1)
                {
                    string region = code.Substring(dashIndex + 1);
                    if (_timeZoneDb.RegionTimeZones.TryGetValue(region, out timeZones))
                    {
                        foreach (TimeZoneInfo tz in timeZones)
                        {
                            if (!_timeZoneSearch.Contains(tz))
                                _timeZoneSearch.Add(tz);
                        }
                    }
                }
            }

            // steam country code
            if (player.SteamSummary.CountryCode != null && _timeZoneDb.RegionTimeZones.TryGetValue(player.SteamSummary.CountryCode, out timeZones))
            {
                foreach (TimeZoneInfo tz in timeZones)
                {
                    if (!_timeZoneSearch.Contains(tz))
                        _timeZoneSearch.Add(tz);
                }
            }

            // UTC
            if (_timeZoneSearch.Count < _timeZoneResults.Length && !_timeZoneSearch.Contains(TimeZoneInfo.Utc))
                _timeZoneSearch.Add(TimeZoneInfo.Utc);

            // fill with remaining time zones
            if (_timeZoneSearch.Count < _timeZoneResults.Length)
            {
                foreach (TimeZoneInfo timeZone in _allTimeZones)
                {
                    if (_timeZoneSearch.Contains(timeZone))
                        continue;

                    _timeZoneSearch.Add(timeZone);
                    if (_timeZoneSearch.Count >= _timeZoneResults.Length)
                        break;
                }
            }

            return _timeZoneSearch.ToArray();
        }

        search = search.Trim();

        if (search.Length > 3 && search.StartsWith("UTC", StringComparison.OrdinalIgnoreCase))
        {
            int plusOrMinusIndex = search.IndexOf('+', 3);
            bool isMinus = false;
            if (plusOrMinusIndex == -1)
            {
                isMinus = true;
                plusOrMinusIndex = search.IndexOf('-', 3);
            }
            if (plusOrMinusIndex != -1 && plusOrMinusIndex < search.Length - 1)
            {
                TimeSpan ts = default;
                if (int.TryParse(search.AsSpan(plusOrMinusIndex + 1), NumberStyles.Any, player.Locale.ParseFormat, out int hours))
                {
                    ts = TimeSpan.FromHours(hours);
                }
                else if (TimeSpan.TryParse(search.AsSpan(plusOrMinusIndex + 1), player.Locale.ParseFormat, out TimeSpan timeSpan))
                {
                    ts = timeSpan is { Days: > 0, Hours: 0, Minutes: 0, Seconds: 0, Milliseconds: 0 }
                        ? new TimeSpan(timeSpan.Days, 0, 0)
                        : timeSpan;
                }

                if (ts.Ticks != 0)
                {
                    if (isMinus)
                        ts = -ts;
                    foreach (TimeZoneInfo tz in _allTimeZones.OrderBy(x => x.DisplayName is [ '+', .. ] or [ '-', .. ]))
                    {
                        if (tz.BaseUtcOffset == ts && !_timeZoneSearch.Contains(tz))
                            _timeZoneSearch.Add(tz);
                    }
                }
            }

            return _timeZoneSearch.ToArray();
        }

        if (search.Equals("UTC", StringComparison.InvariantCultureIgnoreCase))
        {
            _timeZoneSearch.Add(TimeZoneInfo.Utc);

            foreach (TimeZoneInfo tz in _allTimeZones)
            {
                if (tz.BaseUtcOffset.Ticks == 0 && !_timeZoneSearch.Contains(tz))
                    _timeZoneSearch.Add(tz);
            }
        }

        TimeZoneInfo? exactMatch;
        try
        {
            exactMatch = TimeZoneInfo.FindSystemTimeZoneById(search);
        }
        catch (TimeZoneNotFoundException)
        {
            exactMatch = null;
        }

        if (exactMatch != null)
        {
            return [ exactMatch ];
        }

        const LevenshteinOptions options = LevenshteinOptions.AutoComplete | LevenshteinOptions.IgnoreCase | LevenshteinOptions.IgnorePunctuation | LevenshteinOptions.IgnoreWhitespace;

        // sort by fuzzy match to native name, english name, and ID name (en-US)
        foreach (TimeZoneInfo fuzzyMatch in _allTimeZones.OrderBy(x => Math.Min(
            StringUtility.LevenshteinDistance(x.Id, search, player.Locale.CultureInfo, options),
            Math.Min(
                string.IsNullOrWhiteSpace(x.DisplayName) ? int.MaxValue : StringUtility.LevenshteinDistance(x.DisplayName, search, player.Locale.CultureInfo, options),
                Math.Min(
                    string.IsNullOrWhiteSpace(x.DaylightName) ? int.MaxValue : StringUtility.LevenshteinDistance(x.DaylightName, search, player.Locale.CultureInfo, options),
                    string.IsNullOrWhiteSpace(x.StandardName) ? int.MaxValue : StringUtility.LevenshteinDistance(x.StandardName, search, player.Locale.CultureInfo, options)
                )
            )
        )))
        {
            if (_timeZoneSearch.Contains(fuzzyMatch))
                continue;

            _timeZoneSearch.Add(fuzzyMatch);
            if (_timeZoneSearch.Count >= _timeZoneResults.Length)
                break;
        }

        return _timeZoneSearch.ToArray();
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
        internal ModalHandle Modal;
        public CSteamID Player { get; }
        public UnturnedUI Owner { get; }
        UnturnedUIElement? IUnturnedUIData.Element => null;

        public bool HasUI { get; set; }
        public bool IsSaving { get; set; }

        public string? LanguageSearch { get; set; }
        public string? CultureSearch { get; set; }
        public string? TimeZoneSearch { get; set; }

        public LanguageInfo? SelectedLanguage { get; set; }
        public CultureInfo? SelectedCulture { get; set; }
        public TimeZoneInfo? SelectedTimeZone { get; set; }

        public LanguageInfo[]? LanguageResults { get; set; }
        public CultureInfo[]? CultureResults { get; set; }
        public TimeZoneInfo[]? TimeZoneResults { get; set; }


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

    [TranslationData("Button text for the save button.")]
    public readonly Translation ButtonSave = new Translation("Save", TranslationOptions.TMProUI);

    [TranslationData("Button text for the cancel button.")]
    public readonly Translation ButtonCancel = new Translation("Cancel", TranslationOptions.TMProUI);

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

    [TranslationData("Format for extra information about a language when it's the selected language.", "Internal language code (es-es, etc)", "Support/implementation percentage")]
    public readonly Translation<string, float> LanguageInfoFormatSelected = new Translation<string, float>("<#0f0>selected</color> <#444>|</color> {0} <#444>|</color> Support: <#fff>{1}", TranslationOptions.TMProUI, arg1Fmt: "P0");

    [TranslationData("Format for extra information about a language when it's not the selected language.", "Internal language code (es-es, etc)", "Support/implementation percentage")]
    public readonly Translation<string, float> LanguageInfoFormatDeselected = new Translation<string, float>("{0} <#444>|</color> Support: <#fff>{1}", TranslationOptions.TMProUI, arg1Fmt: "P0");

    [TranslationData("Format for extra information about a culture when it's the selected culture.", "System culture code (en-US, etc)")]
    public readonly Translation<string> CultureInfoFormatSelected = new Translation<string>("<#0f0>selected</color> <#444>|</color> {0}", TranslationOptions.TMProUI);

    [TranslationData("Format for extra information about a culture when it's not the selected culture.", "System culture code (en-US, etc)", IsPriorityTranslation = false)]
    public readonly Translation<string> CultureInfoFormatDeselected = new Translation<string>("{0}", TranslationOptions.TMProUI);

    [TranslationData("Format for extra information about a time zone when it's the selected time zone.", "System time zone code (America/New_York, etc)")]
    public readonly Translation<string, DateTime> TimeZoneInfoFormatSelected = new Translation<string, DateTime>("<#0f0>selected</color> <#444>|</color> {0} <#444>|</color> <#fff>{1}", TranslationOptions.TMProUI, arg1Fmt: "t");

    [TranslationData("Format for extra information about a time zone when it's not the selected time zone.", "System time zone code (America/New_York, etc)", IsPriorityTranslation = false)]
    public readonly Translation<string, DateTime> TimeZoneInfoFormatDeselected = new Translation<string, DateTime>("{0} <#444>|</color> <#fff>{1}", TranslationOptions.TMProUI, arg1Fmt: "t");

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