using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Invokes a fob UI update for the flag list.
/// </summary>
public interface IFlagsNeedUIUpdateEvent
{
    LanguageSetEnumerator EnumerateApplicableSets(ITranslationService translationService, TranslationOptions options, ref bool ticketsOnly);
}