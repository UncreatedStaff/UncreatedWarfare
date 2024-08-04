using System.Globalization;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Signs;

/// <summary>
/// Handles direct translations in signs.
/// </summary>
internal class TranslatableSignInstanceProvider : ISignInstanceProvider
{
    private string _translationKey = null!;
    private Translation? _translation;

    /// <inheritdoc />
    bool ISignInstanceProvider.CanBatchTranslate => true;

    void ISignInstanceProvider.Initialize(BarricadeDrop barricade, string extraInfo)
    {
        _translationKey = extraInfo;
        _translation = Translation.FromSignId(_translationKey);
    }

    public string Translate(LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        return _translation == null ? _translationKey : _translation.Translate(language, culture);
    }
}
