using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Kits.Whitelists;
public class WhitelistTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Item Restrictions";

    [TranslationData("Sent to an admin after they whitelist an item.", IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> WhitelistAdded = new Translation<ItemAsset>("<#a0ad8e>Whitelisted item: {0}.",
        arg0Fmt: RarityColorAddon.Instance
    );

    [TranslationData("Sent to an admin after they update the amount of an item's whitelist.", IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset, int> WhitelistSetAmount = new Translation<ItemAsset, int>(
        "<#a0ad8e>Amount for whitelisted item: {0} set to {1}.",
        arg0Fmt: RarityColorAddon.Instance
    );

    [TranslationData("Sent to an admin after they un-whitelist an item.", IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> WhitelistRemoved = new Translation<ItemAsset>(
        "<#a0ad8e>Removed whitelist for: {0}.",
        arg0Fmt: RarityColorAddon.Instance
    );

    [TranslationData("Sent to an admin after they try to whitelist an item when it's already added.", IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> WhitelistAlreadyAdded = new Translation<ItemAsset>(
        "<#ff8c69>{0} is already whitelisted.",
        arg0Fmt: RarityColorAddon.Instance
    );

    [TranslationData("Sent to an admin after they try to un-whitelist an item when it isn't whitelisted.", IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> WhitelistAlreadyRemoved = new Translation<ItemAsset>(
        "<#ff8c69>{0} is not whitelisted.",
        arg0Fmt: RarityColorAddon.Instance
    );

    [TranslationData("Sent to an admin after they try to whitelist an item but the item can't be found.", IsPriorityTranslation = false)]
    public readonly Translation<string> WhitelistItemNotID = new Translation<string>("<#ff8c69>Failed to find an item matching <#fff>{0}</color>.");

    [TranslationData("Sent to an admin after they try to whitelist an item but multiple matching items are found.", IsPriorityTranslation = false)]
    public readonly Translation<string> WhitelistMultipleResults = new Translation<string>("<#ff8c69><#fff>{0}</color> found multiple results, please narrow your search or use the item's <#cedcde>GUID</color>.");

    [TranslationData("Sent to an admin after they try to whitelist an item but the amount they entered isn't a valid positive number or 'infinite'.", IsPriorityTranslation = false)]
    public readonly Translation<string> WhitelistInvalidAmount = new Translation<string>("<#ff8c69><#fff>{0}</color> couldn't be read as an amount (<#cedcde>1-255</color> or <#cedcde>infinity</color>).");

    [TranslationData("Sent to a player when they try to pick up an un-whitelisted item that isn't in their kit.")]
    public readonly Translation<ItemAsset> WhitelistProhibitedPickup = new Translation<ItemAsset>(
        "<#ff8c69>{0} can't be picked up.",
        arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance)
    );

    [TranslationData("Sent to a player when they try to store an un-whitelisted item in a storage.")]
    public readonly Translation<ItemAsset> WhitelistProhibitedStore = new Translation<ItemAsset>(
        "<#ff8c69>{0} can't be placed in storage.",
        arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance)
    );

    [TranslationData("Sent to a player when they try to salvage an un-whitelisted placeable that isn't in their kit.")]
    public readonly Translation<ItemAsset> WhitelistProhibitedSalvage = new Translation<ItemAsset>(
        "<#ff8c69>{0} can't be salvaged.",
        arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance)
    );

    [TranslationData("Sent to a player when they try to pick up an un-whitelisted item that isn't in their kit or when they have too many of the item already in their inventory.")]
    public readonly Translation<int, ItemAsset> WhitelistProhibitedPickupAmt = new Translation<int, ItemAsset>(
        "<#ff8c69>You can't carry more than {0} {1}.",
        arg1Fmt: new ArgumentFormat(PluralAddon.WhenArgument(0), RarityColorAddon.Instance)
    );
    
    [TranslationData("Sent to a player when they try to place a barricade on a vehicle.")]
    public readonly Translation WhitelistProhibitedPlaceOnFriendlyVehicle = new Translation("<#ff8c69>You are not allowed to place barricades on friendly vehicles.");

    [TranslationData("Sent to a player when they try to place an un-whitelisted placeable that isn't in their kit.")]
    public readonly Translation<ItemAsset> WhitelistProhibitedPlace = new Translation<ItemAsset>(
        "<#ff8c69>You're not allowed to place {0}.",
        arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance)
    );

    [TranslationData("Sent to a player when they try to place an un-whitelisted placeable that isn't in their kit, or too many are already placed by them.")]
    public readonly Translation<int, ItemAsset> WhitelistProhibitedPlaceAmt = new Translation<int, ItemAsset>(
        "<#ff8c69>You're not allowed to place more than {0} {1}.",
        arg1Fmt: new ArgumentFormat(PluralAddon.WhenArgument(0), RarityColorAddon.Instance)
    );

    [TranslationData("Sent to a player when they try to pick up an un-whitelisted item without a kit.")]
    public readonly Translation WhitelistNoKit = new Translation("<#ff8c69>Get a kit first before you can pick up items.");
}
