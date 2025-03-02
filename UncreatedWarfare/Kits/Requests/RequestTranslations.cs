using System;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Kits.Requests;
public class RequestTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Requests/Common";

    [TranslationData("Sends a generic request error that's used to abstract requests a bit more.")]
    public readonly Translation<string> RequestError = new Translation<string>("<#ff8c69>Unable to fufill request: <#ffffff>{0}</color>.");

    [TranslationData("Sends a generic successful request message that's used to abstract requests a bit more.")]
    public readonly Translation<IRequestable<object>> RequestedSuccess = new Translation<IRequestable<object>>("<#abcd9e>Successfully requested <#ffffff>{0}</color>.");
    
    [TranslationData("Sent when a player tries to request something that requires purchasing, but they can't afford it yet.", "The player's current balance", "Total credits required")]
    public readonly Translation<int, int> RequestNotOwnedCreditsCannotAfford = new Translation<int, int>("<#a8918a>You only have <#b8ffc1>C </color><#ffffff>{0}</color> / <#b8ffc1>C </color><#ffffff>{1}</color> needed to unlock this.");

    [TranslationData("Sent when a player tries to request something that costs real money.")]
    public readonly Translation<string> RequestNotOwnedDonor = new Translation<string>("<#a8918a>You don't have access to this premium content. This is available for <#ffffff>{0}</color> and can be purchased in our <#7483c4>Discord</color>.");

    [TranslationData("Sent when a player tries to request something that they haven't been given access to, such as an exclusive kit.")]
    public readonly Translation RequestMissingAccess = new Translation("<#a8918a>You don't have access to this content.");


    [TranslationData("Gets sent to a player who's discord is not linked to their steam account (part 1).")]
    public readonly Translation DiscordNotLinked1 = new Translation("<#9cffb3>Your account must be linked in our Discord server to use this command.");

    [TranslationData("Gets sent to a player who's discord is not linked to their steam account (part 2).", "Player's Steam64 ID")]
    public readonly Translation<IPlayer> DiscordNotLinked2 = new Translation<IPlayer>("<#9cffb3>Type <#7483c4>/discord</color> then type <#fff>/link {0}</color> in <#c480d9>#warfare-stats</color>.", arg0Fmt: WarfarePlayer.FormatColoredSteam64);

    [TranslationData("Sent to a player when they try to take a player's access to a kit that already doesn't have access.", IsPriorityTranslation = false)]
    public readonly Translation<Cooldown> RequestKitOnCooldown = new Translation<Cooldown>("<#ff8c69>You can request this kit again in: <#bafeff>{0}</color>.", arg0Fmt: Cooldown.FormatTimeShort);

    [TranslationData]
    public readonly Translation<Cooldown> RequestKitOnGlobalCooldown = new Translation<Cooldown>("<#ff8c69>You can request another kit again in: <#bafeff>{0}</color>.", arg0Fmt: Cooldown.FormatTimeShort);

    [TranslationData("Sent if a player tries to upgrade their loadout but either isn't linked or isn't in the Discord server.")]
    public readonly Translation RequestUpgradeNotInDiscordServer = new Translation("<#a4baa9>Join the <#7483c4>Discord</color> server (<#fff>/discord</color>) to open a ticket.");

    [TranslationData("Sent if a player tries to upgrade their loadout but the Discord bot can't connect to the server (occasionally happens due to maintenance).")]
    public readonly Translation RequestUpgradeNotConnected = new Translation("<#a4baa9>The loadout upgrade system is not available right now, please try again later.");

    [TranslationData("Sent if a player tries to upgrade their loadout but they already requested it before.", "The name of the kit they're trying to upgrade")]
    public readonly Translation<Kit> RequestUpgradeAlreadyOpen = new Translation<Kit>("<#a4baa9>You already have a ticket open for <#ffebbd>{0}</color>.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent if a player tries to upgrade their loadout but there are too many tickets open (Discord has a channel limit).")]
    public readonly Translation RequestUpgradeTooManyTicketsOpen = new Translation("<#a4baa9>There are too many tickets open right now, please try again later.");

    [TranslationData("Generic error trying to upgrade a player's loadout.", "Generic error message")]
    public readonly Translation<string> RequestUpgradeError = new Translation<string>("<#a4baa9>Error opening ticket: <#fff>{0}</color>.", arg0Fmt: UppercaseAddon.Instance);

    [TranslationData("Sent when a player tries to upgrade a kit that isn't a loadout.", "The name of the kit they're trying to upgrade")]
    public readonly Translation<Kit> RequestUpgradeOnKit = new Translation<Kit>("<#a4baa9><#ffebbd>{0}</color> can't be upgraded.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent when a player does /request upgrade on a loadout that was created in a previous season and hasn't been updated yet.", "The name of the kit they're upgrading")]
    public readonly Translation<Kit> TicketOpened = new Translation<Kit>("<#a8918a>An upgrade request was opened in your name for <#ffebbd>{0}</color>. Please fill it out as soon as possible.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent when a player requests a kit from a sign.", "The class of the kit they requested")]
    public readonly Translation<Class> RequestSignGiven = new Translation<Class>("<#a8918a>You have been allocated a <#cedcde>{0}</color> kit.", arg0Fmt: UppercaseAddon.Instance);

    [TranslationData("Sent when a player tries to request something but isn't looking at a sign or vehicle.")]
    public readonly Translation RequestNoTarget = new Translation("<#a4baa9>You must be looking at a request sign or vehicle.");

    [TranslationData("Sent when a player buys a kit using /buy.", "Number of credits spent")]
    public readonly Translation<int> RequestKitBought = new Translation<int>("<#c4a36a>Kit bought for <#b8ffc1>C </color><#ffffff>{0}</color>. Request it with '<#b3b0ab>/request</color>'.");

    [TranslationData("Sent when a player tries to buy a loadout from an empty loadout sign.")]
    public readonly Translation RequestBuyLoadout = new Translation("<#a8918a>Join our discord (/discord) to purchase a custom loadout..");

    [TranslationData("Sent when a player tries to request a kit but the sign isn't linked to an existing kit.")]
    public readonly Translation RequestKitNotRegistered = new Translation("<#a8918a>This has not been created yet.");

    [TranslationData("Sent when a player tries to request a kit they already have.")]
    public readonly Translation RequestKitAlreadyOwned = new Translation("<#a8918a>You already have this kit. Type /ammo on an <#cedcde>AMMO CRATE</color> to restock your kit.");

    [TranslationData("Sent when a player tries to request a kit that is currently being upgraded.")]
    public readonly Translation RequestKitNeedsSetup = new Translation("<#a8918a>This kit needs to be setup by an admin. Check your upgrade ticket.");

    [TranslationData("Sent when a player tries to request a kit that has been disabled by an admin.")]
    public readonly Translation RequestKitDisabled = new Translation("<#a8918a>This kit is disabled.");

    [TranslationData("Sent when a player tries to request a kit that is blacklisted on the current map.")]
    public readonly Translation RequestKitMapBlacklisted = new Translation("<#a8918a>This kit is not allowed on this map.");

    [TranslationData("Sent when a player tries to request a kit that is blacklisted for the player's team.")]
    public readonly Translation RequestKitFactionBlacklisted = new Translation("<#a8918a>Your team is not allowed to use this kit.");

    [TranslationData("Sent when a player tries to request a kit that requires Nitro boosting the Discord server.")]
    public readonly Translation RequestKitMissingNitro = new Translation("<#a8918a>You must be <#e00ec9>NITRO BOOSTING</color> to use this kit.");

    [TranslationData("Sent when a player tries to request a kit that belongs to the other team.", "The team that owns the kit.")]
    public readonly Translation<FactionInfo> RequestKitWrongTeam = new Translation<FactionInfo>("<#a8918a>You must be part of {0} to request this kit.", arg0Fmt: FactionInfo.FormatShortName);

    [TranslationData("Sent when a player tries to buy a kit that's either free or not purchasable using in-game currency.")]
    public readonly Translation RequestNotBuyable = new Translation("<#a8918a>This kit cannot be purchased with credits.");

    [TranslationData("Sent when a player tries to request a kit but they're too low level.", "Name of the level needed", "Number of the level needed")]
    public readonly Translation<WarfareRank, WarfareRank> RequestKitLowLevel = new Translation<WarfareRank, WarfareRank>("<#b3ab9f>You must be <#ffc29c>{0}</color> ({1}) to use this kit.", arg0Fmt: WarfareRank.FormatName, arg1Fmt: WarfareRank.FormatLPrefixedNumeric);

    [TranslationData("Sent when a player tries to request a kit but they're missing a completed quest.", "Name of the quest")]
    public readonly Translation<QuestAsset> RequestKitQuestIncomplete = new Translation<QuestAsset>("<#b3ab9f>You have to complete {0} to request this kit.", arg0Fmt: QuestTemplate.FormatColorQuestAsset);

    [TranslationData("Sent when a player tries to request a vehicle but they don't have enough credits.", "Number of credits missing", "Total credits required")]
    public readonly Translation<int, int> RequestVehicleCantAfford = new Translation<int, int>("<#a8918a>You are missing <#b8ffc1>C </color><#ffffff>{0}</color> / <#b8ffc1>C </color><#ffffff>{1}</color> needed to request this vehicle.");

    [TranslationData("Sent when a player tries to request a vehicle too soon after requesting a previous vehicle.", "Time left")]
    public readonly Translation<Cooldown> RequestVehicleCooldown = new Translation<Cooldown>("<#b3ab9f>This vehicle can't be requested for another: <#ffe2ab>{0}</color>.", arg0Fmt: Cooldown.FormatTimeShort);

    [TranslationData("Sent when a player tries to request a vehicle but they have to be a Squad Leader.")]
    public readonly Translation RequestVehicleNotSquadLeader = new Translation("<#b3ab9f>You must be a <#cedcde>SQUAD LEADER</color> in order to request this vehicle.");

    [TranslationData("Sent when a player tries to request a vehicle but they have to be in a Squad.")]
    public readonly Translation RequestVehicleNotInSquad = new Translation("<#b3ab9f>You must be <#cedcde>IN A SQUAD</color> in order to request this vehicle.");

    [TranslationData("Sent when a player tries to request a vehicle but they don't have a kit equipped.")]
    public readonly Translation RequestVehicleNoKit = new Translation("<#a8918a>Get a kit before you request vehicles.");

    [TranslationData("Sent when a player tries to request a vehicle that belongs to the other team.", "The other team")]
    public readonly Translation<FactionInfo> RequestVehicleOtherTeam = new Translation<FactionInfo>("<#a8918a>You must be on {0} to request this vehicle.", arg0Fmt: FactionInfo.FormatColorDisplayName);

    [TranslationData("Sent when a player tries to request a vehicle but needs another kit class.", "The name of the required class")]
    public readonly Translation<Class> RequestVehicleWrongClass = new Translation<Class>("<#b3ab9f>You need a <#cedcde><uppercase>{0}</uppercase></color> kit in order to request this vehicle.");

    [TranslationData("Sent when a player tries to request a vehicle but they're too low level.", "Name of the level needed", "Number of the level needed")]
    public readonly Translation<WarfareRank, WarfareRank> RequestVehicleMissingLevels = new Translation<WarfareRank, WarfareRank>("<#b3ab9f>You must be <#ffc29c>{0}</color> ({1}) to request this vehicle.", arg0Fmt: WarfareRank.FormatName, arg1Fmt: WarfareRank.FormatLPrefixedNumeric);

    [TranslationData("Sent when a player tries to request a vehicle but they're missing a completed quest.", "Name of the quest")]
    public readonly Translation<QuestAsset> RequestVehicleQuestIncomplete = new Translation<QuestAsset>("<#b3ab9f>You have to complete {0} to request this vehicle.", arg0Fmt: QuestTemplate.FormatColorQuestAsset);

    [TranslationData("Sent when a player tries to request a vehicle that's owned by another player.", "The other player")]
    public readonly Translation<IPlayer> RequestVehicleAlreadyRequested = new Translation<IPlayer>("<#a8918a>This vehicle was already requested by {0}.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData("Sent when a player tries to request a vehicle when they already have one nearby.", "The name of the existing vehicle")]
    public readonly Translation<InteractableVehicle> RequestVehicleAlreadyOwned = new Translation<InteractableVehicle>("<#a8918a>You already have a nearby {0}.", arg0Fmt: WarfareVehicleInfo.FormatNameColored);

    [TranslationData("Sent when a player tries to request a vehicle.", "The name of the vehicle")]
    public readonly Translation<WarfareVehicleInfo> RequestVehicleSuccess = new Translation<WarfareVehicleInfo>("<#b3a591>This {0} is now yours to take into battle.", arg0Fmt: WarfareVehicleInfo.FormatNameColored);

    [TranslationData("Sent when a player tries to request a vehicle that isn't in stock.", "The name of the vehicle")]
    public readonly Translation<WarfareVehicleInfo> RequestVehicleDead = new Translation<WarfareVehicleInfo>("<#b3a591>The {0} was destroyed and will be restocked soon.", arg0Fmt: WarfareVehicleInfo.FormatNameColored);

    [TranslationData("Sent when a player tries to request a vehicle but is permanently asset banned for all vehicles.")]
    public readonly Translation RequestVehicleAssetBannedGlobalPermanent = new Translation("<#b3a591>You are permanently banned from using all vehicles.");

    [TranslationData("Sent when a player tries to request a vehicle but is temporarily asset banned for all vehicles.", "Time left on asset ban")]
    public readonly Translation<TimeSpan> RequestVehicleAssetBannedGlobal = new Translation<TimeSpan>("<#b3a591>You are banned from using all vehicles for another {0}.", arg0Fmt: TimeAddon.Create(TimeSpanFormatType.Long));

    [TranslationData("Sent when a player tries to request a vehicle but is permanently asset banned for some vehicles.", "List of banned vehicles", "Time left on asset ban")]
    public readonly Translation<string> RequestVehicleAssetBannedPermanent = new Translation<string>("<#b3a591>You are permanently banned from using <#fff>{0}</color>.");

    [TranslationData("Sent when a player tries to request a vehicle but is temporarily asset banned for some vehicles.", "List of banned vehicles", "Time left on asset ban")]
    public readonly Translation<TimeSpan, string> RequestVehicleAssetBanned = new Translation<TimeSpan, string>("<#b3a591>You are banned from using <#fff>{1}</color> for another {0}.", arg0Fmt: TimeAddon.Create(TimeSpanFormatType.Long));

    #region Vehicle Request Delays
    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by a time delay.", "Time left")]
    public readonly Translation<TimeSpan> RequestVehicleTimeDelay = new Translation<TimeSpan>("<#b3ab9f>This vehicle is delayed for another: <#7094dd>{0}</color>.", arg0Fmt: TimeAddon.Create(TimeSpanFormatType.Long));

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 1 in Insurgency on attack.", "The cache that needs to be destroyed")]
    public readonly Translation<Cache> RequestVehicleCacheDelayAtk1 = new Translation<Cache>("<#b3ab9f>Destroy <color=#7094dd>{0}</color> to request this vehicle.", arg0Fmt: Flags.NameFormat);

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 1 in Insurgency on defense.", "The cache that needs to be lost")]
    public readonly Translation<Cache> RequestVehicleCacheDelayDef1 = new Translation<Cache>("<#b3ab9f>You can't request this vehicle until you lose <color=#7094dd>{0}</color>.", arg0Fmt: Flags.NameFormat);

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 1 in Insurgency on attack, but the cache is undiscovered")]
    public readonly Translation RequestVehicleCacheDelayAtkUndiscovered1 = new Translation("<#b3ab9f><color=#7094dd>Discover and Destroy</color> the next cache to request this vehicle.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 1 in Insurgency on defense, but the cache is undiscovered")]
    public readonly Translation RequestVehicleCacheDelayDefUndiscovered1 = new Translation("<#b3ab9f>You can't request this vehicle until you've <color=#7094dd>uncovered and lost</color> your next cache.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 2+ in Insurgency on attack.", "Number of caches that need to be destroyed")]
    public readonly Translation<int> RequestVehicleCacheDelayMultipleAtk = new Translation<int>("<#b3ab9f>Destroy <#7094dd>{0} more caches</color> to request this vehicle.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 2+ in Insurgency on defense.", "Number of caches that need to be lost")]
    public readonly Translation<int> RequestVehicleCacheDelayMultipleDef = new Translation<int>("<#b3ab9f>You can't request this vehicle until you've lost <#7094dd>{0} more caches</color>.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 1 in a flag gamemode.", "Flag that needs captured")]
    public readonly Translation<IDeployable> RequestVehicleFlagDelay1 = new Translation<IDeployable>("<#b3ab9f>Capture {0} to request this vehicle.", TranslationOptions.PerTeamTranslation, Flags.ColorNameDiscoverFormat);

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 1 in a flag gamemode.", "Flag that needs lost")]
    public readonly Translation<IDeployable> RequestVehicleLoseFlagDelay1 = new Translation<IDeployable>("<#b3ab9f>You can't request this vehicle until you lose {0}.", TranslationOptions.PerTeamTranslation, Flags.ColorNameDiscoverFormat);

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 2+ in a flag gamemode.", "Number of flags that need to be captured")]
    public readonly Translation<int> RequestVehicleFlagDelayMultiple = new Translation<int>("<#b3ab9f>Capture <#7094dd>{0} more flags</color> to request this vehicle.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 2+ in a flag gamemode.", "Number of flags that need to be lost")]
    public readonly Translation<int> RequestVehicleLoseFlagDelayMultiple = new Translation<int>("<#b3ab9f>You can't request this vehicle until you lose <#7094dd>{0} more flags</color>.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by a staging delay.")]
    public readonly Translation RequestVehicleStagingDelay = new Translation("<#a6918a>This vehicle can only be requested after the game starts.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an unknown/misconfigured delay.")]
    public readonly Translation<string> RequestVehicleUnknownDelay = new Translation<string>("<#b3ab9f>This vehicle is delayed because: <#7094dd>{0}</color>.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by a minimum teammate count delay.")]
    public readonly Translation<int> RequestVehicleTeammatesDelay = new Translation<int>("<#b3ab9f>This vehicle is delayed until <#7094dd>{0}v{0}</color> players online.");
    #endregion

    #region Trait Request Delays
    [TranslationData("Sent when a player tries to request a trait but it's delayed by a time delay.", "Time left")]
    public readonly Translation<TimeSpan> RequestTraitTimeDelay = new Translation<TimeSpan>("<#b3ab9f>This trait is delayed for another: <#7094dd>{0}</color>.", arg0Fmt: TimeAddon.Create(TimeSpanFormatType.Long));

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 1 in Insurgency on attack.", "The cache that needs to be destroyed")]
    public readonly Translation<Cache> RequestTraitCacheDelayAtk1 = new Translation<Cache>("<#b3ab9f>Destroy <color=#7094dd>{0}</color> to request this trait.", arg0Fmt: Flags.NameFormat);

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 1 in Insurgency on defense.", "The cache that needs to be lost")]
    public readonly Translation<Cache> RequestTraitCacheDelayDef1 = new Translation<Cache>("<#b3ab9f>You can't request this trait until you lose <color=#7094dd>{0}</color>.", arg0Fmt: Flags.NameFormat);

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 1 in Insurgency on attack, but the cache is undiscovered")]
    public readonly Translation RequestTraitCacheDelayAtkUndiscovered1 = new Translation("<#b3ab9f><color=#7094dd>Discover and Destroy</color> the next cache to request this trait.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 1 in Insurgency on defense, but the cache is undiscovered")]
    public readonly Translation RequestTraitCacheDelayDefUndiscovered1 = new Translation("<#b3ab9f>You can't request this trait until you've <color=#7094dd>uncovered and lost</color> your next cache.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 2+ in Insurgency on attack.", "Number of caches that need to be destroyed")]
    public readonly Translation<int> RequestTraitCacheDelayMultipleAtk = new Translation<int>("<#b3ab9f>Destroy <#7094dd>{0} more caches</color> to request this trait.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 2+ in Insurgency on defense.", "Number of caches that need to be lost")]
    public readonly Translation<int> RequestTraitCacheDelayMultipleDef = new Translation<int>("<#b3ab9f>You can't request this trait until you've lost <#7094dd>{0} more caches</color>.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 1 in a flag gamemode.", "Flag that needs captured")]
    public readonly Translation<IDeployable> RequestTraitFlagDelay1 = new Translation<IDeployable>("<#b3ab9f>Capture {0} to request this trait.", TranslationOptions.PerTeamTranslation, Flags.ColorNameDiscoverFormat);

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 1 in a flag gamemode.", "Flag that needs lost")]
    public readonly Translation<IDeployable> RequestTraitLoseFlagDelay1 = new Translation<IDeployable>("<#b3ab9f>You can't request this trait until you lose {0}.", TranslationOptions.PerTeamTranslation, Flags.ColorNameDiscoverFormat);

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 2+ in a flag gamemode.", "Number of flags that need to be captured")]
    public readonly Translation<int> RequestTraitFlagDelayMultiple = new Translation<int>("<#b3ab9f>Capture <#7094dd>{0} more flags</color> to request this trait.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 2+ in a flag gamemode.", "Number of flags that need to be lost")]
    public readonly Translation<int> RequestTraitLoseFlagDelayMultiple = new Translation<int>("<#b3ab9f>You can't request this trait until you lose <#7094dd>{0} more flags</color>.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by a staging delay.")]
    public readonly Translation RequestTraitStagingDelay = new Translation("<#a6918a>This trait can only be requested after the game starts.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an unknown/misconfigured delay.")]
    public readonly Translation<string> RequestTraitUnknownDelay = new Translation<string>("<#b3ab9f>This trait is delayed because: <#7094dd>{0}</color>.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by a minimum teammate count delay.")]
    public readonly Translation<int> RequestTraitTeammatesDelay = new Translation<int>("<#b3ab9f>This trait is delayed until <#7094dd>{0}v{0}</color> players online.");
    #endregion
}

