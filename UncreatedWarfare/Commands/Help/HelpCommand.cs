using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("help", "commands", "cmd", "wtf", "hlep", "hepl", "tutorial", "h", "?"), HideFromHelp]
public sealed class HelpCommand : IExecutableCommand
{
    private readonly CommandDispatcher _commandDispatcher;
    private readonly HelpCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public HelpCommand(TranslationInjection<HelpCommandTranslations> translations, CommandDispatcher commandDispatcher)
    {
        _commandDispatcher = commandDispatcher;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string? commandName))
        {
            SendDefaultHelp();
            return;
        }

        CommandInfo? foundCommand = _commandDispatcher.FindCommand(commandName);

        if (foundCommand == null)
        {
            SendDefaultHelp();
            return;
        }

        ISyntaxWriter writer = CreateSyntaxWriter(false);
        CommandSyntaxFormatter formatter = new CommandSyntaxFormatter(writer, Context.ServiceProvider.GetRequiredService<UserPermissionStore>());

        CommandSyntaxFormatter.SyntaxStringInfo syntaxInfo = await formatter.GetSyntaxString(
            foundCommand,
            Context.Parameters.Slice(1, Context.Parameters.Count - 1),
            Context.Flags.LastOrDefault(),
            Context.Caller,
            commandName,
            token
        );

        writer = CreateSyntaxWriter(true);
        string? description = formatter.GetRichDescription(
            foundCommand,
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

    private void SendDefaultHelp()
    {
        if (_translations.HelpOutputCombined.HasLanguage(Context.Language) && !Context.IMGUI)
        {
            Context.Reply(_translations.HelpOutputCombined);
        }
        else
        {
            Context.Reply(_translations.HelpOutputDiscord);
            Context.Reply(_translations.HelpOutputDeploy);
            Context.Reply(_translations.HelpOutputRequest);
        }
    }
}

public class HelpCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Help";

    [TranslationData("Output from help describing how to use /discord.")]
    public readonly Translation HelpOutputDiscord = new Translation("<#b3ffb3>For more info, join our <#7483c4>Discord</color> server: <#fff>/discord</color>.");

    [TranslationData("Output from help describing how to use /request.")]
    public readonly Translation HelpOutputRequest = new Translation("<#b3ffb3>To get gear, look at a sign in the barracks and type <#fff>/request</color> (or <#fff>/req</color>).");

    [TranslationData("Output from help describing how to use /deploy.")]
    public readonly Translation HelpOutputDeploy = new Translation("<#b3ffb3>To deploy to battle, type <#fff>/deploy <location></color>. The locations are on the left side of your screen.");


    [TranslationData("Output from help describing common things in one message for non-IMGUI users.")]
    public readonly Translation HelpOutputCombined = new Translation("<#b3ffb3>To get gear, look at a sign in the barracks and type <#fff>/request</color> (or <#fff>/req</color>). To deploy to battle, type <#fff>/deploy <location></color> with any of the FOBs listed on the left of your screen. For more info, join our <#7483c4>Discord</color> server: <#fff>/discord</color>.");

}