using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.Linq;
using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Events.Models;

namespace Uncreated.Warfare.Translations.Languages;

/// <summary>
/// Handles managing localization and internationalization data.
/// </summary>
[Priority(1)]
public class LanguageService : IAsyncEventListener<PlayerPending>, IHostedService
{
    private readonly ILanguageDataStore _languageDataStore;
    private readonly ILogger<LanguageService> _logger;
    private readonly ICachableLanguageDataStore? _languageDataStoreCache;

    private LanguageInfo? _fallback;
    private CultureInfo? _defaultCulture;

    /// <summary>
    /// The configured default language code from system config.
    /// </summary>
    public string DefaultLanguageCode { get; }

    /// <summary>
    /// The configured default culture code from system config.
    /// </summary>
    public string DefaultCultureCode { get; }

    public LanguageService(ILanguageDataStore languageDataStore, IConfiguration systemConfig, ILogger<LanguageService> logger)
    {
        _languageDataStore = languageDataStore;
        _logger = logger;
        _languageDataStoreCache = languageDataStore as ICachableLanguageDataStore;
        DefaultLanguageCode = systemConfig["default_language"] ?? "en-us";
        DefaultCultureCode = systemConfig["default_culture"] ?? CultureInfo.CurrentCulture.Name;
    }

    async UniTask IHostedService.StartAsync(CancellationToken token)
    {
        if (_languageDataStoreCache != null)
        {
            await _languageDataStoreCache.ReloadCache(token);
        }

        _fallback = await _languageDataStore.GetInfo(DefaultLanguageCode, true, true, token);
        if (_fallback is null)
        {
            _logger.LogWarning("Missing default LanguageInfo for \"{0}\".", DefaultLanguageCode);
        }
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    // get player language data when they join
    async UniTask IAsyncEventListener<PlayerPending>.HandleEventAsync(PlayerPending e, IServiceProvider serviceProvider, CancellationToken token)
    {
        e.AsyncData.LanguagePreferences = await _languageDataStore.GetLanguagePreferences(e.Steam64.m_SteamID, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the default language used for logging and freshly-joined players.
    /// </summary>
    /// <remarks>Always returns a value.</remarks>
    public LanguageInfo GetDefaultLanguage()
    {
        if (_languageDataStoreCache == null)
        {
            return FallbackDefaultLanguage;
        }

        LanguageInfo? info = _languageDataStoreCache.GetInfoCached(DefaultLanguageCode, true);
        return info ?? FallbackDefaultLanguage;
    }

    /// <summary>
    /// Gets the default culture used for logging and freshly-joined players.
    /// </summary>
    /// <remarks>Always returns a value.</remarks>
    public CultureInfo GetDefaultCulture()
    {
        if (_defaultCulture != null)
            return _defaultCulture;

        if (!TryGetCultureInfo(DefaultCultureCode, out CultureInfo culture))
            culture = CultureInfo.CurrentCulture;

        _defaultCulture = culture;
        return culture;
    }

    /// <summary>
    /// Gets the default culture for the given <paramref name="language"/>.
    /// </summary>
    /// <remarks>Always returns a value.</remarks>
    public CultureInfo GetDefaultCulture(LanguageInfo? language)
    {
        if (language == null)
            return GetDefaultCulture();

        if (language.DefaultCultureCode != null)
        {
            if (TryGetCultureInfo(language.DefaultCultureCode, out CultureInfo culture))
                return culture;
        }
        else if (language.SupportedCultures is { Count: > 0 })
        {
            string code = (language.SupportedCultures.FirstOrDefault(x =>
                               x.CultureCode.Length == 5 && char.ToUpperInvariant(x.CultureCode[0]) == x.CultureCode[3] &&
                               char.ToUpperInvariant(x.CultureCode[1]) == x.CultureCode[4]) ??
                           language.SupportedCultures[0]).CultureCode;

            if (TryGetCultureInfo(code, out CultureInfo culture))
                return culture;
        }

        return Data.LocalLocale;
    }

    /// <summary>
    /// Attempts to find an existing culture from the given culture code.
    /// </summary>
    public bool TryGetCultureInfo(string code, out CultureInfo cultureInfo)
    {
        if (code.Equals("invariant", StringComparison.InvariantCultureIgnoreCase))
        {
            cultureInfo = CultureInfo.InvariantCulture;
            return true;
        }

        if (code.Equals("current", StringComparison.InvariantCultureIgnoreCase))
        {
            cultureInfo = CultureInfo.CurrentCulture;
            return true;
        }

        // why is there no better way to do this
        try
        {
            cultureInfo = CultureInfo.GetCultureInfo(code);
            return true;
        }
        catch (CultureNotFoundException)
        {
            cultureInfo = null!;
            return false;
        }
    }

    /// <summary>
    /// Fallback default language so <see cref="GetDefaultLanguage"/> can always return a value.
    /// </summary>
    private LanguageInfo FallbackDefaultLanguage => _fallback ??= new LanguageInfo
    {
        Key = 0,
        Code = DefaultLanguageCode,
        DisplayName = "English",
        Aliases =
        [
            new LanguageAlias { Alias = "English" },
            new LanguageAlias { Alias = "American" },
            new LanguageAlias { Alias = "British" },
            new LanguageAlias { Alias = "Inglés" },
            new LanguageAlias { Alias = "Ingles" },
            new LanguageAlias { Alias = "Inglesa" }
        ],
        Contributors =
        [
            new LanguageContributor { Contributor = 76561198267927009 },
            new LanguageContributor { Contributor = 76561198857595123 }
        ],
        HasTranslationSupport = true,
        DefaultCultureCode = "en-US",
        RequiresIMGUI = false,
        SupportedCultures =
        [
           new LanguageCulture { CultureCode = "en-001" },
           new LanguageCulture { CultureCode = "en-029" },
           new LanguageCulture { CultureCode = "en-150" },
           new LanguageCulture { CultureCode = "en-AE" },
           new LanguageCulture { CultureCode = "en-AG" },
           new LanguageCulture { CultureCode = "en-AI" },
           new LanguageCulture { CultureCode = "en-AS" },
           new LanguageCulture { CultureCode = "en-AT" },
           new LanguageCulture { CultureCode = "en-AU" },
           new LanguageCulture { CultureCode = "en-BB" },
           new LanguageCulture { CultureCode = "en-BE" },
           new LanguageCulture { CultureCode = "en-BI" },
           new LanguageCulture { CultureCode = "en-BM" },
           new LanguageCulture { CultureCode = "en-BS" },
           new LanguageCulture { CultureCode = "en-BW" },
           new LanguageCulture { CultureCode = "en-BZ" },
           new LanguageCulture { CultureCode = "en-CA" },
           new LanguageCulture { CultureCode = "en-CC" },
           new LanguageCulture { CultureCode = "en-CH" },
           new LanguageCulture { CultureCode = "en-CK" },
           new LanguageCulture { CultureCode = "en-CM" },
           new LanguageCulture { CultureCode = "en-CX" },
           new LanguageCulture { CultureCode = "en-CY" },
           new LanguageCulture { CultureCode = "en-DE" },
           new LanguageCulture { CultureCode = "en-DK" },
           new LanguageCulture { CultureCode = "en-DM" },
           new LanguageCulture { CultureCode = "en-ER" },
           new LanguageCulture { CultureCode = "en-FI" },
           new LanguageCulture { CultureCode = "en-FJ" },
           new LanguageCulture { CultureCode = "en-FK" },
           new LanguageCulture { CultureCode = "en-FM" },
           new LanguageCulture { CultureCode = "en-GB" },
           new LanguageCulture { CultureCode = "en-GD" },
           new LanguageCulture { CultureCode = "en-GG" },
           new LanguageCulture { CultureCode = "en-GH" },
           new LanguageCulture { CultureCode = "en-GI" },
           new LanguageCulture { CultureCode = "en-GM" },
           new LanguageCulture { CultureCode = "en-GU" },
           new LanguageCulture { CultureCode = "en-GY" },
           new LanguageCulture { CultureCode = "en-HK" },
           new LanguageCulture { CultureCode = "en-ID" },
           new LanguageCulture { CultureCode = "en-IE" },
           new LanguageCulture { CultureCode = "en-IL" },
           new LanguageCulture { CultureCode = "en-IM" },
           new LanguageCulture { CultureCode = "en-IN" },
           new LanguageCulture { CultureCode = "en-IO" },
           new LanguageCulture { CultureCode = "en-JE" },
           new LanguageCulture { CultureCode = "en-JM" },
           new LanguageCulture { CultureCode = "en-KE" },
           new LanguageCulture { CultureCode = "en-KI" },
           new LanguageCulture { CultureCode = "en-KN" },
           new LanguageCulture { CultureCode = "en-KY" },
           new LanguageCulture { CultureCode = "en-LC" },
           new LanguageCulture { CultureCode = "en-LR" },
           new LanguageCulture { CultureCode = "en-LS" },
           new LanguageCulture { CultureCode = "en-MG" },
           new LanguageCulture { CultureCode = "en-MH" },
           new LanguageCulture { CultureCode = "en-MO" },
           new LanguageCulture { CultureCode = "en-MP" },
           new LanguageCulture { CultureCode = "en-MS" },
           new LanguageCulture { CultureCode = "en-MT" },
           new LanguageCulture { CultureCode = "en-MU" },
           new LanguageCulture { CultureCode = "en-MW" },
           new LanguageCulture { CultureCode = "en-MY" },
           new LanguageCulture { CultureCode = "en-NA" },
           new LanguageCulture { CultureCode = "en-NF" },
           new LanguageCulture { CultureCode = "en-NG" },
           new LanguageCulture { CultureCode = "en-NL" },
           new LanguageCulture { CultureCode = "en-NR" },
           new LanguageCulture { CultureCode = "en-NU" },
           new LanguageCulture { CultureCode = "en-NZ" },
           new LanguageCulture { CultureCode = "en-PG" },
           new LanguageCulture { CultureCode = "en-PH" },
           new LanguageCulture { CultureCode = "en-PK" },
           new LanguageCulture { CultureCode = "en-PN" },
           new LanguageCulture { CultureCode = "en-PR" },
           new LanguageCulture { CultureCode = "en-PW" },
           new LanguageCulture { CultureCode = "en-RW" },
           new LanguageCulture { CultureCode = "en-SB" },
           new LanguageCulture { CultureCode = "en-SC" },
           new LanguageCulture { CultureCode = "en-SD" },
           new LanguageCulture { CultureCode = "en-SE" },
           new LanguageCulture { CultureCode = "en-SG" },
           new LanguageCulture { CultureCode = "en-SH" },
           new LanguageCulture { CultureCode = "en-SI" },
           new LanguageCulture { CultureCode = "en-SL" },
           new LanguageCulture { CultureCode = "en-SS" },
           new LanguageCulture { CultureCode = "en-SX" },
           new LanguageCulture { CultureCode = "en-SZ" },
           new LanguageCulture { CultureCode = "en-TC" },
           new LanguageCulture { CultureCode = "en-TK" },
           new LanguageCulture { CultureCode = "en-TO" },
           new LanguageCulture { CultureCode = "en-TT" },
           new LanguageCulture { CultureCode = "en-TV" },
           new LanguageCulture { CultureCode = "en-TZ" },
           new LanguageCulture { CultureCode = "en-UG" },
           new LanguageCulture { CultureCode = "en-UM" },
           new LanguageCulture { CultureCode = "en-US" },
           new LanguageCulture { CultureCode = "en-VC" },
           new LanguageCulture { CultureCode = "en-VG" },
           new LanguageCulture { CultureCode = "en-VI" },
           new LanguageCulture { CultureCode = "en-VU" },
           new LanguageCulture { CultureCode = "en-WS" },
           new LanguageCulture { CultureCode = "en-ZA" },
           new LanguageCulture { CultureCode = "en-ZM" },
           new LanguageCulture { CultureCode = "en-ZW" }
        ]
    };
}