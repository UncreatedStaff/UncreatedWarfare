using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;

namespace Uncreated.Warfare.Commands.CommandSystem;
public sealed class VanillaCommand : IExecutableCommand, IComparable<VanillaCommand>, IComparable<SDG.Unturned.Command>
{
    private readonly EAdminType _allowedUsers;
    public SDG.Unturned.Command Command { get; }
    public CommandStructure? Structure { get; set; }
    SemaphoreSlim? IExecutableCommand.Semaphore { get; set; }
    string IExecutableCommand.CommandName => Command.command;
    EAdminType IExecutableCommand.AllowedPermissions => _allowedUsers;
    bool IExecutableCommand.ExecuteAsynchronously => false;
    int IExecutableCommand.Priority => 0;
    IReadOnlyList<string>? IExecutableCommand.Aliases => null;
    bool IExecutableCommand.Synchronize => false;
    public VanillaCommand(SDG.Unturned.Command cmd)
    {
        Command = cmd;
        _allowedUsers = CommandHandler.GetVanillaPermissions(cmd);
    }

    bool IExecutableCommand.CheckPermission(CommandInteraction ctx)
    {
        if (ctx.IsConsole || ctx.Caller.Player.channel.owner.isAdmin) return true;

        return ctx.Caller.PermissionCheck(_allowedUsers, PermissionComparison.AtLeast);
    }

    void IExecutableCommand.Execute(CommandInteraction interaction)
    {
        Command.check(interaction.CallerCSteamID, Command.command, string.Join("/", interaction.Parameters));
    }
    Task IExecutableCommand.Execute(CommandInteraction interaction, CancellationToken token) => throw new NotImplementedException();
    CommandInteraction IExecutableCommand.SetupCommand(UCPlayer? caller, string[] args, string message, bool keepSlash)
    {
        if (!keepSlash && args.Length > 0)
        {
            ref string end = ref args[args.Length - 1];
            // removes the accidental ending backslash from the last argument
            if (end.Length > 1 && end.EndsWith("\\", StringComparison.Ordinal))
                end = end.Substring(0, end.Length - 1);
        }

        return new CommandInteraction(new CommandInteraction.ContextData(caller, args, message), this);
    }

    int IComparable<SDG.Unturned.Command>.CompareTo(SDG.Unturned.Command other) => Command.CompareTo(other);
    int IComparable<VanillaCommand>.CompareTo(VanillaCommand other) => Command.CompareTo(other.Command);
}
