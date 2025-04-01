using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;

namespace Uncreated.Warfare.Commands;

[Command("structure", "struct"), MetadataFile]
internal sealed class StructureCommand : ICommand;

public sealed class StructureTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Structure";

    [TranslationData]
    public readonly Translation StructureNoTarget = new Translation("<#ff8c69>You must be looking at a barricade, structure, or vehicle.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> StructureSaved = new Translation<ItemAsset>("<#e6e3d5>Saved <#c6d4b8>{0}</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> StructureUpdated = new Translation<ItemAsset>("<#e6e3d5>Updated save for <#c6d4b8>{0}</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> StructureAlreadySaved = new Translation<ItemAsset>("<#e6e3d5><#c6d4b8>{0}</color> is already saved.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> StructureUnsaved = new Translation<ItemAsset>("<#e6e3d5>Removed <#c6d4b8>{0}</color> save.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> StructureAlreadyUnsaved = new Translation<ItemAsset>("<#ff8c69><#c6d4b8>{0}</color> is not saved.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<Asset> StructureDestroyed = new Translation<Asset>("<#e6e3d5>Destroyed <#c6d4b8>{0}</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation StructureNotDestroyable = new Translation("<#ff8c69>That object can not be destroyed.");

    [TranslationData]
    public readonly Translation StructureExamineNotExaminable = new Translation("<#ff8c69>That object can not be examined.");

    [TranslationData]
    public readonly Translation StructureExamineNotLocked = new Translation("<#ff8c69>This vehicle is not locked.");

    [TranslationData]
    public readonly Translation<Asset, IPlayer, FactionInfo> StructureExamineLastOwnerPrompt = new Translation<Asset, IPlayer, FactionInfo>("Last owner of {0}: {1}, Team: {2}.", TranslationOptions.TMProUI | TranslationOptions.NoRichText, arg1Fmt: WarfarePlayer.FormatPlayerName, arg2Fmt: FactionInfo.FormatDisplayName);

    [TranslationData]
    public readonly Translation<Asset, IPlayer, IPlayer, FactionInfo> StructureExamineLastOwnerChat = new Translation<Asset, IPlayer, IPlayer, FactionInfo>("<#c6d4b8>Last owner of <#e6e3d5>{0}</color>: {1} <i>({2})</i>, Team: {3}.", TranslationOptions.TMProUI | TranslationOptions.NoRichText, arg0Fmt: RarityColorAddon.Instance, arg1Fmt: WarfarePlayer.FormatColoredPlayerName, arg2Fmt: WarfarePlayer.FormatSteam64, arg3Fmt: FactionInfo.FormatColorDisplayName);

    [TranslationData]
    public readonly Translation<Asset, IPlayer, FactionInfo> VehicleExamineLastOwnerPrompt = new Translation<Asset, IPlayer, FactionInfo>("Owner of {0}: {1}, Team: {2}.", TranslationOptions.TMProUI | TranslationOptions.NoRichText, arg1Fmt: WarfarePlayer.FormatPlayerName, arg2Fmt: FactionInfo.FormatDisplayName);

    [TranslationData]
    public readonly Translation<Asset, IPlayer, IPlayer, FactionInfo> VehicleExamineLastOwnerChat = new Translation<Asset, IPlayer, IPlayer, FactionInfo>("<#c6d4b8>Owner of <#e6e3d5>{0}</color>: {1} <i>({2})</i>, Team: {3}.", TranslationOptions.TMProUI | TranslationOptions.NoRichText, arg0Fmt: RarityColorAddon.Instance, arg1Fmt: WarfarePlayer.FormatColoredPlayerName, arg2Fmt: WarfarePlayer.FormatSteam64, arg3Fmt: FactionInfo.FormatColorDisplayName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string> StructureSaveInvalidProperty = new Translation<string>("<#ff8c69>{0} isn't a valid a structure property. Try putting 'owner' or 'group'.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string, string> StructureSaveInvalidSetValue = new Translation<string, string>("<#ff8c69><#ddd>{0}</color> isn't a valid value for structure property: <#a0ad8e>{1}</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string> StructureSaveNotJsonSettable = new Translation<string>("<#ff8c69><#a0ad8e>{0}</color> is not marked as settable.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string, ItemAsset, string> StructureSaveSetProperty = new Translation<string, ItemAsset, string>("<#a0ad8e>Set <#8ce4ff>{0}</color> for {1} save to: <#ffffff>{2}</color>.", arg1Fmt: RarityColorAddon.Instance);
}