using System.Globalization;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Signs;

public interface ISignInstanceProvider
{
    /// <summary>
    /// If a translation can be shared between all players using the same language, instead of per-player.
    /// </summary>
    bool CanBatchTranslate { get; }
    void Initialize(BarricadeDrop barricade, string extraInfo);
    string Translate(LanguageInfo language, CultureInfo culture, WarfarePlayer? player);
}