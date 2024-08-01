using DanielWillett.ReflectionTools;
using System;
using System.Reflection;
using Uncreated.Warfare.Commands.Dispatch;

namespace Uncreated.Warfare.Commands;

[Command("help", "commands", "tutorial", "h"), Priority(1)]
[HelpMetadata(nameof(GetHelpMetadata))]
public sealed class HelpCommand : IExecutableCommand
{
    private readonly CommandDispatcher _dispatcher;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public HelpCommand(CommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Offers assistance with command syntax and information.",
            Parameters =
            [
                new CommandParameter("Command", typeof(IExecutableCommand))
                {
                    Description = "See specific information about a command. Type arguments to see more.",
                    IsOptional = true,
                    Parameters =
                    [
                        new CommandParameter("Arguments", typeof(object))
                        {
                            IsOptional = true,
                            IsRemainder = true
                        }
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string range))
        {
            SendDefaultHelp();
            return;
        }

        CommandType? cmd = _dispatcher.FindCommand(range);

        if (cmd == null)
        {
            Context.Reply(T.UnknownCommandHelp);
            return;
        }

        cmd.Structure?.OnHelpCommand(Context, cmd); // will throw exception if it has data

        await CommandDispatcher.AssertPermissions(cmd, Context, token);
        await UniTask.SwitchToMainThread(token);

        if (cmd.VanillaCommand != null)
        {
            if (cmd.VanillaCommand.help != null)
            {
                Context.ReplyString(cmd.VanillaCommand.help, "b3ffb3");
            }

            if (cmd.VanillaCommand.info != null)
            {
                Context.ReplyString(cmd.VanillaCommand.info, "b3ffb3");
            }

            if (Context.Responded)
                return;
        }

        Type type = cmd.GetType();

        FieldInfo? f1 = type.GetField("Help", BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? f2 = type.GetField("Syntax", BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        string? rtn = null;

        if (f1 != null && f1.GetValue(null) is string help)
        {
            rtn = help;
        }

        if (f2 != null && f2.GetValue(null) is string syntax)
        {
            if (rtn == null)
                throw Context.ReplyString(syntax);
            
            Context.ReplyString(rtn);
            throw Context.ReplyString(syntax);

        }

        if (rtn != null)
            throw Context.ReplyString(rtn);
    }

    private void SendDefaultHelp()
    {
        if (T.HelpOutputCombined.HasLanguage(Context.Language) && !Context.IMGUI)
        {
            Context.Reply(T.HelpOutputCombined);
        }
        else
        {
            Context.Reply(T.HelpOutputDiscord);
            Context.Reply(T.HelpOutputDeploy);
            Context.Reply(T.HelpOutputRequest);
        }
    }
}
