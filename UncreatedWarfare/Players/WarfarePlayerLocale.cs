using System;
using System.Globalization;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Players;

public class WarfarePlayerLocale
{
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
            LanguageInfo info = value.Language ?? Localization.GetDefaultLanguage();
            bool updated = false;

            IsDefaultLanguage = info.Code.Equals(L.Default, StringComparison.OrdinalIgnoreCase);

            if (!(value.Culture != null && Localization.TryGetCultureInfo(value.Culture, out CultureInfo culture)) &&
                !(info is { DefaultCultureCode: { } defaultCultureName } && Localization.TryGetCultureInfo(defaultCultureName, out culture)))
            {
                culture = Data.LocalLocale;
            }

            if (_init && (CultureInfo == null || !CultureInfo.Name.Equals(culture.Name, StringComparison.Ordinal)))
            {
                L.Log($"Updated culture for {Player}: {CultureInfo?.DisplayName ?? "null"} -> {culture.DisplayName}.");
                updated = true;
            }

            CultureInfo = culture;
            ParseFormat = value.UseCultureForCommandInput ? culture.NumberFormat : Data.LocalLocale.NumberFormat;

            if (_init && LanguageInfo != info)
            {
                L.Log($"Updated language for {Player}: {LanguageInfo?.DisplayName ?? "null"} -> {info.DisplayName}.");
                updated = true;
            }

            LanguageInfo = info;

            IsDefaultCulture = CultureInfo.Name.Equals(Data.LocalLocale.Name, StringComparison.Ordinal);

            _preferences = value;

            if (updated)
                InvokeOnLocaleUpdated(Player);
        }
    }

    public LanguageInfo LanguageInfo { get; private set; }
    public bool IsDefaultLanguage { get; private set; }
    public bool IsDefaultCulture { get; private set; }
    public WarfarePlayerLocale(WarfarePlayer player, LanguagePreferences preferences)
    {
        Player = player;
        Preferences = preferences;
        _init = true;
    }
    internal Task Apply(CancellationToken token = default)
    {
        Preferences = Preferences;
        PreferencesIsDirty = false;
        return Data.LanguageDataStore.UpdateLanguagePreferences(Preferences, token);
    }
    internal Task Update(string? language, CultureInfo? culture, bool holdSave = false, CancellationToken token = default)
    {
        bool save = false;
        if (culture != null && !culture.Name.Equals(CultureInfo.Name, StringComparison.Ordinal))
        {
            L.Log($"Updated culture for {Player}: {CultureInfo.DisplayName} -> {culture.DisplayName}.");
            ActionLog.Add(ActionLogType.ChangeCulture, CultureInfo.Name + " >> " + culture.Name, Player.Steam64.m_SteamID);
            CultureInfo = culture;
            Preferences.Culture = culture.Name;
            IsDefaultCulture = culture.Name.Equals(Data.LocalLocale.Name, StringComparison.Ordinal);
            ParseFormat = Preferences.UseCultureForCommandInput ? culture.NumberFormat : Data.LocalLocale.NumberFormat;
            save = true;
        }

        if (language != null && Data.LanguageDataStore.GetInfoCached(language) is { } languageInfo && !languageInfo.Code.Equals(LanguageInfo.Code, StringComparison.Ordinal))
        {
            L.Log($"Updated language for {Player}: {LanguageInfo.DisplayName} -> {languageInfo.DisplayName}.");
            ActionLog.Add(ActionLogType.ChangeLanguage, LanguageInfo.Code + " >> " + languageInfo.Code, Player.Steam64.m_SteamID);
            Preferences.Language = languageInfo;
            Preferences.LanguageId = languageInfo.Key;
            IsDefaultLanguage = languageInfo.Code.Equals(L.Default, StringComparison.OrdinalIgnoreCase);
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
                Task task = Data.LanguageDataStore.UpdateLanguagePreferences(Preferences, token);
                InvokeOnLocaleUpdated(Player);
                PreferencesIsDirty = false;
                return task;
            }
        }

        return Task.CompletedTask;
    }

    private static void InvokeOnLocaleUpdated(WarfarePlayer player)
    {
        if (OnLocaleUpdated == null)
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
                L.LogError($"Error updating locale for {player}.");
                L.LogError(ex);
            }
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread(CancellationToken.None);
                try
                {
                    OnLocaleUpdated?.Invoke(player);
                }
                catch (Exception ex)
                {
                    L.LogError($"Error updating locale for {player}.");
                    L.LogError(ex);
                }
            });
        }
    }
}