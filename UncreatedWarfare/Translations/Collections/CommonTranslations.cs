using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations.Addons;

namespace Uncreated.Warfare.Translations.Collections;

public class CommonTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Common";

    [TranslationData("Sent when a command is not used correctly.", "Command usage.")]
    public readonly Translation<string> CorrectUsage = new Translation<string>("<#ff8c69>Correct usage: {0}.");

    [TranslationData("A command or feature hasn't been completed or implemented.")]
    public readonly Translation NotImplemented = new Translation("<#ff8c69>This command hasn't been implemented yet.");

    [TranslationData("A player ran an unknown command.")]
    public readonly Translation UnknownCommand = new Translation("<#ff8c69>Unknown command. <#b3ffb3>Type <#fff>/help</color> to learn more.");

    [TranslationData("A command or feature can only be used by the server console.")]
    public readonly Translation ConsoleOnly = new Translation("<#ff8c69>This command can only be called from console.");

    [TranslationData("A command or feature can only be used by a player (instead of the server console).")]
    public readonly Translation PlayersOnly = new Translation("<#ff8c69>This command can not be called from console.");

    [TranslationData("A command or feature is on cooldown.", "Time until available", "Feature or command name")]
    public readonly Translation<Cooldown, string> CommandCooldown = new Translation<Cooldown, string>("<#ff8c69>You can't use <#fff>{1}</color> for another <#aaa>{0}</color>.", arg0Fmt: Cooldown.FormatTimeLong, arg1Fmt: LowercaseAddon.Instance);

    [TranslationData("A player name or ID search turned up no results.")]
    public readonly Translation PlayerNotFound = new Translation("<#ff8c69>Player not found.");

    [TranslationData("A command didn't respond to an interaction, or a command chose to throw a vague error response to an uncommon problem.")]
    public readonly Translation UnknownError = new Translation("<#ff8c69>We ran into an unknown error executing that command.");

    [TranslationData("A vanilla command didn't print a response.")]
    public readonly Translation VanillaCommandDidNotRespond = new Translation("<#d09595>The vanilla command you ran didn't print a response.");

    [TranslationData("An async command was cancelled mid-execution.")]
    public readonly Translation ErrorCommandCancelled = new Translation("<#ff8c69>This command was cancelled during it's execution. This could be caused by the game ending or a bug.");

    [TranslationData("A command is disabled in the current gamemode type (ex, /deploy in a gamemode without FOBs).")]
    public readonly Translation GamemodeError = new Translation("<#ff8c69>This command is not enabled in this gamemode.");

    [TranslationData("The caller of a command is not allowed to use the command.")]
    public readonly Translation NoPermissions = new Translation("<#ff8c69>You do not have permission to use this command.");

    [TranslationData("The caller of a command is not allowed to use the command.")]
    public readonly Translation<PermissionLeaf> NoPermissionsSpecific = new Translation<PermissionLeaf>("<#ff8c69>You do not have the permission {0} to use this command.");

    [TranslationData("A command or feature is turned off in the configuration or for the current layout.")]
    public readonly Translation NotEnabled = new Translation("<#ff8c69>This feature is not currently enabled.");

    [TranslationData("The caller of a command has permission to use the command but isn't on duty.")]
    public readonly Translation NotOnDuty = new Translation("<#ff8c69>You must be on duty to execute that command.");

    [TranslationData("The value of a parameter was not in a valid time span format.", "Inputted text.")]
    public readonly Translation<string> InvalidTime = new Translation<string>("<#ff8c69><#d09595>{0}</color> should be in a valid <#cedcde>TIME SPAN</color> format. Example: <#d09595>10d12h</color>, <#d09595>4mo15d12h</color>, <#d09595>2y</color>, <#d09595>permanent</color>.", arg0Fmt: WarfarePlayer.FormatCharacterName);
    
    [TranslationData("A player tried to get help with an unknown command.")]
    public readonly Translation UnknownCommandHelp = new Translation("<#ff8c69>Unknown command. <#b3ffb3>Type <#fff>/help <command name></color> to look up a command.");
}