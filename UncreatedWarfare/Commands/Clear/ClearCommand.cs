using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("clear", "clr"), MetadataFile]
public class ClearCommand : ICommand;
public class ClearTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Clear";

    [TranslationData("Sent when a user tries to clear from console and doesn't provide a player name.", IsPriorityTranslation = false)]
    public readonly Translation ClearNoPlayerConsole = new Translation("Specify a player name when clearing from console.");

    [TranslationData("Sent when a player clears their own inventory.", IsPriorityTranslation = false)]
    public readonly Translation ClearInventorySelf = new Translation("<#e6e3d5>Cleared your inventory.");

    [TranslationData("Sent when a user clears another player's inventory.", "The other player", IsPriorityTranslation = false)]
    public readonly Translation<IPlayer> ClearInventoryOther = new Translation<IPlayer>("<#e6e3d5>Cleared {0}'s inventory.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData("Sent when a user clears all dropped items.", IsPriorityTranslation = false)]
    public readonly Translation ClearItems = new Translation("<#e6e3d5>Cleared all dropped items.");

    [TranslationData("Sent when a user clears all dropped items within a given range.", "The range in meters", IsPriorityTranslation = false)]
    public readonly Translation<float> ClearItemsInRange = new Translation<float>("<#e6e3d5>Cleared all dropped items in {0}m.", arg0Fmt: "F0");

    [TranslationData("Sent when a user clears all items dropped by another player.", "The player", IsPriorityTranslation = false)]
    public readonly Translation<IPlayer> ClearItemsOther = new Translation<IPlayer>("<#e6e3d5>Cleared {0}'s dropped items.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData("Sent when a user clears all placed structures and barricades.", IsPriorityTranslation = false)]
    public readonly Translation ClearStructures = new Translation("<#e6e3d5>Cleared all placed structures and barricades.");

    [TranslationData("Sent when a user clears all spawned vehicles.", IsPriorityTranslation = false)]
    public readonly Translation ClearVehicles = new Translation("<#e6e3d5>Cleared all vehicles.");
}