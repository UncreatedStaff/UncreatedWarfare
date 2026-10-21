using System.Globalization;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Signs;

public abstract class BaseSignInstanceProvider
{
    internal bool IsQueued;
    internal bool IsQueuedAllPlayers;
    internal HashSet<ulong>? Players;
    
    public BarricadeDrop Barricade { get; }

    protected BaseSignInstanceProvider(BarricadeDrop barricade)
    {
        Barricade = barricade;
    }

    /// <summary>
    /// How translations are batched together. Use <see cref="TranslationOptions.PerPlayerTranslation"/> to disable batching completely.
    /// </summary>
    public virtual TranslationOptions GroupOptions => LanguageSets.DefaultTranslationOptions;

    /// <summary>
    /// Text to display if nothing else can be.
    /// </summary>
    public abstract string FallbackText { get; }

    public abstract void Initialize(BarricadeDrop barricade, string extraInfo, IServiceProvider serviceProvider);
    public abstract string Translate(ITranslationValueFormatter formatter, IServiceProvider serviceProvider, LanguageInfo language, CultureInfo culture, WarfarePlayer? player);

    public override int GetHashCode()
    {
        return unchecked ( (int)Barricade.instanceID );
    }
}