public class RequestKitsTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Requests/Kits";

    [TranslationData("Sent when a player tries to request a kit that needs upgraded from a previous season.")]
    public readonly Translation NeedsUpgrade = new Translation("Upgrade required");

    [TranslationData("Sent when a player tries to request a kit that needs to be set up by an admin, or is in the process of being set up.")]
    public readonly Translation NeedsSetup = new Translation("Set up required by admin");

    [TranslationData("Sent when a player tries to request a kit that is disabled or out of date.")]
    public readonly Translation KitDisabled = new Translation("Kit disabled");

    [TranslationData("Sent when a player tries to request a kit that is disabled on the current map.")]
    public readonly Translation KitMapNotAllowed = new Translation("Kit not allowed on this map");

    [TranslationData("Sent when a player tries to request a kit that is disabled for their team.")]
    public readonly Translation KitTeamNotAllowed = new Translation("Kit not allowed for your team");

    [TranslationData("Sent when a player tries to request a kit but there is already someone in their squad who is using it.")]
    public readonly Translation RequestKitTakenInSquad = new Translation("Someone in your squad is already using this kit");
    
    [TranslationData("Sent when a player tries to request a kit but there aren't enough players in the squad to request it.")]
    public readonly Translation<int> RequestKitNotEnoughSquadMembers = new Translation<int>("This kit is only usable with {0} members in your squad");

    [TranslationData("Sent when a player tries to request a kit but there are too many players already using the same class of kit.")]
    public readonly Translation<int, Class> RequestKitClassLimited = new Translation<int, Class>("Too many players (<#d9e882>{0}</color>) on your team are using <#cedcde>{1}</color> kits", arg1Fmt: UppercaseAddon.Instance);

    [TranslationData("Sent when a player tries to request a kit but they have to be a Squad Leader.")]
    public readonly Translation RequestKitNotSquadleader = new Translation("You must be the leader of a squad in order to request a <#cedcde>SQUAD LEADER</color> kit");

    [TranslationData("Sent when a player tries to request a kit but it's already equipped.")]
    public readonly Translation AlreadyEquipped = new Translation("Kit already equipped. Use /ammo to refill your kit");

    [TranslationData("Sent when a player tries to request a kit but they're on a global cooldown.")]
    public readonly Translation<Cooldown> OnGlobalCooldown = new Translation<Cooldown>("On cooldown for another <#bafeff>{0}</color>", arg0Fmt: Cooldown.FormatTimeShort);

    [TranslationData("Sent when a player tries to request a premium kit but they're on a cooldown.")]
    public readonly Translation<Cooldown> OnCooldown = new Translation<Cooldown>("Premium kit on cooldown for another <#bafeff>{0}</color>", arg0Fmt: Cooldown.FormatTimeShort);

    [TranslationData("Sent when a player tries to request a premium kit that requires boosting in Discord but they aren't.")]
    public readonly Translation RequiresNitroBoost = new Translation("Requires <#e00ec9>NITRO BOOST</color> in <#7483c4>Discord</color>");
    
    [TranslationData("Sent when a player successfully purchases a kit.")]
    public readonly Translation<Kit, int> KitPurchaseSuccess = new Translation<Kit, int>("<#f3e2b4>You have successfully purchased kit <#ffffff>{0}</color> for <#b8ffc1>C</color> <#ffffff>{1}</color> credits.");

    
    [TranslationData("Modal heading for when a player is asked if they want to purchase a kit that they don't yet own.")]
    public readonly Translation ModalConfirmPurchaseKitHeading = new Translation("Purchase Kit");

    [TranslationData("Modal description for when a player is asked if they want to purchase a kit that they don't yet own.")]
    public readonly Translation<Kit, int> ModalConfirmPurchaseKitDescription = new Translation<Kit, int>("Purchase kit <#ffffff>{0}</color> for <#b8ffc1>C</color> <#ffffff>{1}</color> credits?");

    [TranslationData("Modal accept button text for when a player is asked if they want to purchase a kit that they don't yet own.")]
    public readonly Translation ModalConfirmPurchaseKitAcceptButton = new Translation("Confirm Purchase");

    [TranslationData("Modal cancel button text for when a player is asked if they want to purchase a kit that they don't yet own.")]
    public readonly Translation ModalConfirmPurchaseKitCancelButton = new Translation("Cancel");
}

