using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Squads;

public class SquadTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Squads";

    public readonly Translation<int> EmptySquadSignTranslation = new Translation<int>("<#7594b4>CREATE SQUAD</color>\n\n<sub><#ddd>SQUAD {0}</color></sub>", TranslationOptions.TMProSign);
    
    public readonly Translation<int> SquadSignHeader = new Translation<int>("SQUAD {0}", TranslationOptions.TMProSign);
    public readonly Translation SquadLimitReached = new Translation("Max number of squads reached.", TranslationOptions.TMProUI);
}