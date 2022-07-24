using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Reflection;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Cache = Uncreated.Warfare.Components.Cache;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare;
internal static class T
{
    private const int NON_TRANSLATION_FIELD_COUNT = 12;
    private const string ERROR_COLOR = "<#ff8c69>";
    private const string SUCCESS_COLOR = "<#e6e3d5>";
    internal const string PLURAL = ":FORMAT_PLURAL";
    internal const string UPPERCASE = "upper";
    internal const string LOWERCASE = "lower";
    internal const string PROPERCASE = "proper";
    public static readonly Translation[] Translations;
    static T()
    {
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Translations = new Translation[fields.Length - NON_TRANSLATION_FIELD_COUNT];
        int i2 = -1;
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
                    Translations[++i2] = tr;
                }
                else
                    L.LogError("Ran out of space in translation array for " + field.Name + " at " + (i2 + 1));
            }
        }

        if (Translations.Length != i2 + 1)
        {
            L.LogWarning("Translations had to resize for some reason from " + Translations.Length + " to " + (i2 + 1) + ". Check to make sure there's only one field that isn't a translation.");
            Array.Resize(ref Translations, i2 + 1);
        }
    }

    /*
     * c$value$ will be replaced by the color "value" on startup
     */

    #region Common Errors
    private const string SECTION_COMMON_ERRORS = "Common_Errors";

    [TranslationData(
        Section = SECTION_COMMON_ERRORS,
        Description = "Sent when a command is not used correctly.",
        LegacyTranslationId = "correct_usage",
        FormattingDescriptions = new string[] { "Command usage." })]
    public static readonly Translation<string> CorrectUsage = new Translation<string>(ERROR_COLOR + "Correct usage: {0}.");

    [TranslationData(
        Section = SECTION_COMMON_ERRORS,
        Description = "A command or feature hasn't been completed or implemented.",
        LegacyTranslationId = "todo")]
    public static readonly Translation NotImplemented   = new Translation(ERROR_COLOR + "This command hasn't been implemented yet.");

    [TranslationData(
        Section = SECTION_COMMON_ERRORS,
        Description = "A command or feature can only be used by the server console.",
        LegacyTranslationId = "command_e_no_console")]
    public static readonly Translation ConsoleOnly      = new Translation(ERROR_COLOR + "This command can only be called from console.");

    [TranslationData(
        Section = SECTION_COMMON_ERRORS,
        Description = "A command or feature can only be used by a player (instead of the server console).",
        LegacyTranslationId = "command_e_no_player")]
    public static readonly Translation PlayersOnly      = new Translation(ERROR_COLOR + "This command can not be called from console.");

    [TranslationData(
        Section = SECTION_COMMON_ERRORS,
        Description = "A player name or ID search turned up no results.",
        LegacyTranslationId = "command_e_player_not_found")]
    public static readonly Translation PlayerNotFound   = new Translation(ERROR_COLOR + "Player not found.");

    [TranslationData(
        Section = SECTION_COMMON_ERRORS,
        Description = "A command didn't respond to an interaction, or a command chose to throw a vague error response to an uncommon problem.",
        LegacyTranslationId = "command_e_unknown_error")]
    public static readonly Translation UnknownError     = new Translation(ERROR_COLOR + "We ran into an unknown error executing that command.");

    [TranslationData(
        Section = SECTION_COMMON_ERRORS,
        Description = "A command is disabled in the current gamemode type (ex, /deploy in a gamemode without FOBs).",
        LegacyTranslationId = "command_e_gamemode")]
    public static readonly Translation GamemodeError    = new Translation(ERROR_COLOR + "This command is not enabled in this gamemode.");

    [TranslationData(
        Section = SECTION_COMMON_ERRORS,
        Description = "The caller of a command is not allowed to use the command.",
        LegacyTranslationId = "no_permissions")]
    public static readonly Translation NoPermissions    = new Translation(ERROR_COLOR + "You do not have permission to use this command.");

    [TranslationData(
        Section = SECTION_COMMON_ERRORS,
        Description = "A command or feature is turned off in the configuration.",
        LegacyTranslationId = "not_enabled")]
    public static readonly Translation NotEnabled       = new Translation(ERROR_COLOR + "This feature is not currently enabled.");

    [TranslationData(
        Section = SECTION_COMMON_ERRORS,
        Description = "The caller of a command has permission to use the command but isn't on duty.",
        LegacyTranslationId = "no_permissions_on_duty")]
    public static readonly Translation NotOnDuty        = new Translation(ERROR_COLOR + "You must be on duty to execute that command.");
    #endregion

    #region Flags
    private const string SECTION_FLAGS = "Flags";
    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "The caller of a command isn't on team 1 or 2.",
        LegacyTranslationId = "gamemode_flag_not_on_cap_team")]
    public static readonly Translation NotOnCaptureTeam             = new Translation(ERROR_COLOR + "You're not on a team that can capture flags.");

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Sent when the player enters the capture radius of an active flag.",
        FormattingDescriptions = new string[] { "Objective in question" },
        LegacyTranslationId = "entered_cap_radius")]
    public static readonly Translation<Flag> EnteredCaptureRadius   = new Translation<Flag>(SUCCESS_COLOR + "You have entered the capture radius of {0}.", Flag.NAME_FORMAT_COLORED);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Sent when the player leaves the capture radius of an active flag.",
        FormattingDescriptions = new string[] { "Objective in question" },
        LegacyTranslationId = "left_cap_radius")]
    public static readonly Translation<Flag> LeftCaptureRadius      = new Translation<Flag>(SUCCESS_COLOR + "You have left the capture radius of {0}.", Flag.NAME_FORMAT_COLORED);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Sent to all players on a flag that's being captured by their team (from neutral).",
        FormattingDescriptions = new string[] { "Objective in question" },
        LegacyTranslationId = "capturing")]
    public static readonly Translation<Flag> FlagCapturing          = new Translation<Flag>(SUCCESS_COLOR + "Your team is capturing {0}!", Flag.NAME_FORMAT_COLORED);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Sent to all players on a flag that's being captured by the other team.",
        FormattingDescriptions = new string[] { "Objective in question" },
        LegacyTranslationId = "losing")]
    public static readonly Translation<Flag> FlagLosing             = new Translation<Flag>(ERROR_COLOR + "Your team is losing {0}!", Flag.NAME_FORMAT_COLORED);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Sent to all players on a flag when it begins being contested.",
        FormattingDescriptions = new string[] { "Objective in question" },
        LegacyTranslationId = "contested")]
    public static readonly Translation<Flag> FlagContested          = new Translation<Flag>("<#c$contested$>{0} is contested, eliminate some enemies to secure it!", Flag.NAME_FORMAT_COLORED);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Sent to all players on a flag that's being cleared by their team (from the other team's ownership).",
        FormattingDescriptions = new string[] { "Objective in question" },
        LegacyTranslationId = "clearing")]
    public static readonly Translation<Flag> FlagClearing           = new Translation<Flag>(SUCCESS_COLOR + "Your team is clearing {0}!", Flag.NAME_FORMAT_COLORED);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Sent to all players on a flag when it gets secured by their team.",
        FormattingDescriptions = new string[] { "Objective in question" },
        LegacyTranslationId = "secured")]
    public static readonly Translation<Flag> FlagSecured            = new Translation<Flag>("<#c$secured$>{0} is secure for now, keep up the defense.", Flag.NAME_FORMAT_COLORED);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Sent to a player that walks in the radius of a flag that isn't their team's objective.",
        FormattingDescriptions = new string[] { "Objective in question" },
        LegacyTranslationId = "nocap")]
    public static readonly Translation<Flag> FlagNoCap              = new Translation<Flag>("<#c$nocap$>{0} is not your objective, check the right of your screen to see which points to attack and defend.", Flag.NAME_FORMAT_COLORED);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Sent to a player that walks in the radius of a flag that is owned by the other team and enough of the other team is on the flag so they can't contest the point.",
        FormattingDescriptions = new string[] { "Objective in question" },
        LegacyTranslationId = "notowned")]
    public static readonly Translation<Flag> FlagNotOwned           = new Translation<Flag>("<#c$nocap$>{0} is owned by the enemies. Get more players to capture it.", Flag.NAME_FORMAT_COLORED);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Sent to a player that walks in the radius of a flag that is owned by the other team and has been locked from recapture.",
        FormattingDescriptions = new string[] { "Objective in question" },
        LegacyTranslationId = "locked")]
    public static readonly Translation<Flag> FlagLocked             = new Translation<Flag>("<#c$locked$>{0} has already been captured, try to protect the objective to win.", Flag.NAME_FORMAT_COLORED);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Sent to all players when a flag gets neutralized.",
        FormattingDescriptions = new string[] { "Objective in question" },
        LegacyTranslationId = "flag_neutralized")]
    public static readonly Translation<Flag> FlagNeutralized        = new Translation<Flag>(SUCCESS_COLOR + "{0} has been neutralized!", Flag.NAME_FORMAT_COLORED_DISCOVER);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Backup translation for team 0 name and short name.",
        LegacyTranslationId = "neutral")]
    public static readonly Translation Neutral          = new Translation("Neutral",       TranslationFlags.UnityUI);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Shows in place of the objective name for an undiscovered flag or objective.",
        LegacyTranslationId = "undiscovered_flag")]
    public static readonly Translation UndiscoveredFlag = new Translation("unknown",       TranslationFlags.UnityUI);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's team is capturing a flag they're on.",
        LegacyTranslationId = "ui_capturing")]
    public static readonly Translation UICapturing      = new Translation("CAPTURING",     TranslationFlags.UnityUI);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's team is losing a flag they're on because there isn't enough of them to contest it.",
        LegacyTranslationId = "ui_losing")]
    public static readonly Translation UILosing         = new Translation("LOSING",        TranslationFlags.UnityUI);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's team is clearing a flag they're on.",
        LegacyTranslationId = "ui_clearing")]
    public static readonly Translation UIClearing       = new Translation("CLEARING",      TranslationFlags.UnityUI);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's team is contested with the other team on the flag they're on.",
        LegacyTranslationId = "ui_contested")]
    public static readonly Translation UIContested      = new Translation("CONTESTED",     TranslationFlags.UnityUI);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's team owns flag they're on.",
        LegacyTranslationId = "ui_secured")]
    public static readonly Translation UISecured        = new Translation("SECURED",       TranslationFlags.UnityUI);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's on a flag that isn't their team's objective.",
        LegacyTranslationId = "ui_nocap")]
    public static readonly Translation UINoCap          = new Translation("NOT OBJECTIVE", TranslationFlags.UnityUI);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's team has too few people on a flag to contest and the other team owns the flag.",
        LegacyTranslationId = "ui_notowned")]
    public static readonly Translation UINotOwned       = new Translation("TAKEN",         TranslationFlags.UnityUI);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the objective they're on is owned by the other team and is locked from recapture.",
        LegacyTranslationId = "ui_locked")]
    public static readonly Translation UILocked         = new Translation("LOCKED",        TranslationFlags.UnityUI);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's in a vehicle on their objective.",
        LegacyTranslationId = "ui_invehicle")]
    public static readonly Translation UIInVehicle      = new Translation("IN VEHICLE",    TranslationFlags.UnityUI);

    [TranslationData(
        Section = SECTION_FLAGS,
        Description = "Shows above the flag list UI.",
        LegacyTranslationId = "ui_capturing")]
    public static readonly Translation FlagsHeader      = new Translation("Flags",         TranslationFlags.UnityUI);
    #endregion

    #region Teams
    public static readonly Translation<FactionInfo> EnteredMain                 = new Translation<FactionInfo>(SUCCESS_COLOR + "You have entered the safety of {0} headquarters!", FactionInfo.DISPLAY_NAME_COLORIZED_FORMAT);
    public static readonly Translation<FactionInfo> LeftMain                    = new Translation<FactionInfo>(SUCCESS_COLOR + "You have left the safety of {0} headquarters!", FactionInfo.DISPLAY_NAME_COLORIZED_FORMAT);
    public static readonly Translation<FactionInfo> TeamJoinDM                  = new Translation<FactionInfo>("<#a0ad8e>You've joined {0}.", FactionInfo.DISPLAY_NAME_COLORIZED_FORMAT);
    public static readonly Translation<FactionInfo, IPlayer> TeamJoinAnnounce   = new Translation<FactionInfo, IPlayer>("<#a0ad8e>{1} joined {0}!", FactionInfo.DISPLAY_NAME_COLORIZED_FORMAT, UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT);
    public static readonly Translation<FactionInfo> TeamWin                     = new Translation<FactionInfo>("<#a0ad8e>{0} has won the battle!", FactionInfo.DISPLAY_NAME_COLORIZED_FORMAT);
    public static readonly Translation<FactionInfo, Flag> TeamCaptured          = new Translation<FactionInfo, Flag>("<#a0ad8e>{0} captured {1}.", FactionInfo.DISPLAY_NAME_COLORIZED_FORMAT, Flag.NAME_FORMAT_COLORED_DISCOVER);
    #endregion

    #region Players
    public static readonly Translation<IPlayer> PlayerConnected                 = new Translation<IPlayer>(SUCCESS_COLOR + "{0} joined the server.");
    public static readonly Translation<IPlayer> PlayerDisconnected              = new Translation<IPlayer>(SUCCESS_COLOR + "{0} left the server.");
    public static readonly Translation<string>   NullTransformKickMessage       = new Translation<string>("Your character is bugged, which messes up our zone plugin. Rejoin or contact a Director if this continues. (discord.gg/{0}).", TranslationFlags.NoColor);
    public static readonly Translation<string>   ChatFilterFeedback             = new Translation<string>(ERROR_COLOR + "Our chat filter flagged <#fdfdfd>{0}</color>, so the message wasn't sent.");
    #endregion

    #region Leaderboards

    #region Shared
    public static readonly Translation StartingSoon                   = new Translation("Starting soon...", TranslationFlags.UnityUI);
    public static readonly Translation<string> NextGameShutdown       = new Translation<string>("<#94cbff>Shutting Down Because: \"{0}\"</color>", TranslationFlags.UnityUI);
    public static readonly Translation<TimeSpan> NextGameShutdownTime = new Translation<TimeSpan>("{0}", TranslationFlags.UnityUI, "mm:ss");

    public static readonly Translation<FactionInfo, FactionInfo> WarstatsHeader = new Translation<FactionInfo, FactionInfo>("{0} vs {1}", TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_COLORIZED_FORMAT, FactionInfo.SHORT_NAME_COLORIZED_FORMAT);
    public static readonly Translation<IPlayer, float> PlayerstatsHeader       = new Translation<IPlayer, float>("{0} - {1}% presence", TranslationFlags.UnityUI, UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT, "P0");
    public static readonly Translation<FactionInfo> WinnerTitle                 = new Translation<FactionInfo>("{0} Wins!", TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_COLORIZED_FORMAT);

    public static readonly Translation<float, string, IPlayer> LongestShot     = new Translation<float, string, IPlayer>("{0} Wins!", TranslationFlags.UnityUI, "F1", arg3Fmt: UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT);
    #endregion

    #region CTFBase
    public static readonly Translation CTFPlayerStats0  = new Translation("Kills: ",            TranslationFlags.UnityUI);
    public static readonly Translation CTFPlayerStats1  = new Translation("Deaths: ",           TranslationFlags.UnityUI);
    public static readonly Translation CTFPlayerStats2  = new Translation("K/D Ratio: ",        TranslationFlags.UnityUI);
    public static readonly Translation CTFPlayerStats3  = new Translation("Kills on Point: ",   TranslationFlags.UnityUI);
    public static readonly Translation CTFPlayerStats4  = new Translation("Time Deployed: ",    TranslationFlags.UnityUI);
    public static readonly Translation CTFPlayerStats5  = new Translation("XP Gained: ",        TranslationFlags.UnityUI);
    public static readonly Translation CTFPlayerStats6  = new Translation("Time on Point: ",    TranslationFlags.UnityUI);
    public static readonly Translation CTFPlayerStats7  = new Translation("Captures: ",         TranslationFlags.UnityUI);
    public static readonly Translation CTFPlayerStats8  = new Translation("Time in Vehicle: ",  TranslationFlags.UnityUI);
    public static readonly Translation CTFPlayerStats9  = new Translation("Teamkills: ",        TranslationFlags.UnityUI);
    public static readonly Translation CTFPlayerStats10 = new Translation("FOBs Destroyed: ",   TranslationFlags.UnityUI);
    public static readonly Translation CTFPlayerStats11 = new Translation("Credits Gained: ",   TranslationFlags.UnityUI);

    public static readonly Translation CTFWarStats0 = new Translation("Duration: ", TranslationFlags.UnityUI);
    public static readonly Translation<FactionInfo> CTFWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",     TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation<FactionInfo> CTFWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",     TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation CTFWarStats3 = new Translation("Flag Captures: ", TranslationFlags.UnityUI);
    public static readonly Translation<FactionInfo> CTFWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",   TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation<FactionInfo> CTFWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",   TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation<FactionInfo> CTFWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",    TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation<FactionInfo> CTFWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",    TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation<FactionInfo> CTFWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ", TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation<FactionInfo> CTFWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ", TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation CTFWarStats10 = new Translation("Teamkill Casualties: ", TranslationFlags.UnityUI);
    public static readonly Translation CTFWarStats11 = new Translation("Longest Shot: ",        TranslationFlags.UnityUI);

    public static readonly Translation CTFHeader0 = new Translation("Kills",   TranslationFlags.UnityUI);
    public static readonly Translation CTFHeader1 = new Translation("Deaths",  TranslationFlags.UnityUI);
    public static readonly Translation CTFHeader2 = new Translation("XP",      TranslationFlags.UnityUI);
    public static readonly Translation CTFHeader3 = new Translation("Credits", TranslationFlags.UnityUI);
    public static readonly Translation CTFHeader4 = new Translation("Caps",    TranslationFlags.UnityUI);
    public static readonly Translation CTFHeader5 = new Translation("Damage",  TranslationFlags.UnityUI);
    #endregion

    #region CTFBase
    public static readonly Translation InsurgencyPlayerStats0  = new Translation("Kills: ",                 TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyPlayerStats1  = new Translation("Deaths: ",                TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyPlayerStats2  = new Translation("Damage Done: ",           TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyPlayerStats3  = new Translation("Objective Kills: ",       TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyPlayerStats4  = new Translation("Time Deployed: ",         TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyPlayerStats5  = new Translation("XP Gained: ",             TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyPlayerStats6  = new Translation("Intelligence Gathered: ", TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyPlayerStats7  = new Translation("Caches Discovered: ",     TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyPlayerStats8  = new Translation("Caches Destroyed: ",      TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyPlayerStats9  = new Translation("Teamkills: ",             TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyPlayerStats10 = new Translation("FOBs Destroyed: ",        TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyPlayerStats11 = new Translation("Credits Gained: ",        TranslationFlags.UnityUI);

    public static readonly Translation InsurgencyWarStats0 = new Translation("Duration: ", TranslationFlags.UnityUI);
    public static readonly Translation<FactionInfo> InsurgencyWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation<FactionInfo> InsurgencyWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation InsurgencyWarStats3 = new Translation("Intelligence Gathered: ", TranslationFlags.UnityUI);
    public static readonly Translation<FactionInfo> InsurgencyWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation<FactionInfo> InsurgencyWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation<FactionInfo> InsurgencyWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation<FactionInfo> InsurgencyWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation<FactionInfo> InsurgencyWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation<FactionInfo> InsurgencyWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
    public static readonly Translation InsurgencyWarStats10 = new Translation("Teamkill Casualties: ", TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyWarStats11 = new Translation("Longest Shot: ",        TranslationFlags.UnityUI);

    public static readonly Translation InsurgencyHeader0 = new Translation("Kills",   TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyHeader1 = new Translation("Deaths",  TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyHeader2 = new Translation("XP",      TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyHeader3 = new Translation("Credits", TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyHeader4 = new Translation("KDR",     TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyHeader5 = new Translation("Damage",  TranslationFlags.UnityUI);
    #endregion

    #endregion

    #region GroupCommand
    public static readonly Translation<ulong, string, Color> CurrentGroup = new Translation<ulong, string, Color>(SUCCESS_COLOR + "Group <#{2}>{0}</color>: <#{2}>{1}</color>");
    public static readonly Translation<ulong, string, Color> CreatedGroup = new Translation<ulong, string, Color>(SUCCESS_COLOR + "Created group <#{2}>{0}</color>: <#{2}>{1}</color>");
    public static readonly Translation<ulong, string, Color> JoinedGroup  = new Translation<ulong, string, Color>(SUCCESS_COLOR + "You have joined group <#{2}>{0}</color>: <#{2}>{1}</color>.");
    public static readonly Translation CantCreateGroup      = new Translation(ERROR_COLOR + "You can't create a group right now.");
    public static readonly Translation NotInGroup           = new Translation(ERROR_COLOR + "You aren't in a group.");
    public static readonly Translation AlreadyInGroup       = new Translation(ERROR_COLOR + "You are already in that group.");
    public static readonly Translation<ulong> GroupNotFound = new Translation<ulong>(ERROR_COLOR + "Could not find group <#4785ff>{0}</color>.");
    #endregion

    #region LangCommand
    public static readonly Translation<string> LanguageList              = new Translation<string>("<#f53b3b>Languages: <#e6e3d5>{0}</color>.");
    public static readonly Translation ResetLanguageHow                  = new Translation("<#f53b3b>Do <#e6e3d5>/lang reset</color> to reset back to default language.");
    public static readonly Translation<LanguageAliasSet> LanguageCurrent = new Translation<LanguageAliasSet>("<#f53b3b>Current language: <#e6e3d5>{0}</color>.", LanguageAliasSet.DISPLAY_NAME_FORMAT);
    public static readonly Translation<LanguageAliasSet> ChangedLanguage = new Translation<LanguageAliasSet>("<#f53b3b>Changed your language to <#e6e3d5>{0}</color>.", LanguageAliasSet.DISPLAY_NAME_FORMAT);
    public static readonly Translation<LanguageAliasSet> LangAlreadySet  = new Translation<LanguageAliasSet>(ERROR_COLOR + "You are already set to <#e6e3d5>{0}</color>.", LanguageAliasSet.DISPLAY_NAME_FORMAT);
    public static readonly Translation<LanguageAliasSet> ResetLanguage   = new Translation<LanguageAliasSet>("<#f53b3b>Reset your language to <#e6e3d5>{0}</color>.", LanguageAliasSet.DISPLAY_NAME_FORMAT);
    public static readonly Translation<LanguageAliasSet> ResetCurrent    = new Translation<LanguageAliasSet>(ERROR_COLOR + "You are already on the default language: <#e6e3d5>{0}</color>.", LanguageAliasSet.DISPLAY_NAME_FORMAT);
    public static readonly Translation<string> LanguageNotFound          = new Translation<string>("<#dd1111>We don't have translations for <#e6e3d5>{0}</color> yet. If you are fluent and want to help, feel free to ask us about submitting translations.", LanguageAliasSet.DISPLAY_NAME_FORMAT);
    #endregion

    #region Toasts
    public static readonly Translation<IPlayer> WelcomeBackMessage = new Translation<IPlayer>("Thanks for playing <#c$uncreated$>Uncreated Warfare</color>!\nWelcome back {0}.", TranslationFlags.UnityUI, UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer> WelcomeMessage     = new Translation<IPlayer>("Welcome to <#c$uncreated$>Uncreated Warfare</color> {0}!\nTalk to the NPCs to get started.", TranslationFlags.UnityUI, UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT);
    #endregion

    #region KitCommand
    public static readonly Translation<Kit> KitCreated          = new Translation<Kit>("<#a0ad8e>Created kit: <#fff>{0}</color>.", Kit.ID_FORMAT);
    public static readonly Translation<Kit> KitOverwrote        = new Translation<Kit>("<#a0ad8e>Overwritten items for kit: <#fff>{0}</color>.", Kit.ID_FORMAT);
    public static readonly Translation<Kit, Kit> KitCopied      = new Translation<Kit, Kit>("<#a0ad8e>Copied data from <#c7b197>{0}</color> into a new kit: <#fff>{0}</color>.", Kit.ID_FORMAT, Kit.ID_FORMAT);
    public static readonly Translation<Kit> KitDeleted          = new Translation<Kit>("<#a0ad8e>Deleted kit: <#fff>{0}</color>.", Kit.ID_FORMAT);
    public static readonly Translation<string> KitSearchResults = new Translation<string>("<#a0ad8e>Matches: <i>{0}</i>.");
    public static readonly Translation<Kit> KitAccessGivenDm    = new Translation<Kit>("<#a0ad8e>You were given access to the kit: <#fff>{0}</color>.", Kit.ID_FORMAT);
    public static readonly Translation<Kit> KitAccessRevokedDm  = new Translation<Kit>("<#a0ad8e>Your access to <#fff>{0}</color> was revoked.", Kit.ID_FORMAT);
    public static readonly Translation<string, Kit, string> KitPropertySet    = new Translation<string, Kit, string>("<#a0ad8e>Set <#aaa>{0}</color> on kit <#fff>{1}</color> to <#aaa><uppercase>{2}</uppercase></color>.", arg2Fmt: Kit.ID_FORMAT);
    public static readonly Translation<string> KitNameTaken                   = new Translation<string>(ERROR_COLOR + "A kit named <#fff>{0}</color> already exists.");
    public static readonly Translation<string> KitNotFound                    = new Translation<string>(ERROR_COLOR + "A kit named <#fff>{0}</color> doesn't exists.");
    public static readonly Translation<string> KitPropertyNotFound            = new Translation<string>(ERROR_COLOR + "Kits don't have a <#eee>{0}</color> property.");
    public static readonly Translation<string> KitPropertyProtected           = new Translation<string>(ERROR_COLOR + "<#eee>{0}</color> can not be changed on kits.");
    public static readonly Translation<IPlayer, Kit> KitAlreadyHasAccess      = new Translation<IPlayer, Kit>(ERROR_COLOR + "{0} already has access to <#fff>{1}</color>.", UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT, Kit.ID_FORMAT);
    public static readonly Translation<IPlayer, Kit> KitAlreadyMissingAccess  = new Translation<IPlayer, Kit>(ERROR_COLOR + "{0} doesn't have access to <#fff>{1}</color>.", UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT, Kit.ID_FORMAT);
    public static readonly Translation<Cooldown> KitOnCooldown                = new Translation<Cooldown>(ERROR_COLOR + "You can request this kit again in: <#bafeff>{0}</color>.", Cooldown.TIMESTAMP_LEFT_FORMAT);
    public static readonly Translation<Cooldown> KitOnGlobalCooldown          = new Translation<Cooldown>(ERROR_COLOR + "You can request another kit again in: <#bafeff>{0}</color>.", Cooldown.TIMESTAMP_LEFT_FORMAT);
    public static readonly Translation<IPlayer, IPlayer, Kit> KitAccessGiven         = new Translation<IPlayer, IPlayer, Kit>("<#a0ad8e>{0} (<#aaa>{1}</color>) was given access to the kit: <#fff>{2}</color>.", UCPlayer.COLORIZED_PLAYER_NAME_FORMAT, UCPlayer.STEAM_64_FORMAT, Kit.ID_FORMAT);
    public static readonly Translation<IPlayer, IPlayer, Kit> KitAccessRevoked       = new Translation<IPlayer, IPlayer, Kit>("<#a0ad8e>{0} (<#aaa>{1}</color>)'s access to <#fff>{2}</color> was taken away.", UCPlayer.COLORIZED_PLAYER_NAME_FORMAT, UCPlayer.STEAM_64_FORMAT, Kit.ID_FORMAT);
    public static readonly Translation<string, Type, string> KitInvalidPropertyValue = new Translation<string, Type, string>(ERROR_COLOR + "<#fff>{2}</color> isn't a valid value for <#eee>{0}</color> (<#aaa>{1}</color>).");
    public static readonly Translation<EClass, IPlayer, IPlayer, Kit> LoadoutCreated = new Translation<EClass, IPlayer, IPlayer, Kit>("<#a0ad8e>Created <#bbc>{0}</color> loadout for {1} (<#aaa>{2}</color>). Kit name: <#fff>{3}</color>.", arg2Fmt: UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT, arg3Fmt: UCPlayer.STEAM_64_FORMAT, arg4Fmt: Kit.ID_FORMAT);
    #endregion

    #region RangeCommand
    public static readonly Translation<float> RangeOutput  = new Translation<float>("<#9e9c99>The range to your squad's marker is: <#8aff9f>{0}m</color>.", "N0");
    public static readonly Translation RangeNoMarker       = new Translation("<#9e9c99>You squad has no marker.");
    public static readonly Translation RangeNotSquadleader = new Translation("<#9e9c99>Only <color=#cedcde>SQUAD LEADERS</color> can place markers.");
    public static readonly Translation RangeNotInSquad     = new Translation("<#9e9c99>You must JOIN A SQUAD in order to do /range.");
    #endregion

    #region Squads
    public static readonly Translation SquadNotOnTeam               = new Translation("<#a89791>You can't join a squad unless you're on a team.");
    public static readonly Translation<Squad> SquadCreated          = new Translation<Squad>("<#a0ad8e>You created {0} squad.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> SquadJoined           = new Translation<Squad>("<#a0ad8e>You joined {0} squad.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> SquadLeft             = new Translation<Squad>("<#a7a8a5>You left {0} squad.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> SquadDisbanded        = new Translation<Squad>("<#a7a8a5>{0} squad was disbanded.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation SquadLockedSquad             = new Translation("<#a7a8a5>You <#6be888>locked</color> your squad.");
    public static readonly Translation SquadUnlockedSquad           = new Translation("<#999e90>You <#6be888>unlocked</color> your squad.");
    public static readonly Translation<Squad> SquadPromoted         = new Translation<Squad>("<#999e90>You're now the <#cedcde>sqauad leader</color> of {0}.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> SquadKicked           = new Translation<Squad>("<#ae8f8f>You were kicked from {0} squad.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<string> SquadNotFound        = new Translation<string>("<#ae8f8f>Failed to find a squad called <#c$neutral$>\"{0}\"</color>. You can also use the first letter of the squad name.");
    public static readonly Translation SquadAlreadyInSquad          = new Translation("<#ae8f8f>You're already in a squad.");
    public static readonly Translation SquadNotInSquad              = new Translation("<#ae8f8f>You're not in a squad yet. Use <#ae8f8f>/squad join <squad></color> to join a squad.");
    public static readonly Translation SquadNotSquadLeader          = new Translation("<#ae8f8f>You're not the leader of your squad.");
    public static readonly Translation<Squad> SquadLocked           = new Translation<Squad>("<#a89791>{0} is locked.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> SquadFull             = new Translation<Squad>("<#a89791>{0} is full.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation SquadTargetNotInSquad        = new Translation("<#a89791>That player isn't in a squad.");
    public static readonly Translation<IPlayer> SquadPlayerJoined   = new Translation<IPlayer>("<#b9bdb3>{0} joined your squad.", UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer> SquadPlayerLeft     = new Translation<IPlayer>("<#b9bdb3>{0} left your squad.", UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer> SquadPlayerPromoted = new Translation<IPlayer>("<#b9bdb3>{0} was promoted to <#cedcde>sqauad leader</color>.", UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer> SquadPlayerKicked   = new Translation<IPlayer>("<#b9bdb3>{0} was kicked from your squad.", UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT);
    public static readonly Translation SquadsDisabled               = new Translation("<#a89791>Squads are disabled in this gamemode.");
    public static readonly Translation<int> SquadsTooMany           = new Translation<int>("<#a89791>There can not be more than {0} squads on a team at once.");

    public static readonly Translation<Squad, int, int> SquadsUIHeaderPlayerCount = new Translation<Squad, int, int>("<#bd6b5b>{0}</color {1}/{2}", TranslationFlags.UnityUI, Squad.NAME_FORMAT);
    public static readonly Translation<int, int> SquadsUIPlayerCountSmall         = new Translation<int, int>("{0}/{1}", TranslationFlags.UnityUI);
    public static readonly Translation<int, int> SquadsUIPlayerCountSmallLocked   = new Translation<int, int>("<#969696>{0}/{1}</color>", TranslationFlags.UnityUI);
    public static readonly Translation squad_ui_expanded                          = new Translation("...", TranslationFlags.UnityUI);
    #endregion

    #region Orders
    public static readonly Translation OrderUsageAll              = new Translation("<#9fa1a6>To give orders: <#9dbccf>/order <squad> <type></color>. Type <#d1bd90>/order actions</color> to see a list of actions.");
    public static readonly Translation<Squad> OrderUsageNoAction  = new Translation<Squad>("<#9fa1a6>Try typing: <#9dbccf>/order <lowercase>{0}</lowercase> <action></color>.", Squad.NAME_FORMAT);
    public static readonly Translation<Squad> OrderUsageBadAction = new Translation<Squad>("<#9fa1a6>Try typing: <#9dbccf>/order <lowercase>{0}</lowercase> <b><action></b></color>. Type <#d1bd90>/order actions</color> to see a list of actions.", Squad.NAME_FORMAT);
    public static readonly Translation<string> OrderActions       = new Translation<string>("<#9fa1a6>Order actions: <#9dbccf>{0}</color>.");
    public static readonly Translation<string> OrderSquadNoExist  = new Translation<string>(ERROR_COLOR + "There is no friendly <lowercase><#c$neutral$>{0}</color></lowercase> squad.");
    public static readonly Translation OrderNotSquadleader        = new Translation(ERROR_COLOR + "You must be a <#cedcde>sqauad leader</color> to give orders.");
    public static readonly Translation<string, string> OrderActionInvalid = new Translation<string, string>(ERROR_COLOR + "<#fff>{0}</color> is not a valid action. Try one of these: <#9dbccf>{1}</color>.");
    public static readonly Translation<Squad> OrderAttackMarkerCTF  = new Translation<Squad>(ERROR_COLOR + "Place a map marker on a <#d1bd90>position</color> or <#d1bd90>flag</color> where you want {0} to attack.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> OrderAttackMarkerIns  = new Translation<Squad>(ERROR_COLOR + "Place a map marker on a <#d1bd90>position</color> or <#d1bd90>cache</color> where you want {0} to attack.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> OrderDefenseMarkerCTF = new Translation<Squad>(ERROR_COLOR + "Place a map marker on a <#d1bd90>position</color> or <#d1bd90>flag</color> where you want {0} to defend.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> OrderDefenseMarkerIns = new Translation<Squad>(ERROR_COLOR + "Place a map marker on a <#d1bd90>position</color> or <#d1bd90>cache</color> where you want {0} to defend.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> OrderBuildFOBError    = new Translation<Squad>(ERROR_COLOR + "Place a map marker on a <#d1bd90>position</color> you want {0} to build a <color=#d1bd90>FOB</color>.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> OrderMoveError        = new Translation<Squad>(ERROR_COLOR + "Place a map marker on a <#d1bd90>position</color> you want {0} to move to.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation OrderBuildFOBExists          = new Translation(ERROR_COLOR + "There is already a friendly FOB near that marker.");
    public static readonly Translation OrderBuildFOBTooMany         = new Translation(ERROR_COLOR + "There are already too many FOBs on your team.");
    public static readonly Translation OrderSquadTooClose           = new Translation(ERROR_COLOR + "{0} is already near that marker. Try placing it further away.");
    public static readonly Translation<Order> OrderSent                = new Translation<Order>("<#9fa1a6>Order sent to {0}: <#9dbccf>{1}</color>.");
    public static readonly Translation<IPlayer, Order> OrderReceived   = new Translation<IPlayer, Order>("<#9fa1a6>{0} has given your squad new orders:" + Environment.NewLine + "<#d4d4d4>{1}</color>.", UCPlayer.COLORIZED_CHARACTER_NAME_FORMAT, Order.MESSAGE_FORMAT);
    public static readonly Translation<IPlayer> OrderUICommander       = new Translation<IPlayer>("Orders from <#a7becf>{0}</color>:", TranslationFlags.UnityUI, UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<Order> OrderUIMessage           = new Translation<Order>("{0}", TranslationFlags.UnityUI, Order.MESSAGE_FORMAT);
    public static readonly Translation<TimeSpan> OrderUITimeLeft       = new Translation<TimeSpan>("- {0}m left", TranslationFlags.UnityUI, "%m");
    public static readonly Translation<int> OrderUIReward              = new Translation<int>("- Reward: {0} XP", TranslationFlags.UnityUI);
    public static readonly Translation<Flag> OrderUIAttackObjective    = new Translation<Flag>("Attack your objective: {0}.", TranslationFlags.UnityUI, Flag.SHORT_NAME_FORMAT_COLORED);
    public static readonly Translation<Flag> OrderUIAttackFlag         = new Translation<Flag>("Attack: {0}.", TranslationFlags.UnityUI, Flag.SHORT_NAME_FORMAT_COLORED);
    public static readonly Translation<Flag> OrderUIDefendObjective    = new Translation<Flag>("Defend your objective: {0}.", TranslationFlags.UnityUI, Flag.SHORT_NAME_FORMAT_COLORED);
    public static readonly Translation<Flag> OrderUIDefendFlag         = new Translation<Flag>("Defend: {0}.", TranslationFlags.UnityUI, Flag.SHORT_NAME_FORMAT_COLORED);
    public static readonly Translation<Cache> OrderUIAttackCache       = new Translation<Cache>("Attack: {0}.", TranslationFlags.UnityUI, FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<Cache> OrderUIDefendCache       = new Translation<Cache>("Defend: {0}.", TranslationFlags.UnityUI, FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<string> OrderUIAttackNearArea   = new Translation<string>("Attack near <#9dbccf>{0}</color>.", TranslationFlags.UnityUI);
    public static readonly Translation<string> OrderUIDefendNearArea   = new Translation<string>("Defend near <#9dbccf>{0}</color>.", TranslationFlags.UnityUI);
    public static readonly Translation<Flag> OrderUIBuildFobFlag       = new Translation<Flag>("Build a FOB on {0}.", TranslationFlags.UnityUI, Flag.SHORT_NAME_FORMAT_COLORED);
    public static readonly Translation<string> OrderUIBuildFobNearArea = new Translation<string>("Build a FOB near <#9dbccf>{0}</color>.", TranslationFlags.UnityUI, Flag.SHORT_NAME_FORMAT_COLORED);
    public static readonly Translation<Cache> OrderUIBuildFobNearCache = new Translation<Cache>("Build a FOB near {0}.", TranslationFlags.UnityUI, FOB.COLORED_NAME_FORMAT);
    #endregion

    #region Rallies
    public static readonly Translation RallySuccess         = new Translation("<#959c8c>You have <#5eff87>rallied</color> with your squad.");
    public static readonly Translation RallyActive          = new Translation("<#959c8c>Your squad has an active <#5eff87>RALLY POINT</color>. Do <#bfbfbf>/rally</color> to rally with your squad.");
    public static readonly Translation<int> RallyWait       = new Translation<int>("<#959c8c>Standby for <#5eff87>RALLY</color> in: <#ffe4b5>{0}s</color>. Do <#a3b4c7>/rally cancel</color> to abort.");
    public static readonly Translation RallyAbort           = new Translation("<#a1a1a1>Cancelled rally deployment.");
    public static readonly Translation RallyObstructed      = new Translation("<#959c8c><#bfbfbf>RALLY</color> is no longer available - there are enemies nearby.");
    public static readonly Translation RallyNoSquadmates    = new Translation("<#99918d>You need more squad members to use a <#bfbfbf>rally point</color>.");
    public static readonly Translation RallyNotSquadleader  = new Translation("<#99918d>You must be a <color=#cedcde>SQUAD LEADER</color> in order to place this.");
    public static readonly Translation RallyAlreadyQueued   = new Translation("<#99918d>You are already waiting on <#5eff87>rally</color> deployment. Do <#a3b4c7>/rally cancel</color> to abort.");
    public static readonly Translation RallyNotQueued       = new Translation("<#959c8c>You aren't waiting on a <#5eff87>rally</color> deployment.");
    public static readonly Translation RallyNotInSquad      = new Translation("<#959c8c>You must be in a squad to use <#5eff87>rallies</color>.");
    public static readonly Translation RallyObstructedPlace = new Translation("<#959c8c>This rally point is obstructed, find a more open place to put it.");
    public static readonly Translation<TimeSpan> RallyUI    = new Translation<TimeSpan>("<#5eff87>RALLY</color> {0}", TranslationFlags.UnityUI, "mm:ss");
    #endregion

    #region Time
    public static readonly Translation TimeSecondSingle = new Translation("second", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeSecondPlural = new Translation("seconds", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeMinuteSingle = new Translation("minute", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeMinutePlural = new Translation("minutes", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeHourSingle   = new Translation("hour", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeHourPlural   = new Translation("hours", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeDaySingle    = new Translation("day", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeDayPlural    = new Translation("days", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeWeekSingle   = new Translation("week", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeWeekPlural   = new Translation("weeks", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeMonthSingle  = new Translation("month", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeMonthsPlural = new Translation("months", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeYearSingle   = new Translation("year", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeYearsPlural  = new Translation("years", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeAnd          = new Translation("and", TranslationFlags.UnityUINoReplace);
    #endregion

    #region FOBs and Buildables
    public static readonly Translation BuildNotInRadius        = new Translation("<#ffab87>This can only be placed inside <#cedcde>FOB RADIUS</color>.");
    public static readonly Translation BuildTickNotInRadius    = new Translation("<#ffab87>There's no longer a friendly FOB nearby.");
    public static readonly Translation<float> BuildSmallRadius = new Translation<float>("<#ffab87>This can only be placed within {0}m of this FOB Radio right now. Expand this range by building a <#cedcde>FOB BUNKER</color>.", "N0");
    public static readonly Translation<float> BuildNoRadio     = new Translation<float>("<#ffab87>This can only be placed within {0}m of a friendly <#cedcde>FOB RADIO</color>.", "N0");
    public static readonly Translation<BuildableData> BuildStructureExists     = new Translation<BuildableData>("<#ffab87>This FOB can't have any more {0}.", PLURAL);
    public static readonly Translation<BuildableData> BuildTickStructureExists = new Translation<BuildableData>("<#ffab87>Too many {0} have already been built on this FOB.", PLURAL);
    public static readonly Translation BuildEnemy              = new Translation("<#ffab87>You may not build on an enemy FOB.");
    public static readonly Translation<int, int> BuildMissingSupplies = new Translation<int, int>("<#ffab87>You're missing nearby build! <#d1c597>Building Supplies: <#e0d8b8>{0}/{1}</color></color>.", PLURAL);
    public static readonly Translation BuildMaxFOBsHit         = new Translation("<#ffab87>The max number of FOBs on your team has been reached.");
    public static readonly Translation BuildFOBUnderwater      = new Translation("<#ffab87>You can't build a FOB underwater.");
    public static readonly Translation<float> BuildFOBTooHigh  = new Translation<float>("<#ffab87>You can't build a FOB more than {0}m above the ground.", "F0");
    public static readonly Translation BuildFOBTooCloseToMain  = new Translation("<#ffab87>You can't build a FOB this close to main base.");
    public static readonly Translation BuildNoLogisticsVehicle = new Translation("<#ffab87>You must be near a friendly <#cedcde>LOGISTICS VEHICLE</color> to place a FOB radio.");
    public static readonly Translation<FOB, float, float> BuildFOBTooClose = new Translation<FOB, float, float>("<#ffa238>You are too close to an existing FOB Radio ({0}: {1}m away). You must be at least {2}m away to place a new radio.", FOB.COLORED_NAME_FORMAT, "F0", "F0");

    public static readonly Translation<FOB, GridLocation, string> FOBUI    = new Translation<FOB, GridLocation, string>("{0}  <#d6d2c7>{1}</color>  {2}", TranslationFlags.UnityUI, FOB.NAME_FORMAT);

    public static readonly Translation CacheDestroyedAttack    = new Translation("<#e8d1a7>WEAPONS CACHE HAS BEEN ELIMINATED", TranslationFlags.UnityUI);
    public static readonly Translation CacheDestroyedDefense   = new Translation("<#deadad>WEAPONS CACHE HAS BEEN DESTROYED", TranslationFlags.UnityUI);
    public static readonly Translation<string> CacheDiscoveredAttack = new Translation<string>("<#e8d1a7>NEW WEAPONS CACHE DISCOVERED NEAR <#e3c59a>{0}</color>", TranslationFlags.UnityUI, UPPERCASE);
    public static readonly Translation CacheDiscoveredDefense  = new Translation("<#d9b9a7>WEAPONS CACHE HAS BEEN COMPROMISED, DEFEND IT", TranslationFlags.UnityUI);
    public static readonly Translation CacheSpawnedDefense     = new Translation("<#a8e0a4>NEW WEAPONS CACHE IS NOW ACTIVE", TranslationFlags.UnityUI);
    #endregion

    #region Deploy
    public static readonly Translation<IDeployable> DeploySuccess           = new Translation<IDeployable>("<#fae69c>You have arrived at {0}.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<IDeployable> DeployNotSpawnableTick  = new Translation<IDeployable>("<#ffa238>{0} is no longer active.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<IDeployable> DeployNotSpawnable      = new Translation<IDeployable>("<#ffa238>{0} is not active.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<IDeployable> DeployDestroyed         = new Translation<IDeployable>("<#ffa238>{0} was destroyed.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<IDeployable> DeployNoBunker          = new Translation<IDeployable>("<#ffaa42>{0} doesn't have a <#cedcde>FOB BUNKER</color>. Your team must build one to use the <#cedcde>FOB</color> as a spawnpoint.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<IDeployable> DeployRadioDamaged      = new Translation<IDeployable>("<#ffaa42>The <#cedcde>FOB RADIO</color> at {0} is damaged. Repair it with an <#cedcde>ENTRENCHING TOOL</color>.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation DeployMoved                          = new Translation("<#ffa238>You moved and can no longer deploy.");
    public static readonly Translation<IDeployable> DeployEnemiesNearbyTick = new Translation<IDeployable>("<#ffa238>You no longer deploy to {0} - there are enemies nearby.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<IDeployable> DeployEnemiesNearby     = new Translation<IDeployable>("<#ffaa42>You cannot deploy to {0} - there are enemies nearby.");
    public static readonly Translation DeployCancelled                      = new Translation("<#fae69c>Active deployment cancelled.");
    public static readonly Translation<string> DeployableNotFound           = new Translation<string>("<#ffa238>There is no location by the name of <#e3c27f>{0}</color>.", UPPERCASE);
    public static readonly Translation DeployNotNearFOB                     = new Translation<string>("<#ffa238>You must be near a friendly <#cedcde>FOB</color> or in <#cedcde>MAIN BASE</color> in order to deploy.", UPPERCASE);
    public static readonly Translation DeployNotNearFOBInsurgency           = new Translation<string>("<#ffa238>You must be near a friendly <#cedcde>FOB</color> or <#e8d1a7>CACHE</color>, or in <#cedcde>MAIN BASE</color> in order to deploy.", UPPERCASE);
    public static readonly Translation<Cooldown> DeployCooldown             = new Translation<Cooldown>("<#ffa238>You can deploy again in: <#e3c27f>{0}</color>", Cooldown.TIME_LEFT_FORMAT);
    public static readonly Translation DeployAlreadyActive                  = new Translation("<#b5a591>You're already deploying somewhere.");
    public static readonly Translation<Cooldown> DeployInCombat             = new Translation<Cooldown>("<#ffaa42>You are in combat, soldier! You can deploy in another: <#e3987f>{0}</color>.", Cooldown.TIME_LEFT_FORMAT);
    public static readonly Translation DeployInjured                        = new Translation("<#ffaa42>You can not deploy while injured, get a medic to revive you or give up.");
    public static readonly Translation DeployLobbyRemoved                   = new Translation("<#fae69c>The lobby has been removed, use  <#e3c27f>/teams</color> to switch teams instead.");
    #endregion

    #region Ammo
    public static readonly Translation AmmoNoTarget                = new Translation("<#ffab87>Look at an AMMO CRATE, AMMO BAG or VEHICLE in order to resupply.");
    public static readonly Translation<int, int> AmmoResuppliedKit = new Translation<int, int>("<#d1bda7>Resupplied kit. Consumed: <#d97568>{0} AMMO</color> <#948f8a>({1} left)</color>.");
    public static readonly Translation<VehicleData, int, int> AmmoResuppliedVehicle = new Translation<VehicleData, int, int>("<#d1bda7>Resupplied {0}. Consumed: <#d97568>{1} AMMO</color> <#948f8a>({2} left)</color>.", VehicleData.COLORED_NAME);
    #endregion

    #region Abandon

    private const string ABANDON_SECTION = "Abandon";
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player isn't looking at a vehicle when doing /abandon.", LegacyTranslationId = "abandon_no_target")]
    public static readonly Translation AbandonNoTarget = new Translation(ERROR_COLOR + "You must be looking at a vehicle.");
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player is looking at a vehicle they didn't request.", LegacyTranslationId = "abandon_not_owned")]
    public static readonly Translation<InteractableVehicle> AbandonNotOwned = new Translation<InteractableVehicle>(ERROR_COLOR + "You did not request that {0}.");
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player does /abandon while not in main.", LegacyTranslationId = "abandon_not_in_main")]
    public static readonly Translation AbandonNotInMain = new Translation(ERROR_COLOR + "You must be in main to abandon a vehicle.");
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player tries to abandon a damaged vehicle.", LegacyTranslationId = "abandon_damaged")]
    public static readonly Translation<InteractableVehicle> AbandonDamaged = new Translation<InteractableVehicle>(ERROR_COLOR + "Your <#cedcde>{0}</color> is damaged, repair it before returning it to the yard.");
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player tries to abandon a vehicle with low fuel.", LegacyTranslationId = "abandon_needs_fuel")]
    public static readonly Translation<InteractableVehicle> AbandonNeedsFuel = new Translation<InteractableVehicle>(ERROR_COLOR + "Your <#cedcde>{0}</color> is not fully fueled, .");
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player tries to abandon a vehicle and all the bays for that vehicle are already full, theoretically should never happen.", LegacyTranslationId = "abandon_no_space")]
    public static readonly Translation<InteractableVehicle> AbandonNoSpace = new Translation<InteractableVehicle>(ERROR_COLOR + "There's no space for <#cedcde>{0}</color> in the yard.", PLURAL);
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player tries to abandon a vehicle that isn't allowed to be abandoned.", LegacyTranslationId = "abandon_not_allowed")]
    public static readonly Translation<InteractableVehicle> AbandonNotAllowed = new Translation<InteractableVehicle>(ERROR_COLOR + "<#cedcde>{0}</color> can not be abandoned.", PLURAL);
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player abandons a vehicle.", LegacyTranslationId = "abandon_success")]
    public static readonly Translation<InteractableVehicle> AbandonSuccess = new Translation<InteractableVehicle>("<#a0ad8e>Your <#cedcde>{0}</color> was returned to the yard.", PLURAL);
    [TranslationData(Section = ABANDON_SECTION, Description = "Credits toast for returning a vehicle soon after requesting it.", LegacyTranslationId = "abandon_compensation_toast")]
    public static readonly Translation AbandonCompensationToast = new Translation("RETURNED VEHICLE", TranslationFlags.UnityUI);
    #endregion

    static Dictionary<string, string> _translations = new Dictionary<string, string>()
    {
            #region AmmoCommand
            { "ammo_error_nocrate", "<color=#ffab87>Look at an AMMO CRATE, AMMO BAG or VEHICLE in order to resupply.</color>" },
            { "ammo_success", "<color=#d1bda7>Resupplied kit. Consumed: <color=#d97568>{0} AMMO</color> <color=#948f8a>({1} left)</color></color>" },
            { "ammo_success_vehicle", "<color=#d1bda7>Resupplied vehicle. Consumed: <color=#d97568>{0} AMMO</color> <color=#948f8a>({1} left)</color></color>" },
            { "ammo_success_main", "<color=#d1bda7>Resupplied kit. Consumed: <color=#d97568>{0} AMMO</color></color>" },
            { "ammo_success_vehicle_main", "<color=#d1bda7>Resupplied vehicle. Consumed: <color=#d97568>{0} AMMO</color></color>" },
            { "ammo_vehicle_cant_rearm", "<color=#b3a6a2>This vehicle can't be resupplied.</color>" },
            { "ammo_auto_resupply", "<color=#b3a6a2>This vehicle will AUTO RESUPPLY when in main. You can also use '<color=#c9bfad>/load <color=#d4c49d>build</color>|<color=#d97568>ammo</color> <amount></color>'.</color>" },
            { "ammo_vehicle_full_already", "<color=#b3a6a2>This vehicle does not need to be resupplied.</color>" },
            { "ammo_not_near_fob", "<color=#b3a6a2>This ammo crate is not built on a friendly FOB.</color>" },
            { "ammo_not_near_repair_station", "<color=#b3a6a2>Your vehicle must be next to a <color=#e3d5ba>REPAIR STATION</color> in order to rearm.</color>" },
            { "ammo_not_in_team", "<color=#b3a6a2>You must be on a team to use this feature.</color>" },
            { "ammo_not_enough_stock", "<color=#b3a6a2>Insufficient ammo. Required: <color=#d97568>{0}/{1} AMMO</color></color>" },
            { "vehicle_staging", "<color=#b3a6a2>You cannot enter this vehicle during the staging phase.</color>" },
            { "ammo_no_kit", "<color=#b3a6a2>You don't have a kit yet. Go and request one at main.</color>" },
            { "ammo_cooldown", "<color=#b7bab1>More AMMO arriving in: <color=#de95a8>{0}</color></color>" },
            { "ammo_not_rifleman", "<color=#b3a6a2>You must be a RIFLEMAN in order to place this Ammo Bag.</color>" },
            #endregion
            
            #region LoadCommand
            { "load_e_novehicle", "<color=#b3a6a2>Look at a friendly LOGISTICS TRUCK or HELICOPTER to load it.</color>" },
            { "load_e_usage", "<color=#b3a6a2>Try typing: '<color=#e6d1b3>/load ammo <amount></color>' or '<color=#e6d1b3>/load build <amount></color>'.</color>" },
            { "load_e_invalidamount", "<color=#b3a6a2>'{0}' is not a valid amount of supplies.</color>" },
            { "load_e_notinmain", "<color=#b3a6a2>You must be in MAIN to load up this vehicle.</color>" },
            { "load_e_notlogi", "<color=#b3a6a2>Only LOGISTICS TRUCKS and TRANSPORT HELICOPTERS can be loaded with supplies.</color>" },
            { "load_e_toofast", "<color=#b3a6a2>Vehicle is moving too fast.</color>" },
            { "load_e_itemassetnotfound", "<color=#b3a6a2>The item required to resupply does not exist. Please report this to the admins.</color>" },
            { "load_s_build", "<color=#d1bda7>Loading complete. <color=#d4c49d>{0} BUILD</color> loaded.</color>" },
            { "load_s_ammo", "<color=#d1bda7>Loading complete. <color=#d97568>{0} AMMO</color> loaded.</color>" },
            #endregion
            
            #region Custom Signs
            { "sign_rules", "Rules\nNo suicide vehicles.\netc." },
            { "sign_kitdelay", "<color=#e6e6e6>All <color=#3bede1>Elite Kits</color> and <color=#32a852>Loadouts</color> are locked for the two weeks of the season.\nThey will be available again after <color=#d8addb>April 1st</color></color>" },
            { "sign_class_desc_squadleader", "\n\n<color=#cecece>Help your squad by supplying them with <color=#f0a31c>rally points</color> and placing <color=#f0a31c>FOB radios</color>.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_rifleman", "\n\n<color=#cecece>Resupply your teammates in the field with an <color=#f0a31c>Ammo Bag</color>.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_medic", "\n\n<color=#cecece><color=#f0a31c>Revive</color> your teammates after they've been injured.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_breacher", "\n\n<color=#cecece>Use <color=#f0a31c>high-powered explosives</color> to take out <color=#f01f1c>enemy FOBs</color>.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_autorifleman", "\n\n<color=#cecece>Equipped with a high-capacity and powerful <color=#f0a31c>LMG</color> to spray-and-pray your enemies.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_machinegunner", "\n\n<color=#cecece>Equipped with a powerful <color=#f0a31c>Machine Gun</color> to shred the enemy team in combat.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_lat", "\n\n<color=#cecece>A balance between an anti-tank and combat loadout, used to conveniently destroy armored enemy vehicles.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_hat", "\n\n<color=#cecece>Equipped with multiple powerful <color=#f0a31c>anti-tank shells</color> to take out any vehicles.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_grenadier", "\n\n<color=#cecece>Equipped with a <color=#f0a31c>grenade launcher</color> to take out enemies behind cover or in light-armored vehicles.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_marksman", "\n\n<color=#cecece>Equipped with a <color=#f0a31c>marksman rifle</color> to take out enemies from medium to high distances.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_sniper", "\n\n<color=#cecece>Equipped with a high-powered <color=#f0a31c>sniper rifle</color> to take out enemies from great distances.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_aprifleman", "\n\n<color=#cecece>Equipped with <color=#f0a31c>explosive traps</color> to cover entry-points and entrap enemy vehicles.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_engineer", "\n\n<color=#cecece>Features 200% <color=#f0a31c>build speed</color> and are equipped with <color=#f0a31c>fortifications</color> and traps to help defend their team's FOBs.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_crewman", "\n\n<color=#cecece>The only kits than can man <color=#f0a31c>armored vehicles</color>.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_pilot", "\n\n<color=#cecece>The only kits that can fly <color=#f0a31c>aircraft</color>.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_class_desc_specops", "\n\n<color=#cecece>Equipped with <color=#f0a31c>night-vision</color> to help see at night.</color>\n<color=#f01f1c>\\/</color>" },
            { "sign_bundle_misc", "<color=#f0a31c>Misc.</color>" },
            { "sign_bundle_caf", "<color=#f0a31c>Canadian Bundle</color>" },
            { "sign_bundle_fr", "<color=#f0a31c>French Bundle</color>" },
            { "sign_bundle_ger", "<color=#f0a31c>German Bundle</color>" },
            { "sign_bundle_usmc", "<color=#f0a31c>USMC Bundle</color>" },
            { "sign_bundle_usa", "<color=#f0a31c>USA Bundle</color>" },
            { "sign_bundle_pl", "<color=#f0a31c>Polish Bundle</color>" },
            { "sign_bundle_idf", "<color=#f0a31c>IDF Bundle</color>" },
            { "sign_bundle_militia", "<color=#f0a31c>Militia Bundle</color>" },
            { "sign_bundle_ru", "<color=#f0a31c>Russia Bundle</color>" },
            { "sign_bundle_soviet", "<color=#f0a31c>Soviet Bundle</color>" },
            { "sign_loadout_info", "<color=#cecece>Loadouts and elite kits can be purchased\nin our <color=#7483c4>Discord</color> server.\n\n<color=#7483c4>/discord</color>" },
            #endregion
            
            #region KickOverrideCommand
            { "kick_syntax", "<color=#9cffb3>Syntax: <i>/kick <player> <reason ...></i>.</color>" },
            { "kick_no_reason_provided", "<color=#9cffb3>You must provide a reason.</color>" },
            { "kick_no_player_found", "<color=#9cffb3>No player found from <color=#d8addb>{0}</color>.</color>" },
            { "kick_kicked_feedback", "<color=#00ffff>You kicked <color=#d8addb>{0}</color>.</color>" },
            { "kick_kicked_broadcast", "<color=#00ffff><color=#d8addb>{0}</color> was kicked by <color=#00ffff>{1}</color>.</color>" },
            { "kick_kicked_broadcast_operator", "<color=#00ffff><color=#d8addb>{0}</color> was kicked by an operator.</color>" },
            { "kick_kicked_console_operator", "{0} ({1}) was kicked by an operator because: {2}." },
            { "kick_kicked_console", "{0} ({1}) was kicked by {2} ({3}) because: {4}." },
            { "kick_autokick_namefilter", "Your name does not contain enough alphanumeric characters in succession (5), please change your name and rejoin." },
            #endregion
            
            #region BanOverrideCommand
            { "ban_syntax", "<color=#9cffb3>Syntax: <i>/ban <player> <duration minutes> <reason ...></i>.</color>" },
            { "ban_permanent_feedback", "<color=#00ffff><color=#d8addb>{0}</color> was <b>permanently</b> banned.</color>" },
            { "ban_permanent_broadcast", "<color=#00ffff><color=#d8addb>{0}</color> was <b>permanently</b> banned by <color=#00ffff>{1}</color>.</color>" },
            { "ban_permanent_broadcast_operator", "<color=#00ffff><color=#d8addb>{0}</color> was <b>permanently</b> banned by an operator.</color>" },
            { "ban_permanent_console_operator", "{0} ({1}) was permanently banned by an operator because: {2}." },
            { "ban_permanent_console", "{0} ({1}) was permanently banned by {2} ({3}) because: {4}." },
            { "ban_feedback", "<color=#00ffff><color=#d8addb>{0}</color> was banned for <color=#9cffb3>{1}</color>.</color>" },
            { "ban_broadcast", "<color=#00ffff><color=#d8addb>{0}</color> was banned by <color=#00ffff>{1}</color> for <color=#9cffb3>{2}</color>.</color>" },
            { "ban_broadcast_operator", "<color=#00ffff><color=#d8addb>{0}</color> was banned by an operator for <color=#9cffb3>{2}</color>.</color>" },
            { "ban_console_operator", "{0} ({1}) was banned by an operator for {3} because: {2}." },
            { "ban_console", "{0} ({1}) was banned by {2} ({3}) for {5} because: {4}." },
            { "ban_no_player_found", "<color=#9cffb3>No player found from <color=#d8addb>{0}</color>.</color>" },
            { "ban_invalid_number", "<color=#9cffb3><color=#9cffb3>{0}</color> should be a whole number between <color=#00ffff>1</color> and <color=#00ffff>2147483647</color>.</color>" },
            { "ban_no_reason_provided", "<color=#9cffb3>You must provide a reason.</color>" },
            #endregion
            
            #region WarnCommand
            { "warn_syntax", "<color=#9cffb3>Syntax: <i>/warn <player> <reason ...></i>.</color>" },
            { "warn_no_player_found", "<color=#9cffb3>No player found from <color=#d8addb>{0}</color>.</color>" },
            { "warn_no_reason_provided", "<color=#9cffb3>You must provide a reason.</color>" },
            { "warn_warned_private_operator", "<color=#ffff00>An operator warned you for: <b>{0}</b>.</color>" },
            { "warn_warned_console_operator", "Warned {0} ({1}) for: {2}" },
            { "warn_warned_broadcast_operator", "<color=#ffff00><color=#d8addb>{0}</color> was warned by an operator.</color>" },
            { "warn_warned_feedback", "<color=#ffff00>You warned <color=#d8addb>{0}</color>.</color>" },
            { "warn_warned_private", "<color=#ffff00><color=#00ffff>{0}</color> warned you for: <b>{1}</b>.</color>" },
            { "warn_warned_console", "{0} ({1}) was warned by {2} ({3}) for: {4}" },
            { "warn_warned_broadcast", "<color=#ffff00><color=#d8addb>{0}</color> was warned by <color=#00ffff>{1}</color>.</color>" },
            #endregion

            #region MuteCommand
            { "mute_syntax", "<color=#9cffb3>Syntax: /mute <voice|text|both> <name or steam64> <permanent | duration in minutes> <reason...></color>" },
            { "mute_no_player_found", "<color=#9cffb3>No online players found with the name <color=#d8addb>{0}</color>. To mute someone that's offline, use their Steam64 ID.</color>" },
            { "mute_cant_read_duration", "<color=#9cffb3>The given value for duration must be a positive number or 'permanent'.</color>" },
            { "mute_feedback", "<color=#00ffff><color=#d8addb>{0}</color> ({1}) was {3} muted for <color=#9cffb3>{2}</color>.</color>" },
            { "mute_feedback_permanent", "<color=#00ffff><color=#d8addb>{0}</color> ({1}) was {2} muted <color=#9cffb3>permanently</color>.</color>" },
            { "mute_broadcast", "<color=#00ffff><color=#d8addb>{0}</color> was {3} muted by <color=#00ffff>{1}</color> for <color=#9cffb3>{2}</color>.</color>" },
            { "mute_broadcast_operator", "<color=#00ffff><color=#d8addb>{0}</color> was {1} muted by an operator for <color=#9cffb3>{2}</color>.</color>" },
            { "mute_broadcast_permanent", "<color=#00ffff><color=#d8addb>{0}</color> was <color=#9cffb3>permanently</color> {2} muted by <color=#00ffff>{1}</color>.</color>" },
            { "mute_broadcast_operator_permanent", "<color=#00ffff><color=#d8addb>{0}</color> was <color=#9cffb3>permanently</color> {1} muted by an operator.</color>" },
            { "mute_dm", "<color=#ffff00><color=#00ffff>{0}</color> {3} muted you for <color=#9cffb3>{2}</color> because: <color=#9cffb3><b>{1}</b></color>.</color>" },
            { "mute_dm_permanent", "<color=#ffff00><color=#00ffff>{0}</color> <color=#9cffb3>permanently</color> {2} muted you for: <color=#9cffb3><b>{1}</b></color>.</color>" },
            { "mute_dm_operator", "<color=#ffff00>An operator {2} muted you for <color=#9cffb3>{1}</color> because: <color=#9cffb3><b>{0}</b></color>.</color>" },
            { "mute_dm_operator_permanent", "<color=#ffff00>An operator <color=#9cffb3>permanently</color> {1} muted you for: <color=#9cffb3><b>{0}</b></color>.</color>" },
            { "text_chat_feedback_muted_permanent", "<color=#ffff00>You're permanently muted in text chat because: {0}.</color>" },
            { "text_chat_feedback_muted", "<color=#ffff00>You're muted in text chat until {0} because {1}.</color>" },
            #endregion

            #region UnmuteCommnad
            { "unmute_not_found", "<color=#9cffb3>\"{0}\" doesn't match a player. To unmute an offline player use their Steam64 ID.</color>" },
            { "unmute_not_muted", "<color=#9cffb3>{0} is not currently muted.</color>" },
            { "unmute_unmuted_broadcast", "<color=#ffff00><color=#d8addb>{0}</color> was unmuted by <color=#00ffff>{1}</color>.</color>" },
            { "unmute_unmuted_broadcast_operator", "<color=#ffff00><color=#d8addb>{0}</color> was unmuted by an operator.</color>" },
            { "unmute_unmuted_dm", "<color=#ffff00><color=#00ffff>{0}</color> has lifted your mute.</color>" },
            { "unmute_unmuted_dm_operator", "<color=#ffff00>Your mute has been lifted.</color>" },
            { "unmute_unmuted", "<color=#ffff00><color=#d8addb>{0}</color> was successfully unmuted.</color>" },
            #endregion

            #region Anti-Main-Camp
            { "amc_reverse_damage", "<color=#f53b3b>Stop <b><color=#ff3300>main-camping</color></b>! Damage is <b>reversed</b> back on you.</color>" },
            #endregion

            #region UnbanCommand
            { "unban_syntax", "<color=#9cffb3>Syntax: <i>/unban <player id></i>.</color>" },
            { "unban_no_player_found", "<color=#9cffb3>No player ID found from <color=#d8addb>{0}</color>.</color>" },
            { "unban_player_not_banned_console", "Player \"{0}\" is not banned. You must use Steam64's for /unban." },
            { "unban_unbanned_console_name_operator", "Sucessfully unbanned {0} ({1})." },
            { "unban_unbanned_console_id_operator", "Sucessfully unbanned {0}." },
            { "unban_unbanned_broadcast_name_operator", "<color=#00ffff><color=#d8addb>{0}</color> was unbanned by an operator.</color>" },
            { "unban_unbanned_broadcast_id_operator", "<color=#00ffff><color=#d8addb>{0}</color> was unbanned by an operator.</color>" },
            { "unban_unbanned_console_name", "{0} ({1}) was unbanned by {2} ({3})." },
            { "unban_unbanned_console_id", "{0} was unbanned by {1} ({2})." },
            { "unban_unbanned_broadcast_name", "<color=#00ffff><color=#d8addb>{0}</color> was unbanned by <color=#00ffff>{1}</color>.</color>" },
            { "unban_unbanned_broadcast_id", "<color=#00ffff><color=#d8addb>{0}</color> was unbanned by <color=#00ffff>{1}</color>.</color>" },
            { "unban_unbanned_feedback_name", "<color=#00ffff>You unbanned <color=#d8addb>{0}</color>.</color>" },
            { "unban_unbanned_feedback_id", "<color=#00ffff>You unbanned <color=#d8addb>{0}</color>.</color>" },
            #endregion
            
            #region LoadBansCommand
            { "loadbans_NoBansErrorText", "There are no banned players." },
            { "loadbans_LogBansDisabled", "Can't upload, Logging bans is disabled." },
            #endregion

            #region DutyCommand
            { "duty_admin_on_console", "{0} ({1}) went on duty." },
            { "duty_admin_off_console", "{0} ({1}) went off duty." },
            { "duty_intern_on_console", "{0} ({1}) went on duty." },
            { "duty_intern_off_console", "{0} ({1}) went off duty." },
            { "duty_on_feedback", "<color=#c6d4b8>You are now <color=#95ff4a>on duty</color>.</color>" },
            { "duty_off_feedback", "<color=#c6d4b8>You are now <color=#ff8c4a>off duty</color>.</color>" },
            { "duty_on_broadcast", "<color=#c6d4b8><color=#d9e882>{0}</color> is now <color=#95ff4a>on duty</color>.</color>" },
            { "duty_off_broadcast", "<color=#c6d4b8><color=#d9e882>{0}</color> is now <color=#ff8c4a>off duty</color>.</color>" },
            #endregion

            #region Teamkills
            { "teamkilled_console_log", "{0} ({1}) teamkilled {2} ({3})!!" },
            #endregion

            #region Restrictions
            { "no_placement_on_vehicle", "<color=#f53b3b>You can't place a{1} <color=#d9e882>{0}</color> on a vehicle!</color>" },
            { "no_place_trap", "<color=#f53b3b>You're not allowed to place a{1} <color=#d9e882>{0}</color> here.</color>" },
            { "cant_steal_batteries", "<color=#f53b3b>Stealing batteries is not allowed.</color>" },
            { "cant_leave_group", "<color=#f53b3b>You are not allowed to manually change groups.</color>" },
            { "cant_store_this_item", "<color=#f53b3b>You are not allowed to store <color=#d9e882>{0}</color>.</color>" },
            { "marker_not_in_squad", "<color=#f53b3b>Only your squad can see markers, join a squad with <color=#d9e882>/squad join <name></color> or <color=#d9e882>/squad create <name></color> to use this feature.</color>" },
            { "entered_enemy_territory", "Too close to enemy base! You will die in {0} second{1}!" },
            #endregion

            #region OnVehicleEnterRequested
            { "vehicle_wait_for_owner", "<color=#bda897>Only the owner (<color=#cedcde>{0}</color>) can enter the driver's seat right now.</color>" },
            { "vehicle_wait_for_owner_or_squad", "<color=#bda897>Only the owner (<color=#cedcde>{0}</color>) and/or members of squad <color=#cedcde>{1}</color> can enter the driver's seat right now.</color>" },
            { "vehicle_no_kit", "<color=#bda897>You cannot get in a vehicle without a kit.</color>" },
            { "vehicle_too_high", "<color=#ff684a>Vehicle is too high off the ground!</color>" },
            { "vehicle_not_valid_kit", "<color=#bda897>You need a <color=#cedcde>{0}</color> kit in order to man this vehicle.</color>" },
            { "vehicle_need_driver", "<color=#bda897>Your vehicle needs a <color=#cedcde>DRIVER</color> before you can switch to the gunner's seat on the battlefield.</color>" },
            { "vehicle_cannot_abandon_driver", "<color=#bda897>You cannot abandon the driver's seat on the battlefield.</color>" },
            { "vehicle_no_passenger_seats", "<color=#bda897>There are no free passenger seats in this vehicle.</color>" },
            #endregion
            
            #region Warnings
            { "friendly_mortar_incoming", "FRIENDLY MORTAR STRIKE INCOMING" },
            { "afk_warning", "<color=#f53b3b>You will be AFK-Kicked in {0} if you don't move.</color>" },
            #endregion
            
            #region BattlEye
            { "battleye_kick_console", "{0} ({1}) was kicked by BattlEye because: \"{2}\"" },
            { "battleye_kick_broadcast", "<color=#00ffff>{0} was kicked by <color=#feed00>BattlEye</color>.</color>" },
            #endregion
            
            #region RequestCommand
            { "request_saved_sign", "<color=#a4baa9>Saved kit: <color=#ffebbd>{0}</color>.</color>" },
            { "request_removed_sign", "<color=#a4baa9>Removed kit sign: <color=#ffebbd>{0}</color>.</color>" },
            { "request_not_looking", "<color=#a8918a>You must be looking at a request sign or vehicle.</color>" },
            { "request_already_saved", "<color=#a8918a>That sign is already saved.</color>" },
            { "request_already_removed", "<color=#a8918a>That sign has already been removed.</color>" },
            { "request_kit_given", "<color=#99918d>You have been allocated a <color=#cedcde>{0}</color> kit!</color>" },
            { "request_kit_boughtcredits", "<color=#c4a36a>Kit bought for <color=#b8ffc1>C </color><color=#ffffff>{0}</color>. Request it with '<color=#b3b0ab>/request</color>'.</color>" },
            { "request_kit_e_kitnoexist", "<color=#a8918a>This kit has not been created yet.</color>" },
            { "request_kit_e_alreadyhaskit", "<color=#a8918a>You already have this kit.</color>" },
            { "request_kit_e_notallowed", "<color=#a8918a>You do not have access to this kit.</color>" },
            { "request_kit_e_notboughtcredits", "<color=#99918d>Look at this sign and type '<color=#ffe2ab>/buy</color>' to unlock this kit permanently for <color=#b8ffc1>C </color><color=#ffffff>{0}</color></color>" },
            { "request_kit_e_notenoughcredits", "<color=#a8918a>You are missing <color=#b8ffc1>C </color><color=#ffffff>{0}</color> needed to unlock this kit.</color>" },
            { "request_kit_e_notbuyablecredits", "<color=#a8918a>This kit cannot be purchased with credits.</color>" },
            { "request_kit_e_limited", "<color=#a8918a>Your team already has a max of {0} players using this kit. Try again later.</color>" },
            { "request_kit_e_wronglevel", "<color=#b3ab9f>You must be rank <color=#ffc29c>{0}</color> to use this kit.</color>" },
            { "request_kit_e_wrongrank", "<color=#b3ab9f>You must be a <color=#{1}>{0}</color> to request this kit.</color>" },
            { "request_kit_e_quest_incomplete", "<color=#b3ab9f>Complete the <color=#ffc29c>{0}</color> quest to request this kit.</color>" },
            { "request_kit_e_notsquadleader", "<color=#b3ab9f>You must be a <color=#cedcde>SQUAD LEADER</color> in order to get this kit.</color>" },
            { "request_loadout_e_notallowed", "<color=#a8918a>You do not own this loadout.</color>" },
            { "request_vehicle_e_notenoughcredits", "<color=#a8918a>You are missing <color=#b8ffc1>C </color><color=#ffffff>{0}</color> needed to request this vehicle.</color>" },
            { "request_vehicle_e_cooldown", "<color=#b3ab9f>This vehicle can be requested in: <color=#ffe2ab>{0}</color>.</color>" },
            { "request_vehicle_e_time_delay", "<color=#b3ab9f>This vehicle is delayed for another: <color=#94cfff>{0}</color>.</color>" },
            { "request_vehicle_e_cache_delay_atk_1", "<color=#b3ab9f>Destroy <color=#94cfff>{0}</color> to request this vehicle.</color>" },
            { "request_vehicle_e_cache_delay_def_1", "<color=#b3ab9f>Lose <color=#94cfff>{0}</color> to request this vehicle.</color>" },
            { "request_vehicle_e_cache_delay_atk_undiscovered_1", "<color=#b3ab9f><color=#94cfff>Discover and Destroy</color> the next cache to request this vehicle.</color>" },
            { "request_vehicle_e_cache_delay_def_undiscovered_1", "<color=#b3ab9f><color=#94cfff>Discover and Lose</color> the next cache to request this vehicle.</color>" },
            { "request_vehicle_e_cache_delay_atk_2+", "<color=#b3ab9f>Destroy <color=#94cfff>{0} more caches</color> to request this vehicle.</color>" },
            { "request_vehicle_e_cache_delay_def_2+", "<color=#b3ab9f>Lose <color=#94cfff>{0} more caches</color> to request this vehicle.</color>" },
            { "request_vehicle_e_flag_delay_1", "<color=#b3ab9f>Capture <color=#94cfff>{0}</color> to request this vehicle.</color>" },
            { "request_vehicle_e_flag_lose_delay_1", "<color=#b3ab9f>Lose <color=#94cfff>{0}</color> to request this vehicle.</color>" },
            { "request_vehicle_e_flag_delay_2+", "<color=#b3ab9f>Capture <color=#94cfff>{0} more flags</color> to request this vehicle.</color>" },
            { "request_vehicle_e_flag_lose_delay_2+", "<color=#b3ab9f>Lose <color=#94cfff>{0} more flags</color> to request this vehicle.</color>" },
            { "request_vehicle_e_staging_delay", "<color=#a6918a>This vehicle can only be requested after the game starts.</color>" },
            { "request_vehicle_e_notinsquad", "<color=#b3ab9f>You must be <color=#cedcde>IN A SQUAD</color> in order to request this vehicle.</color>" },
            { "request_vehicle_e_nokit", "<color=#a8918a>Get a kit before you request vehicles.</color>" },
            { "request_vehicle_e_notinteam", "<color=#a8918a>You must be on the other team to request this vehicle.</color>" },
            { "request_vehicle_e_wrongkit", "<color=#b3ab9f>You need a {0} kit in order to request this vehicle.</color>" },
            { "request_vehicle_e_wronglevel", "<color=#b3ab9f>You must be rank <color=#ffc29c>{0}</color> to request this vehicle.</color>" },
            { "request_vehicle_e_wrongrank", "<color=#b3ab9f>You must be a <color=#{1}>{0}</color> to request this vehicle.</color>" },
            { "request_vehicle_e_quest_incomplete", "<color=#b3ab9f>Complete the <color=#ffc29c>{0}</color> quest to request this vehicle.</color>" },
            { "request_vehicle_e_alreadyrequested", "<color=#a8918a>This vehicle has already been requested.</color>" },
            { "request_vehicle_e_already_owned", "<color=#a8918a>You have already requested a nearby vehicle.</color>" },
            { "request_vehicle_e_unknown_delay", "<color=#b3ab9f>This vehicle is delayed because: <color=#94cfff>{0}</color>.</color>" },
            { "request_vehicle_given", "<color=#b3a591>This <color=#ffe2ab>{0}</color> is now yours to take into battle.</color>" },
            #endregion
            
            #region StructureCommand
            { "structure_not_looking", "<color=#ff8c69>You must be looking at a barricade, structure, or vehicle.</color>" },
            { "structure_saved", "<color=#e6e3d5>Saved <color=#c6d4b8>{0}</color>.</color>" },
            { "structure_saved_already", "<color=#e6e3d5><color=#c6d4b8>{0}</color> is already saved.</color>" },
            { "structure_unsaved", "<color=#e6e3d5><color=#e6e3d5>Removed <color=#c6d4b8>{0}</color> save.</color>" },
            { "structure_unsaved_already", "<color=#ff8c69><color=#c6d4b8>{0}</color> is not saved.</color>" },
            { "structure_popped", "<color=#e6e3d5>Destroyed <color=#c6d4b8>{0}</color>.</color>" },
            { "structure_pop_not_poppable", "<color=#ff8c69>That object can not be destroyed.</color>" },
            { "structure_examine_not_examinable", "<color=#ff8c69>That object can not be examined.</color>" },
            { "structure_examine_not_locked", "<color=#ff8c69>This vehicle is not locked.</color>" },
            { "structure_last_owner_web_prompt", "Last owner of {0}: {1}, Team: {2}." },
            { "structure_last_owner_chat", "<color=#c6d4b8>Last owner of <color=#e6e3d5>{0}</color>: <color=#{3}>{1} <i>({2})</i></color>, Team: <color=#{5}>{4}</color>.</color>" },
            #endregion
            
            #region WhitelistCommand
            { "whitelist_added", "<color=#a0ad8e>Whitelisted item: <color=#ffffff>{0}</color></color>" },
            { "whitelist_removed", "<color=#a0ad8e>Un-whitelisted item: <color=#ffffff>{0}</color></color>" },
            { "whitelist_e_exist", "<color=#ff8c69>That item is already whitelisted.</color>" },
            { "whitelist_e_noexist", "<color=#ff8c69>That item is not yet whitelisted.</color>" },
            { "whitelist_e_invalidid", "<color=#ff8c69>{0} is not a valid item ID.</color> " },
            { "whitelist_e_invalidamount", "<color=#ff8c69>{0} is not a valid number.</color> " },
            { "whitelist_notallowed", "<color=#ff8c69>The item is not allowed to be picked up.</color> " },
            { "whitelist_maxamount", "<color=#ff8c69>You are not allowed to carry any more of this item.</color> " },
            { "whitelist_kit_maxamount", "<color=#ff8c69>Your kit does not allow you to have any more of this item.</color> " },
            { "whitelist_nokit", "<color=#ff8c69>Get a kit first before you can pick up items.</color> " },
            { "whitelist_nosalvage", "<color=#ff8c69>You are not allowed to salvage that.</color> " },
            { "whitelist_noplace", "<color=#ff8c69>You are not allowed to place that.</color> " },
            { "whitelist_toomanyplaced", "<color=#ff8c69>You cannot place more than {0} of those.</color> " },
            { "whitelist_noeditsign", "<color=#ff8c69>You are not allowed to edit that sign.</color> " },
            #endregion
            
            #region VehiclebayCommand
            { "vehiclebay_added", "<color=#a0ad8e>Added requestable vehicle to the vehicle bay: <color=#ffffff>{0}</color></color>" },
            { "vehiclebay_removed", "<color=#a0ad8e>Removed requestable vehicle from the vehicle bay: <color=#ffffff>{0}</color></color>" },
            { "vehiclebay_setprop", "<color=#a0ad8e>Set <color=#8ce4ff>{0}</color> for vehicle <color=#ffb89c>{1}</color> to: <color=#ffffff>{2}</color></color>" },
            { "vehiclebay_setitems", "<color=#a0ad8e>Successfuly set the rearm list for vehicle <color=#ffffff>{0}</color> from your inventory. It will now drop <color=#8ce4ff>{1}</color> items with /ammo.</color>" },
            { "vehiclebay_savemeta", "<color=#a0ad8e>Successfully saved all metadata for vehicle <color=#ffffff>{0}</color>.</color>" },
            { "vehiclebay_cleareditems", "<color=#a0ad8e>Successfuly cleared the rearm list for this vehicle.</color>" },
            { "vehiclebay_seatadded", "<color=#a0ad8e>Made seat <color=#ffffff>{0}</color> a crewman seat for this vehicle.</color>" },
            { "vehiclebay_seatremoved", "<color=#a0ad8e>Seat <color=#ffffff>{0}</color> is no longer a crewman seat for this vehicle.</color>" },
            { "vehiclebay_e_novehicle", "<color=#ff8c69>Look at a vehicle or spawner barricade to use this command.</color>" },
            { "vehiclebay_e_exist", "<color=#ff8c69>That vehicle is already added to the vehicle bay.</color>" },
            { "vehiclebay_e_noexist", "<color=#ff8c69>That vehicle has not been added to the vehicle bay.</color>" },
            { "vehiclebay_e_invalidprop", "<color=#ff8c69>{0} isn't a valid a vehicle property. Try putting 'level', 'team', 'rearmcost' etc.</color>" },
            { "vehiclebay_e_invalidarg", "<color=#ff8c69>{0} isn't a valid value for vehicle property: {1}</color>" },
            { "vehiclebay_e_not_settable", "<color=#ff8c69>{0} is not marked as settable.</color>" },
            { "vehiclebay_e_not_added", "<color=#ff8c69><color=#ffffff>{0}</color> has not been added to the vehicle bay yet. Look at one and do /vb add.</color>" },
            { "vehiclebay_e_invalidseat", "<color=#ff8c69>{0} isn't a valid value for vehicle property: {1}</color>" },
            { "vehiclebay_e_seatexist", "<color=#ff8c69>This vehicle already has a crew seat with index: {0}</color>" },
            { "vehiclebay_e_seatnoexist", "<color=#ff8c69>This vehicle does not have a crew seat with index: {0}</color>" },
            { "vehiclebay_e_gamemode_not_active", "<color=#ff8c69>You may not enter a vehicle right now as the game has not started.</color>" },
            { "vehiclebay_delay_added", "<color=#a0ad8e>Added delay of type {0}:{1} during {2} gamemode.</color>" },
            { "vehiclebay_delay_removed", "<color=#a0ad8e>Removed {0} matching delays.</color>" },
            { "vehiclebay_spawn_registered", "<color=#a0ad8e>Successfully registered spawn. <color=#ffffff>{0}s</color> will spawn here.</color>" },
            { "vehiclebay_spawn_deregistered", "<color=#a0ad8e>Successfully deregistered spawn.</color>" },
            { "vehiclebay_link_started", "<color=#a0ad8e>Started linking, do /vb link on the sign now.</color>" },
            { "vehiclebay_link_finished", "<color=#a0ad8e>Successfully registered vehicle sign link.</color>" },
            { "vehiclebay_unlink_success", "<color=#a0ad8e>Successfully unlinked vehicle sign.</color>" },
            { "vehiclebay_link_not_started", "<color=#ff8c69>You must do /vb link on a vehicle bay first.</color>" },
            { "vehiclebay_spawn_forced", "<color=#a0ad8e>Skipped timer for <color=#ffffff>{0}</color>.</color>" },
            { "vehiclebay_e_invalidid", "<color=#ff8c69>{0} is not a valid vehicle ID.</color>" },
            { "vehiclebay_e_invalidbayid", "<color=#ff8c69>{0} is not a valid vehicle bay item.</color>" },
            { "vehiclebay_e_idnotfound", "<color=#ff8c69>Could not find vehicle with ID: {0}</color>" },
            { "vehiclebay_e_spawnexist", "<color=#ff8c69>This spawn is already registered to <color=#8ce4ff>{0}</color>. Unregister it first.</color>" },
            { "vehiclebay_e_spawnnoexist", "<color=#ff8c69>This spawn is not registered.</color>" },
            { "vehiclebay_check_registered", "<color=#a0ad8e>This spawn (<color=#8ce4ff>{0}</color>) is registered with vehicle: <color=#ffffff>{1} - {2}</color></color>" },
            { "vehiclebay_check_notregistered", "<color=#a0ad8e>This spawn is not registered.</color>" },
            #endregion

            #region Vehicle Death Messages
            { "VEHICLE_DESTROYED", "{0} took out a {1} with a {2} from {3}m away." },
            { "VEHICLE_DESTROYED_UNKNOWN", "{0} took out a {1}." },
            { "VEHICLE_TEAMKILLED", "{0} blew up a friendly {1}." },
            #endregion
            
            #region OfficerCommand
            { "officer_promoted", "<color=#9e9788>Congratulations, you have been <color=#e3b552>PROMOTED</color> to <color=#e05353>{0}</color> of <color=#baccca>{1}</color>!</color>" },
            { "officer_demoted", "<color=#9e9788>You have been <color=#c47f5c>DEMOTED</color> to <color=#e05353>{0}</color> of <color=#baccca>{1}</color>.</color>" },
            { "officer_discharged", "<color=#9e9788>You have been <color=#ab2e2e>DISCHARGED</color> from the officer ranks for unacceptable behaviour.</color>" },
            { "officer_announce_promoted", "<color=#9e9788><color=#c4daff>{0}</color> has been <color=#e3b552>PROMOTED</color> to <color=#e05353>{1}</color> of <color=#baccca>{2}</color>!</color>" },
            { "officer_announce_demoted", "<color=#9e9788><color=#c4daff>{0}</color> has been <color=#c47f5c>DEMOTED</color> to <color=#e05353>{1}</color> of <color=#baccca>{2}</color>.</color>" },
            { "officer_announce_discharged", "<color=#9e9788><color=#c4daff>{0}</color> has been <color=#ab2e2e>DISCHARGED</color> from the rank of <color=#e05353>{1}s</color> for unacceptable behaviour.</color>" },
            { "officer_e_playernotfound", "<color=#b08989>'{0}' is not a valid online player or Steam64 ID.</color>" },
            { "officer_e_invalidrank", "<color=#b08989>'{0}' is not a valid officer level. Try numbers 1 - 5.</color>" },
            { "officer_s_changedrank", "<color=#c6d6c1>{0}'s officer rank was successfully changed to {1} of {2}.</color>" },
            { "officer_s_discharged", "<color=#c6d6c1>{0} was successfully discharged.</color>" },
            #endregion
            
            #region ClearCommand
            { "clear_inventory_console_identity", "Specify a player name when clearing from console." }, // runs from console only, no color needed.
            { "clear_inventory_player_not_found", "<color=#ff8c69>A player was not found from <color=#8ce4ff>\"{0}\"</color>.</color>" },
            { "clear_inventory_self", "<color=#e6e3d5>Cleared your inventory.</color>" },
            { "clear_inventory_others", "<color=#e6e3d5>Cleared <color=#8ce4ff>{0}</color>'s inventory.</color>" },
            { "clear_items_cleared", "<color=#e6e3d5>Cleared all dropped items.</color>" },
            { "clear_structures_cleared", "<color=#e6e3d5>Cleared all placed structures and barricades.</color>" },
            { "clear_vehicles_cleared", "<color=#e6e3d5>Cleared all vehicles.</color>" },
            #endregion
            
            #region UCDeaths
            { "zombie", "a zombie" },
            #endregion
            
            #region ShutdownOverrideCommand
            { "shutdown_syntax", "<color=#9cffb3>Corrent syntax: /shutdown <aftergame|*seconds*|instant> <reason>.</color>" },
            { "shutdown_broadcast_after_game", "<color=#00ffff>A shutdown has been scheduled after this game because: \"<color=#6699ff>{0}</color>\".</color>" },
            { "shutdown_broadcast_after_game_daily", "<color=#00ffff>A daily restart will occur after this game. Down-time estimate: <color=#6699ff>2 minutes</color>.</color>" },
            { "shutdown_broadcast_after_game_canceled", "<color=#00ffff>The scheduled shutdown has been canceled.</color>" },
            { "shutdown_broadcast_after_game_canceled_console", "The scheduled shutdown was canceled." },
            { "shutdown_broadcast_after_game_canceled_console_player", "The scheduled shutdown was canceled by {0}." },
            { "shutdown_broadcast_after_time", "<color=#00ffff>A shutdown has been scheduled in {0} because: \"<color=#6699ff>{1}</color>\".</color>" },
            { "shutdown_broadcast_after_game_console", "A shutdown has been scheduled after this game because: \"{0}\"." },
            { "shutdown_broadcast_after_game_reminder", "<color=#00ffff>A shutdown is scheduled to occur after this game because: \"<color=#6699ff>{0}</color>\".</color>" },
            { "shutdown_broadcast_after_game_console_player", "A shutdown has been scheduled after this game by {0} because: \"{1}\"." },
            { "shutdown_broadcast_after_time_console", "A shutdown has been scheduled in {0} because: \"{1}\"." },
            #endregion
            
            #region RequestSigns
            { "kit_name", "<b>{0}</b>" },
            { "kit_weapons", "<b>{0}</b>" },
            { "kit_price_dollars", "$ {0:N2}" },
            { "kit_premium_exclusive", "EXCLUSIVE" },
            { "kit_required_level", "<color=#{1}>{0}</color>" }, // {0} = level number
            { "kit_required_rank", "<color=#{1}>Rank: {0}</color>" },
            { "kit_required_quest", "<color=#{1}>Quest: <color=#ffffff>{0}</color></color>" },
            { "kit_required_quest_unknown", "<color=#{1}>Finish <color=#ffffff>{0}</color> quest{2}</color>" },
            { "kit_required_quest_done", "<color=#ff974d>Kit Unlocked</color>" },
            { "kit_premium_owned", "OWNED" },
            { "kit_cost", "<color=#b8ffc1>C</color> <color=#ffffff>{0}</color>" },
            { "kit_unlimited", "unlimited" },
            { "kit_player_count", "{0}/{1}" },
            { "sign_kit_request", "{0}\n{1}\n{2}\n{3}" },
            { "loadout_name", "LOADOUT {0}\n" },
            { "loadout_name_owned", "" },
            #endregion
            
            #region Vehiclebay Signs
            { "vbs_tickets_postfix", "Tickets" },
            { "vbs_state_ready", "Ready!  <b>/request</b>" },
            { "vbs_state_dead", "{0}:{1}" },
            { "vbs_state_active", "{0}" },
            { "vbs_state_idle", "Idle: {0}:{1}" },
            { "vbs_state_delay_staging", "Locked Until Start" },
            { "vbs_state_delay_time", "Locked: {0}:{1}" },
            { "vbs_state_delay_flags_1", "Capture {0}" },
            { "vbs_state_delay_flags_lose_1", "Lose {0}" },
            { "vbs_state_delay_caches_atk_1", "Destroy {0}" },
            { "vbs_state_delay_caches_atk_undiscovered_1", "Discover Next Cache" },
            { "vbs_state_delay_caches_def_1", "Lose {0}" },
            { "vbs_state_delay_caches_def_undiscovered_1", "Lose Next Cache" },
            { "vbs_state_delay_flags_lose_2+", "Lose {0} more flags" },
            { "vbs_state_delay_flags_2+", "Capture {0} more flags" },
            { "vbs_state_delay_caches_atk_2+", "Destroy {0} more caches" },
            { "vbs_state_delay_caches_def_2+", "Lose {0} more caches" },
            #endregion

            #region ReviveManager
            { "heal_e_notmedic", "<color=#bdae9d>Only a <color=#ff758f>MEDIC</color> can heal or revive teammates.</color>" },
            { "heal_e_enemy", "<color=#bdae9d>You cannot aid enemy soldiers.</color>" },
            #endregion
            
            #region ReloadCommand
            { "reload_syntax", "<color=#ff8c69>Syntax: /reload [help|module].</color>" },
            { "reload_reloaded_all", "<color=#e6e3d5>Reloaded all Uncreated Warfare components.</color>" },
            { "reload_reloaded_translations", "<color=#e6e3d5>Reloaded all translation files.</color>" },
            { "reload_reloaded_flags", "<color=#e6e3d5>Reloaded flag data.</color>" },
            { "reload_reloaded_flags_gm", "<color=#ff8c69>You must be on a flag gamemode to use this command!</color>" },
            { "reload_reloaded_permissions", "<color=#e6e3d5>Reloaded the permission saver file.</color>" },
            { "reload_reloaded_generic", "<color=#e6e3d5>Reloaded the '{0}' config file.</color>" },
            { "reload_reloaded_tcp", "<color=#e6e3d5>Tried to close any existing TCP connection to UCDiscord and re-open it.</color>" },
            { "reload_reloaded_sql", "<color=#e6e3d5>Reopened the MySql Connection.</color>" },
            #endregion
            
            #region Debug Commands
            { "test_no_method", "<color=#ff8c69>No method found called <color=#ff758f>{0}</color>.</color>" },
            { "test_error_executing", "<color=#ff8c69>Ran into an error while executing: <color=#ff758f>{0} - {1}</color>.</color>" },
            { "test_multiple_matches", "<color=#ff8c69>Multiple methods match <color=#ff758f>{0}</color>.</color>" },

            { "test_zonearea_syntax", "<color=#ff8c69>Syntax: <i>/test zonearea [active|all] <show extra zones: true|false> <show path: true|false> <show range: true|false></i>.</color>" },
            { "test_zonearea_started", "<color=#e6e3d5>Picture has to generate, wait around a minute.</color>" },

            { "test_givexp_player_not_found", "<color=#ff8c69>Could not find player named <color=#ff758f>{0}</color></color>" },
            { "test_givexp_success", "<color=#e6e3d5>Given {0} XP to {1}.</color>" },
            { "test_givexp_invalid_amount", "<color=#ff8c69><color=#ff758f>{0}</color> is not a valid amount (Int32).</color>" },

            { "test_givecredits_player_not_found", "<color=#ff8c69>Could not find player named <color=#ff758f>{0}</color></color>" },
            { "test_givecredits_success", "<color=#e6e3d5>Given {0} credits to {1}.</color>" },
            { "test_givecredits_invalid_amount", "<color=#ff8c69><color=#ff758f>{0}</color> is not a valid amount (Int32).</color>" },

            { "test_zone_not_in_zone", "<color=#e6e3d5>No flag zone found at position <color=#4785ff>({0}, {1}, {2})</color> - <color=#4785ff>{3}°</color>, out of <color=#4785ff>{4}</color> registered flags.</color>" },
            { "test_zone_current_zone", "<color=#e6e3d5>You are in flag zone: <color=#4785ff>{0}</color>, at position <color=#4785ff>({1}, {2}, {3})</color>.</color>" },

            { "test_time_enabled_console", "Enabled coroutine timing." },

            { "test_down_success", "<color=#e6e3d5>Applied <color=#8ce4ff>{0}</color> damage to player.</color>" },

            { "test_sign_no_sign", "<color=#ff8c69>No sign found.</color>" },
            { "test_sign_success", "<color=#e6e3d5>Sign text: <color=#8ce4ff>\"{0}\"</color>.</color>" },

            { "test_gamemode_skipped_staging", "<color=#e6e3d5>The staging phase was skipped.</color>" },
            { "test_gamemode_loaded_gamemode", "<color=#e6e3d5>Loaded gamemode: {0}.</color>" },
            { "test_gamemode_failed_loading_gamemode", "<color=#ff8c69>Failed to load gamemode \"{0}\". Check Console.</color>" },
            { "test_gamemode_type_not_found", "<color=#ff8c69>There is no gamemode with the name \"{0}\".</color>" },

            { "test_trackstats_enabled", "<color=#e6e3d5>Tracking stats has been enabled.</color>" },
            { "test_trackstats_disabled", "<color=#e6e3d5>Tracking stats has been disabled.</color>" },

            { "test_destroyblocker_failure", "<color=#ff8c69>Found no zone blockers to destroy.</color>" },
            { "test_destroyblocker_success", "<color=#e6e3d5>Destroyed {0} zone blocker{1}.</color>" },

            { "test_resetlobby_success", "<color=#e6e3d5>Reset {0}'s lobby state.</color>" },

            { "test_instid_not_found", "<color=#ff8c69>An object with an Instance ID was not found.</color>" },
            { "test_instid_found_barricade", "<color=#e6e3d5>Found barricade with instance id {0}.</color>" },
            { "test_instid_found_structure", "<color=#e6e3d5>Found structure with instance id {0}.</color>" },
            { "test_instid_found_vehicle", "<color=#e6e3d5>Found vehicle with instance id {0}.</color>" },
            { "test_instid_found_object", "<color=#e6e3d5>Found level object with instance id {0}.</color>" },

            { "test_playersave_success", "<color=#e6e3d5>Successfully set {1} in {0}'s playersave to {2}.</color>" },
            { "test_playersave_field_not_found", "<color=#ff8c69>Couldn't find a field by the name {0} in PlayerSave.</color>" },
            { "test_playersave_field_protected", "<color=#ff8c69>The field {0} in PlayerSave must have the JsonSettable attribute applied to it to set it.</color>" },
            { "test_playersave_couldnt_parse", "<color=#ff8c69>Couldn't convert {0} to a value {1} can use.</color>" },
            { "test_playersave_not_found", "<color=#ff8c69>A player with that ID has not joined.</color>" },

            #endregion

            #region Phases
            { "phases_briefing", "BRIEFING PHASE" },
            { "phases_preparation", "PREPARATION PHASE" },
            { "phases_invasion_attack", "BRIEFING PHASE" },
            { "phases_invasion_defense", "PREPARATION PHASE\nFORTIFY {0}" },
            #endregion
            
            #region XP Toasts
            { "xp_from_operator", "FROM OPERATOR" },
            { "xp_from_player", "FROM {0}" },
            { "xp_healed_teammate", "HEALED {0}" },
            { "xp_enemy_downed", "<color=#e3e3e3>DOWNED</color>" },
            { "xp_friendly_downed", "<color=#e3e3e3>DOWNED FRIENDLY</color>" },
            { "xp_enemy_killed", "KILLED ENEMY" },
            { "xp_kill_assist", "ASSIST" },
            { "xp_vehicle_assist", "VEHICLE ASSIST" },
            { "xp_driver_assist", "DRIVER ASSIST" },
            { "xp_spotted_assist", "SPOTTER" },
            { "xp_friendly_killed", "TEAMKILLED" },
            { "xp_fob_killed", "FOB DESTROYED" },
            { "xp_fob_teamkilled", "FRIENDLY FOB DESTROYED" },
            { "xp_fob_in_use", "FOB IN USE" },
            { "xp_supplies_unloaded", "RESUPPLIED FOB" },
            { "xp_resupplied_teammate", "RESUPPLIED TEAMMATE" },
            { "xp_repaired_vehicle", "REPAIRED VEHICLE" },
            { "xp_fob_repaired_vehicle", "FOB REPAIRED VEHICLE" },
            { "xp_vehicle_destroyed", "{0} DESTROYED" },
            { "xp_aircraft_destroyed", "{0} SHOT DOWN" },

            { "xp_flag_captured", "FLAG CAPTURED" },
            { "xp_flag_neutralized", "FLAG NEUTRALIZED" },
            { "xp_flag_attack", "ATTACK" },
            { "xp_flag_defend", "DEFENSE" },
            { "xp_cache_killed", "CACHE DESTROYED" },
            { "xp_cache_teamkilled", "FRIENDLY CACHE DESTROYED" },

            { "xp_squad_bonus", "SQUAD BONUS" },
            { "xp_on_duty", "ON DUTY" },

            { "xp_transporting_players", "TRANSPORTING PLAYERS" },

            { "gain_xp", "+{0} XP" },
            { "loss_xp", "-{0} XP" },
            { "gain_credits", "+{0} <color=#b8ffc1>C</color>" },
            { "subtract_credits", "-{0} <color=#b8ffc1>C</color>" },
            { "loss_credits", "-{0} <color=#d69898>C</color>" },
            { "promoted_xp_1", "YOU HAVE BEEN <color=#ffbd8a>PROMOTED</color> TO" },
            { "promoted_xp_2", "{0}" },
            { "demoted_xp_1", "YOU HAVE BEEN <color=#e86868>DEMOTED</color> TO" },
            { "demoted_xp_2", "{0}" },
            #endregion

            #region Injured UI
            { "injured_ui_header", "You are injured" },
            { "injured_ui_give_up", "Press <b>'/'</b> to give up.\n " },
            { "injured_chat", "<color=#ff8c69>You were injured, press <color=#cedcde><plugin_2/></color> to give up.</color>" },
            #endregion
            
            #region Insurgency
            { "insurgency_ui_unknown_attack", "<color=#696969>Undiscovered</color>" },
            { "insurgency_ui_unknown_defense", "<color=#696969>Unknown</color>" },
            { "insurgency_ui_destroyed_attack", "<color=#5a6e5c>Destroyed</color>" },
            { "insurgency_ui_destroyed_defense", "<color=#6b5858>Lost</color>" },
            { "insurgency_ui_cache_attack", "<color=#ffca61>{0}</color> <color=#c2c2c2>{1}</color>" },
            { "insurgency_ui_cache_defense_undiscovered", "<color=#b780d9>{0}</color> <color=#c2c2c2>{1}</color>" },
            { "insurgency_ui_cache_defense_discovered", "<color=#555bcf>{0}</color> <color=#c2c2c2>{1}</color>" },
            { "caches_header", "Caches" },
            #endregion
            
            #region ReportCommand
            { "report_syntax", "<color=#9cffb3>Corrent syntax: /report <player> [\"report reason\"] [custom message...]</color>" },
            { "report_reasons", "<color=#9cffb3>Report reasons: -none-, \"chat abuse\", \"voice chat abuse\", \"soloing vehicles\", \"wasteing assets\", \"teamkilling\", \"fob greifing\".</color>" },
            { "report_discord_not_linked", "<color=#9cffb3>Your account must be linked in our Discord server to use this command. Type <color=#7483c4>/discord</color> then type <color=#ffffff>-link {0}</color> in <color=#c480d9>#warfare-stats</color>.</color>" },
            { "report_player_not_found", "<color=#9cffb3>Unable to find a player with that name, you can use their <color=#ffffff>Steam64 ID</color> instead, as names are only stored until they've been offline for 20 minutes.</color>" },
            { "report_unknown_error", "<color=#9cffb3>Unable to generate a report for an unknown reason, check your syntax again with <color=#ffffff>/report help</color>.</color>" },
            { "report_success_p1", "<color=#c480d9>Successfully reported {0} for <color=#ffffff>{1}</color> as a <color=#00ffff>{2}</color> report.</color>" },
            { "report_success_p2", "<color=#c480d9>If possible please post evidence in <color=#ffffff>#player-reports</color> in our <color=#7483c4>Discord</color> server.</color>" },
            { "report_notify_admin", "<color=#c480d9>{0} reported {1} for <color=#ffffff>{2}</color> as a <color=#00ffff>{3}</color> report.\nCheck <color=#c480d9>#player-reports</color> for more information.</color>" },
            { "report_notify_violator", "<color=#c480d9>You've been reported for <color=#00ffff>{0}</color>.\nCheck <color=#ffffff>#player-reports</color> in our <color=#7483c4>Discord</color> (/discord) for more information and to defend yourself.</color>" },
            { "report_notify_violator_chat_p1", "<color=#c480d9>You've been reported for <color=#00ffff>{0} - {1}</color>.</color>" },
            { "report_notify_violator_chat_p2", "<color=#c480d9>Check <color=#ffffff>#player-reports</color> in our <color=#7483c4>Discord</color> (/discord) for more information and to defend yourself.</color>" },
            { "report_console", "{0} ({1}) reported {2} ({3}) for \"{4}\" as a {5} report." },
            { "report_console_record", "Report against {0} ({1}) record: \"{2}\"" },
            { "report_console_record_failed", "Report against {0} ({1}) failed to send to UCDB." },
            { "report_cooldown", "You've already reported {0} in the past hour." },
            { "report_cancelled", "You did not confirm your report in time." },
            { "report_confirm", "Did you mean to report {1} <i><color=#444444>{0}</color></i>? Type <color=#ff8c69>/confirm</color> to continue." },
            { "report_not_connected", "<color=#ff8c69>The report system is not available right now, please try again later.</color>" },
            #endregion
            
            #region Tips
            { "tip_place_radio", "Place a <color=#ababab>FOB RADIO</color>." },
            { "tip_place_bunker", "Build a <color=#a5c3d9>FOB BUNKER</color> so that your team can spawn." },
            { "tip_unload_supplies", "<color=#d9c69a>DROP SUPPLIES</color> onto the FOB." },
            { "tip_help_build", "<color=#d9c69a>{0} needs help building!</color>" },
            { "tip_logi_resupplied", "Your {0} has been auto resupplied." },
            #endregion

            #region ZoneCommand
            { "zone_syntax", "<color=#ff8c69>Syntax: /zone <visualize|go|edit|list|create|util></color>" },
            { "zone_visualize_no_results", "<color=#ff8c69>You aren't in any existing zone.</color>" },
            { "zone_go_no_results", "<color=#ff8c69>Couldn't find a zone by that name.</color>" },
            { "zone_go_success", "<color=#e6e3d5>Teleported to <color=#5a6e5c>{0}</color>.</color>" },
            { "zone_visualize_success", "<color=#e6e3d5>Spawned {0} particles around <color=#cedcde>{1}</color>.</color>" },
            { "enter_zone_test", "<color=#e6e3d5>You've entered the zone <color=#cedcde>{0}</color>.</color>" },
            { "exit_zone_test", "<color=#e6e3d5>You've exited the zone <color=#cedcde>{0}</color>.</color>" },

            // zone delete
            { "delete_zone_badvalue_self", "<color=#ff8c69>You must be standing in 1 zone (not 0 or multiple). Alternatively, provide a zone name as another argument.</color>" },
            { "delete_zone_badvalue", "<color=#ff8c69>Failed to find a zone named \"{0}\".</color>" },
            { "delete_zone_confirm", "Did you mean to delete <color=#666666>{0}</color>? Type <color=#ff8c69>/confirm</color> to continue." },
            { "delete_zone_success", "<color=#e6e3d5>Deleted <color=#666666>{0}</color>.</color>" },
            { "delete_zone_deleted_working_zone", "<color=#ff8c69>Someone deleted the zone you're working on, saving this will create a new one.</color>" },

            // zone create
            { "create_zone_syntax", "<color=#ff8c69>Syntax: /zone create <polygon|rectangle|circle> <name>.</color>" },
            { "create_zone_success", "<color=#e6e3d5>Started zone builder for {0}, a {1} zone.</color>" },
            { "create_zone_name_taken", "<color=#ff8c69>\"{0}\" is already in use by another zone.</color>" },
            { "create_zone_name_taken_2", "<color=#ff8c69>\"{0}\" is already in use by another zone being created by {1}.</color>" },

            // zone edit
            { "edit_zone_syntax", "<color=#ff8c69>Syntax: /zone edit <existing|maxheight|minheight|finalize|cancel|addpoint|delpoint|clearpoints|setpoint|orderpoint|radius|sizex|sizez|center|name|shortname|type> [value]</color>" },
            { "edit_zone_not_started", "<color=#ff8c69>Start creating a zone with <color=#ffffff>/zone create <polygon|rectangle|circle> <name></color>.</color>" },
            { "edit_zone_finalize_exists", "<color=#ff8c69>There's already a zone saved with that id.</color>" },
            { "edit_zone_finalize_success", "<color=#e6e3d5>Successfully finalized and saved {0}.</color>" },
            { "edit_zone_finalize_failure", "<color=#ff8c69>The provided zone data was invalid because: <color=#ffffff>{0}</color></color>" },
            { "edit_zone_finalize_use_case", "<color=#ff8c69>Before saving you must set a use case with /zone edit use case <type>: \"flag\", \"lobby\", \"t1_main\", \"t2_main\", \"t1_amc\", or \"t2_amc\".</color>" },
            { "edit_zone_finalize_success_overwrite", "<color=#e6e3d5>Successfully overwrote {0}.</color>" },
            { "edit_zone_cancel_success", "<color=#e6e3d5>Successfully cancelled making {0}.</color>" },
            { "edit_zone_finalize_error", "<color=#ff8c69>There was a problem finalizing your zone: \"{0}\".</color>" },
            { "edit_zone_maxheight_badvalue", "<color=#ff8c69>Maximum Height must be a decimal or whole number, or leave it blank to use the player's current height.</color>" },
            { "edit_zone_maxheight_success", "<color=#e6e3d5>Set maximum height to {0}.</color>" },
            { "edit_zone_minheight_badvalue", "<color=#ff8c69>Minimum Height must be a decimal or whole number, or leave it blank to use the player's current height.</color>" },
            { "edit_zone_minheight_success", "<color=#e6e3d5>Set minimum height to {0}.</color>" },
            { "edit_zone_type_badvalue", "<color=#ff8c69>Type must be rectangle, circle, or polygon.</color>" },
            { "edit_zone_type_already_set", "<color=#ff8c69>This zone is already a {0}.</color>" },
            { "edit_zone_type_success", "<color=#e6e3d5>Set type to {0}.</color>" },
            { "edit_zone_addpoint_badvalues", "<color=#ff8c69>Adding a point requires either: blank (appends, current pos), <index> (current pos), <x> <z> (appends), or <index> <x> <z> parameters.</color>" },
            { "edit_zone_addpoint_success", "<color=#e6e3d5>Added point #{0} at {1}.</color>" },
            { "edit_zone_delpoint_badvalues", "<color=#ff8c69>Deleting a point requires either: nearby X and Z parameters, a point number, or leave them blank to use the player's current position.</color>" },
            { "edit_zone_point_number_not_point", "<color=#ff8c69>Point #{0} is not defined.</color>" },
            { "edit_zone_point_none_nearby", "<color=#ff8c69>There is no point near {0}.</color>" },
            { "edit_zone_delpoint_success", "<color=#e6e3d5>Removed point #{0} at {1}.</color>" },
            { "edit_zone_setpoint_badvalues", "<color=#ff8c69>Moving a point requires either: blank (move nearby closer), <nearby src x> <nearby src z> <dest x> <dest z>, <pt num> (destination is player position }, <pt num> <dest x> <dest z>, or <nearby src x> <nearby src z> (destination is nearby player).</color>" },
            { "edit_zone_setpoint_success", "<color=#e6e3d5>Moved point #{0} from {1} to {2}.</color>" },
            { "edit_zone_orderpoint_success", "<color=#e6e3d5>Moved point #{0} to index #{1}.</color>" },
            { "edit_zone_orderpoint_badvalue", "<color=#ff8c69>Ordering a point requires either: <from-index> <to-index>, <to-index> (from is nearby player), or <src x> <src z> <to-index>.</color>" },
            { "edit_zone_radius_badvalue", "<color=#ff8c69>Radius must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.</color>" },
            { "edit_zone_radius_success", "<color=#e6e3d5>Set radius to {0}.</color>" },
            { "edit_zone_sizex_badvalue", "<color=#ff8c69>Size X must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.</color>" },
            { "edit_zone_sizex_success", "<color=#e6e3d5>Set size x to {0}.</color>" },
            { "edit_zone_sizez_badvalue", "<color=#ff8c69>Size Z must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.</color>" },
            { "edit_zone_sizez_success", "<color=#e6e3d5>Set size z to {0}.</color>" },
            { "edit_zone_center_badvalue", "<color=#ff8c69>To set center you must provide two decimal or whole numbers, or leave them blank to use the player's current position.</color>" },
            { "edit_zone_center_success", "<color=#e6e3d5>Set center position to {0}.</color>" },
            { "edit_zone_clearpoints_success", "<color=#e6e3d5>Cleared all polygon points.</color>" },
            { "edit_zone_clearpoints_uncleared", "<color=#e6e3d5>Restored {0} point{1}.</color>" },
            { "edit_zone_name_badvalue", "<color=#ff8c69>Name requires one string argument. Quotation marks aren't required.</color>" },
            { "edit_zone_name_success", "<color=#e6e3d5>Set name to \"{0}\".</color>" },
            { "edit_zone_short_name_badvalue", "<color=#ff8c69>Short name requires one string argument. Quotation marks aren't required.</color>" },
            { "edit_zone_short_name_success", "<color=#e6e3d5>Set short name to \"{0}\".</color>" },
            { "edit_zone_short_name_removed", "<color=#e6e3d5>Removed short name.</color>" },
            { "edit_zone_existing_badvalue", "<color=#ff8c69>Edit existing zone requires the zone name as a parameter. Alternatively stand in the zone (without overlapping another).</color>" },
            { "edit_zone_existing_in_progress", "<color=#ff8c69>Cancel or finalize the zone you're currently editing first.</color>" },
            { "edit_zone_existing_success", "<color=#e6e3d5>Started editing zone {0}, a {1} zone.</color>" },
            { "edit_zone_use_case_badvalue", "<color=#ff8c69>Use case requires one string argument: \"flag\", \"lobby\", \"t1_main\", \"t2_main\", \"t1_amc\", or \"t2_amc\".</color>" },
            { "edit_zone_use_case_success", "<color=#e6e3d5>Set use case to \"{0}\".</color>" },
            { "edit_zone_undo_failure", "<color=#ff8c69>There is nothing to undo.</color>" },
            { "edit_zone_redo_failure", "<color=#ff8c69>There is nothing to redo.</color>" },


            // edit zone ui
            { "edit_zone_ui_suggested_command_1", "/ze maxheight [value]" },
            { "edit_zone_ui_suggested_command_2", "/ze minheight [value]" },
            { "edit_zone_ui_suggested_command_3", "/ze finalize" },
            { "edit_zone_ui_suggested_command_4", "/ze cancel" },
            { "edit_zone_ui_suggested_command_5_p", "/ze addpt [x z]" },
            { "edit_zone_ui_suggested_command_6_p", "/ze delpt [number | x z]" },
            { "edit_zone_ui_suggested_command_7_p", "/ze setpt <number | src: x z | number dest: x z | src: x z dest: x z>" },
            { "edit_zone_ui_suggested_command_8_p", "/ze orderpt <from-index to-index | to-index | src: x z to-index>" },
            { "edit_zone_ui_suggested_command_9_c", "/ze radius [value]" },
            { "edit_zone_ui_suggested_command_10_r", "/ze sizex [value]" },
            { "edit_zone_ui_suggested_command_11_r", "/ze sizez [value]" },
            { "edit_zone_ui_suggested_command_12", "/zone util location" },
            { "edit_zone_ui_suggested_command_13", "/ze type <rectangle | circle | polygon>" },
            { "edit_zone_ui_suggested_command_14_p", "/ze clearpoints" },
            { "edit_zone_ui_suggested_commands", "Suggested Commands" },
            { "edit_zone_ui_y_limits", "Y: {0} - {1}" },
            { "edit_zone_ui_y_limits_infinity", "∞" },

            // zone util
            { "util_zone_syntax", "<color=#ff8c69>Syntax: /zone util <location></color>" },
            { "util_zone_location", "<color=#e6e3d5>Location: {0}, {1}, {2} | Yaw: {3}°.</color>" },
            #endregion

            #region Teams
            { "teams_e_cooldown", "<color=#ff8c69>You can't use /teams for another {0}.</color>" },
            #endregion

            #region Spotting
            { "spotted", "<color=#b9ffaa>SPOTTED</color>" },
            #endregion

            #region VehicleTypes
            { "HUMVEE", "Humvee" },
            { "TRANSPORT", "Transport Truck" },
            { "LOGISTICS", "Logistics Truck" },
            { "SCOUT_CAR", "Scout Car" },
            { "APC", "APC" },
            { "IFV", "IFV" },
            { "MBT", "Tank" },
            { "HELI_TRANSPORT", "Transport Heli" },
            { "HELI_ATTACK", "Attack Heli" },
            { "JET", "Jet" },
            { "EMPLACEMENT", "Emplacement" },
            #endregion

            #region TeleportCommand
            { "tp_target_dead", "<color=#8f9494><color=#{1}>{0}</color> is not alive.</color>" },
            { "tp_entered_vehicle", "<color=#bfb9ac>You were put in <color=#{2}>{1}</color>'s <color=#dddddd>{0}</color>.</color>" },
            { "tp_teleported_player", "<color=#bfb9ac>You were teleported to <color=#{1}>{0}</color>.</color>" },
            { "tp_obstructed_player", "<color=#8f9494>Failed to teleport you to <color=#{1}>{0}</color>, their position is obstructed.</color>" },
            { "tp_location_not_found", "<color=#8f9494>Failed to find a location similar to <color=#dddddd>{0}</color>.</color>" },
            { "tp_teleported_location", "<color=#bfb9ac>You were teleported to <color=#dddddd>{0}</color>.</color>" },
            { "tp_obstructed_location", "<color=#8f9494>Failed to teleport you to <color=#dddddd>{0}</color>, it's position is obstructed.</color>" },
            { "tp_entered_vehicle_other", "<color=#bfb9ac><color=#{4}>{3}</color> was put in <color=#{2}>{1}</color>'s <color=#dddddd>{0}</color>.</color>" },
            { "tp_teleported_player_other", "<color=#bfb9ac><color=#{3}>{2}</color> was teleported to <color=#{1}>{0}</color>.</color>" },
            { "tp_obstructed_player_other", "<color=#8f9494>Failed to teleport <color=#{3}>{2}</color> to <color=#{1}>{0}</color>, their position is obstructed.</color>" },
            { "tp_teleported_location_other", "<color=#bfb9ac><color=#{2}>{1}</color> was teleported to <color=#dddddd>{0}</color>.</color>" },
            { "tp_obstructed_location_other", "<color=#8f9494>Failed to teleport <color=#{2}>{1}</color> to <color=#dddddd>{0}</color>, it's position is obstructed.</color>" },
            { "tp_target_not_found", "<color=#8f9494>Failed to find a player from <color=#dddddd>{0}</color></color>" },
            { "tp_invalid_coordinates", "<color=#8f9494>Use of coordinates should look like: <color=#eeeeee>/tp [player] <x y z></color>.</color>" },
            { "tp_teleported_player_location", "<color=#bfb9ac>You were teleported to <color=#eeeeee>{0}</color>.</color>" },
            { "tp_obstructed_player_location", "<color=#8f9494>Failed to teleport you to <color=#eeeeee>{0}</color>, that point is obstructed.</color>" },
            { "tp_teleported_player_location_other", "<color=#bfb9ac><color=#{2}>{1}</color> was teleported to <color=#eeeeee>{0}</color>.</color>" },
            { "tp_obstructed_player_location_other", "<color=#8f9494>Failed to teleport <color=#{2}>{1}</color> to <color=#eeeeee>{0}</color>, that point is obstructed.</color>" },
            #endregion

            #region HealCommand
            { "heal_player", "<color=#ff9966>You healed <color=#{1}>{0}</color>.</color>" },
            { "heal_self", "<color=#ff9966>You we're healed.</color>" },
            #endregion

            #region GodCommand
            { "god_mode_enabled", "<color=#bfb9ac>God mode enabled.</color>" },
            { "god_mode_disabled", "<color=#bfb9ac>God mode disabled.</color>" },
            #endregion

            #region VanishCommand
            { "vanish_mode_enabled", "<color=#bfb9ac>Vanish mode enabled.</color>" },
            { "vanish_mode_disabled", "<color=#bfb9ac>Vanish mode disabled.</color>" },
            #endregion

            #region PermissionCommand
            { "permissions_current", "<color=#bfb9ac>Current permisions: <color=#ffdf91>{0}</color>.</color>" },
            { "permissions_grant_success", "<color=#bfb9ac><color=#7f8182>{1}</color> <color=#dddddd>({2})</color> is now a <color=#ffdf91>{0}</color>.</color>" },
            { "permissions_grant_already", "<color=#bfb9ac><color=#7f8182>{1}</color> <color=#dddddd>({2})</color> is already at the <color=#ffdf91>{0}</color> level.</color>" },
            { "permissions_revoke_already", "<color=#bfb9ac><color=#7f8182>{0}</color> <color=#dddddd>({1})</color> is already a <color=#ffdf91>member</color>.</color>" },
            { "permissions_revoke_success", "<color=#bfb9ac><color=#7f8182>{0}</color> <color=#dddddd>({1})</color> is now a <color=#ffdf91>member</color>.</color>" },
            #endregion

            #region Win UI
            { "win_ui_value_tickets", "{0} Tickets" },
            { "win_ui_value_caches", "{0} Caches Left" },
            { "win_ui_header_winner", "{0}\r\nhas won the battle!" },
            #endregion
    };
}
