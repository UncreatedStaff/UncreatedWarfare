using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Players;
public sealed class PlayersTranslations : PropertiesTranslationCollection
{
    // todo: some of these are unused

    protected override string FileName => "Players";

    [TranslationData("Gets broadcasted when a player connects.", "Connecting player")]
    public readonly Translation<IPlayer> PlayerConnected = new Translation<IPlayer>("<#e6e3d5>{0} joined the server.");

    [TranslationData("Gets broadcasted when a player disconnects.", "Disconnecting player")]
    public readonly Translation<IPlayer> PlayerDisconnected = new Translation<IPlayer>("<#e6e3d5>{0} left the server.");

    [TranslationData("Kick message for a player that suffers from a rare bug which will cause GameObject.get_transform() to throw a NullReferenceException (not return null). They are kicked if this happens.", "Discord Join Code")]
    public readonly Translation<string> NullTransformKickMessage = new Translation<string>("Your character is bugged, which messes up our zone plugin. Rejoin or contact a Director if this continues. (discord.gg/{0}).");

    [TranslationData("Gets sent to a player who is attempting to main camp the other team.")]
    public readonly Translation AntiMainCampWarning = new Translation("<#fa9e9e>Stop <b><#ff3300>main-camping</color></b>! Damage is <b>reversed</b> back on you.");

    [TranslationData("Gets sent to a player who is trying to place a non-whitelisted barricade on a vehicle.", "Barricade being placed")]
    public readonly Translation<ItemBarricadeAsset> NoPlacementOnVehicle = new Translation<ItemBarricadeAsset>("<#fa9e9e>You can't place {0} on a vehicle!</color>", arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance));

    [TranslationData("Generic message sent when a player is placing something in a place they shouldn't.", "Item being placed")]
    public readonly Translation<ItemAsset> ProhibitedPlacement = new Translation<ItemAsset>("<#fa9e9e>You're not allowed to place {0} here.", arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance));

    [TranslationData("Generic message sent when a player is dropping an item where they shouldn't.", "Item being dropped", "Zone or flag the player is dropping their item in.")]
    public readonly Translation<ItemAsset, IDeployable> ProhibitedDropZone = new Translation<ItemAsset, IDeployable>("<#fa9e9e>You're not allowed to drop {0} in {1}.", arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance), arg1Fmt: Flags.ColorNameDiscoverFormat);

    [TranslationData("Generic message sent when a player is picking up an item where they shouldn't.", "Item being picked up", "Zone or flag the player is picking up their item in.")]
    public readonly Translation<ItemAsset, IDeployable> ProhibitedPickupZone = new Translation<ItemAsset, IDeployable>("<#fa9e9e>You're not allowed to pick up {0} in {1}.", arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance), arg1Fmt: Flags.ColorNameDiscoverFormat);

    [TranslationData("Generic message sent when a player is placing something in a zone they shouldn't be.", "Item being placed", "Zone or flag the player is placing their item in.")]
    public readonly Translation<ItemAsset, IDeployable> ProhibitedPlacementZone = new Translation<ItemAsset, IDeployable>("<#fa9e9e>You're not allowed to place {0} in {1}.", arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance), arg1Fmt: Flags.ColorNameDiscoverFormat);

    [TranslationData("Sent when a player tries to steal a battery.")]
    public readonly Translation NoStealingBatteries = new Translation("<#fa9e9e>Stealing batteries is not allowed.</color>");

    [TranslationData("Sent when a player tries to manually leave their group.")]
    public readonly Translation NoLeavingGroup = new Translation("<#fa9e9e>You are not allowed to manually change groups, use <#cedcde>/teams</color> instead.");

    [TranslationData("Message sent when a player tries to place a non-whitelisted item in a storage inventory.", "Item being stored")]
    public readonly Translation<ItemAsset> ProhibitedStoring = new Translation<ItemAsset>("<#fa9e9e>You are not allowed to store {0}.", arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance));

    [TranslationData("Sent when a player tries to point or mark while not a squad leader.")]
    public readonly Translation MarkerNotInSquad = new Translation("<#fa9e9e>Only your squad can see markers. Create a squad with <#cedcde>/squad create</color> to use this feature.");

    [TranslationData("Sent on a SEVERE toast when the player enters enemy territory.", "Seconds until death")]
    public readonly Translation<string> EnteredEnemyTerritory = new Translation<string>("ENEMY HQ PROXIMITY\nLEAVE IMMEDIATELY\nDEAD IN <uppercase>{0}</uppercase>", TranslationOptions.UnityUI);

    [TranslationData("Sent 2 times before a player is kicked for inactivity.", "Time code")]
    public readonly Translation<string> InactivityWarning = new Translation<string>("<#fa9e9e>You will be AFK-Kicked in <#cedcde>{0}</color> if you don't move.</color>");

    [TranslationData("Broadcasted when a player is removed from the game by BattlEye.", "Player being kicked.")]
    public readonly Translation<IPlayer> BattlEyeKickBroadcast = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was kicked by <#feed00>BattlEye</color>.", arg0Fmt: WarfarePlayer.FormatPlayerName);

    [TranslationData("Sent when an unauthorized player attempts to edit a sign.")]
    public readonly Translation ProhibitedSignEditing = new Translation("<#ff8c69>You are not allowed to edit that sign.");

    [TranslationData("Sent when a player tries to craft a blacklisted blueprint.")]
    public readonly Translation NoCraftingBlueprint = new Translation("<#b3a6a2>Crafting is disabled for this item.");

    [TranslationData("Shows above the XP UI when divisions are enabled.", "Branch (Division) the player is a part of.")]
    public readonly Translation<Branch> XPUIDivision = new Translation<Branch>("{0} Division");

    [TranslationData("Tells the player that the game detected they have started nitro boosting.")]
    public readonly Translation StartedNitroBoosting = new Translation("<#e00ec9>Thank you for nitro boosting! In-game perks have been activated.");

    [TranslationData("Tells the player that the game detected they have stopped nitro boosting.")]
    public readonly Translation StoppedNitroBoosting = new Translation("<#9b59b6>Your nitro boost(s) have expired. In-game perks have been deactivated.");

    [TranslationData("Tells the player that they can't remove clothes which have item storage.")]
    public readonly Translation NoRemovingClothing = new Translation("<#b3a6a2>You can not remove clothes with storage from your kit.");

    [TranslationData("Tells the player that they can't unlock vehicles from the vehicle bay.")]
    public readonly Translation UnlockVehicleNotAllowed = new Translation("<#b3a6a2>You can not unlock a requested vehicle.");

    [TranslationData("Goes on the warning UI.")]
    public readonly Translation MortarWarning = new Translation("FRIENDLY MORTAR\nINCOMING", TranslationOptions.TMProUI);

    [TranslationData("Sent on the injure UI.")]
    public readonly Translation InjuredUIHeader = new Translation("You are injured", TranslationOptions.TMProUI);

    [TranslationData("Sent on the injure UI telling the player how to give up")]
    public readonly Translation InjuredUIGiveUp = new Translation("Press <color=#cecece><b><plugin_2/></b></color> to give up.", TranslationOptions.TMProUI);

    [TranslationData("Sent in chat to tell the player how to give up.")]
    public readonly Translation InjuredUIGiveUpChat = new Translation("<#ff8c69>You were injured, press <color=#cedcde><plugin_2/></color> to give up.");
}