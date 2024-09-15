using System;
using System.Collections.Generic;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Ranks;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Zones;
using Cache = Uncreated.Warfare.Components.Cache;

namespace Uncreated.Warfare;
internal static class T
{
    /*
     * Extra Notation
     *
     *
     * Arguments
     *
     * Zero based, surrounded in curly brackets.
     *  Example (Translation<int, ItemAsset>): Given you {0}x {1}.
     *   -> Given you 4x M4A1.
     *
     *
     * Formatting
     *
     * Premade Formatting (Constants):
     *  • FormatPlural              "$plural$"  See Below
     *  • FormatUppercase           "upper"     Turns the argument UPPERCASE.
     *  • FormatLowercase           "lower"     Turns the argument lowercase.
     *  • FormatPropercase          "proper"    Turns the argument ProperCase.
     *  • FormatTimeLong            "tlong"     Turns time to: 3 minutes and 4 seconds, etc.
     *  • FormatTimeShort_MM_SS     "tshort1"   Turns time to: 03:04, etc.
     *  • FormatTimeShort_HH_MM_SS  "tshort2"   Turns time to: 01:03:04, etc.
     *   + Time can be int, uint, float (all in seconds), or TimeSpan
     *  • FormatRarityColor         "rarity"    Colors assets to their rarity color.
     *
     * Other formats are stored in the most prominant class of the interface (UCPlayer for IPlayer, FOB for IDeployable, etc.)
     * Anything that would work in type.ToString(string, IFormatProvider) will work here.
     *
     *
     * Color substitution from color dictionary.
     *
     * "c$value$" will be replaced by the color "value" from the color dictionary on startup.
     *  Example: You need 100 more <#c$credits$>credits</color>.
     *
     *
     * Conditional pluralization of existing terms.
     *
     * "${p:arg:text}"  will replace text with the plural version of text if {arg} is not one.
     * "${p:arg:text!}" will replace text with the plural version of text if {arg} is one.
     *  Example: There ${p:0:is} {0} ${p:0:apple}, ${p:0:it} ${p:0:is} ${p:0:a }${p:0:fruit}. ${p:0:It} ${p:0:taste!} good.
     *   -> ({0} = 1) There is 1 apple, it is a fruit. It tastes good.
     *   -> ({0} = 3) There are 3 apples, they are fruits. They taste good.
     *
     *
     * Conditional pluralization of argument values.
     *
     * Using the format: "'xxxx' + FormatPlural" will replace the value for that argument with the plural version.
     *  Example: You cant place {1} here. arg1Fmt: RarityFormat + FormatPlural
     *   -> You can't place <#xxxxx>FOB Radios</color> here.
     *
     * Using the format: "'xxxx' + FormatPlural + '{arg}'" will replace the value for that argument with the plural version if {arg} is not one.
     *  Example: There are {0} {1} already on this FOB. arg1Fmt: RarityFormat + FormatPlural + "{0}"
     *   -> (4, FOB Radio Asset) There are 4 <#xxxxx>FOB Radios</color> already on this FOB.
     */

    #region Common Errors
    private const string SectionCommonErrors = "Common Errors";

    [TranslationData(SectionCommonErrors, "Sent when a command is not used correctly.", "Command usage.")]
    public static readonly Translation<string> CorrectUsage = new Translation<string>("<#ff8c69>Correct usage: {0}.");

    [TranslationData(SectionCommonErrors, "A command or feature hasn't been completed or implemented.")]
    public static readonly Translation NotImplemented = new Translation("<#ff8c69>This command hasn't been implemented yet.");

    [TranslationData(SectionCommonErrors, "A player ran an unknown command.")]
    public static readonly Translation UnknownCommand = new Translation("<#ff8c69>Unknown command. <#b3ffb3>Type <#fff>/help</color> to learn more.");

    [TranslationData(SectionCommonErrors, "A command or feature can only be used by the server console.")]
    public static readonly Translation ConsoleOnly = new Translation("<#ff8c69>This command can only be called from console.");

    [TranslationData(SectionCommonErrors, "A command or feature can only be used by a player (instead of the server console).")]
    public static readonly Translation PlayersOnly = new Translation("<#ff8c69>This command can not be called from console.");

    [TranslationData(SectionCommonErrors, "A command or feature is on cooldown.")]
    public static readonly Translation<Cooldown, string> CommandCooldown = new Translation<Cooldown, string>("<#ff8c69>You can't use <#fff>/{1}</color> for another <#aaa>{0}</color>.", arg0Fmt: Cooldown.FormatTimeLong, arg1Fmt: FormatLowercase);

    [TranslationData(SectionCommonErrors, "A player name or ID search turned up no results.")]
    public static readonly Translation PlayerNotFound = new Translation("<#ff8c69>Player not found.");

    [TranslationData(SectionCommonErrors, "A command didn't respond to an interaction, or a command chose to throw a vague error response to an uncommon problem.")]
    public static readonly Translation UnknownError = new Translation("<#ff8c69>We ran into an unknown error executing that command.");

    [TranslationData(SectionCommonErrors, "A vanilla command didn't print a response.")]
    public static readonly Translation VanillaCommandDidNotRespond = new Translation("<#d09595>The vanilla command you ran didn't print a response.");

    [TranslationData(SectionCommonErrors, "An async command was cancelled mid-execution.")]
    public static readonly Translation ErrorCommandCancelled = new Translation("<#ff8c69>This command was cancelled during it's execution. This could be caused by the game ending or a bug.");

    [TranslationData(SectionCommonErrors, "A command is disabled in the current gamemode type (ex, /deploy in a gamemode without FOBs).")]
    public static readonly Translation GamemodeError = new Translation("<#ff8c69>This command is not enabled in this gamemode.");

    [TranslationData(SectionCommonErrors, "The caller of a command is not allowed to use the command.")]
    public static readonly Translation NoPermissions = new Translation("<#ff8c69>You do not have permission to use this command.");

    [TranslationData(SectionCommonErrors, "The caller of a command is not allowed to use the command.")]
    public static readonly Translation<PermissionLeaf> NoPermissionsSpecific = new Translation<PermissionLeaf>("<#ff8c69>You do not have the permission {0} to use this command.");

    [TranslationData(SectionCommonErrors, "A command or feature is turned off in the configuration or for the current layout.")]
    public static readonly Translation NotEnabled = new Translation("<#ff8c69>This feature is not currently enabled.");

    [TranslationData(SectionCommonErrors, "The caller of a command has permission to use the command but isn't on duty.")]
    public static readonly Translation NotOnDuty = new Translation("<#ff8c69>You must be on duty to execute that command.");

    [TranslationData(SectionCommonErrors, "The value of a parameter was not in a valid time span format.", "Inputted text.")]
    public static readonly Translation<string> InvalidTime = new Translation<string>("<#ff8c69><#d09595>{0}</color> should be in a valid <#cedcde>TIME SPAN</color> format. Example: <#d09595>10d12h</color>, <#d09595>4mo15d12h</color>, <#d09595>2y</color>, <#d09595>permanent</color>.", arg0Fmt: UCPlayer.FormatCharacterName);
    #endregion

    #region Flags
    private const string SectionFlags = "Flags";

    [TranslationData(SectionFlags, "The caller of a command isn't on team 1 or 2.")]
    public static readonly Translation NotOnCaptureTeam = new Translation("<#ff8c69>You're not on a valid team.");

