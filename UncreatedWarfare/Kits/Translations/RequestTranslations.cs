using System;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Ranks;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Kits.Translations;
public class RequestTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Requests";

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

    [TranslationData("Sent if a player tries to upgrade their loadout but they it's already up to date.", "The name of the kit they're trying to upgrade")]
    public readonly Translation<Kit> DoesNotNeedUpgrade = new Translation<Kit>("<#a4baa9><#ffebbd>{0}</color> does not need to be upgraded. If you're trying to update the kit and it was created during this season, open a help ticket.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent if an admin tries to unlock a kit that isn't locked.", "The name of the kit", IsPriorityTranslation = false)]
    public readonly Translation<Kit> DoesNotNeedUnlock = new Translation<Kit>("<#a4baa9><#ffebbd>{0}</color> does not need to be unlocked.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent if an admin tries to lock a kit that is already locked.", "The name of the kit", IsPriorityTranslation = false)]
    public readonly Translation<Kit> DoesNotNeedLock = new Translation<Kit>("<#a4baa9><#ffebbd>{0}</color> does not need to be locked.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent when a player tries to upgrade a kit that isn't a loadout.", "The name of the kit they're trying to upgrade")]
    public readonly Translation<Kit> RequestUpgradeOnKit = new Translation<Kit>("<#a4baa9><#ffebbd>{0}</color> can't be upgraded.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent when a player does /request upgrade on a loadout that was created in a previous season and hasn't been updated yet.", "The name of the kit they're upgrading")]
    public readonly Translation<Kit> TicketOpened = new Translation<Kit>("<#a8918a>An upgrade request was opened in your name for <#ffebbd>{0}</color>. Please fill it out as soon as possible.", arg0Fmt: Kit.FormatDisplayName);

    [TranslationData("Sent when a player requests a kit from a sign.", "The class of the kit they requested")]
    public readonly Translation<Class> RequestSignGiven = new Translation<Class>("<#a8918a>You have been allocated a <#cedcde>{0}</color> kit.", arg0Fmt: UppercaseAddon.Instance);

    [TranslationData("Sent when a player tries to request something but isn't looking at a sign or vehicle.")]
    public readonly Translation RequestNoTarget = new Translation("<#a4baa9>You must be looking at a request sign or vehicle.");
    
    [TranslationData("Sent when a player buys a kit using /buy.", "Number of credits spent")]
    public readonly Translation<int> RequestKitBought = new Translation<int>("<#c4a36a>Kit bought for <#c$credits$>C </color><#ffffff>{0}</color>. Request it with '<#b3b0ab>/request</color>'.");

    [TranslationData("Sent when a player tries to request a kit but the sign isn't linked to an existing kit.")]
    public readonly Translation RequestKitNotRegistered = new Translation("<#a8918a>This kit has not been created yet.");

    [TranslationData("Sent when a player tries to request a kit they already have.")]
    public readonly Translation RequestKitAlreadyOwned = new Translation("<#a8918a>You already have this kit. Type /ammo on an <#cedcde>AMMO CRATE</color> to restock your kit.");

    [TranslationData("Sent when a player tries to request a kit that needs upgraded from a previous season.")]
    public readonly Translation RequestKitNeedsUpgrade = new Translation("<#a8918a>This kit needs to be upgraded. Use <#fff>/request upgrade</color> to start a ticket.");

    [TranslationData("Sent when a player tries to request a kit that is currently being upgraded.")]
    public readonly Translation RequestKitNeedsSetup = new Translation("<#a8918a>This kit needs to be setup by an admin. Check your upgrade ticket.");

    [TranslationData("Sent when a player tries to request a kit that has been disabled by an admin.")]
    public readonly Translation RequestKitDisabled = new Translation("<#a8918a>This kit is disabled.");

    [TranslationData("Sent when a player tries to request a kit that is blacklisted on the current map.")]
    public readonly Translation RequestKitMapBlacklisted = new Translation("<#a8918a>This kit is not allowed on this map.");

    [TranslationData("Sent when a player tries to request a kit that is blacklisted for the player's team.")]
    public readonly Translation RequestKitFactionBlacklisted = new Translation("<#a8918a>Your team is not allowed to use this kit.");

    [TranslationData("Sent when a player tries to request a kit that they haven't been given access to, such as an elite kit.")]
    public readonly Translation RequestKitMissingAccess = new Translation("<#a8918a>You don't have access to this kit.");

    [TranslationData("Sent when a player tries to request a kit that requires Nitro boosting the Discord server.")]
    public readonly Translation RequestKitMissingNitro = new Translation("<#a8918a>You must be <#e00ec9>NITRO BOOSTING</color> to use this kit.");

    [TranslationData("Sent when a player tries to request a kit that requires purchasing.", "Total credits required")]
    public readonly Translation<int> RequestKitNotBought = new Translation<int>("<#99918d>Look at this sign and type '<#ffe2ab>/buy</color>' to unlock this kit permanently for <#c$credits$>C </color><#ffffff>{0}</color>.");

    [TranslationData("Sent when a player tries to request a kit that requires purchasing, but they can't afford the kit yet.", "Number of credits missing", "Total credits required")]
    public readonly Translation<int, int> RequestKitCantAfford = new Translation<int, int>("<#a8918a>You are missing <#c$credits$>C </color><#ffffff>{0}</color> / <#c$credits$>C </color><#ffffff>{1}</color> needed to unlock this kit.");

    [TranslationData("Sent when a player tries to request a kit that belongs to the other team.", "The team that owns the kit.")]
    public readonly Translation<FactionInfo> RequestKitWrongTeam = new Translation<FactionInfo>("<#a8918a>You must be part of {0} to request this kit.", arg0Fmt: FactionInfo.FormatShortName);

    [TranslationData("Sent when a player tries to buy a kit that's either free or not purchasable using in-game currency.")]
    public readonly Translation RequestNotBuyable = new Translation("<#a8918a>This kit cannot be purchased with credits.");

    [TranslationData("Sent when a player tries to request a kit but there are too many players already using it.")]
    public readonly Translation<int> RequestKitLimited = new Translation<int>("<#a8918a>Your team already has a max of <#d9e882>{0}</color> players using this kit. Try again later.");

    [TranslationData("Sent when a player tries to request a kit but they're too low level.", "Name of the level needed", "Number of the level needed")]
    public readonly Translation<LevelData, LevelData> RequestKitLowLevel = new Translation<LevelData, LevelData>("<#b3ab9f>You must be <#ffc29c>{0}</color> (L {1}) to use this kit.", arg0Fmt: LevelData.FormatName, arg1Fmt: LevelData.FormatNumeric);

    [TranslationData("Sent when a player tries to request a kit but they're too low rank.", "Name of the rank needed", IsPriorityTranslation = false)]
    public readonly Translation<RankData> RequestKitLowRank = new Translation<RankData>("<#b3ab9f>You must be {0} to use this kit.", arg0Fmt: RankData.FormatColorName);

    [TranslationData("Sent when a player tries to request a kit but they're missing a completed quest.", "Name of the quest")]
    public readonly Translation<QuestAsset> RequestKitQuestIncomplete = new Translation<QuestAsset>("<#b3ab9f>You have to complete {0} to request this kit.", arg0Fmt: BaseQuestData.FormatColorQuestAsset);

    [TranslationData("Sent when a player tries to request a kit but they have to be a Squad Leader.")]
    public readonly Translation RequestKitNotSquadleader = new Translation("<#b3ab9f>You must be a <#cedcde>SQUAD LEADER</color> in order to get this kit.");

    [TranslationData("Sent when a player tries to request a loadout from an empty loadout sign.")]
    public readonly Translation RequestLoadoutNotOwned = new Translation("<#a8918a>You do not own this loadout.");

    [TranslationData("Sent when a player tries to request a vehicle but they don't have enough credits.", "Number of credits missing", "Total credits required")]
    public readonly Translation<int, int> RequestVehicleCantAfford = new Translation<int, int>("<#a8918a>You are missing <#c$credits$>C </color><#ffffff>{0}</color> / <#c$credits$>C </color><#ffffff>{1}</color> needed to request this vehicle.");
    
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
    public readonly Translation<LevelData, LevelData> RequestVehicleMissingLevels = new Translation<LevelData, LevelData>("<#b3ab9f>You must be <#ffc29c>{0}</color> (L {1}) to request this vehicle.", arg0Fmt: LevelData.FormatName, arg1Fmt: LevelData.FormatNumeric);

    [TranslationData("Sent when a player tries to request a vehicle but they're too low rank.", "Name of the rank needed", IsPriorityTranslation = false)]
    public readonly Translation<RankData> RequestVehicleRankIncomplete = new Translation<RankData>("<#b3ab9f>You must be {0} to request this vehicle.", arg0Fmt: RankData.FormatColorName);

    [TranslationData("Sent when a player tries to request a vehicle but they're missing a completed quest.", "Name of the quest")]
    public readonly Translation<QuestAsset> RequestVehicleQuestIncomplete = new Translation<QuestAsset>("<#b3ab9f>You have to complete {0} to request this vehicle.", arg0Fmt: BaseQuestData.FormatColorQuestAsset);

    [TranslationData("Sent when a player tries to request a vehicle that's owned by another player.", "The other player")]
    public readonly Translation<IPlayer> RequestVehicleAlreadyRequested = new Translation<IPlayer>("<#a8918a>This vehicle was already requested by {0}.", arg0Fmt: UCPlayer.FormatColoredCharacterName);

    [TranslationData("Sent when a player tries to request a vehicle when they already have one nearby.", "The name of the existing vehicle")]
    public readonly Translation<InteractableVehicle> RequestVehicleAlreadyOwned = new Translation<InteractableVehicle>("<#a8918a>You already have a nearby {0}.", arg0Fmt: VehicleData.COLORED_NAME);

    [TranslationData("Sent when a player tries to request a vehicle.", "The name of the vehicle")]
    public readonly Translation<VehicleData> RequestVehicleSuccess = new Translation<VehicleData>("<#b3a591>This {0} is now yours to take into battle.", arg0Fmt: VehicleData.COLORED_NAME);

    [TranslationData("Sent when a player tries to request a vehicle that isn't in stock.", "The name of the vehicle")]
    public readonly Translation<VehicleData> RequestVehicleDead = new Translation<VehicleData>("<#b3a591>The {0} was destroyed and will be restocked soon.", arg0Fmt: VehicleData.COLORED_NAME);

    [TranslationData("Sent when a player tries to request a vehicle but is permanently asset banned for all vehicles.")]
    public readonly Translation RequestVehicleAssetBannedGlobalPermanent = new Translation("<#b3a591>You are permanently banned from using all vehicles.");

    [TranslationData("Sent when a player tries to request a vehicle but is temporarily asset banned for all vehicles.", "Time left on asset ban")]
    public readonly Translation<TimeSpan> RequestVehicleAssetBannedGlobal = new Translation<TimeSpan>("<#b3a591>You are banned from using all vehicles for another {0}.", arg0Fmt: FormatTimeLong);

    [TranslationData("Sent when a player tries to request a vehicle but is permanently asset banned for some vehicles.", "List of banned vehicles", "Time left on asset ban")]
    public readonly Translation<string> RequestVehicleAssetBannedPermanent = new Translation<string>("<#b3a591>You are permanently banned from using <#fff>{0}</color>.");

    [TranslationData("Sent when a player tries to request a vehicle but is temporarily asset banned for some vehicles.", "List of banned vehicles", "Time left on asset ban")]
    public readonly Translation<TimeSpan, string> RequestVehicleAssetBanned = new Translation<TimeSpan, string>("<#b3a591>You are banned from using <#fff>{1}</color> for another {0}.", arg0Fmt: FormatTimeLong);

    #region Vehicle Request Delays
    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by a time delay.", "Time left")]
    public readonly Translation<TimeSpan> RequestVehicleTimeDelay = new Translation<TimeSpan>("<#b3ab9f>This vehicle is delayed for another: <#c$vbs_delay$>{0}</color>.", arg0Fmt: FormatTimeLong);

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 1 in Insurgency on attack.", "The cache that needs to be destroyed")]
    public readonly Translation<Cache> RequestVehicleCacheDelayAtk1 = new Translation<Cache>("<#b3ab9f>Destroy <color=#c$vbs_delay$>{0}</color> to request this vehicle.", arg0Fmt: FOB.FormatName);

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 1 in Insurgency on defense.", "The cache that needs to be lost")]
    public readonly Translation<Cache> RequestVehicleCacheDelayDef1 = new Translation<Cache>("<#b3ab9f>You can't request this vehicle until you lose <color=#c$vbs_delay$>{0}</color>.", arg0Fmt: FOB.FormatName);

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 1 in Insurgency on attack, but the cache is undiscovered")]
    public readonly Translation RequestVehicleCacheDelayAtkUndiscovered1 = new Translation("<#b3ab9f><color=#c$vbs_delay$>Discover and Destroy</color> the next cache to request this vehicle.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 1 in Insurgency on defense, but the cache is undiscovered")]
    public readonly Translation RequestVehicleCacheDelayDefUndiscovered1 = new Translation("<#b3ab9f>You can't request this vehicle until you've <color=#c$vbs_delay$>uncovered and lost</color> your next cache.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 2+ in Insurgency on attack.", "Number of caches that need to be destroyed")]
    public readonly Translation<int> RequestVehicleCacheDelayMultipleAtk = new Translation<int>("<#b3ab9f>Destroy <#c$vbs_delay$>{0} more caches</color> to request this vehicle.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 2+ in Insurgency on defense.", "Number of caches that need to be lost")]
    public readonly Translation<int> RequestVehicleCacheDelayMultipleDef = new Translation<int>("<#b3ab9f>You can't request this vehicle until you've lost <#c$vbs_delay$>{0} more caches</color>.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 1 in a flag gamemode.", "Flag that needs captured")]
    public readonly Translation<IDeployable> RequestVehicleFlagDelay1 = new Translation<IDeployable>("<#b3ab9f>Capture {0} to request this vehicle.", TranslationOptions.PerTeamTranslation, Flag.COLOR_NAME_DISCOVER_FORMAT);

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 1 in a flag gamemode.", "Flag that needs lost")]
    public readonly Translation<IDeployable> RequestVehicleLoseFlagDelay1 = new Translation<IDeployable>("<#b3ab9f>You can't request this vehicle until you lose {0}.", TranslationOptions.PerTeamTranslation, Flag.COLOR_NAME_DISCOVER_FORMAT);

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 2+ in a flag gamemode.", "Number of flags that need to be captured")]
    public readonly Translation<int> RequestVehicleFlagDelayMultiple = new Translation<int>("<#b3ab9f>Capture <#c$vbs_delay$>{0} more flags</color> to request this vehicle.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an objective delay of 2+ in a flag gamemode.", "Number of flags that need to be lost")]
    public readonly Translation<int> RequestVehicleLoseFlagDelayMultiple = new Translation<int>("<#b3ab9f>You can't request this vehicle until you lose <#c$vbs_delay$>{0} more flags</color>.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by a staging delay.")]
    public readonly Translation RequestVehicleStagingDelay = new Translation("<#a6918a>This vehicle can only be requested after the game starts.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by an unknown/misconfigured delay.")]
    public readonly Translation<string> RequestVehicleUnknownDelay = new Translation<string>("<#b3ab9f>This vehicle is delayed because: <#c$vbs_delay$>{0}</color>.");

    [TranslationData("Sent when a player tries to request a vehicle but it's delayed by a minimum teammate count delay.")]
    public readonly Translation<int> RequestVehicleTeammatesDelay = new Translation<int>("<#b3ab9f>This vehicle is delayed until <#c$vbs_delay$>{0}v{0}</color> players online.");
    #endregion

    #region Trait Request Delays
    [TranslationData("Sent when a player tries to request a trait but it's delayed by a time delay.", "Time left")]
    public readonly Translation<TimeSpan> RequestTraitTimeDelay = new Translation<TimeSpan>("<#b3ab9f>This trait is delayed for another: <#c$vbs_delay$>{0}</color>.", arg0Fmt: FormatTimeLong);

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 1 in Insurgency on attack.", "The cache that needs to be destroyed")]
    public readonly Translation<Cache> RequestTraitCacheDelayAtk1 = new Translation<Cache>("<#b3ab9f>Destroy <color=#c$vbs_delay$>{0}</color> to request this trait.", arg0Fmt: FOB.FormatName);

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 1 in Insurgency on defense.", "The cache that needs to be lost")]
    public readonly Translation<Cache> RequestTraitCacheDelayDef1 = new Translation<Cache>("<#b3ab9f>You can't request this trait until you lose <color=#c$vbs_delay$>{0}</color>.", arg0Fmt: FOB.FormatName);

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 1 in Insurgency on attack, but the cache is undiscovered")]
    public readonly Translation RequestTraitCacheDelayAtkUndiscovered1 = new Translation("<#b3ab9f><color=#c$vbs_delay$>Discover and Destroy</color> the next cache to request this trait.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 1 in Insurgency on defense, but the cache is undiscovered")]
    public readonly Translation RequestTraitCacheDelayDefUndiscovered1 = new Translation("<#b3ab9f>You can't request this trait until you've <color=#c$vbs_delay$>uncovered and lost</color> your next cache.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 2+ in Insurgency on attack.", "Number of caches that need to be destroyed")]
    public readonly Translation<int> RequestTraitCacheDelayMultipleAtk = new Translation<int>("<#b3ab9f>Destroy <#c$vbs_delay$>{0} more caches</color> to request this trait.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 2+ in Insurgency on defense.", "Number of caches that need to be lost")]
    public readonly Translation<int> RequestTraitCacheDelayMultipleDef = new Translation<int>("<#b3ab9f>You can't request this trait until you've lost <#c$vbs_delay$>{0} more caches</color>.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 1 in a flag gamemode.", "Flag that needs captured")]
    public readonly Translation<IDeployable> RequestTraitFlagDelay1 = new Translation<IDeployable>("<#b3ab9f>Capture {0} to request this trait.", TranslationOptions.PerTeamTranslation, Flag.COLOR_NAME_DISCOVER_FORMAT);

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 1 in a flag gamemode.", "Flag that needs lost")]
    public readonly Translation<IDeployable> RequestTraitLoseFlagDelay1 = new Translation<IDeployable>("<#b3ab9f>You can't request this trait until you lose {0}.", TranslationOptions.PerTeamTranslation, Flag.COLOR_NAME_DISCOVER_FORMAT);

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 2+ in a flag gamemode.", "Number of flags that need to be captured")]
    public readonly Translation<int> RequestTraitFlagDelayMultiple = new Translation<int>("<#b3ab9f>Capture <#c$vbs_delay$>{0} more flags</color> to request this trait.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an objective delay of 2+ in a flag gamemode.", "Number of flags that need to be lost")]
    public readonly Translation<int> RequestTraitLoseFlagDelayMultiple = new Translation<int>("<#b3ab9f>You can't request this trait until you lose <#c$vbs_delay$>{0} more flags</color>.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by a staging delay.")]
    public readonly Translation RequestTraitStagingDelay = new Translation("<#a6918a>This trait can only be requested after the game starts.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by an unknown/misconfigured delay.")]
    public readonly Translation<string> RequestTraitUnknownDelay = new Translation<string>("<#b3ab9f>This trait is delayed because: <#c$vbs_delay$>{0}</color>.");

    [TranslationData("Sent when a player tries to request a trait but it's delayed by a minimum teammate count delay.")]
    public readonly Translation<int> RequestTraitTeammatesDelay = new Translation<int>("<#b3ab9f>This trait is delayed until <#c$vbs_delay$>{0}v{0}</color> players online.");
    #endregion
}
