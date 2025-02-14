using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players;

public class WarfarePlayerLocale
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EventDispatcher _eventDispatcher;
    public static event Action<WarfarePlayer>? OnLocaleUpdated;

    private readonly bool _init;

    public WarfarePlayer Player { get; }
    public string Language => LanguageInfo.Code;
    public CultureInfo CultureInfo { get; private set; }
    internal bool PreferencesIsDirty { get; set; }
    public NumberFormatInfo ParseFormat { get; set; }
    public TimeZoneInfo TimeZone { get; set; }

    public LanguagePreferences Preferences
    {
        get;
        set
        {
            value.Steam64 = Player.Steam64.m_SteamID;

            LanguageService langService = _serviceProvider.GetRequiredService<LanguageService>();
            ILogger<WarfarePlayerLocale> logger = _serviceProvider.GetRequiredService<ILogger<WarfarePlayerLocale>>();
            bool updated = false;

            langService.GetDefaultLocaleSettings(Player.SteamPlayer.language, value, Player.SteamSummary,
                out LanguageInfo language,
                out CultureInfo culture,
                out TimeZoneInfo timeZone
            );

            if (_init)
            {
                if (LanguageInfo == null || language != LanguageInfo)
                {
                    logger.LogInformation("Updated language for {0}: {1} -> {2}.", Player, LanguageInfo?.DisplayName ?? "null", language.DisplayName);
                    updated = true;
                }

                if (CultureInfo == null || !culture.Name.Equals(CultureInfo.Name))
                {
                    logger.LogInformation("Updated culture for {0}: {1} -> {2}.", Player, CultureInfo?.DisplayName ?? "null", culture.DisplayName);
                    updated = true;
                }

                if (TimeZone == null || !timeZone.Equals(TimeZone))
                {
                    logger.LogInformation("Updated time zone for {0}: {1} -> {2}.", Player, TimeZone?.Id ?? "null", timeZone.Id);
                    updated = true;
                }
            }

            IsDefaultLanguage = language.Equals(langService.GetDefaultLanguage());
            IsDefaultCulture = culture.Name.Equals(langService.GetDefaultCulture().Name, StringComparison.Ordinal);
            IsUtcTime = timeZone.Equals(TimeZoneInfo.Utc);
            LanguageInfo = language;
            TimeZone = timeZone;
            CultureInfo = culture;
            ParseFormat = value.UseCultureForCommandInput
                ? culture.NumberFormat
                : langService.GetDefaultCulture().NumberFormat;
            PreferencesIsDirty |= updated;

            value.LanguageId = language.Key;
            value.Language = null!;
            field = value;

            if (updated)
            {
                value.LastUpdated = DateTimeOffset.UtcNow;
                InvokeOnLocaleUpdated();
            }
        }
    }

    public LanguageInfo LanguageInfo { get; private set; }
    public bool IsDefaultLanguage { get; private set; }
    public bool IsDefaultCulture { get; private set; }
    public bool IsUtcTime { get; private set; }

 #pragma warning disable CS8618
    public WarfarePlayerLocale(WarfarePlayer player, LanguagePreferences preferences, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Player = player;
        Preferences = preferences;
        _init = true;

        _eventDispatcher = serviceProvider.GetRequiredService<EventDispatcher>();
    }
#pragma warning restore CS8618

    internal Task Apply(CancellationToken token = default)
    {
        Preferences = Preferences;
        PreferencesIsDirty = false;
        ILanguageDataStore dataStore = _serviceProvider.GetRequiredService<ILanguageDataStore>();
        return dataStore.UpdateLanguagePreferences(Preferences, token);
    }

    private void InvokeOnLocaleUpdated()
    {
        if (OnLocaleUpdated == null || !Player.IsOnline)
            return;

        // ReSharper disable once ConstantConditionalAccessQualifier
        if (GameThread.IsCurrent)
        {
            try
            {
                OnLocaleUpdated.Invoke(Player);
            }
            catch (Exception ex)
            {
                ILogger<WarfarePlayerLocale> logger = _serviceProvider.GetRequiredService<ILogger<WarfarePlayerLocale>>();
                logger.LogError(ex, "Error updating locale for {0}.", Player);
            }

            PlayerLocaleUpdated args = new PlayerLocaleUpdated { Player = Player };

            _ = _eventDispatcher.DispatchEventAsync(args);
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread(CancellationToken.None);
                if (!Player.IsOnline)
                    return;

                try
                {
                    OnLocaleUpdated?.Invoke(Player);
                }
                catch (Exception ex)
                {
                    ILogger<WarfarePlayerLocale> logger = _serviceProvider.GetRequiredService<ILogger<WarfarePlayerLocale>>();
                    logger.LogError(ex, "Error updating locale for {0}.", Player);
                }

                PlayerLocaleUpdated args = new PlayerLocaleUpdated { Player = Player };

                _ = _eventDispatcher.DispatchEventAsync(args);
            });
        }
    }
}