    [TranslationData(SectionFlags, "Sent when the player enters the capture radius of an active flag.", "Objective in question")]
    public static readonly Translation<Flag> EnteredCaptureRadius = new Translation<Flag>("<#e6e3d5>You have entered the capture radius of {0}.", arg0Fmt: Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent when the player leaves the capture radius of an active flag.", "Objective in question")]
    public static readonly Translation<Flag> LeftCaptureRadius = new Translation<Flag>("<#ff8c69>You have left the capture radius of {0}.", arg0Fmt: Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to all players on a flag that's being captured by their team (from neutral).", "Objective in question")]
    public static readonly Translation<Flag> FlagCapturing = new Translation<Flag>("<#e6e3d5>Your team is capturing {0}!", arg0Fmt: Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to all players on a flag that's being captured by the other team.", "Objective in question")]
    public static readonly Translation<Flag> FlagLosing = new Translation<Flag>("<#ff8c69>Your team is losing {0}!", arg0Fmt: Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to all players on a flag when it begins being contested.", "Objective in question")]
    public static readonly Translation<Flag> FlagContested = new Translation<Flag>("<#c$contested$>{0} is contested, eliminate some enemies to secure it!", arg0Fmt: Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to all players on a flag that's being cleared by their team (from the other team's ownership).", "Objective in question")]
    public static readonly Translation<Flag> FlagClearing = new Translation<Flag>("<#e6e3d5>Your team is clearing {0}!", arg0Fmt: Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to all players on a flag when it gets secured by their team.", "Objective in question")]
    public static readonly Translation<Flag> FlagSecured = new Translation<Flag>("<#c$secured$>{0} is secure for now, keep up the defense.", arg0Fmt: Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to a player that walks in the radius of a flag that isn't their team's objective.", "Objective in question")]
    public static readonly Translation<Flag> FlagNoCap = new Translation<Flag>("<#c$nocap$>{0} is not your objective, check the right of your screen to see which points to attack and defend.", arg0Fmt: Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to a player that walks in the radius of a flag that is owned by the other team and enough of the other team is on the flag so they can't contest the point.", "Objective in question")]
    public static readonly Translation<Flag> FlagNotOwned = new Translation<Flag>("<#c$nocap$>{0} is owned by the enemies. Get more players to capture it.", arg0Fmt: Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to a player that walks in the radius of a flag that is owned by the other team and has been locked from recapture.", "Objective in question")]
    public static readonly Translation<Flag> FlagLocked = new Translation<Flag>("<#c$locked$>{0} has already been captured, try to protect the objective to win.", arg0Fmt: Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to all players when a flag gets neutralized.", "Objective in question")]
    public static readonly Translation<Flag> FlagNeutralized = new Translation<Flag>("<#e6e3d5>{0} has been neutralized!", TranslationOptions.PerTeamTranslation, Flag.COLOR_NAME_DISCOVER_FORMAT);

    [TranslationData(SectionFlags, "Gets broadcasted when a team captures a flag.")]
    public static readonly Translation<FactionInfo, Flag> TeamCaptured = new Translation<FactionInfo, Flag>("<#a0ad8e>{0} captured {1}.", TranslationOptions.PerTeamTranslation, FactionInfo.FormatColorDisplayName, Flag.COLOR_NAME_DISCOVER_FORMAT);

    [TranslationData(SectionFlags, "Backup translation for team 0 name and short name.")]
    public static readonly Translation Neutral = new Translation("Neutral", TranslationOptions.UnityUI);

    [TranslationData(SectionFlags, "Shows in place of the objective name for an undiscovered flag or objective.")]
    public static readonly Translation UndiscoveredFlag = new Translation("<color=#c$undiscovered_flag$>unknown</color>", TranslationOptions.UnityUI);

    [TranslationData(SectionFlags, "Shows in place of the objective name for an undiscovered flag or objective.")]
    public static readonly Translation UndiscoveredFlagNoColor = new Translation("unknown");
    
    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's team is capturing a flag they're on.")]
    public static readonly Translation UICapturing = new Translation("CAPTURING",     TranslationOptions.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's team is losing a flag they're on because there isn't enough of them to contest it.")]
    public static readonly Translation UILosing = new Translation("LOSING", TranslationOptions.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's team is clearing a flag they're on.")]
    public static readonly Translation UIClearing = new Translation("CLEARING", TranslationOptions.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's team is contested with the other team on the flag they're on.")]
    public static readonly Translation UIContested = new Translation("CONTESTED", TranslationOptions.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's team owns flag they're on.")]
    public static readonly Translation UISecured = new Translation("SECURED", TranslationOptions.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's on a flag that isn't their team's objective.")]
    public static readonly Translation UINoCap = new Translation("NOT OBJECTIVE", TranslationOptions.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's team has too few people on a flag to contest and the other team owns the flag.")]
    public static readonly Translation UINotOwned = new Translation("TAKEN", TranslationOptions.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the objective they're on is owned by the other team and is locked from recapture.")]
    public static readonly Translation UILocked = new Translation("LOCKED", TranslationOptions.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's in a vehicle on their objective.")]
    public static readonly Translation UIInVehicle = new Translation("IN VEHICLE", TranslationOptions.UnityUI);

    [TranslationData(SectionFlags, "Shows above the flag list UI.")]
    public static readonly Translation FlagsHeader = new Translation("Flags", TranslationOptions.UnityUI);

    [TranslationData(SectionFlags, "Shows above the cache list UI.")]
    public static readonly Translation CachesHeader = new Translation("Caches", TranslationOptions.UnityUI);
    #endregion

    #region Zones
    private const string SectionZones = "Zones";

    [TranslationData(SectionZones, Parameters = [ "X (m)", "Y (m)", "Z (m)", "Yaw (°)"])]
    public static readonly Translation<float, float, float, float> ZoneUtilLocation = new Translation<float, float, float, float>("<#e6e3d5>Location: {0}, {1}, {2} | Yaw: {3}°.", arg0Fmt: "0.##", arg1Fmt: "0.##", arg2Fmt: "0.##", arg3Fmt: "0.##");

    [TranslationData(SectionZones)]
    public static readonly Translation ZoneNoResultsLocation = new Translation("<#ff8c69>You aren't in any existing zone.");

    [TranslationData(SectionZones)]
    public static readonly Translation ZoneNoResultsName = new Translation("<#ff8c69>Couldn't find a zone by that name.");

    [TranslationData(SectionZones)]
    public static readonly Translation ZoneNoResults = new Translation("<#ff8c69>You must be in a zone or specify a valid zone name to use this command.");

    [TranslationData(SectionZones)]
    public static readonly Translation<int, Zone> ZoneVisualizeSuccess = new Translation<int, Zone>("<#e6e3d5>Spawned {0} particles around <color=#cedcde>{1}</color>.", arg1Fmt: Flag.NAME_FORMAT);

    [TranslationData(SectionZones, IsPriorityTranslation = false)]
    public static readonly Translation<Zone> ZoneGoSuccess = new Translation<Zone>("<#e6e3d5>Teleported to <#5a6e5c>{0}</color>.", arg0Fmt: Flag.NAME_FORMAT);

    [TranslationData(SectionZones, IsPriorityTranslation = false)]
    public static readonly Translation<string> ZoneGoSuccessRaw = new Translation<string>("<#e6e3d5>Teleported to <#cedcde>{0}</color>.");

    [TranslationData(SectionZones, IsPriorityTranslation = false)]
    public static readonly Translation<GridLocation> ZoneGoSuccessGridLocation = new Translation<GridLocation>("<#e6e3d5>Teleported to <#ff8c69>{0}</color>.", arg0Fmt: Flag.NAME_FORMAT);
    #endregion

    #region Players
    private const string SectionPlayers = "Players";

    [TranslationData(SectionPlayers, "Gets sent to a player who's discord is not linked to their steam account (part 1).")]
    public static readonly Translation DiscordNotLinked = new Translation("<#9cffb3>Your account must be linked in our Discord server to use this command.");

    [TranslationData(SectionPlayers, "Gets sent to a player who's discord is not linked to their steam account (part 2).", "Player's Steam64 ID")]
    public static readonly Translation<IPlayer> DiscordNotLinked2 = new Translation<IPlayer>("<#9cffb3>Type <#7483c4>/discord</color> then type <#fff>/link {0}</color> in <#c480d9>#warfare-stats</color>.", arg0Fmt: UCPlayer.FormatColoredSteam64);

    [TranslationData(SectionPlayers, "Gets broadcasted when a player connects.", "Connecting player")]
    public static readonly Translation<IPlayer> PlayerConnected = new Translation<IPlayer>("<#e6e3d5>{0} joined the server.");

    [TranslationData(SectionPlayers, "Gets broadcasted when a player disconnects.", "Disconnecting player")]
    public static readonly Translation<IPlayer> PlayerDisconnected = new Translation<IPlayer>("<#e6e3d5>{0} left the server.");

    [TranslationData(SectionPlayers, "Kick message for a player that suffers from a rare bug which will cause GameObject.get_transform() to throw a NullReferenceException (not return null). They are kicked if this happens.", "Discord Join Code")]
    public static readonly Translation<string> NullTransformKickMessage = new Translation<string>("Your character is bugged, which messes up our zone plugin. Rejoin or contact a Director if this continues. (discord.gg/{0}).");

    [TranslationData(SectionPlayers, "Gets sent to a player when their message gets blocked by the chat filter.", "Section of the message that matched the chat filter.")]
    public static readonly Translation<string> ChatFilterFeedback = new Translation<string>("<#ff8c69>Our chat filter flagged <#fdfdfd>{0}</color>, so the message wasn't sent.");
    
    [TranslationData(SectionPlayers, "Gets sent to a player when their message gets blocked by the chat filter.", "Amount of alphanumeric characters in succession.")]
    public static readonly Translation<int> NameFilterKickMessage = new Translation<int>("Your name does not contain enough alphanumeric characters in succession ({0}), please change your name and rejoin.");
    
    [TranslationData(SectionPlayers, "Gets sent to a player if they're banned when they join.")]
    public static readonly Translation<string, int> RejectBanned = new Translation<string, int>("You are banned for {1}: \"{0}\".", arg1Fmt: TimeAddon.Create(TimeFormatType.Short));

    [TranslationData(SectionPlayers, "Gets sent to a player if they're permanently banned when they join.")]
    public static readonly Translation<string> RejectPermanentBanned = new Translation<string>("You are permanently banned: \"{0}\".");
    
    [TranslationData(SectionPlayers, "Gets sent to a player when their nick name gets blocked by the chat filter.", "Violating text.")]
    public static readonly Translation<string> NameProfanityNickNameKickMessage = new Translation<string>("Your nickname is in violation of our profanity filter: \"{0}\". Please change your name and rejoin.");
    
    [TranslationData(SectionPlayers, "Gets sent to a player when their character name gets blocked by the chat filter.", "Violating text.")]
    public static readonly Translation<string> NameProfanityCharacterNameKickMessage = new Translation<string>("Your character name is in violation of our profanity filter: \"{0}\". Please change your name and rejoin.");
    
    [TranslationData(SectionPlayers, "Gets sent to a player when their player name gets blocked by the chat filter.", "Violating text.")]
    public static readonly Translation<string> NameProfanityPlayerNameKickMessage = new Translation<string>("Your Steam name is in violation of our profanity filter: \"{0}\". Please change your name and rejoin.");
    
    [TranslationData(SectionPlayers, "Gets sent to a player who is attempting to main camp the other team.")]
    public static readonly Translation AntiMainCampWarning = new Translation("<#fa9e9e>Stop <b><#ff3300>main-camping</color></b>! Damage is <b>reversed</b> back on you.");
    
    [TranslationData(SectionPlayers, "Gets sent to a player who is trying to place a non-whitelisted barricade on a vehicle.", "Barricade being placed")]
    public static readonly Translation<ItemBarricadeAsset> NoPlacementOnVehicle = new Translation<ItemBarricadeAsset>("<#fa9e9e>You can't place {0} on a vehicle!</color>", arg0Fmt: FormatRarityColor + FormatPlural);
    
    [TranslationData(SectionPlayers, "Generic message sent when a player is placing something in a place they shouldn't.", "Item being placed")]
    public static readonly Translation<ItemAsset> ProhibitedPlacement = new Translation<ItemAsset>("<#fa9e9e>You're not allowed to place {0} here.", arg0Fmt: FormatRarityColor + FormatPlural);
    
    [TranslationData(SectionPlayers, "Generic message sent when a player is dropping an item where they shouldn't.", "Item being dropped", "Zone or flag the player is dropping their item in.")]
    public static readonly Translation<ItemAsset, IDeployable> ProhibitedDropZone = new Translation<ItemAsset, IDeployable>("<#fa9e9e>You're not allowed to drop {0} in {1}.", arg0Fmt: FormatRarityColor + FormatPlural, arg1Fmt: Flag.COLOR_NAME_DISCOVER_FORMAT);
    
    [TranslationData(SectionPlayers, "Generic message sent when a player is picking up an item where they shouldn't.", "Item being picked up", "Zone or flag the player is picking up their item in.")]
    public static readonly Translation<ItemAsset, IDeployable> ProhibitedPickupZone = new Translation<ItemAsset, IDeployable>("<#fa9e9e>You're not allowed to pick up {0} in {1}.", arg0Fmt: FormatRarityColor + FormatPlural, arg1Fmt: Flag.COLOR_NAME_DISCOVER_FORMAT);
    
    [TranslationData(SectionPlayers, "Generic message sent when a player is placing something in a zone they shouldn't be.", "Item being placed", "Zone or flag the player is placing their item in.")]
    public static readonly Translation<ItemAsset, IDeployable> ProhibitedPlacementZone = new Translation<ItemAsset, IDeployable>("<#fa9e9e>You're not allowed to place {0} in {1}.", arg0Fmt: FormatRarityColor + FormatPlural, arg1Fmt: Flag.COLOR_NAME_DISCOVER_FORMAT);
    
    [TranslationData(SectionPlayers, "Sent when a player tries to steal a battery.")]
    public static readonly Translation NoStealingBatteries = new Translation("<#fa9e9e>Stealing batteries is not allowed.</color>");
    
    [TranslationData(SectionPlayers, "Sent when a player tries to manually leave their group.")]
    public static readonly Translation NoLeavingGroup = new Translation("<#fa9e9e>You are not allowed to manually change groups, use <#cedcde>/teams</color> instead.");
    
    [TranslationData(SectionPlayers, "Message sent when a player tries to place a non-whitelisted item in a storage inventory.", "Item being stored")]
    public static readonly Translation<ItemAsset> ProhibitedStoring = new Translation<ItemAsset>("<#fa9e9e>You are not allowed to store {0}.", arg0Fmt: FormatRarityColor + FormatPlural);
    
    [TranslationData(SectionPlayers, "Sent when a player tries to point or mark while not a squad leader.")]
    public static readonly Translation MarkerNotInSquad = new Translation("<#fa9e9e>Only your squad can see markers. Create a squad with <#cedcde>/squad create</color> to use this feature.");
    
    [TranslationData(SectionPlayers, "Sent on a SEVERE toast when the player enters enemy territory.", "Seconds until death")]
    public static readonly Translation<string> EnteredEnemyTerritory = new Translation<string>("ENEMY HQ PROXIMITY\nLEAVE IMMEDIATELY\nDEAD IN <uppercase>{0}</uppercase>", TranslationOptions.UnityUI);
    
    [TranslationData(SectionPlayers, "Sent 2 times before a player is kicked for inactivity.", "Time code")]
    public static readonly Translation<string> InactivityWarning = new Translation<string>("<#fa9e9e>You will be AFK-Kicked in <#cedcde>{0}</color> if you don't move.</color>");
    
    [TranslationData(SectionPlayers, "Broadcasted when a player is removed from the game by BattlEye.", "Player being kicked.")]
    public static readonly Translation<IPlayer> BattlEyeKickBroadcast = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was kicked by <#feed00>BattlEye</color>.", arg0Fmt: UCPlayer.FormatPlayerName);
    
    [TranslationData(SectionPlayers, "Sent when an unauthorized player attempts to edit a sign.")]
    public static readonly Translation ProhibitedSignEditing = new Translation("<#ff8c69>You are not allowed to edit that sign.");
    
    [TranslationData(SectionPlayers, "Sent when a player tries to use a command while not in main.")]
    public static readonly Translation NotInMain = new Translation("<#b3a6a2>You must be in <#cedcde>MAIN</color> to use this command.");
    
    [TranslationData(SectionPlayers, "Sent when a player tries to craft a blacklisted blueprint.")]
    public static readonly Translation NoCraftingBlueprint = new Translation("<#b3a6a2>Crafting is disabled for this item.");

    [TranslationData(SectionPlayers, "Shows above the XP UI when divisions are enabled.", "Branch (Division) the player is a part of.")]
    public static readonly Translation<Branch> XPUIDivision = new Translation<Branch>("{0} Division");

    [TranslationData(SectionPlayers, "Tells the player that the game detected they have started nitro boosting.")]
    public static readonly Translation StartedNitroBoosting = new Translation("<#e00ec9>Thank you for nitro boosting! In-game perks have been activated.");

    [TranslationData(SectionPlayers, "Tells the player that the game detected they have stopped nitro boosting.")]
    public static readonly Translation StoppedNitroBoosting = new Translation("<#9b59b6>Your nitro boost(s) have expired. In-game perks have been deactivated.");

    [TranslationData(SectionPlayers, "Tells the player that they can't remove clothes which have item storage.")]
    public static readonly Translation NoRemovingClothing = new Translation("<#b3a6a2>You can not remove clothes with storage from your kit.");

    [TranslationData(SectionPlayers, "Tells the player that they can't unlock vehicles from the vehicle bay.")]
    public static readonly Translation UnlockVehicleNotAllowed = new Translation("<#b3a6a2>You can not unlock a requested vehicle.");

    [TranslationData(SectionPlayers, "Goes on the warning UI.")]
    public static readonly Translation MortarWarning = new Translation("FRIENDLY MORTAR\nINCOMING", TranslationOptions.TMProUI);
    #endregion

    #region Leaderboards

    private const string SectionLeaderboard = "Leaderboard";
    #region Shared
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation StartingSoon                   = new Translation("Starting soon...", TranslationOptions.UnityUI);

    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<string> NextGameShutdown       = new Translation<string>("<#94cbff>Shutting Down Because: \"{0}\"</color>", TranslationOptions.UnityUI);

    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<TimeSpan> NextGameShutdownTime = new Translation<TimeSpan>("{0}", TranslationOptions.UnityUI, TimeAddon.Create(TimeFormatType.CountdownMinutesSeconds));

    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo, FactionInfo> WarstatsHeader = new Translation<FactionInfo, FactionInfo>("{0} vs {1}", TranslationOptions.UnityUI, FactionInfo.FormatColorShortName, FactionInfo.FormatColorShortName);
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<IPlayer, float> PlayerstatsHeader       = new Translation<IPlayer, float>("{0} - {1} presence", TranslationOptions.UnityUI, UCPlayer.FormatColoredCharacterName, "P0");
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> WinnerTitle                 = new Translation<FactionInfo>("{0} Wins!", TranslationOptions.UnityUI, FactionInfo.FormatColorShortName);

    [TranslationData(SectionLeaderboard, Parameters = [ "Distance", "Gun Name", "Player" ])]
    public static readonly Translation<float, ItemAsset, IPlayer> LongestShot     = new Translation<float, ItemAsset, IPlayer>("{0}m - {1}\n{2}", TranslationOptions.UnityUI, "F1", arg2Fmt: UCPlayer.FormatColoredCharacterName);
    #endregion

    #region CTFBase
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats0  = new Translation("Kills: ",            TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats1  = new Translation("Deaths: ",           TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats2  = new Translation("K/D Ratio: ",        TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats3  = new Translation("Kills on Point: ",   TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats4  = new Translation("Time Deployed: ",    TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats5  = new Translation("XP Gained: ",        TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats6  = new Translation("Time on Point: ",    TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats7  = new Translation("Captures: ",         TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats8  = new Translation("Time in Vehicle: ",  TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats9  = new Translation("Teamkills: ",        TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats10 = new Translation("FOBs Destroyed: ",   TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats11 = new Translation("Credits Gained: ",   TranslationOptions.UnityUI);


    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFWarStats0 = new Translation("Duration: ", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",     TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",     TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFWarStats3 = new Translation("Flag Captures: ", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",   TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",   TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",    TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",    TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ", TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ", TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFWarStats10 = new Translation("Teamkill Casualties: ", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFWarStats11 = new Translation("Longest Shot: ",        TranslationOptions.UnityUI);


    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFHeader0 = new Translation("Kills",   TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFHeader1 = new Translation("Deaths",  TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFHeader2 = new Translation("XP",      TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFHeader3 = new Translation("Caps", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFHeader4 = new Translation("Vehicles\nDestr.",    TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFHeader5 = new Translation("Aircraft\nDestr.",  TranslationOptions.UnityUI);
    #endregion

    #region Insurgency
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats0  = new Translation("Kills: ",                 TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats1  = new Translation("Deaths: ",                TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats2  = new Translation("Damage Done: ",           TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats3  = new Translation("Objective Kills: ",       TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats4  = new Translation("Time Deployed: ",         TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats5  = new Translation("XP Gained: ",             TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats6  = new Translation("Intelligence Gathered: ", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats7  = new Translation("Caches Discovered: ",     TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats8  = new Translation("Caches Destroyed: ",      TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats9  = new Translation("Teamkills: ",             TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats10 = new Translation("FOBs Destroyed: ",        TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats11 = new Translation("Credits Gained: ",        TranslationOptions.UnityUI);


    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyWarStats0 = new Translation("Duration: ", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyWarStats3 = new Translation("Intelligence Gathered: ", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyWarStats10 = new Translation("Teamkill Casualties: ", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyWarStats11 = new Translation("Longest Shot: ",        TranslationOptions.UnityUI);

    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyHeader0 = new Translation("Kills",   TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyHeader1 = new Translation("Deaths",  TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyHeader2 = new Translation("XP",      TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyHeader3 = new Translation("KDR", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyHeader4 = new Translation("Vehicles\nDestr.",     TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyHeader5 = new Translation("Aircraft\nDestr.",  TranslationOptions.UnityUI);
    #endregion

    #region Conquest
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats0  = new Translation("Kills: ",                 TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats1  = new Translation("Deaths: ",                TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats2  = new Translation("Damage Done: ",           TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats3  = new Translation("Objective Kills: ",       TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats4  = new Translation("Time Deployed: ",         TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats5  = new Translation("XP Gained: ",             TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats6  = new Translation("Revives: ",               TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats7  = new Translation("Flags Captured: ",        TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats8  = new Translation("Time on Flag: ",          TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats9  = new Translation("Teamkills: ",             TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats10 = new Translation("FOBs Destroyed: ",        TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats11 = new Translation("Credits Gained: ",        TranslationOptions.UnityUI);

    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestWarStats0 = new Translation("Duration: ",                                      TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestWarStats3 = new Translation("Flag Captures: ",                                 TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestWarStats10 = new Translation("Teamkill Casualties: ", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestWarStats11 = new Translation("Longest Shot: ",        TranslationOptions.UnityUI);

    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestHeader0 = new Translation("Kills",   TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestHeader1 = new Translation("Deaths",  TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestHeader2 = new Translation("XP",      TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestHeader3 = new Translation("Captures", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestHeader4 = new Translation("Vehicles\nDestr.",     TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestHeader5 = new Translation("Aircraft\nDestr.",  TranslationOptions.UnityUI);
    #endregion

    #region Hardpoint
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats0  = new Translation("Kills: ",                 TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats1  = new Translation("Deaths: ",                TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats2  = new Translation("Damage Done: ",           TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats3  = new Translation("Objective Kills: ",       TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats4  = new Translation("Time Deployed: ",         TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats5  = new Translation("XP Gained: ",             TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats6  = new Translation("Revives: ",               TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats7  = new Translation("Points Gained: ",         TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats8  = new Translation("Time on Flag: ",          TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats9  = new Translation("Teamkills: ",             TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats10 = new Translation("FOBs Destroyed: ",        TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats11 = new Translation("Credits Gained: ",        TranslationOptions.UnityUI);

    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointWarStats0 = new Translation("Duration: ",                                      TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointWarStats3 = new Translation("Contesting Time: ",                               TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationOptions.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointWarStats10 = new Translation("Teamkill Casualties: ", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointWarStats11 = new Translation("Longest Shot: ",        TranslationOptions.UnityUI);

    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointHeader0 = new Translation("Kills",    TranslationOptions.UnityUI);

    [TranslationData(SectionLeaderboard)]                                             
    public static readonly Translation HardpointHeader1 = new Translation("Deaths",   TranslationOptions.UnityUI);

    [TranslationData(SectionLeaderboard)]                                             
    public static readonly Translation HardpointHeader2 = new Translation("XP",       TranslationOptions.UnityUI);

    [TranslationData(SectionLeaderboard)]                                             
    public static readonly Translation HardpointHeader3 = new Translation("Cap Seconds",  TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointHeader4 = new Translation("Vehicles\nDestr.", TranslationOptions.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointHeader5 = new Translation("Aircraft\nDestr.",  TranslationOptions.UnityUI);
    #endregion

    #endregion
    
    #region LangCommand
    private const string SectionLanguages = "Languages";
    
    [TranslationData(SectionLanguages, "Output from /lang, lists all languages.", "Comma-serparated list of languages")]
    public static readonly Translation<string> LanguageList              = new Translation<string>("<#f53b3b>Languages: <#e6e3d5>{0}</color>.");
    
    [TranslationData(SectionLanguages, "Fallback usage output from /lang, explains /lang reset.")]
    public static readonly Translation ResetLanguageHow                  = new Translation("<#f53b3b>Do <#e6e3d5>/lang reset</color> to reset back to default language.");
    
    [TranslationData(SectionLanguages, "Result from using /lang refresh, reloads ui with updated text.")]
    public static readonly Translation LanguageRefreshed                 = new Translation("<#f53b3b>Refreshed all signs and UI.");
    
    [TranslationData(SectionLanguages, "Output from /lang current, tells the player their selected language.", "Current Language")]
    public static readonly Translation<LanguageInfo> LanguageCurrent = new Translation<LanguageInfo>("<#f53b3b>Current language: <#e6e3d5>{0}</color>.", arg0Fmt: LanguageInfo.FormatDisplayName);
    
    [TranslationData(SectionLanguages, "Output from /lang <language>, tells the player their new language.", "New Language")]
    public static readonly Translation<LanguageInfo> ChangedLanguage = new Translation<LanguageInfo>("<#f53b3b>Changed your language to <#e6e3d5>{0}</color>.", arg0Fmt: LanguageInfo.FormatDisplayName);
    
    [TranslationData(SectionLanguages, "Output from /lang <language> when the player is using already that language.", "Current Language")]
    public static readonly Translation<LanguageInfo> LangAlreadySet  = new Translation<LanguageInfo>("<#ff8c69>You are already set to <#e6e3d5>{0}</color>.", arg0Fmt: LanguageInfo.FormatDisplayName);
    
    [TranslationData(SectionLanguages, "Output from /lang reset, tells the player their language changed to the default language.", "Default Language")]
    public static readonly Translation<LanguageInfo> ResetLanguage   = new Translation<LanguageInfo>("<#f53b3b>Reset your language to <#e6e3d5>{0}</color>.", arg0Fmt: LanguageInfo.FormatDisplayName);
    
    [TranslationData(SectionLanguages, "Output from /lang reset when the player is using already that language.", "Default Language")]
    public static readonly Translation<LanguageInfo> ResetCurrent    = new Translation<LanguageInfo>("<#ff8c69>You are already on the default language: <#e6e3d5>{0}</color>.", arg0Fmt: LanguageInfo.FormatDisplayName);
    
    [TranslationData(SectionLanguages, "Output from /lang <language> when the language isn't found.", "Input language")]
    public static readonly Translation<string> LanguageNotFound          = new Translation<string>("<#dd1111>We don't have translations for <#e6e3d5>{0}</color> yet. If you are fluent and want to help, feel free to ask us about submitting translations.");

    [TranslationData(SectionLanguages, "Tells the player that IMGUI is recommended for this language and how to enable it (part 1).", "Language id")]
    public static readonly Translation<LanguageInfo> IMGUITip1       = new Translation<LanguageInfo>("<#f53b3b>{0} recommends using IMGUI mode. do <#fff>/options imgui true</color>...", arg0Fmt: LanguageInfo.FormatDisplayName);
    [TranslationData(SectionLanguages, "Tells the player that IMGUI is recommended for this language and how to enable it (part 2).")]
    public static readonly Translation IMGUITip2                         = new Translation("<#f53b3b>... go to your steam launch options and add <#fff>-Glazier IMGUI</color> to them.");

    [TranslationData(SectionLanguages, "Tells the player that IMGUI is not recommended for this language and how to enable it (part 1).", "Language id")]
    public static readonly Translation<LanguageInfo> NoIMGUITip1     = new Translation<LanguageInfo>("<#f53b3b>{0} recommends not using IMGUI mode. do <#fff>/options imgui false</color>...", arg0Fmt: LanguageInfo.FormatDisplayName);
    [TranslationData(SectionLanguages, "Tells the player that IMGUI is not recommended for this language and how to enable it (part 2).")]
    public static readonly Translation NoIMGUITip2 = new Translation("<#f53b3b>... go to your steam launch options and remove <#fff>-Glazier IMGUI</color>.");
    #endregion

    #region Toasts
    private const string SectionToasts = "Toasts";
    [TranslationData(SectionToasts, "Sent when the player joins for the 1st time.")]
    public static readonly Translation<IPlayer> WelcomeMessage = new Translation<IPlayer>("Welcome to <#c$uncreated$>Uncreated Warfare</color> {0}!\nCheck out our tutorial to get started (follow the signs).", TranslationOptions.UnityUI, UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionToasts, "Broadcasted when a game is loading.", "Next gamemode")]
    public static readonly Translation<Gamemode> LoadingGamemode = new Translation<Gamemode>("Loading New Gamemode\n<#66ff99>{0}</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionToasts, "Broadcasted when a player joins and their data is loading.")]
    public static readonly Translation LoadingOnJoin = new Translation("Loading Player Data", TranslationOptions.TMProUI);
    [TranslationData(SectionToasts, "Title for the welcome message.")]
    public static readonly Translation WelcomeMessageTitle = new Translation("Welcome to Uncreated Warfare", TranslationOptions.TMProUI);
    #endregion

    #region RangeCommand
    private const string SectionRange = "Range Command";
    [TranslationData(SectionRange)]
    public static readonly Translation<float> RangeOutput  = new Translation<float>("<#9e9c99>The range to your squad's marker is: <#8aff9f>{0}m</color>.", arg0Fmt: "N0");
    [TranslationData(SectionRange)]
    public static readonly Translation RangeNoMarker       = new Translation("<#9e9c99>You squad has no marker.");
    [TranslationData(SectionRange)]
    public static readonly Translation RangeNotSquadleader = new Translation("<#9e9c99>Only <#cedcde>SQUAD LEADERS</color> can place markers.");
    [TranslationData(SectionRange)]
    public static readonly Translation RangeNotInSquad     = new Translation("<#9e9c99>You must JOIN A SQUAD in order to do /range.");
    #endregion

    #region Squads
    private const string SectionSquads = "Squads";
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadNotOnTeam               = new Translation("<#a89791>You can't join a squad unless you're on a team.");
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadCreated          = new Translation<Squad>("<#a0ad8e>You created {0} squad.", arg0Fmt: Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadJoined           = new Translation<Squad>("<#a0ad8e>You joined {0} squad.", arg0Fmt: Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadLeft             = new Translation<Squad>("<#a7a8a5>You left {0} squad.", arg0Fmt: Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadDisbanded        = new Translation<Squad>("<#a7a8a5>{0} squad was disbanded.", arg0Fmt: Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadLockedSquad             = new Translation("<#a7a8a5>You <#6be888>locked</color> your squad.");
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadUnlockedSquad           = new Translation("<#999e90>You <#fff>unlocked</color> your squad.");
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadPromoted         = new Translation<Squad>("<#999e90>You're now the <#cedcde>squad leader</color> of {0}.", arg0Fmt: Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadKicked           = new Translation<Squad>("<#ae8f8f>You were kicked from {0} squad.", arg0Fmt: Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<string> SquadNotFound        = new Translation<string>("<#ae8f8f>Failed to find a squad called \"<#c$neutral$>{0}</color>\". You can also use the first letter of the squad name.");
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadAlreadyInSquad          = new Translation("<#ae8f8f>You're already in a squad.");
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadNotInSquad              = new Translation("<#ae8f8f>You're not in a squad yet. Use <#ae8f8f>/squad join <squad></color> to join a squad.");
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadNotSquadLeader          = new Translation("<#ae8f8f>You're not the leader of your squad.");
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadLocked           = new Translation<Squad>("<#a89791>{0} is locked.", arg0Fmt: Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadFull             = new Translation<Squad>("<#a89791>{0} is full.", arg0Fmt: Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadTargetNotInSquad        = new Translation("<#a89791>That player isn't in a squad.");
    [TranslationData(SectionSquads)]
    public static readonly Translation<IPlayer> SquadPlayerJoined   = new Translation<IPlayer>("<#b9bdb3>{0} joined your squad.", arg0Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<IPlayer> SquadPlayerLeft     = new Translation<IPlayer>("<#b9bdb3>{0} left your squad.", arg0Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<IPlayer> SquadPlayerPromoted = new Translation<IPlayer>("<#b9bdb3>{0} was promoted to <#cedcde>squad leader</color>.", arg0Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<IPlayer> SquadPlayerKicked   = new Translation<IPlayer>("<#b9bdb3>{0} was kicked from your squad.", arg0Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadsDisabled               = new Translation("<#a89791>Squads are disabled in this gamemode.");
    [TranslationData(SectionSquads)]
    public static readonly Translation<int> SquadsTooMany           = new Translation<int>("<#a89791>There can not be more than {0} ${p:0:squad} on a team at once.");
    [TranslationData(SectionSquads)]
    public static readonly Translation<int> SquadsTooManyPlayerCount = new Translation<int>("<#a89791>There are too many squads right now. More squads are unlocked once your team reaches {0} ${p:0:member}.");
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadWarningNoMembers = new Translation("<#a89791>Your squad will be DISBANDED unless others join", TranslationOptions.TMProUI);
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadWarningWrongKit = new Translation("<#a89791>You must request a SQUADLEADER kit", TranslationOptions.TMProUI);


    [TranslationData(SectionSquads, IsPriorityTranslation = false)]
    public static readonly Translation<Squad, int, int> SquadsUIHeaderPlayerCount    = new Translation<Squad, int, int>("<#bd6b5b>{0}</color> {1}/{2}", TranslationOptions.UnityUI, Squad.FormatName);
    [TranslationData(SectionSquads, IsPriorityTranslation = false)]
    public static readonly Translation<int, int> SquadsUIPlayerCountList             = new Translation<int, int>("{0}/{1}", TranslationOptions.UnityUI);
    [TranslationData(SectionSquads, IsPriorityTranslation = false)]
    public static readonly Translation<int, int, char> SquadsUIPlayerCountListLocked = new Translation<int, int, char>("{2} {0}/{1}", TranslationOptions.UnityUI);
    [TranslationData(SectionSquads, IsPriorityTranslation = false)]
    public static readonly Translation<int, int> SquadsUIPlayerCountSmall            = new Translation<int, int>("{0}/{1}", TranslationOptions.UnityUI);
    [TranslationData(SectionSquads, IsPriorityTranslation = false)]
    public static readonly Translation<int, int> SquadsUIPlayerCountSmallLocked      = new Translation<int, int>("<#969696>{0}/{1}</color>", TranslationOptions.UnityUI);
    [TranslationData(SectionSquads, IsPriorityTranslation = false)]
    public static readonly Translation SquadUIExpanded                               = new Translation("...", TranslationOptions.UnityUI);
    #endregion

    #region Rallies
    private const string SectionRallies = "Rallies";
    [TranslationData(SectionRallies)]
    public static readonly Translation RallySuccess = new Translation("<#959c8c>You have <#c$rally$>rallied</color> with your squad.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyNotActiveSL = new Translation("<#959c8c>Your squad doesn't have an active <#c$rally$>RALLY POINT</color>. Place one to allow you and your squad to deploy to it.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyActiveSL = new Translation("<#959c8c><#c$rally$>RALLY POINT</color> is now active. Do <#bfbfbf>/rally</color> to rally your squad to this position.");
    [TranslationData(SectionRallies)]
    public static readonly Translation<int> RallyWait = new Translation<int>("<#959c8c>Standby for <#c$rally$>RALLY</color> in: <#ffe4b5>{0} ${p:0:second}</color>. Do <#a3b4c7>/rally cancel</color> to be excluded.");
    [TranslationData(SectionRallies)]
    public static readonly Translation<int> RallyWaitSL = new Translation<int>("<#959c8c>Standby for <#c$rally$>RALLY</color> in: <#ffe4b5>{0} ${p:0:second}</color>. Do <#a3b4c7>/rally cancel</color> to cancel deployment.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyCancel = new Translation("<#a1a1a1>Cancelled rally deployment.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyObstructed = new Translation("<#959c8c><#bfbfbf>RALLY</color> is no longer available - there are enemies nearby.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyNoSquadmates = new Translation("<#99918d>You need more squad members to use a <#bfbfbf>rally point</color>.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyNotSquadleader = new Translation("<#99918d>You must be a <color=#cedcde>SQUAD LEADER</color> in order to <#c$rally$>rally</color> your squad.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyAlreadyDeploying = new Translation("<#99918d>You are already waiting on <#c$rally$>rally</color> deployment. Do <#a3b4c7>/rally cancel</color> to abort.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyNoCancel = new Translation("<#959c8c>Your squad is not waiting on a <#c$rally$>rally</color> deployment.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyNoCancelPerm = new Translation("<#959c8c>Try <#a3b4c7>/rally cancel</color> to be excluded from <#c$rally$>rally</color> deployment.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyNoDeny = new Translation("<#959c8c>You aren't waiting on a <#c$rally$>rally</color> deployment.");
    [TranslationData(SectionRallies)]
    public static readonly Translation<Cooldown> RallyCooldown = new Translation<Cooldown>("<#959c8c>You can rally your squad again in: <#e3c27f>{0}</color>", arg0Fmt: Cooldown.FormatTimeLong);
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyObstructedPlace = new Translation("<#959c8c>This rally point is obstructed, find a more open place to put it.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyEnemiesNearby = new Translation("<#9e7a6c>Cannot place rally when there are enemies nearby.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyEnemiesNearbyTp = new Translation("<#9e7a6c>There are enemies near your RALLY. Deployment is no longer possible.");
    [TranslationData(SectionRallies)]
    public static readonly Translation<int> RallyToast = new Translation<int>("<#959c8c><#c$rally$>RALLY</color> IN <#ffe4b5>{0}</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionRallies, IsPriorityTranslation = false)]
    public static readonly Translation<string> RallyUI = new Translation<string>("<#c$rally$>RALLY</color> {0}", TranslationOptions.UnityUI);
    [TranslationData(SectionRallies, IsPriorityTranslation = false)]
    public static readonly Translation<TimeSpan, string> RallyUITimer = new Translation<TimeSpan, string>("<#c$rally$>RALLY</color> {0} {1}", TranslationOptions.UnityUI, TimeAddon.Create(TimeFormatType.CountdownMinutesSeconds));
    #endregion

    #region Time
    private const string SectionTime = "Time Strings";
    [TranslationData(SectionTime, "Permanent time, lasts forever.")]
    public static readonly Translation TimePermanent    = new Translation("permanent", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "1 second (singular).")]
    public static readonly Translation TimeSecondSingle = new Translation("second", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "X seconds (plural).")]
    public static readonly Translation TimeSecondPlural = new Translation("seconds", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "1 minute (singular).")]
    public static readonly Translation TimeMinuteSingle = new Translation("minute", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "X minutes (plural).")]
    public static readonly Translation TimeMinutePlural = new Translation("minutes", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "1 hour (singular).")]
    public static readonly Translation TimeHourSingle   = new Translation("hour", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "X hours (plural).")]
    public static readonly Translation TimeHourPlural   = new Translation("hours", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "1 day (singular).")]
    public static readonly Translation TimeDaySingle    = new Translation("day", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "X days (plural).")]
    public static readonly Translation TimeDayPlural    = new Translation("days", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "1 week (singular).")]
    public static readonly Translation TimeWeekSingle   = new Translation("week", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "X weeks (plural).")]
    public static readonly Translation TimeWeekPlural   = new Translation("weeks", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "1 month (singular).")]
    public static readonly Translation TimeMonthSingle  = new Translation("month", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "X months (plural).")]
    public static readonly Translation TimeMonthPlural  = new Translation("months", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "1 year (singular).")]
    public static readonly Translation TimeYearSingle   = new Translation("year", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "X years (plural).")]
    public static readonly Translation TimeYearPlural   = new Translation("years", TranslationOptions.UnityUINoReplace);
    [TranslationData(SectionTime, "Joining keyword (1 hour \"and\" 30 minutes).")]
    public static readonly Translation TimeAnd          = new Translation("and", TranslationOptions.UnityUINoReplace);
    #endregion

    #region Popup
    private const string SectionPopup = "Popup";
    [TranslationData(SectionPopup)]
    public static readonly Translation ButtonOK = new Translation("OK", TranslationOptions.TMProUI);
    [TranslationData(SectionPopup)]
    public static readonly Translation ButtonCancel = new Translation("Cancel", TranslationOptions.TMProUI);
    [TranslationData(SectionPopup)]
    public static readonly Translation ButtonYes = new Translation("Yes", TranslationOptions.TMProUI);
    [TranslationData(SectionPopup)]
    public static readonly Translation ButtonNo = new Translation("No", TranslationOptions.TMProUI);
    #endregion

    #region Load Command
    private const string SectionLoad = "Load Supplies";
    [TranslationData(SectionLoad)]
    public static readonly Translation LoadNoTarget = new Translation("<#b3a6a2>Look at a friendly <#cedcde>LOGISTICS VEHICLE</color>.");
    [TranslationData(SectionLoad)]
    public static readonly Translation LoadUsage = new Translation("<#b3a6a2>Try typing: '<#e6d1b3>/load ammo <amount|'half'></color>' or '<#e6d1b3>/load build <amount|'half'></color>'.");
    [TranslationData(SectionLoad)]
    public static readonly Translation<string> LoadInvalidAmount = new Translation<string>("<#b3a6a2>'{0}' is not a valid amount of supplies.", arg0Fmt: FormatUppercase);
    [TranslationData(SectionLoad)]
    public static readonly Translation LoadNotInMain = new Translation("<#b3a6a2>You must be in <#cedcde>MAIN</color> to load up this vehicle.");
    [TranslationData(SectionLoad)]
    public static readonly Translation LoadNotLogisticsVehicle = new Translation("<#b3a6a2>Only <#cedcde>LOGISTICS VEHICLES</color> can be loaded with supplies.");
    [TranslationData(SectionLoad)]
    public static readonly Translation LoadSpeed = new Translation("<#b3a6a2>You can only load supplies while the vehicle is stopped.");
    [TranslationData(SectionLoad)]
    public static readonly Translation LoadAlreadyLoading = new Translation("<#b3a6a2>You can only load one type of supply at once.");
    [TranslationData(SectionLoad)]
    public static readonly Translation<int> LoadCompleteBuild = new Translation<int>("<#d1bda7>Loading complete. <#c$build$>{0} BUILD</color> loaded.");
    [TranslationData(SectionLoad)]
    public static readonly Translation<int> LoadCompleteAmmo = new Translation<int>("<#d1bda7>Loading complete. <#c$ammo$>{0} AMMO</color> loaded.");
    #endregion

    #region Announcements
    [TranslationData(Description = "Announcement telling people to join the discord by typing /discord.")]
    public static readonly Translation AnnouncementDiscord = new Translation("<#b3b3b3>Have you joined our <#7483c4>Discord</color> server yet? Type <#7483c4>/discord</color> to join.");
    [TranslationData(Description = "Announcement telling people how to return to base from FOBs.")]
    public static readonly Translation AnnouncementDeployMain = new Translation("<#c2b7a5>You can deploy back to main by doing <#ffffff>/deploy main</color> while near a friendly FOB.");
    [TranslationData(Description = "Announcement telling people the best ways to earn XP.")]
    public static readonly Translation AnnouncementRankUp = new Translation("<#92a692>Capture <#ffffff>flags</color> and build <#ffffff>FOBs</color> to rank up and earn respect amongst your team.");
    [TranslationData(Description = "Announcement telling people not to waste assets.")]
    public static readonly Translation AnnouncementDontWasteAssets = new Translation("<#c79675>Do not waste vehicles, ammo, build, or other assets! You may risk punishment if you're reported or caught.");
    [TranslationData(Description = "Announcement telling people to communicate and listen to higher-ups.")]
    public static readonly Translation AnnouncementListenToSuperiors = new Translation("<#a2a7ba>Winning requires coordination and teamwork. Listen to your superior officers, and communicate!");
    [TranslationData(Description = "Announcement telling people to build FOBs to help their team.")]
    public static readonly Translation AnnouncementBuildFOBs = new Translation("<#9da6a6>Building <color=#54e3ff>FOBs</color> is vital for advancing operations. Grab a logistics truck and go build one!");
    [TranslationData(Description = "Announcement telling people to join or create a squad.")]
    public static readonly Translation AnnouncementSquads = new Translation("<#c2b7a5>Join a squad with <#ffffff>/squad join</color> or create one with <#ffffff>/squad create</color> to earn extra XP among other benefits.");
    [TranslationData(Description = "Announcement telling people about the different way our chat works.")]
    public static readonly Translation AnnouncementChatChanges = new Translation("<#a2a7ba>Use area chat while in a squad to communicate with only them or group chat to communicate with your entire <#54e3ff>team</color>.");
    [TranslationData(Description = "Announcement telling people about the abandon command.")]
    public static readonly Translation AnnouncementAbandon = new Translation("<#b3b3b3>Done with your vehicle? Type <#ffffff>/abandon</color> while in main base to get some credits back and free up the vehicle for your team.");
    [TranslationData(Description = "Announcement telling people about soloing.")]
    public static readonly Translation AnnouncementSoloing = new Translation("<#c79675>Soloing armor vehicles, attack helis, and jets is against the rules. Make sure you have a passenger for these vehicles.");
    [TranslationData(Description = "Announcement telling people about reporting with /report.")]
    public static readonly Translation AnnouncementReport = new Translation("<#c2b7a5>See someone breaking rules? Use the <#ffffff>/report</color> command to help admins see context about the report.</color>");
    #endregion

    #region Kick Command
    private const string SectionKick = "Kick";
    [TranslationData(SectionKick, IsPriorityTranslation = false)]
    public static readonly Translation NoReasonProvided                       = new Translation("<#9cffb3>You must provide a reason.");
    [TranslationData(SectionKick, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer> KickSuccessFeedback           = new Translation<IPlayer>("<#00ffff>You kicked <#d8addb>{0}</color>.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionKick)]
    public static readonly Translation<IPlayer, IPlayer> KickSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#00ffff><#d8addb>{0}</color> was kicked by <#" + TeamManager.AdminColorHex + ">{1}</color>.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatPlayerName);
    [TranslationData(SectionKick)]
    public static readonly Translation<IPlayer> KickSuccessBroadcastOperator  = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was kicked by an operator.", arg0Fmt: UCPlayer.FormatCharacterName);
    #endregion

    #region Ban Command
    private const string SectionBan = "Ban";
    [TranslationData(SectionBan, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer> BanPermanentSuccessFeedback           = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was <b>permanently</b> banned.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionBan)]
    public static readonly Translation<IPlayer, IPlayer> BanPermanentSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#00ffff><#d8addb>{0}</color> was <b>permanently</b> banned by <#" + TeamManager.AdminColorHex + ">{1}</color>.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatPlayerName);
    [TranslationData(SectionBan)]
    public static readonly Translation<IPlayer> BanPermanentSuccessBroadcastOperator  = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was <b>permanently</b> banned by an operator.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionBan, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, string> BanSuccessFeedback            = new Translation<IPlayer, string>("<#00ffff><#d8addb>{0}</color> was banned for <#9cffb3>{1}</color>.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionBan)]
    public static readonly Translation<IPlayer, IPlayer, string> BanSuccessBroadcast  = new Translation<IPlayer, IPlayer, string>("<#00ffff><#d8addb>{0}</color> was banned for <#9cffb3>{2}</color> by <#" + TeamManager.AdminColorHex + ">{1}</color>.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatPlayerName);
    [TranslationData(SectionBan)]
    public static readonly Translation<IPlayer, string> BanSuccessBroadcastOperator   = new Translation<IPlayer, string>("<#00ffff><#d8addb>{0}</color> was banned for <#9cffb3>{1}</color> by an operator.", arg0Fmt: UCPlayer.FormatCharacterName);
    #endregion

    #region Unban Command
    private const string SectionUnban = "Unban";
    [TranslationData(SectionUnban, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer> UnbanNotBanned = new Translation<IPlayer>("<#9cffb3><#d8addb>{0}</color> is not currently banned.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionUnban, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer> UnbanSuccessFeedback = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was unbanned.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionUnban)]
    public static readonly Translation<IPlayer, IPlayer> UnbanSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#00ffff><#d8addb>{0}</color> was unbanned by <#" + TeamManager.AdminColorHex + ">{1}</color>.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatPlayerName);
    [TranslationData(SectionUnban)]
    public static readonly Translation<IPlayer> UnbanSuccessBroadcastOperator = new Translation<IPlayer>("<#ffff00><#d8addb>{0}</color> was unbanned by an operator.", arg0Fmt: UCPlayer.FormatCharacterName);
    #endregion

    #region Warn Command
    private const string SectionWarn = "Warn";
    [TranslationData(SectionWarn, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer> WarnSuccessFeedback           = new Translation<IPlayer>("<#ffff00>You warned <#d8addb>{0}</color>.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionWarn)]
    public static readonly Translation<IPlayer, IPlayer> WarnSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#ffff00><#d8addb>{0}</color> was warned by <#" + TeamManager.AdminColorHex + ">{1}</color>.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatPlayerName);
    [TranslationData(SectionWarn)]
    public static readonly Translation<IPlayer> WarnSuccessBroadcastOperator  = new Translation<IPlayer>("<#ffff00><#d8addb>{0}</color> was warned by an operator.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionWarn)]
    public static readonly Translation<IPlayer, string> WarnSuccessDM         = new Translation<IPlayer, string>("<color=#ffff00><color=#" + TeamManager.AdminColorHex + ">{0}</color> warned you for <color=#ffffff>{1}</color>.</color>", TranslationOptions.TMProUI, UCPlayer.FormatPlayerName);
    [TranslationData(SectionWarn)]
    public static readonly Translation<string> WarnSuccessDMOperator          = new Translation<string>("<color=#ffff00>An operator warned you for <color=#ffffff>{0}</color>.</color>", TranslationOptions.TMProUI, UCPlayer.FormatPlayerName);
    [TranslationData(SectionWarn)]
    public static readonly Translation WarnSuccessTitle = new Translation("<color=#ffff00>Warning", TranslationOptions.TMProUI);
    #endregion

    #region Mute Command
    private const string SectionMute = "Mute";
    [TranslationData(SectionMute, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, IPlayer, MuteType> MutePermanentSuccessFeedback = new Translation<IPlayer, IPlayer, MuteType>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <b>permanently</b> <#cedcde>{2}</color> muted.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatSteam64, arg2Fmt: FormatLowercase);
    [TranslationData(SectionMute, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, IPlayer, string, MuteType> MuteSuccessFeedback  = new Translation<IPlayer, IPlayer, string, MuteType>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <#cedcde>{3}</color> muted for <#9cffb3>{2}</color>.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatSteam64, arg3Fmt: FormatLowercase);
    [TranslationData(SectionMute)]
    public static readonly Translation<IPlayer, IPlayer, MuteType> MutePermanentSuccessBroadcastOperator  = new Translation<IPlayer, IPlayer, MuteType>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <b>permanently</b> <#cedcde>{2}</color> muted by an operator.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatSteam64, arg2Fmt: FormatLowercase);
    [TranslationData(SectionMute)]
    public static readonly Translation<IPlayer, IPlayer, MuteType, IPlayer> MutePermanentSuccessBroadcast = new Translation<IPlayer, IPlayer, MuteType, IPlayer>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <b>permanently</b> <#cedcde>{2}</color> muted by <#" + TeamManager.AdminColorHex + ">{3}</color>.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatSteam64, arg2Fmt: FormatLowercase, arg3Fmt: UCPlayer.FormatPlayerName);
    [TranslationData(SectionMute)]
    public static readonly Translation<IPlayer, IPlayer, string, MuteType> MuteSuccessBroadcastOperator   = new Translation<IPlayer, IPlayer, string, MuteType>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <#cedcde>{3}</color> muted by an operator for <#9cffb3>{2}</color>.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatSteam64, arg3Fmt: FormatLowercase);
    [TranslationData(SectionMute)]
    public static readonly Translation<IPlayer, IPlayer, string, MuteType, IPlayer> MuteSuccessBroadcast  = new Translation<IPlayer, IPlayer, string, MuteType, IPlayer>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <#cedcde>{3}</color> muted by <#" + TeamManager.AdminColorHex + ">{4}</color> for <#9cffb3>{2}</color>.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatSteam64, arg3Fmt: FormatLowercase, arg4Fmt: UCPlayer.FormatPlayerName);
    [TranslationData(SectionMute)]
    public static readonly Translation<IPlayer, string, string, MuteType> MuteSuccessDM  = new Translation<IPlayer, string, string, MuteType>("<#ffff00><#" + TeamManager.AdminColorHex + ">{0}</color> <#9cffb3>{3}</color> muted you for <#9cffb3>{2}</color> because: <#9cffb3>{1}</color>.", arg0Fmt: UCPlayer.FormatPlayerName, arg3Fmt: FormatLowercase);
    [TranslationData(SectionMute)]
    public static readonly Translation<IPlayer, string, MuteType> MuteSuccessDMPermanent = new Translation<IPlayer, string, MuteType>("<#ffff00><#" + TeamManager.AdminColorHex + ">{0}</color> permanently <#9cffb3>{2}</color> muted you because: <#9cffb3>{1}</color>.", arg0Fmt: UCPlayer.FormatPlayerName, arg2Fmt: FormatLowercase);
    [TranslationData(SectionMute)]
    public static readonly Translation<string, string, MuteType> MuteSuccessDMOperator   = new Translation<string, string, MuteType>("<#ffff00>An operator <#9cffb3>{2}</color> muted you for <#9cffb3>{1}</color> because: <#9cffb3>{0}</color>.", arg2Fmt: FormatLowercase);
    [TranslationData(SectionMute)]
    public static readonly Translation<string, MuteType> MuteSuccessDMPermanentOperator  = new Translation<string, MuteType>("<#ffff00>>An operator permanently <#9cffb3>{1}</color> muted you because: <#9cffb3>{0}</color>.", arg1Fmt: FormatLowercase);

    [TranslationData(SectionMute)]
    public static readonly Translation<string> MuteTextChatFeedbackPermanent  = new Translation<string>("<#ffff00>You're permanently muted in text chat because: <#9cffb3>{0}</color>.");
    [TranslationData(SectionMute)]
    public static readonly Translation<DateTime, string> MuteTextChatFeedback = new Translation<DateTime, string>("<#ffff00>You're muted in text chat until <#cedcde>{0}</color> because <#9cffb3>{1}</color>.", arg0Fmt: "r");
    #endregion

    #region Unmute Command
    private const string SectionUnmute = "Unmute";
    [TranslationData(SectionUnmute, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer> UnmuteNotMuted                  = new Translation<IPlayer>("<#9cffb3><#d8addb>{0}</color> is not currently muted.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionUnmute, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer> UnmuteSuccessFeedback           = new Translation<IPlayer>("<#ffff00><#d8addb>{0}</color> was unmuted.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionUnmute)]
    public static readonly Translation<IPlayer, IPlayer> UnmuteSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#ffff00><#d8addb>{0}</color> was unmuted by <#" + TeamManager.AdminColorHex + ">{1}</color>.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatPlayerName);
    [TranslationData(SectionUnmute)]
    public static readonly Translation<IPlayer> UnmuteSuccessBroadcastOperator  = new Translation<IPlayer>("<#ffff00><#d8addb>{0}</color> was unmuted by an operator.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionUnmute)]
    public static readonly Translation<IPlayer> UnmuteSuccessDM                 = new Translation<IPlayer>("<#ffff00><#" + TeamManager.AdminColorHex + ">{0}</color> unmuted you.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionUnmute)]
    public static readonly Translation UnmuteSuccessDMOperator                  = new Translation("<#ffff00>An operator unmuted you.");
    #endregion

    #region Speed Command
    private const string SectionSpeed = "Speed Command";
    [TranslationData(SectionSpeed, IsPriorityTranslation = false)]
    public static readonly Translation<string> SpeedMultiplierInvalidValue = new Translation<string>("<#b3a6a2>Speed multiplier <#fff>{0}</color> is invalid.");
    [TranslationData(SectionSpeed, IsPriorityTranslation = false)]
    public static readonly Translation<float> SpeedMultiplierAlreadySet = new Translation<float>("<#b3a6a2>Speed multiplier is already set to <#fff>{0}</color>.");
    [TranslationData(SectionSpeed, IsPriorityTranslation = false)]
    public static readonly Translation<float, IPlayer> SetSpeedMultiplier = new Translation<float, IPlayer>("<#d1bda7>Set {0}'s speed multiplier to <#fff>{0}</color>.", arg0Fmt: "0.##", arg1Fmt: UCPlayer.FormatColoredCharacterName);
    #endregion

    #region Jump Command
    private const string SectionJump = "Speed Command";
    [TranslationData(SectionJump, IsPriorityTranslation = false)]
    public static readonly Translation<string> JumpMultiplierInvalidValue = new Translation<string>("<#b3a6a2>Jump multiplier <#fff>{0}</color> is invalid.");
    [TranslationData(SectionJump, IsPriorityTranslation = false)]
    public static readonly Translation<float> JumpMultiplierAlreadySet = new Translation<float>("<#b3a6a2>Jump multiplier is already set to <#fff>{0}</color>.");
    [TranslationData(SectionJump, IsPriorityTranslation = false)]
    public static readonly Translation<float, IPlayer> SetJumpMultiplier = new Translation<float, IPlayer>("<#d1bda7>Set {0}'s speed multiplier to <#fff>{0}</color>.", arg0Fmt: "0.##", arg1Fmt: UCPlayer.FormatColoredCharacterName);
    #endregion

    #region Strutures
    private const string SectionStructures = "Structures";
    [TranslationData(SectionStructures)]
    public static readonly Translation StructureNoTarget = new Translation("<#ff8c69>You must be looking at a barricade, structure, or vehicle.");
    [TranslationData(SectionStructures, IsPriorityTranslation = false)]
    public static readonly Translation<ItemAsset> StructureSaved = new Translation<ItemAsset>("<#e6e3d5>Saved <#c6d4b8>{0}</color>.");
    [TranslationData(SectionStructures, IsPriorityTranslation = false)]
    public static readonly Translation<ItemAsset> StructureAlreadySaved = new Translation<ItemAsset>("<#e6e3d5><#c6d4b8>{0}</color> is already saved.");
    [TranslationData(SectionStructures, IsPriorityTranslation = false)]
    public static readonly Translation<ItemAsset> StructureUnsaved = new Translation<ItemAsset>("<#e6e3d5>Removed <#c6d4b8>{0}</color> save.");
    [TranslationData(SectionStructures, IsPriorityTranslation = false)]
    public static readonly Translation<ItemAsset> StructureAlreadyUnsaved = new Translation<ItemAsset>("<#ff8c69><#c6d4b8>{0}</color> is not saved.");
    [TranslationData(SectionStructures, IsPriorityTranslation = false)]
    public static readonly Translation<Asset> StructureDestroyed = new Translation<Asset>("<#e6e3d5>Destroyed <#c6d4b8>{0}</color>.");
    [TranslationData(SectionStructures, IsPriorityTranslation = false)]
    public static readonly Translation StructureNotDestroyable = new Translation("<#ff8c69>That object can not be destroyed.");
    [TranslationData(SectionStructures)]
    public static readonly Translation StructureExamineNotExaminable = new Translation("<#ff8c69>That object can not be examined.");
    [TranslationData(SectionStructures)]
    public static readonly Translation StructureExamineNotLocked = new Translation("<#ff8c69>This vehicle is not locked.");
    [TranslationData(SectionStructures)]
    public static readonly Translation<Asset, IPlayer, FactionInfo> StructureExamineLastOwnerPrompt = new Translation<Asset, IPlayer, FactionInfo>("Last owner of {0}: {1}, Team: {2}.", TranslationOptions.TMProUI | TranslationOptions.NoRichText, arg1Fmt: UCPlayer.FormatPlayerName, arg2Fmt: FactionInfo.FormatDisplayName);
    [TranslationData(SectionStructures)]
    public static readonly Translation<Asset, IPlayer, IPlayer, FactionInfo> StructureExamineLastOwnerChat = new Translation<Asset, IPlayer, IPlayer, FactionInfo>("<#c6d4b8>Last owner of <#e6e3d5>{0}</color>: {1} <i>({2})</i>, Team: {3}.", TranslationOptions.TMProUI | TranslationOptions.NoRichText, FormatRarityColor, arg1Fmt: UCPlayer.FormatColoredPlayerName, arg2Fmt: UCPlayer.FormatSteam64, arg3Fmt: FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionStructures)]
    public static readonly Translation<Asset, IPlayer, FactionInfo, string, ulong> VehicleExamineLastOwnerPrompt = new Translation<Asset, IPlayer, FactionInfo, string, ulong>("Owner of {0}: {1}, Team: {2}. Previous Owner: {3} ({4}).", TranslationOptions.TMProUI | TranslationOptions.NoRichText, arg1Fmt: UCPlayer.FormatPlayerName, arg2Fmt: FactionInfo.FormatDisplayName);
    [TranslationData(SectionStructures)]
    public static readonly Translation<Asset, IPlayer, IPlayer, FactionInfo, string, ulong> VehicleExamineLastOwnerChat = new Translation<Asset, IPlayer, IPlayer, FactionInfo, string, ulong>("<#c6d4b8>Owner of <#e6e3d5>{0}</color>: {1} <i>({2})</i>, Team: {3}. Previous Owner: {4} <i>({5})</i>.", TranslationOptions.TMProUI | TranslationOptions.NoRichText, FormatRarityColor, arg1Fmt: UCPlayer.FormatColoredPlayerName, arg2Fmt: UCPlayer.FormatSteam64, arg3Fmt: FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionStructures, IsPriorityTranslation = false)]
    public static readonly Translation<string> StructureSaveInvalidProperty = new Translation<string>("<#ff8c69>{0} isn't a valid a structure property. Try putting 'owner' or 'group'.");
    [TranslationData(SectionStructures, IsPriorityTranslation = false)]
    public static readonly Translation<string, string> StructureSaveInvalidSetValue = new Translation<string, string>("<#ff8c69><#ddd>{0}</color> isn't a valid value for structure property: <#a0ad8e>{1}</color>.");
    [TranslationData(SectionStructures, IsPriorityTranslation = false)]
    public static readonly Translation<string> StructureSaveNotJsonSettable = new Translation<string>("<#ff8c69><#a0ad8e>{0}</color> is not marked as settable.");
    [TranslationData(SectionStructures, IsPriorityTranslation = false)]
    public static readonly Translation<string, ItemAsset, string> StructureSaveSetProperty = new Translation<string, ItemAsset, string>("<#a0ad8e>Set <#8ce4ff>{0}</color> for {1} save to: <#ffffff>{2}</color>.", arg1Fmt: FormatRarityColor);
    #endregion

    #region Vehicles
    private const string SectionVehicles = "Vehicles";
    private const string SectionVehicleBay = "Vehicle Bay";
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset> VehicleStaging = new Translation<VehicleAsset>("<#b3a6a2>You can't enter a {0} during the <#cedcde>STAGING PHASE</color>.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleWaitForOwner = new Translation<IPlayer>("<#bda897>Only the owner, {0}, can enter the driver's seat right now.", arg0Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer, Squad> VehicleWaitForOwnerOrSquad = new Translation<IPlayer, Squad>("<#bda897>Only the owner, {0}, or members of {1} Squad can enter the driver's seat right now.", arg0Fmt: UCPlayer.FormatColoredCharacterName, arg1Fmt: Squad.FormatColorName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation VehicleNoKit = new Translation("<#ff684a>You can't get in a vehicle without a kit.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation VehicleTooHigh = new Translation("<#ff684a>The vehicle is too high off the ground to exit.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation VehicleAbandoningPilot = new Translation("<#ff684a>You cannot abandon the pilot's seat right now.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<Class> VehicleMissingKit = new Translation<Class>("<#bda897>You need a <#cedcde>{0}</color> kit in order to man this vehicle.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation VehicleDriverNeeded = new Translation("<#bda897>Your vehicle needs a <#cedcde>DRIVER</color> before you can switch to the gunner's seat on the battlefield.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation VehicleAbandoningDriver = new Translation("<#bda897>You cannot abandon the driver's seat on the battlefield.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation VehicleNoPassengerSeats = new Translation("<#bda897>There are no free passenger seats in this vehicle.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation VehicleEnterGameNotStarted = new Translation("<#ff8c69>You may not enter a vehicle right now, the game has not started.");

    [TranslationData(SectionVehicles)]
    public static readonly Translation VehicleMustBeLookingAtLinkedVehicle = new Translation("<#ff8c69>You must be looking at a vehicle or own only one nearby.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<FactionInfo> VehicleNotOnSameTeam = new Translation<FactionInfo>("<#ff8c69>This vehicle is on {0} but you're not.", arg0Fmt: FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleLinkedVehicleNotOwnedByCaller = new Translation<IPlayer>("<#ff8c69>This vehicle is owned by {0}.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, IPlayer> VehicleGiven = new Translation<VehicleAsset, IPlayer>("<#d1bda7>Gave your <#a0ad8e>{0}</color> to {1}.", arg1Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, IPlayer> VehicleGivenDm = new Translation<VehicleAsset, IPlayer>("<#d1bda7>{1} gave you their <#a0ad8e>{0}</color>.", arg1Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleTargetNotInVehicle = new Translation<IPlayer>("<#ff8c69>{0} is not in a vehicle.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<int> VehicleSeatNotValidOutOfRange = new Translation<int>("<#ff8c69>That vehicle doesn't have <#ddd>{0}</color> ${p:0:seat}.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<string> VehicleSeatNotValidText = new Translation<string>("<#ff8c69>Unable to choose a seat from \"<#fff>{0}</color>\".");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<int> VehicleSeatNotOccupied = new Translation<int>("<#ff8c69>Seat <#ddd>#{0}</color> is not occupied.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, IPlayer, int> VehicleOwnerKickedDM = new Translation<VehicleAsset, IPlayer, int>("<#d1a8a8>The owner of the <#ccc>{0}</color>, {1}, kicked you out of seat <#ddd>#{2}</color>.", arg1Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, IPlayer, int> VehicleOwnerTookSeatDM = new Translation<VehicleAsset, IPlayer, int>("<#d1a8a8>The owner of the <#ccc>{0}</color>, {1}, took seat <#ddd>#{2}</color> from you.", arg1Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, IPlayer, int> VehicleKickedPlayer = new Translation<VehicleAsset, IPlayer, int>("<#d1bda7>Kicked {1} out of seat <#ddd>#{2}</color> in your <#ccc>{0}</color>.", arg1Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleSwappedSeats = new Translation<IPlayer>("<#d1bda7>Swapped seats with {0}.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, int> VehicleEnterFailed = new Translation<VehicleAsset, int>("<#ff8c69>Unable to put you in seat <#ddd>#{1}</color> of your <#ccc>{0}</color>.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, int> VehicleEnterForceSwapped = new Translation<VehicleAsset, int>("<#d1bda7>Put you in seat <#ddd>#{1}</color> of your <#ccc>{0}</color>.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, IPlayer> VehicleSwapRequestNotInSameVehicle = new Translation<VehicleAsset, IPlayer>("<#ff8c69>You must be in the same <#ccc>{0}</color> as {1}.", arg1Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer, int, IPlayer> VehicleSwapRequestAlreadySent = new Translation<IPlayer, int, IPlayer>("<#ff8c69>{0} already has a pending swap request from {2}, try again in <#ccc>{1}</color> ${p:1:second}.", arg0Fmt: UCPlayer.FormatColoredNickName, arg2Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer, int, string> VehicleSentSwapRequestDm = new Translation<IPlayer, int, string>("<#d1bda7>{0} wants to swap from seat <#ddd>#{1}</color> with you. Do <#fff>{2}</color> to respond.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer, int, int> VehicleSwapRequestSent = new Translation<IPlayer, int, int>("<#d1bda7>Sent {0} a swap request for seat <#ddd>#{1}</color>. They have <#ccc>{2}</color> ${p:2:second} to respond.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleSwapRequestDeniedByTarget = new Translation<IPlayer>("<#d1a8a8>{0} denied your swap request.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleSwapRequestTimedOutByTarget = new Translation<IPlayer>("<#d1a8a8>{0} didn't respond to your swap request.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleSwapRequestAcceptedByTarget = new Translation<IPlayer>("<#d1bda7>{0} accepted your swap request.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation VehicleSwapRequestNotSent = new Translation("<#d1a8a8>You do not have any pending swap requests.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleSwapRequestDenied = new Translation<IPlayer>("<#d1a8a8>Denied {0}'s swap request.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleSwapRequestAccepted = new Translation<IPlayer>("<#d1bda7>Accepted {0}'s swap request.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset> VehicleTooFarAway = new Translation<VehicleAsset>("<#ff8c69>Your {0} is too far away.");

    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayAdded = new Translation<VehicleAsset>("<#a0ad8e>Added {0} to the vehicle bay.", arg0Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayRemoved = new Translation<VehicleAsset>("<#a0ad8e>Removed {0} from the vehicle bay.", arg0Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<string, VehicleAsset, string> VehicleBaySetProperty = new Translation<string, VehicleAsset, string>("<#a0ad8e>Set <#8ce4ff>{0}</color> for vehicle {1} to: <#ffffff>{2}</color>.", arg1Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBaySavedMeta = new Translation<VehicleAsset>("<#a0ad8e>Successfuly set the rearm list for vehicle {0} from your inventory.", arg0Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayClearedItems = new Translation<VehicleAsset>("<#a0ad8e>Successfuly cleared the rearm list for vehicle {0} from your inventory.", arg0Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset, int> VehicleBaySetItems = new Translation<VehicleAsset, int>("<#a0ad8e>Successfuly set the rearm list for vehicle {0} from your inventory. It will now drop <#8ce4ff>{1}</color> ${p:1:item} on restock.", arg0Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<byte, VehicleAsset> VehicleBaySeatAdded = new Translation<byte, VehicleAsset>("<#a0ad8e>Made seat <#ffffff>#{0}</color> a crewman seat for {1}.", arg1Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<byte, VehicleAsset> VehicleBaySeatAlreadyAdded = new Translation<byte, VehicleAsset>("<#a0ad8e>Set <#ffffff>#{0}</color> was already a crewman seat for {1}.", arg1Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<byte, VehicleAsset> VehicleBaySeatRemoved = new Translation<byte, VehicleAsset>("<#a0ad8e>Seat <#ffffff>#{0}</color> is no longer a crewman seat for {1}.", arg1Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<byte, VehicleAsset> VehicleBaySeatNotAdded = new Translation<byte, VehicleAsset>("<#a0ad8e>Seat <#ffffff>#{0}</color> wasn't a crewman seat for {1}.", arg1Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation VehicleBayNoTarget = new Translation("<#ff8c69>Look at a vehicle, spawn pad, or sign to use this command.");
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayAlreadyAdded = new Translation<VehicleAsset>("<#ff8c69>{0} is already added to the vehicle bay.", arg0Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayNotAdded = new Translation<VehicleAsset>("<#ff8c69>{0} has not been added to the vehicle bay.", arg0Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<string> VehicleBayInvalidProperty = new Translation<string>("<#ff8c69>{0} isn't a valid a vehicle property. Try putting 'Faction', 'RespawnTime', 'CreditCost' etc.");
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<string, string> VehicleBayInvalidSetValue = new Translation<string, string>("<#ff8c69><#ddd>{0}</color> isn't a valid value for vehicle property: <#a0ad8e>{1}</color>.");
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<string> VehicleBayNotCommandSettable = new Translation<string>("<#ff8c69><#a0ad8e>{0}</color> is not marked as settable.");
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<byte, VehicleAsset> VehicleBayCrewSeatAlreadySet = new Translation<byte, VehicleAsset>("<#ff8c69><#ffffff>#{0}</color> is already marked as a crew seat in {1}.", arg1Fmt: FormatRarityColor + FormatPlural);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<byte, VehicleAsset> VehicleBayCrewSeatNotSet = new Translation<byte, VehicleAsset>("<#ff8c69><#ffffff>#{0}</color> isn't marked as a crew seat in {1}.", arg1Fmt: FormatRarityColor + FormatPlural);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<DelayType, float, string?> VehicleBayAddedDelay = new Translation<DelayType, float, string?>("<#a0ad8e>Added delay of type <#fff>{0}</color>:<#ddd>{1}</color> during <#ddd>{2}</color> gamemode.", arg1Fmt: "N1");
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<int> VehicleBayRemovedDelay = new Translation<int>("<#a0ad8e>Removed {0} matching ${p:0:delay}.");
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBaySpawnRegistered = new Translation<VehicleAsset>("<#a0ad8e>Successfully registered spawn. {0} will spawn here.", arg0Fmt: FormatRarityColor + FormatPlural);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBaySpawnDeregistered = new Translation<VehicleAsset>("<#a0ad8e>Successfully deregistered {0} spawn.", arg0Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation VehicleBayLinkStarted = new Translation("<#a0ad8e>Started linking, do <#ddd>/vb link</color> on the sign now.");
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayLinkFinished = new Translation<VehicleAsset>("<#a0ad8e>Successfully linked vehicle sign to a {0} vehicle bay.", arg0Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayUnlinked = new Translation<VehicleAsset>("<#a0ad8e>Successfully unlinked {0} vehicle sign.", arg0Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation VehicleBayLinkNotStarted = new Translation("<#ff8c69>You must do /vb link on a vehicle bay first.");
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayForceSuccess = new Translation<VehicleAsset>("<#a0ad8e>Skipped timer for that {0} vehicle bay.", arg0Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<string> VehicleBayInvalidInput = new Translation<string>("<#ff8c69><#fff>{0}</color> is not a valid vehicle.");
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<ItemAsset> VehicleBayInvalidBayItem = new Translation<ItemAsset>("<#ff8c69>{0} are not valid vehicle bays.", arg0Fmt: FormatRarityColor + FormatPlural);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBaySpawnAlreadyRegistered = new Translation<VehicleAsset>("<#ff8c69>This spawn is already registered to a {0}. Unregister it first with <#fff>/vb unreg</color>.", arg0Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation VehicleBaySpawnNotRegistered = new Translation("<#ff8c69>This vehicle bay is not registered.");
    [TranslationData(SectionVehicleBay, IsPriorityTranslation = false)]
    public static readonly Translation<uint, VehicleAsset, ushort> VehicleBayCheck = new Translation<uint, VehicleAsset, ushort>("<#a0ad8e>This spawn (<#8ce4ff>{0}</color>) is registered with vehicle: {1} <#fff>({2})</color>.", arg1Fmt: FormatRarityColor);
    #endregion

    #region Vehicle Deaths
    private const string SectionVehicleDeathMessages = "Vehicle Death Messages";
    [TranslationData(SectionVehicleDeathMessages)]
    public static readonly Translation<IPlayer, VehicleAsset, string, float, string> VehicleDestroyed = new Translation<IPlayer, VehicleAsset, string, float, string>("<#c$death_background$>{0} took out a <#{4}>{1}</color> with a {2} from {3}m away.", UCPlayer.FormatColoredCharacterName, arg2Fmt: "F0");
    [TranslationData(SectionVehicleDeathMessages)]
    public static readonly Translation<IPlayer, VehicleAsset, string> VehicleDestroyedUnknown = new Translation<IPlayer, VehicleAsset, string>("<#c$death_background$>{0} took out a <#{2}>{1}</color>.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionVehicleDeathMessages)]
    public static readonly Translation<IPlayer, VehicleAsset, string> VehicleTeamkilled = new Translation<IPlayer, VehicleAsset, string>("<#c$death_background_teamkill$>{0} blew up a friendly <#{2}>{1}</color>.", UCPlayer.FormatColoredCharacterName);
    #endregion

    #region Shutdown
    private const string SectionShutdown = "Shutdown Broadcasts";
    [TranslationData(SectionShutdown)]
    public static readonly Translation<string> ShutdownBroadcastAfterGame = new Translation<string>("<#00ffff>A shutdown has been scheduled after this game because: \"<#6699ff>{0}</color>\".");
    [TranslationData(SectionShutdown)]
    public static readonly Translation<string> ShutdownBroadcastDaily = new Translation<string>("<#00ffff>A daily restart will occur after this game. Down-time estimate: <#6699ff>2 minutes</color>.", TranslationOptions.SuppressWarnings);
    [TranslationData(SectionShutdown)]
    public static readonly Translation ShutdownBroadcastCancelled = new Translation("<#00ffff>The scheduled shutdown has been canceled.");
    [TranslationData(SectionShutdown)]
    public static readonly Translation<string, string> ShutdownBroadcastTime = new Translation<string, string>("<#00ffff>A shutdown has been scheduled in {0} because: \"<color=#6699ff>{1}</color>\".");
    [TranslationData(SectionShutdown)]
    public static readonly Translation<string> ShutdownBroadcastReminder = new Translation<string>("<#00ffff>A shutdown is scheduled to occur after this game because: \"<#6699ff>{0}</color>\".");
    #endregion

    #region Traits
    private const string SectionTraits = "Traits";
    [TranslationData(SectionTraits, "Sent when the player leaves their post as squad leader while under the effect of a trait requiring squad leader.", "The trait requiring squad leader")]
    public static readonly Translation<Trait> TraitDisabledSquadLeaderDemoted = new Translation<Trait>("<#e86868><#c$trait$>{0}</color> is disabled until it expires or you become <#cedcde>SQUAD LEADER</color> again.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player leaves a squad while under the effect of a trait requiring a squad.", "The trait requiring a squad")]
    public static readonly Translation<Trait> TraitDisabledSquadLeft = new Translation<Trait>("<#e86868><#c$trait$>{0}</color> is disabled until you join a <#cedcde>SQUAD</color> again.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player equips a kit that's not supported by the trait.", "The trait requiring a kit")]
    public static readonly Translation<Trait> TraitDisabledKitNotSupported = new Translation<Trait>("<#e86868><#c$trait$>{0}</color> is disabled until you switch to a supported kit type.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player performs an action that allows their trait to be reactivated.", "The trait being reactivated")]
    public static readonly Translation<Trait> TraitReactivated = new Translation<Trait>("<#e86868><#c$trait$>{0}</color> has been reactivated.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when one of a player's traits expires through time.", "The trait that expired")]
    public static readonly Translation<TraitData> TraitExpiredTime = new Translation<TraitData>("<#e86868><#c$trait$>{0}</color> has expired and is no longer active.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when one of a player's traits expires through death.", "The trait that expired")]
    public static readonly Translation<TraitData> TraitExpiredDeath = new Translation<TraitData>("<#e86868><#c$trait$>{0}</color> has expired after your death and is no longer active.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait which is locked by the current gamemode.", "The locked trait", "Current gamemode")]
    public static readonly Translation<TraitData, Gamemode> RequestTraitGamemodeLocked = new Translation<TraitData, Gamemode>("<#ff8c69><#c$trait$>{0}</color> is <#c$locked$>locked</color> during <#cedcde><uppercase>{1}</uppercase></color> games.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait while they already have it.", "The existing trait")]
    public static readonly Translation<TraitData> TraitAlreadyActive = new Translation<TraitData>("<#ff8c69>You are already under <#c$trait$>{0}</color>'s effects.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait meant for another team.", "The trait", "Trait's intended team")]
    public static readonly Translation<TraitData, FactionInfo> RequestTraitWrongTeam = new Translation<TraitData, FactionInfo>("<#ff8c69>You can only use <#c$trait$>{0}</color> on {1}.", arg0Fmt: TraitData.FormatName, arg1Fmt: FactionInfo.FormatColorShortName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait without a kit.")]
    public static readonly Translation RequestTraitNoKit = new Translation("<#ff8c69>Request a kit before trying to request traits.");
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait with a kit class the trait doesn't allow.", "The trait", "Invalid class")]
    public static readonly Translation<TraitData, Class> RequestTraitClassLocked = new Translation<TraitData, Class>("<#ff8c69>You can't use <#c$trait$>{0}</color> while a <#cedcde><uppercase>{1}</uppercase></color> kit is equipped.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait while under the global trait cooldown.", "Global cooldown shared between all traits")]
    public static readonly Translation<Cooldown> RequestTraitGlobalCooldown = new Translation<Cooldown>("<#ff8c69>You can request a trait again in <#cedcde>{0}</color>.", arg0Fmt: Cooldown.FormatTimeShort);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait while under the individual trait cooldown.", "Trait on cooldown", "Individual cooldown for this trait")]
    public static readonly Translation<TraitData, Cooldown> RequestTraitSingleCooldown = new Translation<TraitData, Cooldown>("<#ff8c69>You can request <#c$trait$>{0}</color> again in <#cedcde>{1}</color>.", arg0Fmt: TraitData.FormatName, arg1Fmt: Cooldown.FormatTimeShort);
    [TranslationData(SectionTraits, "Sent when the player tries to request a buff when they already have the max amount (6).")]
    public static readonly Translation RequestTraitTooManyBuffs = new Translation("<#ff8c69>You can't have more than <#cedcde>six</color> buffs active at once.");
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait which requires squad leader while not being squad leader or in a squad.", "Trait being requested")]
    public static readonly Translation<TraitData> RequestTraitNotSquadLeader = new Translation<TraitData>("<#ff8c69>You have to be a <#cedcde>SQUAD LEADER</color> to request <#c$trait$>{0}</color>.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait which requires squad leader while not in a squad.", "Trait being requested")]
    public static readonly Translation<TraitData> RequestTraitNoSquad = new Translation<TraitData>("<#ff8c69>You have to be in a <#cedcde>SQUAD</color> to request <#c$trait$>{0}</color>.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait while too low of a level.", "Trait being requested", "Required Level")]
    public static readonly Translation<TraitData, LevelData> RequestTraitLowLevel = new Translation<TraitData, LevelData>("<#ff8c69>You must be at least <#cedcde>{1}</color> to request <#c$trait$>{0}</color>.", arg0Fmt: TraitData.FormatName, arg1Fmt: LevelData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait while too low of a rank.", "Trait being requested", "Required Rank")]
    public static readonly Translation<TraitData, RankData> RequestTraitLowRank = new Translation<TraitData, RankData>("<#ff8c69>You must be at least {1} to request <#c$trait$>{0}</color>.", arg0Fmt: TraitData.FormatName, arg1Fmt: RankData.FormatColorName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait while missing a completed quest.", "Trait being requested", "Required Rank")]
    public static readonly Translation<TraitData, QuestAsset> RequestTraitQuestIncomplete = new Translation<TraitData, QuestAsset>("<#ff8c69>You must be at least {1} to request <#c$trait$>{0}</color>.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player successfully requests a trait.", "Trait being requested")]
    public static readonly Translation<TraitData> RequestTraitGiven = new Translation<TraitData>("<#a8918a>Your <#c$trait$>{0}</color> has been activated.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player successfully requests a trait with a timer.", "Trait being requested", "Time left")]
    public static readonly Translation<TraitData, string> RequestTraitGivenTimer = new Translation<TraitData, string>("<#a8918a>Your <#c$trait$>{0}</color> has been activated. It will expire in <#cedcde>{1}</color>.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player successfully requests a trait that expires on death.", "Trait being requested")]
    public static readonly Translation<TraitData> RequestTraitGivenUntilDeath = new Translation<TraitData>("<#a8918a>Your <#c$trait$>{0}</color> has been activated. It will last until you die.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player successfully requests a trait but it's still staging phase.", "Trait being requested")]
    public static readonly Translation<TraitData> TraitAwaitingStagingPhase = new Translation<TraitData>("<#a8918a><#c$trait$>{0}</color> will be activated once <#cedcde>STAGING PHASE</color> is over.", arg0Fmt: TraitData.FormatName);
    #region Trait Command
    private const string SectionTraitCommand = "Traits / Trait Command";
    [TranslationData(SectionTraitCommand, "Shown when a trait name is not able to be matched up with a TraitData.", "Inputted search")]
    public static readonly Translation<string> TraitNotFound = new Translation<string>("<#66ffcc>Unable to find a trait named <#fff>{0}</color>.");
    [TranslationData(SectionTraitCommand, "Shown when a trait is removed.", "Trait that got removed.")]
    public static readonly Translation<TraitData> TraitRemoved = new Translation<TraitData>("<#66ffcc>Removed <#c$trait$>{0}</color>.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraitCommand, "Shown when someone tries to remove a trait which they don't have.", "Trait the player tried to remove")]
    public static readonly Translation<TraitData> TraitNotActive = new Translation<TraitData>("<#ff8c69>You're not under <#c$trait$>{0}</color>'s effects.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraitCommand, "Shown when someone tries to clear their traits with no traits.")]
    public static readonly Translation NoTraitsToClear = new Translation("<#ff8c69>You have no active traits.");
    [TranslationData(SectionTraitCommand, "Shown when someone clears their traits.", "Number of traits removed.")]
    public static readonly Translation<int> TraitsCleared = new Translation<int>("<#66ffcc>Removed {0} trait(s).");
    [TranslationData(SectionTraitCommand, "Shown when someone clears their traits.", "Target trait", "Property name", "Value")]
    public static readonly Translation<TraitData, string, string> TraitSetProperty = new Translation<TraitData, string, string>("<#66ffcc>Set <#c$trait$>{0}</color> / <#fff>{1}</color> to <uppercase><#cedcde>{2}</color></uppercase>.", arg0Fmt: TraitData.FormatTypeName);
    [TranslationData(SectionTraitCommand, "Shown when someone enteres an invalid property name to /trait set.", "Input text")]
    public static readonly Translation<string> TraitInvalidProperty = new Translation<string>("<#ff8c69><uppercase><#cedcde>{0}</color></uppercase> is not a valid property name for traits.");
    [TranslationData(SectionTraitCommand, "Shown when someone enteres an invalid property name to /trait set.", "Value", "Property name")]
    public static readonly Translation<string, string, Type> TraitInvalidSetValue = new Translation<string, string, Type>("<#ff8c69><uppercase><#cedcde>{0}</color></uppercase> is not a valid value for <#fff>{1}</color> (expected {2}).");
    [TranslationData(SectionTraitCommand, "Shown when someone enteres an invalid property name to /trait set.", "Property name")]
    public static readonly Translation<string> TraitNotJsonSettable = new Translation<string>("<#ff8c69><#fff>{0}</color> is not a property that can be changed in-game.");
    #endregion
    #region Trait Signs
    private const string SectionTraitSigns = "Traits / Sign";
    [TranslationData(SectionTraitSigns, "Shows instead of the credits when Credit Cost is 0.")]
    public static readonly Translation TraitSignFree = new Translation("<#c$kit_level_dollars_owned$>FREE</color>");
    [TranslationData(SectionTraitSigns, "Shows instead of the unlock requirements when a trait is unlocked.")]
    public static readonly Translation TraitSignUnlocked = new Translation("<#99ff99>Unlocked</color>");
    [TranslationData(SectionTraitSigns, "Shows when you're not in a squad and it's required.")]
    public static readonly Translation TraitSignRequiresSquad = new Translation("<#c$vbs_delay$>Join a Squad</color>");
    [TranslationData(SectionTraitSigns, "Shows when you're not in a squad or not a squad leader and it's required.")]
    public static readonly Translation TraitSignRequiresSquadLeader = new Translation("<#c$vbs_delay$>Squad Leaders Only</color>");
    [TranslationData(SectionTraitSigns, "Shows when you dont have a kit or have an unarmed kit.")]
    public static readonly Translation TraitSignNoKit = new Translation("<#c$vbs_delay$>Request a Kit</color>");
    [TranslationData(SectionTraitSigns, "Shows when the trait is locked in the current gamemode.")]
    public static readonly Translation TraitGamemodeBlacklisted = new Translation("<#c$vbs_delay$>Locked</color>");
    [TranslationData(SectionTraitSigns, "Shows when the kit class you have isn't compatible with the trait.", "Class name")]
    public static readonly Translation<Class> TraitSignClassBlacklisted = new Translation<Class>("<#c$vbs_delay$>Locked for {0}</color>", arg0Fmt: FormatPlural);
    [TranslationData(SectionTraitSigns, "Shows when the kit class you have isn't compatible with the trait and theres a kit whitelist with 1 class.", "Class name")]
    public static readonly Translation<Class> TraitSignClassWhitelisted1 = new Translation<Class>("<#c$vbs_delay$>{0} Required</color>");
    [TranslationData(SectionTraitSigns, "Shows when the kit class you have isn't compatible with the trait and theres a kit whitelist with 2 classes.", "Class name")]
    public static readonly Translation<Class, Class> TraitSignClassWhitelisted2 = new Translation<Class, Class>("<#c$vbs_delay$>{0} or {1} Required</color>");
    [TranslationData(SectionTraitSigns, "Shows when you currently have the trait and it expires in time.", "Minutes", "Seconds")]
    public static readonly Translation<int, int> TraitSignAlreadyActiveTime = new Translation<int, int>("<#c$vbs_delay$>Already Active: {0}:{1}</color>", arg1Fmt: "D2");
    [TranslationData(SectionTraitSigns, "Shows when you currently have the trait and it expires on death.")]
    public static readonly Translation TraitSignAlreadyActiveDeath = new Translation("<#c$vbs_delay$>Already Active</color>");
    [TranslationData(SectionTraitSigns, "Shows when you are on either global or individual cooldown (whichever is longer).", "Minutes", "Seconds")]
    public static readonly Translation<int, int> TraitSignCooldown = new Translation<int, int>("<#c$vbs_delay$>On Cooldown: {0}:{1}</color>", arg1Fmt: "D2");
    #endregion
    #region Trait Interactions
    private const string SectionTraitInteractions = "Traits / Interactions";
    [TranslationData(SectionTraitInteractions, "Sent when the player consumes their self-revive.", "Self-revive trait data.")]
    public static readonly Translation<TraitData> TraitUsedSelfRevive = new Translation<TraitData>("<#c$trait$>{0}</color> <#d97568>consumed</color>.", arg0Fmt: TraitData.FormatName);
    [TranslationData(SectionTraitInteractions, "Sent when the player tries to use their self-revive on cooldown.", "Self-revive trait data.", "Time string")]
    public static readonly Translation<TraitData, TimeSpan> TraitSelfReviveCooldown = new Translation<TraitData, TimeSpan>("<#c$trait$>{0}</color> can not be used for another {1}.", arg0Fmt: TraitData.FormatName, arg1Fmt: TimeAddon.Create(TimeFormatType.Long));
    [TranslationData(SectionTraitInteractions, "Sent when the player isn't in a vehicle with Ace Armor.", "Ace armor trait data.")]
    public static readonly Translation<TraitData> AceArmorDisabledNotInVehicle = new Translation<TraitData>("<#e86868><#c$trait$>{0}</color> is disabled until you are driving an <#cedcde>ARMORED</color> vehicle.", arg0Fmt: TraitData.FormatName);
    #endregion
    #endregion

    #region Request Signs
    private const string SectionRequestSigns = "Kit Signs";
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitFree = new Translation("<#c$kit_free$>FREE</color>");
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitExclusive = new Translation("<#c$kit_level_dollars_exclusive$>EXCLUSIVE</color>");
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitNitroBoostOwned = new Translation("<#f66fe6>BOOSTING</color>");
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitNitroBoostNotOwned = new Translation("<#9b59b6>NITRO BOOST</color>");
    [TranslationData(SectionRequestSigns, IsPriorityTranslation = false)]
    public static readonly Translation<string> KitName = new Translation<string>("<b>{0}</b>");
    [TranslationData(SectionRequestSigns, IsPriorityTranslation = false)]
    public static readonly Translation<string> KitWeapons = new Translation<string>("<b>{0}</b>");
    [TranslationData(SectionRequestSigns, IsPriorityTranslation = false)]
    public static readonly Translation<decimal> KitPremiumCost = new Translation<decimal>("<#c$kit_level_dollars$>$ {0}</color>", arg0Fmt: "N2");
    [TranslationData(SectionRequestSigns, Parameters = [ "Level", "Color depending on player's current level." ])]
    public static readonly Translation<string, Color> KitRequiredLevel = new Translation<string, Color>("<#f0a31c>Rank:</color> <#{1}>{0}</color>");
    [TranslationData(SectionRequestSigns, Parameters = [ "Rank", "Color depending on player's current rank." ])]
    public static readonly Translation<RankData, Color> KitRequiredRank = new Translation<RankData, Color>("<#{1}>Rank: {0}</color>");
    [TranslationData(SectionRequestSigns, Parameters = [ "Quest", "Color depending on whether the player has completed the quest." ])]
    public static readonly Translation<QuestAsset, Color> KitRequiredQuest = new Translation<QuestAsset, Color>("<#{1}>Quest: <#fff>{0}</color></color>");
    [TranslationData(SectionRequestSigns, Parameters = [ "Number of quests needed.", "Color depending on whether the player has completed the quest(s).", "s if {0} != 1" ])]
    public static readonly Translation<int, Color, string> KitRequiredQuestsMultiple = new Translation<int, Color, string>("<#{1}>Finish <#fff>{0}</color> quest{2}.</color>");
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitRequiredQuestsComplete = new Translation("<#ff974d>Kit Unlocked</color>");
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitPremiumOwned = new Translation("<#c$kit_level_dollars_owned$>OWNED</color>");
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitCommanderTakenByViewer = new Translation("<#c$kit_level_dollars_owned$>You are the <#cedcde>COMMANDER</color>.</color>");
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation<IPlayer> KitCommanderTaken = new Translation<IPlayer>("<#f0a31c>Taken by <#fff>{0}</color></color>", arg0Fmt: UCPlayer.FormatNickName);
    [TranslationData(SectionRequestSigns, IsPriorityTranslation = false)]
    public static readonly Translation<int> KitCreditCost = new Translation<int>("<#c$credits$>C</color> <#fff>{0}</color>");
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitUnlimited = new Translation("<#c$kit_unlimited_players$>unlimited</color>");
    [TranslationData(SectionRequestSigns, IsPriorityTranslation = false)]
    public static readonly Translation<int, int> KitPlayerCount = new Translation<int, int>("{0}/{1}");
    [TranslationData(SectionRequestSigns, IsPriorityTranslation = false)]
    public static readonly Translation<string> LoadoutName = new Translation<string>("<#c$kit_level_dollars$>LOADOUT {0}</color>", arg0Fmt: FormatUppercase);
    [TranslationData(SectionRequestSigns, IsPriorityTranslation = false)]
    public static readonly Translation KitLoadoutUpgrade = new Translation("<#33cc33>/req upgrade</color>");
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitLoadoutSetup = new Translation("<#3399ff>PENDING SETUP</color>");
    #endregion

    #region Vehicle Bay Signs
    private const string SectionVBS = "Vehicle Signs";
    [TranslationData(SectionVBS)]
    public static readonly Translation<int> VBSTickets = new Translation<int>("<#c$vbs_ticket_number$>{0}</color> <#c$vbs_ticket_label$>Tickets</color>");
    [TranslationData(SectionVBS)]
    public static readonly Translation VBSStateReady = new Translation("<#c$vbs_ready$>Ready!</color> <#aaa><b>/request</b></color>");
    [TranslationData(SectionVBS, Parameters = [ "Minutes", "Seconds" ], IsPriorityTranslation = false)]
    public static readonly Translation<int, int> VBSStateDead = new Translation<int, int>("<#c$vbs_dead$>{0}:{1}</color>", arg1Fmt: "D2");
    [TranslationData(SectionVBS, Parameters = [ "Nearest location." ], IsPriorityTranslation = false)]
    public static readonly Translation<string> VBSStateActive = new Translation<string>("<#c$vbs_active$>{0}</color>");
    [TranslationData(SectionVBS, Parameters = [ "Minutes", "Seconds" ])]
    public static readonly Translation<int, int> VBSStateIdle = new Translation<int, int>("<#c$vbs_idle$>Idle: {0}:{1}</color>", arg1Fmt: "D2");
    [TranslationData(SectionVBS)]
    public static readonly Translation VBSDelayStaging = new Translation("<#c$vbs_delay$>Locked Until Start</color>");
    [TranslationData(SectionVBS, Parameters = [ "Minutes", "Seconds" ])]
    public static readonly Translation<int, int> VBSDelayTime = new Translation<int, int>("<#c$vbs_delay$>Locked: {0}:{1}</color>", arg1Fmt: "D2");
    [TranslationData(SectionVBS)]
    public static readonly Translation<int> VBSDelayTeammates = new Translation<int>("<#c$vbs_delay$>Locked until {0}v{0}</color>");
    [TranslationData(SectionVBS)]
    public static readonly Translation<Flag> VBSDelayCaptureFlag = new Translation<Flag>("<#c$vbs_delay$>Capture {0}</color>", TranslationOptions.PerTeamTranslation, Flag.SHORT_NAME_DISCOVER_FORMAT);
    [TranslationData(SectionVBS)]
    public static readonly Translation<Flag> VBSDelayLoseFlag = new Translation<Flag>("<#c$vbs_delay$>Lose {0}</color>", TranslationOptions.PerTeamTranslation, Flag.SHORT_NAME_DISCOVER_FORMAT);
    [TranslationData(SectionVBS)]
    public static readonly Translation<int> VBSDelayLoseFlagMultiple = new Translation<int>("<#c$vbs_delay$>Lose {0} more flags.</color>");
    [TranslationData(SectionVBS)]
    public static readonly Translation<int> VBSDelayCaptureFlagMultiple = new Translation<int>("<#c$vbs_delay$>Capture {0} more flags.</color>");
    [TranslationData(SectionVBS)]
    public static readonly Translation<Cache> VBSDelayAttackCache = new Translation<Cache>("<#c$vbs_delay$>Destroy {0}</color>", arg0Fmt: FOB.FormatLocationName);
    [TranslationData(SectionVBS)]
    public static readonly Translation VBSDelayAttackCacheUnknown = new Translation("<#c$vbs_delay$>Destroy Next Cache</color>");
    [TranslationData(SectionVBS)]
    public static readonly Translation<int> VBSDelayAttackCacheMultiple = new Translation<int>("<#c$vbs_delay$>Destroy {0} more caches.</color>");
    [TranslationData(SectionVBS)]
    public static readonly Translation<Cache> VBSDelayDefendCache = new Translation<Cache>("<#c$vbs_delay$>Lose {0}</color>", arg0Fmt: FOB.FormatLocationName);
    [TranslationData(SectionVBS)]
    public static readonly Translation VBSDelayDefendCacheUnknown = new Translation("<#c$vbs_delay$>Lose Next Cache</color>");
    [TranslationData(SectionVBS)]
    public static readonly Translation<int> VBSDelayDefendCacheMultiple = new Translation<int>("<#c$vbs_delay$>Lose {0} more caches.</color>");
    #endregion

    #region Revives
    private const string SectionRevives = "Revives";
    [TranslationData(SectionRevives)]
    public static readonly Translation ReviveNotMedic = new Translation("<#bdae9d>Only a <color=#ff758f>MEDIC</color> can heal or revive teammates.");
    [TranslationData(SectionRevives)]
    public static readonly Translation ReviveHealEnemies = new Translation("<#bdae9d>You cannot aid enemy soldiers.");
    #endregion

    #region Reload Command
    private const string SectionReload = "Reload Command";
    [TranslationData(SectionReload, IsPriorityTranslation = false)]
    public static readonly Translation ReloadedAll = new Translation("<#e6e3d5>Reloaded all Uncreated Warfare components.");
    [TranslationData(SectionReload, IsPriorityTranslation = false)]
    public static readonly Translation<int> ReloadedTranslations = new Translation<int>("<#e6e3d5>Reloaded all translation files. {0} total translation points.");
    [TranslationData(SectionReload, IsPriorityTranslation = false)]
    public static readonly Translation ReloadedFlags = new Translation("<#e6e3d5>Reloaded flag data.");
    [TranslationData(SectionReload, IsPriorityTranslation = false)]
    public static readonly Translation ReloadFlagsInvalidGamemode = new Translation("<#ff8c69>You must be on a flag gamemode to use this command!");
    [TranslationData(SectionReload, IsPriorityTranslation = false)]
    public static readonly Translation ReloadedPermissions = new Translation("<#e6e3d5>Reloaded the permission saver file.");
    [TranslationData(SectionReload, IsPriorityTranslation = false)]
    public static readonly Translation ReloadedTCP = new Translation("<#e6e3d5>Tried to close any existing TCP connection to homebase and re-open it.");
    [TranslationData(SectionReload, IsPriorityTranslation = false)]
    public static readonly Translation ReloadedSQL = new Translation("<#e6e3d5>Reopened the MySql Connection.");
    [TranslationData(SectionReload, IsPriorityTranslation = false)]
    public static readonly Translation<string> ReloadedGeneric = new Translation<string>("<#e6e3d5>Reloaded the '{0}' module.");
    #endregion

    #region Debug Commands
    private const string SectionDebugCommand = "Test Command";
    [TranslationData(SectionDebugCommand)]
    public static readonly Translation<string> DebugNoMethod = new Translation<string>("<#ff8c69>No method found called <#ff758f>{0}</color>.");
    [TranslationData(SectionDebugCommand)]
    public static readonly Translation<string, string> DebugErrorExecuting = new Translation<string, string>("<#ff8c69>Ran into an error while executing: <#ff758f>{0}</color> - <#ff758f>{1}</color>.");
    [TranslationData(SectionDebugCommand)]
    public static readonly Translation<string> DebugMultipleMatches = new Translation<string>("<#ff8c69>Multiple methods match <#ff758f>{0}</color>.");
    #endregion

    #region Phases
    private const string SectionPhases = "Phases";
    [TranslationData(SectionPhases)]
    public static readonly Translation PhaseBriefing                      = new Translation("BRIEFING PHASE", TranslationOptions.TMProUI);
    [TranslationData(SectionPhases)]
    public static readonly Translation PhasePreparation                   = new Translation("PREPARATION PHASE", TranslationOptions.TMProUI);
    [TranslationData(SectionPhases)]
    public static readonly Translation PhaseBreifingInvasionAttack        = new Translation("BRIEFING PHASE", TranslationOptions.TMProUI);
    [TranslationData(SectionPhases)]
    public static readonly Translation<Flag> PhaseBreifingInvasionDefense = new Translation<Flag>("PREPARATION PHASE\nFORTIFY {0}", TranslationOptions.TMProUI, Flag.COLOR_SHORT_NAME_FORMAT);
    #endregion

    #region XP Toasts
    private const string SectionXPCreditsToast = "XP / Credit / Supply Toast Messages";
    [TranslationData(SectionXPCreditsToast, "XP or credits given from the console.")]
    public static readonly Translation XPToastFromOperator = new Translation("FROM OPERATOR", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast, "XP or credits given by an admin.")]
    public static readonly Translation XPToastFromPlayer = new Translation("FROM ADMIN", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastHealedTeammate = new Translation("HEALED TEAMMATE", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastEnemyInjured = new Translation("<color=#e3e3e3>DOWNED</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFriendlyInjured = new Translation("<color=#e3e3e3>DOWNED FRIENDLY</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastEnemyKilled = new Translation("KILLED ENEMY", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastKillAssist = new Translation("ASSIST", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastKillVehicleAssist = new Translation("VEHICLE ASSIST", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastKillDriverAssist = new Translation("DRIVER ASSIST", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastSpotterAssist = new Translation("SPOTTER", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFriendlyKilled = new Translation("TEAMKILLED", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastSuicide = new Translation("SUICIDE", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFOBDestroyed = new Translation("FOB DESTROYED", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFriendlyFOBDestroyed = new Translation("FRIENDLY FOB DESTROYED", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastBunkerDestroyed = new Translation("BUNKER DESTROYED", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFriendlyBunkerDestroyed = new Translation("FRIENDLY BUNKER DESTROYED", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFOBUsed = new Translation("FOB IN USE", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastSuppliesUnloaded = new Translation("RESUPPLIED FOB", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastResuppliedTeammate = new Translation("RESUPPLIED TEAMMATE", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastRepairedVehicle = new Translation("REPAIRED VEHICLE", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFOBRepairedVehicle = new Translation("FOB REPAIRED VEHICLE", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation<VehicleType> XPToastVehicleDestroyed = new Translation<VehicleType>("{0} DESTROYED", TranslationOptions.TMProUI, FormatUppercase);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation<VehicleType> XPToastAircraftDestroyed = new Translation<VehicleType>("{0} SHOT DOWN", TranslationOptions.TMProUI, FormatUppercase);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation<VehicleType> XPToastFriendlyVehicleDestroyed = new Translation<VehicleType>("FRIENDLY {0} DESTROYED", TranslationOptions.TMProUI, FormatUppercase);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation<VehicleType> XPToastFriendlyAircraftDestroyed = new Translation<VehicleType>("FRIENDLY {0} SHOT DOWN", TranslationOptions.TMProUI, FormatUppercase);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastTransportingPlayers = new Translation("TRANSPORTING PLAYERS", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastAceArmorRefund = new Translation("ACE ARMOR SHARE", TranslationOptions.TMProUI);

    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFlagCaptured = new Translation("FLAG CAPTURED", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFlagNeutralized = new Translation("FLAG NEUTRALIZED", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFlagAttackTick = new Translation("ATTACK", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFlagDefenseTick = new Translation("DEFENSE", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastCacheDestroyed = new Translation("CACHE DESTROYED", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFriendlyCacheDestroyed = new Translation("FRIENDLY CACHE DESTROYED", TranslationOptions.TMProUI);

    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastSquadBonus = new Translation("SQUAD BONUS", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastOnDuty = new Translation("ON DUTY", TranslationOptions.TMProUI);

    [TranslationData(SectionXPCreditsToast, IsPriorityTranslation = false)]
    public static readonly Translation<int> FOBToastGainBuild = new Translation<int>("<color=#c$build$>+{0} BUILD</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPriorityTranslation = false)]
    public static readonly Translation<int> FOBToastLoseBuild = new Translation<int>("<color=#c$build$>-{0} BUILD</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPriorityTranslation = false)]
    public static readonly Translation<int> FOBToastGainAmmo = new Translation<int>("<color=#c$ammo$>+{0} AMMO</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPriorityTranslation = false)]
    public static readonly Translation<int> FOBToastLoseAmmo = new Translation<int>("<color=#c$ammo$>-{0} AMMO</color>", TranslationOptions.TMProUI);

    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation FOBResourceToastLoadSupplies = new Translation("LOAD SUPPLIES");
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation FOBResourceToastRearmVehicle = new Translation("REARM VEHICLE");
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation FOBResourceToastRearmPlayer = new Translation("REARM");
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation FOBResourceToastRepairVehicle = new Translation("REPAIR");

    [TranslationData(SectionXPCreditsToast, IsPriorityTranslation = false)]
    public static readonly Translation<int> XPToastGainXP = new Translation<int>("+{0} XP", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPriorityTranslation = false)]
    public static readonly Translation<int> XPToastLoseXP = new Translation<int>("-{0} XP", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPriorityTranslation = false)]
    public static readonly Translation<int> XPToastGainCredits = new Translation<int>("+{0} <color=#c$credits$>C</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPriorityTranslation = false)]
    public static readonly Translation<int> XPToastPurchaseCredits = new Translation<int>("-{0} <color=#c$credits$>C</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPriorityTranslation = false)]
    public static readonly Translation<int> XPToastLoseCredits = new Translation<int>("-{0} <color=#d69898>C</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation ToastPromoted = new Translation("YOU HAVE BEEN <color=#ffbd8a>PROMOTED</color> TO", TranslationOptions.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation ToastDemoted = new Translation("YOU HAVE BEEN <color=#e86868>DEMOTED</color> TO", TranslationOptions.TMProUI);
    #endregion

    #region Injured UI
    [TranslationData(SectionRevives)]
    public static readonly Translation InjuredUIHeader = new Translation("You are injured", TranslationOptions.TMProUI);
    [TranslationData(SectionRevives)]
    public static readonly Translation InjuredUIGiveUp = new Translation("Press <color=#cecece><b><plugin_2/></b></color> to give up.", TranslationOptions.TMProUI);
    [TranslationData(SectionRevives)]
    public static readonly Translation InjuredUIGiveUpChat = new Translation("<#ff8c69>You were injured, press <color=#cedcde><plugin_2/></color> to give up.");
    #endregion

    #region Insurgency
    private const string SectionInsurgency = "Gamemode Insurgency";
    [TranslationData(SectionInsurgency)]
    public static readonly Translation InsurgencyListHeader = new Translation("Caches", TranslationOptions.UnityUI);
    [TranslationData(SectionInsurgency)]
    public static readonly Translation InsurgencyUnknownCacheAttack = new Translation("<color=#696969>Undiscovered</color>", TranslationOptions.UnityUI);
    [TranslationData(SectionInsurgency)]
    public static readonly Translation InsurgencyUnknownCacheDefense = new Translation("<color=#696969>Unknown</color>", TranslationOptions.UnityUI);
    [TranslationData(SectionInsurgency)]
    public static readonly Translation InsurgencyDestroyedCacheAttack = new Translation("<color=#5a6e5c>Destroyed</color>", TranslationOptions.UnityUI);
    [TranslationData(SectionInsurgency)]
    public static readonly Translation InsurgencyDestroyedCacheDefense = new Translation("<color=#6b5858>Lost</color>", TranslationOptions.UnityUI);
    [TranslationData(SectionInsurgency, IsPriorityTranslation = false)]
    public static readonly Translation<Cache, Cache> InsurgencyCacheAttack = new Translation<Cache, Cache>("<color=#ff7661>{0}</color> <color=#c2c2c2>{1}</color>", TranslationOptions.UnityUI, FOB.FormatName, FOB.FormatLocationName);
    [TranslationData(SectionInsurgency, IsPriorityTranslation = false)]
    public static readonly Translation<Cache, Cache> InsurgencyCacheDefense = new Translation<Cache, Cache>("<color=#555bcf>{0}</color> <color=#c2c2c2>{1}</color>", TranslationOptions.UnityUI, FOB.FormatName, FOB.FormatLocationName);
    [TranslationData(SectionInsurgency, IsPriorityTranslation = false)]
    public static readonly Translation<Cache, Cache> InsurgencyCacheDefenseUndiscovered = new Translation<Cache, Cache>("<color=#b780d9>{0}</color> <color=#c2c2c2>{1}</color>", TranslationOptions.UnityUI, FOB.FormatName, FOB.FormatLocationName);
    #endregion

    #region Hardpoint
    private const string SectionHardpoint = "Gamemode Hardpoint";
    [TranslationData(SectionHardpoint)]
    public static readonly Translation<IObjective, float> HardpointFirstObjective = new Translation<IObjective, float>("Hold {0} to win! A new objective will be chosen in <#cedcde>{1}</color>.", arg0Fmt: Flag.COLOR_NAME_FORMAT, arg1Fmt: TimeAddon.Create(TimeFormatType.Long));
    [TranslationData(SectionHardpoint)]
    public static readonly Translation<IObjective, float> HardpointObjectiveChanged = new Translation<IObjective, float>("New objective: {0}! The next objective will be chosen in <#cedcde>{1}</color>.", arg0Fmt: Flag.COLOR_NAME_FORMAT, arg1Fmt: TimeAddon.Create(TimeFormatType.Long));
    [TranslationData(SectionHardpoint)]
    public static readonly Translation<IObjective, FactionInfo> HardpointObjectiveStateCaptured = new Translation<IObjective, FactionInfo>("{0} is being held by {1}!", arg0Fmt: Flag.COLOR_NAME_FORMAT, arg1Fmt: FactionInfo.FormatColorShortName);
    [TranslationData(SectionHardpoint)]
    public static readonly Translation<IObjective, FactionInfo> HardpointObjectiveStateLost = new Translation<IObjective, FactionInfo>("{0} is no longer being held by {1}!", arg0Fmt: Flag.COLOR_NAME_FORMAT, arg1Fmt: FactionInfo.FormatColorShortName);
    [TranslationData(SectionHardpoint)]
    public static readonly Translation<IObjective> HardpointObjectiveStateLostContest = new Translation<IObjective>("{0} is no longer <#c$contested$>contested</color>!", arg0Fmt: Flag.COLOR_NAME_FORMAT);
    [TranslationData(SectionHardpoint)]
    public static readonly Translation<IObjective> HardpointObjectiveStateContested = new Translation<IObjective>("{0} is <#c$contested$>contested</color>!", arg0Fmt: Flag.COLOR_NAME_FORMAT);
    #endregion

    #region Report Command
    private const string SectionReport = "Reporting";
    [TranslationData(SectionReport, Description = "Possible report arguments, do not translate the reasons.")]
    public static readonly Translation ReportReasons = new Translation("<#9cffb3>Report reasons: -none-, \"chat abuse\", \"voice chat abuse\", \"soloing vehicles\", \"wasteing assets\", \"teamkilling\", \"fob griefing\", \"cheating\".");
    [TranslationData(SectionReport)]
    public static readonly Translation ReportPlayerNotFound = new Translation("<#9cffb3>Unable to find a player with that name, you can use their <color=#ffffff>Steam64 ID</color> instead, as names are only stored until they've been offline for 20 minutes.");
    [TranslationData(SectionReport)]
    public static readonly Translation ReportUnknownError = new Translation("<#9cffb3>Unable to generate a report for an unknown reason, check your syntax again with <color=#ffffff>/report help</color>.");
    [TranslationData(SectionReport)]
    public static readonly Translation<IPlayer, string, string> ReportSuccessMessage = new Translation<IPlayer, string, string>("<#c480d9>Successfully reported {0} for <#fff>{1}</color> as a <#00ffff>{2}</color> report. If possible please post evidence in <#ffffff>#player-reports</color> in our <#7483c4>Discord</color> server.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionReport)]
    public static readonly Translation<IPlayer, string, string> ReportSuccessMessage1 = new Translation<IPlayer, string, string>("<#c480d9>Successfully reported {0} for <#fff>{1}</color> as a <#00ffff>{2}</color> report.", arg0Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionReport)]
    public static readonly Translation ReportSuccessMessage2 = new Translation("<#c480d9>If possible please post evidence in <#ffffff>#player-reports</color> in our <#7483c4>Discord</color> server.");
    [TranslationData(SectionReport)]
    public static readonly Translation<IPlayer, IPlayer, string, string> ReportNotifyAdmin = new Translation<IPlayer, IPlayer, string, string>("<#c480d9>{0} reported {1} for <#fff>{2}</color> as a <#00ffff>{3}</color> report. Check <#c480d9>#player-reports</color> for more information.", arg0Fmt: UCPlayer.FormatCharacterName, arg1Fmt: UCPlayer.FormatCharacterName);
    [TranslationData(SectionReport)]
    public static readonly Translation<string> ReportNotifyViolatorToast = new Translation<string>("<#c480d9>You've been reported for <#00ffff>{0}</color>.\nCheck <#fff>#player-reports</color> in our <#7483c4>Discord</color> (/discord) for more information and to defend yourself.", TranslationOptions.TMProUI);
    [TranslationData(SectionReport)]
    public static readonly Translation ReportNotifyViolatorToastTitle = new Translation("You Were Reported", TranslationOptions.TMProUI);
    [TranslationData(SectionReport)]
    public static readonly Translation<string, string> ReportNotifyViolatorMessage = new Translation<string, string>("<#c480d9>You've been reported for <#00ffff>{0} - {1}</color>. Check <#fff>#player-reports</color> in our <#7483c4>Discord</color> (/discord) for more information and to defend yourself.");
    [TranslationData(SectionReport)]
    public static readonly Translation<string, string> ReportNotifyViolatorMessage1 = new Translation<string, string>("<#c480d9>You've been reported for <#00ffff>{0} - {1}</color>.");
    [TranslationData(SectionReport)]
    public static readonly Translation ReportNotifyViolatorMessage2 = new Translation("<#c480d9>Check <#fff>#player-reports</color> in our <#7483c4>Discord</color> (/discord) for more information and to defend yourself.");
    [TranslationData(SectionReport)]
    public static readonly Translation<IPlayer> ReportCooldown = new Translation<IPlayer>("<#9cffb3>You've already reported {0} in the past hour.", arg0Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionReport)]
    public static readonly Translation<ulong, IPlayer> ReportConfirm = new Translation<ulong, IPlayer>("<#c480d9>Did you mean to report {1} <i><#444>{0}</color></i>? Type <#ff8c69>/confirm</color> to continue.", arg1Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionReport)]
    public static readonly Translation ReportCancelled = new Translation("<#ff8c69>You didn't confirm your report in time.");
    [TranslationData(SectionReport)]
    public static readonly Translation ReportNotConnected = new Translation("<#ff8c69>The report system is not available right now, please try again later.");
    #endregion

    #region DailyQuests
    private const string SectionDailyQuests = "Daily Quests";
    [TranslationData(SectionDailyQuests, "Sent when new daily quests are put into action.")]
    public static readonly Translation<DateTime> DailyQuestsNewIndex = new Translation<DateTime>("<#66ccff>New daily quests have been generated! They will be active until <#cedcde>{0}</color> UTC.", arg0Fmt: "G");
    [TranslationData(SectionDailyQuests, "Sent 1 hour before new daily quests are put into action.")]
    public static readonly Translation DailyQuestsOneHourRemaining = new Translation("<#66ccff>You have one hour until new daily quests will be generated!");
    #endregion

    #region Tips
    private const string SectionTips = "In-game Tips";
    [TranslationData(SectionTips)]
    public static readonly Translation<IPlayer> TipUAVRequest = new Translation<IPlayer>("<#d9c69a>{0} Requested a UAV!", TranslationOptions.TMProUI, UCPlayer.FormatColoredNickName);
    [TranslationData(SectionTips)]
    public static readonly Translation TipPlaceRadio = new Translation("Place a <#ababab>FOB RADIO</color>.", TranslationOptions.TMProUI);
    [TranslationData(SectionTips)]
    public static readonly Translation TipPlaceBunker = new Translation("Build a <#a5c3d9>FOB BUNKER</color> so that your team can spawn.", TranslationOptions.TMProUI);
    [TranslationData(SectionTips)]
    public static readonly Translation TipUnloadSupplies = new Translation("<#d9c69a>DROP SUPPLIES</color> onto the FOB.", TranslationOptions.TMProUI);
    [TranslationData(SectionTips)]
    public static readonly Translation<IPlayer> TipHelpBuild = new Translation<IPlayer>("<#d9c69a>{0} needs help building!", TranslationOptions.TMProUI, UCPlayer.FormatColoredNickName);
    [TranslationData(SectionTips)]
    public static readonly Translation<VehicleType> TipLogisticsVehicleResupplied = new Translation<VehicleType>("Your <#009933>{0}</color> has been auto resupplied.", TranslationOptions.TMProUI, FormatUppercase);
    [TranslationData(SectionTips)]
    public static readonly Translation TipActionMenu = new Translation("Press <#a5c3d9><plugin_1/></color> for field actions", TranslationOptions.TMProUI);
    [TranslationData(SectionTips)]
    public static readonly Translation TipActionMenuSl = new Translation("Press <#a5c3d9><plugin_1/></color> for <#85c996>squad actions</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionTips)]
    public static readonly Translation TipCallMedic = new Translation("You are hurt. Press <#d9a5bb><plugin_1/></color> to call for a medic.", TranslationOptions.TMProUI);
    #endregion

    #region Teams
    private const string SectionTeams = "Teams";
    [TranslationData(SectionTeams, "Gets sent to the player when they walk or teleport into main base.")]
    public static readonly Translation<FactionInfo> EnteredMain = new Translation<FactionInfo>("<#e6e3d5>You have entered the safety of the {0} headquarters!", arg0Fmt: FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionTeams, "Gets sent to the player when they walk or teleport out of main base.")]
    public static readonly Translation<FactionInfo> LeftMain = new Translation<FactionInfo>("<#e6e3d5>You have left the safety of the {0} headquarters!", arg0Fmt: FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionTeams, "Gets sent to the player when they join a team.")]
    public static readonly Translation<FactionInfo> TeamJoinDM = new Translation<FactionInfo>("<#a0ad8e>You've joined {0}.", arg0Fmt: FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionTeams, "Gets broadcasted to everyone when someone joins a team.")]
    public static readonly Translation<FactionInfo, IPlayer> TeamJoinAnnounce = new Translation<FactionInfo, IPlayer>("<#a0ad8e>{1} joined {0}!", arg0Fmt: FactionInfo.FormatColorDisplayName, arg1Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeams, "Gets broadcasted when the game is over.")]
    public static readonly Translation<FactionInfo> TeamWin = new Translation<FactionInfo>("<#a0ad8e>{0} has won the battle!", arg0Fmt: FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionTeams)]
    public static readonly Translation<Cooldown> TeamsCooldown = new Translation<Cooldown>("<#ff8c69>You can't use /teams for another {0}.", arg0Fmt: Cooldown.FormatTimeLong);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIHeader = new Translation("Choose a Team", TranslationOptions.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIClickToJoin = new Translation("CLICK TO JOIN", TranslationOptions.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIJoined = new Translation("JOINED", TranslationOptions.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIFull = new Translation("<#bf6363>FULL", TranslationOptions.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIConfirm = new Translation("CONFIRM", TranslationOptions.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIBack = new Translation("BACK", TranslationOptions.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIJoining = new Translation("<#999999>JOINING...", TranslationOptions.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsShuffleQueued = new Translation("Teams will be SHUFFLED next game.");
    #endregion

    #region Spotting
    private const string SectionSpotting = "Spotting";
    [TranslationData(SectionSpotting)]
    public static readonly Translation SpottedToast = new Translation("<#b9ffaa>SPOTTED", TranslationOptions.TMProUI);
    [TranslationData(SectionSpotting, Parameters = [ "Team color of the speaker.", "Target" ])]
    public static readonly Translation<Color, string> SpottedMessage = new Translation<Color, string>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Enemy {1} spotted!");
    [TranslationData(SectionSpotting)]
    public static readonly Translation SpottedTargetPlayer = new Translation("contact");
    [TranslationData(SectionSpotting)]
    public static readonly Translation SpottedTargetFOB = new Translation("FOB");
    [TranslationData(SectionSpotting)]
    public static readonly Translation SpottedTargetCache = new Translation("Cache");
    #endregion

    #region Actions
    private const string SectionActions = "Actions";
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> NeedMedicChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: I need a medic here!");
    [TranslationData(SectionActions)]
    public static readonly Translation<string> NeedMedicToast = new Translation<string>("<#a1998d>{0} needs healing.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> NeedAmmoChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: I need some ammo here!");
    [TranslationData(SectionActions)]
    public static readonly Translation<string> NeedAmmoToast = new Translation<string>("<#a1998d>{0} needs ammunition.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> NeedRideChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Hey, I need a ride!");
    [TranslationData(SectionActions)]
    public static readonly Translation<string> NeedRideToast = new Translation<string>("<#a1998d>{0} needs a ride.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> NeedSupportChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: I need help over here!");
    [TranslationData(SectionActions)]
    public static readonly Translation<string> NeedSupportToast = new Translation<string>("<#a1998d>{0} needs help.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> HeliPickupChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Requesting helicopter transport!");
    [TranslationData(SectionActions)]
    public static readonly Translation<string> HeliPickupToast = new Translation<string>("<#a1998d><#dbb67f>{0}</color> needs transport.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> HeliDropoffChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Requesting drop off at this position!");
    [TranslationData(SectionActions)]
    public static readonly Translation<string> HeliDropoffToast = new Translation<string>("<#a1998d><#dbb67f>{0}</color> is requesting drop off.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> SuppliesBuildChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Requesting FOB building supplies!");
    [TranslationData(SectionActions)]
    public static readonly Translation<string> SuppliesBuildToast = new Translation<string>("<#a1998d><#dbb67f>{0}</color> needs FOB supplies.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> SuppliesAmmoChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Requesting FOB ammunition supplies!");
    [TranslationData(SectionActions)]
    public static readonly Translation<string> SuppliesAmmoToast = new Translation<string>("<#a1998d><#dbb67f>{0}</color> needs FOB ammunition.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> AirSupportChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Requesting close air support!");
    [TranslationData(SectionActions)]
    public static readonly Translation<string> AirSupportToast = new Translation<string>("<#a1998d><#dbb67f>{0}</color> needs air support.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> ArmorSupportChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Requesting armor support!");
    [TranslationData(SectionActions)]
    public static readonly Translation<string> ArmorSupportToast = new Translation<string>("<#a1998d><#dbb67f>{0}</color> needs armor support.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> ThankYouChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Thank you!");
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> SorryChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Sorry.");
    [TranslationData(SectionActions)]
    public static readonly Translation AttackToast = new Translation("<#a1998d>Attack the marked position.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation DefendToast = new Translation("<#a1998d>Defend the marked position.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation MoveToast = new Translation("<#a1998d>Move to the marked position.", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation BuildToast = new Translation("<#a1998d>Build near the marked position.", TranslationOptions.TMProUI);

    [TranslationData(SectionActions)]
    public static readonly Translation ActionErrorInMain = new Translation("<#9e7d7d>Unavailable in main", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation ActionErrorNoMarker = new Translation("<#9e7d7d>Place a MARKER first", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation ActionErrorNotInHeli = new Translation("<#9e7d7d>You are not inside a HELICOPTER", TranslationOptions.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation ActionErrorInVehicle = new Translation("<#9e7d7d>Unavailable in vehicle", TranslationOptions.TMProUI);
    #endregion

    #region Teleport
    private const string SectionTeleport = "Teleport Command";
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer> TeleportTargetDead = new Translation<IPlayer>("<#8f9494>{0} is not alive.", arg0Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport)]
    public static readonly Translation<IPlayer, InteractableVehicle> TeleportSelfSuccessVehicle = new Translation<IPlayer, InteractableVehicle>("<#bfb9ac>You were put in {0}'s {1}.", arg0Fmt: UCPlayer.FormatColoredCharacterName, arg1Fmt: FormatRarityColor);
    [TranslationData(SectionTeleport)]
    public static readonly Translation<IPlayer> TeleportSelfSuccessPlayer = new Translation<IPlayer>("<#bfb9ac>You were teleported to {0}.", arg0Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer> TeleportSelfPlayerObstructed = new Translation<IPlayer>("<#8f9494>Failed to teleport you to {0}, their position is obstructed.", arg0Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<string> TeleportLocationNotFound = new Translation<string>("<#8f9494>Failed to find a location similar to <#ddd>{0}</color>.");
    [TranslationData(SectionTeleport)]
    public static readonly Translation<string> TeleportSelfLocationSuccess = new Translation<string>("<#bfb9ac>You were teleported to <#ddd>{0}</color>.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<string> TeleportStopJump = new Translation<string>("<#bfb9ac>You will no longer jump on right punch.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<string> TeleportStartJump = new Translation<string>("<#bfb9ac>You will jump on right punch. Do <#ddd>/tp jump stop</color> to stop.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<string> TeleportSelfLocationObstructed = new Translation<string>("<#8f9494>Failed to teleport you to <#ddd>{0}</color>, it's position is obstructed.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation TeleportWaypointNotFound = new Translation("<#8f9494>You must have a waypoint placed on the map.");
    [TranslationData(SectionTeleport)]
    public static readonly Translation<GridLocation> TeleportSelfWaypointSuccess = new Translation<GridLocation>("<#bfb9ac>You were teleported to your waypoint in <#ddd>{0}</color>.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<GridLocation> TeleportSelfWaypointObstructed = new Translation<GridLocation>("<#8f9494>Failed to teleport you to your waypoint in <#ddd>{0}</color>, it's position is obstructed.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<GridLocation> TeleportGridLocationNotFound = new Translation<GridLocation>("<#8f9494>There is no terrain at <#ddd>{0}</color>.");
    [TranslationData(SectionTeleport)]
    public static readonly Translation<GridLocation> TeleportSelfGridLocationSuccess = new Translation<GridLocation>("<#bfb9ac>You were teleported to <#ddd>{0}</color>.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<GridLocation> TeleportSelfGridLocationObstructed = new Translation<GridLocation>("<#8f9494>Failed to teleport you to <#ddd>{0}</color>, it's position is obstructed.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, GridLocation> TeleportOtherWaypointSuccess = new Translation<IPlayer, GridLocation>("<#bfb9ac>{0} was teleported to your waypoint in <#ddd>{1}</color>.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, GridLocation> TeleportOtherWaypointObstructed = new Translation<IPlayer, GridLocation>("<#8f9494>Failed to teleport {0} to your waypoint in <#ddd>{1}</color>, it's position is obstructed.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, GridLocation> TeleportOtherGridLocationSuccess = new Translation<IPlayer, GridLocation>("<#bfb9ac>{0} was teleported to <#ddd>{1}</color>.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, GridLocation> TeleportOtherGridLocationObstructed = new Translation<IPlayer, GridLocation>("<#8f9494>Failed to teleport {0} to <#ddd>{1}</color>, it's position is obstructed.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, IPlayer, InteractableVehicle> TeleportOtherSuccessVehicle = new Translation<IPlayer, IPlayer, InteractableVehicle>("<#bfb9ac>{0} was put in {1}'s {2}.", arg0Fmt: UCPlayer.FormatColoredCharacterName, arg1Fmt: UCPlayer.FormatColoredCharacterName, arg2Fmt: FormatRarityColor);
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, IPlayer> TeleportOtherSuccessPlayer = new Translation<IPlayer, IPlayer>("<#bfb9ac>{0} was teleported to {1}.", arg0Fmt: UCPlayer.FormatColoredCharacterName, arg1Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, IPlayer> TeleportOtherObstructedPlayer = new Translation<IPlayer, IPlayer>("<#8f9494>Failed to teleport {0} to {1}, their position is obstructed.", arg0Fmt: UCPlayer.FormatColoredCharacterName, arg1Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, string> TeleportOtherSuccessLocation = new Translation<IPlayer, string>("<#bfb9ac>{0} was teleported to <#ddd>{1}</color>.", arg0Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, string> TeleportOtherObstructedLocation = new Translation<IPlayer, string>("<#8f9494>Failed to teleport {0} to <#ddd>{1}</color>, it's position is obstructed.", arg0Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation<string> TeleportTargetNotFound = new Translation<string>("<#8f9494>Failed to find a player from <#ddd>{0}</color>.");
    [TranslationData(SectionTeleport, IsPriorityTranslation = false)]
    public static readonly Translation TeleportInvalidCoordinates = new Translation("<#8f9494>Use of coordinates should look like: <#eee>/tp [player] <x y z></color>.");
    #endregion

    #region Heal Command
    private const string SectionHeal = "Heal Command";
    [TranslationData(SectionHeal, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer> HealPlayer = new Translation<IPlayer>("<#ff9966>You healed {0}.", arg0Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionHeal)]
    public static readonly Translation HealSelf = new Translation("<#ff9966>You were healed.");
    #endregion

    #region God Command
    private const string SectionGod = "God Command";
    [TranslationData(SectionGod, IsPriorityTranslation = false)]
    public static readonly Translation GodModeEnabled = new Translation("<#bfb9ac>God mode <#99ff66>enabled</color>.");
    [TranslationData(SectionGod, IsPriorityTranslation = false)]
    public static readonly Translation GodModeDisabled = new Translation("<#ff9966>God mode <#ff9999>disabled</color>.");
    #endregion

    #region Vanish Command
    private const string SectionVanish = "Vanish Command";
    [TranslationData(SectionVanish, IsPriorityTranslation = false)]
    public static readonly Translation VanishModeEnabled = new Translation("<#bfb9ac>Vanish mode <#99ff66>enabled</color>.");
    [TranslationData(SectionVanish, IsPriorityTranslation = false)]
    public static readonly Translation VanishModeDisabled = new Translation("<#ff9966>Vanish mode <#ff9999>disabled</color>.");
    #endregion

    #region Permission Command
    private const string SectionPermission = "Permission Command";
    [TranslationData(SectionPermission)]
    public static readonly Translation<string> PermissionsCurrent = new Translation<string>("<#bfb9ac>Current permisions: <color=#ffdf91>{0}</color>.");
    [TranslationData(SectionPermission, IsPriorityTranslation = false)]
    public static readonly Translation<EAdminType, IPlayer, ulong> PermissionGrantSuccess = new Translation<EAdminType, IPlayer, ulong>("<#bfb9ac><#7f8182>{1}</color> <#ddd>({2})</color> is now a <#ffdf91>{0}</color>.");
    [TranslationData(SectionPermission, IsPriorityTranslation = false)]
    public static readonly Translation<EAdminType, IPlayer, ulong> PermissionGrantAlready = new Translation<EAdminType, IPlayer, ulong>("<#bfb9ac><#7f8182>{1}</color> <#ddd>({2})</color> is already at the <#ffdf91>{0}</color> level.");
    [TranslationData(SectionPermission, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, ulong> PermissionRevokeSuccess = new Translation<IPlayer, ulong>("<#bfb9ac><#7f8182>{0}</color> <#ddd>({1})</color> is now a <#ffdf91>member</color>.");
    [TranslationData(SectionPermission, IsPriorityTranslation = false)]
    public static readonly Translation<IPlayer, ulong> PermissionRevokeAlready = new Translation<IPlayer, ulong>("<#bfb9ac><#7f8182>{0}</color> <#ddd>({1})</color> is already a <#ffdf91>member</color>.");
    #endregion

    #region Win UI
    private const string SectionWinUI = "Win UI";
    [TranslationData(SectionWinUI)]
    public static readonly Translation<int> WinUIValueTickets = new Translation<int>("{0} ${p:0:Ticket}", TranslationOptions.UnityUI);
    [TranslationData(SectionWinUI)]
    public static readonly Translation<int> WinUIValueCaches = new Translation<int>("{0} ${p:0:Cache} Left", TranslationOptions.UnityUI);
    [TranslationData(SectionWinUI)]
    public static readonly Translation<FactionInfo> WinUIHeaderWinner = new Translation<FactionInfo>("{0}\r\nhas won the battle!", TranslationOptions.UnityUI, FactionInfo.FormatColorDisplayName);
    #endregion

    #region UAV
    private const string SectionUAV = "UAVs";
    [TranslationData(SectionUAV, "Sent to the owner of a UAV when it's destroyed as an event of their death.")]
    public static readonly Translation UAVDestroyedDeath = new Translation("<#e86868>Your <#cc99ff>UAV</color> was destroyed because you died.");
    [TranslationData(SectionUAV, "Sent to the owner of a UAV when it's destroyed as an event of their death.")]
    public static readonly Translation UAVDestroyedTimer = new Translation("<#e86868>Your <#cc99ff>UAV</color> is no longer active.");
    [TranslationData(SectionUAV, "Sent to the owner of a newly deployed UAV when a marker isn't placed.")]
    public static readonly Translation UAVDeployedSelf = new Translation("<#33cccc>A <#cc99ff>UAV</color> has been activated at your location.");
    [TranslationData(SectionUAV, "Sent to the owner of a newly deployed UAV if the timer in game config is set when a marker isn't placed.")]
    public static readonly Translation<int> UAVDeployedTimeSelf = new Translation<int>("<#33cccc>A <#cc99ff>UAV</color> has been dispatched to your location. It will arrive in {0} ${p:0:second}.");
    [TranslationData(SectionUAV, "Sent to the owner of a newly deployed UAV when a marker is placed.")]
    public static readonly Translation<GridLocation> UAVDeployedMarker = new Translation<GridLocation>("<#33cccc>A <#cc99ff>UAV</color> has been activated at <#fff>{0}</color>.");
    [TranslationData(SectionUAV, "Sent to the owner of a newly deployed UAV if the timer in game config is set when a marker is placed.")]
    public static readonly Translation<GridLocation, int> UAVDeployedTimeMarker = new Translation<GridLocation, int>("<#33cccc>A <#cc99ff>UAV</color> has been dispatched to <#fff>{0}</color>. It will arrive in {1} ${p:1:second}.");
    [TranslationData(SectionUAV, "Sent to the commander of a newly deployed UAV when a marker isn't placed.")]
    public static readonly Translation<GridLocation, IPlayer> UAVDeployedSelfCommander = new Translation<GridLocation, IPlayer>("<#33cccc>A <#cc99ff>UAV</color> has been activated at {1}'s location (<#fff>{0}</color>).", arg1Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionUAV, "Sent to the commander of a newly deployed UAV if the timer in game config is set when a marker isn't placed.")]
    public static readonly Translation<int, GridLocation, IPlayer> UAVDeployedTimeSelfCommander = new Translation<int, GridLocation, IPlayer>("<#33cccc>A <#cc99ff>UAV</color> has been dispatched to {2}'s location (<#fff>{1}</color>). It will arrive in {0} ${p:0:second}.", arg2Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionUAV, "Sent to the commander of a newly deployed UAV when a marker is placed.")]
    public static readonly Translation<GridLocation, IPlayer> UAVDeployedMarkerCommander = new Translation<GridLocation, IPlayer>("<#33cccc>A <#cc99ff>UAV</color> has been activated at <#fff>{0}</color> for {1}.", arg1Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionUAV, "Sent to the commander of a newly deployed UAV if the timer in game config is set when a marker is placed.")]
    public static readonly Translation<GridLocation, int, IPlayer> UAVDeployedTimeMarkerCommander = new Translation<GridLocation, int, IPlayer>("<#33cccc>A <#cc99ff>UAV</color> has been dispatched to <#fff>{0}</color> for {2}. It will arrive in {1} ${p:1:second}.", arg2Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionUAV, "Sent when the player tries to request a UAV without a kit.")]
    public static readonly Translation RequestUAVNoKit = new Translation("<#e86868>Request a <#cedcde>SQUAD LEADER</color> kit before trying to request a <#cc99ff>UAV</color>.");
    [TranslationData(SectionUAV, "Sent when the player tries to request a UAV while not a Squadleader.")]
    public static readonly Translation RequestUAVNotSquadleader = new Translation("<#e86868>You have to be a squad leader and have a <#cedcde>SQUAD LEADER</color> kit to request a <#cc99ff>UAV</color>.");
    [TranslationData(SectionUAV, "Sent when the player requests a UAV from someone other than themselves as feedback.", "The active commander.")]
    public static readonly Translation<IPlayer> RequestUAVSent = new Translation<IPlayer>("<#33cccc>A request was sent to <#c$commander$>{0}</color> for a <#cc99ff>UAV</color>.", arg0Fmt: UCPlayer.FormatNickName);
    [TranslationData(SectionUAV, "Sent when the player requests a UAV from someone other than themselves to the commander.", "The requester of the UAV.", "The requester's squad.", "Location of request.")]
    public static readonly Translation<IPlayer, Squad, GridLocation> RequestUAVTell = new Translation<IPlayer, Squad, GridLocation>("<#33cccc>{0} from squad <#cedcde><uppercase>{1}</uppercase></color> wants to deploy a <#cc99ff>UAV</color> at <#fff>{2}</color>.\n<#cedcde>Type /confirm or /deny in the next 15 seconds.", UCPlayer.FormatColoredNickName, Squad.FormatName);
    [TranslationData(SectionUAV, "Sent when the player tries to request a UAV while no one on their team has a commander kit.")]
    public static readonly Translation RequestUAVNoActiveCommander = new Translation("<#e86868>There's currently no players with the <#c$commander$>commander</color> kit on your team. <#cc99ff>UAV</color>s must be requested from a <#c$commander$>commander</color>.");
    [TranslationData(SectionUAV, "Sent to the commander if the requester disconnected before the commander confirmed.", "The requester.")]
    public static readonly Translation<IPlayer> RequestUAVRequesterLeft = new Translation<IPlayer>("<#e86868>The <#cc99ff>UAV</color> request was cancelled because {0} disconnected.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionUAV, "Sent to the requested if the commander disconnected before they confirmed.", "The commander.")]
    public static readonly Translation<IPlayer> RequestUAVCommanderLeft = new Translation<IPlayer>("<#e86868>Your <#cc99ff>UAV</color> request was cancelled because <#c$commander$>{0}</color> disconnected.", arg0Fmt: UCPlayer.FormatNickName);
    [TranslationData(SectionUAV, "Sent to the commander if the requester changes teams before the commander confirmed.", "The requester.")]
    public static readonly Translation<IPlayer> RequestUAVRequesterChangedTeams = new Translation<IPlayer>("<#e86868>The <#cc99ff>UAV</color> request was cancelled because {0} changed teams.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionUAV, "Sent to the requested if the commander changes team before they confirmed.", "The commander.")]
    public static readonly Translation<IPlayer> RequestUAVCommanderChangedTeams = new Translation<IPlayer>("<#e86868>Your <#cc99ff>UAV</color> request was cancelled because <#c$commander$>{0}</color> changed teams.", arg0Fmt: UCPlayer.FormatNickName);
    [TranslationData(SectionUAV, "Sent to the commander if the requester changes classes to a non-SL class, leaves their squad, or promotes someone else before the commander confirmed.", "The requester.")]
    public static readonly Translation<IPlayer> RequestUAVRequesterNotSquadLeader = new Translation<IPlayer>("<#e86868>The <#cc99ff>UAV</color> request was cancelled because {0} changed teams.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionUAV, "Sent to the requested if the commander stops being commander before they confirmed.", "The commander.")]
    public static readonly Translation<IPlayer> RequestUAVCommanderNoLongerCommander = new Translation<IPlayer>("<#e86868>Your <#cc99ff>UAV</color> request was cancelled because {0} is no longer the <#c$commander$>commander</color>.", arg0Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionUAV, "Sent to the requested if the commander denies their UAV request.", "The commander.")]
    public static readonly Translation<IPlayer> RequestUAVDenied = new Translation<IPlayer>("<#e86868>Your <#cc99ff>UAV</color> request was denied by <#c$commander$>{0}</color>.", arg0Fmt: UCPlayer.FormatNickName);
    [TranslationData(SectionUAV, "Sent to the requested if someone else is already requesting a UAV.")]
    public static readonly Translation RequestAlreadyActive = new Translation("<#e86868>Someone else on your team is already requesting a <#cc99ff>UAV</color>.");

    #endregion

    #region Kit Menu UI
    private const string SectionKitMenuUI = "Kit Menu";
    [TranslationData(SectionKitMenuUI, "Text that goes on the base kits tab.")]
    public static readonly Translation KitMenuUITabBaseKits    = new Translation("Base Kits", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Text that goes on the elite kits tab.")]
    public static readonly Translation KitMenuUITabEliteKits   = new Translation("Elite Kits", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Text that goes on the loadouts tab.")]
    public static readonly Translation KitMenuUITabLoadouts    = new Translation("Loadouts", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Text that goes on the special kits tab.")]
    public static readonly Translation KitMenuUITabSpecialKits = new Translation("Special Kits", TranslationOptions.TMProUI);

    [TranslationData(SectionKitMenuUI, "Label that goes in front of the filter dropdown.")]
    public static readonly Translation KitMenuUIFilterLabel = new Translation("Filter", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of the faction in kit info.")]
    public static readonly Translation KitMenuUIFactionLabel = new Translation("Faction", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of the class in kit info.")]
    public static readonly Translation KitMenuUIClassLabel = new Translation("Class", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of the included items list in kit info.")]
    public static readonly Translation KitMenuUIIncludedItemsLabel = new Translation("Included Items", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Value for kit type (KitType.Public).")]
    public static readonly Translation KitMenuUIKitTypeLabelPublic = new Translation("Public Kit", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Value for kit type (KitType.Elite).")]
    public static readonly Translation KitMenuUIKitTypeLabelElite = new Translation("Elite Kit", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Value for kit type (KitType.Special).")]
    public static readonly Translation KitMenuUIKitTypeLabelSpecial = new Translation("Special/Event Kit", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Value for kit type (KitType.Loadout).")]
    public static readonly Translation KitMenuUIKitTypeLabelLoadout = new Translation("Custom Loadout", TranslationOptions.TMProUI);

    [TranslationData(SectionKitMenuUI, "Label that goes in front of playtime in kit stats.")]
    public static readonly Translation KitMenuUIPlaytimeLabel = new Translation("Playtime", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of total kills in kit stats.")]
    public static readonly Translation KitMenuUIKillsLabel = new Translation("Total Kills", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of total deaths in kit stats.")]
    public static readonly Translation KitMenuUIDeathsLabel = new Translation("Total Deaths", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills in kit stats.")]
    public static readonly Translation KitMenuUIPrimaryKillsLabel = new Translation("Primary Kills", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary average kill distance in kit stats.")]
    public static readonly Translation KitMenuUIPrimaryAvgDstLabel = new Translation("Primary Avg. Dst.", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of secondary kills in kit stats.")]
    public static readonly Translation KitMenuUISecondaryKillsLabel = new Translation("Secondary Kills", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of DBNO states in kit stats.")]
    public static readonly Translation KitMenuUIDBNOLabel = new Translation("Injures Without Kill", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of distance traveled in kit stats.")]
    public static readonly Translation KitMenuUIDistanceTraveledLabel = new Translation("Distance Traveled", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of tickets lost in kit stats.")]
    public static readonly Translation KitMenuUITicketsLostLabel = new Translation("Tickets Lost", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of tickets gained in kit stats.")]
    public static readonly Translation KitMenuUITicketsGainedLabel = new Translation("Tickets Recovered", TranslationOptions.TMProUI);

    [TranslationData(SectionKitMenuUI, "Label for kit stats title.")]
    public static readonly Translation KitMenuUIStatsLabel = new Translation("Stats", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label for kit actions title.")]
    public static readonly Translation KitMenuUIActionsLabel = new Translation("Actions", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button action 1, request kit.")]
    public static readonly Translation KitMenuUIActionRequestKitLabel = new Translation("Request Kit", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button action 2, buy kit (can afford).", "Credit cost")]
    public static readonly Translation<int> KitMenuUIActionBuyPublicKitCanAffordLabel = new Translation<int>("<#ccffff>Buy Kit <#c$credits$>C</color> <#fff>{0}</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button action 2, buy kit (can't afford).", "Credit cost")]
    public static readonly Translation<int> KitMenuUIActionBuyPublicKitCantAffordLabel = new Translation<int>("<#ff6666>Requires <#c$credits$>C</color> <#fff>{0}</color>", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button action 3, order kit.", "Price", "Currency Prefix")]
    public static readonly Translation<decimal, string> KitMenuUIActionBuyPremiumKitLabel = new Translation<decimal, string>("<#ccffff>Open Ticket <#c$kit_level_dollars$>{1}</color> <#fff>{0}</color>", TranslationOptions.TMProUI, "C");
    [TranslationData(SectionKitMenuUI, "Label actions button action not in main.")]
    public static readonly Translation KitMenuUIActionNotInMainKitLabel = new Translation("<#ff6666>Not in Main", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button action premium not linked.", "Price", "Currency Prefix")]
    public static readonly Translation<decimal, string> KitMenuUIActionBuyPremiumKitNotLinkedLabel = new Translation<decimal, string>("<#ccffff>Premium kit: <#c$kit_level_dollars$>{1}</color> <#fff>{0}</color>", TranslationOptions.TMProUI, "C");
    [TranslationData(SectionKitMenuUI, "Label actions button action premium unlock requirement not met.", "Price", "Currency Prefix")]
    public static readonly Translation<decimal, string> KitMenuUIActionBuyPublicUnlockReqNotMetLabel = new Translation<decimal, string>("<#ccffff>Premium kit: <#c$kit_level_dollars$>{1}</color> <#fff>{0}</color>", TranslationOptions.TMProUI, "C");
    [TranslationData(SectionKitMenuUI, "Label actions button staff give kit.")]
    public static readonly Translation KitMenuUIActionGiveKitLabel = new Translation("<#0099ff>Give Kit", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button staff edit kit.")]
    public static readonly Translation KitMenuUIActionEditKitLabel = new Translation("<#0099ff>Edit Kit</color> (Coming Soon)", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button staff set loadout items kit.")]
    public static readonly Translation KitMenuUIActionSetLoadoutItemsLabel = new Translation("<#0099ff>Set Loadout Items", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Shown when a player doesn't have access to a kit.")]
    public static readonly Translation KitMenuUIActionNoAccessLabel = new Translation("No Access", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Shown when a kit's faction is not assigned.")]
    public static readonly Translation KitMenuUINoFaction = new Translation("Unaffiliated", TranslationOptions.TMProUI);

    /* CLASS STATS */
    // squadleader
    [TranslationData(SectionKitMenuUI, "Label that goes in front of FOBs started for Squadleaders in kit stats.")]
    public static readonly Translation KitMenuUISquadLeaderFOBsStartedLabel = new Translation("FOBs Started", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of UAVs requested for Squadleaders in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUISquadLeaderUAVsRequestedLabel = new Translation("UAVs Requested", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of targets spotted for Squadleaders in kit stats.")]
    public static readonly Translation KitMenuUISquadLeaderTargetsSpottedLabel = new Translation("Targets Spotted", TranslationOptions.TMProUI);

    // rifleman
    [TranslationData(SectionKitMenuUI, "Label that goes in front of self restocked for Riflemen in kit stats.")]
    public static readonly Translation KitMenuUIRiflemanSelfRestockedLabel = new Translation("Self Restocked", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of teammates restocked for Riflemen in kit stats.")]
    public static readonly Translation KitMenuUIRiflemanTeammatesRestockedLabel = new Translation("Teammates Restocked", TranslationOptions.TMProUI);
    
    // medic
    [TranslationData(SectionKitMenuUI, "Label that goes in front of teammates healed for Medics in kit stats.")]
    public static readonly Translation KitMenuUIMedicTeammatesHealedLabel = new Translation("Teammates Healed", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of teammates revived for Medics in kit stats.")]
    public static readonly Translation KitMenuUIMedicTeammatesRevivedLabel = new Translation("Teammates Revived", TranslationOptions.TMProUI);

    // breacher
    [TranslationData(SectionKitMenuUI, "Label that goes in front of structures destroyed for Breachers in kit stats.")]
    public static readonly Translation KitMenuUIBreacherStructuresDestroyedLabel = new Translation("Structures Destroyed", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of radios destroyed for Breachers in kit stats.")]
    public static readonly Translation KitMenuUIBreacherRadiosDestroyedLabel = new Translation("Radios Destroyed", TranslationOptions.TMProUI);

    // auto-rifleman
    [TranslationData(SectionKitMenuUI, "Label that goes in front of spray n pray streak (most kills without reloading) for Automatic Riflemen in kit stats.")]
    public static readonly Translation KitMenuUIAutoRiflemanStructuresDestroyedLabel = new Translation("Spray n Pray Streak", TranslationOptions.TMProUI);

    // grenadier
    [TranslationData(SectionKitMenuUI, "Label that goes in front of grenade kills for Grenadiers in kit stats.")]
    public static readonly Translation KitMenuUIGrenadierGrenadeKillsLabel = new Translation("Grenade Kills", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of vehicle kills for Grenadiers in kit stats.")]
    public static readonly Translation KitMenuUIGrenadierVehicleKillsLabel = new Translation("Vehicle Kills", TranslationOptions.TMProUI);

    // machine gunner
    [TranslationData(SectionKitMenuUI, "Label that goes in front of spray n pray streak (most kills without reloading) for Machine Gunners in kit stats.")]
    public static readonly Translation KitMenuUIMachineGunnerStructuresDestroyedLabel = new Translation("Spray n Pray Streak", TranslationOptions.TMProUI);
    
    // LAT
    [TranslationData(SectionKitMenuUI, "Label that goes in front of vehicle kills for LATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUILATVehicleKillsLabel = new Translation("Vehicle Kills", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of LAT player kills for LATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUILATPlayerKillsLabel = new Translation("LAT Player Kills", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of structure kills for LATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUILATStructuresDestroyedLabel = new Translation("Structures Destroyed", TranslationOptions.TMProUI);
    
    // HAT
    [TranslationData(SectionKitMenuUI, "Label that goes in front of vehicle kills for HATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIHATVehicleKillsLabel = new Translation("Vehicle Kills", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of HAT player kills for HATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIHATPlayerKillsLabel = new Translation("HAT Player Kills", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of structure kills for HATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIHATStructuresDestroyedLabel = new Translation("Structures Destroyed", TranslationOptions.TMProUI);

    // marksman
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills from 150m to 250m away for Marksmen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIMarksmanKills100mLabel = new Translation("Kills 150m-250m", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills from 250m to 350m away for Marksmen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIMarksmanKills200mLabel = new Translation("Kills 250m-350m", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills over 350m away for Marksmen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIMarksmanKills300mLabel = new Translation("Kills 350m+", TranslationOptions.TMProUI);

    // sniper
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills from 200m to 300m away for Snipers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUISniperKills200mLabel = new Translation("Kills 200m-300m", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills from 300m to 400m away for Snipers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUISniperKills300mLabel = new Translation("Kills 300m-400m", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills over 400m away for Snipers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUISniperKills400mLabel = new Translation("Kills 400m+", TranslationOptions.TMProUI);

    // ap-rifleman
    [TranslationData(SectionKitMenuUI, "Label that goes in front of vehicle kills for AP Riflemen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIAPRiflemanVehicleKillsLabel = new Translation("Vehicle Kills", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of player trap kills for AP Riflemen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIAPRiflemanTrapKillsLabel = new Translation("Trap Kills", TranslationOptions.TMProUI);

    // combat engineer
    [TranslationData(SectionKitMenuUI, "Label that goes in front of shovel points for Combat Engineers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUICombatEngineerShovelsLabel = new Translation("Shovel Points", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of fortifications built for Combat Engineers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUICombatEngineerFortificationsBuiltLabel = new Translation("Fortifications Built", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of emplacements built for Combat Engineers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUICombatEngineerEmplacementsBuiltLabel = new Translation("Emplacements Built", TranslationOptions.TMProUI);

    // crewman
    [TranslationData(SectionKitMenuUI, "Label that goes in front of km driven for Crewmen in kit stats.")]
    public static readonly Translation KitMenuUICrewmanKmDrivenLabel = new Translation("Distance Driven (km)", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of driver assists for Crewmen in kit stats.")]
    public static readonly Translation KitMenuUICrewmanDriverAssistsLabel = new Translation("Driver Assists", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of total transport distance built for Crewmen in kit stats.")]
    public static readonly Translation KitMenuUICrewmanTransportDistanceLabel = new Translation("Ttl. Transport Dst.", TranslationOptions.TMProUI);

    // pilot
    [TranslationData(SectionKitMenuUI, "Label that goes in front of km flown for Pilots in kit stats.")]
    public static readonly Translation KitMenuUIPilotKmDrivenLabel = new Translation("Distance Flown (km)", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of pilot assists for Pilots in kit stats.")]
    public static readonly Translation KitMenuUIPilotDriverAssistsLabel = new Translation("Pilot Assists", TranslationOptions.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of total transport distance built for Pilots in kit stats.")]
    public static readonly Translation KitMenuUIPilotTransportDistanceLabel = new Translation("Ttl. Transport Dst.", TranslationOptions.TMProUI);

    // spec ops
    [TranslationData(SectionKitMenuUI, "Label that goes in front of night vision kills for Special Ops in kit stats.")]
    public static readonly Translation KitMenuUISpecOpsNVGKillsLabel = new Translation("NVG Kills (Night)", TranslationOptions.TMProUI);
    #endregion

    #region Options
    private const string SectionOptions = "Options";

    [TranslationData(SectionOptions, "Sent to the caller when the value given for the option is not parsable.", "Option name", "Type expected")]
    public static readonly Translation<string, Type> OptionsInvalidValue = new Translation<string, Type>("<#ff8c69>Expected a <#ddd>{1}</color> value for option <#ddd>{0}</color>.");

    [TranslationData(SectionOptions, "Sent to the caller when the value given for the option is already set.", "Option name", "Value of option")]
    public static readonly Translation<string, string> OptionsAlreadySet = new Translation<string, string>("<#ff8c69>Option <#ddd>{0}</color> is already set to <#ddd>{1}</color>.");

    [TranslationData(SectionOptions, "Sent to the caller when the value given for the option is set.", "Option name", "Value of option")]
    public static readonly Translation<string, string> OptionsSet = new Translation<string, string>("<#ff8c69>Option <#ddd>{0}</color> sucessfully set to <#ddd>{1}</color>.");
    #endregion

    #region Help Command
    private const string SectionHelp = "Help";

    [TranslationData(SectionHelp, "Output from help describing how to use /discord.")]
    public static readonly Translation HelpOutputDiscord = new Translation("<#b3ffb3>For more info, join our <#7483c4>Discord</color> server: <#fff>/discord</color>.");
    
    [TranslationData(SectionHelp, "Output from help describing how to use /request.")]
    public static readonly Translation HelpOutputRequest = new Translation("<#b3ffb3>To get gear, look at a sign in the barracks and type <#fff>/request</color> (or <#fff>/req</color>).");
    
    [TranslationData(SectionHelp, "Output from help describing how to use /deploy.")]
    public static readonly Translation HelpOutputDeploy = new Translation("<#b3ffb3>To deploy to battle, type <#fff>/deploy <location></color>. The locations are on the left side of your screen.");
    
    [TranslationData(SectionHelp, "Output from help describing how to use /deploy.")]
    public static readonly Translation HelpOutputCombined = new Translation("<#b3ffb3>To get gear, look at a sign in the barracks and type <#fff>/request</color> (or <#fff>/req</color>). To deploy to battle, type <#fff>/deploy <location></color> with any of the FOBs listed on the left of your screen. For more info, join our <#7483c4>Discord</color> server: <#fff>/discord</color>.");

    [TranslationData(SectionCommonErrors, "A player tried to get help with an unknown command.")]
    public static readonly Translation UnknownCommandHelp = new Translation("<#ff8c69>Unknown command. <#b3ffb3>Type <#fff>/help <command name></color> to look up a command.");
    #endregion

    [FormatDisplay(typeof(object), "Plural")]
    internal const string FormatPlural = "$plural$";
    [FormatDisplay(typeof(object), "Uppercase")]
    internal const string FormatUppercase = "upper";
    [FormatDisplay(typeof(object), "Lowercase")]
    internal const string FormatLowercase = "lower";
    [FormatDisplay(typeof(object), "Proper Case")]
    internal const string FormatPropercase = "proper";
    [FormatDisplay(typeof(QuestAsset), "Asset Rarity")]
    [FormatDisplay(typeof(VehicleAsset), "Asset Rarity")]
    [FormatDisplay(typeof(ItemAsset), "Asset Rarity")]
    internal const string FormatRarityColor = "rarity";
    public static readonly Translation[] Translations;
    public static readonly Dictionary<string, Translation> Signs;
    internal static readonly List<string> AllLanguages = new List<string>(4);
}
