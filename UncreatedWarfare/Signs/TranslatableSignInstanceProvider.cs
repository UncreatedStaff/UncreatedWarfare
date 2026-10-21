using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Collections;

namespace Uncreated.Warfare.Signs;

/// <summary>
/// Handles direct translations in signs.
/// </summary>
[SignPrefix("sign_")]
internal class TranslatableSignInstanceProvider : BaseSignInstanceProvider
{
    private readonly ITranslationService _translationService;
    private string _translationKey = null!;
    private SignTranslation? _translation;

    /// <inheritdoc />
    public override string FallbackText => _translationKey;

    /// <inheritdoc />
    public override TranslationOptions GroupOptions => TranslationOptions.PerLanguageTranslation;

    public TranslatableSignInstanceProvider(BarricadeDrop barricade, ITranslationService translationService) : base(barricade)
    {
        _translationService = translationService;
    }

    public override void Initialize(BarricadeDrop barricade, string extraInfo, IServiceProvider serviceProvider)
    {
        _translationKey = extraInfo;

        SignTranslations translations = _translationService.Get<SignTranslations>();

        _translation = translations.Translations.Values
            .OfType<SignTranslation>()
            .FirstOrDefault(x => string.Equals(x.SignId, _translationKey, StringComparison.Ordinal));
    }

    public override string Translate(ITranslationValueFormatter formatter, IServiceProvider serviceProvider, LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        return _translation == null ? _translationKey : _translation.Translate(language);
    }
}