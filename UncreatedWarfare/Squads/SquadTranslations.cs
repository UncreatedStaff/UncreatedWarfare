using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Squads;

public class SquadTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Squads";

    public readonly Translation<int> EmptySquadSignTranslation = new Translation<int>("<#7594b4>CREATE SQUAD</color>\n\n<sub><#ddd>SQUAD {0}</color></sub>", TranslationOptions.TMProSign);

    public readonly Translation<Squad> SquadPromoted = new Translation<Squad>("<#99ccff>You were promoted to leader of {0}.", arg0Fmt: Squad.FormatColorName);
    public readonly Translation<Squad> SquadKicked = new Translation<Squad>("<#ff9966>You were kicked from {0}.", arg0Fmt: Squad.FormatColorName);

    public readonly Translation<int> SquadSignHeader = new Translation<int>("SQUAD {0}", TranslationOptions.TMProSign);
    public readonly Translation SquadLimitReached = new Translation("Max number of squads reached.", TranslationOptions.TMProUI);
    public readonly Translation SquadNameFilterViolated = new Translation("Squad name violates name filter.", TranslationOptions.TMProUI);


    public readonly Translation SquadsTitle = new Translation("Join a Squad", TranslationOptions.TMProUI);
    public readonly Translation SquadButtonLeave = new Translation("Leave", TranslationOptions.TMProUI);
    public readonly Translation SquadButtonJoin = new Translation("Join", TranslationOptions.TMProUI);
    public readonly Translation SquadButtonCreate = new Translation("Create Squad", TranslationOptions.TMProUI);
    public readonly Translation SquadButtonDone = new Translation("Done", TranslationOptions.TMProUI);
    public readonly Translation SquadLockedLabel = new Translation("Locked", TranslationOptions.TMProUI);
    public readonly Translation SquadLockedDescription = new Translation("except friends and group members", TranslationOptions.TMProUI);
    public readonly Translation<string> SquadLeader = new Translation<string>("Leader: {0}", TranslationOptions.TMProUI);
    public readonly Translation SquadSquadNamePlaceholder = new Translation("Squad name", TranslationOptions.TMProUI);
    public readonly Translation<Kit> SquadKitOptionLabel = new Translation<Kit>("Give <#fff>{0}</color>", TranslationOptions.TMProUI, Kit.FormatRichWithSpriteAndClass);
}