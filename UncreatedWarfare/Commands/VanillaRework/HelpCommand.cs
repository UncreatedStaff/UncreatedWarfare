using System;
using System.Reflection;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public sealed class HelpCommand : Command
{
    private const string Syntax = "/help [command] [arguments...]";
    private const string Help = "Offers assistance with command syntax.";

    public HelpCommand() : base("help", EAdminType.MEMBER, 1)
    {
        AddAlias("commands");
        AddAlias("tutorial");
        Structure = new CommandStructure
        {
            Description = Help,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Command", typeof(IExecutableCommand))
                {
                    Description = "See specific information about a command. Type arguments to see more.",
                    IsOptional = true,
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Arguments", typeof(object))
                        {
                            IsOptional = true,
                            IsRemainder = true
                        }
                    }
                }
            }
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
        if (ctx.TryGet(0, out string range))
        {
            IExecutableCommand? cmd = CommandHandler.FindCommand(range);

            if (cmd != null)
            {
                cmd.Structure?.OnHelpCommand(ctx, cmd); // will throw exception if it has data

                if (!cmd.CheckPermission(ctx))
                    throw ctx.SendNoPermission();

                if (cmd is VanillaCommand vcmd)
                {
                    if (vcmd.Command.help != null)
                        ctx.ReplyString(vcmd.Command.help, "b3ffb3");
                    if (vcmd.Command.info != null)
                        ctx.ReplyString(vcmd.Command.info, "b3ffb3");

                    if (ctx.Responded) return;
                }

                Type type = cmd.GetType();
                FieldInfo? f1 = type.GetField("Help",
                    BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo? f2 = type.GetField("Syntax",
                    BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                string? rtn = null;

                if (f1 != null && f1.GetValue(null) is string help)
                    rtn = help;
                if (f2 != null && f2.GetValue(null) is string syntax)
                {
                    if (rtn != null)
                    {
                        ctx.ReplyString(rtn);
                        throw ctx.ReplyString(syntax);
                    }

                    throw ctx.ReplyString(syntax);
                }
                
                if (rtn != null)
                    throw ctx.ReplyString(rtn);

                return;
            }
        }

        if (T.HelpOutputCombined.HasLanguage(ctx.LanguageInfo) && !ctx.IMGUI)
        {
            ctx.Reply(T.HelpOutputCombined);
        }
        else
        {
            ctx.Reply(T.HelpOutputDiscord);
            ctx.Reply(T.HelpOutputDeploy);
            ctx.Reply(T.HelpOutputRequest);
        }
    }
}