public class RequestVehicleTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Requests/Vehicles";


    [TranslationData("Sent when a player tries to request a vehicle that isn't spawned.")]
    public readonly Translation NotAvailable = new Translation("Vehicle not ready");

    [TranslationData("Sent when a player tries to request a vehicle that is already owned.")]
    public readonly Translation AlreadyRequested = new Translation("Vehicle already owned");

    [TranslationData("Sent when a player tries to request a vehicle that is in another team's main base.")]
    public readonly Translation IncorrectTeam = new Translation("Vehicle not allowed for your team");

    [TranslationData("Sent when a player tries to request a vehicle that requires a specific class.")]
    public readonly Translation<Class> IncorrectKitClass = new Translation<Class>("You need a <#cedcde>{0}</color> kit in order to request this vehicle.", arg0Fmt: UppercaseAddon.Instance);

    [TranslationData("Sent when a player tries to request a vehicle but already owns one nearby.")]
    public readonly Translation<VehicleAsset> AnotherVehicleAlreadyOwned = new Translation<VehicleAsset>("<#cedcde>{0}</color> already requested", arg0Fmt: RarityColorAddon.Instance);

    [TranslationData("Sent when a player is asset banned over all vehicles permanently.")]
    public readonly Translation AssetBannedGlobalPermanent = new Translation("Permanently asset banned");

    [TranslationData("Sent when a player is asset banned over all vehicles for a set time.")]
    public readonly Translation<TimeSpan> AssetBannedGlobal = new Translation<TimeSpan>("Asset banned for another <#fff>{0}</color>", arg0Fmt: TimeAddon.Create(TimeSpanFormatType.Short));

    [TranslationData("Sent when a player is asset banned over all vehicles permanently.")]
    public readonly Translation<string> AssetBannedPermanent = new Translation<string>("Permanently asset banned from: <#ddd>{0}</color>");

    [TranslationData("Sent when a player is asset banned over all vehicles for a set time.")]
    public readonly Translation<string, TimeSpan> AssetBanned = new Translation<string, TimeSpan>("Asset banned from: <#ddd>{0}</color> for another <#fff>{1}</color>", arg1Fmt: TimeAddon.Create(TimeSpanFormatType.Short));
}