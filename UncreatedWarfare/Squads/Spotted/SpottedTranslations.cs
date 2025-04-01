using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Squads.Spotted;
internal sealed class SpottedTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Spotted";

    [TranslationData]
    public readonly Translation SpottedToast = new Translation("<#b9ffaa>SPOTTED", TranslationOptions.TMProUI);

    [TranslationData(Parameters = [ "Team color of the speaker.", "Target" ])]
    public readonly Translation<Color, string> SpottedMessage = new Translation<Color, string>("[T] <color=#{0}><noparse>%SPEAKER%</noparse></color>: Enemy {1} spotted!", TranslationOptions.UnityUINoReplace);

    [TranslationData]
    public readonly Translation SpottedTargetPlayer = new Translation("contact", TranslationOptions.UnityUINoReplace);

    [TranslationData]
    public readonly Translation SpottedTargetFOB = new Translation("FOB", TranslationOptions.UnityUINoReplace);

    [TranslationData]
    public readonly Translation SpottedTargetCache = new Translation("Cache", TranslationOptions.UnityUINoReplace);
}
