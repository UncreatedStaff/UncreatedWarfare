﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Ranks;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Cache = Uncreated.Warfare.Components.Cache;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

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
    public static readonly Translation<Cooldown, string> CommandCooldown = new Translation<Cooldown, string>("<#ff8c69>You can't use <#fff>/{1}</color> for another <#aaa>{0}</color>.", Cooldown.FormatTimeLong, FormatLowercase);

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

    [TranslationData(SectionCommonErrors, "A command or feature is turned off in the configuration.")]
    public static readonly Translation NotEnabled = new Translation("<#ff8c69>This feature is not currently enabled.");

    [TranslationData(SectionCommonErrors, "The caller of a command has permission to use the command but isn't on duty.")]
    public static readonly Translation NotOnDuty = new Translation("<#ff8c69>You must be on duty to execute that command.");

    [TranslationData(SectionCommonErrors, "The value of a parameter was not in a valid time span format.", "Inputted text.")]
    public static readonly Translation<string> InvalidTime = new Translation<string>("<#ff8c69><#d09595>{0}</color> should be in a valid <#cedcde>TIME SPAN</color> format. Example: <#d09595>10d12h</color>, <#d09595>4mo15d12h</color>, <#d09595>2y</color>, <#d09595>permanent</color>.", UCPlayer.FormatCharacterName);
    #endregion

    #region Flags
    private const string SectionFlags = "Flags";

    [TranslationData(SectionFlags, "The caller of a command isn't on team 1 or 2.")]
    public static readonly Translation NotOnCaptureTeam = new Translation("<#ff8c69>You're not on a valid team.");

    [TranslationData(SectionFlags, "Sent when the player enters the capture radius of an active flag.", "Objective in question")]
    public static readonly Translation<Flag> EnteredCaptureRadius = new Translation<Flag>("<#e6e3d5>You have entered the capture radius of {0}.", Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent when the player leaves the capture radius of an active flag.", "Objective in question")]
    public static readonly Translation<Flag> LeftCaptureRadius = new Translation<Flag>("<#ff8c69>You have left the capture radius of {0}.", Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to all players on a flag that's being captured by their team (from neutral).", "Objective in question")]
    public static readonly Translation<Flag> FlagCapturing = new Translation<Flag>("<#e6e3d5>Your team is capturing {0}!", Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to all players on a flag that's being captured by the other team.", "Objective in question")]
    public static readonly Translation<Flag> FlagLosing = new Translation<Flag>("<#ff8c69>Your team is losing {0}!", Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to all players on a flag when it begins being contested.", "Objective in question")]
    public static readonly Translation<Flag> FlagContested = new Translation<Flag>("<#c$contested$>{0} is contested, eliminate some enemies to secure it!", Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to all players on a flag that's being cleared by their team (from the other team's ownership).", "Objective in question")]
    public static readonly Translation<Flag> FlagClearing = new Translation<Flag>("<#e6e3d5>Your team is clearing {0}!", Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to all players on a flag when it gets secured by their team.", "Objective in question")]
    public static readonly Translation<Flag> FlagSecured = new Translation<Flag>("<#c$secured$>{0} is secure for now, keep up the defense.", Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to a player that walks in the radius of a flag that isn't their team's objective.", "Objective in question")]
    public static readonly Translation<Flag> FlagNoCap = new Translation<Flag>("<#c$nocap$>{0} is not your objective, check the right of your screen to see which points to attack and defend.", Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to a player that walks in the radius of a flag that is owned by the other team and enough of the other team is on the flag so they can't contest the point.", "Objective in question")]
    public static readonly Translation<Flag> FlagNotOwned = new Translation<Flag>("<#c$nocap$>{0} is owned by the enemies. Get more players to capture it.", Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to a player that walks in the radius of a flag that is owned by the other team and has been locked from recapture.", "Objective in question")]
    public static readonly Translation<Flag> FlagLocked = new Translation<Flag>("<#c$locked$>{0} has already been captured, try to protect the objective to win.", Flag.COLOR_NAME_FORMAT);

    [TranslationData(SectionFlags, "Sent to all players when a flag gets neutralized.", "Objective in question")]
    public static readonly Translation<Flag> FlagNeutralized = new Translation<Flag>("<#e6e3d5>{0} has been neutralized!", TranslationFlags.PerTeamTranslation, Flag.COLOR_NAME_DISCOVER_FORMAT);

    [TranslationData(SectionFlags, "Gets broadcasted when a team captures a flag.")]
    public static readonly Translation<FactionInfo, Flag> TeamCaptured = new Translation<FactionInfo, Flag>("<#a0ad8e>{0} captured {1}.", TranslationFlags.PerTeamTranslation, FactionInfo.FormatColorDisplayName, Flag.COLOR_NAME_DISCOVER_FORMAT);

    [TranslationData(SectionFlags, "Backup translation for team 0 name and short name.")]
    public static readonly Translation Neutral = new Translation("Neutral", TranslationFlags.UnityUI);

    [TranslationData(SectionFlags, "Shows in place of the objective name for an undiscovered flag or objective.")]
    public static readonly Translation UndiscoveredFlag = new Translation("<color=#c$undiscovered_flag$>unknown</color>", TranslationFlags.UnityUI);

    [TranslationData(SectionFlags, "Shows in place of the objective name for an undiscovered flag or objective.")]
    public static readonly Translation UndiscoveredFlagNoColor = new Translation("unknown", TranslationFlags.NoColorOptimization);
    
    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's team is capturing a flag they're on.")]
    public static readonly Translation UICapturing = new Translation("CAPTURING",     TranslationFlags.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's team is losing a flag they're on because there isn't enough of them to contest it.")]
    public static readonly Translation UILosing = new Translation("LOSING", TranslationFlags.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's team is clearing a flag they're on.")]
    public static readonly Translation UIClearing = new Translation("CLEARING", TranslationFlags.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's team is contested with the other team on the flag they're on.")]
    public static readonly Translation UIContested = new Translation("CONTESTED", TranslationFlags.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's team owns flag they're on.")]
    public static readonly Translation UISecured = new Translation("SECURED", TranslationFlags.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's on a flag that isn't their team's objective.")]
    public static readonly Translation UINoCap = new Translation("NOT OBJECTIVE", TranslationFlags.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's team has too few people on a flag to contest and the other team owns the flag.")]
    public static readonly Translation UINotOwned = new Translation("TAKEN", TranslationFlags.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the objective they're on is owned by the other team and is locked from recapture.")]
    public static readonly Translation UILocked = new Translation("LOCKED", TranslationFlags.UnityUI);

    [TranslationData(SectionFlags, "Shows on the Capture UI when the player's in a vehicle on their objective.")]
    public static readonly Translation UIInVehicle = new Translation("IN VEHICLE", TranslationFlags.UnityUI);

    [TranslationData(SectionFlags, "Shows above the flag list UI.")]
    public static readonly Translation FlagsHeader = new Translation("Flags", TranslationFlags.UnityUI);

    [TranslationData(SectionFlags, "Shows above the cache list UI.")]
    public static readonly Translation CachesHeader = new Translation("Caches", TranslationFlags.UnityUI);
    #endregion

    #region Players
    private const string SectionPlayers = "Players";

    [TranslationData(SectionPlayers, "Gets sent to a player who's discord is not linked to their steam account (part 1).")]
    public static readonly Translation DiscordNotLinked = new Translation("<#9cffb3>Your account must be linked in our Discord server to use this command.");

    [TranslationData(SectionPlayers, "Gets sent to a player who's discord is not linked to their steam account (part 2).", "Player's Steam64 ID")]
    public static readonly Translation<IPlayer> DiscordNotLinked2 = new Translation<IPlayer>("<#9cffb3>Type <#7483c4>/discord</color> then type <#fff>/link {0}</color> in <#c480d9>#warfare-stats</color>.", UCPlayer.FormatColoredSteam64);

    [TranslationData(SectionPlayers, "Gets broadcasted when a player connects.", "Connecting player")]
    public static readonly Translation<IPlayer> PlayerConnected = new Translation<IPlayer>("<#e6e3d5>{0} joined the server.");

    [TranslationData(SectionPlayers, "Gets broadcasted when a player disconnects.", "Disconnecting player")]
    public static readonly Translation<IPlayer> PlayerDisconnected = new Translation<IPlayer>("<#e6e3d5>{0} left the server.");

    [TranslationData(SectionPlayers, "Kick message for a player that suffers from a rare bug which will cause GameObject.get_transform() to throw a NullReferenceException (not return null). They are kicked if this happens.", "Discord Join Code")]
    public static readonly Translation<string> NullTransformKickMessage = new Translation<string>("Your character is bugged, which messes up our zone plugin. Rejoin or contact a Director if this continues. (discord.gg/{0}).", TranslationFlags.NoColorOptimization);

    [TranslationData(SectionPlayers, "Gets sent to a player when their message gets blocked by the chat filter.", "Section of the message that matched the chat filter.")]
    public static readonly Translation<string> ChatFilterFeedback = new Translation<string>("<#ff8c69>Our chat filter flagged <#fdfdfd>{0}</color>, so the message wasn't sent.");
    
    [TranslationData(SectionPlayers, "Gets sent to a player when their message gets blocked by the chat filter.", "Amount of alphanumeric characters in succession.")]
    public static readonly Translation<int> NameFilterKickMessage = new Translation<int>("Your name does not contain enough alphanumeric characters in succession ({0}), please change your name and rejoin.", TranslationFlags.NoColorOptimization);
    
    [TranslationData(SectionPlayers, "Gets sent to a player if they're banned when they join.")]
    public static readonly Translation<string, int> RejectBanned = new Translation<string, int>("You are banned for {1}: \"{0}\".", TranslationFlags.NoColorOptimization, arg1Fmt: FormatTimeLongAbbreviated);

    [TranslationData(SectionPlayers, "Gets sent to a player if they're permanently banned when they join.")]
    public static readonly Translation<string> RejectPermanentBanned = new Translation<string>("You are permanently banned: \"{0}\".", TranslationFlags.NoColorOptimization);
    
    [TranslationData(SectionPlayers, "Gets sent to a player when their nick name gets blocked by the chat filter.", "Violating text.")]
    public static readonly Translation<string> NameProfanityNickNameKickMessage = new Translation<string>("Your nickname is in violation of our profanity filter: \"{0}\". Please change your name and rejoin.", TranslationFlags.NoColorOptimization);
    
    [TranslationData(SectionPlayers, "Gets sent to a player when their character name gets blocked by the chat filter.", "Violating text.")]
    public static readonly Translation<string> NameProfanityCharacterNameKickMessage = new Translation<string>("Your character name is in violation of our profanity filter: \"{0}\". Please change your name and rejoin.", TranslationFlags.NoColorOptimization);
    
    [TranslationData(SectionPlayers, "Gets sent to a player when their player name gets blocked by the chat filter.", "Violating text.")]
    public static readonly Translation<string> NameProfanityPlayerNameKickMessage = new Translation<string>("Your Steam name is in violation of our profanity filter: \"{0}\". Please change your name and rejoin.", TranslationFlags.NoColorOptimization);
    
    [TranslationData(SectionPlayers, "Gets sent to a player who is attempting to main camp the other team.")]
    public static readonly Translation AntiMainCampWarning = new Translation("<#fa9e9e>Stop <b><#ff3300>main-camping</color></b>! Damage is <b>reversed</b> back on you.");
    
    [TranslationData(SectionPlayers, "Gets sent to a player who is trying to place a non-whitelisted barricade on a vehicle.", "Barricade being placed")]
    public static readonly Translation<ItemBarricadeAsset> NoPlacementOnVehicle = new Translation<ItemBarricadeAsset>("<#fa9e9e>You can't place {0} on a vehicle!</color>", FormatRarityColor + FormatPlural);
    
    [TranslationData(SectionPlayers, "Generic message sent when a player is placing something in a place they shouldn't.", "Item being placed")]
    public static readonly Translation<ItemAsset> ProhibitedPlacement = new Translation<ItemAsset>("<#fa9e9e>You're not allowed to place {0} here.", FormatRarityColor + FormatPlural);
    
    [TranslationData(SectionPlayers, "Generic message sent when a player is dropping an item where they shouldn't.", "Item being dropped", "Zone or flag the player is dropping their item in.")]
    public static readonly Translation<ItemAsset, IZone> ProhibitedDropZone = new Translation<ItemAsset, IZone>("<#fa9e9e>You're not allowed to drop {0} in {1}.", FormatRarityColor + FormatPlural, Flag.COLOR_NAME_DISCOVER_FORMAT);
    
    [TranslationData(SectionPlayers, "Generic message sent when a player is picking up an item where they shouldn't.", "Item being picked up", "Zone or flag the player is picking up their item in.")]
    public static readonly Translation<ItemAsset, IZone> ProhibitedPickupZone = new Translation<ItemAsset, IZone>("<#fa9e9e>You're not allowed to pick up {0} in {1}.", FormatRarityColor + FormatPlural, Flag.COLOR_NAME_DISCOVER_FORMAT);
    
    [TranslationData(SectionPlayers, "Generic message sent when a player is placing something in a zone they shouldn't be.", "Item being placed", "Zone or flag the player is placing their item in.")]
    public static readonly Translation<ItemAsset, IZone> ProhibitedPlacementZone = new Translation<ItemAsset, IZone>("<#fa9e9e>You're not allowed to place {0} in {1}.", FormatRarityColor + FormatPlural, Flag.COLOR_NAME_DISCOVER_FORMAT);
    
    [TranslationData(SectionPlayers, "Sent when a player tries to steal a battery.")]
    public static readonly Translation NoStealingBatteries = new Translation("<#fa9e9e>Stealing batteries is not allowed.</color>");
    
    [TranslationData(SectionPlayers, "Sent when a player tries to manually leave their group.")]
    public static readonly Translation NoLeavingGroup = new Translation("<#fa9e9e>You are not allowed to manually change groups, use <#cedcde>/teams</color> instead.");
    
    [TranslationData(SectionPlayers, "Message sent when a player tries to place a non-whitelisted item in a storage inventory.", "Item being stored")]
    public static readonly Translation<ItemAsset> ProhibitedStoring = new Translation<ItemAsset>("<#fa9e9e>You are not allowed to store {0}.", FormatRarityColor + FormatPlural);
    
    [TranslationData(SectionPlayers, "Sent when a player tries to point or mark while not a squad leader.")]
    public static readonly Translation MarkerNotInSquad = new Translation("<#fa9e9e>Only your squad can see markers. Create a squad with <#cedcde>/squad create</color> to use this feature.");
    
    [TranslationData(SectionPlayers, "Sent on a SEVERE toast when the player enters enemy territory.", "Seconds until death")]
    public static readonly Translation<string> EnteredEnemyTerritory = new Translation<string>("ENEMY HQ PROXIMITY\nLEAVE IMMEDIATELY\nDEAD IN <uppercase>{0}</uppercase>", TranslationFlags.UnityUI);
    
    [TranslationData(SectionPlayers, "Sent 2 times before a player is kicked for inactivity.", "Time code")]
    public static readonly Translation<string> InactivityWarning = new Translation<string>("<#fa9e9e>You will be AFK-Kicked in <#cedcde>{0}</color> if you don't move.</color>");
    
    [TranslationData(SectionPlayers, "Broadcasted when a player is removed from the game by BattlEye.", "Player being kicked.")]
    public static readonly Translation<IPlayer> BattlEyeKickBroadcast = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was kicked by <#feed00>BattlEye</color>.", UCPlayer.FormatPlayerName);
    
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
    public static readonly Translation MortarWarning = new Translation("FRIENDLY MORTAR\nINCOMING", TranslationFlags.TMProUI);
    #endregion

    #region Leaderboards

    private const string SectionLeaderboard = "Leaderboard";
    #region Shared
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation StartingSoon                   = new Translation("Starting soon...", TranslationFlags.UnityUI);

    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<string> NextGameShutdown       = new Translation<string>("<#94cbff>Shutting Down Because: \"{0}\"</color>", TranslationFlags.UnityUI);

    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<TimeSpan> NextGameShutdownTime = new Translation<TimeSpan>("{0}", TranslationFlags.UnityUI, "mm\\:ss");

    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo, FactionInfo> WarstatsHeader = new Translation<FactionInfo, FactionInfo>("{0} vs {1}", TranslationFlags.UnityUI, FactionInfo.FormatColorShortName, FactionInfo.FormatColorShortName);
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<IPlayer, float> PlayerstatsHeader       = new Translation<IPlayer, float>("{0} - {1} presence", TranslationFlags.UnityUI, UCPlayer.FormatColoredCharacterName, "P0");
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> WinnerTitle                 = new Translation<FactionInfo>("{0} Wins!", TranslationFlags.UnityUI, FactionInfo.FormatColorShortName);

    [TranslationData(SectionLeaderboard, FormattingDescriptions = new string[] { "Distance", "Gun Name", "Player" })]
    public static readonly Translation<float, ItemAsset, IPlayer> LongestShot     = new Translation<float, ItemAsset, IPlayer>("{0}m - {1}\n{2}", TranslationFlags.UnityUI, "F1", arg2Fmt: UCPlayer.FormatColoredCharacterName);
    #endregion

    #region CTFBase
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats0  = new Translation("Kills: ",            TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats1  = new Translation("Deaths: ",           TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats2  = new Translation("K/D Ratio: ",        TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats3  = new Translation("Kills on Point: ",   TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats4  = new Translation("Time Deployed: ",    TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats5  = new Translation("XP Gained: ",        TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats6  = new Translation("Time on Point: ",    TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats7  = new Translation("Captures: ",         TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats8  = new Translation("Time in Vehicle: ",  TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats9  = new Translation("Teamkills: ",        TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats10 = new Translation("FOBs Destroyed: ",   TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFPlayerStats11 = new Translation("Credits Gained: ",   TranslationFlags.UnityUI);


    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFWarStats0 = new Translation("Duration: ", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",     TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",     TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFWarStats3 = new Translation("Flag Captures: ", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",   TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",   TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",    TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",    TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ", TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> CTFWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ", TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFWarStats10 = new Translation("Teamkill Casualties: ", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFWarStats11 = new Translation("Longest Shot: ",        TranslationFlags.UnityUI);


    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFHeader0 = new Translation("Kills",   TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFHeader1 = new Translation("Deaths",  TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFHeader2 = new Translation("XP",      TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFHeader3 = new Translation("Caps", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFHeader4 = new Translation("Vehicles\nDestr.",    TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation CTFHeader5 = new Translation("Aircraft\nDestr.",  TranslationFlags.UnityUI);
    #endregion

    #region Insurgency
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats0  = new Translation("Kills: ",                 TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats1  = new Translation("Deaths: ",                TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats2  = new Translation("Damage Done: ",           TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats3  = new Translation("Objective Kills: ",       TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats4  = new Translation("Time Deployed: ",         TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats5  = new Translation("XP Gained: ",             TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats6  = new Translation("Intelligence Gathered: ", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats7  = new Translation("Caches Discovered: ",     TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats8  = new Translation("Caches Destroyed: ",      TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats9  = new Translation("Teamkills: ",             TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats10 = new Translation("FOBs Destroyed: ",        TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyPlayerStats11 = new Translation("Credits Gained: ",        TranslationFlags.UnityUI);


    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyWarStats0 = new Translation("Duration: ", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyWarStats3 = new Translation("Intelligence Gathered: ", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> InsurgencyWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyWarStats10 = new Translation("Teamkill Casualties: ", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyWarStats11 = new Translation("Longest Shot: ",        TranslationFlags.UnityUI);

    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyHeader0 = new Translation("Kills",   TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyHeader1 = new Translation("Deaths",  TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyHeader2 = new Translation("XP",      TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyHeader3 = new Translation("KDR", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyHeader4 = new Translation("Vehicles\nDestr.",     TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation InsurgencyHeader5 = new Translation("Aircraft\nDestr.",  TranslationFlags.UnityUI);
    #endregion

    #region Conquest
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats0  = new Translation("Kills: ",                 TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats1  = new Translation("Deaths: ",                TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats2  = new Translation("Damage Done: ",           TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats3  = new Translation("Objective Kills: ",       TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats4  = new Translation("Time Deployed: ",         TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats5  = new Translation("XP Gained: ",             TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats6  = new Translation("Revives: ",               TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats7  = new Translation("Flags Captured: ",        TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats8  = new Translation("Time on Flag: ",          TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats9  = new Translation("Teamkills: ",             TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats10 = new Translation("FOBs Destroyed: ",        TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestPlayerStats11 = new Translation("Credits Gained: ",        TranslationFlags.UnityUI);

    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestWarStats0 = new Translation("Duration: ",                                      TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestWarStats3 = new Translation("Flag Captures: ",                                 TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> ConquestWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestWarStats10 = new Translation("Teamkill Casualties: ", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestWarStats11 = new Translation("Longest Shot: ",        TranslationFlags.UnityUI);

    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestHeader0 = new Translation("Kills",   TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestHeader1 = new Translation("Deaths",  TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestHeader2 = new Translation("XP",      TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestHeader3 = new Translation("Captures", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestHeader4 = new Translation("Vehicles\nDestr.",     TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation ConquestHeader5 = new Translation("Aircraft\nDestr.",  TranslationFlags.UnityUI);
    #endregion

    #region Hardpoint
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats0  = new Translation("Kills: ",                 TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats1  = new Translation("Deaths: ",                TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats2  = new Translation("Damage Done: ",           TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats3  = new Translation("Objective Kills: ",       TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats4  = new Translation("Time Deployed: ",         TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats5  = new Translation("XP Gained: ",             TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats6  = new Translation("Revives: ",               TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats7  = new Translation("Points Gained: ",         TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats8  = new Translation("Time on Flag: ",          TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats9  = new Translation("Teamkills: ",             TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats10 = new Translation("FOBs Destroyed: ",        TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointPlayerStats11 = new Translation("Credits Gained: ",        TranslationFlags.UnityUI);

    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointWarStats0 = new Translation("Duration: ",                                      TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointWarStats3 = new Translation("Contesting Time: ",                               TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation<FactionInfo> HardpointWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationFlags.UnityUI, FactionInfo.FormatShortName);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointWarStats10 = new Translation("Teamkill Casualties: ", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointWarStats11 = new Translation("Longest Shot: ",        TranslationFlags.UnityUI);

    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointHeader0 = new Translation("Kills",    TranslationFlags.UnityUI);

    [TranslationData(SectionLeaderboard)]                                             
    public static readonly Translation HardpointHeader1 = new Translation("Deaths",   TranslationFlags.UnityUI);

    [TranslationData(SectionLeaderboard)]                                             
    public static readonly Translation HardpointHeader2 = new Translation("XP",       TranslationFlags.UnityUI);

    [TranslationData(SectionLeaderboard)]                                             
    public static readonly Translation HardpointHeader3 = new Translation("Cap Seconds",  TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointHeader4 = new Translation("Vehicles\nDestr.", TranslationFlags.UnityUI);
    
    [TranslationData(SectionLeaderboard)]
    public static readonly Translation HardpointHeader5 = new Translation("Aircraft\nDestr.",  TranslationFlags.UnityUI);
    #endregion

    #endregion

    #region GroupCommand
    private const string SectionGroup = "Groups";
    
    [TranslationData(SectionGroup, "Output from /group, tells the player their current group.", "Group ID", "Group Name", "Team Color (if applicable)")]
    public static readonly Translation<ulong, string, Color> CurrentGroup = new Translation<ulong, string, Color>("<#e6e3d5>Group <#{2}>{0}</color>: <#{2}>{1}</color>");
    
    [TranslationData(SectionGroup, "Output from /group join <id>.", "Group ID", "Group Name", "Team Color (if applicable)", IsPrioritizedTranslation = false)]
    public static readonly Translation<ulong, string, Color> JoinedGroup  = new Translation<ulong, string, Color>("<#e6e3d5>You have joined group <#{2}>{0}</color>: <#{2}>{1}</color>.");
    
    [TranslationData(SectionGroup, "Output from /group when the player is not in a group.", IsPrioritizedTranslation = false)]
    public static readonly Translation NotInGroup           = new Translation("<#ff8c69>You aren't in a group.");
    
    [TranslationData(SectionGroup, "Output from /group join <id> when the player is already in that group.", IsPrioritizedTranslation = false)]
    public static readonly Translation AlreadyInGroup       = new Translation("<#ff8c69>You are already in that group.");
    
    [TranslationData(SectionGroup, "Output from /group join <id> when the group is not found.", "Input", IsPrioritizedTranslation = false)]
    public static readonly Translation<string> GroupNotFound = new Translation<string>("<#ff8c69>Could not find group <#4785ff>{0}</color>.");
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
    public static readonly Translation<LanguageInfo> LanguageCurrent = new Translation<LanguageInfo>("<#f53b3b>Current language: <#e6e3d5>{0}</color>.", LanguageInfo.FormatDisplayName);
    
    [TranslationData(SectionLanguages, "Output from /lang <language>, tells the player their new language.", "New Language")]
    public static readonly Translation<LanguageInfo> ChangedLanguage = new Translation<LanguageInfo>("<#f53b3b>Changed your language to <#e6e3d5>{0}</color>.", LanguageInfo.FormatDisplayName);
    
    [TranslationData(SectionLanguages, "Output from /lang <language> when the player is using already that language.", "Current Language")]
    public static readonly Translation<LanguageInfo> LangAlreadySet  = new Translation<LanguageInfo>("<#ff8c69>You are already set to <#e6e3d5>{0}</color>.", LanguageInfo.FormatDisplayName);
    
    [TranslationData(SectionLanguages, "Output from /lang reset, tells the player their language changed to the default language.", "Default Language")]
    public static readonly Translation<LanguageInfo> ResetLanguage   = new Translation<LanguageInfo>("<#f53b3b>Reset your language to <#e6e3d5>{0}</color>.", LanguageInfo.FormatDisplayName);
    
    [TranslationData(SectionLanguages, "Output from /lang reset when the player is using already that language.", "Default Language")]
    public static readonly Translation<LanguageInfo> ResetCurrent    = new Translation<LanguageInfo>("<#ff8c69>You are already on the default language: <#e6e3d5>{0}</color>.", LanguageInfo.FormatDisplayName);
    
    [TranslationData(SectionLanguages, "Output from /lang <language> when the language isn't found.", "Input language")]
    public static readonly Translation<string> LanguageNotFound          = new Translation<string>("<#dd1111>We don't have translations for <#e6e3d5>{0}</color> yet. If you are fluent and want to help, feel free to ask us about submitting translations.");

    [TranslationData(SectionLanguages, "Tells the player that IMGUI is recommended for this language and how to enable it (part 1).", "Language id")]
    public static readonly Translation<LanguageInfo> IMGUITip1       = new Translation<LanguageInfo>("<#f53b3b>{0} recommends using IMGUI mode. do <#fff>/options imgui true</color>...", LanguageInfo.FormatDisplayName);
    [TranslationData(SectionLanguages, "Tells the player that IMGUI is recommended for this language and how to enable it (part 2).")]
    public static readonly Translation IMGUITip2                         = new Translation("<#f53b3b>... go to your steam launch options and add <#fff>-Glazier IMGUI</color> to them.");

    [TranslationData(SectionLanguages, "Tells the player that IMGUI is not recommended for this language and how to enable it (part 1).", "Language id")]
    public static readonly Translation<LanguageInfo> NoIMGUITip1     = new Translation<LanguageInfo>("<#f53b3b>{0} recommends not using IMGUI mode. do <#fff>/options imgui false</color>...", LanguageInfo.FormatDisplayName);
    [TranslationData(SectionLanguages, "Tells the player that IMGUI is not recommended for this language and how to enable it (part 2).")]
    public static readonly Translation NoIMGUITip2 = new Translation("<#f53b3b>... go to your steam launch options and remove <#fff>-Glazier IMGUI</color>.");
    #endregion

    #region Toasts
    private const string SectionToasts = "Toasts";
    [TranslationData(SectionToasts, "Sent when the player joins for the 1st time.")]
    public static readonly Translation<IPlayer> WelcomeMessage = new Translation<IPlayer>("Welcome to <#c$uncreated$>Uncreated Warfare</color> {0}!\nCheck out our tutorial to get started (follow the signs).", TranslationFlags.UnityUI, UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionToasts, "Broadcasted when a game is loading.", "Next gamemode")]
    public static readonly Translation<Gamemode> LoadingGamemode = new Translation<Gamemode>("Loading New Gamemode\n<#66ff99>{0}</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionToasts, "Broadcasted when a player joins and their data is loading.")]
    public static readonly Translation LoadingOnJoin = new Translation("Loading Player Data", TranslationFlags.TMProUI);
    [TranslationData(SectionToasts, "Title for the welcome message.")]
    public static readonly Translation WelcomeMessageTitle = new Translation("Welcome to Uncreated Warfare", TranslationFlags.TMProUI);
    #endregion

    #region KitCommand
    private const string SectionKits = "Kits";
    
    [TranslationData(SectionKits, "Sent when the player creates a new kit with /kit create <name>", "New Kit", IsPrioritizedTranslation = false)]
    public static readonly Translation<Kit> KitCreated          = new Translation<Kit>("<#a0ad8e>Created kit: <#fff>{0}</color>.", Kit.IdFormat);
    
    [TranslationData(SectionKits, "Sent when the player overwrites the items in a kit with /kit create <name>", "Overwritten Kit", IsPrioritizedTranslation = false)]
    public static readonly Translation<Kit> KitOverwrote        = new Translation<Kit>("<#a0ad8e>Overwritten items for kit: <#fff>{0}</color>.", Kit.IdFormat);
    
    [TranslationData(SectionKits, "Sent when the player tries overwriting the items in a kit with /kit create <name>. They must /confirm first.", "Overwritten Kit", "Overwritten Kit", IsPrioritizedTranslation = false)]
    public static readonly Translation<Kit, Kit> KitConfirmOverride = new Translation<Kit, Kit>("<#c480d9>Type <#aaa>/confirm</color> in the next 10 seconds if you want to override the items in <#fff>{0}</color> (<#aaa>{1}</color>).", Kit.IdFormat, Kit.DisplayNameFormat);
    
    [TranslationData(SectionKits, "Sent when the player tries deleting kit with /kit delete <name>. They must /confirm first.", "Deleting Kit", "Deleting Kit", IsPrioritizedTranslation = false)]
    public static readonly Translation<Kit, Kit> KitConfirmDelete = new Translation<Kit, Kit>("<#c480d9>Type <#aaa>/confirm</color> in the next 10 seconds if you want to delete <#fff>{0}</color> (<#aaa>{1}</color>).", Kit.IdFormat, Kit.DisplayNameFormat);
    
    [TranslationData(SectionKits, "Sent when the player doesn't /confirm in time for overwriting kit items.", IsPrioritizedTranslation = false)]
    public static readonly Translation KitCancelOverride        = new Translation("<#ff8c69>Item override cancelled.");

    [TranslationData(SectionKits, "Sent when the player doesn't /confirm in time for deleting a kit.", IsPrioritizedTranslation = false)]
    public static readonly Translation KitCancelDelete          = new Translation("<#ff8c69>Deleting kit cancelled.");
    
    [TranslationData(SectionKits, "Sent when the player copies a kit with /kit copyfrom <source> <name>", "Source Kit", "New Kit", IsPrioritizedTranslation = false)]
    public static readonly Translation<Kit, Kit> KitCopied      = new Translation<Kit, Kit>("<#a0ad8e>Copied data from <#c7b197>{0}</color> into a new kit: <#fff>{1}</color>.", Kit.IdFormat, Kit.IdFormat);
    
    [TranslationData(SectionKits, "Sent when the player deletes a kit with /kit delete <name>", IsPrioritizedTranslation = false)]
    public static readonly Translation<Kit> KitDeleted          = new Translation<Kit>("<#a0ad8e>Deleted kit: <#fff>{0}</color>.", Kit.IdFormat);
    [TranslationData(SectionKits)]
    public static readonly Translation<string> KitSearchResults = new Translation<string>("<#a0ad8e>Matches: <i>{0}</i>.");
    [TranslationData(SectionKits)]
    public static readonly Translation<Kit> KitAccessGivenDm    = new Translation<Kit>("<#a0ad8e>You were given access to the kit: <#fff>{0}</color>.", Kit.IdFormat);
    [TranslationData(SectionKits)]
    public static readonly Translation<Kit> KitAccessRevokedDm  = new Translation<Kit>("<#a0ad8e>Your access to <#fff>{0}</color> was revoked.", Kit.IdFormat);
    [TranslationData(SectionKits)]
    public static readonly Translation KitHotkeyNotHoldingItem  = new Translation("<#ff8c69>You must be holding an item from your kit to set a hotkey.");
    [TranslationData(SectionKits)]
    public static readonly Translation<ItemAsset> KitHotkeyNotHoldingValidItem = new Translation<ItemAsset>("<#ff8c69><#ffe6d7>{0}</color> can not be eqipped in a hotkey slot <#ddd>(3-9 and 0)</color>.");
    [TranslationData(SectionKits)]
    public static readonly Translation<ItemAsset, byte, Kit> KitHotkeyBinded   = new Translation<ItemAsset, byte, Kit>("<#a0ad8e>Binded <#e8e2d1>{0}</color> to slot <#e8e2d1>{1}</color> for <#fff>{2}</color>.", arg2Fmt: Kit.DisplayNameFormat);
    [TranslationData(SectionKits)]
    public static readonly Translation<byte, Kit> KitHotkeyUnbinded            = new Translation<byte, Kit>("<#a0ad8e>Unbinded slot <#e8e2d1>{0}</color> for <#fff>{1}</color>.", arg1Fmt: Kit.DisplayNameFormat);
    [TranslationData(SectionKits)]
    public static readonly Translation<byte, Kit> KitHotkeyNotFound            = new Translation<byte, Kit>("<#ff8c69>Slot <#e8e2d1>{0}</color> for <#fff>{1}</color> was not binded.", arg1Fmt: Kit.DisplayNameFormat);
    [TranslationData(SectionKits)]
    public static readonly Translation<Kit> KitLayoutsNotSupported             = new Translation<Kit>("<#ff8c69>A custom layout is not allowed for <#fff>{0}</color>.", Kit.DisplayNameFormat);
    [TranslationData(SectionKits)]
    public static readonly Translation<Kit> KitLayoutSaved                     = new Translation<Kit>("<#a0ad8e>Custom layout for <#fff>{0}</color> saved.", Kit.DisplayNameFormat);
    [TranslationData(SectionKits)]
    public static readonly Translation<Kit> KitLayoutReset                     = new Translation<Kit>("<#a0ad8e>Custom layout for <#fff>{0}</color> reset.", Kit.DisplayNameFormat);
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> KitLanguageNotFound             = new Translation<string>("<#ff8c69>Language not found: <#fff>{0}</color>.");
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<string, Kit, string> KitPropertySet     = new Translation<string, Kit, string>("<#a0ad8e>Set <#aaa>{0}</color> on kit <#fff>{1}</color> to <#aaa><uppercase>{2}</uppercase></color>.", arg1Fmt: Kit.IdFormat);
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> KitNameTaken                    = new Translation<string>("<#ff8c69>A kit named <#fff>{0}</color> already exists.");
    [TranslationData(SectionKits)]
    public static readonly Translation<string> KitNotFound                     = new Translation<string>("<#ff8c69>A kit named <#fff>{0}</color> doesn't exist.");
    [TranslationData(SectionKits)]
    public static readonly Translation<Kit> KitRenameNotLoadout                = new Translation<Kit>("<#ff8c69><#fff>{0}</color> isn't a loadout.", Kit.DisplayNameFormat);
    [TranslationData(SectionKits)]
    public static readonly Translation<string> KitRenameFilterVoilation        = new Translation<string>("<#ff8c69>Your name violates our chat filter near: <#fff>'{0}'</color>.");
    [TranslationData(SectionKits)]
    public static readonly Translation<string, string, string> KitRenamed      = new Translation<string, string, string>("<#a0ad8e>Renamed loadout <#fff>{0}</color> from <#ddd>\"{1}\"</color> to <#ddd>\"{2}\"</color>.");
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> KitPropertyNotFound             = new Translation<string>("<#ff8c69>Kits don't have a <#eee>{0}</color> property.");
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> KitPropertyProtected            = new Translation<string>("<#ff8c69><#eee>{0}</color> can not be changed on kits.");
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, Kit> KitAlreadyHasAccess       = new Translation<IPlayer, Kit>("<#ff8c69>{0} already has access to <#fff>{1}</color>.", UCPlayer.FormatColoredCharacterName, Kit.IdFormat);
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, Kit> KitAlreadyMissingAccess   = new Translation<IPlayer, Kit>("<#ff8c69>{0} doesn't have access to <#fff>{1}</color>.", UCPlayer.FormatColoredCharacterName, Kit.IdFormat);
    [TranslationData(SectionKits)]
    public static readonly Translation<Cooldown> KitOnCooldown                 = new Translation<Cooldown>("<#ff8c69>You can request this kit again in: <#bafeff>{0}</color>.", Cooldown.FormatTimeShort);
    [TranslationData(SectionKits)]
    public static readonly Translation<Cooldown> KitOnGlobalCooldown           = new Translation<Cooldown>("<#ff8c69>You can request another kit again in: <#bafeff>{0}</color>.", Cooldown.FormatTimeShort);
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, ulong, Kit> KitAccessGiven           = new Translation<IPlayer, ulong, Kit>("<#a0ad8e>{0} (<#aaa>{1}</color>) was given access to the kit: <#fff>{2}</color>.", UCPlayer.FormatColoredPlayerName, arg2Fmt: Kit.IdFormat);
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, ulong, Kit> KitAccessRevoked         = new Translation<IPlayer, ulong, Kit>("<#a0ad8e>{0} (<#aaa>{1}</color>)'s access to <#fff>{2}</color> was taken away.", UCPlayer.FormatColoredPlayerName, arg2Fmt: Kit.IdFormat);
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<string, Type, string> KitInvalidPropertyValue = new Translation<string, Type, string>("<#ff8c69><#fff>{2}</color> isn't a valid value for <#eee>{0}</color> (<#aaa>{1}</color>).");
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<Class, IPlayer, ulong, Kit> LoadoutCreated    = new Translation<Class, IPlayer, ulong, Kit>("<#a0ad8e>Created <#bbc>{0}</color> loadout for {1} (<#aaa>{2}</color>). Kit name: <#fff>{3}</color>.", arg1Fmt: UCPlayer.FormatColoredCharacterName, arg3Fmt: Kit.IdFormat);
    [TranslationData(SectionKits)]
    public static readonly Translation<ItemAsset> KitProhibitedPickupAmt             = new Translation<ItemAsset>("<#ff8c69>Your kit does not allow you to have any more {0}.", FormatRarityColor + FormatPlural);
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> FactionNotFoundCreateKit              = new Translation<string>("<#ff8c69>Unable to find a faction called <#fff>{0}</color>.");
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> ClassNotFoundCreateKit                = new Translation<string>("<#ff8c69>There is no kit class named <#fff>{0}</color>.");
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> TypeNotFoundCreateKit                 = new Translation<string>("<#ff8c69>There is no kit type named <#fff>{0}</color>. Use: 'public', 'elite', 'special', 'loadout'.");
    [TranslationData(SectionKits)]
    public static readonly Translation<Kit> KitFavoriteAlreadyFavorited              = new Translation<Kit>("<#ff8c69><#e8e2d1>{0}</color> is already <#fd0>favorited</color>.");
    [TranslationData(SectionKits)]
    public static readonly Translation<Kit> KitFavoriteAlreadyUnfavorited            = new Translation<Kit>("<#ff8c69><#e8e2d1>{0}</color> is already <#fff>unfavorited</color>.");
    [TranslationData(SectionKits)]
    public static readonly Translation<Kit> KitFavorited                             = new Translation<Kit>("<#a0ad8e>Added <#e8e2d1>{0}</color> to your <#fd0>favorites</color>.");
    [TranslationData(SectionKits)]
    public static readonly Translation<Kit> KitUnfavorited                           = new Translation<Kit>("<#a0ad8e>Removed <#e8e2d1>{0}</color> from your <#fd0>favorites</color>.");
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation KitLoadoutIdBadFormat                         = new Translation("Kit name must be in format: <b>765XXXXXXXXXXXXXX_X..</b>.");
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<Kit, Class> LoadoutUpgraded                   = new Translation<Kit, Class>("<#a0ad8e>Upgraded <#e8e2d1>{0}</color> to a new <#fff>{1}</color> kit.", Kit.DisplayNameFormat, FormatUppercase);
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<Kit> KitUpgraded                              = new Translation<Kit>("<#a0ad8e>Upgraded <#e8e2d1>{0}</color>.", Kit.DisplayNameFormat);
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<Kit> KitUnlocked                              = new Translation<Kit>("<#a0ad8e>Unlocked <#e8e2d1>{0}</color>.", Kit.DisplayNameFormat);
    [TranslationData(SectionKits, IsPrioritizedTranslation = false)]
    public static readonly Translation<Kit> KitLocked                                = new Translation<Kit>("<#a0ad8e>Locked <#e8e2d1>{0}</color>.", Kit.DisplayNameFormat);
    [TranslationData(SectionKits, IsPrioritizedTranslation = true)]
    public static readonly Translation<Kit> DMLoadoutUnlocked                        = new Translation<Kit>("<#a0ad8e>Your kit, <#e8e2d1>{0}</color>, is ready.", Kit.DisplayNameFormat);

    [TranslationData(SectionKits, "Sent when the caller doesn't enter a valid integer for level.", "Skill name", "Max level", IsPrioritizedTranslation = false)]
    public static readonly Translation<string, int> KitInvalidSkillsetLevel          = new Translation<string, int>("<#ff8c69>Please give a level between <#fff>0</color> and <#fff>{1}</color> for <#ddd>{0}</color>");

    [TranslationData(SectionKits, "Sent when the caller doesn't enter a valid integer for level.", "Skill name", "Max level", IsPrioritizedTranslation = false)]
    public static readonly Translation<string> KitInvalidSkillset                    = new Translation<string>("<#ff8c69>\"<#fff>{0}</color>\" is not a valid skill name, use the displayed value in-game.");

    [TranslationData(SectionKits, "Sent when the skillset requested to be removed isn't present.", "Skill set", IsPrioritizedTranslation = false)]
    public static readonly Translation<Skillset, Kit> KitSkillsetNotFound            = new Translation<Skillset, Kit>("<#ff8c69>\"<#ddd>{0}</color>\" is not overridden by <#fff>{1}</color>.", Skillset.FormatNoLevel, Kit.DisplayNameFormat);

    [TranslationData(SectionKits, "Sent when a skillset is removed.", "Skill set", "Kit target", IsPrioritizedTranslation = false)]
    public static readonly Translation<Skillset, Kit> KitSkillsetRemoved             = new Translation<Skillset, Kit>("<#a0ad8e>\"<#ddd>{0}</color>\" was removed from <#fff>{1}</color>.", arg1Fmt: Kit.DisplayNameFormat);

    [TranslationData(SectionKits, "Sent when a skillset is added.", "Skill set", "Kit target", IsPrioritizedTranslation = false)]
    public static readonly Translation<Skillset, Kit> KitSkillsetAdded               = new Translation<Skillset, Kit>("<#a0ad8e>\"<#ddd>{0}</color>\" was added to <#fff>{1}</color>.", arg1Fmt: Kit.DisplayNameFormat);
    #endregion

    #region RangeCommand
    private const string SectionRange = "Range Command";
    [TranslationData(SectionRange)]
    public static readonly Translation<float> RangeOutput  = new Translation<float>("<#9e9c99>The range to your squad's marker is: <#8aff9f>{0}m</color>.", "N0");
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
    public static readonly Translation<Squad> SquadCreated          = new Translation<Squad>("<#a0ad8e>You created {0} squad.", Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadJoined           = new Translation<Squad>("<#a0ad8e>You joined {0} squad.", Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadLeft             = new Translation<Squad>("<#a7a8a5>You left {0} squad.", Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadDisbanded        = new Translation<Squad>("<#a7a8a5>{0} squad was disbanded.", Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadLockedSquad             = new Translation("<#a7a8a5>You <#6be888>locked</color> your squad.");
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadUnlockedSquad           = new Translation("<#999e90>You <#fff>unlocked</color> your squad.");
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadPromoted         = new Translation<Squad>("<#999e90>You're now the <#cedcde>squad leader</color> of {0}.", Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadKicked           = new Translation<Squad>("<#ae8f8f>You were kicked from {0} squad.", Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<string> SquadNotFound        = new Translation<string>("<#ae8f8f>Failed to find a squad called \"<#c$neutral$>{0}</color>\". You can also use the first letter of the squad name.");
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadAlreadyInSquad          = new Translation("<#ae8f8f>You're already in a squad.");
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadNotInSquad              = new Translation("<#ae8f8f>You're not in a squad yet. Use <#ae8f8f>/squad join <squad></color> to join a squad.");
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadNotSquadLeader          = new Translation("<#ae8f8f>You're not the leader of your squad.");
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadLocked           = new Translation<Squad>("<#a89791>{0} is locked.", Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<Squad> SquadFull             = new Translation<Squad>("<#a89791>{0} is full.", Squad.FormatColorName);
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadTargetNotInSquad        = new Translation("<#a89791>That player isn't in a squad.");
    [TranslationData(SectionSquads)]
    public static readonly Translation<IPlayer> SquadPlayerJoined   = new Translation<IPlayer>("<#b9bdb3>{0} joined your squad.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<IPlayer> SquadPlayerLeft     = new Translation<IPlayer>("<#b9bdb3>{0} left your squad.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<IPlayer> SquadPlayerPromoted = new Translation<IPlayer>("<#b9bdb3>{0} was promoted to <#cedcde>squad leader</color>.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionSquads)]
    public static readonly Translation<IPlayer> SquadPlayerKicked   = new Translation<IPlayer>("<#b9bdb3>{0} was kicked from your squad.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadsDisabled               = new Translation("<#a89791>Squads are disabled in this gamemode.");
    [TranslationData(SectionSquads)]
    public static readonly Translation<int> SquadsTooMany           = new Translation<int>("<#a89791>There can not be more than {0} ${p:0:squad} on a team at once.");
    [TranslationData(SectionSquads)]
    public static readonly Translation<int> SquadsTooManyPlayerCount = new Translation<int>("<#a89791>There are too many squads right now. More squads are unlocked once your team reaches {0} ${p:0:member}.");
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadWarningNoMembers = new Translation("<#a89791>Your squad will be DISBANDED unless others join", TranslationFlags.TMProUI);
    [TranslationData(SectionSquads)]
    public static readonly Translation SquadWarningWrongKit = new Translation("<#a89791>You must request a SQUADLEADER kit", TranslationFlags.TMProUI);


    [TranslationData(SectionSquads, IsPrioritizedTranslation = false)]
    public static readonly Translation<Squad, int, int> SquadsUIHeaderPlayerCount    = new Translation<Squad, int, int>("<#bd6b5b>{0}</color> {1}/{2}", TranslationFlags.UnityUI, Squad.FormatName);
    [TranslationData(SectionSquads, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, int> SquadsUIPlayerCountList             = new Translation<int, int>("{0}/{1}", TranslationFlags.UnityUI);
    [TranslationData(SectionSquads, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, int, char> SquadsUIPlayerCountListLocked = new Translation<int, int, char>("{2} {0}/{1}", TranslationFlags.UnityUI);
    [TranslationData(SectionSquads, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, int> SquadsUIPlayerCountSmall            = new Translation<int, int>("{0}/{1}", TranslationFlags.UnityUI);
    [TranslationData(SectionSquads, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, int> SquadsUIPlayerCountSmallLocked      = new Translation<int, int>("<#969696>{0}/{1}</color>", TranslationFlags.UnityUI);
    [TranslationData(SectionSquads, IsPrioritizedTranslation = false)]
    public static readonly Translation SquadUIExpanded                               = new Translation("...", TranslationFlags.UnityUI);
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
    public static readonly Translation<Cooldown> RallyCooldown = new Translation<Cooldown>("<#959c8c>You can rally your squad again in: <#e3c27f>{0}</color>", Cooldown.FormatTimeLong);
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyObstructedPlace = new Translation("<#959c8c>This rally point is obstructed, find a more open place to put it.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyEnemiesNearby = new Translation("<#9e7a6c>Cannot place rally when there are enemies nearby.");
    [TranslationData(SectionRallies)]
    public static readonly Translation RallyEnemiesNearbyTp = new Translation("<#9e7a6c>There are enemies near your RALLY. Deployment is no longer possible.");
    [TranslationData(SectionRallies)]
    public static readonly Translation<int> RallyToast = new Translation<int>("<#959c8c><#c$rally$>RALLY</color> IN <#ffe4b5>{0}</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionRallies, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> RallyUI = new Translation<string>("<#c$rally$>RALLY</color> {0}", TranslationFlags.UnityUI);
    [TranslationData(SectionRallies, IsPrioritizedTranslation = false)]
    public static readonly Translation<TimeSpan, string> RallyUITimer = new Translation<TimeSpan, string>("<#c$rally$>RALLY</color> {0} {1}", TranslationFlags.UnityUI, "mm\\:ss");
    #endregion

    #region Time
    private const string SectionTime = "Time Strings";
    [TranslationData(SectionTime, "Permanent time, lasts forever.")]
    public static readonly Translation TimePermanent    = new Translation("permanent", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "1 second (singular).")]
    public static readonly Translation TimeSecondSingle = new Translation("second", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "X seconds (plural).")]
    public static readonly Translation TimeSecondPlural = new Translation("seconds", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "1 minute (singular).")]
    public static readonly Translation TimeMinuteSingle = new Translation("minute", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "X minutes (plural).")]
    public static readonly Translation TimeMinutePlural = new Translation("minutes", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "1 hour (singular).")]
    public static readonly Translation TimeHourSingle   = new Translation("hour", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "X hours (plural).")]
    public static readonly Translation TimeHourPlural   = new Translation("hours", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "1 day (singular).")]
    public static readonly Translation TimeDaySingle    = new Translation("day", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "X days (plural).")]
    public static readonly Translation TimeDayPlural    = new Translation("days", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "1 week (singular).")]
    public static readonly Translation TimeWeekSingle   = new Translation("week", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "X weeks (plural).")]
    public static readonly Translation TimeWeekPlural   = new Translation("weeks", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "1 month (singular).")]
    public static readonly Translation TimeMonthSingle  = new Translation("month", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "X months (plural).")]
    public static readonly Translation TimeMonthPlural  = new Translation("months", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "1 year (singular).")]
    public static readonly Translation TimeYearSingle   = new Translation("year", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "X years (plural).")]
    public static readonly Translation TimeYearPlural   = new Translation("years", TranslationFlags.UnityUINoReplace);
    [TranslationData(SectionTime, "Joining keyword (1 hour \"and\" 30 minutes).")]
    public static readonly Translation TimeAnd          = new Translation("and", TranslationFlags.UnityUINoReplace);
    #endregion

    #region FOBs and Buildables
    private const string SectionFOBs = "FOBs / Buildables";
    [TranslationData(SectionFOBs)]
    public static readonly Translation BuildNotInRadius        = new Translation("<#ffab87>This can only be placed inside <#cedcde>FOB RADIUS</color>.");
    [TranslationData(SectionFOBs)]
    public static readonly Translation BuildTickNotInRadius    = new Translation("<#ffab87>There's no longer a friendly FOB nearby.");
    [TranslationData(SectionFOBs)]
    public static readonly Translation<float> BuildSmallRadius = new Translation<float>("<#ffab87>This can only be placed within {0}m of this FOB Radio right now. Expand this range by building a <#cedcde>FOB BUNKER</color>.", "N0");
    [TranslationData(SectionFOBs)]
    public static readonly Translation BuildLegacyExplanation  = new Translation("<#ffab87>Hit the foundation with your Entrenching Tool to build it.");
    [TranslationData(SectionFOBs)]
    public static readonly Translation<float> BuildNoRadio     = new Translation<float>("<#ffab87>This can only be placed within {0}m of a friendly <#cedcde>FOB RADIO</color>.", "N0");
    [TranslationData(SectionFOBs)]
    public static readonly Translation<int, BuildableData> BuildLimitReached = new Translation<int, BuildableData>("<#ffab87>This FOB already has {0} {1}.", "F0", FormatPlural + "{0}");
    [TranslationData(SectionFOBs)]
    public static readonly Translation<int, BuildableData> RegionalBuildLimitReached = new Translation<int, BuildableData>("<#ffab87>You cannot place more than {0} {1} in this area.", "F0", FormatPlural + "{0}");
    [TranslationData(SectionFOBs)]
    public static readonly Translation<BuildableData> BuildTickStructureExists = new Translation<BuildableData>("<#ffab87>Too many {0} have already been built on this FOB.", FormatPlural);
    [TranslationData(SectionFOBs)]
    public static readonly Translation BuildEnemy              = new Translation("<#ffab87>You may not build on an enemy FOB.");
    [TranslationData(SectionFOBs)]
    public static readonly Translation<int, int> BuildMissingSupplies = new Translation<int, int>("<#ffab87>You're missing nearby build! <#c$build$>Building Supplies: <#e0d8b8>{0}/{1}</color></color>.");
    [TranslationData(SectionFOBs)]
    public static readonly Translation BuildMaxFOBsHit         = new Translation("<#ffab87>The max number of FOBs on your team has been reached.");
    [TranslationData(SectionFOBs)]
    public static readonly Translation BuildFOBUnderwater      = new Translation("<#ffab87>You can't build a FOB underwater.");
    [TranslationData(SectionFOBs)]
    public static readonly Translation<float> BuildFOBTooHigh  = new Translation<float>("<#ffab87>You can't build a FOB more than {0}m above the ground.", "F0");
    [TranslationData(SectionFOBs)]
    public static readonly Translation BuildFOBTooCloseToMain  = new Translation("<#ffab87>You can't build a FOB this close to main base.");
    [TranslationData(SectionFOBs)]
    public static readonly Translation BuildNoLogisticsVehicle = new Translation("<#ffab87>You must be near a friendly <#cedcde>LOGISTICS VEHICLE</color> to place a FOB radio.");
    [TranslationData(SectionFOBs)]
    public static readonly Translation<FOB, float, float> BuildFOBTooClose = new Translation<FOB, float, float>("<#ffa238>You are too close to an existing FOB Radio ({0}: {1}m away). You must be at least {2}m away to place a new radio.", FOB.FormatNameColored, "F0", "F0");
    [TranslationData(SectionFOBs)]
    public static readonly Translation<float, float> BuildBunkerTooClose = new Translation<float, float>("<#ffa238>You are too close to an existing FOB Bunker ({0}m away). You must be at least {1}m away to place a new radio.", "F0", "F0");
    [TranslationData(SectionFOBs, IsPrioritizedTranslation = false)]
    public static readonly Translation BuildInvalidAsset = new Translation("<#ffa238>This buildable has invalid barricade assets (contact devs).");
    [TranslationData(SectionFOBs)]
    public static readonly Translation BuildableNotAllowed = new Translation("<#ffa238>You are not allowed to place this buildable.");
    [TranslationData(SectionFOBs, IsPrioritizedTranslation = false)]
    public static readonly Translation<IDeployable, GridLocation, string> FOBUI    = new Translation<IDeployable, GridLocation, string>("{0}  <color=#d6d2c7>{1}</color>  {2}", TranslationFlags.UnityUI, FOB.FormatNameColored);
    [TranslationData(SectionFOBs)]
    public static readonly Translation CacheDestroyedAttack    = new Translation("<#e8d1a7>WEAPONS CACHE HAS BEEN ELIMINATED", TranslationFlags.TMProUI);
    [TranslationData(SectionFOBs)]
    public static readonly Translation CacheDestroyedDefense   = new Translation("<#deadad>WEAPONS CACHE HAS BEEN DESTROYED", TranslationFlags.TMProUI);
    [TranslationData(SectionFOBs)]
    public static readonly Translation<string> CacheDiscoveredAttack = new Translation<string>("<color=#e8d1a7>NEW WEAPONS CACHE DISCOVERED NEAR <color=#e3c59a>{0}</color></color>", TranslationFlags.TMProUI, FormatUppercase);
    [TranslationData(SectionFOBs)]
    public static readonly Translation CacheDiscoveredDefense  = new Translation("<#d9b9a7>WEAPONS CACHE HAS BEEN COMPROMISED, DEFEND IT", TranslationFlags.TMProUI);
    [TranslationData(SectionFOBs)]
    public static readonly Translation CacheSpawnedDefense     = new Translation("<#a8e0a4>NEW WEAPONS CACHE IS NOW ACTIVE", TranslationFlags.TMProUI);
    #endregion

    #region Deploy
    private const string SectionDeploy = "Deployment";
    [TranslationData(SectionDeploy)]
    public static readonly Translation<IDeployable> DeploySuccess           = new Translation<IDeployable>("<#fae69c>You have arrived at {0}.", FOB.FormatNameColored);
    [TranslationData(SectionDeploy)]
    public static readonly Translation<IDeployable, int> DeployStandby      = new Translation<IDeployable, int>("<#fae69c>Now deploying to {0}. You will arrive in <#eee>{1} ${p:1:second}</color>", FOB.FormatNameColored);
    [TranslationData(SectionDeploy)]
    public static readonly Translation<IDeployable> DeployNotSpawnableTick  = new Translation<IDeployable>("<#ffa238>{0} is no longer active.", FOB.FormatNameColored);
    [TranslationData(SectionDeploy)]
    public static readonly Translation<IDeployable> DeployNotSpawnable      = new Translation<IDeployable>("<#ffa238>{0} is not active.", FOB.FormatNameColored);
    [TranslationData(SectionDeploy)]
    public static readonly Translation<IDeployable> DeployDestroyed         = new Translation<IDeployable>("<#ffa238>{0} was destroyed.", FOB.FormatNameColored);
    [TranslationData(SectionDeploy)]
    public static readonly Translation<IDeployable> DeployNoBunker          = new Translation<IDeployable>("<#ffaa42>{0} doesn't have a <#cedcde>FOB BUNKER</color>. Your team must build one to use the <#cedcde>FOB</color> as a spawnpoint.", FOB.FormatNameColored);
    [TranslationData(SectionDeploy)]
    public static readonly Translation<IDeployable> DeployRadioDamaged      = new Translation<IDeployable>("<#ffaa42>The <#cedcde>FOB RADIO</color> at {0} is damaged. Repair it with an <#cedcde>ENTRENCHING TOOL</color>.", FOB.FormatNameColored);
    [TranslationData(SectionDeploy)]
    public static readonly Translation DeployMoved                          = new Translation("<#ffa238>You moved and can no longer deploy.");
    [TranslationData(SectionDeploy)]
    public static readonly Translation DeployDamaged                        = new Translation("<#ffa238>You were damaged and can no longer deploy.");
    [TranslationData(SectionDeploy)]
    public static readonly Translation<IDeployable> DeployEnemiesNearbyTick = new Translation<IDeployable>("<#ffa238>You no longer deploy to {0} - there are enemies nearby.", FOB.FormatNameColored);
    [TranslationData(SectionDeploy)]
    public static readonly Translation<IDeployable> DeployEnemiesNearby     = new Translation<IDeployable>("<#ffaa42>You cannot deploy to {0} - there are enemies nearby.");
    [TranslationData(SectionDeploy)]
    public static readonly Translation DeployCancelled                      = new Translation("<#fae69c>Active deployment cancelled.");
    [TranslationData(SectionDeploy)]
    public static readonly Translation<string> DeployableNotFound           = new Translation<string>("<#ffa238>There is no location by the name of <#e3c27f>{0}</color>.", FormatUppercase);
    [TranslationData(SectionDeploy)]
    public static readonly Translation<IDeployable> DeployableAlreadyOnFOB  = new Translation<IDeployable>("<#ffa238>You are already on <#e3c27f>{0}</color>.", FOB.FormatNameColored);
    [TranslationData(SectionDeploy)]
    public static readonly Translation DeployNotNearFOB                     = new Translation("<#ffa238>You must be near a friendly <#cedcde>FOB</color> or in <#cedcde>MAIN BASE</color> in order to deploy.");
    [TranslationData(SectionDeploy)]
    public static readonly Translation DeployNotNearFOBInsurgency           = new Translation("<#ffa238>You must be near a friendly <#cedcde>FOB</color> or <#e8d1a7>CACHE</color>, or in <#cedcde>MAIN BASE</color> in order to deploy.");
    [TranslationData(SectionDeploy)]
    public static readonly Translation<Cooldown> DeployCooldown             = new Translation<Cooldown>("<#ffa238>You can deploy again in: <#e3c27f>{0}</color>", Cooldown.FormatTimeLong);
    [TranslationData(SectionDeploy)]
    public static readonly Translation DeployAlreadyActive                  = new Translation("<#b5a591>You're already deploying somewhere.");
    [TranslationData(SectionDeploy)]
    public static readonly Translation<Cooldown> DeployInCombat             = new Translation<Cooldown>("<#ffaa42>You are in combat, soldier! You can deploy in another: <#e3987f>{0}</color>.", Cooldown.FormatTimeLong);
    [TranslationData(SectionDeploy)]
    public static readonly Translation DeployInjured                        = new Translation("<#ffaa42>You can not deploy while injured, get a medic to revive you or give up.");
    [TranslationData(SectionDeploy)]
    public static readonly Translation DeployLobbyRemoved                   = new Translation("<#fae69c>The lobby has been removed, use <#e3c27f>/teams</color> to switch teams instead.");
    #endregion

    #region Ammo
    private const string SectionAmmo = "Ammo";
    [TranslationData(SectionAmmo)]
    public static readonly Translation AmmoNoTarget                = new Translation("<#ffab87>Look at an <#cedcde>AMMO CRATE</color>, <#cedcde>AMMO BAG</color> or <#cedcde>VEHICLE</color> in order to resupply.");
    [TranslationData(SectionAmmo)]
    public static readonly Translation<int, int> AmmoResuppliedKit = new Translation<int, int>("<#d1bda7>Resupplied kit. Consumed: <#c$ammo$>{0} AMMO</color> <#948f8a>({1} left)</color>.");
    [TranslationData(SectionAmmo)]
    public static readonly Translation<int> AmmoResuppliedKitMain  = new Translation<int>("<#d1bda7>Resupplied kit. Consumed: <#c$ammo$>{0} AMMO</color>.");
    [TranslationData(SectionAmmo)]
    public static readonly Translation AmmoAutoSupply              = new Translation("<#b3a6a2>This vehicle will <#cedcde>AUTO RESUPPLY</color> when in main. You can also use '<color=#c9bfad>/load <color=#c$build$>build</color>|<color=#c$ammo$>ammo</color> <amount></color>'.");
    [TranslationData(SectionAmmo)]
    public static readonly Translation AmmoNotNearFOB              = new Translation("<#b3a6a2>This ammo crate is not built on a friendly FOB.");
    [TranslationData(SectionAmmo)]
    public static readonly Translation<int, int> AmmoOutOfStock    = new Translation<int, int>("<#b3a6a2>Insufficient ammo. Required: <#c$ammo$>{0}/{1} AMMO</color>.");
    [TranslationData(SectionAmmo)]
    public static readonly Translation AmmoNoKit                   = new Translation("<#b3a6a2>You don't have a kit yet. Go request one from the armory in your team's headquarters.");
    [TranslationData(SectionAmmo)]
    public static readonly Translation AmmoWrongTeam = new Translation("<#b3a6a2>You cannot rearm with enemy ammunition.");
    [TranslationData(SectionAmmo)]
    public static readonly Translation<Cooldown> AmmoCooldown      = new Translation<Cooldown>("<#b7bab1>More <#cedcde>AMMO</color> arriving in: <color=#de95a8>{0}</color>", Cooldown.FormatTimeShort);
    [TranslationData(SectionAmmo)]
    public static readonly Translation AmmoNotRifleman             = new Translation("<#b7bab1>You must be a <#cedcde>RIFLEMAN</color> in order to place this <#cedcde>AMMO BAG</color>.");
    [TranslationData(SectionAmmo)]
    public static readonly Translation AmmoNotNearRepairStation    = new Translation("<#b3a6a2>Your vehicle must be next to a <#cedcde>REPAIR STATION</color> in order to rearm.");
    [TranslationData(SectionAmmo)]
    public static readonly Translation<VehicleData, int, int> AmmoResuppliedVehicle = new Translation<VehicleData, int, int>("<#d1bda7>Resupplied {0}. Consumed: <#c$ammo$>{1} AMMO</color> <#948f8a>({2} left)</color>.", VehicleData.COLORED_NAME);
    [TranslationData(SectionAmmo)]
    public static readonly Translation<VehicleData, int> AmmoResuppliedVehicleMain  = new Translation<VehicleData, int>("<#d1bda7>Resupplied {0}. Consumed: <#c$ammo$>{1} AMMO</color>.", VehicleData.COLORED_NAME);
    [TranslationData(SectionAmmo)]
    public static readonly Translation AmmoVehicleCantRearm             = new Translation("<#d1bda7>You cannot ressuply this vehicle.");
    [TranslationData(SectionAmmo)]
    public static readonly Translation AmmoInVehicle                    = new Translation("<#d1bda7>You cannot ressuply this vehicle while flying, try exiting the vehicle.");
    [TranslationData(SectionAmmo)]
    public static readonly Translation<VehicleData> AmmoVehicleFullAlready          = new Translation<VehicleData>("<#b3a6a2>Your {0} does not need to be resupplied.", VehicleData.COLORED_NAME);
    [TranslationData(SectionAmmo)]
    public static readonly Translation<VehicleData> AmmoVehicleNotNearRepairStation = new Translation<VehicleData>("<#b3a6a2>Your {0} must be next to a <color=#e3d5ba>REPAIR STATION</color> in order to rearm.", VehicleData.COLORED_NAME);
    #endregion

    #region Popup
    private const string SectionPopup = "Popup";
    [TranslationData(SectionPopup)]
    public static readonly Translation ButtonOK = new Translation("OK", TranslationFlags.TMProUI);
    [TranslationData(SectionPopup)]
    public static readonly Translation ButtonCancel = new Translation("Cancel", TranslationFlags.TMProUI);
    [TranslationData(SectionPopup)]
    public static readonly Translation ButtonYes = new Translation("Yes", TranslationFlags.TMProUI);
    [TranslationData(SectionPopup)]
    public static readonly Translation ButtonNo = new Translation("No", TranslationFlags.TMProUI);
    #endregion

    #region Load Command
    private const string SectionLoad = "Load Supplies";
    [TranslationData(SectionLoad)]
    public static readonly Translation LoadNoTarget = new Translation("<#b3a6a2>Look at a friendly <#cedcde>LOGISTICS VEHICLE</color>.");
    [TranslationData(SectionLoad)]
    public static readonly Translation LoadUsage = new Translation("<#b3a6a2>Try typing: '<#e6d1b3>/load ammo <amount|'half'></color>' or '<#e6d1b3>/load build <amount|'half'></color>'.");
    [TranslationData(SectionLoad)]
    public static readonly Translation<string> LoadInvalidAmount = new Translation<string>("<#b3a6a2>'{0}' is not a valid amount of supplies.", FormatUppercase);
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

    #region Signs
    private const string SectionSigns = "Signs";
        [TranslationData(Section = SectionSigns, SignId = "rules", Description = "Server rules")]
    public static readonly Translation SignRules = new Translation("Rules\nNo suicide vehicles.\netc.", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "kitdelay", Description = "Shown on new seasons when elite kits and loadouts are locked.")]
    public static readonly Translation SignKitDelay = new Translation("<#e6e6e6>All <#3bede1>Elite Kits</color> and <#32a852>Loadouts</color> are locked for the two weeks of the season.\nThey will be available again after <#d8addb>April 2nd, 2023</color>.", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_squadleader")]
    public static readonly Translation SignClassDescriptionSquadleader   = new Translation("\n\n<#cecece>Help your squad by supplying them with <#f0a31c>rally points</color> and placing <#f0a31c>FOB radios</color>.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_rifleman")]
    public static readonly Translation SignClassDescriptionRifleman      = new Translation("\n\n<#cecece>Resupply your teammates in the field with an <#f0a31c>Ammo Bag</color>.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_medic")]
    public static readonly Translation SignClassDescriptionMedic         = new Translation("\n\n<#cecece><#f0a31c>Revive</color> your teammates after they've been injured.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_breacher")]
    public static readonly Translation SignClassDescriptionBreacher      = new Translation("\n\n<#cecece>Use <#f0a31c>high-powered explosives</color> to take out <#f01f1c>enemy FOBs</color>.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_autorifleman")]
    public static readonly Translation SignClassDescriptionAutoRifleman  = new Translation("\n\n<#cecece>Equipped with a high-capacity and powerful <#f0a31c>LMG</color> to spray-and-pray your enemies.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_machinegunner")]
    public static readonly Translation SignClassDescriptionMachineGunner = new Translation("\n\n<#cecece>Equipped with a powerful <#f0a31c>Machine Gun</color> to shred the enemy team in combat.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_lat")]
    public static readonly Translation SignClassDescriptionLAT           = new Translation("\n\n<#cecece>A balance between an anti-tank and combat loadout, used to conveniently destroy <#f01f1c>armored enemy vehicles</color>.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_hat")]
    public static readonly Translation SignClassDescriptionHAT           = new Translation("\n\n<#cecece>Equipped with multiple powerful <#f0a31c>anti-tank shells</color> to take out any vehicles.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_grenadier")]
    public static readonly Translation SignClassDescriptionGrenadier     = new Translation("\n\n<#cecece>Equipped with a <#f0a31c>grenade launcher</color> to take out enemies behind cover or in light-armored vehicles.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_marksman")]
    public static readonly Translation SignClassDescriptionMarksman      = new Translation("\n\n<#cecece>Equipped with a <#f0a31c>marksman rifle</color> to take out enemies from medium to high distances.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_sniper")]
    public static readonly Translation SignClassDescriptionSniper        = new Translation("\n\n<#cecece>Equipped with a high-powered <#f0a31c>sniper rifle</color> to take out enemies from great distances.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_aprifleman")]
    public static readonly Translation SignClassDescriptionAPRifleman    = new Translation("\n\n<#cecece>Equipped with <#f0a31c>explosive traps</color> to cover entry-points and entrap enemy vehicles.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_engineer")]
    public static readonly Translation SignClassDescriptionEngineer      = new Translation("\n\n<#cecece>Features 200% <#f0a31c>build speed</color> and are equipped with <#f0a31c>fortifications</color> and traps to help defend their team's FOBs.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_crewman")]
    public static readonly Translation SignClassDescriptionCrewman       = new Translation("\n\n<#cecece>The only kits than can man <#f0a31c>armored vehicles</color>.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_pilot")]
    public static readonly Translation SignClassDescriptionPilot         = new Translation("\n\n<#cecece>The only kits that can fly <#f0a31c>aircraft</color>.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "class_desc_specops")]
    public static readonly Translation SignClassDescriptionSpecOps       = new Translation("\n\n<#cecece>Equipped with <#f0a31c>night-vision</color> to help see at night.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "bundle_misc")]
    public static readonly Translation SignBundleMisc       = new Translation("<#fff>Misc.", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "bundle_caf")]
    public static readonly Translation SignBundleCanada     = new Translation("<#fff>Canadian Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "bundle_fr")]
    public static readonly Translation SignBundleFrance     = new Translation("<#fff>French Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "bundle_ger")]
    public static readonly Translation SignBundleGermany    = new Translation("<#fff>German Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "bundle_usmc")]
    public static readonly Translation SignBundleUSMC       = new Translation("<#fff>USMC Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "bundle_usa")]
    public static readonly Translation SignBundleUSA        = new Translation("<#fff>USA Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "bundle_pl")]
    public static readonly Translation SignBundlePoland     = new Translation("<#fff>Polish Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "bundle_idf")]
    public static readonly Translation SignBundleIsrael     = new Translation("<#fff>IDF Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "bundle_militia")]
    public static readonly Translation SignBundleMilitia    = new Translation("<#fff>Militia Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "bundle_ru")]
    public static readonly Translation SignBundleRussia     = new Translation("<#fff>Russia Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "bundle_soviet")]
    public static readonly Translation SignBundleSoviet     = new Translation("<#fff>Soviet Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "bundle_special")]
    public static readonly Translation SignBundleSpecial    = new Translation("<#fff>Special Kits", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "loadout_info", Description = "Information on how to obtain a loadout.")]
    public static readonly Translation SignLoadoutInfo      = new Translation("<#cecece>Loadouts and elite kits can be purchased\nin our <#7483c4>Discord</color> server.</color>\n\n<#7483c4>/discord</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "air_solo_warning", Description = "Soloing warning positioned near attack heli and jet.")]
    public static readonly Translation SignAirSoloingWarning = new Translation("<color=#f01f1c><b>Do not exit main without another <#cedcde>PILOT</color> for the Jet or Attack Heli\n\n\n<color=#ff6600>YOU WILL BE BANNED FOR 6 DAYS WITHOUT WARNING!<b></color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "armor_solo_warning", Description = "Soloing warning positioned near armor requests.")]
    public static readonly Translation SignArmorSoloingWarning = new Translation("<color=#f01f1c><b>Do not exit main without another <#cedcde>CREWMAN</color> while driving any vehicles that require a <#cedcde>CREWMAN</color> kit!\n\n\n<color=#ff6600>YOU WILL BE BANNED FOR 6 DAYS WITHOUT WARNING!<b></color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "waiting_warning", Description = "Warning about waiting for vehicles while the server is full.")]
    public static readonly Translation SignWaitingWarning = new Translation("<color=#f01f1c>Waiting for vehicles to spawn when the server is full for more than 2 minutes will result in a KICK or BAN</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "waiting_notice_1", Description = "Change notice about waiting for vehicles while the server is full (part 1).")]
    public static readonly Translation SignWaitingNoticePart1 = new Translation("<color=yellow>Warning:</color>\n<color=white>Due to players sitting at base waiting for air assets, we've decided that if the server capacity is full and air assets aren't available for</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "waiting_notice_2", Description = "Change notice about waiting for vehicles while the server is full (part 2).")]
    public static readonly Translation SignWaitingNoticePart2 = new Translation("<color=white>a considerable amount of time, we reserve the right to warn you not to. If you continue to sit around we will kick you to allow another player in the queue to play.</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "solo_notice_1", Description = "Notice about soloing (part 1).")]
    public static readonly Translation SignSoloNoticePart1 = new Translation("<color=#f01f1c>You are not allowed to take out the following vehicles without a passenger:</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "solo_notice_2_t1", Description = "Notice about soloing (part 2, team 1).")]
    public static readonly Translation SignSoloNoticePart2T1 = new Translation("<#f01f1c>- Abrams\n- LAV\n- Stryker\n- Attack Heli\n- Fighter Jet\n</color>\n<#ec8100>You will get banned for <#ff6600>3 days</color> if you do!</color>\n<#f01f1c>If your passenger leaves, return to base (RTB).</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "solo_notice_2_t2", Description = "Notice about soloing (part 2, team 2).")]
    public static readonly Translation SignSoloNoticePart2T2 = new Translation("<#f01f1c>- T-90\n- BTR-82A\n- BDRM\n- BMP-2\n- Attack Heli\n- Fighter Jet\n</color>\n<#ec8100>You will get banned for <#ff6600>3 days</color> if you do!</color>\n<#f01f1c>If your passenger leaves, return to base (RTB).</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "tutorial_arrow", Description = "Points to the tutorial with a caption.")]
    public static readonly Translation SignTutorialArrow = new Translation("<#2df332>Small tutorial this way!\n<#ff6600><b><---</b>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "tutorial_get_kit_1", Description = "Tells the player about kits and how to request them (part 1).")]
    public static readonly Translation SignTutorialGetKitPart1 = new Translation("<#ff6600><b>How do I get a kit?</b>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "tutorial_get_kit_2", Description = "Tells the player about kits and how to request them (part 2).")]
    public static readonly Translation SignTutorialGetKitPart2 = new Translation("<#cEcEcE>Look at kit sign and type <#2df332>/req</color> in chat to recieve the kit.", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "tutorial_get_kit_3", Description = "Tells the player about kits and how to request them (part 3).")]
    public static readonly Translation SignTutorialGetKitPart3 = new Translation("<#cEcEcE>Some kits are unlocked using <#c$credits$>credits</color>. Look at the sign and do <#2df332>/buy</color> to unlock the kit.", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "tutorial_get_vehicle_1", Description = "Tells the player about vehicles and how to request them (part 1).")]
    public static readonly Translation SignTutorialGetVehiclePart1 = new Translation("<#ff6600><b>How do I get a vehicle?</b>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "tutorial_get_vehicle_2", Description = "Tells the player about vehicles and how to request them (part 2).")]
    public static readonly Translation SignTutorialGetVehiclePart2 = new Translation("<#cEcEcE>Look at the vehicle you'd like to request and type in chat <#2df332>/req</color> to unlock the vehicle.", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "tutorial_get_vehicle_3", Description = "Tells the player about vehicles and how to request them (part 3).")]
    public static readonly Translation SignTutorialGetVehiclePart3 = new Translation("<#cEcEcE>Some vehicles require a special kit. Request a <#cedcde>CREWMAN</color> or <#cedcde>PILOT</color> kit to gain acces to them!", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "tutorial_faq_header", Description = "Header for the FAQ (frequently asked questions) section of the tutorial.")]
    public static readonly Translation SignTutorialFAQHeader = new Translation("<#ff6600><b>FAQ</b>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "tutorial_faq_give_up_q", Description = "(question) This FAQ explains how to Give Up after being injured.")]
    public static readonly Translation SignTutorialFAQGiveUpQ = new Translation("<#2df332>Q: Help! I can't reset when downed!", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "tutorial_faq_give_up_a", Description = "(answer) This FAQ explains how to Give Up after being injured.")]
    public static readonly Translation SignTutorialFAQGiveUpA = new Translation("<#cEcEcE>A: Press the '/' button on your keyboard to give up when injured. If this doesn't work, Head to your <#2df332>keybind settings</color> and set <#f32d2d>Code Hotkey #3</color> to your preference!", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "discord_link", Description = "Has the discord link.")]
    public static readonly Translation SignDiscordLink = new Translation("<color=#CECECE>Need help? Join our <color=#7483c4>Discord</color> server!\n<#6796ce>discord.gg/" + UCWarfare.Config.DiscordInviteCode + "</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "saddam_hussein", Description = "Saddam Hussein.", IsPrioritizedTranslation = false)]
    public static readonly Translation SignSaddamHussein = new Translation("<color=red>Saddam Hussein\n ▇▅▆▇▆▅▅█</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SectionSigns, SignId = "elite_kit_pointer", Description = "Points to the building with elite kits.")]
    public static readonly Translation SignEliteKitPointer = new Translation("<color=#f0a31c>Elite kits found in this building     --></color>", TranslationFlags.TMProSign);


    [TranslationData(Section = SectionSigns, SignId = "faq1_1")]
    public static readonly Translation SignFAQ1Part1 = new Translation("""
                                                                       <#fa6122>Giving up or Flaring
                                                                       """, TranslationFlags.TMProSign);
    [TranslationData(Section = SectionSigns, SignId = "faq1_2")]
    public static readonly Translation SignFAQ1Part2 = new Translation("""
                                                                       <#2df332>Q: Help! I can't give up when I'm downed or flare in a air vehicle </color>
                                                                       """, TranslationFlags.TMProSign);
    [TranslationData(Section = SectionSigns, SignId = "faq1_3")]
    public static readonly Translation SignFAQ1Part3 = new Translation("""
                                                                        <#FFFFFF>A: Press <#2df332>/</color> on your keyboard to give up when downed or flare. If this doesn't work head to</color>
                                                                        <#f01f1c>Controls -> Mods/Plugins -> Code Hotkey#3
                                                                        """, TranslationFlags.TMProSign);

    [TranslationData(Section = SectionSigns, SignId = "faq2_1")]
    public static readonly Translation SignFAQ2Part1 = new Translation("""
                                                                       <#fa6122>Squad Commands
                                                                       """, TranslationFlags.TMProSign);
    [TranslationData(Section = SectionSigns, SignId = "faq2_2")]
    public static readonly Translation SignFAQ2Part2 = new Translation("""
                                                                       <#fa6122>To Join a Squad
                                                                       <#FFFFFF>/Squad join <#2df332>(Name)</color>
                                                                       <#fa6122>To Create a Squad
                                                                       <#FFFFFF>/Squad Create
                                                                       """, TranslationFlags.TMProSign);
    [TranslationData(Section = SectionSigns, SignId = "faq2_3")]
    public static readonly Translation SignFAQ2Part3 = new Translation("""
                                                                       <#fa6122>Other Squad Commands
                                                                       <#FFFFFF>/squad leave
                                                                       /squad disband
                                                                       /squad promote <#2df332>(player)</color>
                                                                       /squad kick <#2df332>(player)
                                                                       </color>/squad lock / unlock
                                                                       """, TranslationFlags.TMProSign);

    [TranslationData(Section = SectionSigns, SignId = "faq3_1")]
    public static readonly Translation SignFAQ3Part1 = new Translation("""
                                                                       <#fa6122>How do I get a kit?
                                                                       """, TranslationFlags.TMProSign);
    [TranslationData(Section = SectionSigns, SignId = "faq3_2")]
    public static readonly Translation SignFAQ3Part2 = new Translation("""
                                                                       <#FFFFFF>If you are new to the server look at a kit sign that says free, and type <#2df332>/request</color> to receive it
                                                                       """, TranslationFlags.TMProSign);
    [TranslationData(Section = SectionSigns, SignId = "faq3_3")]
    public static readonly Translation SignFAQ3Part3 = new Translation("""
                                                                       <#FFFFFF>Some kits are not free and will need to be purchased with in-game credits, do <#2df332>/buy</color> on the kit sign to purchase it
                                                                       """, TranslationFlags.TMProSign);

    [TranslationData(Section = SectionSigns, SignId = "faq4_1")]
    public static readonly Translation SignFAQ4Part1 = new Translation("""
                                                                       <#fa6122>How do I deploy to the battlefield?
                                                                       """, TranslationFlags.TMProSign);
    [TranslationData(Section = SectionSigns, SignId = "faq4_2")]
    public static readonly Translation SignFAQ4Part2 = new Translation("""
                                                                       <#FFFFFF>On the left side of your screen, you can see the FOBs that belong to your team. Each fob displays a different number <#6698FF>(FOB#)</color>, and location.
                                                                       """, TranslationFlags.TMProSign);
    [TranslationData(Section = SectionSigns, SignId = "faq4_3")]
    public static readonly Translation SignFAQ4Part3 = new Translation("""
                                                                       <#FFFFFF>To deploy you have to type: /dep fob(number) <#2df332>e.g. /deploy fob1</color> Check its location before deploying.
                                                                       """, TranslationFlags.TMProSign);

    [TranslationData(Section = SectionSigns, SignId = "faq5_1")]
    public static readonly Translation SignFAQ5Part1 = new Translation("""
                                                                       <#fa6122>How do I get a vehicle?
                                                                       """, TranslationFlags.TMProSign);
    [TranslationData(Section = SectionSigns, SignId = "faq5_2")]
    public static readonly Translation SignFAQ5Part2 = new Translation("""
                                                                       <#FFFFFF>Look at a vehicle you would like to request, and type <#2df332>/request</color> to unlock the vehicle
                                                                       """, TranslationFlags.TMProSign);
    [TranslationData(Section = SectionSigns, SignId = "faq5_3")]
    public static readonly Translation SignFAQ5Part3 = new Translation("""
                                                                       <#FFFFFF>Some Vehicles require a special kit: Like Helicopters APCs, IFVs, and MBTs. You'll need to request and/or buy, <#2df332>the crewman or pilot kit to gain access to them
                                                                       """, TranslationFlags.TMProSign);
    #endregion

    #region Announcements
    private const string SectionAnnouncements = "Announcements";
    [TranslationData(Section = SectionAnnouncements, Description = "Announcement telling people to join the discord by typing /discord.", IsAnnounced = true)]
    public static readonly Translation AnnouncementDiscord = new Translation("<#b3b3b3>Have you joined our <#7483c4>Discord</color> server yet? Type <#7483c4>/discord</color> to join.");
    [TranslationData(Section = SectionAnnouncements, Description = "Announcement telling people how to return to base from FOBs.", IsAnnounced = true)]
    public static readonly Translation AnnouncementDeployMain = new Translation("<#c2b7a5>You can deploy back to main by doing <#ffffff>/deploy main</color> while near a friendly FOB.");
    [TranslationData(Section = SectionAnnouncements, Description = "Announcement telling people the best ways to earn XP.", IsAnnounced = true)]
    public static readonly Translation AnnouncementRankUp = new Translation("<#92a692>Capture <#ffffff>flags</color> and build <#ffffff>FOBs</color> to rank up and earn respect amongst your team.");
    [TranslationData(Section = SectionAnnouncements, Description = "Announcement telling people not to waste assets.", IsAnnounced = true)]
    public static readonly Translation AnnouncementDontWasteAssets = new Translation("<#c79675>Do not waste vehicles, ammo, build, or other assets! You may risk punishment if you're reported or caught.");
    [TranslationData(Section = SectionAnnouncements, Description = "Announcement telling people to communicate and listen to higher-ups.", IsAnnounced = true)]
    public static readonly Translation AnnouncementListenToSuperiors = new Translation("<#a2a7ba>Winning requires coordination and teamwork. Listen to your superior officers, and communicate!");
    [TranslationData(Section = SectionAnnouncements, Description = "Announcement telling people to build FOBs to help their team.", IsAnnounced = true)]
    public static readonly Translation AnnouncementBuildFOBs = new Translation("<#9da6a6>Building <color=#54e3ff>FOBs</color> is vital for advancing operations. Grab a logistics truck and go build one!");
    [TranslationData(Section = SectionAnnouncements, Description = "Announcement telling people to join or create a squad.", IsAnnounced = true)]
    public static readonly Translation AnnouncementSquads = new Translation("<#c2b7a5>Join a squad with <#ffffff>/squad join</color> or create one with <#ffffff>/squad create</color> to earn extra XP among other benefits.");
    [TranslationData(Section = SectionAnnouncements, Description = "Announcement telling people about the different way our chat works.", IsAnnounced = true)]
    public static readonly Translation AnnouncementChatChanges = new Translation("<#a2a7ba>Use area chat while in a squad to communicate with only them or group chat to communicate with your entire <#54e3ff>team</color>.");
    [TranslationData(Section = SectionAnnouncements, Description = "Announcement telling people about the abandon command.", IsAnnounced = true)]
    public static readonly Translation AnnouncementAbandon = new Translation("<#b3b3b3>Done with your vehicle? Type <#ffffff>/abandon</color> while in main base to get some credits back and free up the vehicle for your team.");
    [TranslationData(Section = SectionAnnouncements, Description = "Announcement telling people about soloing.", IsAnnounced = true)]
    public static readonly Translation AnnouncementSoloing = new Translation("<#c79675>Soloing armor vehicles, attack helis, and jets is against the rules. Make sure you have a passenger for these vehicles.");
    [TranslationData(Section = SectionAnnouncements, Description = "Announcement telling people about reporting with /report.", IsAnnounced = true)]
    public static readonly Translation AnnouncementReport = new Translation("<#c2b7a5>See someone breaking rules? Use the <#ffffff>/report</color> command to help admins see context about the report.</color>");
    #endregion

    #region Kick Command
    private const string SectionKick = "Kick";
    [TranslationData(SectionKick, IsPrioritizedTranslation = false)]
    public static readonly Translation NoReasonProvided                       = new Translation("<#9cffb3>You must provide a reason.");
    [TranslationData(SectionKick, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer> KickSuccessFeedback           = new Translation<IPlayer>("<#00ffff>You kicked <#d8addb>{0}</color>.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionKick)]
    public static readonly Translation<IPlayer, IPlayer> KickSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#00ffff><#d8addb>{0}</color> was kicked by <#" + TeamManager.AdminColorHex + ">{1}</color>.", UCPlayer.FormatCharacterName, UCPlayer.FormatPlayerName);
    [TranslationData(SectionKick)]
    public static readonly Translation<IPlayer> KickSuccessBroadcastOperator  = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was kicked by an operator.", UCPlayer.FormatCharacterName);
    #endregion

    #region Ban Command
    private const string SectionBan = "Ban";
    [TranslationData(SectionBan, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer> BanPermanentSuccessFeedback           = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was <b>permanently</b> banned.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionBan)]
    public static readonly Translation<IPlayer, IPlayer> BanPermanentSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#00ffff><#d8addb>{0}</color> was <b>permanently</b> banned by <#" + TeamManager.AdminColorHex + ">{1}</color>.", UCPlayer.FormatCharacterName, UCPlayer.FormatPlayerName);
    [TranslationData(SectionBan)]
    public static readonly Translation<IPlayer> BanPermanentSuccessBroadcastOperator  = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was <b>permanently</b> banned by an operator.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionBan, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, string> BanSuccessFeedback            = new Translation<IPlayer, string>("<#00ffff><#d8addb>{0}</color> was banned for <#9cffb3>{1}</color>.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionBan)]
    public static readonly Translation<IPlayer, IPlayer, string> BanSuccessBroadcast  = new Translation<IPlayer, IPlayer, string>("<#00ffff><#d8addb>{0}</color> was banned for <#9cffb3>{2}</color> by <#" + TeamManager.AdminColorHex + ">{1}</color>.", UCPlayer.FormatCharacterName, UCPlayer.FormatPlayerName);
    [TranslationData(SectionBan)]
    public static readonly Translation<IPlayer, string> BanSuccessBroadcastOperator   = new Translation<IPlayer, string>("<#00ffff><#d8addb>{0}</color> was banned for <#9cffb3>{1}</color> by an operator.", UCPlayer.FormatCharacterName);
    #endregion

    #region Unban Command
    private const string SectionUnban = "Unban";
    [TranslationData(SectionUnban, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer> UnbanNotBanned = new Translation<IPlayer>("<#9cffb3><#d8addb>{0}</color> is not currently banned.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionUnban, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer> UnbanSuccessFeedback = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was unbanned.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionUnban)]
    public static readonly Translation<IPlayer, IPlayer> UnbanSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#00ffff><#d8addb>{0}</color> was unbanned by <#" + TeamManager.AdminColorHex + ">{1}</color>.", UCPlayer.FormatCharacterName, UCPlayer.FormatPlayerName);
    [TranslationData(SectionUnban)]
    public static readonly Translation<IPlayer> UnbanSuccessBroadcastOperator = new Translation<IPlayer>("<#ffff00><#d8addb>{0}</color> was unbanned by an operator.", UCPlayer.FormatCharacterName);
    #endregion

    #region Warn Command
    private const string SectionWarn = "Warn";
    [TranslationData(SectionWarn, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer> WarnSuccessFeedback           = new Translation<IPlayer>("<#ffff00>You warned <#d8addb>{0}</color>.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionWarn)]
    public static readonly Translation<IPlayer, IPlayer> WarnSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#ffff00><#d8addb>{0}</color> was warned by <#" + TeamManager.AdminColorHex + ">{1}</color>.", UCPlayer.FormatCharacterName, UCPlayer.FormatPlayerName);
    [TranslationData(SectionWarn)]
    public static readonly Translation<IPlayer> WarnSuccessBroadcastOperator  = new Translation<IPlayer>("<#ffff00><#d8addb>{0}</color> was warned by an operator.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionWarn)]
    public static readonly Translation<IPlayer, string> WarnSuccessDM         = new Translation<IPlayer, string>("<color=#ffff00><color=#" + TeamManager.AdminColorHex + ">{0}</color> warned you for <color=#ffffff>{1}</color>.</color>", TranslationFlags.TMProUI, UCPlayer.FormatPlayerName);
    [TranslationData(SectionWarn)]
    public static readonly Translation<string> WarnSuccessDMOperator          = new Translation<string>("<color=#ffff00>An operator warned you for <color=#ffffff>{0}</color>.</color>", TranslationFlags.TMProUI, UCPlayer.FormatPlayerName);
    [TranslationData(SectionWarn)]
    public static readonly Translation WarnSuccessTitle = new Translation("<color=#ffff00>Warning", TranslationFlags.TMProUI);
    #endregion

    #region Mute Command
    private const string SectionMute = "Mute";
    [TranslationData(SectionMute, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, IPlayer, MuteType> MutePermanentSuccessFeedback = new Translation<IPlayer, IPlayer, MuteType>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <b>permanently</b> <#cedcde>{2}</color> muted.", UCPlayer.FormatCharacterName, UCPlayer.FormatSteam64, FormatLowercase);
    [TranslationData(SectionMute, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, IPlayer, string, MuteType> MuteSuccessFeedback  = new Translation<IPlayer, IPlayer, string, MuteType>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <#cedcde>{3}</color> muted for <#9cffb3>{2}</color>.", UCPlayer.FormatCharacterName, UCPlayer.FormatSteam64, arg3Fmt: FormatLowercase);
    [TranslationData(SectionMute)]
    public static readonly Translation<IPlayer, IPlayer, MuteType> MutePermanentSuccessBroadcastOperator  = new Translation<IPlayer, IPlayer, MuteType>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <b>permanently</b> <#cedcde>{2}</color> muted by an operator.", UCPlayer.FormatCharacterName, UCPlayer.FormatSteam64, FormatLowercase);
    [TranslationData(SectionMute)]
    public static readonly Translation<IPlayer, IPlayer, MuteType, IPlayer> MutePermanentSuccessBroadcast = new Translation<IPlayer, IPlayer, MuteType, IPlayer>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <b>permanently</b> <#cedcde>{2}</color> muted by <#" + TeamManager.AdminColorHex + ">{3}</color>.", UCPlayer.FormatCharacterName, UCPlayer.FormatSteam64, FormatLowercase, UCPlayer.FormatPlayerName);
    [TranslationData(SectionMute)]
    public static readonly Translation<IPlayer, IPlayer, string, MuteType> MuteSuccessBroadcastOperator   = new Translation<IPlayer, IPlayer, string, MuteType>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <#cedcde>{3}</color> muted by an operator for <#9cffb3>{2}</color>.", UCPlayer.FormatCharacterName, UCPlayer.FormatSteam64, arg3Fmt: FormatLowercase);
    [TranslationData(SectionMute)]
    public static readonly Translation<IPlayer, IPlayer, string, MuteType, IPlayer> MuteSuccessBroadcast  = new Translation<IPlayer, IPlayer, string, MuteType, IPlayer>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <#cedcde>{3}</color> muted by <#" + TeamManager.AdminColorHex + ">{4}</color> for <#9cffb3>{2}</color>.", UCPlayer.FormatCharacterName, UCPlayer.FormatSteam64, arg3Fmt: FormatLowercase, arg4Fmt: UCPlayer.FormatPlayerName);
    [TranslationData(SectionMute)]
    public static readonly Translation<IPlayer, string, string, MuteType> MuteSuccessDM  = new Translation<IPlayer, string, string, MuteType>("<#ffff00><#" + TeamManager.AdminColorHex + ">{0}</color> <#9cffb3>{3}</color> muted you for <#9cffb3>{2}</color> because: <#9cffb3>{1}</color>.", UCPlayer.FormatPlayerName, arg3Fmt: FormatLowercase);
    [TranslationData(SectionMute)]
    public static readonly Translation<IPlayer, string, MuteType> MuteSuccessDMPermanent = new Translation<IPlayer, string, MuteType>("<#ffff00><#" + TeamManager.AdminColorHex + ">{0}</color> permanently <#9cffb3>{2}</color> muted you because: <#9cffb3>{1}</color>.", UCPlayer.FormatPlayerName, arg2Fmt: FormatLowercase);
    [TranslationData(SectionMute)]
    public static readonly Translation<string, string, MuteType> MuteSuccessDMOperator   = new Translation<string, string, MuteType>("<#ffff00>An operator <#9cffb3>{2}</color> muted you for <#9cffb3>{1}</color> because: <#9cffb3>{0}</color>.", arg2Fmt: FormatLowercase);
    [TranslationData(SectionMute)]
    public static readonly Translation<string, MuteType> MuteSuccessDMPermanentOperator  = new Translation<string, MuteType>("<#ffff00>>An operator permanently <#9cffb3>{1}</color> muted you because: <#9cffb3>{0}</color>.", arg1Fmt: FormatLowercase);

    [TranslationData(SectionMute)]
    public static readonly Translation<string> MuteTextChatFeedbackPermanent  = new Translation<string>("<#ffff00>You're permanently muted in text chat because: <#9cffb3>{0}</color>.");
    [TranslationData(SectionMute)]
    public static readonly Translation<DateTime, string> MuteTextChatFeedback = new Translation<DateTime, string>("<#ffff00>You're muted in text chat until <#cedcde>{0}</color> because <#9cffb3>{1}</color>.", "r");
    #endregion

    #region Unmute Command
    private const string SectionUnmute = "Unmute";
    [TranslationData(SectionUnmute, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer> UnmuteNotMuted                  = new Translation<IPlayer>("<#9cffb3><#d8addb>{0}</color> is not currently muted.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionUnmute, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer> UnmuteSuccessFeedback           = new Translation<IPlayer>("<#ffff00><#d8addb>{0}</color> was unmuted.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionUnmute)]
    public static readonly Translation<IPlayer, IPlayer> UnmuteSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#ffff00><#d8addb>{0}</color> was unmuted by <#" + TeamManager.AdminColorHex + ">{1}</color>.", UCPlayer.FormatCharacterName, UCPlayer.FormatPlayerName);
    [TranslationData(SectionUnmute)]
    public static readonly Translation<IPlayer> UnmuteSuccessBroadcastOperator  = new Translation<IPlayer>("<#ffff00><#d8addb>{0}</color> was unmuted by an operator.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionUnmute)]
    public static readonly Translation<IPlayer> UnmuteSuccessDM                 = new Translation<IPlayer>("<#ffff00><#" + TeamManager.AdminColorHex + ">{0}</color> unmuted you.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionUnmute)]
    public static readonly Translation UnmuteSuccessDMOperator                  = new Translation("<#ffff00>An operator unmuted you.");
    #endregion

    #region IPWhitelist
    private const string SectionIPWhitelist = "IP Whitelist";
    [TranslationData(SectionIPWhitelist, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, IPv4Range> IPWhitelistSuccess = new Translation<IPlayer, IPv4Range>("<#00ffff>Whitelisted the IP range: <#9cffb3>{1}</color> for {0}.");
    [TranslationData(SectionIPWhitelist, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, IPv4Range> IPUnwhitelistSuccess = new Translation<IPlayer, IPv4Range>("<#00ffff>Unwhitelisted the IP range: <#9cffb3>{1}</color> for {0}.");
    [TranslationData(SectionIPWhitelist, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, IPv4Range> IPWhitelistNotFound = new Translation<IPlayer, IPv4Range>("<#b3a6a2>The IP range: <#9cffb3>{1}</color> is not whitelisted for {0}.");
    #endregion

    #region Duty Command
    private const string SectionDuty = "Duty Command";
    [TranslationData(SectionDuty, IsPrioritizedTranslation = false)]
    public static readonly Translation DutyOnFeedback            = new Translation("<#c6d4b8>You are now <#95ff4a>on duty</color>.");
    [TranslationData(SectionDuty, IsPrioritizedTranslation = false)]
    public static readonly Translation DutyOffFeedback           = new Translation("<#c6d4b8>You are now <#ff8c4a>off duty</color>.");
    [TranslationData(SectionDuty)]
    public static readonly Translation<IPlayer> DutyOnBroadcast  = new Translation<IPlayer>("<#c6d4b8><#d9e882>{0}</color> is now <#95ff4a>on duty</color>.");
    [TranslationData(SectionDuty)]
    public static readonly Translation<IPlayer> DutyOffBroadcast = new Translation<IPlayer>("<#c6d4b8><#d9e882>{0}</color> is now <#ff8c4a>off duty</color>.");
    #endregion

    #region Speed Command
    private const string SectionSpeed = "Speed Command";
    [TranslationData(SectionSpeed, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> SpeedMultiplierInvalidValue = new Translation<string>("<#b3a6a2>Speed multiplier <#fff>{0}</color> is invalid.");
    [TranslationData(SectionSpeed, IsPrioritizedTranslation = false)]
    public static readonly Translation<float> SpeedMultiplierAlreadySet = new Translation<float>("<#b3a6a2>Speed multiplier is already set to <#fff>{0}</color>.");
    [TranslationData(SectionSpeed, IsPrioritizedTranslation = false)]
    public static readonly Translation<float, IPlayer> SetSpeedMultiplier = new Translation<float, IPlayer>("<#d1bda7>Set {0}'s speed multiplier to <#fff>{0}</color>.", "0.##", UCPlayer.FormatColoredCharacterName);
    #endregion

    #region Jump Command
    private const string SectionJump = "Speed Command";
    [TranslationData(SectionJump, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> JumpMultiplierInvalidValue = new Translation<string>("<#b3a6a2>Jump multiplier <#fff>{0}</color> is invalid.");
    [TranslationData(SectionJump, IsPrioritizedTranslation = false)]
    public static readonly Translation<float> JumpMultiplierAlreadySet = new Translation<float>("<#b3a6a2>Jump multiplier is already set to <#fff>{0}</color>.");
    [TranslationData(SectionJump, IsPrioritizedTranslation = false)]
    public static readonly Translation<float, IPlayer> SetJumpMultiplier = new Translation<float, IPlayer>("<#d1bda7>Set {0}'s speed multiplier to <#fff>{0}</color>.", "0.##", UCPlayer.FormatColoredCharacterName);
    #endregion

    #region Request
    private const string SectionRequest = "Request Command";
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestUpgradeNotInDiscordServer = new Translation("<#a4baa9>Join the <#7483c4>Discord</color> server (<#fff>/discord</color>) to open a ticket.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestUpgradeNotConnected = new Translation("<#a4baa9>The loadout upgrade system is not available right now, please try again later.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<Kit> RequestUpgradeAlreadyOpen = new Translation<Kit>("<#a4baa9>You already have a ticket open for <#ffebbd>{0}</color>.", Kit.DisplayNameFormat);
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestUpgradeTooManyTicketsOpen = new Translation("<#a4baa9>There are too many tickets open right now, please try again later.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<string> RequestUpgradeError = new Translation<string>("<#a4baa9>Error opening ticket: <#fff>{0}</color>.", FormatUppercase);
    [TranslationData(SectionRequest)]
    public static readonly Translation<Kit> DoesNotNeedUpgrade = new Translation<Kit>("<#a4baa9><#ffebbd>{0}</color> does not need to be upgraded.", Kit.DisplayNameFormat);
    [TranslationData(SectionRequest, IsPrioritizedTranslation = false)]
    public static readonly Translation<Kit> DoesNotNeedUnlock = new Translation<Kit>("<#a4baa9><#ffebbd>{0}</color> does not need to be unlocked.", Kit.DisplayNameFormat);
    [TranslationData(SectionRequest, IsPrioritizedTranslation = false)]
    public static readonly Translation<Kit> DoesNotNeedLock = new Translation<Kit>("<#a4baa9><#ffebbd>{0}</color> does not need to be locked.", Kit.DisplayNameFormat);
    [TranslationData(SectionRequest)]
    public static readonly Translation<Kit> RequestUpgradeOnKit = new Translation<Kit>("<#a4baa9><#ffebbd>{0}</color> can't be upgraded.", Kit.DisplayNameFormat);
    [TranslationData(SectionRequest)]
    public static readonly Translation<Kit> TicketOpened = new Translation<Kit>("<#a8918a>An upgrade request was opened in your name for <#ffebbd>{0}</color>. Please fill it out as soon as possible.", Kit.DisplayNameFormat);
    [TranslationData(SectionRequest)]
    public static readonly Translation<Class> RequestSignGiven = new Translation<Class>("<#a8918a>You have been allocated a <#cedcde>{0}</color> kit.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestNoTarget = new Translation("<#a4baa9>You must be looking at a request sign or vehicle.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestKitBought = new Translation<int>("<#c4a36a>Kit bought for <#c$credits$>C </color><#ffffff>{0}</color>. Request it with '<#b3b0ab>/request</color>'.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestKitNotRegistered = new Translation("<#a8918a>This kit has not been created yet.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestKitAlreadyOwned = new Translation("<#a8918a>You already have this kit.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestKitNeedsUpgrade = new Translation("<#a8918a>This kit needs to be upgraded. Use <#fff>/request upgrade</color> to start a ticket.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestKitNeedsSetup = new Translation("<#a8918a>This kit needs to be setup by an admin. Check your upgrade ticket.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestKitDisabled = new Translation("<#a8918a>This kit is disabled.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestKitMapBlacklisted = new Translation("<#a8918a>This kit is not allowed on this map.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestKitFactionBlacklisted = new Translation("<#a8918a>Your team is not allowed to use this kit.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestKitMissingAccess = new Translation("<#a8918a>You don't have access to this kit.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestKitMissingNitro = new Translation("<#a8918a>You must be <#e00ec9>NITRO BOOSTING</color> to use this kit.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestKitNotBought = new Translation<int>("<#99918d>Look at this sign and type '<#ffe2ab>/buy</color>' to unlock this kit permanently for <#c$credits$>C </color><#ffffff>{0}</color>.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int, int> RequestKitCantAfford = new Translation<int, int>("<#a8918a>You are missing <#c$credits$>C </color><#ffffff>{0}</color> / <#c$credits$>C </color><#ffffff>{1}</color> needed to unlock this kit.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<FactionInfo> RequestKitWrongTeam = new Translation<FactionInfo>("<#a8918a>You must be part of {0} to request this kit.", FactionInfo.FormatShortName);
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestNotBuyable = new Translation("<#a8918a>This kit cannot be purchased with credits.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestKitLimited = new Translation<int>("<#a8918a>Your team already has a max of <#d9e882>{0}</color> players using this kit. Try again later.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<LevelData> RequestKitLowLevel = new Translation<LevelData>("<#b3ab9f>You must be <#ffc29c>{0}</color> to use this kit.", LevelData.FormatName);
    [TranslationData(SectionRequest)]
    public static readonly Translation<RankData> RequestKitLowRank = new Translation<RankData>("<#b3ab9f>You must be {0} to use this kit.", RankData.FormatColorName);
    [TranslationData(SectionRequest)]
    public static readonly Translation<QuestAsset> RequestKitQuestIncomplete = new Translation<QuestAsset>("<#b3ab9f>You have to complete {0} to request this kit.", BaseQuestData.FormatColorQuestAsset);
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestKitNotSquadleader = new Translation("<#b3ab9f>You must be a <#cedcde>SQUAD LEADER</color> in order to get this kit.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestLoadoutNotOwned = new Translation("<#a8918a>You do not own this loadout.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int, int> RequestVehicleCantAfford = new Translation<int, int>("<#a8918a>You are missing <#c$credits$>C </color><#ffffff>{0}</color> / <#c$credits$>C </color><#ffffff>{1}</color> needed to request this vehicle.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<Cooldown> RequestVehicleCooldown = new Translation<Cooldown>("<#b3ab9f>This vehicle can't be requested for another: <#ffe2ab>{0}</color>.", Cooldown.FormatTimeShort);
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestVehicleNotSquadLeader = new Translation("<#b3ab9f>You must be a <#cedcde>SQUAD LEADER</color> in order to request this vehicle.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestVehicleNotInSquad = new Translation("<#b3ab9f>You must be <#cedcde>IN A SQUAD</color> in order to request this vehicle.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestVehicleNoKit = new Translation("<#a8918a>Get a kit before you request vehicles.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<FactionInfo> RequestVehicleOtherTeam = new Translation<FactionInfo>("<#a8918a>You must be on {0} to request this vehicle.", FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionRequest)]
    public static readonly Translation<Class> RequestVehicleWrongClass = new Translation<Class>("<#b3ab9f>You need a <#cedcde><uppercase>{0}</uppercase></color> kit in order to request this vehicle.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<LevelData> RequestVehicleMissingLevels = new Translation<LevelData>("<#b3ab9f>You must be <#ffc29c>{0}</color> to request this vehicle.", LevelData.FormatName);
    [TranslationData(SectionRequest)]
    public static readonly Translation<RankData> RequestVehicleRankIncomplete = new Translation<RankData>("<#b3ab9f>You must be {0} to request this vehicle.", RankData.FormatColorName);
    [TranslationData(SectionRequest)]
    public static readonly Translation<QuestAsset> RequestVehicleQuestIncomplete = new Translation<QuestAsset>("<#b3ab9f>You have to complete {0} to request this vehicle.", BaseQuestData.FormatColorQuestAsset);
    [TranslationData(SectionRequest)]
    public static readonly Translation<IPlayer> RequestVehicleAlreadyRequested = new Translation<IPlayer>("<#a8918a>This vehicle was already requested by {0}.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionRequest)]
    public static readonly Translation<InteractableVehicle> RequestVehicleAlreadyOwned = new Translation<InteractableVehicle>("<#a8918a>You already have a nearby {0}.", VehicleData.COLORED_NAME);
    [TranslationData(SectionRequest)]
    public static readonly Translation<VehicleData> RequestVehicleSuccess = new Translation<VehicleData>("<#b3a591>This {0} is now yours to take into battle.", VehicleData.COLORED_NAME);
    [TranslationData(SectionRequest)]
    public static readonly Translation<VehicleData> RequestVehicleDead = new Translation<VehicleData>("<#b3a591>The {0} was destroyed and will be restocked soon.", VehicleData.COLORED_NAME);
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestVehicleAssetBannedGlobalPermanent = new Translation("<#b3a591>You are permanently banned from using all vehicles.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<TimeSpan> RequestVehicleAssetBannedGlobal = new Translation<TimeSpan>("<#b3a591>You are banned from using all vehicles for another {0}.", FormatTimeLong);
    [TranslationData(SectionRequest)]
    public static readonly Translation<string> RequestVehicleAssetBannedPermanent = new Translation<string>("<#b3a591>You are permanently banned from using <#fff>{0}</color>.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<TimeSpan, string> RequestVehicleAssetBanned = new Translation<TimeSpan, string>("<#b3a591>You are banned from using <#fff>{1}</color> for another {0}.", FormatTimeLong);

    #region Vehicle Request Delays
    [TranslationData(SectionRequest)]
    public static readonly Translation<string> RequestVehicleTimeDelay = new Translation<string>("<#b3ab9f>This vehicle is delayed for another: <#c$vbs_delay$>{0}</color>.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<Cache> RequestVehicleCacheDelayAtk1 = new Translation<Cache>("<#b3ab9f>Destroy <color=#c$vbs_delay$>{0}</color> to request this vehicle.", FOB.FormatName);
    [TranslationData(SectionRequest)]
    public static readonly Translation<Cache> RequestVehicleCacheDelayDef1 = new Translation<Cache>("<#b3ab9f>You can't request this vehicle until you lose <color=#c$vbs_delay$>{0}</color>.", FOB.FormatName);
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestVehicleCacheDelayAtkUndiscovered1 = new Translation("<#b3ab9f><color=#c$vbs_delay$>Discover and Destroy</color> the next cache to request this vehicle.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestVehicleCacheDelayDefUndiscovered1 = new Translation("<#b3ab9f>You can't request this vehicle until you've <color=#c$vbs_delay$>uncovered and lost</color> your next cache.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestVehicleCacheDelayMultipleAtk = new Translation<int>("<#b3ab9f>Destroy <#c$vbs_delay$>{0} more caches</color> to request this vehicle.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestVehicleCacheDelayMultipleDef = new Translation<int>("<#b3ab9f>You can't request this vehicle until you've lost <#c$vbs_delay$>{0} more caches</color>.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<Flag> RequestVehicleFlagDelay1 = new Translation<Flag>("<#b3ab9f>Capture {0} to request this vehicle.", TranslationFlags.PerTeamTranslation, Flag.COLOR_NAME_DISCOVER_FORMAT);
    [TranslationData(SectionRequest)]
    public static readonly Translation<Flag> RequestVehicleLoseFlagDelay1 = new Translation<Flag>("<#b3ab9f>You can't request this vehicle until you lose {0}.", TranslationFlags.PerTeamTranslation, Flag.COLOR_NAME_DISCOVER_FORMAT);
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestVehicleFlagDelayMultiple = new Translation<int>("<#b3ab9f>Capture <#c$vbs_delay$>{0} more flags</color> to request this vehicle.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestVehicleLoseFlagDelayMultiple = new Translation<int>("<#b3ab9f>You can't request this vehicle until you lose <#c$vbs_delay$>{0} more flags</color>.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestVehicleStagingDelay = new Translation("<#a6918a>This vehicle can only be requested after the game starts.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<string> RequestVehicleUnknownDelay = new Translation<string>("<#b3ab9f>This vehicle is delayed because: <#c$vbs_delay$>{0}</color>.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestVehicleTeammatesDelay = new Translation<int>("<#b3ab9f>This vehicle is delayed until <#c$vbs_delay$>{0}v{0}</color> players online.");
    #endregion

    #region Trait Request Delays
    [TranslationData(SectionRequest)]
    public static readonly Translation<string> RequestTraitTimeDelay = new Translation<string>("<#b3ab9f>This trait is delayed for another: <#c$vbs_delay$>{0}</color>.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<Cache> RequestTraitCacheDelayAtk1 = new Translation<Cache>("<#b3ab9f>Destroy <color=#c$vbs_delay$>{0}</color> to request this trait.", FOB.FormatName);
    [TranslationData(SectionRequest)]
    public static readonly Translation<Cache> RequestTraitCacheDelayDef1 = new Translation<Cache>("<#b3ab9f>You can't request this trait until you lose <color=#c$vbs_delay$>{0}</color>.", FOB.FormatName);
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestTraitCacheDelayAtkUndiscovered1 = new Translation("<#b3ab9f><color=#c$vbs_delay$>Discover and Destroy</color> the next cache to request this trait.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestTraitCacheDelayDefUndiscovered1 = new Translation("<#b3ab9f>You can't request this trait until you've <color=#c$vbs_delay$>uncovered and lost</color> your next cache.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestTraitCacheDelayMultipleAtk = new Translation<int>("<#b3ab9f>Destroy <#c$vbs_delay$>{0} more caches</color> to request this trait.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestTraitCacheDelayMultipleDef = new Translation<int>("<#b3ab9f>You can't request this trait until you've lost <#c$vbs_delay$>{0} more caches</color>.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<Flag> RequestTraitFlagDelay1 = new Translation<Flag>("<#b3ab9f>Capture {0} to request this trait.", TranslationFlags.PerTeamTranslation, Flag.COLOR_NAME_DISCOVER_FORMAT);
    [TranslationData(SectionRequest)]
    public static readonly Translation<Flag> RequestTraitLoseFlagDelay1 = new Translation<Flag>("<#b3ab9f>You can't request this trait until you lose {0}.", TranslationFlags.PerTeamTranslation, Flag.COLOR_NAME_DISCOVER_FORMAT);
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestTraitFlagDelayMultiple = new Translation<int>("<#b3ab9f>Capture <#c$vbs_delay$>{0} more flags</color> to request this trait.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestTraitLoseFlagDelayMultiple = new Translation<int>("<#b3ab9f>You can't request this trait until you lose <#c$vbs_delay$>{0} more flags</color>.");
    [TranslationData(SectionRequest)]
    public static readonly Translation RequestTraitStagingDelay = new Translation("<#a6918a>This trait can only be requested after the game starts.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<string> RequestTraitUnknownDelay = new Translation<string>("<#b3ab9f>This trait is delayed because: <#c$vbs_delay$>{0}</color>.");
    [TranslationData(SectionRequest)]
    public static readonly Translation<int> RequestTraitTeammatesDelay = new Translation<int>("<#b3ab9f>This trait is delayed until <#c$vbs_delay$>{0}v{0}</color> players online.");
    #endregion

    #endregion

    #region Strutures
    private const string SectionStructures = "Structures";
    [TranslationData(SectionStructures)]
    public static readonly Translation StructureNoTarget = new Translation("<#ff8c69>You must be looking at a barricade, structure, or vehicle.");
    [TranslationData(SectionStructures, IsPrioritizedTranslation = false)]
    public static readonly Translation<SavedStructure> StructureSaved = new Translation<SavedStructure>("<#e6e3d5>Saved <#c6d4b8>{0}</color>.");
    [TranslationData(SectionStructures, IsPrioritizedTranslation = false)]
    public static readonly Translation<SavedStructure> StructureAlreadySaved = new Translation<SavedStructure>("<#e6e3d5><#c6d4b8>{0}</color> is already saved.");
    [TranslationData(SectionStructures, IsPrioritizedTranslation = false)]
    public static readonly Translation<SavedStructure> StructureUnsaved = new Translation<SavedStructure>("<#e6e3d5>Removed <#c6d4b8>{0}</color> save.");
    [TranslationData(SectionStructures, IsPrioritizedTranslation = false)]
    public static readonly Translation<ItemAsset> StructureAlreadyUnsaved = new Translation<ItemAsset>("<#ff8c69><#c6d4b8>{0}</color> is not saved.");
    [TranslationData(SectionStructures, IsPrioritizedTranslation = false)]
    public static readonly Translation<Asset> StructureDestroyed = new Translation<Asset>("<#e6e3d5>Destroyed <#c6d4b8>{0}</color>.");
    [TranslationData(SectionStructures, IsPrioritizedTranslation = false)]
    public static readonly Translation StructureNotDestroyable = new Translation("<#ff8c69>That object can not be destroyed.");
    [TranslationData(SectionStructures)]
    public static readonly Translation StructureExamineNotExaminable = new Translation("<#ff8c69>That object can not be examined.");
    [TranslationData(SectionStructures)]
    public static readonly Translation StructureExamineNotLocked = new Translation("<#ff8c69>This vehicle is not locked.");
    [TranslationData(SectionStructures)]
    public static readonly Translation<Asset, IPlayer, FactionInfo> StructureExamineLastOwnerPrompt = new Translation<Asset, IPlayer, FactionInfo>("Last owner of {0}: {1}, Team: {2}.", TranslationFlags.TMProUI | TranslationFlags.NoRichText, arg1Fmt: UCPlayer.FormatPlayerName, arg2Fmt: FactionInfo.FormatDisplayName);
    [TranslationData(SectionStructures)]
    public static readonly Translation<Asset, IPlayer, IPlayer, FactionInfo> StructureExamineLastOwnerChat = new Translation<Asset, IPlayer, IPlayer, FactionInfo>("<#c6d4b8>Last owner of <#e6e3d5>{0}</color>: {1} <i>({2})</i>, Team: {3}.", TranslationFlags.TMProUI | TranslationFlags.NoRichText, FormatRarityColor, arg1Fmt: UCPlayer.FormatColoredPlayerName, arg2Fmt: UCPlayer.FormatSteam64, arg3Fmt: FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionStructures)]
    public static readonly Translation<Asset, IPlayer, FactionInfo, string, ulong> VehicleExamineLastOwnerPrompt = new Translation<Asset, IPlayer, FactionInfo, string, ulong>("Owner of {0}: {1}, Team: {2}. Previous Owner: {3} ({4}).", TranslationFlags.TMProUI | TranslationFlags.NoRichText, arg1Fmt: UCPlayer.FormatPlayerName, arg2Fmt: FactionInfo.FormatDisplayName);
    [TranslationData(SectionStructures)]
    public static readonly Translation<Asset, IPlayer, IPlayer, FactionInfo, string, ulong> VehicleExamineLastOwnerChat = new Translation<Asset, IPlayer, IPlayer, FactionInfo, string, ulong>("<#c6d4b8>Owner of <#e6e3d5>{0}</color>: {1} <i>({2})</i>, Team: {3}. Previous Owner: {4} <i>({5})</i>.", TranslationFlags.TMProUI | TranslationFlags.NoRichText, FormatRarityColor, arg1Fmt: UCPlayer.FormatColoredPlayerName, arg2Fmt: UCPlayer.FormatSteam64, arg3Fmt: FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionStructures, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> StructureSaveInvalidProperty = new Translation<string>("<#ff8c69>{0} isn't a valid a structure property. Try putting 'owner' or 'group'.");
    [TranslationData(SectionStructures, IsPrioritizedTranslation = false)]
    public static readonly Translation<string, string> StructureSaveInvalidSetValue = new Translation<string, string>("<#ff8c69><#ddd>{0}</color> isn't a valid value for structure property: <#a0ad8e>{1}</color>.");
    [TranslationData(SectionStructures, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> StructureSaveNotJsonSettable = new Translation<string>("<#ff8c69><#a0ad8e>{0}</color> is not marked as settable.");
    [TranslationData(SectionStructures, IsPrioritizedTranslation = false)]
    public static readonly Translation<string, ItemAsset, string> StructureSaveSetProperty = new Translation<string, ItemAsset, string>("<#a0ad8e>Set <#8ce4ff>{0}</color> for {1} save to: <#ffffff>{2}</color>.", arg1Fmt: FormatRarityColor);
    #endregion

    #region Whitelist
    private const string SectionWhitelist = "Whitelist";
    [TranslationData(SectionWhitelist, IsPrioritizedTranslation = false)]
    public static readonly Translation<ItemAsset> WhitelistAdded = new Translation<ItemAsset>("<#a0ad8e>Whitelisted item: {0}.", FormatRarityColor);
    [TranslationData(SectionWhitelist, IsPrioritizedTranslation = false)]
    public static readonly Translation<ItemAsset, int> WhitelistSetAmount = new Translation<ItemAsset, int>("<#a0ad8e>Amount for whitelisted item: {0} set to {1}.", FormatRarityColor);
    [TranslationData(SectionWhitelist, IsPrioritizedTranslation = false)]
    public static readonly Translation<ItemAsset> WhitelistRemoved = new Translation<ItemAsset>("<#a0ad8e>Removed whitelist for: {0}.", FormatRarityColor);
    [TranslationData(SectionWhitelist, IsPrioritizedTranslation = false)]
    public static readonly Translation<ItemAsset> WhitelistAlreadyAdded = new Translation<ItemAsset>("<#ff8c69>{0} is already whitelisted.", FormatRarityColor);
    [TranslationData(SectionWhitelist, IsPrioritizedTranslation = false)]
    public static readonly Translation<ItemAsset> WhitelistAlreadyRemoved = new Translation<ItemAsset>("<#ff8c69>{0} is not whitelisted.", FormatRarityColor);
    [TranslationData(SectionWhitelist, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> WhitelistItemNotID = new Translation<string>("<#ff8c69><uppercase>{0}</uppercase> couldn't be read as an <#cedcde>ITEM ID</color>.");
    [TranslationData(SectionWhitelist, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> WhitelistMultipleResults = new Translation<string>("<#ff8c69><uppercase>{0}</uppercase> found multiple results, please narrow your search or use an <#cedcde>ITEM ID</color>.");
    [TranslationData(SectionWhitelist, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> WhitelistInvalidAmount = new Translation<string>("<#ff8c69><uppercase>{0}</uppercase> couldn't be read as a <#cedcde>AMOUNT</color> (1-250).");
    [TranslationData(SectionWhitelist)]
    public static readonly Translation<ItemAsset> WhitelistProhibitedPickup = new Translation<ItemAsset>("<#ff8c69>{0} can't be picked up.", FormatRarityColor + FormatPlural);
    [TranslationData(SectionWhitelist)]
    public static readonly Translation<ItemAsset> WhitelistProhibitedSalvage = new Translation<ItemAsset>("<#ff8c69>{0} can't be salvaged.", FormatRarityColor + FormatPlural);
    [TranslationData(SectionWhitelist)]
    public static readonly Translation<ItemAsset> WhitelistProhibitedPickupAmt = new Translation<ItemAsset>("<#ff8c69>You can't carry any more {0}.", FormatRarityColor + FormatPlural);
    [TranslationData(SectionWhitelist)]
    public static readonly Translation<ItemAsset> WhitelistProhibitedPlace = new Translation<ItemAsset>("<#ff8c69>You're not allowed to place {0}.", FormatRarityColor + FormatPlural);
    [TranslationData(SectionWhitelist)]
    public static readonly Translation<int, ItemAsset> WhitelistProhibitedPlaceAmt = new Translation<int, ItemAsset>("<#ff8c69>You're not allowed to place more than {0} {1}.", FormatRarityColor + FormatPlural + "{0}");
    [TranslationData(SectionWhitelist)]
    public static readonly Translation WhitelistNoKit = new Translation("<#ff8c69>Get a kit first before you can pick up items.");
    #endregion

    #region Vehicles
    private const string SectionVehicles = "Vehicles";
    private const string SectionVehicleBay = "Vehicle Bay";
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset> VehicleStaging = new Translation<VehicleAsset>("<#b3a6a2>You can't enter a {0} during the <#cedcde>STAGING PHASE</color>.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleWaitForOwner = new Translation<IPlayer>("<#bda897>Only the owner, {0}, can enter the driver's seat right now.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer, Squad> VehicleWaitForOwnerOrSquad = new Translation<IPlayer, Squad>("<#bda897>Only the owner, {0}, or members of {1} Squad can enter the driver's seat right now.", UCPlayer.FormatColoredCharacterName, Squad.FormatColorName);
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
    public static readonly Translation<FactionInfo> VehicleNotOnSameTeam = new Translation<FactionInfo>("<#ff8c69>This vehicle is on {0} but you're not.", FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleLinkedVehicleNotOwnedByCaller = new Translation<IPlayer>("<#ff8c69>This vehicle is owned by {0}.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, IPlayer> VehicleGiven = new Translation<VehicleAsset, IPlayer>("<#d1bda7>Gave your <#a0ad8e>{0}</color> to {1}.", arg1Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, IPlayer> VehicleGivenDm = new Translation<VehicleAsset, IPlayer>("<#d1bda7>{1} gave you their <#a0ad8e>{0}</color>.", arg1Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleTargetNotInVehicle = new Translation<IPlayer>("<#ff8c69>{0} is not in a vehicle.", UCPlayer.FormatColoredNickName);
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
    public static readonly Translation<IPlayer> VehicleSwappedSeats = new Translation<IPlayer>("<#d1bda7>Swapped seats with {0}.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, int> VehicleEnterFailed = new Translation<VehicleAsset, int>("<#ff8c69>Unable to put you in seat <#ddd>#{1}</color> of your <#ccc>{0}</color>.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, int> VehicleEnterForceSwapped = new Translation<VehicleAsset, int>("<#d1bda7>Put you in seat <#ddd>#{1}</color> of your <#ccc>{0}</color>.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset, IPlayer> VehicleSwapRequestNotInSameVehicle = new Translation<VehicleAsset, IPlayer>("<#ff8c69>You must be in the same <#ccc>{0}</color> as {1}.", arg1Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer, int, IPlayer> VehicleSwapRequestAlreadySent = new Translation<IPlayer, int, IPlayer>("<#ff8c69>{0} already has a pending swap request from {2}, try again in <#ccc>{1}</color> ${p:1:second}.", UCPlayer.FormatColoredNickName, arg2Fmt: UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer, int, string> VehicleSentSwapRequestDm = new Translation<IPlayer, int, string>("<#d1bda7>{0} wants to swap from seat <#ddd>#{1}</color> with you. Do <#fff>{2}</color> to respond.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer, int, int> VehicleSwapRequestSent = new Translation<IPlayer, int, int>("<#d1bda7>Sent {0} a swap request for seat <#ddd>#{1}</color>. They have <#ccc>{2}</color> ${p:2:second} to respond.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleSwapRequestDeniedByTarget = new Translation<IPlayer>("<#d1a8a8>{0} denied your swap request.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleSwapRequestTimedOutByTarget = new Translation<IPlayer>("<#d1a8a8>{0} didn't respond to your swap request.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleSwapRequestAcceptedByTarget = new Translation<IPlayer>("<#d1bda7>{0} accepted your swap request.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation VehicleSwapRequestNotSent = new Translation("<#d1a8a8>You do not have any pending swap requests.");
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleSwapRequestDenied = new Translation<IPlayer>("<#d1a8a8>Denied {0}'s swap request.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<IPlayer> VehicleSwapRequestAccepted = new Translation<IPlayer>("<#d1bda7>Accepted {0}'s swap request.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionVehicles)]
    public static readonly Translation<VehicleAsset> VehicleTooFarAway = new Translation<VehicleAsset>("<#ff8c69>Your {0} is too far away.");

    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayAdded = new Translation<VehicleAsset>("<#a0ad8e>Added {0} to the vehicle bay.", FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayRemoved = new Translation<VehicleAsset>("<#a0ad8e>Removed {0} from the vehicle bay.", FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<string, VehicleAsset, string> VehicleBaySetProperty = new Translation<string, VehicleAsset, string>("<#a0ad8e>Set <#8ce4ff>{0}</color> for vehicle {1} to: <#ffffff>{2}</color>.", arg1Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBaySavedMeta = new Translation<VehicleAsset>("<#a0ad8e>Successfuly set the rearm list for vehicle {0} from your inventory.", FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayClearedItems = new Translation<VehicleAsset>("<#a0ad8e>Successfuly cleared the rearm list for vehicle {0} from your inventory.", FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset, int> VehicleBaySetItems = new Translation<VehicleAsset, int>("<#a0ad8e>Successfuly set the rearm list for vehicle {0} from your inventory. It will now drop <#8ce4ff>{1}</color> ${p:1:item} on restock.", FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<byte, VehicleAsset> VehicleBaySeatAdded = new Translation<byte, VehicleAsset>("<#a0ad8e>Made seat <#ffffff>#{0}</color> a crewman seat for {1}.", arg1Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<byte, VehicleAsset> VehicleBaySeatAlreadyAdded = new Translation<byte, VehicleAsset>("<#a0ad8e>Set <#ffffff>#{0}</color> was already a crewman seat for {1}.", arg1Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<byte, VehicleAsset> VehicleBaySeatRemoved = new Translation<byte, VehicleAsset>("<#a0ad8e>Seat <#ffffff>#{0}</color> is no longer a crewman seat for {1}.", arg1Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<byte, VehicleAsset> VehicleBaySeatNotAdded = new Translation<byte, VehicleAsset>("<#a0ad8e>Seat <#ffffff>#{0}</color> wasn't a crewman seat for {1}.", arg1Fmt: FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation VehicleBayNoTarget = new Translation("<#ff8c69>Look at a vehicle, spawn pad, or sign to use this command.");
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayAlreadyAdded = new Translation<VehicleAsset>("<#ff8c69>{0} is already added to the vehicle bay.", FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayNotAdded = new Translation<VehicleAsset>("<#ff8c69>{0} has not been added to the vehicle bay.", FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> VehicleBayInvalidProperty = new Translation<string>("<#ff8c69>{0} isn't a valid a vehicle property. Try putting 'Faction', 'RespawnTime', 'CreditCost' etc.");
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<string, string> VehicleBayInvalidSetValue = new Translation<string, string>("<#ff8c69><#ddd>{0}</color> isn't a valid value for vehicle property: <#a0ad8e>{1}</color>.");
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> VehicleBayNotCommandSettable = new Translation<string>("<#ff8c69><#a0ad8e>{0}</color> is not marked as settable.");
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<byte, VehicleAsset> VehicleBayCrewSeatAlreadySet = new Translation<byte, VehicleAsset>("<#ff8c69><#ffffff>#{0}</color> is already marked as a crew seat in {1}.", arg1Fmt: FormatRarityColor + FormatPlural);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<byte, VehicleAsset> VehicleBayCrewSeatNotSet = new Translation<byte, VehicleAsset>("<#ff8c69><#ffffff>#{0}</color> isn't marked as a crew seat in {1}.", arg1Fmt: FormatRarityColor + FormatPlural);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<DelayType, float, string?> VehicleBayAddedDelay = new Translation<DelayType, float, string?>("<#a0ad8e>Added delay of type <#fff>{0}</color>:<#ddd>{1}</color> during <#ddd>{2}</color> gamemode.", arg1Fmt: "N1");
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> VehicleBayRemovedDelay = new Translation<int>("<#a0ad8e>Removed {0} matching ${p:0:delay}.");
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBaySpawnRegistered = new Translation<VehicleAsset>("<#a0ad8e>Successfully registered spawn. {0} will spawn here.", FormatRarityColor + FormatPlural);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBaySpawnDeregistered = new Translation<VehicleAsset>("<#a0ad8e>Successfully deregistered {0} spawn.", FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation VehicleBayLinkStarted = new Translation("<#a0ad8e>Started linking, do <#ddd>/vb link</color> on the sign now.");
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayLinkFinished = new Translation<VehicleAsset>("<#a0ad8e>Successfully linked vehicle sign to a {0} vehicle bay.", FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayUnlinked = new Translation<VehicleAsset>("<#a0ad8e>Successfully unlinked {0} vehicle sign.", FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation VehicleBayLinkNotStarted = new Translation("<#ff8c69>You must do /vb link on a vehicle bay first.");
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBayForceSuccess = new Translation<VehicleAsset>("<#a0ad8e>Skipped timer for that {0} vehicle bay.", FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> VehicleBayInvalidInput = new Translation<string>("<#ff8c69><#fff>{0}</color> is not a valid vehicle.");
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<ItemAsset> VehicleBayInvalidBayItem = new Translation<ItemAsset>("<#ff8c69>{0} are not valid vehicle bays.", FormatRarityColor + FormatPlural);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation<VehicleAsset> VehicleBaySpawnAlreadyRegistered = new Translation<VehicleAsset>("<#ff8c69>This spawn is already registered to a {0}. Unregister it first with <#fff>/vb unreg</color>.", FormatRarityColor);
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
    public static readonly Translation VehicleBaySpawnNotRegistered = new Translation("<#ff8c69>This vehicle bay is not registered.");
    [TranslationData(SectionVehicleBay, IsPrioritizedTranslation = false)]
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

    #region Clear
    private const string SectionClear = "Clear Command";
    [TranslationData(SectionClear, IsPrioritizedTranslation = false)]
    public static readonly Translation ClearNoPlayerConsole = new Translation("Specify a player name when clearing from console.", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionClear, IsPrioritizedTranslation = false)]
    public static readonly Translation ClearInventorySelf = new Translation("<#e6e3d5>Cleared your inventory.");
    [TranslationData(SectionClear, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer> ClearInventoryOther = new Translation<IPlayer>("<#e6e3d5>Cleared {0}'s inventory.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionClear, IsPrioritizedTranslation = false)]
    public static readonly Translation ClearItems = new Translation("<#e6e3d5>Cleared all dropped items.");
    [TranslationData(SectionClear, IsPrioritizedTranslation = false)]
    public static readonly Translation<float> ClearItemsInRange = new Translation<float>("<#e6e3d5>Cleared all dropped items in {0}m.", "F0");
    [TranslationData(SectionClear, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer> ClearItemsOther = new Translation<IPlayer>("<#e6e3d5>Cleared {0}'s dropped items.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionClear, IsPrioritizedTranslation = false)]
    public static readonly Translation ClearStructures = new Translation("<#e6e3d5>Cleared all placed structures and barricades.");
    [TranslationData(SectionClear, IsPrioritizedTranslation = false)]
    public static readonly Translation ClearVehicles = new Translation("<#e6e3d5>Cleared all vehicles.");
    #endregion

    #region Shutdown
    private const string SectionShutdown = "Shutdown Broadcasts";
    [TranslationData(SectionShutdown)]
    public static readonly Translation<string> ShutdownBroadcastAfterGame = new Translation<string>("<#00ffff>A shutdown has been scheduled after this game because: \"<#6699ff>{0}</color>\".");
    [TranslationData(SectionShutdown)]
    public static readonly Translation<string> ShutdownBroadcastDaily = new Translation<string>("<#00ffff>A daily restart will occur after this game. Down-time estimate: <#6699ff>2 minutes</color>.", TranslationFlags.SuppressWarnings);
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
    public static readonly Translation<Trait> TraitDisabledSquadLeaderDemoted = new Translation<Trait>("<#e86868><#c$trait$>{0}</color> is disabled until it expires or you become <#cedcde>SQUAD LEADER</color> again.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player leaves a squad while under the effect of a trait requiring a squad.", "The trait requiring a squad")]
    public static readonly Translation<Trait> TraitDisabledSquadLeft = new Translation<Trait>("<#e86868><#c$trait$>{0}</color> is disabled until you join a <#cedcde>SQUAD</color> again.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player equips a kit that's not supported by the trait.", "The trait requiring a kit")]
    public static readonly Translation<Trait> TraitDisabledKitNotSupported = new Translation<Trait>("<#e86868><#c$trait$>{0}</color> is disabled until you switch to a supported kit type.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player performs an action that allows their trait to be reactivated.", "The trait being reactivated")]
    public static readonly Translation<Trait> TraitReactivated = new Translation<Trait>("<#e86868><#c$trait$>{0}</color> has been reactivated.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when one of a player's traits expires through time.", "The trait that expired")]
    public static readonly Translation<TraitData> TraitExpiredTime = new Translation<TraitData>("<#e86868><#c$trait$>{0}</color> has expired and is no longer active.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when one of a player's traits expires through death.", "The trait that expired")]
    public static readonly Translation<TraitData> TraitExpiredDeath = new Translation<TraitData>("<#e86868><#c$trait$>{0}</color> has expired after your death and is no longer active.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait which is locked by the current gamemode.", "The locked trait", "Current gamemode")]
    public static readonly Translation<TraitData, Gamemode> RequestTraitGamemodeLocked = new Translation<TraitData, Gamemode>("<#ff8c69><#c$trait$>{0}</color> is <#c$locked$>locked</color> during <#cedcde><uppercase>{1}</uppercase></color> games.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait while they already have it.", "The existing trait")]
    public static readonly Translation<TraitData> TraitAlreadyActive = new Translation<TraitData>("<#ff8c69>You are already under <#c$trait$>{0}</color>'s effects.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait meant for another team.", "The trait", "Trait's intended team")]
    public static readonly Translation<TraitData, FactionInfo> RequestTraitWrongTeam = new Translation<TraitData, FactionInfo>("<#ff8c69>You can only use <#c$trait$>{0}</color> on {1}.", TraitData.FormatName, FactionInfo.FormatColorShortName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait without a kit.")]
    public static readonly Translation RequestTraitNoKit = new Translation("<#ff8c69>Request a kit before trying to request traits.");
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait with a kit class the trait doesn't allow.", "The trait", "Invalid class")]
    public static readonly Translation<TraitData, Class> RequestTraitClassLocked = new Translation<TraitData, Class>("<#ff8c69>You can't use <#c$trait$>{0}</color> while a <#cedcde><uppercase>{1}</uppercase></color> kit is equipped.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait while under the global trait cooldown.", "Global cooldown shared between all traits")]
    public static readonly Translation<Cooldown> RequestTraitGlobalCooldown = new Translation<Cooldown>("<#ff8c69>You can request a trait again in <#cedcde>{0}</color>.", Cooldown.FormatTimeShort);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait while under the individual trait cooldown.", "Trait on cooldown", "Individual cooldown for this trait")]
    public static readonly Translation<TraitData, Cooldown> RequestTraitSingleCooldown = new Translation<TraitData, Cooldown>("<#ff8c69>You can request <#c$trait$>{0}</color> again in <#cedcde>{1}</color>.", TraitData.FormatName, Cooldown.FormatTimeShort);
    [TranslationData(SectionTraits, "Sent when the player tries to request a buff when they already have the max amount (6).")]
    public static readonly Translation RequestTraitTooManyBuffs = new Translation("<#ff8c69>You can't have more than <#cedcde>six</color> buffs active at once.");
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait which requires squad leader while not being squad leader or in a squad.", "Trait being requested")]
    public static readonly Translation<TraitData> RequestTraitNotSquadLeader = new Translation<TraitData>("<#ff8c69>You have to be a <#cedcde>SQUAD LEADER</color> to request <#c$trait$>{0}</color>.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait which requires squad leader while not in a squad.", "Trait being requested")]
    public static readonly Translation<TraitData> RequestTraitNoSquad = new Translation<TraitData>("<#ff8c69>You have to be in a <#cedcde>SQUAD</color> to request <#c$trait$>{0}</color>.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait while too low of a level.", "Trait being requested", "Required Level")]
    public static readonly Translation<TraitData, LevelData> RequestTraitLowLevel = new Translation<TraitData, LevelData>("<#ff8c69>You must be at least <#cedcde>{1}</color> to request <#c$trait$>{0}</color>.", TraitData.FormatName, LevelData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait while too low of a rank.", "Trait being requested", "Required Rank")]
    public static readonly Translation<TraitData, RankData> RequestTraitLowRank = new Translation<TraitData, RankData>("<#ff8c69>You must be at least {1} to request <#c$trait$>{0}</color>.", TraitData.FormatName, RankData.FormatColorName);
    [TranslationData(SectionTraits, "Sent when the player tries to request a trait while missing a completed quest.", "Trait being requested", "Required Rank")]
    public static readonly Translation<TraitData, QuestAsset> RequestTraitQuestIncomplete = new Translation<TraitData, QuestAsset>("<#ff8c69>You must be at least {1} to request <#c$trait$>{0}</color>.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player successfully requests a trait.", "Trait being requested")]
    public static readonly Translation<TraitData> RequestTraitGiven = new Translation<TraitData>("<#a8918a>Your <#c$trait$>{0}</color> has been activated.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player successfully requests a trait with a timer.", "Trait being requested", "Time left")]
    public static readonly Translation<TraitData, string> RequestTraitGivenTimer = new Translation<TraitData, string>("<#a8918a>Your <#c$trait$>{0}</color> has been activated. It will expire in <#cedcde>{1}</color>.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player successfully requests a trait that expires on death.", "Trait being requested")]
    public static readonly Translation<TraitData> RequestTraitGivenUntilDeath = new Translation<TraitData>("<#a8918a>Your <#c$trait$>{0}</color> has been activated. It will last until you die.", TraitData.FormatName);
    [TranslationData(SectionTraits, "Sent when the player successfully requests a trait but it's still staging phase.", "Trait being requested")]
    public static readonly Translation<TraitData> TraitAwaitingStagingPhase = new Translation<TraitData>("<#a8918a><#c$trait$>{0}</color> will be activated once <#cedcde>STAGING PHASE</color> is over.", TraitData.FormatName);
    #region Trait Command
    private const string SectionTraitCommand = "Traits / Trait Command";
    [TranslationData(SectionTraitCommand, "Shown when a trait name is not able to be matched up with a TraitData.", "Inputted search")]
    public static readonly Translation<string> TraitNotFound = new Translation<string>("<#66ffcc>Unable to find a trait named <#fff>{0}</color>.");
    [TranslationData(SectionTraitCommand, "Shown when a trait is removed.", "Trait that got removed.")]
    public static readonly Translation<TraitData> TraitRemoved = new Translation<TraitData>("<#66ffcc>Removed <#c$trait$>{0}</color>.", TraitData.FormatName);
    [TranslationData(SectionTraitCommand, "Shown when someone tries to remove a trait which they don't have.", "Trait the player tried to remove")]
    public static readonly Translation<TraitData> TraitNotActive = new Translation<TraitData>("<#ff8c69>You're not under <#c$trait$>{0}</color>'s effects.", TraitData.FormatName);
    [TranslationData(SectionTraitCommand, "Shown when someone tries to clear their traits with no traits.")]
    public static readonly Translation NoTraitsToClear = new Translation("<#ff8c69>You have no active traits.");
    [TranslationData(SectionTraitCommand, "Shown when someone clears their traits.", "Number of traits removed.")]
    public static readonly Translation<int> TraitsCleared = new Translation<int>("<#66ffcc>Removed {0} trait(s).");
    [TranslationData(SectionTraitCommand, "Shown when someone clears their traits.", "Target trait", "Property name", "Value")]
    public static readonly Translation<TraitData, string, string> TraitSetProperty = new Translation<TraitData, string, string>("<#66ffcc>Set <#c$trait$>{0}</color> / <#fff>{1}</color> to <uppercase><#cedcde>{2}</color></uppercase>.", TraitData.FormatTypeName);
    [TranslationData(SectionTraitCommand, "Shown when someone enteres an invalid property name to /trait set.", "Input text")]
    public static readonly Translation<string> TraitInvalidProperty = new Translation<string>("<#ff8c69><uppercase><#cedcde>{0}</color></uppercase> is not a valid property name for traits.");
    [TranslationData(SectionTraitCommand, "Shown when someone enteres an invalid property name to /trait set.", "Value", "Property name")]
    public static readonly Translation<string, string> TraitInvalidSetValue = new Translation<string, string>("<#ff8c69><uppercase><#cedcde>{0}</color></uppercase> is not a valid value for <#fff>{1}</color>.");
    [TranslationData(SectionTraitCommand, "Shown when someone enteres an invalid property name to /trait set.", "Property name")]
    public static readonly Translation<string> TraitNotJsonSettable = new Translation<string>("<#ff8c69><#fff>{0}</color> is not a property that can be changed in-game.");
    #endregion
    #region Trait Signs
    private const string SectionTraitSigns = "Traits / Sign";
    [TranslationData(SectionTraitSigns, "Shows instead of the credits when Credit Cost is 0.")]
    public static readonly Translation TraitSignFree = new Translation("<#c$kit_level_dollars_owned$>FREE</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionTraitSigns, "Shows instead of the unlock requirements when a trait is unlocked.")]
    public static readonly Translation TraitSignUnlocked = new Translation("<#99ff99>Unlocked</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionTraitSigns, "Shows when you're not in a squad and it's required.")]
    public static readonly Translation TraitSignRequiresSquad = new Translation("<#c$vbs_delay$>Join a Squad</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionTraitSigns, "Shows when you're not in a squad or not a squad leader and it's required.")]
    public static readonly Translation TraitSignRequiresSquadLeader = new Translation("<#c$vbs_delay$>Squad Leaders Only</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionTraitSigns, "Shows when you dont have a kit or have an unarmed kit.")]
    public static readonly Translation TraitSignNoKit = new Translation("<#c$vbs_delay$>Request a Kit</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionTraitSigns, "Shows when the trait is locked in the current gamemode.")]
    public static readonly Translation TraitGamemodeBlacklisted = new Translation("<#c$vbs_delay$>Locked</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionTraitSigns, "Shows when the kit class you have isn't compatible with the trait.", "Class name")]
    public static readonly Translation<Class> TraitSignClassBlacklisted = new Translation<Class>("<#c$vbs_delay$>Locked for {0}</color>", TranslationFlags.NoColorOptimization, FormatPlural);
    [TranslationData(SectionTraitSigns, "Shows when the kit class you have isn't compatible with the trait and theres a kit whitelist with 1 class.", "Class name")]
    public static readonly Translation<Class> TraitSignClassWhitelisted1 = new Translation<Class>("<#c$vbs_delay$>{0} Required</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionTraitSigns, "Shows when the kit class you have isn't compatible with the trait and theres a kit whitelist with 2 classes.", "Class name")]
    public static readonly Translation<Class, Class> TraitSignClassWhitelisted2 = new Translation<Class, Class>("<#c$vbs_delay$>{0} or {1} Required</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionTraitSigns, "Shows when you currently have the trait and it expires in time.", "Minutes", "Seconds")]
    public static readonly Translation<int, int> TraitSignAlreadyActiveTime = new Translation<int, int>("<#c$vbs_delay$>Already Active: {0}:{1}</color>", TranslationFlags.NoColorOptimization, arg1Fmt: "D2");
    [TranslationData(SectionTraitSigns, "Shows when you currently have the trait and it expires on death.")]
    public static readonly Translation TraitSignAlreadyActiveDeath = new Translation("<#c$vbs_delay$>Already Active</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionTraitSigns, "Shows when you are on either global or individual cooldown (whichever is longer).", "Minutes", "Seconds")]
    public static readonly Translation<int, int> TraitSignCooldown = new Translation<int, int>("<#c$vbs_delay$>On Cooldown: {0}:{1}</color>", TranslationFlags.NoColorOptimization, arg1Fmt: "D2");
    #endregion
    #region Trait Interactions
    private const string SectionTraitInteractions = "Traits / Interactions";
    [TranslationData(SectionTraitInteractions, "Sent when the player consumes their self-revive.", "Self-revive trait data.")]
    public static readonly Translation<TraitData> TraitUsedSelfRevive = new Translation<TraitData>("<#c$trait$>{0}</color> <#d97568>consumed</color>.", TraitData.FormatName);
    [TranslationData(SectionTraitInteractions, "Sent when the player tries to use their self-revive on cooldown.", "Self-revive trait data.", "Time string")]
    public static readonly Translation<TraitData, string> TraitSelfReviveCooldown = new Translation<TraitData, string>("<#c$trait$>{0}</color> can not be used for another {1}.", TraitData.FormatName);
    [TranslationData(SectionTraitInteractions, "Sent when the player isn't in a vehicle with Ace Armor.", "Ace armor trait data.")]
    public static readonly Translation<TraitData> AceArmorDisabledNotInVehicle = new Translation<TraitData>("<#e86868><#c$trait$>{0}</color> is disabled until you are driving an <#cedcde>ARMORED</color> vehicle.", TraitData.FormatName);
    #endregion
    #endregion

    #region Request Signs
    private const string SectionRequestSigns = "Kit Signs";
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitFree = new Translation("<#c$kit_free$>FREE</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitExclusive = new Translation("<#c$kit_level_dollars_exclusive$>EXCLUSIVE</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitNitroBoostOwned = new Translation("<#f66fe6>BOOSTING</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitNitroBoostNotOwned = new Translation("<#9b59b6>NITRO BOOST</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> KitName = new Translation<string>("<b>{0}</b>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> KitWeapons = new Translation<string>("<b>{0}</b>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns, IsPrioritizedTranslation = false)]
    public static readonly Translation<decimal> KitPremiumCost = new Translation<decimal>("<#c$kit_level_dollars$>$ {0}</color>", TranslationFlags.NoColorOptimization, "N2");
    [TranslationData(SectionRequestSigns, FormattingDescriptions = new string[] { "Level", "Color depending on player's current level." })]
    public static readonly Translation<string, Color> KitRequiredLevel = new Translation<string, Color>("<#f0a31c>Rank:</color> <#{1}>{0}</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns, FormattingDescriptions = new string[] { "Rank", "Color depending on player's current rank." })]
    public static readonly Translation<RankData, Color> KitRequiredRank = new Translation<RankData, Color>("<#{1}>Rank: {0}</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns, FormattingDescriptions = new string[] { "Quest", "Color depending on whether the player has completed the quest." })]
    public static readonly Translation<QuestAsset, Color> KitRequiredQuest = new Translation<QuestAsset, Color>("<#{1}>Quest: <#fff>{0}</color></color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns, FormattingDescriptions = new string[] { "Number of quests needed.", "Color depending on whether the player has completed the quest(s).", "s if {0} != 1" })]
    public static readonly Translation<int, Color, string> KitRequiredQuestsMultiple = new Translation<int, Color, string>("<#{1}>Finish <#fff>{0}</color> quest{2}.</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitRequiredQuestsComplete = new Translation("<#ff974d>Kit Unlocked</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitPremiumOwned = new Translation("<#c$kit_level_dollars_owned$>OWNED</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitCommanderTakenByViewer = new Translation("<#c$kit_level_dollars_owned$>You are the <#cedcde>COMMANDER</color>.</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation<IPlayer> KitCommanderTaken = new Translation<IPlayer>("<#f0a31c>Taken by <#fff>{0}</color></color>", TranslationFlags.NoColorOptimization, UCPlayer.FormatNickName);
    [TranslationData(SectionRequestSigns, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> KitCreditCost = new Translation<int>("<#c$credits$>C</color> <#fff>{0}</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitUnlimited = new Translation("<#c$kit_unlimited_players$>unlimited</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, int> KitPlayerCount = new Translation<int, int>("{0}/{1}", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> LoadoutName = new Translation<string>("<#c$kit_level_dollars$>LOADOUT {0}</color>", TranslationFlags.NoColorOptimization, FormatUppercase);
    [TranslationData(SectionRequestSigns, IsPrioritizedTranslation = false)]
    public static readonly Translation KitLoadoutUpgrade = new Translation("<#33cc33>/req upgrade</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionRequestSigns)]
    public static readonly Translation KitLoadoutSetup = new Translation("<#3399ff>PENDING SETUP</color>", TranslationFlags.NoColorOptimization);
    #endregion

    #region Vehicle Bay Signs
    private const string SectionVBS = "Vehicle Signs";
    [TranslationData(SectionVBS)]
    public static readonly Translation<int> VBSTickets = new Translation<int>("<#c$vbs_ticket_number$>{0}</color> <#c$vbs_ticket_label$>Tickets</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionVBS)]
    public static readonly Translation VBSStateReady = new Translation("<#c$vbs_ready$>Ready!</color> <#aaa><b>/request</b></color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionVBS, FormattingDescriptions = new string[] { "Minutes", "Seconds" }, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, int> VBSStateDead = new Translation<int, int>("<#c$vbs_dead$>{0}:{1}</color>", TranslationFlags.NoColorOptimization, arg1Fmt: "D2");
    [TranslationData(SectionVBS, FormattingDescriptions = new string[] { "Nearest location." }, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> VBSStateActive = new Translation<string>("<#c$vbs_active$>{0}</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionVBS, FormattingDescriptions = new string[] { "Minutes", "Seconds" })]
    public static readonly Translation<int, int> VBSStateIdle = new Translation<int, int>("<#c$vbs_idle$>Idle: {0}:{1}</color>", TranslationFlags.NoColorOptimization, arg1Fmt: "D2");
    [TranslationData(SectionVBS)]
    public static readonly Translation VBSDelayStaging = new Translation("<#c$vbs_delay$>Locked Until Start</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionVBS, FormattingDescriptions = new string[] { "Minutes", "Seconds" })]
    public static readonly Translation<int, int> VBSDelayTime = new Translation<int, int>("<#c$vbs_delay$>Locked: {0}:{1}</color>", TranslationFlags.NoColorOptimization, arg1Fmt: "D2");
    [TranslationData(SectionVBS)]
    public static readonly Translation<int> VBSDelayTeammates = new Translation<int>("<#c$vbs_delay$>Locked until {0}v{0}</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionVBS)]
    public static readonly Translation<Flag> VBSDelayCaptureFlag = new Translation<Flag>("<#c$vbs_delay$>Capture {0}</color>", TranslationFlags.NoColorOptimization | TranslationFlags.PerTeamTranslation, Flag.SHORT_NAME_DISCOVER_FORMAT);
    [TranslationData(SectionVBS)]
    public static readonly Translation<Flag> VBSDelayLoseFlag = new Translation<Flag>("<#c$vbs_delay$>Lose {0}</color>", TranslationFlags.NoColorOptimization | TranslationFlags.PerTeamTranslation, Flag.SHORT_NAME_DISCOVER_FORMAT);
    [TranslationData(SectionVBS)]
    public static readonly Translation<int> VBSDelayLoseFlagMultiple = new Translation<int>("<#c$vbs_delay$>Lose {0} more flags.</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionVBS)]
    public static readonly Translation<int> VBSDelayCaptureFlagMultiple = new Translation<int>("<#c$vbs_delay$>Capture {0} more flags.</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionVBS)]
    public static readonly Translation<Cache> VBSDelayAttackCache = new Translation<Cache>("<#c$vbs_delay$>Destroy {0}</color>", TranslationFlags.NoColorOptimization, FOB.FormatLocationName);
    [TranslationData(SectionVBS)]
    public static readonly Translation VBSDelayAttackCacheUnknown = new Translation("<#c$vbs_delay$>Destroy Next Cache</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionVBS)]
    public static readonly Translation<int> VBSDelayAttackCacheMultiple = new Translation<int>("<#c$vbs_delay$>Destroy {0} more caches.</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionVBS)]
    public static readonly Translation<Cache> VBSDelayDefendCache = new Translation<Cache>("<#c$vbs_delay$>Lose {0}</color>", TranslationFlags.NoColorOptimization, FOB.FormatLocationName);
    [TranslationData(SectionVBS)]
    public static readonly Translation VBSDelayDefendCacheUnknown = new Translation("<#c$vbs_delay$>Lose Next Cache</color>", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionVBS)]
    public static readonly Translation<int> VBSDelayDefendCacheMultiple = new Translation<int>("<#c$vbs_delay$>Lose {0} more caches.</color>", TranslationFlags.NoColorOptimization);
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
    [TranslationData(SectionReload, IsPrioritizedTranslation = false)]
    public static readonly Translation ReloadedAll = new Translation("<#e6e3d5>Reloaded all Uncreated Warfare components.");
    [TranslationData(SectionReload, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> ReloadedTranslations = new Translation<int>("<#e6e3d5>Reloaded all translation files. {0} total translation points.");
    [TranslationData(SectionReload, IsPrioritizedTranslation = false)]
    public static readonly Translation ReloadedFlags = new Translation("<#e6e3d5>Reloaded flag data.");
    [TranslationData(SectionReload, IsPrioritizedTranslation = false)]
    public static readonly Translation ReloadFlagsInvalidGamemode = new Translation("<#ff8c69>You must be on a flag gamemode to use this command!");
    [TranslationData(SectionReload, IsPrioritizedTranslation = false)]
    public static readonly Translation ReloadedPermissions = new Translation("<#e6e3d5>Reloaded the permission saver file.");
    [TranslationData(SectionReload, IsPrioritizedTranslation = false)]
    public static readonly Translation ReloadedTCP = new Translation("<#e6e3d5>Tried to close any existing TCP connection to homebase and re-open it.");
    [TranslationData(SectionReload, IsPrioritizedTranslation = false)]
    public static readonly Translation ReloadedSQL = new Translation("<#e6e3d5>Reopened the MySql Connection.");
    [TranslationData(SectionReload, IsPrioritizedTranslation = false)]
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
    public static readonly Translation PhaseBriefing                      = new Translation("BRIEFING PHASE", TranslationFlags.TMProUI);
    [TranslationData(SectionPhases)]
    public static readonly Translation PhasePreparation                   = new Translation("PREPARATION PHASE", TranslationFlags.TMProUI);
    [TranslationData(SectionPhases)]
    public static readonly Translation PhaseBreifingInvasionAttack        = new Translation("BRIEFING PHASE", TranslationFlags.TMProUI);
    [TranslationData(SectionPhases)]
    public static readonly Translation<Flag> PhaseBreifingInvasionDefense = new Translation<Flag>("PREPARATION PHASE\nFORTIFY {0}", TranslationFlags.TMProUI, Flag.COLOR_SHORT_NAME_FORMAT);
    #endregion

    #region XP Toasts
    private const string SectionXPCreditsToast = "XP / Credit / Supply Toast Messages";
    [TranslationData(SectionXPCreditsToast, "XP or credits given from the console.")]
    public static readonly Translation XPToastFromOperator = new Translation("FROM OPERATOR", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast, "XP or credits given by an admin.")]
    public static readonly Translation XPToastFromPlayer = new Translation("FROM ADMIN", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastHealedTeammate = new Translation("HEALED TEAMMATE", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastEnemyInjured = new Translation("<color=#e3e3e3>DOWNED</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFriendlyInjured = new Translation("<color=#e3e3e3>DOWNED FRIENDLY</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastEnemyKilled = new Translation("KILLED ENEMY", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastKillAssist = new Translation("ASSIST", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastKillVehicleAssist = new Translation("VEHICLE ASSIST", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastKillDriverAssist = new Translation("DRIVER ASSIST", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastSpotterAssist = new Translation("SPOTTER", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFriendlyKilled = new Translation("TEAMKILLED", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastSuicide = new Translation("SUICIDE", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFOBDestroyed = new Translation("FOB DESTROYED", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFriendlyFOBDestroyed = new Translation("FRIENDLY FOB DESTROYED", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastBunkerDestroyed = new Translation("BUNKER DESTROYED", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFriendlyBunkerDestroyed = new Translation("FRIENDLY BUNKER DESTROYED", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFOBUsed = new Translation("FOB IN USE", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastSuppliesUnloaded = new Translation("RESUPPLIED FOB", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastResuppliedTeammate = new Translation("RESUPPLIED TEAMMATE", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastRepairedVehicle = new Translation("REPAIRED VEHICLE", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFOBRepairedVehicle = new Translation("FOB REPAIRED VEHICLE", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation<VehicleType> XPToastVehicleDestroyed = new Translation<VehicleType>("{0} DESTROYED", TranslationFlags.TMProUI, FormatUppercase);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation<VehicleType> XPToastAircraftDestroyed = new Translation<VehicleType>("{0} SHOT DOWN", TranslationFlags.TMProUI, FormatUppercase);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation<VehicleType> XPToastFriendlyVehicleDestroyed = new Translation<VehicleType>("FRIENDLY {0} DESTROYED", TranslationFlags.TMProUI, FormatUppercase);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation<VehicleType> XPToastFriendlyAircraftDestroyed = new Translation<VehicleType>("FRIENDLY {0} SHOT DOWN", TranslationFlags.TMProUI, FormatUppercase);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastTransportingPlayers = new Translation("TRANSPORTING PLAYERS", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastAceArmorRefund = new Translation("ACE ARMOR SHARE", TranslationFlags.TMProUI);

    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFlagCaptured = new Translation("FLAG CAPTURED", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFlagNeutralized = new Translation("FLAG NEUTRALIZED", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFlagAttackTick = new Translation("ATTACK", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFlagDefenseTick = new Translation("DEFENSE", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastCacheDestroyed = new Translation("CACHE DESTROYED", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastFriendlyCacheDestroyed = new Translation("FRIENDLY CACHE DESTROYED", TranslationFlags.TMProUI);

    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastSquadBonus = new Translation("SQUAD BONUS", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation XPToastOnDuty = new Translation("ON DUTY", TranslationFlags.TMProUI);

    [TranslationData(SectionXPCreditsToast, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> FOBToastGainBuild = new Translation<int>("<color=#c$build$>+{0} BUILD</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> FOBToastLoseBuild = new Translation<int>("<color=#c$build$>-{0} BUILD</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> FOBToastGainAmmo = new Translation<int>("<color=#c$ammo$>+{0} AMMO</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> FOBToastLoseAmmo = new Translation<int>("<color=#c$ammo$>-{0} AMMO</color>", TranslationFlags.TMProUI);

    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation FOBResourceToastLoadSupplies = new Translation("LOAD SUPPLIES");
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation FOBResourceToastRearmVehicle = new Translation("REARM VEHICLE");
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation FOBResourceToastRearmPlayer = new Translation("REARM");
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation FOBResourceToastRepairVehicle = new Translation("REPAIR");

    [TranslationData(SectionXPCreditsToast, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> XPToastGainXP = new Translation<int>("+{0} XP", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> XPToastLoseXP = new Translation<int>("-{0} XP", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> XPToastGainCredits = new Translation<int>("+{0} <color=#c$credits$>C</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> XPToastPurchaseCredits = new Translation<int>("-{0} <color=#c$credits$>C</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> XPToastLoseCredits = new Translation<int>("-{0} <color=#d69898>C</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation ToastPromoted = new Translation("YOU HAVE BEEN <color=#ffbd8a>PROMOTED</color> TO", TranslationFlags.TMProUI);
    [TranslationData(SectionXPCreditsToast)]
    public static readonly Translation ToastDemoted = new Translation("YOU HAVE BEEN <color=#e86868>DEMOTED</color> TO", TranslationFlags.TMProUI);
    #endregion

    #region Injured UI
    [TranslationData(SectionRevives)]
    public static readonly Translation InjuredUIHeader = new Translation("You are injured", TranslationFlags.TMProUI);
    [TranslationData(SectionRevives)]
    public static readonly Translation InjuredUIGiveUp = new Translation("Press <color=#cecece><b><plugin_2/></b></color> to give up.", TranslationFlags.TMProUI);
    [TranslationData(SectionRevives)]
    public static readonly Translation InjuredUIGiveUpChat = new Translation("<#ff8c69>You were injured, press <color=#cedcde><plugin_2/></color> to give up.");
    #endregion

    #region Insurgency
    private const string SectionInsurgency = "Gamemode Insurgency";
    [TranslationData(SectionInsurgency)]
    public static readonly Translation InsurgencyListHeader = new Translation("Caches", TranslationFlags.UnityUI);
    [TranslationData(SectionInsurgency)]
    public static readonly Translation InsurgencyUnknownCacheAttack = new Translation("<color=#696969>Undiscovered</color>", TranslationFlags.UnityUI);
    [TranslationData(SectionInsurgency)]
    public static readonly Translation InsurgencyUnknownCacheDefense = new Translation("<color=#696969>Unknown</color>", TranslationFlags.UnityUI);
    [TranslationData(SectionInsurgency)]
    public static readonly Translation InsurgencyDestroyedCacheAttack = new Translation("<color=#5a6e5c>Destroyed</color>", TranslationFlags.UnityUI);
    [TranslationData(SectionInsurgency)]
    public static readonly Translation InsurgencyDestroyedCacheDefense = new Translation("<color=#6b5858>Lost</color>", TranslationFlags.UnityUI);
    [TranslationData(SectionInsurgency, IsPrioritizedTranslation = false)]
    public static readonly Translation<Cache, Cache> InsurgencyCacheAttack = new Translation<Cache, Cache>("<color=#ff7661>{0}</color> <color=#c2c2c2>{1}</color>", TranslationFlags.UnityUI, FOB.FormatName, FOB.FormatLocationName);
    [TranslationData(SectionInsurgency, IsPrioritizedTranslation = false)]
    public static readonly Translation<Cache, Cache> InsurgencyCacheDefense = new Translation<Cache, Cache>("<color=#555bcf>{0}</color> <color=#c2c2c2>{1}</color>", TranslationFlags.UnityUI, FOB.FormatName, FOB.FormatLocationName);
    [TranslationData(SectionInsurgency, IsPrioritizedTranslation = false)]
    public static readonly Translation<Cache, Cache> InsurgencyCacheDefenseUndiscovered = new Translation<Cache, Cache>("<color=#b780d9>{0}</color> <color=#c2c2c2>{1}</color>", TranslationFlags.UnityUI, FOB.FormatName, FOB.FormatLocationName);
    #endregion

    #region Hardpoint
    private const string SectionHardpoint = "Gamemode Hardpoint";
    [TranslationData(SectionHardpoint)]
    public static readonly Translation<IObjective, float> HardpointFirstObjective = new Translation<IObjective, float>("Hold {0} to win! A new objective will be chosen in <#cedcde>{1}</color>.", Flag.COLOR_NAME_FORMAT, FormatTimeLong);
    [TranslationData(SectionHardpoint)]
    public static readonly Translation<IObjective, float> HardpointObjectiveChanged = new Translation<IObjective, float>("New objective: {0}! The next objective will be chosen in <#cedcde>{1}</color>.", Flag.COLOR_NAME_FORMAT, FormatTimeLong);
    [TranslationData(SectionHardpoint)]
    public static readonly Translation<IObjective, FactionInfo> HardpointObjectiveStateCaptured = new Translation<IObjective, FactionInfo>("{0} is being held by {1}!", Flag.COLOR_NAME_FORMAT, FactionInfo.FormatColorShortName);
    [TranslationData(SectionHardpoint)]
    public static readonly Translation<IObjective, FactionInfo> HardpointObjectiveStateLost = new Translation<IObjective, FactionInfo>("{0} is no longer being held by {1}!", Flag.COLOR_NAME_FORMAT, FactionInfo.FormatColorShortName);
    [TranslationData(SectionHardpoint)]
    public static readonly Translation<IObjective> HardpointObjectiveStateLostContest = new Translation<IObjective>("{0} is no longer <#c$contested$>contested</color>!", Flag.COLOR_NAME_FORMAT);
    [TranslationData(SectionHardpoint)]
    public static readonly Translation<IObjective> HardpointObjectiveStateContested = new Translation<IObjective>("{0} is <#c$contested$>contested</color>!", Flag.COLOR_NAME_FORMAT);
    #endregion

    #region Report Command
    private const string SectionReport = "Reporting";
    [TranslationData(SectionReport, Description = "Possible report arguments, do not translate the reasons.")]
    public static readonly Translation ReportReasons = new Translation("<#9cffb3>Report reasons: -none-, \"chat abuse\", \"voice chat abuse\", \"soloing vehicles\", \"wasteing assets\", \"teamkilling\", \"fob greifing\", \"cheating\".");
    [TranslationData(SectionReport)]
    public static readonly Translation ReportPlayerNotFound = new Translation("<#9cffb3>Unable to find a player with that name, you can use their <color=#ffffff>Steam64 ID</color> instead, as names are only stored until they've been offline for 20 minutes.");
    [TranslationData(SectionReport)]
    public static readonly Translation ReportUnknownError = new Translation("<#9cffb3>Unable to generate a report for an unknown reason, check your syntax again with <color=#ffffff>/report help</color>.");
    [TranslationData(SectionReport)]
    public static readonly Translation<IPlayer, string, string> ReportSuccessMessage = new Translation<IPlayer, string, string>("<#c480d9>Successfully reported {0} for <#fff>{1}</color> as a <#00ffff>{2}</color> report. If possible please post evidence in <#ffffff>#player-reports</color> in our <#7483c4>Discord</color> server.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionReport)]
    public static readonly Translation<IPlayer, string, string> ReportSuccessMessage1 = new Translation<IPlayer, string, string>("<#c480d9>Successfully reported {0} for <#fff>{1}</color> as a <#00ffff>{2}</color> report.", UCPlayer.FormatCharacterName);
    [TranslationData(SectionReport)]
    public static readonly Translation ReportSuccessMessage2 = new Translation("<#c480d9>If possible please post evidence in <#ffffff>#player-reports</color> in our <#7483c4>Discord</color> server.");
    [TranslationData(SectionReport)]
    public static readonly Translation<IPlayer, IPlayer, string, string> ReportNotifyAdmin = new Translation<IPlayer, IPlayer, string, string>("<#c480d9>{0} reported {1} for <#fff>{2}</color> as a <#00ffff>{3}</color> report. Check <#c480d9>#player-reports</color> for more information.", UCPlayer.FormatCharacterName, UCPlayer.FormatCharacterName);
    [TranslationData(SectionReport)]
    public static readonly Translation<string> ReportNotifyViolatorToast = new Translation<string>("<#c480d9>You've been reported for <#00ffff>{0}</color>.\nCheck <#fff>#player-reports</color> in our <#7483c4>Discord</color> (/discord) for more information and to defend yourself.", TranslationFlags.TMProUI);
    [TranslationData(SectionReport)]
    public static readonly Translation ReportNotifyViolatorToastTitle = new Translation("You Were Reported", TranslationFlags.TMProUI);
    [TranslationData(SectionReport)]
    public static readonly Translation<string, string> ReportNotifyViolatorMessage = new Translation<string, string>("<#c480d9>You've been reported for <#00ffff>{0} - {1}</color>. Check <#fff>#player-reports</color> in our <#7483c4>Discord</color> (/discord) for more information and to defend yourself.");
    [TranslationData(SectionReport)]
    public static readonly Translation<string, string> ReportNotifyViolatorMessage1 = new Translation<string, string>("<#c480d9>You've been reported for <#00ffff>{0} - {1}</color>.");
    [TranslationData(SectionReport)]
    public static readonly Translation ReportNotifyViolatorMessage2 = new Translation("<#c480d9>Check <#fff>#player-reports</color> in our <#7483c4>Discord</color> (/discord) for more information and to defend yourself.");
    [TranslationData(SectionReport)]
    public static readonly Translation<IPlayer> ReportCooldown = new Translation<IPlayer>("<#9cffb3>You've already reported {0} in the past hour.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionReport)]
    public static readonly Translation<ulong, IPlayer> ReportConfirm = new Translation<ulong, IPlayer>("<#c480d9>Did you mean to report {1} <i><#444>{0}</color></i>? Type <#ff8c69>/confirm</color> to continue.", arg1Fmt: UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionReport)]
    public static readonly Translation ReportCancelled = new Translation("<#ff8c69>You didn't confirm your report in time.");
    [TranslationData(SectionReport)]
    public static readonly Translation ReportNotConnected = new Translation("<#ff8c69>The report system is not available right now, please try again later.");
    #endregion

    #region Abandon
    private const string SectionAbandon = "Abandon";
    [TranslationData(SectionAbandon, "Sent when a player isn't looking at a vehicle when doing /abandon.")]
    public static readonly Translation AbandonNoTarget = new Translation("<#ff8c69>You must be looking at a vehicle.");
    [TranslationData(SectionAbandon, "Sent when a player is looking at a vehicle they didn't request.")]
    public static readonly Translation<InteractableVehicle> AbandonNotOwned = new Translation<InteractableVehicle>("<#ff8c69>You did not request that {0}.");
    [TranslationData(SectionAbandon, "Sent when a player does /abandon while not in main.")]
    public static readonly Translation AbandonNotInMain = new Translation("<#ff8c69>You must be in main to abandon a vehicle.");
    [TranslationData(SectionAbandon, "Sent when a player tries to abandon a damaged vehicle.")]
    public static readonly Translation<InteractableVehicle> AbandonDamaged = new Translation<InteractableVehicle>("<#ff8c69>Your <#cedcde>{0}</color> is damaged, repair it before returning it to the yard.");
    [TranslationData(SectionAbandon, "Sent when a player tries to abandon a vehicle with low fuel.")]
    public static readonly Translation<InteractableVehicle> AbandonNeedsFuel = new Translation<InteractableVehicle>("<#ff8c69>Your <#cedcde>{0}</color> is not fully fueled.");
    [TranslationData(SectionAbandon, "Sent when a player tries to abandon a vehicle and all the bays for that vehicle are already full, theoretically should never happen.")]
    public static readonly Translation<InteractableVehicle> AbandonNoSpace = new Translation<InteractableVehicle>("<#ff8c69>There's no space for <#cedcde>{0}</color> in the yard.", FormatPlural);
    [TranslationData(SectionAbandon, "Sent when a player tries to abandon a vehicle that isn't allowed to be abandoned.")]
    public static readonly Translation<InteractableVehicle> AbandonNotAllowed = new Translation<InteractableVehicle>("<#ff8c69><#cedcde>{0}</color> can not be abandoned.", FormatPlural);
    [TranslationData(SectionAbandon, "Sent when a player abandons a vehicle.")]
    public static readonly Translation<InteractableVehicle> AbandonSuccess = new Translation<InteractableVehicle>("<#a0ad8e>Your <#cedcde>{0}</color> was returned to the yard.");
    [TranslationData(SectionAbandon, "Credits toast for returning a vehicle soon after requesting it.")]
    public static readonly Translation AbandonCompensationToast = new Translation("RETURNED VEHICLE", TranslationFlags.TMProUI);
    [TranslationData(SectionAbandon, "Credits toast for returning a vehicle soon after requesting it, but not getting anything because the vehicle was transferred.")]
    public static readonly Translation AbandonCompensationToastTransferred = new Translation("+0 <color=#c$credits$>C</color> [GIVEN]\nRETURNED VEHICLE", TranslationFlags.TMProUI);
    #endregion
    
    #region DailyQuests
    private const string SectionDailyQuests = "Daily Quests";
    [TranslationData(SectionDailyQuests, "Sent when new daily quests are put into action.")]
    public static readonly Translation<DateTime> DailyQuestsNewIndex = new Translation<DateTime>("<#66ccff>New daily quests have been generated! They will be active until <#cedcde>{0}</color> UTC.", "G");
    [TranslationData(SectionDailyQuests, "Sent 1 hour before new daily quests are put into action.")]
    public static readonly Translation DailyQuestsOneHourRemaining = new Translation("<#66ccff>You have one hour until new daily quests will be generated!");
    #endregion

    #region Tips
    private const string SectionTips = "In-game Tips";
    [TranslationData(SectionTips)]
    public static readonly Translation<IPlayer> TipUAVRequest = new Translation<IPlayer>("<#d9c69a>{0} Requested a UAV!", TranslationFlags.TMProUI, UCPlayer.FormatColoredNickName);
    [TranslationData(SectionTips)]
    public static readonly Translation TipPlaceRadio = new Translation("Place a <#ababab>FOB RADIO</color>.", TranslationFlags.TMProUI);
    [TranslationData(SectionTips)]
    public static readonly Translation TipPlaceBunker = new Translation("Build a <#a5c3d9>FOB BUNKER</color> so that your team can spawn.", TranslationFlags.TMProUI);
    [TranslationData(SectionTips)]
    public static readonly Translation TipUnloadSupplies = new Translation("<#d9c69a>DROP SUPPLIES</color> onto the FOB.", TranslationFlags.TMProUI);
    [TranslationData(SectionTips)]
    public static readonly Translation<IPlayer> TipHelpBuild = new Translation<IPlayer>("<#d9c69a>{0} needs help building!", TranslationFlags.TMProUI, UCPlayer.FormatColoredNickName);
    [TranslationData(SectionTips)]
    public static readonly Translation<VehicleType> TipLogisticsVehicleResupplied = new Translation<VehicleType>("Your <#009933>{0}</color> has been auto resupplied.", TranslationFlags.TMProUI, FormatUppercase);
    [TranslationData(SectionTips)]
    public static readonly Translation TipActionMenu = new Translation("Press <#a5c3d9><plugin_1/></color> for field actions", TranslationFlags.TMProUI);
    [TranslationData(SectionTips)]
    public static readonly Translation TipActionMenuSl = new Translation("Press <#a5c3d9><plugin_1/></color> for <#85c996>squad actions</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionTips)]
    public static readonly Translation TipCallMedic = new Translation("You are hurt. Press <#d9a5bb><plugin_1/></color> to call for a medic.", TranslationFlags.TMProUI);
    #endregion

    #region Zone Command
    private const string SectionZones = "Zone Command";
    [TranslationData(SectionZones)]
    public static readonly Translation ZoneNoResultsLocation = new Translation("<#ff8c69>You aren't in any existing zone.");
    [TranslationData(SectionZones)]
    public static readonly Translation ZoneNoResultsName = new Translation("<#ff8c69>Couldn't find a zone by that name.");
    [TranslationData(SectionZones)]
    public static readonly Translation ZoneNoResults = new Translation("<#ff8c69>You must be in a zone or specify a valid zone name to use this command.");
    [TranslationData(SectionZones, IsPrioritizedTranslation = false)]
    public static readonly Translation<Zone> ZoneGoSuccess = new Translation<Zone>("<#e6e3d5>Teleported to <#5a6e5c>{0}</color>.", Flag.NAME_FORMAT);
    [TranslationData(SectionZones, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> ZoneGoSuccessRaw = new Translation<string>("<#e6e3d5>Teleported to <#cedcde>{0}</color>.");
    [TranslationData(SectionZones, IsPrioritizedTranslation = false)]
    public static readonly Translation<GridLocation> ZoneGoSuccessGridLocation = new Translation<GridLocation>("<#e6e3d5>Teleported to <#ff8c69>{0}</color>.", Flag.NAME_FORMAT);
    [TranslationData(SectionZones)]
    public static readonly Translation<int, Zone> ZoneVisualizeSuccess = new Translation<int, Zone>("<#e6e3d5>Spawned {0} particles around <color=#cedcde>{1}</color>.", arg1Fmt: Flag.NAME_FORMAT);

    // Zone > Delete
    private const string SectionZonesDelete = SectionZones + " / Delete";
    [TranslationData(SectionZonesDelete, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneDeleteZoneNotInZone = new Translation("<#ff8c69>You must be standing in 1 zone (not 0 or multiple). Alternatively, provide a zone name as another argument.");
    [TranslationData(SectionZonesDelete, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> ZoneDeleteZoneNotFound = new Translation<string>("<#ff8c69>Failed to find a zone named \"{0}\".");
    [TranslationData(SectionZonesDelete, IsPrioritizedTranslation = false)]
    public static readonly Translation<Zone> ZoneDeleteDidNotConfirm = new Translation<Zone>("<#ff8c69>{0} was not deleted, you did not <#ff8c69>confirm</color>.");
    [TranslationData(SectionZonesDelete, IsPrioritizedTranslation = false)]
    public static readonly Translation<Zone> ZoneDeleteZoneConfirm = new Translation<Zone>("<#a5c3d9>Are you sure you want to delete <#666>{0}</color>? Type <#ff8c69>/confirm</color> to continue.", Flag.NAME_FORMAT);
    [TranslationData(SectionZonesDelete, IsPrioritizedTranslation = false)]
    public static readonly Translation<Zone> ZoneDeleteZoneSuccess = new Translation<Zone>("<#e6e3d5>Deleted <#666>{0}</color>.", Flag.NAME_FORMAT);
    [TranslationData(SectionZonesDelete, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneDeleteEditingZoneDeleted = new Translation("<#ff8c69>Someone deleted the zone you're working on, saving this will create a new one.");

    // Zone > Create
    private const string SectionZonesCreate = SectionZones + " / Create";
    [TranslationData(SectionZonesCreate, IsPrioritizedTranslation = false)]
    public static readonly Translation<string, ZoneType> ZoneCreated = new Translation<string, ZoneType>("<#e6e3d5>Started zone builder for {0}, a {1} zone.", Flag.NAME_FORMAT);
    [TranslationData(SectionZonesCreate, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> ZoneCreateNameTaken = new Translation<string>("<#ff8c69>The name \"{0}\" is already in use by another zone.");
    [TranslationData(SectionZonesCreate, IsPrioritizedTranslation = false)]
    public static readonly Translation<string, IPlayer> ZoneCreateNameTakenEditing = new Translation<string, IPlayer>("<#ff8c69>The name \"{0}\" is already in use by another zone being created by {1}.", arg1Fmt: UCPlayer.FormatColoredCharacterName);

    // Zone > Edit
    private const string SectionZonesEdit = SectionZones + " / Edit";
    [TranslationData(SectionZonesEdit, IsPrioritizedTranslation = false)]
    public static readonly Translation<int> ZoneEditPointNotDefined = new Translation<int>("<#ff8c69>Point <#ff9999>#{0}</color> is not defined.");
    [TranslationData(SectionZonesEdit, IsPrioritizedTranslation = false)]
    public static readonly Translation<Vector2> ZoneEditPointNotNearby = new Translation<Vector2>("<#ff8c69>There is no point near <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > Existing
    private const string SectionZonesEditExisting = SectionZonesEdit + " / Existing";
    [TranslationData(SectionZonesEditExisting, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditExistingInvalid = new Translation("<#ff8c69>Edit existing zone requires the zone name as a parameter. Alternatively stand in the zone (without overlapping another).");
    [TranslationData(SectionZonesEditExisting, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditExistingInProgress = new Translation("<#ff8c69>Cancel or finalize the zone you're currently editing first.");
    [TranslationData(SectionZonesEditExisting, IsPrioritizedTranslation = false)]
    public static readonly Translation<string, ZoneType> ZoneEditExistingSuccess = new Translation<string, ZoneType>("<#e6e3d5>Started editing zone <#fff>{0}</color>, a <#ff9999>{1}</color> zone.");

    // Zone > Edit > Finalize
    private const string SectionZonesEditFinalize = SectionZonesEdit + " / Finalize";
    [TranslationData(SectionZonesEditFinalize, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditNotStarted = new Translation("<#ff8c69>Start creating a zone with <#fff>/zone create <polygon|rectangle|circle> <name></color>.");
    [TranslationData(SectionZonesEditFinalize, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditFinalizeExists = new Translation("<#ff8c69>There's already a zone saved with that name.");
    [TranslationData(SectionZonesEditFinalize, IsPrioritizedTranslation = false)]
    public static readonly Translation<Zone> ZoneEditFinalizeSuccess = new Translation<Zone>("<#e6e3d5>Successfully finalized and saved {0}.", Flag.NAME_FORMAT);
    [TranslationData(SectionZonesEditFinalize, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> ZoneEditFinalizeFailure = new Translation<string>("<#ff8c69>The provided zone data was invalid because: <#fff>{0}</color>.");
    [TranslationData(SectionZonesEditFinalize, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditFinalizeUseCaseUnset = new Translation("<#ff8c69>Before saving you must set a use case with /zone edit use case <type>: \"flag\", \"lobby\", \"t1_main\", \"t2_main\", \"t1_amc\", or \"t2_amc\".");
    [TranslationData(SectionZonesEditFinalize, IsPrioritizedTranslation = false)]
    public static readonly Translation<Zone> ZoneEditFinalizeOverwrote = new Translation<Zone>("<#e6e3d5>Successfully overwrote <#fff>{0}</color>.", Flag.NAME_FORMAT);

    // Zone > Edit > Cancel
    private const string SectionZonesEditCancel = SectionZonesEdit + " / Cancel";
    [TranslationData(SectionZonesEditCancel, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> ZoneEditCancelled = new Translation<string>("<#e6e3d5>Successfully cancelled making <#fff>{0}</color>.");

    // Zone > Edit > Type
    private const string SectionZonesEditType = SectionZonesEdit + " / Type";
    [TranslationData(SectionZonesEditType, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditTypeInvlaid = new Translation("<#ff8c69>Type must be rectangle, circle, or polygon.");
    [TranslationData(SectionZonesEditType, IsPrioritizedTranslation = false)]
    public static readonly Translation<ZoneType> ZoneEditTypeAlreadySet = new Translation<ZoneType>("<#ff8c69>This zone is already a <#ff9999>{0}</color>.");
    [TranslationData(SectionZonesEditType, IsPrioritizedTranslation = false)]
    public static readonly Translation<ZoneType> ZoneEditTypeSuccess = new Translation<ZoneType>("<#ff8c69>Set type to <#ff9999>{0}</color>.");

    // Zone > Edit > Max-Height
    private const string SectionZonesEditMaxHeight = SectionZonesEdit + " / Max-Height";
    [TranslationData(SectionZonesEditMaxHeight, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditMaxHeightInvalid = new Translation("<#ff8c69>Maximum Height must be a decimal or whole number, or leave it blank to use the player's current height.");
    [TranslationData(SectionZonesEditMaxHeight, IsPrioritizedTranslation = false)]
    public static readonly Translation<float> ZoneEditMaxHeightSuccess = new Translation<float>("<#e6e3d5>Set maximum height to <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > Min-Height
    private const string SectionZonesEditMinHeight = SectionZonesEdit + " / Min-Height";
    [TranslationData(SectionZonesEditMinHeight, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditMinHeightInvalid = new Translation("<#ff8c69>Minimum Height must be a decimal or whole number, or leave it blank to use the player's current height.");
    [TranslationData(SectionZonesEditMinHeight, IsPrioritizedTranslation = false)]
    public static readonly Translation<float> ZoneEditMinHeightSuccess = new Translation<float>("<#e6e3d5>Set minimum height to <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > Add-Point
    private const string SectionZonesEditAddPoint = SectionZonesEdit + " / Add Point";
    [TranslationData(SectionZonesEditAddPoint, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditAddPointInvalid = new Translation("<#ff8c69>Adding a point requires either: blank (appends, current pos), <index> (current pos), <x> <z> (appends), or <index> <x> <z> parameters.");
    [TranslationData(SectionZonesEditAddPoint, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, Vector2> ZoneEditAddPointSuccess = new Translation<int, Vector2>("<#e6e3d5>Added point <#ff9999>#{0}</color> at <#ff9999>{1}</color>.", arg1Fmt: "0.##");

    // Zone > Edit > Delete-Point
    private const string SectionZonesEditDeletePoint = SectionZonesEdit + " / Delete Point";
    [TranslationData(SectionZonesEditDeletePoint, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditDeletePointInvalid = new Translation("<#ff8c69>Deleting a point requires either: nearby X and Z parameters, a point number, or leave them blank to use the player's current position");
    [TranslationData(SectionZonesEditDeletePoint, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, Vector2> ZoneEditDeletePointSuccess = new Translation<int, Vector2>("<#e6e3d5>Removed point <#ff9999>#{0}</color> at <#ff9999>{1}</color>.", arg1Fmt: "0.##");

    // Zone > Edit > Set-Point
    private const string SectionZonesEditSetPoint = SectionZonesEdit + " / Set Point";
    [TranslationData(SectionZonesEditSetPoint, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSetPointInvalid = new Translation("<#ff8c69>Moving a point requires either: blank (move nearby closer), <nearby src x> <nearby src z> <dest x> <dest z>, <pt num> (destination is player position), <pt num> <dest x> <dest z>, or <nearby src x> <nearby src z> (destination is nearby player).");
    [TranslationData(SectionZonesEditSetPoint, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, Vector2, Vector2> ZoneEditSetPointSuccess = new Translation<int, Vector2, Vector2>("<#e6e3d5>Set position of point <#ff9999>#{0}</color> to <#ff9999>{1}</color> (from <#cdcedc>{2}</color>).", arg1Fmt: "0.##", arg2Fmt: "0.##");

    // Zone > Edit > Order-Point
    private const string SectionZonesEditOrderPoint = SectionZonesEdit + " / Order Point";
    [TranslationData(SectionZonesEditOrderPoint, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditOrderPointInvalid = new Translation("<#ff8c69>Ordering a point requires either: <from-index> <to-index>, <to-index> (from is nearby player), or <src x> <src z> <to-index>.");
    [TranslationData(SectionZonesEditOrderPoint, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, int> ZoneEditOrderPointSuccess = new Translation<int, int>("<#e6e3d5>Moved point <#ff9999>#{0}</color> to index <#ff9999>#{1}</color>.");

    // Zone > Edit > Clear-Points
    private const string SectionZonesEditClearPoints = SectionZonesEdit + " / Clear Points";
    [TranslationData(SectionZonesEditClearPoints, FormattingDescriptions = new string[] { "Amount of points restored.", "\"s\" unless {0} == 1." }, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, string> ZoneEditUnclearedSuccess = new Translation<int, string>("<#e6e3d5>Restored {0} point{1}.");
    [TranslationData(SectionZonesEditClearPoints, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditClearSuccess = new Translation("<#e6e3d5>Cleared all polygon points.");

    // Zone > Edit > Radius
    private const string SectionZonesEditRadius = SectionZonesEdit + " / Radius";
    [TranslationData(SectionZonesEditRadius, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditRadiusInvalid = new Translation("<#ff8c69>Radius must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.");
    [TranslationData(SectionZonesEditRadius, IsPrioritizedTranslation = false)]
    public static readonly Translation<float> ZoneEditRadiusSuccess = new Translation<float>("<#e6e3d5>Set radius to <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > See Adjacencies
    private const string SectionZonesEditSeeAdjacencies = SectionZonesEdit + " / See Adjacencies";
    [TranslationData(SectionZonesEditSeeAdjacencies, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSeeAdjacenciesNone = new Translation("<#ff8c69>This zone has no adjacencies.");
    [TranslationData(SectionZonesEditSeeAdjacencies, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSeeAdjacenciesNoneWithAdjacents = new Translation("<#e6e3d5>This zone has no adjacencies, but is adjacent to:");
    [TranslationData(SectionZonesEditSeeAdjacencies, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSeeAdjacencies = new Translation("<#e6e3d5>This zone has the following adjacencies:");
    [TranslationData(SectionZonesEditSeeAdjacencies, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSeeAdjacents = new Translation("<#e6e3d5>It's adjacent to:");

    // Zone > Edit > Add Adjacency
    private const string SectionZonesEditAddAdjacency = SectionZonesEdit + " / Add Adjacency";
    [TranslationData(SectionZonesEditAddAdjacency, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditAddAdjacencyInvalid = new Translation("<#ff8c69>Adding an adjacency requires either: <zone> or <zone> <weight (float)> parameters.");
    [TranslationData(SectionZonesEditAddAdjacency, IsPrioritizedTranslation = false)]
    public static readonly Translation<Zone> ZoneEditAddAdjacencyAlreadyAdded = new Translation<Zone>("<#ff8c69>This zone already has <#ff9999>{0}</color> as an adjacency.");
    [TranslationData(SectionZonesEditAddAdjacency, IsPrioritizedTranslation = false)]
    public static readonly Translation<Zone, float> ZoneEditAddAdjacencySuccess = new Translation<Zone, float>("<#e6e3d5>Added adjacency to <#ff9999>{0}</color> with a weight of <#ff9999>{1}</color>.", arg1Fmt: "0.##");

    // Zone > Edit > Delete Adjacency
    private const string SectionZonesEditRemoveAdjacency = SectionZonesEdit + " / Remove Adjacency";
    [TranslationData(SectionZonesEditRemoveAdjacency, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditDeleteAdjacencyInvalid = new Translation("<#ff8c69>Deleting an adjacency requires a <zone> parameter.");
    [TranslationData(SectionZonesEditRemoveAdjacency, IsPrioritizedTranslation = false)]
    public static readonly Translation<Zone> ZoneEditDeleteAdjacencyNotFound = new Translation<Zone>("<#ff8c69>This zone is not adjacent to <#ff9999>{0}</color>.");
    [TranslationData(SectionZonesEditRemoveAdjacency, IsPrioritizedTranslation = false)]
    public static readonly Translation<Zone, float> ZoneEditDeleteAdjacencySuccess = new Translation<Zone, float>("<#e6e3d5>Removed adjacency to <#ff9999>{0}</color> with a weight of <#ff9999>{1}</color>.", arg1Fmt: "0.##");

    // Zone > Edit > Clear Adjacencies
    private const string SectionZonesEditClearAdjacencies = SectionZonesEdit + " / Clear Adjacencies";
    [TranslationData(SectionZonesEditClearAdjacencies, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditClearAdjacencyInvalid = new Translation("<#ff8c69>This zone has no adjacencies.");
    [TranslationData(SectionZonesEditClearAdjacencies, IsPrioritizedTranslation = false)]
    public static readonly Translation<Zone, float> ZoneEditClearAdjacenciesSuccess = new Translation<Zone, float>("<#e6e3d5>Removed adjacency to <#ff9999>{0}</color> with a weight of <#ff9999>{1}</color>.", arg1Fmt: "0.##");

    // Zone > Edit > Add Grid Object
    private const string SectionZonesEditAddGridObject = SectionZonesEdit + " / Add Grid Object";
    [TranslationData(SectionZonesEditAddGridObject, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditAddGridObjInvalid = new Translation("<#ff8c69>You must be looking at an interactable object.");
    [TranslationData(SectionZonesEditAddGridObject, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditAddGridObjAlreadyExists = new Translation("<#ff8c69>That object is already a grid object.");
    [TranslationData(SectionZonesEditAddGridObject, IsPrioritizedTranslation = false)]
    public static readonly Translation<ObjectAsset> ZoneEditAddGridObjSuccess = new Translation<ObjectAsset>("<#e6e3d5>Added <#ff9999>{0}</color> as a grid object.");
    [TranslationData(SectionZonesEditAddGridObject, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, string> ZoneEditAddGridObjAllSuccess = new Translation<int, string>("<#e6e3d5>Added <#ff9999>{0}</color> grid object{1}.");

    // Zone > Edit > Delete Grid Object
    private const string SectionZonesEditRemoveGridObject = SectionZonesEdit + " / Remove Grid Object";
    [TranslationData(SectionZonesEditRemoveGridObject, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditDelGridObjInvalid = new Translation("<#ff8c69>You must be looking at an interactable object.");
    [TranslationData(SectionZonesEditRemoveGridObject, IsPrioritizedTranslation = false)]
    public static readonly Translation<ObjectAsset> ZoneEditDelGridObjDoesntExist = new Translation<ObjectAsset>("<#e6e3d5>The object <#ff9999>{0}</color> was never added.");
    [TranslationData(SectionZonesEditRemoveGridObject, IsPrioritizedTranslation = false)]
    public static readonly Translation<ObjectAsset> ZoneEditDelGridObjSuccess = new Translation<ObjectAsset>("<#e6e3d5>Removed <#ff9999>{0}</color> as a grid object.");
    [TranslationData(SectionZonesEditRemoveGridObject, IsPrioritizedTranslation = false)]
    public static readonly Translation<int, string> ZoneEditDelGridObjAllSuccess = new Translation<int, string>("<#e6e3d5>Removed <#ff9999>{0}</color> grid object{1}.");

    // Zone > Edit > Size-X
    private const string SectionZonesEditSizeX = SectionZonesEdit + " / Size-X";
    [TranslationData(SectionZonesEditSizeX, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSizeXInvalid = new Translation("<#ff8c69>Size X must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.");
    [TranslationData(SectionZonesEditSizeX, IsPrioritizedTranslation = false)]
    public static readonly Translation<float> ZoneEditSizeXSuccess = new Translation<float>("<#e6e3d5>Set size x to <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > Size-Z
    private const string SectionZonesEditSizeZ = SectionZonesEdit + " / Size-Z";
    [TranslationData(SectionZonesEditSizeZ, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSizeZInvalid = new Translation("<#ff8c69>Size Z must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.");
    [TranslationData(SectionZonesEditSizeZ, IsPrioritizedTranslation = false)]
    public static readonly Translation<float> ZoneEditSizeZSuccess = new Translation<float>("<#e6e3d5>Set size z to <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > Center
    private const string SectionZonesEditCenter = SectionZonesEdit + " / Center";
    [TranslationData(SectionZonesEditCenter, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditCenterInvalid = new Translation("<#ff8c69>To set center you must provide two decimal or whole numbers, or leave them blank to use the player's current position.");
    [TranslationData(SectionZonesEditCenter, IsPrioritizedTranslation = false)]
    public static readonly Translation<Vector2> ZoneEditCenterSuccess = new Translation<Vector2>("<#e6e3d5>Set center position to <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > Spawn
    private const string SectionZonesEditSpawn = SectionZonesEdit + " / Spawn";
    [TranslationData(SectionZonesEditSpawn, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSpawnInvalid = new Translation("<#ff8c69>To set spawn point you must provide two decimal or whole numbers, or leave them blank to use the player's current position.");
    [TranslationData(SectionZonesEditSpawn, IsPrioritizedTranslation = false)]
    public static readonly Translation<Vector2> ZoneEditSpawnSuccess = new Translation<Vector2>("<#e6e3d5>Set spawn point to <#ff9999>{0}</color>.", "0.##");
    [TranslationData(SectionZonesEditSpawn, IsPrioritizedTranslation = false)]
    public static readonly Translation<Vector2, float> ZoneEditSpawnSuccessRotation = new Translation<Vector2, float>("<#e6e3d5>Set spawn point to <#ff9999>{0}</color> and yaw to <#ff9999>{1}</color>°.", "0.##", "0.##");

    // Zone > Edit > Name
    private const string SectionZonesEditName = SectionZonesEdit + " / Name";
    [TranslationData(SectionZonesEditName, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditNameInvalid = new Translation("<#ff8c69>Name requires one string argument. Quotation marks aren't required.");
    [TranslationData(SectionZonesEditName, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> ZoneEditNameSuccess = new Translation<string>("<#e6e3d5>Set name to \"<#ff9999>{0}</color>\".");

    // Zone > Edit > Short-Name
    private const string SectionZonesEditShortName = SectionZonesEdit + " / Short Name";
    [TranslationData(SectionZonesEditShortName, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditShortNameInvalid = new Translation("<#ff8c69>Short name requires one string argument. Quotation marks aren't required.");
    [TranslationData(SectionZonesEditShortName, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> ZoneEditShortNameSuccess = new Translation<string>("<#e6e3d5>Set short name to \"<#ff9999>{0}</color>\".");
    [TranslationData(SectionZonesEditShortName, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditShortNameRemoved = new Translation("<#e6e3d5>Removed short name.");

    // Zone > Edit > Use-Case
    private const string SectionZonesEditUseCase = SectionZonesEdit + " / Use Case";
    [TranslationData(SectionZonesEditUseCase, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditUseCaseInvalid = new Translation("<#ff8c69>Use case requires one string argument: \"other\", \"flag\", \"lobby\", \"team1main\", \"team2main\", \"team1maincampzone\", or \"team2maincampzone\".");
    [TranslationData(SectionZonesEditUseCase, IsPrioritizedTranslation = false)]
    public static readonly Translation<ZoneUseCase> ZoneEditUseCaseSuccess = new Translation<ZoneUseCase>("<#e6e3d5>Set use case to \"<#ff9999>{0}</color>\".");

    // Zone > Edit > Flags
    private const string SectionZonesEditFlags = SectionZonesEdit + " / Flags";
    [TranslationData(SectionZonesEditFlags, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditFlagsInvalid = new Translation("<#ff8c69>Use case requires one remainder string argument with comma separated values: \"none\", \"safezone\", \"nobuilding\", \"nofobbuilding\", \"noradios\", \"nobunkers\", \"norallies\", \"notraps\", \"nodropitems\", and/or \"nopickitems\".");
    [TranslationData(SectionZonesEditFlags, IsPrioritizedTranslation = false)]
    public static readonly Translation<ZoneFlags> ZoneEditFlagsSuccess = new Translation<ZoneFlags>("<#e6e3d5>Set use case to \"<#ff9999>{0}</color>\".");

    // Zone > Edit > Transactions
    private const string SectionZonesEditTransactions = SectionZonesEdit + " / Transactions";
    [TranslationData(SectionZonesEditTransactions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditUndoEmpty = new Translation("<#ff8c69>There is nothing to undo.");
    [TranslationData(SectionZonesEditTransactions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditRedoEmpty = new Translation("<#ff8c69>There is nothing to redo.");

    // Zone > Edit > UI
    private const string SectionZonesEditUI = SectionZonesEdit + " / UI";
    // ReSharper disable once InconsistentNaming
    [TranslationData(SectionZonesEditUI, FormattingDescriptions = new string[] { "Minimum Height (or ∞ if not set)", "Maximum Height (or ∞ if not set)" }, IsPrioritizedTranslation = false)]
    public static readonly Translation<string, string> ZoneEditUIYLimits = new Translation<string, string>("Y: {0} - {1}", TranslationFlags.UnityUI);
    // ReSharper disable once InconsistentNaming
    [TranslationData(SectionZonesEditUI, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditUIYLimitsInfinity = new Translation("∞", TranslationFlags.UnityUI);

    // Zone > Edit > UI > Suggestions
    private const string SectionZonesEditUISuggestions = SectionZonesEditUI + " / Suggestions";
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommandsHeader = new Translation("Suggested Commands", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand1  = new Translation("/ze maxheight [value]", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand2  = new Translation("/ze minheight [value]", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand3  = new Translation("/ze finalize", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand4  = new Translation("/ze cancel", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand5  = new Translation("/ze addpt [x z]", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand6  = new Translation("/ze delpt [number | x z]", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand7  = new Translation("/ze setpt <number | src: x z | number dest: x z | src: x z dest: x z>", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand8  = new Translation("/ze orderpt <from-index to-index | to-index | src: x z to-index>", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand9  = new Translation("/ze radius [value]", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand10 = new Translation("/ze sizex [value]", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand11 = new Translation("/ze sizez [value]", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand12 = new Translation("/zone util location", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand13 = new Translation("/ze type <rectangle | circle | polygon>", TranslationFlags.UnityUI);
    [TranslationData(SectionZonesEditUISuggestions, IsPrioritizedTranslation = false)]
    public static readonly Translation ZoneEditSuggestedCommand14 = new Translation("/ze clearpoints", TranslationFlags.UnityUI);

    // Zone > Util
    private const string SectionZonesUtil = SectionZones + " / Util";

    // Zone > Util > Location
    private const string SectionZonesUtilLocation = SectionZonesUtil + " / Location";

    [TranslationData(SectionZonesUtilLocation, FormattingDescriptions = new string[] { "X (m)", "Y (m)", "Z (m)", "Yaw (°)" })]
    public static readonly Translation<float, float, float, float> ZoneUtilLocation = new Translation<float, float, float, float>("<#e6e3d5>Location: {0}, {1}, {2} | Yaw: {3}°.", "0.##", "0.##", "0.##", "0.##");
    #endregion

    #region Teams
    private const string SectionTeams = "Teams";
    [TranslationData(SectionTeams, "Gets sent to the player when they walk or teleport into main base.")]
    public static readonly Translation<FactionInfo> EnteredMain = new Translation<FactionInfo>("<#e6e3d5>You have entered the safety of the {0} headquarters!", FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionTeams, "Gets sent to the player when they walk or teleport out of main base.")]
    public static readonly Translation<FactionInfo> LeftMain = new Translation<FactionInfo>("<#e6e3d5>You have left the safety of the {0} headquarters!", FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionTeams, "Gets sent to the player when they join a team.")]
    public static readonly Translation<FactionInfo> TeamJoinDM = new Translation<FactionInfo>("<#a0ad8e>You've joined {0}.", FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionTeams, "Gets broadcasted to everyone when someone joins a team.")]
    public static readonly Translation<FactionInfo, IPlayer> TeamJoinAnnounce = new Translation<FactionInfo, IPlayer>("<#a0ad8e>{1} joined {0}!", FactionInfo.FormatColorDisplayName, UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeams, "Gets broadcasted when the game is over.")]
    public static readonly Translation<FactionInfo> TeamWin = new Translation<FactionInfo>("<#a0ad8e>{0} has won the battle!", FactionInfo.FormatColorDisplayName);
    [TranslationData(SectionTeams)]
    public static readonly Translation<Cooldown> TeamsCooldown = new Translation<Cooldown>("<#ff8c69>You can't use /teams for another {0}.", Cooldown.FormatTimeLong);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIHeader = new Translation("Choose a Team", TranslationFlags.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIClickToJoin = new Translation("CLICK TO JOIN", TranslationFlags.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIJoined = new Translation("JOINED", TranslationFlags.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIFull = new Translation("<#bf6363>FULL", TranslationFlags.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIConfirm = new Translation("CONFIRM", TranslationFlags.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIBack = new Translation("BACK", TranslationFlags.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsUIJoining = new Translation("<#999999>JOINING...", TranslationFlags.TMProUI);
    [TranslationData(SectionTeams)]
    public static readonly Translation TeamsShuffleQueued = new Translation("Teams will be SHUFFLED next game.");
    #endregion

    #region Spotting
    private const string SectionSpotting = "Spotting";
    [TranslationData(SectionSpotting)]
    public static readonly Translation SpottedToast = new Translation("<#b9ffaa>SPOTTED", TranslationFlags.TMProUI);
    [TranslationData(SectionSpotting, FormattingDescriptions = new string[] { "Team color of the speaker.", "Target" })]
    public static readonly Translation<Color, string> SpottedMessage = new Translation<Color, string>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Enemy {1} spotted!", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionSpotting)]
    public static readonly Translation SpottedTargetPlayer = new Translation("contact", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionSpotting)]
    public static readonly Translation SpottedTargetFOB = new Translation("FOB", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionSpotting)]
    public static readonly Translation SpottedTargetCache = new Translation("Cache", TranslationFlags.NoColorOptimization);
    #endregion

    #region Actions
    private const string SectionActions = "Actions";
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> NeedMedicChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: I need a medic here!", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionActions)]
    public static readonly Translation<string> NeedMedicToast = new Translation<string>("<#a1998d>{0} needs healing.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> NeedAmmoChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: I need some ammo here!", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionActions)]
    public static readonly Translation<string> NeedAmmoToast = new Translation<string>("<#a1998d>{0} needs ammunition.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> NeedRideChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Hey, I need a ride!", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionActions)]
    public static readonly Translation<string> NeedRideToast = new Translation<string>("<#a1998d>{0} needs a ride.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> NeedSupportChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: I need help over here!", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionActions)]
    public static readonly Translation<string> NeedSupportToast = new Translation<string>("<#a1998d>{0} needs help.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> HeliPickupChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Requesting helicopter transport!", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionActions)]
    public static readonly Translation<string> HeliPickupToast = new Translation<string>("<#a1998d><#dbb67f>{0}</color> needs transport.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> HeliDropoffChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Requesting drop off at this position!", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionActions)]
    public static readonly Translation<string> HeliDropoffToast = new Translation<string>("<#a1998d><#dbb67f>{0}</color> is requesting drop off.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> SuppliesBuildChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Requesting FOB building supplies!", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionActions)]
    public static readonly Translation<string> SuppliesBuildToast = new Translation<string>("<#a1998d><#dbb67f>{0}</color> needs FOB supplies.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> SuppliesAmmoChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Requesting FOB ammunition supplies!", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionActions)]
    public static readonly Translation<string> SuppliesAmmoToast = new Translation<string>("<#a1998d><#dbb67f>{0}</color> needs FOB ammunition.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> AirSupportChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Requesting close air support!", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionActions)]
    public static readonly Translation<string> AirSupportToast = new Translation<string>("<#a1998d><#dbb67f>{0}</color> needs air support.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> ArmorSupportChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Requesting armor support!", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionActions)]
    public static readonly Translation<string> ArmorSupportToast = new Translation<string>("<#a1998d><#dbb67f>{0}</color> needs armor support.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> ThankYouChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Thank you!", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionActions)]
    public static readonly Translation<Color> SorryChat = new Translation<Color>("[T] <#{0}><noparse>%SPEAKER%</noparse></color>: Sorry.", TranslationFlags.NoColorOptimization);
    [TranslationData(SectionActions)]
    public static readonly Translation AttackToast = new Translation("<#a1998d>Attack the marked position.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation DefendToast = new Translation("<#a1998d>Defend the marked position.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation MoveToast = new Translation("<#a1998d>Move to the marked position.", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation BuildToast = new Translation("<#a1998d>Build near the marked position.", TranslationFlags.TMProUI);

    [TranslationData(SectionActions)]
    public static readonly Translation ActionErrorInMain = new Translation("<#9e7d7d>Unavailable in main", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation ActionErrorNoMarker = new Translation("<#9e7d7d>Place a MARKER first", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation ActionErrorNotInHeli = new Translation("<#9e7d7d>You are not inside a HELICOPTER", TranslationFlags.TMProUI);
    [TranslationData(SectionActions)]
    public static readonly Translation ActionErrorInVehicle = new Translation("<#9e7d7d>Unavailable in vehicle", TranslationFlags.TMProUI);
    #endregion

    #region Teleport
    private const string SectionTeleport = "Teleport Command";
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer> TeleportTargetDead = new Translation<IPlayer>("<#8f9494>{0} is not alive.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport)]
    public static readonly Translation<IPlayer, InteractableVehicle> TeleportSelfSuccessVehicle = new Translation<IPlayer, InteractableVehicle>("<#bfb9ac>You were put in {0}'s {1}.", UCPlayer.FormatColoredCharacterName, FormatRarityColor);
    [TranslationData(SectionTeleport)]
    public static readonly Translation<IPlayer> TeleportSelfSuccessPlayer = new Translation<IPlayer>("<#bfb9ac>You were teleported to {0}.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer> TeleportSelfPlayerObstructed = new Translation<IPlayer>("<#8f9494>Failed to teleport you to {0}, their position is obstructed.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> TeleportLocationNotFound = new Translation<string>("<#8f9494>Failed to find a location similar to <#ddd>{0}</color>.");
    [TranslationData(SectionTeleport)]
    public static readonly Translation<string> TeleportSelfLocationSuccess = new Translation<string>("<#bfb9ac>You were teleported to <#ddd>{0}</color>.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> TeleportStopJump = new Translation<string>("<#bfb9ac>You will no longer jump on right punch.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> TeleportStartJump = new Translation<string>("<#bfb9ac>You will jump on right punch. Do <#ddd>/tp jump stop</color> to stop.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> TeleportSelfLocationObstructed = new Translation<string>("<#8f9494>Failed to teleport you to <#ddd>{0}</color>, it's position is obstructed.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation TeleportWaypointNotFound = new Translation("<#8f9494>You must have a waypoint placed on the map.");
    [TranslationData(SectionTeleport)]
    public static readonly Translation<GridLocation> TeleportSelfWaypointSuccess = new Translation<GridLocation>("<#bfb9ac>You were teleported to your waypoint in <#ddd>{0}</color>.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<GridLocation> TeleportSelfWaypointObstructed = new Translation<GridLocation>("<#8f9494>Failed to teleport you to your waypoint in <#ddd>{0}</color>, it's position is obstructed.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<GridLocation> TeleportGridLocationNotFound = new Translation<GridLocation>("<#8f9494>There is no terrain at <#ddd>{0}</color>.");
    [TranslationData(SectionTeleport)]
    public static readonly Translation<GridLocation> TeleportSelfGridLocationSuccess = new Translation<GridLocation>("<#bfb9ac>You were teleported to <#ddd>{0}</color>.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<GridLocation> TeleportSelfGridLocationObstructed = new Translation<GridLocation>("<#8f9494>Failed to teleport you to <#ddd>{0}</color>, it's position is obstructed.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, GridLocation> TeleportOtherWaypointSuccess = new Translation<IPlayer, GridLocation>("<#bfb9ac>{0} was teleported to your waypoint in <#ddd>{1}</color>.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, GridLocation> TeleportOtherWaypointObstructed = new Translation<IPlayer, GridLocation>("<#8f9494>Failed to teleport {0} to your waypoint in <#ddd>{1}</color>, it's position is obstructed.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, GridLocation> TeleportOtherGridLocationSuccess = new Translation<IPlayer, GridLocation>("<#bfb9ac>{0} was teleported to <#ddd>{1}</color>.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, GridLocation> TeleportOtherGridLocationObstructed = new Translation<IPlayer, GridLocation>("<#8f9494>Failed to teleport {0} to <#ddd>{1}</color>, it's position is obstructed.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, IPlayer, InteractableVehicle> TeleportOtherSuccessVehicle = new Translation<IPlayer, IPlayer, InteractableVehicle>("<#bfb9ac>{0} was put in {1}'s {2}.", UCPlayer.FormatColoredCharacterName, UCPlayer.FormatColoredCharacterName, FormatRarityColor);
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, IPlayer> TeleportOtherSuccessPlayer = new Translation<IPlayer, IPlayer>("<#bfb9ac>{0} was teleported to {1}.", UCPlayer.FormatColoredCharacterName, UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, IPlayer> TeleportOtherObstructedPlayer = new Translation<IPlayer, IPlayer>("<#8f9494>Failed to teleport {0} to {1}, their position is obstructed.", UCPlayer.FormatColoredCharacterName, UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, string> TeleportOtherSuccessLocation = new Translation<IPlayer, string>("<#bfb9ac>{0} was teleported to <#ddd>{1}</color>.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, string> TeleportOtherObstructedLocation = new Translation<IPlayer, string>("<#8f9494>Failed to teleport {0} to <#ddd>{1}</color>, it's position is obstructed.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation<string> TeleportTargetNotFound = new Translation<string>("<#8f9494>Failed to find a player from <#ddd>{0}</color>.");
    [TranslationData(SectionTeleport, IsPrioritizedTranslation = false)]
    public static readonly Translation TeleportInvalidCoordinates = new Translation("<#8f9494>Use of coordinates should look like: <#eee>/tp [player] <x y z></color>.");
    #endregion

    #region Heal Command
    private const string SectionHeal = "Heal Command";
    [TranslationData(SectionHeal, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer> HealPlayer = new Translation<IPlayer>("<#ff9966>You healed {0}.", UCPlayer.FormatColoredCharacterName);
    [TranslationData(SectionHeal)]
    public static readonly Translation HealSelf = new Translation("<#ff9966>You were healed.");
    #endregion

    #region God Command
    private const string SectionGod = "God Command";
    [TranslationData(SectionGod, IsPrioritizedTranslation = false)]
    public static readonly Translation GodModeEnabled = new Translation("<#bfb9ac>God mode <#99ff66>enabled</color>.");
    [TranslationData(SectionGod, IsPrioritizedTranslation = false)]
    public static readonly Translation GodModeDisabled = new Translation("<#ff9966>God mode <#ff9999>disabled</color>.");
    #endregion

    #region Vanish Command
    private const string SectionVanish = "Vanish Command";
    [TranslationData(SectionVanish, IsPrioritizedTranslation = false)]
    public static readonly Translation VanishModeEnabled = new Translation("<#bfb9ac>Vanish mode <#99ff66>enabled</color>.");
    [TranslationData(SectionVanish, IsPrioritizedTranslation = false)]
    public static readonly Translation VanishModeDisabled = new Translation("<#ff9966>Vanish mode <#ff9999>disabled</color>.");
    #endregion

    #region Permission Command
    private const string SectionPermission = "Permission Command";
    [TranslationData(SectionPermission)]
    public static readonly Translation<string> PermissionsCurrent = new Translation<string>("<#bfb9ac>Current permisions: <color=#ffdf91>{0}</color>.");
    [TranslationData(SectionPermission, IsPrioritizedTranslation = false)]
    public static readonly Translation<EAdminType, IPlayer, ulong> PermissionGrantSuccess = new Translation<EAdminType, IPlayer, ulong>("<#bfb9ac><#7f8182>{1}</color> <#ddd>({2})</color> is now a <#ffdf91>{0}</color>.");
    [TranslationData(SectionPermission, IsPrioritizedTranslation = false)]
    public static readonly Translation<EAdminType, IPlayer, ulong> PermissionGrantAlready = new Translation<EAdminType, IPlayer, ulong>("<#bfb9ac><#7f8182>{1}</color> <#ddd>({2})</color> is already at the <#ffdf91>{0}</color> level.");
    [TranslationData(SectionPermission, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, ulong> PermissionRevokeSuccess = new Translation<IPlayer, ulong>("<#bfb9ac><#7f8182>{0}</color> <#ddd>({1})</color> is now a <#ffdf91>member</color>.");
    [TranslationData(SectionPermission, IsPrioritizedTranslation = false)]
    public static readonly Translation<IPlayer, ulong> PermissionRevokeAlready = new Translation<IPlayer, ulong>("<#bfb9ac><#7f8182>{0}</color> <#ddd>({1})</color> is already a <#ffdf91>member</color>.");
    #endregion

    #region Win UI
    private const string SectionWinUI = "Win UI";
    [TranslationData(SectionWinUI)]
    public static readonly Translation<int> WinUIValueTickets = new Translation<int>("{0} ${p:0:Ticket}", TranslationFlags.UnityUI);
    [TranslationData(SectionWinUI)]
    public static readonly Translation<int> WinUIValueCaches = new Translation<int>("{0} ${p:0:Cache} Left", TranslationFlags.UnityUI);
    [TranslationData(SectionWinUI)]
    public static readonly Translation<FactionInfo> WinUIHeaderWinner = new Translation<FactionInfo>("{0}\r\nhas won the battle!", TranslationFlags.UnityUI, FactionInfo.FormatColorDisplayName);
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
    public static readonly Translation<IPlayer> RequestUAVSent = new Translation<IPlayer>("<#33cccc>A request was sent to <#c$commander$>{0}</color> for a <#cc99ff>UAV</color>.", UCPlayer.FormatNickName);
    [TranslationData(SectionUAV, "Sent when the player requests a UAV from someone other than themselves to the commander.", "The requester of the UAV.", "The requester's squad.", "Location of request.")]
    public static readonly Translation<IPlayer, Squad, GridLocation> RequestUAVTell = new Translation<IPlayer, Squad, GridLocation>("<#33cccc>{0} from squad <#cedcde><uppercase>{1}</uppercase></color> wants to deploy a <#cc99ff>UAV</color> at <#fff>{2}</color>.\n<#cedcde>Type /confirm or /deny in the next 15 seconds.", UCPlayer.FormatColoredNickName, Squad.FormatName);
    [TranslationData(SectionUAV, "Sent when the player tries to request a UAV while no one on their team has a commander kit.")]
    public static readonly Translation RequestUAVNoActiveCommander = new Translation("<#e86868>There's currently no players with the <#c$commander$>commander</color> kit on your team. <#cc99ff>UAV</color>s must be requested from a <#c$commander$>commander</color>.");
    [TranslationData(SectionUAV, "Sent to the commander if the requester disconnected before the commander confirmed.", "The requester.")]
    public static readonly Translation<IPlayer> RequestUAVRequesterLeft = new Translation<IPlayer>("<#e86868>The <#cc99ff>UAV</color> request was cancelled because {0} disconnected.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionUAV, "Sent to the requested if the commander disconnected before they confirmed.", "The commander.")]
    public static readonly Translation<IPlayer> RequestUAVCommanderLeft = new Translation<IPlayer>("<#e86868>Your <#cc99ff>UAV</color> request was cancelled because <#c$commander$>{0}</color> disconnected.", UCPlayer.FormatNickName);
    [TranslationData(SectionUAV, "Sent to the commander if the requester changes teams before the commander confirmed.", "The requester.")]
    public static readonly Translation<IPlayer> RequestUAVRequesterChangedTeams = new Translation<IPlayer>("<#e86868>The <#cc99ff>UAV</color> request was cancelled because {0} changed teams.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionUAV, "Sent to the requested if the commander changes team before they confirmed.", "The commander.")]
    public static readonly Translation<IPlayer> RequestUAVCommanderChangedTeams = new Translation<IPlayer>("<#e86868>Your <#cc99ff>UAV</color> request was cancelled because <#c$commander$>{0}</color> changed teams.", UCPlayer.FormatNickName);
    [TranslationData(SectionUAV, "Sent to the commander if the requester changes classes to a non-SL class, leaves their squad, or promotes someone else before the commander confirmed.", "The requester.")]
    public static readonly Translation<IPlayer> RequestUAVRequesterNotSquadLeader = new Translation<IPlayer>("<#e86868>The <#cc99ff>UAV</color> request was cancelled because {0} changed teams.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionUAV, "Sent to the requested if the commander stops being commander before they confirmed.", "The commander.")]
    public static readonly Translation<IPlayer> RequestUAVCommanderNoLongerCommander = new Translation<IPlayer>("<#e86868>Your <#cc99ff>UAV</color> request was cancelled because {0} is no longer the <#c$commander$>commander</color>.", UCPlayer.FormatColoredNickName);
    [TranslationData(SectionUAV, "Sent to the requested if the commander denies their UAV request.", "The commander.")]
    public static readonly Translation<IPlayer> RequestUAVDenied = new Translation<IPlayer>("<#e86868>Your <#cc99ff>UAV</color> request was denied by <#c$commander$>{0}</color>.", UCPlayer.FormatNickName);
    [TranslationData(SectionUAV, "Sent to the requested if someone else is already requesting a UAV.")]
    public static readonly Translation RequestAlreadyActive = new Translation("<#e86868>Someone else on your team is already requesting a <#cc99ff>UAV</color>.");

    #endregion

    #region Attach
    private const string SectionAttach = "Attach";

    [TranslationData(SectionAttach, "Sent when a player tries to use /attach without holding a gun.", IsPrioritizedTranslation = false)]
    public static readonly Translation AttachNoGunHeld = new Translation("<#ff8c69>You must be holding a gun to attach an attachment.");

    [TranslationData(SectionAttach, "Sent when a player tries to use /attach remove without providing a valid attachment type.", "Caller's input", IsPrioritizedTranslation = false)]
    public static readonly Translation<string> AttachClearInvalidType = new Translation<string>("<#ff8c69><#fff>{0}</color> is not a valid attachment type. Enter one of the following: <#fff><sight|tact|grip|barrel|ammo></color>.");

    [TranslationData(SectionAttach, "Sent when a player tries to use /attach remove <type> without that attachment.", "Held gun asset", "Type of attachment", IsPrioritizedTranslation = false)]
    public static readonly Translation<ItemGunAsset, AttachmentType> AttachClearAlreadyGone = new Translation<ItemGunAsset, AttachmentType>("<#ff8c69>There is not a <#cedcde>{1}</color> on your {0}.", FormatRarityColor, FormatUppercase);

    [TranslationData(SectionAttach, "Sent when a player successfully uses /attach remove <type>.", "Held gun asset", "Type of attachment", IsPrioritizedTranslation = false)]
    public static readonly Translation<ItemGunAsset, AttachmentType> AttachClearSuccess = new Translation<ItemGunAsset, AttachmentType>("<#bfb9ac>You removed the <#cedcde>{1}</color> from your {0}.", FormatRarityColor, FormatUppercase);

    [TranslationData(SectionAttach, "Sent when a player successfully uses /attach <attachment>.", "Held gun asset", "Type of attachment", "Attachment item asset", IsPrioritizedTranslation = false)]
    public static readonly Translation<ItemGunAsset, AttachmentType, ItemCaliberAsset> AttachSuccess = new Translation<ItemGunAsset, AttachmentType, ItemCaliberAsset>("<#bfb9ac>Added {2} as a <#cedcde>{1}</color> to your {0}.", FormatRarityColor, FormatUppercase, FormatRarityColor);

    [TranslationData(SectionAttach, "Sent when a player tries to attach an item but either it's not an attachment or can't be found.", "Caller's input", IsPrioritizedTranslation = false)]
    public static readonly Translation<string> AttachCaliberNotFound = new Translation<string>("<#ff8c69>Unable to find an attachment named <#fff>{0}</color>.", FormatPropercase);

    [TranslationData(SectionAttach, "Sent when a player successfully sets the ammo count of a gun.", "Held gun asset", "Amount of ammo", IsPrioritizedTranslation = false)]
    public static readonly Translation<ItemGunAsset, byte> AttachSetAmmoSuccess = new Translation<ItemGunAsset, byte>("<#bfb9ac>Set the ammo count in your {0} to <#fff>{1}</color>.", FormatRarityColor);

    [TranslationData(SectionAttach, "Sent when a player successfully sets the ammo count of a gun.", "Held gun asset", "Amount of ammo", IsPrioritizedTranslation = false)]
    public static readonly Translation<ItemGunAsset, EFiremode> AttachSetFiremodeSuccess = new Translation<ItemGunAsset, EFiremode>("<#bfb9ac>Set the fire mode of your {0} to <#cedcde>{1}</color>.", FormatRarityColor, FormatUppercase);
    #endregion

    #region Kit Menu UI
    private const string SectionKitMenuUI = "Kit Menu";
    [TranslationData(SectionKitMenuUI, "Text that goes on the base kits tab.")]
    public static readonly Translation KitMenuUITabBaseKits    = new Translation("Base Kits", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Text that goes on the elite kits tab.")]
    public static readonly Translation KitMenuUITabEliteKits   = new Translation("Elite Kits", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Text that goes on the loadouts tab.")]
    public static readonly Translation KitMenuUITabLoadouts    = new Translation("Loadouts", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Text that goes on the special kits tab.")]
    public static readonly Translation KitMenuUITabSpecialKits = new Translation("Special Kits", TranslationFlags.TMProUI);

    [TranslationData(SectionKitMenuUI, "Label that goes in front of the filter dropdown.")]
    public static readonly Translation KitMenuUIFilterLabel = new Translation("Filter", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of the faction in kit info.")]
    public static readonly Translation KitMenuUIFactionLabel = new Translation("Faction", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of the class in kit info.")]
    public static readonly Translation KitMenuUIClassLabel = new Translation("Class", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of the included items list in kit info.")]
    public static readonly Translation KitMenuUIIncludedItemsLabel = new Translation("Included Items", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Value for kit type (KitType.Public).")]
    public static readonly Translation KitMenuUIKitTypeLabelPublic = new Translation("Public Kit", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Value for kit type (KitType.Elite).")]
    public static readonly Translation KitMenuUIKitTypeLabelElite = new Translation("Elite Kit", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Value for kit type (KitType.Special).")]
    public static readonly Translation KitMenuUIKitTypeLabelSpecial = new Translation("Special/Event Kit", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Value for kit type (KitType.Loadout).")]
    public static readonly Translation KitMenuUIKitTypeLabelLoadout = new Translation("Custom Loadout", TranslationFlags.TMProUI);

    [TranslationData(SectionKitMenuUI, "Label that goes in front of playtime in kit stats.")]
    public static readonly Translation KitMenuUIPlaytimeLabel = new Translation("Playtime", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of total kills in kit stats.")]
    public static readonly Translation KitMenuUIKillsLabel = new Translation("Total Kills", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of total deaths in kit stats.")]
    public static readonly Translation KitMenuUIDeathsLabel = new Translation("Total Deaths", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills in kit stats.")]
    public static readonly Translation KitMenuUIPrimaryKillsLabel = new Translation("Primary Kills", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary average kill distance in kit stats.")]
    public static readonly Translation KitMenuUIPrimaryAvgDstLabel = new Translation("Primary Avg. Dst.", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of secondary kills in kit stats.")]
    public static readonly Translation KitMenuUISecondaryKillsLabel = new Translation("Secondary Kills", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of DBNO states in kit stats.")]
    public static readonly Translation KitMenuUIDBNOLabel = new Translation("Injures Without Kill", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of distance traveled in kit stats.")]
    public static readonly Translation KitMenuUIDistanceTraveledLabel = new Translation("Distance Traveled", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of tickets lost in kit stats.")]
    public static readonly Translation KitMenuUITicketsLostLabel = new Translation("Tickets Lost", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of tickets gained in kit stats.")]
    public static readonly Translation KitMenuUITicketsGainedLabel = new Translation("Tickets Recovered", TranslationFlags.TMProUI);

    [TranslationData(SectionKitMenuUI, "Label for kit stats title.")]
    public static readonly Translation KitMenuUIStatsLabel = new Translation("Stats", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label for kit actions title.")]
    public static readonly Translation KitMenuUIActionsLabel = new Translation("Actions", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button action 1, request kit.")]
    public static readonly Translation KitMenuUIActionRequestKitLabel = new Translation("Request Kit", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button action 2, buy kit (can afford).", "Credit cost")]
    public static readonly Translation<int> KitMenuUIActionBuyPublicKitCanAffordLabel = new Translation<int>("<#ccffff>Buy Kit <#c$credits$>C</color> <#fff>{0}</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button action 2, buy kit (can't afford).", "Credit cost")]
    public static readonly Translation<int> KitMenuUIActionBuyPublicKitCantAffordLabel = new Translation<int>("<#ff6666>Requires <#c$credits$>C</color> <#fff>{0}</color>", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button action 3, order kit.", "Price", "Currency Prefix")]
    public static readonly Translation<decimal, string> KitMenuUIActionBuyPremiumKitLabel = new Translation<decimal, string>("<#ccffff>Open Ticket <#c$kit_level_dollars$>{1}</color> <#fff>{0}</color>", TranslationFlags.TMProUI, "C");
    [TranslationData(SectionKitMenuUI, "Label actions button action not in main.")]
    public static readonly Translation KitMenuUIActionNotInMainKitLabel = new Translation("<#ff6666>Not in Main", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button action premium not linked.", "Price", "Currency Prefix")]
    public static readonly Translation<decimal, string> KitMenuUIActionBuyPremiumKitNotLinkedLabel = new Translation<decimal, string>("<#ccffff>Premium kit: <#c$kit_level_dollars$>{1}</color> <#fff>{0}</color>", TranslationFlags.TMProUI, "C");
    [TranslationData(SectionKitMenuUI, "Label actions button action premium unlock requirement not met.", "Price", "Currency Prefix")]
    public static readonly Translation<decimal, string> KitMenuUIActionBuyPublicUnlockReqNotMetLabel = new Translation<decimal, string>("<#ccffff>Premium kit: <#c$kit_level_dollars$>{1}</color> <#fff>{0}</color>", TranslationFlags.TMProUI, "C");
    [TranslationData(SectionKitMenuUI, "Label actions button staff give kit.")]
    public static readonly Translation KitMenuUIActionGiveKitLabel = new Translation("<#0099ff>Give Kit", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button staff edit kit.")]
    public static readonly Translation KitMenuUIActionEditKitLabel = new Translation("<#0099ff>Edit Kit</color> (Coming Soon)", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label actions button staff set loadout items kit.")]
    public static readonly Translation KitMenuUIActionSetLoadoutItemsLabel = new Translation("<#0099ff>Set Loadout Items", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Shown when a player doesn't have access to a kit.")]
    public static readonly Translation KitMenuUIActionNoAccessLabel = new Translation("No Access", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Shown when a kit's faction is not assigned.")]
    public static readonly Translation KitMenuUINoFaction = new Translation("Unaffiliated", TranslationFlags.TMProUI);

    /* CLASS STATS */
    // squadleader
    [TranslationData(SectionKitMenuUI, "Label that goes in front of FOBs started for Squadleaders in kit stats.")]
    public static readonly Translation KitMenuUISquadLeaderFOBsStartedLabel = new Translation("FOBs Started", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of UAVs requested for Squadleaders in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUISquadLeaderUAVsRequestedLabel = new Translation("UAVs Requested", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of targets spotted for Squadleaders in kit stats.")]
    public static readonly Translation KitMenuUISquadLeaderTargetsSpottedLabel = new Translation("Targets Spotted", TranslationFlags.TMProUI);

    // rifleman
    [TranslationData(SectionKitMenuUI, "Label that goes in front of self restocked for Riflemen in kit stats.")]
    public static readonly Translation KitMenuUIRiflemanSelfRestockedLabel = new Translation("Self Restocked", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of teammates restocked for Riflemen in kit stats.")]
    public static readonly Translation KitMenuUIRiflemanTeammatesRestockedLabel = new Translation("Teammates Restocked", TranslationFlags.TMProUI);
    
    // medic
    [TranslationData(SectionKitMenuUI, "Label that goes in front of teammates healed for Medics in kit stats.")]
    public static readonly Translation KitMenuUIMedicTeammatesHealedLabel = new Translation("Teammates Healed", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of teammates revived for Medics in kit stats.")]
    public static readonly Translation KitMenuUIMedicTeammatesRevivedLabel = new Translation("Teammates Revived", TranslationFlags.TMProUI);

    // breacher
    [TranslationData(SectionKitMenuUI, "Label that goes in front of structures destroyed for Breachers in kit stats.")]
    public static readonly Translation KitMenuUIBreacherStructuresDestroyedLabel = new Translation("Structures Destroyed", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of radios destroyed for Breachers in kit stats.")]
    public static readonly Translation KitMenuUIBreacherRadiosDestroyedLabel = new Translation("Radios Destroyed", TranslationFlags.TMProUI);

    // auto-rifleman
    [TranslationData(SectionKitMenuUI, "Label that goes in front of spray n pray streak (most kills without reloading) for Automatic Riflemen in kit stats.")]
    public static readonly Translation KitMenuUIAutoRiflemanStructuresDestroyedLabel = new Translation("Spray n Pray Streak", TranslationFlags.TMProUI);

    // grenadier
    [TranslationData(SectionKitMenuUI, "Label that goes in front of grenade kills for Grenadiers in kit stats.")]
    public static readonly Translation KitMenuUIGrenadierGrenadeKillsLabel = new Translation("Grenade Kills", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of vehicle kills for Grenadiers in kit stats.")]
    public static readonly Translation KitMenuUIGrenadierVehicleKillsLabel = new Translation("Vehicle Kills", TranslationFlags.TMProUI);

    // machine gunner
    [TranslationData(SectionKitMenuUI, "Label that goes in front of spray n pray streak (most kills without reloading) for Machine Gunners in kit stats.")]
    public static readonly Translation KitMenuUIMachineGunnerStructuresDestroyedLabel = new Translation("Spray n Pray Streak", TranslationFlags.TMProUI);
    
    // LAT
    [TranslationData(SectionKitMenuUI, "Label that goes in front of vehicle kills for LATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUILATVehicleKillsLabel = new Translation("Vehicle Kills", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of LAT player kills for LATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUILATPlayerKillsLabel = new Translation("LAT Player Kills", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of structure kills for LATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUILATStructuresDestroyedLabel = new Translation("Structures Destroyed", TranslationFlags.TMProUI);
    
    // HAT
    [TranslationData(SectionKitMenuUI, "Label that goes in front of vehicle kills for HATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIHATVehicleKillsLabel = new Translation("Vehicle Kills", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of HAT player kills for HATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIHATPlayerKillsLabel = new Translation("HAT Player Kills", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of structure kills for HATs in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIHATStructuresDestroyedLabel = new Translation("Structures Destroyed", TranslationFlags.TMProUI);

    // marksman
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills from 150m to 250m away for Marksmen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIMarksmanKills100mLabel = new Translation("Kills 150m-250m", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills from 250m to 350m away for Marksmen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIMarksmanKills200mLabel = new Translation("Kills 250m-350m", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills over 350m away for Marksmen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIMarksmanKills300mLabel = new Translation("Kills 350m+", TranslationFlags.TMProUI);

    // sniper
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills from 200m to 300m away for Snipers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUISniperKills200mLabel = new Translation("Kills 200m-300m", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills from 300m to 400m away for Snipers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUISniperKills300mLabel = new Translation("Kills 300m-400m", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of primary kills over 400m away for Snipers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUISniperKills400mLabel = new Translation("Kills 400m+", TranslationFlags.TMProUI);

    // ap-rifleman
    [TranslationData(SectionKitMenuUI, "Label that goes in front of vehicle kills for AP Riflemen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIAPRiflemanVehicleKillsLabel = new Translation("Vehicle Kills", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of player trap kills for AP Riflemen in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUIAPRiflemanTrapKillsLabel = new Translation("Trap Kills", TranslationFlags.TMProUI);

    // combat engineer
    [TranslationData(SectionKitMenuUI, "Label that goes in front of shovel points for Combat Engineers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUICombatEngineerShovelsLabel = new Translation("Shovel Points", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of fortifications built for Combat Engineers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUICombatEngineerFortificationsBuiltLabel = new Translation("Fortifications Built", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of emplacements built for Combat Engineers in kit stats.")]
    // ReSharper disable once InconsistentNaming
    public static readonly Translation KitMenuUICombatEngineerEmplacementsBuiltLabel = new Translation("Emplacements Built", TranslationFlags.TMProUI);

    // crewman
    [TranslationData(SectionKitMenuUI, "Label that goes in front of km driven for Crewmen in kit stats.")]
    public static readonly Translation KitMenuUICrewmanKmDrivenLabel = new Translation("Distance Driven (km)", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of driver assists for Crewmen in kit stats.")]
    public static readonly Translation KitMenuUICrewmanDriverAssistsLabel = new Translation("Driver Assists", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of total transport distance built for Crewmen in kit stats.")]
    public static readonly Translation KitMenuUICrewmanTransportDistanceLabel = new Translation("Ttl. Transport Dst.", TranslationFlags.TMProUI);

    // pilot
    [TranslationData(SectionKitMenuUI, "Label that goes in front of km flown for Pilots in kit stats.")]
    public static readonly Translation KitMenuUIPilotKmDrivenLabel = new Translation("Distance Flown (km)", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of pilot assists for Pilots in kit stats.")]
    public static readonly Translation KitMenuUIPilotDriverAssistsLabel = new Translation("Pilot Assists", TranslationFlags.TMProUI);
    [TranslationData(SectionKitMenuUI, "Label that goes in front of total transport distance built for Pilots in kit stats.")]
    public static readonly Translation KitMenuUIPilotTransportDistanceLabel = new Translation("Ttl. Transport Dst.", TranslationFlags.TMProUI);

    // spec ops
    [TranslationData(SectionKitMenuUI, "Label that goes in front of night vision kills for Special Ops in kit stats.")]
    public static readonly Translation KitMenuUISpecOpsNVGKillsLabel = new Translation("NVG Kills (Night)", TranslationFlags.TMProUI);
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
    #endregion

    [FormatDisplay(typeof(object), "Plural")]
    internal const string FormatPlural = "$plural$";
    [FormatDisplay(typeof(object), "Uppercase")]
    internal const string FormatUppercase = "upper";
    [FormatDisplay(typeof(object), "Lowercase")]
    internal const string FormatLowercase = "lower";
    [FormatDisplay(typeof(object), "Proper Case")]
    internal const string FormatPropercase = "proper";
    [FormatDisplay(typeof(float),    "Time (Long, seconds)")]
    [FormatDisplay(typeof(uint),     "Time (Long, seconds)")]
    [FormatDisplay(typeof(int),      "Time (Long, seconds)")]
    [FormatDisplay(typeof(TimeSpan), "Time (Long)")]
    internal const string FormatTimeLong = "tlong";
    [FormatDisplay(typeof(float),    "Time (Long Abbreviated, seconds)")]
    [FormatDisplay(typeof(uint),     "Time (Long Abbreviated, seconds)")]
    [FormatDisplay(typeof(int),      "Time (Long Abbreviated, seconds)")]
    [FormatDisplay(typeof(TimeSpan), "Time (Long Abbreviated)")]
    internal const string FormatTimeLongAbbreviated = "tlonga";
    [FormatDisplay(typeof(float),    "Time (Short mm:ss, seconds)")]
    [FormatDisplay(typeof(uint),     "Time (Short mm:ss, seconds)")]
    [FormatDisplay(typeof(int),      "Time (Short mm:ss, seconds)")]
    [FormatDisplay(typeof(TimeSpan), "Time (Short mm:ss)")]
    // ReSharper disable once InconsistentNaming
    internal const string FormatTimeShort_MM_SS = "tshort1";
    [FormatDisplay(typeof(float),    "Time (Short hh:mm:ss, seconds)")]
    [FormatDisplay(typeof(uint),     "Time (Short hh:mm:ss, seconds)")]
    [FormatDisplay(typeof(int),      "Time (Short hh:mm:ss, seconds)")]
    [FormatDisplay(typeof(TimeSpan), "Time (Short hh:mm:ss)")]
    // ReSharper disable once InconsistentNaming
    internal const string FormatTimeShort_HH_MM_SS = "tshort2";
    [FormatDisplay(typeof(QuestAsset), "Asset Rarity")]
    [FormatDisplay(typeof(VehicleAsset), "Asset Rarity")]
    [FormatDisplay(typeof(ItemAsset), "Asset Rarity")]
    internal const string FormatRarityColor = "rarity";
    public static readonly Translation[] Translations;
    public static readonly Dictionary<string, Translation> Signs;
    internal static readonly List<string> AllLanguages = new List<string>(4);
    static T()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking("Translation reflection");
#endif
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(x => typeof(Translation).IsAssignableFrom(x.FieldType)).ToArray();
        Translations = new Translation[fields.Length];
        int i2 = -1;
        int signCt = 0;
        int publicTranslations = 0;
        for (int i = 0; i < fields.Length; ++i)
        {
            FieldInfo field = fields[i];
            if (typeof(Translation).IsAssignableFrom(field.FieldType))
            {
                if (field.GetValue(null) is not Translation tr)
                    L.LogError("Failed to convert " + field.Name + " to a translation!");
                else if (i2 + 1 < Translations.Length)
                {
                    tr.Key = field.Name;
                    tr.Id = i2;
                    tr.AttributeData = Attribute.GetCustomAttribute(field, typeof(TranslationDataAttribute)) as TranslationDataAttribute;
                    if (tr.AttributeData is not { IsPrioritizedTranslation: false })
                        ++publicTranslations;
                    tr.Init();
                    if (tr.AttributeData is not null)
                    {
                        if (!string.IsNullOrEmpty(tr.AttributeData.SignId))
                            ++signCt;
                    }
                    else L.LogWarning($"Translation missing attribute: {field.Name}{(tr.DeclarationLineNumber.HasValue ? " at: L# " + tr.DeclarationLineNumber?.ToString(Data.AdminLocale) : ".")}");
                    Translations[++i2] = tr;
                }
                else
                    L.LogError("Ran out of space in translation array for " + field.Name + " at " + (i2 + 1), method: "TRANSLATIONS");
            }
        }

        if (Translations.Length != i2 + 1)
        {
            L.LogWarning("Translations had to resize for some reason from " + Translations.Length + " to " + (i2 + 1) + ".",
                method: "TRANSLATIONS");
            Array.Resize(ref Translations, i2 + 1);
        }
        Signs = new Dictionary<string, Translation>(signCt);
        for (int i = 0; i < Translations.Length; ++i)
        {
            Translation tr = Translations[i];
            if (tr.AttributeData is not null && !string.IsNullOrEmpty(tr.AttributeData.SignId))
            {
                if (Signs.ContainsKey(tr.AttributeData.SignId!))
                    L.LogWarning("Duplicate Sign ID: \"" + tr.AttributeData.SignId + "\" in translation \"" + tr.Key + "\".", method: "TRANSLATIONS");
                else
                    Signs.Add(tr.AttributeData.SignId!, tr);
            }
        }
        Localization.ClearSection(TranslationSection.Primary);
        Localization.IncrementSection(TranslationSection.Primary, publicTranslations);
    }
    public static string Translate(this TranslationList list, UCPlayer player) => TryTranslate(list, player.Locale.LanguageInfo, out string local) ? local : string.Empty;
    public static string Translate(this TranslationList list, LanguageInfo language) => TryTranslate(list, language, out string local) ? local : string.Empty;
    public static bool TryTranslate(this TranslationList list, UCPlayer player, out string local) => TryTranslate(list, player.Locale.LanguageInfo, out local);
    public static bool TryTranslate(this TranslationList list, LanguageInfo language, out string local)
    {
        if (list is null)
        {
            local = null!;
            return false;
        }

        language ??= Localization.GetDefaultLanguage();
        if (list.TryGetValue(language.Code, out local) || (!language.IsDefault && list.TryGetValue(L.Default, out local)))
            return true;
        return (local = list.Values.FirstOrDefault()!) != null;
    }
}
