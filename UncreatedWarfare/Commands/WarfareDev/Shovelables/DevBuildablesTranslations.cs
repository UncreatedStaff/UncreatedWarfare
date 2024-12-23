using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Kits.Whitelists;
public class DevBuildablesTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "WarfareDev Buildables";
    
    [TranslationData("Sent to an admin/developer when they try to use a command but barricade state storage is not supported.", IsPriorityTranslation = false)]
    public readonly Translation StateStorageNotSupported = new Translation("<#ff8c69>Barricade state storage must be supported in order to use this command.");

    [TranslationData("Sent to an admin/developer when they try to use a command but factions are not supported.", IsPriorityTranslation = false)]
    public readonly Translation FactionsNotSupported = new Translation("<#ff8c69>You cannot specify a faction right now because factions are not supported.");

    [TranslationData("Sent to an admin/developer when they try to save a barricade's state under an unknown faction.", IsPriorityTranslation = false)]
    public readonly Translation<string> FactionsNotFound = new Translation<string>("<#ff8c69>'{0}' is not a known faction.");

    [TranslationData("Sent to an admin/developer when they try to use a command but fobs are not supported.", IsPriorityTranslation = false)]
    public readonly Translation NotLookingAtBarricade = new Translation("<#ff8c69>You must be looking at a buildable.");

    [TranslationData("Sent to an admin/developer when they successfully save a barricade's state.", IsPriorityTranslation = false)]
    public readonly Translation<ItemPlaceableAsset, FactionInfo> BarricadeSaveStateSuccess = new Translation<ItemPlaceableAsset, FactionInfo>(
        "<#a0ad8e>Successfully saved buildable state for barricade '<#ffcd78>{0}</color>' under faction <#9dc0d4>{1}</color>.");
}
