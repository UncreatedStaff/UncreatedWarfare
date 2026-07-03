using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Interaction.Commands.Syntax;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("help", "commands", "cmd", "wtf", "hlep", "hepl", "tutorial", "h", "?"), HideFromHelp]
public sealed class HelpCommand : IExecutableCommand
{
    private readonly CommandDispatcher _commandDispatcher;
    private readonly UserPermissionStore _permissionStore;
    private readonly HelpCommandTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public HelpCommand(
        TranslationInjection<HelpCommandTranslations> translations,
        CommandDispatcher commandDispatcher,
        UserPermissionStore permissionStore)
    {
        _commandDispatcher = commandDispatcher;
        _permissionStore = permissionStore;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string? commandName))
        {
            await SendDefaultHelp();
            return;
        }

        CommandInfo? foundCommand = _commandDispatcher.FindCommand(commandName);

        if (foundCommand == null)
        {
            await SendDefaultHelp();
            return;
        }

        ISyntaxWriter writer = CreateSyntaxWriter(false);
        CommandSyntaxFormatter formatter = new CommandSyntaxFormatter(writer, _permissionStore);

        CommandSyntaxFormatter.SyntaxStringInfo syntaxInfo = await formatter.GetSyntaxString(
            foundCommand,
            Context.Parameters.Slice(1, Context.Parameters.Count - 1),
            Context.Flags.LastOrDefault().FlagName,
            Context.Caller,
            commandName,
            token
        );

        writer = CreateSyntaxWriter(true);
        string? description = formatter.GetRichDescription(
            syntaxInfo.TargetParameter,
            syntaxInfo.TargetFlag,
            writer,
            Context.Language
        );

        Color richTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        Context.ReplyString(syntaxInfo.Syntax, richTextColor);

        if (!string.IsNullOrWhiteSpace(description))
        {
            Context.ReplyString(description, richTextColor);
        }
    }

    private ISyntaxWriter CreateSyntaxWriter(bool close)
    {
        if (Context.Caller.IsTerminal)
        {
            return new TerminalSyntaxWriter(close, Context.Culture, Context.ServiceProvider.GetRequiredService<ITranslationService>().TerminalColoring);
        }

        if (Context.Player != null)
        {
            return Context.IMGUI ? new IMGUISyntaxWriter(Context.Culture) : new TMProSyntaxWriter(close, Context.Culture);
        }

        return new PlainTextSyntaxWriter(Context.Culture);
    }

    private async Task SendDefaultHelp()
    {
        Color richTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        if (Context.Player is { IsOnDuty: true } || Context.Caller.IsTerminal || Context.MatchFlag('c', "commands"))
        {
            if (Context.IMGUI)
            {
                Context.ReplyString("Not supported in IMGUI mode, switch to uGUI or UIToolkit instead in /options.");
                return;
            }

            StringBuilder bldr = new StringBuilder();

            foreach (CommandInfo command in _commandDispatcher.Commands)
            {
                if (command.HideFromHelp || command.HideFromCommandList)
                    continue;

                if (!await _commandDispatcher.HasPermissionForCommand(command, Context.Caller, Context.Token))
                    continue;

                if (bldr.Length > 0 && !Context.Caller.IsTerminal)
                {
                    bldr.AppendLine();
                }

                ISyntaxWriter writer = CreateSyntaxWriter(true);
                CommandSyntaxFormatter formatter = new CommandSyntaxFormatter(writer, _permissionStore);

                CommandSyntaxFormatter.SyntaxStringInfo str = await formatter.GetSyntaxString(command, Array.Empty<string>(), null, Context.Caller, token: Context.Token);

                if (Context.Caller.IsTerminal)
                    bldr.AppendLine();

                bldr.AppendLine(str.Syntax);

                writer = CreateSyntaxWriter(true);
                string? description = formatter.GetRichDescription(command.Metadata, null, writer, Context.Language);
                if (!string.IsNullOrEmpty(description))
                    bldr.AppendLine(description);

                if (bldr.Length > 1024)
                {
                    Context.ReplyString(bldr.ToString(), richTextColor);
                    bldr.Clear();
                }
            }

            if (bldr.Length > 0)
            {
                Context.ReplyString(bldr.ToString(), richTextColor);
            }
            return;
        }

        if (_translations.HelpOutputCombined.HasLanguage(Context.Language) && !Context.IMGUI)
        {
            Context.Reply(_translations.HelpOutputCombined);
        }
        else
        {
            Context.Reply(_translations.HelpOutputDiscord);
            Context.Reply(_translations.HelpOutputCommands);
            Context.Reply(_translations.HelpOutputInfo);
        }
    }
}

public class HelpCommandTranslations : TranslationCollection
{
    public override string Name => "Commands/Help";

    [TranslationData("Output from help describing how to use /discord.")]
    public readonly Translation HelpOutputDiscord = new Translation("<#b3ffb3>For more info, join our <#7483c4>Discord</color> server: <#fff>/discord</color>.");

    [TranslationData("Output from help describing how to use /deploy.")]
    public readonly Translation HelpOutputCommands = new Translation("<#b3ffb3>Type <#fff>/help -c</color> to see all available commands.");

    [TranslationData("Output from help describing how to use /deploy.")]
    public readonly Translation HelpOutputInfo = new Translation("<#b3ffb3>Talk to the NPCs around spawn to get started.");

    [TranslationData("Output from help describing common things in one message for non-IMGUI users.")]
    public readonly Translation HelpOutputCombined = new Translation("<#b3ffb3>Talk to the NPCs around spawn to get started. Type <#fff>/help -c</color> to see all available commands. For more info, join our <#7483c4>Discord</color> server: <#fff>/discord</color>.");

}