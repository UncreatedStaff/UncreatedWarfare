using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Skillsets;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Commands;

[Command("kit", "k", "whale"), MetadataFile]
public class KitCommand : ICommand;
public class KitCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Kit";

    [TranslationData("Sent when a player tries to give themselves or someone else a kit but isn't looking at a sign.")]
    public readonly Translation KitOperationNoTarget = new Translation("<#a4baa9>You must be looking at a request sign or enter a kit ID.");

    [TranslationData("Sent when the player gives themselves a kit via command.", "The Kit that was given", IsPriorityTranslation = false)]
    public readonly Translation<Kit> KitGiveSuccess = new Translation<Kit>("<#a0ad8e>Equipped kit: <#fff>{0}</color>.", arg0Fmt: Kit.FormatDisplayName);
    
    [TranslationData("Sent when the player gives someone a kit via command.", "The Kit that was given", "The player that received the kit", IsPriorityTranslation = false)]
    public readonly Translation<Kit, IPlayer> KitGiveSuccessToPlayer = new Translation<Kit, IPlayer>("<#a0ad8e>Equipped <#fff>{0}</color> for {1}.", arg0Fmt: Kit.FormatDisplayName, arg1Fmt: WarfarePlayer.FormatColoredCharacterName);
    
    [TranslationData("Sent when the player creates a new kit with /kit create <name>", "New Kit", IsPriorityTranslation = false)]
    public readonly Translation<Kit> KitCreated = new Translation<Kit>("<#a0ad8e>Created kit: <#fff>{0}</color>.", arg0Fmt: Kit.FormatId);

    [TranslationData("Sent when the player overwrites the items in a kit with /kit create <name>", "Overwritten Kit", IsPriorityTranslation = false)]
    public readonly Translation<Kit> KitOverwrote = new Translation<Kit>("<#a0ad8e>Overwritten items for kit: <#fff>{0}</color>.", arg0Fmt: Kit.FormatId);

    [TranslationData("Sent when the player tries overwriting the items in a kit with /kit create <name>. They must /confirm first.", "Overwritten Kit", "Overwritten Kit", IsPriorityTranslation = false)]
    public readonly Translation<Kit, Kit> KitConfirmOverride = new Translation<Kit, Kit>("<#c480d9>Type <#aaa>/confirm</color> in the next 15 seconds if you want to override the items in <#fff>{0}</color> (<#aaa>{1}</color>).", arg0Fmt: Kit.FormatId, arg1Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent when the player tries deleting kit with /kit delete <name>. They must /confirm first.", "Deleting Kit", "Deleting Kit", IsPriorityTranslation = false)]
    public readonly Translation<Kit, Kit> KitConfirmDelete = new Translation<Kit, Kit>("<#c480d9>Type <#aaa>/confirm</color> in the next 15 seconds if you want to delete <#fff>{0}</color> (<#aaa>{1}</color>).", arg0Fmt: Kit.FormatId, arg1Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent when the player doesn't /confirm in time for overwriting kit items.", IsPriorityTranslation = false)]
    public readonly Translation KitCancelOverride = new Translation("<#ff8c69>Item override cancelled.");

    [TranslationData("Sent when the player doesn't /confirm in time for deleting a kit.", IsPriorityTranslation = false)]
    public readonly Translation KitCancelDelete = new Translation("<#ff8c69>Deleting kit cancelled.");

    [TranslationData("Sent when the player copies a kit with /kit copyfrom <source> <name>", "Source Kit", "New Kit", IsPriorityTranslation = false)]
    public readonly Translation<Kit, Kit> KitCopied = new Translation<Kit, Kit>("<#a0ad8e>Copied data from <#c7b197>{0}</color> into a new kit: <#fff>{1}</color>.", arg0Fmt: Kit.FormatId, arg1Fmt: Kit.FormatId);
    
    [TranslationData("Sent when the player tried to copy a kit that isn't a loadout to a kit with a loadout-style name.", "Source Kit", "New Kit", IsPriorityTranslation = false)]
    public readonly Translation<Kit, string> KitCopyNonLoadoutToLoadout = new Translation<Kit, string>("<#a0ad8e>Can't copy <#c7b197>{0}</color> to a kit called <#fff>{1}</color>. The source kit has to also be a loadout.", arg0Fmt: Kit.FormatId);

    [TranslationData("Sent when the player deletes a kit with /kit delete <name>", IsPriorityTranslation = false)]
    public readonly Translation<Kit> KitDeleted = new Translation<Kit>("<#a0ad8e>Deleted kit: <#fff>{0}</color>.", arg0Fmt: Kit.FormatId);

    [TranslationData("Response to /kit search with a list of results.")]
    public readonly Translation<string> KitSearchResults = new Translation<string>("<#a0ad8e>Matches: <i>{0}</i>.");
    
    [TranslationData("Sent to a player when they're granted access to a kit.")]
    public readonly Translation<Kit> KitAccessGivenDm = new Translation<Kit>("<#a0ad8e>You were given access to the kit: <#fff>{0}</color>.", arg0Fmt: Kit.FormatId);

    [TranslationData("Sent to a player when they lose access to a kit.")]
    public readonly Translation<Kit> KitAccessRevokedDm = new Translation<Kit>("<#a0ad8e>Your access to <#fff>{0}</color> was revoked.", arg0Fmt: Kit.FormatId);

    [TranslationData("Sent when a player requests the default loadout for a given class.", "The class of the loadout they requested", IsPriorityTranslation = false)]
    public readonly Translation<Class> RequestDefaultLoadoutGiven = new Translation<Class>("<#a8918a>Given default items for a <#cedcde>{0}</color> loadout.", arg0Fmt: UppercaseAddon.Instance);

    [TranslationData("Sent to a player when they try to bind a hotkey without holding an item.")]
    public readonly Translation KitHotkeyNotHoldingItem = new Translation("<#ff8c69>You must be holding an item from your kit to set a hotkey.");

    [TranslationData("Sent to a player when they try to bind a hotkey without holding an item from their kit.")]
    public readonly Translation<ItemAsset> KitHotkeyNotHoldingValidItem = new Translation<ItemAsset>("<#ff8c69><#ffe6d7>{0}</color> can't be eqipped in a hotkey slot <#ddd>(3 through 0)</color>.", arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance));

    [TranslationData("Sent to a player when they try to bind a hotkey without a kit equipped.")]
    public readonly Translation KitHotkeyNoKit = new Translation("<#ff8c69>You can not bind hotkeys unless you have a kit equipped.");
    
    [TranslationData("Sent to a player when they try to bind a hotkey to an invalid number.")]
    public readonly Translation KitHotkeyInvalidSlot = new Translation("<#ff8c69>You can only bind items to hotkeys 3 through 0.");

    [TranslationData("Sent to a player when they bind a hotkey to an item in their kit.")]
    public readonly Translation<ItemAsset, byte, Kit> KitHotkeyBinded = new Translation<ItemAsset, byte, Kit>("<#a0ad8e>Binded <#e8e2d1>{0}</color> to slot <#e8e2d1>{1}</color> for <#fff>{2}</color>.", arg2Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent to a player when they unbind a hotkey to an item in their kit.")]
    public readonly Translation<byte, Kit> KitHotkeyUnbound = new Translation<byte, Kit>("<#a0ad8e>Unbound slot <#e8e2d1>{0}</color> for <#fff>{1}</color>.", arg1Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent to a player when they try to unbind a hotkey that isn't bound.")]
    public readonly Translation<byte, Kit> KitHotkeyNotFound = new Translation<byte, Kit>("<#ff8c69>Slot <#e8e2d1>{0}</color> for <#fff>{1}</color> was not bound.", arg1Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent to a player when they try to save a layout without a kit equipped.")]
    public readonly Translation KitLayoutNoKit = new Translation("<#ff8c69>You can not save your kit's item layout unless you have a kit equipped.");

    [TranslationData("Sent to a player when they save a custom layout (where their items go) for their kit.")]
    public readonly Translation<Kit> KitLayoutSaved = new Translation<Kit>("<#a0ad8e>Custom layout for <#fff>{0}</color> saved.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent to a player when they reset their kit's custom layout (where their items go).")]
    public readonly Translation<Kit> KitLayoutReset = new Translation<Kit>("<#a0ad8e>Custom layout for <#fff>{0}</color> reset.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent to a player when they try to set the sign text of a kit to a language not in our database.", IsPriorityTranslation = false)]
    public readonly Translation<string> KitLanguageNotFound = new Translation<string>("<#ff8c69>Language not found: <#fff>{0}</color>.");

    [TranslationData("Sent to a player when they set a generic property of a kit.", IsPriorityTranslation = false)]
    public readonly Translation<string, Kit, string> KitPropertySet = new Translation<string, Kit, string>("<#a0ad8e>Set <#aaa>{0}</color> on kit <#fff>{1}</color> to <#aaa><uppercase>{2}</uppercase></color>.", arg1Fmt: Kit.FormatId);

    [TranslationData("Sent to a player when they try to create or rename a kit and the ID is already taken.", IsPriorityTranslation = false)]
    public readonly Translation<string> KitNameTaken = new Translation<string>("<#ff8c69>A kit named <#fff>{0}</color> already exists.");

    [TranslationData("Sent to a player when they try to use a kit ID as an argument but it doesn't exist.")]
    public readonly Translation<string> KitNotFound = new Translation<string>("<#ff8c69>A kit named <#fff>{0}</color> doesn't exist.");
    
    [TranslationData("Sent to a player when they try to rename a kit that isn't their loadout.")]
    public readonly Translation<Kit> KitRenameNotLoadout = new Translation<Kit>("<#ff8c69><#fff>{0}</color> isn't a loadout.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent to a player when they try to rename their loadout and the name is a violation of the chat filter.")]
    public readonly Translation<string> KitRenameFilterVoilation = new Translation<string>("<#ff8c69>Your name violates our chat filter near: <#fff>'{0}'</color>.");

    [TranslationData("Sent to a player when they try to rename a kit that hasn't been upgraded or is still being upgraded.")]
    public readonly Translation<Kit> KitRenameNoAccess = new Translation<Kit>("<#ff8c69><#fff>{0}</color> is still being set up or isn't upgraded yet.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent to a player after change their loadout's display name.")]
    public readonly Translation<string, string, string> KitRenamed = new Translation<string, string, string>("<#a0ad8e>Renamed loadout <#fff>{0}</color> from <#ddd>\"{1}\"</color> to <#ddd>\"{2}\"</color>.");
    
    [TranslationData("Sent to a player when they try to change a kit property that doesn't exist.", IsPriorityTranslation = false)]
    public readonly Translation<string> KitPropertyNotFound = new Translation<string>("<#ff8c69>Kits don't have a <#eee>{0}</color> property.");

    [TranslationData("Sent to a player when they try to change a kit property that can't be changed.", IsPriorityTranslation = false)]
    public readonly Translation<string> KitPropertyProtected = new Translation<string>("<#ff8c69><#eee>{0}</color> can not be changed on kits.");

    [TranslationData("Sent to a player when they try to change a kit property but the value they enter can't be parsed.", IsPriorityTranslation = false)]
    public readonly Translation<string, Type, string> KitInvalidPropertyValue = new Translation<string, Type, string>("<#ff8c69><#fff>{2}</color> isn't a valid value for <#eee>{0}</color> (<#aaa>{1}</color>).");
    
    [TranslationData("Sent to a player when they try to give a player access to a kit that already has access.", IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, Kit> KitAlreadyHasAccess = new Translation<IPlayer, Kit>("<#ff8c69>{0} already has access to <#fff>{1}</color>.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName, arg1Fmt: Kit.FormatId);

    [TranslationData("Sent to a player when they try to take a player's access to a kit that already doesn't have access.", IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, Kit> KitAlreadyMissingAccess = new Translation<IPlayer, Kit>("<#ff8c69>{0} doesn't have access to <#fff>{1}</color>.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName, arg1Fmt: Kit.FormatId);

    [TranslationData("Sent to a player when they give another player access to a kit.", IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, IPlayer, Kit> KitAccessGiven = new Translation<IPlayer, IPlayer, Kit>("<#a0ad8e>{0} (<#aaa>{1}</color>) was given access to the kit: <#fff>{2}</color>.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName, arg1Fmt: WarfarePlayer.FormatSteam64, arg2Fmt: Kit.FormatId);
    
    [TranslationData("Sent to a player when they remove another player's access to a kit.", IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, IPlayer, Kit> KitAccessRevoked = new Translation<IPlayer, IPlayer, Kit>("<#a0ad8e>{0} (<#aaa>{1}</color>)'s access to <#fff>{2}</color> was taken away.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName, arg1Fmt: WarfarePlayer.FormatSteam64, arg2Fmt: Kit.FormatId);

    [TranslationData("Sent to a player after they start creating a new loadout.", IsPriorityTranslation = false)]
    public readonly Translation<Class, IPlayer, IPlayer, Kit> LoadoutCreated = new Translation<Class, IPlayer, IPlayer, Kit>("<#a0ad8e>Created <#bbc>{0}</color> loadout for {1} (<#aaa>{2}</color>). Kit name: <#fff>{3}</color>.", arg1Fmt: WarfarePlayer.FormatColoredCharacterName, arg2Fmt: WarfarePlayer.FormatSteam64, arg3Fmt: Kit.FormatId);
    
    [TranslationData("Sent to a player when they try to create a kit with a faction that isn't in our database.", IsPriorityTranslation = false)]
    public readonly Translation<string> FactionNotFound = new Translation<string>("<#ff8c69>Unable to find a faction called <#fff>{0}</color>.");
    
    [TranslationData("Sent to a player when they try to create a kit with an unknown class type.", IsPriorityTranslation = false)]
    public readonly Translation<string> ClassNotFound = new Translation<string>("<#ff8c69>There is no kit class named <#fff>{0}</color>.");
    
    [TranslationData("Sent to a player when they try to create a kit with an kit type.", IsPriorityTranslation = false)]
    public readonly Translation<string> TypeNotFound = new Translation<string>("<#ff8c69>There is no kit type named <#fff>{0}</color>. Use: 'public', 'elite', 'special', 'loadout'.");

    [TranslationData("Sent to a player when they try to favorite a kit they already have favorited.")]
    public readonly Translation<Kit> KitFavoriteAlreadyFavorited = new Translation<Kit>("<#ff8c69><#e8e2d1>{0}</color> is already <#fd0>favorited</color>.");

    [TranslationData("Sent to a player when they try to unfavorite a kit they don't have favorited.")]
    public readonly Translation<Kit> KitFavoriteAlreadyUnfavorited = new Translation<Kit>("<#ff8c69><#e8e2d1>{0}</color> is already <#fff>unfavorited</color>.");

    [TranslationData("Sent to a player after they add a kit to their favorites.")]
    public readonly Translation<Kit> KitFavorited = new Translation<Kit>("<#a0ad8e>Added <#e8e2d1>{0}</color> to your <#fd0>favorites</color>.");

    [TranslationData("Sent to a player after they remove a kit from their favorites.")]
    public readonly Translation<Kit> KitUnfavorited = new Translation<Kit>("<#a0ad8e>Removed <#e8e2d1>{0}</color> from your <#fd0>favorites</color>.");

    [TranslationData("Sent to a player when they try to use an invalid loadout ID as a kit ID argument.", IsPriorityTranslation = false)]
    public readonly Translation KitLoadoutIdBadFormat = new Translation("Kit name must be in format: <b>765XXXXXXXXXXXXXX_X..</b>.");

    [TranslationData("Sent to a player after they start upgrading a loadout.", IsPriorityTranslation = false)]
    public readonly Translation<Kit, Class> LoadoutUpgraded = new Translation<Kit, Class>("<#a0ad8e>Upgraded <#e8e2d1>{0}</color> to a new <#fff>{1}</color> kit.", arg0Fmt: Kit.FormatDisplayName, arg1Fmt: UppercaseAddon.Instance);

    [TranslationData("Sent to a player after they update the season of a normal kit.", IsPriorityTranslation = false)]
    public readonly Translation<Kit> KitUpgraded = new Translation<Kit>("<#a0ad8e>Upgraded <#e8e2d1>{0}</color>.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent if a player tries to upgrade their loadout but they it's already up to date.", "The name of the kit they're trying to upgrade")]
    public readonly Translation<Kit> DoesNotNeedUpgrade = new Translation<Kit>("<#a4baa9><#ffebbd>{0}</color> does not need to be upgraded. If you're trying to update the kit and it was created during this season, open a help ticket.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent to a player after they enable a normal kit or unlock a loadout.", IsPriorityTranslation = false)]
    public readonly Translation<Kit> KitUnlocked = new Translation<Kit>("<#a0ad8e>Unlocked <#e8e2d1>{0}</color>.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent to a player after their loadout is finished being made by staff.")]
    public readonly Translation<Kit> DMLoadoutUnlocked = new Translation<Kit>("<#a0ad8e>Your kit, <#e8e2d1>{0}</color>, is ready.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent if an admin tries to unlock a kit that isn't locked.", "The name of the kit", IsPriorityTranslation = false)]
    public readonly Translation<Kit> DoesNotNeedUnlock = new Translation<Kit>("<#a4baa9><#ffebbd>{0}</color> does not need to be unlocked.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent to a player after they disable a normal kit or lock a loadout.", IsPriorityTranslation = false)]
    public readonly Translation<Kit> KitLocked = new Translation<Kit>("<#a0ad8e>Locked <#e8e2d1>{0}</color>.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent if an admin tries to lock a kit that is already locked.", "The name of the kit", IsPriorityTranslation = false)]
    public readonly Translation<Kit> DoesNotNeedLock = new Translation<Kit>("<#a4baa9><#ffebbd>{0}</color> does not need to be locked.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent when the caller doesn't enter a valid integer for level.", "Skill name", "Max level", IsPriorityTranslation = false)]
    public readonly Translation<string, int> KitInvalidSkillsetLevel = new Translation<string, int>("<#ff8c69>Please give a level between <#fff>0</color> and <#fff>{1}</color> for <#ddd>{0}</color>");

    [TranslationData("Sent when the caller doesn't enter a valid integer for level.", "Skill name", "Max level", IsPriorityTranslation = false)]
    public readonly Translation<string> KitInvalidSkillset = new Translation<string>("<#ff8c69>\"<#fff>{0}</color>\" is not a valid skill name, use the displayed value in-game.");

    [TranslationData("Sent when the skillset requested to be removed isn't present.", "Skill set", IsPriorityTranslation = false)]
    public readonly Translation<Skillset, Kit> KitSkillsetNotFound = new Translation<Skillset, Kit>("<#ff8c69>\"<#ddd>{0}</color>\" is not overridden by <#fff>{1}</color>.", arg0Fmt: Skillset.FormatNoLevel, arg1Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent when a skillset is removed.", "Skill set", "Kit target", IsPriorityTranslation = false)]
    public readonly Translation<Skillset, Kit> KitSkillsetRemoved = new Translation<Skillset, Kit>("<#a0ad8e>\"<#ddd>{0}</color>\" was removed from <#fff>{1}</color>.", arg1Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent when a skillset is added.", "Skill set", "Kit target", IsPriorityTranslation = false)]
    public readonly Translation<Skillset, Kit> KitSkillsetAdded = new Translation<Skillset, Kit>("<#a0ad8e>\"<#ddd>{0}</color>\" was added to <#fff>{1}</color>.", arg1Fmt: Kit.FormatDisplayName);
}