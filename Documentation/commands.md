# Commands
Commands are created with the following template

`Commands/ShowerCommand.cs`
```cs
// first is the name, the rest are aliases
[Command("shower", "wash"), MetadataFile]
internal sealed class ShowerCommand : IExecutableCommand
{
    public required CommandContext Context { get; init; }

    public ShowerCommand(/* inject services */) { }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        throw Context.SendNotImplemented();
    }
}
```

Commands should also be accompanied by a YAML metadata file which informs the /help menu. The name should be the name of the original file with `.meta.yml` replacing `.cs`. The `MetadataFile` attribute is required on all commands that have metadata files for this to work.

`Commands/ShowerCommand.meta.yml`
```yml
Description: "Take a shower via command."
Aliases: [ "wash" ]
Parameters:
  - Name: duration
    # The command is expecting a TimeSpan
    Type: System.TimeSpan
    Description: "How long to shower for."
    Parameters:
      - Name: temperature
        Type: double
        Optional: true
        Description: "Degrees fahrenheit of the water to shower in."
  - Name: end
    # 'Verbatim' means use the word 'end' directly, not a value
    Type: Verbatim
    Aliases: [ "stop", "off" ]
    Description: "Stop showering."
```

This configuration creates two possible commands:
* `/shower <duration> [temperature]`
* `/shower end`

When used in /help it would show this:
* `/help shower` -> `/shower <duration | end>` (`end` would be in bold)
* `/help shower duration` -> `/shower duration [temperature]`

Parameter properties:

| Property      | Description                                                                                                                                                                                                                                                                                                                                                   | Default Value |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------- |
| Name          | Name of the parameter. This is also the primary alias for Verbatim properties.                                                                                                                                                                                                                                                                                | *required*    |
| Alias/Aliases |  List of alternative names for Verbatim properties.                                                                                                                                                                                                                                                                                                           | *empty list*  |
| Type/Types    | Type of value the parameter takes. Can be a primitive type like `int`, `bool`, etc, a system type like `System.TimeSpan`, a type from Uncreated like `Uncreated.Warfare.Players.IPlayer`, or `Verbatim`, meaning to take the keyword literally (like **favorite** in `/kit favorite <kit id>`). Multiple types can be used when multiple values are accepted. | *required*    |
| Parameters    | List of sub-parameters.                                                                                                                                                                                                                                                                                                                                       | *empty list*  |
| Flags         | List of flags. These are special parameters starting with a hyphen at any location in the command. They usually add special behavior to a command, like `-e` modifies `/v` to enter the vehicle as soon as it spawns. For example, an admin could type `/v -e hatchback` or `/v hatchback -e`                                                                 | *empty list*  |
| Description   | Short description of the parameter.                                                                                                                                                                                                                                                                                                                           | null          |
| Optional      | Whether or not the parameter is optional, showing with square brackets in `/help`.                                                                                                                                                                                                                                                                            | false         |
| Remainder     | Whether or not the parameter includes all parameters after this one including spaces.                                                                                                                                                                                                                                                                         | false         |
| Permission    | Permission required to execute this command. Only required if that branch of the command is more restrictive than the original command.                                                                                                                                                                                                                       | null          |
| Chain         | Number of parameters to chain together as one argument. For example, **x** has a Chain of 3 in `/tp <x y z>`, which otherwise would show as `/tp <x> <y> <z>`                                                                                                                                                                                                 | 0             |

## Attributes

### Command
Required for all commands, defines the name and aliases of the command, and optionally allows you to override the permission name for it.

### SubCommandOf
Creates an automatic parameter relationship between one command and another. So for example, `KitFavoriteCommand` would be a sub-command of `KitCommand`.

### RedirectCommandTo
Redirects one command to another command's execute function. This is especially used when a base command's default operation is one of the subcommands. For example, `/kit hotkey` redirects to `/kit hotkey add`.

### SynchronizedCommand
Only allows one of that command to be executing at once.

### HideFromHelp
Keeps the command from showing up in `/help`, and removes the warning for the command missing a metadata file.

### MetadataFile
Uses a trick with the `[CallerFilePath]` attribute to get the command's original folder location and is required to be able to find the metadata YAML file.

### AndNeedsPermission
Requires an extra permission to allow players to run the command.

### OrNeedsPermission
Adds an alternative permission that allows players to run the command.

### DisableAutoHelp
By default, if you type **help** as the last argument, the system will rearange the command to put help in front. For example, typing `/kit give help` will execute `/help kit give`. This attribute disables this behavior.


## Writing Commands

Commands have access to a CommandContext object for parsing arguments and handling interactions. Explore the different functions on your own but the important ones are:
* `Context.AssertRanByPlayer();`
    * This also marks Context.Player as not null to remove warnings.
* `Context.Get` / `Context.TryGet`
    * Parse an argument.
* `Context.GetRange`
    * Parse a remainder string (multiple arguments at once).
* `Context.MatchParameter`
    * Test a Verbatim parameter.
* `Context.Reply` / `Context.ReplyString`
    * Reply with a translation or raw string.
* `Context.TryGetXyzTarget`
    * Raycast from the player. Returns false if not ran by a player.
* `Context.TryGetSteamId`
    * Get a steam ID parameter from "me" (the current player), a Steam64 ID in any form, or a steam profile link.

Most functions like Reply are thread-safe, and will send the message on the next frame. Check the XML summary to see if it is thread-safe.

```cs
private readonly ShowerService _showerService;

// ...

public ShowerCommand(ShowerService showerService)
{
    _showerService = showerService;
}

public async UniTask ExecuteAsync(CancellationToken token)
{
    Context.AssertRanByPlayer();

    if (Context.MatchParameter(0, "end"))
    {
        // You can throw the results of most functions to reply and return in one line.
        throw Context.ReplyString("<#9cfcb7>Ended shower early.");
    }

    if (!Context.TryGet(0, out string? durationString))
    {
        throw Context.SendCorrectUsage("/shower <duration> [temperature]");
    }

    // parse a string like 8m30s
    TimeSpan duration = FormattingUtility.ParseTimespan(durationString);

    double temperature = 86d;
    if (Context.HasArg(1) && !Context.TryGet(1, out temperature))
    {
        throw Context.ReplyString("<#8f9494>Expected a numeric value for temperature.");
    }

    Context.ReplyString($"<#ffff99>Starting shower at {temperature} degrees fahrenheit...");

    // Log to console
    Context.Logger.LogTrace($"Starting shower for {Context.Player}.");

    await _showerService.ShowerAsync(Context.Player, duration, temperature, token);

    string timeString = FormattingUtility.ToTimeString(duration);
    Context.ReplyString($"<#9cfcb7>Done showering after {timeString}.");
}
```