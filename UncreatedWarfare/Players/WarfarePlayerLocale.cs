using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players;

public class WarfarePlayerLocale
{
    private readonly IServiceProvider _serviceProvider;
    public static event Action<WarfarePlayer>? OnLocaleUpdated;

    private LanguagePreferences _preferences;
    private readonly bool _init;

    public WarfarePlayer Player { get; }
    public string Language => LanguageInfo.Code;
    public CultureInfo CultureInfo { get; private set; }
    internal bool PreferencesIsDirty { get; set; }
    public NumberFormatInfo ParseFormat { get; set; }
    public LanguagePreferences Preferences
    {
        get => _preferences;
        set
        {
            LanguageService langService = _serviceProvider.GetRequiredService<LanguageService>();
            LanguageInfo info = value.Language ?? langService.GetDefaultLanguage();
            ILogger<WarfarePlayerLocale> logger = _serviceProvider.GetRequiredService<ILogger<WarfarePlayerLocale>>();
            bool updated = false;

            IsDefaultLanguage = info.Equals(langService.GetDefaultLanguage());

            if (!(value.Culture != null && langService.TryGetCultureInfo(value.Culture, out CultureInfo culture)) &&
                !(info is { DefaultCultureCode: { } defaultCultureName } && langService.TryGetCultureInfo(defaultCultureName, out culture)))
            {
                culture = langService.GetDefaultCulture();
            }

            if (_init && (CultureInfo == null || !CultureInfo.Name.Equals(culture.Name, StringComparison.Ordinal)))
            {
                logger.LogInformation("Updated culture for {0}: {1} -> {2}.", Player, CultureInfo?.DisplayName ?? "null", culture.DisplayName);
                updated = true;
            }

            CultureInfo = culture;
            ParseFormat = value.UseCultureForCommandInput ? culture.NumberFormat : langService.GetDefaultCulture().NumberFormat;

            if (_init && LanguageInfo != info)
            {
                logger.LogInformation("Updated language for {0}: {1} -> {2}.", Player, LanguageInfo?.DisplayName ?? "null", info.DisplayName);
                updated = true;
            }

            LanguageInfo = info;

            IsDefaultCulture = CultureInfo.Name.Equals(langService.GetDefaultCulture().Name, StringComparison.Ordinal);

            _preferences = value;

            if (updated)
                InvokeOnLocaleUpdated(Player);
        }
    }

    public LanguageInfo LanguageInfo { get; private set; }
    public bool IsDefaultLanguage { get; private set; }
    public bool IsDefaultCulture { get; private set; }
    public WarfarePlayerLocale(WarfarePlayer player, LanguagePreferences preferences, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Player = player;
        Preferences = preferences;
        _init = true;
    }
    internal Task Apply(CancellationToken token = default)
    {
        Preferences = Preferences;
        PreferencesIsDirty = false;
        ILanguageDataStore dataStore = _serviceProvider.GetRequiredService<ILanguageDataStore>();
        return dataStore.UpdateLanguagePreferences(Preferences, token);
    }
    internal Task Update(string? language, CultureInfo? culture, bool holdSave = false, CancellationToken token = default)
    {
        ILogger<WarfarePlayerLocale> logger = _serviceProvider.GetRequiredService<ILogger<WarfarePlayerLocale>>();
        LanguageService languageService = _serviceProvider.GetRequiredService<LanguageService>();
        bool save = false;
        if (culture != null && !culture.Name.Equals(CultureInfo.Name, StringComparison.Ordinal))
        {
            logger.LogInformation("Updated culture for {0}: {1} -> {2}.", Player, CultureInfo.DisplayName, culture.DisplayName);
            ActionLog.Add(ActionLogType.ChangeCulture, CultureInfo.Name + " >> " + culture.Name, Player.Steam64.m_SteamID);
            CultureInfo = culture;
            Preferences.Culture = culture.Name;
            IsDefaultCulture = culture.Name.Equals(languageService.GetDefaultCulture().Name, StringComparison.Ordinal);
            ParseFormat = Preferences.UseCultureForCommandInput ? culture.NumberFormat : languageService.GetDefaultCulture().NumberFormat;
            save = true;
        }

        ICachableLanguageDataStore dataStore = _serviceProvider.GetRequiredService<ICachableLanguageDataStore>();
        if (language != null && dataStore.GetInfoCached(language) is { } languageInfo && !languageInfo.Code.Equals(LanguageInfo.Code, StringComparison.Ordinal))
        {
            logger.LogInformation("Updated language for {0}: {1} -> {2}.", Player, LanguageInfo.DisplayName, languageInfo.DisplayName);
            ActionLog.Add(ActionLogType.ChangeLanguage, LanguageInfo.Code + " >> " + languageInfo.Code, Player.Steam64.m_SteamID);
            Preferences.Language = languageInfo;
            Preferences.LanguageId = languageInfo.Key;
            IsDefaultLanguage = languageInfo.Equals(languageService.GetDefaultLanguage());
            LanguageInfo = languageInfo;
            save = true;
        }

        if (save)
        {
            Preferences.LastUpdated = DateTime.UtcNow;
            if (holdSave)
            {
                InvokeOnLocaleUpdated(Player);
                PreferencesIsDirty = true;
            }
            else
            {
                Task task = dataStore.UpdateLanguagePreferences(Preferences, token);
                InvokeOnLocaleUpdated(Player);
                PreferencesIsDirty = false;
                return task;
            }
        }

        return Task.CompletedTask;
    }

    private void InvokeOnLocaleUpdated(WarfarePlayer player)
    {
        if (OnLocaleUpdated == null || !player.IsOnline)
            return;

        // ReSharper disable once ConstantConditionalAccessQualifier
        if (GameThread.IsCurrent)
        {
            try
            {
                OnLocaleUpdated.Invoke(player);
            }
            catch (Exception ex)
            {
                ILogger<WarfarePlayerLocale> logger = _serviceProvider.GetRequiredService<ILogger<WarfarePlayerLocale>>();
                logger.LogError(ex, "Error updating locale for {0}.", player);
            }
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread(CancellationToken.None);
                if (!player.IsOnline)
                    return;

                try
                {
                    OnLocaleUpdated?.Invoke(player);
                }
                catch (Exception ex)
                {
                    ILogger<WarfarePlayerLocale> logger = _serviceProvider.GetRequiredService<ILogger<WarfarePlayerLocale>>();
                    logger.LogError(ex, "Error updating locale for {0}.", player);
                }
            });
        }
    }
}