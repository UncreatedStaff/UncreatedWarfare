using Uncreated.Warfare.Layouts.Tickets;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.UI;

public sealed class FlagUITranslations : PropertiesTranslationCollection
{
    protected override string FileName => "UI/Flags";

    [TranslationData("Shown when your team is capturing the flag.")]
    public readonly Translation<string> Capturing = new Translation<string>("Capturing {0}", TranslationOptions.TMProUI);

    [TranslationData("Shown when your team is losing the flag because the other team has more players.")]
    public readonly Translation<string> Losing = new Translation<string>("Losing {0}", TranslationOptions.TMProUI);

    [TranslationData("Shown when your team is holding the flag after it has been captured.")]
    public readonly Translation<string> Secured = new Translation<string>("{0} Secured", TranslationOptions.TMProUI);

    [TranslationData("Shown when the flag has not been captured by either team.")]
    public readonly Translation<string> Neutralized = new Translation<string>("{0} Neutralized", TranslationOptions.TMProUI);

    [TranslationData("Shown when your team lost the flag and you dont have enough people on the flag to clear.")]
    public readonly Translation<string> Lost = new Translation<string>("{0} Lost", TranslationOptions.TMProUI);

    [TranslationData("Shown when your team and the other team have the same amount of people on the flag.")]
    public readonly Translation<string> Contesting = new Translation<string>("Contesting {0}", TranslationOptions.TMProUI);

    [TranslationData("Shown when you're on a flag but it's not the objective.")]
    public readonly Translation<string> Ineffective = new Translation<string>("{0} Lost - Ineffective force", TranslationOptions.TMProUI);

    [TranslationData("Shown when your team is capturing a flag still owned by the other team.")]
    public readonly Translation<string> Clearing = new Translation<string>("Clearing {0}", TranslationOptions.TMProUI);

    [TranslationData("Shown when you're trying to capture a flag while in a vehicle.")]
    public readonly Translation InVehicle = new Translation("In Vehicle", TranslationOptions.TMProUI);

    [TranslationData("Shown in Invasion when a flag has already been captured by attackers and can't be recaptured.")]
    public readonly Translation<string> Locked = new Translation<string>("{0} Locked", TranslationOptions.TMProUI);

    [TranslationData("Shown on the flag list when ticket bleed is None.")]
    public readonly Translation TicketBleedNone = new Translation(string.Empty, TranslationOptions.TMProUI);

    [TranslationData("Shown on the flag list when ticket bleed is Minor.")]
    public readonly Translation TicketBleedMinor = new Translation("<#e88e8e>-1 per minute</color>", TranslationOptions.TMProUI);

    [TranslationData("Shown on the flag list when ticket bleed is Major.")]
    public readonly Translation TicketBleedMajor = new Translation("<#e88e8e>-2 per minute</color>", TranslationOptions.TMProUI);

    [TranslationData("Shown on the flag list when ticket bleed is Drastic.")]
    public readonly Translation TicketBleedDrastic = new Translation("<#e88e8e>-3 per minute</color>", TranslationOptions.TMProUI);

    [TranslationData("Shown on the flag list when ticket bleed is Catastrophic.")]
    public readonly Translation TicketBleedCatastrophic = new Translation("<#e88e8e>-60 per minute</color>", TranslationOptions.TMProUI);

    public Translation GetBleedMessage(TicketBleedSeverity bleedSeverity)
    {
        switch (bleedSeverity)
        {
            case TicketBleedSeverity.Minor:
                return TicketBleedMinor;
            case TicketBleedSeverity.Major:
                return TicketBleedMajor;
            case TicketBleedSeverity.Drastic:
                return TicketBleedDrastic;
            case TicketBleedSeverity.Catastrophic:
                return TicketBleedCatastrophic;
            default:
                return TicketBleedNone;
        }
    }
}