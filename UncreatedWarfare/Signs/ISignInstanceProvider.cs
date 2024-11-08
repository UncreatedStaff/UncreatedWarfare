using System;
using System.Globalization;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Signs;

public interface ISignInstanceProvider
{
    /// <summary>
    /// If a translation can be shared between all players using the same language, instead of per-player.
    /// </summary>
    bool CanBatchTranslate { get; }

    /// <summary>
    /// Text to display if nothing else can be.
    /// </summary>
    string FallbackText { get; }

    void Initialize(BarricadeDrop barricade, string extraInfo, IServiceProvider serviceProvider);
    string Translate(ITranslationValueFormatter formatter, IServiceProvider serviceProvider, LanguageInfo language, CultureInfo culture, WarfarePlayer? player);
}